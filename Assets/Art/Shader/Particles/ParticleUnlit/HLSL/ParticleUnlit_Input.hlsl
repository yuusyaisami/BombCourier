#ifndef BC_PARTICLES_PARTICLE_UNLIT_INPUT_INCLUDED
#define BC_PARTICLES_PARTICLE_UNLIT_INPUT_INCLUDED

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;

    float _BlendMode;
    float _Alpha;
    float _Brightness;
    float _UseVertexColor;
    float _SoftCircleStrength;
    float _EdgeFadePower;
    float _EdgeFadeStrength;
CBUFFER_END

#endif // BC_PARTICLES_PARTICLE_UNLIT_INPUT_INCLUDED