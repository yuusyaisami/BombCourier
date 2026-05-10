Shader "BC/PickupOutlineInvertedHull"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 1, 0, 1)
        _OutlineWidth ("Outline Width", Float) = 0.035
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "Outline"

            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                positionWS += normalWS * _OutlineWidth;

                output.positionHCS = TransformWorldToHClip(positionWS);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }

            ENDHLSL
        }
    }
}