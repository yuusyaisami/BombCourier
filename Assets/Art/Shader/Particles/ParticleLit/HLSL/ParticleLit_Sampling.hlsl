#ifndef BC_PARTICLES_PARTICLE_LIT_SAMPLING_INCLUDED
#define BC_PARTICLES_PARTICLE_LIT_SAMPLING_INCLUDED

float2 BC_ParticleLitBuildBaseUV(float2 uv)
{
    return uv * _BaseMap_ST.xy + _BaseMap_ST.zw;
}

half4 BC_ParticleLitSampleBase(float2 uv)
{
    return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
}

float3 BC_ParticleLitSampleNormalTS(float2 uv)
{
    float4 packedNormal = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv);
    return UnpackNormalScale(packedNormal, _NormalScale);
}

#endif // BC_PARTICLES_PARTICLE_LIT_SAMPLING_INCLUDED