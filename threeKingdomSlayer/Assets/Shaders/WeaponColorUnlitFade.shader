Shader "Custom/WeaponColorUnlitFade"
{
    Properties
    {
        _MainTex ("Authored Color", 2D) = "white" {}
        _Color ("Tint / Fade", Color) = (1,1,1,1)
        [HideInInspector] _SrcBlend ("Source Blend", Float) = 5
        [HideInInspector] _DstBlend ("Destination Blend", Float) = 10
        [HideInInspector] _ZWrite ("Color Pass Depth Write", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Back
        ZTest LEqual

        // This single-submesh weapon contains overlapping metal shells. Select
        // the nearest surface before blending so fade does not reveal unsorted
        // inner triangles. Fully invisible weapons must not write scene depth.
        Pass
        {
            Name "VisibleSurfaceDepth"
            ZWrite On
            ColorMask 0
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragDepth
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

            fixed4 fragDepth(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                clip(tex2D(_MainTex, i.uv).a * _Color.a - 0.00001);
                return 0;
            }
            ENDCG
        }

        UsePass "Custom/WeaponColorUnlit/AUTHOREDCOLOR"
    }
    Fallback Off
}
