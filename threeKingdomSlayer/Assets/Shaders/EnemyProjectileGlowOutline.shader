Shader "Custom/EnemyProjectileGlowOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HDR] _OutlineColor ("Outline Color", Color) = (1.5,0.02,0.01,1)
        _OutlineWidth ("Outline Width", Range(0,10)) = 1.5
        [HDR] _GlowColor ("Glow Color", Color) = (1.2,0.01,0,0.45)
        _GlowWidth ("Glow Width", Range(0,16)) = 4
        _GlowPulseSpeed ("Glow Pulse Speed", Range(0,10)) = 3
        _GlowPulseAmount ("Glow Pulse Amount", Range(0,1)) = 0.18
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "GlowOutline"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _OutlineColor;
            float _OutlineWidth;
            fixed4 _GlowColor;
            float _GlowWidth;
            float _GlowPulseSpeed;
            float _GlowPulseAmount;

            v2f vert(appdata IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color;
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif
                return OUT;
            }

            fixed SampleMaxAlpha(float2 uv, float2 offset)
            {
                fixed alpha = 0;
                alpha = max(alpha, tex2D(_MainTex, uv + float2( offset.x, 0)).a);
                alpha = max(alpha, tex2D(_MainTex, uv + float2(-offset.x, 0)).a);
                alpha = max(alpha, tex2D(_MainTex, uv + float2(0,  offset.y)).a);
                alpha = max(alpha, tex2D(_MainTex, uv + float2(0, -offset.y)).a);
                alpha = max(alpha, tex2D(_MainTex, uv + float2( offset.x,  offset.y)).a);
                alpha = max(alpha, tex2D(_MainTex, uv + float2(-offset.x,  offset.y)).a);
                alpha = max(alpha, tex2D(_MainTex, uv + float2( offset.x, -offset.y)).a);
                alpha = max(alpha, tex2D(_MainTex, uv + float2(-offset.x, -offset.y)).a);
                return alpha;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed spriteAlpha = tex2D(_MainTex, IN.texcoord).a;
                float2 outlineOffset = _MainTex_TexelSize.xy * _OutlineWidth;
                float2 glowOffset = _MainTex_TexelSize.xy * _GlowWidth;
                float2 midGlowOffset = glowOffset * 0.55;

                fixed outlineNeighbor = SampleMaxAlpha(IN.texcoord, outlineOffset);
                fixed glowNeighbor = max(SampleMaxAlpha(IN.texcoord, glowOffset), SampleMaxAlpha(IN.texcoord, midGlowOffset));
                fixed outside = 1.0 - smoothstep(0.04, 0.25, spriteAlpha);
                fixed outlineMask = outside * smoothstep(0.18, 0.65, outlineNeighbor);
                fixed glowMask = outside * smoothstep(0.03, 0.55, glowNeighbor) * (1.0 - outlineMask);

                float pulse = 1.0 + sin(_Time.y * _GlowPulseSpeed) * _GlowPulseAmount;
                fixed vertexAlpha = IN.color.a;
                fixed4 outline = fixed4(_OutlineColor.rgb * outlineMask, _OutlineColor.a * outlineMask) * vertexAlpha;
                fixed4 glow = fixed4(_GlowColor.rgb * glowMask * pulse, _GlowColor.a * glowMask * pulse) * vertexAlpha;
                fixed4 result = outline + glow;
                result.rgb *= result.a;
                clip(result.a - 0.003);
                return result;
            }
            ENDCG
        }

        Pass
        {
            Name "Normal"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;

            v2f vert(appdata IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, IN.texcoord) * IN.color;
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
