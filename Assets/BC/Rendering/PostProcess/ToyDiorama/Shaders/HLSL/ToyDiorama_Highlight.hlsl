#ifndef BC_TOY_DIORAMA_HIGHLIGHT_INCLUDED
#define BC_TOY_DIORAMA_HIGHLIGHT_INCLUDED

struct ToyDiorama_CreamHighlightData
{
	float3 color;
	float mask;
};

// クリーム色ハイライトへの再配色に使うソフトしきい値マスクです。
float ToyDiorama_CalculateCreamHighlightMask(float3 color)
{
	float luminance = ToyDiorama_Luminance(color);
	float softness = max(_ToyDioramaCreamHighlightSoftness, 0.001);
	return smoothstep(
		_ToyDioramaCreamHighlightThreshold - softness,
		_ToyDioramaCreamHighlightThreshold + softness,
		luminance);
}

// 輝度を保持したまま暖色ハイライトを適用し、過度な発光感を防ぎます。
ToyDiorama_CreamHighlightData ToyDiorama_ApplyCreamHighlight(float3 color)
{
	ToyDiorama_CreamHighlightData data;
	data.mask = ToyDiorama_CalculateCreamHighlightMask(color);

	float3 creamTarget = ToyDiorama_ColorizePreserveLuminance(
		color,
		_ToyDioramaCreamHighlightColor.rgb);

	data.color = saturate(lerp(
		color,
		creamTarget,
		data.mask * saturate(_ToyDioramaCreamHighlightStrength)));

	return data;
}

#endif