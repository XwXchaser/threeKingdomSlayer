Shader "Custom/WeaponMangaSweep"
{
    Properties
    {
        _BrushTex ("Brush Mask", 2D) = "white" {}
        _Color ("Overall Tint", Color) = (1,1,1,1)
        _CoreColor ("Ivory Core", Color) = (1,0.96,0.82,0.96)
        _GoldColor ("Warm Gold", Color) = (1,0.48,0.08,0.92)
        _RedColor ("Vermilion Edge", Color) = (0.72,0.055,0.025,0.88)
        _EdgeWidth ("Red Edge Width (across stroke)", Range(0.01,0.6)) = 0.18
        _CoreWidth ("Ivory Core Width (across stroke)", Range(0.05,1)) = 0.5
        _AgePower ("Age Fade", Range(0.5,4)) = 1.35
        _BrushBreakup ("Brush Breakup", Range(0,1)) = 0.16
        _TipCoreBoost ("Tip Core Boost", Range(0,1)) = 0.3
        _DebugGeometry ("Debug Across / Vertex Color", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One OneMinusSrcAlpha
        ZTest LEqual
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 brush : TEXCOORD1;
                float2 across : TEXCOORD2;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 brush : TEXCOORD1;
                float2 across : TEXCOORD2;
                fixed4 color : COLOR;
            };

            sampler2D _BrushTex;
            float4 _BrushTex_ST;
            fixed4 _Color;
            fixed4 _CoreColor;
            fixed4 _GoldColor;
            fixed4 _RedColor;
            float _EdgeWidth;
            float _CoreWidth;
            float _AgePower;
            float _BrushBreakup;
            float _TipCoreBoost;
            float _DebugGeometry;

            v2f vert(appdata v)
            {
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.brush = v.brush;
                o.across = v.across;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // uv.x is the age of this time slice; uv.y is normalized shaft length
                // (0 tail, 1 tip). TEXCOORD2.x is the signed position across this stroke's
                // own width: -1 inner rim, +1 leading rim. Each stroke therefore colors
                // itself instead of being cut out of a shared surface by fixed UV ranges.
                float age = saturate(i.uv.x);
                float shaft = saturate(i.uv.y);
                float across = i.across.x;
                float rim = abs(across);
                // Body fades only across the outermost sliver, so the vermilion rim below
                // stays inside the opaque part of the stroke instead of being faded away.
                const float feather = 0.04;
                float body = 1.0 - smoothstep(1.0 - feather, 1.0, rim);

                // Ivory core along the middle of the stroke, warm gold toward both rims.
                float core = 1.0 - smoothstep(_CoreWidth * 0.35, max(_CoreWidth, 0.02), rim);
                core = saturate(core + _TipCoreBoost * shaft * (1.0 - rim));
                float3 rgb = lerp(_GoldColor.rgb, _CoreColor.rgb, smoothstep(0.15, 0.7, core));

                // Vermilion edge rides the convex (leading) rim, reaching full saturation
                // before the body fade so it reads as a distinct outer stroke.
                float edgeWidth = max(_EdgeWidth, 0.001);
                float edge = smoothstep(1.0 - edgeWidth, 1.0 - feather * 1.5, across);
                float edgeBrush = smoothstep(0.16, 0.3, i.brush.x) * (1.0 - smoothstep(0.78, 0.9, i.brush.x));
                float redEdge = saturate(edge * edgeBrush);
                rgb = lerp(rgb, _RedColor.rgb, redEdge);

                float2 brushUv = float2(i.brush.x, shaft * 2.0);
                float brush = tex2D(_BrushTex, TRANSFORM_TEX(brushUv, _BrushTex)).a;
                // Keep the mask out of body alpha. Even a rim-only mask reads as extra claw marks here.
                float rimDetail = lerp(1.0, brush, _BrushBreakup * smoothstep(0.96, 1.0, rim));
                rgb *= lerp(1.0, rimDetail, redEdge);

                float ageFade = pow(saturate(1.0 - age), _AgePower);
                float colorAlpha = lerp(_CoreColor.a, _RedColor.a, redEdge);
                float alpha = body * ageFade * i.color.a * _Color.a * colorAlpha;
                clip(alpha - 0.004);

                rgb = lerp(rgb, float3(rim, shaft, across * 0.5 + 0.5), _DebugGeometry);
                return fixed4(rgb * _Color.rgb * alpha, alpha);
            }
            ENDCG
        }
    }
    Fallback Off
}
