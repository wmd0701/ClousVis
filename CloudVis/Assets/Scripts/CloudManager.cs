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

	public enum Component{waterContent = 1, iceContent = 0, rainMixingRatio = 2, showAll = 3};
	[Header("Isosurface Settings")]
	public bool showIsosurface = false;

	public Component shownComponent = Component.showAll;


	[Range(0, 1)]
	public float isovalue = 0.3f;
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
		VisManager.shownComponent = (int)shownComponent;
		VisManager.visTexture = cloudTexture;
		VisManager.noiseTexture = noiseTexture;
		VisManager.showIsosurface = showIsosurface;
	}
	private void OnEnable() {
		Shader cloudSh = Shader.Find("Unlit/CloudShader");
		if (cloudMaterial == null) cloudMaterial = new Material(cloudSh);
		cloudMaterial.SetTexture("CloudTexture", cloudTexture);
		cloudMaterial.SetFloat("densityThreshold", densityThreshold);
		cloudMaterial.SetFloat("darknessThreshold", darknessThreshold);
		cloudMaterial.SetVector("phaseParameters", new Vector4(forwardScattering, backScattering, basicBrightness, phaseFactor));
		cloudMaterial.SetInt("lightSteps", lightSteps);
		VisManager.isovalue = isovalue;
		VisManager.shownComponent = (int)shownComponent;
		VisManager.visTexture = cloudTexture;
		VisManager.noiseTexture = noiseTexture;
		VisManager.showIsosurface = showIsosurface;
	}

	private void Update()
	{
		var input = Input.inputString;
		switch (input)
		{
			case "1":
				ci_densityThreshold = 1.0f - ci_densityThreshold;
				break;
			case "2":
				cw_densityThreshold = 1.0f - cw_densityThreshold;
				break;
			case "3":
				qr_densityThreshold = 1.0f - qr_densityThreshold;
				break;
			case "4":
				showWind = !showWind;
				break;
			case "5":
				showIsosurface = !showIsosurface;
				break;
			case "0":
				ci_densityThreshold = 0.0f;
				cw_densityThreshold = 0.0f;
				qr_densityThreshold = 0.0f;
				pres_densityThreshold = 0.0f;
				showWind = false;
				showIsosurface = false;
				break;
			default:
				VectorFieldVisualizer.enabled = showWind;
				break;
		}

		
		if (Input.GetKey("j"))
			isovalue -= isovalueStep;
		if (Input.GetKey("k"))
			isovalue += isovalueStep;
		if (Input.GetKey("m"))
			shownComponent = (Component)(((int)shownComponent + 1) % 4);

		isovalue = Mathf.Clamp(isovalue, 0.0f, 1.0f);
		VisManager.isovalue = isovalue;
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
		VisManager.isovalue = isovalue;
		VisManager.shownComponent = (int)shownComponent;
		VisManager.visTexture = cloudTexture;
		VisManager.noiseTexture = noiseTexture;
		VisManager.showIsosurface = showIsosurface;
	}
}