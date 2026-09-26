Shader "CurvedScroll/Authoring Background"
{
 Properties { _MainTex("Background",2D)="white"{} _Lift("Vertical UV lift",Float)=0.4 _ImageAspect("Image aspect",Float)=0.666667 }
 SubShader { Tags {"Queue"="Background" "RenderType"="Transparent"} Cull Off ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 #include "ScrollProjection.cginc"
 sampler2D _MainTex;float _Lift,_ImageAspect,_SegmentStart,_SegmentEnd,_FadeStart,_LastBackground;
 struct appdata{float4 vertex:POSITION;float2 uv:TEXCOORD0;};
 struct v2f{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;};
 v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;
 if(_ScrollProjectionEnabled>.5){
 o.vertex=float4(v.vertex.xy*2,1,1);
 o.vertex.y *= _ProjectionParams.x;
 #if defined(UNITY_REVERSED_Z)
 o.vertex.z=0;
 #endif
 float aspect=_ScreenParams.x/_ScreenParams.y;
 if(aspect<_ImageAspect)o.uv.x=(o.uv.x-.5)*aspect/_ImageAspect+.5;
 else o.uv.y=(o.uv.y-.5)*_ImageAspect/aspect+.5;
 o.uv.y-=_Lift;
 }return o;}
 fixed4 frag(v2f i):SV_Target{
 fixed4 c=tex2D(_MainTex,i.uv);
 if(_ScrollProjectionEnabled>.5){
 clip(_ScrollDistance-_FadeStart);
 if(_LastBackground<.5)clip(_SegmentEnd-_ScrollDistance-.00001);
 c.a*=(_SegmentStart<=0)?1:smoothstep(_FadeStart,max(_FadeStart+.001,_SegmentStart),_ScrollDistance);
 }return c;}
 ENDCG
 }
 }
}
