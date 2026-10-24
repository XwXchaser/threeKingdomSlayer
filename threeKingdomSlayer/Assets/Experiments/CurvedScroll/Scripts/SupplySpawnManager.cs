using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 局外补给生成管理器（就绪态 / 落地页 / 主菜单）。
///
/// 职责：
///   • 按「生成时刻表」<see cref="ISupplySpawnSchedule"/> 核对哪些固定点位上**应当**有补给物件；
///   • 该有的就生成、不该有的就销毁（离开就绪态、轮次到期、已被领过）；
///   • 玩家重新回到该页面时，下一次核对就等价于「按服务器倒计时重新查询并刷新」。
///
/// 点位是**固定**的（不随机）：避免和场景里已有的摆件撞在一起、也不挡路中线。
/// 时刻表默认是本地模拟（<see cref="LocalSupplySpawnSchedule"/>），
/// 接真后端时只要换掉时刻表实现，玩法代码不用动。
/// </summary>
[DisallowMultipleComponent]
public sealed class SupplySpawnManager : MonoBehaviour
{
    [Tooltip("局外生成计划（固定点位 + 轮次周期）")]
    public SupplySpawnPlan plan;

    [Tooltip("核对间隔（秒）：多久按倒计时表核对一次")]
    public float refreshInterval = 1f;

    [Tooltip("路线根（留空自动找 YScrollSample；路线场景是 additive 加载的，可能晚于本组件 Awake）")]
    public YScrollSample route;

    [Tooltip("打印核对日志")]
    public bool logRefresh;

    private ISupplySpawnSchedule _schedule;
    private float _refreshTimer;
    private readonly List<BreakableProp> _spawned = new List<BreakableProp>();

    private void Awake()
    {
        if (route == null) route = FindObjectOfType<YScrollSample>();
        // 本地模拟：用墙钟当“服务器时间”，这样跨启动、跨会话的轮次号是连续的
        // （用 Time.realtimeSinceStartup 的话每次启动都从第 0 轮开始，实例 id 会撞车）。
        _schedule = new LocalSupplySpawnSchedule(plan, () => DateTimeOffset.UtcNow.ToUnixTimeSeconds());
    }

    private void Update()
    {
        _refreshTimer -= Time.unscaledDeltaTime;
        if (_refreshTimer > 0f) return;
        _refreshTimer = Mathf.Max(0.2f, refreshInterval);
        Refresh();
    }

    /// <summary>按时刻表核对：应存在的生成 / 不该存在的销毁。</summary>
    public void Refresh()
    {
        if (_schedule == null || plan == null) return;
        if (route == null) route = FindObjectOfType<YScrollSample>();
        if (route == null) return;

        bool hub = BreakableProp.HubState;
        var slots = hub ? _schedule.GetActiveSlots() : null;

        for (int i = _spawned.Count - 1; i >= 0; i--)
        {
            var prop = _spawned[i];
            if (prop == null) { _spawned.RemoveAt(i); continue; }
            bool keep = hub && slots != null && ContainsSlot(slots, prop.PropId);
            if (keep) continue;
            prop.DespawnWithFade();
            _spawned.RemoveAt(i);
        }

        if (slots == null) return;
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (HasInstance(slot.instanceId)) continue;
            if (SaveManager.IsSupplyClaimed(slot.instanceId)) continue; // 同一个实例不重复发
            Spawn(slot);
        }
    }

    private static bool ContainsSlot(IReadOnlyList<SupplySpawnSlot> slots, string instanceId)
    {
        for (int i = 0; i < slots.Count; i++)
            if (slots[i].instanceId == instanceId) return true;
        return false;
    }

    private bool HasInstance(string instanceId)
    {
        for (int i = 0; i < _spawned.Count; i++)
            if (_spawned[i] != null && _spawned[i].PropId == instanceId) return true;
        return false;
    }

    private void Spawn(SupplySpawnSlot slot)
    {
        var point = slot.point;
        if (point == null || point.propPrefab == null) return;

        var root = route.transform;
        Vector3 world = root.TransformPoint(point.localPosition);
        var go = Instantiate(point.propPrefab, world, Quaternion.identity, root);
        var prop = go.GetComponent<BreakableProp>();
        if (prop == null)
        {
            Debug.LogWarning("[Supply] 预制体上没有 BreakableProp：" + point.propPrefab.name);
            Destroy(go);
            return;
        }

        prop.ConfigureOutOfRun(slot.instanceId, point.coinReward);

        var follower = go.GetComponent<RoutePropFollower>();
        if (follower != null)
        {
            follower.route = route;
            follower.localAnchor = point.localPosition;
        }

        _spawned.Add(prop);
        if (logRefresh) Debug.Log($"[Supply] 生成 {slot.instanceId} @ {point.localPosition}");
    }
}
