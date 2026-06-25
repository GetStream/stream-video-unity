#import <AVFoundation/AVFoundation.h>

// Single source of truth for AVAudioSession. The native WebRTC plugin (miniaudio)
// is told not to touch the session. Configures PlayAndRecord + VideoChat (-> VPIO
// HW AEC/NS/AGC, loudspeaker default) on call start; deactivates on call end.
// Also installs (once) observers for interruption-ended and media-services-reset
// to reapply the session while a call is still in progress.

static BOOL s_streamSessionConfigured = NO;

extern "C" {

// Hard-override the output to the built-in loudspeaker even if a wired/Bluetooth
// headset is connected. Not called from Configure; invoke explicitly for an
// explicit speakerphone toggle.
void _StreamForceOutputToSpeaker() {
    AVAudioSession *audioSession = [AVAudioSession sharedInstance];
    NSError *error = nil;
    BOOL success = [audioSession overrideOutputAudioPort:AVAudioSessionPortOverrideSpeaker error:&error];
    if (!success || error) {
        NSLog(@"[StreamVideo iOS Audio] Error forcing speaker: success=%d, error=%@", success, error);
    }
}

void _StreamClearOutputOverride() {
    AVAudioSession *audioSession = [AVAudioSession sharedInstance];
    NSError *error = nil;
    BOOL success = [audioSession overrideOutputAudioPort:AVAudioSessionPortOverrideNone error:&error];
    if (!success || error) {
        NSLog(@"[StreamVideo iOS Audio] Failed to clear speaker override: success=%d, error=%@", success, error);
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

static BOOL StreamActivateAudioSession() {
    AVAudioSession *audioSession = [AVAudioSession sharedInstance];
    NSError *error = nil;
    BOOL ok = [audioSession setActive:YES error:&error];
    if (!ok) {
        NSLog(@"[StreamVideo iOS Audio] Failed to activate audio session: %@", error);
    }
    return ok;
}

// VPIO HW AEC needs ~5-10 ms IO buffers to converge. iOS may snap to a larger
// value if another component locked the session first (see Configure log line).
static const double kStreamPreferredIOBufferDuration = 0.010;
static const double kStreamPreferredSampleRate       = 48000.0;

static void StreamApplyPreferredIOParams() {
    AVAudioSession *audioSession = [AVAudioSession sharedInstance];
    NSError *error = nil;

    if (![audioSession setPreferredSampleRate:kStreamPreferredSampleRate error:&error]) {
        NSLog(@"[StreamVideo iOS Audio] setPreferredSampleRate(%.0f) failed: %@",
              kStreamPreferredSampleRate, error);
    }

    error = nil;
    if (![audioSession setPreferredIOBufferDuration:kStreamPreferredIOBufferDuration error:&error]) {
        NSLog(@"[StreamVideo iOS Audio] setPreferredIOBufferDuration(%.3f) failed: %@",
              kStreamPreferredIOBufferDuration, error);
    }
}

// Returns YES iff the session ended up VPIO-compatible (PlayAndRecord + VideoChat/VoiceChat).
static BOOL StreamReapplySession() {
    BOOL categoryOk = StreamApplyVideoChatCategory();
    BOOL activateOk = StreamActivateAudioSession();
    // Apply preferred I/O params AFTER activation; otherwise iOS evaluates them
    // against the wrong hardware route and silently drops them.
    StreamApplyPreferredIOParams();
    AVAudioSession *audioSession = [AVAudioSession sharedInstance];
    BOOL playAndRecord = [audioSession.category isEqualToString:AVAudioSessionCategoryPlayAndRecord];
    BOOL voiceProcessing = [audioSession.mode isEqualToString:AVAudioSessionModeVideoChat] ||
                           [audioSession.mode isEqualToString:AVAudioSessionModeVoiceChat];
    return categoryOk && activateOk && playAndRecord && voiceProcessing;
}

// Reapply the session after system events (interruption-ended, media-services-reset).
// Note: media-services-reset also tears down miniaudio's ma_device; recovering audio
// fully would require RtcSession.TryRestartAudioPlayback / TryRestartAudioRecording
// from C#. Today this only reapplies the session.
static void StreamEnsureNotificationObserversInstalled() {
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        NSNotificationCenter *center = [NSNotificationCenter defaultCenter];

        [center addObserverForName:AVAudioSessionInterruptionNotification
                            object:nil
                             queue:[NSOperationQueue mainQueue]
                        usingBlock:^(NSNotification * _Nonnull note) {
            NSNumber *typeValue = note.userInfo[AVAudioSessionInterruptionTypeKey];
            if (typeValue == nil) return;
            AVAudioSessionInterruptionType type = (AVAudioSessionInterruptionType)typeValue.unsignedIntegerValue;

            if (type == AVAudioSessionInterruptionTypeEnded && s_streamSessionConfigured) {
                BOOL ok = StreamReapplySession();
                if (!ok) {
                    NSLog(@"[StreamVideo iOS Audio] Failed to reapply session after interruption");
                }
            }
        }];

        [center addObserverForName:AVAudioSessionMediaServicesWereResetNotification
                            object:nil
                             queue:[NSOperationQueue mainQueue]
                        usingBlock:^(NSNotification * _Nonnull note) {
            NSLog(@"[StreamVideo iOS Audio] mediaServicesWereReset (sessionConfigured=%@)",
                  s_streamSessionConfigured ? @"YES" : @"NO");
            if (s_streamSessionConfigured) {
                BOOL ok = StreamReapplySession();
                if (!ok) {
                    NSLog(@"[StreamVideo iOS Audio] Failed to reapply session after media services reset");
                }
            }
        }];
    });
}

// Returns 1 on success (PlayAndRecord + VideoChat/VoiceChat, active), 0 on failure.
int _StreamConfigureAudioSessionForWebRTC() {
    AVAudioSession *audioSession = [AVAudioSession sharedInstance];

    StreamEnsureNotificationObserversInstalled();

    // Mark intent BEFORE applying so observers triggered by a concurrent system
    // event reapply rather than skip.
    s_streamSessionConfigured = YES;

    BOOL ok = StreamReapplySession();

    BOOL voiceProcessing = [audioSession.mode isEqualToString:AVAudioSessionModeVideoChat] ||
                           [audioSession.mode isEqualToString:AVAudioSessionModeVoiceChat];

    double ioBufferMs = audioSession.IOBufferDuration * 1000.0;
    double preferredIoBufferMs = kStreamPreferredIOBufferDuration * 1000.0;
    BOOL ioBufferOk = ioBufferMs <= preferredIoBufferMs * 2.0;

    NSLog(@"[StreamVideo iOS Audio] Configured: category=%@ mode=%@ sampleRate=%.0f IOBuffer=%.1fms (preferred %.1fms) %@",
          audioSession.category, audioSession.mode,
          audioSession.sampleRate,
          ioBufferMs, preferredIoBufferMs,
          ioBufferOk ? @"OK" : @"TOO LARGE - VPIO AEC will be ineffective");

    if (!voiceProcessing) {
        NSLog(@"[StreamVideo iOS Audio] WARNING: hardware noise cancellation is NOT active. "
              "AVAudioSession.mode must be VideoChat or VoiceChat. Current mode=%@", audioSession.mode);
    }

    if (!ioBufferOk) {
        // Most common cause on Unity: the engine locked the session with a 2048-frame
        // buffer before the call started; iOS won't shrink it for our secondary client.
        NSLog(@"[StreamVideo iOS Audio] WARNING: IOBufferDuration is %.1f ms, exceeds the %.1f ms "
              "preferred for VPIO AEC. Hardware echo cancellation will likely be ineffective. "
              "Try Unity Project Settings -> Audio -> DSP Buffer Size = 'Best Latency' or 'Good Latency'.",
              ioBufferMs, preferredIoBufferMs);
    }

    _StreamMaximizeInputGain();

    return ok ? 1 : 0;
}

// Tear down the session so other apps can resume audio. Safe to call when no
// session was configured.
void _StreamDeconfigureAudioSession() {
    AVAudioSession *audioSession = [AVAudioSession sharedInstance];
    NSError *error = nil;

    // Clear intent first so observers don't reapply during teardown.
    s_streamSessionConfigured = NO;

    BOOL ok = [audioSession setActive:NO
                          withOptions:AVAudioSessionSetActiveOptionNotifyOthersOnDeactivation
                                error:&error];
    if (!ok || error) {
        NSLog(@"[StreamVideo iOS Audio] Failed to deactivate audio session: %@", error);
    }
}

// Returns 1 if the session is configured for VoiceProcessingIO
// (PlayAndRecord + VideoChat/VoiceChat), 0 otherwise. Configuration only;
// does not verify IOBufferDuration is small enough for AEC to converge.
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
