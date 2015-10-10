using System;

namespace CRH.Framework.Common
{
    public static class Utils
    {
        /// <summary>
        /// Padd a string to match target size
        /// </summary>
        /// <param name="str">The string to padd</param>
        /// <param name="targetSize">The target size</param>
        /// <param name="padChar">The char to fill with</param>
        /// <returns></returns>
        public static string PaddStr(string str, int targetSize, char padChar = ' ')
        {
            while (str.Length < targetSize)
                str += padChar;
            return str;
        }

        /// <summary>
        /// Pre-padd a string to match target size
        /// </summary>
        /// <param name="str">The string to pre-padd</param>
        /// <param name="targetSize">The target size</param>
        /// <param name="padChar">The char to fill with</param>
        /// <returns></returns>
        public static string PrePaddStr(string str, int targetSize, char padChar = ' ')
        {
            while (str.Length < targetSize)
                str = padChar + str;
            return str;
        }
    }
}
