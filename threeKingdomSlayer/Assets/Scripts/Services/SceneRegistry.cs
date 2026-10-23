using System.Collections.Generic;

/// <summary>
/// 场景实例注册表：替代「FindObjectsOfType 扫全部」的遍历。
///
/// 用法：
///   - 组件在 OnEnable 里 <see cref="Register"/>，在 OnDisable / OnDestroy 里 <see cref="Unregister"/>。
///   - 需要「处理场景里所有激活实例」时，遍历 <see cref="Snapshot"/> 返回的副本。
///
/// 语义与 FindObjectsOfType&lt;T&gt;() 对齐：只包含激活（activeInHierarchy）的实例。
/// 未激活对象不在注册表里；需要包含未激活对象时请用 Resources / 显式列表，不要用本注册表。
///
/// 为什么快照：遍历过程中处理对象可能触发 Unregister（如 Destroy / 回池），
/// 直接遍历原列表会失效，因此遍历副本。
/// </summary>
public static class SceneRegistry<T> where T : class
{
    private static readonly List<T> _items = new List<T>();

    /// <summary>当前登记的数量（只读，不含已注销项）。</summary>
    public static int Count => _items.Count;

    /// <summary>登记一个实例；重复登记无副作用。</summary>
    public static void Register(T item)
    {
        if (item == null || _items.Contains(item)) return;
        _items.Add(item);
    }

    /// <summary>注销一个实例；未登记过的实例无副作用。</summary>
    public static void Unregister(T item)
    {
        if (item == null) return;
        _items.Remove(item);
    }

    /// <summary>返回当前所有登记实例的副本，遍历期间可安全地注销/销毁。</summary>
    public static T[] Snapshot() => _items.ToArray();
}
