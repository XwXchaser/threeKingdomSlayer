Shader "Hidden/BlurEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurSize ("Blur Size", Range(0, 10)) = 1
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // Pass 0: Downsample (box filter to half-res)
        Pass
        {
            Name "Downsample"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _BlurSize;

            v2f vert(appdata_img v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord; return o; }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 ts = _MainTex_TexelSize.xy * _BlurSize;
                fixed4 c = tex2D(_MainTex, i.uv) * 4;
                c += tex2D(_MainTex, i.uv + float2(ts.x, ts.y));
                c += tex2D(_MainTex, i.uv + float2(-ts.x, ts.y));
                c += tex2D(_MainTex, i.uv + float2(ts.x, -ts.y));
                c += tex2D(_MainTex, i.uv + float2(-ts.x, -ts.y));
                return c * 0.125;
            }
            ENDCG
        }

        // Pass 1: Horizontal blur (5-tap)
        Pass
        {
            Name "Horizontal"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _BlurSize;

            v2f vert(appdata_img v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord; return o; }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 ts = _MainTex_TexelSize * _BlurSize;
                fixed4 c = tex2D(_MainTex, i.uv) * 0.227027;
                c += tex2D(_MainTex, i.uv + float2(ts.x, 0)) * 0.316216;
                c += tex2D(_MainTex, i.uv - float2(ts.x, 0)) * 0.316216;
                c += tex2D(_MainTex, i.uv + float2(ts.x * 2, 0)) * 0.070270;
                c += tex2D(_MainTex, i.uv - float2(ts.x * 2, 0)) * 0.070270;
                return c;
            }
            ENDCG
        }

        // Pass 2: Vertical blur (5-tap)
        Pass
        {
            Name "Vertical"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _BlurSize;

            v2f vert(appdata_img v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord; return o; }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 ts = _MainTex_TexelSize * _BlurSize;
                fixed4 c = tex2D(_MainTex, i.uv) * 0.227027;
                c += tex2D(_MainTex, i.uv + float2(0, ts.y)) * 0.316216;
                c += tex2D(_MainTex, i.uv - float2(0, ts.y)) * 0.316216;
                c += tex2D(_MainTex, i.uv + float2(0, ts.y * 2)) * 0.070270;
                c += tex2D(_MainTex, i.uv - float2(0, ts.y * 2)) * 0.070270;
                return c;
            }
            ENDCG
        }
    }
}
