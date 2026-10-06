Shader "CurvedScroll/Y Route Surface"
{
    Properties
    {
        _MainTex("Surface", 2D) = "white" {}
        _Tint("Tint", Color) = (1,1,1,1)
        _NearClip("Near clip", Float) = -80
        _FarClip("Far clip", Float) = 140
    }
    SubShader
    {
        Tags { "Queue"="Geometry+2" "RenderType"="Opaque" }
        Cull Off
        ZWrite On
        Lighting Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Tint;
            float _YSEnabled;
            float _NearClip;
            float _FarClip;
            float4 _YSPose;
            float4x4 _YSToLocal;
            float4x4 _YSToWorld;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float z : TEXCOORD1;
            };

            float3 ProjectY(float3 p)
            {
                float2 q = p.xz - _YSPose.xz;
                float s = sin(_YSPose.w);
                float c = cos(_YSPose.w);
                p.x = c * q.x - s * q.y;
                p.z = s * q.x + c * q.y;
                return p;
            }

            v2f vert(appdata input)
            {
                v2f output;
                float3 world = mul(unity_ObjectToWorld, input.vertex).xyz;
                float3 local = mul(_YSToLocal, float4(world, 1)).xyz;
                if (_YSEnabled > 0.5)
                {
                    local = ProjectY(local);
                    world = mul(_YSToWorld, float4(local, 1)).xyz;
                }
                output.vertex = mul(UNITY_MATRIX_VP, float4(world, 1));
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.z = local.z;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                if (_YSEnabled > 0.5)
                    clip(min(input.z - _NearClip, _FarClip - input.z));
                return tex2D(_MainTex, input.uv) * _Tint;
            }
            ENDCG
        }
    }
}
