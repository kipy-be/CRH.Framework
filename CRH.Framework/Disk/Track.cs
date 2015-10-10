using System;
using System.IO;
using CRH.Framework.Common;

namespace CRH.Framework.Disk
{
    public abstract class Track
    {
        protected TrackType  m_type;
        protected FileStream m_fileStream;

        protected int  m_trackNumber;
        protected int  m_sectorSize;
        protected long m_offset;
        protected long m_size;
        protected uint m_pregapSize;
        protected uint m_postgapSize;
        protected long m_pauseOffset;
        protected uint m_pauseSize;
        protected bool m_hasPause;

    // Constructors

        /// <summary>
        /// Track (abstract)
        /// </summary>
        /// <param name="fileStream">The ISO stream</param>
        /// <param name="trackNumber">The track number</param>
        /// <param name="type">The type of the track (data, audio)</param>
        public Track(FileStream fileStream, int trackNumber, TrackType type)
        {
            m_fileStream  = fileStream;
            m_trackNumber = trackNumber;
            m_type        = type;
            m_pregapSize  = 0;
            m_postgapSize = 0;
            m_pauseSize   = 0;
            m_hasPause    = false;
        }

    // Methods

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
        internal void SeekSector(long lba)
        {
            try
            {
                m_fileStream.Position = m_offset + LBAToOffset(lba);
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
        /// Number of the track
        /// </summary>
        public int TrackNumber
        {
            get { return m_trackNumber; }
        }

        /// <summary>
        /// Offset of the track
        /// </summary>
        public long Offset
        {
            get { return m_offset; }
            internal set { m_offset = value; }
        }

        /// <summary>
        /// Size of the track in sectors
        /// </summary>
        public long Size
        {
            get { return m_size; }
            internal set { m_size = value; }
        }

        /// <summary>
        /// Size of the pregap in sectors
        /// </summary>
        public uint PregapSize
        {
            get { return m_pregapSize; }
            internal set { m_pregapSize = value; }
        }

        /// <summary>
        /// Size of the postgap in sectors
        /// </summary>
        public uint PostgapSize
        {
            get { return m_postgapSize; }
            internal set { m_postgapSize = value; }
        }

        /// <summary>
        /// Offset of the pause
        /// </summary>
        public long PauseOffset
        {
            get { return m_pauseOffset; }
            internal set { m_pauseOffset = value; }
        }

        /// <summary>
        /// Size of pause in sectors
        /// </summary>
        public uint PauseSize
        {
            get { return m_pauseSize; }
            internal set
            {
                m_pauseSize = value;
                m_hasPause  = value > 0;
            }
        }

        /// <summary>
        /// Has pause
        /// </summary>
        internal bool HasPause
        {
            get { return m_hasPause; }
            set { m_hasPause = value; }
        }

        /// <summary>
        /// Is data track
        /// </summary>
        public bool IsData
        {
            get { return m_type == TrackType.DATA; }
        }

        /// <summary>
        /// Is audio track
        /// </summary>
        public bool IsAudio
        {
            get { return m_type == TrackType.AUDIO; }
        }

        /// <summary>
        /// Size of the sector
        /// </summary>
        public int SectorSize
        {
            get { return m_sectorSize; }
        }
    }
}
