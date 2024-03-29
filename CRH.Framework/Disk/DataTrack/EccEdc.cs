﻿/*
    Based on C sources by yuaiyu1987 from http://linux.programdevelop.com/1752355/
*/

namespace CRH.Framework.Disk.DataTrack
{
    internal static class EccEdc
    {
        static private byte[] _eccFLookupTable;
        static private byte[] _eccBLookupTable;
        static private uint[] _edcLookupTable;

        private static byte[] _headerBackup;

        /// <summary>
        /// Initialize lookup tables
        /// </summary>
        static EccEdc()
        {
            _headerBackup = new byte[4];

            _eccFLookupTable = new byte[256];
            _eccBLookupTable = new byte[256];
            _edcLookupTable  = new uint[256];

            uint i, j, edc;
	        for (i = 0; i < 256; i++)
	        {
                j = (uint)((i << 1) ^ ((i & 0x80) != 0 ? 0x11D : 0));
                _eccFLookupTable[i] = (byte)j;
                _eccBLookupTable[i ^ j] = (byte)i;
		        edc = i;
                for (j = 0; j < 8; j++)
                {
                    edc = (uint)((edc >> 1) ^ ((edc & 1) != 0 ? 0xD8018001 : 0));
                }
		        _edcLookupTable[i] = edc;
	        }
        }

        /// <summary>
        /// Compute Ecc/edc for the given sector
        /// </summary>
        /// <param name="sector">The sector</param>
        internal static void EccEdcCompute(byte[] sector, SectorMode mode)
        {
            switch (mode)
            {
                case SectorMode.MODE1:
                    EdcBlockCompute(sector, 0, DataTrack.GetSectorDataSize(mode) + 16);
                    EccCompute(sector);
                    break;

                case SectorMode.XA_FORM1:
                    EdcBlockCompute(sector, 16, DataTrack.SUBHEADER_SIZE + DataTrack.GetSectorDataSize(mode));
                    _headerBackup[0] = sector[12]; _headerBackup[1] = sector[13]; _headerBackup[2] = sector[14]; _headerBackup[3] = sector[15];
                    sector[12] = sector[13] = sector[14] = sector[15] = 0;
                    EccCompute(sector);
                    sector[12] = _headerBackup[0]; sector[13] = _headerBackup[1]; sector[14] = _headerBackup[2]; sector[15] = _headerBackup[3];
                    break;

                case SectorMode.XA_FORM2:
                    EdcBlockCompute(sector, 16, DataTrack.SUBHEADER_SIZE + DataTrack.GetSectorDataSize(mode));
                    break;
            }
        }

        /// <summary>
        /// Compute Edc
        /// </summary>
        /// <param name="sector">The sector</param>
        /// <param name="offset">Offset of the block to compute</param>
        /// <param name="count">Length of the block to compute</param>
        private static void EdcBlockCompute(byte[] sector, int offset, int length)
        {
            uint edc = 0;

            for (int i = offset, max = offset + length; i < max; i++)
            {
                edc = (uint)((edc >> 8) ^ _edcLookupTable[(edc ^ (sector[i])) & 0xFF]);
            }

            sector[2072] = (byte)(edc & 0xFF);
            sector[2073] = (byte)((edc >> 8) & 0xFF);
            sector[2074] = (byte)((edc >> 16) & 0xFF);
            sector[2075] = (byte)((edc >> 24) & 0xFF);
        }

        /// <summary>
        /// Compute ECC P and Q
        /// </summary>
        private static void EccCompute(byte[] sector)
        {
            EccBlockCompute(sector, 86, 24, 2, 86, 2076);  // P
            EccBlockCompute(sector, 52, 43, 86, 88, 2248); // Q
        }

        /// <summary>
        /// Compute ECC (P or Q)
        /// </summary>
        private static void EccBlockCompute(byte[] sector, uint majorCount, uint minorCount, uint majorMult, uint minorInc, int destOffset)
        {
            uint size = majorCount * minorCount;
            uint major, minor;
            byte eccA, eccB;

            for (major = 0; major < majorCount; major++)
            {
                uint i = (uint)((major >> 1) * majorMult + (major & 1));
                eccA = 0;
                eccB = 0;

                for (minor = 0; minor < minorCount; minor++)
                {
                    byte temp = sector[i + 12];
                    i += minorInc;

                    if (i >= size)
                    {
                        i -= size;
                    }

                    eccA ^= temp;
                    eccB ^= temp;
                    eccA = _eccFLookupTable[eccA];
                }

                eccA = _eccBLookupTable[_eccFLookupTable[eccA] ^ eccB];
                sector[major + destOffset] = eccA;
                sector[major + majorCount + destOffset] = (byte)(eccA ^ eccB);
            }
        }
    }
}
