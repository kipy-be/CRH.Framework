using System;
using System.IO;
using CRH.Framework.Common;

namespace CRH.Framework.Disk
{
    public abstract class DiskBase
    {
        protected string     m_fileUrl;
        protected FileInfo   m_file;
        protected FileStream m_fileStream;
        protected bool       m_fileOpen;

        protected IsoType    m_type;
        protected DiskMode   m_mode;
        protected SectorMode m_defaultSectorMode;
        protected int        m_sectorSize;
        protected bool       m_isXa;

        protected PrimaryVolumeDescriptor m_primaryVolumeDescriptor;

    // Constructors

        internal DiskBase(string fileUrl, IsoType type, DiskMode mode, int sectorSize = -1)
        {
            m_fileUrl        = fileUrl;
            m_type           = type;
            m_mode           = mode;
            m_sectorSize     = sectorSize == -1 ? (mode == DiskMode.RAW ? 2048 : 2352) : sectorSize;
            m_fileOpen       = false;
            m_isXa      = false;

            switch (m_mode)
            {
                case DiskMode.MODE1:
                    m_defaultSectorMode = SectorMode.MODE1;
                    break;
                case DiskMode.MODE2:
                    m_defaultSectorMode = SectorMode.MODE2;
                    break;
                case DiskMode.MODE2_XA:
                    m_defaultSectorMode = SectorMode.XA_FORM1;
                    break;
                case DiskMode.RAW:
                default:
                    m_defaultSectorMode = SectorMode.RAW;
                    break;
            }
        }

    // Abstract methods

        public abstract void Close();

    // Méthods

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
        public void SeekSector(long lba)
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
        public DiskMode Mode
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