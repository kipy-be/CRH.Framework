using System;
using System.IO;

namespace CRH.Framework.IO.Checksum
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
        public static uint Compute(Stream stream)
        {
            uint crc = 0xFFFFFFFF;
            byte b;
            byte[] buffer = new byte[512];
            int dataRead;
            while(stream.Position < stream.Length)
            {
                dataRead = stream.Read(buffer, 0, 512);
                for (int i = 0; i < dataRead; i++)
                {
                    b = (byte)((crc & 0xFF) ^ buffer[i]);
                    crc = (uint)((crc >> 8) ^ m_lookupTable[b]);
                }
            }
            return ~crc;
        }
    }
}
