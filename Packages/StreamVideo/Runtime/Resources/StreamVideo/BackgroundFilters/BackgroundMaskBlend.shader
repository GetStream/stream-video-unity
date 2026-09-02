Shader "Hidden/StreamVideo/BackgroundMaskBlend"
{
    Properties
    {
        _MainTex ("Original", 2D) = "white" {}
        _Blurred ("Blurred", 2D) = "white" {}
        _Mask ("Mask", 2D) = "black" {}
        _SmoothMin ("Smooth Min", Range(0, 1)) = 0.45
        _SmoothMax ("Smooth Max", Range(0, 1)) = 0.8
        _ExpandPixels ("Expand Pixels", Float) = 3
        _DebugMode ("Debug Mode", Float) = 0
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
            float4 _Mask_TexelSize;
            float _SmoothMin;
            float _SmoothMax;
            float _ExpandPixels;
            float _DebugMode;

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

            float SampleDilatedMask(float2 uv)
            {
                float2 t = _Mask_TexelSize.xy * _ExpandPixels;
                float m = tex2D(_Mask, uv).r;
                m = max(m, tex2D(_Mask, uv + float2(t.x, 0)).r);
                m = max(m, tex2D(_Mask, uv - float2(t.x, 0)).r);
                m = max(m, tex2D(_Mask, uv + float2(0, t.y)).r);
                m = max(m, tex2D(_Mask, uv - float2(0, t.y)).r);
                m = max(m, tex2D(_Mask, uv + t).r);
                m = max(m, tex2D(_Mask, uv - t).r);
                m = max(m, tex2D(_Mask, uv + float2(t.x, -t.y)).r);
                m = max(m, tex2D(_Mask, uv + float2(-t.x, t.y)).r);
                return m;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 original = tex2D(_MainTex, i.uv);
                float4 blurred = tex2D(_Blurred, i.uv);
                float mask = SampleDilatedMask(i.uv);
                float person = smoothstep(_SmoothMin, _SmoothMax, mask);
                float4 color = lerp(blurred, original, person);

                if (_DebugMode > 1.5)
                {
                    float3 debug = lerp(float3(1, 0, 0), float3(0, 1, 0), person);
                    return float4(lerp(color.rgb, debug, 0.5), 1);
                }

                if (_DebugMode > 0.5)
                {
                    return float4(mask, mask, mask, 1);
                }

                return color;
            }
            ENDCG
        }
    }
    Fallback Off
}
