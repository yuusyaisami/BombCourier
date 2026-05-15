Shader "BC/Particles/TrailUnlit"
{
    Properties
    {
        [Enum(Alpha,0,Additive,1,Premultiply,2,Multiply,3)] _BlendMode ("Blend Mode", Float) = 0
        [Enum(Off,0,Front,1,Back,2)] _Cull ("Cull", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test", Float) = 4

        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _Alpha ("Alpha", Range(0, 1)) = 1
        _Brightness ("Brightness", Range(0, 8)) = 1
        [Enum(Off,0,On,1)] _UseVertexColor ("Use Vertex Color", Float) = 1

        _UVScrollSpeed ("UV Scroll Speed", Vector) = (0, 0, 0, 0)

        _NoiseMap ("Noise Map", 2D) = "white" {}
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0
        _NoiseScale ("Noise Scale", Range(0.01, 16)) = 1
        _NoiseScrollSpeed ("Noise Scroll Speed", Vector) = (0, 0, 0, 0)

        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0
        _DissolveSoftness ("Dissolve Softness", Range(0.001, 1)) = 0.2

        [Enum(U,0,V,1)] _EdgeFadeAxis ("Edge Fade Axis", Float) = 1
        _EdgeFadePower ("Edge Fade Power", Range(0.1, 8)) = 1
        _EdgeFadeStrength ("Edge Fade Strength", Range(0, 1)) = 0

        [HDR] _EmissionColor ("Emission Color", Color) = (0, 0, 0, 1)
        _EmissionStrength ("Emission Strength", Range(0, 16)) = 0

        [HideInInspector] _SrcBlend ("Source Blend", Float) = 5
        [HideInInspector] _DstBlend ("Destination Blend", Float) = 10
        [HideInInspector] _ZWrite ("Z Write", Float) = 0
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [HideInInspector] _QueueOffset ("Queue Offset", Float) = 0
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
            "UniversalMaterialType" = "Unlit"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Blend [_SrcBlend] [_DstBlend]
            ColorMask [_ColorMask]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex TrailUnlitVertex
            #pragma fragment TrailUnlitFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "HLSL/Passes/TrailUnlit_ForwardPass.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "BC.Rendering.TrailUnlitShaderGUI"
}
