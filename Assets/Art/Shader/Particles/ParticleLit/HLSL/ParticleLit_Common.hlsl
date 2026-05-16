#ifndef BC_PARTICLES_PARTICLE_LIT_COMMON_INCLUDED
#define BC_PARTICLES_PARTICLE_LIT_COMMON_INCLUDED

float BC_ParticleLitSafePositive(float value, float fallbackValue)
{
    return max(value, fallbackValue);
}

float BC_ParticleLitUsesPremultiply(float blendMode)
{
    return step(1.5, blendMode);
}

float3 BC_ParticleLitSafeNormalize(float3 value, float3 fallback)
{
    float lengthSquared = dot(value, value);
    return lengthSquared > 1e-8 ? value * rsqrt(lengthSquared) : fallback;
}

float3 BC_ParticleLitBuildFallbackNormalWS(float3 positionWS)
{
    return BC_ParticleLitSafeNormalize(_WorldSpaceCameraPos.xyz - positionWS, float3(0.0, 0.0, 1.0));
}

float3x3 BC_ParticleLitBuildTangentToWorld(float3 normalWS, float4 tangentWS, float3 positionWS)
{
    float3 resolvedNormalWS = BC_ParticleLitSafeNormalize(normalWS, BC_ParticleLitBuildFallbackNormalWS(positionWS));
    float3 resolvedTangentWS = BC_ParticleLitSafeNormalize(tangentWS.xyz, float3(1.0, 0.0, 0.0));
    float tangentSign = abs(tangentWS.w) > 1e-4 ? tangentWS.w : 1.0;
    float3 resolvedBitangentWS = BC_ParticleLitSafeNormalize(cross(resolvedNormalWS, resolvedTangentWS) * tangentSign, float3(0.0, 1.0, 0.0));
    return float3x3(resolvedTangentWS, resolvedBitangentWS, resolvedNormalWS);
}

float3 BC_ParticleLitBuildNormalWS(float3 normalTS, float3 normalWS, float4 tangentWS, float3 positionWS)
{
    float3x3 tangentToWorld = BC_ParticleLitBuildTangentToWorld(normalWS, tangentWS, positionWS);
    return BC_ParticleLitSafeNormalize(mul(normalTS, tangentToWorld), BC_ParticleLitBuildFallbackNormalWS(positionWS));
}

#endif // BC_PARTICLES_PARTICLE_LIT_COMMON_INCLUDED