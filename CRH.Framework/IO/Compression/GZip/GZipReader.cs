using System;
using System.IO;
using System.IO.Compression;
using CRH.Framework.Common;

namespace CRH.Framework.IO.Compression
{
    public class GZipReader : GZip
    {
    // Constructors

        public GZipReader()
            : base()
        { }

    // Methods

        /// <summary>
        /// Decompress streamIn into streamOut
        /// </summary>
        /// <param name="streamIn">The compressed stream</param>
        /// <param name="streamOut">The stream to write uncompressed data into</param>
        public void Decompress(Stream streamIn, Stream streamOut)
        {
            try
            {
                m_metas.Read(streamIn, (uint)streamIn.Length);
                streamIn.Position = m_metas.DataOffset;
                using (DeflateStream dfIn = new DeflateStream(streamIn, CompressionMode.Decompress))
                {
                    dfIn.CopyTo(streamOut);
                }
            }
            catch (FrameworkException ex)
            {
                throw ex;
            }
            catch(Exception)
            {
                throw new FrameworkException("Error while decompressing gzip data : unable to decompress data");
            }
        }
    }
}
