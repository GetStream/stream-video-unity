using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Libs.iOSAudioManagers;
using StreamVideo.Core.LowLevelClient;
using StreamVideo.Core.Utils;
using StreamVideo.Libs.DeviceManagers;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.Utils;
using UnityEngine;

#pragma warning disable CS0162 // Disable unreachable code warning
namespace StreamVideo.Core.DeviceManagers
{
    internal class StreamAudioDeviceManager : DeviceManagerBase<MicrophoneDeviceInfo>, IStreamAudioDeviceManager
    {
        //StreamTodo: user can add/remove devices, we should detect this and expose DeviceAdded, DeviceRemoved events
        
        public override event DeviceEnabledChangeHandler IsEnabledChanged;

        public override bool IsEnabled
        {
            get => RtcSession.PublisherAudioTrackIsEnabled;
            protected set => RtcSession.TrySetPublisherAudioTrackEnabled(value);
        }

        public override IEnumerable<MicrophoneDeviceInfo> EnumerateDevices()
        {
            // Dummy call to ensure Unity requests Android permissions for audio recording. StreamTODO: create AndroidManifest with proper permissions and ensure it's being composed into final manifest file.
            var devices = Microphone.devices;
            foreach (var d in devices)
            {
            }
#if !UNITY_IOS //StreamTODO: resolve better. Perhaps just add IOS implementation to NativeAudioDeviceManager
            if (RtcSession.UseNativeAudioBindings)
            {
                NativeAudioDeviceManager.GetAudioInputDevices(ref _inputDevicesBuffer);
                foreach (var device in _inputDevicesBuffer)
                {
                    if (device == default)
                    {
                        continue;
                    }

                    yield return new MicrophoneDeviceInfo(device.Id, device.Name);
                }
            }
            else
#endif
            {
                foreach (var deviceName in Microphone.devices)
                {
                    yield return new MicrophoneDeviceInfo(deviceName);
                }
            }
        }

        protected override async Task<bool> OnTestDeviceAsync(MicrophoneDeviceInfo device, int msTimeout)
        {
            if (RtcSession.UseNativeAudioBindings)
            {
                //StreamTODO: Implement device testing via native binding
                return false;
            }

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
        /// <param name="device">Device to set as currently selected one. Only devices obtained via <see cref="EnumerateDevices"/> are valid</param>
        /// <param name="enable">Should the device get enabled right away? For a microphone device, it means the audio capturing will start immediately</param>
        /// <exception cref="ArgumentException">Thrown when the provided device has an invalid name</exception>
        public void SelectDevice(MicrophoneDeviceInfo device, bool enable)
        {
            Logs.WarningIfDebug(
                $"{nameof(SelectedDevice)} CALLED. SelectedDevice: {SelectedDevice}, IsEnabled: {IsEnabled}, New Device: {device}, Enable: {enable}");

            if (!device.IsValid)
            {
                throw new ArgumentException($"{nameof(device)} argument is not valid. The device name is empty.");
            }

            if (SelectedDevice == device && IsEnabled == enable)
            {
                Logs.WarningIfDebug(
                    $"{nameof(SelectedDevice)} call ignored. Nothing changed. SelectedDevice: {SelectedDevice}, IsEnabled: {IsEnabled}, New Device: {device}, Enable: {enable}");
                return;
            }

            TryStopRecording();

            SelectedDevice = device;

#if STREAM_DEBUG_ENABLED
            Logs.Info($"Changed microphone device to: {SelectedDevice}, Enable: {enable}");
#endif

            //StreamTodo: in some cases starting the mic recording before the call was causing the recorded audio being played in speakers with Unity Audio API
            //I think the reason was that AudioSource was being captured by an AudioListener but once I've joined the call, this disappeared
            //Check if we can have this AudioSource to be ignored by AudioListener's or otherwise mute it when there is not active call session

            IsEnabled = enable;

#if !UNITY_IOS //StreamTODO: resolve better
            if (RtcSession.UseNativeAudioBindings)
            {
                SetAudioRoutingAsync((NativeAudioDeviceManager.AudioRouting)SelectedDevice.IntId.Value).LogIfFailed();
            }
#endif
        }

        //StreamTodo: https://docs.unity3d.com/ScriptReference/AudioSource-ignoreListenerPause.html perhaps this should be enabled so that AudioListener doesn't affect recorded audio

        internal StreamAudioDeviceManager(RtcSession rtcSession, IInternalStreamVideoClient client, ILogs logs)
            : base(rtcSession, client, logs)
        {
            RtcSession.PublisherAudioTrackIsEnabledChanged += OnPublisherAudioTrackIsEnabledChanged;
            RtcSession.PublisherAudioTrackChanged += OnPublisherAudioTrackChanged;

            logs.WarningIfDebug($"[Audio] StreamAudioDeviceManager initialized. Native Audio: {RtcSession.UseNativeAudioBindings}");
            
            GetOrCreateTargetAudioSource();
        }

        protected override void OnDeviceChanging(MicrophoneDeviceInfo prev, MicrophoneDeviceInfo current)
        {
            base.OnDeviceChanging(prev, current);
            RtcSession.SetAudioRecordingDevice(current);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            TrySyncMicrophoneAudioSourceReadPosWithMicrophoneWritePos();
        }

        protected override void OnDisposing()
        {
            RtcSession.PublisherAudioTrackIsEnabledChanged -= OnPublisherAudioTrackIsEnabledChanged;
            RtcSession.PublisherAudioTrackChanged -= OnPublisherAudioTrackChanged;
            
            TryStopRecording();

            if (_targetAudioSourceContainer != null)
            {
                UnityEngine.Object.Destroy(_targetAudioSourceContainer);
            }

            base.OnDisposing();
        }

        //StreamTodo: wrap all operations on the Microphone devices + monitor for devices list changes
        //We could also allow to smart pick device -> sample each device and check which of them are actually gathering any input

        private AudioSource _targetAudioSource;
        private GameObject _targetAudioSourceContainer;
        private string _recordingDeviceName;

        private NativeAudioDeviceManager.AudioDeviceInfo[] _inputDevicesBuffer
            = new NativeAudioDeviceManager.AudioDeviceInfo[128];

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
        
        private async Task SetAudioRoutingAsync(NativeAudioDeviceManager.AudioRouting audioRoute)
        {
            Logs.WarningIfDebug($"{nameof(SelectedDevice)}. Setting preferred audio route to: " + SelectedDevice.Name);
            NativeAudioDeviceManager.SetPreferredAudioRoute(audioRoute);

            // StreamTODO: fix this. The audio route change takes some time. We need a callback or polling to know when to restart the native audio playback and recording
            // This has currently no impact because the Android implementation only exposes a single routing so it won't be changed
            await Task.Delay(500);
            Logs.WarningIfDebug($"{nameof(SelectedDevice)}. Setting preferred audio route to: " + SelectedDevice.Name +
                                " RESTARTING OBOE");
            RtcSession.TryRestartAudioRecording();
            RtcSession.TryRestartAudioPlayback();
        }
        
        private void UpdateAudioHandling()
        {
            var isEnabled = RtcSession.PublisherAudioTrackIsEnabled && RtcSession.Publisher != null && RtcSession.Publisher.PublisherAudioTrack != null;
            if (isEnabled && SelectedDevice.IsValid && !string.Equals(SelectedDevice.Name, _recordingDeviceName, StringComparison.Ordinal))
            {
                TryStopRecording();

                StartRecording(SelectedDevice);
            }

            if (!isEnabled)
            {
                TryStopRecording();
            }
        }

        private void StartRecording(MicrophoneDeviceInfo device)
        {
            if (!device.IsValid)
            {
                Logs.Error("Cannot start recording: the selected microphone device is not valid.");
                return;
            }

            //StreamTODO: We currently need this because in StreamPeerConnection ctor we check for audio source to create audio track. Refactor this dependency because we're progressively moving towards native audio handling
            var targetAudioSource = GetOrCreateTargetAudioSource();

            
#if UNITY_IOS && !UNITY_EDITOR
            var log = IOSAudioManager.GetCurrentSettings();
            Debug.LogError("[Audio] iOS Audio Session Info before starting microphone: " + log);
            IOSAudioManager.ConfigureForWebRTC();
#endif
            
            if (RtcSession.UseNativeAudioBindings)
            {
                return;
            }


            // StreamTodo: use Microphone.GetDeviceCaps to get min/max frequency -> validate it and pass to Microphone.Start

            // Sample rate must probably match the one used in AudioCustomFilter (this is what's being sent to webRTC). It's currently using AudioSettings.outputSampleRate
            _recordingDeviceName = SelectedDevice.Name;
            targetAudioSource.clip
                = Microphone.Start(_recordingDeviceName, loop: true, lengthSec: 1, AudioSettings.outputSampleRate);
            targetAudioSource.loop = true;

            using (new DebugStopwatchScope(Logs, "Waiting for microphone to start recording"))
            {
                while (!(Microphone.GetPosition(SelectedDevice.Name) > 0))
                {
                    // StreamTodo: add timeout. Otherwise might hang application
                }
            }
            targetAudioSource.Play();
            Logs.WarningIfDebug("[Audio] Started recording from microphone: " + device);
            
#if UNITY_IOS && !UNITY_EDITOR
            var log2 = IOSAudioManager.GetCurrentSettings();
            Debug.LogError("[Audio] iOS Audio Session Info AFTER starting microphone: " + log2);
            ForceSpeakerAsync().LogIfFailed();

#endif
            
        }

        private async Task ForceSpeakerAsync()
        {
            await Task.Delay(500);
            IOSAudioManager.ForceLoudspeaker();
            Debug.LogError("Force speaker output");
        }
        
        private void TryStopRecording()
        {
            if (RtcSession.UseNativeAudioBindings)
            {
                return;
            }

            if (string.IsNullOrEmpty(_recordingDeviceName))
            {
                return;
            }

            if (Microphone.IsRecording(_recordingDeviceName))
            {
                Microphone.End(_recordingDeviceName);
            }
            
            Logs.WarningIfDebug("[Audio] Stopped recording from microphone: " + _recordingDeviceName);
            _recordingDeviceName = null;
        }

        private void TrySyncMicrophoneAudioSourceReadPosWithMicrophoneWritePos()
        {
            if (RtcSession.UseNativeAudioBindings)
            {
                return;
            }

            var isRecording = IsEnabled && SelectedDevice.IsValid && Microphone.IsRecording(SelectedDevice.Name) &&
                              _targetAudioSource != null;
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
        
        private void OnPublisherAudioTrackIsEnabledChanged(bool isEnabled)
        {
            try
            {
                // StreamTodo: refactor OnSetEnabled to Async + execute without waiting. We fire the event first for UI to be update immediately. The OnSetEnabled can be slow if it needs to start microphone
                IsEnabledChanged?.Invoke(isEnabled);
            }
            catch (Exception e)
            {
                Logs.Exception(e);
            }
            
            UpdateAudioHandling();
        }
        
        private void OnPublisherAudioTrackChanged()
        {
            Logs.WarningIfDebug("[Audio] PublisherAudioTrackChanged event received");
            UpdateAudioHandling();
        }
    }
}
#pragma warning restore CS0162 // Re-enable unreachable code warning