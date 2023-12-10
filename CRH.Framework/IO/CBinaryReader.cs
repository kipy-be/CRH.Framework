using System;
using System.IO;
using System.Text;

namespace CRH.Framework.IO
{
    /// <summary>
    /// CBinaryReader
    /// BinaryReader extension (including big endian support)
    /// </summary>
    public class CBinaryReader : BinaryReader
    {
        public CBinaryReader(Stream input)
            : base(input)
        {}

        public CBinaryReader(Stream input, Encoding encoding)
            : base(input, encoding)
        {}

        public CBinaryReader(byte[] buffer)
            : base(new MemoryStream(buffer))
        {}

        public CBinaryReader(byte[] buffer, Encoding encoding)
            : base(new MemoryStream(buffer), encoding)
        {}

        /// <summary>
        /// Read int16 (BE)
        /// </summary>
        /// <returns></returns>
        public short ReadInt16BE()
        {
            byte[] buffer = ReadBytes(2);
            return (short)
                ((buffer[0] << 8)
                | buffer[1]);
        }

        /// <summary>
        /// Read uint16 (BE)
        /// </summary>
        /// <returns></returns>
        public ushort ReadUInt16BE()
        {
            byte[] buffer = ReadBytes(2);
            return (ushort)
                ((buffer[0] << 8)
                | buffer[1]);
        }

        /// <summary>
        /// Read int32 (BE)
        /// </summary>
        /// <returns></returns>
        public int ReadInt32BE()
        {
            byte[] buffer = ReadBytes(4);
            return (int)
                ( (buffer[0] << 24)
                | (buffer[1] << 16)
                | (buffer[2] << 8)
                |  buffer[3]);
        }

        /// <summary>
        /// Read uint32 (BE)
        /// </summary>
        /// <returns></returns>
        public uint ReadUInt32BE()
        {
            byte[] buffer = ReadBytes(4);
            return (uint)
                ( (buffer[0] << 24)
                | (buffer[1] << 16)
                | (buffer[2] << 8)
                |  buffer[3]);
        }

        /// <summary>
        /// Read a ASCII string until maxSize is reached or 0x00 is read
        /// </summary>
        /// <param name="maxSize">Max size of the string to read</param>
        /// <param name="trim">Trim the string</param>
        /// <returns></returns>
        public string ReadAsciiString(int maxSize = -1, bool trim = true)
        {
            using (var ms = new MemoryStream())
            {
                byte b;
                int bytesRead = 0;

                if (maxSize > -1)
                {
                    while (bytesRead < maxSize && (b = ReadByte()) != 0)
                    {
                        ms.WriteByte(b);
                        bytesRead++;
                    }
                }
                else
                {
                    while ((b = ReadByte()) != 0)
                    {
                        ms.WriteByte(b);
                        bytesRead++;
                    }
                }

                return trim
                    ? Encoding.ASCII.GetString(ms.ToArray()).TrimEnd()
                    : Encoding.ASCII.GetString(ms.ToArray());
            }
        }

        /// <summary>
        /// Read hexadecimal
        /// </summary>
        /// <param name="size">bytes to read</param>
        /// <returns></returns>
        public string ReadHexa(int size)
        {
            byte[] buffer = new byte[size];
            Read(buffer, 0, size);
            return BitConverter.ToString(buffer).Replace("-", string.Empty);
        }

        /// <summary>
        /// Read byte without consuming it
        /// </summary>
        /// <returns></returns>
        public short TestByte()
        {
            byte b;

            try
            {
                b = ReadByte();
                Position--;
            }
            catch(Exception)
            {
                return -1;
            }

            return b;
        }

    // Accessors

        /// <summary>
        /// Position of the base stream
        /// </summary>
        public long Position
        {
            get => BaseStream.Position;
            set => BaseStream.Position = value;
        }

        /// <summary>
        /// Length of the base stream
        /// </summary>
        public long Length => BaseStream.Length;
    }
}