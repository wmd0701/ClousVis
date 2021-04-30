using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class CloudManager : MonoBehaviour
{
	public Shader cloudShader; // clouds are a post-process shader
	private Material cloudMaterial;

	public Texture3D cloudTexture;

	// initialize the material on startup (editor as well as game mode)
	private void Start() { 
		if(cloudMaterial == null) cloudMaterial = new Material(cloudShader);
		cloudMaterial.SetTexture("CloudTexture", cloudTexture);
	}
	private void OnEnable() {
		Shader cloudSh = Shader.Find("Unlit/CloudShader");
		if (cloudMaterial == null) cloudMaterial = new Material(cloudSh);
		cloudMaterial.SetTexture("CloudTexture", cloudTexture);
	}


	[ImageEffectOpaque] // is applied after rendering all opaque elements, but before transparent ones.
	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if (cloudMaterial == null) cloudMaterial = new Material(cloudShader);
		cloudMaterial.SetTexture("CloudTexture", cloudTexture);

		Graphics.Blit(source, destination, cloudMaterial); // copies the rendered image (from camera, to _MainTex from shader)
	}
}