using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 规则与表现 —— 教学关导演的**规则与表现**部分 ✅（由 TutorialStageDirector.cs 拆出 ✅ ✅）
/// </summary>
public partial class TutorialStageDirector : MonoBehaviour
{
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

        if (!_sceneDimBound && dim)
        {
            // 只认路线表现层端口（IRouteMotionPresenter），不认具体的 Y 分叉实验类 —— 依赖方向朝下。
            var presenter = FindRoutePresenter();
            if (presenter == null || !presenter.TryGetActiveScene(out var routeScene))
            {
                Debug.Log("[Tutorial] 场景压暗：路线场景未就绪，跳过压暗");
                return;
            }
            _sceneDim.Bind(routeScene);
            _sceneDimBound = true;
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
