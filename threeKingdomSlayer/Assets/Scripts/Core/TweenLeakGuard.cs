using System.Collections;
using System.Reflection;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// DOTween 泄漏守卫：定期扫描**活跃 tween**，把「目标对象已经被销毁」的 tween 杀掉。
///
/// 背景（2026-10-29 实测）：
/// 玩家阵亡 → 返回主菜单 → Battle 场景被卸载并重载 → 场景里的 UI / 攻击视觉被销毁，
/// 但仍挂在 DOTween 全局管理器上的 tween 不会自动死（DOTween 不会杀掉目标已销毁的 tween）。
/// 那些 tween 在下一帧继续写 `Transform.localScale` → **MissingReferenceException**。
/// 实测栈：`Sequence.ApplyInternalCycle → Vector3Plugin.EvaluateAndApply → Transform.set_localScale`。
///
/// 规则来源：<c>memory/dotween-destroyed-transform.md</c> ——「任何访问 Transform / SpriteRenderer 的
/// Tween，必须在对象销毁前按创建时同一 target 显式 Kill」。本组件是这条规则的**兜底**：
/// 个别对象漏了清理时（例如对象是运行时新建、且被"回池"成 inactive —— Unity 对
/// 「从未激活过的对象」不调用 OnDestroy），由它兜住，并把来源打进日志便于定位。
///
/// 只杀「目标已销毁」的 tween —— 目标还活着的 tween 一律不动，避免误伤正常动画。
/// 扫描用反射读 DOTween 内部活跃列表；反射失败就静默停用（不报错、不影响游戏）。
///
/// **时序**：清扫必须在 DOTween 求值**之前**跑，否则被销毁目标的 tween 会在本帧先写一次
/// localScale 而抛错。因此本组件用 <see cref="DefaultExecutionOrderAttribute"/> 把 Update 提到
/// 全局最前（DOTween 自己的 Update 是默认序 0）。
/// </summary>
[DefaultExecutionOrder(-1000)]
public sealed class TweenLeakGuard : MonoBehaviour
{
    private static TweenLeakGuard _instance;

    private static FieldInfo _activeTweensField;
    private static FieldInfo _sequencedTweensField;
    private static FieldInfo _totActiveTweensField;
    private static bool _reflectionReady;
    private static bool _reflectionFailed;
    private static bool _warnedOnce;

    /// <summary>自动创建（常驻，跨场景）。场景重载不会重复创建。</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null) return;
        var go = new GameObject("[TweenLeakGuard]");
        go.hideFlags = HideFlags.HideInHierarchy;
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<TweenLeakGuard>();
        // **场景卸载的当下就清**：场景对象此刻已经被销毁，而残留 tween 会在下一帧去写它们的
        // localScale → MissingReferenceException。这里再清一次是双保险。
        UnityEngine.SceneManagement.SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private static void OnSceneUnloaded(UnityEngine.SceneManagement.Scene scene)
    {
        Sweep();
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
        UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void Update()
    {
        Sweep();
    }

    /// <summary>扫一遍活跃 tween，杀掉目标已销毁的那些（并打印来源）。</summary>
    private static void Sweep()
    {
        if (!TryPrepareReflection()) return;

        // 没有活跃 tween 就直接跳过（避免每帧白扫一遍空数组）
        if (_totActiveTweensField != null)
        {
            int active = (int)_totActiveTweensField.GetValue(null);
            if (active <= 0) return;
        }

        var tweens = _activeTweensField.GetValue(null) as Tween[];
        if (tweens == null) return;

        for (int i = 0; i < tweens.Length; i++)
        {
            var tween = tweens[i];
            if (tween == null || !tween.IsActive()) continue;
            if (!HasDestroyedTarget(tween)) continue;

            Debug.LogWarning($"[TweenLeakGuard] 杀掉目标已销毁的 tween：id={tween.id} type={tween.GetType().Name} " +
                              $"target={DescribeTarget(tween)}");
            if (!_warnedOnce)
            {
                _warnedOnce = true;
                Debug.LogWarning("[TweenLeakGuard] 目标已销毁的 tween 已清理（首次出现时会打印来源，方便定位是哪条链路漏了清理）");
            }
            tween.Kill(false);
        }
    }

    /// <summary>打印出问题的目标名，方便定位是哪条链路漏了清理。</summary>
    private static string DescribeTarget(Tween tween)
    {
        object target = tween.target;
        if (target is UnityEngine.Object unityObject && unityObject == null)
            return "(已销毁的 Unity 对象)";

        if (tween is Sequence sequence)
        {
            var nested = _sequencedTweensField.GetValue(sequence) as IList;
            if (nested != null)
            {
                for (int i = 0; i < nested.Count; i++)
                {
                    var inner = nested[i] as Tween;
                    if (inner == null) continue;
                    if (!(inner.target is UnityEngine.Object innerObject) || innerObject != null) continue;
                    string innerName = innerObject != null ? innerObject.name : innerObject.GetType().Name;
                    return $"{innerObject.GetType().Name}({innerName})";
                }
            }
        }

        if (target is UnityEngine.Object alive) return $"{alive.GetType().Name}({alive.name})";
        return target != null ? target.GetType().Name : "null";
    }

    /// <summary>
    /// tween（或序列里嵌套的 tween）的目标是否已被销毁。
    /// 序列自身的 <c>target</c> 常常是 null（没 SetTarget），所以必须看嵌套 tween。
    /// </summary>
    private static bool HasDestroyedTarget(Tween tween)
    {
        if (IsDestroyed(tween.target)) return true;

        if (!(tween is Sequence sequence)) return false;

        var nested = _sequencedTweensField.GetValue(sequence) as IList;
        if (nested == null) return false;

        for (int i = 0; i < nested.Count; i++)
        {
            var inner = nested[i] as Tween;
            if (inner != null && IsDestroyed(inner.target)) return true;
        }
        return false;
    }

    /// <summary>
    /// Unity 的「假 null」：对象被 Destroy 后引用还在，但 <c>== null</c> 为 true。
    /// 纯托管 null（没设 target）不算「已销毁」。
    /// </summary>
    private static bool IsDestroyed(object target)
    {
        if (target == null) return false;
        if (target is UnityEngine.Object unityObject) return unityObject == null;
        return false;
    }

    private static bool TryPrepareReflection()
    {
        if (_reflectionReady) return true;
        if (_reflectionFailed) return false;

        try
        {
            var managerType = typeof(DOTween).Assembly.GetType("DG.Tweening.Core.TweenManager");
            _activeTweensField = managerType?.GetField("_activeTweens", BindingFlags.NonPublic | BindingFlags.Static);
            _sequencedTweensField = typeof(Sequence).GetField("sequencedTweens", BindingFlags.NonPublic | BindingFlags.Instance);
            _totActiveTweensField = managerType?.GetField("totActiveTweens", BindingFlags.NonPublic | BindingFlags.Static);

            if (_activeTweensField == null || _sequencedTweensField == null)
            {
                _reflectionFailed = true;
                return false;
            }
            _reflectionReady = true;
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[TweenLeakGuard] 反射初始化失败，守卫停用：{e.Message}");
            _reflectionFailed = true;
            return false;
        }
    }
}
