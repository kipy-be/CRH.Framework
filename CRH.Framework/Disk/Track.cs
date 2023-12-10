using CRH.Framework.Common;
using System;
using System.IO;

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
        public long SectorPosition => _fileStream.Position / _sectorSize;

        /// <summary>
        /// Number of sectors
        /// </summary>
        public long SectorCount => _fileStream.Length / _sectorSize;

        /// <summary>
        /// Number of the track
        /// </summary>
        public int TrackNumber => _trackNumber;

        /// <summary>
        /// Offset of the track
        /// </summary>
        public long Offset
        {
            get => _offset;
            internal set => _offset = value;
        }

        /// <summary>
        /// Size of the track in sectors
        /// </summary>
        public long Size
        {
            get => _size;
            internal set => _size = value;
        }

        /// <summary>
        /// Size of the pregap in sectors
        /// </summary>
        public uint PregapSize
        {
            get => _pregapSize;
            internal set => _pregapSize = value;
        }

        /// <summary>
        /// Size of the postgap in sectors
        /// </summary>
        public uint PostgapSize
        {
            get => _postgapSize;
            internal set => _postgapSize = value;
        }

        /// <summary>
        /// Offset of the pause
        /// </summary>
        public long PauseOffset
        {
            get => _pauseOffset;
            internal set => _pauseOffset = value;
        }

        /// <summary>
        /// Size of pause in sectors
        /// </summary>
        public uint PauseSize
        {
            get => _pauseSize;
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
            get => _hasPause;
            set => _hasPause = value;
        }

        /// <summary>
        /// Is data track
        /// </summary>
        public bool IsData => _type == TrackType.DATA;

        /// <summary>
        /// Is audio track
        /// </summary>
        public bool IsAudio => _type == TrackType.AUDIO;

        /// <summary>
        /// Size of the sector
        /// </summary>
        public int SectorSize => _sectorSize;
    }
}
