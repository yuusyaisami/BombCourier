#ifndef BC_PARTICLES_TRAIL_UNLIT_FORWARD_PASS_INCLUDED
#define BC_PARTICLES_TRAIL_UNLIT_FORWARD_PASS_INCLUDED

#include "../TrailUnlit_Input.hlsl"
#include "../TrailUnlit_Common.hlsl"
#include "../TrailUnlit_Sampling.hlsl"
#include "../TrailUnlit_Surface.hlsl"

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

Varyings TrailUnlitVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.color = input.color;
    output.uv = input.uv;
    return output;
}

half4 TrailUnlitFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 baseUV = BC_TrailBuildBaseUV(input.uv, _Time.y);
    return BC_TrailBuildSurfaceColor(baseUV, input.uv, input.color, _Time.y);
}

#endif // BC_PARTICLES_TRAIL_UNLIT_FORWARD_PASS_INCLUDED
