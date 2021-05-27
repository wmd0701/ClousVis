using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinimapCameraMovement : MonoBehaviour
{

    private Camera m_MainCamera;

    // Start is called before the first frame update
    void Start()
    {
        m_MainCamera = Camera.main;
    }

    // LateUpdate is called once per frame after Update and FixedUpdate
    void LateUpdate()
    {
        Vector3 currentPosition = m_MainCamera.transform.position;

        // keep y component
        currentPosition.y = transform.position.y;

        transform.position = currentPosition;

        transform.rotation = Quaternion.Euler(90f, m_MainCamera.transform.eulerAngles.y, 0f);
    }
}
