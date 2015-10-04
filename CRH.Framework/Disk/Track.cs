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
        protected long m_pregapOffset;
        protected uint m_pregapSize;
        protected bool m_pregapStored;
        protected long m_postgapOffset;
        protected uint m_postgapSize;
        protected bool m_postgapStored;

    // Constructors

        /// <summary>
        /// Track (abstract)
        /// </summary>
        /// <param name="fileStream">The iso stream</param>
        /// <param name="trackNumber">The track number</param>
        /// <param name="type">The type of the track (data, audio)</param>
        public Track(FileStream fileStream, int trackNumber, TrackType type)
        {
            m_fileStream    = fileStream;
            m_trackNumber   = trackNumber;
            m_type          = type;
            m_pregapOffset  = 0;
            m_pregapSize    = 0;
            m_pregapStored  = false;
            m_postgapOffset = 0;
            m_postgapSize   = 0;
            m_postgapStored = false;
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
        /// Offset of the pregap
        /// </summary>
        public long PregapOffset
        {
            get { return m_pregapOffset; }
            internal set { m_pregapOffset = value; }
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
        /// Is pregrap stored on the ISO or only refered in CUE
        /// </summary>
        public bool IsPregapStored
        {
            get { return m_pregapStored; }
            internal set { m_pregapStored = value; }
        }

        /// <summary>
        /// Offset of the postgap
        /// </summary>
        public long PostgapOffset
        {
            get { return m_postgapOffset; }
            internal set { m_postgapOffset = value; }
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
        /// Is postgap stored on the ISO or only refered in CUE
        /// </summary>
        public bool IsPostgapStored
        {
            get { return m_postgapStored; }
            internal set { m_postgapStored = value; }
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
