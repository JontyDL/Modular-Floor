Shader "Skybox/Star Twinkle"
{
    Properties
    {
        _Tint ("Tint Color", Color) = (1, 1, 1, 1)
        _Exposure ("Exposure", Range(0, 8)) = 1.0
        _Rotation ("Rotation", Range(0, 360)) = 0
        [NoScaleOffset] _Tex ("Star Cubemap (HDR)", Cube) = "black" {}

        [Header(Twinkle)]
        _TwinkleFraction ("Fraction Of Stars That Twinkle", Range(0, 1)) = 0.6
        _TwinkleAmount ("Twinkle Depth", Range(0, 1)) = 0.8
        _TwinkleSpeed ("Twinkle Speed", Range(0, 10)) = 2.5
        _TwinkleSharpness ("Twinkle Sharpness", Range(1, 8)) = 3
        _CellScale ("Star Group Scale", Range(10, 400)) = 120
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            samplerCUBE _Tex;
            half4 _Tex_HDR;
            half4 _Tint;
            half _Exposure;
            float _Rotation;
            float _TwinkleFraction, _TwinkleAmount, _TwinkleSpeed, _TwinkleSharpness, _CellScale;

            struct appdata { float4 vertex : POSITION; };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            float3 RotateAroundY(float3 v, float degrees)
            {
                float a = radians(degrees);
                float s, c;
                sincos(a, s, c);
                return float3(c * v.x - s * v.z, v.y, s * v.x + c * v.z);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.dir = RotateAroundY(v.vertex.xyz, _Rotation);
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            // Cheap 3D hash -> [0,1]
            float Hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.zyx + 31.32);
                return frac((p.x + p.y) * p.z);
            }

            // Smooth value noise in 3D. Smooth = every pixel of one star gets
            // (almost) the same value, so the whole star twinkles together.
            float ValueNoise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = Hash31(i + float3(0,0,0));
                float n100 = Hash31(i + float3(1,0,0));
                float n010 = Hash31(i + float3(0,1,0));
                float n110 = Hash31(i + float3(1,1,0));
                float n001 = Hash31(i + float3(0,0,1));
                float n101 = Hash31(i + float3(1,0,1));
                float n011 = Hash31(i + float3(0,1,1));
                float n111 = Hash31(i + float3(1,1,1));

                return lerp(lerp(lerp(n000, n100, f.x), lerp(n010, n110, f.x), f.y),
                            lerp(lerp(n001, n101, f.x), lerp(n011, n111, f.x), f.y), f.z);
            }

            half4 frag(v2f i) : SV_Target
            {
                float3 dir = normalize(i.dir);

                half4 tex = texCUBE(_Tex, dir);
                half3 col = DecodeHDR(tex, _Tex_HDR);

                // Three independent random values per region of sky.
                float3 p = dir * _CellScale;
                float rSelect = ValueNoise(p);
                float rPhase  = ValueNoise(p + 17.31);
                float rSpeed  = ValueNoise(p + 43.77);

                // Value noise clusters around 0.5, so stretch it to use the full range.
                rSelect = saturate((rSelect - 0.5) * 2.2 + 0.5);

                // 1 = this star is allowed to twinkle, 0 = steady star.
                float twinkles = smoothstep(1.0 - _TwinkleFraction - 0.08,
                                            1.0 - _TwinkleFraction + 0.08, rSelect);
                if (_TwinkleFraction >= 0.999) twinkles = 1.0;
                if (_TwinkleFraction <= 0.001) twinkles = 0.0;

                // Each twinkling star gets its own phase and speed.
                float t = _Time.y * _TwinkleSpeed * lerp(0.5, 1.5, rSpeed);
                float wave = 0.5 + 0.5 * sin(t + rPhase * 6.2831853);
                wave = pow(wave, _TwinkleSharpness);   // mostly bright, brief dips

                float twinkleBrightness = lerp(1.0 - _TwinkleAmount, 1.0, wave);
                float brightness = lerp(1.0, twinkleBrightness, twinkles);

                col *= brightness;
                col *= _Tint.rgb * unity_ColorSpaceDouble.rgb * _Exposure;
                return half4(col, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
