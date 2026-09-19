Shader "CurvedScrollLab/ValleyGround"
{
 Properties { _Road("Road",2D)="white"{} _Side("Roadside",2D)="white"{} _HalfWidth("Half Width",Float)=3.5 _Travel("Travel",Float)=0 _TileSize("Tile size",Float)=4 _BlendWidth("Road shoulder transition",Float)=0.5 }
 SubShader { Tags { "RenderType"="Opaque" } Cull Off
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _Road,_Side; float _HalfWidth,_Travel,_TileSize,_BlendWidth;
 struct input { float4 vertex:POSITION; }; struct output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float x:TEXCOORD1; };
 output vert(input v){output o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=float2(v.vertex.x,v.vertex.z+_Travel)/max(.1,_TileSize);o.x=v.vertex.x;return o;}
 fixed4 frag(output i):SV_Target {float blend=smoothstep(_HalfWidth-_BlendWidth*.5,_HalfWidth+_BlendWidth*.5,abs(i.x));return lerp(tex2D(_Road,i.uv),tex2D(_Side,i.uv),blend);}
 ENDCG
 } }
}
