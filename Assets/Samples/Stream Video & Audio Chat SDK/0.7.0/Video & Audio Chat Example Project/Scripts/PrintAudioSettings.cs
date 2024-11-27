using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PrintAudioSettings : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Print audio settings SUBSCRIBE To BUTTON CLICK");
        GetComponent<Button>().onClick.AddListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        Debug.Log("Print audio settings clicked");
        AudioSessionMonitor.Instance.GetCurrentSettings();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
