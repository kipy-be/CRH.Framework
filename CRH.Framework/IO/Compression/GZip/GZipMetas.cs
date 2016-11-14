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

        private GZipCompressionMethod _method;

        private byte     _flags;
        private uint     _dataOffset;
        private uint     _dataSize;
        private uint     _dataRealSize;
        private uint     _crc32;
        private string   _comment;
        private ushort   _crc;
        private string   _extra;
        private string   _name;
        private DateTime _date;
        private byte     _xfl;
        private byte     _os;

    // Constructors

        internal GZipMetas()
        {
            _method     = GZipCompressionMethod.DEFLATE;
            _dataOffset = 0;
            _dataSize   = 0;
            _flags      = 0;
            _xfl        = 4;
            _os         = 0;
            _date       = DateTime.Now;
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
                var reader = new CBinaryReader(stream);

                if (reader.ReadUInt16() != SIGNATURE)
                    throw new FrameworkException("Error while parsing gzip : gzip signature not found");

                _method = (GZipCompressionMethod)reader.ReadByte();
                _flags  = reader.ReadByte();
                _date   = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(reader.ReadUInt32());
                _xfl    = reader.ReadByte();
                _os     = reader.ReadByte();

                if (HasExtra)
                {
                    int extraSize = reader.ReadUInt16();
                    _extra = reader.ReadAsciiString(extraSize);
                    reader.Position++;
                }

                if (HasName)
                    _name = reader.ReadAsciiString();

                if (HasComment)
                    _comment = reader.ReadAsciiString();

                if (HasCrc)
                    _crc = reader.ReadUInt16();

                _dataOffset = (uint)reader.Position;
                _dataSize = size - _dataOffset - FOOTER_SIZE;
                reader.Position = size - FOOTER_SIZE;
                _crc32 = reader.ReadUInt32();
                _dataRealSize = reader.ReadUInt32();
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
                writer.Write((byte)_method);
                writer.Write(_flags);
                writer.Write((uint)((_date - new DateTime(1970, 1, 1).ToLocalTime()).TotalSeconds));
                writer.Write(_xfl);
                writer.Write(_os);

                if (HasExtra)
                {
                    writer.Write((ushort)_extra.Length);
                    writer.WriteAsciiString(_extra);
                }

                if (HasName)
                {
                    writer.WriteAsciiString(_name);
                    writer.Write((byte)0);
                }

                if (HasComment)
                {
                    writer.WriteAsciiString(_comment);
                    writer.Write((byte)0);
                }

                if (HasCrc)
                    writer.Write(_crc);
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
                var writer = new CBinaryWriter(stream);

                writer.Write(_crc32);
                writer.Write(_dataRealSize);
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
            return (_flags & (byte)mask) > 0;
        }

        /// <summary>
        /// Set flag state into Flags field
        /// </summary>
        private void SetFlag(GZipFlag mask, bool value)
        {
            if (value)
                _flags |= (byte)mask;
            else
                _flags &= (byte)(0xFF ^ (byte)mask);
        }

    // Accessors

        /// <summary>
        /// The compression method
        /// Supported : deflate
        /// </summary>
        internal GZipCompressionMethod Method
        {
            get { return _method; }
            set { _method = value; }
        }

        /// <summary>
        /// Flags
        /// </summary>
        internal byte Flags
        {
            get { return _flags; }
            set { _flags = value; }
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
            get { return _dataOffset; }
            set { _dataOffset = value; }
        }

        /// <summary>
        /// The data size (compressed)
        /// </summary>
        internal uint DataSize
        {
            get { return _dataSize; }
            set { _dataSize = value; }
        }

        /// <summary>
        /// The data size (uncompressed)
        /// </summary>
        internal uint DataRealSize
        {
            get { return _dataRealSize; }
            set { _dataRealSize = value; }
        }

        /// <summary>
        /// The CRC32
        /// </summary>
        internal uint Crc32
        {
            get { return _crc32; }
            set { _crc32 = value; }
        }

        /// <summary>
        /// Comment
        /// </summary>
        public string Comment
        {
            get { return _comment; }
            set
            {
                _comment = value;
                if (!HasComment)
                    HasComment = true;
            }
        }

        /// <summary>
        /// CRC16
        /// </summary>
        public ushort Crc
        {
            get { return _crc; }
            set
            {
                _crc = value;
                if (!HasCrc)
                    HasCrc = true;
            }
        }

        /// <summary>
        /// Extra
        /// </summary>
        public string Extra
        {
            get { return _extra; }
            set
            { 
                _extra = value;
                if (!HasExtra)
                    HasExtra = true;
            }
        }

        /// <summary>
        /// Name
        /// </summary>
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                if (!HasName)
                    HasName = true;
            }
        }

        /// <summary>
        /// Modification date
        /// </summary>
        public DateTime Date
        {
            get { return _date; }
            set { _date = value; }
        }

        /// <summary>
        /// XLF
        /// </summary>
        public byte Xfl
        {
            get { return _xfl; }
            set { _xfl = value; }
        }

        /// <summary>
        /// OS
        /// </summary>
        public byte Os
        {
            get { return _os; }
            set { _os = value; }
        }
    }
}
