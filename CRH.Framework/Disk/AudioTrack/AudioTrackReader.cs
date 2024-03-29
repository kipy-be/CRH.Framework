﻿using CRH.Framework.Common;
using CRH.Framework.IO;
using System;
using System.IO;

namespace CRH.Framework.Disk.AudioTrack
{
    public sealed class AudioTrackReader : AudioTrack, ITrackReader
    {
        private CBinaryReader _stream;

        /// <summary>
        /// AudioTrackReader
        /// </summary>
        /// <param name="trackNumber">The track number</param>
        internal AudioTrackReader(CBinaryReader stream, int trackNumber)
            : base((FileStream)stream.BaseStream, trackNumber)
        {
            _stream = stream;

            SeekSector(0);
        }

        /// <summary>
        /// Read a sector's data
        /// </summary>
        public byte[] ReadSector()
        {
            try
            {
                return _stream.ReadBytes(_sectorSize);
            }
            catch (FrameworkException)
            {
                throw;
            }
            catch (EndOfStreamException)
            {
                throw new FrameworkException("Errow while reading sector : end of file occured");
            }
            catch (Exception)
            {
                throw new FrameworkException("Errow while reading sector : unable to read sector");
            }
        }

        /// <summary>
        /// Read a sector's data
        /// </summary>
        /// <param name="lba">Sector's LBA to read</param>
        public byte[] ReadSector(long lba)
        {
            SeekSector(lba);
            return ReadSector();
        }

        /// <summary>
        /// Read several consecutives sectors's data
        /// </summary>
        /// <param name="count">Number of sectors to read</param>
        public byte[] ReadSectors(int count)
        {
            return _stream.ReadBytes(count * _sectorSize);
        }

        /// <summary>
        /// Read several consecutives sectors data (only data : does not include modes specifics fields)
        /// </summary>
        /// <param name="lba">Starting sector's LBA</param>
        /// <param name="count">Number of sectors to read</param>
        public byte[] ReadSectors(long lba, int count)
        {
            SeekSector(lba);
            return ReadSectors(count);
        }

        /// <summary>
        /// Read the audio track
        /// </summary>
        /// <param name="stream">The stream to write the data</param>
        public void Read(Stream stream)
        {
            SeekSector(0);
            for (int sectorsReads = 0; sectorsReads < _size; sectorsReads++)
            {
                stream.Write(ReadSector(), 0, _sectorSize);
            }

            stream.Flush();
        }

        /// <summary>
        /// Read the audio track
        /// </summary>
        public Stream Read()
        {
            MemoryStream ms = new MemoryStream();
            Read(ms);

            return ms;
        }

        /// <summary>
        /// Extract the audio track
        /// </summary>
        /// <param name="outFilePath">The full path of the disk's file (eg : /FOO/BAR/FILE.EXT)</param>
        /// <param name="container">The container used for the file</param>
        public void Extract(string outFilePath, AudioFileContainer container)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outFilePath));
                using (var fs = new FileStream(outFilePath, FileMode.Create, FileAccess.Write))
                {
                    if(container == AudioFileContainer.WAVE)
                    {
                        using(var stream = new CBinaryWriter(fs))
                        {
                            // WAVE Header
                            uint dataSize = (uint)(_size * _sectorSize);
                            stream.WriteAsciiString("RIFF");          // RIFF sign
                            stream.Write((uint)(dataSize + 44 - 8));  // File size - 8, wave header is 44 bytes long
                            stream.WriteAsciiString("WAVE");          // Format ID
                            stream.WriteAsciiString("fmt", 4);        // Format bloc ID
                            stream.Write((uint)16);                   // Bloc size
                            stream.Write((ushort)0x01);               // PCM
                            stream.Write((ushort)2);                  // Channels
                            stream.Write((uint)44100);                // Frequency
                            stream.Write((uint)(44100 * 2 * 16 / 8)); // Bytes per sec (frequency * bytes per bloc)
                            stream.Write((ushort)(2 * 16 / 8));       // Bytes per bloc (channels * bits per sample / 8)
                            stream.Write((ushort)16);                 // Bits per sample
                            stream.WriteAsciiString("data");          // data bloc sign
                            stream.Write((uint)dataSize);             // data size
                            Read(fs);
                         }
                    }
                    else
                        Read(fs);
                }
            }
            catch (FrameworkException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new FrameworkException("Error while writing audio file : unable to write file \"{0}\"", outFilePath);
            }
        }
    }
}
