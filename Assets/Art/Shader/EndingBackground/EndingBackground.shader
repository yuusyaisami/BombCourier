// BC/EndingBackground
// URP unlit 背景 shader。UI Image の Material としても使えるよう、UI の描画設定と Mask/RectMask2D に対応する。
// 2 枚のノイズを独立スクロールし、合成結果を 3x3 の近似ガウスでぼかしてから 2 色グラデーション化する。
Shader "BC/EndingBackground"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

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

        // ---- Stencil (UI Mask 対応) ----
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "UniversalMaterialType" = "Unlit"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _ClipRect;
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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTexA);
            SAMPLER(sampler_NoiseTexA);
            TEXTURE2D(_NoiseTexB);
            SAMPLER(sampler_NoiseTexB);

            float Get2DClipping(float2 position, float4 clipRect)
            {
                float2 inside = step(clipRect.xy, position) * step(position, clipRect.zw);
                return inside.x * inside.y;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.worldPosition = input.positionOS;
                output.color = input.color * _Color;
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

                half4 color = lerp(_LowColor, _HighColor, normalized);
                color.a *= SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                color *= input.color;

#ifdef UNITY_UI_CLIP_RECT
                color.a *= Get2DClipping(input.worldPosition.xy, _ClipRect);
#endif
#ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
#endif
                return color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
