Shader "Custom/SpikeTrapOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (0.8, 0.8, 0.8, 1)
        _OutlineWidth ("Outline Width", Range(0, 10)) = 2.0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent+100"
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
            CGPROGRAM
            #pragma vertex SpriteOutlineVert
            #pragma fragment SpriteOutlineFrag
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
                float alpha = tex2D(_MainTex, IN.texcoord).a;

                float2 offset = _MainTex_TexelSize.xy * _OutlineWidth;
                float n = tex2D(_MainTex, IN.texcoord + float2(0, offset.y)).a;
                float s = tex2D(_MainTex, IN.texcoord + float2(0, -offset.y)).a;
                float e = tex2D(_MainTex, IN.texcoord + float2(offset.x, 0)).a;
                float w = tex2D(_MainTex, IN.texcoord + float2(-offset.x, 0)).a;

                float neighborMax = max(max(n, s), max(e, w));
                float isOutline = step(alpha, 0.1) * step(0.5, neighborMax);

                clip(isOutline - 0.5);
                return fixed4(_OutlineColor.rgb, isOutline) * IN.color;
            }
            ENDCG
        }
    }
}
