#import "AudioSessionMonitor.h"
#import <AVFoundation/AVFoundation.h>

static AudioSessionMonitor *sharedInstance = nil;

@interface AudioSessionMonitor()

@property (nonatomic, strong) NSNotificationCenter *notificationCenter;

@end

// Silence missing UnitySendMessage implementation
#if !defined(UnitySendMessage)
void UnitySendMessage(const char* gameObjectName, const char* methodName, const char* message);
#endif

@implementation AudioSessionMonitor

+ (instancetype)sharedInstance {
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        sharedInstance = [[self alloc] init];
    });
    return sharedInstance;
}

- (instancetype)init {
    self = [super init];
    if (self) {
        _notificationCenter = [NSNotificationCenter defaultCenter];
    }
    return self;
}

- (void)prepareAudioSessionForRecording {
    AVAudioSession *audioSession = [AVAudioSession sharedInstance];
    NSError *error = nil;
    
    // Set category
    AVAudioSessionCategoryOptions options = AVAudioSessionCategoryOptionDefaultToSpeaker | AVAudioSessionCategoryOptionAllowBluetooth;
    
    if(@available(iOS 10.0, *)){
        options |= AVAudioSessionCategoryOptionAllowBluetoothA2DP;
    } else {
        NSLog(@"[Stream] Add option AVAudioSessionCategoryOptionAllowBluetoothA2DP");
    }
    
    BOOL success = [audioSession setCategory:AVAudioSessionCategoryPlayAndRecord mode:AVAudioSessionModeVideoChat options:options error:&error];
    
    if(!success){
        NSLog(@"[Stream] Error setting audio session category: %@", error.localizedDescription);
    } else {
        NSLog(@"[Stream] Success in setCategory");
    }
    
    // Set audio session mode
    if(@available(iOS 5.0, *) && false){ // TEMP DISABLE for testing. perhaps not needed at all, the setCategory has mode
        success = [audioSession setMode:AVAudioSessionModeVideoChat error:&error];
        
        if(!success){
            NSLog(@"[Stream] Error setting audio session category: %@", error.localizedDescription);
        } else {
            NSLog(@"[Stream] Success in setMod:AVAudioSessionModeVideoChate");
        }
    }
    
    // Set preferred sample rate and buffer duration
    if(@available(iOS 6.0, *)) {
        success = [audioSession setPreferredSampleRate:16000 error:&error];
        
        if(!success){
            NSLog(@"[Stream] Error setting audio session category: %@", error.localizedDescription);
        }
        
        success = [audioSession setPreferredIOBufferDuration:0.01 error:&error];
        
        if(!success){
            NSLog(@"[Stream] Error setting audio session category: %@", error.localizedDescription);
        }
    }
    
    // Activate the audio session
    success = [audioSession setActive:YES error:&error];
    if(!success) {
        NSLog(@"[Stream] Error setting audio session category: %@", error.localizedDescription);
    } else {
        NSLog(@"[Stream] Audio Session prepared sucessfully for recording with low latency.");
    }
    
}


- (NSDictionary *)getCurrentAudioSettings {
    AVAudioSession *session = [AVAudioSession sharedInstance];
    NSMutableDictionary *settings = [NSMutableDictionary dictionary];

    // Basic session properties
    settings[@"category"] = session.category ?: @"Unknown";
    settings[@"mode"] = session.mode ?: @"Unknown";

    // Category options
    AVAudioSessionCategoryOptions options = session.categoryOptions;
    settings[@"categoryOptions"] = @{
        @"allowBluetooth": @(((options & AVAudioSessionCategoryOptionAllowBluetooth) != 0)),
        @"allowBluetoothA2DP": @(((options & AVAudioSessionCategoryOptionAllowBluetoothA2DP) != 0)),
        @"allowAirPlay": @(((options & AVAudioSessionCategoryOptionAllowAirPlay) != 0)),
        @"defaultToSpeaker": @(((options & AVAudioSessionCategoryOptionDefaultToSpeaker) != 0)),
        @"mixWithOthers": @(((options & AVAudioSessionCategoryOptionMixWithOthers) != 0)),
        @"interruptSpokenAudioAndMixWithOthers": @(((options & AVAudioSessionCategoryOptionInterruptSpokenAudioAndMixWithOthers) != 0)),
    };

    // Routing information
    AVAudioSessionRouteDescription *route = session.currentRoute;
    if (route) {
        NSMutableArray *inputs = [NSMutableArray array];
        NSMutableArray *outputs = [NSMutableArray array];

        for (AVAudioSessionPortDescription *port in route.inputs) {
            [inputs addObject:@{
                @"portType": port.portType ?: @"Unknown",
                @"portName": port.portName ?: @"Unknown",
                @"channels": @(port.channels.count),
            }];
        }

        for (AVAudioSessionPortDescription *port in route.outputs) {
            [outputs addObject:@{
                @"portType": port.portType ?: @"Unknown",
                @"portName": port.portName ?: @"Unknown",
                @"channels": @(port.channels.count),
            }];
        }

        settings[@"routing"] = @{
            @"inputs": inputs,
            @"outputs": outputs
        };
    } else {
        settings[@"routing"] = @{@"error": @"No route available"};
    }

    // Technical settings
    settings[@"sampleRate"] = @{
        @"preferred": @(session.preferredSampleRate),
        @"current": @(session.sampleRate)
    };

    settings[@"IOBufferDuration"] = @{
        @"preferred": @(session.preferredIOBufferDuration),
        @"current": @(session.IOBufferDuration)
    };

    settings[@"latency"] = @{
        @"input": @(session.inputLatency),
        @"output": @(session.outputLatency)
    };

    // Hardware status
    settings[@"hardware"] = @{
        @"inputAvailable": @(session.isInputAvailable),
        @"otherAudioPlaying": @(session.isOtherAudioPlaying),
        @"inputGain": @(session.inputGain),
        @"outputVolume": @(session.outputVolume),
    };

    return settings;
}


- (void)startMonitoring{
    [self registerNotifications];
}

- (void)stopMonitoring{
    [self unregisterNotifications];
}

- (void)registerNotifications{
    NSNotificationCenter *center = [NSNotificationCenter defaultCenter];
    
    [center addObserverForName:AVAudioSessionRouteChangeNotification
                        object:nil
                         queue:[NSOperationQueue mainQueue] usingBlock:^(NSNotification *notification) {
        NSDictionary *settings = [self getCurrentAudioSettings];
        NSLog(@"[Stream] Audio Route Changed: %@", settings);
        
        NSNumber *reasonValue = notification.userInfo[AVAudioSessionRouteChangeReasonKey];
        AVAudioSessionRouteChangeReason reason = (AVAudioSessionRouteChangeReason)[reasonValue unsignedIntegerValue];
        
        // Map reason to a string
        NSString *reasonLabel = [self getLabelForRouteChangeReason:reason];
        
        NSDictionary *eventInfo = @{
          @"type": @"routeChange",
          @"settings": settings,
          @"reason": reasonLabel
        };
        
        // To JSON and send to Unity
        NSError *error;
        NSData *jsonData = [NSJSONSerialization dataWithJSONObject:eventInfo options:0 error:&error];
        if(jsonData){
            NSString *jsonString = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
            UnitySendMessage("AudioMonitor", "OnAudioSessionEvent", [jsonString UTF8String]);
        }
    }];
    
    [center addObserverForName:AVAudioSessionInterruptionNotification object:nil queue:[NSOperationQueue mainQueue] usingBlock:^(NSNotification *notification) {
        NSInteger type = [notification.userInfo[AVAudioSessionInterruptionTypeKey] integerValue];
        NSString *label = [self labelForInterruptionType:(AVAudioSessionInterruptionType)type];
        
        NSNumber *reasonValue = notification.userInfo[AVAudioSessionInterruptionReasonKey];
        NSString *reasonLabel = reasonValue ? [self labelForInterruptionReason:(AVAudioSessionInterruptionReason)[reasonValue integerValue]] : @"Unknown";
        
        NSLog(@"[Stream] Audio Session Interrupted: %@, Reason: %@", label, reasonLabel);
        
        NSDictionary *eventInfo = @{
          @"type": @"interruption",
          @"interruptionType": label,
          @"reason": reasonLabel
        };
        
        // To JSON and send to Unity
        NSError *error;
        NSData *jsonData = [NSJSONSerialization dataWithJSONObject:eventInfo options:0 error:&error];
        if(jsonData){
            NSString *jsonString = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
            UnitySendMessage("AudioMonitor", "OnAudioSessionEvent", [jsonString UTF8String]);
        }
    }];
    
    [center addObserverForName:AVAudioSessionMediaServicesWereResetNotification object:nil queue:[NSOperationQueue mainQueue] usingBlock:^(NSNotification *notification) {
        NSLog(@"[Stream] Audio Session Media Services Reset");
        
        NSDictionary *eventInfo = @{
          @"type": @"reset",
        };
        
        // To JSON and send to Unity
        NSError *error;
        NSData *jsonData = [NSJSONSerialization dataWithJSONObject:eventInfo options:0 error:&error];
        if(jsonData){
            NSString *jsonString = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
            UnitySendMessage("AudioMonitor", "OnAudioSessionEvent", [jsonString UTF8String]);
        }
    }];
}

- (void)unregisterNotifications {
    [self.notificationCenter removeObserver:self];
}

- (NSString *)getLabelForRouteChangeReason:(AVAudioSessionRouteChangeReason)reason {
    switch (reason) {
        case AVAudioSessionRouteChangeReasonUnknown:
            return @"Unknown";
        case AVAudioSessionRouteChangeReasonNewDeviceAvailable:
            return @"NewDeviceAvailable";
        case AVAudioSessionRouteChangeReasonOldDeviceUnavailable:
            return @"OldDeviceUnavailable";
        case AVAudioSessionRouteChangeReasonCategoryChange:
            return @"CategoryChange";
        case AVAudioSessionRouteChangeReasonOverride:
            return @"Override";
        case AVAudioSessionRouteChangeReasonWakeFromSleep:
            return @"WakeFromSleep";
        case AVAudioSessionRouteChangeReasonNoSuitableRouteForCategory:
            return @"NoSuitableRouteForCategory";
        case AVAudioSessionRouteChangeReasonRouteConfigurationChange:
            return @"RouteConfigurationChange";
        default:
            return @"Unknown";
    }
}

- (NSString *)labelForInterruptionType:(AVAudioSessionInterruptionType)type {
    switch (type) {
        case AVAudioSessionInterruptionTypeBegan:
            return @"Began";
        case AVAudioSessionInterruptionTypeEnded:
            return @"Ended";
        default:
            return @"Unknown";
    }
}

- (NSString *)labelForInterruptionReason:(AVAudioSessionInterruptionReason)reason {
    switch (reason) {
        case AVAudioSessionInterruptionReasonAppWasSuspended:
            return @"AppWasSuspended";
        case AVAudioSessionInterruptionReasonBuiltInMicMuted:
            return @"BuiltInMicMuted";
        default:
            return @"Unknown";
    }
}


@end


// Unity C interface implementation
const char* AudioMonitor_GetCurrentSettings(){
    NSDictionary *settings = [[AudioSessionMonitor sharedInstance] getCurrentAudioSettings];
    NSError *error;
    NSData *jsonData = [NSJSONSerialization dataWithJSONObject:settings options:0 error:&error];
    
    if(jsonData){
        NSString *jsonString = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
        
        return strdup([jsonString UTF8String]);
    }
    
    return strdup("{}");
}

void AudioMonitor_StartMonitoring(){
    [[AudioSessionMonitor sharedInstance] startMonitoring];
}

void AudioMonitor_StopMonitoring(){
    [[AudioSessionMonitor sharedInstance] stopMonitoring];
}

void AudioMonitor_PrepareAudioSessionForRecording(){
    [[AudioSessionMonitor sharedInstance] prepareAudioSessionForRecording];
}

