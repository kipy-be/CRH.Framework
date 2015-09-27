﻿using System;
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
        /// <param name="str">The string to write</param>
        /// <param name="minSize">The minimum size of the string (will be padded otherwise)</param>
        /// <param name="paddChar">The padding char used (default = space)</param>
        /// </summary>
        public void WriteAsciiString(string str, int minSize = 0, string paddChar = " ")
        {
            while (str.Length < minSize)
                str += paddChar;

            this.Write(Encoding.ASCII.GetBytes(str));
        }

        /// <summary>
        /// Write padding
        /// </summary>
        /// <param name="length">Length of padding</param>
        /// <param name="value">Value to use (default 0)</param>
        public void WritePadding(int length, byte value = 0)
        {
            for (int i = 0; i < length; i++)
                this.Write(value);
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
