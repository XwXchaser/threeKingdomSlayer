// Vertex wind for authored scenery (trees, bushes, banners) on the Y route.
// Same cost model as YGrass: a real subdivided mesh, sway driven by _Time inside the
// vertex shader, per-prop phase from the authored route position.
// Difference from grass: grass bends as a whole. Trees and flags must keep their trunk
// and pole rigid, so the sway weight is shaped by an authored content rect plus a mode:
//   0 = tree  (height ramp, trunk below the canopy stays fixed)
//   1 = bush  (height power curve, root fixed, whole canopy bends like grass)
//   2 = flag  (horizontal band starting right of the pole, free edge bends most)
Shader "CurvedScroll/Y Scenery Wind"
{
    Properties
    {
        _MainTex("Sprite", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _UVRect("Sprite UV Rect (xy min, zw size)", Vector) = (0,0,1,1)
        _ContentRect("Content Rect (xy min, zw size in sprite space)", Vector) = (0,0,1,1)
        _WindMode("Wind Mode (0 tree / 1 bush / 2 flag)", Range(0, 2)) = 0
        _WindStart("Wind Start (canopy height / cloth band start)", Range(0, 1)) = 0.3
        _WindEnd("Wind End (canopy feather / cloth band softness)", Range(0, 1)) = 0.6
        _WindPower("Wind Power (bush height curve)", Range(0.2, 4)) = 1.7
        _WindStrength("Wind Strength (fraction of height)", Range(0, 0.25)) = 0.02
        _WindSpeed("Wind Speed (radians/sec)", Range(0, 6)) = 1
        _WindPhase("Wind Phase", Range(-10, 10)) = 0
        _WindWave("Wind Flutter", Range(0, 3)) = 0.3
        _WindWaveFrequency("Wind Flutter Frequency", Range(0, 12)) = 4
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

            float _YSEnabled, _YSViewDistance;
            float4 _YSPose;
            float4x4 _YSToLocal, _YSToWorld;
            float4 _UVRect, _ContentRect;
            float _WindMode, _WindStart, _WindEnd, _WindPower;
            float _WindStrength, _WindSpeed, _WindPhase, _WindWave, _WindWaveFrequency;
            float _WindTimeOverride;

            struct wind_appdata
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct wind_v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float WindWeight(float2 spriteUv)
            {
                float2 c = spriteUv - _ContentRect.xy;
                float2 n = c / max(_ContentRect.zw, float2(.0001, .0001));
                n = saturate(n);
                if (_WindMode < .5)
                {
                    // Tree: trunk below the canopy stays rigid, the crown bends and the
                    // very top travels the most.
                    return smoothstep(_WindStart, _WindEnd, n.y) * lerp(.55, 1., n.y);
                }
                if (_WindMode < 1.5)
                {
                    // Bush: no trunk at all, so this is the grass curve with a fixed root.
                    return pow(n.y, _WindPower);
                }
                // Flag: the pole (left of the band) never moves, the free cloth edge swings most.
                return smoothstep(_WindStart, _WindStart + _WindEnd, n.x) * lerp(.85, 1., n.y);
            }

            wind_v2f vert(wind_appdata input)
            {
                wind_v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float4 vertex = input.vertex;
                float2 texcoord = _UVRect.xy + input.texcoord * _UVRect.zw;
                float3 world = mul(unity_ObjectToWorld, vertex).xyz;
                float fade = 1;

                // Wind is independent of the route projection: it must be visible in Edit Mode
                // authoring too, where _YSEnabled is off.
                float sx = length(unity_ObjectToWorld._m00_m10_m20);
                float sy = length(unity_ObjectToWorld._m01_m11_m21);
                float windTime = _WindTimeOverride >= 0 ? _WindTimeOverride : _Time.y;
                float authoredPhase = dot(unity_ObjectToWorld._m03_m13_m23.xz, float2(12.9898, 78.233)) + _WindPhase;
                float sway = sin(windTime * _WindSpeed + authoredPhase) * .72
                           + sin(windTime * (_WindSpeed * .47) + authoredPhase * 1.71) * .28;
                float flutter = sin(windTime * _WindSpeed * 1.7 + authoredPhase * 1.3
                                  + input.texcoord.y * _WindWaveFrequency) * _WindWave;
                float windOffset = (sway + flutter) * _WindStrength * WindWeight(input.texcoord) * sy;

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

                    anchor.xy += float2(vertex.x * sx + windOffset, vertex.y * sy);
                    world = mul(_YSToWorld, float4(anchor, 1)).xyz;
                    float fadeEnd = max(80, _YSViewDistance + 12);
                    fade = saturate((depth + 14) / 3) * saturate((fadeEnd - depth) / 10);
                }
                else
                {
                    float3 right = normalize(mul((float3x3)unity_ObjectToWorld, float3(1, 0, 0)));
                    world += right * windOffset;
                }
                output.vertex = mul(UNITY_MATRIX_VP, float4(world, 1));
                output.texcoord = texcoord;
                output.color = input.color * _Color * _RendererColor;
                output.color.a *= fade;
                return output;
            }

            fixed4 frag(wind_v2f input) : SV_Target
            {
                fixed4 color = SampleSpriteTexture(input.texcoord) * input.color;
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
