Shader "Custom/FactionIconURP"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _ColorWhite ("White Replacement (Primary)", Color) = (1,1,1,1)
        _ColorBlack ("Black Replacement (Secondary)", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "PreviewType"="Plane"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

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
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorWhite;
                float4 _ColorBlack;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                half lum = dot(col.rgb, half3(0.299, 0.587, 0.114));

                half4 finalCol = lerp(_ColorBlack, _ColorWhite, lum);

                finalCol.a = col.a * input.color.a;
                return finalCol;
            }
            ENDHLSL
        }
    }
}