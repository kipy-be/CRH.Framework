using System;
using System.IO;

namespace CRH.Framework.Disk
{
    public abstract class Track
    {
        protected TrackType  m_type;
        protected FileStream m_fileStream;

    // Constructors

        public Track(FileStream fileStream, TrackType type)
        {
            m_fileStream = fileStream;
            m_type       = type;
        }

    // Accessors

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
    }
}
