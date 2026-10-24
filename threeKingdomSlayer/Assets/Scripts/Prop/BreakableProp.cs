using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 可破坏补给物（木箱 / 酒缸）。
/// 设计要点：
/// - 按“带血量的单位”处理，但不显示血条；血量在 Inspector 配置。
/// - **点中本体**才算命中：点一下落在物件的点击半径内 → 该物件受击（与所在列无关）。
/// - 分层：局内层（关卡态）奖励进局内资源并记路线快照；局外层（就绪态）奖励进存档金币，
///   交互后渐变消失并销毁，不进路线快照。
/// - 所有引用都来自 Inspector，不使用 Resources.Load / 字符串查找。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class BreakableProp : MonoBehaviour, ISupplyPropTarget
{

    /// <summary>
    /// 关卡/阵型信息（只读）：道具按 x 坐标反查所在列。
    /// 历史上直接摸 StageController 门面，现只依赖角色接口（2026-10-20 批次 1）。
    /// </summary>
    private IStageFormation _stageFormation;

    private IStageFormation StageFormation => _stageFormation ??= StageRoles.Formation;
    // 存档条目 PropSaveEntry 已提成顶层类型（数据层，2026-10-22 asmdef 分层第二批）。
    [Header("身份")]
    [Tooltip("场景内唯一标识，用于存档记录；留空时用 场景名/对象名 自动生成")]
    [SerializeField] private string propId = "";
    [Tooltip("-1 = 按世界 X 自动匹配最近的敌人列；>=0 强制指定列")]
    [SerializeField] private int columnOverride = -1;

    [Header("美术")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("常态图（未破坏）")]
    [SerializeField] private Sprite baseSprite;
    [Tooltip("破坏序列帧，按顺序播放；最后一帧作为残骸保留")]
    [SerializeField] private Sprite[] breakFrames = new Sprite[0];
    [SerializeField] private float breakFrameFps = 12f;
    [Tooltip("末帧停留时间；<=0 表示立刻隐藏")]
    [SerializeField] private float holdLastFrameSeconds = 1.2f;

    [Header("耐久")]
    [SerializeField] private int maxHits = 1;

    [Header("奖励（只在首次破坏时结算一次）")]
    [SerializeField] private int coinReward = 100;
    [Tooltip("勾选表示把必杀能量直接充满（酒缸用）")]
    [SerializeField] private bool fillUltimateEnergy = false;

    [Header("交互提示")]
    [Tooltip("用描边（材质需为 Custom/EnemyOutline）做可交互提示；关闭则退回轻微明度呼吸")]
    [SerializeField] private bool useOutline = true;
    [SerializeField] private Color outlineColor = new Color(1f, 0.93f, 0.66f, 1f);
    [Tooltip("描边宽度脉动区间（材质参数，单位＝贴图像素；1024 贴图建议 10~18）")]
    [SerializeField] private Vector2 outlineWidthRange = new Vector2(10f, 16f);
    [SerializeField] private float hintPulseHz = 0.9f;
    [Tooltip("提示只在玩家靠近时出现（到玩家的水平距离上限）")]
    [SerializeField] private float hintVisibleDistance = 14f;
    [Tooltip("可被戳中的距离上限（到玩家的水平距离）：超出则远处空挥不会远程打碎本道具")]
    [SerializeField] private float maxInteractDistance = 16f;
    [Tooltip("破坏后是否直接消失（false = 保留末帧残骸，残骸随路线一起走）")]
    [SerializeField] private bool hideWhenDestroyed = false;

    [Header("层与命中")]
    [Tooltip("局内层：关卡态可交互，奖励进局内资源，状态记路线快照；局外层：就绪态可交互，奖励进存档金币，交互后渐变消失并销毁")]
    [SerializeField] private SupplyLayer supplyLayer = SupplyLayer.InRun;
    [Tooltip("点中判定的世界半径（以物件中心为圆心）")]
    [SerializeField] private float hitRadius = 0.95f;
    [Tooltip("局外层：被交互后渐变消失并销毁的时长（秒）")]
    [SerializeField] private float fadeOutSeconds = 1f;
    [Tooltip("局外层：奖励的局外金币（存档货币）；<=0 时用 coinReward")]
    [SerializeField] private int outOfRunCoinReward = 100;

    /// <summary>当前在场且可交互的道具。</summary>
    public static readonly List<BreakableProp> All = new List<BreakableProp>();

    /// <summary>
    /// 当前是否处于「就绪态（主菜单 / 落地页）」。
    /// 由路线运行器在状态切换时登记：就绪态 = 局外层物件可交互；关卡态 = 局内层可交互。
    /// 没有登记时默认 false（局内层可交互），保证单场景 / 教学场景也能用。
    /// </summary>
    public static bool HubState { get; set; }

    private static readonly Dictionary<string, PropSaveEntry> SavedStates = new Dictionary<string, PropSaveEntry>();

    public string PropId => string.IsNullOrEmpty(propId) ? AutoId : propId;
    public bool IsDestroyed { get; private set; }
    public int HitsTaken { get; private set; }
    public SupplyLayer Layer => supplyLayer;
    public float HitRadius => hitRadius;

    private string AutoId => gameObject.scene.name + "/" + name;

    private MaterialPropertyBlock _mpb;
    private Coroutine _breakRoutine;
    private Coroutine _blinkRoutine;
    private float _hintTime;

    // ---------------- lifecycle ----------------

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            if (baseSprite == null) baseSprite = spriteRenderer.sprite;
        }
        _mpb = new MaterialPropertyBlock();

        // 从存档恢复：已破坏的道具直接呈现残骸，不注册、不再发奖励。
        if (SavedStates.TryGetValue(PropId, out var saved) && saved.destroyed)
        {
            IsDestroyed = true;
            ApplyRuinVisual();
        }
    }

    private void OnEnable()
    {
        if (!IsDestroyed && !All.Contains(this)) All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
        StopHint();
    }

    private void Update()
    {
        if (IsDestroyed || !ShouldShowHint())
        {
            if (_hintTime > 0f) StopHint();
            return;
        }

        _hintTime += GameplayPause.UnscaledDelta * hintPulseHz;
        float pulse = (Mathf.Sin(_hintTime * Mathf.PI * 2f) + 1f) * 0.5f;
        ApplyHint(pulse);
    }

    /// <summary>提示是否应该出现：未破坏 + 玩家已经靠近。</summary>
    private bool ShouldShowHint()
    {
        return DistanceToPlayer() <= hintVisibleDistance;
    }

    /// <summary>可被戳中：距离在范围内（防止在别的节点空挥远程打碎本道具）。</summary>
    public bool IsWithinInteractDistance()
    {
        return DistanceToPlayer() <= maxInteractDistance;
    }

    /// <summary>
    /// 到玩家的水平距离（以玩家为基准，不含高度差）。
    /// 不用相机：战斗相机在玩家身后约 10m，用相机会把路边的道具误判成“太远”。
    /// </summary>
    private float DistanceToPlayer()
    {
        var ps = PlayerState.Instance;
        if (ps == null) return 0f;
        Vector3 d = transform.position - ps.transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    /// <summary>可交互提示：描边脉动（材质为 Custom/EnemyOutline 时生效）。</summary>
    private void ApplyHint(float pulse)
    {
        if (spriteRenderer == null) return;
        spriteRenderer.GetPropertyBlock(_mpb);
        if (useOutline)
        {
            float width = Mathf.Lerp(outlineWidthRange.x, outlineWidthRange.y, pulse);
            _mpb.SetColor("_OutlineColor", outlineColor);
            _mpb.SetFloat("_OutlineWidth", width);
            _mpb.SetFloat("_OutlineEnabled", 1f);
        }
        else
        {
            // 没有描边材质时的退化表现：轻微明度呼吸
            _mpb.SetColor("_Color", Color.Lerp(Color.white, outlineColor, pulse * 0.35f));
        }
        spriteRenderer.SetPropertyBlock(_mpb);
    }

    private void StopHint()
    {
        _hintTime = 0f;
        if (spriteRenderer != null)
        {
            spriteRenderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat("_OutlineEnabled", 0f);
            spriteRenderer.SetPropertyBlock(_mpb);
        }
    }

    // ---------------- column resolution ----------------

    /// <summary>本道具当前所属的敌人列（0..4）。</summary>
    public int ResolveColumn()
    {
        if (columnOverride >= 0) return columnOverride;

        var stage = StageFormation;
        if (stage != null)
        {
            float x = transform.position.x;
            int best = 2;
            float bestDistance = float.MaxValue;
            for (int col = 0; col < 5; col++)
            {
                float d = Mathf.Abs(x - stage.GetFormationOffset(col, 0));
                if (d < bestDistance) { bestDistance = d; best = col; }
            }
            return best;
        }

        var cam = Camera.main;
        if (cam == null) return 2;
        float screenX = cam.WorldToScreenPoint(transform.position).x / Mathf.Max(1, Screen.width);
        return Mathf.Clamp(Mathf.FloorToInt(screenX * 5f), 0, 4);
    }

    // ---------------- hit handling ----------------

    /// <summary>可交互：未破坏 + 可见 + 在距离内 + 属于当前状态的层。</summary>
    public bool IsSupplyInteractable
    {
        get
        {
            if (IsDestroyed) return false;
            if (spriteRenderer == null || !spriteRenderer.enabled) return false;
            if (!IsWithinInteractDistance()) return false;
            // 局外层只在就绪态（主菜单）可交互；局内层只在关卡态可交互。
            return supplyLayer == SupplyLayer.OutOfRun ? HubState : !HubState;
        }
    }

    /// <summary>被戳中（由 ISupplyPropTarget 契约调用）。</summary>
    public void ApplySupplyHit(AttackType attackType, float damage)
    {
        if (!IsSupplyInteractable) return;
        ApplyHit(damage);
    }

    /// <summary>屏幕落点 → 命中的补给物件（取离落点最近的一个）。</summary>
    public static bool TryHitAtScreenPoint(Vector2 screenPos, out BreakableProp target)
    {
        target = null;
        var cam = Camera.main;
        if (cam == null) return false;
        float bestDistance = float.MaxValue;
        for (int i = All.Count - 1; i >= 0; i--)
        {
            var prop = All[i];
            if (prop == null || !prop.IsSupplyInteractable) continue;
            if (!prop.TryGetScreenHitDistance(cam, screenPos, out float distance)) continue;
            if (distance > prop.hitRadius) continue;
            if (distance >= bestDistance) continue;
            bestDistance = distance;
            target = prop;
        }
        return target != null;
    }

    /// <summary>屏幕落点到本道具的世界距离（把落点投影到与本道具同深的平面上算，分辨率无关）。</summary>
    private bool TryGetScreenHitDistance(Camera cam, Vector2 screenPos, out float distance)
    {
        distance = 0f;
        Vector3 camPos = cam.transform.position;
        Vector3 forward = cam.transform.forward;
        float depth = Vector3.Dot(transform.position - camPos, forward);
        if (depth <= 0.01f) return false;
        Ray ray = cam.ScreenPointToRay(screenPos);
        float forwardDot = Vector3.Dot(ray.direction, forward);
        if (Mathf.Abs(forwardDot) < 0.0001f) return false;
        float t = depth / forwardDot;
        if (t <= 0f) return false;
        distance = Vector3.Distance(camPos + ray.direction * t, transform.position);
        return true;
    }

    /// <summary>局外生成时由管理器配置：层 / 实例 id / 局外金币。</summary>
    public void ConfigureOutOfRun(string instanceId, int coinReward)
    {
        supplyLayer = SupplyLayer.OutOfRun;
        propId = instanceId;
        outOfRunCoinReward = coinReward;
    }

    /// <summary>局外层：到期消失（渐变后销毁，不结算）。</summary>
    public void DespawnWithFade()
    {
        if (IsDestroyed) return;
        IsDestroyed = true;
        All.Remove(this);
        StopHint();
        StartCoroutine(FadeOutAndDestroy());
    }

    /// <summary>由攻击系统调用：命中该列最靠前的可破坏道具。</summary>
    [System.Obsolete("补给物件改为「点中本体」判定，不再按列连带命中；见 TryHitAtScreenPoint。")]
    public static bool TryHitColumn(int column, AttackType attackType, float damage)
    {
        for (int i = All.Count - 1; i >= 0; i--)
        {
            var prop = All[i];
            if (prop == null || prop.IsDestroyed) continue;
            if (prop.ResolveColumn() != column) continue;
            // 距离门槛：不在身边的道具不响应远程空挥（否则在别的节点也能把 N1 的箱子打碎）
            if (!prop.IsWithinInteractDistance()) continue;
            prop.ApplyHit(damage);
            return true;
        }
        return false;
    }

    public void ApplyHit(float damage)
    {
        if (IsDestroyed) return;
        HitsTaken++;
        if (HitsTaken < Mathf.Max(1, maxHits))
        {
            if (_blinkRoutine != null) StopCoroutine(_blinkRoutine);
            _blinkRoutine = StartCoroutine(HitBlink());
            return;
        }
        Break();
    }

    private IEnumerator HitBlink()
    {
        if (spriteRenderer == null) yield break;
        spriteRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor("_Color", Color.white);
        spriteRenderer.SetPropertyBlock(_mpb);
        yield return new WaitForSeconds(0.06f);
        spriteRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor("_Color", Color.white);
        spriteRenderer.SetPropertyBlock(_mpb);
        _blinkRoutine = null;
    }

    private void Break()
    {
        if (IsDestroyed) return;
        IsDestroyed = true;
        All.Remove(this);
        StopHint();

        string propId = PropId;
        Vector3 worldPos = transform.position;
        int coins = supplyLayer == SupplyLayer.OutOfRun
            ? (outOfRunCoinReward > 0 ? outOfRunCoinReward : coinReward)
            : coinReward;

        var entry = new PropSaveEntry { propId = propId, destroyed = true, grantedCoins = coins };
        bool outOfRun = supplyLayer == SupplyLayer.OutOfRun;

        // 状态记账：**立即**做（不能因为动画被中断而丢进度 / 丢去重标记）。
        // 注意 PropSaveEntry 是 struct，存进字典的是当时的副本 → 字段必须在这里就填好。
        if (outOfRun)
        {
            // 局外层：同一个实例不重复发（防反复进出主菜单刷金币）。
            // 必须在这里标，不能等动画播完——否则「已碎 → 销毁 → 管理器重新生成」的竞态会开出一个重复物件。
            SaveManager.MarkSupplyClaimed(propId);
        }
        else
        {
            SavedStates[propId] = entry;
            PropDestroyNotifiers.Current?.NotifyPropDestroyed();
        }

        // 发奖：交给表现端口（UI 播完「金币飞进计数器」的动画再入账；没有 UI 时端口立即回调）。
        // 回调幂等：最多入账一次。
        bool granted = false;
        System.Action grant = () =>
        {
            if (granted) return;
            granted = true;
            if (outOfRun)
            {
                // 局外层：奖励进存档金币；**不进路线快照**（那是局内进度，不是局外状态）
                if (coins > 0) SaveManager.AddCoins(coins);
            }
            else
            {
                if (coins > 0 && PlayerState.Instance != null) PlayerState.Instance.AddCoins(coins);
                if (fillUltimateEnergy && UltimateSystem.Instance != null)
                {
                    int missing = Mathf.Max(0, UltimateSystem.Instance.maxUltimateEnergy - UltimateSystem.Instance.CurrentEnergy);
                    if (missing > 0) UltimateSystem.Instance.AddEnergy(missing);
                }
            }
        };

        SupplyRewardPort.Raise(new SupplyRewardInfo
        {
            worldPosition = worldPos,
            layer = supplyLayer,
            coins = coins,
            propId = propId,
            grant = grant
        });

        if (breakFrames != null && breakFrames.Length > 0 && spriteRenderer != null)
            _breakRoutine = StartCoroutine(PlayBreakFrames());
        else
        {
            ApplyRuinVisual();
            if (outOfRun) StartCoroutine(FadeOutAndDestroy());
        }
    }

    private IEnumerator PlayBreakFrames()
    {
        float interval = 1f / Mathf.Max(1f, breakFrameFps);
        for (int i = 0; i < breakFrames.Length; i++)
        {
            if (breakFrames[i] == null) continue;
            spriteRenderer.sprite = breakFrames[i];
            yield return new WaitForSeconds(interval);
        }
        if (holdLastFrameSeconds > 0f && supplyLayer == SupplyLayer.InRun)
            yield return new WaitForSeconds(holdLastFrameSeconds);
        // 残骸保留最后一帧：**不写 renderer.enabled**（可见性归 RoutePropFollower / 距离窗口所有），
        // 否则会与窗口抢同一个开关，导致「已破坏的物件在远处复活成残骸」。
        ApplyDestroyedVisibility();
        if (supplyLayer == SupplyLayer.OutOfRun) StartCoroutine(FadeOutAndDestroy());
        _breakRoutine = null;
    }

    /// <summary>局外层：渐变消失后销毁（用 unscaled 计时，暂停时也会收尾）。</summary>
    private IEnumerator FadeOutAndDestroy()
    {
        float duration = Mathf.Max(0.05f, fadeOutSeconds);
        float startAlpha = spriteRenderer != null ? spriteRenderer.color.a : 1f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (spriteRenderer != null)
            {
                var c = spriteRenderer.color;
                c.a = Mathf.Lerp(startAlpha, 0f, t);
                spriteRenderer.color = c;
            }
            // 描边是材质属性，得跟着一起淡出（否则残影会留着）
            if (spriteRenderer != null)
            {
                spriteRenderer.GetPropertyBlock(_mpb);
                _mpb.SetFloat("_OutlineEnabled", 1f - t);
                spriteRenderer.SetPropertyBlock(_mpb);
            }
            yield return null;
        }
        Destroy(gameObject);
    }

    /// <summary>破坏后的可见性：默认保留残骸；hideWhenDestroyed 时直接隐藏对象。</summary>
    private void ApplyDestroyedVisibility()
    {
        if (hideWhenDestroyed)
            gameObject.SetActive(false);
    }

    private void ApplyRuinVisual()
    {
        if (spriteRenderer == null) return;
        if (breakFrames != null && breakFrames.Length > 0 && breakFrames[breakFrames.Length - 1] != null)
        {
            spriteRenderer.sprite = breakFrames[breakFrames.Length - 1];
            // 不写 renderer.enabled：可见性由 RoutePropFollower（路线物件）或距离窗口所有
            ApplyDestroyedVisibility();
        }
        else
        {
            ApplyDestroyedVisibility();
        }
    }

    // ---------------- persistence ----------------

    public static PropSaveEntry[] ExportStates()
    {
        var list = new List<PropSaveEntry>(SavedStates.Count);
        foreach (var kv in SavedStates)
        {
            if (kv.Value.destroyed) list.Add(kv.Value);
        }
        return list.ToArray();
    }

    /// <summary>合并导入：不会把本次运行中已破坏的道具重置为未破坏。</summary>
    public static void ImportStates(List<PropSaveEntry> states)
    {
        if (states == null) return;
        for (int i = 0; i < states.Count; i++)
        {
            var s = states[i];
            if (string.IsNullOrEmpty(s.propId) || !s.destroyed) continue;
            SavedStates[s.propId] = s;
        }
    }

    [ContextMenu("调试/立即破坏")]
    private void DebugBreak()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[BreakableProp] 仅运行时可用"); return; }
        ApplyHit(1f);
    }

    private void OnValidate()
    {
        maxHits = Mathf.Max(1, maxHits);
        breakFrameFps = Mathf.Max(1f, breakFrameFps);
    }
}
