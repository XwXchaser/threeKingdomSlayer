Shader "CurvedScroll/Y Ground J1 Branches"
{
    Properties
    {
        _Road("Common and right dirt road",2D)="white"{}
        _Shoulder("Dirt shoulder",2D)="white"{}
        _Outer("Outer ground",2D)="white"{}
        _LeftStoneRoad("Left stone road",2D)="white"{}
        _LeftStoneSides("Left stone shoulder",2D)="white"{}
        _LeftStoneTileSize("Stone texture world size",Float)=6
        _LeftStoneRoadBlend("Stone road to shoulder blend",Range(0.05,2))=0.45
        _StoneEntryLength("Stone entrance transition",Range(1,20))=8
        _BranchMaterialBlend("Branch material blend width",Range(0.1,2))=0.6
        _RoadHalfWidth("Road half width",Range(1,6))=2.5
        _ShoulderWidth("Shoulder width",Range(0.1,4))=1.2
        _JunctionDistance("Junction distance",Float)=190
        _CurveRadius("Curve radius",Float)=28.1595573
        _RoadBlend("Road to shoulder blend",Range(0.05,2))=0.65
        _OuterBlend("Shoulder to outer blend",Range(0.05,2))=0.85
        _ShoulderTint("Shoulder tint",Color)=(1,1,1,1)
        _OuterTint("Outer tint",Color)=(1,1,1,1)
        _LeftStoneTint("Left stone tint",Color)=(1,1,1,1)
        [HideInInspector] _TutorialDim("Tutorial dim",Color)=(1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float _YSEnabled, _YSAngle, _YSViewDistance;
            float4 _YSPose;
            float4x4 _YSToLocal, _YSToWorld;
            sampler2D _Road, _Shoulder, _Outer, _LeftStoneRoad, _LeftStoneSides;
            float _RoadHalfWidth, _ShoulderWidth, _RoadBlend, _OuterBlend, _JunctionDistance, _CurveRadius;
            float _LeftStoneTileSize, _LeftStoneRoadBlend, _StoneEntryLength, _BranchMaterialBlend;
            fixed4 _ShoulderTint, _OuterTint, _LeftStoneTint;
            fixed4 _TutorialDim;
            struct appdata { float4 vertex:POSITION; };
            struct v2f { float4 pos:SV_POSITION; float2 map:TEXCOORD0; float z:TEXCOORD1; };
            float3 project(float3 p)
            {
                float2 q=p.xz-_YSPose.xz;
                float s=sin(_YSPose.w),c=cos(_YSPose.w);
                p.x=c*q.x-s*q.y;p.z=s*q.x+c*q.y;
                float d=max(0,p.z-18);
                p.y-=.009*d*d*smoothstep(0,6,d);
                return p;
            }
            v2f vert(appdata i)
            {
                v2f o;
                float3 w=mul(unity_ObjectToWorld,i.vertex).xyz;
                float3 p=mul(_YSToLocal,float4(w,1)).xyz;
                o.map=p.xz;
                if(_YSEnabled>.5){p=project(p);w=mul(_YSToWorld,float4(p,1)).xyz;}
                o.pos=mul(UNITY_MATRIX_VP,float4(w,1));o.z=p.z;
                return o;
            }
            float lineDist(float2 p,float2 a,float2 b)
            {
                float2 d=b-a;
                return length(p-a-d*saturate(dot(p-a,d)/max(.0001,dot(d,d))));
            }
            float branchDist(float2 map,float sign)
            {
                float2 p=float2(map.x*sign,map.y-_JunctionDistance);
                float t=clamp(atan2(p.y,_CurveRadius-p.x),0,_YSAngle);
                float2 arc=float2(_CurveRadius*(1-cos(t)),_CurveRadius*sin(t));
                float2 end=float2(_CurveRadius*(1-cos(_YSAngle)),_CurveRadius*sin(_YSAngle));
                return min(length(p-arc),lineDist(p,end,end+float2(sin(_YSAngle),cos(_YSAngle))*600));
            }
            fixed4 frag(v2f i):SV_Target
            {
                if(_YSEnabled>.5)clip(min(i.z+14,_YSViewDistance-i.z));
                float commonDist=lineDist(i.map,float2(0,-30),float2(0,_JunctionDistance));
                float leftDist=branchDist(i.map,-1);
                float rightDist=branchDist(i.map,1);
                float dist=min(commonDist,min(leftDist,rightDist));
                float outerStart=_RoadHalfWidth+_ShoulderWidth;
                float roadToShoulder=smoothstep(_RoadHalfWidth-_RoadBlend*.5,_RoadHalfWidth+_RoadBlend*.5,dist);
                float shoulderToOuter=smoothstep(outerStart-_OuterBlend*.5,outerStart+_OuterBlend*.5,dist);
                fixed4 road=tex2D(_Road,i.map/4);
                fixed4 shoulder=tex2D(_Shoulder,i.map/4)*_ShoulderTint;
                fixed4 outer=tex2D(_Outer,i.map/4)*_OuterTint;
                fixed4 dirt=lerp(lerp(road,shoulder,roadToShoulder),outer,shoulderToOuter);

                // Use authored route distances, so the material stays on the left branch during a turn.
                // The common lane remains dirt, including the complete N1 and E0 sections.
                float entrance=smoothstep(0,max(.1,_StoneEntryLength),i.map.y-_JunctionDistance);
                float leftWeight=entrance*smoothstep(-_BranchMaterialBlend,_BranchMaterialBlend,rightDist-leftDist);
                float leftRoadEdge=smoothstep(_RoadHalfWidth-_LeftStoneRoadBlend*.5,_RoadHalfWidth+_LeftStoneRoadBlend*.5,leftDist);
                float leftOuterEdge=smoothstep(outerStart-_OuterBlend*.5,outerStart+_OuterBlend*.5,leftDist);
                float2 stoneUV=i.map/max(.1,_LeftStoneTileSize);
                fixed4 stone=tex2D(_LeftStoneRoad,stoneUV)*_LeftStoneTint;
                fixed4 stoneSides=tex2D(_LeftStoneSides,stoneUV)*_LeftStoneTint;
                fixed4 leftGround=lerp(lerp(stone,stoneSides,leftRoadEdge),outer,leftOuterEdge);
                return lerp(dirt,leftGround,leftWeight) * _TutorialDim;
            }
            ENDCG
        }
    }
}
