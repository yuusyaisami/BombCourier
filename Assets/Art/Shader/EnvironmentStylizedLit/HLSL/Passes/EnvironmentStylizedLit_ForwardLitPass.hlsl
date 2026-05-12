#ifndef BC_ENVIRONMENT_STYLIZED_LIT_FORWARD_LIT_PASS_INCLUDED
#define BC_ENVIRONMENT_STYLIZED_LIT_FORWARD_LIT_PASS_INCLUDED

#include "../EnvironmentStylizedLit_Input.hlsl"
#include "../EnvironmentStylizedLit_Common.hlsl"

struct ESL_Attributes
{
    float4 positionOS : POSITION;
};

struct ESL_Varyings
{
    float4 positionHCS : SV_POSITION;
};

ESL_Varyings ESL_Vertex(ESL_Attributes input)
{
    ESL_Varyings output;
    output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
    return output;
}

half4 ESL_Fragment(ESL_Varyings input) : SV_Target
{
    return half4(0.0h, 0.0h, 0.0h, 0.0h);
}

#endif