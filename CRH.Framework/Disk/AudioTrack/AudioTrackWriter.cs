using System;
using System.IO;
using CRH.Framework.Common;
using CRH.Framework.IO;

namespace CRH.Framework.Disk.AudioTrack
{
    public sealed class AudioTrackWriter : AudioTrack, ITrackWriter
    {
        private CBinaryWriter _stream;

        private bool _prepared;
        private bool _finalized;

        /// <summary>
        /// AudioTrackWriter
        /// </summary>
        /// <param name="stream">The stream of iso</param>
        /// <param name="trackNumber">The track number</param>
        internal AudioTrackWriter(CBinaryWriter stream, int trackNumber)
            : base((FileStream)stream.BaseStream, trackNumber)
        {
            _stream    = stream;
            _finalized = false;
        }

        /// <summary>
        /// Prepare the track (Add pregap and pause if set)
        /// </summary>
        public void Prepare()
        {
            try
            {
                if (_prepared)
                    return;

                _fileStream.Position = _fileStream.Length;

                if (_pauseSize > 0)
                {
                    _pauseOffset = _fileStream.Length;
                    WriteEmptySectors((int)_pauseSize);
                }

                _offset = _fileStream.Length;

                _prepared = true;
            }
            catch (FrameworkException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new FrameworkException("Errow while preparing track : unable to prepare audio track");
            }
        }

        /// <summary>
        /// Finalise the track
        /// </summary>
        public void FinalizeTrack()
        {
            try
            {
                if (_finalized)
                {
                    return;
                }

                if (!_prepared)
                { 
                    throw new FrameworkException("Error while finalizing ISO : AudioTrack has not been prepared");
                }

                _fileStream.Position = _fileStream.Length;

                if (_postgapSize > 0)
                {
                    WriteEmptySectors((int)_postgapSize);
                }

                _finalized = true;
            }
            catch (FrameworkException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new FrameworkException("Errow while finalizing track : unable to finalize audio track");
            }
        }

        /// <summary>
        /// Write a sector
        /// </summary>
        public void WriteSector(byte[] data)
        {
            try
            {
                _stream.Write(data);
            }
            catch (Exception)
            {
                throw new FrameworkException("Errow while writing sector : unable to write sector");
            }
        }

        /// <summary>
        /// Write a sector at the specified lba
        /// </summary>
        /// <param name="lba">Sector's LBA</param>
        public void WriteSector(long lba, byte[] data)
        {
            SeekSector(lba);
            WriteSector(data);
        }

        /// <summary>
        /// Write an empty sector
        /// </summary>
        public void WriteEmptySector()
        {
            WriteSector(new byte[_sectorSize]);
        }

        /// <summary>
        /// Write empty sectors
        /// </summary>
        /// <param name="count">Number of sectors to write</param>
        public void WriteEmptySectors(int count)
        {
            byte[] data = new byte[_sectorSize];

            for (int i = 0; i < count; i++)
            { 
                WriteSector(data);
            }
        }

        /// <summary>
        /// Write the audio track
        /// </summary>
        /// <param name="stream">The source stream of the file</param>
        /// <param name="container">The container used by the file (default : raw)</param>
        public void Write(Stream stream, AudioFileContainer container = AudioFileContainer.RAW)
        {
            try
            {
                byte[] buffer = new byte[_sectorSize];
                int dataRead;

                stream.Position = (container == AudioFileContainer.WAVE) ? 44 : 0;
                _size = ((stream.Length - stream.Position) / _sectorSize) + 1;

                for (int sectorsDone = 0; sectorsDone < _size; sectorsDone++)
                {
                    dataRead = stream.Read(buffer, 0, _sectorSize);

                    if (dataRead < _sectorSize)
                    {
                        for (int i = dataRead; i < _sectorSize; i++)
                        {
                            buffer[i] = 0;
                        }
                    }

                    WriteSector(buffer);
                }
            }
            catch (Exception)
            {
                throw new FrameworkException("Error while writing audio track : unable to write audio track");
            }
        }

        /// <summary>
        /// Is the track finalized
        /// </summary>
        public bool IsFinalized => _finalized;
    }
}
