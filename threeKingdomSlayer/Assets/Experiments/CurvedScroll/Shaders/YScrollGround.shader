Shader "CurvedScroll/Y Sample Ground" {
Properties {_Road("Road",2D)="white"{} _Side("Terrain",2D)="white"{} }
SubShader {Tags {"RenderType"="Opaque"} Cull Off
Pass {CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
float _YSEnabled,_YSAngle;float4 _YSPose;float4x4 _YSToLocal,_YSToWorld;
sampler2D _Road,_Side;
struct a{float4 vertex:POSITION;};struct v{float4 pos:SV_POSITION;float2 map:TEXCOORD0;float z:TEXCOORD1;};
float3 project(float3 p){float2 q=p.xz-_YSPose.xz;float s=sin(_YSPose.w),c=cos(_YSPose.w);p.x=c*q.x-s*q.y;p.z=s*q.x+c*q.y;float d=max(0,p.z-18);p.y-=.009*d*d*smoothstep(0,6,d);return p;}
v vert(a i){v o;float3 w=mul(unity_ObjectToWorld,i.vertex).xyz;float3 p=mul(_YSToLocal,float4(w,1)).xyz;o.map=i.vertex.xz;
if(_YSEnabled>.5){p=project(p);w=mul(_YSToWorld,float4(p,1)).xyz;}o.pos=mul(UNITY_MATRIX_VP,float4(w,1));o.z=p.z;return o;}
float lineDist(float2 p,float2 a,float2 b){float2 d=b-a;return length(p-a-d*saturate(dot(p-a,d)/dot(d,d)));}
fixed4 frag(v i):SV_Target{
if(_YSEnabled>.5)clip(min(i.z+14,85-i.z));
float dist=lineDist(i.map,float2(0,-30),float2(0,20));
// Circular fork arc followed by straight exits; exactly the same radius and angle as travel sampling.
for(int k=0;k<2;k++){float sign=k==0?-1:1;float2 p=float2(i.map.x*sign,i.map.y-20);float t=clamp(atan2(p.y,18-p.x),0,_YSAngle);float2 arc=float2(18*(1-cos(t)),18*sin(t));dist=min(dist,length(p-arc));float2 e=float2(18*(1-cos(_YSAngle)),18*sin(_YSAngle));dist=min(dist,lineDist(p,e,e+float2(sin(_YSAngle),cos(_YSAngle))*100));}
float road=1-smoothstep(2.3,2.7,dist);return lerp(tex2D(_Side,i.map/4),tex2D(_Road,i.map/4),road);
}
ENDCG
}}
}
