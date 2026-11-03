using UnityEngine;

/// <summary>
/// **秘籍掉落系统**（2026-11-03 C2）。
///
/// 秘籍 = 局外**永久**解锁物（技巧类）：单次获得即永久生效，**不走 ItemInventory**
/// （那是局内道具栏：有槽位上限、满了弹弃置窗、局末清空）。
///
/// 复用的现成通道（不新建系统）：
///   • 可见掉落物 → <see cref="ExpGemManager"/> 的宝石 prefab（与 <c>HealthPotionManager</c> 同一套路）；
///   • 存档 → <see cref="SaveManager"/> + <see cref="ManualSaveSection"/>（与 TalentSaveSection 同构）。
///
/// 静态类（不需要挂组件、不需要改场景）：prefab 与父容器都从 <see cref="ExpGemManager"/> 取。
/// </summary>
public static class ManualDropSystem
{
    /// <summary>掉落物飞行速度（屏幕像素/秒，与血包一致）。</summary>
    private const float DefaultFlySpeed = 1200f;

    /// <summary>是否已经获得该秘籍（存档判定）。</summary>
    public static bool IsUnlocked(string manualId) => ManualSaveSection.IsUnlocked(SaveManager.Load(), manualId);

    /// <summary>写存档：永久解锁（幂等；已获得返回 false）。</summary>
    public static bool Grant(ManualDefinition manual)
    {
        if (manual == null) return false;
        string id = manual.ManualId;
        if (string.IsNullOrEmpty(id)) return false;

        var data = SaveManager.Load();
        if (ManualSaveSection.IsUnlocked(data, id)) return false;

        ManualSaveSection.Unlock(data, id);
        SaveManager.Save(data);
        Debug.Log($"[ManualDropSystem] 获得秘籍：{manual.displayName}（id={id}）");
        return true;
    }

    /// <summary>
    /// 生成可见的秘籍掉落物：生成 → 飞向玩家 → 到达即解锁。
    /// 没有宝石 prefab / 父容器时**直接解锁**（没有表现也不能丢内容）。
    /// </summary>
    public static void SpawnDrop(ManualDefinition manual, Vector3 worldPos)
    {
        if (manual == null) return;
        // 已经拿到过就不掉了（避免重复表现与重复发放）
        if (IsUnlocked(manual.ManualId)) return;

        var gemManager = ExpGemManager.Instance;
        if (gemManager == null || gemManager.gemPrefab == null || gemManager.gemParent == null)
        {
            Grant(manual);
            return;
        }

        var go = Object.Instantiate(gemManager.gemPrefab, gemManager.gemParent);
        go.transform.SetAsLastSibling();

        var gem = go.GetComponent<ExpGem>();
        if (gem == null) gem = go.AddComponent<ExpGem>();
        if (manual.icon != null) gem.SetVisual(manual.icon, Color.white);

        var rt = go.GetComponent<RectTransform>();
        if (rt != null && Camera.main != null) rt.position = Camera.main.WorldToScreenPoint(worldPos);

        var img = go.GetComponent<UnityEngine.UI.Image>();
        if (img != null) img.raycastTarget = false;

        gem.expAmount = 0f;   // 不使用经验值
        gem.speed = DefaultFlySpeed;
        gem.targetPosition = GetPlayerScreenPosition(rt);
        gem.onArrived = g =>
        {
            if (g != null) Object.Destroy(g.gameObject);
            Grant(manual);
        };
    }

    /// <summary>掉落物的飞行目标：玩家所在位置（屏幕坐标）。</summary>
    private static Vector3 GetPlayerScreenPosition(RectTransform fallback)
    {
        var ps = PlayerState.Instance;
        if (ps != null && Camera.main != null)
            return Camera.main.WorldToScreenPoint(ps.transform.position);
        return fallback != null ? fallback.position + Vector3.up * 200f : Vector3.zero;
    }
}
