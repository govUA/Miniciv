Shader "Custom/BorderStripes"
{
    Properties
    {
        _PrimaryColor ("Primary Color", Color) = (1, 0, 0, 1)
        _SecondaryColor ("Secondary Color", Color) = (0, 0, 1, 1)
        _StripeDensity ("Stripe Density", Float) = 5.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _PrimaryColor;
                float4 _SecondaryColor;
                float _StripeDensity;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float value = frac(i.uv.x * _StripeDensity);

                float pattern = step(0.5, value);

                half4 finalColor = lerp(_PrimaryColor, _SecondaryColor, pattern);

                return finalColor;
            }
            ENDHLSL
        }
    }
}