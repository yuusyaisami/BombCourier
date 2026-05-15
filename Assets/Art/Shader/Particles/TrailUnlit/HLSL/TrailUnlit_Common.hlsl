#ifndef BC_PARTICLES_TRAIL_UNLIT_COMMON_INCLUDED
#define BC_PARTICLES_TRAIL_UNLIT_COMMON_INCLUDED

float BC_TrailSafePositive(float value, float fallbackValue)
{
    return max(value, fallbackValue);
}

float2 BC_TrailScrollUV(float2 uv, float2 scrollSpeed, float timeSeconds)
{
    return uv + scrollSpeed * timeSeconds;
}

float BC_TrailSelectEdgeCoordinate(float2 uv, float edgeFadeAxis)
{
    return lerp(uv.x, uv.y, step(0.5, edgeFadeAxis));
}

// Trail renderers can flip the width direction depending on texture mode,
// so the axis is author-controlled instead of being hard-coded.
float BC_TrailCalculateEdgeFade(float2 uv, float edgeFadeAxis, float edgeFadePower, float edgeFadeStrength)
{
    float widthCoordinate = saturate(BC_TrailSelectEdgeCoordinate(uv, edgeFadeAxis));
    float edgeDistance = abs(widthCoordinate * 2.0 - 1.0);
    float centerMask = 1.0 - pow(edgeDistance, BC_TrailSafePositive(edgeFadePower, 0.001));
    return lerp(1.0, saturate(centerMask), saturate(edgeFadeStrength));
}

float BC_TrailCalculateDissolveMask(float noiseValue, float dissolveAmount, float dissolveSoftness)
{
    float amount = saturate(dissolveAmount);
    float softness = BC_TrailSafePositive(dissolveSoftness, 0.001);
    float threshold = amount * (1.0 + softness) - softness;
    return saturate((noiseValue - threshold) / softness);
}

bool BC_TrailUsesPremultiply(float blendMode)
{
    return blendMode > 1.5 && blendMode < 2.5;
}

#endif // BC_PARTICLES_TRAIL_UNLIT_COMMON_INCLUDED
