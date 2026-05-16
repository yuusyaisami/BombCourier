Shader "BC/Particles/ParticleLit"
{
    Properties
    {
        [Enum(Alpha,0,Additive,1,Premultiply,2)] _BlendMode ("Blend Mode", Float) = 0
        [Enum(Off,0,Front,1,Back,2)] _Cull ("Cull", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test", Float) = 4

        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _Alpha ("Alpha", Range(0, 1)) = 1
        [Enum(Off,0,On,1)] _UseVertexColor ("Use Vertex Color", Float) = 1

        [Normal] _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalScale ("Normal Strength", Range(0, 2)) = 1

        _Smoothness ("Smoothness", Range(0, 1)) = 0.35
        _Metallic ("Metallic", Range(0, 1)) = 0
        _LightInfluence ("Light Influence", Range(0, 1)) = 1

        [HDR] _EmissionColor ("Emission Color", Color) = (0, 0, 0, 1)
        _EmissionStrength ("Emission Strength", Range(0, 8)) = 0

        _QueueOffset ("Queue Offset", Range(-50, 50)) = 0

        [HideInInspector] _SrcBlend ("Source Blend", Float) = 5
        [HideInInspector] _DstBlend ("Destination Blend", Float) = 10
        [HideInInspector] _ZWrite ("Z Write", Float) = 0
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "UniversalMaterialType" = "Lit"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Blend [_SrcBlend] [_DstBlend]
            ColorMask [_ColorMask]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ParticleLitVertex
            #pragma fragment ParticleLitFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "HLSL/Passes/ParticleLit_ForwardPass.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "BC.Rendering.ParticleLitShaderGUI"
}