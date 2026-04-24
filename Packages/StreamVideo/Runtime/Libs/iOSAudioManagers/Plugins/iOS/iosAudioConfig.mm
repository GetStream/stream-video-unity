#import <AVFoundation/AVFoundation.h>

// This plugin is THE single source of truth for AVAudioSession on iOS.
//
// The native WebRTC plugin (miniaudio) is explicitly told NOT to touch the
// AVAudioSession (sessionCategory = none, noAudioSessionActivate = true,
// noAudioSessionDeactivate = true). Everything below - category, mode,
// options, activation, deactivation, route overrides - is owned here so that
// there is exactly one place to change if behavior needs to change.
//
// Configuration we apply when a call starts:
//   - category = AVAudioSessionCategoryPlayAndRecord
//   - mode     = AVAudioSessionModeVideoChat
//   - options  = DefaultToSpeaker | AllowBluetooth | AllowBluetoothA2DP | AllowAirPlay
//   - active   = YES
//
// Why VideoChat (not VoiceChat)?
//   Both modes engage CoreAudio's VoiceProcessingIO audio unit, which is
//   what gives us hardware AEC, noise suppression and automatic gain control.
//   The difference:
//     - VoiceChat is a "phone call" mode: ringer-volume scope, defaults to
//       the receiver/earpiece. That produces a quiet "hold it to your ear"
//       experience, which is not what we want for a video call.
//     - VideoChat is the mode used by Zoom, Google Meet and Teams: media-
//       volume scope, defaults to the loudspeaker, and still lets wired or
//       Bluetooth headphones take over automatically when connected.
//
// The C# layer calls _StreamConfigureAudioSessionForWebRTC BEFORE the
// native recorder is started, so the VoiceProcessingIO audio unit opens
// with the right session in place, and calls _StreamDeconfigureAudioSession
// when capture stops so other apps can resume their normal audio.

extern "C" {

void _StreamForceOutputToSpeaker() {
    // Optional: hard-override the output route to the built-in loudspeaker,
    // even if a wired/Bluetooth headset is connected. This is intentionally
    // NOT called from _StreamConfigureAudioSessionForWebRTC so headphone
    // users keep their headphones - call it explicitly only when you want
    // to force the speaker (e.g. a "speakerphone" toggle button).
    AVAudioSession *audioSession = [AVAudioSession sharedInstance];
    NSError *error = nil;

    BOOL success = [audioSession overrideOutputAudioPort:AVAudioSessionPortOverrideSpeaker error:&error];

    if (error || !success) {
        NSLog(@"[StreamVideo iOS Audio] Error forcing speaker: success=%d, error=%@", success, error);
    } else {
        NSLog(@"[StreamVideo iOS Audio] Speaker override applied (forced built-in speaker)");
    }
}

void _StreamClearOutputOverride() {
    // Removes any previously applied output port override so the session
    // falls back to the route iOS would normally choose (headphones if
    // connected, otherwise loudspeaker because of DefaultToSpeaker).
    AVAudioSession *audioSession = [AVAudioSession sharedInstance];
    NSError *error = nil;
    BOOL success = [audioSession overrideOutputAudioPort:AVAudioSessionPortOverrideNone error:&error];
    if (error || !success) {
        NSLog(@"[StreamVideo iOS Audio] Failed to clear speaker override: success=%d, error=%@", success, error);
    } else {
        NSLog(@"[StreamVideo iOS Audio] Cleared speaker override (using default route)");
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

// Reassert PlayAndRecord + VideoChat mode so iOS routes capture/playback
// through the VoiceProcessingIO audio unit (hardware AEC/NS/AGC) and
// defaults the output to the loudspeaker at media volume.
// Returns YES on success, NO on failure (does not throw).
static BOOL StreamApplyVideoChatCategory() {
    AVAudioSession *audioSession = [AVAudioSession sharedInstance];
    NSError *error = nil;

    AVAudioSessionCategoryOptions options =
        AVAudioSessionCategoryOptionDefaultToSpeaker |
        AVAudioSessionCategoryOptionAllowBluetooth |
        AVAudioSessionCategoryOptionAllowBluetoothA2DP |
        AVAudioSessionCategoryOptionAllowAirPlay;

    BOOL ok = [audioSession setCategory:AVAudioSessionCategoryPlayAndRecord
                                   mode:AVAudioSessionModeVideoChat
                                options:options
                                  error:&error];
    if (!ok || error) {
        NSLog(@"[StreamVideo iOS Audio] Failed to set PlayAndRecord/VideoChat: %@", error);
        return NO;
    }
    return YES;
}

// Activate the audio session. Safe to call multiple times.
static void StreamActivateAudioSession() {
    AVAudioSession *audioSession = [AVAudioSession sharedInstance];
    NSError *error = nil;
    if (![audioSession setActive:YES error:&error]) {
        NSLog(@"[StreamVideo iOS Audio] Failed to activate audio session: %@", error);
    }
}

void _StreamConfigureAudioSessionForWebRTC() {
    AVAudioSession *audioSession = [AVAudioSession sharedInstance];

    NSLog(@"[StreamVideo iOS Audio] ConfigureAudioSessionForWebRTC");

    // Reapply category/mode in case something else (Unity, another plugin)
    // changed it after miniaudio's context init.
    StreamApplyVideoChatCategory();
    StreamActivateAudioSession();

    BOOL voiceProcessing = [audioSession.mode isEqualToString:AVAudioSessionModeVideoChat] ||
                           [audioSession.mode isEqualToString:AVAudioSessionModeVoiceChat];

    NSLog(@"[StreamVideo iOS Audio]   Category: %@, Mode: %@", audioSession.category, audioSession.mode);
    NSLog(@"[StreamVideo iOS Audio]   Sample Rate: %.0f Hz, IO Buffer: %.1f ms",
          audioSession.sampleRate, audioSession.IOBufferDuration * 1000.0);
    NSLog(@"[StreamVideo iOS Audio]   Hardware AEC/NS/AGC (VoiceProcessingIO): %@",
          voiceProcessing ? @"ENABLED" : @"DISABLED (mode is not VideoChat/VoiceChat!)");

    // Log the resolved output route so it's obvious whether we ended up on
    // the loudspeaker, AirPods, wired headphones, etc.
    AVAudioSessionRouteDescription *route = audioSession.currentRoute;
    for (AVAudioSessionPortDescription *output in route.outputs) {
        NSLog(@"[StreamVideo iOS Audio]   Output route: %@ (%@)", output.portName, output.portType);
    }

    if (!voiceProcessing) {
        NSLog(@"[StreamVideo iOS Audio] WARNING: hardware noise cancellation is NOT active. "
              "AVAudioSession.mode must be VideoChat or VoiceChat. Current mode=%@", audioSession.mode);
    }

    // Note: we deliberately do NOT call overrideOutputAudioPort:Speaker here.
    // VideoChat + DefaultToSpeaker already gives us the Zoom/Meet behavior:
    // loudspeaker by default, headphones/Bluetooth take over automatically.
    // If you want a hard "speakerphone" override, call _StreamForceOutputToSpeaker
    // explicitly from the C# layer.
    _StreamMaximizeInputGain();
}

// Tear down the session we put up. Called when the call ends or capture
// stops so other apps (music, navigation, etc.) can resume their audio at
// normal volume. Safe to call when no session was configured.
void _StreamDeconfigureAudioSession() {
    AVAudioSession *audioSession = [AVAudioSession sharedInstance];
    NSError *error = nil;

    BOOL ok = [audioSession setActive:NO
                          withOptions:AVAudioSessionSetActiveOptionNotifyOthersOnDeactivation
                                error:&error];
    if (!ok || error) {
        NSLog(@"[StreamVideo iOS Audio] Failed to deactivate audio session: %@", error);
    } else {
        NSLog(@"[StreamVideo iOS Audio] Audio session deactivated (notified other apps)");
    }
}

// Returns 1 if hardware AEC/NS/AGC (VoiceProcessingIO) is currently active, 0 otherwise.
int _StreamIsHardwareNoiseCancellationActive() {
    AVAudioSession *audioSession = [AVAudioSession sharedInstance];
    BOOL voiceProcessing = [audioSession.mode isEqualToString:AVAudioSessionModeVideoChat] ||
                           [audioSession.mode isEqualToString:AVAudioSessionModeVoiceChat];
    BOOL playAndRecord = [audioSession.category isEqualToString:AVAudioSessionCategoryPlayAndRecord];
    return (voiceProcessing && playAndRecord) ? 1 : 0;
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
    [info appendFormat:@"Hardware AEC/NS/AGC (VoiceProcessingIO): %@\n", voiceProcessing ? @"ENABLED" : @"DISABLED"];

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
