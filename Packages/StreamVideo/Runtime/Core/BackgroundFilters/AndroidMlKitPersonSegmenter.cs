using System;
using StreamVideo.Libs.Logs;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Rendering;
#endif
using Object = UnityEngine.Object;

namespace StreamVideo.Core.BackgroundFilters
{
    /// <summary>
    /// Android ML Kit selfie segmenter. Async process, last-mask reuse, downscaled input via AsyncGPUReadback.
    /// Does not block <c>OnUpdate</c> and does not ReadPixels the publish texture.
    /// Input is scaled so the short side is <see cref="MinMaskInputSize"/> (ML Kit's 256px floor) while
    /// keeping the camera aspect. Rotation is not applied; mask and composite stay in WebCamTexture space.
    /// </summary>
    internal sealed class AndroidMlKitPersonSegmenter : IPersonSegmenter
    {
        public const int MinMaskInputSize = 256;

        public static bool TryCreate(ILogs logs, out AndroidMlKitPersonSegmenter segmenter)
        {
            segmenter = null;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                var native = CreateNative();
                if (native == null)
                {
                    logs?.Warning("Background filter: ML Kit is not available on this device.");
                    return false;
                }

                if (!native.Call<bool>("isSupported"))
                {
                    native.Dispose();
                    logs?.Warning("Background filter: ML Kit selfie segmentation is not supported.");
                    return false;
                }

                if (!native.Call<bool>("create"))
                {
                    native.Dispose();
                    logs?.Warning("Background filter: failed to create the ML Kit segmenter.");
                    return false;
                }

                segmenter = new AndroidMlKitPersonSegmenter(logs, native);
                return true;
            }
            catch (Exception e)
            {
                logs?.Warning("Background filter: ML Kit init failed: " + e.Message);
                return false;
            }
#else
            return false;
#endif
        }

        public bool IsSupported => true;

        public bool HasMask => _maskTexture != null && _hasMask;

        public Texture MaskTexture => _maskTexture;

        public void RequestSegmentation(Texture source)
        {
            if (_paused || source == null || _readbackInFlight)
            {
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            if (_native == null || _native.Call<bool>("isBusy"))
            {
                return;
            }

            EnsureDownscaleRt(source);
            Graphics.Blit(source, _downscaleRt);
            _lastSource = source;
            LogSubmitOrientation(source);

            if (SystemInfo.supportsAsyncGPUReadback)
            {
                _readbackInFlight = true;
                AsyncGPUReadback.Request(_downscaleRt, 0, TextureFormat.RGBA32, OnReadback);
                return;
            }

            // Last-resort: downscaled input only (never the publish RT).
            ReadbackSynchronouslyAndProcess();
#endif
        }

        public void Pause()
        {
            _paused = true;
        }

        public void Resume()
        {
            _paused = false;
        }

        public void Dispose()
        {
            _paused = true;
            CameraOrientationDebug.Flush(_logs);

            if (_maskTexture != null)
            {
                Object.Destroy(_maskTexture);
                _maskTexture = null;
            }

            if (_downscaleRt != null)
            {
                if (RenderTexture.active == _downscaleRt)
                {
                    RenderTexture.active = null;
                }

                _downscaleRt.Release();
                Object.Destroy(_downscaleRt);
                _downscaleRt = null;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            if (_native != null)
            {
                try
                {
                    _native.Call("destroy");
                }
                catch (Exception e)
                {
                    _logs?.Warning("Background filter: ML Kit destroy failed: " + e.Message);
                }

                _native.Dispose();
                _native = null;
            }
#endif
        }

        internal void PumpPendingMask()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_native == null || _paused)
            {
                return;
            }

            var width = _native.Call<int>("getMaskWidth");
            var height = _native.Call<int>("getMaskHeight");
            var maskBytes = _native.Call<sbyte[]>("takeMaskIfNew");
            if (maskBytes == null || width <= 0 || height <= 0)
            {
                return;
            }

            UploadMask(ToByteArray(maskBytes), width, height);
#endif
        }

        private const string JavaClass = "io.getstream.unitybackgroundfilters.UnityMlKitPersonSegmenter";

        private readonly ILogs _logs;
        private bool _paused;
        private bool _hasMask;
        private bool _readbackInFlight;
        private Texture2D _maskTexture;
        private RenderTexture _downscaleRt;
        private Texture _lastSource;
#if UNITY_ANDROID && !UNITY_EDITOR
        private Texture2D _syncReadbackTexture;
        private sbyte[] _rgbaSbytes;
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject _native;
#endif

        private AndroidMlKitPersonSegmenter(ILogs logs
#if UNITY_ANDROID && !UNITY_EDITOR
            , AndroidJavaObject native
#endif
        )
        {
            _logs = logs;
#if UNITY_ANDROID && !UNITY_EDITOR
            _native = native;
#if STREAM_DEBUG_ENABLED
            try
            {
                _native.Call("setDebugLogs", true);
            }
            catch (Exception e)
            {
                _logs?.Warning("Background filter: failed to enable ML Kit debug logs: " + e.Message);
            }
#endif
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject CreateNative()
        {
            return new AndroidJavaObject(JavaClass);
        }

        private void OnReadback(AsyncGPUReadbackRequest request)
        {
            _readbackInFlight = false;
            if (_paused || _native == null)
            {
                return;
            }

            if (request.hasError)
            {
                _logs?.Warning("Background filter: mask input readback failed.");
                return;
            }

            var data = request.GetData<byte>();
            SubmitRgba(data.ToArray(), _downscaleRt.width, _downscaleRt.height);
        }

        private void ReadbackSynchronouslyAndProcess()
        {
            var width = _downscaleRt.width;
            var height = _downscaleRt.height;
            if (_syncReadbackTexture == null || _syncReadbackTexture.width != width ||
                _syncReadbackTexture.height != height)
            {
                if (_syncReadbackTexture != null)
                {
                    Object.Destroy(_syncReadbackTexture);
                }

                _syncReadbackTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            }

            var prev = RenderTexture.active;
            RenderTexture.active = _downscaleRt;
            _syncReadbackTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
            _syncReadbackTexture.Apply(false, false);
            RenderTexture.active = prev;

            SubmitRgba(_syncReadbackTexture.GetRawTextureData(), width, height);
        }

        private void SubmitRgba(byte[] rgba, int width, int height)
        {
            if (rgba == null || _native == null)
            {
                return;
            }

            var webcam = _lastSource as WebCamTexture;
            var webcamRot = webcam != null ? webcam.videoRotationAngle : -1;
            CameraOrientationDebug.Log(_logs, "mlkit.submit",
                "rgba=" + width + "x" + height + " bytes=" + rgba.Length
                + " mlkitRotationDegrees=0 (webcam space)"
                + " webcamRot=" + webcamRot
                + " mirrored=" + (webcam != null && webcam.videoVerticallyMirrored)
                + " gfx=" + SystemInfo.graphicsDeviceType
                + " asyncReadback=" + SystemInfo.supportsAsyncGPUReadback);
            _native.Call("processAsync", ToSByteArray(rgba), width, height);
        }

        private void UploadMask(byte[] mask, int width, int height)
        {
            if (mask == null || mask.Length < width * height)
            {
                return;
            }

            if (_maskTexture == null || _maskTexture.width != width || _maskTexture.height != height)
            {
                if (_maskTexture != null)
                {
                    Object.Destroy(_maskTexture);
                }

                _maskTexture = new Texture2D(width, height, TextureFormat.R8, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "StreamMlKitPersonMask",
                };
            }

            _maskTexture.SetPixelData(mask, 0);
            _maskTexture.Apply(false, false);
            _hasMask = true;

#if STREAM_DEBUG_ENABLED
            var hits = 0;
            var needed = width * height;
            for (var i = 0; i < needed; i++)
            {
                if (mask[i] > 128)
                {
                    hits++;
                }
            }

            var inputW = _downscaleRt != null ? _downscaleRt.width : 0;
            var inputH = _downscaleRt != null ? _downscaleRt.height : 0;
            CameraOrientationDebug.RecordMask(_logs,
                "mask=" + width + "x" + height
                + " input=" + inputW + "x" + inputH
                + " aspectMatch=" + (width * inputH == height * inputW)
                + " | " + CameraOrientationDebug.DescribeWebCam(_lastSource as WebCamTexture),
                hits / (float)Mathf.Max(1, needed));
#endif
        }

        private static byte[] ToByteArray(sbyte[] source)
        {
            if (source == null)
            {
                return null;
            }

            var dest = new byte[source.Length];
            Buffer.BlockCopy(source, 0, dest, 0, source.Length);
            return dest;
        }

        private sbyte[] ToSByteArray(byte[] source)
        {
            if (source == null)
            {
                return null;
            }

            if (_rgbaSbytes == null || _rgbaSbytes.Length < source.Length)
            {
                _rgbaSbytes = new sbyte[source.Length];
            }

            Buffer.BlockCopy(source, 0, _rgbaSbytes, 0, source.Length);
            return _rgbaSbytes;
        }

        private void EnsureDownscaleRt(Texture source)
        {
            GetMaskInputSize(source.width, source.height, out var width, out var height);
            if (_downscaleRt != null && _downscaleRt.width == width && _downscaleRt.height == height)
            {
                return;
            }

            if (_downscaleRt != null)
            {
                _downscaleRt.Release();
                Object.Destroy(_downscaleRt);
            }

            _downscaleRt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = "StreamBgFilterMaskInput",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            _downscaleRt.Create();
        }

        private void LogSubmitOrientation(Texture source)
        {
            var webcam = source as WebCamTexture;
            CameraOrientationDebug.Log(_logs, "mlkit.downscale",
                CameraOrientationDebug.DescribeTexture("source", source)
                + " | " + CameraOrientationDebug.DescribeTexture("downscale", _downscaleRt)
                + " | " + (webcam != null
                    ? CameraOrientationDebug.DescribeWebCam(webcam)
                    : "sourceIsWebCam=false")
                + " blit=Graphics.Blit(source, downscale) no pixel rotation");
        }

        private static void GetMaskInputSize(int sourceWidth, int sourceHeight, out int width, out int height)
        {
            sourceWidth = Mathf.Max(2, sourceWidth);
            sourceHeight = Mathf.Max(2, sourceHeight);
            var shortSide = Mathf.Min(sourceWidth, sourceHeight);
            if (shortSide <= MinMaskInputSize)
            {
                width = sourceWidth;
                height = sourceHeight;
                return;
            }

            var scale = MinMaskInputSize / (float)shortSide;
            width = Mathf.Max(2, Mathf.RoundToInt(sourceWidth * scale));
            height = Mathf.Max(2, Mathf.RoundToInt(sourceHeight * scale));
        }
#endif
    }
}
