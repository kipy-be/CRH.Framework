namespace CRH.Framework.Disk.DataTrack
{
    internal enum XaSubModeFlag : byte
    {
        EOR           = 1,
        VIDEO         = 1 << 1,
        AUDIO         = 1 << 2,
        DATA          = 1 << 3,
        TRIGGER_ON    = 1 << 4,
        FORM2         = 1 << 5,
        REAL_TIME     = 1 << 6,
        EOF           = 1 << 7
    }
}
