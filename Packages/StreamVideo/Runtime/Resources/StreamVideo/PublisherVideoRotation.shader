Shader "Hidden/StreamVideo/PublisherVideoRotation"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _UvTransform ("UV Transform", Vector) = (1, 0, 0, 1)
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
            // Row-major 2x2 matrix (x, y | z, w) applied around the texture center
            float4 _UvTransform;

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
                float2 centered = i.uv - 0.5;
                float2 source = float2(
                    dot(_UvTransform.xy, centered),
                    dot(_UvTransform.zw, centered)) + 0.5;
                return tex2D(_MainTex, source);
            }
            ENDCG
        }
    }
    Fallback Off
}
