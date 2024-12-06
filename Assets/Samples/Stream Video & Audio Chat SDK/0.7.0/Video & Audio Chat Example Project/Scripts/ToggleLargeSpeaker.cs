using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ToggleLargeSpeaker : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnButtonClick);
    }
    
    private void OnButtonClick()
    {
        Debug.Log("Toggle large speaker clicked");
        AudioSessionMonitor.Instance.ToggleLargeSpeaker();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
