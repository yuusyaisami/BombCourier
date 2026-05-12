Shader "BC/PostProcess/ToyDioramaComposite"
{
    Properties
    {
        _ToyDioramaEnabled ("Enabled", Float) = 1
        _ToyDioramaQualityTier ("Quality Tier", Float) = 1
        _ToyDioramaDebugView ("Debug View", Float) = 0

        _ToyDioramaExposure ("Exposure", Range(-4, 4)) = 0
        _ToyDioramaContrast ("Contrast", Range(0, 3)) = 1
        _ToyDioramaSaturation ("Saturation", Range(0, 2)) = 1
        _ToyDioramaBlackLift ("Black Lift", Range(0, 1)) = 0.08
        _ToyDioramaWhiteSoftClamp ("White Soft Clamp", Range(0, 1)) = 0.25
        _ToyDioramaPastelStrength ("Pastel Strength", Range(0, 1)) = 0.30
        _ToyDioramaHighSaturationCompress ("High Saturation Compress", Range(0, 1)) = 0.50
        _ToyDioramaPastelLuminanceBias ("Pastel Luminance Bias", Range(-1, 1)) = 0.15

        _ToyDioramaShadowTint ("Shadow Tint", Color) = (0.46, 0.50, 0.68, 1)
        _ToyDioramaMidTint ("Mid Tint", Color) = (1, 1, 1, 1)
        _ToyDioramaHighlightTint ("Highlight Tint", Color) = (1, 0.95, 0.86, 1)
        _ToyDioramaCreamHighlightColor ("Cream Highlight Color", Color) = (1, 0.98, 0.93, 1)

        _ToyDioramaShadowTintStrength ("Shadow Tint Strength", Range(0, 1)) = 0.35
        _ToyDioramaMidTintStrength ("Mid Tint Strength", Range(0, 1)) = 0
        _ToyDioramaHighlightTintStrength ("Highlight Tint Strength", Range(0, 1)) = 0.20
        _ToyDioramaCreamHighlightStrength ("Cream Highlight Strength", Range(0, 1)) = 0.25
        _ToyDioramaCreamHighlightThreshold ("Cream Highlight Threshold", Range(0, 1)) = 0.70
        _ToyDioramaCreamHighlightSoftness ("Cream Highlight Softness", Range(0.001, 1)) = 0.10
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "ToyDioramaComposite"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment ToyDiorama_Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "HLSL/ToyDiorama_Input.hlsl"
            #include "HLSL/ToyDiorama_Common.hlsl"
            #include "HLSL/ToyDiorama_Pastel.hlsl"
            #include "HLSL/ToyDiorama_Highlight.hlsl"
            #include "HLSL/ToyDiorama_ColorGrade.hlsl"
            #include "HLSL/ToyDiorama_Debug.hlsl"

            half4 ToyDiorama_Fragment(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 source = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float3 beforeColorGrade = source.rgb;
                ToyDiorama_ColorPipelineData pipelineData = ToyDiorama_EvaluateColorPipeline(beforeColorGrade);
                float3 outputColor = ToyDiorama_ApplyDebugView(beforeColorGrade, pipelineData, uv);

                return half4(outputColor, source.a);
            }

            ENDHLSL
        }
    }

    FallBack Off
}