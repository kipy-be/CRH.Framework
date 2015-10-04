using System;
using System.Collections.Generic;
using System.IO;
using CRH.Framework.Common;

namespace CRH.Framework.Disk
{
    public abstract class Disk
    {
        protected string     m_fileUrl;
        protected FileInfo   m_file;
        protected FileStream m_fileStream;
        protected bool       m_fileOpen;

        protected DiskFileSystem m_system;
        protected List<Track>    m_tracks;

    // Constructors

        /// <summary>
        /// Disk (abstract)
        /// </summary>
        /// <param name="fileUrl">Path to the ISO file</param>
        /// <param name="system">File system used for data track</param>
        internal Disk(string fileUrl, DiskFileSystem system)
        {
            m_fileUrl  = fileUrl;
            m_fileOpen = false;
            m_system   = system;

            m_tracks = new List<Track>();
        }

    // Abstract methods

        public abstract void Close();

    // Accessors

        /// <summary>
        /// Tracks of the disk
        /// </summary>
        public IEnumerable<Track> Tracks
        {
            get { return m_tracks; }
        }

        /// <summary>
        /// Single or first track of the disk
        /// </summary>
        public Track Track
        {
            get
            {
                if (m_tracks.Count == 0)
                    throw new FrameworkException("Error while getting track : track does not exists");
                return m_tracks[0];
            }
        }

        /// <summary>
        /// Number of tracks
        /// </summary>
        public int TracksCount
        {
            get { return m_tracks.Count; }
        }
    }
}
