using System.Collections.Generic;
using System.IO;

namespace CRH.Framework.Disk
{
    public abstract class Disk
    {
        protected FileInfo   _file;
        protected FileStream _fileStream;
        protected bool       _fileOpen;

        protected DiskFileSystem _system;
        protected List<Track>    _tracks;

        protected bool _hasDataTrack;

        /// <summary>
        /// Disk (abstract)
        /// </summary>
        /// <param name="fileUrl">Path to the ISO file</param>
        /// <param name="system">File system used for data track</param>
        internal Disk(DiskFileSystem system)
        {
            _fileOpen     = false;
            _system       = system;
            _hasDataTrack = false;

            _tracks = new List<Track>();
        }

        public abstract void Close();

        /// <summary>
        /// Number of tracks
        /// </summary>
        public int TracksCount=> _tracks.Count;
    }
}
