#ifndef BC_ENVIRONMENT_STYLIZED_LIT_FORWARD_LIT_PASS_INCLUDED
#define BC_ENVIRONMENT_STYLIZED_LIT_FORWARD_LIT_PASS_INCLUDED

#include "../EnvironmentStylizedLit_Input.hlsl"
#include "../EnvironmentStylizedLit_Common.hlsl"
#include "../EnvironmentStylizedLit_Surface.hlsl"

struct ESL_Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

struct ESL_Varyings
{
    float4 positionHCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    half fogFactor : TEXCOORD1;
};

ESL_Varyings ESL_Vertex(ESL_Attributes input)
{
    ESL_Varyings output;
    output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
    output.uv = input.uv;
    output.fogFactor = ComputeFogFactor(output.positionHCS.z);
    return output;
}

half4 ESL_Fragment(ESL_Varyings input) : SV_Target
{
    ESL_SurfaceData surfaceData = ESL_BuildSurfaceData(input.uv);
    ESL_ApplyAlphaClip(surfaceData.alpha);

    half3 color = half3(surfaceData.albedo + surfaceData.emission);
    color = MixFog(color, input.fogFactor);
    return half4(color, surfaceData.alpha);
}

#endif