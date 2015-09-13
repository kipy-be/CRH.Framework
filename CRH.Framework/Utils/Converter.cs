using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CRH.Framework.Utils
{
    public static class Converter
    {
        /// <summary>
        /// Convert decimal to hexadecimal representation
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <param name="minSize">The minimum size of the result (will be pre-padded with 0)</param>
        /// <param name="prefix">Prefix result with "0x"</param>
        private static string DecToHex<T>(T value, int minSize = 2, bool prefix = false)
        {
            string hexValue = String.Format("{0:X}", value);

            while (hexValue.Length < minSize)
                hexValue = '0' + hexValue;

            return (prefix ? "0x" : "") + hexValue;
        }

        /// <summary>
        /// Convert decimal to hexadecimal representation
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <param name="minSize">The minimum size of the result (will be pre-padded with 0)</param>
        /// <param name="prefix">Prefix result with "0x"</param>
        public static string DecToHex(byte value, int minSize = 2, bool prefix = false)
        {
            return DecToHex<byte>(value, minSize, prefix);
        }

        /// <summary>
        /// Convert decimal to hexadecimal representation
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <param name="minSize">The minimum size of the result (will be pre-padded with 0)</param>
        /// <param name="prefix">Prefix result with "0x"</param>
        public static string DecToHex(uint value, int minSize = 2, bool prefix = false)
        {
            return DecToHex<uint>(value, minSize, prefix);
        }

        /// <summary>
        /// Convert decimal to hexadecimal representation
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <param name="minSize">The minimum size of the result (will be pre-padded with 0)</param>
        /// <param name="prefix">Prefix result with "0x"</param>
        public static string DecToHex(int value, int minSize = 2, bool prefix = false)
        {
            return DecToHex<int>(value, minSize, prefix);
        }

        /// <summary>
        /// Convert decimal to hexadecimal representation
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <param name="minSize">The minimum size of the result (will be pre-padded with 0)</param>
        /// <param name="prefix">Prefix result with "0x"</param>
        public static string DecToHex(long value, int minSize = 2, bool prefix = false)
        {
            return DecToHex<long>(value, minSize, prefix);
        }

        /// <summary>
        /// Convert decimal to hexadecimal representation
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <param name="minSize">The minimum size of the result (will be pre-padded with 0)</param>
        /// <param name="prefix">Prefix result with "0x"</param>
        public static string DecToHex(ulong value, int minSize = 2, bool prefix = false)
        {
            return DecToHex<ulong>(value, minSize, prefix);
        }

        /// <summary>
        /// Convert decimal to BCD
        /// </summary>
        /// <param name="value">The value to convert</param>
        public static byte DecToBcd(byte value)
        {
            return (byte)(((value / 10) * 16) + (value % 10));
        }

        /// <summary>
        /// Convert BCD to decimal
        /// </summary>
        /// <param name="value">The value to convert</param>
        public static byte BcdToDec(byte value)
        {
            return (byte)(((value / 16) * 10) + (value % 16));
        }
    }
}