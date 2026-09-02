using UnityEngine;
using Object = UnityEngine.Object;

namespace StreamVideo.Core.BackgroundFilters
{
    /// <summary>
    /// GPU composite: temporal mask EMA, half-res separable blur, mask blend.
    /// First pass is a default blit so Android OES WebCamTextures become a regular RT.
    /// </summary>
    internal sealed class BackgroundCompositor : IVideoFilter
    {
        public const float DefaultSmoothing = 0.8f;
        public const float DefaultSmoothstepMin = 0.6f;
        public const float DefaultSmoothstepMax = 0.9f;

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
            }

            Graphics.Blit(_sourceRt, _halfRt);

            var iterations = GetBlurIterations(_intensity);
            var spread = GetBlurSpread(_intensity);
            var blurSource = _halfRt;
            var blurDest = _blurPingRt;

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
            Graphics.Blit(_sourceRt, destination, _blendMaterial);
        }

        public void Release()
        {
            ReleaseRt(ref _sourceRt);
            ReleaseRt(ref _halfRt);
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

        private Texture _mask;
        private BlurIntensity _intensity = BlurIntensity.Medium;

        private RenderTexture _sourceRt;
        private RenderTexture _halfRt;
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
                    return 3;
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
                    return 1.6f;
                default:
                    return 1.25f;
            }
        }

        private void EnsureResources(int width, int height, RenderTextureFormat format)
        {
            EnsureMaterials();

            var halfW = Mathf.Max(2, width / 2);
            var halfH = Mathf.Max(2, height / 2);

            _sourceRt = EnsureColorRt(_sourceRt, width, height, format, "StreamBgFilterSource");
            _halfRt = EnsureColorRt(_halfRt, halfW, halfH, format, "StreamBgFilterHalf");
            _blurPingRt = EnsureColorRt(_blurPingRt, halfW, halfH, format, "StreamBgFilterBlurPing");
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

            var rt = new RenderTexture(width, height, 0, RenderTextureFormat.R8)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            rt.Create();
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
