using System;
using System.IO;
using System.IO.Compression;
using CRH.Framework.Common;
using CRH.Framework.IO.Checksum;

namespace CRH.Framework.IO.Compression
{
    public class GZipWriter : GZip
    {
    // Constructors

        public GZipWriter()
            : base()
        { }

    // Methods

        /// <summary>
        /// Compress streamIn into streamOut
        /// </summary>
        /// <param name="streamIn">The stream to compress</param>
        /// <param name="streamOut">The stream to write compressed data into</param>
        public void Compress(Stream streamIn, Stream streamOut)
        {
            try
            {
                m_metas.WriteHeader(streamOut);
                m_metas.DataRealSize = (uint)streamIn.Length;
                using(DeflateStream dfOut = new DeflateStream(streamOut, CompressionMode.Compress, true))
                {
                    streamIn.CopyTo(dfOut);
                }
                streamIn.Position = 0;
                m_metas.Crc32 = Crc32.Compute(streamIn);
                m_metas.WriteFooter(streamOut);
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
