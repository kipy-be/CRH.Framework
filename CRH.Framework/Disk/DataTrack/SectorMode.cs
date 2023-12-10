namespace CRH.Framework.Disk.DataTrack
{
    /// <summary>
    /// Sector's mode
    /// </summary>
    public enum SectorMode
    {
        RAW = -1,
        MODE0 = 0,
        MODE1 = 1,
        MODE2 = 2,

        /// <summary>
        /// Aka MODE2_FORM1
        /// </summary>
        XA_FORM1 = 21,

        /// <summary>
        /// Aka MODE2_FORM2
        /// </summary
        XA_FORM2 = 22
    }
}
