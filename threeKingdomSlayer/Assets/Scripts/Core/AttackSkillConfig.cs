using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 攻击技能配置 - ScriptableObject
/// 每个技能配置是一个独立的 .asset 文件，包含伤害、范围、冷却等所有参数。
/// 策划可在 Inspector 中拖拽不同的技能资产来装配武将的攻击组合。
/// </summary>
[CreateAssetMenu(fileName = "AttackSkillConfig", menuName = "一夫当关/攻击技能配置")]
public class AttackSkillConfig : ScriptableObject
{
    [Header("基本信息")]
    [Tooltip("技能编号，唯一标识")]
    public int id;
    public AttackType attackType;
    public DamageType damageType = DamageType.Sweep;

    [Header("伤害")]
    [Tooltip("基础伤害")]
    public float damage = 30f;
    [Tooltip("架势伤害（Launch/Parry 使用）")]
    public float poiseDamage = 0f;

    [Header("骑兵冲锋克制 / 重攻击声明")]
    [Tooltip("勾选后，本次攻击命中可以打断骑兵敌人（109）的冲锋。把某种攻击视为“蓄力攻击 / 重攻击”时在这里声明即可，不需要改代码；像斩击这类普攻与蓄力共用的动作由输入分支（isCharged）补充判定。")]
    public bool interruptsCavalryCharge;

    [Header("范围与冷却")]
    [Tooltip("影响排数")]
    public int rangeRows = 1;
    [Tooltip("冷却时间（秒）— 旧模式：独立技能CD")]
    public float cooldown = 0.5f;
    [Tooltip("攻击动作时长（秒）— 新模式：动作锁定。攻速会缩放此值。应与该攻击的动画/特效总时长匹配，让玩家感知到'因为还在挥刀所以不能做其他动作'")]
    public float actionDuration = 0.3f;

    [Header("挑飞特殊参数")]
    [Tooltip("挑飞持续时间（秒），仅 Launch 有效")]
    public float launchDuration = 2f;

    [Header("特效")]
    [Tooltip("攻击波预制体（可为空，使用默认 Quad）")]
    public GameObject attackWavePrefab;

    [Header("戳击偏移")]
    [Tooltip("生成位置 Y 偏移（相对敌人中心）")]
    public float stabSpawnYOffset = 1.5f;
    [Tooltip("生成位置 Z 偏移（相对敌人中心）")]
    public float stabSpawnZOffset = 0.5f;
    [Tooltip("仅戳刺视觉沿攻击方向额外前伸的世界距离，不影响命中、伤害或射程")]
    public float stabVisualReachOffset = 0.5f;
    [Tooltip("Stab 视觉终点在面向相机的圆盘内随机偏移的基础半径；会随视觉射程缓慢缩放，不影响命中、伤害或射程")]
    [Min(0f)]
    public float stabVisualTargetRandomRadius = 0.12f;

    [Header("斩击扇形扫掠")]
    [Tooltip("扫掠半宽（X 轴范围，默认 5）")]
    public float slashSweepHalfWidth = 5f;
    [Tooltip("扇形旋转角度（度，默认 50）")]
    public float slashSweepAngle = 50f;
    [Tooltip("扫掠持续时长（秒，默认 0.25）")]
    public float slashSweepDuration = 0.25f;
    [Tooltip("扫掠方向：跟随手势（默认）/ 强制右到左 / 强制左到右。连段终结技用强制右到左表达「朝左上」")]
    public SlashSweepDirection slashSweepDirection = SlashSweepDirection.FromGesture;
    [Tooltip("扫掠路径斜度（度）：让扫掠线左高右低（配合强制右到左即「朝左上」）；0 = 水平")]
    [Range(-45f, 45f)]
    public float slashMovementTiltDegrees = 0f;
    [Tooltip("覆盖手势带来的画面内倾斜角（连段终结技用固定值，与手势无关）")]
    public bool slashOverrideVisualTilt = false;
    [Range(-30f, 30f)]
    public float slashVisualTiltDegrees = 0f;
    [Tooltip("生成位置 Y 偏移（相对敌人中心）")]
    public float slashSpawnYOffset = 1.5f;
    [Tooltip("生成位置 Z 偏移（相对敌人中心）")]
    public float slashSpawnZOffset = 0.5f;

    [Header("招架扫掠")]
    [Tooltip("Z 轴旋转幅度（度，默认 100）。枪尾从起始角扫到起始角+此值")]
    [Range(30f, 180f)]
    public float parrySweepAngle = 100f;
    [Tooltip("扫掠持续时长（秒，默认 0.25）")]
    [Range(0.1f, 0.5f)]
    public float parrySweepDuration = 0.25f;
    [Tooltip("生成位置 X 偏移（相对玩家位置）")]
    public float parrySpawnXOffset = 0f;
    [Tooltip("生成位置 Y 偏移（相对玩家位置）")]
    public float parrySpawnYOffset = 1.5f;
    [Tooltip("生成位置 Z 偏移（相对玩家位置）")]
    public float parrySpawnZOffset = 0f;
    [Tooltip("每次招架 Z 起始角随机偏移范围（度，默认 15）")]
    [Range(0f, 30f)]
    public float parryAngleVariance = 15f;

    [Header("挑飞上挑")]
    [Tooltip("Z 轴旋转幅度（度，默认 90）。枪头从低到高上挑")]
    [Range(30f, 180f)]
    public float launchFlickAngle = 90f;
    [Tooltip("上挑持续时长（秒，默认 0.20）")]
    [Range(0.1f, 0.5f)]
    public float launchFlickDuration = 0.20f;
    [Tooltip("上挑前下压蓄势时长；不改变攻击判定，仅影响视觉分段。")]
    [Range(0.03f, 0.25f)]
    public float launchWindupDuration = 0.10f;
    [Tooltip("上挑前向下蓄势的视觉距离。")]
    [Min(0f)]
    public float launchWindupDistance = 0.42f;
    [Tooltip("根据蓄力位置相对玩家左右偏移追加的上挑侧倾角。")]
    [Range(0f, 30f)]
    public float launchSideTilt = 12f;
    [Tooltip("生成位置 X 偏移（相对玩家位置）")]
    public float launchSpawnXOffset = 0f;
    [Tooltip("生成位置 Y 偏移（相对玩家位置）")]
    public float launchSpawnYOffset = 1.5f;
    [Tooltip("生成位置 Z 偏移（相对玩家位置）")]
    public float launchSpawnZOffset = 0f;
    [Tooltip("每次上挑 Z 起始角随机偏移范围（度，默认 15）")]
    [Range(0f, 30f)]
    public float launchAngleVariance = 15f;
    [Tooltip("上挑时世界 Y 轴上升高度（默认 1.0）")]
    public float launchRiseHeight = 1.0f;
    [Tooltip("挑飞起手跳过预备：直接从当前枪体姿态上挑（连段终结技用，预备已由蓄力拉回完成）")]
    public bool launchSkipWindup = false;
    [Header("挑飞・左上扫击表现（保留击飞效果，只改动作语言）")]
    [Tooltip("开启后挑飞不再是「下压→上挑」，而是从当前握持姿态一记大幅朝左上的扫击；伤害/击飞仍由挑飞结算")]
    public bool launchSweepMode = false;
    [Tooltip("扫击终点枪尖方向：屏幕向上的分量")]
    public float launchSweepUp = 1f;
    [Tooltip("扫击终点枪尖方向：屏幕向左的分量")]
    public float launchSweepLeft = 0.7f;
    [Tooltip("扫击终点枪尖方向：镜头纵深分量")]
    public float launchSweepForward = 0.35f;
    [Tooltip("扫击终点位置：向上偏移（世界单位）")]
    public float launchSweepRise = 1f;
    [Tooltip("扫击终点位置：向左偏移（世界单位）")]
    public float launchSweepLeftShift = 1.8f;

    [Header("大招")]
    [Tooltip("命中时获得能量（非大招技能有效）")]
    public int ultimateEnergyGain = 10;

    [Header("蓄力等级伤害倍率")]
    [Tooltip("按蓄力等级缩放伤害，index 0 = 一级。留空表示蓄力等级不改变数值")]
    public List<float> chargeLevelDamageMultipliers = new List<float>();

    [Header("戳击动作（角度与力度）")]
    [Tooltip("起手占整段的比例。越大越有蓄势感。默认 0.12")]
    public float stabWindupRatio = 0.12f;
    [Tooltip("刺出占整段的比例。越小越急。默认 0.28")]
    public float stabThrustRatio = 0.28f;
    [Tooltip("穿入占整段的比例。默认 0.08")]
    public float stabPenetrationRatio = 0.08f;
    [Tooltip("起手末端停顿（秒），用于做出「蓄势一拍」。默认 0")]
    public float stabWindupHoldSeconds = 0f;
    [Header("刺入轨迹（枪尾自由，只要求枪尖命中本列）")]
    [Tooltip("枪尾相对基准起点的横向偏移（世界单位，正=右）。用于制造斜向刺入")]
    public float stabTailOffsetRight = 0f;
    [Tooltip("枪尾相对基准起点的纵向偏移（世界单位，正=上）。正值会形成从上往下刺入（枪尾在上）")]
    public float stabTailOffsetUp = 0f;
    [Tooltip("枪尾相对基准起点的前后偏移（世界单位，正=更靠近目标）。负值更靠后，冲刺距离更长")]
    public float stabTailOffsetForward = 0f;
    [Range(-20f, 20f)]
    [Tooltip("画面内姿态倾角（度，绕射线轴）。只影响姿态、不改变轨迹；建议不超过 10")]
    public float stabVisualTiltDegrees = 0f;
    [Range(-20f, 20f)]
    [Tooltip("绕枪身长轴的自转（度）：刺出段由 0 渐变到该值，回收段转回 0。建议 3~8，过大在 2D 精灵上会变薄")]
    public float stabRollDegrees = 0f;
    [Range(0f, 1f)]
    [Tooltip("高速帧在「刺出段」内的起点比例。默认 0.1（对齐第一击的处理）")]
    public float stabSpeedFrameStart01 = 0.1f;
    [Range(0f, 1f)]
    [Tooltip("高速帧在「刺出段」内的终点比例。默认 1.0（对齐第一击的处理）")]
    public float stabSpeedFrameEnd01 = 1.0f;
    [Tooltip("刺出开始时的模糊强度。默认 28（对齐第一击的处理）")]
    public float stabBlurThrust = 28f;
    [Tooltip("高速帧显示期间的模糊强度。默认 14（对齐第一击的处理）")]
    public float stabBlurSpeedFrame = 14f;
    [Tooltip("穿入阶段的模糊强度。默认 18（对齐第一击的处理）")]
    public float stabBlurPenetration = 18f;
    [Tooltip("刺出时的枪身长度倍率（力度）。默认 1.18")]
    public float stabThrustLengthScale = 1.18f;
    [Tooltip("刺出时的枪身宽度倍率。默认 0.86")]
    public float stabThrustWidthScale = 0.86f;
    [Tooltip("运动模糊强度倍率（力度）。默认 1")]
    public float stabMotionBlurScale = 1f;
    [Tooltip("首次命中的反馈强度（卡肉分级）")]
    public HitFeedbackStrength stabFirstHitStrength = HitFeedbackStrength.Standard;

    [Header("连段蓄力保持")]
    [Tooltip("按住蓄力时枪体被拉回到回收段的什么位置（0 = 不收回，1 = 完全收回）。拉回的位移本身就是蓄力条")]
    [Range(0f, 1f)]
    public float chargeHoldRetractRatio = 0.75f;
    [Tooltip("从按下到拉满蓄势位所需的按住时长（秒），应与一级蓄力门槛一致")]
    [Min(0.05f)]
    public float chargeHoldPullSeconds = 0.3f;
    [Tooltip("拉回时枪身额外抬起的仰角（度）：枪尖向上预压，为挑飞蓄势")]
    public float chargeHoldPitchDegrees = 8f;
    [Tooltip("拉满蓄势位后，先继续向后一顿的时长（秒）：表达「开始蓄力」，之后才开始微颤")]
    [Min(0f)]
    public float chargeHoldSettleSeconds = 0.12f;
    [Tooltip("向后一顿的额外回收距离比例（相对整段回收距离）")]
    [Range(0f, 1f)]
    public float chargeHoldSettleRatio = 0.1f;
    [Tooltip("蓄势位的微颤幅度（世界单位），沿枪身长轴前后抖动")]
    [Min(0f)]
    public float chargeHoldShakeAmplitude = 0.06f;
    [Tooltip("微颤频率（Hz）")]
    [Min(1f)]
    public float chargeHoldShakeFrequency = 14f;

    [Header("蓄力指向（做前置的节点用）")]
    [Tooltip("蓄力保持期间枪身朝目标列的偏摆上限（度）。0 = 不偏摆。按几何需要填：相邻列约 11~22°、跨两列约 44°，建议 ≥ 45 才不会截断")]
    [Range(-90f, 90f)]
    public float chargeHoldYawDegrees = 0f;
    [Tooltip("指向偏摆的平滑时间（秒）：越大越顺滑、越小越跟手。0 = 不做平滑（直接切换）")]
    [Min(0f)]
    public float chargeHoldYawSmoothSeconds = 0.12f;

    [Header("释放指向（C2 用）")]
    [Tooltip("刺出段起点的多少比例内把蓄力偏摆归零并过冲。0 = 不处理。写在【释放招式】上")]
    [Range(0f, 1f)]
    public float stabRedirectSnapRatio = 0f;
    [Tooltip("归零后的过冲角（度），负值 = 朝指向反方向过冲一点")]
    [Range(-10f, 0f)]
    public float stabRedirectOvershootDegrees = -3f;

    [Header("命中震动（C2 用）")]
    [Tooltip("命中瞬间枪体沿枪轴的回弹幅度（世界单位）。0 = 不震动")]
    [Min(0f)]
    public float hitShakeAmplitude = 0f;
    [Tooltip("命中瞬间垂直于枪轴的抖动幅度（世界单位）")]
    [Min(0f)]
    public float hitShakeLateral = 0f;
    [Tooltip("命中瞬间绕枪身长轴的滚转幅度（度）")]
    [Min(0f)]
    public float hitShakeRollDegrees = 0f;
    [Tooltip("命中瞬间枪尖的俯仰点头幅度（度）")]
    [Min(0f)]
    public float hitShakePitchDegrees = 0f;
    [Tooltip("命中震动总时长（秒）。由独立计时驱动，不挂进会被卡肉暂停的主序列")]
    [Min(0f)]
    public float hitShakeDuration = 0.1f;
    [Tooltip("命中震动频率（Hz）")]
    [Min(1f)]
    public float hitShakeFrequency = 18f;
    [Tooltip("第二排命中的震动倍率（第 1 排 = 1）")]
    [Range(0f, 1f)]
    public float hitShakeSecondRowScale = 0.6f;

    [Header("命中位移")]
    [Tooltip("本次攻击每命中一个目标时施加的击退排数（0 = 不击退）。走既有 ApplyPushWave / PostDisplacementFillUp 通道")]
    [Min(0)]
    public int pushBackRows = 0;

    [Header("接续（招式图，一层结构）")]
    [Tooltip("允许用进入本招式的手势重复本招式（Stab→Stab→Stab）。关闭即为串尾终止")]
    public bool repeatSelf = true;
    [Tooltip("覆盖本招式的接续窗口。关闭时按攻击类型派生默认窗口")]
    public bool overrideWindow = false;
    [Range(0f, 1f)] public float windowStart01 = 0f;
    [Range(0f, 1f)] public float windowEnd01 = 1f;
    [Tooltip("后继输入。留空则该招式为串尾，无法再被取消接续")]
    public List<AttackMoveEdge> moveEdges = new List<AttackMoveEdge>();

    /// <summary>是否存在任何后继（显式边或默认重复）：「按住时本段先不结束」的宽限据此判断</summary>
    public bool HasAnyContinuation()
    {
        if (moveEdges != null)
        {
            for (int i = 0; i < moveEdges.Count; i++)
                if (moveEdges[i] != null && moveEdges[i].next != null) return true;
        }
        return repeatSelf;
    }

    /// <summary>是否存在要求蓄力的后继边：连段中按住时是否让枪体停住等蓄力据此判断</summary>
    public bool HasChargeContinuation()
    {
        if (moveEdges == null) return false;
        for (int i = 0; i < moveEdges.Count; i++)
            if (moveEdges[i] != null && moveEdges[i].next != null && moveEdges[i].minChargeLevel >= 1) return true;
        return false;
    }
}

/// <summary>
/// 横扫方向来源：手势方向（普通斩击）或招式资产强制指定（连段终结技）。
/// </summary>
public enum SlashSweepDirection
{
    FromGesture = 0,
    LeftToRight = 1,
    RightToLeft = 2
}

/// <summary>
/// 招式接续边：在某段接续窗口内接受某手势，转移到后继招式。
/// </summary>
[System.Serializable]
public class AttackMoveEdge
{
    [Tooltip("接受的手势")]
    public MoveGesture gesture;

    [Tooltip("最低蓄力等级：0 = 任意输入；1 = 必须达到一级蓄力。未达标时该边不参与匹配（例如未蓄力的竖滑是招架，不会被当成终结技）")]
    public int minChargeLevel = 0;

    [Tooltip("是否覆盖窗口。关闭时使用本招式或按类型派生的默认窗口")]
    public bool overrideWindow = false;

    [Range(0f, 1f)] public float windowStart01 = 0f;

    [Range(0f, 1f)] public float windowEnd01 = 1f;

    [Tooltip("后继招式。留空表示该输入不转移节点")]
    public AttackSkillConfig next;
}
