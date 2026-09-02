Shader "Hidden/StreamVideo/BackgroundMaskTemporal"
{
    Properties
    {
        _MainTex ("New Mask", 2D) = "white" {}
        _PrevMask ("Previous Mask", 2D) = "black" {}
        _Smoothing ("Smoothing", Range(0, 1)) = 0.8
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
            sampler2D _PrevMask;
            float _Smoothing;

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
                float newMask = tex2D(_MainTex, i.uv).r;
                float prevMask = tex2D(_PrevMask, i.uv).r;
                // Symmetric EMA so background pixels decay. The previous formula used
                // alpha = smoothing * newMask, which froze old person pixels at 0.
                float mask = lerp(prevMask, newMask, _Smoothing);
                return float4(mask, mask, mask, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
