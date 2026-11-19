// 砸地裂纹特效：unlit + 径向显现（_Reveal 从 0 到 1 ⇒ 裂纹从中心向外"长"出来）
// 贴图中心 = 命中点；_Softness 控制显现边缘的硬度。
Shader "Custom/GroundCrackReveal"
{
    Properties
    {
        _MainTex ("Crack (RGBA)", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Reveal ("Reveal Radius (0=hidden,1=full)", Range(0,1)) = 0
        _Fade ("Fade", Range(0,1)) = 1
        _Softness ("Edge Softness", Range(0.001,0.6)) = 0.10
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _Reveal;
            float _Fade;
            float _Softness;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                float2 d = i.uv - 0.5;
                float r = length(d) * 2.0;                 // 0 = 中心, 1 = 外接圆
                // 显现边缘：r < _Reveal 可见，边界处按 _Softness 软化
                float edge = saturate((_Reveal - r) / max(_Softness, 0.0001));
                float a = c.a * edge * _Color.a * _Fade;
                clip(a - 0.004);
                return fixed4(c.rgb * _Color.rgb, a);
            }
            ENDCG
        }
    }
}
