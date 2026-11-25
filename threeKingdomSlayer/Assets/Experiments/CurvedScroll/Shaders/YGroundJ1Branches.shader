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
        _StoneExitStart("Stone exit start (m, 0 = off)",Float)=0
        _StoneExitFade("Stone exit fade length (m)",Range(1,60))=16
        [HideInInspector] _YSLapIndex("Per-copy lap index (runtime)",Float)=0
        [HideInInspector] _YSLapBranchSign("Per-copy branch direction (runtime)",Float)=-1
        _YSGroundMaxZ("Ground max draw distance (m, 0 = use view distance)",Float)=0
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
            float _YSEnabled, _YSAngle, _YSViewDistance, _YSCycleLength;
            float _YSLapIndex, _YSLapBranchSign;
            float4 _YSPose;
            float4x4 _YSToLocal, _YSToWorld;
            sampler2D _Road, _Shoulder, _Outer, _LeftStoneRoad, _LeftStoneSides;
            float _RoadHalfWidth, _ShoulderWidth, _RoadBlend, _OuterBlend, _JunctionDistance, _CurveRadius;
            float _LeftStoneTileSize, _LeftStoneRoadBlend, _StoneEntryLength, _BranchMaterialBlend;
            float _StoneExitStart, _StoneExitFade;
            float _YSGroundMaxZ;
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
                // 本地顶点乘缩放得到每圈地图空间的米数；有限剧情关保留原始采样。
                o.map=i.vertex.xz*float2(length(unity_ObjectToWorld._m00_m10_m20),
                                         length(unity_ObjectToWorld._m02_m12_m22));
                if(_YSEnabled>.5){p=project(p);w=mul(_YSToWorld,float4(p,1)).xyz;}
                o.pos=mul(UNITY_MATRIX_VP,float4(w,1));o.z=p.z;
                return o;
            }
            // 本圈分支末端的位置和切线。它同时定义本圈的终点以及下一圈的起点。
            float2 lapEnd(float branchSign)
            {
                float tail=max(0.0,_YSCycleLength-_JunctionDistance-_CurveRadius*_YSAngle);
                return float2(branchSign*(_CurveRadius*(1-cos(_YSAngle))+sin(_YSAngle)*tail),
                              _JunctionDistance+_CurveRadius*sin(_YSAngle)+cos(_YSAngle)*tail);
            }
            float2 lapForward(float branchSign)
            {
                return float2(branchSign*sin(_YSAngle),cos(_YSAngle));
            }
            // HLSL clip 保留 0，两个副本的边界采用互补的 < / >=，避免共面闪烁。
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
            // 路面走廊的坐标：横向有符号距离、纵向沿路米数。只在窄路面/路肩内使用；
            // 远离路线时这种坐标会在弯道内侧发生折叠，绝不能用来映射整张大地面。
            float2 roadCoordinates(float2 map,float branchSign)
            {
                float radius=max(.001,_CurveRadius);
                float2 p=float2(map.x*branchSign,map.y-_JunctionDistance);
                float t=clamp(atan2(p.y,radius-p.x),0,_YSAngle);
                float2 arc=float2(radius*(1-cos(t)),radius*sin(t));
                float2 end=float2(radius*(1-cos(_YSAngle)),radius*sin(_YSAngle));
                float2 tangent=float2(sin(_YSAngle),cos(_YSAngle));
                float tail=max(0,dot(p-end,tangent));
                float2 fromStart=p-float2(0,min(p.y,0));
                float2 fromArc=p-arc;
                float2 fromTail=p-(end+tangent*tail);
                float startDistance=dot(fromStart,fromStart);
                float arcDistance=dot(fromArc,fromArc);
                float tailDistance=dot(fromTail,fromTail);
                if(startDistance<=arcDistance&&startDistance<=tailDistance)
                    return float2(map.x,min(map.y,_JunctionDistance));
                if(arcDistance<=tailDistance)
                    return float2(branchSign*dot(fromArc,float2(cos(t),-sin(t))),
                                  _JunctionDistance+radius*t);
                return float2(branchSign*dot(p-end,float2(tangent.y,-tangent.x)),
                              _JunctionDistance+radius*_YSAngle+tail);
            }
            fixed4 frag(v2f i):SV_Target
            {
                // ★ 下沉是 0.009*d²，**无上限**：100m 处 60m、200m 处 324m、380m 处 1179m。
                //   一旦把它画到很远，那一段就是一面“融化的陡壁”，纹理自然被拉烂，相机一动就闪。
                //   ⇒ **地面只画到一个固定的跟前距离**（_YSGroundMaxZ），与“第几圈/哪份副本”无关
                //   ⇒ 前方永远有恒定距离的实心地面：不会越接近接缝越短，也不会把远处崖壁拉进画面。
                float maxZ = (_YSGroundMaxZ > .5) ? _YSGroundMaxZ : _YSViewDistance;
                if(_YSEnabled>.5)clip(min(i.z+14, maxZ-i.z));
                float endless=(_YSEnabled>.5&&_YSCycleLength>.5)?1.0:0.0;
                float2 forward=lapForward(_YSLapBranchSign);
                float2 end=lapEnd(_YSLapBranchSign);
                float endDistance=dot(i.map-end,forward);
                if(endless>.5)
                {
                    if(_YSLapIndex>.5 && i.map.y<0)discard;
                    if(endDistance>=0)discard;
                }

                float commonDist=lineDist(i.map,float2(0,-30),float2(0,_JunctionDistance));
                float leftDist=branchDist(i.map,-1);
                float rightDist=branchDist(i.map,1);
                float dist=min(commonDist,min(leftDist,rightDist));
                float outerStart=_RoadHalfWidth+_ShoulderWidth;
                float roadToShoulder=smoothstep(_RoadHalfWidth-_RoadBlend*.5,_RoadHalfWidth+_RoadBlend*.5,dist);
                float shoulderToOuter=smoothstep(outerStart-_OuterBlend*.5,outerStart+_OuterBlend*.5,dist);
                // 本圈纹理相位随真实沿路距离延续；按 4m 周期约束，避免使用无界的 k*L。
                // 材质使用 Repeat，后续圈入口的相位恰等于前一圈终点。
                float phase=0;
                if(endless>.5) phase=fmod(_YSLapIndex*fmod(_YSCycleLength,4.0),4.0);
                float2 phaseOffset=float2(0,phase);
                // 地图基线始终使用本圈原始坐标；相位只用于道路走廊和圈界过渡。
                float2 mapUV=i.map/4;

                // 道路走廊跟随本圈分支逐渐转向，替代旧的“地图坐标一直斜铺”及圈界双纹理混合。
                // 半径只延伸到路肩外少量过渡，防止沿路坐标在远离弯道时拉伸或折叠。
                float corridorWeight=0;
                float2 corridorUV=mapUV;
                if(endless>.5)
                {
                    float2 roadMap=roadCoordinates(i.map,_YSLapBranchSign);
                    corridorUV=(roadMap+phaseOffset)/4;
                    corridorWeight=1-smoothstep(outerStart,outerStart+1.5,abs(roadMap.x));
                }

                fixed4 road=tex2D(_Road,mapUV);
                fixed4 shoulder=tex2D(_Shoulder,mapUV)*_ShoulderTint;
                fixed4 outer=tex2D(_Outer,mapUV)*_OuterTint;
                if(corridorWeight>0)
                {
                    road=lerp(road,tex2D(_Road,corridorUV),corridorWeight);
                    shoulder=lerp(shoulder,tex2D(_Shoulder,corridorUV)*_ShoulderTint,corridorWeight);
                    outer=lerp(outer,tex2D(_Outer,corridorUV)*_OuterTint,corridorWeight);
                }
                // 路外地面仍沿用旧地图坐标。圈界处只衔接圈界局部的两套坐标，避免
                // 对全地图使用沿路 UV 或世界 XZ；路心不再进行两套土纹颜色叠加。
                float seamMix=0;
                float2 nextMap=i.map;
                if(endless>.5)
                {
                    nextMap=float2(forward.y*(i.map.x-end.x)-forward.x*(i.map.y-end.y),
                                   forward.x*(i.map.x-end.x)+forward.y*(i.map.y-end.y));
                    seamMix=smoothstep(-24.0,-4.0,endDistance)*(1-corridorWeight);
                }
                if(seamMix>0)
                {
                    float2 nextPhaseOffset=float2(0,fmod(phase+fmod(_YSCycleLength,4.0),4.0));
                    float2 nextUV=(nextMap+nextPhaseOffset)/4;
                    road=lerp(road,tex2D(_Road,nextUV),seamMix);
                    shoulder=lerp(shoulder,tex2D(_Shoulder,nextUV)*_ShoulderTint,seamMix);
                    outer=lerp(outer,tex2D(_Outer,nextUV)*_OuterTint,seamMix);
                }
                fixed4 dirt=lerp(lerp(road,shoulder,roadToShoulder),outer,shoulderToOuter);

                // Use authored route distances, so the material stays on the left branch during a turn.
                // The common lane remains dirt, including the complete N1 and E0 sections.
                float entrance=smoothstep(0,max(.1,_StoneEntryLength),i.map.y-_JunctionDistance);
                // ★ 石板收尾（2026-11-23）：无限卷轴一圈的**末端**在地图空间是村庄青石路，而下一圈的开头是
                //   N1 土路 ⇒ 圈界两侧材质必然跳（实测 379.9→380.1 石板占比 78%→16%，"整屏石→土"）。
                //   让石板在**本圈边界之前**淡回土路 ⇒ 圈界两侧都是土路 ⇒ 只剩贴图相位的差异。
                //   ⚠ 位置必须按「本圈**沿路**长度」反推成 flat z，不能用沿路米数直接填：
                //     分叉后每 1 米沿路只推进 cos(45°)≈0.707 米 flat z，圈界（沿路 380）处的 flat z 只有 ≈328.6。
                //     旧值 350/26 是按「沿路 380」填的 ⇒ 淡出区间 350→376 在圈界**还没开始** ⇒ 什么都没修到。
                //   ⚠ 收尾必须在圈界**之前**结束，且多留一个路宽：圈界在 flat z 上是斜的（转 45°），
                //     同一条横断面上 map.y 相差 ±(路宽+路肩)·sin45°，留出这段才不会在圈界上剩半条石板。
                //   ⚠ 剧情关走 else 分支（_YSCycleLength = 0），公式与改动前**逐字相同** ⇒ 逐像素不变。
                float stoneExit=1;
                if(_YSCycleLength>.5)
                {
                    float arcEndZ=_JunctionDistance+_CurveRadius*sin(_YSAngle);
                    float tailLen=max(0.0,_YSCycleLength-_JunctionDistance-_CurveRadius*_YSAngle);
                    float cycleEndZ=arcEndZ+cos(_YSAngle)*tailLen;
                    float exitEnd=cycleEndZ-(_RoadHalfWidth+_ShoulderWidth);
                    float exitStart=max(0.0,exitEnd-max(1.0,_StoneExitFade));
                    stoneExit=1-smoothstep(exitStart,exitEnd,i.map.y);
                }
                else if(_StoneExitStart>.5)stoneExit=1-smoothstep(_StoneExitStart,_StoneExitStart+max(1,_StoneExitFade),i.map.y);
                float leftWeight=entrance*smoothstep(-_BranchMaterialBlend,_BranchMaterialBlend,rightDist-leftDist)*stoneExit;
                float leftRoadEdge=smoothstep(_RoadHalfWidth-_LeftStoneRoadBlend*.5,_RoadHalfWidth+_LeftStoneRoadBlend*.5,leftDist);
                float leftOuterEdge=smoothstep(outerStart-_OuterBlend*.5,outerStart+_OuterBlend*.5,leftDist);
                float2 stoneUV = i.map / max(.1, _LeftStoneTileSize);
                fixed4 stone=tex2D(_LeftStoneRoad,stoneUV)*_LeftStoneTint;
                fixed4 stoneSides=tex2D(_LeftStoneSides,stoneUV)*_LeftStoneTint;
                fixed4 leftGround=lerp(lerp(stone,stoneSides,leftRoadEdge),outer,leftOuterEdge);
                return lerp(dirt,leftGround,leftWeight) * _TutorialDim;
            }
            ENDCG
        }
    }
}
