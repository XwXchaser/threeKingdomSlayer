Shader "CurvedScroll/Authoring Sprite"
{
 Properties {
  [PerRendererData] _MainTex("Sprite",2D)="white"{}
  _Color("Tint",Color)=(1,1,1,1)
  [MaterialToggle] PixelSnap("Pixel snap",Float)=0
  [HideInInspector] _RendererColor("RendererColor",Color)=(1,1,1,1)
  [HideInInspector] _Flip("Flip",Vector)=(1,1,1,1)
  [PerRendererData] _AlphaTex("Alpha",2D)="white"{}
  [PerRendererData] _EnableExternalAlpha("ExternalAlpha",Float)=0
 }
 SubShader {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True"}
  Cull Off Lighting Off ZWrite Off Blend One OneMinusSrcAlpha
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_instancing
   #pragma multi_compile _ PIXELSNAP_ON
   #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
   #include "UnitySprites.cginc"
   #include "ScrollProjection.cginc"
   float _ScrollAnchor;
   v2f vert(appdata_t IN) {
    v2f o;
    UNITY_SETUP_INSTANCE_ID(IN);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    float4 v=UnityFlipSprite(IN.vertex,_Flip);
    float3 world=mul(unity_ObjectToWorld,v).xyz;
    o.vertex=mul(UNITY_MATRIX_VP,float4(ProjectScroll(world,_ScrollAnchor,1),1));
    o.texcoord=IN.texcoord;
    o.color=IN.color*_Color*_RendererColor;
    o.color.a*=ScrollFade(_ScrollAnchor);
    #ifdef PIXELSNAP_ON
    o.vertex=UnityPixelSnap(o.vertex);
    #endif
    return o;
   }
   fixed4 frag(v2f i):SV_Target {fixed4 c=SampleSpriteTexture(i.texcoord)*i.color;c.rgb*=c.a;return c;}
   ENDCG
  }
 }
}
