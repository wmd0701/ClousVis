Shader "Unlit/TestShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float3 rayOrigin : TEXCOORD1;
				float3 rayDirection : TEXCOORD2;
			};

			sampler2D _MainTex;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.rayOrigin = _WorldSpaceCameraPos;

				float3 cameraViewDir = mul(unity_CameraInvProjection, float4(v.uv * 2 - 1, 0, -1));
				o.rayDirection = mul(unity_CameraToWorld, float4(cameraViewDir, 0));
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float3 rayOrigin = i.rayOrigin;
				float3 rayDirection = normalize(i.rayDirection);

				// sample the texture
				fixed4 col = 0;
				col.rb = float2(2.0f,2.0f)-i.uv;

				float2 relSize = i.uv * float2(1429.0f, 1556.0f);

				uint evennesy = (uint)relSize.y % 2;
				uint evennesx = (uint)relSize.x % 2;
				if (evennesx) { col.r = 0; }
				if (!evennesy) { col.b = 0; }

				return col;
			}
			ENDCG
		}
	}
}
