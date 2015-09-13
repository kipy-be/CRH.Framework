using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using CRH.Framework.Common;
using CRH.Framework.IO;
using CRH.Framework.Utils;

namespace CRH.Framework.Disk
{
    public sealed class DiskReader : DiskBase
    {
        private CBinaryReader m_stream;
        private DiskIndex m_index;

        private bool m_descriptorsRead;
        private bool m_indexBuilt;

        private static Regex m_regFileName = new Regex("(.+?)(?:;[0-9]+)?$");

    // Constructors

        /// <summary>
        /// DiskReader
        /// </summary>
        /// <param name="fileUrl">Path to the ISO file to read</param>
        /// <param name="type">Type of ISO (ISO9660 / ISO9660_UDF)</param>
        /// <param name="mode">Disk mode</param>
        /// <param name="sectorSize">The sector size (default depends on mode)</param>
        /// <param name="readDescriptors">Read descriptors immediately</param>
        /// <param name="buildIndex">Build the index cache immediately</param>
        public DiskReader(string fileUrl, IsoType type, DiskMode mode, int sectorSize = -1, bool readDescriptors = true, bool buildIndex = true)
            : base(fileUrl, type, mode, sectorSize)
        {
            m_descriptorsRead = false;
            m_indexBuilt      = false;

            try
            {
                m_file       = new FileInfo(m_fileUrl);
                m_fileStream = new FileStream(m_file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                m_stream     = new CBinaryReader(m_fileStream);
                m_fileOpen   = true;

                if (readDescriptors)
                    ReadVolumeDescriptors();

                if (buildIndex)
                    BuildIndex();

                m_stream.Position = 0;
            }
            catch (FrameworkException ex)
            {
                throw ex;
            }
            catch (Exception)
            {
                throw new FrameworkException("Error while reading ISO : Unable to open the ISO File");
            }
        }

    // Methods

        /// <summary>
        /// Close the file and dispose it
        /// </summary>
        public override void Close()
        {
            if (!m_fileOpen)
                return;

            m_stream.CloseAndDispose();
            m_fileOpen = false;
        }

        /// <summary>
        /// Read a sector
        /// </summary>
        /// <param name="mode">Sector's mode</param>
        public DiskSector ReadSector(SectorMode mode)
        {
            try
            {
                DiskSector sector = new DiskSector(mode, m_sectorSize);

                if (mode == SectorMode.MODE1 || mode == SectorMode.MODE2 || mode == SectorMode.XA_FORM1 || mode == SectorMode.XA_FORM2)
                {
                    m_stream.Read(sector.Sync, 0, DiskSector.SYNC_SIZE);
                    if (!sector.Sync.IsEquals(DiskSector.SYNC))
                        throw new FrameworkException("Error while reading sector : sync is invalid");

                    m_stream.Read(sector.Header, 0, DiskSector.HEADER_SIZE);
                }

                if (mode == SectorMode.XA_FORM1 || mode == SectorMode.XA_FORM2)
                {
                    m_stream.Read(sector.SubHeader, 0, DiskSector.SUBHEADER_SIZE / 2);
                    if (!sector.SubHeader.IsEquals(m_stream.ReadBytes(DiskSector.SUBHEADER_SIZE / 2)))
                        throw new FrameworkException("Error while reading sector : subheader is invalid");
                }

                m_stream.Read(sector.Data, 0, sector.DataLength);

                if (mode == SectorMode.MODE1 || mode == SectorMode.XA_FORM1)
                    m_stream.Read(sector.Edc, 0, DiskSector.EDC_SIZE);

                if (mode == SectorMode.MODE1)
                    m_stream.Read(sector.Intermediate, 0, DiskSector.INTERMEDIATE_SIZE);

                if (mode == SectorMode.MODE1 || mode == SectorMode.XA_FORM1)
                {
                    m_stream.Read(sector.EccP, 0, DiskSector.ECC_P_SIZE);
                    m_stream.Read(sector.EccQ, 0, DiskSector.ECC_Q_SIZE);
                }

                if (mode == SectorMode.XA_FORM2)
                    m_stream.Read(sector.Edc, 0, DiskSector.EDC_SIZE);

                return sector;
            }
            catch (FrameworkException ex)
            {
                throw ex;
            }
            catch (EndOfStreamException)
            {
                throw new FrameworkException("Errow while reading sector : end of file occured");
            }
            catch (Exception)
            {
                throw new FrameworkException("Errow while reading sector : unable to read sector");
            }
        }

        /// <summary>
        /// Read a sector
        /// </summary>
        /// <param name="lba">Sector's LBA to read</param>
        /// <param name="mode">Sector's mode</param>
        public DiskSector ReadSector(long lba, SectorMode mode)
        {
            SeekSector(lba);
            return ReadSector(mode);
        }

        /// <summary>
        /// Read several consecutives sectors
        /// </summary>
        /// <param name="count">Number of sectors to read</param>
        /// <param name="mode">Sector's mode</param>
        /// <returns></returns>
        public DiskSector[] ReadSectors(int count, SectorMode mode)
        {
            DiskSector[] sectors = new DiskSector[count];

            for (int i = 0; i < count; i++)
                sectors[i] = ReadSector(mode);

            return sectors;
        }

        /// <summary>
        /// Read several consecutives sectors
        /// </summary>
        /// <param name="lba">Starting sector's LBA</param>
        /// <param name="count">Number of sectors to read</param>
        /// <param name="mode">Sector's mode</param>
        /// <returns></returns>
        public DiskSector[] ReadSectors(long lba, int count, SectorMode mode)
        {
            SeekSector(lba);
            return ReadSectors(count, mode);
        }

        /// <summary>
        /// Read a sector's data (only data : does not include modes specifics fields)
        /// </summary>
        /// <param name="mode">Sector's mode</param>
        private byte[] ReadSectorData(SectorMode mode)
        {
            try
            {
                byte[] data;

                if (mode == SectorMode.MODE1 || mode == SectorMode.MODE2 || mode == SectorMode.XA_FORM1 || mode == SectorMode.XA_FORM2)
                    m_stream.Position += (DiskSector.SYNC_SIZE + DiskSector.HEADER_SIZE);

                if (mode == SectorMode.XA_FORM1 || mode == SectorMode.XA_FORM2)
                    m_stream.Position += DiskSector.SUBHEADER_SIZE;

                data = m_stream.ReadBytes(DiskSector.GetDataSize(m_sectorSize, mode));

                if (mode == SectorMode.MODE1 || mode == SectorMode.XA_FORM1)
                    m_stream.Position += DiskSector.EDC_SIZE;

                if (mode == SectorMode.MODE1)
                    m_stream.Position += DiskSector.INTERMEDIATE_SIZE;

                if (mode == SectorMode.MODE1 || mode == SectorMode.XA_FORM1)
                    m_stream.Position += DiskSector.ECC_SIZE;

                if (mode == SectorMode.XA_FORM2)
                    m_stream.Position += DiskSector.EDC_SIZE;

                return data;
            }
            catch (EndOfStreamException)
            {
                throw new FrameworkException("Errow while reading sector : end of file occured");
            }
            catch (Exception)
            {
                throw new FrameworkException("Errow while reading sector : unable to read sector");
            }
        }

        /// <summary>
        /// Read a sector's data (only data : does not include modes specifics fields)
        /// </summary>
        /// <param name="lba">Sector's LBA to read</param>
        /// <param name="mode">Sector's mode</param>
        public byte[] ReadSectorData(long lba, SectorMode mode)
        {
            SeekSector(lba);
            return ReadSectorData(mode);
        }

        /// <summary>
        /// Read several consecutives sectors data (only data : does not include modes specifics fields)
        /// </summary>
        /// <param name="count">Number of sectors to read</param>
        /// <param name="mode">Sector's mode</param>
        /// <returns></returns>
        public byte[] ReadSectorsData(int count, SectorMode mode)
        {
            int dataSize = DiskSector.GetDataSize(m_sectorSize, mode);
            byte[] data = new byte[count * dataSize];

            for (int i = 0, offset = 0; i < count; i++, offset += dataSize)
                CBuffer.Copy(ReadSectorData(mode), data, 0, offset);

            return data;
        }

        /// <summary>
        /// Read several consecutives sectors data (only data : does not include modes specifics fields)
        /// </summary>
        /// <param name="lba">Starting sector's LBA</param>
        /// <param name="count">Number of sectors to read</param>
        /// <param name="mode">Sector's mode</param>
        /// <returns></returns>
        public byte[] ReadSectorsData(long lba, int count, SectorMode mode)
        {
            SeekSector(lba);
            return ReadSectorsData(count, mode);
        }

        /// <summary>
        /// Read a file
        /// </summary>
        /// <param name="lba">Sector's lba</param>
        /// <param name="size">The size of the file</param>
        /// <param name="mode">Sector's mode</param>
        /// <param name="stream">The stream to write the data</param>
        private void ReadFile(long lba, long size, SectorMode mode, Stream stream)
        {
            int sectorDataSize = DiskSector.GetDataSize(m_sectorSize, mode);
            long bytesRead = 0;
            SeekSector(lba);

            long remaining;
            while (bytesRead < size)
            {
                remaining = size - bytesRead;
                if (remaining >= sectorDataSize)
                    stream.Write(ReadSectorData(mode), 0, sectorDataSize);
                else
                    stream.Write(ReadSectorData(mode), 0, (int)remaining);

                bytesRead += sectorDataSize;
            }

            stream.Flush();
        }

        /// <summary>
        /// Read a file
        /// </summary>
        /// <param name="filePath">The full file path of the file (eg : /FOO/BAR/FILE.EXT)</param>
        /// <param name="stream">The stream to write the data</param>
        public void ReadFile(string filePath, Stream stream)
        {
            if (!m_indexBuilt)
                throw new FrameworkException("Error : You must build the index cache first");

            try
            {
                DiskIndexEntry entry = m_index.GetEntry(filePath);
                if (entry == null)
                    throw new FrameworkException("File not found : unable to find file \"{0}\"", filePath);

                if (entry.IsDIrectory)
                    throw new FrameworkException("Not a file : specified path seems to be a directory, not a file", filePath);

                SectorMode mode = (m_mode != DiskMode.MODE2_XA)
                                    ? m_defaultSectorMode
                                    : entry.DirectoryEntry.XaEntry.IsMode2Form1
                                        ? SectorMode.XA_FORM1
                                        : SectorMode.XA_FORM2;

                ReadFile(entry.DirectoryEntry.ExtentLba, entry.DirectoryEntry.ExtentSize, mode, stream);
            }
            catch(FrameworkException ex)
            {
                throw ex;
            }
            catch (Exception)
            {
                throw new FrameworkException("Error while reading file : unable to read file \"{0}\"", filePath);
            }
        }

        /// <summary>
        /// Extract a file
        /// </summary>
        /// <param name="filePath">The full path of the disk's file (eg : /FOO/BAR/FILE.EXT)</param>
        /// <param name="stream">The full path of the</param>
        public void ExtractFile(string filePath, string outFilePath)
        {
            if (!m_indexBuilt)
                throw new FrameworkException("Error : You must build the index cache first");

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outFilePath));
                using (FileStream fs = new FileStream(outFilePath, FileMode.Create, FileAccess.Write))
                {
                    ReadFile(filePath, fs);
                }
            }
            catch (FrameworkException ex)
            {
                throw ex;
            }
            catch (Exception)
            {
                throw new FrameworkException("Error while writing file : unable to write file \"{0}\"", outFilePath);
            }
        }

        /// <summary>
        /// Check if a file exists
        /// </summary>
        /// <param name="filePath">The full file path of the file (eg : /FOO/BAR/FILE.EXT)</param>
        /// <returns></returns>
        public bool FileExists(string filePath)
        {
            if (!m_indexBuilt)
                throw new FrameworkException("Error : You must build the index cache first");

            return m_index.GetEntry(filePath) != null;
        }

        /// <summary>
        /// Read the disk descriptors
        /// </summary>
        public void ReadVolumeDescriptors()
        {
            if (m_descriptorsRead)
                return;

            try
            {
                VolumeDescriptor descriptor;
                bool endOfList = false;
                bool hasPrimaryDescriptor = false;
                SeekSector(16);

                do
                {
                    using (CBinaryReader stream = new CBinaryReader(ReadSectorData(m_defaultSectorMode)))
                    {
                        descriptor = ReadVolumeDescriptor(stream);
                    }

                    switch (descriptor.Type)
                    {
                        case VolumeDescriptorType.PRIMARY:
                            m_primaryVolumeDescriptor = (PrimaryVolumeDescriptor)descriptor;
                            hasPrimaryDescriptor = true;
                            break;
                        case VolumeDescriptorType.SET_TERMINATOR:
                            endOfList = true;
                            break;
                    }
                } while (!endOfList);

                m_descriptorsRead = true;

                if (!hasPrimaryDescriptor)
                    throw new FrameworkException("Error while reading volume descriptors : no primary descriptor found");
            }
            catch (FrameworkException ex)
            {
                throw ex;
            }
            catch (Exception)
            {
                throw new FrameworkException("Error while reading volume descriptors : Invalid list of descriptors");
            }
        }

        /// <summary>
        /// Read a volume descriptor according to its type
        /// </summary>
        /// <param name="stream">The stream to read</param>
        private VolumeDescriptor ReadVolumeDescriptor(CBinaryReader stream)
        {
            try
            {
                byte descriptorType = stream.ReadByte();
                string id = stream.ReadAsciiString(5);

                if (id != VolumeDescriptor.VOLUME_ID)
                    throw new FrameworkException("Error while reading volume descriptors : ISO9660 Volume descriptor was expected");

                switch (descriptorType)
                {
                    case (byte)VolumeDescriptorType.PRIMARY:
                        return ReadPrimaryVolumeDescriptor(stream);
                    case (byte)VolumeDescriptorType.SET_TERMINATOR:
                        return new SetTerminatorVolumeDescriptor();
                    case (byte)VolumeDescriptorType.BOOT:
                    case (byte)VolumeDescriptorType.PARTITION:
                    case (byte)VolumeDescriptorType.SUPPLEMENTARY:
                        throw new FrameworkNotSupportedException("Error while reading volume descriptors : Only primary volume descriptor is currently supported");
                    default:
                        throw new FrameworkException("Error while reading volume descriptors : ISO9660 volume descriptor was expected");
                }
            }
            catch (FrameworkException ex)
            {
                throw ex;
            }
            catch (Exception)
            {
                throw new FrameworkException("Error while reading VolumeDescriptor : Invalid ISO9660 volume descriptor");
            }
        }

        /// <summary>
        /// Read a primary volume descriptor
        /// </summary>
        /// <param name="stream">The stream to read</param>
        private PrimaryVolumeDescriptor ReadPrimaryVolumeDescriptor(CBinaryReader stream)
        {
            PrimaryVolumeDescriptor descriptor;

            try
            {
                byte version = stream.ReadByte();

                descriptor = new PrimaryVolumeDescriptor(version);

                descriptor.Unused1 = stream.ReadByte();
                descriptor.SystemId = stream.ReadAsciiString(32);
                descriptor.VolumeId = stream.ReadAsciiString(32);
                descriptor.Unused2 = stream.ReadBytes(8);

                descriptor.VolumeSpaceSize = stream.ReadUInt32();
                if (descriptor.VolumeSpaceSize != stream.ReadUInt32BE())
                    throw new FrameworkException("Error while reading PrimaryVolumeDescriptor : VolumeSpaceSize is not valid");

                descriptor.Unused3 = stream.ReadBytes(32);

                descriptor.VolumeSetSize = stream.ReadUInt16();
                if (descriptor.VolumeSetSize != stream.ReadUInt16BE())
                    throw new FrameworkException("Error while reading PrimaryVolumeDescriptor : VolumeSetSize is not valid");

                descriptor.VolumeSequenceNumber = stream.ReadUInt16();
                if (descriptor.VolumeSequenceNumber != stream.ReadUInt16BE())
                    throw new FrameworkException("Error while reading PrimaryVolumeDescriptor : VolumeSequenceNumber  is not valid");

                descriptor.LogicalBlockSize = stream.ReadUInt16();
                if (descriptor.LogicalBlockSize != stream.ReadUInt16BE())
                    throw new FrameworkException("Error while reading PrimaryVolumeDescriptor : LogicalBlockSize  is not valid");

                descriptor.PathTableSize = stream.ReadUInt32();
                if (descriptor.PathTableSize != stream.ReadUInt32BE())
                    throw new FrameworkException("Error while reading PrimaryVolumeDescriptor : PathTableSize  is not valid");

                descriptor.TypeLPathTableLBA = stream.ReadUInt32();
                descriptor.OptTypeLPathTableLBA = stream.ReadUInt32();
                descriptor.TypeMPathTableLBA = stream.ReadUInt32BE();
                descriptor.OptTypeMPathTableLBA = stream.ReadUInt32BE();
                descriptor.RootDirectoryEntry = ReadDirectoryEntry(stream);

                // TODO : cas des fichiers
                descriptor.VolumeSetId = stream.ReadAsciiString(128);
                descriptor.PublisherId = stream.ReadAsciiString(128);
                descriptor.PreparerId = stream.ReadAsciiString(128);
                descriptor.ApplicationId = stream.ReadAsciiString(128);
                descriptor.CopyrightFileId = stream.ReadAsciiString(38);
                descriptor.AbstractFileId = stream.ReadAsciiString(36);
                descriptor.BibliographicFileId = stream.ReadAsciiString(37);
                //

                descriptor.CreationDate = VolumeDescriptor.ToDateTime(stream.ReadBytes(17));
                descriptor.ModificationDate = VolumeDescriptor.ToDateTime(stream.ReadBytes(17));
                descriptor.ExpirationDate = VolumeDescriptor.ToDateTime(stream.ReadBytes(17));
                descriptor.EffectiveDate = VolumeDescriptor.ToDateTime(stream.ReadBytes(17));
                descriptor.FileStructureVersion = stream.ReadByte();
                descriptor.Unused4 = stream.ReadByte();
                descriptor.ApplicationData = stream.ReadBytes(512);
                descriptor.Reserved = stream.ReadBytes(653);

                // if the disk is CDROM/XA (and then contains an XaEntry in his DirectoryEntries),
                // "CD-XA001" can be read at offset 0x400 of the pvd (actually offset 0x8D of ApplicationData field)
                if (CBuffer.ReadAsciiString(descriptor.ApplicationData, 0x8D, 8) == VolumeDescriptor.VOLUME_XA)
                    m_isXa = true;
            }
            catch (FrameworkException ex)
            {
                throw ex;
            }
            catch (Exception)
            {
                throw new FrameworkException("Error while reading PrimaryVolumeDescriptor : PrimaryVolumeDescriptor is not valid");
            }

            return descriptor;
        }

        /// <summary>
        /// Read a directory entry
        /// </summary>
        /// <param name="stream">The stream to read</param>
        private DirectoryEntry ReadDirectoryEntry(CBinaryReader stream)
        {
            DirectoryEntry entry = null;

            try
            {
                long position = stream.Position;

                entry = new DirectoryEntry();
                entry.Length = stream.ReadByte();
                entry.ExtendedAttributeRecordlength = stream.ReadByte();

                entry.ExtentLba = stream.ReadUInt32();
                if (entry.ExtentLba != stream.ReadUInt32BE())
                    throw new FrameworkException("Error while reading DirectoryEntry : ExtentLBA is not valid");

                entry.ExtentSize = stream.ReadUInt32();
                if (entry.ExtentSize != stream.ReadUInt32BE())
                    throw new FrameworkException("Error while reading DirectoryEntry : ExtentSize is not valid");

                byte[] buffer = stream.ReadBytes(7);
                entry.Date = new DateTime(buffer[0] + 1900, buffer[1], buffer[2], buffer[3], buffer[4], buffer[5], DateTimeKind.Utc);

                entry.Flags = stream.ReadByte();
                entry.FileUnitSize = stream.ReadByte();
                entry.Interleave = stream.ReadByte();

                entry.VolumeSequenceNumber = stream.ReadUInt16();
                if (entry.VolumeSequenceNumber != stream.ReadUInt16BE())
                    throw new FrameworkException("Error while reading DirectoryEntry : VolumeSequenceNumber is not valid");

                entry.NameLength = stream.ReadByte();
                entry.Name = m_regFileName.Match(stream.ReadAsciiString(entry.NameLength, false)).Groups[1].Value;

                if (entry.NameLength % 2 == 0)
                    stream.Position += 1;

                if (m_isXa && (stream.Position != position + entry.Length))
                {
                    entry.XaEntry = new XaEntry();
                    entry.XaEntry.GroupId = stream.ReadUInt16BE();
                    entry.XaEntry.UserId = stream.ReadUInt16BE();
                    entry.XaEntry.Attributes = stream.ReadUInt16BE();

                    entry.XaEntry.Signature = stream.ReadAsciiString(2);
                    if (entry.XaEntry.Signature != XaEntry.XA_SIGNATURE)
                        throw new FrameworkException("Error while reading DirectoryEntry : XaEntry is not valid");

                    entry.XaEntry.FileNumber = stream.ReadByte();
                    entry.XaEntry.Unused = stream.ReadBytes(5);
                }
            }
            catch (FrameworkException ex)
            {
                throw ex;
            }
            catch (Exception)
            {
                throw new FrameworkException("Error while reading DirectoryEntry : DirectoryEntry is not valid");
            }

            return entry;
        }

        /// <summary>
        /// Read a path table entry
        /// </summary>
        /// <param name="stream">The stream to read</param>
        /// <param name="type">Type of the path table (LE or BE)</param>
        /// <returns></returns>
        private PathTableEntry ReadPathTableEntry(CBinaryReader stream, PathTableType type)
        {
            PathTableEntry entry;

            try
            {
                entry = new PathTableEntry(type);
                entry.DirectoryIdLength = stream.ReadByte();
                entry.ExtendedAttributeRecordlength = stream.ReadByte();

                entry.ExtentLBA = type == PathTableType.L_PATH_TABLE ? stream.ReadUInt32() : stream.ReadUInt32BE();

                entry.ParentDirectoryNumber = stream.ReadUInt16();
                entry.DirectoryId = stream.ReadAsciiString(entry.DirectoryIdLength);
            }
            catch (FrameworkException ex)
            {
                throw ex;
            }
            catch (Exception)
            {
                throw new FrameworkException("Error while reading PathTableEntry : PathTableEntry is not valid");
            }

            return entry;
        }

        /// <summary>
        /// Fetch all directory entries to build the internal index
        /// </summary>
        public void BuildIndex()
        {
            if (m_indexBuilt)
                return;

            if (!m_descriptorsRead)
                throw new FrameworkException("Error : You must read the descriptors first");

            try
            {
                DiskIndexEntry root = new DiskIndexEntry(null, m_primaryVolumeDescriptor.RootDirectoryEntry);
                m_index = new DiskIndex(root);
                AddDirectoryToIndex(root);
                m_indexBuilt = true;
            }
            catch (FrameworkException ex)
            {
                throw ex;
            }
            catch(Exception)
            {
                throw new FrameworkException("Error while building the index : unable to read data to build the internal index cache");
            }
        }

        /// <summary>
        /// Fetch a directory to build the internal index (recursive)
        /// </summary>
        private void AddDirectoryToIndex(DiskIndexEntry indexDirectoryEntry)
        {
            DirectoryEntry entry;
            DiskIndexEntry indexEntry;
            long size         = indexDirectoryEntry.DirectoryEntry.ExtentSize;
            int  sectorsCount = (int)(size / DiskSector.GetDataSize(m_sectorSize, m_defaultSectorMode));

            CBinaryReader stream = new CBinaryReader(ReadSectorsData(indexDirectoryEntry.DirectoryEntry.ExtentLba, sectorsCount, m_defaultSectorMode));

            // First directory entry of a directory entry is the directory itself, so let's skip it
            ReadDirectoryEntry(stream);

            // Second directory entry is the parent directory entry.
            // As we parse the data from root to children, it has already been handled, so let's skip it too
            ReadDirectoryEntry(stream);

            while (stream.Position < size)
            {
                short b = stream.TestByte();

                if (b == 0)
                {
                    // DirectoryEntry cannot be "splitted" on two sectors
                    int dataSize = DiskSector.GetDataSize(m_sectorSize, m_defaultSectorMode);
                    stream.Position = (((stream.Position / dataSize) + 1) * dataSize);
                    b = stream.TestByte();
                }

                if (b <= 0)
                    break;
                else
                {
                    entry = ReadDirectoryEntry(stream);

                    indexEntry = new DiskIndexEntry(indexDirectoryEntry, entry);
                    indexDirectoryEntry.Add(indexEntry);
                    m_index.AddToIndex(indexEntry);

                    if (indexEntry.IsDIrectory)
                        AddDirectoryToIndex(indexEntry);
                }
            }

            stream.CloseAndDispose();
        }

    // Accessors

        /// <summary>
        /// Entries
        /// </summary>
        public IEnumerable<DiskIndexEntry> Entries
        {
            get
            {
                if (!m_indexBuilt)
                    throw new FrameworkException("Error : You must build the index cache first");
                return m_index.Entries;
            }
        }

        /// <summary>
        /// Entries (directories only)
        /// </summary>
        public IEnumerable<DiskIndexEntry> DirectoryEntries
        {
            get
            {
                if (!m_indexBuilt)
                    throw new FrameworkException("Error : You must build the index cache first");
                return m_index.GetDirectories();
            }
        }


        /// <summary>
        /// Entries (files only)
        /// </summary>
        public IEnumerable<DiskIndexEntry> FileEntries
        {
            get
            {
                if (!m_indexBuilt)
                    throw new FrameworkException("Error : You must build the index cache first");
                return m_index.GetFiles();
            }
        }
    }
}