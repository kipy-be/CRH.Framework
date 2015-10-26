using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CRH.Framework.IO.Compression
{
    public abstract class GZip
    {
        protected GZipMetas m_metas;

    // Constructors

        public GZip()
        {
            m_metas = new GZipMetas();
        }

    // Accessors

        public GZipMetas Metas
        {
            get { return m_metas; }
        }
    }
}
