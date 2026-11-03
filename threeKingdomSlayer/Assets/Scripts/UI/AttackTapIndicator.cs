using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// **常驻点击特效**（2026-11-01 用户新增需求）：
/// 「当敌人出现且玩家可以攻击时，常驻显示点击特效提示玩家点击触发攻击，
///   **仅最前排**的敌人会附带这个特效，让玩家知道可以点击攻击这些敌人。」
///
/// 外观：**和教学的手势提示同一套视觉与几何**（手 + 光圈，素材与锚点都取自
/// <see cref="TutorialGestureStyle"/>）—— 之前两处各写一套，导致
/// 「教学和常态引导长得不一样」「手指和光圈对不齐」（2026-11-02 用户反馈）。
///
/// 尺寸：**相对敌人**（光圈直径 = 敌人屏幕高度 × 倍数），
/// 而不是固定设计像素 —— 战斗 HUD 画布（1080×2160）与教学提示画布（1080×1920）缩放不同，
/// 固定像素会让常态提示在正式关里大得离谱（用户：「光圈当前非常大」）。
///
/// 规则：
///   • 玩家的基础招式 rangeRows = 1 → **只打得到第 0 排** → 只有最前排挂提示；
///   • 还在补齐/移动中的敌人不给（玩家此刻点不到它）；
///   • 敌人死亡 / 被对象池回收 → **立刻撤掉**（否则光圈会停在没有敌人的地方）；
///   • 对话中 / 三选一中 / 结算中 / 招式冷却中不显示；
///   • 纯视觉，**不拦点击**（raycastTarget = false）。
/// </summary>
public class AttackTapIndicator : MonoBehaviour
{
    [Header("外观（与教学提示同一套素材）")]
    [Tooltip("手的贴图（留空 = 不用手，只留光圈）")]
    public Sprite handSprite;

    [Tooltip("手的点按帧序列（教学提示用的那套；留空 = 程序化按压）")]
    public Sprite[] handTapFrames;

    [Tooltip("光圈贴图（留空 = 程序化圆环）")]
    public Sprite ringSprite;

    [Tooltip("光圈颜色")]
    public Color ringColor = new Color(1f, 0.93f, 0.66f, 0.85f);

    [Tooltip("手的不透明度")]
    [Range(0f, 1f)] public float handAlpha = 0.95f;

    [Header("尺寸（相对敌人）")]
    [Tooltip("光圈直径 = 敌人屏幕高度的倍数（教学镜头下敌人约 1100px 高 → 0.3 ≈ 330px，当胸口一圈的光晕）")]
    public float ringHeightRatio = 0.3f;

    [Tooltip("手尺寸 = 光圈直径 × 该比例（与教学提示 308/240 一致）")]
    public float handToRingRatio = TutorialGestureStyle.BaseHandSize / TutorialGestureStyle.BaseRingSize;

    [Tooltip("尺寸的兜底值（量不到敌人屏幕高度时用）")]
    public float fallbackRingSize = TutorialGestureStyle.BaseRingSize;

    [Header("动画")]
    [Tooltip("呼吸速度（次/秒）")]
    public float pulseSpeed = 1.6f;

    [Tooltip("呼吸幅度（缩放浮动范围）")]
    public float pulseAmount = 0.10f;

    [Tooltip("光圈自转速度（度/秒）")]
    public float ringSpinSpeed = 16f;

    [Tooltip("一次「点一下」的时长（秒）")]
    public float tapDuration = 1.0f;

    [Tooltip("帧序列每帧间隔（秒）")]
    public float tapFrameInterval = 0.08f;

    [Tooltip("按压时的倾角（度）。\n" +
             "**绕掌心转**（pivot=掌心）→ 掌心在按压过程中不动，只有指尖沿弧线下沉。\n" +
             "指尖离圈心最大距离 = 2×|掌心→指尖|×sin(θ/2)：8°→约 25 画布单位（光圈半径的 14%）。")]
    public float tapPressAngle = TutorialGestureStyle.PressAngle;

    [Header("闪烁")]
    [Tooltip("手 + 光圈是否**周期闪烁**（默认开）。\n" +
             "一直常驻会被当成可点的按钮；闪烁才读作「提示你现在可以点这里」。")]
    public bool blinkEnabled = true;

    [Tooltip("一个闪烁周期（秒）")]
    public float blinkPeriod = 1.8f;

    [Tooltip("周期里「亮着」的时间占比（0.55 = 亮 1.0s、灭 0.8s）")]
    [Range(0.05f, 1f)] public float blinkOnRatio = 0.55f;

    [Tooltip("亮/灭边界的淡入淡出时长（秒）；0 = 硬切")]
    public float blinkFadeSeconds = 0.22f;

    [Header("规则")]
    [Tooltip("检查间隔（秒）")]
    public float checkInterval = 0.1f;

    [Tooltip("只给**最前排**（rowIndex == 0）的敌人挂提示")]
    public bool frontRowOnly = true;

    [Tooltip("前排有多只时**只给随机 1 只**（2026-11-02 用户反馈：「多个敌人出现时应该随机给 1 个而不是都给」）。\n" +
             "随机结果**保持稳定**：当前这只还在前排就继续用它，它没了/前排换人了才重抽（否则会乱跳）。")]
    public bool singleRandomTarget = true;

    [Tooltip("招式冷却中就不显示（玩家现在点不出招）")]
    public bool hideWhileAttackOnCooldown = true;

    [Tooltip("调试用：教学拍进行中也显示（默认 false —— 教学自己有手势提示）")]
    public bool showDuringTeaching = false;

    private class Unit
    {
        public Enemy enemy;
        public GameObject root;
        public Image ring;
        public Image ripple;
        public Image hand;
        public RectTransform handRect;
        public RectTransform ringRect;
    }

    private readonly List<Unit> _units = new List<Unit>();
    private Canvas _canvas;
    private RectTransform _canvasRect;
    private float _nextCheck;
    private float _time;
    private static Sprite _fallbackRing;

    /// <summary>当前挂着引导的那只（随机选的，但要稳定 —— 换人只在它没了/前排换人时）。</summary>
    private Enemy _target;

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas != null) _canvasRect = _canvas.transform as RectTransform;
    }

    private void Update()
    {
        if (_canvas == null) _canvas = GetComponentInParent<Canvas>();
        if (_canvasRect == null && _canvas != null) _canvasRect = _canvas.transform as RectTransform;

        _time += Time.unscaledDeltaTime;

        // 教学手势提示在屏上 → 常驻引导**立刻**撤掉（不等下一次 0.1s 刷新：
        // 否则会有 ≤0.1s 的窗口两套手+光圈同时在屏，玩家看到两个提示）。
        var hint = TutorialGestureHint.Instance;
        if (!showDuringTeaching && hint != null && hint.IsShowing)
        {
            for (int i = 0; i < _units.Count; i++)
                if (_units[i].root != null) _units[i].root.SetActive(false);
            return;
        }

        if (Time.unscaledTime >= _nextCheck)
        {
            _nextCheck = Time.unscaledTime + Mathf.Max(0.02f, checkInterval);
            Refresh();
        }

        Animate();
    }

    /// <summary>按当前前排敌人重建提示。</summary>
    private void Refresh()
    {
        var targets = PickTargets();
        while (_units.Count < targets.Count) _units.Add(CreateUnit());
        while (_units.Count > targets.Count)
        {
            int last = _units.Count - 1;
            if (_units[last].root != null) Destroy(_units[last].root);
            _units.RemoveAt(last);
        }

        for (int i = 0; i < _units.Count; i++)
        {
            var unit = _units[i];
            if (unit.root == null) continue;
            unit.enemy = targets[i];

            // 目标失效（死了 / 被回收 / 禁用）→ **立刻撤掉**，否则光圈会停在没有敌人的地方
            if (unit.enemy == null || unit.enemy.state == EnemyState.Dead
                || !unit.enemy.gameObject.activeInHierarchy
                || unit.enemy.currentHealth <= 0f)
            {
                unit.root.SetActive(false);
                continue;
            }

            if (!TryGetScreenPoint(unit.enemy, out var screenPos))
            {
                unit.root.SetActive(false);
                continue;
            }
            unit.root.SetActive(true);

            if (_canvasRect == null ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, screenPos, null, out var local))
                continue;

            // 尺寸按**敌人的屏幕高度**算（不写死设计像素）→ 正式关里不再过大
            float enemyScreenH = GetEnemyScreenHeight(unit.enemy);
            float canvasScale = _canvasRect != null ? _canvasRect.rect.height / Mathf.Max(1f, Screen.height) : 1f;
            float ringUnits = enemyScreenH > 1f
                ? (enemyScreenH * ringHeightRatio) / Mathf.Max(0.01f, canvasScale)
                : fallbackRingSize;
            float handUnits = ringUnits * handToRingRatio;

            if (unit.ringRect != null) unit.ringRect.sizeDelta = new Vector2(ringUnits, ringUnits);
            if (unit.ripple != null) ((RectTransform)unit.ripple.transform).sizeDelta = new Vector2(ringUnits, ringUnits);
            if (unit.handRect != null)
            {
                unit.handRect.sizeDelta = new Vector2(handUnits, handUnits);
                // pivot 在掌心 → 位置补偏移，让**指尖**落在目标点（与教学提示同一套）
                //
                // ⚠️ 这里只能给「**相对根节点**的偏移」：手是 TapHint 根节点的子物体，
                //    而根节点已经摆在目标点上了 → 写成画布绝对坐标会把位置**算两遍**
                //    （指尖落到 2×目标点，敌人离画面中心越远偏得越多）。
                //    2026-11-02 正式关实测：敌人偏 ±176 画布单位 → 指尖偏圈心 ~200px。
                unit.handRect.anchoredPosition = TutorialGestureStyle.HandPositionOffset(handUnits);
            }
            unit.root.GetComponent<RectTransform>().anchoredPosition = local;
        }
    }

    /// <summary>
    /// 这一轮要给谁挂提示。
    ///
    /// 前排往往有 2~3 只（用户 2026-11-02 反馈：「多个敌人出现时，常态攻击指引应该随机给 1 个而不是都给」）——
    /// 全给的话玩家不知道该点哪个，而且满屏都是手。
    /// 但**随机必须稳定**：每 0.1s 刷新都重抽 → 引导会在几只敌人之间乱跳，比全给更糟。
    /// 规则：当前这只还在候选里就继续用它；它没了 / 前排换人了才重抽。
    /// </summary>
    private List<Enemy> PickTargets()
    {
        var picked = new List<Enemy>(1);
        var candidates = CollectTargets();
        if (candidates.Count == 0)
        {
            _target = null;
            return picked;
        }

        Enemy chosen = null;
        if (!singleRandomTarget)
        {
            // 关掉随机 = 回到旧行为（前排全给）
            picked.AddRange(candidates);
            _target = candidates[0];
            return picked;
        }

        if (_target != null && candidates.Contains(_target)) chosen = _target;
        else chosen = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        _target = chosen;
        picked.Add(chosen);
        return picked;
    }

    /// <summary>该给哪些敌人挂提示。</summary>
    private List<Enemy> CollectTargets()
    {
        var result = new List<Enemy>();
        if (!CanPlayerAttack()) return result;

        var em = EnemyManager.Instance;
        if (em == null) return result;
        var alive = em.GetAllAliveEnemies();
        if (alive == null) return result;

        for (int i = 0; i < alive.Count; i++)
        {
            var e = alive[i];
            if (e == null || e.state == EnemyState.Dead) continue;
            if (!e.gameObject.activeInHierarchy || e.currentHealth <= 0f) continue;
            // **只给最前排（rowIndex == 0）**：玩家的基础招式 rangeRows = 1 → 只打得到第 0 排。
            if (frontRowOnly && e.rowIndex != 0) continue;
            // 还在补齐/移动中的敌人不给：玩家此刻点不到它（它马上还会往前走）。
            if (e.state == EnemyState.Moving || e.IsMovingForward) continue;
            result.Add(e);
        }
        return result;
    }

    /// <summary>玩家现在能不能攻击（不能就别提示）。</summary>
    private bool CanPlayerAttack()
    {
        var stage = StageController.Instance;
        if (stage != null && stage.CurrentState != StageState.InProgress) return false;

        // 暂停中（三选一弹窗 / 阻塞台词 / 教学冻结）不显示；
        // 教学的非阻塞台词**不**暂停 → 那时候照常显示（提示是常驻的）。
        if (Time.timeScale <= 0.01f) return false;

        // 教学拍进行中**不显示**（教学自己就有手势提示，两者叠在一起会看不清）
        var director = TutorialStageDirector.Instance;
        if (!showDuringTeaching && director != null && director.BeatActive) return false;

        // 教学手势提示**只要还在屏上**就不显示（不只是拍进行中）：
        // 三选一那只手、路线箭头那只手出现时，拍早已结束（BeatActive=false），
        // 但它们同样是指路的手 —— 两套手+光圈同时出现，玩家不知道该点哪个。
        var hint = TutorialGestureHint.Instance;
        if (!showDuringTeaching && hint != null && hint.IsShowing) return false;

        var choice = UpgradeChoiceManager.Instance;
        if (choice != null && choice.IsChoosing) return false;

        if (hideWhileAttackOnCooldown)
        {
            var ps = PlayerState.Instance;
            if (ps != null && !ps.IsAttackReady(AttackType.Stab)) return false;
        }
        return true;
    }

    /// <summary>敌人的屏幕坐标（用可见本体中心，不是脚底）。</summary>
    private static bool TryGetScreenPoint(Enemy enemy, out Vector2 screenPos)
    {
        screenPos = Vector2.zero;
        if (enemy == null) return false;
        var cam = Camera.main;
        if (cam == null) return false;
        var renderer = enemy.GetComponentInChildren<SpriteRenderer>();
        Vector3 world = renderer != null ? renderer.bounds.center : enemy.transform.position + Vector3.up * 1.33f;
        var sp = cam.WorldToScreenPoint(world);
        if (sp.z < 0f) return false;
        screenPos = new Vector2(sp.x, sp.y);
        return true;
    }

    /// <summary>敌人可见本体在屏幕上的高度（像素）。</summary>
    private static float GetEnemyScreenHeight(Enemy enemy)
    {
        if (enemy == null) return 0f;
        var cam = Camera.main;
        if (cam == null) return 0f;
        var renderer = enemy.GetComponentInChildren<SpriteRenderer>();
        Vector3 min, max;
        if (renderer != null) { min = renderer.bounds.min; max = renderer.bounds.max; }
        else { min = enemy.transform.position; max = min + Vector3.up * 2.66f; }
        var a = cam.WorldToScreenPoint(min);
        var b = cam.WorldToScreenPoint(max);
        if (a.z < 0f || b.z < 0f) return 0f;
        return Mathf.Abs(b.y - a.y);
    }

    /// <summary>建一个「手 + 光圈」的提示单元（挂在 Canvas 下）。</summary>
    private Unit CreateUnit()
    {
        // 光圈挂在 **Canvas 下**（位置用 Canvas 本地坐标，挂在子物体上会多一层偏移）。
        Transform parent = _canvasRect != null ? _canvasRect : transform;
        var root = new GameObject("TapHint", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        root.transform.SetAsFirstSibling();     // 画在 HUD 元素下面
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = new Vector2(fallbackRingSize, fallbackRingSize);

        var ringGo = new GameObject("Ring", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        ringGo.transform.SetParent(root.transform, false);
        var ringRect = ringGo.GetComponent<RectTransform>();
        ringRect.anchorMin = ringRect.anchorMax = new Vector2(0.5f, 0.5f);
        ringRect.pivot = new Vector2(0.5f, 0.5f);
        ringRect.sizeDelta = new Vector2(fallbackRingSize, fallbackRingSize);
        var ringImg = ringGo.GetComponent<Image>();
        ringImg.sprite = ringSprite != null ? ringSprite : GetFallbackRing();
        ringImg.color = ringColor;
        ringImg.raycastTarget = false;

        // 扩散波纹（第二圈）：周期性向外扩 + 淡出 → 光圈有「动感」，不是死在那儿
        var rippleGo = new GameObject("Ripple", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        rippleGo.transform.SetParent(root.transform, false);
        var rippleRect = rippleGo.GetComponent<RectTransform>();
        rippleRect.anchorMin = rippleRect.anchorMax = new Vector2(0.5f, 0.5f);
        rippleRect.pivot = new Vector2(0.5f, 0.5f);
        rippleRect.sizeDelta = new Vector2(fallbackRingSize, fallbackRingSize);
        var rippleImg = rippleGo.GetComponent<Image>();
        rippleImg.sprite = ringImg.sprite;
        rippleImg.color = new Color(ringColor.r, ringColor.g, ringColor.b, 0f);
        rippleImg.raycastTarget = false;

        Image handImg = null;
        RectTransform handRect = null;
        if (handSprite != null || (handTapFrames != null && handTapFrames.Length > 0))
        {
            var handGo = new GameObject("Hand", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            handGo.transform.SetParent(root.transform, false);
            handRect = handGo.GetComponent<RectTransform>();
            handRect.anchorMin = handRect.anchorMax = new Vector2(0.5f, 0.5f);
            handRect.pivot = TutorialGestureStyle.HandPivot;   // 掌心（旋转支点）
            handRect.sizeDelta = new Vector2(fallbackRingSize * handToRingRatio, fallbackRingSize * handToRingRatio);
            handImg = handGo.GetComponent<Image>();
            handImg.sprite = handSprite;
            var c = Color.white; c.a = handAlpha;
            handImg.color = c;
            handImg.raycastTarget = false;
        }

        return new Unit
        {
            root = root,
            ring = ringImg,
            ripple = rippleImg,
            hand = handImg,
            handRect = handRect,
            ringRect = ringRect,
        };
    }

    /// <summary>闪烁系数（0~1）：亮着=1，灭着=0，边界做平滑过渡。</summary>
    private float BlinkAlpha()
    {
        if (!blinkEnabled || blinkPeriod <= 0.01f) return 1f;
        float onTime = Mathf.Clamp01(blinkOnRatio) * blinkPeriod;
        float fade = Mathf.Clamp(blinkFadeSeconds, 0f, onTime * 0.5f);
        float tt = Time.unscaledTime % blinkPeriod;
        if (tt > onTime) return 0f;                 // 灭着
        if (fade <= 0.001f) return 1f;             // 硬切
        float edge = Mathf.Min(tt, onTime - tt);  // 距离「亮→灭 / 灭→亮」边界的距离
        return Mathf.Clamp01(edge / fade);
    }

    /// <summary>呼吸 + 自转 + 扩散波纹 + 点按循环（和教学的点按表现一致）。</summary>
    private void Animate()
    {
        float breathe = 1f + Mathf.Sin(_time * pulseSpeed * Mathf.PI * 2f) * pulseAmount;
        float cycle = Mathf.Max(0.2f, tapDuration);
        float phase = (_time % cycle) / cycle;
        float blink = BlinkAlpha();

        for (int i = 0; i < _units.Count; i++)
        {
            var unit = _units[i];
            if (unit.root == null || !unit.root.activeSelf) continue;

            if (unit.ring != null)
            {
                // 呼吸 + 自转 → 光圈是「活的」
                unit.ring.rectTransform.localScale = new Vector3(breathe, breathe, 1f);
                unit.ring.rectTransform.localRotation = Quaternion.Euler(0f, 0f, _time * ringSpinSpeed);
                var c = ringColor;
                c.a = ringColor.a * (0.7f + 0.3f * breathe) * blink;
                unit.ring.color = c;
            }

            if (unit.ripple != null)
            {
                // 扩散波纹：每隔一拍从中心向外扩一圈并淡出
                float rp = (_time % cycle) / cycle;
                float s = Mathf.Lerp(0.65f, 1.6f, rp);
                unit.ripple.rectTransform.localScale = new Vector3(s, s, 1f);
                var rc = ringColor;
                rc.a = ringColor.a * Mathf.Max(0f, 1f - rp) * 0.7f * blink;
                unit.ripple.color = rc;
            }

            if (unit.hand != null && unit.handRect != null)
            {
                // 闪烁：手和光圈一起亮灭（常驻会被当成按钮）
                var hc = unit.hand.color;
                hc.a = handAlpha * blink;
                unit.hand.color = hc;

                // 点按：前 35% 压下去，后 65% 回弹。
                // pivot 在**掌心** → 手绕掌心转、掌心不动（2026-11-02 用户反馈：
                // 「实际是绕着手指尖在转，不像点击」—— 之前还有一条逐帧补偿把指尖钉死在圈心，
                //  等价于绕指尖转；现在**不做位置补偿**，指尖只在静止姿态参与定位）。
                float press = phase < 0.35f
                    ? Mathf.SmoothStep(0f, 1f, phase / 0.35f)
                    : 1f - Mathf.SmoothStep(0f, 1f, (phase - 0.35f) / 0.65f);
                float angle = tapPressAngle * press;      // 正角度 = 指尖下沉（读作“按下去”）
                float scale = Mathf.Lerp(1f, 0.94f, press);
                unit.handRect.localRotation = Quaternion.Euler(0f, 0f, angle);
                unit.handRect.localScale = Vector3.one * scale;
                // **不再回写 anchoredPosition**：位置由 Refresh() 按「指尖落在圈心」摆好，
                // 按压只改旋转/缩放（绕掌心）。

                // 帧序列版（教学同款）：按帧切图
                if (handTapFrames != null && handTapFrames.Length > 0)
                {
                    float frameTime = phase * cycle;
                    int index = Mathf.Clamp(Mathf.FloorToInt(frameTime / Mathf.Max(0.01f, tapFrameInterval)), 0, handTapFrames.Length - 1);
                    var sprite = handTapFrames[index];
                    if (sprite != null && unit.hand.sprite != sprite) unit.hand.sprite = sprite;
                }
            }
        }
    }

    /// <summary>没有配贴图时程序化画一个圆环（描边）。</summary>
    private static Sprite GetFallbackRing()
    {
        if (_fallbackRing != null) return _fallbackRing;
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            name = "TapRing_Procedural"
        };
        var px = new Color32[size * size];
        float c = size * 0.5f;
        float outer = size * 0.5f - 2f;
        float inner = outer - 7f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                if (d <= outer && d >= inner) px[y * size + x] = new Color32(255, 255, 255, 235);
                else px[y * size + x] = new Color32(255, 255, 255, 0);
            }
        }
        tex.SetPixels32(px);
        tex.Apply();
        _fallbackRing = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _fallbackRing;
    }
}
