#import <AVFoundation/AVFoundation.h>

extern "C" {
    // Force audio to loudspeaker (bottom speaker, not earpiece)
    void ForceOutputToSpeaker() {
        AVAudioSession *audioSession = [AVAudioSession sharedInstance];
        NSError *error = nil;
        
        // Override output to speaker
        [audioSession overrideOutputAudioPort:AVAudioSessionPortOverrideSpeaker error:&error];
        
        if (error) {
            NSLog(@"Error forcing speaker output: %@", error);
        } else {
            NSLog(@"✓ Audio output forced to LOUDSPEAKER");
        }
    }
    
    // Get current audio route info
    const char* GetAudioRouteInfo() {
        AVAudioSession *audioSession = [AVAudioSession sharedInstance];
        AVAudioSessionRouteDescription *route = audioSession.currentRoute;
        
        NSMutableString *info = [NSMutableString string];
        [info appendString:@"=== Audio Route Info ===\n"];
        
        // Output info
        [info appendString:@"OUTPUTS:\n"];
        for (AVAudioSessionPortDescription *output in route.outputs) {
            [info appendFormat:@"  - %@ (%@)\n", output.portName, output.portType];
        }
        
        // Input info
        [info appendString:@"INPUTS:\n"];
        for (AVAudioSessionPortDescription *input in route.inputs) {
            [info appendFormat:@"  - %@ (%@)\n", input.portName, input.portType];
        }
        
        [info appendString:@"========================"];
        
        return strdup([info UTF8String]);
    }
    
    // Get current audio session info (updated with route info)
    const char* GetAudioSessionInfo() {
        AVAudioSession *audioSession = [AVAudioSession sharedInstance];
        AVAudioSessionRouteDescription *route = audioSession.currentRoute;
        
        NSString *category = audioSession.category;
        NSString *mode = audioSession.mode;
        AVAudioSessionCategoryOptions options = audioSession.categoryOptions;
        
        // Build info string
        NSMutableString *info = [NSMutableString string];
        [info appendFormat:@"=== iOS Audio Session Info ===\n"];
        [info appendFormat:@"Category: %@\n", category];
        [info appendFormat:@"Mode: %@\n", mode];
        
        // Parse options
        [info appendString:@"Options:\n"];
        if (options & AVAudioSessionCategoryOptionMixWithOthers)
            [info appendString:@"  - MixWithOthers\n"];
        if (options & AVAudioSessionCategoryOptionDuckOthers)
            [info appendString:@"  - DuckOthers\n"];
        if (options & AVAudioSessionCategoryOptionAllowBluetooth)
            [info appendString:@"  - AllowBluetooth\n"];
        if (options & AVAudioSessionCategoryOptionDefaultToSpeaker)
            [info appendString:@"  - DefaultToSpeaker ✓\n"];
        if (options & AVAudioSessionCategoryOptionInterruptSpokenAudioAndMixWithOthers)
            [info appendString:@"  - InterruptSpokenAudioAndMixWithOthers\n"];
        if (options & AVAudioSessionCategoryOptionAllowBluetoothA2DP)
            [info appendString:@"  - AllowBluetoothA2DP\n"];
        if (options & AVAudioSessionCategoryOptionAllowAirPlay)
            [info appendString:@"  - AllowAirPlay\n"];
        
        // Voice processing info
        BOOL voiceProcessingEnabled = NO;
        if ([mode isEqualToString:AVAudioSessionModeVoiceChat] || 
            [mode isEqualToString:AVAudioSessionModeVideoChat]) {
            voiceProcessingEnabled = YES;
        }
        
        [info appendFormat:@"\nVoice Processing (AEC/AGC/NS): %@\n", 
            voiceProcessingEnabled ? @"ENABLED ✓" : @"DISABLED ✗"];
        
        // Current audio route
        [info appendString:@"\n--- Current Audio Route ---\n"];
        [info appendString:@"OUTPUT: "];
        if (route.outputs.count > 0) {
            AVAudioSessionPortDescription *output = route.outputs[0];
            [info appendFormat:@"%@ (%@)", output.portName, output.portType];
            
            // Highlight if using loudspeaker
            if ([output.portType isEqualToString:AVAudioSessionPortBuiltInSpeaker]) {
                [info appendString:@" 🔊 LOUDSPEAKER"];
            } else if ([output.portType isEqualToString:AVAudioSessionPortBuiltInReceiver]) {
                [info appendString:@" ⚠️ EARPIECE (not loudspeaker!)"];
            }
            [info appendString:@"\n"];
        } else {
            [info appendString:@"None\n"];
        }
        
        [info appendString:@"INPUT: "];
        if (route.inputs.count > 0) {
            AVAudioSessionPortDescription *input = route.inputs[0];
            [info appendFormat:@"%@ (%@)\n", input.portName, input.portType];
        } else {
            [info appendString:@"None\n"];
        }
        
        [info appendString:@"=============================="];
        
        return strdup([info UTF8String]);
    }
    
    // Set to Default mode (minimal processing) + FORCE SPEAKER
    void SetAudioModeDefault() {
        AVAudioSession *audioSession = [AVAudioSession sharedInstance];
        NSError *error = nil;
        
        [audioSession setCategory:AVAudioSessionCategoryPlayAndRecord
                             mode:AVAudioSessionModeDefault
                          options:AVAudioSessionCategoryOptionAllowBluetooth |
                                  AVAudioSessionCategoryOptionDefaultToSpeaker
                            error:&error];
        
        if (error) {
            NSLog(@"Error setting Default mode: %@", error);
            return;
        }
        
        [audioSession setActive:YES error:&error];
        if (error) {
            NSLog(@"Error activating audio session: %@", error);
            return;
        }
        
        // Force speaker output
        ForceOutputToSpeaker();
        
        NSLog(@"✓ Audio mode set to: Default (NO voice processing) + LOUDSPEAKER");
    }
    
    // Set to VoiceChat mode (enables AEC/AGC/NS) + FORCE SPEAKER
    void SetAudioModeVoiceChat() {
        AVAudioSession *audioSession = [AVAudioSession sharedInstance];
        NSError *error = nil;
        
        [audioSession setCategory:AVAudioSessionCategoryPlayAndRecord
                             mode:AVAudioSessionModeVoiceChat
                          options:AVAudioSessionCategoryOptionAllowBluetooth |
                                  AVAudioSessionCategoryOptionDefaultToSpeaker
                            error:&error];
        
        if (error) {
            NSLog(@"Error setting VoiceChat mode: %@", error);
            return;
        }
        
        [audioSession setActive:YES error:&error];
        if (error) {
            NSLog(@"Error activating audio session: %@", error);
            return;
        }
        
        // Force speaker output
        ForceOutputToSpeaker();
        
        NSLog(@"✓ Audio mode set to: VoiceChat (AEC/AGC/NS ENABLED) + LOUDSPEAKER");
    }
    
    // Set to VideoChat mode (enables AEC/AGC/NS, optimized for video) + FORCE SPEAKER
    void SetAudioModeVideoChat() {
        AVAudioSession *audioSession = [AVAudioSession sharedInstance];
        NSError *error = nil;
        
        [audioSession setCategory:AVAudioSessionCategoryPlayAndRecord
                             mode:AVAudioSessionModeVideoChat
                          options:AVAudioSessionCategoryOptionAllowBluetooth |
                                  AVAudioSessionCategoryOptionDefaultToSpeaker
                            error:&error];
        
        if (error) {
            NSLog(@"Error setting VideoChat mode: %@", error);
            return;
        }
        
        [audioSession setActive:YES error:&error];
        if (error) {
            NSLog(@"Error activating audio session: %@", error);
            return;
        }
        
        // Force speaker output
        ForceOutputToSpeaker();
        
        NSLog(@"✓ Audio mode set to: VideoChat (AEC/AGC/NS ENABLED) + LOUDSPEAKER");
    }
    
    // Legacy function
    void ConfigureAudioSessionForWebRTC() {
        SetAudioModeVideoChat();
    }
}