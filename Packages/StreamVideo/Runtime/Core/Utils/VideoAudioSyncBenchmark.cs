using System;
using System.Collections.Generic;
using System.Linq;
using StreamVideo.Core.StatefulModels;
using StreamVideo.Core.StatefulModels.Tracks;
using StreamVideo.Libs.Logs;
using StreamVideo.Libs.Time;
using UnityEngine;

namespace StreamVideo.Core.Utils
{
    /// <summary>
    /// This tool is used to benchmark the audio and video sync.
    ///
    /// How it works:
    /// A stream of video with a sequence of black frames with short sequences of white frames is expected. The white frames should be accompanied by a sound beep.
    /// The tool will detect a switch to bright frames and expects to receive a beep sound at the same time.
    /// The time between the frame switch and the sound beep is the audio and video sync delay.
    /// </summary>
    internal class VideoAudioSyncBenchmark
    {
        internal class Results
        {
            public IReadOnlyList<float> AudioDelays => _audioDelays;

            public static Results GenerateResults(IEnumerable<float> audioDelays)
            {
                var results = new Results();
                results._audioDelays.AddRange(audioDelays);
                return results;
            }

            private readonly List<float> _audioDelays = new List<float>();
        }

        public VideoAudioSyncBenchmark(ITimeService timeService, ILogs logs)
        {
            _timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
            _logs = logs ?? throw new ArgumentNullException(nameof(logs));
        }

        public void Init(IStreamCall call)
        {
            Log("Init benchmark");
            var remoteParticipant = call.Participants.FirstOrDefault(p => !p.IsLocalParticipant);
            if (remoteParticipant != null)
            {
                Log("Remote participant found. Getting video and audio tracks");
                GetVideoAndAudioTracks(remoteParticipant);
                return;
            }

            Log("Remote participant not found. Waiting for participant to join");
            call.ParticipantJoined += CallOnParticipantJoined;

            void CallOnParticipantJoined(IStreamVideoCallParticipant participant)
            {
                call.ParticipantJoined -= CallOnParticipantJoined;

                Log("Participant joined. Getting video and audio tracks");
                GetVideoAndAudioTracks(participant);
            }
        }

        public void Update()
        {
            if(_videoTrack == null || _videoTrack.TargetTexture == null || _audioTrack == null || _audioTrack.TargetAudioSource == null)
            {
                return;
            }
            
            EvaluateVideoFrame(_videoTrack.TargetTexture);
            EvaluateAudioFrame(_audioTrack.TargetAudioSource);
        }

        public Results Finish()
        {
            Log("Benchmark finished. Generating results.");
            var results = Results.GenerateResults(EnumerateResults());
            Clear();
            return results;

            IEnumerable<float> EnumerateResults()
            {
                var frameAndSoundPairs = Math.Min(_brightFramesReceivedAt.Count, _beepSoundReceivedAt.Count);

                if (_brightFramesReceivedAt.Count != _beepSoundReceivedAt.Count)
                {
                    Log("Warning. Bright frames and beep sounds count mismatch: " + _brightFramesReceivedAt.Count +
                        " vs " + _beepSoundReceivedAt.Count);
                }

                var totalDiff = 0f;
                for (int i = 0; i < frameAndSoundPairs; i++)
                {
                    var brightFrameTime = _brightFramesReceivedAt[i];
                    var beepSoundTime = _beepSoundReceivedAt[i];

                    var timeDiff = beepSoundTime - brightFrameTime;
                    Log("Audio and video sync delay: " + timeDiff);
                    totalDiff += timeDiff;
                    yield return timeDiff;
                }

                var averageDiff = totalDiff / frameAndSoundPairs;
                Log("All delays parsed. Average audio and video sync delay: " + averageDiff);
            }
        }

        // Determined via testing
        private const float BeepAudioVolumeThreshold = 0.001f;
        
        private const string LogsPrefix = "[VideoAudioSyncBenchmark] ";

        private readonly ITimeService _timeService;
        private readonly ILogs _logs;

        private readonly List<float> _brightFramesReceivedAt = new List<float>();
        private readonly List<float> _beepSoundReceivedAt = new List<float>();
        private readonly float[] _audioBuffer = new float[2048 * 2 * 2];

        private StreamVideoTrack _videoTrack;
        private StreamAudioTrack _audioTrack;

        private Texture2D _textureBuffer;
        private bool _prevIsBrightFrame;
        private bool _prevIsBeepSound;

        private void GetVideoAndAudioTracks(IStreamVideoCallParticipant participant)
        {
            if (participant.VideoTrack != null && participant.AudioTrack != null)
            {
                Log("Video and audio tracks already present. Starting benchmark");
                Start(participant.VideoTrack as StreamVideoTrack, participant.AudioTrack as StreamAudioTrack);
                return;
            }

            Log("Waiting for video and audio tracks to be added");
            participant.TrackAdded += ParticipantOnTrackAdded;

            void ParticipantOnTrackAdded(IStreamVideoCallParticipant streamVideoCallParticipant, IStreamTrack track)
            {
                if (participant.VideoTrack == null || participant.AudioTrack == null)
                {
                    return;
                }

                participant.TrackAdded -= ParticipantOnTrackAdded;
                Log("Video and audio tracks received. Starting benchmark");
                Start(participant.VideoTrack as StreamVideoTrack, participant.AudioTrack as StreamAudioTrack);
            }
        }

        // StreamTodo: we could start after receiving specific sequence of frames like r -> g > r > b -> start
        private void Start(StreamVideoTrack videoTrack, StreamAudioTrack audioTrack)
        {
            _videoTrack = videoTrack ?? throw new ArgumentNullException(nameof(videoTrack));
            _audioTrack = audioTrack ?? throw new ArgumentNullException(nameof(audioTrack));
        }

        private void EvaluateVideoFrame(RenderTexture texture)
        {
            var buffer = GetTextureBuffer(texture);
            RenderTexture.active = texture;
            buffer.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            buffer.Apply();

            var isBrightFrame = IsBrightFrame(buffer);

            if (!_prevIsBrightFrame && isBrightFrame)
            {
                Log("Bright frame detected at: " +
                    _timeService.Time); //StreamTodo: count how many bright frames received in sequence
                _brightFramesReceivedAt.Add(_timeService.Time);
            }

            _prevIsBrightFrame = isBrightFrame;
        }

        private void EvaluateAudioFrame(AudioSource audioSource)
        {
            audioSource.GetOutputData(_audioBuffer, 0);

            const float sampleRate = 44100;
            var samplesPerFrame = sampleRate * _timeService.DeltaTime;
            var maxFrames = Math.Min(samplesPerFrame, _audioBuffer.Length);

            float maxVolume = 0;
            for (var i = 0; i < maxFrames; i++)
            {
                var volume = Mathf.Abs(_audioBuffer[i]);
                if (volume > maxVolume)
                {
                    maxVolume = Mathf.Abs(volume);
                }
            }

            var isBeep = maxVolume > BeepAudioVolumeThreshold;

            if (isBeep && !_prevIsBeepSound)
            {
                Log($"Beep sound detected at: {_timeService.Time} and max volume: {maxVolume}");
                _beepSoundReceivedAt.Add(_timeService.Time);
            }

            _prevIsBeepSound = isBeep;
        }

        private Texture2D GetTextureBuffer(RenderTexture texture)
        {
            if (_textureBuffer == null || _textureBuffer.width != texture.width ||
                _textureBuffer.height != texture.height)
            {
                _textureBuffer = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, mipChain: false);
            }

            return _textureBuffer;
        }

        private static bool IsBrightFrame(Texture2D texture)
        {
            float totalBrightness = 0;
            for (var x = 0; x < texture.width; x++)
            {
                for (var y = 0; y < texture.height; y++)
                {
                    var pixel = texture.GetPixel(x, y);
                    totalBrightness += (pixel.r + pixel.g + pixel.b) / 3f;
                }
            }

            var averageBrightness = totalBrightness / (texture.width * texture.height);
            return averageBrightness > 0.5f;
        }

        private void Log(string message) => _logs.Warning(LogsPrefix + message);

        private void Clear()
        {
            _brightFramesReceivedAt.Clear();
            _beepSoundReceivedAt.Clear();
            
            _videoTrack = null;
            _audioTrack = null;
        }
    }
}