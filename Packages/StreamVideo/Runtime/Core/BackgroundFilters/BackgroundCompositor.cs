using UnityEngine;
using Object = UnityEngine.Object;

namespace StreamVideo.Core.BackgroundFilters
{
    /// <summary>
    /// GPU composite: temporal mask EMA, downscaled separable blur, mask blend.
    /// Light/Medium/Heavy all blur at half-res. Person pixels are excluded from the
    /// blur kernel so skin/hair does not bleed into the background.
    /// First pass is a default blit so Android OES WebCamTextures become a regular RT.
    /// </summary>
    internal sealed class BackgroundCompositor : IVideoFilter
    {
        public const float DefaultSmoothing = 0.9f;
        public const float DefaultSmoothstepMin = 0.45f;
        public const float DefaultSmoothstepMax = 0.8f;
        public const float DefaultMaskExpandPixels = 3f;

        public bool IsReady => _blendMaterial != null;

        public void SetMask(Texture mask)
        {
            _mask = mask;
        }

        public void SetIntensity(BlurIntensity intensity)
        {
            _intensity = intensity;
        }

        public void Apply(Texture source, RenderTexture destination)
        {
            if (source == null || destination == null)
            {
                return;
            }

            EnsureResources(destination.width, destination.height, destination.format);

            if (!IsReady)
            {
                Graphics.Blit(source, destination);
                return;
            }

            Graphics.Blit(source, _sourceRt);

            if (_mask != null)
            {
                _temporalMaterial.SetTexture(PrevMaskId, _prevMaskRt);
                _temporalMaterial.SetFloat(SmoothingId, DefaultSmoothing);
                Graphics.Blit(_mask, _maskRt, _temporalMaterial);
                Graphics.Blit(_maskRt, _prevMaskRt);
            }
            else
            {
                Graphics.Blit(Texture2D.blackTexture, _maskRt);
                Graphics.Blit(Texture2D.blackTexture, _prevMaskRt);
            }

            Graphics.Blit(_sourceRt, _blurRt);

            var iterations = GetBlurIterations(_intensity);
            var spread = GetBlurSpread(_intensity);
            var blurSource = _blurRt;
            var blurDest = _blurPingRt;

            _blurMaterial.SetTexture(MaskId, _maskRt);
            for (var i = 0; i < iterations; i++)
            {
                _blurMaterial.SetFloat(SpreadId, spread);
                _blurMaterial.SetVector(DirectionId, new Vector4(1f, 0f, 0f, 0f));
                Graphics.Blit(blurSource, blurDest, _blurMaterial);

                _blurMaterial.SetVector(DirectionId, new Vector4(0f, 1f, 0f, 0f));
                Graphics.Blit(blurDest, blurSource, _blurMaterial);
            }

            _blendMaterial.SetTexture(BlurredId, blurSource);
            _blendMaterial.SetTexture(MaskId, _maskRt);
            _blendMaterial.SetFloat(SmoothMinId, DefaultSmoothstepMin);
            _blendMaterial.SetFloat(SmoothMaxId, DefaultSmoothstepMax);
            _blendMaterial.SetFloat(ExpandPixelsId, DefaultMaskExpandPixels);
            _blendMaterial.SetFloat(DebugModeId, BackgroundFilter.DebugView);
            Graphics.Blit(_sourceRt, destination, _blendMaterial);
        }

        public void Release()
        {
            ReleaseRt(ref _sourceRt);
            ReleaseRt(ref _blurRt);
            ReleaseRt(ref _blurPingRt);
            ReleaseRt(ref _maskRt);
            ReleaseRt(ref _prevMaskRt);
            DestroyMaterial(ref _temporalMaterial);
            DestroyMaterial(ref _blurMaterial);
            DestroyMaterial(ref _blendMaterial);
            _mask = null;
        }

        private static readonly int PrevMaskId = Shader.PropertyToID("_PrevMask");
        private static readonly int SmoothingId = Shader.PropertyToID("_Smoothing");
        private static readonly int DirectionId = Shader.PropertyToID("_Direction");
        private static readonly int SpreadId = Shader.PropertyToID("_Spread");
        private static readonly int BlurredId = Shader.PropertyToID("_Blurred");
        private static readonly int MaskId = Shader.PropertyToID("_Mask");
        private static readonly int SmoothMinId = Shader.PropertyToID("_SmoothMin");
        private static readonly int SmoothMaxId = Shader.PropertyToID("_SmoothMax");
        private static readonly int ExpandPixelsId = Shader.PropertyToID("_ExpandPixels");
        private static readonly int DebugModeId = Shader.PropertyToID("_DebugMode");

        private Texture _mask;
        private BlurIntensity _intensity = BlurIntensity.Heavy;

        private RenderTexture _sourceRt;
        private RenderTexture _blurRt;
        private RenderTexture _blurPingRt;
        private RenderTexture _maskRt;
        private RenderTexture _prevMaskRt;

        private Material _temporalMaterial;
        private Material _blurMaterial;
        private Material _blendMaterial;

        private static int GetBlurIterations(BlurIntensity intensity)
        {
            switch (intensity)
            {
                case BlurIntensity.Light:
                    return 1;
                case BlurIntensity.Heavy:
                    return 4;
                default:
                    return 2;
            }
        }

        private static float GetBlurSpread(BlurIntensity intensity)
        {
            switch (intensity)
            {
                case BlurIntensity.Light:
                    return 1f;
                case BlurIntensity.Heavy:
                    return 2f;
                default:
                    return 1.25f;
            }
        }

        private void EnsureResources(int width, int height, RenderTextureFormat format)
        {
            EnsureMaterials();

            var blurW = Mathf.Max(2, width / 2);
            var blurH = Mathf.Max(2, height / 2);

            _sourceRt = EnsureColorRt(_sourceRt, width, height, format, "StreamBgFilterSource");
            _blurRt = EnsureColorRt(_blurRt, blurW, blurH, format, "StreamBgFilterBlur");
            _blurPingRt = EnsureColorRt(_blurPingRt, blurW, blurH, format, "StreamBgFilterBlurPing");
            _maskRt = EnsureMaskRt(_maskRt, width, height, "StreamBgFilterMask");
            _prevMaskRt = EnsureMaskRt(_prevMaskRt, width, height, "StreamBgFilterPrevMask");
        }

        private void EnsureMaterials()
        {
            if (_temporalMaterial == null)
            {
                _temporalMaterial = CreateMaterial("Hidden/StreamVideo/BackgroundMaskTemporal",
                    "StreamVideo/BackgroundFilters/BackgroundMaskTemporal");
            }

            if (_blurMaterial == null)
            {
                _blurMaterial = CreateMaterial("Hidden/StreamVideo/BackgroundSeparableBlur",
                    "StreamVideo/BackgroundFilters/BackgroundSeparableBlur");
            }

            if (_blendMaterial == null)
            {
                _blendMaterial = CreateMaterial("Hidden/StreamVideo/BackgroundMaskBlend",
                    "StreamVideo/BackgroundFilters/BackgroundMaskBlend");
            }
        }

        private static Material CreateMaterial(string shaderName, string resourcesPath)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                shader = Resources.Load<Shader>(resourcesPath);
            }

            if (shader == null)
            {
                return null;
            }

            return new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        private static RenderTexture EnsureColorRt(RenderTexture current, int width, int height,
            RenderTextureFormat format, string name)
        {
            if (current != null && current.width == width && current.height == height)
            {
                return current;
            }

            ReleaseRt(ref current);

            var rt = new RenderTexture(width, height, 0, format)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false,
            };
            rt.Create();
            return rt;
        }

        private static RenderTexture EnsureMaskRt(RenderTexture current, int width, int height, string name)
        {
            if (current != null && current.width == width && current.height == height)
            {
                return current;
            }

            ReleaseRt(ref current);

            // Linear so Unity requests R8_UNorm. Default R8 is sRGB, which GLES often rejects.
            var rt = new RenderTexture(width, height, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            rt.Create();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = prev;
            return rt;
        }

        private static void ReleaseRt(ref RenderTexture rt)
        {
            if (rt == null)
            {
                return;
            }

            if (RenderTexture.active == rt)
            {
                RenderTexture.active = null;
            }

            rt.Release();
            Object.Destroy(rt);
            rt = null;
        }

        private static void DestroyMaterial(ref Material material)
        {
            if (material == null)
            {
                return;
            }

            Object.Destroy(material);
            material = null;
        }
    }
}
