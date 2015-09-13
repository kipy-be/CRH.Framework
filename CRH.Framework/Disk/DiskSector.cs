using System;
using CRH.Framework.Common;
using CRH.Framework.IO;
using CRH.Framework.Utils;

namespace CRH.Framework.Disk
{
    public enum HeaderMode : byte
    {
        EMPTY = 0,
        MODE1 = 1,
        MODE2 = 2
    }

    public sealed class DiskSector
    {
        public const int SYNC_SIZE         = 12;
        public const int HEADER_SIZE       = 4;
        public const int SUBHEADER_SIZE    = 8;
        
        public const int EDC_SIZE          = 4;
        public const int INTERMEDIATE_SIZE = 8;
        public const int ECC_SIZE          = 276;
        public const int ECC_P_SIZE        = 172;
        public const int ECC_Q_SIZE        = 104;

        public static readonly byte[] SYNC = new byte[] { 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00 };

        private SectorMode m_mode;
        private int m_size;
        private int m_dataSize;

        private byte[] m_sync;
        private byte[] m_header;
        private byte[] m_subHeader;
        private byte[] m_data;
        private byte[] m_edc;
        private byte[] m_intermediate;
        private byte[] m_eccP;
        private byte[] m_eccQ;

    // Constructors

        /// <summary>
        /// DiskSector
        /// </summary>
        /// <param name="mode">Sector's mode</param>
        /// <param name="size">Sector's size</param>
        internal DiskSector(SectorMode mode, int size)
        {
            m_mode         = mode;
            m_size         = size;
            m_dataSize     = GetDataSize(size, mode);

            m_sync         = new byte[SYNC_SIZE];
            m_header       = new byte[HEADER_SIZE];
            m_subHeader    = new byte[SUBHEADER_SIZE / 2];
            m_data         = new byte[m_dataSize];
            m_edc          = new byte[EDC_SIZE];
            m_intermediate = new byte[INTERMEDIATE_SIZE];
            m_eccP         = new byte[ECC_P_SIZE];
            m_eccQ         = new byte[ECC_Q_SIZE];
        }

    // Methods

        /// <summary>
        /// Get the user data size of a sector
        /// </summary>
        /// <param name="mode">Sector's mode</param>
        /// <param name="size">Sector's size</param>
        /// <returns></returns>
        internal static int GetDataSize(int size, SectorMode mode)
        {
            switch(mode)
            {
                case SectorMode.MODE1:
                    return size - (SYNC_SIZE + HEADER_SIZE + INTERMEDIATE_SIZE + EDC_SIZE + ECC_SIZE);
                case SectorMode.MODE2:
                    return size - (SYNC_SIZE + HEADER_SIZE);
                case SectorMode.XA_FORM1:
                    return size - (SYNC_SIZE + HEADER_SIZE + SUBHEADER_SIZE + EDC_SIZE + ECC_SIZE);
                case SectorMode.XA_FORM2:
                    return size - (SYNC_SIZE + HEADER_SIZE + SUBHEADER_SIZE + EDC_SIZE);
                case SectorMode.RAW:
                default:
                    return size;
            }
        }

        /// <summary>
        /// Compute the EDC and ECC fields
        /// </summary>
        internal void ComputeEdcEcc()
        {
            throw new FrameworkNotYetImplementedException();
        }

    // Accessors

        /// <summary>
        /// The mode of the sector
        /// </summary>
        public SectorMode Mode
        {
            get { return m_mode; }
        }

        /// <summary>
        /// The size of the sector (including metadata)
        /// </summary>
        public int Size
        {
            get { return m_size; }
        }

        /// <summary>
        /// The size of the user's data (size of sector excluding metadata)
        /// </summary>
        public int DataSize
        {
            get { return m_dataSize; }
        }

        /// <summary>
        /// Sync (metadata)
        /// Used in mode : 1, 2
        /// </summary>
        internal byte[] Sync
        {
            get { return m_sync; }
            set { m_sync = value; }
        }

        /// <summary>
        /// Header (metadata)
        /// Used in mode : 1, 2
        /// </summary>
        internal byte[] Header
        {
            get { return m_header; }
            set { m_header = value; }
        }

        /// <summary>
        /// Minute (stored in header)
        /// Used in mode : 1, 2
        /// </summary>
        internal byte Minute
        {
            get { return Converter.BcdToDec(m_header[0]); }
            set { m_header[0] = Converter.DecToBcd(value); }
        }

        /// <summary>
        /// Second (stored in header)
        /// Used in mode : 1, 2
        /// </summary>
        internal byte Second
        {
            get { return Converter.BcdToDec(m_header[1]); }
            set { m_header[1] = Converter.DecToBcd(value); }
        }

        /// <summary>
        /// Block (stored in header)
        /// Note : they are 75 blocs in on second
        /// Used in mode : 1, 2
        /// </summary>
        internal byte Block
        {
            get { return Converter.BcdToDec(m_header[2]); }
            set { m_header[2] = Converter.DecToBcd(value); }
        }

        /// <summary>
        /// HMode (stored in header)
        /// Used in mode : 1, 2
        /// Value : 0 if sector is empty, mode otherwise (1 or 2)
        /// </summary>
        internal byte HMode
        {
            get { return m_header[3]; }
            set { m_header[3] = value; }
        }

        /// <summary>
        /// Is Empty block
        /// </summary>
        internal bool IsEmpty
        {
            get { return m_header[3] == 0; }
        }

        /// <summary>
        /// SubHeader (metadata)
        /// Used in mode : 2
        /// </summary>
        internal byte[] SubHeader
        {
            get { return m_subHeader; }
            set { m_subHeader = value; }
        }

        /// <summary>
        /// User's data
        /// </summary>
        public byte[] Data
        {
            get { return m_data; }
            set { m_data = value; }
        }

        /// <summary>
        /// Error detecting code (metadata)
        /// Used in mode : 1, 2
        /// </summary>
        internal byte[] Edc
        {
            get { return m_edc; }
            set { m_edc = value; }
        }

        /// <summary>
        /// Intermediate (padding) (metadata)
        /// Used in mode : 1
        /// </summary>
        internal byte[] Intermediate
        {
            get { return m_intermediate; }
            set { m_intermediate = value; }
        }

        /// <summary>
        /// Error correcting code (P) (metadata)
        /// Used in mode : 1, 2
        /// </summary>
        internal byte[] EccP
        {
            get { return m_eccP; }
            set { m_eccP = value; }
        }

        /// <summary>
        /// Error correcting code (Q) (metadata)
        /// Used in mode : 1, 2
        /// </summary>
        internal byte[] EccQ
        {
            get { return m_eccQ; }
            set { m_eccQ = value; }
        }
    }
}