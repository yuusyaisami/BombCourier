#ifndef BC_PARTICLES_TRAIL_UNLIT_SURFACE_INCLUDED
#define BC_PARTICLES_TRAIL_UNLIT_SURFACE_INCLUDED

half4 BC_TrailBuildSurfaceColor(float2 baseUV, float2 rawUV, half4 vertexColor, float timeSeconds)
{
    half4 color = BC_TrailSampleBase(baseUV) * _BaseColor;

    UNITY_BRANCH
    if (_UseVertexColor > 0.5)
    {
        color *= vertexColor;
    }

    float2 noiseUV = BC_TrailBuildNoiseUV(rawUV, timeSeconds);
    half noiseValue = BC_TrailSampleNoise(noiseUV);

    float alpha = color.a * saturate(_Alpha);
    alpha *= lerp(1.0, noiseValue, saturate(_NoiseStrength));
    alpha *= BC_TrailCalculateDissolveMask(noiseValue, _DissolveAmount, _DissolveSoftness);
    alpha *= BC_TrailCalculateEdgeFade(rawUV, _EdgeFadeAxis, _EdgeFadePower, _EdgeFadeStrength);

    color.a = saturate(alpha);
    color.rgb *= max(_Brightness, 0.0);
    color.rgb += _EmissionColor.rgb * max(_EmissionStrength, 0.0) * color.a;

    UNITY_BRANCH
    if (BC_TrailUsesPremultiply(_BlendMode))
    {
        color.rgb *= color.a;
    }

    return color;
}

#endif // BC_PARTICLES_TRAIL_UNLIT_SURFACE_INCLUDED
