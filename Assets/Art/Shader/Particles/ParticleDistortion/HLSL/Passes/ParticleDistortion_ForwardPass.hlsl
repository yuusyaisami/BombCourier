#ifndef BC_PARTICLES_PARTICLE_DISTORTION_FORWARD_PASS_INCLUDED
#define BC_PARTICLES_PARTICLE_DISTORTION_FORWARD_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

#include "../ParticleDistortion_Input.hlsl"
#include "../ParticleDistortion_Common.hlsl"
#include "../ParticleDistortion_Sampling.hlsl"
#include "../ParticleDistortion_Surface.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    half4 color : COLOR;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    half4 color : COLOR;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings ParticleDistortionVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
    output.positionCS = positionInputs.positionCS;
    output.color = input.color;
    output.uv = input.uv;
    return output;
}

half4 ParticleDistortionFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    return BC_ParticleDistortionBuildSurfaceColor(input.uv, input.color, input.positionCS, _Time.y);
}

#endif // BC_PARTICLES_PARTICLE_DISTORTION_FORWARD_PASS_INCLUDED