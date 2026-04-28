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
//
// Resilience:
//   _StreamConfigureAudioSessionForWebRTC also installs (once) NSNotification
//   observers for AVAudioSessionInterruptionNotification and
//   AVAudioSessionMediaServicesWereResetNotification. While the SDK
//   currently wants the session up (s_streamSessionConfigured == YES),
//   those observers reapply the category/mode and reactivate the session.
//   This handles the race where a phone-call interruption or a system
//   audio-server reset deactivates us mid-call and the next StartAudioPlayback
//   would otherwise fail with '!rec'.

// Tracks whether the SDK currently wants the session up. Set YES on
// Configure, NO on Deconfigure. The interruption/reset observers consult
// this so they don't reapply after the SDK explicitly tore the session down.
static BOOL s_streamSessionConfigured = NO;

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
// Returns YES on success, NO on failure.
static BOOL StreamActivateAudioSession() {
    AVAudioSession *audioSession = [AVAudioSession sharedInstance];
    NSError *error = nil;
    BOOL ok = [audioSession setActive:YES error:&error];
    if (!ok) {
        NSLog(@"[StreamVideo iOS Audio] Failed to activate audio session: %@", error);
    }
    return ok;
}

// Apply our preferred low-latency I/O parameters. VPIO's hardware echo
// canceller is tuned for ~5-10 ms slices; if the AVAudioSession's
// IOBufferDuration is much larger (e.g. Unity's default is ~42 ms / 2048
// frames at 48 kHz) the AEC adaptive filter has a 4x larger reference
// latency than it expects and effectively does not converge. The user
// then hears their own voice echoed back from the remote participant.
//
// We ask for 10 ms here. iOS may still snap to the closest hardware-
// supported duration (especially when another component in the same app
// has already locked a larger buffer); the result is read back below and
// logged so a regression can be diagnosed without rebuilding.
//
// Sample rate is pinned to 48 kHz to match Opus / WebRTC's internal rate
// and avoid an expensive resample on the iOS audio unit boundary.
static const double kStreamPreferredIOBufferDuration = 0.010;  // 10 ms
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

// Reapply our category/mode, reactivate the session, and reapply preferred
// low-latency I/O parameters. Used by both the public Configure entry
// point and the interruption/reset observers below.
// Returns YES iff both setCategory and setActive succeeded AND the resulting
// state is the one VPIO needs (PlayAndRecord + VideoChat/VoiceChat).
static BOOL StreamReapplySession() {
    BOOL categoryOk = StreamApplyVideoChatCategory();
    BOOL activateOk = StreamActivateAudioSession();
    // Preferred I/O params must be set AFTER the session is active in our
    // category - otherwise iOS treats them as advisory against the wrong
    // hardware route and silently drops them.
    StreamApplyPreferredIOParams();
    AVAudioSession *audioSession = [AVAudioSession sharedInstance];
    BOOL playAndRecord = [audioSession.category isEqualToString:AVAudioSessionCategoryPlayAndRecord];
    BOOL voiceProcessing = [audioSession.mode isEqualToString:AVAudioSessionModeVideoChat] ||
                           [audioSession.mode isEqualToString:AVAudioSessionModeVoiceChat];
    return categoryOk && activateOk && playAndRecord && voiceProcessing;
}

// Install (once) observers that reapply our session if the system tears it
// down (interruption ended, audio services reset). Intentionally never
// removed: registration cost is one notification block per app lifetime.
static void StreamEnsureNotificationObserversInstalled() {
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        NSNotificationCenter *center = [NSNotificationCenter defaultCenter];

        // Phone calls, Siri, etc. can interrupt the session. iOS sends:
        //   Began -> our session goes inactive (we don't need to do anything)
        //   Ended -> we should reactivate (and ideally re-set category in case
        //            something snuck a setCategory call in while we were
        //            inactive, e.g. another plugin reacting to the same event)
        [center addObserverForName:AVAudioSessionInterruptionNotification
                            object:nil
                             queue:[NSOperationQueue mainQueue]
                        usingBlock:^(NSNotification * _Nonnull note) {
            NSNumber *typeValue = note.userInfo[AVAudioSessionInterruptionTypeKey];
            if (typeValue == nil) return;
            AVAudioSessionInterruptionType type = (AVAudioSessionInterruptionType)typeValue.unsignedIntegerValue;

            if (type == AVAudioSessionInterruptionTypeBegan) {
                NSLog(@"[StreamVideo iOS Audio] Interruption BEGAN (sessionConfigured=%@)",
                      s_streamSessionConfigured ? @"YES" : @"NO");
                return;
            }

            if (type == AVAudioSessionInterruptionTypeEnded) {
                NSNumber *opts = note.userInfo[AVAudioSessionInterruptionOptionKey];
                BOOL shouldResume = opts != nil &&
                                    ((AVAudioSessionInterruptionOptions)opts.unsignedIntegerValue
                                     & AVAudioSessionInterruptionOptionShouldResume);
                NSLog(@"[StreamVideo iOS Audio] Interruption ENDED (shouldResume=%@, sessionConfigured=%@)",
                      shouldResume ? @"YES" : @"NO",
                      s_streamSessionConfigured ? @"YES" : @"NO");

                if (s_streamSessionConfigured) {
                    BOOL ok = StreamReapplySession();
                    NSLog(@"[StreamVideo iOS Audio] Reapplied session after interruption: %@",
                          ok ? @"OK" : @"FAILED");
                }
            }
        }];

        // The audio server (mediaserverd) can be reset by iOS, which puts
        // every audio session back to its defaults. Apple recommends apps
        // listen for this and reconfigure from scratch.
        //
        // NOTE: a media-services reset also tears down every CoreAudio
        // graph (including miniaudio's ma_device / AURemoteIO). Reapplying
        // the AVAudioSession here is necessary but not sufficient to
        // recover audio - the C# side would also need to call
        // RtcSession.TryRestartAudioPlayback / TryRestartAudioRecording.
        // This is logged but not surfaced to C# yet; if recovery becomes a
        // requirement, add a callback from this observer up to C#.
        [center addObserverForName:AVAudioSessionMediaServicesWereResetNotification
                            object:nil
                             queue:[NSOperationQueue mainQueue]
                        usingBlock:^(NSNotification * _Nonnull note) {
            NSLog(@"[StreamVideo iOS Audio] mediaServicesWereReset (sessionConfigured=%@)",
                  s_streamSessionConfigured ? @"YES" : @"NO");
            if (s_streamSessionConfigured) {
                BOOL ok = StreamReapplySession();
                NSLog(@"[StreamVideo iOS Audio] Reapplied session after media services reset: %@",
                      ok ? @"OK" : @"FAILED");
            }
        }];

        NSLog(@"[StreamVideo iOS Audio] Installed AVAudioSession interruption + reset observers");
    });
}

// Returns 1 on success (session is in PlayAndRecord + VideoChat/VoiceChat,
// active), 0 on failure. Errors are logged via NSLog.
int _StreamConfigureAudioSessionForWebRTC() {
    AVAudioSession *audioSession = [AVAudioSession sharedInstance];

    NSLog(@"[StreamVideo iOS Audio] ConfigureAudioSessionForWebRTC");

    StreamEnsureNotificationObserversInstalled();

    // Mark intent BEFORE applying so the observers (if they fire mid-apply
    // due to a concurrent system event) reapply rather than do nothing.
    s_streamSessionConfigured = YES;

    // Reapply category/mode in case something else (Unity, another plugin)
    // changed it after miniaudio's context init.
    BOOL ok = StreamReapplySession();

    BOOL voiceProcessing = [audioSession.mode isEqualToString:AVAudioSessionModeVideoChat] ||
                           [audioSession.mode isEqualToString:AVAudioSessionModeVoiceChat];

    double ioBufferMs = audioSession.IOBufferDuration * 1000.0;
    double preferredIoBufferMs = kStreamPreferredIOBufferDuration * 1000.0;
    BOOL ioBufferOk = ioBufferMs <= preferredIoBufferMs * 2.0; // tolerate up to ~20 ms

    NSLog(@"[StreamVideo iOS Audio]   Category: %@, Mode: %@", audioSession.category, audioSession.mode);
    NSLog(@"[StreamVideo iOS Audio]   Sample Rate: %.0f Hz (preferred %.0f), IO Buffer: %.1f ms (preferred %.1f) %@",
          audioSession.sampleRate, kStreamPreferredSampleRate,
          ioBufferMs, preferredIoBufferMs,
          ioBufferOk ? @"OK" : @"TOO LARGE - VPIO AEC will be ineffective");
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

    if (!ioBufferOk) {
        // VPIO's hardware AEC depends on a small (~10 ms) IO buffer. If it's
        // much larger, the user will hear their own voice echoed back from
        // the remote side. Most common cause on Unity: the engine claimed the
        // AVAudioSession with a 2048-frame buffer (Project Settings -> Audio ->
        // DSP Buffer Size) before the call started, and iOS won't shrink it
        // for our secondary client. Fix in Unity Project Settings ("Best
        // Latency" or "Good Latency"), or set
        // AudioSettings.GetConfiguration().dspBufferSize accordingly before
        // initialising audio.
        NSLog(@"[StreamVideo iOS Audio] WARNING: IOBufferDuration is %.1f ms, exceeds the %.1f ms "
              "preferred for VPIO AEC. Hardware echo cancellation will likely be ineffective. "
              "Most common cause: Unity's audio DSP buffer size locked the AVAudioSession at a "
              "larger value before the call started. Try setting Project Settings -> Audio -> "
              "DSP Buffer Size to 'Best Latency' or 'Good Latency'.",
              ioBufferMs, preferredIoBufferMs);
    }

    // Note: we deliberately do NOT call overrideOutputAudioPort:Speaker here.
    // VideoChat + DefaultToSpeaker already gives us the Zoom/Meet behavior:
    // loudspeaker by default, headphones/Bluetooth take over automatically.
    // If you want a hard "speakerphone" override, call _StreamForceOutputToSpeaker
    // explicitly from the C# layer.
    _StreamMaximizeInputGain();

    return ok ? 1 : 0;
}

// Tear down the session we put up. Called when the call ends or capture
// stops so other apps (music, navigation, etc.) can resume their audio at
// normal volume. Safe to call when no session was configured.
void _StreamDeconfigureAudioSession() {
    AVAudioSession *audioSession = [AVAudioSession sharedInstance];
    NSError *error = nil;

    // Clear intent first so the interruption/reset observers (if any
    // notification arrives during teardown) do not reapply the session.
    s_streamSessionConfigured = NO;

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
