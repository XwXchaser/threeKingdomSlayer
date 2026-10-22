using System;
using UnityEngine;

/// <summary>
/// **Y 场景规格**（2026-10-23）：把「手摆的场景」变成「可编辑、可 diff、可校验的数据」。
///
/// 为什么用 ScriptableObject 而不是 JSON：
/// 规格是**运行时/构建期都要读的数据** ✅，按 <c>design/anti-ghost-reference.md</c> ✅
/// 必须能从 Inspector 追踪到源头 ✅；JSON 留给美术生产记录（<c>slice_manifest.json</c> 等 ✅）。
///
/// 坐标约定：**展开态**（flat）✅ —— 场景里的作者位就是展开态 ✅，弯曲由 shader 做 ✅（见 <c>ScrollWorldItem</c> ✅）。
/// 排序约定：<see cref="YSceneGroup.sortBaseline"/> 是**组基准** ✅，
/// 组内条目用 <see cref="YPropEntry.sortBias"/>（相对偏移 ✅）表达前后 ✅；
/// **组基准 + 最大 sortBias 不得越过世界基准** ✅（否则整组会压在所有场景物之上 ✅）。
/// </summary>
[CreateAssetMenu(fileName = "YSceneSpec", menuName = "一夫当关/Y 场景规格")]
public sealed class YSceneSpec : ScriptableObject
{
    [Tooltip("路线 id（与 FakeRouteStageConfig 对齐）")]
    public string routeId = "";
    [Tooltip("关卡 id")]
    public int stageId = -1;

    [Tooltip("世界基准 sortingOrder：所有常驻布景都用它。组基准 + 组内最大 sortBias 不得大于它。")]
    public int renderWorldBaseline = 0;

    [Tooltip("每条分支的地面材质（分叉路要两套：左支 / 共用）")]
    public YGroundProfile[] groundProfiles = Array.Empty<YGroundProfile>();

    [Tooltip("节点布景规格")]
    public YNodeSceneSpec[] nodeSpecs = Array.Empty<YNodeSceneSpec>();

    [Tooltip("过道段规格（按需装载的段 prefab）")]
    public YSegmentSpec[] segmentSpecs = Array.Empty<YSegmentSpec>();
}

/// <summary>一个过道段（按需装载的段 prefab）。</summary>
[Serializable]
public sealed class YSegmentSpec
{
    [Tooltip("段 prefab（沿路装饰的一段）")]
    public GameObject prefab;
    [Tooltip("段起点沿路米数")]
    public float startDistance = 0f;
    [Tooltip("段终点沿路米数")]
    public float endDistance = 0f;
    [Tooltip("常驻（不参与按需卸载）")]
    public bool alwaysResident = false;
}

/// <summary>一条分支的地面材质（分叉路：左支一套 + 共用一套）。</summary>
[Serializable]
public sealed class YGroundProfile
{
    [Tooltip("分支标识（common / left / right）")]
    public string branchId = "common";
    [Tooltip("地面材质（含该分支用到的贴图）")]
    public Material material;
    [Tooltip("分支混合权重（0~1）")]
    [Range(0f, 1f)] public float blend = 0.6f;
}

/// <summary>一个节点的布景规格。</summary>
[Serializable]
public sealed class YNodeSceneSpec
{
    [Tooltip("节点 id（与 FakeRouteNodeConfig.nodeId 对齐）")]
    public string nodeId = "";
    [Tooltip("锚点沿路米数（权威值在 ScrollRouteNodePresentation.worldDistance，不等距）")]
    public float anchorDistance = 0f;
    [Tooltip("分组（每组一个基准 sortingOrder）")]
    public YSceneGroup[] groups = Array.Empty<YSceneGroup>();
    [Tooltip("可交互物（箱子等）")]
    public YPropEntry[] interactives = Array.Empty<YPropEntry>();
}

/// <summary>一个分组：某个容器路径下的全部条目。</summary>
[Serializable]
public sealed class YSceneGroup
{
    [Tooltip("组路径（相对节点根，'/' 分隔；空 = 直接挂在节点根下）")]
    public string path = "";
    [Tooltip("组基准 sortingOrder（必须让 组基准+最大sortBias ≤ 世界基准）")]
    public int sortBaseline = 0;
    [Tooltip("组内条目（路径以本组路径开头）")]
    public YPropEntry[] entries = Array.Empty<YPropEntry>();

    /// <summary>组名（路径的最后一段 ✅，仅用于显示 ✅）。</summary>
    public string GroupName => string.IsNullOrEmpty(path) ? "(节点根)" : path.Substring(path.LastIndexOf('/') + 1);
}

/// <summary>一条布景（一个 GameObject）。</summary>
[Serializable]
public sealed class YPropEntry
{
    [Tooltip("对象名")]
    public string name = "";
    [Tooltip("完整相对路径（相对节点根，'/' 分隔）—— 容器层级靠它还原 ✅")]
    public string path = "";
    public Vector3 localPosition = Vector3.zero;
    public Vector3 localEulerAngles = Vector3.zero;
    public Vector3 localScale = Vector3.one;

    [Tooltip("渲染方式：Sprite = 透明精灵（按 z 排序）；Solid3D = 立体件（写深度，会遮挡更远的东西）")]
    public YRenderKind renderKind = YRenderKind.Sprite;
    [Tooltip("精灵（Sprite 模式）")]
    public Sprite sprite;
    [Tooltip("网格（Solid3D 模式）")]
    public Mesh mesh;
    [Tooltip("材质")]
    public Material material;

    [Tooltip("相对组基准的排序偏移（组内前后关系）")]
    public int sortBias = 0;
    [Tooltip("脚点约定：Foot = 脚点贴地（导入时 pivot 按 Alpha 底边）；Center = 中心")]
    public YPivotMode pivotMode = YPivotMode.Foot;
    [Tooltip("脚点相对物体原点的偏移（脚点模式 = 精灵 pivot 到精灵底边的距离）")]
    public float footOffsetY = 0f;
    [Tooltip("风动网格（树/旗的 Wind Mesh 子件）")]
    public bool isWindMesh = false;
    [Tooltip("生成时是否激活（场景里有一批隐藏件，不记就会被动点亮）")]
    public bool active = true;
    [Tooltip("被手工微调过：生成器不再覆盖这一条")]
    public bool userModified = false;
}

public enum YRenderKind { Sprite, Solid3D }
public enum YPivotMode { Foot, Center }
