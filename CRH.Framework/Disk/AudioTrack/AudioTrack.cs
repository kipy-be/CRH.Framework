using System.IO;

namespace CRH.Framework.Disk.AudioTrack
{
    /// <summary>
    /// AudioTrack abstract base
    /// </summary>
    public abstract class AudioTrack : Track
    {
        /// <summary>
        /// AudioTrack (abstract)
        /// </summary>
        /// <param name="fileStream">The ISO stream</param>
        /// <param name="trackNumber">The track number</param>
        internal AudioTrack(FileStream fileStream, int trackNumber)
            : base(fileStream, trackNumber, TrackType.AUDIO)
        {
            _sectorSize = 2352;
        }
    }
}
