Shader "Hidden/Custom/AnalogTVStatic"
{
    Properties
    {
        _NoiseIntensity    ("Noise Intensity", Range(0, 1)) = 0.08
        _NoiseScale        ("Noise Scale", Float) = 320

        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.08
        _ScanlineDensity   ("Scanline Density", Float) = 1.0

        _JitterAmount      ("Jitter Amount", Range(0, 0.02)) = 0.002
        _JitterBands       ("Jitter Bands", Float) = 180
        _JitterSpeed       ("Jitter Speed", Float) = 24

        _DriftAmount       ("Drift Amount", Range(0, 0.02)) = 0.0015
        _DriftFrequency    ("Drift Frequency", Float) = 80

        _BurstIntensity    ("Burst Intensity", Range(0, 1)) = 0.18
        _BurstChance       ("Burst Chance", Range(0, 1)) = 0.08

        _BrightnessFlicker ("Brightness Flicker", Range(0, 0.2)) = 0.03
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
        }

        Cull Off
        ZWrite Off
        ZTest Always
        Blend Off

        Pass
        {
            Name "AnalogTVStaticPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _NoiseIntensity;
            float _NoiseScale;

            float _ScanlineIntensity;
            float _ScanlineDensity;

            float _JitterAmount;
            float _JitterBands;
            float _JitterSpeed;

            float _DriftAmount;
            float _DriftFrequency;

            float _BurstIntensity;
            float _BurstChance;

            float _BrightnessFlicker;

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float t = _Time.y;

                // 줄 단위 좌우 흔들림
                float bandId = floor(uv.y * _JitterBands);
                float jitterSeed = floor(t * _JitterSpeed);
                float jitter = (Hash21(float2(bandId, jitterSeed)) - 0.5) * 2.0 * _JitterAmount;

                // 느린 수평 드리프트
                float drift = sin(uv.y * _DriftFrequency + t * 15.0) * _DriftAmount;

                float2 sampleUV = uv + float2(jitter + drift, 0.0);
                sampleUV = saturate(sampleUV);

                // 현재 화면 샘플
                Varyings shiftedInput = input;
                shiftedInput.texcoord = sampleUV;
                half4 color = FragBlit(shiftedInput, sampler_LinearClamp);

                // 미세한 지지직 노이즈
                float2 fineCell = floor(sampleUV * _NoiseScale) + t * 60.0;
                float fineNoise = (Hash21(fineCell) - 0.5) * 2.0;
                color.rgb += fineNoise * _NoiseIntensity;

                // 스캔라인
                float scan = 0.5 + 0.5 * sin(sampleUV.y * _ScreenParams.y * _ScanlineDensity);
                float scanMul = lerp(1.0, 0.75 + 0.25 * scan, _ScanlineIntensity);
                color.rgb *= scanMul;

                // 가끔 강하게 튀는 버스트 노이즈
                float burstGate = step(1.0 - _BurstChance, Hash21(float2(floor(t * 3.0), 7.77)));
                float2 burstCell = floor(sampleUV * (_NoiseScale * 0.35)) + t * 120.0 + 11.0;
                float burstNoise = (Hash21(burstCell) - 0.5) * 2.0;
                color.rgb += burstNoise * (_BurstIntensity * burstGate);

                // 밝기 깜빡임
                float flicker = 1.0 + ((Hash21(float2(floor(t * 24.0), 99.0)) - 0.5) * 2.0 * _BrightnessFlicker);
                color.rgb *= flicker;

                color.a = 1.0;
                return saturate(color);
            }
            ENDHLSL
        }
    }

    Fallback Off
}