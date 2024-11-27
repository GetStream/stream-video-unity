using UnityEngine;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

#if UNITY_IOS

public class AudioSessionMonitor : MonoBehaviour
{
    public event Action<AudioRouteChangeEventData> OnAudioRouteChanged;
    public event Action<AudioInterruptionEventData> OnAudioInterruption;
    
    public static AudioSessionMonitor Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("AudioSessionMonitor");
                _instance = go.AddComponent<AudioSessionMonitor>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }
    
    public void StartMonitoring()
    {
        AudioMonitor_StartMonitoring();
    }

    public void StopMonitoring()
    {
        AudioMonitor_StopMonitoring();
    }

    public void PrepareForRecording()
    {
        AudioMonitor_PrepareAudioSessionForRecording();
    }
    
    protected void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    protected void Start()
    {
        PrepareForRecording();
        StartMonitoring();

        GetCurrentSettings();
    }

    protected void Update()
    {
        while (_receivedEvents.TryDequeue(out var msg))
        {
            Debug.Log("Received event!");
            Debug.Log(msg);
        }
    }

    protected void OnDestroy()
    {
        StopMonitoring();
    }
    
    private static AudioSessionMonitor _instance;
    
    private readonly ConcurrentQueue<string> _receivedEvents = new ConcurrentQueue<string>();
    
    [DllImport("__Internal")]
    private static extern IntPtr AudioMonitor_GetCurrentSettings();
    
    [DllImport("__Internal")]
    private static extern void AudioMonitor_StartMonitoring();
    
    [DllImport("__Internal")]
    private static extern void AudioMonitor_StopMonitoring();
    
    [DllImport("__Internal")]
    private static extern void AudioMonitor_PrepareAudioSessionForRecording();

    // Called by native code through UnitySendMessage
    private void OnAudioSessionEvent(string jsonData)
    {
        _receivedEvents.Enqueue(jsonData);
        try
        {
            var eventData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);
            string eventType = eventData["type"].ToString();

            switch (eventType)
            {
                case "routeChange":
                    var routeEvent = new AudioRouteChangeEventData
                    {
                        Reason = eventData["reason"].ToString(),
                        Settings = ParseAudioSettings(eventData["settings"])
                    };
                    OnAudioRouteChanged?.Invoke(routeEvent);
                    break;

                case "interruption":
                    var interruptEvent = new AudioInterruptionEventData
                    {
                        Reason = eventData["reason"].ToString(),
                        InterruptionType = Convert.ToInt32(eventData["interruptionType"]),
                        Settings = ParseAudioSettings(eventData["settings"])
                    };
                    OnAudioInterruption?.Invoke(interruptEvent);
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error parsing audio session event: {e.Message}");
        }
    }

    public AudioSettings GetCurrentSettings()
    {
        _receivedEvents.Enqueue("------------ GetCurrentSettings");
        
        #if UNITY_IOS && !UNITY_EDITOR
        IntPtr ptr = AudioMonitor_GetCurrentSettings();
        if (ptr != IntPtr.Zero)
        {
            string jsonString = Marshal.PtrToStringAnsi(ptr);
            _receivedEvents.Enqueue(jsonString);
            //Debug.Log(jsonString);
            Marshal.FreeHGlobal(ptr);
            
            try
            {
                var rawSettings = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
                return ParseAudioSettings(rawSettings);
            }
            catch (Exception e)
            {
                _receivedEvents.Enqueue($"[Error] Error parsing audio settings: {e.Message}");
            }
        }
        #endif
        
        return new AudioSettings();
    }

    private AudioSettings ParseAudioSettings(object rawSettings)
    {
        var dict = rawSettings as Dictionary<string, object>;
        if (dict == null) return new AudioSettings();

        var settings = new AudioSettings
        {
            Category = dict["category"]?.ToString(),
            Mode = dict["mode"]?.ToString()
        };

        // Parse category options
        var options = dict["categoryOptions"] as Dictionary<string, object>;
        if (options != null)
        {
            settings.CategoryOptions = new AudioCategoryOptions
            {
                AllowBluetooth = Convert.ToBoolean(options["allowBluetooth"]),
                AllowBluetoothA2DP = Convert.ToBoolean(options["allowBluetoothA2DP"]),
                AllowAirPlay = Convert.ToBoolean(options["allowAirPlay"]),
                DefaultToSpeaker = Convert.ToBoolean(options["defaultToSpeaker"]),
                MixWithOthers = Convert.ToBoolean(options["mixWithOthers"]),
                DuckOthers = Convert.ToBoolean(options["duckOthers"])
            };
        }

        // Parse routing information
        var routing = dict["routing"] as Dictionary<string, object>;
        if (routing != null)
        {
            settings.Routing = new AudioRouting
            {
                Inputs = ParseAudioPorts(routing["inputs"]),
                Outputs = ParseAudioPorts(routing["outputs"])
            };
        }

        // Parse hardware status
        var hardware = dict["hardware"] as Dictionary<string, object>;
        if (hardware != null)
        {
            settings.Hardware = new AudioHardwareStatus
            {
                InputAvailable = Convert.ToBoolean(hardware["inputAvailable"]),
                OtherAudioPlaying = Convert.ToBoolean(hardware["otherAudioPlaying"]),
                InputGain = Convert.ToSingle(hardware["inputGain"]),
                OutputVolume = Convert.ToSingle(hardware["outputVolume"])
            };
        }

        return settings;
    }

    private List<AudioPort> ParseAudioPorts(object rawPorts)
    {
        var ports = new List<AudioPort>();
        var portList = rawPorts as Newtonsoft.Json.Linq.JArray;
        
        if (portList != null)
        {
            foreach (var port in portList)
            {
                ports.Add(new AudioPort
                {
                    PortType = port["portType"]?.ToString(),
                    PortName = port["portName"]?.ToString(),
                    Channels = Convert.ToInt32(port["channels"]),
                    IsBuiltIn = Convert.ToBoolean(port["isBuiltIn"])
                });
            }
        }
        
        return ports;
    }
}

public class AudioSettings
{
    public string Category { get; set; }
    public string Mode { get; set; }
    public AudioCategoryOptions CategoryOptions { get; set; }
    public AudioRouting Routing { get; set; }
    public AudioHardwareStatus Hardware { get; set; }
}

public class AudioCategoryOptions
{
    public bool AllowBluetooth { get; set; }
    public bool AllowBluetoothA2DP { get; set; }
    public bool AllowAirPlay { get; set; }
    public bool DefaultToSpeaker { get; set; }
    public bool MixWithOthers { get; set; }
    public bool DuckOthers { get; set; }
}

public class AudioRouting
{
    public List<AudioPort> Inputs { get; set; }
    public List<AudioPort> Outputs { get; set; }
}

public class AudioPort
{
    public string PortType { get; set; }
    public string PortName { get; set; }
    public int Channels { get; set; }
    public bool IsBuiltIn { get; set; }
}

public class AudioHardwareStatus
{
    public bool InputAvailable { get; set; }
    public bool OtherAudioPlaying { get; set; }
    public float InputGain { get; set; }
    public float OutputVolume { get; set; }
}

public class AudioRouteChangeEventData
{
    public string Reason { get; set; }
    public AudioSettings Settings { get; set; }
}

public class AudioInterruptionEventData
{
    public string Reason { get; set; }
    public int InterruptionType { get; set; }
    public AudioSettings Settings { get; set; }
}

#endif