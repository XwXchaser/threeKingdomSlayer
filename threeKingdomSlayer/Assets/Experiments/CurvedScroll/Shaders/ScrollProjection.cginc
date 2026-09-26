#ifndef AUTHORING_SCROLL_PROJECTION
#define AUTHORING_SCROLL_PROJECTION
float _ScrollProjectionEnabled, _ScrollDistance, _ScrollNear;
float4 _ScrollBend;
float4x4 _ScrollWorldToLocal, _ScrollLocalToWorld;
float ScrollDrop(float z)
{
    float x = max(0, z - _ScrollBend.x);
    float t = saturate(x / max(.1, _ScrollBend.z));
    return _ScrollBend.y * x * x * t * t * (3 - 2 * t);
}
float3 ProjectScroll(float3 world, float anchor, float rigid)
{
    if (_ScrollProjectionEnabled < .5) return world;
    float3 p = mul(_ScrollWorldToLocal, float4(world,1)).xyz;
    p.z -= _ScrollDistance;
    p.y -= ScrollDrop(lerp(p.z, anchor - _ScrollDistance, rigid));
    return mul(_ScrollLocalToWorld, float4(p,1)).xyz;
}
float ScrollFade(float anchor)
{
    if (_ScrollProjectionEnabled < .5) return 1;
    float z = anchor - _ScrollDistance;
    return saturate((z - _ScrollNear) / 3) * saturate((_ScrollBend.w - z) / 10);
}
#endif
