namespace CRH.Framework.Common
{
    internal static class ExtensionMethods
    {
        /// <summary>
        /// To hexadecimal representation
        /// </summary>
        /// <param name="minSize">The minimum size of the result (will be pre-padded with 0)</param>
        /// <param name="prefix">Prefix result with "0x"</param>
        internal static string ToHex(this byte value, int minSize = 2, bool prefix = false)
        {
            return Converter.DecToHex(value, minSize, prefix);
        }

        /// <summary>
        /// To hexadecimal representation
        /// </summary>
        /// <param name="minSize">The minimum size of the result (will be pre-padded with 0)</param>
        /// <param name="prefix">Prefix result with "0x"</param>
        internal static string ToHex(this uint value, int minSize = 2, bool prefix = false)
        {
            return Converter.DecToHex(value, minSize, prefix);
        }

        /// <summary>
        /// To hexadecimal representation
        /// </summary>
        /// <param name="minSize">The minimum size of the result (will be pre-padded with 0)</param>
        /// <param name="prefix">Prefix result with "0x"</param>
        internal static string ToHex(this int value, int minSize = 2, bool prefix = false)
        {
            return Converter.DecToHex(value, minSize, prefix);
        }

        /// <summary>
        /// To hexadecimal representation
        /// </summary>
        /// <param name="minSize">The minimum size of the result (will be pre-padded with 0)</param>
        /// <param name="prefix">Prefix result with "0x"</param>
        internal static string ToHex(this long value, int minSize = 2, bool prefix = false)
        {
            return Converter.DecToHex(value, minSize, prefix);
        }

        /// <summary>
        /// To hexadecimal representation
        /// </summary>
        /// <param name="minSize">The minimum size of the result (will be pre-padded with 0)</param>
        /// <param name="prefix">Prefix result with "0x"</param>
        internal static string ToHex(this ulong value, int minSize = 2, bool prefix = false)
        {
            return Converter.DecToHex(value, minSize, prefix);
        }
    }
}
