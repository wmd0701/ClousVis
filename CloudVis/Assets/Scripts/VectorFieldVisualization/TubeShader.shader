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
        StructuredBuffer<float> _SegmentLengthBuffer;
        StructuredBuffer<float> _MaxLengthBuffer;
        StructuredBuffer<float> _MinLengthBuffer;

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

            // Compute the global mininmal and maximal segment length.
            float globalMin = 100.0f;    // Should be large enough.
            float globalMax = 0.0f;
            for (uint i = 0; i < _StreamlineCount; i++) {
                globalMin = _MinLengthBuffer[i] < globalMin ? _MinLengthBuffer[i] : globalMin;
                globalMax = _MaxLengthBuffer[i] > globalMax ? _MaxLengthBuffer[i] : globalMax;
            }

            // The "time" at which to evaluate the gradient.
            float time = (_SegmentLengthBuffer[idx] - globalMin) / (globalMax - globalMin);

            // Clamping <time> to avoid coloring artifacts (shouldn't be necessary, just for security).
            time = clamp(time, 0.0f, 1.0f);

            // Ok now I know that there is an issue with the size of time.
            float interval = 1.0f / 7.0f;   // Size of interval enclosed by color knots in gradient.
            int idx0 = (int) (time / interval);
            int idx1 = idx0 + 1;
            half4 c1 = half4(_GradientColors[idx0], 0.0f);
            half4 c2 = half4(_GradientColors[idx1], 0.0f);
            float surplus = time - ((float) idx0) * interval;
            v.color = 7.0f * (interval - surplus) * c1 + 7.0f * surplus * c2;

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