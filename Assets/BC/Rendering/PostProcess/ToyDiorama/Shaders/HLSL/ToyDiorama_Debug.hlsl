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

float3 ToyDiorama_ApplyDebugView(float3 beforeColorGrade, ToyDiorama_ColorPipelineData pipelineData, float2 uv)
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

    return pipelineData.afterColorGrade;
}

#endif