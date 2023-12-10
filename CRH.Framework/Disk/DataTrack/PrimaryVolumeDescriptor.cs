using System;

namespace CRH.Framework.Disk.DataTrack
{
    /// <summary>
    /// Primary Volume Descriptor (PVD)
    /// </summary>
    public sealed class PrimaryVolumeDescriptor : VolumeDescriptor
    {
        private byte _unused1;
        private string _systemId;
        private string _volumeId;
        private byte[] _unused2;
        private uint _volumeSpaceSize;
        private byte[] _unused3;
        private ushort _volumeSetSize;
        private ushort _volumeSequenceNumber;
        private ushort _logicalBlockSize;
        private uint _pathTableSize;
        private uint _typeLPathTableLBA;
        private uint _optTypeLPathTableLBA;
        private uint _typeMPathTableLBA;
        private uint _optTypeMPathTableLBA;
        private DirectoryEntry _rootDirectoryEntry;
        private string _volumeSetId;
        private string _publisherId;
        private string _preparerId;
        private string _applicationId;
        private string _copyrightFileId;
        private string _abstractFileId;
        private string _bibliographicFileId;
        private DateTime _creationDate;
        private DateTime _modificationDate;
        private DateTime _expirationDate;
        private DateTime _effectiveDate;
        private byte _fileStructureVersion;
        private byte _unused4;
        private byte[] _applicationData;
        private byte[] _reserved;

        // Constructors

        internal PrimaryVolumeDescriptor(byte version)
            : base(VolumeDescriptorType.PRIMARY, version)
        {
            _unused1 = 0;
            _systemId = "";
            _volumeId = "";
            _unused2 = new byte[8];
            _volumeSpaceSize = 0;
            _unused3 = new byte[32];
            _volumeSetSize = 1;
            _volumeSequenceNumber = 1;
            _logicalBlockSize = 2048;
            _pathTableSize = 0;
            _typeLPathTableLBA = 0;
            _optTypeLPathTableLBA = 0;
            _typeMPathTableLBA = 0;
            _optTypeMPathTableLBA = 0;
            _volumeSetId = "";
            _publisherId = "";
            _preparerId = "";
            _applicationId = "";
            _copyrightFileId = "";
            _abstractFileId = "";
            _bibliographicFileId = "";
            _creationDate = DateTime.Now;
            _fileStructureVersion = 1;
            _unused4 = 0;
            _applicationData = new byte[512];
            _reserved = new byte[653];
        }

        // Accessors

        /// <summary>
        /// Unused
        /// </summary>
        public byte Unused1
        {
            get => _unused1;
            set => _unused1 = value;
        }

        /// <summary>
        /// The name of the system that the disk target (eg Playstation)
        /// Size : 32 bytes
        /// </summary>
        public string SystemId
        {
            get => _systemId;
            set => _systemId = value;
        }

        /// <summary>
        /// Identifier of the disk
        /// Size : 32 bytes
        /// </summary>
        public string VolumeId
        {
            get => _volumeId;
            set => _volumeId = value;
        }

        /// <summary>
        /// Unused
        /// Size : 8 bytes
        /// </summary>
        public byte[] Unused2
        {
            get => _unused2;
            set => _unused2 = value;
        }

        /// <summary>
        /// Size of the disk (Number of logical sectors)
        /// </summary>
        public uint VolumeSpaceSize
        {
            get => _volumeSpaceSize;
            set => _volumeSpaceSize = value;
        }

        /// <summary>
        /// Unused
        /// Size : 32 bytes
        /// </summary>
        public byte[] Unused3
        {
            get => _unused3;
            set => _unused3 = value;
        }

        /// <summary>
        /// Total number of disk(s)
        /// </summary>
        public ushort VolumeSetSize
        {
            get => _volumeSetSize;
            set => _volumeSetSize = value;
        }

        /// <summary>
        /// Disk number
        /// </summary>
        public ushort VolumeSequenceNumber
        {
            get => _volumeSequenceNumber;
            set => _volumeSequenceNumber = value;
        }

        /// <summary>
        /// User's data size for the sector
        /// </summary>
        public ushort LogicalBlockSize
        {
            get => _logicalBlockSize;
            set => _logicalBlockSize = value;
        }

        /// <summary>
        /// Size of path table (in bytes)
        /// </summary>
        public uint PathTableSize
        {
            get => _pathTableSize;
            set => _pathTableSize = value;
        }

        /// <summary>
        /// LBA of the path table which data are stored only in little endian
        /// </summary>
        public uint TypeLPathTableLBA
        {
            get => _typeLPathTableLBA;
            set => _typeLPathTableLBA = value;
        }

        /// <summary>
        /// LBA of the optional path table which data are stored only in little endian
        /// Value : 0x00 if no optional path table
        /// </summary>
        public uint OptTypeLPathTableLBA
        {
            get => _optTypeLPathTableLBA;
            set => _optTypeLPathTableLBA = value;
        }

        /// <summary>
        /// LBA of the path table which data are stored only in big endian
        /// </summary>
        public uint TypeMPathTableLBA
        {
            get => _typeMPathTableLBA;
            set => _typeMPathTableLBA = value;
        }

        /// <summary>
        /// LBA of the optional path table which data are stored only in big endian
        /// Value : 0x00 if no optional path table
        /// </summary>
        public uint OptTypeMPathTableLBA
        {
            get => _optTypeMPathTableLBA;
            set => _optTypeMPathTableLBA = value;
        }

        /// <summary>
        /// Directory entry for root
        /// Size : 34 bytes
        /// </summary>
        public DirectoryEntry RootDirectoryEntry
        {
            get => _rootDirectoryEntry;
            set => _rootDirectoryEntry = value;
        }

        /// <summary>
        /// Identifier of the volume set (if several disks)
        /// Size : 128 bytes
        /// </summary>
        public string VolumeSetId
        {
            get => _volumeSetId;
            set => _volumeSetId = value;
        }

        /// <summary>
        /// Identifier of the publisher
        /// Size : 128 bytes
        /// Value : name, file or empty (padded with 0x20)
        /// If file, begins with 0x5F followed by the name of file (located at the root directory)
        /// </summary>
        public string PublisherId
        {
            get => _publisherId;
            set => _publisherId = value;
        }

        /// <summary>
        /// Identifier of the preparer(s)
        /// Size : 128 bytes
        /// Value : name, file or empty (padded with 0x20)
        /// If file, begins with 0x5F followed by the name of file (located at the root directory)
        /// </summary>
        public string PreparerId
        {
            get => _preparerId;
            set => _preparerId = value;
        }

        /// <summary>
        /// Identifier of the application
        /// Size : 128 bytes
        /// Value : name, file or empty (padded with 0x20)
        /// If file, begins with 0x5F followed by the name of file (located at the root directory)
        /// </summary>
        public string ApplicationId
        {
            get => _applicationId;
            set => _applicationId = value;
        }

        /// <summary>
        /// Name of file that contains some copyright informations
        /// Size : 38 bytes
        /// Value : file or empty (padded with 0x20)
        /// If file, the name of file (located at the root directory)
        /// </summary>
        public string CopyrightFileId
        {
            get => _copyrightFileId;
            set => _copyrightFileId = value;
        }

        /// <summary>
        /// Name of file that contains some additionnal informations about the volume
        /// Size : 36 bytes
        /// Value : file or empty (padded with 0x20)
        /// If file, the name of file (located at the root directory)
        /// </summary>
        public string AbstractFileId
        {
            get => _abstractFileId;
            set => _abstractFileId = value;
        }

        /// <summary>
        /// Name of file that contains bibliographic informations about the volume
        /// Size : 37 bytes
        /// Value : file or empty (padded with 0x20)
        /// If file, the name of file (located at the root directory)
        /// </summary>
        public string BibliographicFileId
        {
            get => _bibliographicFileId;
            set => _bibliographicFileId = value;
        }

        /// <summary>
        /// Creation date
        /// </summary>
        public DateTime CreationDate
        {
            get => _creationDate;
            set => _creationDate = value;
        }

        /// <summary>
        /// Modification date
        /// </summary>
        public DateTime ModificationDate
        {
            get => _modificationDate;
            set => _modificationDate = value;
        }

        /// <summary>
        /// Optional expiration date after wich data are considered obsolete
        /// </summary>
        public DateTime ExpirationDate
        {
            get => _expirationDate;
            set => _expirationDate = value;
        }

        /// <summary>
        /// Optional effective date after which data may be used
        /// </summary>
        public DateTime EffectiveDate
        {
            get => _effectiveDate;
            set => _effectiveDate = value;
        }

        /// <summary>
        /// Structure version
        /// Value : always 0x01
        /// </summary>
        public byte FileStructureVersion
        {
            get => _fileStructureVersion;
            set => _fileStructureVersion = value;
        }

        /// <summary>
        /// Unused
        /// </summary>
        public byte Unused4
        {
            get => _unused4;
            set => _unused4 = value;
        }

        /// <summary>
        /// Data that are not ISO9660-specific
        /// Size : 512 bytes
        /// </summary>
        public byte[] ApplicationData
        {
            get => _applicationData;
            set => _applicationData = value;
        }

        /// <summary>
        /// Reserved to ISO
        /// Size : 653 bytes
        /// </summary>
        public byte[] Reserved
        {
            get => _reserved;
            set => _reserved = value;
        }
    }
}