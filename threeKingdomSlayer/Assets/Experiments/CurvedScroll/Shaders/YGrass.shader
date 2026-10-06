Shader "CurvedScroll/Y Grass"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _WindStrength("Wind Strength (height ratio)", Range(0, 0.35)) = 0.16
        _WindSpeed("Wind Speed (radians/sec)", Range(0, 4)) = 1.5
        _WindPhase("Wind Phase", Range(-10, 10)) = 0
        [HideInInspector] _WindTimeOverride("Wind Preview Time (-1 = automatic)", Float) = -1
        [HideInInspector] _RendererColor("Renderer Color", Color) = (1,1,1,1)
        [HideInInspector] _Flip("Flip", Vector) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" "DisableBatching"="True" }
        Cull Off ZWrite Off Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnitySprites.cginc"
            float _YSEnabled;
            float4 _YSPose;
            float4x4 _YSToLocal, _YSToWorld;
            float _WindStrength;
            float _WindSpeed;
            float _WindPhase;
            float _WindTimeOverride;
            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float4 vertex = UnityFlipSprite(input.vertex, _Flip);
                float3 world = mul(unity_ObjectToWorld, vertex).xyz;
                float fade = 1;
                if (_YSEnabled > .5)
                {
                    float3 anchor = mul(_YSToLocal, float4(unity_ObjectToWorld._m03_m13_m23, 1)).xyz;
                    float authoredPhase = dot(anchor.xz, float2(12.9898, 78.233)) + _WindPhase;
                    float2 delta = anchor.xz - _YSPose.xz;
                    float s = sin(_YSPose.w), c = cos(_YSPose.w);
                    anchor.x = c * delta.x - s * delta.y;
                    anchor.z = s * delta.x + c * delta.y;
                    float depth = anchor.z;
                    float bend = max(0, depth - 18);
                    anchor.y -= .009 * bend * bend * smoothstep(0, 6, bend);

                    // Cylindrical camera-facing card plus the saved authoring Y offset.
                    // Only grass uses this material; route sampling and other props are unchanged.
                    float3 eye = mul(_YSToLocal, float4(_WorldSpaceCameraPos, 1)).xyz;
                    float2 away = anchor.xz - eye.xz;
                    away /= max(length(away), .0001);
                    float3 authoredRight = mul((float3x3)_YSToLocal, unity_ObjectToWorld._m00_m10_m20);
                    float2 authorYaw = float2(authoredRight.x, -authoredRight.z);
                    authorYaw /= max(length(authorYaw), .0001);
                    float3 right = float3(away.y, 0, -away.x);
                    float3 rotatedRight = float3(authorYaw.x * right.x + authorYaw.y * right.z, 0,
                                               -authorYaw.y * right.x + authorYaw.x * right.z);
                    float sx = length(unity_ObjectToWorld._m00_m10_m20);
                    float sy = length(unity_ObjectToWorld._m01_m11_m21);
                    float windTime = _WindTimeOverride >= 0 ? _WindTimeOverride : _Time.y;
                    float sway = sin(windTime * _WindSpeed + authoredPhase) * .72
                               + sin(windTime * (_WindSpeed * .47) + authoredPhase * 1.71) * .28;
                    // Bottom-pivot sprites keep their roots fixed. With a subdivided
                    // grass mesh the quadratic weight produces a visible soft bend.
                    float height01 = saturate(input.texcoord.y);
                    float bendWeight = height01 * height01;
                    float windOffset = sway * _WindStrength * max(vertex.y, 0) * sy * bendWeight;
                    anchor += rotatedRight * (vertex.x * sx + windOffset);
                    anchor.y += vertex.y * sy;
                    world = mul(_YSToWorld, float4(anchor, 1)).xyz;
                    fade = saturate((depth + 14) / 3) * saturate((80 - depth) / 10);
                }
                output.vertex = mul(UNITY_MATRIX_VP, float4(world, 1));
                output.texcoord = input.texcoord;
                output.color = input.color * _Color * _RendererColor;
                output.color.a *= fade;
                return output;
            }
            fixed4 frag(v2f input):SV_Target
            {
                fixed4 color = SampleSpriteTexture(input.texcoord) * input.color;
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
