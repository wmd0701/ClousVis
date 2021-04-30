
using UnityEngine;

[ExecuteInEditMode]
public class VolumeBoundary : MonoBehaviour
{

    public bool showBoundaries = true;

    void OnDrawGizmos()
    {
        if (showBoundaries)
        {
            Gizmos.color = new Color(1.0f, 0.0f, 0.0f);
            Gizmos.DrawWireCube(new Vector3(0.0f, 1.83f+198.17f/2.0f, 0.0f), new Vector3(7504.0f, 198.17f, 7781.0f));
        }
    }
}