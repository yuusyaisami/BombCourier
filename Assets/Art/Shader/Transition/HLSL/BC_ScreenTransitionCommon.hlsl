#ifndef BC_SCREEN_TRANSITION_COMMON_INCLUDED
#define BC_SCREEN_TRANSITION_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

TEXTURE2D_X(_TextureFrom);
TEXTURE2D_X(_TextureTo);

CBUFFER_START(UnityPerMaterial)
    float _Progress;
    int _Mode;
    float _UseExplicitTo;
    float4 _FromUvScaleOffset;
    float4 _ToUvScaleOffset;
    float _Feather;
    float _NoiseScale;
    float _NoiseStrength;
    float _Seed;
    float4 _Direction;
    float4 _Center;
    float _Aspect;
CBUFFER_END

inline float2 BC_TransformUv(float2 uv, float4 scaleOffset)
{
    return uv * scaleOffset.xy + scaleOffset.zw;
}

inline half4 SampleFrom(float2 uv)
{
    float2 transformedUv = BC_TransformUv(uv, _FromUvScaleOffset);
    return SAMPLE_TEXTURE2D_X(_TextureFrom, sampler_LinearClamp, transformedUv);
}

inline half4 SampleTo(float2 uv)
{
    float2 transformedUv = BC_TransformUv(uv, _ToUvScaleOffset);

    if (_UseExplicitTo > 0.5)
        return SAMPLE_TEXTURE2D_X(_TextureTo, sampler_LinearClamp, transformedUv);

    return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, transformedUv);
}

#endif
