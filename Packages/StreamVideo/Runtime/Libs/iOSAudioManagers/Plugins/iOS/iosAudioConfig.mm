#import <AVFoundation/AVFoundation.h>

extern "C" {
    // Get current audio session info
    const char* GetAudioSessionInfo() {
        AVAudioSession *audioSession = [AVAudioSession sharedInstance];
        
        NSString *category = audioSession.category;
        NSString *mode = audioSession.mode;
        AVAudioSessionCategoryOptions options = audioSession.categoryOptions;
        BOOL isActive = audioSession.isOtherAudioPlaying; // Basic check
        
        // Build info string
        NSMutableString *info = [NSMutableString string];
        [info appendFormat:@"=== iOS Audio Session Info ===\n"];
        [info appendFormat:@"Category: %@\n", category];
        [info appendFormat:@"Mode: %@\n", mode];
        [info appendFormat:@"Active: %@\n", audioSession.isOtherAudioPlaying ? @"YES" : @"NO"];
        
        // Parse options
        [info appendString:@"Options:\n"];
        if (options & AVAudioSessionCategoryOptionMixWithOthers)
            [info appendString:@"  - MixWithOthers\n"];
        if (options & AVAudioSessionCategoryOptionDuckOthers)
            [info appendString:@"  - DuckOthers\n"];
        if (options & AVAudioSessionCategoryOptionAllowBluetooth)
            [info appendString:@"  - AllowBluetooth\n"];
        if (options & AVAudioSessionCategoryOptionDefaultToSpeaker)
            [info appendString:@"  - DefaultToSpeaker\n"];
        if (options & AVAudioSessionCategoryOptionInterruptSpokenAudioAndMixWithOthers)
            [info appendString:@"  - InterruptSpokenAudioAndMixWithOthers\n"];
        if (options & AVAudioSessionCategoryOptionAllowBluetoothA2DP)
            [info appendString:@"  - AllowBluetoothA2DP\n"];
        if (options & AVAudioSessionCategoryOptionAllowAirPlay)
            [info appendString:@"  - AllowAirPlay\n"];
        
        // Voice processing info (AEC is enabled when using VoiceChat/VideoChat modes)
        BOOL voiceProcessingEnabled = NO;
        if ([mode isEqualToString:AVAudioSessionModeVoiceChat] || 
            [mode isEqualToString:AVAudioSessionModeVideoChat]) {
            voiceProcessingEnabled = YES;
        }
        
        [info appendFormat:@"\nVoice Processing (AEC/AGC/NS): %@\n", 
            voiceProcessingEnabled ? @"ENABLED ✓" : @"DISABLED ✗"];
        
        [info appendString:@"=============================="];
        
        // Convert to C string (Unity will handle memory)
        return strdup([info UTF8String]);
    }
    
    // Set to Default mode (minimal processing)
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
        } else {
            NSLog(@"✓ Audio mode set to: Default (NO voice processing)");
        }
    }
    
    // Set to VoiceChat mode (enables AEC/AGC/NS)
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
        } else {
            NSLog(@"✓ Audio mode set to: VoiceChat (AEC/AGC/NS ENABLED)");
        }
    }
    
    // Set to VideoChat mode (enables AEC/AGC/NS, optimized for video)
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
        } else {
            NSLog(@"✓ Audio mode set to: VideoChat (AEC/AGC/NS ENABLED)");
        }
    }
    
    // Legacy function (now just calls VideoChat)
    void ConfigureAudioSessionForWebRTC() {
        SetAudioModeVideoChat();
    }
}