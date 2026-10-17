Shader "CurvedScroll/Y Village Wall Prototype"
{
    Properties
    {
        [NoScaleOffset] _MainTex("Stone surface atlas", 2D) = "white" {}
        _Color("Face tint", Color) = (1,1,1,1)
        _OutlineColor("Edge tint", Color) = (0.035,0.035,0.035,1)
        _OutlineWidth("Edge width (world units)", Range(0.005,0.15)) = 0.035
        _AtlasSize("Atlas size (px)", Vector) = (2048,2048,0,0)
        _SideARect("Side A rect (top-left px)", Vector) = (64,64,1664,816)
        _SideBRect("Side B rect (top-left px)", Vector) = (64,912,1664,1664)
        _TopRect("Top rect (top-left px)", Vector) = (64,1760,1664,1980)
        _EndRect("End rect (top-left px)", Vector) = (1760,64,1980,816)
    }
    SubShader
    {
        Tags { "Queue"="Geometry+5" "RenderType"="Opaque" "DisableBatching"="True" }
        Cull Off
        ZWrite On
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Color, _OutlineColor;
            float _OutlineWidth;
            float4 _AtlasSize;
            float4 _SideARect, _SideBRect, _TopRect, _EndRect;
            float _YSEnabled, _YSViewDistance;
            float4 _YSPose;
            float4x4 _YSToLocal, _YSToWorld;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float2 faceSize : TEXCOORD1;
                fixed4 color : COLOR;
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 faceSize : TEXCOORD1;
                fixed4 color : COLOR;
                float sideSelector : TEXCOORD3;
                float depth : TEXCOORD2;
            };
            v2f vert(appdata input)
            {
                v2f output;
                float3 world = mul(unity_ObjectToWorld, input.vertex).xyz;
                float3 p = mul(_YSToLocal, float4(world, 1)).xyz;
                if (_YSEnabled > 0.5)
                {
                    float2 q = p.xz - _YSPose.xz;
                    float s = sin(_YSPose.w), c = cos(_YSPose.w);
                    p.x = c * q.x - s * q.y;
                    p.z = s * q.x + c * q.y;
                    float bend = max(0, p.z - 18);
                    p.y -= .009 * bend * bend * smoothstep(0, 6, bend);
                    world = mul(_YSToWorld, float4(p, 1)).xyz;
                }
                output.pos = mul(UNITY_MATRIX_VP, float4(world, 1));
                output.uv = input.uv;
                output.faceSize = input.faceSize;
                output.color = input.color;
                output.sideSelector = input.vertex.x > 0.0 ? 1.0 : 0.0;
                output.depth = p.z;
                return output;
            }
            float2 AtlasUV(float2 faceUV, float4 rect)
            {
                float2 atlasSize = max(_AtlasSize.xy, float2(1, 1));
                float2 rectMin = (float2(rect.x, atlasSize.y - rect.w) + 0.5) / atlasSize;
                float2 rectMax = (float2(rect.z, atlasSize.y - rect.y) - 0.5) / atlasSize;
                return lerp(rectMin, rectMax, saturate(faceUV));
            }

            fixed4 frag(v2f input) : SV_Target
            {
                if (_YSEnabled > 0.5)
                    clip(min(input.depth + 14, _YSViewDistance - input.depth));
                float2 d = min(input.uv, 1 - input.uv) * input.faceSize;
                float edge = min(d.x, d.y);
                float aa = max(fwidth(edge), .001);
                float body = smoothstep(_OutlineWidth - aa, _OutlineWidth + aa, edge);

                // Mesh uv2 stores the authored physical face size. The local x sign
                // distinguishes the two long wall faces; the atlas cells stay explicit
                // and Inspector-traceable without changing the wall mesh.
                float2 atlasUV;
                if (input.faceSize.x > 1.0 && input.faceSize.y > 1.0)
                    atlasUV = AtlasUV(input.uv, input.sideSelector > 0.5 ? _SideBRect : _SideARect);
                else if (input.faceSize.x > 1.0)
                    atlasUV = AtlasUV(input.uv, _TopRect);
                else
                    atlasUV = AtlasUV(input.uv, _EndRect);

                fixed4 stone = tex2D(_MainTex, atlasUV) * input.color * _Color;
                stone.a = 1;
                return lerp(_OutlineColor, stone, body);
            }
            ENDCG
        }
    }
}
