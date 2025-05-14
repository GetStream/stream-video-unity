using System;
using StreamVideo.Core;

namespace StreamVideo.ExampleProject
{
    internal class AudioProcessingConfig
    {
        public event Action Updated; 
        public bool Enabled { get; set; }
        public bool EchoEnabled { get; set; }
        public bool AutoGainEnabled { get; set; }
        public bool NoiseEnabled { get; set; }

        public int NoiseLvl
        {
            get => _noiseLvl;
            set => _noiseLvl = value % 4;
        }

        private int _noiseLvl;

        public AudioProcessingConfig(IStreamVideoClient client)
        {
            _client = client;
        }

        public void LoadCurrentConfig()
        {
            _client.GetAudioProcessingModuleConfig(out var enabled, out var echoEnabled,
                out var autoGainEnabled, out var noiseEnabled, out var noiseLvl);
            Enabled = enabled;
            EchoEnabled = echoEnabled;
            AutoGainEnabled = autoGainEnabled;
            NoiseEnabled = noiseEnabled;
            NoiseLvl = noiseLvl;
            Updated?.Invoke();
        }

        public void Apply()
        {
            _client.SetAudioProcessingModule(Enabled, EchoEnabled, AutoGainEnabled, NoiseEnabled, NoiseLvl);
        }

        private readonly IStreamVideoClient _client;
    }
}