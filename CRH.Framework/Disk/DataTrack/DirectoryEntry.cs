using System;
using System.IO;
using CRH.Framework.Common;
using CRH.Framework.IO;

namespace CRH.Framework.Disk.DataTrack
{
    /// <summary>
    /// Directory entry flag mask
    /// </summary>
    internal enum DirectoryEntryFlag
    {
        HIDDEN        = 1,
        DIRECTORY     = 1 << 1,
        ASSOCIATED    = 1 << 2,
        FORMAT_IN_EXT = 1 << 3,
        PERMS_IN_EXT  = 1 << 4,
        NOT_FINAL     = 1 << 7
    }

    /// <summary>
    /// Directory entry
    /// </summary>
    public sealed class DirectoryEntry
    {
        private bool     m_hasXa;
        private byte     m_length;
        private byte     m_extendedAttributeRecordlength;
        private uint     m_extentLba;
        private uint     m_extentSize;
        private DateTime m_date;
        private byte     m_flags;
        private byte     m_fileUnitSize;
        private byte     m_interleave;
        private ushort   m_volumeSequenceNumber;
        private string   m_name;
        private XaEntry  m_xaEntry;

    // Constructors

        /// <summary>
        /// Directory entry
        /// </summary>
        /// <param name="hasXa">The entry has a XA field</param>
        public DirectoryEntry(bool hasXa = false)
        {
            m_hasXa                         = hasXa;
            m_length                        = 34;
            m_extendedAttributeRecordlength = 0;
            m_extentLba                     = 0;
            m_date                          = DateTime.Now;
            m_flags                         = 0;
            m_fileUnitSize                  = 0;
            m_interleave                    = 0;
            m_volumeSequenceNumber          = 1;
            m_name                          = "\0";

            if (hasXa)
            {
                m_xaEntry = new XaEntry();
                m_length += XaEntry.SIZE;
            }
        }

    // Methods

        /// <summary>
        /// Get specific flag state from Flags field
        /// </summary>
        private bool GetFlag(DirectoryEntryFlag mask)
        {
            return (m_flags & (byte)mask) > 0;
        }

        /// <summary>
        /// Set flag state into Flags field
        /// </summary>
        private void SetFlag(DirectoryEntryFlag mask, bool value)
        {
            if (value)
                m_flags |= (byte)mask;
            else
                m_flags &= (byte)(0xFF ^ (byte)mask);
        }

    // Accessors

        /// <summary>
        /// Got a XA entry
        /// </summary>
        internal bool HasXa
        {
            get { return m_hasXa; }
            set
            {
                m_hasXa = value;
            }
        }

        /// <summary>
        /// Size of the entry
        /// </summary>
        public byte Length
        {
            get { return m_length; }
            internal set { m_length = value; }
        }

        /// <summary>
        /// Size of the extended attribute area
        /// </summary>
        public byte ExtendedAttributeRecordlength
        {
            get { return m_extendedAttributeRecordlength; }
            internal set { m_extendedAttributeRecordlength = value; }
        }

        /// <summary>
        /// LBA of the extent (Another directory entry if directory or file's data)
        /// </summary>
        public uint ExtentLba
        {
            get { return m_extentLba; }
            internal set { m_extentLba = value; }
        }

        /// <summary>
        /// Size of extent
        /// </summary>
        public uint ExtentSize
        {
            get { return m_extentSize; }
            internal set { m_extentSize = value; }
        }

        /// <summary>
        /// Date
        /// </summary>
        public DateTime Date
        {
            get { return m_date; }
            internal set { m_date = value; }
        }

        /// <summary>
        /// Flags
        /// </summary>
        public byte Flags
        {
            get { return m_flags; }
            internal set { m_flags = value; }
        }

        /// <summary>
        /// Is a hidden file/directory
        /// </summary>
        public bool IsHidden
        {
            get { return GetFlag(DirectoryEntryFlag.HIDDEN); }
            internal set { SetFlag(DirectoryEntryFlag.HIDDEN, value); }
        }

        /// <summary>
        /// Is a directory
        /// </summary>
        public bool IsDirectory
        {
            get { return GetFlag(DirectoryEntryFlag.DIRECTORY); }
            internal set { SetFlag(DirectoryEntryFlag.DIRECTORY, value); }
        }

        /// <summary>
        /// Is an associated file
        /// </summary>
        public bool IsAssociatedFile
        {
            get { return GetFlag(DirectoryEntryFlag.ASSOCIATED); }
            internal set { SetFlag(DirectoryEntryFlag.ASSOCIATED, value); }
        }

        /// <summary>
        /// Has some informations about the file itself stored in extended area
        /// </summary>
        public bool HasFormatInfosInExtended
        {
            get { return GetFlag(DirectoryEntryFlag.FORMAT_IN_EXT); }
            internal set { SetFlag(DirectoryEntryFlag.FORMAT_IN_EXT, value); }
        }

        /// <summary>
        /// Has file/directory permissions stored in extended area (unix style)
        /// </summary>
        public bool HasPermissionsInfosInExtended
        {
            get { return GetFlag(DirectoryEntryFlag.PERMS_IN_EXT); }
            internal set { SetFlag(DirectoryEntryFlag.PERMS_IN_EXT, value); }
        }

        /// <summary>
        /// Is final entry
        /// </summary>
        internal bool IsFinal
        {
            get { return !GetFlag(DirectoryEntryFlag.NOT_FINAL); }
            set { SetFlag(DirectoryEntryFlag.NOT_FINAL, !value); }
        }

        /// <summary>
        /// File unit size for files written in interleaved mode
        /// Value : 0x00 if not interleave
        /// </summary>
        internal byte FileUnitSize
        {
            get { return m_fileUnitSize; }
            set { m_fileUnitSize = value; }
        }

        /// <summary>
        /// Interleave gap size for files written in interleaved mode
        /// Value : 0x00 if not interleave
        /// </summary>
        internal byte Interleave
        {
            get { return m_interleave; }
            set { m_interleave = value; }
        }

        /// <summary>
        /// Disk number
        /// </summary>
        internal ushort VolumeSequenceNumber
        {
            get { return m_volumeSequenceNumber; }
            set { m_volumeSequenceNumber = value; }
        }

        /// <summary>
        /// Name of entry
        /// </summary>
        public string Name
        {
            get { return m_name; }
            internal set
            {
                if (value.Length < 1)
                    throw new FrameworkException("Entry name is empty");

                if (value.Length > 0xFF)
                    throw new FrameworkException("Entry name is too long");

                m_name = value;
            }
        }

        /// <summary>
        /// XA entry
        /// </summary>
        internal XaEntry XaEntry
        {
            get { return m_xaEntry; }
            set
            {
                m_xaEntry = value;
                m_hasXa   = true;
            }
        }
    }
}