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
            struct v2f { float4 pos:SV_POSITION; float2 map:TEXCOORD0; float z:TEXCOORD1; float2 wuv:TEXCOORD2; };
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
                // ★ 图案坐标必须用**物件自己的局部米**，不能用路线根局部：
                //   无限卷轴时每圈的地面副本带着累积旋转摆放，用路线根坐标算出来的
                //   路面图案会跟着旋转一起跑（第二圈就没路了）。
                //   第 0 圈：地形在路线根下、位置 0、缩放 6 ⇒ 与旧写法逐像素相同。
                o.map=i.vertex.xz*float2(length(unity_ObjectToWorld._m00_m10_m20),
                                         length(unity_ObjectToWorld._m02_m12_m22));
                // ★ 贴图采样坐标 = **世界 XZ**（不是每份副本自己的局部坐标）。
                //   地面是"同一张地图×每圈旋转 45°"：用局部坐标采样时，同一个世界点在不同圈副本里
                //   的局部坐标差一个旋转 ⇒ 换圈那一瞬贴图"重新洗一次牌"。
                //   用世界 XZ 后，同一个世界点永远是同一个 UV ⇒ 图案完全连续、不旋转、不换相位。
                //   第 0 圈 / 剧情关：路线根在原点、地面缩放 6 ⇒ 世界 XZ ≡ 局部 XZ ⇒ **逐像素不变**。
                o.wuv=w.xz;
                if(_YSEnabled>.5){p=project(p);w=mul(_YSToWorld,float4(p,1)).xyz;}
                o.pos=mul(UNITY_MATRIX_VP,float4(w,1));o.z=p.z;
                return o;
            }
            /// <summary>展开态 flat 坐标 → 沿路米数（左支）。用于按圈裁剪 / 沿路贴图。</summary>
            /// ⚠ 必须用**三候选取最近点**（直段 / 弧 / 尾段）：
            ///   尾段上的点相对弧心的角度也小于 _YSAngle，只用角度判会把它当成弧上 ⇒ 沿路距离卡在弧尾不再增长
            ///  （实测旧写法：d=250/300/380/600 全算出 ~208~211，导致贴图被搞乱）。
            ///   当前版本在 0~600m 误差 0.00，且横向 ±100m 不变。
            float AlongRoute(float2 m)
            {
                float px = m.x, py = m.y;
                // 候选1：直段
                float d1 = (py <= _JunctionDistance) ? abs(px) : 1e9;
                float s1 = min(py, _JunctionDistance);
                // 候选2：弧（左支弧心在 (-R, junction)）
                float t = clamp(atan2(py - _JunctionDistance, px + _CurveRadius), 0.0, _YSAngle);
                float cx = -_CurveRadius + _CurveRadius * cos(t);
                float cz = _JunctionDistance + _CurveRadius * sin(t);
                float d2 = length(float2(px - cx, py - cz));
                float s2 = _JunctionDistance + _CurveRadius * t;
                // 候选3：尾段
                float ax = -_CurveRadius * (1.0 - cos(_YSAngle));
                float az = _JunctionDistance + _CurveRadius * sin(_YSAngle);
                float dx = -sin(_YSAngle), dz = cos(_YSAngle);
                float tail = (px - ax) * dx + (py - az) * dz;
                float s3 = _JunctionDistance + _CurveRadius * _YSAngle + tail;
                float d3 = (tail >= 0.0) ? abs((px - ax) * dz - (py - az) * dx) : 1e9;
                float best = s1, bd = d1;
                if (d2 < bd) { bd = d2; best = s2; }
                if (d3 < bd) { best = s3; }
                return best;
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
                // ★ 下沉是 0.009*d²，**无上限**：100m 处 60m、200m 处 324m、380m 处 1179m。
                //   一旦把它画到很远，那一段就是一面“融化的陡壁”，纹理自然被拉烂，相机一动就闪。
                //   ⇒ **地面只画到一个固定的跟前距离**（_YSGroundMaxZ），与“第几圈/哪份副本”无关
                //   ⇒ 前方永远有恒定距离的实心地面：不会越接近接缝越短，也不会把远处崖壁拉进画面。
                float maxZ = (_YSGroundMaxZ > .5) ? _YSGroundMaxZ : _YSViewDistance;
                if(_YSEnabled>.5)clip(min(i.z+14, maxZ-i.z));
                // ★ 每圈的地面副本只画**本圈那一段**（否则相邻两圈的副本会大面积重叠 / 闪面）。
                // ★ 2026-11-21：裁剪坐标从 **flat z** 改成 **沿路米数**。
                //
                //   为什么必须用沿路长度：分叉后尾段每 1 米沿路只推进 cos(45°)=0.707 米 flat z，
                //   而相邻两圈的副本彼此转了 45° ⇒ 用 flat z 裁剪时两块"平板"**斜交**，
                //   实测 d=280~560 全程 **2 份副本重叠** ⇒ 近景打像素（表现为左右滑动错位、镜头一动就闪）。
                //   用沿路长度裁剪 ⇒ 各圈副本沿路**首尾相接、严格不重叠**，且镜头前方始终接得上。
                // ★ 每圈的地面副本只画**本圈那一段**（否则相邻两圈的副本会大面积重叠 / 闪面）。
                // ★ 2026-11-21：**只封上界，不设下界**。
                //   地面网格在 flat z 上是 −25..175（带缩放 −150..1050）；
                //   之前写成 `min(map.y, cycle - map.y)` ⇒ 把 **map.y < 0**（= 脚下与身后那一段）也裁掉了
                //   ⇒ 无限关**开局近处地面空缺**（剧情关不走这段裁剪，所以没这个毛病）。
                //   半径固定为 0（同时只有一份地面）⇒ 不需要下界，也不可能重叠。
                // ⚠ 2026-11-21 回退：裁剪回到 **flat z 只封上界**，地面保持**单份**（半径 0）。
                //   “沿路裁剪 + 半径1 + 沿路贴图”那一版把**开局脚下地面**弄坏了（实测用户反馈），已撤。
                if(_YSEnabled>.5&&_YSCycleLength>.5)clip(_YSCycleLength-i.map.y);
                float commonDist=lineDist(i.map,float2(0,-30),float2(0,_JunctionDistance));
                float leftDist=branchDist(i.map,-1);
                float rightDist=branchDist(i.map,1);
                float dist=min(commonDist,min(leftDist,rightDist));
                float outerStart=_RoadHalfWidth+_ShoulderWidth;
                float roadToShoulder=smoothstep(_RoadHalfWidth-_RoadBlend*.5,_RoadHalfWidth+_RoadBlend*.5,dist);
                float shoulderToOuter=smoothstep(outerStart-_OuterBlend*.5,outerStart+_OuterBlend*.5,dist);
                // ★ 土路贴图 UV = **地图空间**（与村庄青石路、与剧情关一致；这是用户认可的外观）。
                //   2026-11-21 试过沿路坐标 (dist, along)：路面附近等距但**离路心越远畸变越大**，
                //   路肩/外侧明显拉伸发歪 ⇒ 已回退。
                fixed4 road=tex2D(_Road,i.map/4);
                fixed4 shoulder=tex2D(_Shoulder,i.map/4)*_ShoulderTint;
                fixed4 outer=tex2D(_Outer,i.map/4)*_OuterTint;
                fixed4 dirt=lerp(lerp(road,shoulder,roadToShoulder),outer,shoulderToOuter);

                // Use authored route distances, so the material stays on the left branch during a turn.
                // The common lane remains dirt, including the complete N1 and E0 sections.
                float entrance=smoothstep(0,max(.1,_StoneEntryLength),i.map.y-_JunctionDistance);
                // ★ 石板收尾（2026-11-21）：无限卷轴在一圈末尾会切回地图开头，而开头是主路土路。
                //   让石板在接缝前先淡回土路 ⇒ 接缝两侧材质一致 ⇒（配合土路 UV 周期 4m、一圈 380 = 95×4）逐像素连续。
                //   _StoneExitStart = 0 ⇒ **关闭**（默认）；剧情关最远只到 325m ⇒ 开启也不影响剧情关。
                float stoneExit=1;
                if(_StoneExitStart>.5)stoneExit=1-smoothstep(_StoneExitStart,_StoneExitStart+max(1,_StoneExitFade),i.map.y);
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
