namespace StreamVideo.Core
{
    /// <summary>
    /// Local pre-encode video effect applied to the published camera texture.
    /// Disable with <see cref="StatefulModels.IStreamCall.SetBackgroundFilter"/> passing <see langword="null"/>.
    /// </summary>
    public abstract class BackgroundFilter
    {
        /// <summary>
        /// Blur the background while keeping the person sharp.
        /// </summary>
        public static BackgroundFilter Blur(BlurIntensity intensity = BlurIntensity.Heavy)
            => new BlurBackgroundFilter(intensity);

        /// <summary>
        /// Compositor debug view: 0 = normal, 1 = person mask, 2 = green person / red background overlay.
        /// Sample More Options cycles this after Heavy. Leave at 0 in product builds.
        /// </summary>
        public static int DebugView { get; set; }

        /// <summary>
        /// Blur strength when this filter is a blur effect.
        /// </summary>
        public BlurIntensity Intensity { get; }

        internal BackgroundFilterKind Kind { get; }

        private protected BackgroundFilter(BackgroundFilterKind kind, BlurIntensity intensity)
        {
            Kind = kind;
            Intensity = intensity;
        }
    }

    internal enum BackgroundFilterKind
    {
        Blur = 0,
    }

    internal sealed class BlurBackgroundFilter : BackgroundFilter
    {
        internal BlurBackgroundFilter(BlurIntensity intensity)
            : base(BackgroundFilterKind.Blur, intensity)
        {
        }
    }
}
