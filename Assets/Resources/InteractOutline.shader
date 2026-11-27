// InteractOutline.shader
// 상호작용 오브젝트 외곽선 전용 셰이더 (URP Unlit)
// 원리: 메시를 법선 방향으로 살짝 부풀린 뒤, 앞면을 잘라내고(Cull Front) 뒷면만 단색으로 그린다.
//       그러면 원본 메시 바깥으로 삐져나온 테두리만 보여서 외곽선처럼 보인다.
// 두께는 화면 기준이라 모델 크기나 카메라 거리와 상관없이 비슷한 굵기로 보인다.
// 밝기가 천천히 오르내리며 빛나는 느낌을 준다 (_PulseSpeed, _PulseMin).
// 사용: InteractOutlineEffect 가 이 셰이더로 머티리얼을 자동으로 만든다 (Resources 폴더에 있어야 함).

Shader "Custom/InteractOutline"
{
    Properties
    {
        _OutlineColor ("외곽선 색", Color) = (1, 0.82, 0.2, 1)
        _OutlineWidth ("외곽선 두께 (화면 기준)", Range(0, 20)) = 8
        _PulseSpeed ("빛남 속도", Range(0, 10)) = 2.5
        _PulseMin ("가장 어두울 때 밝기", Range(0, 1)) = 0.6
    }

    SubShader
    {
        // 불투명 오브젝트 다음에 그려지도록 큐를 뒤로 미룬다
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+100"
        }

        Pass
        {
            Name "Outline"

            // 앞면을 잘라 뒷면만 그린다 (부풀린 껍데기의 바깥 테두리만 남김)
            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
                float _PulseSpeed;
                float _PulseMin;
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

            // 가장 어두운 값과 1 사이를 천천히 오가는 빛남 값
            float GetPulse()
            {
                float wave = 0.5 + 0.5 * sin(_Time.y * _PulseSpeed);
                return lerp(_PulseMin, 1.0, wave);
            }

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                // 원래 위치를 화면 좌표로 바꾼 뒤, 법선 방향으로 화면 기준 일정 두께만큼 민다
                float4 positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float3 normalCS = TransformWorldToHClipDir(normalWS);

                float2 offset = normalize(normalCS.xy + float2(0.0001, 0.0001));
                // 가로세로 비율 보정 (가로로 넓은 화면에서도 두께가 같아 보이게)
                offset.x *= _ScreenParams.y / _ScreenParams.x;

                // 빛날 때 아주 살짝 굵어진다
                float width = _OutlineWidth * lerp(0.85, 1.15, GetPulse());
                positionCS.xy += offset * width * 0.002 * positionCS.w;

                OUT.positionHCS = positionCS;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float pulse = GetPulse();
                return half4(_OutlineColor.rgb, _OutlineColor.a * pulse);
            }

            ENDHLSL
        }
    }
}
