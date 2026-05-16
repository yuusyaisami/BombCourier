#ifndef BC_PARTICLES_PARTICLE_LIT_FORWARD_PASS_INCLUDED
#define BC_PARTICLES_PARTICLE_LIT_FORWARD_PASS_INCLUDED

#include "../ParticleLit_Input.hlsl"
#include "../ParticleLit_Common.hlsl"
#include "../ParticleLit_Sampling.hlsl"
#include "../ParticleLit_Lighting.hlsl"
#include "../ParticleLit_Surface.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    half4 color : COLOR;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    float4 tangentWS : TEXCOORD2;
    half4 color : COLOR;
    float2 uv : TEXCOORD3;
    half fogFactor : TEXCOORD4;
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings ParticleLitVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.positionCS = positionInputs.positionCS;
    output.positionWS = positionInputs.positionWS;
    output.normalWS = normalInputs.normalWS;
    output.tangentWS = float4(normalInputs.tangentWS, input.tangentOS.w * GetOddNegativeScale());
    output.color = input.color;
    output.uv = input.uv;
    output.fogFactor = ComputeFogFactor(output.positionCS.z);
    return output;
}

half4 ParticleLitFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 baseUV = BC_ParticleLitBuildBaseUV(input.uv);
    half4 color = BC_ParticleLitBuildSurfaceColor(baseUV, input.color, input.normalWS, input.tangentWS, input.positionWS);
    color.rgb = MixFog(color.rgb, input.fogFactor);
    return color;
}

#endif // BC_PARTICLES_PARTICLE_LIT_FORWARD_PASS_INCLUDED