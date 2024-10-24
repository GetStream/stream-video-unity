﻿using System;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.UI;

namespace StreamVideo.Core.StatefulModels.Tracks
{
    public class StreamVideoTrack : BaseStreamTrack<VideoStreamTrack>
    {
        internal RenderTexture TargetTexture => _targetTexture;
        
        public StreamVideoTrack(MediaStreamTrack track)
            : base(track)
        {
        }

        //StreamTodo: can we remove Unity dependency? 
        public void SetRenderTarget(RawImage targetImage)
        {
            if (targetImage == null)
            {
                throw new ArgumentNullException(nameof(targetImage));
            }

            _targetImage = targetImage;
        }

        internal override void Update()
        {
            base.Update();

            if (_targetImage == null || Track == null || Track.Texture == null)
            {
                return;
            }

            var source = Track.Texture;

            if (_targetTexture == null)
            {
                _targetTexture = new RenderTexture(source.width, source.height, 0, RenderTextureFormat.Default);
                _targetImage.texture = _targetTexture;
            }

            var sizeRatio = (float)source.width / source.height;

            var sizeChanged = source.width != _targetTexture.width || source.height != _targetTexture.height;
            if (sizeChanged)
            {
#if STREAM_DEBUG_ENABLED
                Debug.LogWarning(
                    $"Sized changed from {_targetTexture.width}:{_targetTexture.height} to {source.width}:{source.height}");
#endif

                _targetTexture.Release();
                _targetTexture.width = source.width;
                _targetTexture.height = source.height;
                _targetTexture.Create();
            }

            //StreamTodo: debug this size, it can get to negative values
            var rect = _targetImage.GetComponent<RectTransform>();
            var current = rect.sizeDelta;
            rect.sizeDelta = new Vector2(current.x, current.x * (1 / sizeRatio));

            //StreamTodo: PERFORMANCE investigate if copying texture is really necessary. Perhaps we can just use the texture from the VideoStreamTrack. Test cross-platform

            //StreamTodo: use CopyTexture if available on this GPU
            Graphics.Blit(source, _targetTexture);
            _targetTexture.IncrementUpdateCount();
        }

        private RenderTexture _targetTexture;
        private RawImage _targetImage;
    }
}