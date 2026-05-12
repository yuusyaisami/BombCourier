#ifndef BC_TOY_DIORAMA_EDGE_TONE_INCLUDED
#define BC_TOY_DIORAMA_EDGE_TONE_INCLUDED

struct ToyDiorama_EdgeToneData
{
	float3 beforeEdgeTone;
	float3 afterEdgeTone;
	float3 color;
	float mask;
};

ToyDiorama_EdgeToneData ToyDiorama_CreateEdgeToneNoOp(float3 color)
{
	ToyDiorama_EdgeToneData data;
	data.beforeEdgeTone = color;
	data.afterEdgeTone = color;
	data.color = color;
	data.mask = 0.0;
	return data;
}

float ToyDiorama_CalculateEdgeMask(float2 uv)
{
	float2 centered = uv * 2.0 - 1.0;
	float aspect = _ScreenParams.x / max(_ScreenParams.y, 1.0);
	centered.x *= aspect;

	float diagonal = max(length(float2(aspect, 1.0)), 0.0001);
	float edgeDistance = saturate(length(centered) / diagonal);
	float edgeStart = saturate(_ToyDioramaEdgeToneRadius);
	float edgeEnd = min(edgeStart + max(_ToyDioramaEdgeToneSoftness, 0.001), 1.0);

	if (edgeEnd <= edgeStart)
	{
		return step(edgeStart, edgeDistance);
	}

	return smoothstep(edgeStart, edgeEnd, edgeDistance);
}

ToyDiorama_EdgeToneData ToyDiorama_ApplyEdgeTone(float3 color, float2 uv)
{
	ToyDiorama_EdgeToneData data;

	data.beforeEdgeTone = color;
	data.mask = ToyDiorama_CalculateEdgeMask(uv);

	float enabled = step(0.5, _ToyDioramaEdgeToneEnabled);
	float effectStrength = data.mask * saturate(_ToyDioramaEdgeToneStrength) * enabled;

	float edgeSaturation = 1.0 - effectStrength * saturate(_ToyDioramaEdgeSaturationFade);
	float3 tonedColor = ToyDiorama_ApplySaturation(color, edgeSaturation);
	float3 tintedColor = ToyDiorama_ColorizePreserveLuminance(tonedColor, _ToyDioramaEdgeToneColor.rgb);

	tonedColor = lerp(tonedColor, tintedColor, effectStrength);
	tonedColor = max(tonedColor + (effectStrength * _ToyDioramaEdgeBrightnessOffset).xxx, 0.0);

	data.afterEdgeTone = saturate(tonedColor);
	data.color = data.afterEdgeTone;

	return data;
}

#endif