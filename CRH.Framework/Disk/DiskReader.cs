using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using CRH.Framework.Common;
using CRH.Framework.IO;
using CRH.Framework.Utils;

namespace CRH.Framework.Disk
{
    public sealed class DiskReader : Disk
    {
        private CBinaryReader m_stream;

    // Constructors

        /// <summary>
        /// DiskReader (multi tracks)
        /// </summary>
        /// <param name="fileUrl">Path to the CUE file to read</param>
        private DiskReader(string fileUrl)
            : base(fileUrl)
        {
            try
            {
                m_file       = new FileInfo(m_fileUrl);
                m_fileStream = new FileStream(m_file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                m_stream     = new CBinaryReader(m_fileStream);
                m_fileOpen   = true;

                m_stream.Position = 0;
            }
            catch (FrameworkException ex)
            {
                throw ex;
            }
            catch (Exception)
            {
                throw new FrameworkException("Error while reading ISO : Unable to open the ISO File");
            }
        }

        /// <summary>
        /// DiskReader (single data track)
        /// </summary>
        /// <param name="fileUrl">Path to the ISO/BIN/IMG file to read</param>
        /// <param name="system">File system used for the track</param>
        /// <param name="mode">The sector mode of the track</param>
        /// <param name="readDescriptors">Read descriptors immediately</param>
        /// <param name="buildIndex">Build the index cache immediately</param>
        private DiskReader(string fileUrl, DataTrackSystem system, DataTrackMode mode, bool readDescriptors = true, bool buildIndex = true)
            : this(fileUrl)
        {
            m_tracks.Add(new DataTrackReader(m_stream, system, mode, readDescriptors, buildIndex));
        }

    // Methods

        /// <summary>
        /// Initialize a new multi tracks DiskReader
        /// </summary>
        /// <param name="fileUrl">Path to the CUE file to read</param>
        public static DiskReader InitMultiTracks(string fileUrl)
        {
            return new DiskReader(fileUrl);
        }

        /// <summary>
        /// Initialize a new single data track DiskReader
        /// </summary>
        /// <param name="fileUrl">Path to the ISO/BIN/IMG file to read</param>
        /// <param name="system">File system used for the track</param>
        /// <param name="mode">The sector mode of the track</param>
        /// <param name="readDescriptors">Read descriptors immediately</param>
        /// <param name="buildIndex">Build the index cache immediately</param>
        public static DiskReader InitSingleTrack(string fileUrl, DataTrackSystem system, DataTrackMode mode, bool readDescriptors = true, bool buildIndex = true)
        {
            return new DiskReader(fileUrl, system, mode, readDescriptors, buildIndex);
        }

        /// <summary>
        /// Close the file and dispose it
        /// </summary>
        public override void Close()
        {
            if (!m_fileOpen)
                return;

            m_stream.CloseAndDispose();
            m_fileOpen = false;
        }
    }
}
