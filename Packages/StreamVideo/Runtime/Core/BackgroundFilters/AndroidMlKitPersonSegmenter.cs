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
    /// GPU readback is pipelined with ML Kit: a new camera frame is read while the previous
    /// <c>processAsync</c> is in flight, then submitted as soon as the segmenter is free.
    /// Does not block <c>OnUpdate</c> and does not ReadPixels the publish texture.
    /// Input is scaled so the short side is <see cref="MinMaskInputSize"/> (ML Kit's 256px floor) while
    /// keeping the camera aspect. Rotation is not applied; mask and composite stay in WebCamTexture space.
    /// <see cref="Dispose"/> is non-blocking: in-flight GPU readback and Java <c>process</c>
    /// release their own resources when they finish.
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
            if (_disposed || _paused || source == null || _readbackInFlight)
            {
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            if (_native == null)
            {
                return;
            }

            // Pipeline GPU readback with ML Kit so mask latency is max(readback, process), not the sum.
            EnsureDownscaleRt(source);
            if (_downscaleRt == null)
            {
                return;
            }

            Graphics.Blit(source, _downscaleRt);
            _lastSource = source;
            LogSubmitOrientation(source);

            if (SystemInfo.supportsAsyncGPUReadback)
            {
                _readbackInFlight = true;
                try
                {
                    AsyncGPUReadback.Request(_downscaleRt, 0, TextureFormat.RGBA32, OnReadback);
                }
                catch (Exception e)
                {
                    _readbackInFlight = false;
                    _logs?.Warning("Background filter: mask input readback request failed: " + e.Message);
                }

                return;
            }

            // Last-resort: downscaled input only (never the publish RT).
            ReadbackSynchronouslyAndProcess();
#endif
        }

        public void Pause()
        {
            _paused = true;
#if UNITY_ANDROID && !UNITY_EDITOR
            _hasPendingRgba = false;
#endif
        }

        public void Resume()
        {
            if (_disposed)
            {
                return;
            }

            _paused = false;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _paused = true;
            _lastSource = null;
            CameraOrientationDebug.Flush(_logs);

#if UNITY_ANDROID && !UNITY_EDITOR
            _hasPendingRgba = false;
            _pendingRgba = null;
            _rgbaSbytes = null;
            DestroyNative();
            DestroyTexture(ref _syncReadbackTexture);
#endif
            DestroyTexture(ref _maskTexture);

            if (!_readbackInFlight)
            {
                ReleaseDownscaleRt();
            }
        }

        internal void PumpPendingMask()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_disposed || _native == null || _paused)
            {
                return;
            }

            var width = _native.Call<int>("getMaskWidth");
            var height = _native.Call<int>("getMaskHeight");
            var maskBytes = _native.Call<sbyte[]>("takeMaskIfNew");
            if (maskBytes != null && width > 0 && height > 0)
            {
                UploadMask(ToByteArray(maskBytes), width, height);
            }

            TrySubmitPending();
#endif
        }

        private const string JavaClass = "io.getstream.unitybackgroundfilters.UnityMlKitPersonSegmenter";

        private readonly ILogs _logs;
        private bool _disposed;
        private bool _paused;
        private bool _hasMask;
        private bool _readbackInFlight;
        private Texture2D _maskTexture;
        private RenderTexture _downscaleRt;
        private Texture _lastSource;
#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject _native;
        private Texture2D _syncReadbackTexture;
        private sbyte[] _rgbaSbytes;
        private byte[] _pendingRgba;
        private int _pendingWidth;
        private int _pendingHeight;
        private bool _hasPendingRgba;
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

            try
            {
                if (_disposed || _paused || _native == null)
                {
                    return;
                }

                if (request.hasError)
                {
                    _logs?.Warning("Background filter: mask input readback failed.");
                    return;
                }

                var data = request.GetData<byte>();
                var length = data.Length;
                if (_pendingRgba == null || _pendingRgba.Length != length)
                {
                    _pendingRgba = new byte[length];
                }

                data.CopyTo(_pendingRgba);
                _pendingWidth = request.width;
                _pendingHeight = request.height;
                _hasPendingRgba = true;
                TrySubmitPending();
            }
            finally
            {
                if (_disposed)
                {
                    ReleaseDownscaleRt();
                }
            }
        }

        private void TrySubmitPending()
        {
            if (_disposed || !_hasPendingRgba || _paused || _native == null)
            {
                return;
            }

            if (_native.Call<bool>("isBusy"))
            {
                return;
            }

            SubmitRgba(_pendingRgba, _pendingWidth, _pendingHeight);
            _hasPendingRgba = false;
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
            if (_disposed || rgba == null || _native == null)
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
            if (_disposed || mask == null || mask.Length < width * height)
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
            if (_disposed)
            {
                return;
            }

            GetMaskInputSize(source.width, source.height, out var width, out var height);
            if (_downscaleRt != null && _downscaleRt.width == width && _downscaleRt.height == height)
            {
                return;
            }

            ReleaseDownscaleRt();

            _downscaleRt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = "StreamBgFilterMaskInput",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            _downscaleRt.Create();
        }

        private void DestroyNative()
        {
            if (_native == null)
            {
                return;
            }

            try
            {
                _native.Call("destroy");
            }
            catch (Exception e)
            {
                _logs?.Warning("Background filter: ML Kit destroy failed: " + e.Message);
            }

            try
            {
                _native.Dispose();
            }
            catch (Exception e)
            {
                _logs?.Warning("Background filter: ML Kit JNI dispose failed: " + e.Message);
            }

            _native = null;
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

        private void ReleaseDownscaleRt()
        {
            if (_downscaleRt == null)
            {
                return;
            }

            try
            {
                if (RenderTexture.active == _downscaleRt)
                {
                    RenderTexture.active = null;
                }

                _downscaleRt.Release();
                Object.Destroy(_downscaleRt);
            }
            catch (Exception e)
            {
                _logs?.Warning("Background filter: failed to release mask input RT: " + e.Message);
            }

            _downscaleRt = null;
        }

        private void DestroyTexture(ref Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            try
            {
                Object.Destroy(texture);
            }
            catch (Exception e)
            {
                _logs?.Warning("Background filter: failed to destroy texture: " + e.Message);
            }

            texture = null;
        }
    }
}
