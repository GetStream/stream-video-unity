using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Core.Utils;
using StreamVideo.Libs.Logs;
using UnityEngine;
using Object = UnityEngine.Object;

namespace StreamVideo.Core.DeviceManagers
{
    internal class StreamAudioDeviceManager : DeviceManagerBase<MicrophoneDeviceInfo>, IStreamAudioDeviceManager
    {
        //StreamTodo: user can add/remove devices, we might want to expose DeviceAdded, DeviceRemoved events
        public override IEnumerable<MicrophoneDeviceInfo> EnumerateDevices()
        {
            foreach (var device in Microphone.devices)
            {
                yield return new MicrophoneDeviceInfo(device);
            }
        }

        protected override async Task<bool> OnTestDeviceAsync(MicrophoneDeviceInfo device, int msTimeout)
        {
            const int sampleRate = 44100;
            var maxRecordingTime = (int)Math.Ceiling(msTimeout / 1000f);

            var clip = Microphone.Start(device.Name, true, maxRecordingTime, sampleRate);
            if (clip == null)
            {
                return false;
            }

            //StreamTodo: check in loop and exit early if device is working already
            await Task.Delay(msTimeout);

            //StreamTodo: should we check Microphone.IsRecording? Also some sources add this after Mic.Start() while (!(Microphone.GetPosition(null) > 0)) { }

            var data = new float[clip.samples * clip.channels];
            clip.GetData(data, 0);
            var hasData = false;
            foreach (var sample in data)
            {
                if (sample != 0f)
                {
                    hasData = true;
                    break;
                }
            }

            return hasData;
        }

        /// <summary>
        /// Select microphone device to capture audio input. Available microphone devices are listed in <see cref="EnumerateDevices"/>.
        /// You can check the currently selected audio device with <see cref="DeviceManagerBase{TDeviceInfo}.SelectedDevice"/>, and
        /// get notified when the selected device changes by subscribing to <see cref="DeviceManagerBase{TDeviceInfo}.SelectedDeviceChanged"/>.
        /// </summary>
        /// <param name="device"></param>
        /// <exception cref="ArgumentException">Thrown when the provided device has an invalid name</exception>
        public void SelectDevice(MicrophoneDeviceInfo device, bool enable)
        {
            if (!device.IsValid)
            {
                throw new ArgumentException($"{nameof(device)} argument is not valid. The device name is empty.");
            }

            TryStopRecording(device);

            SelectedDevice = device;

#if STREAM_DEBUG_ENABLED
            Logs.Info($"Changed microphone device to: {SelectedDevice}");
#endif

            //StreamTodo: in some cases starting the mic recording before the call was causing the recorded audio being played in speakers
            //I think the reason was that AudioSource was being captured by an AudioListener but once I've joined the call, this disappeared
            //Check if we can have this AudioSource to be ignored by AudioListener's or otherwise mute it when there is not active call session

            SetEnabled(enable);
        }

        //StreamTodo: https://docs.unity3d.com/ScriptReference/AudioSource-ignoreListenerPause.html perhaps this should be enabled so that AudioListener doesn't affect recorded audio

        internal StreamAudioDeviceManager(RtcSession rtcSession, IInternalStreamVideoClient client, ILogs logs)
            : base(rtcSession, client, logs)
        {
        }

        protected override void OnSetEnabled(bool isEnabled)
        {
            if (isEnabled && SelectedDevice.IsValid)
            {
                TryStopRecording(SelectedDevice);
                
                var targetAudioSource = GetOrCreateTargetAudioSource();

                // StreamTodo: use Microphone.GetDeviceCaps to get min/max frequency -> validate it and pass to Microphone.Start

                targetAudioSource.clip
                    = Microphone.Start(SelectedDevice.Name, loop: true, lengthSec: 10, AudioSettings.outputSampleRate);
                targetAudioSource.loop = true;

                using (new DebugStopwatchScope(Logs, "Waiting for microphone to start recording"))
                {
                    while (!(Microphone.GetPosition(SelectedDevice.Name) > 0))
                    {
                        // StreamTodo: add timeout. Otherwise might hang application
                    }
                }

                targetAudioSource.Play();
            }

            if (!isEnabled)
            {
                TryStopRecording(SelectedDevice);
            }

            RtcSession.TrySetAudioTrackEnabled(isEnabled);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            TrySyncMicrophoneAudioSourceReadPosWithMicrophoneWritePos();
        }

        protected override void OnDisposing()
        {
            TryStopRecording(SelectedDevice);

            if (_targetAudioSourceContainer != null)
            {
                Object.Destroy(_targetAudioSourceContainer);
            }

            base.OnDisposing();
        }

        //StreamTodo: wrap all operations on the Microphone devices + monitor for devices list changes
        //We could also allow to smart pick device -> sample each device and check which of them are actually gathering any input

        private AudioSource _targetAudioSource;
        private GameObject _targetAudioSourceContainer;

        private AudioSource GetOrCreateTargetAudioSource()
        {
            if (_targetAudioSource != null)
            {
                return _targetAudioSource;
            }

            _targetAudioSourceContainer = new GameObject
            {
                name = $"[Stream][{nameof(StreamAudioDeviceManager)}] Microphone Buffer",
#if STREAM_DEBUG_ENABLED
                hideFlags = HideFlags.DontSave
#else
                hideFlags = HideFlags.HideInHierarchy | HideFlags.DontSave
#endif
            };

            _targetAudioSource = _targetAudioSourceContainer.AddComponent<AudioSource>();
            Client.SetAudioInputSource(_targetAudioSource);
            return _targetAudioSource;
        }

        private static void TryStopRecording(MicrophoneDeviceInfo device)
        {
            if (!device.IsValid)
            {
                return;
            }

            if (Microphone.IsRecording(device.Name))
            {
                Microphone.End(device.Name);
            }
        }
        
        private void TrySyncMicrophoneAudioSourceReadPosWithMicrophoneWritePos()
        {
            var isRecording = IsEnabled && SelectedDevice.IsValid && Microphone.IsRecording(SelectedDevice.Name) && _targetAudioSource != null;
            if (!isRecording)
            {
                return;
            }
            
            var microphonePosition = Microphone.GetPosition(SelectedDevice.Name);
            if (microphonePosition >= 0 && _targetAudioSource.timeSamples > microphonePosition)
            {
                _targetAudioSource.timeSamples = microphonePosition;
            }
        }
    }
}