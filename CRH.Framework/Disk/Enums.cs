using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CRH.Framework.Disk
{
    /// <summary>
    /// Track's type
    /// </summary>
    public enum TrackType
    {
        DATA  = 1,
        AUDIO = 2
    }

    /// <summary>
    /// Disk's file system
    /// </summary>
    public enum DiskFileSystem
    {
        ISO9660,
        ISO9660_UDF
    }

    /// <summary>
    /// Platform (console)
    /// </summary>
    public enum Platform
    {
        PS1,
        PSP,
        PS2
    }
}