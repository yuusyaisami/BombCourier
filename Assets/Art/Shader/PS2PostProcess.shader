Shader "BC/PostProcess/PS2PostProcess"
{
    Properties
    {
        _ColorSteps ("Color Steps", Range(8, 256)) = 48
        _Contrast ("Contrast", Range(0.5, 2.0)) = 1.15
        _Saturation ("Saturation", Range(0.0, 2.0)) = 0.9
        _DitherStrength ("Dither Strength", Range(0.0, 1.0)) = 0.25
        _PixelSnap ("Pixel Snap", Range(1, 8)) = 1
        _VignetteStrength ("Vignette Strength", Range(0.0, 1.0)) = 0.15
    }

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
            Name "PS2PostProcess"

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _ColorSteps;
            float _Contrast;
            float _Saturation;
            float _DitherStrength;
            float _PixelSnap;
            float _VignetteStrength;

            float Bayer4x4(int2 p)
            {
                int x = p.x & 3;
                int y = p.y & 3;
                int index = y * 4 + x;

                float value = 0.0;

                if (index == 0) value = 0.0;
                else if (index == 1) value = 8.0;
                else if (index == 2) value = 2.0;
                else if (index == 3) value = 10.0;
                else if (index == 4) value = 12.0;
                else if (index == 5) value = 4.0;
                else if (index == 6) value = 14.0;
                else if (index == 7) value = 6.0;
                else if (index == 8) value = 3.0;
                else if (index == 9) value = 11.0;
                else if (index == 10) value = 1.0;
                else if (index == 11) value = 9.0;
                else if (index == 12) value = 15.0;
                else if (index == 13) value = 7.0;
                else if (index == 14) value = 13.0;
                else value = 5.0;

                return value / 16.0;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;

                // 軽いピクセル感。
                // RTをCanvasに貼るのではなく、画面サンプリングUVだけを粗くする。
                if (_PixelSnap > 1.0)
                {
                    float2 screenSize = _ScreenParams.xy;
                    float2 virtualSize = screenSize / _PixelSnap;
                    uv = floor(uv * virtualSize) / virtualSize;
                }

                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float3 color = col.rgb;

                // Saturation
                float luminance = dot(color, float3(0.299, 0.587, 0.114));
                color = lerp(luminance.xxx, color, _Saturation);

                // Contrast
                color = (color - 0.5) * _Contrast + 0.5;

                // Ordered dithering
                int2 pixel = int2(input.positionCS.xy);
                float dither = Bayer4x4(pixel) - 0.5;
                color += dither * (_DitherStrength / max(8.0, _ColorSteps));

                // Color quantization
                float steps = max(2.0, _ColorSteps);
                color = floor(saturate(color) * steps) / steps;

                // Vignette
                float2 centered = input.texcoord * 2.0 - 1.0;
                float vignette = saturate(1.0 - dot(centered, centered) * _VignetteStrength);
                color *= vignette;

                return half4(saturate(color), col.a);
            }

            ENDHLSL
        }
    }
}