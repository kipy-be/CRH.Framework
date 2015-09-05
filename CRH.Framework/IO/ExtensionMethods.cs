using System;
using System.IO;
using CRH.Framework.IO;

namespace CRH.Framework.IO
{
    internal static class ExtensionMethods
    {
        /// <summary>
        /// Close and dispose the stream
        /// </summary>
        internal static void CloseAndDispose(this StreamReader stream)
        {
            if (stream != null)
            {
                stream.Close();
                stream.Dispose();
                stream = null;
            }
        }

        /// <summary>
        /// Close and dispose the stream
        /// </summary>
        internal static void CloseAndDispose(this StreamWriter stream)
        {
            if (stream != null)
            {
                stream.Close();
                stream.Dispose();
                stream = null;
            }
        }

        /// <summary>
        /// Close and dispose the stream
        /// </summary>
        internal static void CloseAndDispose(this FileStream stream)
        {
            if (stream != null)
            {
                stream.Close();
                stream.Dispose();
                stream = null;
            }
        }

        /// <summary>
        /// Close and dispose the stream
        /// </summary>
        internal static void CloseAndDispose(this CBinaryReader stream)
        {
            if (stream != null)
            {
                stream.Close();
                stream.Dispose();
                stream = null;
            }
        }
    }
}
