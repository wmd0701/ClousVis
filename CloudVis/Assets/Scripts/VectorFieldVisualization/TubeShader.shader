Shader "StreamTubeShader"
{
    Properties
    {
        _Smoothness("Smoothness", Range(0, 1)) = 0
        _Metallic("Metallic", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        
        CGPROGRAM

        #pragma surface surf Standard vertex:vert addshadow nolightmap
        #pragma instancing_options procedural:setup

        struct Input
        {
            half4 param : COLOR;
        };

        half _Smoothness;
        half _Metallic;

        float _Radius;

        half3 _VolumeBoundaryMin;

        float4x4 _LocalToWorld;
        float4x4 _WorldToLocal;

        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED

        StructuredBuffer<float3> _StreamlineBuffer;
        StructuredBuffer<float3> _TangentBuffer;
        StructuredBuffer<float3> _NormalBuffer;
        StructuredBuffer<float3> _GradientColors;
        StructuredBuffer<float> _ExtremalLinesBuffer;

        uint _MaxStreamlineCount;
        uint _StreamlineCount;
        uint _IteratorSteps;
        uint _IndexOffset;

        #endif

        half3 SwapYZ(half3 vec) {
            return half3(vec.x, vec.z, vec.y);
        }

        void vert(inout appdata_full v, out Input data)
        {
            UNITY_INITIALIZE_OUTPUT(Input, data);

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED

            float angle = v.vertex.x; // Angle in slice
            float cap = v.vertex.y; // -1:head, +1:tail
            float seg = v.vertex.z; // Segment index 
            float param = seg / 1025.0f;
            // Index of the current slice in the buffers.
            uint idx = unity_InstanceID + _MaxStreamlineCount * seg;

            float3 p = SwapYZ(_StreamlineBuffer[idx]);  // Position
            float3 t = SwapYZ(_TangentBuffer[idx]);     // Curve-TNB: Tangent 
            float3 n = SwapYZ(_NormalBuffer[idx]);      // Curve-TNB: Normal
            float3 b = cross(t, n);             // Curve-TNB: Binormal

            float3 normal = n * cos(angle) + b * sin(angle); // Surface normal

            // Feedback the results.
            v.vertex = float4(p + normal * _Radius * (1 - abs(cap)), 1);
            v.normal = normal * (1 - abs(cap)) + n * cap;

            int idx0 = ((int) (param / (1.0f / 7.0f)));
            int idx1 = idx0 + 1;
            half4 c1 = half4(_GradientColors[idx0], 0.0f);
            half4 c2 = half4(_GradientColors[idx1], 0.0f);
            float surplus = param - ((float) idx0) * (1.0f / 7.0f);
            v.color = c1 * (1.0f - surplus) + c2 * surplus;

            #endif
        }

        void setup()
        {
            unity_ObjectToWorld = _LocalToWorld;
            unity_WorldToObject = _WorldToLocal;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = IN.param;
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
        }

        ENDCG
    }
    FallBack "Diffuse"
}