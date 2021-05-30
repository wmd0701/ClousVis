using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class CloudManager : MonoBehaviour
{
	public Shader cloudShader; // clouds are a post-process shader
	private Material cloudMaterial;

	public Texture3D cloudTexture_clw;
	public Texture3D cloudTexture_cli;
	public Texture3D cloudTexture_qr;
	public Texture3D cloudTexture_pres;
	private Texture3D to_visualize;

	private int index = 1;

	// initialize the material on startup (editor as well as game mode)
	private void Start() { 
		if(cloudMaterial == null) cloudMaterial = new Material(cloudShader);
		to_visualize = cloudTexture_clw;
		cloudMaterial.SetTexture("CloudTexture", to_visualize);
	}
	private void OnEnable() {
		Shader cloudSh = Shader.Find("Unlit/CloudShader");
		if (cloudMaterial == null) cloudMaterial = new Material(cloudSh);
		to_visualize = cloudTexture_clw;
		cloudMaterial.SetTexture("CloudTexture", to_visualize);
	}

	private void Update()
	{
		var input = Input.inputString;
		switch (input)
		{
			case "1": if (index != 1) index = 1; to_visualize = cloudTexture_clw; break;
			case "2": if (index != 2) index = 2; to_visualize = cloudTexture_cli; break;
			case "3": if (index != 3) index = 3; to_visualize = cloudTexture_qr; break;
			case "4": if (index != 4) index = 4; to_visualize = cloudTexture_pres; break;
			default: break;
		} 
	}

    [ImageEffectOpaque] // is applied after rendering all opaque elements, but before transparent ones.
	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if (cloudMaterial == null) cloudMaterial = new Material(cloudShader);
		cloudMaterial.SetTexture("CloudTexture", to_visualize);

		Graphics.Blit(source, destination, cloudMaterial);
	}
}