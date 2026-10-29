using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 规则与表现 —— 教学关导演的**规则与表现**部分 ✅（由 TutorialStageDirector.cs 拆出 ✅ ✅）
/// </summary>
public partial class TutorialStageDirector : MonoBehaviour
{
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

    /// <summary>手势种类 → 它教的招式。</summary>
    private static AttackType? AttackTypeFor(GestureHintKind kind)
    {
        switch (kind)
        {
            case GestureHintKind.Tap: return AttackType.Stab;
            case GestureHintKind.Swipe: return AttackType.Slash;
            case GestureHintKind.ChargedSwipe: return AttackType.Launch;
            case GestureHintKind.Parry:
            case GestureHintKind.ParryHold: return AttackType.Parry;
            default: return null;
        }
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
            // N1 给一次升级，固定冰雾
            choices.suppressChoices = false;
            choices.forcedChoices = tutorialIceSkill != null
                ? new List<UpgradeDefinition> { tutorialIceSkill }
                : new List<UpgradeDefinition>();
        }
        else
        {
            // E0 是破盾与冲锋教学，不放三选一，避免随机技能盖掉冰雾
            choices.forcedChoices = new List<UpgradeDefinition>();
            choices.suppressChoices = true;
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

    /// <summary>只压暗路线场景（背景/地形/植被），不动角色与敌人。</summary>
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
            if (!haveScene)
            {
                Debug.Log("[Tutorial] 场景压暗：路线场景未就绪，跳过压暗");
                return;
            }
            _sceneDim.Bind(routeScene);
            _sceneDimBound = true;
            _dimBoundScene = routeScene;
            _dimBoundSceneValid = true;
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
