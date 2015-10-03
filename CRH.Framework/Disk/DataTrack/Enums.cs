using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CRH.Framework.Disk
{
    /// <summary>
    /// DataTrack's file system
    /// </summary>
    public enum DataTrackSystem
    {
        ISO9660,
        ISO9660_UDF
    }

    /// <summary>
    /// DataTrack's mode
    /// </summary>
    public enum DataTrackMode
    {
        RAW = 0,
        MODE1 = 1,
        MODE2 = 2,
        MODE2_XA = 3
    }

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

    /// <summary>
    /// Path table type (little or big endian)
    /// </summary>
    internal enum PathTableType
    {
        LE = 1,
        BE = 2
    }
}
