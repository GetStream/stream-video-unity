#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

@interface AudioSessionMonitor : NSObject

- (NSDictionary *)getCurrentAudioSettings;
- (void)startMonitoring;
- (void)stopMonitoring;

//#ifdev __cplusplus
extern "C" {
//#endif
    
    const char* AudioMonitor_GetCurrentSettings();
    void AudioMonitor_StartMonitoring();
    void AudioMonitor_StopMonitoring();
    void AudioMonitor_StopMonitoring();
    void AudioMonitor_PrepareAudioSessionForRecording();
    void AudioMonitor_ToggleLargeSpeaker(int enabled);
    
//#ifdev __cplusplus
}
//#endif


@end

NS_ASSUME_NONNULL_END
