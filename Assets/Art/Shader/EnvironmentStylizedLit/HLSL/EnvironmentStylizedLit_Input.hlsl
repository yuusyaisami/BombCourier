#ifndef BC_ENVIRONMENT_STYLIZED_LIT_INPUT_INCLUDED
#define BC_ENVIRONMENT_STYLIZED_LIT_INPUT_INCLUDED

CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
    float4 _BaseMap_ST;

    float _AlphaClip;
    float _Cutoff;
    float _Cull;

    float _NormalScale;

    float _OcclusionStrength;

    float4 _EmissionColor;
    float _EmissionStrength;

    float _Metallic;
    float _Smoothness;
    float4 _SpecularColor;
CBUFFER_END

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);

TEXTURE2D(_OcclusionMap);
SAMPLER(sampler_OcclusionMap);

TEXTURE2D(_EmissionMap);
SAMPLER(sampler_EmissionMap);

#endif