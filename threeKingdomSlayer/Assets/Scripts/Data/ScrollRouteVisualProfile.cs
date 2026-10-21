using System;
using UnityEngine;

[Serializable]
public sealed class ScrollPropPlacement
{
    public Sprite sprite;
    [Tooltip("横向世界坐标；0 保留给中央战斗区。")]
    public float x;
    [Tooltip("前后距离；负值更靠近镜头。")]
    public float z;
    [Min(0.05f)] public float scale = 1f;
    public int sortingOrder;
    public bool flipX;
    public bool loop = true;
}

public enum ScrollEnvironmentMode
{
    ValleyRoad,
    StrongholdEntrance,
    StrongholdInterior,
    NoRoad
}

[CreateAssetMenu(fileName = "NewScrollRouteVisualProfile", menuName = "一夫当关/卷轴道路视觉配置")]
public sealed class ScrollRouteVisualProfile : ScriptableObject
{
    [Header("Environment")]
    public ScrollEnvironmentMode environmentMode = ScrollEnvironmentMode.ValleyRoad;
    public bool showRoad = true;
    public bool showShoulders = true;

    [Header("Road presentation only")]
    [Min(3f)] public float roadWidth = 7f;
    [Range(0f, 0.04f)] public float curvature = 0.012f;
    public Material terrainMaterial;
    public Material groundMaterial;
    public Texture2D background;
    public Sprite[] nearSprites;
    public Sprite[] middleSprites;
    public Sprite[] shoulderSprites;
    public Sprite[] foregroundSprites;
    public ScrollPropPlacement[] fixedPlacements;
    [Tooltip("固定摆放相对该区段起点的局部距离；区段本身按 startDistance 放入世界序列。")]
    public bool placementsUseLocalDistance = true;
    [Range(0f, 0.7f)] public float backgroundLift = 0.4f;
    public float tileSize = 4f;
    public float shoulderLength = 8f;
    public float shoulderWidth = 1.1f;
    public float nearMargin = 3f;
    public float middleMargin = 1.2f;
    [Range(4, 100)] public int largePropCount = 32;
    [Range(8, 200)] public int smallPropCount = 80;
    [Range(0.2f, 3f)] public float largePropScale = 1.3f;
    [Range(0.1f, 2f)] public float smallPropScale = 0.65f;
    [Min(0f)] public float roadClearance = 0.35f;
}
