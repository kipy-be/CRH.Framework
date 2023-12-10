using CRH.Framework.Common;
using System;

namespace CRH.Framework.Disk.DataTrack
{
    /// <summary>
    /// Directory entry
    /// </summary>
    public sealed class DirectoryEntry
    {
        private bool     _hasXa;
        private byte     _length;
        private byte     _extendedAttributeRecordlength;
        private uint     _extentLba;
        private uint     _extentSize;
        private DateTime _date;
        private byte     _flags;
        private byte     _fileUnitSize;
        private byte     _interleave;
        private ushort   _volumeSequenceNumber;
        private string   _name;
        private XaEntry  _xaEntry;

        /// <summary>
        /// Directory entry
        /// </summary>
        /// <param name="hasXa">The entry has a XA field</param>
        public DirectoryEntry(bool hasXa = false)
        {
            _hasXa                         = hasXa;
            _length                        = 34;
            _extendedAttributeRecordlength = 0;
            _extentLba                     = 0;
            _date                          = DateTime.Now;
            _flags                         = 0;
            _fileUnitSize                  = 0;
            _interleave                    = 0;
            _volumeSequenceNumber          = 1;
            _name                          = "\0";

            if (hasXa)
            {
                _xaEntry = new XaEntry();
                _length += XaEntry.SIZE;
            }
        }

        /// <summary>
        /// Get specific flag state from Flags field
        /// </summary>
        private bool GetFlag(DirectoryEntryFlag mask)
        {
            return (_flags & (byte)mask) > 0;
        }

        /// <summary>
        /// Set flag state into Flags field
        /// </summary>
        private void SetFlag(DirectoryEntryFlag mask, bool value)
        {
            if (value)
            {
                _flags |= (byte)mask;
            }
            else
            {
                _flags &= (byte)(0xFF ^ (byte)mask);
            }
        }

        /// <summary>
        /// Got a XA entry
        /// </summary>
        internal bool HasXa
        {
            get => _hasXa;
            set => _hasXa = value;
        }

        /// <summary>
        /// Size of the entry
        /// </summary>
        public byte Length
        {
            get => _length;
            internal set => _length = value;
        }

        /// <summary>
        /// Size of the extended attribute area
        /// </summary>
        public byte ExtendedAttributeRecordlength
        {
            get => _extendedAttributeRecordlength; 
            internal set => _extendedAttributeRecordlength = value;
        }

        /// <summary>
        /// LBA of the extent (Another directory entry if directory or file's data)
        /// </summary>
        public uint ExtentLba
        {
            get => _extentLba;
            internal set => _extentLba = value;
        }

        /// <summary>
        /// Size of extent
        /// </summary>
        public uint ExtentSize
        {
            get => _extentSize;
            internal set => _extentSize = value;
        }

        /// <summary>
        /// Date
        /// </summary>
        public DateTime Date
        {
            get => _date;
            internal set => _date = value;
        }

        /// <summary>
        /// Flags
        /// </summary>
        public byte Flags
        {
            get => _flags;
            internal set => _flags = value;
        }

        /// <summary>
        /// Is a hidden file/directory
        /// </summary>
        public bool IsHidden
        {
            get => GetFlag(DirectoryEntryFlag.HIDDEN);
            internal set => SetFlag(DirectoryEntryFlag.HIDDEN, value);
        }

        /// <summary>
        /// Is a directory
        /// </summary>
        public bool IsDirectory
        {
            get => GetFlag(DirectoryEntryFlag.DIRECTORY);
            internal set => SetFlag(DirectoryEntryFlag.DIRECTORY, value);
        }

        /// <summary>
        /// Is an associated file
        /// </summary>
        public bool IsAssociatedFile
        {
            get => GetFlag(DirectoryEntryFlag.ASSOCIATED);
            internal set => SetFlag(DirectoryEntryFlag.ASSOCIATED, value);
        }

        /// <summary>
        /// Has some informations about the file itself stored in extended area
        /// </summary>
        public bool HasFormatInfosInExtended
        {
            get => GetFlag(DirectoryEntryFlag.FORMAT_IN_EXT);
            internal set => SetFlag(DirectoryEntryFlag.FORMAT_IN_EXT, value);
        }

        /// <summary>
        /// Has file/directory permissions stored in extended area (unix style)
        /// </summary>
        public bool HasPermissionsInfosInExtended
        {
            get => GetFlag(DirectoryEntryFlag.PERMS_IN_EXT);
            internal set => SetFlag(DirectoryEntryFlag.PERMS_IN_EXT, value);
        }

        /// <summary>
        /// Is final entry
        /// </summary>
        internal bool IsFinal
        {
            get => !GetFlag(DirectoryEntryFlag.NOT_FINAL);
            set => SetFlag(DirectoryEntryFlag.NOT_FINAL, !value);
        }

        /// <summary>
        /// File unit size for files written in interleaved mode
        /// Value : 0x00 if not interleave
        /// </summary>
        internal byte FileUnitSize
        {
            get => _fileUnitSize;
            set => _fileUnitSize = value;
        }

        /// <summary>
        /// Interleave gap size for files written in interleaved mode
        /// Value : 0x00 if not interleave
        /// </summary>
        internal byte Interleave
        {
            get => _interleave;
            set => _interleave = value;
        }

        /// <summary>
        /// Disk number
        /// </summary>
        internal ushort VolumeSequenceNumber
        {
            get => _volumeSequenceNumber;
            set => _volumeSequenceNumber = value;
        }

        /// <summary>
        /// Name of entry
        /// </summary>
        public string Name
        {
            get => _name;
            internal set
            {
                if (value.Length > 255)
                {
                    throw new FrameworkException("Entry name is too long");
                }

                _name = value;
            }
        }

        /// <summary>
        /// XA entry
        /// </summary>
        internal XaEntry XaEntry
        {
            get => _xaEntry;
            set
            {
                _xaEntry = value;
                _hasXa   = true;
            }
        }
    }
}