using System;
using System.Collections.Generic;
using System.IO;
using CRH.Framework.Common;

namespace CRH.Framework.Disk
{
    /// <summary>
    /// DataTrack abstract base
    /// </summary>
    public abstract class DataTrack : Track
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

        protected DataTrackSystem m_system;
        protected DataTrackMode   m_mode;
        protected SectorMode m_defaultSectorMode;
        protected int        m_sectorSize;
        protected bool       m_isXa;

        protected PrimaryVolumeDescriptor m_primaryVolumeDescriptor;
        protected DataTrackEntriesOrder   m_entriesOrder;

    // Constructors

        /// <summary>
        /// DataTrack (abstract)
        /// </summary>
        /// <param name="fileStream">The iso file stream</param>
        /// <param name="system">File system used for this data track</param>
        /// <param name="mode">The sector mode of the track</param>
        internal DataTrack(FileStream fileStream, DataTrackSystem system, DataTrackMode mode)
            : base(fileStream, TrackType.DATA)
        {
            m_system     = system;
            m_mode       = mode;
            m_sectorSize = mode == DataTrackMode.RAW ? 2048 : 2352;
            m_isXa       = false;

            switch (m_mode)
            {
                case DataTrackMode.MODE1:
                    m_defaultSectorMode = SectorMode.MODE1;
                    break;
                case DataTrackMode.MODE2:
                    m_defaultSectorMode = SectorMode.MODE2;
                    break;
                case DataTrackMode.MODE2_XA:
                    m_defaultSectorMode = SectorMode.XA_FORM1;
                    m_isXa = true;
                    break;
                case DataTrackMode.RAW:
                default:
                    m_defaultSectorMode = SectorMode.RAW;
                    break;
            }
        }

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
        private long LBAToOffset(long lba)
        {
            return m_sectorSize * lba;
        }

        /// <summary>
        /// Move to a specific sector's LBA
        /// </summary>
        internal void SeekSector(long lba)
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

    // Abstract accessors

        public abstract IEnumerable<DataTrackIndexEntry> Entries { get; }
        public abstract IEnumerable<DataTrackIndexEntry> DirectoryEntries { get; }
        public abstract IEnumerable<DataTrackIndexEntry> FileEntries { get; }

    // Accessors

        /// <summary>
        /// Get or set the order in which entries are iterated
        /// </summary>
        public DataTrackEntriesOrder EntriesOrder
        {
            get { return m_entriesOrder; }
            set { m_entriesOrder = value; }
        }

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
        public DataTrackSystem System
        {
            get { return m_system; }
        }

        /// <summary>
        /// DataTrack's sector mode
        /// </summary>
        public DataTrackMode Mode
        {
            get { return m_mode; }
        }

        /// <summary>
        /// Disk's defaut sector mode
        /// </summary>
        internal SectorMode DefautSectorMode
        {
            get { return m_defaultSectorMode; }
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