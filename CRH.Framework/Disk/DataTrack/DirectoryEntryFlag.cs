namespace CRH.Framework.Disk.DataTrack
{
    /// <summary>
    /// Directory entry flag mask
    /// </summary>
    internal enum DirectoryEntryFlag
    {
        HIDDEN        = 1,
        DIRECTORY     = 1 << 1,
        ASSOCIATED    = 1 << 2,
        FORMAT_IN_EXT = 1 << 3,
        PERMS_IN_EXT  = 1 << 4,
        NOT_FINAL     = 1 << 7
    }
}