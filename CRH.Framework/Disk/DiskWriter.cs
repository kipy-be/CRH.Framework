using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using CRH.Framework.Common;
using CRH.Framework.IO;
using CRH.Framework.Utils;
using CRH.Framework.Disk.DataTrack;

namespace CRH.Framework.Disk
{
    public sealed class DiskWriter : Disk
    {
        private CBinaryWriter m_stream;

    // Constructors

        /// <summary>
        /// DiskWriter (multi tracks)
        /// </summary>
        /// <param name="fileUrl">Path to the ISO file to create</param>
        /// <param name="system">File system used for data track</param>
        /// <param name="overwriteIfExists">Overwite file if exists</param>
        private DiskWriter(string fileUrl, DiskFileSystem system, bool overwriteIfExists = true)
            : base(fileUrl, system)
        {
            try
            {
                m_file       = new FileInfo(m_fileUrl);
                m_fileStream = new FileStream(m_file.FullName, overwriteIfExists ? FileMode.Create : FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                m_stream     = new CBinaryWriter(m_fileStream);
                m_fileOpen   = true;
            }
            catch (FrameworkException ex)
            {
                throw ex;
            }
            catch (Exception)
            {
                throw new FrameworkException("Error while while writing ISO : Unable to create the ISO File");
            }
        }

        /// <summary>
        /// DiskWriter (single data track)
        /// </summary>
        /// <param name="fileUrl">Path to the ISO file to create</param>
        /// <param name="system">File system used for the track</param>
        /// <param name="mode">The sector mode of the track</param>
        /// <param name="overwriteIfExists">Overwite file if exists</param>
        private DiskWriter(string fileUrl, DiskFileSystem system, DataTrackMode mode, bool overwriteIfExists = true)
            : this(fileUrl, system, overwriteIfExists)
        {
            m_tracks.Add(new DataTrackWriter(m_stream, 1, system, mode));
        }

    // Methods

        /// <summary>
        /// Initialize a new multi tracks DiskWriter
        /// </summary>
        /// <param name="fileUrl">Path to the ISO file to create</param>
        /// <param name="system">File system used for the track</param>
        /// <param name="overwriteIfExists">Overwite file if exists</param>
        public static DiskWriter InitMultiTracks(string fileUrl, DiskFileSystem system, bool overwriteIfExists = true)
        {
            return new DiskWriter(fileUrl, system, overwriteIfExists);
        }

        /// <summary>
        /// Initialize a new single data track DiskWriter
        /// </summary>
        /// <param name="fileUrl">Path to the ISO file to create</param>
        /// <param name="system">File system used for the track</param>
        /// <param name="mode">The sector mode of the track</param>
        /// <param name="overwriteIfExists">Overwite file if exists</param>
        public static DiskWriter InitSingleTrack(string fileUrl, DiskFileSystem system, DataTrackMode mode, bool overwriteIfExists = true)
        {
            return new DiskWriter(fileUrl, system, mode, overwriteIfExists);
        }

        /// <summary>
        /// Close the file and dispose it
        /// </summary>
        public override void Close()
        {
            if (!m_fileOpen)
                return;

            foreach(Track track in m_tracks)
            {
                if(track.IsData && !((DataTrackWriter)track).IsFinalized)
                    throw new FrameworkException("Error while closing ISO : data track is not finalized, it will be unreadable");
            }

            m_stream.CloseAndDispose();
            m_fileOpen = false;
        }
    }
}
