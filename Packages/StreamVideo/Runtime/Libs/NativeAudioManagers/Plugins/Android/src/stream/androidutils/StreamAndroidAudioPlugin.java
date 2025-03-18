package com.stream.audioutils;

import android.content.Context;
import android.media.AudioDeviceInfo;
import android.media.AudioManager;
import android.os.Build;

import com.unity3d.player.UnityPlayer;

public class StreamAndroidAudioPlugin {

    public static String SetupAudioModeForVideoCall() {
        try {
            Context context = UnityPlayer.currentActivity;
            AudioManager audioManager = (AudioManager) context.getSystemService(Context.AUDIO_SERVICE);

            // Set communication mode without changing routing
            audioManager.setMode(AudioManager.MODE_IN_COMMUNICATION);

            // Build a result to report what was actually set
            StringBuilder result = new StringBuilder();

            // Try to enable audio processing features without changing routing
            try {
                // Echo cancellation parameters
                audioManager.setParameters("ec_enable=true");
                audioManager.setParameters("ec_supported=true");

                // Noise suppression
                audioManager.setParameters("noise_suppression=true");

                // Automatic gain control
                audioManager.setParameters("agc_enable=true");

                // Try different fluence settings if available (Qualcomm devices)
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
                    // Try various fluence modes - device will use what it supports
                    String[] fluenceModes = {"fluence=broadside", "fluence=endfire", "fluence=dualmic"};
                    for (String mode : fluenceModes) {
                        audioManager.setParameters(mode);
                        String fluenceParam = audioManager.getParameters("fluence");
                        if (fluenceParam != null && !fluenceParam.isEmpty()) {
                            result.append("Fluence: ").append(fluenceParam).append("; ");
                            break; // Stop once we find a supported mode
                        }
                    }
                }

                // Read back what was actually set
                String echoCancel = audioManager.getParameters("ec_enable");
                if (echoCancel != null && !echoCancel.isEmpty()) {
                    result.append("EC: ").append(echoCancel).append("; ");
                }

                String noiseSuppression = audioManager.getParameters("noise_suppression");
                if (noiseSuppression != null && !noiseSuppression.isEmpty()) {
                    result.append("NS: ").append(noiseSuppression).append("; ");
                }

            } catch (Exception e) {
                result.append("Warning: Some audio parameters not supported: ").append(e.getMessage());
            }

            return result.toString(); // Return info about what was set
        } catch (Exception e) {
            return "Error: " + e.getMessage();
        }

    }

    public static String GetAudioDebugInfo() {
        StringBuilder debugInfo = new StringBuilder();
        try {
            Context context = UnityPlayer.currentActivity;
            AudioManager audioManager = (AudioManager) context.getSystemService(Context.AUDIO_SERVICE);

            // Core audio settings
            debugInfo.append("audioMode=").append(getAudioModeName(audioManager.getMode())).append("|");
            debugInfo.append("speakerphoneOn=").append(audioManager.isSpeakerphoneOn()).append("|");
            debugInfo.append("microphoneMute=").append(audioManager.isMicrophoneMute()).append("|");
            debugInfo.append("musicActive=").append(audioManager.isMusicActive()).append("|");
            debugInfo.append("ringerMode=").append(getRingerModeName(audioManager.getRingerMode())).append("|");
            debugInfo.append("bluetoothScoOn=").append(audioManager.isBluetoothScoOn()).append("|");
            debugInfo.append("bluetoothA2dpOn=").append(audioManager.isBluetoothA2dpOn()).append("|");
            debugInfo.append("wiredHeadsetOn=").append(audioManager.isWiredHeadsetOn()).append("|");

            // Volume levels
            debugInfo.append("volumeVoiceCall=").append(audioManager.getStreamVolume(AudioManager.STREAM_VOICE_CALL))
                    .append("/").append(audioManager.getStreamMaxVolume(AudioManager.STREAM_VOICE_CALL)).append("|");
            debugInfo.append("volumeSystem=").append(audioManager.getStreamVolume(AudioManager.STREAM_SYSTEM))
                    .append("/").append(audioManager.getStreamMaxVolume(AudioManager.STREAM_SYSTEM)).append("|");
            debugInfo.append("volumeRing=").append(audioManager.getStreamVolume(AudioManager.STREAM_RING))
                    .append("/").append(audioManager.getStreamMaxVolume(AudioManager.STREAM_RING)).append("|");
            debugInfo.append("volumeMusic=").append(audioManager.getStreamVolume(AudioManager.STREAM_MUSIC))
                    .append("/").append(audioManager.getStreamMaxVolume(AudioManager.STREAM_MUSIC)).append("|");

            // Android version
            debugInfo.append("androidSDK=").append(Build.VERSION.SDK_INT).append("|");
            debugInfo.append("androidRelease=").append(Build.VERSION.RELEASE).append("|");
            debugInfo.append("deviceModel=").append(Build.MODEL).append("|");
            debugInfo.append("deviceManufacturer=").append(Build.MANUFACTURER).append("|");

            // Available audio devices (Android 6.0+)
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
                AudioDeviceInfo[] devices = audioManager.getDevices(AudioManager.GET_DEVICES_ALL);
                debugInfo.append("connectedAudioDevices=").append(devices.length).append("|");

                StringBuilder deviceTypes = new StringBuilder();
                for (int i = 0; i < devices.length; i++) {
                    AudioDeviceInfo device = devices[i];
                    deviceTypes.append(getDeviceTypeName(device.getType()));
                    if (i < devices.length - 1) {
                        deviceTypes.append(",");
                    }
                }
                debugInfo.append("audioDeviceTypes=").append(deviceTypes.toString()).append("|");
            }

            // Current audio routing (Android 6.0+)
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
                debugInfo.append("hasDefaultMic=").append(hasDefaultMicrophone(audioManager)).append("|");
                debugInfo.append("hasDefaultSpeaker=").append(hasDefaultSpeaker(audioManager)).append("|");
            }

            // Try to get vendor-specific audio parameters if available
            try {
                String noiseSuppression = audioManager.getParameters("noise_suppression");
                if (noiseSuppression != null && !noiseSuppression.isEmpty()) {
                    debugInfo.append("noiseSuppression=").append(noiseSuppression).append("|");
                }

                String echoCancel = audioManager.getParameters("ec_enable");
                if (echoCancel != null && !echoCancel.isEmpty()) {
                    debugInfo.append("echoCancel=").append(echoCancel).append("|");
                }

                String fluenceParam = audioManager.getParameters("fluence");
                if (fluenceParam != null && !fluenceParam.isEmpty()) {
                    debugInfo.append("fluence=").append(fluenceParam).append("|");
                }
            } catch (Exception e) {
                debugInfo.append("vendorParamsError=").append(e.getMessage()).append("|");
            }

        } catch (Exception e) {
            return "error=" + e.getMessage();
        }

        // Remove trailing pipe if present
        if (debugInfo.length() > 0 && debugInfo.charAt(debugInfo.length() - 1) == '|') {
            debugInfo.setLength(debugInfo.length() - 1);
        }

        return debugInfo.toString();
    }

    // Helper methods to convert codes to readable names
    private static String getAudioModeName(int mode) {
        switch (mode) {
            case AudioManager.MODE_NORMAL:
                return "NORMAL";
            case AudioManager.MODE_RINGTONE:
                return "RINGTONE";
            case AudioManager.MODE_IN_CALL:
                return "IN_CALL";
            case AudioManager.MODE_IN_COMMUNICATION:
                return "IN_COMMUNICATION";
            default:
                return "UNKNOWN_" + mode;
        }
    }

    private static String getRingerModeName(int mode) {
        switch (mode) {
            case AudioManager.RINGER_MODE_NORMAL:
                return "NORMAL";
            case AudioManager.RINGER_MODE_SILENT:
                return "SILENT";
            case AudioManager.RINGER_MODE_VIBRATE:
                return "VIBRATE";
            default:
                return "UNKNOWN_" + mode;
        }
    }

    // Only called when SDK >= M
    private static String getDeviceTypeName(int type) {
        switch (type) {
            case AudioDeviceInfo.TYPE_BUILTIN_EARPIECE:
                return "EARPIECE";
            case AudioDeviceInfo.TYPE_BUILTIN_SPEAKER:
                return "SPEAKER";
            case AudioDeviceInfo.TYPE_BUILTIN_MIC:
                return "MIC";
            case AudioDeviceInfo.TYPE_BLUETOOTH_SCO:
                return "BT_SCO";
            case AudioDeviceInfo.TYPE_BLUETOOTH_A2DP:
                return "BT_A2DP";
            case AudioDeviceInfo.TYPE_WIRED_HEADSET:
                return "HEADSET";
            case AudioDeviceInfo.TYPE_WIRED_HEADPHONES:
                return "HEADPHONES";
            case AudioDeviceInfo.TYPE_USB_DEVICE:
                return "USB";
            case AudioDeviceInfo.TYPE_USB_HEADSET:
                return "USB_HEADSET";
            default:
                return "OTHER_" + type;
        }
    }

    // Only called when SDK >= M
    private static boolean hasDefaultMicrophone(AudioManager audioManager) {
        AudioDeviceInfo[] devices = audioManager.getDevices(AudioManager.GET_DEVICES_INPUTS);
        for (AudioDeviceInfo device : devices) {
            if (device.getType() == AudioDeviceInfo.TYPE_BUILTIN_MIC) {
                return true;
            }
        }
        return false;
    }

    // Only called when SDK >= M
    private static boolean hasDefaultSpeaker(AudioManager audioManager) {
        AudioDeviceInfo[] devices = audioManager.getDevices(AudioManager.GET_DEVICES_OUTPUTS);
        for (AudioDeviceInfo device : devices) {
            if (device.getType() == AudioDeviceInfo.TYPE_BUILTIN_SPEAKER) {
                return true;
            }
        }
        return false;
    }
}