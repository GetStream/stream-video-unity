namespace StreamVideo.ExampleProject
{
    /// <summary>
    /// Debug helper for simulcast testing. Subscribers do not pick a layer directly — they request
    /// a video dimension and the SFU maps it to q/h/f. These presets target each layer reliably.
    /// </summary>
    public enum ForcedSimulcastConsumeLayer
    {
        Auto = 0,
        Full = 1,
        Half = 2,
        Quarter = 3,
    }
}
