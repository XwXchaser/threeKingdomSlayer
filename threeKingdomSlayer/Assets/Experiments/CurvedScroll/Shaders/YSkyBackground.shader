Shader "CurvedScroll/Y Sky Background"
{
    Properties
    {
        _MainTex("Sky", 2D) = "white" {}
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
            // ★ 镜头震动跟随（2026-11-12）：xy = clip 空间位移，z = 滚转（弧度）
            //   由 CameraFeedbackController 每帧写入；只在相机偏离基准位时非零
            //   ⇒ 推进/转向时远景仍然「固定或弱视差」（design/curved-scroll-travel-experiment.md）。
            float4 _LocusScreenShake;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                // Rendered from SkyScreenQuad.asset: a full-rect quad whose UVs are authored
                // here (0..1, v=1 at the top). Do not go back to a SpriteRenderer: sprite mesh
                // V orientation is not the same in the editor and in an Android build, which
                // flipped this background upside down on device (found 2026-11-09).
                float2 clipXY = v.uv * 2 - 1;
                // ★ 镜头震动跟随（2026-11-12）：
                //   这张四边形是**钉在屏幕上的**（clip 空间、不过投影矩阵）⇒ 相机一震它不动、
                //   世界在动 ⇒ 读成「地动山摇」。本游戏是竖屏第一人称（镜头 = 玩家的头），
                //   被顶一下时**整帧应该一起动**，所以把相机的屏幕位移与滚转一起应用到这里。
                //   注意：位移/滚转都在 Y 翻转**之前**应用 ⇒ 与世界内容同样被镜像，两边始终一致。
                if (_LocusScreenShake.z != 0.0)
                {
                    float s = sin(_LocusScreenShake.z);
                    float c = cos(_LocusScreenShake.z);
                    clipXY = float2(clipXY.x * c - clipXY.y * s, clipXY.x * s + clipXY.y * c);
                }
                clipXY += _LocusScreenShake.xy;
                // A hand-written clip-space quad never goes through the projection matrix, so it
                // misses the vertical flip a RenderTexture target applies to everything else (the
                // Scene and Game view render into one, which is why this background looked right
                // in the editor and upside down on device). _ProjectionParams.x is -1 when the
                // projection is flipped, so this puts the quad in the same space as the rest of
                // the scene on every target.
                clipXY.y *= _ProjectionParams.x;
                o.vertex = float4(clipXY, 0, 1);
                #if defined(UNITY_REVERSED_Z)
                    o.vertex.z = 0.000001;
                #else
                    o.vertex.z = 0.999999;
                #endif
                // Sampling follows the mesh UVs, so v=1 stays glued to the top vertex no matter
                // which way the target flips the geometry.
                o.uv = v.uv;
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float screenAspect = _ScreenParams.x / _ScreenParams.y;
                float imageAspect = max(0.01, _ImageAspect);
                // Horizontal may crop, vertical must not: the image always spans the full
                // screen height so the visible content never changes with the aspect ratio.
                // (Scaling the vertical axis, as min(1,imageAspect/screenAspect) did, made the
                // image zoom in and crop top/bottom on screens wider than the image itself.)
                float2 cover = float2(min(1, screenAspect / imageAspect), 1);
                float2 uv;
                uv.x = (i.uv.x - 0.5) * cover.x + 0.5;
                // v=1 is the image's top row and the quad's v=1 vertex is the screen's top row,
                // so the offset only slides the art: negative values crop the image's top.
                uv.y = i.uv.y + _VerticalOffset;
                return tex2D(_MainTex, saturate(uv)) * _Color;
            }
            ENDCG
        }
    }
}
