using System.IO;
using System.Text;

namespace CRH.Framework.IO
{
    /// <summary>
    /// CBinaryWriter
    /// BinaryWriter extension (including big endian support)
    /// </summary>
    public class CBinaryWriter : BinaryWriter
    {
        public CBinaryWriter(Stream input)
            : base(input)
        {}

        public CBinaryWriter(Stream input, Encoding encoding)
            : base(input, encoding)
        {}

        public CBinaryWriter(byte[] buffer)
            : base(new MemoryStream(buffer))
        {}

        public CBinaryWriter(byte[] buffer, Encoding encoding)
            : base(new MemoryStream(buffer), encoding)
        {}

    // Methods

        /// <summary>
        /// Write int16 (BE)
        /// </summary>
        public void WriteBE(short value)
        {
            byte[] buffer = [
                (byte)(value >> 8),
                (byte)(value)
            ];
            Write(buffer);
        }

        /// <summary>
        /// Write uint16 (BE)
        /// </summary>
        public void WriteBE(ushort value)
        {
            byte[] buffer = [
                (byte)(value >> 8),
                (byte)(value)
            ];
            Write(buffer);
        }

        /// <summary>
        /// Write int32 (BE)
        /// </summary>
        public void WriteBE(int value)
        {
            byte[] buffer = [
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)(value)
            ];
            Write(buffer);
        }

        /// <summary>
        /// Write uint32 (BE)
        /// </summary>
        public void WriteBE(uint value)
        {
            byte[] buffer = [
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)(value)
            ];
            Write(buffer);
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
            {
                str += paddChar;
            }

            Write(Encoding.ASCII.GetBytes(str));
        }

        /// <summary>
        /// Write a string with the specified encoding
        /// </summary>
        /// <param name="str"></param>
        /// <param name="encoding"></param>
        public void WriteString(string str, Encoding encoding)
        {
            Write(encoding.GetBytes(str));
        }

        /// <summary>
        /// Write padding
        /// </summary>
        /// <param name="length">Length of padding</param>
        /// <param name="value">Value to use (default 0)</param>
        public void WritePadding(int length, byte value = 0)
        {
            for (int i = 0; i < length; i++)
            {
                Write(value);
            }
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
