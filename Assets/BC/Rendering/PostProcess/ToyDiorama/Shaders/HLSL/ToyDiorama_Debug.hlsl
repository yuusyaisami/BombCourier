#ifndef BC_TOY_DIORAMA_DEBUG_INCLUDED
#define BC_TOY_DIORAMA_DEBUG_INCLUDED

#define TOY_DIORAMA_DEBUG_OFF 0
#define TOY_DIORAMA_DEBUG_SOURCE_COLOR 1
#define TOY_DIORAMA_DEBUG_LUMINANCE 2
#define TOY_DIORAMA_DEBUG_UV 3
#define TOY_DIORAMA_DEBUG_SHADOW_MASK 4
#define TOY_DIORAMA_DEBUG_MID_MASK 5
#define TOY_DIORAMA_DEBUG_HIGHLIGHT_MASK 6
#define TOY_DIORAMA_DEBUG_BEFORE_COLOR_GRADE 7
#define TOY_DIORAMA_DEBUG_AFTER_COLOR_GRADE 8
#define TOY_DIORAMA_DEBUG_PASTEL_MASK 9
#define TOY_DIORAMA_DEBUG_HIGH_SATURATION_MASK 10
#define TOY_DIORAMA_DEBUG_CREAM_HIGHLIGHT_MASK 11
#define TOY_DIORAMA_DEBUG_BEFORE_PASTEL 12
#define TOY_DIORAMA_DEBUG_AFTER_PASTEL 13
#define TOY_DIORAMA_DEBUG_EDGE_MASK 14
#define TOY_DIORAMA_DEBUG_BEFORE_EDGE_TONE 15
#define TOY_DIORAMA_DEBUG_AFTER_EDGE_TONE 16
#define TOY_DIORAMA_DEBUG_RAW_DEPTH 17
#define TOY_DIORAMA_DEBUG_LINEAR_DEPTH 18
#define TOY_DIORAMA_DEBUG_DEPTH_HAZE_MASK 19
#define TOY_DIORAMA_DEBUG_BEFORE_DEPTH_HAZE 20
#define TOY_DIORAMA_DEBUG_AFTER_DEPTH_HAZE 21
#define TOY_DIORAMA_DEBUG_GRAIN 22
#define TOY_DIORAMA_DEBUG_BEFORE_GRAIN 23
#define TOY_DIORAMA_DEBUG_AFTER_GRAIN 24
#define TOY_DIORAMA_DEBUG_BLOOM_PREFILTER 25
#define TOY_DIORAMA_DEBUG_BLOOM_BLUR 26
#define TOY_DIORAMA_DEBUG_BLOOM_COMPOSITE 27
#define TOY_DIORAMA_DEBUG_HALATION_MASK 28
#define TOY_DIORAMA_DEBUG_BEFORE_BLOOM 29
#define TOY_DIORAMA_DEBUG_AFTER_BLOOM 30

bool ToyDiorama_IsPreBloomDebugView(int debugView)
{
    return debugView == TOY_DIORAMA_DEBUG_SOURCE_COLOR ||
        debugView == TOY_DIORAMA_DEBUG_LUMINANCE ||
        debugView == TOY_DIORAMA_DEBUG_UV ||
        debugView == TOY_DIORAMA_DEBUG_SHADOW_MASK ||
        debugView == TOY_DIORAMA_DEBUG_MID_MASK ||
        debugView == TOY_DIORAMA_DEBUG_HIGHLIGHT_MASK ||
        debugView == TOY_DIORAMA_DEBUG_BEFORE_COLOR_GRADE ||
        debugView == TOY_DIORAMA_DEBUG_PASTEL_MASK ||
        debugView == TOY_DIORAMA_DEBUG_HIGH_SATURATION_MASK ||
        debugView == TOY_DIORAMA_DEBUG_CREAM_HIGHLIGHT_MASK ||
        debugView == TOY_DIORAMA_DEBUG_BEFORE_PASTEL ||
        debugView == TOY_DIORAMA_DEBUG_AFTER_PASTEL ||
        debugView == TOY_DIORAMA_DEBUG_RAW_DEPTH ||
        debugView == TOY_DIORAMA_DEBUG_LINEAR_DEPTH ||
        debugView == TOY_DIORAMA_DEBUG_DEPTH_HAZE_MASK ||
        debugView == TOY_DIORAMA_DEBUG_BEFORE_DEPTH_HAZE ||
        debugView == TOY_DIORAMA_DEBUG_AFTER_DEPTH_HAZE ||
        debugView == TOY_DIORAMA_DEBUG_BEFORE_BLOOM;
}

bool ToyDiorama_IsBloomDebugView(int debugView)
{
    return debugView == TOY_DIORAMA_DEBUG_BLOOM_PREFILTER ||
        debugView == TOY_DIORAMA_DEBUG_BLOOM_BLUR ||
        debugView == TOY_DIORAMA_DEBUG_BLOOM_COMPOSITE ||
        debugView == TOY_DIORAMA_DEBUG_HALATION_MASK;
}

float3 ToyDiorama_ApplyPreBloomDebugView(float3 beforeColorGrade, ToyDiorama_ColorPipelineData pipelineData, float2 uv)
{
    int debugView = (int)round(_ToyDioramaDebugView);

    if (debugView == TOY_DIORAMA_DEBUG_SOURCE_COLOR || debugView == TOY_DIORAMA_DEBUG_BEFORE_COLOR_GRADE)
    {
        return beforeColorGrade;
    }

    if (debugView == TOY_DIORAMA_DEBUG_LUMINANCE)
    {
        return ToyDiorama_Luminance(beforeColorGrade).xxx;
    }

    if (debugView == TOY_DIORAMA_DEBUG_UV)
    {
        return float3(uv, 0.0);
    }

    if (debugView == TOY_DIORAMA_DEBUG_SHADOW_MASK)
    {
        return pipelineData.colorGradeMasks.shadow.xxx;
    }

    if (debugView == TOY_DIORAMA_DEBUG_MID_MASK)
    {
        return pipelineData.colorGradeMasks.mid.xxx;
    }

    if (debugView == TOY_DIORAMA_DEBUG_HIGHLIGHT_MASK)
    {
        return pipelineData.colorGradeMasks.highlight.xxx;
    }

    if (debugView == TOY_DIORAMA_DEBUG_PASTEL_MASK)
    {
        return pipelineData.pastelData.pastelMask.xxx;
    }

    if (debugView == TOY_DIORAMA_DEBUG_HIGH_SATURATION_MASK)
    {
        return pipelineData.pastelData.highSaturationMask.xxx;
    }

    if (debugView == TOY_DIORAMA_DEBUG_CREAM_HIGHLIGHT_MASK)
    {
        return pipelineData.creamHighlightData.mask.xxx;
    }

    if (debugView == TOY_DIORAMA_DEBUG_BEFORE_PASTEL)
    {
        return pipelineData.beforePastel;
    }

    if (debugView == TOY_DIORAMA_DEBUG_AFTER_PASTEL)
    {
        return pipelineData.afterPastel;
    }

    if (debugView == TOY_DIORAMA_DEBUG_EDGE_MASK)
    {
        return pipelineData.edgeToneData.mask.xxx;
    }

    if (debugView == TOY_DIORAMA_DEBUG_BEFORE_EDGE_TONE)
    {
        return pipelineData.edgeToneData.beforeEdgeTone;
    }

    if (debugView == TOY_DIORAMA_DEBUG_AFTER_EDGE_TONE)
    {
        return pipelineData.edgeToneData.afterEdgeTone;
    }

    if (debugView == TOY_DIORAMA_DEBUG_RAW_DEPTH)
    {
        return pipelineData.depthHazeData.rawDepth.xxx;
    }

    if (debugView == TOY_DIORAMA_DEBUG_LINEAR_DEPTH)
    {
        return pipelineData.depthHazeData.linearDepth.xxx;
    }

    if (debugView == TOY_DIORAMA_DEBUG_DEPTH_HAZE_MASK)
    {
        return pipelineData.depthHazeData.mask.xxx;
    }

    if (debugView == TOY_DIORAMA_DEBUG_BEFORE_DEPTH_HAZE)
    {
        return pipelineData.depthHazeData.beforeDepthHaze;
    }

    if (debugView == TOY_DIORAMA_DEBUG_AFTER_DEPTH_HAZE)
    {
        return pipelineData.depthHazeData.afterDepthHaze;
    }

    if (debugView == TOY_DIORAMA_DEBUG_BEFORE_BLOOM)
    {
        return pipelineData.beforeBloom;
    }

    return pipelineData.beforeBloom;
}

float3 ToyDiorama_ApplyFinalDebugView(float3 preBloomColor, ToyDiorama_FinalColorPipelineData pipelineData)
{
    int debugView = (int)round(_ToyDioramaDebugView);

    if (ToyDiorama_IsPreBloomDebugView(debugView))
    {
        return preBloomColor;
    }

    if (debugView == TOY_DIORAMA_DEBUG_BLOOM_PREFILTER ||
        debugView == TOY_DIORAMA_DEBUG_BLOOM_BLUR ||
        debugView == TOY_DIORAMA_DEBUG_BLOOM_COMPOSITE)
    {
        return pipelineData.bloomComposite.rgb;
    }

    if (debugView == TOY_DIORAMA_DEBUG_HALATION_MASK)
    {
        return pipelineData.bloomComposite.a.xxx;
    }

    if (debugView == TOY_DIORAMA_DEBUG_AFTER_COLOR_GRADE)
    {
        return pipelineData.afterColorGrade;
    }

    if (debugView == TOY_DIORAMA_DEBUG_AFTER_BLOOM)
    {
        return pipelineData.afterBloom;
    }

    if (debugView == TOY_DIORAMA_DEBUG_EDGE_MASK)
    {
        return pipelineData.edgeToneData.mask.xxx;
    }

    if (debugView == TOY_DIORAMA_DEBUG_BEFORE_EDGE_TONE)
    {
        return pipelineData.edgeToneData.beforeEdgeTone;
    }

    if (debugView == TOY_DIORAMA_DEBUG_AFTER_EDGE_TONE)
    {
        return pipelineData.edgeToneData.afterEdgeTone;
    }

    if (debugView == TOY_DIORAMA_DEBUG_GRAIN)
    {
        float grainDisplay = 0.5 + pipelineData.grainData.grainContribution / max(_ToyDioramaGrainStrength * 2.0, 0.0001);
        return saturate(grainDisplay).xxx;
    }

    if (debugView == TOY_DIORAMA_DEBUG_BEFORE_GRAIN)
    {
        return pipelineData.grainData.beforeGrain;
    }

    if (debugView == TOY_DIORAMA_DEBUG_AFTER_GRAIN)
    {
        return pipelineData.grainData.afterGrain;
    }

    return pipelineData.afterColorGrade;
}

float3 ToyDiorama_ApplyDebugView(float3 beforeColorGrade, ToyDiorama_ColorPipelineData pipelineData, float2 uv)
{
    return ToyDiorama_ApplyPreBloomDebugView(beforeColorGrade, pipelineData, uv);
}

#endif