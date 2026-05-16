#ifndef BC_PARTICLES_PARTICLE_DISTORTION_COMMON_INCLUDED
#define BC_PARTICLES_PARTICLE_DISTORTION_COMMON_INCLUDED

float BC_ParticleDistortionSafePositive(float value, float fallbackValue)
{
    return max(value, fallbackValue);
}

float BC_ParticleDistortionUsesPremultiply(float blendMode)
{
    return step(1.5, blendMode);
}

float2 BC_ParticleDistortionBuildSignedOffset(float4 distortionSample)
{
    return distortionSample.rg * 2.0 - 1.0;
}

float BC_ParticleDistortionBuildEdgeFade(float2 rawUV)
{
    float2 centeredUV = abs(rawUV * 2.0 - 1.0);
    float edgeMask = 1.0 - saturate(max(centeredUV.x, centeredUV.y));
    float softenedMask = pow(edgeMask, BC_ParticleDistortionSafePositive(_EdgeFadePower, 0.1));
    return lerp(1.0, softenedMask, saturate(_EdgeFadeStrength));
}

float BC_ParticleDistortionBuildAlpha(float mapAlpha, half4 vertexColor, float edgeFade)
{
    float alpha = saturate(_Alpha) * saturate(mapAlpha) * saturate(edgeFade);

    if (_UseVertexColor > 0.5)
    {
        alpha *= saturate(vertexColor.a);
    }

    return saturate(alpha);
}

#endif // BC_PARTICLES_PARTICLE_DISTORTION_COMMON_INCLUDED