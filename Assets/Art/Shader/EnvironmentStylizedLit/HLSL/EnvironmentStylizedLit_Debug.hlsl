#ifndef BC_ENVIRONMENT_STYLIZED_LIT_DEBUG_INCLUDED
#define BC_ENVIRONMENT_STYLIZED_LIT_DEBUG_INCLUDED

#include "EnvironmentStylizedLit_StylizedDiffuse.hlsl"

#define ESL_DEBUG_OFF 0
#define ESL_DEBUG_NDOTL 1
#define ESL_DEBUG_WRAPPED_LIGHT 2
#define ESL_DEBUG_STEPPED_LIGHT 3
#define ESL_DEBUG_BAND_COLOR 4
#define ESL_DEBUG_WORLD_NOISE 5
#define ESL_DEBUG_BAND_NOISE 6

float3 ESL_EncodeDebugNoise(float noiseValue)
{
	if (abs(noiseValue) <= 1e-4)
	{
		return 0.0.xxx;
	}

	return saturate(noiseValue * 0.5 + 0.5).xxx;
}

bool ESL_IsDebugViewActive()
{
	return round(_DebugView) > ESL_DEBUG_OFF;
}

float3 ESL_ApplyDebugView(float3 color, ESL_StylizedDiffuseData diffuseData)
{
	int debugView = (int)round(_DebugView);

	if (debugView == ESL_DEBUG_NDOTL)
	{
		return diffuseData.ndotl.xxx;
	}

	if (debugView == ESL_DEBUG_WRAPPED_LIGHT)
	{
		return diffuseData.wrappedLight.xxx;
	}

	if (debugView == ESL_DEBUG_STEPPED_LIGHT)
	{
		return diffuseData.steppedLight.xxx;
	}

	if (debugView == ESL_DEBUG_BAND_COLOR)
	{
		return diffuseData.shadowedBandColor;
	}

	if (debugView == ESL_DEBUG_WORLD_NOISE)
	{
		return ESL_EncodeDebugNoise(diffuseData.worldNoise);
	}

	if (debugView == ESL_DEBUG_BAND_NOISE)
	{
		return ESL_EncodeDebugNoise(diffuseData.bandNoise);
	}

	return color;
}

#endif