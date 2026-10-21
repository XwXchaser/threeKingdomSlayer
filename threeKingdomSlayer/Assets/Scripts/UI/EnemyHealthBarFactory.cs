using UnityEngine;

/// <summary>
/// **敌人血条的工厂**：内核（<c>Enemy</c> / <c>SharedHealthGroup</c>）只通过
/// <see cref="IEnemyHealthBarFactory"/> 拿血条视图，**创建 UI 组件的动作在界面层做**。
///
/// 2026-10-22（asmdef 分层第六批）：内核 → 界面最后一条硬引用
/// （<c>Enemy → EnemyHealthBar</c>）的端口化。
/// </summary>
public sealed class EnemyHealthBarFactory : IEnemyHealthBarFactory
{
    public static readonly IEnemyHealthBarFactory Instance = new EnemyHealthBarFactory();

    /// <summary>取这个敌人身上的血条（没有就建一个，行为与改造前一致）。</summary>
    public IEnemyHealthBarView GetOrCreate(GameObject enemyObject)
    {
        if (enemyObject == null) return null;
        var bar = enemyObject.GetComponent<EnemyHealthBar>();
        if (bar == null) bar = enemyObject.AddComponent<EnemyHealthBar>();
        return bar;
    }

    /// <summary>应用启动后自动登记（界面层登记、内核层使用，依赖方向朝下）。</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoRegister()
    {
        CombatPresenters.EnemyHealthBars = Instance;
    }
}
