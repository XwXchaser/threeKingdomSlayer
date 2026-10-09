Shader "CurvedScroll/Y Sample Scenery E0 Route" {
Properties {[PerRendererData] _MainTex("Sprite",2D)="white"{} _Color("Color",Color)=(1,1,1,1) [HideInInspector] _RendererColor("RendererColor",Color)=(1,1,1,1) [HideInInspector] _Flip("Flip",Vector)=(1,1,1,1)}
SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" "DisableBatching"="True"} Cull Off ZWrite Off Blend One OneMinusSrcAlpha
Pass{CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnitySprites.cginc"
float _YSEnabled;float _YSViewDistance;float4 _YSPose;float4x4 _YSToLocal,_YSToWorld;
v2f vert(appdata_t i){v2f o;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
float4 v=UnityFlipSprite(i.vertex,_Flip);float3 w=mul(unity_ObjectToWorld,v).xyz;float fade=1;
if(_YSEnabled>.5){
float3 anchor=mul(_YSToLocal,float4(unity_ObjectToWorld._m03_m13_m23,1)).xyz;
float2 q=anchor.xz-_YSPose.xz;float s=sin(_YSPose.w),c=cos(_YSPose.w);anchor.x=c*q.x-s*q.y;anchor.z=s*q.x+c*q.y;
float d=max(0,anchor.z-18);anchor.y-=.009*d*d*smoothstep(0,6,d);
float sx=length(unity_ObjectToWorld._m00_m10_m20),sy=length(unity_ObjectToWorld._m01_m11_m21);
anchor.xy+=v.xy*float2(sx,sy);w=mul(_YSToWorld,float4(anchor,1)).xyz;
float fadeEnd=max(80,_YSViewDistance+12);fade=saturate((anchor.z+14)/3)*saturate((fadeEnd-anchor.z)/10);
}
o.vertex=mul(UNITY_MATRIX_VP,float4(w,1));o.texcoord=i.texcoord;o.color=i.color*_Color*_RendererColor;o.color.a*=fade;return o;}
fixed4 frag(v2f i):SV_Target{fixed4 c=tex2D(_MainTex,i.texcoord)*i.color;c.rgb*=c.a;return c;}
ENDCG
}}
}
