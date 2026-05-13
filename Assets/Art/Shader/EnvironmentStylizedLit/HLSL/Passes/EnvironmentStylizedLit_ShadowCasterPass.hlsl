#ifndef BC_ENVIRONMENT_STYLIZED_LIT_SHADOW_CASTER_PASS_INCLUDED
#define BC_ENVIRONMENT_STYLIZED_LIT_SHADOW_CASTER_PASS_INCLUDED

#include "../EnvironmentStylizedLit_Input.hlsl"
#include "../EnvironmentStylizedLit_Sampling.hlsl"

float3 _LightDirection;
float3 _LightPosition;

struct ESL_ShadowAttributes
{
	float4 positionOS : POSITION;
	float3 normalOS : NORMAL;
	float2 uv : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct ESL_ShadowVaryings
{
	float2 uv : TEXCOORD0;
	float3 positionWS : TEXCOORD1;
	float3 normalWS : TEXCOORD2;
	float4 positionCS : SV_POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

float4 ESL_GetShadowPositionHClip(ESL_ShadowAttributes input)
{
	float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
	float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

	#if _CASTING_PUNCTUAL_LIGHT_SHADOW
	float3 lightDirectionWS = normalize(_LightPosition - positionWS);
	#else
	float3 lightDirectionWS = _LightDirection;
	#endif

	float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
	return ApplyShadowClamping(positionCS);
}

ESL_ShadowVaryings ESL_ShadowPassVertex(ESL_ShadowAttributes input)
{
	ESL_ShadowVaryings output = (ESL_ShadowVaryings)0;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	output.uv = input.uv;
	output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
	output.normalWS = TransformObjectToWorldNormal(input.normalOS);
	output.positionCS = ESL_GetShadowPositionHClip(input);
	return output;
}

half4 ESL_ShadowPassFragment(ESL_ShadowVaryings input, FRONT_FACE_TYPE facing : FRONT_FACE_SEMANTIC) : SV_TARGET
{
	UNITY_SETUP_INSTANCE_ID(input);
	ESL_ApplyAlphaClipFromSurface(input.uv, input.positionWS, ESL_ApplyFaceSignToNormal(input.normalWS, facing));
	return 0;
}

#endif