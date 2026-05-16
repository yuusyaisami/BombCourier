#ifndef BC_PARTICLES_PARTICLE_UNLIT_SAMPLING_INCLUDED
#define BC_PARTICLES_PARTICLE_UNLIT_SAMPLING_INCLUDED

half4 BC_ParticleSampleBase(float2 uv)
{
    return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
}

float2 BC_ParticleBuildBaseUV(float2 uv)
{
    return uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
}

#endif // BC_PARTICLES_PARTICLE_UNLIT_SAMPLING_INCLUDED