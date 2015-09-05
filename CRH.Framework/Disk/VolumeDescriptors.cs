using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CRH.Framework.Common;
using CRH.Framework.IO;

namespace CRH.Framework.Disk
{
    /// <summary>
    /// Volume descriptor type
    /// </summary>
    public enum VolumeDescriptorType : byte
    {
        BOOT           = 0x00,
        PRIMARY        = 0x01,
        SUPPLEMENTARY  = 0x02,
        PARTITION      = 0x03,
        SET_TERMINATOR = 0xFF
    }

    /// <summary>
    /// VolumeDescriptor (Base)
    /// </summary>
    public abstract class VolumeDescriptor
    {
        public const string VOLUME_ID = "CD001";
        public const string VOLUME_XA = "CD-XA001";

        protected VolumeDescriptorType m_type;
        protected string m_id;
        protected byte   m_version;

    // Constructors

        internal VolumeDescriptor(VolumeDescriptorType type, byte version)
        {
            m_type    = type;
            m_version = version;
            m_id      = VOLUME_ID;
        }

    // Methods

        /// <summary>
        /// Convert the specific datetime format of descriptor to DateTime
        /// </summary>
        /// <param name="value">The buffer to read</param>
        internal static DateTime ToDateTime(byte[] buffer)
        {
            if (buffer.Length == 17)
            {
                String value = Encoding.ASCII.GetString(buffer, 0, 16);
                byte timeZone = buffer[16];
                if (value.Equals("0000000000000000"))
                    return DateTime.MinValue;
                else
                {
                    DateTime date = new DateTime
                    (
                        Int32.Parse(value.Substring(0, 4)),       // Year (1 to 9999)
                        Int32.Parse(value.Substring(4, 2)),       // Month (1 to 12)
                        Int32.Parse(value.Substring(6, 2)),       // Day (1 to 31)
                        Int32.Parse(value.Substring(8, 2)),       // Hours (0 to 23)
                        Int32.Parse(value.Substring(10, 2)),      // Minutes (0 to 59)
                        Int32.Parse(value.Substring(12, 2)),      // Seconds (0 to 59)
                        Int32.Parse(value.Substring(14, 2)) * 10  // Hundredth of seconds (0 to 99)
                    );

                    // There's also a timezone, but realy... who cares ?
                    // Just for info, format is :
                    // int8 with a value range of 0 to 100 (0 = -48 to 100 = 52, the value is then multiplied by 15 to obtain the timezone in minutes)
                    return (DateTime)date;
                }
            }
            else
                return DateTime.MinValue;
        }

    // Accessors

        /// <summary>
        /// Type of Volume Descriptor
        /// </summary>
        public VolumeDescriptorType Type
        {
            get { return m_type; }
            set { m_type = value; }
        }

        /// <summary>
        /// Id
        /// Size : 5 bytes
        /// Value : always "CD001"
        /// </summary>
        public string Id
        {
            get { return m_id; }
            set { m_id = value; }
        }

        /// <summary>
        /// Version
        /// Value : always 0x01
        /// </summary>
        public byte Version
        {
            get { return m_version; }
            set { m_version = value; }
        }
    }

    /// <summary>
    /// Primary Volume Descriptor (PVD)
    /// </summary>
    public sealed class PrimaryVolumeDescriptor : VolumeDescriptor
    {
        private byte     m_unused1;
        private string   m_systemId;
        private string   m_volumeId;
        private byte[]   m_unused2;
        private uint     m_volumeSpaceSize;
        private byte[]   m_unused3;
        private ushort   m_volumeSetSize;
        private ushort   m_volumeSequenceNumber;
        private ushort   m_logicalBlockSize;
        private uint     m_pathTableSize;
        private uint     m_typeLPathTableLBA;
        private uint     m_optTypeLPathTableLBA;
        private uint     m_typeMPathTableLBA;
        private uint     m_optTypeMPathTableLBA;
        private DirectoryEntry m_rootDirectoryEntry;
        private string   m_volumeSetId;
        private string   m_publisherId;
        private string   m_preparerId;
        private string   m_applicationId;
        private string   m_copyrightFileId;
        private string   m_abstractFileId;
        private string   m_bibliographicFileId;
        private DateTime m_creationDate;
        private DateTime m_modificationDate;
        private DateTime m_expirationDate;
        private DateTime m_effectiveDate;
        private byte     m_fileStructureVersion;
        private byte     m_unused4;
        private byte[]   m_applicationData;
        private byte[]   m_reserved;

    // Constructors

        internal PrimaryVolumeDescriptor(byte version)
            : base(VolumeDescriptorType.PRIMARY, version)
        {
            m_unused1              = 0;
            m_systemId             = "";
            m_volumeId             = "";
            m_unused2              = new byte[8];
            m_volumeSpaceSize      = 0;
            m_unused3              = new byte[32];
            m_volumeSetSize        = 1;
            m_volumeSequenceNumber = 1;
            m_logicalBlockSize     = 2048;
            m_pathTableSize        = 0;
            m_typeLPathTableLBA    = 0;
            m_optTypeLPathTableLBA = 0;
            m_typeMPathTableLBA    = 0;
            m_optTypeMPathTableLBA = 0;
            m_volumeSetId          = "";
            m_publisherId          = "";
            m_preparerId           = "";
            m_applicationId        = "";
            m_copyrightFileId      = "";
            m_abstractFileId       = "";
            m_bibliographicFileId  = "";
            m_creationDate         = DateTime.Now;
            m_fileStructureVersion = 1;
            m_unused4              = 0;
            m_applicationData      = new byte[512];
            m_reserved             = new byte[653];
        }

    // Accessors

        /// <summary>
        /// Unused
        /// </summary>
        public byte Unused1
        {
            get { return m_unused1; }
            set { m_unused1 = value; }
        }

        /// <summary>
        /// The name of the system that the disk target (eg Playstation)
        /// Size : 32 bytes
        /// </summary>
        public string SystemId
        {
            get { return m_systemId; }
            set { m_systemId = value; }
        }

        /// <summary>
        /// Identifier of the disk
        /// Size : 32 bytes
        /// </summary>
        public string VolumeId
        {
            get { return m_volumeId; }
            set { m_volumeId = value; }
        }

        /// <summary>
        /// Unused
        /// Size : 8 bytes
        /// </summary>
        public byte[] Unused2
        {
            get { return m_unused2; }
            set { m_unused2 = value; }
        }

        /// <summary>
        /// Size of the disk (Number of logical sectors)
        /// </summary>
        public uint VolumeSpaceSize
        {
            get { return m_volumeSpaceSize; }
            set { m_volumeSpaceSize = value; }
        }

        /// <summary>
        /// Unused
        /// Size : 32 bytes
        /// </summary>
        public byte[] Unused3
        {
            get { return m_unused3; }
            set { m_unused3 = value; }
        }

        /// <summary>
        /// Total number of disk(s)
        /// </summary>
        public ushort VolumeSetSize
        {
            get { return m_volumeSetSize; }
            set { m_volumeSetSize = value; }
        }

        /// <summary>
        /// Disk number
        /// </summary>
        public ushort VolumeSequenceNumber
        {
            get { return m_volumeSequenceNumber; }
            set { m_volumeSequenceNumber = value; }
        }

        /// <summary>
        /// User's data size for the sector
        /// </summary>
        public ushort LogicalBlockSize
        {
            get { return m_logicalBlockSize; }
            set { m_logicalBlockSize = value; }
        }

        /// <summary>
        /// Size of path table (in bytes)
        /// </summary>
        public uint PathTableSize
        {
            get { return m_pathTableSize; }
            set { m_pathTableSize = value; }
        }

        /// <summary>
        /// LBA of the path table which data are stored only in little endian
        /// </summary>
        public uint TypeLPathTableLBA
        {
            get { return m_typeLPathTableLBA; }
            set { m_typeLPathTableLBA = value; }
        }

        /// <summary>
        /// LBA of the optional path table which data are stored only in little endian
        /// Value : 0x00 if no optional path table
        /// </summary>
        public uint OptTypeLPathTableLBA
        {
            get { return m_optTypeLPathTableLBA; }
            set { m_optTypeLPathTableLBA = value; }
        }

        /// <summary>
        /// LBA of the path table which data are stored only in big endian
        /// </summary>
        public uint TypeMPathTableLBA
        {
            get { return m_typeMPathTableLBA; }
            set { m_typeMPathTableLBA = value; }
        }

        /// <summary>
        /// LBA of the optional path table which data are stored only in big endian
        /// Value : 0x00 if no optional path table
        /// </summary>
        public uint OptTypeMPathTableLBA
        {
            get { return m_optTypeMPathTableLBA; }
            set { m_optTypeMPathTableLBA = value; }
        }

        /// <summary>
        /// Directory entry for root
        /// Size : 34 bytes
        /// </summary>
        public DirectoryEntry RootDirectoryEntry
        {
            get { return m_rootDirectoryEntry; }
            set { m_rootDirectoryEntry = value; }
        }

        /// <summary>
        /// Identifier of the volume set (if several disks)
        /// Size : 128 bytes
        /// </summary>
        public string VolumeSetId
        {
            get { return m_volumeSetId; }
            set { m_volumeSetId = value; }
        }

        /// <summary>
        /// Identifier of the publisher
        /// Size : 128 bytes
        /// Value : name, file or empty (padded with 0x20)
        /// If file, begins with 0x5F followed by the name of file (located at the root directory)
        /// </summary>
        public string PublisherId
        {
            get { return m_publisherId; }
            set { m_publisherId = value; }
        }

        /// <summary>
        /// Identifier of the preparer(s)
        /// Size : 128 bytes
        /// Value : name, file or empty (padded with 0x20)
        /// If file, begins with 0x5F followed by the name of file (located at the root directory)
        /// </summary>
        public string PreparerId
        {
            get { return m_preparerId; }
            set { m_preparerId = value; }
        }

        /// <summary>
        /// Identifier of the application
        /// Size : 128 bytes
        /// Value : name, file or empty (padded with 0x20)
        /// If file, begins with 0x5F followed by the name of file (located at the root directory)
        /// </summary>
        public string ApplicationId
        {
            get { return m_applicationId; }
            set { m_applicationId = value; }
        }

        /// <summary>
        /// Name of file that contains some copyright informations
        /// Size : 38 bytes
        /// Value : file or empty (padded with 0x20)
        /// If file, the name of file (located at the root directory)
        /// </summary>
        public string CopyrightFileId
        {
            get { return m_copyrightFileId; }
            set { m_copyrightFileId = value; }
        }

        /// <summary>
        /// Name of file that contains some additionnal informations about the volume
        /// Size : 36 bytes
        /// Value : file or empty (padded with 0x20)
        /// If file, the name of file (located at the root directory)
        /// </summary>
        public string AbstractFileId
        {
            get { return m_abstractFileId; }
            set { m_abstractFileId = value; }
        }

        /// <summary>
        /// Name of file that contains bibliographic informations about the volume
        /// Size : 37 bytes
        /// Value : file or empty (padded with 0x20)
        /// If file, the name of file (located at the root directory)
        /// </summary>
        public string BibliographicFileId
        {
            get { return m_bibliographicFileId; }
            set { m_bibliographicFileId = value; }
        }

        /// <summary>
        /// Creation date
        /// </summary>
        public DateTime CreationDate
        {
            get { return m_creationDate; }
            set { m_creationDate = value; }
        }

        /// <summary>
        /// Modification date
        /// </summary>
        public DateTime ModificationDate
        {
            get { return m_modificationDate; }
            set { m_modificationDate = value; }
        }

        /// <summary>
        /// Optional expiration date after wich data are considered obsolete
        /// </summary>
        public DateTime ExpirationDate
        {
            get { return m_expirationDate; }
            set { m_expirationDate = value; }
        }

        /// <summary>
        /// Optional effective date after which data may be used
        /// </summary>
        public DateTime EffectiveDate
        {
            get { return m_effectiveDate; }
            set { m_effectiveDate = value; }
        }

        /// <summary>
        /// Structure version
        /// Value : always 0x01
        /// </summary>
        public byte FileStructureVersion
        {
            get { return m_fileStructureVersion; }
            set { m_fileStructureVersion = value; }
        }

        /// <summary>
        /// Unused
        /// </summary>
        public byte Unused4
        {
            get { return m_unused4; }
            set { m_unused4 = value; }
        }

        /// <summary>
        /// Data that are not ISO9660-specific
        /// Size : 512 bytes
        /// </summary>
        public byte[] ApplicationData
        {
            get { return m_applicationData; }
            set { m_applicationData = value; }
        }

        /// <summary>
        /// Reserved to ISO
        /// Size : 653 bytes
        /// </summary>
        public byte[] Reserved
        {
            get { return m_reserved; }
            set { m_reserved = value; }
        }
    }

    /// <summary>
    /// Set Terminator Volume Descriptor
    /// </summary>
    public sealed class SetTerminatorVolumeDescriptor : VolumeDescriptor
    {
    // Constructors

        public SetTerminatorVolumeDescriptor()
            : base(VolumeDescriptorType.SET_TERMINATOR, 1)
        {}
    }
}