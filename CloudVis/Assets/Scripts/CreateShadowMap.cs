using UnityEngine;
using UnityEditor;
using System.IO;

[ExecuteInEditMode]
public class CreateShadowMap : MonoBehaviour
{
	public bool compute = false;
	public bool darken = false;

	public Texture3D clouds;
	public Transform lightt;

	public Texture2D coverage;
	public Texture2D albedo;

	Vector3 lowerLeft = new Vector3(-3752.0f, 0.0f, -3890.0f);


	// Start is called before the first frame update
	void Start()
	{
		
	}

	// Update is called once per frame
	void Update()
	{
		if (compute)
		{
			Vector2 step = new Vector2(7504.0f / 2048.0f, 7781.0f / 2048.0f);
			Vector3 dir = -lightt.transform.forward;
			float steplength = (200.0f / 150.0f) / dir.y;

			Texture2D shadowMap = new Texture2D(2048,2048,TextureFormat.RFloat, true);
			float[] coverages = new float[2048 * 2048];

			for(int i = 0; i < 2048; i++)
			{
				for(int j = 0; j < 2048; j++)
				{
					float total_shadow = 0.0f;

					Vector3 pos = lowerLeft;
					pos.x += i * step.x;
					pos.z += j * step.y;

					for(int s = 0; s < 150; s++) // should sample every cloud height-layer once.
					{
						Vector3 samplePoint = pos + steplength * dir * s;

						Vector3 cloudIndex = new Vector3((samplePoint.x - lowerLeft.x) / 7504.0f,
														 (samplePoint.z - lowerLeft.z) / 7781.0f,
														 (samplePoint.y - lowerLeft.y) / 198.17f);
						float coverage = clouds.GetPixelBilinear(cloudIndex.x, cloudIndex.y, cloudIndex.z).r;

						total_shadow += coverage;					 
                    }
					coverages[i*2048+j] = total_shadow;
				}
			}
					// write to texture...
					shadowMap.SetPixelData(coverages, 0);
					shadowMap.Apply();

			AssetDatabase.CreateAsset(shadowMap, "Assets/shadowMap.asset");

			compute = false;
		}
	
		if(darken)
        {
			Texture2D shadowed = new Texture2D(2048, 2048, TextureFormat.RGB24, true);

			float maxR = 0.0f;
			float avgR = 0.0f;
			for (int i = 0; i < 2048; i++)
			{
				for (int j = 0; j < 2048; j++)
				{
					float v = coverage.GetPixel(i, j).r;
					if (v > maxR) maxR = v;
					avgR += v;
				}
			}
			avgR /= (2048 * 2048);
			Debug.Log(maxR + " " + avgR);

			for (int i = 0; i < 2048; i++)
            {
				for(int j = 0; j < 2048; j++)
                {
					Color c = albedo.GetPixelBilinear((float)i / 2048.0f, (float)j / 2048.0f);
					float shadowing = coverage.GetPixel(i, j).r;
					shadowing /= (maxR-8.0f);
					shadowing = Mathf.Min(1.0f, shadowing+0.1f);
					shadowed.SetPixel(i, j, new Color(c.r * (1.0f - shadowing), c.g * (1.0f - shadowing), c.b * (1.0f - shadowing)));
                }
            }
			shadowed.Apply(true);
			shadowed.Compress(true);
			AssetDatabase.CreateAsset(shadowed, "Assets/Images/shadowed.asset");

			darken = false;
        }
	}
}
