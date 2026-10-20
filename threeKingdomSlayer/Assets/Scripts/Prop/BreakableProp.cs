using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 可破坏补给物（木箱 / 酒缸）。
/// 设计要点：
/// - 按“带血量的单位”处理，但不显示血条；血量在 Inspector 配置。
/// - 玩家戳击所在列即命中本列的道具；道具破坏后立刻从注册表移除，不再能被戳击。
/// - 奖励只在首次破坏时结算一次，并写入路线快照（已破坏 + 已获取内容）。
/// 所有引用都来自 Inspector，不使用 Resources.Load / 字符串查找。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class BreakableProp : MonoBehaviour
{
    [Serializable]
    public struct PropSaveEntry
    {
        public string propId;
        public bool destroyed;
        public int grantedCoins;
        public int grantedUltimateEnergy;
    }

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
    [SerializeField] private bool showHint = true;
    [SerializeField] private float hintPulseHz = 0.9f;
    [SerializeField] private Color hintTint = new Color(1f, 0.93f, 0.66f, 1f);
    [Range(0f, 1f)][SerializeField] private float hintPulseAmount = 0.35f;

    /// <summary>当前在场且可交互的道具。</summary>
    public static readonly List<BreakableProp> All = new List<BreakableProp>();

    private static readonly Dictionary<string, PropSaveEntry> SavedStates = new Dictionary<string, PropSaveEntry>();

    public string PropId => string.IsNullOrEmpty(propId) ? AutoId : propId;
    public bool IsDestroyed { get; private set; }
    public int HitsTaken { get; private set; }

    private string AutoId => gameObject.scene.name + "/" + name;

    private MaterialPropertyBlock _mpb;
    private Coroutine _breakRoutine;
    private Coroutine _blinkRoutine;
    private float _hintTime;
    private Color _idleColor = Color.white;

    // ---------------- lifecycle ----------------

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            if (baseSprite == null) baseSprite = spriteRenderer.sprite;
            _idleColor = spriteRenderer.color;
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
        if (IsDestroyed || !showHint || spriteRenderer == null)
        {
            if (_hintTime > 0f) StopHint();
            return;
        }

        _hintTime += Time.unscaledDeltaTime * hintPulseHz;
        float pulse = (Mathf.Sin(_hintTime * Mathf.PI * 2f) + 1f) * 0.5f * hintPulseAmount;
        spriteRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor("_Color", Color.Lerp(_idleColor, hintTint, pulse));
        spriteRenderer.SetPropertyBlock(_mpb);
    }

    private void StopHint()
    {
        _hintTime = 0f;
        if (spriteRenderer != null)
        {
            spriteRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor("_Color", _idleColor);
            spriteRenderer.SetPropertyBlock(_mpb);
        }
    }

    // ---------------- column resolution ----------------

    /// <summary>本道具当前所属的敌人列（0..4）。</summary>
    public int ResolveColumn()
    {
        if (columnOverride >= 0) return columnOverride;

        var stage = StageController.Instance;
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

    /// <summary>由攻击系统调用：命中该列最靠前的可破坏道具。</summary>
    public static bool TryHitColumn(int column, AttackType attackType, float damage)
    {
        for (int i = All.Count - 1; i >= 0; i--)
        {
            var prop = All[i];
            if (prop == null || prop.IsDestroyed) continue;
            if (prop.ResolveColumn() != column) continue;
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
        _mpb.SetColor("_Color", _idleColor);
        spriteRenderer.SetPropertyBlock(_mpb);
        _blinkRoutine = null;
    }

    private void Break()
    {
        if (IsDestroyed) return;
        IsDestroyed = true;
        All.Remove(this);
        StopHint();

        var entry = new PropSaveEntry { propId = PropId, destroyed = true };
        if (coinReward > 0 && PlayerState.Instance != null)
        {
            PlayerState.Instance.AddCoins(coinReward);
            entry.grantedCoins = coinReward;
        }
        if (fillUltimateEnergy && UltimateSystem.Instance != null)
        {
            int missing = Mathf.Max(0, UltimateSystem.Instance.maxUltimateEnergy - UltimateSystem.Instance.CurrentEnergy);
            if (missing > 0) UltimateSystem.Instance.AddEnergy(missing);
            entry.grantedUltimateEnergy = missing;
        }

        SavedStates[PropId] = entry;
        FakeRouteRuntime.Instance?.NotifyPropDestroyed();

        if (breakFrames != null && breakFrames.Length > 0 && spriteRenderer != null)
            _breakRoutine = StartCoroutine(PlayBreakFrames());
        else
            ApplyRuinVisual();
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
        if (holdLastFrameSeconds > 0f) yield return new WaitForSeconds(holdLastFrameSeconds);
        // 残骸保留最后一帧：隐藏交互渲染但不销毁对象，避免存档回放时位置漂移。
        spriteRenderer.enabled = false;
        _breakRoutine = null;
    }

    private void ApplyRuinVisual()
    {
        if (spriteRenderer == null) return;
        if (breakFrames != null && breakFrames.Length > 0 && breakFrames[breakFrames.Length - 1] != null)
        {
            spriteRenderer.sprite = breakFrames[breakFrames.Length - 1];
            spriteRenderer.enabled = holdLastFrameSeconds > 0f;
        }
        else
        {
            spriteRenderer.enabled = false;
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
