Shader "Custom/InteractionHighlightOutline"
{
    // 상호작용 가능 기물 강조용 아웃라인 셰이더(인버티드 헐 기법).
    // 정점을 노멀 방향으로 부풀린 뒤 앞면을 컬링해서, 뒷면만 원본 실루엣 바깥 테두리로 보이게 한다.
    // ZTest는 기본값(LEqual)이라 벽 등에 가려지면 정상적으로 안 보인다.
    Properties
    {
        _OutlineColor("Outline Color", Color) = (1, 0.9, 0.2, 1)
        _OutlineWidth("Outline Width", Float) = 0.02
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Front

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings { float4 positionHCS : SV_POSITION; };

            CBUFFER_START(UnityPerMaterial)
            float4 _OutlineColor;
            float _OutlineWidth;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 inflated = IN.positionOS.xyz + normalize(IN.normalOS) * _OutlineWidth;
                OUT.positionHCS = TransformObjectToHClip(inflated);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}
