#ifndef BC_PARTICLES_PARTICLE_UNLIT_FORWARD_PASS_INCLUDED
#define BC_PARTICLES_PARTICLE_UNLIT_FORWARD_PASS_INCLUDED

#include "../ParticleUnlit_Input.hlsl"
#include "../ParticleUnlit_Common.hlsl"
#include "../ParticleUnlit_Sampling.hlsl"
#include "../ParticleUnlit_Debug.hlsl"
#include "../ParticleUnlit_Surface.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    half4 color : COLOR;
    float2 uv : TEXCOORD0;
    float4 custom1 : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    half4 color : COLOR;
    float2 uv : TEXCOORD0;
    float4 custom1 : TEXCOORD1;
    float3 positionWS : TEXCOORD2;
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings ParticleUnlitVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.color = input.color;
    output.uv = input.uv;
    output.custom1 = input.custom1;
    output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    return output;
}

half4 ParticleUnlitFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 baseUV = BC_ParticleBuildBaseUV(input.uv);
    return BC_ParticleBuildSurfaceColor(baseUV, input.uv, input.color, input.custom1, input.positionWS, input.positionCS, _Time.y);
}

#endif // BC_PARTICLES_PARTICLE_UNLIT_FORWARD_PASS_INCLUDED