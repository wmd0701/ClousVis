Shader "Unlit/CloudShader"
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
			// original max height: 198.170f
			static const float3 volumeBoundsMin = float3(-3752.0f, 2.0f*1.83f, -3890.5f);
			static const float3 volumeBoundsMax = float3(3752.0f, 2.0f*198.17f, 3890.5f);

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

			Texture3D<float4> CloudTexture;
			SamplerState samplerCloudTexture;

			sampler2D noiseTexture;

			sampler2D _MainTex;
			sampler2D _CameraDepthTexture;

			float4 phaseParameters;
			float darknessThreshold;
			int lightSteps;
			float ci_densityThreshold;
			float cw_densityThreshold;
			float qr_densityThreshold;
			float pres_densityThreshold;
			float densityThreshold;
			float4 _LightColor0;

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

			// Henyey-greenstein phase function
			float henyeygreenstein(float a, float g) {
				float g2 = g * g;
				return (1 - g2) / (4 * 3.1415f * pow(1 + g2 - 2 * g * (a), 1.5f));
			}

			float phase(float a) {
				float scatterBlend = henyeygreenstein(a, phaseParameters.x) * 0.5f + henyeygreenstein(a, -phaseParameters.y) * 0.5f;
				return phaseParameters.z + scatterBlend * phaseParameters.w;
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

			float sampleCloudDensity(float3 pos)
			{
				// calculate texture sample positions:
				const float3 boundsSize = volumeBoundsMax - volumeBoundsMin;
				float3 uvw = (pos - volumeBoundsMin) / boundsSize;

				float4 all_densities = CloudTexture.SampleLevel(samplerCloudTexture, uvw, 0);

				float density = 0.0f;
				density += 0.03f * ci_densityThreshold * all_densities.x;
				density = density < densityThreshold ? 0.0f : density;
				density += 0.03f * cw_densityThreshold * all_densities.y;
				density = density < densityThreshold ? 0.0f : density;
				density += qr_densityThreshold * all_densities.z;
				density = 0.1f * density < densityThreshold ? 0.0f : density;
				density += pres_densityThreshold * all_densities.w;
				density = density*10.0f < densityThreshold ? 0.0f : density;
				return density;
			}



			float lightmarch(float3 position) {
				float3 directionToLight = _WorldSpaceLightPos0.xyz;
				float distanceInsideVolume = rayVolumeDistance(position, 1.0f / directionToLight).y;

				float stepSize = distanceInsideVolume / lightSteps;

				float totalDensity = 0;

				for (int step = 0; step < lightSteps; step++) {
					position += directionToLight * stepSize;
					totalDensity += max(0, sampleCloudDensity(position) * stepSize);
				}

				return darknessThreshold + exp(-totalDensity) * (1.0f - darknessThreshold);
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
				float3 worldColor = tex2D(_MainTex, i.uv);
				if (distanceInsideVolume < 0.0001f) return float4(worldColor,0);

				// find max ray distance (distance to end of bounds or an object in the world.
				float expDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
				float depth = LinearEyeDepth(expDepth) * viewLength;
				float maxMarchingDistance = min(depth - distanceToVolume, distanceInsideVolume);

				float thickness = 0.0f;

				// do the actual raymarching
				float3 volumeStart = rayOrigin + rayDirection * distanceToVolume;
				static const float stepSize = 3.0f;
				float3 marchingPosition;
				float marchedDistance = 5.0f * tex2D(noiseTexture,i.uv);

				// angle to sun for halo effect
				float phaseangle = phase(dot(-rayDirection, _WorldSpaceLightPos0.xyz));

				float easingDistanceInv = 1/100.0f;
				

				float transmittance = 1.0f;
				float3 light = float3(0.0f,0.0f,0.0f);

				while (marchedDistance < maxMarchingDistance)
				{
					marchingPosition = volumeStart + marchedDistance * rayDirection;

					float density = sampleCloudDensity(marchingPosition);

					//float relativeDistance = length(marchingPosition - rayOrigin) * easingDistanceInv;
					//float easing = relativeDistance < 1.0f ? 0.1 * relativeDistance : 1.0f;
					// *easing;

					
					if (density > 0) {
						float lightInfluence = lightmarch(marchingPosition);
						light += density * stepSize * transmittance * lightInfluence * phaseangle;
						transmittance *= exp(-density * stepSize);
					}
					
					if (transmittance <= 0.01f) { // terminate early
						break;
					}

					marchedDistance += stepSize;
				}
				
				float3 cloudColor = light * _LightColor0;
				float3 finalColor = worldColor * transmittance + cloudColor;

				return float4(finalColor,0.0f);
			}
			ENDCG
		}
	}
}
