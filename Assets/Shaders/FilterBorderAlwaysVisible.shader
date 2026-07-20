Shader "Custom/FilterBorderAlwaysVisible"
{
    // 필터 테두리 전용 셰이더. 렌더 큐를 채움(fill)보다 뒤로 둬서, 필터 자기 자신의 채움 메시에
    // 가려지는 모서리도 항상 위에 그려지게 한다(벽 등 다른 오브젝트에는 정상적으로 가려짐 — ZTest 기본값 유지).
    Properties
    {
        _BaseColor("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+10" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionHCS : SV_POSITION; };

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return _BaseColor;
            }
            ENDHLSL
        }
    }
}
