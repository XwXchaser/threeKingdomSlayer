using UnityEngine;

/// <summary>
/// 敌人描边表现（穿刺提示 / 霸体 / C 技起始帧 / QTE / 死亡与转阶段抑制）。
///
/// 从 <c>Enemy</c>（原 5224 行上帝类）的「描边系统」区段抽出。职责边界：
///   • 本类只管材质属性与优先级顺序，不持有任何游戏状态；
///   • 状态由 <c>Enemy</c> 每帧打包成 <see cref="OutlineFacts"/> 传入；
///   • 不是 MonoBehaviour —— 由 Enemy 自己 new 出来，避免改动 prefab 序列化、
///     也避免在对象池复用路径上多一次 GetComponent。
///
/// 优先级（与拆分前完全一致）：死亡/转阶段 &gt; 调试强制 &gt; QTE &gt; 霸体 &gt; C 技帧 &gt; 穿刺提示 &gt; 无。
/// </summary>
public sealed class EnemyOutline
{
    /// <summary>Enemy 侧的状态快照。全部是值类型，避免每帧分配。</summary>
    public struct OutlineFacts
    {
        public bool PierceHighlighted;
        public bool Dead;
        public bool PhaseTransitioning;
        public bool QteAttacking;
        public bool SuperArmor;
        public bool CFrameAttacking;
        public Enemy.OutlineDebugMode DebugMode;
        public Color CColor;
        public float CWidth;
        public Color SuperArmorColor;
        public float SuperArmorWidth;
        public Color QteColor;
        public float QteWidth;
    }

    private const float OUTLINE_WIDTH_DEFAULT = 8f;
    private const float PIERCE_OUTLINE_WIDTH = 3f;
    private const float PIERCE_HIGHLIGHT_MIN_STRENGTH = 0.30f;
    private const float PIERCE_HIGHLIGHT_MAX_STRENGTH = 0.80f;
    private static readonly Color OUTLINE_COLOR_C_DEFAULT = Color.red;
    private static readonly Color OUTLINE_COLOR_SUPER_ARMOR_DEFAULT = new Color(1f, 0.5f, 0f); // 橙色
    private static readonly Color OUTLINE_COLOR_QTE_DEFAULT = new Color(0.2f, 0.4f, 1f);        // 蓝色

    private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();

    public void Apply(SpriteRenderer renderer, in OutlineFacts f)
    {
        if (renderer == null) return;

        renderer.GetPropertyBlock(_block);

        bool showPierceHighlight = f.PierceHighlighted
            && !f.Dead
            && !f.PhaseTransitioning
            && !f.QteAttacking
            && !f.SuperArmor
            && !f.CFrameAttacking;
        float piercePulse = showPierceHighlight
            ? 0.5f + 0.5f * Mathf.Sin(GameplayPause.UnscaledTime * 14f * Mathf.PI)
            : 0f;
        float pierceHighlightStrength = Mathf.Lerp(
            PIERCE_HIGHLIGHT_MIN_STRENGTH,
            PIERCE_HIGHLIGHT_MAX_STRENGTH,
            piercePulse);
        _block.SetFloat("_PierceHighlightStrength", showPierceHighlight ? pierceHighlightStrength : 0f);

        // 死亡或转阶段时不显示描边
        if (f.Dead || f.PhaseTransitioning)
        {
            _block.SetFloat("_OutlineEnabled", 0f);
            renderer.SetPropertyBlock(_block);
            return;
        }

        // 调试强制描边（优先于所有游戏状态）
        if (f.DebugMode != Enemy.OutlineDebugMode.None)
        {
            ApplyDebugOutline(renderer, in f);
            return;
        }

        // QTE 描边优先级最高（覆盖霸体 / CFrame 描边）
        if (f.QteAttacking)
        {
            Color c = f.QteColor != Color.clear ? f.QteColor : OUTLINE_COLOR_QTE_DEFAULT;
            float w = f.QteWidth > 0f ? f.QteWidth : OUTLINE_WIDTH_DEFAULT;
            _block.SetColor("_OutlineColor", c);
            _block.SetFloat("_OutlineWidth", w);
            _block.SetFloat("_OutlineEnabled", 1f);
        }
        else if (f.SuperArmor)
        {
            Color c = f.SuperArmorColor != Color.clear ? f.SuperArmorColor : OUTLINE_COLOR_SUPER_ARMOR_DEFAULT;
            float w = f.SuperArmorWidth > 0f ? f.SuperArmorWidth : OUTLINE_WIDTH_DEFAULT;
            _block.SetColor("_OutlineColor", c);
            _block.SetFloat("_OutlineWidth", w);
            _block.SetFloat("_OutlineEnabled", 1f);
        }
        else if (f.CFrameAttacking)
        {
            Color c = f.CColor != Color.clear ? f.CColor : OUTLINE_COLOR_C_DEFAULT;
            float w = f.CWidth > 0f ? f.CWidth : OUTLINE_WIDTH_DEFAULT;
            _block.SetColor("_OutlineColor", c);
            _block.SetFloat("_OutlineWidth", w);
            _block.SetFloat("_OutlineEnabled", 1f);
        }
        else if (f.PierceHighlighted)
        {
            _block.SetColor("_OutlineColor", Color.white);
            _block.SetFloat("_OutlineWidth", PIERCE_OUTLINE_WIDTH);
            _block.SetFloat("_OutlineEnabled", 0.35f + 0.65f * piercePulse);
        }
        else
        {
            _block.SetFloat("_OutlineEnabled", 0f);
        }

        renderer.SetPropertyBlock(_block);
    }

    private void ApplyDebugOutline(SpriteRenderer renderer, in OutlineFacts f)
    {
        Color c;
        float w;
        switch (f.DebugMode)
        {
            case Enemy.OutlineDebugMode.CAttack:
                c = f.CColor != Color.clear ? f.CColor : OUTLINE_COLOR_C_DEFAULT;
                w = f.CWidth > 0f ? f.CWidth : OUTLINE_WIDTH_DEFAULT;
                break;
            case Enemy.OutlineDebugMode.SuperArmor:
                c = f.SuperArmorColor != Color.clear ? f.SuperArmorColor : OUTLINE_COLOR_SUPER_ARMOR_DEFAULT;
                w = f.SuperArmorWidth > 0f ? f.SuperArmorWidth : OUTLINE_WIDTH_DEFAULT;
                break;
            case Enemy.OutlineDebugMode.QTE:
                c = f.QteColor != Color.clear ? f.QteColor : OUTLINE_COLOR_QTE_DEFAULT;
                w = f.QteWidth > 0f ? f.QteWidth : OUTLINE_WIDTH_DEFAULT;
                break;
            default:
                return;
        }
        _block.SetColor("_OutlineColor", c);
        _block.SetFloat("_OutlineWidth", w);
        _block.SetFloat("_OutlineEnabled", 1f);
        renderer.SetPropertyBlock(_block);
    }
}
