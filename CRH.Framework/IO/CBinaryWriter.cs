using System;
using System.IO;
using System.Text;

namespace CRH.Framework.IO
{
    /// <summary>
    /// CBinaryWriter
    /// BinaryWriter extension (including big endian support)
    /// </summary>
    public class CBinaryWriter :  System.IO.BinaryWriter
    {
    // Constructors

        public CBinaryWriter(Stream input)
            : base(input)
        {}

        public CBinaryWriter(Stream input, System.Text.Encoding encoding)
            : base(input, encoding)
        {}

        public CBinaryWriter(byte[] buffer)
            : base(new MemoryStream(buffer))
        {}

        public CBinaryWriter(byte[] buffer, System.Text.Encoding encoding)
            : base(new MemoryStream(buffer), encoding)
        {}

    // Methods

        /// <summary>
        /// Write int16 (BE)
        /// </summary>
        public void WriteInt16BE(short value)
        {
            byte[] buffer = new byte[2];
            buffer[0] = (byte)(value >> 8);
            buffer[1] = (byte)(value);
            this.Write(buffer);
        }

        /// <summary>
        /// Write uint16 (BE)
        /// </summary>
        public void WriteUInt16BE(ushort value)
        {
            byte[] buffer = new byte[2];
            buffer[0] = (byte)(value >> 8);
            buffer[1] = (byte)(value);
            this.Write(buffer);
        }

        /// <summary>
        /// Write int32 (BE)
        /// </summary>
        public void WriteInt32BE(int value)
        {
            byte[] buffer = new byte[4];
            buffer[0] = (byte)(value >> 24);
            buffer[1] = (byte)(value >> 16);
            buffer[2] = (byte)(value >> 8);
            buffer[3] = (byte)(value);
            this.Write(buffer);
        }

        /// <summary>
        /// Write uint32 (BE)
        /// </summary>
        public void WriteUInt32BE(uint value)
        {
            byte[] buffer = new byte[4];
            buffer[0] = (byte)(value >> 24);
            buffer[1] = (byte)(value >> 16);
            buffer[2] = (byte)(value >> 8);
            buffer[3] = (byte)(value);
            this.Write(buffer);
        }

        /// <summary>
        /// Write a ASCII string
        /// </summary>
        public void WriteAsciiString(string str)
        {
            this.Write(Encoding.ASCII.GetBytes(str));
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
