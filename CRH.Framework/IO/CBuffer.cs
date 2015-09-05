using System;
using System.IO;
using System.Text;

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
            if (trim)
                return Encoding.ASCII.GetString(data, index, count).TrimEnd();
            else
                return Encoding.ASCII.GetString(data, index, count);
        }

        /// <summary>
        /// Copy buffer's data to a new one
        /// </summary>
        /// <param name="data">The data to read</param>
        /// <param name="index">Start offset</param>
        /// <param name="count">Number of bytes to read (if not set, all bytes from source will be read)</param>
        /// <returns></returns>
        public static byte[] Create(byte[] data, int index, int count = -1)
        {
            if (count == -1)
                count = data.Length;

            byte[] buffer = new byte[count];

            for (int i = 0; i < count; i++)
                buffer[i] = data[index + i];

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
                count = data.Length;

            for (int i = 0; i < count; i++)
                buffer[indexBuffer + i] = data[indexData + i];

            return buffer;
        }
    }
}