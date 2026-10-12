Shader "Custom/URP_LowPolyOcean_Final"
{
    Properties
    {
        _BaseColor ("Water Color", Color) = (0.0, 0.5, 0.7, 1.0)
        _WaveSpeed ("Wave Speed", Float) = 1.0
        _WaveScale ("Wave Scale / Frequency", Float) = 0.5
        _WaveHeight ("Wave Height / Amplitude", Float) = 0.2
        _Smoothness ("Smoothness", Range(0,1)) = 0.6
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Geometry" 
        }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Handle main light shadow variants automatically
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _WaveSpeed;
                float _WaveScale;
                float _WaveHeight;
                half _Smoothness;
                half _Metallic;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);     // world position of vertex

                float time = _Time.y * _WaveSpeed;
                float waveValue = sin(worldPos.x * _WaveScale + time) * 
                                  cos(worldPos.z * _WaveScale + time);      
                worldPos.y += waveValue * _WaveHeight;                              // using sin and cos wave to displace vertex position

                output.positionWS = worldPos;
                output.positionCS = TransformWorldToHClip(worldPos);
                
                return output;                                                      // update vertex coordinates
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Re-calculate normal vectors safely using screen-space
                float3 faceNormal = normalize(cross(ddy(input.positionWS), ddx(input.positionWS)));

                // Populate full Light Input structure
                InputData inputData = (InputData)0;
                inputData.normalWS = faceNormal;
                inputData.positionWS = input.positionWS;
                inputData.viewDirectionWS = SafeNormalize(GetCameraPositionWS() - input.positionWS);
                
                inputData.shadowCoord = float4(0, 0, 0, 0);         // remove shadows casted from other objects

                // Surface material data configuration
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = _BaseColor.rgb;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = float3(0, 0, 1); // Point tangent defaults up
                surfaceData.occlusion = 1.0;
                surfaceData.alpha = _BaseColor.a;

                // Process lighting math completely through the URP pipeline standard loop
                half4 color = UniversalFragmentPBR(inputData, surfaceData);

                return color;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}