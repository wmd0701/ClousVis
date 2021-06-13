using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class CloudManager : MonoBehaviour
{
	public Shader cloudShader; // clouds are a post-process shader
	private Material cloudMaterial;

	public Texture3D cloudTexture;
	public Texture2D noiseTexture;

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
	public float ci_densityThreshold = 0.0f;
	[Range(0, 1)]
	public float cw_densityThreshold = 0.0f;
	[Range(0, 1)]
	public float qr_densityThreshold = 0.0f;
	[Range(0, 1)]
	public float pres_densityThreshold = 0.0f;

	public bool showWind = false;

	[Range(0, 1)]
	public float isovalue = 1.0f;
	public bool showIsolines = false;
	public float isovalueStep = 0.03f;

	// initialize the material on startup (editor as well as game mode)
	private void Start() { 
		if(cloudMaterial == null) cloudMaterial = new Material(cloudShader);
		cloudMaterial.SetTexture("CloudTexture", cloudTexture);
		cloudMaterial.SetFloat("darknessThreshold", darknessThreshold);
		cloudMaterial.SetFloat("densityThreshold", densityThreshold);
		cloudMaterial.SetVector("phaseParameters", new Vector4(forwardScattering, backScattering, basicBrightness, phaseFactor));
		cloudMaterial.SetInt("lightSteps", lightSteps);
		VectorFieldVisualizer.enabled = showWind;
		VisManager.isovalue = isovalue;
		VisManager.showIsolines = showIsolines;
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
				ci_densityThreshold = 1.0f;
				cw_densityThreshold = 0.0f;
				qr_densityThreshold = 0.0f;
				pres_densityThreshold = 0.0f;
				showWind = false;
				isovalue = 1.0f;
				showIsolines = false;
				break;
			case "2":
				ci_densityThreshold = 0.0f;
				cw_densityThreshold = 1.0f;
				qr_densityThreshold = 0.0f;
				pres_densityThreshold = 0.0f;
				showWind = false;
				isovalue = 1.0f;
				showIsolines = false;
				break;
			case "3":
				ci_densityThreshold = 0.0f;
				cw_densityThreshold = 0.0f;
				qr_densityThreshold = 1.0f;
				pres_densityThreshold = 0.0f;
				showWind = false;
				isovalue = 1.0f;
				showIsolines = false;
				break;
			case "4":
				ci_densityThreshold = 0.0f;
				cw_densityThreshold = 0.0f;
				qr_densityThreshold = 0.0f;
				pres_densityThreshold = 1.0f;
				showWind = false;
				isovalue = 1.0f;
				showIsolines = false;
				break;
			case "5":
				ci_densityThreshold = 0.0f;
				cw_densityThreshold = 0.0f;
				qr_densityThreshold = 0.0f;
				pres_densityThreshold = 0.0f;
				showWind = false;
				isovalue = 0.9f;
				showIsolines = true;
				break;
			case "6":
				ci_densityThreshold = 0.0f;
				cw_densityThreshold = 0.0f;
				qr_densityThreshold = 0.0f;
				pres_densityThreshold = 0.0f;
				showWind = true;
				isovalue = 1.0f;
				showIsolines = false;
				break;
			case "0":
				ci_densityThreshold = 0.0f;
				cw_densityThreshold = 0.0f;
				qr_densityThreshold = 0.0f;
				pres_densityThreshold = 0.0f;
				showWind = false;
				isovalue = 1.0f;
				showIsolines = false;
				break;
			default:
				VectorFieldVisualizer.enabled = showWind;
				VisManager.isovalue = isovalue;
				VisManager.showIsolines = showIsolines;
				break;
		}

		if (showIsolines) 
		{
			if (Input.GetKey("j"))
				isovalue -= isovalueStep;
			if (Input.GetKey("k"))
				isovalue += isovalueStep;

			isovalue = Mathf.Clamp(isovalue, 0.0f, 1.0f);
			VisManager.isovalue = isovalue;
		}
	}

    [ImageEffectOpaque] // is applied after rendering all opaque elements, but before transparent ones.
	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if (cloudMaterial == null) cloudMaterial = new Material(cloudShader);
		cloudMaterial.SetTexture("CloudTexture", cloudTexture);
		cloudMaterial.SetTexture("noiseTexture", noiseTexture);
		cloudMaterial.SetFloat("densityThreshold", densityThreshold);
		cloudMaterial.SetFloat("darknessThreshold", darknessThreshold);
		cloudMaterial.SetFloat("densityThreshold", densityThreshold);
		cloudMaterial.SetFloat("ci_densityThreshold", ci_densityThreshold);
		cloudMaterial.SetFloat("cw_densityThreshold", cw_densityThreshold);
		cloudMaterial.SetFloat("qr_densityThreshold", qr_densityThreshold);
		cloudMaterial.SetFloat("pres_densityThreshold", pres_densityThreshold);
		cloudMaterial.SetVector("phaseParameters", new Vector4(forwardScattering, backScattering, basicBrightness, phaseFactor));
		cloudMaterial.SetInt("lightSteps", lightSteps);
		Graphics.Blit(source, destination, cloudMaterial);
	}
}