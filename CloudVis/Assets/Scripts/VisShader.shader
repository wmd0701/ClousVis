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
			static const float3 volumeBoundsMin = float3(-3752.0f, 2.0f*1.830f, -3890.5f);
			static const float3 volumeBoundsMax = float3(3752.0f, 2.0f*198.170f, 3890.5f);

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

			Texture3D VisTexture;
			SamplerState samplerVisTexture;

			sampler2D noiseTexture;

			bool showIsosurface;
			float isovalue;
			int shownComponent;

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
/*
			float sampleCloudDensity(float3 pos)
			{
				// calculate texture sample positions:
				const float3 boundsSize = volumeBoundsMax - volumeBoundsMin;
				float3 uvw = (pos - volumeBoundsMin) / boundsSize;

				float4 all_densities = VisTexture.SampleLevel(samplerVisTexture, uvw, 0);

				float density = cw_densityThreshold * all_densities.y;
				density = density < densityThreshold ? 0.0f : density;
				density += ci_densityThreshold * all_densities.x;
				density = density < densityThreshold ? 0.0f : density;
				// TODO same for water... how do we color these differently?
				return density;
			}
*/
			float sampleData(float3 pos)
			{
				const float3 boundsSize = volumeBoundsMax - volumeBoundsMin;
				float3 uvw = (pos - volumeBoundsMin) / boundsSize;	
				float3 density = VisTexture.SampleLevel(samplerVisTexture, uvw.xyz, 0);
				return density;
			}

			float3 getTexPos(float3 pos)
			{
				const float3 boundsSize = volumeBoundsMax - volumeBoundsMin;
				float3 uvw = (pos - volumeBoundsMin) / boundsSize;
				//uvw.y = 1.0f - uvw.y;
				return uvw;
			}

			float3 sampleCloudData(float3 pos)
			{
				float3 density = VisTexture.SampleLevel(samplerVisTexture, pos.xyz, 0);
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
				int steps = 1;
				// do the actual raymarching
				float3 volumeStart = rayOrigin + rayDirection * distanceToVolume;
				static const float stepSize = 0.8f;
				float3 marchingPosition = {0.0f, 0.0f, 0.0f};
				float marchedDistance = 5.0f * tex2D(noiseTexture, i.uv);

				// set colour values for different data types
				float3 cCLI = float3(148.0f, 158.0f, 148.0f)/255.0f;		// CLI colour
                float3 cCLW = float3(93.0f, 126.0f, 172.0f)/255.0f;		// CLW colour
                float3 cQR = float3(165.0f, 85.0f, 80.0f)/255.0f;			// QR colour

				float isoDistance = 200.0f;		// distance between isolines in meters
				float3 texPos;
				float3 sample = {0.0f, 0.0f, 0.0f};
				bool inIso = false;

				switch(shownComponent)
					{
						case(0):	// CLI
							while (marchedDistance < maxMarchingDistance)
							{
								marchingPosition = volumeStart + marchedDistance * rayDirection;
								texPos = getTexPos(marchingPosition);
								sample = sampleCloudData(texPos);
								//sample = sampleData(marchingPosition);
								steps += 1;
								marchedDistance += stepSize;
								if (sample.x > isovalue) {
									inIso = true;
									break;
								}
							}
							cloudColor.rgb = inIso ? cCLI : cloudColor.rgb;
							break;

						case(1):	// CLW
							while (marchedDistance < maxMarchingDistance)
							{
								marchingPosition = volumeStart + marchedDistance * rayDirection;
								texPos = getTexPos(marchingPosition);
								sample = sampleCloudData(texPos);
								//sample = sampleData(marchingPosition);
								steps += 1;
								marchedDistance += stepSize;
								if (sample.y > isovalue) {
									inIso = true;
									break;
								}
							}
							cloudColor.rgb = inIso ? cCLW : cloudColor.rgb;
							break;

						case(2):	// QR
							while (marchedDistance < maxMarchingDistance)
							{
								marchingPosition = volumeStart + marchedDistance * rayDirection;
								texPos = getTexPos(marchingPosition);
								sample = sampleCloudData(texPos);
								//sample = sampleData(marchingPosition);
								steps += 1;
								marchedDistance += stepSize;
								if (sample.z > isovalue) {
									inIso = true;
									break;
								}
							}
							cloudColor.rgb = inIso ? cQR : cloudColor.rgb;
							break;

						case(3):	// total water content
							float totalWater;
							while (marchedDistance < maxMarchingDistance)
							{
								marchingPosition = volumeStart + marchedDistance * rayDirection;
								texPos = getTexPos(marchingPosition);
								sample = sampleCloudData(texPos);
								//sample = sampleData(marchingPosition);
								steps += 1;
								marchedDistance += stepSize;
								totalWater = sample.x + sample.y + sample.z;
								if (totalWater / 2.0437f > isovalue) {		// normalize to max possible value in dataset (2.0437)
									inIso = true;
									break;
								}
							}
							cloudColor.rgb = inIso ? ((sample.x * cCLW + sample.y * cCLI + sample.z * cQR) / totalWater) : cloudColor.rgb;
							break;
						default:
							break;
					}


				

				// compute gradient vector
				//float3 g_x = sampleCloudData(float3(texPos.x + 1/1429, texPos.yz)) - sampleCloudData(float3(texPos.x - 1/1429, texPos.yz));
				//float3 g_y = sampleCloudData(float3(texPos.x, texPos.y + 1/1556, texPos.z)) - sampleCloudData(float3(texPos.x, texPos.y - 1/1556, texPos.z));
				//float3 g_z = sampleCloudData(float3(texPos.xy, texPos.z + 1/150)) - sampleCloudData(float3(texPos.xy, texPos.z - 1/150));
				//stepSize *= 100;

				float3 g_x = sampleCloudData(getTexPos(float3(marchingPosition.x + stepSize, marchingPosition.yz)))
							- sampleCloudData(getTexPos(float3(marchingPosition.x - stepSize, marchingPosition.yz)));
				float3 g_y = sampleCloudData(getTexPos(float3(marchingPosition.x, marchingPosition.y + stepSize, marchingPosition.z)))
							- sampleCloudData(getTexPos(float3(marchingPosition.x, marchingPosition.y - stepSize, marchingPosition.z)));
				float3 g_z = sampleCloudData(getTexPos(float3(marchingPosition.xy, marchingPosition.z + stepSize)))
							- sampleCloudData(getTexPos(float3(marchingPosition.xy, marchingPosition.z - stepSize)));


				float3 gradient = {0.0f, 0.0f, 0.0f};
				if (shownComponent != 3) {
					gradient = float3(g_x[shownComponent], g_y[shownComponent], g_z[shownComponent]);
				} else {
					float sum_x = g_x[0] + g_x[1] + g_x[2];
					float sum_y = g_y[0] + g_y[1] + g_y[2];
					float sum_z = g_z[0] + g_z[1] + g_z[2];
					gradient = float3(sum_x, sum_y, sum_z);
				}

				float3 surfaceNormal = normalize(gradient);
				float3 directionToLight = -normalize(_WorldSpaceLightPos0.xyz);
				float3 viewDirection = normalize(-rayDirection);
				
				float4 diff = saturate(dot(surfaceNormal, directionToLight));
				float3 reflect = normalize(2 * diff * surfaceNormal - directionToLight);
				float4 specular = pow(saturate(dot(reflect, viewDirection)), 12);

/*
				if (specular.y > 0.0f) {
					cloudColor.rgb = float3(1.0f, 0.0f, 0.0f);
				}
*/
				

				//cloudColor.rgb = cloudColor.rgb * gradient.z;
				//cloudColor.r = sampleCloudData(float3(texPos.x + 1/1429, texPos.yz)).r;
//				cloudColor.rgb = sampleCloudData(float3(texPos.x + 0.1, texPos.yz)).rgb - sampleCloudData(float3(texPos.x - 0.1, texPos.yz)).rgb;

				// if (g_z > 0.0f) {cloudColor.rgb = float3(1.0f, g_y, g_z);}

				cloudColor = inIso ? cloudColor + cloudColor * diff + specular : cloudColor;

				// if (showIsosurface)
					return cloudColor;
				// else
					// return fixed4(0.0f, 0.0f, 0.0f, 0.0f);
			}
			ENDCG
		}
	}
}
