using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class VisManager : MonoBehaviour
{
    public Shader visShader; // clouds are a post-process shader
    private Material visMaterial;

    public Texture3D visTexture;

    public bool showIsolines = true;

    public enum Component{waterContent = 1, iceContent = 0, rainMixingRatio = 2, showAll = 3};

    public Component shownComponent = Component.showAll;

    [Range(0, 1)]
    public float isovalue = 0.1f;


    // initialize the material on startup (editor as well as game mode)
    private void Start()
    {
        if (visMaterial == null) visMaterial = new Material(visShader);
        visMaterial.SetTexture("VisTexture", visTexture);
        visMaterial.SetInt("showIsosurface", Convert.ToInt32(showIsolines));
        visMaterial.SetFloat("isovalue", isovalue);
        visMaterial.SetInt("shownComponent", (int)shownComponent);
    }
    private void OnEnable()
    {
        Shader cloudSh = Shader.Find("Unlit/VisShader");
        if (visMaterial == null) visMaterial = new Material(cloudSh);
        visMaterial.SetTexture("VisTexture", visTexture);
        visMaterial.SetInt("showIsosurface", Convert.ToInt32(showIsolines));
        visMaterial.SetFloat("isovalue", isovalue);
        visMaterial.SetInt("shownComponent", (int)shownComponent);
    }


    [ImageEffectOpaque] // is applied after rendering all opaque elements, but before transparent ones.
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (visMaterial == null) visMaterial = new Material(visShader);
        visMaterial.SetTexture("VisTexture", visTexture);
        visMaterial.SetInt("showIsosurface", Convert.ToInt32(showIsolines));
        visMaterial.SetFloat("isovalue", isovalue);
        visMaterial.SetInt("shownComponent", (int)shownComponent);

        Graphics.Blit(source, destination, visMaterial); // copies the rendered image (from camera, to _MainTex from shader)
    }
}
