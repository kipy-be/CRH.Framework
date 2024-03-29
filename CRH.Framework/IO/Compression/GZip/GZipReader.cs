﻿using CRH.Framework.Common;
using System;
using System.IO;
using System.IO.Compression;

namespace CRH.Framework.IO.Compression
{
    public class GZipReader : GZip
    {
        public GZipReader()
            : base()
        { }

        /// <summary>
        /// Decompress streamIn into streamOut
        /// </summary>
        /// <param name="streamIn">The compressed stream</param>
        /// <param name="streamOut">The stream to write uncompressed data into</param>
        public void Decompress(Stream streamIn, Stream streamOut)
        {
            try
            {
                _metas.Read(streamIn, (uint)streamIn.Length);
                streamIn.Position = _metas.DataOffset;
                using (DeflateStream dfIn = new DeflateStream(streamIn, CompressionMode.Decompress))
                {
                    dfIn.CopyTo(streamOut);
                }
            }
            catch (FrameworkException)
            {
                throw;
            }
            catch(Exception)
            {
                throw new FrameworkException("Error while decompressing gzip data : unable to decompress data");
            }
        }
    }
}
