Shader "Hidden/BC/PostProcess/ToyDioramaBloom"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "ToyDioramaBloomPlaceholder"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment ToyDioramaBloom_Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            half4 ToyDioramaBloom_Fragment(Varyings input) : SV_Target
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
            }

            ENDHLSL
        }
    }

    FallBack Off
}