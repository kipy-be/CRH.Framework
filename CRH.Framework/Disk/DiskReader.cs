using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using CRH.Framework.Common;
using CRH.Framework.Disk.AudioTrack;
using CRH.Framework.Disk.DataTrack;
using CRH.Framework.IO;

namespace CRH.Framework.Disk
{
    public sealed class DiskReader : Disk
    {
        private CBinaryReader m_stream;

        private static Regex m_regCueKeyWord = new Regex("^[ \t]*([^ \t]+)");
        private static Regex m_regCueFile    = new Regex("FILE[ \t]+\"([^\"]+)\"[ \t]+BINARY", RegexOptions.IgnoreCase);
        private static Regex m_regCueTrack   = new Regex("TRACK[ \t]+([0-9]+)[ \t]+(.+)", RegexOptions.IgnoreCase);
        private static Regex m_regCueIndex   = new Regex("INDEX[ \t]+([0-9]+)[ \t]+([0-9]+):([0-9]+):([0-9]+)", RegexOptions.IgnoreCase);
        private static Regex m_regCueGap     = new Regex("(?:PRE|POST)GAP[ \t]+([0-9]+):([0-9]+):([0-9]+)", RegexOptions.IgnoreCase);

    // Constructors

        /// <summary>
        /// DiskReader (multi tracks)
        /// </summary>
        /// <param name="fileUrl">Path to the CUE file to read</param>
        /// <param name="system">File system used for data track</param>
        private DiskReader(string fileUrl, DiskFileSystem system)
            : base(system)
        {
            try
            {
                FileInfo cueFile = new FileInfo(fileUrl);
                
                using (FileStream cueFileStream = new FileStream(cueFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (StreamReader cueStream   = new StreamReader(cueFileStream))
                {
                    Dictionary<int, Track> tracksDic = new Dictionary<int, Track>();
                    List<long> indexOffsets = new List<long>();
                    Match match = null;
                    Track track = null;
                    int trackNumber = 0;
                    int m, s, b;

                    string line;
                    string keyWord;
                    while ((line = cueStream.ReadLine()) != null)
                    {
                        keyWord = m_regCueKeyWord.Match(line).Groups[1].Value.ToUpper();

                        switch (keyWord)
                        {
                            case "FILE":
                                if (m_fileOpen)
                                    throw new FrameworkException("Error while parsing cue sheet : framework does not support multi files per cue but only one file with multi tracks");

                                fileUrl = m_regCueFile.Match(line).Groups[1].Value;

                                if (!(fileUrl.StartsWith("/") || fileUrl.StartsWith("\\") || fileUrl.Contains(":/") || fileUrl.Contains(":\\")))
                                    fileUrl = cueFile.DirectoryName + "/" + fileUrl;

                                m_file = new FileInfo(fileUrl);

                                if (!m_file.Exists)
                                    throw new FrameworkException("Error while parsing cue sheet : targeted file \"{0}\" not found", fileUrl);

                                string extension = m_file.Extension.ToUpper();
                                if (!(extension == ".BIN" || extension == ".IMG" || extension == ".ISO"))
                                    throw new FrameworkException("Error while parsing cue sheet : targeted file \"{0}\" is not a BIN/IMG/ISO file", fileUrl);


                                m_fileStream = new FileStream(m_file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                                m_stream = new CBinaryReader(m_fileStream);
                                m_fileOpen = true;
                                break;

                            case "TRACK":
                                if (!m_fileOpen)
                                    throw new FrameworkException("Error while parsing cue sheet : TRACK defined before FILE");

                                match = m_regCueTrack.Match(line);

                                if (!int.TryParse(match.Groups[1].Value, out trackNumber))
                                    throw new FrameworkException("Error while parsing cue sheet : track number invalid");

                                string mode = match.Groups[2].Value.ToUpper();

                                if ((!mode.StartsWith("MODE") && !m_hasDataTrack))
                                    throw new FrameworkException("Error while parsing cue sheet : only Mixed Mode multi tracks disks are supported, the first track must be a DATA track");
                                else if ((mode.StartsWith("MODE") && m_hasDataTrack))
                                    throw new FrameworkException("Error while parsing cue sheet : only Mixed Mode multi tracks disks are supported, it must contains only one DATA track");

                                switch (mode)
                                {
                                    case "RAW":
                                    case "RAW/2048":
                                    case "MODE1/2048":
                                        track = new DataTrackReader(m_stream, trackNumber, m_system, DataTrackMode.RAW, false, false);
                                        m_hasDataTrack = true;
                                        break;
                                    case "MODE1/2352":
                                        track = new DataTrackReader(m_stream, trackNumber, m_system, DataTrackMode.MODE1, false, false);
                                        m_hasDataTrack = true;
                                        break;
                                    case "MODE2/2336":
                                        track = new DataTrackReader(m_stream, trackNumber, m_system, DataTrackMode.MODE2, false, false);
                                        m_hasDataTrack = true;
                                        break;
                                    case "MODE2/2352":
                                        track = new DataTrackReader(m_stream, trackNumber, m_system, DataTrackMode.MODE2_XA, false, false);
                                        m_hasDataTrack = true;
                                        break;
                                    case "AUDIO":
                                        track = new AudioTrackReader(m_stream, trackNumber);
                                        break;
                                    default:
                                        throw new FrameworkException("Error while parsing cue sheet : unknown/not supported track type \"{0}\"", mode);
                                }
                                tracksDic.Add(trackNumber, track);
                                m_tracks.Add(track);
                                break;

                            case "INDEX":
                                track = tracksDic[trackNumber];

                                match = m_regCueIndex.Match(line);

                                int indexNumber;
                                if (!int.TryParse(match.Groups[1].Value, out indexNumber) || indexNumber > 2)
                                    throw new FrameworkException("Error while parsing cue sheet : index number invalid");

                                if (!int.TryParse(match.Groups[2].Value, out m)
                                    || !int.TryParse(match.Groups[3].Value, out s) || s > 59
                                    || !int.TryParse(match.Groups[4].Value, out b) || s > 74)
                                    throw new FrameworkException("Error while parsing cue sheet : index time code invalid");

                                uint offset = (uint)(((((m * 60) + s) * 75) + b) * track.SectorSize);
                                indexOffsets.Add(offset);

                                if (indexNumber == 0)
                                    track.HasPause = true;

                                break;

                            case "PREGAP":
                            case "POSTGAP":
                                track = tracksDic[trackNumber];

                                match = m_regCueGap.Match(line);

                                if (!int.TryParse(match.Groups[1].Value, out m)
                                    || !int.TryParse(match.Groups[2].Value, out s) || s > 59
                                    || !int.TryParse(match.Groups[3].Value, out b) || s > 74)
                                    throw new FrameworkException("Error while parsing cue sheet : gap size invalid");

                                uint gapSize = (uint)((((m * 60) + s) * 75) + b);

                                if (keyWord == "PREGAP")
                                    track.PregapSize = gapSize;
                                else
                                    track.PostgapSize = gapSize;

                                break;
                        }
                    }
                    indexOffsets.Add(m_fileStream.Length);

                    for (int i = 0, u = 0, max = m_tracks.Count; i < max; i++)
                    {
                        track = m_tracks[i];
                        if (track.HasPause)
                        {
                            track.PauseOffset = indexOffsets[u];
                            track.PauseSize   = (uint)((indexOffsets[u + 1] - indexOffsets[u]) / track.SectorSize);
                            u++;
                        }

                        track.Offset = indexOffsets[u];
                        track.Size = (uint)((indexOffsets[u + 1] - indexOffsets[u]) / track.SectorSize);
                        u++;
                    }
                }
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
        /// <param name="system">File system used for the data track</param>
        /// <param name="mode">The sector mode of the track</param>
        /// <param name="readDescriptors">Read descriptors immediately</param>
        /// <param name="buildIndex">Build the index cache immediately</param>
        private DiskReader(string fileUrl, DiskFileSystem system, DataTrackMode mode, bool readDescriptors = true, bool buildIndex = true)
            : base(system)
        {
            try
            {
                m_file = new FileInfo(fileUrl);
                m_fileStream = new FileStream(m_file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                m_stream = new CBinaryReader(m_fileStream);
                m_fileOpen = true;

                m_stream.Position = 0;

                m_tracks.Add(new DataTrackReader(m_stream, 1, system, mode, readDescriptors, buildIndex));
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

    // Methods

        /// <summary>
        /// Initialize a new multi tracks DiskReader
        /// </summary>
        /// <param name="fileUrl">Path to the CUE file to read</param>
        /// <param name="system">File system used for data track</param>
        public static DiskReader InitFromCue(string fileUrl, DiskFileSystem system)
        {
            return new DiskReader(fileUrl, system);
        }

        /// <summary>
        /// Initialize a new single data track DiskReader
        /// </summary>
        /// <param name="fileUrl">Path to the ISO/BIN/IMG file to read</param>
        /// <param name="system">File system used for the track</param>
        /// <param name="mode">The sector mode of the track</param>
        /// <param name="readDescriptors">Read descriptors immediately</param>
        /// <param name="buildIndex">Build the index cache immediately</param>
        public static DiskReader InitFromIso(string fileUrl, DiskFileSystem system, DataTrackMode mode, bool readDescriptors = true, bool buildIndex = true)
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

        /// <summary>
        /// Get audio tracks only
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<Track> GetAudioTracks()
        {
            foreach (Track track in m_tracks)
            {
                if (track.IsAudio)
                    yield return track;
            }
        }

    // Accessors

        /// <summary>
        /// Tracks of the disk
        /// </summary>
        public IEnumerable<Track> Tracks
        {
            get { return m_tracks; }
        }

        /// <summary>
        /// Audio tracks of the disk
        /// </summary>
        public IEnumerable<Track> AudioTracks
        {
            get { return GetAudioTracks(); }
        }

        /// <summary>
        /// Single or first data track of the disk
        /// </summary>
        public Track DataTrack
        {
            get
            {
                if (m_tracks.Count == 0)
                    throw new FrameworkException("Error while getting track : track does not exists");
                return m_tracks[0];
            }
        }
    }
}
