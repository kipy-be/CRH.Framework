using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CRH.Framework.IO.Compression
{
    public abstract class GZip
    {
        protected GZipMetas _metas;

    // Constructors

        public GZip()
        {
            _metas = new GZipMetas();
        }

    // Accessors

        public GZipMetas Metas
        {
            get { return _metas; }
        }
    }
}
