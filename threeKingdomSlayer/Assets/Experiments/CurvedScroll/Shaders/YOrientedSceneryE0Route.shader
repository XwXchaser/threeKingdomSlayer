Shader "CurvedScroll/Y Oriented Scenery E0 Route"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite",2D)="white"{}
        _Color("Tint",Color)=(1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor",Color)=(1,1,1,1)
        [HideInInspector] _Flip("Flip",Vector)=(1,1,1,1)
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
            float _YSEnabled, _YSViewDistance;
            float4 _YSPose;
            float4x4 _YSToLocal, _YSToWorld;

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
                    float2 delta = anchor.xz - _YSPose.xz;
                    float s = sin(_YSPose.w), c = cos(_YSPose.w);
                    anchor.x = c * delta.x - s * delta.y;
                    anchor.z = s * delta.x + c * delta.y;
                    float depth = anchor.z;
                    float bend = max(0, depth - 18);
                    anchor.y -= .009 * bend * bend * smoothstep(0, 6, bend);

                    // Keep the card camera-facing while applying the authored
                    // local X direction. Fences can therefore follow a branch
                    // tangent instead of becoming a horizontal screen barrier.
                    float3 eye = mul(_YSToLocal, float4(_WorldSpaceCameraPos, 1)).xyz;
                    float2 away = anchor.xz - eye.xz;
                    away /= max(length(away), .0001);
                    float3 authoredRight3 = mul((float3x3)_YSToLocal, unity_ObjectToWorld._m00_m10_m20);
                    float2 authoredRight = float2(authoredRight3.x, -authoredRight3.z);
                    authoredRight /= max(length(authoredRight), .0001);
                    float3 cameraRight = float3(away.y, 0, -away.x);
                    float3 orientedRight = float3(
                        authoredRight.x * cameraRight.x + authoredRight.y * cameraRight.z,
                        0,
                        -authoredRight.y * cameraRight.x + authoredRight.x * cameraRight.z);
                    float sx = length(unity_ObjectToWorld._m00_m10_m20);
                    float sy = length(unity_ObjectToWorld._m01_m11_m21);
                    anchor += orientedRight * (vertex.x * sx);
                    anchor.y += vertex.y * sy;
                    world = mul(_YSToWorld, float4(anchor, 1)).xyz;
                    float fadeEnd = max(80, _YSViewDistance + 12);
                    fade = saturate((depth + 14) / 3) * saturate((fadeEnd - depth) / 10);
                }
                output.vertex = mul(UNITY_MATRIX_VP, float4(world, 1));
                output.texcoord = input.texcoord;
                output.color = input.color * _Color * _RendererColor;
                output.color.a *= fade;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 color = SampleSpriteTexture(input.texcoord) * input.color;
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
