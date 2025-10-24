#import <AVFoundation/AVFoundation.h>

// ==================================================================================
// iOS Audio Configuration Plugin for Unity WebRTC
// 
// IMPORTANT: This plugin works ALONGSIDE native player/recorder, which handles:
//   - Audio session category & mode (VoiceChat mode for AEC)
//   - VoiceProcessingIO audio unit setup
//   - Sample rate and buffer duration preferences
//
// This plugin ONLY handles:
//   - Speaker routing override (forces loudspeaker over earpiece)
//   - Input gain optimization (maximizes microphone sensitivity)
//
// Call ConfigureAudioSessionForWebRTC() AFTER miniaudio initializes to apply
// speaker routing and input gain optimizations WITHOUT overriding miniaudio's
// VoiceChat mode and audio unit configuration.
// ==================================================================================

extern "C" {
    // Force audio to loudspeaker (bottom speaker, not earpiece)
    // This does NOT change category/mode - only routing
void ForceOutputToSpeaker() {
    NSLog(@"🔊 [iOS Audio Plugin] ForceOutputToSpeaker() called");
    
    AVAudioSession *audioSession = [AVAudioSession sharedInstance];
    NSError *error = nil;
    
    // Log current audio session state
    NSLog(@"🔊 [iOS Audio Plugin] Current category: %@, mode: %@", audioSession.category, audioSession.mode);
    
    // Log BEFORE
    AVAudioSessionRouteDescription *beforeRoute = audioSession.currentRoute;
    NSLog(@"🔊 [iOS Audio Plugin] Route BEFORE override: %@", beforeRoute);
    if (beforeRoute.outputs.count > 0) {
        NSLog(@"🔊 [iOS Audio Plugin]   Output port: %@", beforeRoute.outputs[0].portType);
    }
    
    // Override output to speaker (does NOT change category or mode)
    BOOL success = [audioSession overrideOutputAudioPort:AVAudioSessionPortOverrideSpeaker error:&error];
    
    if (error || !success) {
        NSLog(@"❌ [iOS Audio Plugin] Error forcing speaker: success=%d, error=%@", success, error);
    } else {
        NSLog(@"✅ [iOS Audio Plugin] overrideOutputAudioPort:Speaker SUCCESS");
    }
    
    // Verify AFTER with delay
    dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(0.2 * NSEC_PER_SEC)), dispatch_get_main_queue(), ^{
        AVAudioSessionRouteDescription *afterRoute = audioSession.currentRoute;
        NSLog(@"🔊 [iOS Audio Plugin] Route AFTER override: %@", afterRoute);
        
        if (afterRoute.outputs.count > 0) {
            AVAudioSessionPortDescription *output = afterRoute.outputs[0];
            NSLog(@"🔊 [iOS Audio Plugin]   Output port: %@", output.portType);
            
            if ([output.portType isEqualToString:AVAudioSessionPortBuiltInReceiver]) {
                NSLog(@"⚠️⚠️⚠️ [iOS Audio Plugin] STILL ON EARPIECE! Speaker override FAILED!");
            } else if ([output.portType isEqualToString:AVAudioSessionPortBuiltInSpeaker]) {
                NSLog(@"✅✅✅ [iOS Audio Plugin] SUCCESS! Audio routed to LOUDSPEAKER");
            } else {
                NSLog(@"ℹ️ [iOS Audio Plugin] Audio routed to: %@", output.portType);
            }
        }
    });
}
    
    // Comprehensive audio session info - includes everything
    const char* GetAudioSessionInfo() {
        AVAudioSession *audioSession = [AVAudioSession sharedInstance];
        AVAudioSessionRouteDescription *route = audioSession.currentRoute;
        
        NSString *category = audioSession.category;
        NSString *mode = audioSession.mode;
        AVAudioSessionCategoryOptions options = audioSession.categoryOptions;
        
        // Build comprehensive info string
        NSMutableString *info = [NSMutableString string];
        [info appendString:@"=== iOS Audio Session Info ===\n\n"];
        
        // Category and Mode
        [info appendFormat:@"Category: %@\n", category];
        [info appendFormat:@"Mode: %@\n\n", mode];
        
        // Voice Processing Status
        BOOL voiceProcessingEnabled = NO;
        if ([mode isEqualToString:AVAudioSessionModeVoiceChat] || 
            [mode isEqualToString:AVAudioSessionModeVideoChat]) {
            voiceProcessingEnabled = YES;
        }
        [info appendFormat:@"Voice Processing (AEC/AGC/NS): %@\n\n", 
            voiceProcessingEnabled ? @"ENABLED ✓" : @"DISABLED ✗"];
        
        // Category Options
        [info appendString:@"--- Category Options ---\n"];
        if (options == 0) {
            [info appendString:@"  (None)\n"];
        } else {
            if (options & AVAudioSessionCategoryOptionMixWithOthers)
                [info appendString:@"  ✓ MixWithOthers\n"];
            if (options & AVAudioSessionCategoryOptionDuckOthers)
                [info appendString:@"  ✓ DuckOthers\n"];
            if (options & AVAudioSessionCategoryOptionAllowBluetooth)
                [info appendString:@"  ✓ AllowBluetooth\n"];
            if (options & AVAudioSessionCategoryOptionDefaultToSpeaker)
                [info appendString:@"  ✓ DefaultToSpeaker\n"];
            if (options & AVAudioSessionCategoryOptionInterruptSpokenAudioAndMixWithOthers)
                [info appendString:@"  ✓ InterruptSpokenAudioAndMixWithOthers\n"];
            if (options & AVAudioSessionCategoryOptionAllowBluetoothA2DP)
                [info appendString:@"  ✓ AllowBluetoothA2DP\n"];
            if (options & AVAudioSessionCategoryOptionAllowAirPlay)
                [info appendString:@"  ✓ AllowAirPlay\n"];
        }
        
        // Audio Route Information
        [info appendString:@"\n--- Audio Routing ---\n"];
        
        // Outputs
        [info appendFormat:@"Output Count: %lu\n", (unsigned long)route.outputs.count];
        if (route.outputs.count > 0) {
            for (AVAudioSessionPortDescription *output in route.outputs) {
                [info appendFormat:@"  • %@ (%@)", output.portName, output.portType];
                
                if ([output.portType isEqualToString:AVAudioSessionPortBuiltInSpeaker]) {
                    [info appendString:@" 🔊 LOUDSPEAKER"];
                } else if ([output.portType isEqualToString:AVAudioSessionPortBuiltInReceiver]) {
                    [info appendString:@" ⚠️ EARPIECE"];
                } else if ([output.portType isEqualToString:AVAudioSessionPortHeadphones]) {
                    [info appendString:@" 🎧 HEADPHONES"];
                } else if ([output.portType isEqualToString:AVAudioSessionPortBluetoothA2DP] ||
                          [output.portType isEqualToString:AVAudioSessionPortBluetoothHFP] ||
                          [output.portType isEqualToString:AVAudioSessionPortBluetoothLE]) {
                    [info appendString:@" 🔵 BLUETOOTH"];
                }
                [info appendString:@"\n"];
            }
        } else {
            [info appendString:@"  (No outputs)\n"];
        }
        
        // Inputs
        [info appendFormat:@"Input Count: %lu\n", (unsigned long)route.inputs.count];
        if (route.inputs.count > 0) {
            for (AVAudioSessionPortDescription *input in route.inputs) {
                [info appendFormat:@"  • %@ (%@)\n", input.portName, input.portType];
            }
        } else {
            [info appendString:@"  (No inputs)\n"];
        }
        
        // Gain Settings
        [info appendString:@"\n--- Gain & Volume ---\n"];
        [info appendFormat:@"System Output Volume: %.2f (%.0f%%)\n", 
            audioSession.outputVolume, audioSession.outputVolume * 100.0];
        
        if (audioSession.isInputGainSettable) {
            [info appendFormat:@"Input Gain: %.2f (%.0f%%) [Settable ✓]\n", 
                audioSession.inputGain, audioSession.inputGain * 100.0];
        } else {
            [info appendFormat:@"Input Gain: %.2f (%.0f%%) [Not Settable ✗]\n", 
                audioSession.inputGain, audioSession.inputGain * 100.0];
        }
        [info appendFormat:@"Input Available: %@\n", audioSession.inputAvailable ? @"YES" : @"NO"];
        
        // Audio Performance
        [info appendString:@"\n--- Performance ---\n"];
        [info appendFormat:@"Sample Rate: %.0f Hz\n", audioSession.sampleRate];
        [info appendFormat:@"IO Buffer Duration: %.1f ms\n", audioSession.IOBufferDuration * 1000.0];
        [info appendFormat:@"Input Latency: %.1f ms\n", audioSession.inputLatency * 1000.0];
        [info appendFormat:@"Output Latency: %.1f ms\n", audioSession.outputLatency * 1000.0];
        
        // Other Audio Info
        [info appendString:@"\n--- Other ---\n"];
        [info appendFormat:@"Other Audio Playing: %@\n", 
            audioSession.isOtherAudioPlaying ? @"YES" : @"NO"];
        [info appendFormat:@"Secondary Audio Hint: %@\n",
            audioSession.secondaryAudioShouldBeSilencedHint ? @"Should be silenced" : @"OK"];
        
        [info appendString:@"\n=============================="];
        
        return strdup([info UTF8String]);
    }
    
    // Maximize input gain ONLY (does NOT change sample rate or buffer duration)
    // Sample rate and buffer duration are managed by miniaudio
    void MaximizeAudioOutput() {
        NSLog(@"🎤 [iOS Audio Plugin] MaximizeAudioOutput() called");
        
        AVAudioSession *audioSession = [AVAudioSession sharedInstance];
        NSError *error = nil;
        
        // Log current settings BEFORE changes
        NSLog(@"🎤 [iOS Audio Plugin] Current sample rate: %.0f Hz", audioSession.sampleRate);
        NSLog(@"🎤 [iOS Audio Plugin] Current buffer duration: %.1f ms", audioSession.IOBufferDuration * 1000.0);
        NSLog(@"🎤 [iOS Audio Plugin] Current input gain: %.2f (%.0f%%)", 
              audioSession.inputGain, audioSession.inputGain * 100.0);
        
        // ONLY set input gain - miniaudio handles sample rate and buffer duration
        if (audioSession.isInputGainSettable) {
            float oldGain = audioSession.inputGain;
            [audioSession setInputGain:1.0 error:&error];
            if (error) {
                NSLog(@"❌ [iOS Audio Plugin] Could not set input gain: %@", error);
                error = nil;
            } else {
                NSLog(@"✅ [iOS Audio Plugin] Input gain changed: %.2f → 1.0 (100%%)", oldGain);
            }
        } else {
            NSLog(@"ℹ️ [iOS Audio Plugin] Input gain not settable on this device");
        }
        
        // DO NOT change sample rate or buffer duration - miniaudio manages these
        NSLog(@"ℹ️ [iOS Audio Plugin] Sample rate and buffer duration managed by miniaudio");
        NSLog(@"✅ [iOS Audio Plugin] MaximizeAudioOutput() complete");
    }
    
    // DEPRECATED: Use ConfigureAudioSessionForWebRTC() instead
    // This function is kept for backward compatibility but does NOT change category/mode
    // Category and mode are managed by miniaudio
    void SetAudioModeDefault() {
        NSLog(@"⚠️ [iOS Audio Plugin] SetAudioModeDefault() called - DEPRECATED");
        NSLog(@"⚠️ [iOS Audio Plugin] Category/mode managed by miniaudio - only setting speaker + gain");
        
        AVAudioSession *audioSession = [AVAudioSession sharedInstance];
        
        // Log current state (DO NOT CHANGE category/mode)
        NSLog(@"ℹ️ [iOS Audio Plugin] Current category: %@", audioSession.category);
        NSLog(@"ℹ️ [iOS Audio Plugin] Current mode: %@", audioSession.mode);
        NSLog(@"ℹ️ [iOS Audio Plugin] miniaudio has already configured category & mode");
        
        // ONLY apply speaker routing and input gain
        ForceOutputToSpeaker();
        MaximizeAudioOutput();
        
        NSLog(@"✅ [iOS Audio Plugin] SetAudioModeDefault() complete (no category/mode changes)");
    }
    
    // DEPRECATED: Use ConfigureAudioSessionForWebRTC() instead
    // This function is kept for backward compatibility but does NOT change category/mode
    // Category and mode are managed by miniaudio
    void SetAudioModeVoiceChat() {
        NSLog(@"⚠️ [iOS Audio Plugin] SetAudioModeVoiceChat() called - DEPRECATED");
        NSLog(@"⚠️ [iOS Audio Plugin] Category/mode managed by miniaudio - only setting speaker + gain");
        
        AVAudioSession *audioSession = [AVAudioSession sharedInstance];
        
        // Log current state (DO NOT CHANGE category/mode)
        NSLog(@"ℹ️ [iOS Audio Plugin] Current category: %@", audioSession.category);
        NSLog(@"ℹ️ [iOS Audio Plugin] Current mode: %@", audioSession.mode);
        
        // Verify VoiceChat mode is set (should be set by miniaudio)
        if ([audioSession.mode isEqualToString:AVAudioSessionModeVoiceChat]) {
            NSLog(@"✅ [iOS Audio Plugin] VoiceChat mode confirmed (set by miniaudio)");
        } else {
            NSLog(@"⚠️ [iOS Audio Plugin] WARNING: Mode is %@, expected VoiceChat", audioSession.mode);
            NSLog(@"⚠️ [iOS Audio Plugin] Ensure miniaudio initialized before calling this");
        }
        
        // ONLY apply speaker routing and input gain
        ForceOutputToSpeaker();
        MaximizeAudioOutput();
        
        // Set input gain again if needed
        NSError *error = nil;
        if (audioSession.isInputGainSettable) {
            [audioSession setInputGain:1.0 error:&error];
            if (error) {
                NSLog(@"⚠️ [iOS Audio Plugin] Could not set input gain: %@", error);
            }
        }
        
        NSLog(@"✅ [iOS Audio Plugin] SetAudioModeVoiceChat() complete (no category/mode changes)");
    }
    
    // DEPRECATED: Use ConfigureAudioSessionForWebRTC() instead
    // This function is kept for backward compatibility but does NOT change category/mode
    // Category and mode are managed by miniaudio
    void SetAudioModeVideoChat() {
        NSLog(@"⚠️ [iOS Audio Plugin] SetAudioModeVideoChat() called - DEPRECATED");
        NSLog(@"⚠️ [iOS Audio Plugin] Category/mode managed by miniaudio - only setting gain");
        
        AVAudioSession *audioSession = [AVAudioSession sharedInstance];
        NSError *error = nil;
        
        // Log current state (DO NOT CHANGE category/mode)
        NSLog(@"ℹ️ [iOS Audio Plugin] Current category: %@", audioSession.category);
        NSLog(@"ℹ️ [iOS Audio Plugin] Current mode: %@", audioSession.mode);
        
        // ONLY set input gain (no speaker override for video chat)
        if (audioSession.isInputGainSettable) {
            float oldGain = audioSession.inputGain;
            [audioSession setInputGain:1.0 error:&error];
            if (error) {
                NSLog(@"❌ [iOS Audio Plugin] Could not set input gain: %@", error);
            } else {
                NSLog(@"✅ [iOS Audio Plugin] Input gain: %.2f → 1.0", oldGain);
            }
        }
        
        NSLog(@"✅ [iOS Audio Plugin] SetAudioModeVideoChat() complete (no category/mode changes)");
    }
    
    // Main configuration function - Call AFTER miniaudio initializes
    // This does NOT override miniaudio's category/mode settings
    // It ONLY adds speaker routing and input gain optimization
    void ConfigureAudioSessionForWebRTC() {
        NSLog(@"🎯 [iOS Audio Plugin] ========================================");
        NSLog(@"🎯 [iOS Audio Plugin] ConfigureAudioSessionForWebRTC() CALLED");
        NSLog(@"🎯 [iOS Audio Plugin] ========================================");
        
        AVAudioSession *audioSession = [AVAudioSession sharedInstance];
        
        // Log what miniaudio has configured
        NSLog(@"🎯 [iOS Audio Plugin] Current Audio Session State:");
        NSLog(@"🎯 [iOS Audio Plugin]   Category: %@", audioSession.category);
        NSLog(@"🎯 [iOS Audio Plugin]   Mode: %@", audioSession.mode);
        NSLog(@"🎯 [iOS Audio Plugin]   Sample Rate: %.0f Hz", audioSession.sampleRate);
        NSLog(@"🎯 [iOS Audio Plugin]   Buffer Duration: %.1f ms", audioSession.IOBufferDuration * 1000.0);
        
        // Check if VoiceChat mode is active (should be set by miniaudio)
        BOOL voiceChatEnabled = [audioSession.mode isEqualToString:AVAudioSessionModeVoiceChat];
        if (voiceChatEnabled) {
            NSLog(@"✅ [iOS Audio Plugin] VoiceChat mode ACTIVE - Hardware AEC enabled by miniaudio");
        } else {
            NSLog(@"⚠️⚠️⚠️ [iOS Audio Plugin] WARNING: VoiceChat mode NOT active!");
            NSLog(@"⚠️ [iOS Audio Plugin] Current mode: %@", audioSession.mode);
            NSLog(@"⚠️ [iOS Audio Plugin] Hardware AEC may not work!");
        }
        
        NSLog(@"🎯 [iOS Audio Plugin] Applying WebRTC optimizations...");
        
        // Apply our optimizations (speaker + gain only)
        ForceOutputToSpeaker();
        MaximizeAudioOutput();
        
        NSLog(@"🎯 [iOS Audio Plugin] ========================================");
        NSLog(@"🎯 [iOS Audio Plugin] ConfigureAudioSessionForWebRTC() COMPLETE");
        NSLog(@"🎯 [iOS Audio Plugin] ========================================");
    }
}
