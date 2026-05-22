#ifndef BC_ENVIRONMENT_STYLIZED_LIT_AMBIENT_INCLUDED
#define BC_ENVIRONMENT_STYLIZED_LIT_AMBIENT_INCLUDED

#include "EnvironmentStylizedLit_Input.hlsl"
#include "EnvironmentStylizedLit_Common.hlsl"

struct ESL_AmbientData
{
	float upWeight;
	float sideWeight;
	float downWeight;
	float3 directionalAmbient;
	float bounceFactor;
	float3 bounceColor;
};

// バウンス光方向を安全に正規化して返します。
float3 ESL_GetBounceDirectionWS()
{
	return ESL_SafeNormalize(_BounceDirection.xyz, float3(0.0, 1.0, 0.0));
}

// 法線Y成分から、上/側面/下の3色を重み付きで合成します。
float3 ESL_EvaluateDirectionalAmbient(float3 normalWS)
{
	float upWeight = saturate(normalWS.y);
	float downWeight = saturate(-normalWS.y);
	float sideWeight = saturate(1.0 - abs(normalWS.y));

	return (_AmbientTopColor.rgb * upWeight
		+ _AmbientSideColor.rgb * sideWeight
		+ _AmbientBottomColor.rgb * downWeight) * saturate(_AmbientStrength);
}

// バウンス方向との内積をラップ補正して、反射寄与係数を作ります。
float ESL_EvaluateBounceFactor(float3 normalWS)
{
	float bounceNdot = saturate(dot(normalWS, ESL_GetBounceDirectionWS()));
	return ESL_ComputeWrappedLight(bounceNdot, saturate(_BounceWrap));
}

// 間接光計算で使う補助データをまとめて評価します。
ESL_AmbientData ESL_EvaluateAmbientData(ESL_InputData inputData)
{
	ESL_AmbientData ambientData;
	ambientData.upWeight = saturate(inputData.normalWS.y);
	ambientData.downWeight = saturate(-inputData.normalWS.y);
	ambientData.sideWeight = saturate(1.0 - abs(inputData.normalWS.y));
	ambientData.directionalAmbient = ESL_EvaluateDirectionalAmbient(inputData.normalWS);
	ambientData.bounceFactor = ESL_EvaluateBounceFactor(inputData.normalWS);
	ambientData.bounceColor = _BounceColor.rgb * saturate(_BounceStrength) * ambientData.bounceFactor;
	return ambientData;
}

#endif