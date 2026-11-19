using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// **装备宝箱**（2026-11-17）：精英 / Boss 死亡 → 地上掉一个宝箱 → 飞向玩家 → 拾取后给一次装备三选一。
///
/// 与经验宝石（<see cref="ExpGem"/>）同一套做法：**屏幕空间 UI Image**，
/// 生成在敌人的屏幕位置，飞向收集点（玩家），到达即算“拾取”。
/// </summary>
public class EquipmentChest : MonoBehaviour
{
    /// <summary>飞行目标（屏幕坐标，生成时锁定）。</summary>
    [System.NonSerialized] public Vector3 targetPosition;

    /// <summary>飞行速度（屏幕像素/秒）。</summary>
    [System.NonSerialized] public float speed = 900f;

    /// <summary>到达回调（可覆盖默认行为）。</summary>
    [System.NonSerialized] public System.Action<EquipmentChest> onArrived;

    private RectTransform _rectTransform;
    private bool _collected;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (_collected) return;
        if (_rectTransform == null) { Destroy(gameObject); return; }

        Vector3 dir = targetPosition - _rectTransform.position;
        float step = speed * Time.deltaTime;
        if (dir.magnitude <= step)
        {
            _collected = true;
            if (onArrived != null) onArrived(this);
            else EquipmentChestManager.Instance?.OnChestArrived(this);
            Destroy(gameObject);
            return;
        }

        _rectTransform.position += dir.normalized * step;
    }
}
