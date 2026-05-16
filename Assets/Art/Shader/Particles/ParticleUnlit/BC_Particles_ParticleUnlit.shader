Shader "BC/Particles/ParticleUnlit"
{
    Properties
    {
        [Enum(Alpha,0,Additive,1,Premultiply,2)] _BlendMode ("Blend Mode", Float) = 0
        [Enum(Off,0,Front,1,Back,2)] _Cull ("Cull", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Z Test", Float) = 4

        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _Alpha ("Alpha", Range(0, 1)) = 1
        _Brightness ("Brightness", Range(0, 8)) = 1
        [Enum(Off,0,On,1)] _UseVertexColor ("Use Vertex Color", Float) = 1
        _SoftCircleStrength ("Soft Circle Strength", Range(0, 1)) = 1
        _EdgeFadePower ("Edge Fade Power", Range(0.1, 8)) = 1.5
        _EdgeFadeStrength ("Edge Fade Strength", Range(0, 1)) = 1
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
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_Cull]
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Blend [_SrcBlend] [_DstBlend]
            ColorMask [_ColorMask]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ParticleUnlitVertex
            #pragma fragment ParticleUnlitFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "HLSL/Passes/ParticleUnlit_ForwardPass.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "BC.Rendering.ParticleUnlitShaderGUI"
}