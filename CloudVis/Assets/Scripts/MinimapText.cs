using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapText : MonoBehaviour
{
    private Camera m_MainCamera;
    private Text minimapText;
 
    // Start is called before the first frame update
    void Start()
    {
        m_MainCamera = Camera.main;
        minimapText = GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {
        minimapText.text = "lon: " + m_MainCamera.transform.position.x.ToString("F2") + "   " + 
                           "lat: " + m_MainCamera.transform.position.z.ToString("F2");
    }
}
