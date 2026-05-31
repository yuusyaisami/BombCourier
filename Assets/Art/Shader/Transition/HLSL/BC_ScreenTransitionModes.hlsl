#ifndef BC_SCREEN_TRANSITION_MODES_INCLUDED
#define BC_SCREEN_TRANSITION_MODES_INCLUDED

inline float BC_EvaluateTransitionT(float t, int mode)
{
    t = saturate(t);

    if (mode == 1)
        return t * t * (3.0 - 2.0 * t);

    return t;
}

inline float BC_EvaluateDirectionalWipe(float2 uv, float t, float2 direction, float feather)
{
    float2 dir = direction;
    float lenSq = dot(dir, dir);
    if (lenSq < 0.0001)
        dir = float2(1.0, 0.0);
    else
        dir *= rsqrt(lenSq);

    float coord = dot((uv - 0.5) * 2.0, dir);
    float threshold = lerp(-1.0, 1.0, saturate(t));
    float edge = max(0.0001, feather);
    return smoothstep(threshold - edge, threshold + edge, coord);
}

inline float BC_EvaluateRadialWipe(float2 uv, float t, float2 center, float aspect, float feather)
{
    float2 delta = uv - center;
    delta.x *= max(0.0001, aspect);
    float dist = length(delta) * 1.41421356;
    float edge = max(0.0001, feather);
    return smoothstep(saturate(t) - edge, saturate(t) + edge, dist);
}

inline float BC_EvaluateBlueNoiseDissolve(
    float2 uv,
    float t,
    float feather,
    float noise,
    float noiseStrength)
{
    float edge = max(0.0001, feather);
    float adjustedT = saturate(t + (noise - 0.5) * saturate(noiseStrength));
    return smoothstep(noise - edge, noise + edge, adjustedT);
}

#endif
