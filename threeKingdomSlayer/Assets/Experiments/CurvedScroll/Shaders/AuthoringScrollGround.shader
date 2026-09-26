Shader "CurvedScroll/Authoring Ground"
{
 Properties {
  _Road("Road",2D)="white"{} _Side("Ground",2D)="white"{}
  _HalfWidth("Road half width",Float)=2.2 _TileSize("Tile size",Float)=4
  _RoadVisible("Road visibility",Range(0,1))=1
 }
 SubShader { Tags {"RenderType"="Opaque"} Cull Off
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   #include "ScrollProjection.cginc"
   sampler2D _Road,_Side;float _HalfWidth,_TileSize,_RoadVisible;
   struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
   struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float x:TEXCOORD1;float z:TEXCOORD2;};
   v2f vert(appdata v){v2f o;float3 world=mul(unity_ObjectToWorld,v.vertex).xyz;
    o.vertex=mul(UNITY_MATRIX_VP,float4(ProjectScroll(world,0,0),1));
    o.uv=v.uv/max(.1,_TileSize);o.x=v.vertex.x;o.z=mul(_ScrollWorldToLocal,float4(world,1)).z-_ScrollDistance;return o;}
   fixed4 frag(v2f i):SV_Target{
    if(_ScrollProjectionEnabled>.5)clip(min(i.z-_ScrollNear,_ScrollBend.w+4-i.z));
    float road=(1-smoothstep(_HalfWidth-.25,_HalfWidth+.25,abs(i.x)))*_RoadVisible;
    return lerp(tex2D(_Side,i.uv),tex2D(_Road,i.uv),road);}
   ENDCG
  }
 }
}
