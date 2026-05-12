Shader "Hidden/BC/PostProcess/ToyDioramaBloom"
{
    Properties
    {
        [HideInInspector] _ToyDioramaBloomSourceTex ("Bloom Source", 2D) = "black" {}
        [HideInInspector] _ToyDioramaSoftBloomEnabled ("Soft Bloom Enabled", Float) = 1
        [HideInInspector] _ToyDioramaSoftBloomThreshold ("Soft Bloom Threshold", Range(0, 1)) = 0.82
        [HideInInspector] _ToyDioramaSoftBloomSoftKnee ("Soft Bloom Soft Knee", Range(0, 1)) = 0.18
        [HideInInspector] _ToyDioramaSoftBloomIntensity ("Soft Bloom Intensity", Range(0, 1)) = 0.14
        [HideInInspector] _ToyDioramaSoftBloomRadius ("Soft Bloom Radius", Range(0, 1)) = 0.65
        [HideInInspector] _ToyDioramaSoftBloomTint ("Soft Bloom Tint", Color) = (1, 0.96, 0.92, 1)
        [HideInInspector] _ToyDioramaHalationEnabled ("Halation Enabled", Float) = 1
        [HideInInspector] _ToyDioramaHalationStrength ("Halation Strength", Range(0, 1)) = 0.04
        [HideInInspector] _ToyDioramaHalationThreshold ("Halation Threshold", Range(0, 1)) = 0.88
        [HideInInspector] _ToyDioramaHalationRadius ("Halation Radius", Range(0, 1)) = 0.55
        [HideInInspector] _ToyDioramaHalationColor ("Halation Color", Color) = (1, 0.74, 0.62, 1)
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
    #include "HLSL/ToyDiorama_Common.hlsl"

    TEXTURE2D_X(_ToyDioramaBloomSourceTex);

    float4 _ToyDioramaSourceTexelSize;
    float _ToyDioramaSoftBloomEnabled;
    float _ToyDioramaSoftBloomThreshold;
    float _ToyDioramaSoftBloomSoftKnee;
    float _ToyDioramaSoftBloomIntensity;
    float _ToyDioramaSoftBloomRadius;
    float4 _ToyDioramaSoftBloomTint;
    float _ToyDioramaHalationEnabled;
    float _ToyDioramaHalationStrength;
    float _ToyDioramaHalationThreshold;
    float _ToyDioramaHalationRadius;
    float4 _ToyDioramaHalationColor;

    float ToyDiorama_BloomMask(float luminance)
    {
        if (_ToyDioramaSoftBloomEnabled < 0.5 || _ToyDioramaSoftBloomIntensity <= 0.0)
        {
            return 0.0;
        }

        float knee = max(_ToyDioramaSoftBloomSoftKnee, 0.001);
        float threshold = saturate(_ToyDioramaSoftBloomThreshold);
        return smoothstep(saturate(threshold - knee), saturate(threshold + knee), luminance);
    }

    float ToyDiorama_BloomBlurRadius()
    {
        return lerp(0.75, 2.75, max(saturate(_ToyDioramaSoftBloomRadius), saturate(_ToyDioramaHalationRadius)));
    }

    float ToyDiorama_HalationMask(float3 sourceColor)
    {
        if (_ToyDioramaHalationEnabled < 0.5 || _ToyDioramaHalationStrength <= 0.0)
        {
            return 0.0;
        }

        float knee = max(_ToyDioramaSoftBloomSoftKnee, 0.02);
        float luminance = ToyDiorama_Luminance(sourceColor);
        return smoothstep(
            saturate(_ToyDioramaHalationThreshold - knee),
            saturate(_ToyDioramaHalationThreshold + knee),
            luminance);
    }

    float3 ToyDiorama_SampleBox4(float2 uv, float2 texelSize, float radius)
    {
        float2 offset = texelSize * radius;
        float3 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-offset.x, -offset.y)).rgb;
        color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(offset.x, -offset.y)).rgb;
        color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(-offset.x, offset.y)).rgb;
        color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(offset.x, offset.y)).rgb;
        return color * 0.25;
    }

    float4 ToyDiorama_BlurAxis(float2 uv, float2 axis)
    {
        float2 texelOffset = axis * _ToyDioramaSourceTexelSize.xy * ToyDiorama_BloomBlurRadius();
        float4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv) * 0.4026;
        color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + texelOffset * 1.38461538) * 0.2442;
        color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - texelOffset * 1.38461538) * 0.2442;
        color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + texelOffset * 3.23076923) * 0.0545;
        color += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - texelOffset * 3.23076923) * 0.0545;
        return color;
    }

    half4 ToyDioramaBloom_PrefilterFragment(Varyings input) : SV_Target
    {
        float3 sourceColor = ToyDiorama_SampleBox4(input.texcoord, _ToyDioramaSourceTexelSize.xy, ToyDiorama_BloomBlurRadius());
        float luminance = ToyDiorama_Luminance(sourceColor);
        float bloomMask = ToyDiorama_BloomMask(luminance);
        float halationMask = ToyDiorama_HalationMask(sourceColor);
        return half4(sourceColor * bloomMask, saturate(luminance * halationMask));
    }

    half4 ToyDioramaBloom_BlurHorizontalFragment(Varyings input) : SV_Target
    {
        return half4(ToyDiorama_BlurAxis(input.texcoord, float2(1.0, 0.0)));
    }

    half4 ToyDioramaBloom_BlurVerticalFragment(Varyings input) : SV_Target
    {
        return half4(ToyDiorama_BlurAxis(input.texcoord, float2(0.0, 1.0)));
    }

    half4 ToyDioramaBloom_CompositeFragment(Varyings input) : SV_Target
    {
        float4 blurredColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
        float3 sourceColor = SAMPLE_TEXTURE2D_X(_ToyDioramaBloomSourceTex, sampler_LinearClamp, input.texcoord).rgb;
        float bloomEnabled = step(0.5, _ToyDioramaSoftBloomEnabled) * step(0.0001, _ToyDioramaSoftBloomIntensity);
        float halationEnabled = step(0.5, _ToyDioramaHalationEnabled) * step(0.0001, _ToyDioramaHalationStrength);

        float3 bloomColor = blurredColor.rgb * _ToyDioramaSoftBloomTint.rgb * saturate(_ToyDioramaSoftBloomIntensity) * bloomEnabled;
        float halationScale = lerp(0.75, 1.35, saturate(_ToyDioramaHalationRadius));
        float halationSignal = saturate(blurredColor.a) * halationEnabled;
        float3 halationColor = _ToyDioramaHalationColor.rgb * saturate(_ToyDioramaHalationStrength) * halationSignal * halationScale;

        return half4(bloomColor + halationColor, halationSignal);
    }
    ENDHLSL

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
            Name "ToyDioramaBloomPrefilter"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment ToyDioramaBloom_PrefilterFragment

            ENDHLSL
        }

        Pass
        {
            Name "ToyDioramaBloomBlurHorizontal"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment ToyDioramaBloom_BlurHorizontalFragment

            ENDHLSL
        }

        Pass
        {
            Name "ToyDioramaBloomBlurVertical"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment ToyDioramaBloom_BlurVerticalFragment

            ENDHLSL
        }

        Pass
        {
            Name "ToyDioramaBloomComposite"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment ToyDioramaBloom_CompositeFragment

            ENDHLSL
        }
    }

    FallBack Off
}