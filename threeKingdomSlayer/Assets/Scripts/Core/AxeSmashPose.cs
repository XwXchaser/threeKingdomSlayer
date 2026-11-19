using UnityEngine;

/// <summary>
/// 抡斧劈砍的**表现参数与位姿**（单一来源）。
/// 运行时（<c>ActiveSkillRunner.RunAxeSmash</c>）与编辑器调姿工具（<c>AxePoseTuningTool</c>）共用同一套数学。
///
/// ★ 2026-11-15：三帧位姿改为**美术在 Battle 场景里摆定的姿态，原样照搬**（美术只摆位置与旋转，
///   缩放按其要求用 2）。帧序：
///   <c>Start</c>（起手）→ <c>Apex</c>（抡到最高）→ <c>End</c>（砸下＝命中帧）。
///
///   握把位置以**相对落点的偏移**存储：
///   换关卡/换阵型（第一排 z 变了）时整段抡斧跟着目标排走，
///   而构图与当前 N1 关完全一致（落点＝第一排敌人胸口）。
///
///   ⚠ 美术摆出来的三帧**刃尖都不碰敌人**（End 帧离落点 2.6 单位、斧身在相机前约 4.5 单位处）——
///     这是美术 2026-11-15 明知并确认的效果（要「斧在近景抡」的观感），伤害仍由技能逻辑结算。
///
/// 基准（2026-11-15 实测）：
///   · 主相机 (0,3,-10) rot18 fov60，竖屏 1080×2160
///   · 第一排敌人（rowIndex=0）transform (0,0.14,-1.0) ⇒ 胸口落点 (0,0.94,-1.6)
///   · 美术摆位时的轴心＝柄尾（爪尖），刃尖＝Prefab 的 Tip 锚点
/// </summary>
public static class AxeSmashPose
{
    /// <summary>运行时缩放（美术摆位时用的比例，原样照搬）。</summary>
    public const float RuntimeScale = 2f;
    /// <summary>刃口落在敌人胸口的高度（相对敌人 transform）。</summary>
    public const float ImpactHeightOffset = 0.8f;
    /// <summary>3D 斧头往相机挪一点，压在 2D 敌人前面（参照锁镰的 z 处理）。</summary>
    public const float DepthInFrontOfRow = 0.6f;

    public const float WindUpSeconds = 0.50f;         // ① 蓄势：把斧举起来（**后半段变慢**＝到位前的减速＝蓄势）
    public const float ApexHoldSeconds = 0.30f;       // ② 绷住：最高点停一拍（张力）
    public const float ChopSeconds = 0.22f;           // ③ 劈下：**要快** —— 出手即有速度，末端砸下去
    public const float HitStopSeconds = 0.12f;        // ④ 顿帧（≈3.6 帧；重量主要落在敌人那一侧）
    public const float FollowThroughSeconds = 0.45f;  // ⑤ 渐变消失：原地淡出（End 就是最终位置）
    /// <summary>命中时镜头的推近幅度（度，≤2° 以免动构图）。</summary>
    public const float ImpactCameraPushDegrees = 1.6f;
    /// <summary>命中时镜头推近的保持时长（秒）。</summary>
    public const float ImpactCameraPushHold = 0.22f;

    /// <summary>砸地裂纹特效覆盖的地面直径（世界单位）。</summary>
    public const float GroundCrackDiameter = 3.0f;

    /// <summary>
    /// **斧 mesh 最低点在“斧根节点局部空间”的位置**（＝斧刃扎进地面的那一点）。
    /// ⚠ 2026-11-16：**不能运行时读 mesh.vertices** —— 斧 FBX 的导入设置 `isReadable = false`，
    ///   运行时访问会抛 “Not allowed to access vertices …”，协程直接中断（连带伤害结算不执行）。
    ///   所以这里用**编辑态量好的常量**（2026-11-16 实测 `Assets/Prefabs/SkillWeapons/Axe3D.prefab`）。
    ///   ★ 换/重导模型后必须重量（菜单：Tools/测试/抡斧/重测斧刃入地点）。
    /// </summary>
    public static Vector3 AxeLowestVertexRootLocal = new Vector3(-0.01534f, 0f, -0.00302f);

    // ───────────────────────── 动态曲线（2026-11-15 重做：蓄势 + 沉重）─────────────────────────
    // 位置与旋转**用不同的缓动**：重家伙的读感来自"头比手慢/快"——
    //   起手时**头滞后于手**（拖着走＝重），下劈时**头超前于手**（甩出去＝有劲）。

    /// <summary>
    /// 起手：**ease-out**（前快后慢）—— 斧举到位前的**后半段明显变慢**，
    /// 到位时速度归零 ⇒ 蓄势感由“减速到位”表达（2026-11-15 用户要求：不要抖动）。
    /// </summary>
    public static float EaseWindUpPos(float t)
    {
        t = Mathf.Clamp01(t);
        float u = 1f - t;
        return 1f - u * u * Mathf.Sqrt(u);   // = 1-(1-t)^2.5
    }

    /// <summary>
    /// 起手（旋转）：同一条 ease-out，但**滞后 18%** ⇒ 斧头拖在后面到位＝重。
    /// </summary>
    public static float EaseWindUpRot(float t)
    {
        t = Mathf.Clamp01((t - 0.18f) / 0.82f);
        float u = 1f - t;
        return 1f - u * u * Mathf.Sqrt(u);
    }

    /// <summary>下劈：出手瞬间就有速度（不能从 0 起），末端猛加速砸下去。</summary>
    public static float EaseChopPos(float t)
    {
        t = Mathf.Clamp01(t);
        return 0.25f * t + 0.75f * t * t * t;
    }

    /// <summary>下劈（旋转）：比位置**更后置** ⇒ 刃口在最后一段甩出去砸下去。</summary>
    public static float EaseChopRot(float t)
    {
        t = Mathf.Clamp01(t);
        return 0.15f * t + 0.85f * t * t * t;
    }

    /// <summary>收势：快速起步、缓慢停下（惯性衰减）。</summary>
    public static float EaseFollow(float t)
    {
        t = Mathf.Clamp01(t);
        float u = 1f - t;
        return 1f - u * u * u;
    }

    /// <summary>落点：敌人 transform + 胸口高度 + 往相机挪。</summary>
    public static Vector3 ImpactPoint(Vector3 enemyPosition)
    {
        return enemyPosition + new Vector3(0f, ImpactHeightOffset, -DepthInFrontOfRow);
    }

    // ───────────────────────── 三帧（美术 2026-11-15 摆定，原样照搬）─────────────────────────
    // ★ 握把＝柄尾＝轴心；**世界绝对坐标**（美术在 Battle 场景里摆的原值）。
    //   本技能是“对排的范围伤害”，伤害由数据结算（每排各自结算），
    //   武器表现**不锁任何敌人的位置** ⇒ 三帧固定在美术摆的位置。
    //   （旋转本来就是世界旋转，不变。）

    /// <summary>起手帧的握把世界坐标。</summary>
    public static readonly Vector3 StartGripPosition = new Vector3(-0.10f, -1.00f, -5.47f);
    /// <summary>最高点帧的握把世界坐标。</summary>
    public static readonly Vector3 ApexGripPosition = new Vector3(-0.60f, -0.80f, -5.97f);
    /// <summary>命中帧的握把世界坐标。</summary>
    public static readonly Vector3 EndGripPosition = new Vector3(-0.04f, -1.20f, -5.84f);

    /// <summary>起手帧的世界旋转（美术摆定的原值）。</summary>
    public static readonly Quaternion StartRotation = new Quaternion(0.5697f, 0.4570f, 0.5095f, 0.4550f).normalized;

    /// <summary>最高点帧的世界旋转（美术摆定的原值）。</summary>
    public static readonly Quaternion ApexRotation = new Quaternion(-0.0682f, 0.7149f, -0.2110f, 0.6631f).normalized;

    /// <summary>命中帧的世界旋转（美术摆定的原值）。</summary>
    public static readonly Quaternion EndRotation = new Quaternion(0.4862f, 0.5135f, 0.4069f, 0.5783f).normalized;

    public static Vector3 StartGrip() { return StartGripPosition; }
    public static Vector3 ApexGrip() { return ApexGripPosition; }
    public static Vector3 EndGrip() { return EndGripPosition; }

    public static Quaternion StartRot { get { return StartRotation; } }
    public static Quaternion ApexRot { get { return ApexRotation; } }
    public static Quaternion EndRot { get { return EndRotation; } }

    /// <summary>命中帧的刃口指向（世界单位向量）：由 End 帧旋转 × 刃尖局部方向推出。</summary>
    public static Vector3 EndAim(Vector3 tipLocal)
    {
        if (tipLocal.sqrMagnitude < 1e-8f) return Vector3.forward;
        return EndRotation * tipLocal.normalized;
    }
}
