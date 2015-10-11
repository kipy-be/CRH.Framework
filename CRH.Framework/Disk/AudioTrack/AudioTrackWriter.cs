using System;
using System.IO;
using CRH.Framework.Common;
using CRH.Framework.IO;

namespace CRH.Framework.Disk.AudioTrack
{
    public sealed class AudioTrackWriter : AudioTrack, ITrackWriter
    {
        private CBinaryWriter m_stream;

        private bool m_prepared;
        private bool m_finalized;

    // Constructors

        /// <summary>
        /// AudioTrackWriter
        /// </summary>
        /// <param name="stream">The stream of iso</param>
        /// <param name="trackNumber">The track number</param>
        internal AudioTrackWriter(CBinaryWriter stream, int trackNumber)
            : base((FileStream)stream.BaseStream, trackNumber)
        {
            m_stream    = stream;
            m_finalized = false;
        }

    // Methods

        /// <summary>
        /// Prepare the track (Add pregap and pause if set)
        /// </summary>
        public void Prepare()
        {
            try
            {
                if (m_prepared)
                    return;

                m_fileStream.Position = m_fileStream.Length;

                if (m_pauseSize > 0)
                {
                    m_pauseOffset = m_fileStream.Length;
                    WriteEmptySectors((int)m_pauseSize);
                }

                m_offset = m_fileStream.Length;

                m_prepared = true;
            }
            catch (FrameworkException ex)
            {
                throw ex;
            }
            catch (Exception)
            {
                throw new FrameworkException("Errow while preparing track : unable to prepare audio track");
            }
        }

        /// <summary>
        /// Finalise the track
        /// </summary>
        public void Finalize()
        {
            try
            {
                if (m_finalized)
                    return;

                if (!m_prepared)
                    throw new FrameworkException("Error while finalizing ISO : AudioTrack has not been prepared");

                m_fileStream.Position = m_fileStream.Length;

                if (m_postgapSize > 0)
                    WriteEmptySectors((int)m_postgapSize);

                m_finalized = true;
            }
            catch (FrameworkException ex)
            {
                throw ex;
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
                m_stream.Write(data);
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
            WriteSector(new byte[m_sectorSize]);
        }

        /// <summary>
        /// Write empty sectors
        /// </summary>
        /// <param name="count">Number of sectors to write</param>
        public void WriteEmptySectors(int count)
        {
            byte[] data = new byte[m_sectorSize];
            for (int i = 0; i < count; i++)
                WriteSector(data);
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
                byte[] buffer = new byte[m_sectorSize];
                int dataRead;

                stream.Position = (container == AudioFileContainer.WAVE) ? 44 : 0;
                m_size = ((stream.Length - stream.Position) / m_sectorSize) + 1;

                for (int sectorsDone = 0; sectorsDone < m_size; sectorsDone++)
                {
                    dataRead = stream.Read(buffer, 0, m_sectorSize);

                    if (dataRead < m_sectorSize)
                    {
                        for (int i = dataRead; i < m_sectorSize; i++)
                            buffer[i] = 0;
                    }

                    WriteSector(buffer);
                }
            }
            catch (Exception)
            {
                throw new FrameworkException("Error while writing audio track : unable to write audio track");
            }
        }

    // Accessors

        /// <summary>
        /// Is the track finalized
        /// </summary>
        public bool IsFinalized
        {
            get { return m_finalized; }
        }
    }
}
