using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class CloudManager : MonoBehaviour
{
	public Shader cloudShader; // clouds are a post-process shader
	private Material cloudMaterial;

	public Texture3D cloudTexture;
	private Texture3D to_visualize;

	[Range(1,200)]
	public int lightSteps = 1;
	[Range(0, 1)]
	public float darknessThreshold = 0.2f;
	[Range(0, 1)]
	public float forwardScattering = 0.3f;
	[Range(0, 1)]
	public float backScattering = 0.8f;
	[Range(0, 1)]
	public float basicBrightness = 0.8f;
	[Range(0, 1)]
	public float phaseFactor = 0.15f;

	[Range(0, 1)]
	public float densityThreshold = 0.0f;
	[Range(0, 1)]
	public float cw_densityThreshold = 0.0f;
	[Range(0, 1)]
	public float ci_densityThreshold = 0.0f;

	// initialize the material on startup (editor as well as game mode)
	private void Start() { 
		if(cloudMaterial == null) cloudMaterial = new Material(cloudShader);
		cloudMaterial.SetTexture("CloudTexture", cloudTexture);

		cloudMaterial.SetFloat("darknessThreshold", darknessThreshold);
		cloudMaterial.SetFloat("densityThreshold", densityThreshold);
		cloudMaterial.SetVector("phaseParameters", new Vector4(forwardScattering, backScattering, basicBrightness, phaseFactor));
		cloudMaterial.SetInt("lightSteps", lightSteps);
	}
	private void OnEnable() {
		Shader cloudSh = Shader.Find("Unlit/CloudShader");
		if (cloudMaterial == null) cloudMaterial = new Material(cloudSh);
		cloudMaterial.SetTexture("CloudTexture", cloudTexture);
		cloudMaterial.SetFloat("densityThreshold", densityThreshold);
		cloudMaterial.SetFloat("darknessThreshold", darknessThreshold);
		cloudMaterial.SetVector("phaseParameters", new Vector4(forwardScattering, backScattering, basicBrightness, phaseFactor));
		cloudMaterial.SetInt("lightSteps", lightSteps);
	}

	private void Update()
	{
		var input = Input.inputString;
		switch (input)
		{
			case "1": 
				cw_densityThreshold = 1.0f;
				ci_densityThreshold = 0.0f;
				break;
			case "2":
				ci_densityThreshold = 1.0f;
				cw_densityThreshold = 0.0f;
				break;
			case "3":  
				break;
			case "4":
				break;
			default: break;
		} 
	}

    [ImageEffectOpaque] // is applied after rendering all opaque elements, but before transparent ones.
	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if (cloudMaterial == null) cloudMaterial = new Material(cloudShader);
		cloudMaterial.SetTexture("CloudTexture", cloudTexture);
		cloudMaterial.SetFloat("densityThreshold", densityThreshold);
		cloudMaterial.SetFloat("darknessThreshold", darknessThreshold);
		cloudMaterial.SetFloat("densityThreshold", densityThreshold);
		cloudMaterial.SetFloat("cw_densityThreshold", cw_densityThreshold);
		cloudMaterial.SetFloat("ci_densityThreshold", ci_densityThreshold);
		cloudMaterial.SetVector("phaseParameters", new Vector4(forwardScattering, backScattering, basicBrightness, phaseFactor));
		cloudMaterial.SetInt("lightSteps", lightSteps);
		Graphics.Blit(source, destination, cloudMaterial);
	}
}