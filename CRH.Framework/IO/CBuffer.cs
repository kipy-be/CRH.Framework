﻿using System.Text;

namespace CRH.Framework.IO
{
    /// <summary>
    /// Helper class for buffers
    /// </summary>
    public static class CBuffer
    {
        /// <summary>
        /// Read an ASCII string from buffer
        /// </summary>
        /// <param name="data">The buffer to read</param>
        /// <param name="index">Start offset</param>
        /// <param name="count">Number of bytes to read</param>
        /// <param name="trim">Trim the string</param>
        /// <returns></returns>
        public static string ReadAsciiString(byte[] data, int index, int count, bool trim = true)
        {
            return trim
                ? Encoding.ASCII.GetString(data, index, count).TrimEnd()
                : Encoding.ASCII.GetString(data, index, count);
        }

        /// <summary>
        /// Copy buffer's data to a new one
        /// </summary>
        /// <param name="data">The data to read</param>
        /// <param name="index">Start offset</param>
        /// <param name="count">Number of bytes to read (if not set, all bytes from source will be read)</param>
        /// <returns></returns>
        public static byte[] Create(byte[] data, int index = 0, int count = -1)
        {
            if (count == -1)
            {
                count = data.Length;
            }

            byte[] buffer = new byte[count];

            for (int i = 0; i < count; i++)
            {
                buffer[i] = data[index + i];
            }

            return buffer;
        }

        /// <summary>
        /// Copy buffer's data to another one
        /// </summary>
        /// <param name="data">The data to read</param>
        /// <param name="buffer">The buffer to write into</param>
        /// <param name="indexData">Reading start offset</param>
        /// <param name="indexBuffer">Writing start offset</param>
        /// <param name="count">Number of bytes to read (if not set, all bytes from source will be read)</param>
        /// <returns></returns>
        public static byte[] Copy(byte[] data, byte[] buffer, int indexData = 0, int indexBuffer = 0, int count = -1)
        {
            if (count == -1)
            {
                count = data.Length;
            }

            for (int i = 0; i < count; i++)
            {
                buffer[indexBuffer + i] = data[indexData + i];
            }

            return buffer;
        }

        /// <summary>
        /// Check if two buffer are equals
        /// </summary>
        /// <param name="buffer1">The first buffer</param>
        /// <param name="buffer2">The second buffer</param>
        /// <returns></returns>
        public static bool IsEquals(byte[] buffer1, byte[] buffer2)
        {
            if (buffer1.Length != buffer2.Length)
            {
                return false;
            }

            for(int i = 0, max = buffer1.Length; i < max; i++)
            {
                if (buffer1[i] != buffer2[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}