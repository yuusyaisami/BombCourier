#ifndef BC_PARTICLES_TRAIL_UNLIT_INPUT_INCLUDED
#define BC_PARTICLES_TRAIL_UNLIT_INPUT_INCLUDED

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);
TEXTURE2D(_NoiseMap);
SAMPLER(sampler_NoiseMap);

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _NoiseMap_ST;
    half4 _BaseColor;
    half4 _EmissionColor;

    float4 _UVScrollSpeed;
    float4 _NoiseScrollSpeed;

    float _BlendMode;
    float _Alpha;
    float _Brightness;
    float _UseVertexColor;

    float _NoiseStrength;
    float _NoiseScale;
    float _DissolveAmount;
    float _DissolveSoftness;

    float _EdgeFadeAxis;
    float _EdgeFadePower;
    float _EdgeFadeStrength;
    float _EmissionStrength;
CBUFFER_END

#endif // BC_PARTICLES_TRAIL_UNLIT_INPUT_INCLUDED
