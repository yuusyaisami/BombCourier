#ifndef BC_ENVIRONMENT_STYLIZED_LIT_DEPTH_ONLY_PASS_INCLUDED
#define BC_ENVIRONMENT_STYLIZED_LIT_DEPTH_ONLY_PASS_INCLUDED

#include "../EnvironmentStylizedLit_Input.hlsl"
#include "../EnvironmentStylizedLit_Sampling.hlsl"

struct ESL_DepthOnlyAttributes
{
	float4 positionOS : POSITION;
	float3 normalOS : NORMAL;
	float2 uv : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct ESL_DepthOnlyVaryings
{
	float2 uv : TEXCOORD0;
	float3 positionWS : TEXCOORD1;
	float3 normalWS : TEXCOORD2;
	float4 positionCS : SV_POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

// DepthOnly頂点シェーダ。Depth出力に必要な座標とAlphaClip用情報を渡します。
ESL_DepthOnlyVaryings ESL_DepthOnlyVertex(ESL_DepthOnlyAttributes input)
{
	ESL_DepthOnlyVaryings output = (ESL_DepthOnlyVaryings)0;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	output.uv = input.uv;
	output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
	output.normalWS = TransformObjectToWorldNormal(input.normalOS);
	output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
	return output;
}

// トライプラナー対応のAlphaClipを行ったうえで深度を書き出します。
half ESL_DepthOnlyFragment(ESL_DepthOnlyVaryings input, FRONT_FACE_TYPE facing : FRONT_FACE_SEMANTIC) : SV_TARGET
{
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

	ESL_ApplyAlphaClipFromSurface(input.uv, input.positionWS, ESL_ApplyFaceSignToNormal(input.normalWS, facing));
	return input.positionCS.z;
}

#endif