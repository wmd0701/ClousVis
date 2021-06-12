using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class VisManager : MonoBehaviour
{
    public Shader visShader; // clouds are a post-process shader
    private Material visMaterial;

    public Texture3D visTexture;

    // initialize the material on startup (editor as well as game mode)
    private void Start()
    {
        if (visMaterial == null) visMaterial = new Material(visShader);
        visMaterial.SetTexture("VisTexture", visTexture);
    }
    private void OnEnable()
    {
        Shader cloudSh = Shader.Find("Unlit/VisShader");
        if (visMaterial == null) visMaterial = new Material(cloudSh);
        visMaterial.SetTexture("VisTexture", visTexture);
    }


    [ImageEffectOpaque] // is applied after rendering all opaque elements, but before transparent ones.
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (visMaterial == null) visMaterial = new Material(visShader);
        visMaterial.SetTexture("VisTexture", visTexture);

        Graphics.Blit(source, destination, visMaterial); // copies the rendered image (from camera, to _MainTex from shader)
    }
}
