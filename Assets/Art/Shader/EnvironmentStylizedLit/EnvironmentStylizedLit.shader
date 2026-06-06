Shader "BC/EnvironmentStylizedLit"
{
    Properties
    {
        [Enum(Opaque,0,Transparent,1,EdgeOnly,2)] _SurfaceMode ("Surface Mode", Float) = 0
        [Enum(Front,2,Back,1,Both,0)] _Cull ("Render Face", Float) = 2
        [Toggle] _AlphaClip ("Alpha Clip", Float) = 0
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5

        [HideInInspector] _SrcBlend ("Src Blend", Float) = 1
        [HideInInspector] _DstBlend ("Dst Blend", Float) = 0
        [HideInInspector] _ZWrite ("Z Write", Float) = 1

        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _FaceAlpha ("Face Alpha", Range(0, 1)) = 1
        _EdgeColor ("Edge Color", Color) = (1, 1, 1, 1)
        _EdgeWidth ("Edge Width", Range(0.25, 8)) = 1

        [Normal] _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalScale ("Normal Scale", Range(0, 2)) = 1

        _OcclusionMap ("Occlusion Map", 2D) = "white" {}
        _OcclusionStrength ("Occlusion Strength", Range(0, 1)) = 1

        _EmissionMap ("Emission Map", 2D) = "black" {}
        [HDR] _EmissionColor ("Emission Color", Color) = (0, 0, 0, 1)
        _EmissionStrength ("Emission Strength", Range(0, 10)) = 0

        _Metallic ("Metallic", Range(0, 1)) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.35
        _SpecularColor ("Specular Color", Color) = (0.2, 0.2, 0.2, 1)
        [Enum(Off,0,Soft,1,Quantized,2,Ceramic,3,Plastic,4)] _SpecularMode ("Specular Mode", Float) = 0
        _SpecularStrength ("Specular Strength", Range(0, 1)) = 0.25
        _SpecularStepCount ("Specular Step Count", Range(1, 5)) = 3
        _SpecularStepSmoothness ("Specular Step Smoothness", Range(0, 0.5)) = 0.1
        _EdgeSheenStrength ("Edge Sheen Strength", Range(0, 1)) = 0.15
        _EdgeSheenPower ("Edge Sheen Power", Range(0.5, 8)) = 2.5
        _EdgeSheenColor ("Edge Sheen Color", Color) = (1.0, 0.97, 0.92, 1)

        _LightStepCount ("Light Step Count", Range(1, 5)) = 3
        _LightStepSmoothness ("Light Step Smoothness", Range(0, 0.5)) = 0.08
        _WrapLighting ("Wrap Lighting", Range(0, 1)) = 0.15
        _BandContrast ("Band Contrast", Range(0.25, 2)) = 1
        _BandOffset ("Band Offset", Range(-1, 1)) = 0

        _DeepShadowColor ("Deep Shadow Color", Color) = (0.34, 0.40, 0.56, 1)
        _ShadowColor ("Shadow Color", Color) = (0.56, 0.63, 0.79, 1)
        _MidColor ("Mid Color", Color) = (0.84, 0.88, 0.93, 1)
        _LightColor ("Light Color", Color) = (1.0, 0.96, 0.90, 1)
        _HighlightColor ("Highlight Color", Color) = (1.0, 0.98, 0.94, 1)

        _ShadowInfluence ("Shadow Influence", Range(0, 1)) = 1
        _ShadowSoftFill ("Shadow Soft Fill", Range(0, 1)) = 0.2
        _ShadowColorBlend ("Shadow Color Blend", Range(0, 1)) = 0.6
        _ShadowStrength ("Shadow Strength", Range(0, 1)) = 0.9

        _AmbientTopColor ("Ambient Top Color", Color) = (0.50, 0.57, 0.70, 1)
        _AmbientSideColor ("Ambient Side Color", Color) = (0.34, 0.38, 0.46, 1)
        _AmbientBottomColor ("Ambient Bottom Color", Color) = (0.22, 0.20, 0.18, 1)
        _AmbientStrength ("Ambient Strength", Range(0, 1)) = 0.35

        _BounceColor ("Bounce Color", Color) = (0.92, 0.78, 0.62, 1)
        _BounceStrength ("Bounce Strength", Range(0, 1)) = 0.2
        _BounceDirection ("Bounce Direction", Vector) = (0, 1, 0, 0)
        _BounceWrap ("Bounce Wrap", Range(0, 1)) = 0.35
        _IndirectShadowColor ("Indirect Shadow Color", Color) = (0.72, 0.78, 0.88, 1)
        _IndirectStrength ("Indirect Strength", Range(0, 1)) = 1
        _IndirectStylizeStrength ("Indirect Stylize Strength", Range(0, 1)) = 0.35
        _CavityStrength ("Cavity Strength", Range(0, 1)) = 0.35
        _CavityColor ("Cavity Color", Color) = (0.68, 0.73, 0.82, 1)

        [Enum(Off,0,FillOnly,1,Quantized,2,Continuous,3)] _AdditionalLightMode ("Additional Light Mode", Float) = 1
        _AdditionalLightIntensity ("Additional Light Intensity", Range(0, 1)) = 0.5
        _AdditionalLightShadowInfluence ("Additional Light Shadow Influence", Range(0, 1)) = 0.65
        _AdditionalLightColorInfluence ("Additional Light Color Influence", Range(0, 1)) = 0.75

        [Toggle] _LightBandEmissionEnabled ("Light Band Emission", Float) = 0
        [HDR] _LightBandEmissionColor ("Light Band Emission Color", Color) = (1.4, 1.2, 1.0, 1)
        _LightBandEmissionIntensity ("Light Band Emission Intensity", Range(0, 20)) = 2
        _LightBandEmissionMin ("Light Band Emission Min", Range(0, 1)) = 0.55
        _LightBandEmissionMax ("Light Band Emission Max", Range(0, 1)) = 1
        _LightBandEmissionFeather ("Light Band Emission Feather", Range(0, 0.5)) = 0.08
        _LightBandEmissionStepMin ("Light Band Emission Step Min", Range(1, 5)) = 3
        _LightBandEmissionStepMax ("Light Band Emission Step Max", Range(1, 5)) = 5
        _LightBandEmissionBandStepBlend ("Light Band/Step Blend", Range(0, 1)) = 1
        _LightBandEmissionAdditionalWeight ("Light Band Additional Weight", Range(0, 2)) = 1
        _LightBandEmissionResponse ("Light Band Response", Range(0.25, 8)) = 2
        _LightBandEmissionSpecialMaskInfluence ("Light Band Vertex A Influence", Range(0, 1)) = 0
        _LightBandEmissionGradientInfluence ("Light Band Gradient Influence", Range(0, 1)) = 0

        [Toggle] _SimpleBoostEmissionEnabled ("Simple Boost Emission", Float) = 0
        [HDR] _SimpleBoostEmissionColor ("Simple Boost Emission Color", Color) = (1.0, 0.95, 0.9, 1)
        _SimpleBoostEmissionIntensity ("Simple Boost Emission Intensity", Range(0, 25)) = 4
        _SimpleBoostFresnelStrength ("Simple Boost Fresnel Strength", Range(0, 4)) = 1
        _SimpleBoostFresnelPower ("Simple Boost Fresnel Power", Range(0.25, 8)) = 2
        [Toggle] _SimpleBoostFresnelInvert ("Simple Boost Fresnel Invert", Float) = 0

        [Toggle(_ESL_TRIPLANAR_BASEMAP)] _TriplanarBaseMapEnabled ("Triplanar Base Map", Float) = 0
        [Toggle(_ESL_TRIPLANAR_NORMALMAP)] _TriplanarNormalMapEnabled ("Triplanar Normal Map", Float) = 0
        [Toggle(_ESL_TRIPLANAR_NOISE)] _TriplanarNoiseEnabled ("Triplanar Noise", Float) = 0
        _TriplanarScale ("Triplanar Scale", Range(0.01, 8)) = 1
        _TriplanarBlendSharpness ("Triplanar Blend Sharpness", Range(1, 8)) = 4

        [Toggle] _VertexColorEnabled ("Vertex Color Enabled", Float) = 0
        _VertexColorCavityStrength ("Vertex Color Cavity Strength", Range(0, 1)) = 1
        _VertexColorBandOffsetStrength ("Vertex Color Band Offset Strength", Range(0, 1)) = 1
        _VertexColorColorVariationStrength ("Vertex Color Color Variation Strength", Range(0, 1)) = 1

        [Toggle] _WorldYGradientEnabled ("World Y Gradient", Float) = 0
        _WorldYGradientTopColor ("World Y Gradient Top Color", Color) = (1.0, 1.0, 1.0, 1)
        _WorldYGradientBottomColor ("World Y Gradient Bottom Color", Color) = (1.0, 1.0, 1.0, 1)
        _WorldYGradientMin ("World Y Gradient Min", Float) = 0
        _WorldYGradientMax ("World Y Gradient Max", Float) = 3
        _WorldYGradientStrength ("World Y Gradient Strength", Range(0, 1)) = 0

        [Enum(WorldNoise,0,ObjectSpaceNoise,1)] _NoiseSpace ("Noise Space", Float) = 0
        _AlbedoNoiseStrength ("Albedo Noise Strength", Range(0, 1)) = 0.18
        _WorldNoiseScale ("World Noise Scale", Range(0.01, 8)) = 0.4
        _WorldNoiseStrength ("World Noise Strength", Range(0, 1)) = 0.35
        _WorldNoiseContrast ("World Noise Contrast", Range(0.25, 4)) = 1.25
        _LightBandNoiseStrength ("Light Band Noise Strength", Range(0, 1)) = 0.08
        _LightBandNoiseScale ("Light Band Noise Scale", Range(0.01, 8)) = 0.75
        _NoiseDistanceFadeStart ("Noise Distance Fade Start", Range(0, 100)) = 12
        _NoiseDistanceFadeEnd ("Noise Distance Fade End", Range(0, 100)) = 32

        [Range(0, 11)] _DebugView ("Debug View", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "UniversalMaterialType" = "Lit"
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            Cull [_Cull]
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM

            #pragma vertex ESL_ShadowPassVertex
            #pragma fragment ESL_ShadowPassFragment
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma shader_feature_local_fragment _ _ESL_TRIPLANAR_BASEMAP
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "HLSL/Passes/EnvironmentStylizedLit_ShadowCasterPass.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            Cull [_Cull]
            ZWrite On
            ZTest LEqual
            ColorMask R

            HLSLPROGRAM

            #pragma vertex ESL_DepthOnlyVertex
            #pragma fragment ESL_DepthOnlyFragment
            #pragma shader_feature_local_fragment _ _ESL_TRIPLANAR_BASEMAP
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "HLSL/Passes/EnvironmentStylizedLit_DepthOnlyPass.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "DepthNormalsOnly"
            Tags { "LightMode" = "DepthNormalsOnly" }

            Cull [_Cull]
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM

            #pragma vertex ESL_DepthNormalsVertex
            #pragma fragment ESL_DepthNormalsFragment
            #pragma shader_feature_local_fragment _ _ESL_TRIPLANAR_NORMALMAP
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "HLSL/Passes/EnvironmentStylizedLit_DepthNormalsPass.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }

            Cull Off

            HLSLPROGRAM

            #pragma vertex ESL_MetaPassVertex
            #pragma fragment ESL_MetaPassFragment
            #pragma shader_feature_local_fragment _ EDITOR_VISUALIZATION
            #pragma shader_feature_local_fragment _ _ESL_TRIPLANAR_BASEMAP
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "HLSL/Passes/EnvironmentStylizedLit_MetaPass.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            ZTest LEqual

            HLSLPROGRAM

            #pragma vertex ESL_Vertex
            #pragma fragment ESL_Fragment
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma shader_feature_local_fragment _ _ESL_TRIPLANAR_BASEMAP
            #pragma shader_feature_local_fragment _ _ESL_TRIPLANAR_NORMALMAP
            #pragma shader_feature_local_fragment _ _ESL_TRIPLANAR_NOISE
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _SCREEN_SPACE_IRRADIANCE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "HLSL/Passes/EnvironmentStylizedLit_ForwardLitPass.hlsl"

            ENDHLSL
        }
    }

    CustomEditor "BC.Rendering.EnvironmentStylizedLitShaderGUI"
    FallBack Off
}