// 포토샵 레이어 합성을 화면 전체에 거는 셰이더.
// 순서: 원본 화면 → 곱하기(Multiply) → 색상 닷지(Color Dodge)
// Color Adjustments는 URP Volume이 이 뒤에 처리한다.
//
// ScreenBlendLayersFeature(Renderer2D의 Renderer Feature)가 카메라 화면에 블릿한다.
// 전체 세기는 전역값 _ScreenBlendWeight(0~1)로 조절하며 코드에서만 넣는다.
// 포토샵과 같은 결과가 나오도록 합성은 sRGB(감마) 값으로 계산한다.
Shader "Yumeji/ScreenBlendLayers"
{
    Properties
    {
        [Header(Multiply)]
        // 회색(명도 0.5) + 불투명도 1 = 화면 명도 x0.5. 색이 들어가면 색조·채도도 바뀐다.
        _MultiplyColor ("곱하기 색", Color) = (0.5, 0.5, 0.5, 1)
        [NoScaleOffset] _MultiplyTex ("곱하기 텍스처 (선택, 색과 곱해짐)", 2D) = "white" {}
        _MultiplyOpacity ("곱하기 불투명도", Range(0, 1)) = 1

        [Header(Color Dodge)]
        _DodgeColor ("색상 닷지 색", Color) = (0.18, 0.2, 0.28, 1)
        [NoScaleOffset] _DodgeTex ("색상 닷지 텍스처 (선택, 색과 곱해짐)", 2D) = "white" {}
        _DodgeOpacity ("색상 닷지 불투명도", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend Off

        Pass
        {
            Name "ScreenBlendLayers"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_MultiplyTex);
            SAMPLER(sampler_MultiplyTex);
            TEXTURE2D(_DodgeTex);
            SAMPLER(sampler_DodgeTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _MultiplyColor;
                half _MultiplyOpacity;
                half4 _DodgeColor;
                half _DodgeOpacity;
            CBUFFER_END

            // 머티리얼 값이 덮어쓰지 않도록 Properties에 넣지 않는다
            float _ScreenBlendWeight;

            half3 ToBlendSpace(half3 c)
            {
            #if UNITY_COLORSPACE_GAMMA
                return c;
            #else
                return LinearToSRGB(c);
            #endif
            }

            half3 FromBlendSpace(half3 c)
            {
            #if UNITY_COLORSPACE_GAMMA
                return c;
            #else
                return SRGBToLinear(c);
            #endif
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                half4 src = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                half3 base = ToBlendSpace(saturate(src.rgb));

                // 곱하기: base * blend
                half4 mulTex = SAMPLE_TEXTURE2D(_MultiplyTex, sampler_MultiplyTex, uv);
                half3 mulColor = ToBlendSpace(_MultiplyColor.rgb) * ToBlendSpace(mulTex.rgb);
                base = lerp(base, base * mulColor, _MultiplyOpacity * _MultiplyColor.a * mulTex.a);

                // 색상 닷지: base / (1 - blend)
                half4 dodgeTex = SAMPLE_TEXTURE2D(_DodgeTex, sampler_DodgeTex, uv);
                half3 dodgeColor = ToBlendSpace(_DodgeColor.rgb) * ToBlendSpace(dodgeTex.rgb);
                half3 dodged = min(1.0, base / max(1e-4, 1.0 - dodgeColor));
                base = lerp(base, dodged, _DodgeOpacity * _DodgeColor.a * dodgeTex.a);

                half3 result = lerp(src.rgb, FromBlendSpace(base), saturate(_ScreenBlendWeight));
                return half4(result, src.a);
            }
            ENDHLSL
        }
    }
}
