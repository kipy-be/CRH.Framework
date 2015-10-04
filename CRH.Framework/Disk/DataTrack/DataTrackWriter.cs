using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using CRH.Framework.Common;
using CRH.Framework.IO;
using CRH.Framework.Utils;

namespace CRH.Framework.Disk.DataTrack
{
    public sealed class DataTrackWriter : DataTrack
    {
        private CBinaryWriter m_stream;
        private DataTrackIndex m_index;

        private bool m_prepared;
        private bool m_finalized;
        private bool m_appendVersionToFileName;

        private static Regex m_regDirectoryName = new Regex("[\\/]([^\\/]+)[\\/]?$");
        private static Regex m_regFileName = new Regex("[\\/]([^\\/]+?)$");

    // Constructors

        /// <summary>
        /// DataTrackWriter
        /// </summary>
        /// <param name="stream">The stream of iso</param>
        /// <param name="trackNumber">The track number</param>
        /// <param name="system">File system used for this data track</param>
        /// <param name="mode">The sector mode of the track</param>
        internal DataTrackWriter(CBinaryWriter stream, int trackNumber, DiskFileSystem system, DataTrackMode mode)
            : base((FileStream)stream.BaseStream, trackNumber,system, mode)
        {
            m_stream    = stream;
            m_prepared  = false;
            m_finalized = false;
            m_appendVersionToFileName = true;

            try
            {
                // Allocation for system area
                WriteEmptySectors(16);
            }
            catch (FrameworkException ex)
            {
                throw ex;
            }
            catch (Exception)
            {
                throw new FrameworkException("Error while while writing data track : unable to write the data track");
            }
        }

    // Methods

        /// <summary>
        /// Init the pvd and allocate some space for the path table and the root directory
        /// </summary>
        /// <param name="volumeId">The volume identifier</param>
        /// <param name="pathTableSize">Size of the path table in sector (default 1)</param>
        /// <param name="rootDirectorySize">Size of the root directory in sector (default 1)</param>
        public void Prepare(string volumeId, int pathTableSize = 1, int rootDirectorySize = 1)
        {
            if (m_prepared)
                return;

            SeekSector(16);
            WriteEmptySectors(2 + pathTableSize * 4);

            DirectoryEntry root = new DirectoryEntry(m_isXa);
            root.IsDirectory = true;
            root.ExtentSize = (uint)(rootDirectorySize * GetSectorDataSize(m_defaultSectorMode));
            root.ExtentLba = (uint)SectorCount;

            m_index = new DataTrackIndex(root);

            m_primaryVolumeDescriptor = new PrimaryVolumeDescriptor(1);
            m_primaryVolumeDescriptor.VolumeId = volumeId;
            m_primaryVolumeDescriptor.PathTableSize = (uint)(pathTableSize * GetSectorDataSize(m_defaultSectorMode));

            // The root directory included in the volume descriptor doesn't allow XA, so let's create a separated one
            m_primaryVolumeDescriptor.RootDirectoryEntry = new DirectoryEntry(false);
            m_primaryVolumeDescriptor.RootDirectoryEntry.IsDirectory = true;
            m_primaryVolumeDescriptor.RootDirectoryEntry.ExtentSize = root.ExtentSize;
            m_primaryVolumeDescriptor.RootDirectoryEntry.ExtentLba = root.ExtentLba;

            WriteEmptySectors(rootDirectorySize);

            m_prepared = true;
        }

        /// <summary>
        /// Finalise the disk (dump descriptors, path table, directory entries, etc.)
        /// </summary>
        public void Finalize()
        {
            if (m_finalized)
                return;

            if(!m_prepared)
                throw new FrameworkException("Error while finalizing ISO : ISO has not been prepared, it will be unreadable");

            uint pathTableSectorSize = (uint)(m_primaryVolumeDescriptor.PathTableSize / GetSectorDataSize(m_defaultSectorMode));
            m_primaryVolumeDescriptor.VolumeSpaceSize = (uint)SectorCount;
            m_primaryVolumeDescriptor.TypeLPathTableLBA = 16 + 2;
            m_primaryVolumeDescriptor.TypeMPathTableLBA = 16 + 2 + pathTableSectorSize * 2;
            if(m_hasOptionalPathTable)
            {
                m_primaryVolumeDescriptor.OptTypeLPathTableLBA = 16 + 2 + pathTableSectorSize;
                m_primaryVolumeDescriptor.OptTypeMPathTableLBA = 16 + 2 + pathTableSectorSize * 3;
            }


            // Write directory entries
            WriteDirectoryEntry(m_index.Root);
            foreach (DataTrackIndexEntry entry in m_index.GetDirectories(DataTrackEntriesOrder.DEFAULT))
                WriteDirectoryEntry(entry);

            // Write path tables
            WritePathTables();

            // Write descriptors
            SeekSector(16);
            WriteSector(GetPrimaryVolumeDescriptorBuffer(), m_defaultSectorMode, XaSubHeader.EndOfRecord);
            WriteSector(GetSetTerminatorVolumeDescriptorBuffer(), m_defaultSectorMode, XaSubHeader.EndOfFile);

            // Write 2 minutes of empty sectors at the end of the track
            SeekSector(SectorCount);
            WriteEmptySectors(150);

            m_finalized = true;
        }

        /// <summary>
        /// Write out the directory entry
        /// </summary>
        private void WriteDirectoryEntry(DataTrackIndexEntry entry)
        {
            SeekSector(entry.Lba);

            int size = (int)entry.Size;
            int sectorSize = GetSectorDataSize(m_defaultSectorMode);
            byte[] data = new byte[size];

            using (CBinaryWriter stream = new CBinaryWriter(data))
            {
                // First directory entry of a directory entry is the directory itself
                stream.Write(GetDirectoryEntryBuffer(entry.DirectoryEntry, true, false));

                // Second directory entry is the parent directory entry.
                if (entry.ParentEntry != null)
                    stream.Write(GetDirectoryEntryBuffer(entry.ParentEntry.DirectoryEntry, false, true));
                else
                    stream.Write(GetDirectoryEntryBuffer(entry.DirectoryEntry, false, true));

                foreach (DataTrackIndexEntry subEntry in entry.SubEntries)
                {
                    // DirectoryEntry cannot be "splitted" on two sectors
                    if ((stream.Position - (stream.Position / sectorSize) * sectorSize) + subEntry.Length >= sectorSize)
                        stream.Position = ((stream.Position / sectorSize) + 1) * sectorSize;

                    if (stream.Position + subEntry.DirectoryEntry.Length < size)
                        stream.Write(GetDirectoryEntryBuffer(subEntry.DirectoryEntry));
                    else
                        throw new FrameworkException("Error while finalizing disk : directory \"{0}\" is too small", entry.FullPath);
                }
            }

            for (int i = 0; i < size; i += sectorSize)
                WriteSector
                (
                    CBuffer.Create(data, i, sectorSize),
                    m_defaultSectorMode,
                    (i + sectorSize >= size) ? XaSubHeader.EndOfFile : XaSubHeader.Basic
                );
        }

        /// <summary>
        /// Write out the path tables
        /// </summary>
        private void WritePathTables()
        {
            int sectorSize = GetSectorDataSize(m_defaultSectorMode);

            byte[] lePathTableData = new byte[m_primaryVolumeDescriptor.PathTableSize];
            byte[] bePathTableData = new byte[m_primaryVolumeDescriptor.PathTableSize];
            Dictionary<string, ushort> dNums = new Dictionary<string, ushort>();
            ushort dNum = 0, refNum;
            int totalSize = 0;

            using (CBinaryWriter lePathTableStream = new CBinaryWriter(lePathTableData))
            using (CBinaryWriter bePathTableStream = new CBinaryWriter(bePathTableData))
            {
                lePathTableStream.Write(GetPathTableEntryBuffer(m_index.Root.DirectoryEntry, PathTableType.LE, 0));
                bePathTableStream.Write(GetPathTableEntryBuffer(m_index.Root.DirectoryEntry, PathTableType.BE, 0));
                dNums.Add(m_index.Root.FullPath, ++dNum);
                totalSize += (8 + m_index.Root.DirectoryEntry.Name.Length + (m_index.Root.DirectoryEntry.Name.Length % 2 != 0 ? 1 : 0));

                foreach (DataTrackIndexEntry entry in m_index.GetDirectories(DataTrackEntriesOrder.DEFAULT))
                {
                    refNum = dNums[entry.ParentEntry.FullPath];
                    lePathTableStream.Write(GetPathTableEntryBuffer(entry.DirectoryEntry, PathTableType.LE, refNum));
                    bePathTableStream.Write(GetPathTableEntryBuffer(entry.DirectoryEntry, PathTableType.BE, refNum));
                    dNums.Add(entry.FullPath, ++dNum);
                    totalSize += (8 + entry.DirectoryEntry.Name.Length + (entry.DirectoryEntry.Name.Length % 2 != 0 ? 1 : 0));
                }
            }

            WritePathTable(m_primaryVolumeDescriptor.TypeLPathTableLBA, lePathTableData);
            WritePathTable(m_primaryVolumeDescriptor.TypeMPathTableLBA, bePathTableData);
            if(m_hasOptionalPathTable)
            {
                WritePathTable(m_primaryVolumeDescriptor.OptTypeLPathTableLBA, lePathTableData);
                WritePathTable(m_primaryVolumeDescriptor.OptTypeMPathTableLBA, bePathTableData);
            }

            m_primaryVolumeDescriptor.PathTableSize = (uint)totalSize;
        }

        /// <summary>
        /// Write out path table
        /// </summary>
        /// <param name="lba"></param>
        /// <param name="data"></param>
        private void WritePathTable(uint lba, byte[] data)
        {
            SeekSector(lba);
            int sectorSize = GetSectorDataSize(m_defaultSectorMode);

            for (int i = 0; i < data.Length; i += sectorSize)
                WriteSector
                (
                    CBuffer.Create(data, i, sectorSize),
                    m_defaultSectorMode,
                    (i + sectorSize >= data.Length) ? XaSubHeader.EndOfFile : XaSubHeader.Basic
                );
        }

        /// <summary>
        /// Write a sector
        /// </summary>
        /// <param name="data">The sector's data to write</param>
        /// <param name="mode">Sector's mode</param>
        /// <param name="subHeader">Subheader (if mode XA_FORM1 or XA_FORM2)</param>
        internal void WriteSector(byte[] data, SectorMode mode, XaSubHeader subHeader = null)
        {
            try
            {
                byte[] buffer = new byte[m_sectorSize];
                using (CBinaryWriter bufferStream = new CBinaryWriter(buffer))
                {
                    if (mode != SectorMode.RAW)
                    {
                        bufferStream.Write(SYNC);
                        long position = SectorPosition + 150;
                        byte[] header = new byte[4];
                        header[3] = (byte)(mode == SectorMode.MODE0 ? 0 : mode == SectorMode.MODE1 ? 1 : 2);
                        header[2] = Converter.DecToBcd((byte)(position % 75)); position /= 75;
                        header[1] = Converter.DecToBcd((byte)(position % 60)); position /= 60;
                        header[0] = Converter.DecToBcd((byte)(position % 60));
                        bufferStream.Write(header); 
                    }

                    if(mode == SectorMode.XA_FORM1 || mode == SectorMode.XA_FORM2)
                    {
                        if (subHeader == null)
                            subHeader = new XaSubHeader();
                        subHeader.IsForm2 = (mode == SectorMode.XA_FORM2);

                        bufferStream.Write(subHeader.File);
                        bufferStream.Write(subHeader.Channel);
                        bufferStream.Write(subHeader.SubMode);
                        bufferStream.Write(subHeader.DataType);

                        // Subheader is written twice
                        bufferStream.Write(subHeader.File);
                        bufferStream.Write(subHeader.Channel);
                        bufferStream.Write(subHeader.SubMode);
                        bufferStream.Write(subHeader.DataType);
                    }

                    bufferStream.Write(data, 0, data.Length);

                    if (mode == SectorMode.MODE1 || mode == SectorMode.XA_FORM1 || mode == SectorMode.XA_FORM2)
                        EccEdc.EccEdcCompute(buffer, mode);

                    m_stream.Write(buffer);
                }
            }
            catch (Exception)
            {
                throw new FrameworkException("Errow while writing sector : unable to write sector");
            }
        }

        /// <summary>
        /// Write a sector at the specified lba
        /// </summary>
        /// <param name="lba">Sector's LBA</param>
        /// <param name="data">The sector to write</param>
        /// <param name="mode">Sector's mode</param>
        /// <param name="subHeader">Subheader (if mode XA_FORM1 or XA_FORM2)</param>
        internal void WriteSector(long lba, byte[] data, SectorMode mode, XaSubHeader subHeader = null)
        {
            SeekSector(lba);
            WriteSector(data, mode, subHeader);
        }

        /// <summary>
        /// Write a sector in default mode
        /// </summary>
        /// <param name="data">The sector to write</param>
        /// <param name="subHeader">Subheader (if mode XA_FORM1 or XA_FORM2)</param>
        internal void WriteSector(byte[] data, XaSubHeader subHeader = null)
        {
            WriteSector(data, m_defaultSectorMode, subHeader);
        }

        /// <summary>
        /// Write a sector at the specified lba
        /// </summary>
        /// <param name="lba">Sector's LBA</param>
        /// <param name="sector">The sector to write</param>
        /// <param name="subHeader">Subheader (if mode XA_FORM1 or XA_FORM2)</param>
        internal void WriteSector(long lba, byte[] data, XaSubHeader subHeader = null)
        {
            SeekSector(lba);
            WriteSector(data, m_defaultSectorMode, subHeader);
        }

        /// <summary>
        /// Write an empty sector
        /// </summary>
        /// <param name="mode">Sector's mode</param>
        public void WriteEmptySector()
        {
            if(m_mode == DataTrackMode.RAW)
                WriteSector(new byte[2048], SectorMode.RAW);
            else
                WriteSector(new byte[2048], SectorMode.MODE0);
        }

        /// <summary>
        /// Write empty sectors
        /// </summary>
        /// <param name="count">Number of sectors to write</param>
        public void WriteEmptySectors(int count)
        {
            byte[] data = new byte[2048];
            if (m_mode == DataTrackMode.RAW)
            {
                for (int i = 0; i < count; i++)
                    WriteSector(data, SectorMode.RAW);
            }
            else
            {
                for (int i = 0; i < count; i++)
                    WriteSector(data, SectorMode.MODE0);
            }
        }

        /// <summary>
        /// Copy sectors from another disk
        /// </summary>
        /// <param name="diskIn">The disk to copy sectors from</param>
        /// <param name="count">Number of sectors to copy</param>
        internal void CopySectors(DataTrackReader diskIn, int count)
        {
            if (diskIn.IsXa)
            {
                XaSubHeader subHeader;
                for (int i = 0; i < count; i++)
                    WriteSector(diskIn.ReadSector(out subHeader), subHeader);
            }
            else
            {
                for (int i = 0; i < count; i++)
                    WriteSector(diskIn.ReadSector());
            }
        }

        /// <summary>
        /// Copy sectors from another disk
        /// </summary>
        /// <param name="reader">The disk to copy sectors from</param>
        /// <param name="readerLba">Starting LBA for reading</param>
        /// <param name="writerLba">Starting LBA for writing</param>
        /// <param name="count">Number of sectors to copy</param>
        internal void CopySectors(DataTrackReader reader, long readerLba, long writerLba, int count)
        {
            SeekSector(writerLba);
            reader.SeekSector(readerLba);
            CopySectors(reader, count);
        }

        /// <summary>
        /// Get a primary volume descriptor data
        /// </summary>
        /// <param name="m_primaryVolumeDescriptor">The primary volume descriptor</param>
        private byte[] GetPrimaryVolumeDescriptorBuffer()
        {
            byte[] buffer = new byte[GetSectorDataSize(m_defaultSectorMode)];
            try
            {
                using (CBinaryWriter stream = new CBinaryWriter(buffer))
                {
                    stream.Write((byte)m_primaryVolumeDescriptor.Type);
                    stream.WriteAsciiString(m_primaryVolumeDescriptor.Id);
                    stream.Write(m_primaryVolumeDescriptor.Version);
                    stream.Write(m_primaryVolumeDescriptor.Unused1);
                    stream.WriteAsciiString(m_primaryVolumeDescriptor.SystemId, 32, " ");
                    stream.WriteAsciiString(m_primaryVolumeDescriptor.VolumeId, 32, " ");
                    stream.Write(m_primaryVolumeDescriptor.Unused2);

                    stream.Write(m_primaryVolumeDescriptor.VolumeSpaceSize);
                    stream.WriteBE(m_primaryVolumeDescriptor.VolumeSpaceSize);

                    stream.Write(m_primaryVolumeDescriptor.Unused3);

                    stream.Write(m_primaryVolumeDescriptor.VolumeSetSize);
                    stream.WriteBE(m_primaryVolumeDescriptor.VolumeSetSize);

                    stream.Write(m_primaryVolumeDescriptor.VolumeSequenceNumber);
                    stream.WriteBE(m_primaryVolumeDescriptor.VolumeSequenceNumber);

                    stream.Write(m_primaryVolumeDescriptor.LogicalBlockSize);
                    stream.WriteBE(m_primaryVolumeDescriptor.LogicalBlockSize);

                    stream.Write(m_primaryVolumeDescriptor.PathTableSize);
                    stream.WriteBE(m_primaryVolumeDescriptor.PathTableSize);

                    stream.Write(m_primaryVolumeDescriptor.TypeLPathTableLBA);
                    stream.Write(m_primaryVolumeDescriptor.OptTypeLPathTableLBA);
                    stream.WriteBE(m_primaryVolumeDescriptor.TypeMPathTableLBA);
                    stream.WriteBE(m_primaryVolumeDescriptor.OptTypeMPathTableLBA);
                    stream.Write(GetDirectoryEntryBuffer(m_primaryVolumeDescriptor.RootDirectoryEntry));

                    // TODO : cas des fichiers
                    stream.WriteAsciiString(m_primaryVolumeDescriptor.VolumeSetId, 128, " ");
                    stream.WriteAsciiString(m_primaryVolumeDescriptor.PublisherId, 128, " ");
                    stream.WriteAsciiString(m_primaryVolumeDescriptor.PreparerId, 128, " ");
                    stream.WriteAsciiString(m_primaryVolumeDescriptor.ApplicationId, 128, " ");
                    stream.WriteAsciiString(m_primaryVolumeDescriptor.CopyrightFileId, 38, " ");
                    stream.WriteAsciiString(m_primaryVolumeDescriptor.AbstractFileId, 36, " ");
                    stream.WriteAsciiString(m_primaryVolumeDescriptor.BibliographicFileId, 37, " ");
                    //

                    stream.Write(VolumeDescriptor.FromDateTime(m_primaryVolumeDescriptor.CreationDate));
                    stream.Write(VolumeDescriptor.FromDateTime(m_primaryVolumeDescriptor.ModificationDate));
                    stream.Write(VolumeDescriptor.FromDateTime(m_primaryVolumeDescriptor.ExpirationDate));
                    stream.Write(VolumeDescriptor.FromDateTime(m_primaryVolumeDescriptor.EffectiveDate));
                    stream.Write(m_primaryVolumeDescriptor.FileStructureVersion);
                    stream.Write(m_primaryVolumeDescriptor.Unused4);

                    if (m_isXa)
                    {
                        using (CBinaryWriter appDataStream = new CBinaryWriter(m_primaryVolumeDescriptor.ApplicationData))
                        {
                            appDataStream.Position = 0x8D;
                            appDataStream.WriteAsciiString(VolumeDescriptor.VOLUME_XA);
                        }
                    }

                    stream.Write(m_primaryVolumeDescriptor.ApplicationData);
                    stream.Write(m_primaryVolumeDescriptor.Reserved);
                }

                return buffer;
            }
            catch (Exception)
            {
                throw new FrameworkException("Error while writing PrimaryVolumeDescriptor : unable to write the descriptor");
            }
        }

        /// <summary>
        /// Get a set terminator volume descriptor data
        /// </summary>
        private byte[] GetSetTerminatorVolumeDescriptorBuffer()
        {
            byte[] buffer = new byte[GetSectorDataSize(m_defaultSectorMode)];
            try
            {
                using (CBinaryWriter stream = new CBinaryWriter(buffer))
                {
                    SetTerminatorVolumeDescriptor descriptor = new SetTerminatorVolumeDescriptor();
                    stream.Write((byte)descriptor.Type);
                    stream.WriteAsciiString(descriptor.Id);
                    stream.Write(descriptor.Version);
                }

                return buffer;
            }
            catch (Exception)
            {
                throw new FrameworkException("Error while writing SetTerminatorVolumeDescriptor : unable to write the descriptor");
            }
        }

        /// <summary>
        /// Get a directory entry's data
        /// </summary>
        /// <param name="entry">The entry</param>
        /// <param name="selfRef">Is a self reference</param>
        /// <param name="parentRef">Is a parent reference</param>
        private byte[] GetDirectoryEntryBuffer(DirectoryEntry entry, bool selfRef = false, bool parentRef = false)
        {
            byte entryLength = entry.Length;

            // If parent or self reference, name is replaced by 0x00 for self or 0x01 for parent
            if(selfRef || parentRef)
            {
                entryLength -= (byte)(entry.Name.Length - 1);
                entryLength -= (byte)(entry.Name.Length % 2 == 0 ? 1 : 0);
            }

            byte[] buffer = new byte[entryLength];
            try
            {
                using (CBinaryWriter stream = new CBinaryWriter(buffer))
                {
                    stream.Write(entryLength);
                    stream.Write(entry.ExtendedAttributeRecordlength);

                    stream.Write(entry.ExtentLba);
                    stream.WriteBE(entry.ExtentLba);

                    stream.Write(entry.ExtentSize);
                    stream.WriteBE(entry.ExtentSize);

                    byte[] dateBuffer = new byte[7];
                    dateBuffer[0] = (byte)(entry.Date.Year - 1900);
                    dateBuffer[1] = (byte)(entry.Date.Month);
                    dateBuffer[2] = (byte)(entry.Date.Day);
                    dateBuffer[3] = (byte)(entry.Date.Hour);
                    dateBuffer[4] = (byte)(entry.Date.Minute);
                    dateBuffer[5] = (byte)(entry.Date.Second);
                    stream.Write(dateBuffer);

                    stream.Write(entry.Flags);
                    stream.Write(entry.FileUnitSize);
                    stream.Write(entry.Interleave);

                    stream.Write(entry.VolumeSequenceNumber);
                    stream.WriteBE(entry.VolumeSequenceNumber);

                    if (!(selfRef || parentRef))
                    {
                        stream.Write((byte)entry.Name.Length);
                        stream.WriteAsciiString(entry.Name);

                        if (entry.Name.Length % 2 == 0)
                            stream.Write((byte)0);
                    }
                    else
                    {
                        stream.Write((byte)1);
                        stream.Write((byte)(selfRef ? 0 : 1));
                    }

                    if (entry.HasXa)
                    {
                        stream.WriteBE(entry.XaEntry.GroupId);
                        stream.WriteBE(entry.XaEntry.UserId);
                        stream.WriteBE(entry.XaEntry.Attributes);
                        stream.WriteAsciiString(entry.XaEntry.Signature);
                        stream.Write(entry.XaEntry.FileNumber);
                        stream.Write(entry.XaEntry.Unused);
                    }
                }

                return buffer;
            }
            catch (Exception)
            {
                throw new FrameworkException("Error while writing DirectoryEntry : unable to write the entry");
            }
        }

        /// <summary>
        /// Get a path table entry's data
        /// </summary>
        /// <param name="entry">The entry</param>
        /// <param name="type">The type of the entry (little endian or big endian)</param>
        private byte[] GetPathTableEntryBuffer(DirectoryEntry entry, PathTableType type, ushort parentNumber)
        {
            int entrySize = 8 + entry.Name.Length + (entry.Name.Length % 2 != 0 ? 1 : 0);
            byte[] buffer = new byte[entrySize];

            using (CBinaryWriter stream = new CBinaryWriter(buffer))
            {
                stream.Write((byte)entry.Name.Length);
                stream.Write(entry.ExtendedAttributeRecordlength);

                if (type == PathTableType.LE)
                    stream.Write(entry.ExtentLba);
                else
                    stream.WriteBE(entry.ExtentLba);

                stream.Write(parentNumber);
                stream.WriteAsciiString(entry.Name);

                if (entry.Name.Length % 2 != 0)
                    stream.Write((byte)0);
            }

            return buffer;
        }

        /// <summary>
        /// Create a directory
        /// </summary>
        /// <param name="path">Full path of the directory to create</param>
        /// <param name="size">Size of the directory in sectors (default 1)</param>
        public void CreateDirectory(string path, int size = 1)
        {
            if (m_index.GetEntry(path) != null)
                throw new FrameworkException("Error while creating directory \"{0}\" : entry already exists", path);

            DataTrackIndexEntry parent = m_index.FindAParent(path);
            if(parent == null)
                throw new FrameworkException("Error while creating directory \"{0}\" : parent directory does not exists", path);

            DirectoryEntry entry = new DirectoryEntry(m_isXa);
            entry.IsDirectory    = true;
            entry.Name           = m_regDirectoryName.Match(path).Groups[1].Value;
            entry.Length        += (byte)(entry.Name.Length - 1);
            entry.Length        += (byte)(entry.Name.Length % 2 == 0 ? 1 : 0);
            entry.ExtentSize     = (uint)(size * GetSectorDataSize(m_defaultSectorMode));
            entry.ExtentLba      = (uint)SectorCount;

            if (m_isXa)
            {
                entry.XaEntry.IsDirectory  = true;
                entry.XaEntry.IsForm1 = true;
            }

            DataTrackIndexEntry indexEntry = new DataTrackIndexEntry(parent, entry);

            m_index.AddToIndex(indexEntry);

            SeekSector(SectorCount);
            WriteEmptySectors(size);
        }

        /// <summary>
        /// Write a file
        /// </summary>
        /// <param name="filePath">The full file path of the file (eg : /FOO/BAR/FILE.EXT)</param>
        /// <param name="stream">The source stream of the file</param>
        public void WriteFile(string filePath, Stream stream)
        {
            if (m_index.GetEntry(filePath) != null)
                throw new FrameworkException("Error while creating file \"{0}\" : entry already exists", filePath);

            DataTrackIndexEntry parent = m_index.FindAParent(filePath);
            if (parent == null)
                throw new FrameworkException("Error while creating file \"{0}\" : parent directory does not exists", filePath);

            DirectoryEntry entry = new DirectoryEntry(m_isXa);
            entry.Name           = m_regFileName.Match(filePath).Groups[1].Value + (m_appendVersionToFileName ? ";1" : "");
            entry.Length        += (byte)(entry.Name.Length - 1);
            entry.Length        += (byte)(entry.Name.Length % 2 == 0 ? 1 : 0);
            entry.ExtentSize     = (uint)stream.Length;
            entry.ExtentLba      = (uint)SectorCount;

            if (m_isXa)
                entry.XaEntry.IsForm1 = true;

            DataTrackIndexEntry indexEntry = new DataTrackIndexEntry(parent, entry);
            m_index.AddToIndex(indexEntry);

            stream.Position = 0;
            SeekSector(SectorCount);
            int dataSize = GetSectorDataSize(m_defaultSectorMode);
            byte[] buffer = new byte[dataSize];
            
            while(stream.Position < stream.Length)
            {
                stream.Read(buffer, 0, dataSize);
                WriteSector
                (
                    buffer,
                    (stream.Position + dataSize >= stream.Length) ? XaSubHeader.EndOfFile : XaSubHeader.Basic
                );
            }
        }

        /// <summary>
        /// Copy a stream (XA sound, str video, etc)
        /// </summary>
        /// <param name="filePath">The full file path of the file to write (eg : /FOO/BAR/FILE.EXT)</param>
        /// <param name="diskIn">The disk to copy stream's sectors from</param>
        /// <param name="entry">The entry of the reading disk</param>
        public void CopyStream(string filePath, DataTrackReader diskIn, DataTrackIndexEntry inEntry)
        {
            if (m_index.GetEntry(filePath) != null)
                throw new FrameworkException("Error while creating file \"{0}\" : entry already exists", filePath);

            DataTrackIndexEntry parent = m_index.FindAParent(filePath);
            if (parent == null)
                throw new FrameworkException("Error while creating file \"{0}\" : parent directory does not exists", filePath);

            DirectoryEntry entry = new DirectoryEntry(m_isXa);
            entry.Name = m_regFileName.Match(filePath).Groups[1].Value + (m_appendVersionToFileName ? ";1" : "");
            entry.Length += (byte)(entry.Name.Length - 1);
            entry.Length += (byte)(entry.Name.Length % 2 == 0 ? 1 : 0);
            entry.ExtentSize = (uint)inEntry.Size;
            entry.ExtentLba = (uint)SectorCount;

            DataTrackIndexEntry indexEntry = new DataTrackIndexEntry(parent, entry);
            m_index.AddToIndex(indexEntry);

            CopySectors(diskIn, entry.ExtentLba, inEntry.Lba, (int)(inEntry.Size / GetSectorDataSize(diskIn.DefautSectorMode)));
        }

        /// <summary>
        /// Copy the system-reserved zone of the iso (16 first sectors)
        /// </summary>
        /// <param name="diskIn">The disk to copy sector's from</param>
        public void CopySystemZone(DataTrackReader diskIn)
        {
            CopySectors(diskIn, 0, 0, 16);
        }

        /// <summary>
        /// Copy the application-reserved zone of the iso (pvd's Application Data) 
        /// </summary>
        /// <param name="diskIn"></param>
        public void CopyApplicationData(DataTrackReader diskIn)
        {
            CBuffer.Copy(diskIn.PrimaryVolumeDescriptor.ApplicationData, m_primaryVolumeDescriptor.ApplicationData);
        }

    // Accessors

        /// <summary>
        /// Is the track finalized
        /// </summary>
        public bool IsFinalized
        {
            get { return m_finalized; }
        }

        /// <summary>
        /// Entries
        /// </summary>
        public override IEnumerable<DataTrackIndexEntry> Entries
        {
            get
            {
                if (!m_prepared)
                    throw new FrameworkException("Error : You must prepare the iso first");
                return m_index.GetEntries();
            }
        }

        /// <summary>
        /// Entries (directories only)
        /// </summary>
        public override IEnumerable<DataTrackIndexEntry> DirectoryEntries
        {
            get
            {
                if (!m_prepared)
                    throw new FrameworkException("Error : You must prepare the iso first");
                return m_index.GetDirectories();
            }
        }


        /// <summary>
        /// Entries (files only)
        /// </summary>
        public override IEnumerable<DataTrackIndexEntry> FileEntries
        {
            get
            {
                if (!m_prepared)
                    throw new FrameworkException("Error : You must prepare the iso first");
                return m_index.GetFiles();
            }
        }

        /// <summary>
        /// Append version to files name (;1)
        /// </summary>
        public bool AppendVersionToFileName
        {
            get { return m_appendVersionToFileName; }
            set { m_appendVersionToFileName = value; }
        }
    }
}
