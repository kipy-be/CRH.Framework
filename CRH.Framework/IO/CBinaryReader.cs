using System;
using System.IO;
using System.Text;

namespace CRH.Framework.IO
{
    /// <summary>
    /// CBinaryReader
    /// BinaryReader extension (including big endian support)
    /// </summary>
    public class CBinaryReader : System.IO.BinaryReader
    {
    // Constructors

        public CBinaryReader(Stream input)
            : base(input)
        {}

        public CBinaryReader(Stream input, System.Text.Encoding encoding)
            : base(input, encoding)
        {}

        public CBinaryReader(byte[] buffer)
            : base(new MemoryStream(buffer))
        {}

        public CBinaryReader(byte[] buffer, System.Text.Encoding encoding)
            : base(new MemoryStream(buffer), encoding)
        {}

    // Methods

        /// <summary>
        /// Read int16 (BE)
        /// </summary>
        /// <returns></returns>
        public short ReadInt16BE()
        {
            byte[] buffer = this.ReadBytes(2);
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
            byte[] buffer = this.ReadBytes(2);
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
            byte[] buffer = this.ReadBytes(4);
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
            byte[] buffer = this.ReadBytes(4);
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
        public string ReadAsciiString(int maxSize, bool trim = true)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                byte b;
                int bytesRead = 0;
                while (bytesRead < maxSize && (b = this.ReadByte()) != 0)
                {
                    ms.WriteByte(b);
                    bytesRead++;
                }

                if (trim)
                    return Encoding.ASCII.GetString(ms.ToArray()).TrimEnd();
                else
                    return Encoding.ASCII.GetString(ms.ToArray());
            }
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
                b = this.ReadByte();
                this.Position--;
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
            get { return this.BaseStream.Position; }
            set { this.BaseStream.Position = value; }
        }
    }
}