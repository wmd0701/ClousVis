using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class VisManager : MonoBehaviour
{
    public static Shader visShader; // clouds are a post-process shader
    private Material visMaterial;

    public static Texture3D visTexture;
    public static Texture2D noiseTexture;

    public static bool showIsosurface = false;


    public static int shownComponent = 3;

    [Range(0, 1)]
    public static float isovalue = 0.3f;


    // initialize the material on startup (editor as well as game mode)
    private void Start()
    {
        if (visMaterial == null) visMaterial = new Material(visShader);
        visMaterial.SetTexture("VisTexture", visTexture);
        visMaterial.SetInt("showIsosurface", Convert.ToInt32(showIsosurface));
        visMaterial.SetFloat("isovalue", isovalue);
        visMaterial.SetInt("shownComponent", (int)shownComponent);
    }
    private void OnEnable()
    {
        Shader cloudSh = Shader.Find("Unlit/VisShader");
        if (visMaterial == null) visMaterial = new Material(cloudSh);
        visMaterial.SetTexture("VisTexture", visTexture);
        visMaterial.SetInt("showIsosurface", Convert.ToInt32(showIsosurface));
        visMaterial.SetFloat("isovalue", isovalue);
        visMaterial.SetInt("shownComponent", (int)shownComponent);
    }


    [ImageEffectOpaque] // is applied after rendering all opaque elements, but before transparent ones.
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (visMaterial == null) visMaterial = new Material(visShader);
        visMaterial.SetTexture("VisTexture", visTexture);
        visMaterial.SetTexture("noiseTexture", noiseTexture);
        visMaterial.SetInt("showIsosurface", Convert.ToInt32(showIsosurface));
        visMaterial.SetFloat("isovalue", isovalue);
        visMaterial.SetInt("shownComponent", (int)shownComponent);

        Graphics.Blit(source, destination, visMaterial); // copies the rendered image (from camera, to _MainTex from shader)
    }
}
