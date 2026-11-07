using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    /// <summary>
    /// 规则与表现 —— 教学关导演的**规则与表现**部分 ✅（由 TutorialStageDirector.cs 拆出 ✅ ✅）
    /// </summary>
public partial class TutorialStageDirector : MonoBehaviour
{
    /// <summary>
    /// **教学文本的压暗（黑幕）**（2026-11-06 用户定）。
    ///
    /// 规则：局内教学文本出现 → 压暗画面；**一直压到这一拍的手势做完**（EndBeat）才撤。
    ///
    /// ⚠️ **压暗只能走「场景压暗」**（<c>TutorialSceneDim</c>：给背景渲染器乘亮度系数），
    ///   **不能用全屏 UI 遮罩** —— 战场是透视相机 + 世界物件（长枪是 MeshRenderer、敌人是精灵），
    ///   任何 Overlay 画布都必然盖在角色上面（层级无解）→ 用户原话：
    ///   「应该让说话的头像、敌人和武器都在遮罩上方，你把这些都遮住怎么教？」
    ///   → 敌人与武器必须保持亮，只有背景被压暗。
    ///
    /// 台词气泡与说话头像都是 UI → 场景压暗不影响它们。
    /// </summary>
    private void SetTeachingDim(bool on)
    {
        SetSceneDim(on);
    }
    // ── 教学招式白名单（还没教到的招式不许发出）────────────────
    // 口径：白名单 = **已经教完的招式 ∪ 当前这一拍要教的招式**。
    // 拍开始时把当前招式加进去（这一拍里玩家必须能出它），拍真的完成了它就转为"已教"。
    private readonly System.Collections.Generic.HashSet<AttackType> _taughtAttacks =
        new System.Collections.Generic.HashSet<AttackType>();
    private bool _attackGateOn;
    /// <summary>当前这一拍正在教的招式（收拍后转入"已教"）。</summary>
    private AttackType? _currentTeachAttack;
    /// <summary>这一拍是否真的完成了（完成才把招式转为"已教"）。</summary>
    private bool _beatCompleted;
    /// <summary>这一拍已经打中几次（TapCombo 用：要打满 requiredHits 次才算完成）。</summary>
    private int _beatHitCount;
    /// <summary>这一拍要打几次才算完成（1 = 一次就完成）。</summary>
    private int _beatRequiredHits = 1;
    /// <summary>这一拍刚打出了预期招式（等**敌人受击**确认「攻击成功」）。</summary>
    private bool _beatAttackFired;
    /// <summary>刚打出的招式类型（命中确认时写日志用）。</summary>
    private AttackType _beatLastAttackType = AttackType.Stab;
    /// <summary>刚打出的招式命中列（命中确认时写日志用）。</summary>
    private int _beatLastAttackColumn = -1;
    /// <summary>本拍正在监听受击的敌人（用来判断「攻击成功」= 真的打中了人）。</summary>
    private readonly List<Enemy> _beatDamageWatchers = new List<Enemy>();
    /// <summary>招式状态机（判定「刚打出去的那一招是不是这一拍教的」）。</summary>
    private PlayerMoveStateMachine _moveMachine;
    /// <summary>
    /// **连招蓄力拍的阶段**：0 = 不是这一拍；1 = 等第一记戳；2 = 等蓄满松手；3 = 已完成。
    /// （两段命中分开算：第一记戳只推进阶段，派生招式才收拍。）
    /// </summary>
    private int _comboChargePhase;

    /// <summary>手势种类 → 它教的招式。</summary>
    private static AttackType? AttackTypeFor(GestureHintKind kind)
    {
        switch (kind)
        {
            case GestureHintKind.Tap: return AttackType.Stab;
            case GestureHintKind.TapCombo: return AttackType.Stab;
            case GestureHintKind.Swipe: return AttackType.Slash;
            case GestureHintKind.ChargedSwipe: return AttackType.Launch;
            case GestureHintKind.Parry:
            case GestureHintKind.ParryHold: return AttackType.Parry;
            default: return null;
        }
    }

    /// <summary>
    /// 这一拍要教的招式：**招式解锁引导**用天赋节点配的招式（<c>guideAttackType</c>），
    /// 其余按拍型推断。
    /// </summary>
    private static AttackType? TeachAttackFor(TutorialGestureStep step)
    {
        if (step != null && step.guideAttackType >= 0) return (AttackType)step.guideAttackType;
        return step != null ? AttackTypeFor(step.kind) : null;
    }

    /// <summary>把白名单刷成「已教完的 + 当前这一拍要教的」。</summary>
    private void ApplyAttackGate(AttackType? extra)
    {
        if (!_attackGateOn) return;
        var set = new System.Collections.Generic.HashSet<AttackType>(_taughtAttacks);
        if (extra.HasValue) set.Add(extra.Value);
        TutorialAttackGate.SetAllowed(set);
    }

    /// <summary>
    /// 只有教学关才限制招式；一离开教学路线（回落地页 / 进正式关）立刻放开。
    ///
    /// 注意：<b>不能</b>在“重新启用”时清空已教招式 —— 路线切场时 <c>RouteFlowHub.Current</c> 会短暂为 null，
    /// 那一帧会被当成“离开教学”→ 清空 → 玩家学过的招式又锁回去了。
    /// 已教招式的生命周期 = **一次教学关流程**（从 N1 第一波开始算），在 <c>OnWaveStarted</c> 里重置。
    /// </summary>
    private void UpdateAttackGate()
    {
        var route = RouteFlowHub.Current;
        bool onTutorial = route != null && tutorialRouteConfig != null && route.RouteConfig == tutorialRouteConfig;
        if (onTutorial)
        {
            if (!_attackGateOn)
            {
                _attackGateOn = true;
                ApplyAttackGate(null);
                Debug.Log("[Tutorial] 招式白名单启用：还没教到的招式不允许发出");
            }
            return;
        }
        if (_attackGateOn)
        {
            _attackGateOn = false;
            TutorialAttackGate.Clear();
        }
    }

    private void ApplyTutorialRules(StageConfig config)
    {
        var choices = UpgradeChoiceManager.Instance;
        if (choices == null) return;
        if (config == n1Battle)
        {
            // 节点一：**不弹三选1**（升级照常发生，但选择被吞掉）
            // —— 玩家全程只有一次三选1，在节点二第一波敌人被击杀、经验升级时给。
            choices.suppressChoices = true;
            choices.forcedChoices = new List<UpgradeDefinition>();
        }
        else
        {
            // 节点二：升级 → 三选1 = 教学技能池（旋风 / 海浪）。
            // 节点二第一波敌人的经验给足 → 那一杀正好升级 → 弹出技能三选1。
            choices.suppressChoices = false;
            choices.forcedChoices = tutorialChoicePool != null
                ? new List<UpgradeDefinition>(tutorialChoicePool)
                : new List<UpgradeDefinition>();
        }
    }

    private static bool TryGetRouteChoiceScreenPosition(out Vector2 screenPos)
    {
        screenPos = Vector2.zero;
        var buttons = FindObjectsOfType<RouteChoiceButton>();
        if (buttons == null || buttons.Length == 0) return false;
        var rect = buttons[0].transform as RectTransform;
        if (rect == null) return false;
        var canvas = rect.GetComponentInParent<Canvas>();
        var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        screenPos = RectTransformUtility.WorldToScreenPoint(cam, rect.TransformPoint(rect.rect.center));
        return true;
    }

    private static void SetEnemiesHold(bool hold)
    {
        if (EnemyManager.Instance == null) return;
        var alive = EnemyManager.Instance.GetAllAliveEnemies();
        for (int i = 0; i < alive.Count; i++)
            if (alive[i] != null) alive[i].tutorialHold = hold;
    }

    /// <summary>
    /// 只压暗**背景**（路线场景的景物 + 天空盒），**不动角色与敌人**。
    ///
    /// ⚠️ 教学关可能**没有路线场景**（背景只剩天空盒）→ 之前这里直接 return → **一点都不暗**，
    ///   这就是「局内教学文本没有黑幕」的真正原因。现在改成：**没场景也压天空盒**。
    /// </summary>
    private void SetSceneDim(bool dim)
    {
        if (_sceneDim == null) _sceneDim = GetComponent<TutorialSceneDim>();
        if (_sceneDim == null) _sceneDim = gameObject.AddComponent<TutorialSceneDim>();
        if (_sceneDim == null) return;
        _sceneDim.dimBrightness = sceneDimBrightness;

        // **每次开压暗都重新绑定**：Y 场景表驱动的节点切换会在**同一个场景里重建景物**
        // （scene handle 不变，但旧的渲染器已销毁）→ 绑一次就会变成“压的是上一批已死对象”。
        var presenter = FindRoutePresenter();
        UnityEngine.SceneManagement.Scene routeScene = default;
        bool haveScene = presenter != null && presenter.TryGetActiveScene(out routeScene);
        if (dim)
        {
            // 没路线场景时传 default → TutorialSceneDim 内部会**只绑天空盒**（背景照旧压暗）
            _sceneDim.Rebind(routeScene);
            _sceneDimBound = true;
            if (haveScene)
            {
                _dimBoundScene = routeScene;
                _dimBoundSceneValid = true;
            }
            else
            {
                _dimBoundSceneValid = false;
            }
        }
        _sceneDim.FadeTo(dim);
    }

    /// 找路线表现层。Unity 的 FindObjectOfType 不接受接口类型，所以扫一遍 MonoBehaviour 再按接口筛
    /// （与 SceneFlowManager 找 ISceneTransitionCamera 同一手法）。
    /// </summary>
    private static IRouteMotionPresenter FindRoutePresenter()
    {
        var behaviours = FindObjectsOfType<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            var presenter = behaviours[i] as IRouteMotionPresenter;
            if (presenter != null) return presenter;
        }
        return null;
    }
}
