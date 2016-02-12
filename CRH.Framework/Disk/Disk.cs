﻿using System;
using System.Collections.Generic;
using System.IO;
using CRH.Framework.Common;

namespace CRH.Framework.Disk
{
    public abstract class Disk
    {
        protected FileInfo   m_file;
        protected FileStream m_fileStream;
        protected bool       m_fileOpen;

        protected DiskFileSystem m_system;
        protected List<Track>    m_tracks;

        protected bool m_hasDataTrack;

    // Constructors

        /// <summary>
        /// Disk (abstract)
        /// </summary>
        /// <param name="fileUrl">Path to the ISO file</param>
        /// <param name="system">File system used for data track</param>
        internal Disk(DiskFileSystem system)
        {
            m_fileOpen     = false;
            m_system       = system;
            m_hasDataTrack = false;

            m_tracks = new List<Track>();
        }

    // Abstract methods

        public abstract void Close();

    // Accessors

        /// <summary>
        /// Number of tracks
        /// </summary>
        public int TracksCount
        {
            get { return m_tracks.Count; }
        }
    }
}
