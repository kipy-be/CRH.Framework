using System;
using System.IO;
using CRH.Framework.Common;

namespace CRH.Framework.Disk
{
    public abstract class Track
    {
        protected TrackType  _type;
        protected FileStream _fileStream;

        protected int  _trackNumber;
        protected int  _sectorSize;
        protected long _offset;
        protected long _size;
        protected uint _pregapSize;
        protected uint _postgapSize;
        protected long _pauseOffset;
        protected uint _pauseSize;
        protected bool _hasPause;

    // Constructors

        /// <summary>
        /// Track (abstract)
        /// </summary>
        /// <param name="fileStream">The ISO stream</param>
        /// <param name="trackNumber">The track number</param>
        /// <param name="type">The type of the track (data, audio)</param>
        public Track(FileStream fileStream, int trackNumber, TrackType type)
        {
            _fileStream  = fileStream;
            _trackNumber = trackNumber;
            _type        = type;
            _pregapSize  = 0;
            _postgapSize = 0;
            _pauseSize   = 0;
            _hasPause    = false;
        }

    // Methods

        /// <summary>
        /// Get offset from LBA
        /// </summary>
        protected long LBAToOffset(long lba)
        {
            return _sectorSize * lba;
        }

        /// <summary>
        /// Move to a specific sector's LBA
        /// </summary>
        internal void SeekSector(long lba)
        {
            try
            {
                _fileStream.Position = _offset + LBAToOffset(lba);
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

        /// <summary>
        /// Position (current LBA)
        /// </summary>
        public long SectorPosition
        {
            get { return _fileStream.Position / _sectorSize; }
        }

        /// <summary>
        /// Number of sectors
        /// </summary>
        public long SectorCount
        {
            get { return _fileStream.Length / _sectorSize; }
        }

    // Accessors

        /// <summary>
        /// Number of the track
        /// </summary>
        public int TrackNumber
        {
            get { return _trackNumber; }
        }

        /// <summary>
        /// Offset of the track
        /// </summary>
        public long Offset
        {
            get { return _offset; }
            internal set { _offset = value; }
        }

        /// <summary>
        /// Size of the track in sectors
        /// </summary>
        public long Size
        {
            get { return _size; }
            internal set { _size = value; }
        }

        /// <summary>
        /// Size of the pregap in sectors
        /// </summary>
        public uint PregapSize
        {
            get { return _pregapSize; }
            internal set { _pregapSize = value; }
        }

        /// <summary>
        /// Size of the postgap in sectors
        /// </summary>
        public uint PostgapSize
        {
            get { return _postgapSize; }
            internal set { _postgapSize = value; }
        }

        /// <summary>
        /// Offset of the pause
        /// </summary>
        public long PauseOffset
        {
            get { return _pauseOffset; }
            internal set { _pauseOffset = value; }
        }

        /// <summary>
        /// Size of pause in sectors
        /// </summary>
        public uint PauseSize
        {
            get { return _pauseSize; }
            internal set
            {
                _pauseSize = value;
                _hasPause  = value > 0;
            }
        }

        /// <summary>
        /// Has pause
        /// </summary>
        internal bool HasPause
        {
            get { return _hasPause; }
            set { _hasPause = value; }
        }

        /// <summary>
        /// Is data track
        /// </summary>
        public bool IsData
        {
            get { return _type == TrackType.DATA; }
        }

        /// <summary>
        /// Is audio track
        /// </summary>
        public bool IsAudio
        {
            get { return _type == TrackType.AUDIO; }
        }

        /// <summary>
        /// Size of the sector
        /// </summary>
        public int SectorSize
        {
            get { return _sectorSize; }
        }
    }

    interface ITrackReader
    { }

    interface ITrackWriter
    {
        void Finalize();
        bool IsFinalized { get; }
    }
}
