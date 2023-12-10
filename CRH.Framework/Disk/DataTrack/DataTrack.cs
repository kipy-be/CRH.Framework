using System.Collections.Generic;
using System.IO;

namespace CRH.Framework.Disk.DataTrack
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

        internal static readonly byte[] SYNC = [0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00];

        protected DiskFileSystem _system;
        protected DataTrackMode  _mode;
        protected SectorMode     _defaultSectorMode;

        protected bool _isXa;
        protected bool _hasOptionalPathTable;

        protected PrimaryVolumeDescriptor _primaryVolumeDescriptor;

        protected DataTrackEntriesOrder _entriesOrder;
        protected DataTrackIndex        _index;

        /// <summary>
        /// DataTrack (abstract)
        /// </summary>
        /// <param name="fileStream">The iso file stream</param>
        /// <param name="trackNumber">The track number</param>
        /// <param name="system">File system used for this data track</param>
        /// <param name="mode">The sector mode of the track</param>
        internal DataTrack(FileStream fileStream, int trackNumber, DiskFileSystem system, DataTrackMode mode)
            : base(fileStream, trackNumber, TrackType.DATA)
        {
            _system     = system;
            _mode       = mode;
            _sectorSize = mode == DataTrackMode.RAW ? 2048 : 2352;
            _isXa       = false;
            _pregapSize = 150;

            switch (_mode)
            {
                case DataTrackMode.MODE1:
                    _defaultSectorMode = SectorMode.MODE1;
                    break;

                case DataTrackMode.MODE2:
                    _defaultSectorMode = SectorMode.MODE2;
                    break;

                case DataTrackMode.MODE2_XA:
                    _defaultSectorMode = SectorMode.XA_FORM1;
                    _isXa = true;
                    break;

                case DataTrackMode.RAW:
                default:
                    _defaultSectorMode = SectorMode.RAW;
                    break;
            }
        }

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

        public abstract IEnumerable<DataTrackIndexEntry> Entries { get; }
        public abstract IEnumerable<DataTrackIndexEntry> DirectoryEntries { get; }
        public abstract IEnumerable<DataTrackIndexEntry> FileEntries { get; }
        public abstract int EntriesCount { get; }
        public abstract int DirectoryEntriesCount { get; }
        public abstract int FileEntriesCount { get; }

        /// <summary>
        /// Get or set the order in which entries are iterated
        /// </summary>
        public DataTrackEntriesOrder EntriesOrder
        {
            get => _entriesOrder;
            set => _entriesOrder = value;
        }

        /// <summary>
        /// Is a CDROM/XA
        /// </summary>
        public bool IsXa
        {
            get => _isXa;
            set => _isXa = value;
        }

        /// <summary>
        /// ISO's structure type (ISO9660, ISO9660_UDF)
        /// </summary>
        public DiskFileSystem System =>_system;

        /// <summary>
        /// DataTrack's sector mode
        /// </summary>
        public DataTrackMode Mode =>  _mode;

        /// <summary>
        /// Disk's defaut sector mode
        /// </summary>
        internal SectorMode DefautSectorMode => _defaultSectorMode;

        /// <summary>
        /// The primary volume descriptor of the disk
        /// </summary>
        public PrimaryVolumeDescriptor PrimaryVolumeDescriptor => _primaryVolumeDescriptor;

        /// <summary>
        /// Has optional path table
        /// </summary>
        public bool HasOptionalPathTable
        {
            get => _hasOptionalPathTable;
            set => _hasOptionalPathTable = value;
        }
    }
}