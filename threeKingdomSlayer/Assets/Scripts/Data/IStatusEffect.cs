using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// **状态效果**（通用扩展点）。
///
/// 状态本体是独立类，**不绑定宿主类型**：同一个容器既能装在敌人身上，也能装在玩家身上
/// （玩家的反伤护盾、冰雾预备就是玩家侧状态），所以「状态是什么」与「状态挂在谁身上」是两件事。
///
/// 约定：
///   • 实例**自己持有状态**与结算逻辑；
///   • <see cref="Tick"/> 只放「每帧都要跑」的部分，依赖宿主状态机的部分由宿主自己调用；
///   • <see cref="Clear"/> 用于宿主回收 / 状态结束时清干净。
///
/// 已有实现：<c>EnemyFreeze</c>（敌人的冻结）、<c>EnemyStatusBurn</c> / <c>EnemyStatusDisease</c>
/// （敌人的灼烧 / 疾病）、<c>ReflectShieldStatus</c>（玩家的反伤护盾）、
/// <c>IceChargeStatus</c>（玩家的破冰预备）。
///
/// 2026-10-22（asmdef 分层第三批）：从 <c>Assets/Scripts/Status/IStatusEffect.cs</c> 下沉到契约层
/// —— 这是**状态机制的扩展点**，任何层都要能实现它（新状态类住在哪一层都行）。
/// </summary>
public interface IStatusEffect
{
    /// <summary>状态 id（如 frozen / reflect_shield / ice_charge）。</summary>
    string StatusId { get; }

    /// <summary>当前是否处于该状态。</summary>
    bool IsActive { get; }

    /// <summary>进度（0~1，进度条用）。</summary>
    float GetProgress();

    /// <summary>层数（无层数概念的返回 0）。</summary>
    int GetStacks();

    /// <summary>同屏多个状态时的优先级（大的排前面）。</summary>
    int GetPriority();

    /// <summary>每帧结算（由容器调用）。</summary>
    void Tick(float deltaTime);

    /// <summary>清空（宿主回收 / 状态结束时调用）。</summary>
    void Clear();
}
