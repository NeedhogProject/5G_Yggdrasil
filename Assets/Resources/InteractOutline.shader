// InteractOutline.shader
// 상호작용 오브젝트 외곽선 전용 셰이더 (URP Unlit)
// 원리: 메시를 법선 방향으로 살짝 부풀린 뒤, 앞면을 잘라내고(Cull Front) 뒷면만 단색으로 그린다.
//       그러면 원본 메시 바깥으로 삐져나온 테두리만 보여서 외곽선처럼 보인다.
// 사용: 이 셰이더로 머티리얼 1개를 만들어 InteractableHighlight 의 Outline Material 칸에 연결한다.

Shader "Custom/InteractOutline"
{
    Properties
    {
        _OutlineColor ("외곽선 색", Color) = (1, 0.85, 0.3, 1)
        _OutlineWidth ("외곽선 두께", Range(0, 10)) = 3
    }

    SubShader
    {
        // 불투명 오브젝트 다음에 그려지도록 큐를 뒤로 미룬다
        Tags
        {
            "RenderType" = "Opaque"
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

            float4 _OutlineColor;
            float _OutlineWidth;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;

                // 정점을 법선 방향으로 밀어 메시를 부풀린다 (두께 값은 0.001 단위로 보정)
                float3 inflatedOS = IN.positionOS.xyz + IN.normalOS * (_OutlineWidth * 0.001);
                OUT.positionHCS = TransformObjectToHClip(inflatedOS);

                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }

            ENDHLSL
        }
    }
}
