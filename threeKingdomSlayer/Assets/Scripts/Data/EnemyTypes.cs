using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 敌人的**共享类型**：状态枚举（EnemyState / EnemyFormState / BossState）、移动订单（RushMove）枚举与命中帧数据结构。
///
/// 2026-10-22（asmdef 分层第一批）：从 <c>Assets/Scripts/Enemy/Enemy.Types.cs</c> 移到数据层
/// —— 这些是**纯数据**（枚举 / 结构体），敌人、玩家、UI、流程都要用；
/// 留在 Enemy 目录会把「数据」绑死在「敌人程序集」上，下层无法引用。
/// </summary>

public enum EnemyState
{
    Idle,
    Moving,
    Attacking,
    Stunned,
    Launched,
    Dead,
    QTEAttacking,  // BOSS QTE 攻击演出中
    GettingUp,
    Frozen,        // 冻结：暂停而非取消一切行动
    IceBreaking    // 破冰动画播放中：位置静止、不可再冻，动画结束才解除冻结
}

public enum EnemyFormState
{
    Normal,
    /// <summary>盾兵破盾表现期：无盾、减伤已失效、增伤未生效（见 design/enemy-1021-shield-guard.md）</summary>
    ShieldBroken,
    Coward
}

public enum RushMoveStartResult
{
    Started,
    AlreadyRushing,
    Deferred,
    Rejected
}

public enum RushMoveMode
{
    Step,
    ContinuousEmptyRun
}

public enum RushMoveOrderOwner
{
    None,
    SpawnEntry,
    WaveMarch,
    PushReturn,
    Boss
}

/// <summary>
/// Boss 推进状态枚举
/// </summary>
public enum BossState
{
    None,        // 非Boss或未进入分阶段推进
    Approaching, // Boss暂停在第3排(rowIndex=2)，等待前两排清空
    InCombat     // Boss已到达应战排(rowIndex=1)，进入战斗
}

/// <summary>
/// 攻击命中帧声明：出伤（近战）/ 击发（远程）必须与这一帧在屏幕上的时刻一致。
/// 运行时按 spriteName 匹配「屏幕上真实显示的精灵」，因此对动画重定时、卡肉、变速都成立；
/// keyIndex 只在编辑器校验器里用于校验/回填（0 基，attack8 = 8）。
/// </summary>
[System.Serializable]
public struct AttackHitFrame
{
    [Tooltip("命中帧的精灵名（如 Enemy_1011_attack8）")]
    public string spriteName;
    [Tooltip("命中帧在 clip 关键帧中的序号（0 基；仅供编辑器校验/回填）")]
    public int keyIndex;
    [Tooltip("该命中帧是否结算一次伤害/发射（多段连击可配多条；false = 只作对齐标记）")]
    public bool resolve;
    [Tooltip("该命中帧在攻击 clip 中的时间（秒）；由编辑器校验器自动回填")]
    public float hitTime;
}

/// <summary>
/// 攻击序列中的一步，定义该步的攻击类型和参数
/// </summary>
[System.Serializable]
public struct AttackStep
{
    [Tooltip("是否C技（霸体窗口 + 红色描边）")]
    public bool isCAttack;
    [Tooltip("动画Trigger名（空=默认Attack）")]
    public string animationTrigger;
    [Tooltip("兼容字段：未声明命中帧时作为前冲/出伤时长；已声明命中帧时由首个 resolve 命中帧时间替代")]
    public float spawnDuration;
    [Tooltip("收招时长（秒）：声明命中帧时自动改为「clip 剩余时间」，此值仅在未声明命中帧时生效")]
    public float drawDuration;
    [Tooltip("该步攻击后的额外冷却（秒）")]
    public float extraCooldown;
    [Tooltip("攻击时是否左右翻转")]
    public bool useFlip;
    [Tooltip("命中帧列表（空 = 沿用旧的 spawnDuration 出伤；非空时由 hitFrames 驱动时序）")]
    public List<AttackHitFrame> hitFrames;
}
