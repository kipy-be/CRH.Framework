namespace CRH.Framework.Disk.DataTrack
{
    /// <summary>
    /// Set Terminator Volume Descriptor
    /// </summary>
    public sealed class SetTerminatorVolumeDescriptor : VolumeDescriptor
    {
        // Constructors

        public SetTerminatorVolumeDescriptor()
            : base(VolumeDescriptorType.SET_TERMINATOR, 1)
        { }
    }
}