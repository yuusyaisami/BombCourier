#ifndef BC_PARTICLES_PARTICLE_LIT_INPUT_INCLUDED
#define BC_PARTICLES_PARTICLE_LIT_INPUT_INCLUDED

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half4 _EmissionColor;

    float _BlendMode;
    float _Alpha;
    float _UseVertexColor;

    float _NormalScale;
    float _Smoothness;
    float _Metallic;
    float _LightInfluence;

    float _EmissionStrength;
CBUFFER_END

#endif // BC_PARTICLES_PARTICLE_LIT_INPUT_INCLUDED