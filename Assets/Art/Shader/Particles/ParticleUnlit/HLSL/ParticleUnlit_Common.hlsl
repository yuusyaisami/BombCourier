#ifndef BC_PARTICLES_PARTICLE_UNLIT_COMMON_INCLUDED
#define BC_PARTICLES_PARTICLE_UNLIT_COMMON_INCLUDED

float BC_ParticleSafePositive(float value, float fallbackValue)
{
    return max(value, fallbackValue);
}

float BC_ParticleUsesPremultiply(float blendMode)
{
    return step(1.5, blendMode);
}

// M2 では四角い板感を抑えることだけを責務にし、複雑な shape mode は入れない。
float BC_ParticleCalculateSoftCircle(float2 uv, float softCircleStrength, float edgeFadePower, float edgeFadeStrength)
{
    float2 centeredUV = uv * 2.0 - 1.0;
    float distanceToCenter = length(centeredUV);
    float circleMask = 1.0 - saturate(distanceToCenter);
    float softCircle = pow(circleMask, BC_ParticleSafePositive(edgeFadePower, 0.001));
    float edgeFade = lerp(1.0, saturate(softCircle), saturate(edgeFadeStrength));
    return lerp(1.0, edgeFade, saturate(softCircleStrength));
}

#endif // BC_PARTICLES_PARTICLE_UNLIT_COMMON_INCLUDED