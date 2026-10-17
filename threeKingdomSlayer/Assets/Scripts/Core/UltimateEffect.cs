using UnityEngine;

/// <summary>
/// 大招效果抽象基类
/// 子类实现具体效果（伤害、Buff、特效等）
/// </summary>
public abstract class UltimateEffect : MonoBehaviour
{
    /// <summary>
    /// 执行大招效果
    /// </summary>
    public abstract void Execute();

    /// <summary>
    /// 取消大招效果（路线旅行、奖励等待、节点失败等非战斗阶段调用）。
    /// 默认无操作，子类可停止协程、恢复玩家/HUD 状态。
    /// </summary>
    public virtual void Cancel() { }

    /// <summary>
    /// 返回效果持续时间（秒），用于控制特效预制体销毁时机
    /// 默认 2 秒
    /// </summary>
    public virtual float GetLifetime() => 2f;
}
