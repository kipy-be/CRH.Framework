using System;

namespace CRH.Framework.Disk
{
    interface ITrackReader
    {}

    interface ITrackWriter
    {
        void Finalize();
        bool IsFinalized { get; }
    }
}