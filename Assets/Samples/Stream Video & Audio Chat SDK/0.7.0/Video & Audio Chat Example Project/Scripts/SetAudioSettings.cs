using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SetAudioSettings : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        Debug.Log("Prepare audio settings clicked");
        AudioSessionMonitor.Instance.ConfigureAudioForRecording();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
