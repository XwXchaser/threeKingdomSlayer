Shader "Custom/WeaponColorUnlit"
{
    Properties
    {
        _MainTex ("Authored Color", 2D) = "white" {}
        _Color ("Tint / Fade", Color) = (1,1,1,1)
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Destination Blend", Float) = 0
        [Enum(Off,0,On,1)] _ZWrite ("Depth Write", Float) = 1
        // ★ 命中确认闪白（REQ-006）：_Color 是**乘法染色乘数** ⇒ 改不亮 ⇒ 必须新增通道。
        //   范式同敌人 Custom/EnemyOutline 的 _PierceHighlightStrength。
        _FlashStrength ("Flash Strength", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" "IgnoreProjector"="True" }
        Cull Back
        ZTest LEqual
        ZWrite [_ZWrite]
        Blend [_SrcBlend] [_DstBlend]

        Pass
        {
            Name "AuthoredColor"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _FlashStrength;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.position = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                // The texture owns all painted light and material detail.
                // Both opaque and fade materials use this same colour path.
                fixed4 c = tex2D(_MainTex, i.uv) * _Color;
                // ★ 闪白（破防的「命中确认」层）：只有 _FlashStrength > 0 时才改写颜色，
                //   正常路径保持与改造前逐字一致（淡出淡入的观感不受影响）。
                float flash = saturate(_FlashStrength);
                if (flash > 0.001)
                {
                    c.rgb = lerp(c.rgb, fixed3(1.0, 1.0, 1.0), flash);
                    c.rgb *= c.a;
                }
                return c;
            }
            ENDCG
        }
    }
    Fallback Off
}
