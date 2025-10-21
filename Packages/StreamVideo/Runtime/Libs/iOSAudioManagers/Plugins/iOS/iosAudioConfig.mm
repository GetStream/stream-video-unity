#import <AVFoundation/AVFoundation.h>

extern "C" {
    // Force audio to loudspeaker (bottom speaker, not earpiece)
void ForceOutputToSpeaker() {
    AVAudioSession *audioSession = [AVAudioSession sharedInstance];
    NSError *error = nil;
    
    // Log BEFORE
    AVAudioSessionRouteDescription *beforeRoute = audioSession.currentRoute;
    NSLog(@"Route BEFORE override: %@", beforeRoute);
    
    // Override output to speaker
    BOOL success = [audioSession overrideOutputAudioPort:AVAudioSessionPortOverrideSpeaker error:&error];
    
    if (error || !success) {
        NSLog(@"❌ Error forcing speaker: success=%d, error=%@", success, error);
    } else {
        NSLog(@"✓ overrideOutputAudioPort called");
    }
    
    // Verify AFTER with delay
    dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)(0.2 * NSEC_PER_SEC)), dispatch_get_main_queue(), ^{
        AVAudioSessionRouteDescription *afterRoute = audioSession.currentRoute;
        NSLog(@"Route AFTER override: %@", afterRoute);
        
        if (afterRoute.outputs.count > 0) {
            AVAudioSessionPortDescription *output = afterRoute.outputs[0];
            if ([output.portType isEqualToString:AVAudioSessionPortBuiltInReceiver]) {
                NSLog(@"⚠️⚠️⚠️ STILL ON EARPIECE! Speaker override FAILED!");
            } else {
                NSLog(@"✓ Confirmed on: %@", output.portType);
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
    
    // Maximize output volume and gain
    void MaximizeAudioOutput() {
        AVAudioSession *audioSession = [AVAudioSession sharedInstance];
        NSError *error = nil;
        
        // Boost input gain to maximum if possible
        if (audioSession.isInputGainSettable) {
            [audioSession setInputGain:1.0 error:&error];
            if (error) {
                NSLog(@"⚠️ Could not set input gain: %@", error);
                error = nil;
            } else {
                NSLog(@"✓ Input gain maximized to 1.0");
            }
        } else {
            NSLog(@"ℹ️ Input gain not settable on this device");
        }
        
        // Optimize for performance and quality
        [audioSession setPreferredSampleRate:48000.0 error:&error];
        if (error) {
            NSLog(@"⚠️ Could not set sample rate: %@", error);
            error = nil;
        }
        
        [audioSession setPreferredIOBufferDuration:0.005 error:&error]; // 5ms
        if (error) {
            NSLog(@"⚠️ Could not set buffer duration: %@", error);
            error = nil;
        }
        
        NSLog(@"✓ Audio output maximized");
    }
    
    // Set to Default mode (minimal processing) + MAXIMUM OUTPUT
    void SetAudioModeDefault() {
        AVAudioSession *audioSession = [AVAudioSession sharedInstance];
        NSError *error = nil;
        
        [audioSession setCategory:AVAudioSessionCategoryPlayAndRecord
                             mode:AVAudioSessionModeDefault
                          options:AVAudioSessionCategoryOptionAllowBluetooth |
                                  AVAudioSessionCategoryOptionDefaultToSpeaker
                            error:&error];
        
        if (error) {
            NSLog(@"❌ Error setting Default mode: %@", error);
            return;
        }
        
        [audioSession setActive:YES error:&error];
        if (error) {
            NSLog(@"❌ Error activating audio session: %@", error);
            return;
        }
        
        // Force speaker output
        ForceOutputToSpeaker();
        
        // Maximize volume
        MaximizeAudioOutput();
        
        NSLog(@"✓ Audio mode: Default (NO voice processing) + LOUDSPEAKER + MAX VOLUME");
    }
    
    // Set to VoiceChat mode (enables AEC/AGC/NS) + MAXIMUM OUTPUT
    void SetAudioModeVoiceChat() {
        AVAudioSession *audioSession = [AVAudioSession sharedInstance];
        NSError *error = nil;
        
        [audioSession setCategory:AVAudioSessionCategoryPlayAndRecord
                             mode:AVAudioSessionModeVoiceChat
                          options:AVAudioSessionCategoryOptionAllowBluetooth |
                                  AVAudioSessionCategoryOptionDefaultToSpeaker
                            error:&error];
        
        if (error) {
            NSLog(@"❌ Error setting VoiceChat mode: %@", error);
            return;
        }
        
        [audioSession setActive:YES error:&error];
        if (error) {
            NSLog(@"❌ Error activating audio session: %@", error);
            return;
        }
        
        // Force speaker output
        ForceOutputToSpeaker();
        
        // Maximize volume
        MaximizeAudioOutput();
        
        NSLog(@"✓ Audio mode: VoiceChat (AEC/AGC/NS ENABLED) + LOUDSPEAKER + MAX VOLUME");
    }
    
    // Set to VideoChat mode (enables AEC/AGC/NS, optimized for video) + MAXIMUM OUTPUT
    void SetAudioModeVideoChat() {
        AVAudioSession *audioSession = [AVAudioSession sharedInstance];
        NSError *error = nil;
        
        [audioSession setCategory:AVAudioSessionCategoryPlayAndRecord
                             mode:AVAudioSessionModeVideoChat
                          options:AVAudioSessionCategoryOptionAllowBluetooth |
                                  AVAudioSessionCategoryOptionDefaultToSpeaker
                            error:&error];
        
        if (error) {
            NSLog(@"❌ Error setting VideoChat mode: %@", error);
            return;
        }
        
        [audioSession setActive:YES error:&error];
        if (error) {
            NSLog(@"❌ Error activating audio session: %@", error);
            return;
        }
        
        // Force speaker output
        ForceOutputToSpeaker();
        
        // Maximize volume
        MaximizeAudioOutput();
        
        NSLog(@"✓ Audio mode: VideoChat (AEC/AGC/NS ENABLED) + LOUDSPEAKER + MAX VOLUME");
    }
    
    // Legacy function - defaults to VideoChat with max volume
    void ConfigureAudioSessionForWebRTC() {
        SetAudioModeVideoChat();
    }
}
