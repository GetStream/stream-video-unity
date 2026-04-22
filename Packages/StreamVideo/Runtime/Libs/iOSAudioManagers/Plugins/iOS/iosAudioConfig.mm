#import <AVFoundation/AVFoundation.h>

// This plugin works alongside the native WebRTC plugin's miniaudio integration.
// Miniaudio handles the audio session category/mode (VoiceChat for AEC) and
// VoiceProcessingIO audio unit setup.
//
// This plugin handles:
//   - Speaker routing override (forces loudspeaker over earpiece)
//   - Input gain optimization
//   - Audio session diagnostics
//
// Call ConfigureAudioSessionForWebRTC() AFTER miniaudio initializes.

extern "C" {

void _StreamForceOutputToSpeaker() {
    AVAudioSession *audioSession = [AVAudioSession sharedInstance];
    NSError *error = nil;

    BOOL success = [audioSession overrideOutputAudioPort:AVAudioSessionPortOverrideSpeaker error:&error];

    if (error || !success) {
        NSLog(@"[StreamVideo iOS Audio] Error forcing speaker: success=%d, error=%@", success, error);
    } else {
        NSLog(@"[StreamVideo iOS Audio] Speaker override applied");
    }
}

void _StreamMaximizeInputGain() {
    AVAudioSession *audioSession = [AVAudioSession sharedInstance];
    NSError *error = nil;

    if (audioSession.isInputGainSettable) {
        [audioSession setInputGain:1.0 error:&error];
        if (error) {
            NSLog(@"[StreamVideo iOS Audio] Could not set input gain: %@", error);
        }
    }
}

void _StreamConfigureAudioSessionForWebRTC() {
    AVAudioSession *audioSession = [AVAudioSession sharedInstance];

    NSLog(@"[StreamVideo iOS Audio] ConfigureAudioSessionForWebRTC");
    NSLog(@"[StreamVideo iOS Audio]   Category: %@, Mode: %@", audioSession.category, audioSession.mode);
    NSLog(@"[StreamVideo iOS Audio]   Sample Rate: %.0f Hz, Buffer: %.1f ms",
          audioSession.sampleRate, audioSession.IOBufferDuration * 1000.0);

    BOOL voiceChatActive = [audioSession.mode isEqualToString:AVAudioSessionModeVoiceChat];
    if (voiceChatActive) {
        NSLog(@"[StreamVideo iOS Audio] VoiceChat mode active - hardware AEC enabled by miniaudio");
    } else {
        NSLog(@"[StreamVideo iOS Audio] WARNING: VoiceChat mode NOT active (mode=%@). Hardware AEC may not work.", audioSession.mode);
    }

    _StreamForceOutputToSpeaker();
    _StreamMaximizeInputGain();
}

const char* _StreamGetAudioSessionInfo() {
    AVAudioSession *audioSession = [AVAudioSession sharedInstance];
    AVAudioSessionRouteDescription *route = audioSession.currentRoute;
    AVAudioSessionCategoryOptions options = audioSession.categoryOptions;

    NSMutableString *info = [NSMutableString string];
    [info appendString:@"=== iOS Audio Session ===\n"];
    [info appendFormat:@"Category: %@\n", audioSession.category];
    [info appendFormat:@"Mode: %@\n", audioSession.mode];

    BOOL voiceProcessing = [audioSession.mode isEqualToString:AVAudioSessionModeVoiceChat] ||
                           [audioSession.mode isEqualToString:AVAudioSessionModeVideoChat];
    [info appendFormat:@"Voice Processing (AEC/AGC/NS): %@\n", voiceProcessing ? @"ENABLED" : @"DISABLED"];

    [info appendString:@"Options:"];
    if (options & AVAudioSessionCategoryOptionMixWithOthers) [info appendString:@" MixWithOthers"];
    if (options & AVAudioSessionCategoryOptionDuckOthers) [info appendString:@" DuckOthers"];
    if (options & AVAudioSessionCategoryOptionAllowBluetooth) [info appendString:@" AllowBluetooth"];
    if (options & AVAudioSessionCategoryOptionDefaultToSpeaker) [info appendString:@" DefaultToSpeaker"];
    if (options & AVAudioSessionCategoryOptionAllowBluetoothA2DP) [info appendString:@" AllowBluetoothA2DP"];
    if (options & AVAudioSessionCategoryOptionAllowAirPlay) [info appendString:@" AllowAirPlay"];
    if (options == 0) [info appendString:@" (None)"];
    [info appendString:@"\n"];

    for (AVAudioSessionPortDescription *output in route.outputs) {
        [info appendFormat:@"Output: %@ (%@)\n", output.portName, output.portType];
    }
    for (AVAudioSessionPortDescription *input in route.inputs) {
        [info appendFormat:@"Input: %@ (%@)\n", input.portName, input.portType];
    }

    [info appendFormat:@"Volume: %.0f%%\n", audioSession.outputVolume * 100.0];
    if (audioSession.isInputGainSettable) {
        [info appendFormat:@"Input Gain: %.0f%% (settable)\n", audioSession.inputGain * 100.0];
    } else {
        [info appendFormat:@"Input Gain: %.0f%% (not settable)\n", audioSession.inputGain * 100.0];
    }
    [info appendFormat:@"Sample Rate: %.0f Hz\n", audioSession.sampleRate];
    [info appendFormat:@"IO Buffer: %.1f ms\n", audioSession.IOBufferDuration * 1000.0];
    [info appendFormat:@"Input Latency: %.1f ms\n", audioSession.inputLatency * 1000.0];
    [info appendFormat:@"Output Latency: %.1f ms\n", audioSession.outputLatency * 1000.0];

    return strdup([info UTF8String]);
}

}
