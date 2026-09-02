Shader "Hidden/StreamVideo/BackgroundMaskBlend"
{
    Properties
    {
        _MainTex ("Original", 2D) = "white" {}
        _Blurred ("Blurred", 2D) = "white" {}
        _Mask ("Mask", 2D) = "black" {}
        _SmoothMin ("Smooth Min", Range(0, 1)) = 0.6
        _SmoothMax ("Smooth Max", Range(0, 1)) = 0.9
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
            sampler2D _Blurred;
            sampler2D _Mask;
            float _SmoothMin;
            float _SmoothMax;

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
                float4 original = tex2D(_MainTex, i.uv);
                float4 blurred = tex2D(_Blurred, i.uv);
                float mask = tex2D(_Mask, i.uv).r;
                float person = smoothstep(_SmoothMin, _SmoothMax, mask);
                return lerp(blurred, original, person);
            }
            ENDCG
        }
    }
    Fallback Off
}
