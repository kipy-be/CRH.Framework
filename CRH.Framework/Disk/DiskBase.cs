using System;
using System.IO;
using CRH.Framework.Common;

namespace CRH.Framework.Disk
{
    public abstract class DiskBase
    {
        internal const int SYNC_SIZE         = 12;
        internal const int HEADER_SIZE       = 4;
        internal const int SUBHEADER_SIZE    = 8;

        internal const int EDC_SIZE          = 4;
        internal const int INTERMEDIATE_SIZE = 8;
        internal const int ECC_SIZE          = 276;
        internal const int ECC_P_SIZE        = 172;
        internal const int ECC_Q_SIZE        = 104;

        internal static readonly byte[] SYNC = new byte[] { 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00 };

        protected string     m_fileUrl;
        protected FileInfo   m_file;
        protected FileStream m_fileStream;
        protected bool       m_fileOpen;

        protected IsoType    m_type;
        protected TrackMode  m_mode;
        protected SectorMode m_defaultSectorMode;
        protected int        m_sectorSize;
        protected bool       m_isXa;

        protected PrimaryVolumeDescriptor m_primaryVolumeDescriptor;

    // Constructors

        internal DiskBase(string fileUrl, IsoType type, TrackMode mode)
        {
            m_fileUrl    = fileUrl;
            m_type       = type;
            m_mode       = mode;
            m_sectorSize = mode == TrackMode.RAW ? 2048 : 2352;
            m_fileOpen   = false;
            m_isXa       = false;

            switch (m_mode)
            {
                case TrackMode.MODE1:
                    m_defaultSectorMode = SectorMode.MODE1;
                    break;
                case TrackMode.MODE2:
                    m_defaultSectorMode = SectorMode.MODE2;
                    break;
                case TrackMode.MODE2_XA:
                    m_defaultSectorMode = SectorMode.XA_FORM1;
                    m_isXa = true;
                    break;
                case TrackMode.RAW:
                default:
                    m_defaultSectorMode = SectorMode.RAW;
                    break;
            }
        }

    // Abstract methods

        public abstract void Close();

    // Méthods

        /// <summary>
        /// Get the sector data size
        /// </summary>
        /// <param name="size"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        internal static int GetSectorDataSize(SectorMode mode)
        {
            switch (mode)
            {
                case SectorMode.MODE2:
                    return 2336;
                case SectorMode.XA_FORM2:
                    return 2324;
                default:
                    return 2048;
            }
        }

        /// <summary>
        /// Get offset from LBA
        /// </summary>
        protected long LBAToOffset(long lba)
        {
            return m_sectorSize * lba;
        }

        /// <summary>
        /// Move to a specific sector's LBA
        /// </summary>
        protected void SeekSector(long lba)
        {
            try
            {
                m_fileStream.Position = LBAToOffset(lba);
            }
            catch (EndOfStreamException)
            {
                throw new FrameworkException("Errow while seeking sector : end of file occured");
            }
            catch (Exception)
            {
                throw new FrameworkException("Errow while seeking sector : unable to seek sector");
            }
        }

    // Accessors

        /// <summary>
        /// Is a CDROM/XA
        /// </summary>
        public bool IsXa
        {
            get { return m_isXa; }
            set { m_isXa = value; }
        }

        /// <summary>
        /// ISO's structure type (ISO9660, ISO9660_UDF)
        /// </summary>
        public IsoType Type
        {
            get { return m_type; }
        }

        /// <summary>
        /// Disk's mode
        /// </summary>
        public TrackMode Mode
        {
            get { return m_mode; }
        }

        /// <summary>
        /// Size of the sector including metadata (FORM's fields)
        /// </summary>
        public int SectorSize
        {
            get { return m_sectorSize; }
        }

        /// <summary>
        /// The primary volume descriptor of the disk
        /// </summary>
        public PrimaryVolumeDescriptor PrimaryVolumeDescriptor
        {
            get { return m_primaryVolumeDescriptor; }
        }

        /// <summary>
        /// Position (current LBA)
        /// </summary>
        public long SectorPosition
        {
            get { return m_fileStream.Position / m_sectorSize; }
        }

        /// <summary>
        /// Number of sectors
        /// </summary>
        public long SectorCount
        {
            get { return m_fileStream.Length / m_sectorSize; }
        }
    }
}