using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using CRH.Framework.Common;
using CRH.Framework.IO;

namespace CRH.Framework.Disk.DataTrack
{
    public sealed class DataTrackReader : DataTrack, ITrackReader
    {
        private CBinaryReader _stream;

        private bool _descriptorsRead;
        private bool _indexBuilt;

        private static Regex _regFileName = new Regex("(.+?)(?:;[0-9]+)?$");

    // Constructors

        /// <summary>
        /// DataTrackReader
        /// </summary>
        /// <param name="stream">The stream of iso</param>
        /// <param name="trackNumber">The track number</param>
        /// <param name="system">File system used for this data track</param>
        /// <param name="mode">The sector mode of the track</param>
        /// <param name="readDescriptors">Read descriptors immediately</param>
        /// <param name="buildIndex">Build the index cache immediately</param>
        internal DataTrackReader(CBinaryReader stream, int trackNumber, DiskFileSystem system, DataTrackMode mode, bool readDescriptors = true, bool buildIndex = true)
            : base((FileStream)stream.BaseStream, trackNumber, system, mode)
        {
            _stream          = stream;
            _descriptorsRead = false;
            _indexBuilt      = false;
            _entriesOrder    = DataTrackEntriesOrder.DEFAULT;

            try
            {
                if (readDescriptors)
                    ReadVolumeDescriptors();

                if (buildIndex)
                    BuildIndex();

                SeekSector(0);
            }
            catch (FrameworkException ex)
            {
                throw ex;
            }
            catch (Exception)
            {
                throw new FrameworkException("Error while reading data track : unable to read the data track");
            }
        }

    // Methods

        /// <summary>
        /// Read a sector's data
        /// </summary>
        /// <param name="mode">Sector's mode</param>
        public byte[] ReadSector(SectorMode mode)
        {
            try
            {
                byte[] buffer;

                int dataSize = GetSectorDataSize(mode);
                buffer = new byte[dataSize];

                if (mode != SectorMode.RAW)
                    _stream.Position += (SYNC_SIZE + HEADER_SIZE);

                if (mode == SectorMode.XA_FORM1 || mode == SectorMode.XA_FORM2)
                   _stream.Position += SUBHEADER_SIZE;
                
                _stream.Read(buffer, 0, dataSize);
                
                if (mode == SectorMode.MODE1 || mode == SectorMode.XA_FORM1)
                    _stream.Position += EDC_SIZE;

                if (mode == SectorMode.MODE1)
                    _stream.Position += INTERMEDIATE_SIZE;

                if (mode == SectorMode.MODE1 || mode == SectorMode.XA_FORM1)
                    _stream.Position += ECC_SIZE;

                if (mode == SectorMode.XA_FORM2)
                    _stream.Position += EDC_SIZE;

                return buffer;
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
        /// Read a sector's data, including sub header
        /// </summary>
        /// <param name="mode">Sector's mode</param>
        /// <param name="subHeader">Sub header container to write sub header to</param>
        public byte[] ReadSector(SectorMode mode, out XaSubHeader subHeader)
        {
            try
            {
                byte[] buffer;
                subHeader = new XaSubHeader();

                int dataSize = GetSectorDataSize(mode);
                buffer = new byte[dataSize];

                if (mode != SectorMode.RAW)
                    _stream.Position += (SYNC_SIZE + HEADER_SIZE);

                if (mode == SectorMode.XA_FORM1 || mode == SectorMode.XA_FORM2)
                {
                    subHeader.File     = _stream.ReadByte();
                    subHeader.Channel  = _stream.ReadByte();
                    subHeader.SubMode  = _stream.ReadByte();
                    subHeader.DataType = _stream.ReadByte();
                    _stream.Position += SUBHEADER_SIZE / 2;
                }

                _stream.Read(buffer, 0, dataSize);

                if (mode == SectorMode.MODE1 || mode == SectorMode.XA_FORM1)
                    _stream.Position += EDC_SIZE;

                if (mode == SectorMode.MODE1)
                    _stream.Position += INTERMEDIATE_SIZE;

                if (mode == SectorMode.MODE1 || mode == SectorMode.XA_FORM1)
                    _stream.Position += ECC_SIZE;

                if (mode == SectorMode.XA_FORM2)
                    _stream.Position += EDC_SIZE;

                return buffer;
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
        /// Read a sector's data
        /// </summary>
        /// <param name="lba">Sector's LBA to read</param>
        /// <param name="mode">Sector's mode</param>
        public byte[] ReadSector(long lba, SectorMode mode)
        {
            SeekSector(lba);
            return ReadSector(mode);
        }

        /// <summary>
        /// Read a sector's data, including sub header
        /// </summary>
        /// <param name="lba">Sector's LBA to read</param>
        /// <param name="mode">Sector's mode</param>
        /// <param name="subHeader">Sub header container to write sub header to</param>
        public byte[] ReadSector(long lba, SectorMode mode, out XaSubHeader subHeader)
        {
            SeekSector(lba);
            return ReadSector(mode, out subHeader);
        }

        /// <summary>
        /// Read a sector's data in defaut sector mode
        /// </summary>
        public byte[] ReadSector()
        {
            return ReadSector(_defaultSectorMode);
        }

        /// <summary>
        /// Read a sector's data in defaut sector mode, including sub header
        /// </summary>
        /// <param name="subHeader">Sub header container to write sub header to</param>
        public byte[] ReadSector(out XaSubHeader subHeader)
        {
            return ReadSector(_defaultSectorMode, out subHeader);
        }

        /// <summary>
        /// Read a sector
        /// </summary>
        /// <param name="lba">Sector's LBA to read</param>
        /// <param name="subHeader">Sub header container to write sub header to</param>
        public byte[] ReadSector(long lba, out XaSubHeader subHeader)
        {
            SeekSector(lba);
            return ReadSector(_defaultSectorMode, out subHeader);
        }

        /// <summary>
        /// Read several consecutives sectors's data
        /// </summary>
        /// <param name="count">Number of sectors to read</param>
        /// <param name="mode">Sector's mode</param>
        public byte[] ReadSectors(int count, SectorMode mode)
        {
            int dataSize = GetSectorDataSize(mode);
            byte[] data = new byte[count * dataSize];

            for (int i = 0, offset = 0; i < count; i++, offset += dataSize)
                CBuffer.Copy(ReadSector(mode), data, 0, offset);

            return data;
        }

        /// <summary>
        /// Read several consecutives sectors data (only data : does not include modes specifics fields)
        /// </summary>
        /// <param name="lba">Starting sector's LBA</param>
        /// <param name="count">Number of sectors to read</param>
        /// <param name="mode">Sector's mode</param>
        public byte[] ReadSectors(long lba, int count, SectorMode mode)
        {
            SeekSector(lba);
            return ReadSectors(count, mode);
        }

        /// <summary>
        /// Read a file
        /// </summary>
        /// <param name="lba">Sector's lba</param>
        /// <param name="size">The size of the file</param>
        /// <param name="mode">Sector's mode</param>
        /// <param name="stream">The stream to write the data</param>
        public void ReadFile(long lba, long size, SectorMode mode, Stream stream)
        {
            int sectorDataSize = GetSectorDataSize(mode);
            long bytesRead = 0;
            SeekSector(lba);

            long remaining;
            while (bytesRead < size)
            {
                remaining = size - bytesRead;
                if (remaining >= sectorDataSize)
                    stream.Write(ReadSector(mode), 0, sectorDataSize);
                else
                    stream.Write(ReadSector(mode), 0, (int)remaining);

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
            if (!_indexBuilt)
                throw new FrameworkException("Error : You must build the index cache first");

            try
            {
                var entry = _index.GetEntry(filePath);
                if (entry == null)
                    throw new FrameworkException("File not found : unable to find file \"{0}\"", filePath);

                if (entry.IsDirectory)
                    throw new FrameworkException("Not a file : specified path seems to be a directory, not a file", filePath);

                var mode = (_mode != DataTrackMode.MODE2_XA)
                                ? _defaultSectorMode
                                : entry.DirectoryEntry.XaEntry.IsForm1
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
        /// Read a file
        /// </summary>
        /// <param name="filePath">The full file path of the file (eg : /FOO/BAR/FILE.EXT)</param>
        public Stream ReadFile(string filePath)
        {
            var stream = new MemoryStream();
            ReadFile(filePath, stream);
            stream.Position = 0;

            return stream;
        }

        /// <summary>
        /// Extract a file
        /// </summary>
        /// <param name="filePath">The full path of the disk's file (eg : /FOO/BAR/FILE.EXT)</param>
        /// <param name="outFilePath">The full path of the output file</param>
        public void ExtractFile(string filePath, string outFilePath)
        {
            if (!_indexBuilt)
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
            if (!_indexBuilt)
                throw new FrameworkException("Error : You must build the index cache first");

            return _index.GetEntry(filePath) != null;
        }

        /// <summary>
        /// Read the disk descriptors
        /// </summary>
        public void ReadVolumeDescriptors()
        {
            if (_descriptorsRead)
                return;

            try
            {
                VolumeDescriptor descriptor;
                bool endOfList = false;
                bool hasPrimaryDescriptor = false;
                SeekSector(16);

                do
                {
                    using (var stream = new CBinaryReader(ReadSector(_defaultSectorMode)))
                    {
                        descriptor = ReadVolumeDescriptor(stream);
                    }

                    switch (descriptor.Type)
                    {
                        case VolumeDescriptorType.PRIMARY:
                            _primaryVolumeDescriptor = (PrimaryVolumeDescriptor)descriptor;
                            hasPrimaryDescriptor = true;
                            break;
                        case VolumeDescriptorType.SET_TERMINATOR:
                            endOfList = true;
                            break;
                    }
                } while (!endOfList);

                _descriptorsRead = true;

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

                if (descriptor.OptTypeLPathTableLBA != 0 || descriptor.OptTypeMPathTableLBA != 0)
                    _hasOptionalPathTable = true;

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
                    _isXa = true;
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

                byte nameLength = stream.ReadByte();
                entry.Name = _regFileName.Match(stream.ReadAsciiString(nameLength, false)).Groups[1].Value;

                if (nameLength % 2 == 0)
                    stream.Position += 1;

                if (_isXa && (stream.Position != position + entry.Length))
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
        /// Fetch all directory entries to build the internal index
        /// </summary>
        public void BuildIndex()
        {
            if (_indexBuilt)
                return;

            if (!_descriptorsRead)
                throw new FrameworkException("Error : You must read the descriptors first");

            try
            {
                long rootLba   = _primaryVolumeDescriptor.RootDirectoryEntry.ExtentLba;
                var stream     = new CBinaryReader(ReadSector(rootLba, _defaultSectorMode));
                var rootEntry  = ReadDirectoryEntry(stream);

                _index = new DataTrackIndex(rootEntry);
                AddDirectoryToIndex(_index.Root);
                _indexBuilt = true;
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
        private void AddDirectoryToIndex(DataTrackIndexEntry indexDirectoryEntry)
        {
            DirectoryEntry entry;
            DataTrackIndexEntry indexEntry;
            long lba          = indexDirectoryEntry.DirectoryEntry.ExtentLba;
            long size         = indexDirectoryEntry.DirectoryEntry.ExtentSize;
            int  sectorsCount = (int)(size / GetSectorDataSize(_defaultSectorMode));

            var stream = new CBinaryReader(ReadSectors(lba, sectorsCount, _defaultSectorMode));

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
                    int dataSize = GetSectorDataSize(_defaultSectorMode);
                    stream.Position = (((stream.Position / dataSize) + 1) * dataSize);
                    b = stream.TestByte();
                }

                if (b <= 0)
                    break;
                else
                {
                    entry = ReadDirectoryEntry(stream);

                    indexEntry = new DataTrackIndexEntry(indexDirectoryEntry, entry);
                    _index.AddToIndex(indexEntry);

                    if (indexEntry.IsDirectory)
                        AddDirectoryToIndex(indexEntry);
                }
            }

            stream.CloseAndDispose();
        }

    // Accessors

        /// <summary>
        /// Entries
        /// </summary>
        public override IEnumerable<DataTrackIndexEntry> Entries
        {
            get
            {
                if (!_indexBuilt)
                    throw new FrameworkException("Error : You must build the index cache first");
                return _index.GetEntries(_entriesOrder);
            }
        }

        /// <summary>
        /// Entries (directories only)
        /// </summary>
        public override IEnumerable<DataTrackIndexEntry> DirectoryEntries
        {
            get
            {
                if (!_indexBuilt)
                    throw new FrameworkException("Error : You must build the index cache first");
                return _index.GetDirectories(_entriesOrder);
            }
        }


        /// <summary>
        /// Entries (files only)
        /// </summary>
        public override IEnumerable<DataTrackIndexEntry> FileEntries
        {
            get
            {
                if (!_indexBuilt)
                    throw new FrameworkException("Error : You must build the index cache first");
                return _index.GetFiles(_entriesOrder);
            }
        }

        /// <summary>
        /// Number of entries
        /// </summary>
        public override int EntriesCount
        {
            get
            {
                if (!_indexBuilt)
                    throw new FrameworkException("Error : You must build the index cache first");
                return _index.EntriesCount;
            }
        }

        /// <summary>
        /// Number of Directory entries
        /// </summary>
        public override int DirectoryEntriesCount
        {
            get
            {
                if (!_indexBuilt)
                    throw new FrameworkException("Error : You must build the index cache first");
                return _index.EntriesCount;
            }
        }

        /// <summary>
        /// Number of file entries
        /// </summary>
        public override int FileEntriesCount
        {
            get
            {
                if (!_indexBuilt)
                    throw new FrameworkException("Error : You must build the index cache first");
                return _index.FileEntriesCount;
            }
        }
    }
}