Shader "BC/Particles/ParticleDistortion"
{
    Properties
    {
        [Enum(Alpha,0,Additive,1,Premultiply,2)] _BlendMode ("Blend Mode", Float) = 0
        [Enum(Off,0,Front,1,Back,2)] _Cull ("Cull", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test", Float) = 4

        _DistortionMap ("Distortion Map", 2D) = "gray" {}
        _DistortionStrength ("Distortion Strength", Range(0, 2)) = 0.12
        _DistortionScale ("Distortion Scale", Range(0.01, 8)) = 1
        _DistortionScrollSpeed ("Distortion Scroll Speed", Vector) = (0.08, 0.02, 0, 0)

        _Alpha ("Alpha", Range(0, 1)) = 0.45
        [Enum(Off,0,On,1)] _UseVertexColor ("Use Vertex Color", Float) = 1
        _EdgeFadePower ("Edge Fade Power", Range(0.1, 8)) = 2.2
        _EdgeFadeStrength ("Edge Fade Strength", Range(0, 1)) = 0.85

        _NoiseMap ("Noise Map", 2D) = "gray" {}
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0
        _NoiseScale ("Noise Scale", Range(0.01, 8)) = 1

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
            "UniversalMaterialType" = "Unlit"
        }

        Pass
        {
            Name "ForwardDistortion"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Blend [_SrcBlend] [_DstBlend]
            ColorMask [_ColorMask]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ParticleDistortionVertex
            #pragma fragment ParticleDistortionFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "HLSL/Passes/ParticleDistortion_ForwardPass.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "BC.Rendering.ParticleDistortionShaderGUI"
}