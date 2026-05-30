// BC/UI/NoiseOutlineWorld
// 3D オブジェクト用スクリーンスペース Noise Outline シェーダー。
// UINoiseOutlineRendererFeature の MaskPass / CompositePass で使用する。
// OutlineInvertedHull.shader と同じ 2-pass 構造 (Mask + Composite)。
Shader "BC/UI/NoiseOutlineWorld"
{
    Properties
    {
        _OutlineColor  ("Outline Color",       Color)      = (1,0.8,0,1)
        _OutlineWidth  ("Outline Width (px)",  Range(1,8)) = 3
        _OutlineSoftness ("Outline Softness",  Range(0,4)) = 1
        _NoiseScale    ("Noise Scale",         Range(1,30))= 6
        _NoiseSpeed    ("Noise Speed",         Range(0,5)) = 1.5
        _Intensity     ("Intensity",           Range(0,1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "Opaque"
        }

        // ---- Pass 0: Mask ----
        Pass
        {
            Name "NoiseOutlineWorldMask"

            Cull   Off
            ZWrite Off
            ZTest  LEqual
            Blend  Off

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _OutlineWidth;
                float  _OutlineSoftness;
                float  _NoiseScale;
                float  _NoiseSpeed;
                float  _Intensity;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionHCS : SV_POSITION; };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return half4(1.0h, 1.0h, 1.0h, 1.0h);
            }
            ENDHLSL
        }

        // ---- Pass 1: Composite (ノイズ変調アウトライン) ----
        Pass
        {
            Name "NoiseOutlineWorldComposite"

            Cull   Off
            ZWrite Off
            ZTest  Always
            Blend  SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #define NOISE_OUTLINE_MAX_RADIUS 8

            TEXTURE2D_X(_NoiseOutlineWorldMaskTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _OutlineWidth;
                float  _OutlineSoftness;
                float  _NoiseScale;
                float  _NoiseSpeed;
                float  _Intensity;
            CBUFFER_END

            float4 _NoiseOutlineWorldMaskTexelSize;

            // ---- Noise ----
            float hash2(float2 p)
            {
                p = frac(p * float2(443.8975, 397.2973));
                p += dot(p, p + 19.19);
                return frac(p.x * p.y);
            }
            float valueNoise2(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(hash2(i),               hash2(i + float2(1,0)), f.x),
                    lerp(hash2(i + float2(0,1)), hash2(i + float2(1,1)), f.x),
                    f.y);
            }

            float SampleMask(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_NoiseOutlineWorldMaskTex, sampler_LinearClamp, uv).r;
            }

            float EvaluateOutlineAlpha(float2 uv)
            {
                float centerMask = SampleMask(uv);
                if (centerMask > 0.5) return 0.0;

                float maxDist = 0.0;
                int radius = clamp((int)_OutlineWidth, 1, NOISE_OUTLINE_MAX_RADIUS);
                float2 texelSize = _NoiseOutlineWorldMaskTexelSize.xy;

                for (int x = -radius; x <= radius; x++)
                {
                    for (int y = -radius; y <= radius; y++)
                    {
                        float dist = length(float2(x, y));
                        if (dist > (float)radius) continue;
                        float2 offset = float2(x, y) * texelSize;
                        float m = SampleMask(uv + offset);
                        if (m > 0.5)
                            maxDist = max(maxDist, (float)radius - dist);
                    }
                }

                float rawAlpha = saturate(maxDist / max(_OutlineSoftness, 0.001));
                return rawAlpha;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float rawAlpha = EvaluateOutlineAlpha(uv);

                // ノイズで変調
                float t = _Time.y * _NoiseSpeed;
                float noise = valueNoise2(uv * _NoiseScale + float2(t, t * 0.7));
                float alpha = rawAlpha * (0.6 + 0.4 * noise) * _Intensity;

                half4 col = _OutlineColor;
                col.a *= alpha;
                return col;
            }
            ENDHLSL
        }
    }
}
