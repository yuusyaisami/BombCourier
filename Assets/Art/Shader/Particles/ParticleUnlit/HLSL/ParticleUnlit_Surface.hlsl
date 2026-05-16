#ifndef BC_PARTICLES_PARTICLE_UNLIT_SURFACE_INCLUDED
#define BC_PARTICLES_PARTICLE_UNLIT_SURFACE_INCLUDED

half4 BC_ParticleBuildSurfaceColor(float2 baseUV, float2 rawUV, half4 vertexColor)
{
    half4 color = BC_ParticleSampleBase(baseUV) * _BaseColor;

    UNITY_BRANCH
    if (_UseVertexColor > 0.5)
    {
        color *= vertexColor;
    }

    float alpha = color.a * saturate(_Alpha);
    alpha *= BC_ParticleCalculateSoftCircle(rawUV, _SoftCircleStrength, _EdgeFadePower, _EdgeFadeStrength);

    color.rgb *= max(_Brightness, 0.0);
    color.a = saturate(alpha);

    if (BC_ParticleUsesPremultiply(_BlendMode) > 0.5)
    {
        color.rgb *= color.a;
    }

    return color;
}

#endif // BC_PARTICLES_PARTICLE_UNLIT_SURFACE_INCLUDED