﻿using System;
using System.Threading.Tasks;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Libs.Auth;
using UnityEngine;

namespace StreamVideo.Core
{
    public interface IStreamVideoClient : IDisposable
    {
        event CallHandler CallStarted;
        event CallHandler CallEnded;
        IStreamCall ActiveCall { get; }

        Task ConnectUserAsync(AuthCredentials credentials);

        void Update();

        Task DisconnectAsync();

        Task<IStreamCall> JoinCallAsync(StreamCallType callType, string callId, bool create, bool ring,
            bool notify);

        void SetAudioInputSource(AudioSource audioSource);

        void SetCameraInputSource(WebCamTexture webCamTexture);

        void SetCameraInputSource(Camera sceneCamera);
    }
}