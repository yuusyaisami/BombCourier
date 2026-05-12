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

float ToyDiorama_CalculateDepthHazeMask(float linearDepth)
{
	float hazeStart = saturate(_ToyDioramaDepthHazeStart);
	float hazeEnd = max(saturate(_ToyDioramaDepthHazeEnd), hazeStart + 0.0001);
	return smoothstep(hazeStart, hazeEnd, saturate(linearDepth));
}

float ToyDiorama_Linear01DepthFromRaw(float rawDepth)
{
	return saturate(Linear01Depth(rawDepth, _ZBufferParams));
}

bool ToyDiorama_ShouldCaptureDepthDebugData()
{
	int debugView = (int)round(_ToyDioramaDebugView);
	return debugView == 17 ||
		debugView == 18 ||
		debugView == 19 ||
		debugView == 20 ||
		debugView == 21;
}

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