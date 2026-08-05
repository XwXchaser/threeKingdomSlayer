Shader "Custom/WeaponDirectionalPixelBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [PerRendererData] _MotionDirectionUV ("Motion Direction UV", Vector) = (1,0,0,0)
        [PerRendererData] _MotionStrengthPixels ("Motion Strength Pixels", Range(0, 64)) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
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
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment MotionBlurFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            float4 _MainTex_TexelSize;
            float4 _MotionDirectionUV;
            float _MotionStrengthPixels;

            fixed4 MotionBlurFrag(v2f IN) : SV_Target
            {
                float2 direction = _MotionDirectionUV.xy;
                float directionLength = max(length(direction), 0.0001);
                direction /= directionLength;

                float strength = max(_MotionStrengthPixels, 0.0);
                float2 texel = _MainTex_TexelSize.xy;
                float2 offset1 = round(direction * strength * 0.20) * texel;
                float2 offset2 = round(direction * strength * 0.40) * texel;
                float2 offset3 = round(direction * strength * 0.60) * texel;
                float2 offset4 = round(direction * strength * 0.80) * texel;
                float2 offset5 = round(direction * strength) * texel;

                fixed4 sample0 = SampleSpriteTexture(IN.texcoord);
                fixed4 sample1 = SampleSpriteTexture(IN.texcoord + offset1);
                fixed4 sample2 = SampleSpriteTexture(IN.texcoord + offset2);
                fixed4 sample3 = SampleSpriteTexture(IN.texcoord + offset3);
                fixed4 sample4 = SampleSpriteTexture(IN.texcoord + offset4);
                fixed4 sample5 = SampleSpriteTexture(IN.texcoord + offset5);

                float weight0 = 1.0;
                float weight1 = 0.78;
                float weight2 = 0.58;
                float weight3 = 0.40;
                float weight4 = 0.26;
                float weight5 = 0.14;
                float alphaSum = sample0.a * weight0 + sample1.a * weight1
                    + sample2.a * weight2 + sample3.a * weight3
                    + sample4.a * weight4 + sample5.a * weight5;
                float alpha = saturate(alphaSum);
                float3 colorSum = sample0.rgb * sample0.a * weight0
                    + sample1.rgb * sample1.a * weight1
                    + sample2.rgb * sample2.a * weight2
                    + sample3.rgb * sample3.a * weight3
                    + sample4.rgb * sample4.a * weight4
                    + sample5.rgb * sample5.a * weight5;
                float3 color = alphaSum > 0.0001 ? colorSum / alphaSum : 0;

                fixed4 result = fixed4(color, alpha) * IN.color;
                result.rgb *= result.a;
                return result;
            }
            ENDCG
        }
    }
}
