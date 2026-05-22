#ifndef BC_TOY_DIORAMA_DEPTH_HAZE_INCLUDED
#define BC_TOY_DIORAMA_DEPTH_HAZE_INCLUDED

struct ToyDiorama_DepthHazeData
{
	float3 beforeDepthHaze;
	float3 afterDepthHaze;
	float3 color;
	float rawDepth;
	float linearDepth;
	float mask;
};

// 深度が使えない/効果無効時に返すNo-Op用データです。
ToyDiorama_DepthHazeData ToyDiorama_CreateDepthHazeNoOp(float3 color)
{
	ToyDiorama_DepthHazeData data;
	data.beforeDepthHaze = color;
	data.afterDepthHaze = color;
	data.color = color;
	data.rawDepth = 0.0;
	data.linearDepth = 0.0;
	data.mask = 0.0;
	return data;
}

// 正規化線形深度をヘイズ合成マスクへ変換します。
float ToyDiorama_CalculateDepthHazeMask(float linearDepth)
{
	float hazeStart = saturate(_ToyDioramaDepthHazeStart);
	float hazeEnd = max(saturate(_ToyDioramaDepthHazeEnd), hazeStart + 0.0001);
	return smoothstep(hazeStart, hazeEnd, saturate(linearDepth));
}

// URPの深度変換ヘルパーを包み、clamp位置を一元化します。
float ToyDiorama_Linear01DepthFromRaw(float rawDepth)
{
	return saturate(Linear01Depth(rawDepth, _ZBufferParams));
}

// ヘイズ無効でも、特定デバッグ表示では深度サンプルが必要です。
bool ToyDiorama_ShouldCaptureDepthDebugData()
{
	int debugView = (int)round(_ToyDioramaDebugView);
	return debugView == 17 ||
		debugView == 18 ||
		debugView == 19 ||
		debugView == 20 ||
		debugView == 21;
}

// 距離ヘイズを適用し、デバッグ表示用の中間値も返します。
ToyDiorama_DepthHazeData ToyDiorama_ApplyDepthHaze(float3 color, float2 uv)
{
	if (_ToyDioramaDepthAvailable < 0.5)
	{
		return ToyDiorama_CreateDepthHazeNoOp(color);
	}

	bool shouldCaptureDepthDebugData = ToyDiorama_ShouldCaptureDepthDebugData();
	bool applyDepthHaze = _ToyDioramaDepthHazeEnabled >= 0.5 && _ToyDioramaDepthHazeStrength > 0.0;

	if (!applyDepthHaze && !shouldCaptureDepthDebugData)
	{
		return ToyDiorama_CreateDepthHazeNoOp(color);
	}

	ToyDiorama_DepthHazeData data;
	data.beforeDepthHaze = color;
	data.rawDepth = SampleSceneDepth(uv);
	data.linearDepth = ToyDiorama_Linear01DepthFromRaw(data.rawDepth);
	data.mask = ToyDiorama_CalculateDepthHazeMask(data.linearDepth);

	if (!applyDepthHaze)
	{
		data.afterDepthHaze = color;
		data.color = color;
		return data;
	}

	// ヘイズ量に応じて脱彩度→距離色へ寄せ、最後にわずかに明度を持ち上げます。
	float hazeStrength = data.mask * saturate(_ToyDioramaDepthHazeStrength);
	float saturation = 1.0 - hazeStrength * saturate(_ToyDioramaDepthHazeSaturationFade);
	float3 softenedColor = ToyDiorama_ApplySaturation(color, saturation);
	float3 hazeTarget = ToyDiorama_ColorizePreserveLuminance(softenedColor, _ToyDioramaDepthHazeColor.rgb);

	float3 hazedColor = lerp(softenedColor, hazeTarget, hazeStrength);
	hazedColor += (hazeStrength * saturate(_ToyDioramaDepthHazeBrightnessLift)).xxx;

	data.afterDepthHaze = saturate(hazedColor);
	data.color = data.afterDepthHaze;

	return data;
}

#endif