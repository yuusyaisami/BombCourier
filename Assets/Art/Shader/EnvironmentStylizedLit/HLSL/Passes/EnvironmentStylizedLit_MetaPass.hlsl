#ifndef BC_ENVIRONMENT_STYLIZED_LIT_META_PASS_INCLUDED
#define BC_ENVIRONMENT_STYLIZED_LIT_META_PASS_INCLUDED

#include "../EnvironmentStylizedLit_Input.hlsl"
#include "../EnvironmentStylizedLit_Sampling.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UniversalMetaPass.hlsl"

struct ESL_MetaAttributes
{
	float4 positionOS : POSITION;
	float3 normalOS : NORMAL;
	float2 uv0 : TEXCOORD0;
	float2 uv1 : TEXCOORD1;
	float2 uv2 : TEXCOORD2;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct ESL_MetaVaryings
{
	float4 positionCS : SV_POSITION;
	float2 uv : TEXCOORD0;
	float3 positionWS : TEXCOORD1;
	float3 normalWS : TEXCOORD2;
	#ifdef EDITOR_VISUALIZATION
	float2 VizUV : TEXCOORD3;
	float4 LightCoord : TEXCOORD4;
	#endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

// Metaパス頂点シェーダ。ベイク用アルベド/エミッション参照座標を構築します。
ESL_MetaVaryings ESL_MetaPassVertex(ESL_MetaAttributes input)
{
	ESL_MetaVaryings output = (ESL_MetaVaryings)0;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);

	output.positionCS = UnityMetaVertexPosition(input.positionOS.xyz, input.uv1, input.uv2);
	output.uv = input.uv0;
	output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
	output.normalWS = TransformObjectToWorldNormal(input.normalOS);
	#ifdef EDITOR_VISUALIZATION
	UnityEditorVizData(input.positionOS.xyz, input.uv0, input.uv1, input.uv2, output.VizUV, output.LightCoord);
	#endif
	return output;
}

// Metaパスフラグメント。ライトマップベイク向けにAlbedo/Emissionを返します。
half4 ESL_MetaPassFragment(ESL_MetaVaryings input) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(input);
	half4 baseColor = ESL_SampleBaseColor(input.uv, input.positionWS, input.normalWS);
	ESL_ApplyAlphaClip(baseColor.a);

	MetaInput metaInput = (MetaInput)0;
	#ifdef EDITOR_VISUALIZATION
	metaInput.VizUV = input.VizUV;
	metaInput.LightCoord = input.LightCoord;
	#endif
	metaInput.Albedo = baseColor.rgb;
	metaInput.Emission = ESL_SampleEmission(input.uv);
	return UnityMetaFragment(metaInput);
}

#endif