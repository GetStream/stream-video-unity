Shader "Hidden/StreamVideo/BackgroundSeparableBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Mask ("Person Mask", 2D) = "black" {}
        _Direction ("Direction", Vector) = (1, 0, 0, 0)
        _Spread ("Spread", Float) = 1
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            sampler2D _Mask;
            float2 _Direction;
            float _Spread;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 stepUv = _Direction * _MainTex_TexelSize.xy * _Spread;
                float weightSum = (1.0 - tex2D(_Mask, i.uv).r) * 0.227027;
                float4 color = tex2D(_MainTex, i.uv) * weightSum;

                float2 uv1 = i.uv + stepUv;
                float w1 = (1.0 - tex2D(_Mask, uv1).r) * 0.1945946;
                weightSum += w1;
                color += tex2D(_MainTex, uv1) * w1;

                float2 uv1n = i.uv - stepUv;
                float w1n = (1.0 - tex2D(_Mask, uv1n).r) * 0.1945946;
                weightSum += w1n;
                color += tex2D(_MainTex, uv1n) * w1n;

                float2 uv2 = i.uv + stepUv * 2;
                float w2 = (1.0 - tex2D(_Mask, uv2).r) * 0.1216216;
                weightSum += w2;
                color += tex2D(_MainTex, uv2) * w2;

                float2 uv2n = i.uv - stepUv * 2;
                float w2n = (1.0 - tex2D(_Mask, uv2n).r) * 0.1216216;
                weightSum += w2n;
                color += tex2D(_MainTex, uv2n) * w2n;

                float2 uv3 = i.uv + stepUv * 3;
                float w3 = (1.0 - tex2D(_Mask, uv3).r) * 0.054054;
                weightSum += w3;
                color += tex2D(_MainTex, uv3) * w3;

                float2 uv3n = i.uv - stepUv * 3;
                float w3n = (1.0 - tex2D(_Mask, uv3n).r) * 0.054054;
                weightSum += w3n;
                color += tex2D(_MainTex, uv3n) * w3n;

                float2 uv4 = i.uv + stepUv * 4;
                float w4 = (1.0 - tex2D(_Mask, uv4).r) * 0.016216;
                weightSum += w4;
                color += tex2D(_MainTex, uv4) * w4;

                float2 uv4n = i.uv - stepUv * 4;
                float w4n = (1.0 - tex2D(_Mask, uv4n).r) * 0.016216;
                weightSum += w4n;
                color += tex2D(_MainTex, uv4n) * w4n;

                return color / max(weightSum, 1e-5);
            }
            ENDCG
        }
    }
    Fallback Off
}
