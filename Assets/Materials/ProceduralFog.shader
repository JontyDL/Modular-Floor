Shader "Custom/ProceduralGroundFog"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0.78, 0.84, 0.88, 1)
        _Opacity ("Opacity", Range(0, 1)) = 0.42
        _NoiseScale ("Noise Scale", Range(0.02, 1)) = 0.12
        _DriftSpeed ("Drift Speed", Range(0, 1)) = 0.12
        _PatternStrength ("Pattern Strength", Range(0, 1)) = 0.18
        _VertexMotion ("Vertex Motion", Range(0, 5)) = 0.04
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "Fog"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float _Opacity;
                float _NoiseScale;
                float _DriftSpeed;
                float _PatternStrength;
                float _VertexMotion;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                f = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));

                return lerp(
                    lerp(a, b, f.x),
                    lerp(c, d, f.x),
                    f.y
                );
            }

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;

                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);

                float time = _Time.y * _DriftSpeed;

                float2 flow = float2(time, time * 0.63);

                float broadNoise = Noise(
                    worldPos.xz * _NoiseScale + flow
                );

                float fineNoise = Noise(
                    worldPos.xz * (_NoiseScale * 2.1)
                    - flow * 0.7
                );

                float combined = broadNoise * 0.75 + fineNoise * 0.25;

                // Subtle vertical motion; the visible drift comes from
                // moving the noise coordinates in the fragment shader.
                worldPos.y += (combined - 0.5) * _VertexMotion;

                OUT.positionWS = worldPos;
                OUT.positionHCS = TransformWorldToHClip(worldPos);

                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float time = _Time.y * _DriftSpeed;
                float2 p = IN.positionWS.xz;

                float2 flow = float2(time, time * 0.63);

                float broadNoise = Noise(p * _NoiseScale + flow);
                float fineNoise = Noise(
                    p * (_NoiseScale * 2.1) - flow * 0.7
                );

                float combined = broadNoise * 0.75 + fineNoise * 0.25;

                // Keep density variation small, avoiding hard cloudy patches.
                float density = lerp(
                    1.0 - _PatternStrength,
                    1.0 + _PatternStrength,
                    combined
                );

                float alpha = saturate(_Opacity * density);

                return half4(_FogColor.rgb, alpha * _FogColor.a);
            }
            ENDHLSL
        }
    }
}