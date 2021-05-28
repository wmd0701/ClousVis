using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MinimapText : MonoBehaviour
{
    public Terrain m_Terrmain;
    public float lon_min = 4.5f;
    public float lon_max = 14.496f;
    public float lat_min = 47.5f;
    public float lat_max = 54.4975f;
    private Camera m_MainCamera;
    private Text minimapText;
    private float x_range;
    private float z_range;
    private float lon_range;
    private float lat_range;

    // Start is called before the first frame update
    void Start()
    {
        m_MainCamera = Camera.main;
        minimapText = GetComponent<Text>();
        x_range = m_Terrmain.GetComponent<Terrain>().terrainData.size.x;
        z_range = m_Terrmain.GetComponent<Terrain>().terrainData.size.z;
        lon_range = lon_max - lon_min;
        lat_range = lat_max - lat_min;
    }

    // Update is called once per frame
    void Update()
    {
        float lon = (m_MainCamera.transform.position.x / x_range + 0.5f) * lon_range + lon_min;
        float lat = (m_MainCamera.transform.position.z / z_range + 0.5f) * lat_range + lat_min;
        minimapText.text = "lon: " + lon.ToString("F2") + "\n" + 
                           "lat: " + lat.ToString("F2") + "\n" + 
                           "ht: " + m_MainCamera.transform.position.y.ToString("F2");
    }
}
