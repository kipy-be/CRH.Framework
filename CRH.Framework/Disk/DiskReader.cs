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
        private CBinaryReader _stream;

        private static Regex _regCueKeyWord = new Regex("^[ \t]*([^ \t]+)");
        private static Regex _regCueFile    = new Regex("FILE[ \t]+\"([^\"]+)\"[ \t]+BINARY", RegexOptions.IgnoreCase);
        private static Regex _regCueTrack   = new Regex("TRACK[ \t]+([0-9]+)[ \t]+(.+)", RegexOptions.IgnoreCase);
        private static Regex _regCueIndex   = new Regex("INDEX[ \t]+([0-9]+)[ \t]+([0-9]+):([0-9]+):([0-9]+)", RegexOptions.IgnoreCase);
        private static Regex _regCueGap     = new Regex("(?:PRE|POST)GAP[ \t]+([0-9]+):([0-9]+):([0-9]+)", RegexOptions.IgnoreCase);

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
                var cueFile = new FileInfo(fileUrl);
                
                using (var cueFileStream = new FileStream(cueFile.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var cueStream   = new StreamReader(cueFileStream))
                {
                    var tracksDic = new Dictionary<int, Track>();
                    var indexOffsets = new List<long>();
                    Match match = null;
                    Track track = null;
                    int trackNumber = 0;
                    int m, s, b;

                    string line;
                    string keyWord;
                    while ((line = cueStream.ReadLine()) != null)
                    {
                        keyWord = _regCueKeyWord.Match(line).Groups[1].Value.ToUpper();

                        switch (keyWord)
                        {
                            case "FILE":
                                if (_fileOpen)
                                {
                                    throw new FrameworkException("Error while parsing cue sheet : framework does not support multi files per cue but only one file with multi tracks");
                                }

                                fileUrl = _regCueFile.Match(line).Groups[1].Value;

                                if (!(fileUrl.StartsWith("/") || fileUrl.StartsWith("\\") || fileUrl.Contains(":/") || fileUrl.Contains(":\\")))
                                {
                                    fileUrl = cueFile.DirectoryName + "/" + fileUrl;
                                }

                                _file = new FileInfo(fileUrl);

                                if (!_file.Exists)
                                {
                                    throw new FrameworkException("Error while parsing cue sheet : targeted file \"{0}\" not found", fileUrl);
                                }

                                string extension = _file.Extension.ToUpper();
                                if (!(extension == ".BIN" || extension == ".IMG" || extension == ".ISO"))
                                {
                                    throw new FrameworkException("Error while parsing cue sheet : targeted file \"{0}\" is not a BIN/IMG/ISO file", fileUrl);
                                }

                                _fileStream = new FileStream(_file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                                _stream = new CBinaryReader(_fileStream);
                                _fileOpen = true;
                                break;

                            case "TRACK":
                                if (!_fileOpen)
                                {
                                    throw new FrameworkException("Error while parsing cue sheet : TRACK defined before FILE");
                                }

                                match = _regCueTrack.Match(line);

                                if (!int.TryParse(match.Groups[1].Value, out trackNumber))
                                {
                                    throw new FrameworkException("Error while parsing cue sheet : track number invalid");
                                }

                                string mode = match.Groups[2].Value.ToUpper();

                                if (!mode.StartsWith("MODE") && !_hasDataTrack)
                                {
                                    throw new FrameworkException("Error while parsing cue sheet : only Mixed Mode multi tracks disks are supported, the first track must be a DATA track");
                                }
                                else if (mode.StartsWith("MODE") && _hasDataTrack)
                                {
                                    throw new FrameworkException("Error while parsing cue sheet : only Mixed Mode multi tracks disks are supported, it must contains only one DATA track");
                                }

                                switch (mode)
                                {
                                    case "RAW":
                                    case "RAW/2048":
                                    case "MODE1/2048":
                                        track = new DataTrackReader(_stream, trackNumber, _system, DataTrackMode.RAW, false, false);
                                        _hasDataTrack = true;
                                        break;

                                    case "MODE1/2352":
                                        track = new DataTrackReader(_stream, trackNumber, _system, DataTrackMode.MODE1, false, false);
                                        _hasDataTrack = true;
                                        break;

                                    case "MODE2/2336":
                                        track = new DataTrackReader(_stream, trackNumber, _system, DataTrackMode.MODE2, false, false);
                                        _hasDataTrack = true;
                                        break;

                                    case "MODE2/2352":
                                        track = new DataTrackReader(_stream, trackNumber, _system, DataTrackMode.MODE2_XA, false, false);
                                        _hasDataTrack = true;
                                        break;

                                    case "AUDIO":
                                        track = new AudioTrackReader(_stream, trackNumber);
                                        break;

                                    default:
                                        throw new FrameworkException("Error while parsing cue sheet : unknown/not supported track type \"{0}\"", mode);
                                }

                                tracksDic.Add(trackNumber, track);
                                _tracks.Add(track);
                                break;

                            case "INDEX":
                                track = tracksDic[trackNumber];

                                match = _regCueIndex.Match(line);

                                int indexNumber;
                                if (!int.TryParse(match.Groups[1].Value, out indexNumber) || indexNumber > 2)
                                {
                                    throw new FrameworkException("Error while parsing cue sheet : index number invalid");
                                }

                                if (!int.TryParse(match.Groups[2].Value, out m)
                                    || !int.TryParse(match.Groups[3].Value, out s) || s > 59
                                    || !int.TryParse(match.Groups[4].Value, out b) || s > 74)
                                {
                                    throw new FrameworkException("Error while parsing cue sheet : index time code invalid");
                                }

                                uint offset = (uint)(((((m * 60) + s) * 75) + b) * track.SectorSize);
                                indexOffsets.Add(offset);

                                if (indexNumber == 0)
                                {
                                    track.HasPause = true;
                                }

                                break;

                            case "PREGAP":
                            case "POSTGAP":
                                track = tracksDic[trackNumber];

                                match = _regCueGap.Match(line);

                                if (!int.TryParse(match.Groups[1].Value, out m)
                                    || !int.TryParse(match.Groups[2].Value, out s) || s > 59
                                    || !int.TryParse(match.Groups[3].Value, out b) || s > 74)
                                {
                                    throw new FrameworkException("Error while parsing cue sheet : gap size invalid");
                                }

                                uint gapSize = (uint)((((m * 60) + s) * 75) + b);

                                if (keyWord == "PREGAP")
                                {
                                    track.PregapSize = gapSize;
                                }
                                else
                                {
                                    track.PostgapSize = gapSize;
                                }

                                break;
                        }
                    }
                    indexOffsets.Add(_fileStream.Length);

                    for (int i = 0, u = 0, max = _tracks.Count; i < max; i++)
                    {
                        track = _tracks[i];
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
                _stream.Position = 0;
            }
            catch (FrameworkException)
            {
                throw;
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
                _file = new FileInfo(fileUrl);
                _fileStream = new FileStream(_file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                _stream = new CBinaryReader(_fileStream);
                _fileOpen = true;

                _stream.Position = 0;

                _tracks.Add(new DataTrackReader(_stream, 1, system, mode, readDescriptors, buildIndex));
            }
            catch (FrameworkException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new FrameworkException("Error while reading ISO : Unable to open the ISO File");
            }
        }

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
            if (!_fileOpen)
            {
                return;
            }

            _stream.CloseAndDispose();
            _fileOpen = false;
        }

        /// <summary>
        /// Get audio tracks only
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<Track> GetAudioTracks()
        {
            foreach (Track track in _tracks)
            {
                if (track.IsAudio)
                {
                    yield return track;
                }
            }
        }

        /// <summary>
        /// Tracks of the disk
        /// </summary>
        public IEnumerable<Track> Tracks => _tracks;

        /// <summary>
        /// Audio tracks of the disk
        /// </summary>
        public IEnumerable<Track> AudioTracks => GetAudioTracks();

        /// <summary>
        /// Single or first data track of the disk
        /// </summary>
        public Track DataTrack
        {
            get
            {
                if (_tracks.Count == 0)
                {
                    throw new FrameworkException("Error while getting track : track does not exists");
                }

                return _tracks[0];
            }
        }
    }
}
