Shader "Hidden/StreamVideo/BackgroundSeparableBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
                float2 step = _Direction * _MainTex_TexelSize.xy * _Spread;
                float4 color = tex2D(_MainTex, i.uv) * 0.227027;
                color += tex2D(_MainTex, i.uv + step) * 0.1945946;
                color += tex2D(_MainTex, i.uv - step) * 0.1945946;
                color += tex2D(_MainTex, i.uv + step * 2) * 0.1216216;
                color += tex2D(_MainTex, i.uv - step * 2) * 0.1216216;
                color += tex2D(_MainTex, i.uv + step * 3) * 0.054054;
                color += tex2D(_MainTex, i.uv - step * 3) * 0.054054;
                color += tex2D(_MainTex, i.uv + step * 4) * 0.016216;
                color += tex2D(_MainTex, i.uv - step * 4) * 0.016216;
                return color;
            }
            ENDCG
        }
    }
    Fallback Off
}
