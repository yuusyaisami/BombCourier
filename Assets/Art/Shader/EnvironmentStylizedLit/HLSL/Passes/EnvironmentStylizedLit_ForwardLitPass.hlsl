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
	float3 barycentric : TEXCOORD3;
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
    float3 barycentric : TEXCOORD10;
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

// 端末回転プリトランスフォーム時も正しい正規化画面UVを返します。
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

// SurfaceDataをもとにライティング入力を構築します。
// GI経路はコンパイル条件（スクリーンGI/ライトマップ/APV）で分岐します。
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

// ForwardLit頂点シェーダ。ライティングに必要な補間値を生成します。
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
    output.barycentric = input.barycentric;
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

// バリセントリック座標から線エッジ用マスクを算出します。
half ESL_EvaluateEdgeMask(float3 barycentric)
{
    float barycentricSum = barycentric.x + barycentric.y + barycentric.z;
    if (abs(barycentricSum - 1.0) > 0.25)
    {
        return 0.0h;
    }

    float3 derivative = max(fwidth(barycentric), 1e-5.xxx);
    float3 smoothed = smoothstep(0.0.xxx, derivative * max(_EdgeWidth, 0.25), barycentric);
    return saturate(1.0 - min(smoothed.x, min(smoothed.y, smoothed.z)));
}

// 面色と線色をアルファ合成し、エッジ専用表示モードの出力色を作ります。
half4 ESL_ComposeFaceAndEdge(half3 faceColor, half faceAlpha, half3 edgeColor, half edgeAlpha)
{
    half clampedFaceAlpha = saturate(faceAlpha);
    half clampedEdgeAlpha = saturate(edgeAlpha);
    half finalAlpha = clampedEdgeAlpha + clampedFaceAlpha * (1.0h - clampedEdgeAlpha);

    if (finalAlpha <= 1e-4h)
    {
        return half4(0.0h, 0.0h, 0.0h, 0.0h);
    }

    half3 premultipliedColor = edgeColor * clampedEdgeAlpha
        + faceColor * clampedFaceAlpha * (1.0h - clampedEdgeAlpha);
    return half4(premultipliedColor / finalAlpha, finalAlpha);
}

// Opaque面のみDecal受信を適用します。DBufferが無効な経路では入力をそのまま保持します。
void ESL_ApplyDecalToSurfaceData(float4 positionHCS, inout ESL_SurfaceData surfaceData, inout ESL_InputData inputData)
{
    if (_ReceiveDecal <= 0.5 || !ESL_IsOpaqueSurfaceMode())
    {
        return;
    }

    #if defined(_DBUFFER)
    half3 albedo = surfaceData.albedo;
    half3 specular = 0.0;
    half3 normalWS = inputData.normalWS;
    half metallic = surfaceData.metallic;
    half occlusion = surfaceData.occlusion;
    half smoothness = surfaceData.smoothness;
    ApplyDecal(positionHCS, albedo, specular, normalWS, metallic, occlusion, smoothness);

    surfaceData.albedo = albedo;
    surfaceData.metallic = metallic;
    surfaceData.occlusion = occlusion;
    surfaceData.smoothness = smoothness;
    inputData.normalWS = normalize(normalWS);
    #endif
}

// ForwardLitフラグメント本体。
// Surface生成 -> StylizedDiffuse評価 -> Debug/Fog -> 必要ならエッジ合成の順で処理します。
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
    ESL_ApplyDecalToSurfaceData(input.positionHCS, surfaceData, inputData);
    ESL_StylizedDiffuseData diffuseData = ESL_EvaluateStylizedDiffuse(inputData, surfaceData);

    // 既存Emissionに、新規2系統の発光加算を重ねて最終発光を作ります。
    half3 color = half3(diffuseData.diffuseColor + surfaceData.emission + diffuseData.emissionAddColor);
    color = ESL_ApplyDebugView(color, diffuseData);

    if (!ESL_IsDebugViewActive())
    {
        color = MixFog(color, inputData.fogFactor);
    }

    if (ESL_IsEdgeOnlySurfaceMode())
    {
        half faceAlpha = surfaceData.alpha * saturate(_FaceAlpha);
        half edgeMask = ESL_EvaluateEdgeMask(abs(input.barycentric));
        return ESL_ComposeFaceAndEdge(color, faceAlpha, _EdgeColor.rgb, edgeMask * _EdgeColor.a);
    }

    return half4(color, surfaceData.alpha);
}

#endif