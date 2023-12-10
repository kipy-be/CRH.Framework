using CRH.Framework.Common;
using CRH.Framework.Disk.AudioTrack;
using CRH.Framework.Disk.DataTrack;
using CRH.Framework.IO;
using System;
using System.IO;

namespace CRH.Framework.Disk
{
    public sealed class DiskWriter : Disk
    {
        private CBinaryWriter _stream;

        /// <summary>
        /// DiskWriter (multi tracks)
        /// </summary>
        /// <param name="fileUrl">Path to the ISO file to create</param>
        /// <param name="system">File system used for data track</param>
        /// <param name="overwriteIfExists">Overwite file if exists</param>
        private DiskWriter(string fileUrl, DiskFileSystem system, bool overwriteIfExists = true)
            : base(system)
        {
            try
            {
                _file = new FileInfo(fileUrl);

                _fileStream = new FileStream(_file.FullName, overwriteIfExists ? FileMode.Create : FileMode.CreateNew, FileAccess.Write, FileShare.Read);
                _stream     = new CBinaryWriter(_fileStream);
                _fileOpen   = true;
            }
            catch (Exception)
            {
                throw new FrameworkException("Error while while writing ISO : Unable to create the ISO File");
            }
        }

        /// <summary>
        /// Initialize a new multi tracks DiskWriter
        /// </summary>
        /// <param name="fileUrl">Path to the ISO file to create</param>
        /// <param name="system">File system used for the track</param>
        /// <param name="overwriteIfExists">Overwite file if exists</param>
        public static DiskWriter Init(string fileUrl, DiskFileSystem system, bool overwriteIfExists = true)
        {
            return new DiskWriter(fileUrl, system, overwriteIfExists);
        }

        /// <summary>
        /// Close the file and dispose it
        /// </summary>
        public override void Close()
        {
            try
            {
                if (!_fileOpen)
                    return;

                foreach (Track track in _tracks)
                {
                    if (track.IsData && !((DataTrackWriter)track).IsFinalized)
                    {
                        throw new FrameworkException("Error while closing ISO : data track is not finalized, it will be unreadable");
                    }
                }

                // Create CUE sheet
                CreateCue();

                _stream.CloseAndDispose();
                _fileOpen = false;
            }
            catch (FrameworkException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new FrameworkException("Error while closing ISO : unable to close the ISO");
            }
        }

        /// <summary>
        /// Add a data track
        /// </summary>
        /// <param name="mode">The sector mode of the track</param>
        /// <returns></returns>
        public DataTrackWriter CreateDataTrack(DataTrackMode mode)
        {
            if (_hasDataTrack)
            {
                throw new FrameworkException("Error while adding track : only Mixed Mode multi tracks disks are supported, it must contains only one DATA track");
            }

            var dataTrack = new DataTrackWriter(_stream, TracksCount + 1, _system, mode);
            
            _tracks.Add(dataTrack);
            _hasDataTrack = true;

            return dataTrack;
        }

        /// <summary>
        /// Add audio track
        /// </summary>
        /// <returns></returns>
        public AudioTrackWriter CreateAudioTrack()
        {
            if (TracksCount == 0)
            {
                throw new FrameworkException("Error while adding track : only Mixed Mode multi tracks disks are supported, the first track must be a DATA track");
            }

            if (!((ITrackWriter)_tracks[TracksCount - 1]).IsFinalized)
            {
                throw new FrameworkException("Error while adding track : you have to finalize the current track before creating new one");
            }

            var audioTrack = new AudioTrackWriter(_stream, TracksCount + 1);

            // Default pregap / pause
            if (TracksCount == 1)
            {
                audioTrack.PregapSize = 150;
            }
            else
            {
                audioTrack.PauseSize = 150;
            }
            
            _tracks.Add(audioTrack);

            return audioTrack;
        }

        /// <summary>
        /// Create a the CUE file for this ISO
        /// </summary>
        private void CreateCue()
        {
            try
            {
                var cueFile = new FileInfo(Path.Combine(_file.DirectoryName, Path.GetFileNameWithoutExtension(_file.FullName) + ".cue"));

                using (var cueFileStream = new FileStream(cueFile.FullName, FileMode.Create, FileAccess.Write, FileShare.Read))
                using (var cueStream     = new StreamWriter(cueFileStream))
                {
                    int maxTrackNumberLength = TracksCount.ToString().Length;
                    int m, s, b, dv;
                    int sectorPos;

                    if (maxTrackNumberLength < 2)
                    {
                        maxTrackNumberLength = 2;
                    }

                    cueStream.WriteLine(string.Format("FILE \"{0}\" BINARY", _file.Name.ToUpper()));
                    foreach(var track in _tracks)
                    {
                        if(track.IsData)
                        {
                            var dataTrack = (DataTrackWriter)track;

                            string mode;
                            switch(dataTrack.Mode)
                            {
                                case DataTrackMode.MODE1:
                                    mode = "MODE1/2352";
                                    break;

                                case DataTrackMode.MODE2:
                                    mode = "MODE2/2336";
                                    break;

                                case DataTrackMode.MODE2_XA:
                                    mode = "MODE2/2352";
                                    break;

                                case DataTrackMode.RAW:
                                default:
                                    mode = "MODE1/2048";
                                    break;
                            }

                            cueStream.WriteLine(string.Format(
                                "  TRACK {0} {1}",
                                Utils.PrePaddStr(track.TrackNumber.ToString(), maxTrackNumberLength, '0'),
                                mode
                            ));

                            cueStream.WriteLine("    INDEX 01 00:00:00");
                        }
                        else if(track.IsAudio)
                        {
                            var audioTrack = (AudioTrackWriter)track;

                            cueStream.WriteLine(string.Format(
                                "  TRACK {0} AUDIO",
                                Utils.PrePaddStr(track.TrackNumber.ToString(), maxTrackNumberLength, '0')
                            ));

                            // Pregap
                            if(audioTrack.PregapSize > 0)
                            {
                                b = (int)(audioTrack.PregapSize % 75); dv = (int)(audioTrack.PregapSize / 75);
                                s = dv % 60; dv /= 60;
                                m = dv % 255;

                                cueStream.WriteLine(string.Format(
                                    "    PREGAP {0}:{1}:{2}",
                                    Utils.PrePaddStr(m.ToString(), 2, '0'),
                                    Utils.PrePaddStr(s.ToString(), 2, '0'),
                                    Utils.PrePaddStr(b.ToString(), 2, '0')
                                ));
                            }

                            // Pause (gap included)
                            if(audioTrack.HasPause)
                            {
                                sectorPos = (int)(audioTrack.PauseOffset / audioTrack.SectorSize);
                                b = sectorPos % 75; dv = sectorPos / 75;
                                s = dv % 60; dv /= 60;
                                m = dv % 255;

                                cueStream.WriteLine(string.Format(
                                    "    INDEX 00 {0}:{1}:{2}",
                                    Utils.PrePaddStr(m.ToString(), 2, '0'),
                                    Utils.PrePaddStr(s.ToString(), 2, '0'),
                                    Utils.PrePaddStr(b.ToString(), 2, '0')
                                ));
                            }

                            // Actual track
                            sectorPos = (int)(audioTrack.Offset / audioTrack.SectorSize);
                            b = sectorPos % 75; dv = sectorPos / 75;
                            s = dv % 60; dv /= 60;
                            m = dv % 255;

                            cueStream.WriteLine(string.Format(
                                "    INDEX 01 {0}:{1}:{2}",
                                Utils.PrePaddStr(m.ToString(), 2, '0'),
                                Utils.PrePaddStr(s.ToString(), 2, '0'),
                                Utils.PrePaddStr(b.ToString(), 2, '0')
                            ));

                            // Postgap
                            if (audioTrack.PostgapSize > 0)
                            {
                                b = (int)(audioTrack.PostgapSize % 75); dv = (int)(audioTrack.PostgapSize / 75);
                                s = dv % 60; dv /= 60;
                                m = dv % 255;

                                cueStream.WriteLine(string.Format(
                                    "    POSTGAP {0}:{1}:{2}",
                                    Utils.PrePaddStr(m.ToString(), 2, '0'),
                                    Utils.PrePaddStr(s.ToString(), 2, '0'),
                                    Utils.PrePaddStr(b.ToString(), 2, '0')
                                ));
                            }
                        }
                    }
                }
            }
            catch(Exception)
            {
                throw new FrameworkException("Error while writing cue : unable to write the cue file");
            }
        }
    }
}
