Shader "BC/PostProcess/ToyDioramaComposite"
{
    Properties
    {
        [HideInInspector] _ToyDioramaBloomTex ("Bloom", 2D) = "black" {}
        _ToyDioramaBlueNoiseTex ("Blue Noise", 2D) = "gray" {}
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
        _ToyDioramaEdgeToneColor ("Edge Tone Color", Color) = (1, 0.98, 0.95, 1)
        _ToyDioramaDepthHazeColor ("Depth Haze Color", Color) = (0.78, 0.86, 0.92, 1)

        _ToyDioramaShadowTintStrength ("Shadow Tint Strength", Range(0, 1)) = 0.35
        _ToyDioramaMidTintStrength ("Mid Tint Strength", Range(0, 1)) = 0
        _ToyDioramaHighlightTintStrength ("Highlight Tint Strength", Range(0, 1)) = 0.20
        _ToyDioramaCreamHighlightStrength ("Cream Highlight Strength", Range(0, 1)) = 0.25
        _ToyDioramaCreamHighlightThreshold ("Cream Highlight Threshold", Range(0, 1)) = 0.70
        _ToyDioramaCreamHighlightSoftness ("Cream Highlight Softness", Range(0.001, 1)) = 0.10
        _ToyDioramaEdgeToneEnabled ("Edge Tone Enabled", Float) = 1
        _ToyDioramaEdgeToneStrength ("Edge Tone Strength", Range(0, 1)) = 0.12
        _ToyDioramaEdgeToneRadius ("Edge Tone Radius", Range(0, 1)) = 0.62
        _ToyDioramaEdgeToneSoftness ("Edge Tone Softness", Range(0.001, 1)) = 0.22
        _ToyDioramaEdgeSaturationFade ("Edge Saturation Fade", Range(0, 1)) = 0.18
        _ToyDioramaEdgeBrightnessOffset ("Edge Brightness Offset", Range(-0.5, 0.5)) = 0
        _ToyDioramaDepthHazeEnabled ("Depth Haze Enabled", Float) = 1
        [HideInInspector] _ToyDioramaDepthAvailable ("Depth Available", Float) = 0
        _ToyDioramaDepthHazeStrength ("Depth Haze Strength", Range(0, 1)) = 0.10
        _ToyDioramaDepthHazeStart ("Depth Haze Start", Range(0, 1)) = 0.45
        _ToyDioramaDepthHazeEnd ("Depth Haze End", Range(0, 1)) = 0.95
        _ToyDioramaDepthHazeSaturationFade ("Depth Haze Saturation Fade", Range(0, 1)) = 0.18
        _ToyDioramaDepthHazeBrightnessLift ("Depth Haze Brightness Lift", Range(0, 0.5)) = 0.04
        _ToyDioramaSoftBloomEnabled ("Soft Bloom Enabled", Float) = 1
        _ToyDioramaSoftBloomThreshold ("Soft Bloom Threshold", Range(0, 1)) = 0.82
        _ToyDioramaSoftBloomSoftKnee ("Soft Bloom Soft Knee", Range(0, 1)) = 0.18
        _ToyDioramaSoftBloomIntensity ("Soft Bloom Intensity", Range(0, 1)) = 0.14
        _ToyDioramaSoftBloomRadius ("Soft Bloom Radius", Range(0, 1)) = 0.65
        _ToyDioramaSoftBloomTint ("Soft Bloom Tint", Color) = (1, 0.96, 0.92, 1)
        _ToyDioramaHalationEnabled ("Halation Enabled", Float) = 1
        _ToyDioramaHalationStrength ("Halation Strength", Range(0, 1)) = 0.04
        _ToyDioramaHalationThreshold ("Halation Threshold", Range(0, 1)) = 0.88
        _ToyDioramaHalationRadius ("Halation Radius", Range(0, 1)) = 0.55
        _ToyDioramaHalationColor ("Halation Color", Color) = (1, 0.74, 0.62, 1)
        _ToyDioramaGrainEnabled ("Grain Enabled", Float) = 1
        _ToyDioramaGrainStrength ("Grain Strength", Range(0, 0.2)) = 0.02
        _ToyDioramaGrainScale ("Grain Scale", Range(0.25, 8)) = 1
        _ToyDioramaGrainResponse ("Grain Response", Range(0, 1)) = 0.60
        _ToyDioramaGrainTemporalStrength ("Grain Temporal Strength", Range(0, 1)) = 0.10
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
            Name "ToyDioramaPreBloom"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment ToyDiorama_PreBloomFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "HLSL/ToyDiorama_Input.hlsl"
            #include "HLSL/ToyDiorama_Common.hlsl"
            #include "HLSL/ToyDiorama_Pastel.hlsl"
            #include "HLSL/ToyDiorama_Highlight.hlsl"
            #include "HLSL/ToyDiorama_DepthHaze.hlsl"
            #include "HLSL/ToyDiorama_EdgeTone.hlsl"
            #include "HLSL/ToyDiorama_Grain.hlsl"
            #include "HLSL/ToyDiorama_ColorGrade.hlsl"
            #include "HLSL/ToyDiorama_Debug.hlsl"

            half4 ToyDiorama_PreBloomFragment(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 source = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float3 beforeColorGrade = source.rgb;
                ToyDiorama_ColorPipelineData pipelineData = ToyDiorama_EvaluatePreBloomPipeline(beforeColorGrade, uv);
                float3 outputColor = ToyDiorama_ApplyPreBloomDebugView(beforeColorGrade, pipelineData, uv);

                return half4(outputColor, 1.0);
            }

            ENDHLSL
        }

        Pass
        {
            Name "ToyDioramaFinalComposite"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment ToyDiorama_FinalCompositeFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "HLSL/ToyDiorama_Input.hlsl"
            #include "HLSL/ToyDiorama_Common.hlsl"
            #include "HLSL/ToyDiorama_Pastel.hlsl"
            #include "HLSL/ToyDiorama_Highlight.hlsl"
            #include "HLSL/ToyDiorama_DepthHaze.hlsl"
            #include "HLSL/ToyDiorama_EdgeTone.hlsl"
            #include "HLSL/ToyDiorama_Grain.hlsl"
            #include "HLSL/ToyDiorama_ColorGrade.hlsl"
            #include "HLSL/ToyDiorama_Debug.hlsl"

            TEXTURE2D_X(_ToyDioramaBloomTex);

            half4 ToyDiorama_FinalCompositeFragment(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 preBloom = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                if (ToyDiorama_IsPreBloomDebugView((int)round(_ToyDioramaDebugView)))
                {
                    return half4(preBloom.rgb, 1.0);
                }

                half4 bloom = SAMPLE_TEXTURE2D_X(_ToyDioramaBloomTex, sampler_LinearClamp, uv);
                ToyDiorama_FinalColorPipelineData pipelineData = ToyDiorama_EvaluateFinalColorPipeline(preBloom.rgb, bloom, uv);
                float3 outputColor = ToyDiorama_ApplyFinalDebugView(preBloom.rgb, pipelineData);

                return half4(outputColor, 1.0);
            }

            ENDHLSL
        }
    }

    FallBack Off
}