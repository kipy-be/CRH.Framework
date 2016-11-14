using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CRH.Framework.Common;
using CRH.Framework.IO;

namespace CRH.Framework.Disk.DataTrack
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

        protected VolumeDescriptorType _type;
        protected string _id;
        protected byte   _version;

    // Constructors

        internal VolumeDescriptor(VolumeDescriptorType type, byte version)
        {
            _type    = type;
            _version = version;
            _id      = VOLUME_ID;
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
                string value = Encoding.ASCII.GetString(buffer, 0, 16);
                byte timeZone = buffer[16];
                if (value.Equals("0000000000000000"))
                    return DateTime.MinValue;
                else
                {
                    var date = new DateTime
                    (
                        int.Parse(value.Substring(0, 4)),       // Year (1 to 9999)
                        int.Parse(value.Substring(4, 2)),       // Month (1 to 12)
                        int.Parse(value.Substring(6, 2)),       // Day (1 to 31)
                        int.Parse(value.Substring(8, 2)),       // Hours (0 to 23)
                        int.Parse(value.Substring(10, 2)),      // Minutes (0 to 59)
                        int.Parse(value.Substring(12, 2)),      // Seconds (0 to 59)
                        int.Parse(value.Substring(14, 2)) * 10  // Hundredth of seconds (0 to 99)
                    );

                    // There's also a timezone, but realy... who cares ?
                    // Just for info, format is :
                    // int8 with a value range of 0 to 100 (0 = -48 to 100 = 52, the value is then multiplied by 15 to obtain the timezone in minutes)
                    return date;
                }
            }
            else
                return DateTime.MinValue;
        }

        private static string DatePartToString(int value, int size)
        {
            string strValue = value.ToString();
            while(strValue.Length < size)
                strValue = "0" + strValue;
            return strValue;
        }

        /// <summary>
        /// Convert the DateTime to specific datetime format of descriptor
        /// </summary>
        /// <param name="date">The date to convert</param>
        internal static byte[] FromDateTime(DateTime date)
        {
            string value  = "";
            byte[] buffer = new byte[17];

            value += DatePartToString(date.Year, 4);
            value += DatePartToString(date.Month, 2);
            value += DatePartToString(date.Day, 2);
            value += DatePartToString(date.Hour, 2);
            value += DatePartToString(date.Minute, 2);
            value += DatePartToString(date.Second, 2);
            value += DatePartToString(date.Millisecond / 10, 2);

            CBuffer.Copy(Encoding.ASCII.GetBytes(value), buffer);

            return buffer;
        }

    // Accessors

        /// <summary>
        /// Type of Volume Descriptor
        /// </summary>
        public VolumeDescriptorType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        /// <summary>
        /// Id
        /// Size : 5 bytes
        /// Value : always "CD001"
        /// </summary>
        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        /// <summary>
        /// Version
        /// Value : always 0x01
        /// </summary>
        public byte Version
        {
            get { return _version; }
            set { _version = value; }
        }
    }

    /// <summary>
    /// Primary Volume Descriptor (PVD)
    /// </summary>
    public sealed class PrimaryVolumeDescriptor : VolumeDescriptor
    {
        private byte     _unused1;
        private string   _systemId;
        private string   _volumeId;
        private byte[]   _unused2;
        private uint     _volumeSpaceSize;
        private byte[]   _unused3;
        private ushort   _volumeSetSize;
        private ushort   _volumeSequenceNumber;
        private ushort   _logicalBlockSize;
        private uint     _pathTableSize;
        private uint     _typeLPathTableLBA;
        private uint     _optTypeLPathTableLBA;
        private uint     _typeMPathTableLBA;
        private uint     _optTypeMPathTableLBA;
        private DirectoryEntry _rootDirectoryEntry;
        private string   _volumeSetId;
        private string   _publisherId;
        private string   _preparerId;
        private string   _applicationId;
        private string   _copyrightFileId;
        private string   _abstractFileId;
        private string   _bibliographicFileId;
        private DateTime _creationDate;
        private DateTime _modificationDate;
        private DateTime _expirationDate;
        private DateTime _effectiveDate;
        private byte     _fileStructureVersion;
        private byte     _unused4;
        private byte[]   _applicationData;
        private byte[]   _reserved;

    // Constructors

        internal PrimaryVolumeDescriptor(byte version)
            : base(VolumeDescriptorType.PRIMARY, version)
        {
            _unused1              = 0;
            _systemId             = "";
            _volumeId             = "";
            _unused2              = new byte[8];
            _volumeSpaceSize      = 0;
            _unused3              = new byte[32];
            _volumeSetSize        = 1;
            _volumeSequenceNumber = 1;
            _logicalBlockSize     = 2048;
            _pathTableSize        = 0;
            _typeLPathTableLBA    = 0;
            _optTypeLPathTableLBA = 0;
            _typeMPathTableLBA    = 0;
            _optTypeMPathTableLBA = 0;
            _volumeSetId          = "";
            _publisherId          = "";
            _preparerId           = "";
            _applicationId        = "";
            _copyrightFileId      = "";
            _abstractFileId       = "";
            _bibliographicFileId  = "";
            _creationDate         = DateTime.Now;
            _fileStructureVersion = 1;
            _unused4              = 0;
            _applicationData      = new byte[512];
            _reserved             = new byte[653];
        }

    // Accessors

        /// <summary>
        /// Unused
        /// </summary>
        public byte Unused1
        {
            get { return _unused1; }
            set { _unused1 = value; }
        }

        /// <summary>
        /// The name of the system that the disk target (eg Playstation)
        /// Size : 32 bytes
        /// </summary>
        public string SystemId
        {
            get { return _systemId; }
            set { _systemId = value; }
        }

        /// <summary>
        /// Identifier of the disk
        /// Size : 32 bytes
        /// </summary>
        public string VolumeId
        {
            get { return _volumeId; }
            set { _volumeId = value; }
        }

        /// <summary>
        /// Unused
        /// Size : 8 bytes
        /// </summary>
        public byte[] Unused2
        {
            get { return _unused2; }
            set { _unused2 = value; }
        }

        /// <summary>
        /// Size of the disk (Number of logical sectors)
        /// </summary>
        public uint VolumeSpaceSize
        {
            get { return _volumeSpaceSize; }
            set { _volumeSpaceSize = value; }
        }

        /// <summary>
        /// Unused
        /// Size : 32 bytes
        /// </summary>
        public byte[] Unused3
        {
            get { return _unused3; }
            set { _unused3 = value; }
        }

        /// <summary>
        /// Total number of disk(s)
        /// </summary>
        public ushort VolumeSetSize
        {
            get { return _volumeSetSize; }
            set { _volumeSetSize = value; }
        }

        /// <summary>
        /// Disk number
        /// </summary>
        public ushort VolumeSequenceNumber
        {
            get { return _volumeSequenceNumber; }
            set { _volumeSequenceNumber = value; }
        }

        /// <summary>
        /// User's data size for the sector
        /// </summary>
        public ushort LogicalBlockSize
        {
            get { return _logicalBlockSize; }
            set { _logicalBlockSize = value; }
        }

        /// <summary>
        /// Size of path table (in bytes)
        /// </summary>
        public uint PathTableSize
        {
            get { return _pathTableSize; }
            set { _pathTableSize = value; }
        }

        /// <summary>
        /// LBA of the path table which data are stored only in little endian
        /// </summary>
        public uint TypeLPathTableLBA
        {
            get { return _typeLPathTableLBA; }
            set { _typeLPathTableLBA = value; }
        }

        /// <summary>
        /// LBA of the optional path table which data are stored only in little endian
        /// Value : 0x00 if no optional path table
        /// </summary>
        public uint OptTypeLPathTableLBA
        {
            get { return _optTypeLPathTableLBA; }
            set { _optTypeLPathTableLBA = value; }
        }

        /// <summary>
        /// LBA of the path table which data are stored only in big endian
        /// </summary>
        public uint TypeMPathTableLBA
        {
            get { return _typeMPathTableLBA; }
            set { _typeMPathTableLBA = value; }
        }

        /// <summary>
        /// LBA of the optional path table which data are stored only in big endian
        /// Value : 0x00 if no optional path table
        /// </summary>
        public uint OptTypeMPathTableLBA
        {
            get { return _optTypeMPathTableLBA; }
            set { _optTypeMPathTableLBA = value; }
        }

        /// <summary>
        /// Directory entry for root
        /// Size : 34 bytes
        /// </summary>
        public DirectoryEntry RootDirectoryEntry
        {
            get { return _rootDirectoryEntry; }
            set { _rootDirectoryEntry = value; }
        }

        /// <summary>
        /// Identifier of the volume set (if several disks)
        /// Size : 128 bytes
        /// </summary>
        public string VolumeSetId
        {
            get { return _volumeSetId; }
            set { _volumeSetId = value; }
        }

        /// <summary>
        /// Identifier of the publisher
        /// Size : 128 bytes
        /// Value : name, file or empty (padded with 0x20)
        /// If file, begins with 0x5F followed by the name of file (located at the root directory)
        /// </summary>
        public string PublisherId
        {
            get { return _publisherId; }
            set { _publisherId = value; }
        }

        /// <summary>
        /// Identifier of the preparer(s)
        /// Size : 128 bytes
        /// Value : name, file or empty (padded with 0x20)
        /// If file, begins with 0x5F followed by the name of file (located at the root directory)
        /// </summary>
        public string PreparerId
        {
            get { return _preparerId; }
            set { _preparerId = value; }
        }

        /// <summary>
        /// Identifier of the application
        /// Size : 128 bytes
        /// Value : name, file or empty (padded with 0x20)
        /// If file, begins with 0x5F followed by the name of file (located at the root directory)
        /// </summary>
        public string ApplicationId
        {
            get { return _applicationId; }
            set { _applicationId = value; }
        }

        /// <summary>
        /// Name of file that contains some copyright informations
        /// Size : 38 bytes
        /// Value : file or empty (padded with 0x20)
        /// If file, the name of file (located at the root directory)
        /// </summary>
        public string CopyrightFileId
        {
            get { return _copyrightFileId; }
            set { _copyrightFileId = value; }
        }

        /// <summary>
        /// Name of file that contains some additionnal informations about the volume
        /// Size : 36 bytes
        /// Value : file or empty (padded with 0x20)
        /// If file, the name of file (located at the root directory)
        /// </summary>
        public string AbstractFileId
        {
            get { return _abstractFileId; }
            set { _abstractFileId = value; }
        }

        /// <summary>
        /// Name of file that contains bibliographic informations about the volume
        /// Size : 37 bytes
        /// Value : file or empty (padded with 0x20)
        /// If file, the name of file (located at the root directory)
        /// </summary>
        public string BibliographicFileId
        {
            get { return _bibliographicFileId; }
            set { _bibliographicFileId = value; }
        }

        /// <summary>
        /// Creation date
        /// </summary>
        public DateTime CreationDate
        {
            get { return _creationDate; }
            set { _creationDate = value; }
        }

        /// <summary>
        /// Modification date
        /// </summary>
        public DateTime ModificationDate
        {
            get { return _modificationDate; }
            set { _modificationDate = value; }
        }

        /// <summary>
        /// Optional expiration date after wich data are considered obsolete
        /// </summary>
        public DateTime ExpirationDate
        {
            get { return _expirationDate; }
            set { _expirationDate = value; }
        }

        /// <summary>
        /// Optional effective date after which data may be used
        /// </summary>
        public DateTime EffectiveDate
        {
            get { return _effectiveDate; }
            set { _effectiveDate = value; }
        }

        /// <summary>
        /// Structure version
        /// Value : always 0x01
        /// </summary>
        public byte FileStructureVersion
        {
            get { return _fileStructureVersion; }
            set { _fileStructureVersion = value; }
        }

        /// <summary>
        /// Unused
        /// </summary>
        public byte Unused4
        {
            get { return _unused4; }
            set { _unused4 = value; }
        }

        /// <summary>
        /// Data that are not ISO9660-specific
        /// Size : 512 bytes
        /// </summary>
        public byte[] ApplicationData
        {
            get { return _applicationData; }
            set { _applicationData = value; }
        }

        /// <summary>
        /// Reserved to ISO
        /// Size : 653 bytes
        /// </summary>
        public byte[] Reserved
        {
            get { return _reserved; }
            set { _reserved = value; }
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