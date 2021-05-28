using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinimapCameraMovement : MonoBehaviour
{

    public float m_MinimapCameraHeight = 200.0f;
    private Camera m_MainCamera;
    private Vector3 currentPosition;

    // Start is called before the first frame update
    void Start()
    {
        m_MainCamera = Camera.main;
        
        currentPosition = m_MainCamera.transform.position;

        // keep y component
        currentPosition.y = m_MinimapCameraHeight;

        transform.position = currentPosition;

        transform.rotation = Quaternion.Euler(90f, m_MainCamera.transform.eulerAngles.y, 0f);
    }

    // LateUpdate is called once per frame after Update and FixedUpdate
    void LateUpdate()
    {
        currentPosition = m_MainCamera.transform.position;

        // keep y component
        currentPosition.y = transform.position.y;

        transform.position = currentPosition;

        transform.rotation = Quaternion.Euler(90f, m_MainCamera.transform.eulerAngles.y, 0f);
    }
}
