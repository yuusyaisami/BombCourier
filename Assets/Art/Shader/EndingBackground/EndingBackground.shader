// BC/EndingBackground
// Plane 向けの URP unlit 背景 shader。
// 2 枚のノイズを独立スクロールし、合成結果を 3x3 の近似ガウスでぼかしてから 2 色グラデーション化する。
Shader "BC/EndingBackground"
{
    Properties
    {
        _NoiseTexA ("Noise Texture A", 2D) = "white" {}
        _NoiseTexB ("Noise Texture B", 2D) = "white" {}

        _NoiseScaleA ("Noise Scale A", Float) = 1
        _NoiseScaleB ("Noise Scale B", Float) = 1
        _NoiseScrollA ("Noise Scroll A", Vector) = (0.02, 0.01, 0, 0)
        _NoiseScrollB ("Noise Scroll B", Vector) = (-0.01, 0.015, 0, 0)

        _NoiseBlendWeight ("Noise Blend Weight", Range(0, 1)) = 0.5
        _NoiseMultiplyStrength ("Noise Multiply Strength", Range(0, 2)) = 0.25
        _BlurRadius ("Blur Radius", Range(0, 0.1)) = 0.02

        _ValueMin ("Value Min", Range(0, 1)) = 0.2
        _ValueMax ("Value Max", Range(0, 1)) = 0.8
        _ValuePower ("Value Power", Range(0.1, 4)) = 1

        _LowColor ("Low Color", Color) = (0.2, 0.25, 0.32, 1)
        _HighColor ("High Color", Color) = (0.92, 0.9, 0.82, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "PreviewType" = "Plane"
            "UniversalMaterialType" = "Unlit"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _NoiseTexA_ST;
                float4 _NoiseTexB_ST;
                float4 _NoiseScrollA;
                float4 _NoiseScrollB;
                float4 _LowColor;
                float4 _HighColor;
                float _NoiseScaleA;
                float _NoiseScaleB;
                float _NoiseBlendWeight;
                float _NoiseMultiplyStrength;
                float _BlurRadius;
                float _ValueMin;
                float _ValueMax;
                float _ValuePower;
            CBUFFER_END

            TEXTURE2D(_NoiseTexA);
            SAMPLER(sampler_NoiseTexA);
            TEXTURE2D(_NoiseTexB);
            SAMPLER(sampler_NoiseTexB);

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            float2 RepeatUv(float2 uv)
            {
                return frac(uv);
            }

            float SampleNoiseA(float2 uv)
            {
                float2 noiseUv = TRANSFORM_TEX(uv, _NoiseTexA);
                noiseUv = RepeatUv(noiseUv * _NoiseScaleA + _Time.y * _NoiseScrollA.xy);
                return SAMPLE_TEXTURE2D(_NoiseTexA, sampler_NoiseTexA, noiseUv).r;
            }

            float SampleNoiseB(float2 uv)
            {
                float2 noiseUv = TRANSFORM_TEX(uv, _NoiseTexB);
                noiseUv = RepeatUv(noiseUv * _NoiseScaleB + _Time.y * _NoiseScrollB.xy);
                return SAMPLE_TEXTURE2D(_NoiseTexB, sampler_NoiseTexB, noiseUv).r;
            }

            float SampleCombinedNoise(float2 uv)
            {
                float noiseA = SampleNoiseA(uv);
                float noiseB = SampleNoiseB(uv);
                float blended = lerp(noiseA, noiseB, saturate(_NoiseBlendWeight));
                return saturate(blended + (noiseA * noiseB) * _NoiseMultiplyStrength);
            }

            float BlurCombinedNoise(float2 uv)
            {
                float2 stepUv = float2(_BlurRadius, _BlurRadius);

                float value = 0.0;
                value += SampleCombinedNoise(uv + stepUv * float2(-1.0, -1.0)) * 1.0;
                value += SampleCombinedNoise(uv + stepUv * float2(0.0, -1.0)) * 2.0;
                value += SampleCombinedNoise(uv + stepUv * float2(1.0, -1.0)) * 1.0;
                value += SampleCombinedNoise(uv + stepUv * float2(-1.0, 0.0)) * 2.0;
                value += SampleCombinedNoise(uv) * 4.0;
                value += SampleCombinedNoise(uv + stepUv * float2(1.0, 0.0)) * 2.0;
                value += SampleCombinedNoise(uv + stepUv * float2(-1.0, 1.0)) * 1.0;
                value += SampleCombinedNoise(uv + stepUv * float2(0.0, 1.0)) * 2.0;
                value += SampleCombinedNoise(uv + stepUv * float2(1.0, 1.0)) * 1.0;
                return value / 16.0;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float blurredValue = BlurCombinedNoise(input.uv);

                float lower = min(_ValueMin, _ValueMax);
                float upper = max(_ValueMin, _ValueMax);
                upper = max(upper, lower + 0.0001);

                float normalized = smoothstep(lower, upper, saturate(blurredValue));
                normalized = pow(saturate(normalized), max(0.0001, _ValuePower));

                half3 color = lerp(_LowColor.rgb, _HighColor.rgb, normalized);
                return half4(color, 1.0h);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
