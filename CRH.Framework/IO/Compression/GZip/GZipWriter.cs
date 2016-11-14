using System;
using System.IO;
using System.IO.Compression;
using CRH.Framework.Common;
using CRH.Framework.IO.Hash;

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
                _metas.WriteHeader(streamOut);
                _metas.DataRealSize = (uint)streamIn.Length;
                using(DeflateStream dfOut = new DeflateStream(streamOut, CompressionMode.Compress, true))
                {
                    streamIn.CopyTo(dfOut);
                }
                streamIn.Position = 0;
                _metas.Crc32 = Crc32.Compute(streamIn);
                _metas.WriteFooter(streamOut);
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
