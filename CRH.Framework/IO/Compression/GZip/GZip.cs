namespace CRH.Framework.IO.Compression
{
    public abstract class GZip
    {
        protected GZipMetas _metas;

        public GZip()
        {
            _metas = new GZipMetas();
        }

        public GZipMetas Metas
        {
            get { return _metas; }
        }
    }
}
