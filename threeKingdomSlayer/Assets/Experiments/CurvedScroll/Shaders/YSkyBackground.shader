Shader "CurvedScroll/Y Sky Background"
{
    Properties
    {
        [PerRendererData] _MainTex("Sky", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _VerticalOffset("Horizon UV offset", Range(-0.5,0.5)) = -0.23
        _ImageAspect("Image width / height", Float) = 0.6666667
    }
    SubShader
    {
        // After Unity's skybox, behind transparent scenery. Far-plane depth test
        // preserves the opaque road; this layer never reads _YS projection globals.
        Tags { "Queue"="Transparent-100" "RenderType"="Transparent" "DisableBatching"="True" }
        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _ImageAspect;
            fixed4 _Color;
            float _VerticalOffset;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                // The saved full-rect sprite is a screen-filling far background,
                // not a world-space wall intersecting the terrain after a turn.
                o.vertex=float4(v.uv*2-1,0,1);
                #if defined(UNITY_REVERSED_Z)
                    o.vertex.z=0.000001;
                #else
                    o.vertex.z=0.999999;
                #endif
                float screenAspect=_ScreenParams.x/_ScreenParams.y;
                float imageAspect=max(0.01,_ImageAspect);
                float2 cover=float2(min(1,screenAspect/imageAspect),min(1,imageAspect/screenAspect));
                o.uv=(v.uv-0.5)*cover+0.5;
                // Sprite UVs are vertically oriented opposite to the screen-space quad.
                // Flip here so the blue sky stays at the top and the warm haze remains at the horizon.
                o.uv.y=1-o.uv.y;
                o.uv.y+=_VerticalOffset;
                return o;
            }
            fixed4 frag(v2f i):SV_Target{return tex2D(_MainTex,saturate(i.uv))*_Color;}
            ENDCG
        }
    }
}
