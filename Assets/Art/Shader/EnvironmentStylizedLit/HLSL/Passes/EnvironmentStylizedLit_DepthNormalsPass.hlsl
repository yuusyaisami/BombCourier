#ifndef BC_ENVIRONMENT_STYLIZED_LIT_DEPTH_NORMALS_PASS_INCLUDED
#define BC_ENVIRONMENT_STYLIZED_LIT_DEPTH_NORMALS_PASS_INCLUDED

#include "../EnvironmentStylizedLit_Input.hlsl"
#include "../EnvironmentStylizedLit_Common.hlsl"
#include "../EnvironmentStylizedLit_Sampling.hlsl"

struct ESL_DepthNormalsAttributes
{
	float4 positionOS : POSITION;
	float3 normalOS : NORMAL;
	float4 tangentOS : TANGENT;
	float2 uv : TEXCOORD0;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct ESL_DepthNormalsVaryings
{
	float2 uv : TEXCOORD0;
	float3 normalWS : TEXCOORD1;
	float4 tangentWS : TEXCOORD2;
	float3 positionWS : TEXCOORD3;
	float4 positionCS : SV_POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

// DepthNormals用頂点シェーダ。ワールド法線/接線を補間可能な形で渡します。
ESL_DepthNormalsVaryings ESL_DepthNormalsVertex(ESL_DepthNormalsAttributes input)
{
	ESL_DepthNormalsVaryings output = (ESL_DepthNormalsVaryings)0;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
	VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
	output.uv = input.uv;
	output.normalWS = normalInputs.normalWS;
	output.tangentWS = float4(normalInputs.tangentWS, input.tangentOS.w * GetOddNegativeScale());
	output.positionWS = positionInputs.positionWS;
	output.positionCS = positionInputs.positionCS;
	return output;
}

// URP/GBuffer設定に合わせて法線をエンコードします。
half4 ESL_EncodeDepthNormal(float3 normalWS)
{
	float3 normalizedNormalWS = NormalizeNormalPerPixel(normalWS);

	#if defined(_GBUFFER_NORMALS_OCT)
	float2 octNormalWS = PackNormalOctQuadEncode(normalizedNormalWS);
	float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
	return half4(PackFloat2To888(remappedOctNormalWS), 0.0);
	#else
	return half4(normalizedNormalWS, 0.0);
	#endif
}

// アルファクリップ考慮で法線を書き出すDepthNormalsフラグメントです。
half4 ESL_DepthNormalsFragment(ESL_DepthNormalsVaryings input, FRONT_FACE_TYPE facing : FRONT_FACE_SEMANTIC) : SV_TARGET
{
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float faceSign = ESL_GetFaceSign(facing);

	ESL_ApplyAlphaClipFromUV(input.uv);

	float3 normalWS = ESL_SampleSurfaceNormalWS(
		input.uv,
		input.positionWS,
		input.normalWS * faceSign,
		float4(input.tangentWS.xyz, input.tangentWS.w * faceSign));

	return ESL_EncodeDepthNormal(normalWS);
}

#endif