#ifndef BC_ENVIRONMENT_STYLIZED_LIT_FORWARD_LIT_PASS_INCLUDED
#define BC_ENVIRONMENT_STYLIZED_LIT_FORWARD_LIT_PASS_INCLUDED

#include "../EnvironmentStylizedLit_Input.hlsl"
#include "../EnvironmentStylizedLit_Common.hlsl"
#include "../EnvironmentStylizedLit_Surface.hlsl"
#include "../EnvironmentStylizedLit_Lighting.hlsl"
#include "../EnvironmentStylizedLit_StylizedDiffuse.hlsl"
#include "../EnvironmentStylizedLit_Debug.hlsl"

struct ESL_Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float4 color : COLOR;
    float2 uv : TEXCOORD0;
	float2 staticLightmapUV : TEXCOORD1;
	float2 dynamicLightmapUV : TEXCOORD2;
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct ESL_Varyings
{
    float4 positionHCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    float4 tangentWS : TEXCOORD2;
    float2 uv : TEXCOORD3;
    float4 vertexColor : TEXCOORD9;
    #ifdef _ADDITIONAL_LIGHTS_VERTEX
    half4 fogFactorAndVertexLight : TEXCOORD4;
    #else
    half fogFactor : TEXCOORD4;
    #endif
    float4 shadowCoord : TEXCOORD5;
    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 6);
    #ifdef DYNAMICLIGHTMAP_ON
    float2 dynamicLightmapUV : TEXCOORD7;
    #endif
    #ifdef USE_APV_PROBE_OCCLUSION
    float4 probeOcclusion : TEXCOORD8;
    #endif
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

float2 ESL_GetNormalizedScreenSpaceUV(float4 positionHCS)
{
    #if defined(UNITY_PRETRANSFORM_TO_DISPLAY_ORIENTATION)
    float2 preRotatedScreenSpaceUV = GetNormalizedScreenSpaceUV(positionHCS);

    switch (UNITY_DISPLAY_ORIENTATION_PRETRANSFORM)
    {
        default:
        case UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_0:
            return preRotatedScreenSpaceUV;
        case UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_90:
            return float2(1.0 - preRotatedScreenSpaceUV.y, preRotatedScreenSpaceUV.x);
        case UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_180:
            return float2(1.0 - preRotatedScreenSpaceUV.x, 1.0 - preRotatedScreenSpaceUV.y);
        case UNITY_DISPLAY_ORIENTATION_PRETRANSFORM_270:
            return float2(preRotatedScreenSpaceUV.y, 1.0 - preRotatedScreenSpaceUV.x);
    }
    #else
    return GetNormalizedScreenSpaceUV(positionHCS);
    #endif
}

ESL_InputData ESL_BuildInputData(ESL_Varyings input, ESL_SurfaceData surfaceData, float faceSign)
{
    float3 faceNormalWS = input.normalWS * faceSign;
    float4 faceTangentWS = float4(input.tangentWS.xyz, input.tangentWS.w * faceSign);
    ESL_InputData inputData;
    inputData.positionWS = input.positionWS;
    inputData.normalWS = surfaceData.hasNormalWSOverride > 0.5
        ? normalize(surfaceData.normalWSOverride)
        : ESL_TransformNormalTangentToWorld(surfaceData.normalTS, faceNormalWS, faceTangentWS);
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    inputData.vertexColor = input.vertexColor;
    inputData.vertexLighting = 0.0;
    inputData.bakedGI = 0.0;
    inputData.shadowCoord = input.shadowCoord;
    inputData.shadowMask = 1.0;
    inputData.normalizedScreenSpaceUV = ESL_GetNormalizedScreenSpaceUV(input.positionHCS);
    inputData.indirectAmbientOcclusion = 1.0;
    inputData.directAmbientOcclusion = 1.0;
    #ifdef _ADDITIONAL_LIGHTS_VERTEX
    inputData.fogFactor = input.fogFactorAndVertexLight.x;
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
    #else
    inputData.fogFactor = input.fogFactor;
    #endif

    #if defined(_SCREEN_SPACE_IRRADIANCE)
    inputData.bakedGI = SAMPLE_GI(_ScreenSpaceIrradiance, input.positionHCS.xy);
    #elif defined(DYNAMICLIGHTMAP_ON)
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, inputData.normalWS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
    #elif !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    inputData.bakedGI = SAMPLE_GI(
        input.vertexSH,
        GetAbsolutePositionWS(inputData.positionWS),
        inputData.normalWS,
        inputData.viewDirectionWS,
        input.positionHCS.xy,
        input.probeOcclusion,
        inputData.shadowMask);
    #else
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
    #endif

    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData.normalizedScreenSpaceUV, surfaceData.occlusion);
    inputData.indirectAmbientOcclusion = aoFactor.indirectAmbientOcclusion;
    inputData.directAmbientOcclusion = aoFactor.directAmbientOcclusion;
    return inputData;
}

ESL_Varyings ESL_Vertex(ESL_Attributes input)
{
    ESL_Varyings output = (ESL_Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.positionHCS = positionInputs.positionCS;
    output.positionWS = positionInputs.positionWS;
    output.normalWS = normalInputs.normalWS;
    output.tangentWS = float4(normalInputs.tangentWS, input.tangentOS.w * GetOddNegativeScale());
    output.uv = input.uv;
    output.vertexColor = input.color;
    half fogFactor = ComputeFogFactor(output.positionHCS.z);
    #ifdef _ADDITIONAL_LIGHTS_VERTEX
    output.fogFactorAndVertexLight = half4(fogFactor, VertexLighting(positionInputs.positionWS, normalInputs.normalWS));
    #else
    output.fogFactor = fogFactor;
    #endif
    output.shadowCoord = GetShadowCoord(positionInputs);
	OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
	#ifdef DYNAMICLIGHTMAP_ON
	output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
	#endif
	OUTPUT_SH4(positionInputs.positionWS, output.normalWS.xyz, GetWorldSpaceNormalizeViewDir(positionInputs.positionWS), output.vertexSH, output.probeOcclusion);
    return output;
}

half4 ESL_Fragment(ESL_Varyings input, FRONT_FACE_TYPE facing : FRONT_FACE_SEMANTIC) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float faceSign = ESL_GetFaceSign(facing);
    ESL_SurfaceContext surfaceContext;
    surfaceContext.uv = input.uv;
    surfaceContext.positionWS = input.positionWS;
    surfaceContext.normalWS = input.normalWS * faceSign;
    surfaceContext.vertexColor = input.vertexColor;
    ESL_SurfaceData surfaceData = ESL_BuildSurfaceData(surfaceContext);
    ESL_ApplyAlphaClip(surfaceData.alpha);
    ESL_InputData inputData = ESL_BuildInputData(input, surfaceData, faceSign);
    ESL_StylizedDiffuseData diffuseData = ESL_EvaluateStylizedDiffuse(inputData, surfaceData);

    half3 color = half3(diffuseData.diffuseColor + surfaceData.emission);
    color = ESL_ApplyDebugView(color, diffuseData);

    if (!ESL_IsDebugViewActive())
    {
        color = MixFog(color, inputData.fogFactor);
    }

    return half4(color, surfaceData.alpha);
}

#endif