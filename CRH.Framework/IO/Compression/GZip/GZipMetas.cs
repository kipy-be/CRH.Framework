using System;
using System.IO;
using CRH.Framework.IO;
using CRH.Framework.Common;

namespace CRH.Framework.IO.Compression
{
    public enum GZipCompressionMethod : byte
    {
        DEFLATE = 8
    }

    internal enum GZipFlag
    {
        HAS_COMMENT = (1 << 3),
        HAS_NAME    = (1 << 4),
        HAS_EXTRA   = (1 << 5),
        HAS_CRC     = (1 << 6),
        IS_TEXT     = (1 << 7)
    }

    public class GZipMetas
    {
        public const ushort SIGNATURE = 0x8B1F;

        public const byte DEFLATE = 8;
        public const byte FOOTER_SIZE = 8;

        private GZipCompressionMethod m_method;

        private byte     m_flags;
        private uint     m_dataOffset;
        private uint     m_dataSize;
        private uint     m_dataRealSize;
        private uint     m_crc32;
        private string   m_comment;
        private ushort   m_crc;
        private string   m_extra;
        private string   m_name;
        private DateTime m_date;
        private byte     m_xfl;
        private byte m_os;

    // Constructors

        internal GZipMetas()
        {
            m_method     = GZipCompressionMethod.DEFLATE;
            m_dataOffset = 0;
            m_dataSize   = 0;
            m_flags      = 0;
            m_xfl        = 4;
            m_os         = 0;
            m_date       = DateTime.Now;
        }

    // Methods

        /// <summary>
        /// Read GZip meta data from stream
        /// </summary>
        /// <param name="stream">The stream to read</param>
        /// <param name="size">The size of the GZip file</param>
        internal void Read(Stream stream, uint size)
        {
            try
            {
                CBinaryReader reader = new CBinaryReader(stream);

                if (reader.ReadUInt16() != SIGNATURE)
                    throw new FrameworkException("Error while parsing gzip : gzip signature not found");

                m_method = (GZipCompressionMethod)reader.ReadByte();
                m_flags  = reader.ReadByte();
                m_date   = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc).AddSeconds(reader.ReadUInt32());
                m_xfl    = reader.ReadByte();
                m_os     = reader.ReadByte();

                if (HasExtra)
                {
                    int extraSize = reader.ReadUInt16();
                    m_extra = reader.ReadAsciiString(extraSize);
                    reader.Position++;
                }

                if (HasName)
                    m_name = reader.ReadAsciiString();

                if (HasComment)
                    m_comment = reader.ReadAsciiString();

                if (HasCrc)
                    m_crc = reader.ReadUInt16();

                m_dataOffset = (uint)reader.Position;
                m_dataSize = size - m_dataOffset - FOOTER_SIZE;
                reader.Position = size - FOOTER_SIZE;
                m_crc32 = reader.ReadUInt32();
                m_dataRealSize = reader.ReadUInt32();
            }
            catch(FrameworkException ex)
            {
                throw ex;
            }
            catch(Exception)
            {
                throw new FrameworkException("Error while parsing gzip data : unable to read meta data");
            }
        }

        /// <summary>
        /// Write header
        /// </summary>
        /// <param name="stream">The stream to write to</param>
        internal void WriteHeader(Stream stream)
        {
            try
            {
                CBinaryWriter writer = new CBinaryWriter(stream);

                writer.Write(SIGNATURE);
                writer.Write((byte)m_method);
                writer.Write(m_flags);
                writer.Write((uint)((m_date - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds));
                writer.Write(m_xfl);
                writer.Write(m_os);

                if (HasExtra)
                {
                    writer.Write((ushort)m_extra.Length);
                    writer.WriteAsciiString(m_extra);
                }

                if (HasName)
                {
                    writer.WriteAsciiString(m_name);
                    writer.Write((byte)0);
                }

                if (HasComment)
                {
                    writer.WriteAsciiString(m_comment);
                    writer.Write((byte)0);
                }

                if (HasCrc)
                    writer.Write(m_crc);
            }
            catch(FrameworkException ex)
            {
                throw ex;
            }
            catch(Exception)
            {
                throw new FrameworkException("Error while writing gzip data : unable to write header");
            }
        }

        /// <summary>
        /// Write footer
        /// </summary>
        /// <param name="stream">The stream to write to</param>
        internal void WriteFooter(Stream stream)
        {
            try
            {
                CBinaryWriter writer = new CBinaryWriter(stream);

                writer.Write(m_crc32);
                writer.Write(m_dataRealSize);
            }
            catch (FrameworkException ex)
            {
                throw ex;
            }
            catch (Exception)
            {
                throw new FrameworkException("Error while writing gzip data : unable to write footer");
            }
        }

        /// <summary>
        /// Get specific flag state from Flags field
        /// </summary>
        private bool GetFlag(GZipFlag mask)
        {
            return (m_flags & (byte)mask) > 0;
        }

        /// <summary>
        /// Set flag state into Flags field
        /// </summary>
        private void SetFlag(GZipFlag mask, bool value)
        {
            if (value)
                m_flags |= (byte)mask;
            else
                m_flags &= (byte)(0xFF ^ (byte)mask);
        }

    // Accessors

        /// <summary>
        /// The compression method
        /// Supported : deflate
        /// </summary>
        internal GZipCompressionMethod Method
        {
            get { return m_method; }
            set { m_method = value; }
        }

        /// <summary>
        /// Flags
        /// </summary>
        internal byte Flags
        {
            get { return m_flags; }
            set { m_flags = value; }
        }

        /// <summary>
        /// Has comment
        /// </summary>
        internal bool HasComment
        {
            get { return GetFlag(GZipFlag.HAS_COMMENT); }
            set { SetFlag(GZipFlag.HAS_COMMENT, value); }
        }

        /// <summary>
        /// Has name
        /// </summary>
        internal bool HasName
        {
            get { return GetFlag(GZipFlag.HAS_NAME); }
            set { SetFlag(GZipFlag.HAS_NAME, value); }
        }

        /// <summary>
        /// Has extra
        /// </summary>
        internal bool HasExtra
        {
            get { return GetFlag(GZipFlag.HAS_EXTRA); }
            set { SetFlag(GZipFlag.HAS_EXTRA, value); }
        }

        /// <summary>
        /// Has crc16
        /// </summary>
        internal bool HasCrc
        {
            get { return GetFlag(GZipFlag.HAS_CRC); }
            set { SetFlag(GZipFlag.HAS_CRC, value); }
        }

        /// <summary>
        /// Is binary (text ortherwise)
        /// </summary>
        public bool IsBinary
        {
            get { return !GetFlag(GZipFlag.IS_TEXT); }
            set { SetFlag(GZipFlag.IS_TEXT, !value); }
        }

        /// <summary>
        /// Is text (binary ortherwise)
        /// </summary>
        public bool IsText
        {
            get { return GetFlag(GZipFlag.IS_TEXT); }
            set { SetFlag(GZipFlag.IS_TEXT, value); }
        }

        /// <summary>
        /// The data offset
        /// </summary>
        internal uint DataOffset
        {
            get { return m_dataOffset; }
            set { m_dataOffset = value; }
        }

        /// <summary>
        /// The data size (compressed)
        /// </summary>
        internal uint DataSize
        {
            get { return m_dataSize; }
            set { m_dataSize = value; }
        }

        /// <summary>
        /// The data size (uncompressed)
        /// </summary>
        internal uint DataRealSize
        {
            get { return m_dataRealSize; }
            set { m_dataRealSize = value; }
        }

        /// <summary>
        /// The CRC32
        /// </summary>
        internal uint Crc32
        {
            get { return m_crc32; }
            set { m_crc32 = value; }
        }

        /// <summary>
        /// Comment
        /// </summary>
        public string Comment
        {
            get { return m_comment; }
            set
            {
                m_comment = value;
                if (!HasComment)
                    HasComment = true;
            }
        }

        /// <summary>
        /// CRC16
        /// </summary>
        public ushort Crc
        {
            get { return m_crc; }
            set
            {
                m_crc = value;
                if (!HasCrc)
                    HasCrc = true;
            }
        }

        /// <summary>
        /// Extra
        /// </summary>
        public string Extra
        {
            get { return m_extra; }
            set
            { 
                m_extra = value;
                if (!HasExtra)
                    HasExtra = true;
            }
        }

        /// <summary>
        /// Name
        /// </summary>
        public string Name
        {
            get { return m_name; }
            set
            {
                m_name = value;
                if (!HasName)
                    HasName = true;
            }
        }

        /// <summary>
        /// Modification date
        /// </summary>
        public DateTime Date
        {
            get { return m_date; }
            set { m_date = value; }
        }

        /// <summary>
        /// XLF
        /// </summary>
        public byte Xfl
        {
            get { return m_xfl; }
            set { m_xfl = value; }
        }

        /// <summary>
        /// OS
        /// </summary>
        public byte Os
        {
            get { return m_os; }
            set { m_os = value; }
        }
    }
}
