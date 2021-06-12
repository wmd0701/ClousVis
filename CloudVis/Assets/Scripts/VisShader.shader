Shader "Unlit/VisShader"
{
	Properties
	{
		_MainTex ("SolidWorldImage", 2D) = "white" {}
	}
		SubShader
	{
		// disable culling and depth writing, but always test
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			// 1 unit = 100m. This way all rendering stays stable enough
			static const float3 volumeBoundsMin = float3(-3752.0f, 1.830f, -3890.5f);
			static const float3 volumeBoundsMax = float3(3752.0f, 198.170f, 3890.5f);

			struct appdata
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 rayOrigin : TEXCOORD1;
				float3 rayDirection : TEXCOORD2;
			};

			Texture3D VisTexture; // change to half
			SamplerState samplerVisTexture;

			sampler2D _MainTex;
			sampler2D _CameraDepthTexture;

			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.pos);
				o.uv = v.uv;
				o.rayOrigin = _WorldSpaceCameraPos;

				float3 cameraViewDir = mul(unity_CameraInvProjection, float4(v.uv * 2 - 1, 0, -1));
				o.rayDirection = mul(unity_CameraToWorld, float4(cameraViewDir, 0));
				return o;
			}


			float2 rayVolumeDistance(float3 rayOrigin, float3 invRayDirection)
			{
				// algorithm from: http://jcgt.org/published/0007/03/04/ and its adaption by Sebastian Lague (coding adventures clouds)
				float3 t0 = (volumeBoundsMin - rayOrigin) * invRayDirection;
				float3 t1 = (volumeBoundsMax - rayOrigin) * invRayDirection;
				float3 tmin = min(t0, t1);
				float3 tmax = max(t0, t1);

				float dstA = max(max(tmin.x, tmin.y), tmin.z);
				float dstB = min(tmax.x, min(tmax.y, tmax.z));

				// Case 1: ray intersects volume from outside (0 <= dstA <= dstB)
				// case 2: ray intersects box from inside (dstA < 0 < dstB)
				// case 3: ray misses box (dstA > dstB)

				float distanceToBox = max(0, dstA);
				float distanceInsideBox = max(0, dstB - distanceToBox);
				return float2(distanceToBox, distanceInsideBox);
			}

			/*
			float3 getGradient(float3 pos)
			{
				VisTexture.SampleLevel(samplerVisTexture, pos.xzy, 0)
			}
			*/

			float3 getTexPos(float3 pos)
			{
				const float3 boundsSize = volumeBoundsMax - volumeBoundsMin;
				float3 uvw = (pos - volumeBoundsMin) / boundsSize;
				uvw.y = 1.0f - uvw.y;
				return uvw;
			}

			float3 sampleCloudData(float3 pos)
			{
				float3 density = VisTexture.SampleLevel(samplerVisTexture, pos.xzy, 0);
				return density;
			}

			float sampleSphereDensity(float3 pos)
			{
				// a simple analytic sphere with constant density.
				float d = length(pos-float3(0.0f,50.0f,0.0f))-100.0f;
				if (d < 0.0) return 0.005f;
				else return 0.0f;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float3 rayOrigin = i.rayOrigin;
				float3 rayDirection = normalize(i.rayDirection);
				float viewLength = length(i.rayDirection); // is slightly different at edges due to rectangular frustum.
				
				// get the actual cloud-volume
				float2 volumeDistances = rayVolumeDistance(rayOrigin, 1/rayDirection);
				float distanceToVolume = volumeDistances.x;
				float distanceInsideVolume = volumeDistances.y;

				// terminate early if there is no cloud volume to raymarch
				float4 worldColor = tex2D(_MainTex, i.uv);
				if (distanceInsideVolume < 0.0001f) return worldColor;

				// find max ray distance (distance to end of bounds or an object in the world.
				float expDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
				float depth = LinearEyeDepth(expDepth) * viewLength;
				float maxMarchingDistance = min(depth - distanceToVolume, distanceInsideVolume);


				fixed4 cloudColor = worldColor;
				float3 cCloudWater = float3(0.0f, 0.0f, 0.0f);
				float thickness = 0.0f;
				int steps = 1;
				// do the actual raymarching
				float3 volumeStart = rayOrigin + rayDirection * distanceToVolume;
				static const float stepSize = 0.4f;
				float3 marchingPosition;
				float marchedDistance = 0.0f; // randomize a bit!

				float easingDistanceInv = 1/100.0f;

                float3 data;	// accumulate the sampled water content over all steps
				float3 sample;	// sampled water content at current step

				float threshold = 0.05f;
				bool showIsosurface = false;
				float inIso = 0.0f;
				float isoDistance = 200.0f;		// distance between isolines in meters

				// set colour values for different data types
				float3 cCLW = float3(216.0f, 218.0f, 231.0f)/255.0f;		// CLW colour
                float3 cCLI = float3(146.0f, 158.0f, 196.0f)/255.0f;		// CLI colour
                float3 cQR = float3(207.0f, 112.0f, 112.0f)/255.0f;			// QR colour

				float3 texPos;

                while (marchedDistance < maxMarchingDistance)
                {
                    marchingPosition = volumeStart + marchedDistance * rayDirection;

					texPos = getTexPos(marchingPosition);
                    sample = sampleCloudData(texPos);

                    // cut off lower values according to thresholds from paper
					// TODO: put this in texture generating script
                    sample.x = sample.x > 0.063f ? sample.x : 0.0f;
                    sample.y = sample.y > 0.148f ? sample.y : 0.0f;
                    sample.z = sample.z > 0.013f ? sample.z : 0.0f;

                    //data += sample;
					data += sample;

                    float density = sample.x + sample.y + sample.z;
                    thickness += density * 0.1f;

					if (showIsosurface && density > threshold) {
						inIso = 1.0f;
						break;
					}


                    if (!showIsosurface && thickness >= 1.0f) { // terminate early
						break;
					}

                    steps += 1;
                    marchedDistance += stepSize;
                }

				if (showIsosurface)
				{
					float3 cloud = float3(1.0f, 1.0f, 1.0f);
					float3 isoline = float3(1.0f, 0.2, 0.2f);
					float3 col;
					float meter = 1/(20000.0f-183.0f);
					col = fmod(texPos.y + 17.0f*meter, isoDistance*meter) > 0.002 ? cloud : isoline;
					//col = cloud;
					cloudColor.rgb = cloudColor.rgb*(1.0f-inIso) + inIso*col;
					//if ((uvw.y - 0.1f * floor(uvw.y/0.1f)) > 0.01) {cloudColor.rgb = float3(0.2f, 0.4f, 0.8f); }
				} else
				{
                	float totalWaterContent = (data.x + data.y + data.z);

					if (totalWaterContent > 0) { cCloudWater = (data.x * cCLW + data.y * cCLI + data.z * cQR) / totalWaterContent; }		// linear combination of CLW, CLI and QR colour
					cloudColor.rgb = cloudColor.rgb*(1.0f-thickness) + thickness*cCloudWater;
				}
				
				//cloudColor.rgb = totalWaterContent > threshold ? cCloudWater : worldColor.rgb;
				//cloudColor.rgb = cloudColor.rgb*(1.0f-thickness) + thickness*cCloudWater;

				return cloudColor;
			}
			ENDCG
		}
	}
}
