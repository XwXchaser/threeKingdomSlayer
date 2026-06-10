Shader "Custom/EnemyOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0

        _OutlineColor ("Outline Color", Color) = (1, 0, 0, 1)
        _OutlineWidth ("Outline Width", Range(0, 10)) = 2.0
        _OutlineEnabled ("Outline Enabled", Float) = 0
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

        // Pass 0: Outline (rendered behind sprite)
        Pass
        {
            Name "Outline"

            CGPROGRAM
            #pragma vertex SpriteOutlineVert
            #pragma fragment SpriteOutlineFrag
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma shader_feature _ OUTLINE_ENABLED
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
            float _OutlineEnabled;

            v2f SpriteOutlineVert(appdata IN)
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

            fixed4 SpriteOutlineFrag(v2f IN) : SV_Target
            {
                fixed alpha = tex2D(_MainTex, IN.texcoord).a;

                // 采样4方向邻域 alpha
                float2 offset = _MainTex_TexelSize.xy * _OutlineWidth;
                fixed n = tex2D(_MainTex, IN.texcoord + float2(0, offset.y)).a;
                fixed s = tex2D(_MainTex, IN.texcoord + float2(0, -offset.y)).a;
                fixed e = tex2D(_MainTex, IN.texcoord + float2(offset.x, 0)).a;
                fixed w = tex2D(_MainTex, IN.texcoord + float2(-offset.x, 0)).a;

                fixed neighborMax = max(max(n, s), max(e, w));

                // 当前像素 alpha 低但邻域有高 alpha → 轮廓像素
                fixed isOutline = step(alpha, 0.1) * step(0.5, neighborMax);

                clip(isOutline * _OutlineEnabled - 0.5);

                return fixed4(_OutlineColor.rgb, isOutline * _OutlineEnabled) * IN.color;
            }
            ENDCG
        }

        // Pass 1: Normal sprite (same as Sprites-Default)
        Pass
        {
            Name "Normal"
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment SpriteFrag
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

            v2f SpriteVert(appdata IN)
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

            fixed4 SpriteFrag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
