namespace CRH.Framework.Disk
{
    interface ITrackWriter
    {
        void FinalizeTrack();
        bool IsFinalized { get; }
    }
}
