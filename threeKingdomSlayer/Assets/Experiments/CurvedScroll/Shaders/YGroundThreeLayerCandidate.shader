Shader "CurvedScroll/Y Ground Three Layer Candidate"
{
    Properties
    {
        _Road("Road", 2D) = "white" {}
        _Shoulder("Shoulder", 2D) = "white" {}
        _Outer("Outer Ground", 2D) = "white" {}
        _RoadHalfWidth("Road half width", Range(1, 6)) = 2.5
        _ShoulderWidth("Shoulder width", Range(0.1, 4)) = 1.2
        _RoadBlend("Road to shoulder blend", Range(0.05, 2)) = 0.65
        _OuterBlend("Shoulder to outer blend", Range(0.05, 2)) = 0.85
        _ShoulderTint("Shoulder tint", Color) = (1, 1, 1, 1)
        _OuterTint("Outer tint", Color) = (1, 1, 1, 1)
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
            float _YSEnabled, _YSAngle;
            float4 _YSPose;
            float4x4 _YSToLocal, _YSToWorld;
            sampler2D _Road, _Shoulder, _Outer;
            float _RoadHalfWidth, _ShoulderWidth, _RoadBlend, _OuterBlend;
            fixed4 _ShoulderTint, _OuterTint;
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
            fixed4 frag(v2f i):SV_Target
            {
                if(_YSEnabled>.5)clip(min(i.z+14,85-i.z));
                float dist=lineDist(i.map,float2(0,-30),float2(0,83.12884));
                for(int k=0;k<2;k++)
                {
                    float sign=k==0?-1:1;
                    float2 p=float2(i.map.x*sign,i.map.y-83.12884);
                    float t=clamp(atan2(p.y,28.1595573-p.x),0,_YSAngle);
                    float2 arc=float2(28.1595573*(1-cos(t)),28.1595573*sin(t));
                    dist=min(dist,length(p-arc));
                    float2 e=float2(28.1595573*(1-cos(_YSAngle)),28.1595573*sin(_YSAngle));
                    dist=min(dist,lineDist(p,e,e+float2(sin(_YSAngle),cos(_YSAngle))*600));
                }
                float roadToShoulder=smoothstep(_RoadHalfWidth-_RoadBlend*.5,_RoadHalfWidth+_RoadBlend*.5,dist);
                float outerStart=_RoadHalfWidth+_ShoulderWidth;
                float shoulderToOuter=smoothstep(outerStart-_OuterBlend*.5,outerStart+_OuterBlend*.5,dist);
                fixed4 road=tex2D(_Road,i.map/4);
                fixed4 shoulder=tex2D(_Shoulder,i.map/4)*_ShoulderTint;
                fixed4 outer=tex2D(_Outer,i.map/4)*_OuterTint;
                return lerp(lerp(road,shoulder,roadToShoulder),outer,shoulderToOuter);
            }
            ENDCG
        }
    }
}
