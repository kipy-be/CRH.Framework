using System;
using System.IO;

namespace CRH.Framework.IO.Hash
{
    public class Crc32
    {
        private static uint[] m_lookupTable;

        /// <summary>
        /// Initialize lookup table
        /// </summary>
        static Crc32()
        {
            uint p = 0xEDB88320;
            m_lookupTable = new uint[256];

            uint tmp = 0;
            for(int i = 0; i < 256; i++)
            {
                tmp = (uint)i;
                for(int j = 8; j > 0; j--)
                {
                    if((tmp & 1) == 1)
                        tmp = (tmp >> 1) ^ p;
                    else
                        tmp >>= 1;
                }
                m_lookupTable[i] = tmp;
            }
        }

        /// <summary>
        /// Compute a CRC32 for the given stream
        /// </summary>
        /// <param name="stream">The stream to read</param>
        /// <param name="start">Start position (default = begining of the stream)</param>
        /// <param name="length">The length to compute (default = to end of stream)</param>
        public static uint Compute(Stream stream, long start = 0, long length = -1)
        {
            long savePosition = stream.Position;
            long endPosition;

            stream.Position = start;
            endPosition = (length == -1)
                            ? stream.Length - stream.Position
                            : stream.Position + length;

            uint crc = 0xFFFFFFFF;

            byte b;
            int bufferSize = 512;
            byte[] buffer = new byte[bufferSize];
            int dataRead;
            while(stream.Position < endPosition)
            {
                if (endPosition - stream.Position < bufferSize)
                    bufferSize = (int)(endPosition - stream.Position);

                dataRead = stream.Read(buffer, 0, bufferSize);
                for (int i = 0; i < dataRead; i++)
                {
                    b = (byte)((crc & 0xFF) ^ buffer[i]);
                    crc = (uint)((crc >> 8) ^ m_lookupTable[b]);
                }
            }

            stream.Position = savePosition;

            return ~crc;
        }
    }
}
