using System;
using System.Collections.Generic;
using System.IO;
using CRH.Framework.Common;

namespace CRH.Framework.Disk.AudioTrack
{
    /// <summary>
    /// AudioTrack abstract base
    /// </summary>
    public abstract class AudioTrack : Track
    {

    // Constructors

        /// <summary>
        /// AudioTrack (abstract)
        /// </summary>
        /// <param name="fileStream">The iso file stream</param>
        /// <param name="trackNumber">The track number</param>
        internal AudioTrack(FileStream fileStream, int trackNumber)
            : base(fileStream, trackNumber, TrackType.AUDIO)
        {
            m_sectorSize = 2352;
        }
    }
}
