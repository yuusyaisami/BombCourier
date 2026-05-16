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

        _MaskMap ("Mask Map", 2D) = "white" {}
        _MaskStrength ("Mask Strength", Range(0, 1)) = 0

        _NoiseMap ("Noise Map", 2D) = "white" {}
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0
        _NoiseScale ("Noise Scale", Range(0.01, 16)) = 1
        _NoiseScrollSpeed ("Noise Scroll Speed", Vector) = (0, 0, 0, 0)
        [Enum(ParticleUV,0)] _NoiseSpace ("Noise Space", Float) = 0

        _DissolveAmount ("Dissolve Amount", Range(0, 1)) = 0
        _DissolveSoftness ("Dissolve Softness", Range(0.001, 1)) = 0.25

        [HDR] _EmissionColor ("Emission Color", Color) = (0, 0, 0, 1)
        _EmissionStrength ("Emission Strength", Range(0, 16)) = 0
        _EmissionAlphaInfluence ("Emission Alpha Influence", Range(0, 1)) = 0.5
        [Enum(Final,0,BaseRGB,1,BaseAlpha,2,VertexColor,3,VertexAlpha,4,MaskDissolve,5,MaskEmission,6,MaskVariation,7,MaskShape,8,Noise,9,DissolveResult,10,EmissionResult,11,SoftCircle,12,Custom1,13,Custom2,14,UV,15)] _DebugMode ("Debug Mode", Float) = 0

        [Enum(Off,0,On,1)] _UseSoftParticles ("Use Soft Particles", Float) = 0
        _SoftParticleDistance ("Soft Particle Distance", Range(0.001, 4)) = 0.75
        [Enum(Off,0,On,1)] _UseCameraFade ("Use Camera Fade", Float) = 0
        _CameraFadeNear ("Camera Fade Near", Range(0, 5)) = 0.15
        _CameraFadeFar ("Camera Fade Far", Range(0.001, 8)) = 0.75

        _SoftCircleStrength ("Soft Circle Strength", Range(0, 1)) = 1
        _EdgeFadePower ("Edge Fade Power", Range(0.1, 8)) = 1.5
        _EdgeFadeStrength ("Edge Fade Strength", Range(0, 1)) = 1
        _QueueOffset ("Queue Offset", Range(-50, 50)) = 0

        [HideInInspector] _QualityTier ("Quality Tier", Float) = 1
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