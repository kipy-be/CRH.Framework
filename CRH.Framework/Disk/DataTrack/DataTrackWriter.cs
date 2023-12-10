using CRH.Framework.Common;
using CRH.Framework.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace CRH.Framework.Disk.DataTrack
{
    public sealed class DataTrackWriter : DataTrack, ITrackWriter
    {
        private CBinaryWriter _stream;

        private bool _prepared;
        private bool _finalized;
        private bool _appendVersionToFileName;

        private static Regex _regDirectoryName = new Regex("[\\/]([^\\/]+)[\\/]?$");
        private static Regex _regFileName = new Regex("[\\/]([^\\/]+?)$");

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
            _stream    = stream;
            _prepared  = false;
            _finalized = false;
            _appendVersionToFileName = true;

            try
            {
                // Allocation for system area
                WriteEmptySectors(16);
            }
            catch (FrameworkException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new FrameworkException("Error while while writing data track : unable to write the data track");
            }
        }

        /// <summary>
        /// Init the pvd and allocate some space for the path table and the root directory
        /// </summary>
        /// <param name="volumeId">The volume identifier</param>
        /// <param name="pathTableSize">Size of the path table in sector (default 1)</param>
        /// <param name="rootDirectorySize">Size of the root directory in sector (default 1)</param>
        public void Prepare(string volumeId, int pathTableSize = 1, int rootDirectorySize = 1)
        {
            try
            {
                if (_prepared)
                {
                    return;
                }

                SeekSector(16);
                WriteEmptySectors(2 + pathTableSize * 4);

                var root = new DirectoryEntry(_isXa);
                root.IsDirectory = true;
                root.ExtentSize = (uint)(rootDirectorySize * GetSectorDataSize(_defaultSectorMode));
                root.ExtentLba = (uint)SectorCount;

                _index = new DataTrackIndex(root);

                _primaryVolumeDescriptor = new PrimaryVolumeDescriptor(1);
                _primaryVolumeDescriptor.VolumeId = volumeId;
                _primaryVolumeDescriptor.PathTableSize = (uint)(pathTableSize * GetSectorDataSize(_defaultSectorMode));

                // The root directory included in the volume descriptor doesn't allow XA, so let's create a separated one
                _primaryVolumeDescriptor.RootDirectoryEntry = new DirectoryEntry(false);
                _primaryVolumeDescriptor.RootDirectoryEntry.IsDirectory = true;
                _primaryVolumeDescriptor.RootDirectoryEntry.ExtentSize = root.ExtentSize;
                _primaryVolumeDescriptor.RootDirectoryEntry.ExtentLba = root.ExtentLba;

                WriteEmptySectors(rootDirectorySize);

                _prepared = true;
            }
            catch (FrameworkException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new FrameworkException("Errow while preparing track : unable to prepare data track");
            }
        }

        /// <summary>
        /// Finalise the track
        /// </summary>
        public void FinalizeTrack()
        {
            try
            {
                if (_finalized)
                {
                    return;
                }

                if (!_prepared)
                {
                    throw new FrameworkException("Error while finalizing ISO : DataTrack has not been prepared, it will be unreadable");
                }

                // Write 2 minutes of empty sectors at the end of the track
                SeekSector(SectorCount);
                WriteEmptySectors(150);

                _finalized = true;
            }
            catch (FrameworkException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new FrameworkException("Errow while finalizing track : unable to finalize data track");
            }
        }

        /// <summary>
        /// Dump descriptors, path table, directory entries, etc.
        /// </summary>
        public void FinaliseFileSystem()
        {
            uint pathTableSectorSize = (uint)(_primaryVolumeDescriptor.PathTableSize / GetSectorDataSize(_defaultSectorMode));
            _primaryVolumeDescriptor.VolumeSpaceSize = (uint)SectorCount;
            _primaryVolumeDescriptor.TypeLPathTableLBA = 16 + 2;
            _primaryVolumeDescriptor.TypeMPathTableLBA = 16 + 2 + pathTableSectorSize * 2;

            if (_hasOptionalPathTable)
            {
                _primaryVolumeDescriptor.OptTypeLPathTableLBA = 16 + 2 + pathTableSectorSize;
                _primaryVolumeDescriptor.OptTypeMPathTableLBA = 16 + 2 + pathTableSectorSize * 3;
            }

            // Write directory entries
            WriteDirectoryEntry(_index.Root);
            foreach (var entry in _index.GetDirectories(DataTrackEntriesOrder.DEFAULT))
            {
                WriteDirectoryEntry(entry);
            }

            // Write path tables
            WritePathTables();

            // Write descriptors
            SeekSector(16);
            WriteSector(GetPrimaryVolumeDescriptorBuffer(), _defaultSectorMode, XaSubHeader.EndOfRecord);
            WriteSector(GetSetTerminatorVolumeDescriptorBuffer(), _defaultSectorMode, XaSubHeader.EndOfFile);
        }

        /// <summary>
        /// Write out the directory entry
        /// </summary>
        private void WriteDirectoryEntry(DataTrackIndexEntry entry)
        {
            SeekSector(entry.Lba);

            int size = (int)entry.Size;
            int sectorSize = GetSectorDataSize(_defaultSectorMode);
            byte[] data = new byte[size];

            using (var stream = new CBinaryWriter(data))
            {
                // First directory entry of a directory entry is the directory itself
                stream.Write(GetDirectoryEntryBuffer(entry.DirectoryEntry, true, false));

                // Second directory entry is the parent directory entry.
                if (entry.ParentEntry != null)
                {
                    stream.Write(GetDirectoryEntryBuffer(entry.ParentEntry.DirectoryEntry, false, true));
                }
                else
                {
                    stream.Write(GetDirectoryEntryBuffer(entry.DirectoryEntry, false, true));
                }

                foreach (var subEntry in entry.SubEntries)
                {
                    // DirectoryEntry cannot be "splitted" on two sectors
                    if ((stream.Position - (stream.Position / sectorSize) * sectorSize) + subEntry.Length >= sectorSize)
                    {
                        stream.Position = ((stream.Position / sectorSize) + 1) * sectorSize;
                    }

                    if (stream.Position + subEntry.DirectoryEntry.Length < size)
                    {
                        stream.Write(GetDirectoryEntryBuffer(subEntry.DirectoryEntry));
                    }
                    else
                    {
                        throw new FrameworkException("Error while finalizing disk : directory \"{0}\" is too small", entry.FullPath);
                    }
                }
            }

            for (int i = 0; i < size; i += sectorSize)
            {
                WriteSector
                (
                    CBuffer.Create(data, i, sectorSize),
                    _defaultSectorMode,
                    (i + sectorSize >= size) ? XaSubHeader.EndOfFile : XaSubHeader.Basic
                );
            }
        }

        /// <summary>
        /// Write out the path tables
        /// </summary>
        private void WritePathTables()
        {
            int sectorSize = GetSectorDataSize(_defaultSectorMode);

            byte[] lePathTableData = new byte[_primaryVolumeDescriptor.PathTableSize];
            byte[] bePathTableData = new byte[_primaryVolumeDescriptor.PathTableSize];
            var dNums = new Dictionary<string, ushort>();
            ushort dNum = 0, refNum;
            int totalSize = 0;

            using (var lePathTableStream = new CBinaryWriter(lePathTableData))
            using (var bePathTableStream = new CBinaryWriter(bePathTableData))
            {
                lePathTableStream.Write(GetPathTableEntryBuffer(_index.Root.DirectoryEntry, PathTableType.LE, 1));
                bePathTableStream.Write(GetPathTableEntryBuffer(_index.Root.DirectoryEntry, PathTableType.BE, 1));
                dNums.Add(_index.Root.FullPath, ++dNum);
                totalSize += (8 + _index.Root.DirectoryEntry.Name.Length + (_index.Root.DirectoryEntry.Name.Length % 2 != 0 ? 1 : 0));

                foreach (var entry in _index.GetDirectories(DataTrackEntriesOrder.DEFAULT))
                {
                    refNum = dNums[entry.ParentEntry.FullPath];
                    lePathTableStream.Write(GetPathTableEntryBuffer(entry.DirectoryEntry, PathTableType.LE, refNum));
                    bePathTableStream.Write(GetPathTableEntryBuffer(entry.DirectoryEntry, PathTableType.BE, refNum));
                    dNums.Add(entry.FullPath, ++dNum);
                    totalSize += (8 + entry.DirectoryEntry.Name.Length + (entry.DirectoryEntry.Name.Length % 2 != 0 ? 1 : 0));
                }
            }

            WritePathTable(_primaryVolumeDescriptor.TypeLPathTableLBA, lePathTableData);
            WritePathTable(_primaryVolumeDescriptor.TypeMPathTableLBA, bePathTableData);
            if(_hasOptionalPathTable)
            {
                WritePathTable(_primaryVolumeDescriptor.OptTypeLPathTableLBA, lePathTableData);
                WritePathTable(_primaryVolumeDescriptor.OptTypeMPathTableLBA, bePathTableData);
            }

            _primaryVolumeDescriptor.PathTableSize = (uint)totalSize;
        }

        /// <summary>
        /// Write out path table
        /// </summary>
        /// <param name="lba"></param>
        /// <param name="data"></param>
        private void WritePathTable(uint lba, byte[] data)
        {
            SeekSector(lba);
            int sectorSize = GetSectorDataSize(_defaultSectorMode);

            for (int i = 0; i < data.Length; i += sectorSize)
            {
                WriteSector
                (
                    CBuffer.Create(data, i, sectorSize),
                    _defaultSectorMode,
                    (i + sectorSize >= data.Length) ? XaSubHeader.EndOfFile : XaSubHeader.Basic
                );
            }
        }

        /// <summary>
        /// Write a sector
        /// </summary>
        /// <param name="data">The sector's data to write</param>
        /// <param name="mode">Sector's mode</param>
        /// <param name="subHeader">Subheader (if mode XA_FORM1 or XA_FORM2)</param>
        public void WriteSector(byte[] data, SectorMode mode, XaSubHeader subHeader = null)
        {
            try
            {
                byte[] buffer = new byte[_sectorSize];
                using (var bufferStream = new CBinaryWriter(buffer))
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
                        {
                            subHeader = new XaSubHeader();
                        }
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
                    {
                        EccEdc.EccEdcCompute(buffer, mode);
                    }

                    _stream.Write(buffer);
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
        public void WriteSector(long lba, byte[] data, SectorMode mode, XaSubHeader subHeader = null)
        {
            SeekSector(lba);
            WriteSector(data, mode, subHeader);
        }

        /// <summary>
        /// Write a sector in default mode
        /// </summary>
        /// <param name="data">The sector to write</param>
        /// <param name="subHeader">Subheader (if mode XA_FORM1 or XA_FORM2)</param>
        public void WriteSector(byte[] data, XaSubHeader subHeader = null)
        {
            WriteSector(data, _defaultSectorMode, subHeader);
        }

        /// <summary>
        /// Write a sector at the specified lba
        /// </summary>
        /// <param name="lba">Sector's LBA</param>
        /// <param name="sector">The sector to write</param>
        /// <param name="subHeader">Subheader (if mode XA_FORM1 or XA_FORM2)</param>
        public void WriteSector(long lba, byte[] data, XaSubHeader subHeader = null)
        {
            SeekSector(lba);
            WriteSector(data, _defaultSectorMode, subHeader);
        }

        /// <summary>
        /// Write an empty sector
        /// </summary>
        public void WriteEmptySector()
        {
            if (_mode == DataTrackMode.RAW)
            {
                WriteSector(new byte[2048], SectorMode.RAW);
            }
            else
            {
                WriteSector(new byte[2048], SectorMode.MODE0);
            }
        }

        /// <summary>
        /// Write empty sectors
        /// </summary>
        /// <param name="count">Number of sectors to write</param>
        public void WriteEmptySectors(int count)
        {
            byte[] data = new byte[2048];
            if (_mode == DataTrackMode.RAW)
            {
                for (int i = 0; i < count; i++)
                {
                    WriteSector(data, SectorMode.RAW);
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    WriteSector(data, SectorMode.MODE0);
                }
            }
        }

        /// <summary>
        /// Copy sectors from another disk
        /// </summary>
        /// <param name="diskIn">The disk to copy sectors from</param>
        /// <param name="mode">Sector's mode</param>
        /// <param name="count">Number of sectors to copy</param>
        public void CopySectors(DataTrackReader diskIn, SectorMode mode, int count)
        {
            if (diskIn.IsXa)
            {
                XaSubHeader subHeader;
                for (int i = 0; i < count; i++)
                {
                    WriteSector(diskIn.ReadSector(mode, out subHeader), mode, subHeader);
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    WriteSector(diskIn.ReadSector());
                }
            }
        }

        /// <summary>
        /// Copy sectors from another disk
        /// </summary>
        /// <param name="reader">The disk to copy sectors from</param>
        /// <param name="mode">Sector's mode</param>
        /// <param name="readerLba">Starting LBA for reading</param>
        /// <param name="writerLba">Starting LBA for writing</param>
        /// <param name="count">Number of sectors to copy</param>
        public void CopySectors(DataTrackReader reader, SectorMode mode, long readerLba, long writerLba, int count)
        {
            SeekSector(writerLba);
            reader.SeekSector(readerLba);
            CopySectors(reader, mode, count);
        }

        /// <summary>
        /// Get a primary volume descriptor data
        /// </summary>
        /// <param name="m_primaryVolumeDescriptor">The primary volume descriptor</param>
        private byte[] GetPrimaryVolumeDescriptorBuffer()
        {
            byte[] buffer = new byte[GetSectorDataSize(_defaultSectorMode)];
            try
            {
                using (var stream = new CBinaryWriter(buffer))
                {
                    stream.Write((byte)_primaryVolumeDescriptor.Type);
                    stream.WriteAsciiString(_primaryVolumeDescriptor.Id);
                    stream.Write(_primaryVolumeDescriptor.Version);
                    stream.Write(_primaryVolumeDescriptor.Unused1);
                    stream.WriteAsciiString(_primaryVolumeDescriptor.SystemId, 32, " ");
                    stream.WriteAsciiString(_primaryVolumeDescriptor.VolumeId, 32, " ");
                    stream.Write(_primaryVolumeDescriptor.Unused2);

                    stream.Write(_primaryVolumeDescriptor.VolumeSpaceSize);
                    stream.WriteBE(_primaryVolumeDescriptor.VolumeSpaceSize);

                    stream.Write(_primaryVolumeDescriptor.Unused3);

                    stream.Write(_primaryVolumeDescriptor.VolumeSetSize);
                    stream.WriteBE(_primaryVolumeDescriptor.VolumeSetSize);

                    stream.Write(_primaryVolumeDescriptor.VolumeSequenceNumber);
                    stream.WriteBE(_primaryVolumeDescriptor.VolumeSequenceNumber);

                    stream.Write(_primaryVolumeDescriptor.LogicalBlockSize);
                    stream.WriteBE(_primaryVolumeDescriptor.LogicalBlockSize);

                    stream.Write(_primaryVolumeDescriptor.PathTableSize);
                    stream.WriteBE(_primaryVolumeDescriptor.PathTableSize);

                    stream.Write(_primaryVolumeDescriptor.TypeLPathTableLBA);
                    stream.Write(_primaryVolumeDescriptor.OptTypeLPathTableLBA);
                    stream.WriteBE(_primaryVolumeDescriptor.TypeMPathTableLBA);
                    stream.WriteBE(_primaryVolumeDescriptor.OptTypeMPathTableLBA);
                    stream.Write(GetDirectoryEntryBuffer(_primaryVolumeDescriptor.RootDirectoryEntry));

                    // TODO : cas des fichiers
                    stream.WriteAsciiString(_primaryVolumeDescriptor.VolumeSetId, 128, " ");
                    stream.WriteAsciiString(_primaryVolumeDescriptor.PublisherId, 128, " ");
                    stream.WriteAsciiString(_primaryVolumeDescriptor.PreparerId, 128, " ");
                    stream.WriteAsciiString(_primaryVolumeDescriptor.ApplicationId, 128, " ");
                    stream.WriteAsciiString(_primaryVolumeDescriptor.CopyrightFileId, 38, " ");
                    stream.WriteAsciiString(_primaryVolumeDescriptor.AbstractFileId, 36, " ");
                    stream.WriteAsciiString(_primaryVolumeDescriptor.BibliographicFileId, 37, " ");
                    //

                    stream.Write(VolumeDescriptor.FromDateTime(_primaryVolumeDescriptor.CreationDate));
                    stream.Write(VolumeDescriptor.FromDateTime(_primaryVolumeDescriptor.ModificationDate));
                    stream.Write(VolumeDescriptor.FromDateTime(_primaryVolumeDescriptor.ExpirationDate));
                    stream.Write(VolumeDescriptor.FromDateTime(_primaryVolumeDescriptor.EffectiveDate));
                    stream.Write(_primaryVolumeDescriptor.FileStructureVersion);
                    stream.Write(_primaryVolumeDescriptor.Unused4);

                    if (_isXa)
                    {
                        using (CBinaryWriter appDataStream = new CBinaryWriter(_primaryVolumeDescriptor.ApplicationData))
                        {
                            appDataStream.Position = 0x8D;
                            appDataStream.WriteAsciiString(VolumeDescriptor.VOLUME_XA);
                        }
                    }

                    stream.Write(_primaryVolumeDescriptor.ApplicationData);
                    stream.Write(_primaryVolumeDescriptor.Reserved);
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
            byte[] buffer = new byte[GetSectorDataSize(_defaultSectorMode)];
            try
            {
                using (var stream = new CBinaryWriter(buffer))
                {
                    var descriptor = new SetTerminatorVolumeDescriptor();
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
                using (var stream = new CBinaryWriter(buffer))
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
                        {
                            stream.Write((byte)0);
                        }
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

            using (var stream = new CBinaryWriter(buffer))
            {
                stream.Write((byte)entry.Name.Length);
                stream.Write(entry.ExtendedAttributeRecordlength);

                if (type == PathTableType.LE)
                {
                    stream.Write(entry.ExtentLba);
                    stream.Write(parentNumber);
                }
                else
                {
                    stream.WriteBE(entry.ExtentLba);
                    stream.WriteBE(parentNumber);
                }
                
                stream.WriteAsciiString(entry.Name);

                if (entry.Name.Length % 2 != 0)
                {
                    stream.Write((byte)0);
                }
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
            if (_index.GetEntry(path) != null)
            {
                throw new FrameworkException("Error while creating directory \"{0}\" : entry already exists", path);
            }

            var parent = _index.FindAParent(path);
            if (parent == null)
            {
                throw new FrameworkException("Error while creating directory \"{0}\" : parent directory does not exists", path);
            }

            var entry = new DirectoryEntry(_isXa);
            entry.IsDirectory    = true;
            entry.Name           = _regDirectoryName.Match(path).Groups[1].Value;
            entry.Length        += (byte)(entry.Name.Length - 1);
            entry.Length        += (byte)(entry.Name.Length % 2 == 0 ? 1 : 0);
            entry.ExtentSize     = (uint)(size * GetSectorDataSize(_defaultSectorMode));
            entry.ExtentLba      = (uint)SectorCount;

            if (_isXa)
            {
                entry.XaEntry.IsDirectory  = true;
                entry.XaEntry.IsForm1 = true;
            }

            var indexEntry = new DataTrackIndexEntry(parent, entry);

            _index.AddToIndex(indexEntry);

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
            CreateFileEntry(filePath, (uint)SectorCount, (uint)stream.Length);

            stream.Position = 0;
            SeekSector(SectorCount);
            int dataSize = GetSectorDataSize(_defaultSectorMode);
            int dataRead;
            byte[] buffer = new byte[dataSize];

            while (stream.Position < stream.Length)
            {
                dataRead = stream.Read(buffer, 0, dataSize);

                if (dataRead < dataSize)
                {
                    for (int i = dataRead; i < dataSize; i++)
                    {
                        buffer[i] = 0;
                    }
                }

                WriteSector
                (
                    buffer,
                    (stream.Position + dataSize >= stream.Length) ? XaSubHeader.EndOfFile : XaSubHeader.Basic
                );
            }
        }

        /// <summary>
        /// Create a file entry for a given content
        /// </summary>
        /// <param name="filePath">The full file path of the file (eg : /FOO/BAR/FILE.EXT)</param>
        /// <param name="lba">The LBA of the file's content</param>
        /// <param name="size">The size of the file's content</param>
        public void CreateFileEntry(string filePath, uint lba, uint size)
        {
            if (_index.GetEntry(filePath) != null)
            {
                throw new FrameworkException("Error while creating file \"{0}\" : entry already exists", filePath);
            }

            var parent = _index.FindAParent(filePath);
            if (parent == null)
            {
                throw new FrameworkException("Error while creating file \"{0}\" : parent directory does not exists", filePath);
            }

            var entry = new DirectoryEntry(_isXa);
            entry.Name = _regFileName.Match(filePath).Groups[1].Value + (_appendVersionToFileName ? ";1" : "");
            entry.Length += (byte)(entry.Name.Length - 1);
            entry.Length += (byte)(entry.Name.Length % 2 == 0 ? 1 : 0);
            entry.ExtentSize = size;
            entry.ExtentLba = lba;

            if (_isXa)
            {
                entry.XaEntry.IsForm1 = true;
            }

            var indexEntry = new DataTrackIndexEntry(parent, entry);
            _index.AddToIndex(indexEntry);
        }

        /// <summary>
        /// Set the file content for a file entry
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="lba"></param>
        /// <param name="size"></param>
        public void SetFileContent(string filePath, uint lba, uint size)
        {
            var entry = _index.GetEntry(filePath);

            if (entry == null)
            {
                throw new FrameworkException("Error while setting file content of \"{0}\" : entry does not exists", filePath);
            }

            entry.DirectoryEntry.ExtentLba  = lba;
            entry.DirectoryEntry.ExtentSize = size;
        }

        /// <summary>
        /// Copy a stream (XA sound, str video, etc)
        /// </summary>
        /// <param name="filePath">The full file path of the file to write (eg : /FOO/BAR/FILE.EXT)</param>
        /// <param name="diskIn">The disk to copy stream's sectors from</param>
        /// <param name="entry">The entry of the reading disk</param>
        public void CopyStream(string filePath, DataTrackReader diskIn, DataTrackIndexEntry inEntry)
        {
            if (_index.GetEntry(filePath) != null)
            {
                throw new FrameworkException("Error while creating file \"{0}\" : entry already exists", filePath);
            }

            var parent = _index.FindAParent(filePath);
            if (parent == null)
            {
                throw new FrameworkException("Error while creating file \"{0}\" : parent directory does not exists", filePath);
            }

            var entry = new DirectoryEntry(_isXa);
            entry.Name = _regFileName.Match(filePath).Groups[1].Value + (_appendVersionToFileName ? ";1" : "");
            entry.Length += (byte)(entry.Name.Length - 1);
            entry.Length += (byte)(entry.Name.Length % 2 == 0 ? 1 : 0);
            entry.ExtentSize = (uint)inEntry.Size;
            entry.ExtentLba = (uint)SectorCount;

            var indexEntry = new DataTrackIndexEntry(parent, entry);
            _index.AddToIndex(indexEntry);

            CopySectors
            (
                diskIn,
                _defaultSectorMode == SectorMode.XA_FORM1
                        ? SectorMode.XA_FORM2
                        : _defaultSectorMode,
                entry.ExtentLba, inEntry.Lba,
                (int)(inEntry.Size / GetSectorDataSize(diskIn.DefautSectorMode))
            );
        }

        /// <summary>
        /// Copy the system-reserved zone of the iso (16 first sectors)
        /// </summary>
        /// <param name="diskIn">The disk to copy sector's from</param>
        public void CopySystemZone(DataTrackReader diskIn)
        {
            CopySectors(diskIn, _defaultSectorMode, 0, 0, 16);
        }

        /// <summary>
        /// Copy the application-reserved zone of the iso (pvd's Application Data) 
        /// </summary>
        /// <param name="diskIn"></param>
        public void CopyApplicationData(DataTrackReader diskIn)
        {
            CBuffer.Copy(diskIn.PrimaryVolumeDescriptor.ApplicationData, _primaryVolumeDescriptor.ApplicationData);
        }

        /// <summary>
        /// Is the track finalized
        /// </summary>
        public bool IsFinalized => _finalized;

        /// <summary>
        /// Entries
        /// </summary>
        public override IEnumerable<DataTrackIndexEntry> Entries
        {
            get
            {
                if (!_prepared)
                {
                    throw new FrameworkException("Error : You must prepare the iso first");
                }

                return _index.GetEntries();
            }
        }

        /// <summary>
        /// Entries (directories only)
        /// </summary>
        public override IEnumerable<DataTrackIndexEntry> DirectoryEntries
        {
            get
            {
                if (!_prepared)
                {
                    throw new FrameworkException("Error : You must prepare the iso first");
                }

                return _index.GetDirectories();
            }
        }


        /// <summary>
        /// Entries (files only)
        /// </summary>
        public override IEnumerable<DataTrackIndexEntry> FileEntries
        {
            get
            {
                if (!_prepared)
                {
                    throw new FrameworkException("Error : You must prepare the iso first");
                }

                return _index.GetFiles();
            }
        }

        /// <summary>
        /// Number of entries
        /// </summary>
        public override int EntriesCount
        {
            get
            {
                if (!_prepared)
                {
                    throw new FrameworkException("Error : You must prepare the iso first");
                }

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
                if (!_prepared)
                {
                    throw new FrameworkException("Error : You must prepare the iso first");
                }

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
                if (!_prepared)
                {
                    throw new FrameworkException("Error : You must prepare the iso first");
                }

                return _index.FileEntriesCount;
            }
        }

        /// <summary>
        /// Append version to files name (;1)
        /// </summary>
        public bool AppendVersionToFileName
        {
            get => _appendVersionToFileName;
            set => _appendVersionToFileName = value;
        }
    }
}
