using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// **教学关自动化驱动**（Play Mode 验证用）。
///
/// 干什么：把教学路线拉起来，然后按当前拍型自动「扮演玩家」：
///   • 需要点按的拍 → 隔 0.4s 点一下前排敌人；
///   • 霸体防御拍 → 敌人抡霸体招时按住防御；
///   • 技能打断拍 → 释放一个主动技能。
/// 然后把过程打进 Console（配合教学导演自己的日志一起看）。
///
/// 用 <see cref="EditorApplication.update"/> 驱动（不挂 MonoBehaviour）：
/// Editor 程序集里的 MonoBehaviour 在 Play Mode 里 AddComponent 会返回 null。
/// </summary>
public static class TutorialFlowDriver
{
    private static TutorialStageDirector _director;
    private static float _startTime;
    private static float _nextTapTime;
    private static bool _running;
    private static bool _prepared;
    private static bool _defendHeld;
    private static bool _skillUsed;
    private static float _nextSkillTime;
    private static List<UpgradeDefinition> _pendingChoices;
    private static float _choicePickAt;
    private static int _tapCount;
    private static FakeRoutePhase _lastPhase = FakeRoutePhase.None;
    private static string _lastStepKey = "none";
    private static readonly List<string> Events = new List<string>();

    /// <summary>在 Play Mode 中启动驱动（由 Locus / 自动化调用）。</summary>
    public static void StartInPlayMode()
    {
        if (_running) { Log("驱动已在运行"); return; }
        // 重置静态状态（允许重跑）
        _director = null;
        _prepared = false;
        _prepareRequested = false;
        _defendHeld = false;
        _skillUsed = false;
        _tapCount = 0;
        _pendingChoices = null;
        _lastPhase = FakeRoutePhase.None;
        _lastStepKey = "none";
        Events.Clear();
        _startTime = Time.unscaledTime;
        _running = true;
        var ucm = UpgradeChoiceManager.Instance;
        if (ucm != null) ucm.OnChoicesReady += OnChoicesReady;
        EditorApplication.update += Tick;
        Log("自动驱动已启动");
    }

    private static void OnChoicesReady(List<UpgradeDefinition> choices)
    {
        _pendingChoices = choices;
        Log("三选一弹窗：" + (choices != null ? choices.Count.ToString() : "0") + " 个候选");
    }

    public static void Stop()
    {
        EditorApplication.update -= Tick;
        _running = false;
        var ucm = UpgradeChoiceManager.Instance;
        if (ucm != null) ucm.OnChoicesReady -= OnChoicesReady;
        if (_defendHeld)
        {
            var ps = PlayerState.Instance;
            if (ps != null) ps.SetDefend(false);
            _defendHeld = false;
        }
        Log("驱动结束。事件：\n" + string.Join("\n", Events));
    }

    private static void Tick()
    {
        if (Time.unscaledTime - _startTime > 180f) { Stop(); return; }

        var flow = RouteFlowHub.Current;
        if (flow == null) return;
        var runtime = flow as FakeRouteRuntime;
        if (runtime == null)
        {
            Log("RouteFlowHub.Current 不是 FakeRouteRuntime，无法驱动");
            Stop();
            return;
        }

        if (_director == null)
        {
            _director = Object.FindObjectOfType<TutorialStageDirector>();
            if (_director == null) return;

            var tutorialRoute = _director.tutorialRouteConfig;
            if (tutorialRoute == null) { Log("教学路线配置为空"); Stop(); return; }

            Log("准备教学路线：" + tutorialRoute.stageName);
            runtime.PrepareForEntry(tutorialRoute);
            _prepareRequested = true;
            _prepareTime = Time.unscaledTime;
            return;   // 准备是异步的，等一会儿再开跑
        }

        if (_prepareRequested && !_prepared)
        {
            // 准备完成前不能开跑（旧路线的 Ready 会骗过阶段判断）
            if (Time.unscaledTime - _prepareTime < 2.5f) return;
            _prepared = true;
            runtime.RunPrepared();
            Log("路线已开跑");
            return;
        }

        Drive(runtime);
    }

    private static bool _prepareRequested;
    private static float _prepareTime;

    private static void Drive(FakeRouteRuntime runtime)
    {
        var alive = EnemyManager.Instance != null ? EnemyManager.Instance.GetAllAliveEnemies() : null;
        Enemy front = null;
        bool cFrame = false;
        if (alive != null)
        {
            for (int i = 0; i < alive.Count; i++)
            {
                var e = alive[i];
                if (e == null || e.state == EnemyState.Dead) continue;
                if (front == null || e.rowIndex < front.rowIndex) front = e;
                if (e.isAttackAnimating && e.isCFrame) cFrame = true;
            }
        }

        var step = _director.CurrentStep;

        // 0) 路线选择：点第一个路牌按钮
        if (runtime.Phase == FakeRoutePhase.ChoosingRoute)
        {
            var btn = Object.FindObjectOfType<RouteChoiceButton>();
            if (btn != null && Time.unscaledTime >= _nextTapTime)
            {
                _nextTapTime = Time.unscaledTime + 1.0f;
                Log("点击路线选择按钮");
                btn.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            }
            return;
        }

        // 0.2) 教学**开场流程**（2026-11-05）：冻场 + 第一段字幕 → 「点击继续」→ 第二段字幕
        //     这段玩家零交互 → 驱动必须等按钮出来点它，**且不许点按**（点了会被冻场吞掉）。
        if (_director.IntroActive)
        {
            var introBtn = _director.continueButton;
            if (introBtn != null && introBtn.IsShowing && Time.unscaledTime >= _nextTapTime)
            {
                _nextTapTime = Time.unscaledTime + 0.5f;
                // ⚠️ 必须走**真实 UI 点击路径**（射线 + EventSystem）→ 直接调回调会把
                //    「按钮其实点不动」这类洞全漏过去（2026-11-06 实测踩到过）。
                bool sent = introBtn.ClickViaUi();
                Log(sent ? "点「点击继续」（真实 UI 路径）" : "点「点击继续」**失败**（按钮点不动）");
            }
            return;
        }

        // 0.5) 拍型变化
        string stepKey = step != null ? (step.kind + "/" + step.waveIndex) : "none";
        if (stepKey != _lastStepKey)
        {
            _lastStepKey = stepKey;
            Log("拍 → " + stepKey);
        }

        // 1) 霸体防御拍：按住防御键（拍结束后要松开，否则玩家在防御位出不了招）
        bool beatRunning = _director.BeatActive;
        if (step != null && step.freezeOnCAttack && beatRunning)
        {
            var ps = PlayerState.Instance;
            // 只在**冻结（教学暂停）时**才按防御键 → 模拟真实玩家「看到提示 → 按住防御」
            // （之前不看 timeScale：拍一开始就按住了 → 冻结时玩家已经是防御位，教学瞬间失去意义）
            bool want = ps != null && ps.WeaponStance != null && !ps.WeaponStance.IsDefending
                        && Time.timeScale == 0f;
            if (want && !_defendHeld) { ps.SetDefend(true); _defendHeld = true; Log("按住防御键"); }
        }
        else if (_defendHeld)
        {
            var ps = PlayerState.Instance;
            if (ps != null) ps.SetDefend(false);
            _defendHeld = false;
            Log("松开防御键");
        }

        // 2) 技能打断拍：**只在冻结（timeScale=0）时**释放技能，模拟真实玩家
        //    （提前放技能会把敌人的 C 技直接压住 → 拍瞬间判定成功结束 → 教学根本没发生）
        if (step != null && step.kind == GestureHintKind.SkillSlot && beatRunning && Time.timeScale == 0f)
        {
            if (Time.unscaledTime >= _nextSkillTime)
            {
                _nextSkillTime = Time.unscaledTime + 2.0f;
                var inv = ActiveSkillInventory.Instance;
                if (inv != null && inv.UsesActiveSkills && inv.Entries.Count > 0)
                {
                    var entry = inv.Entries[0];
                    var def = entry.definition;
                    Log("释放技能：" + (def != null ? def.displayName : "?"));
                    inv.TryActivate(entry.id);
                }
            }
        }

        // 2.5) 三选一：自动选第一个（教学关全程只有这一次）
        //     故意**等 1.5s 再选**：教学提示手要指着推荐卡，等一下才看得见。
        var ucm = UpgradeChoiceManager.Instance;
        if (ucm != null && ucm.IsChoosing && _pendingChoices != null && _pendingChoices.Count > 0)
        {
            if (_choicePickAt == 0f) _choicePickAt = Time.unscaledTime + 20f;
            if (Time.unscaledTime >= _choicePickAt)
            {
                var pick = _pendingChoices[0];
                Log("三选一 → 选择 " + (pick != null ? pick.displayName : "?"));
                ucm.ConfirmChoice(pick);
                _pendingChoices = null;
                _choicePickAt = 0f;
            }
        }

        // 3) 点按：拍正在「禁止出招」时停手，其他时候一直点（把敌人打死推进波次）
        //    ⚠️ 技能拍（SkillSlot）**无论拍开没开都不点**：
        //     那一拍现在先等玩家选完三选1 再开拍（用户期望的流程），
        //     这段等待期里驱动要是继续点，会把教学目标打死 → 拍直接死掉（2026-11-02 实测）。
        //     （霸体防御拍 freezeOnCAttack 仍然只在「拍进行中」才停手：
        //     拍一结束就要继续点，把上一波敌人打死才能推进到下一波。）
        // 3.5) 教学拍指定的手势：按闸门来（点一下 / 横划 / 连招蓄满松手）
        var inputMgr = InputManager.Instance;
        if (inputMgr != null && front != null && Time.unscaledTime >= _nextTapTime)
        {
            MoveGesture teachGesture = MoveGesture.Tap;
            int teachCharge = 0;
            bool hasTeachGesture = false;
            switch (inputMgr.tutorialGestureTarget)
            {
                case TutorialGestureTarget.Tap: teachGesture = MoveGesture.Tap; hasTeachGesture = true; break;
                case TutorialGestureTarget.SwipeHorizontal: teachGesture = MoveGesture.SwipeHorizontal; hasTeachGesture = true; break;
                case TutorialGestureTarget.ChargedVertical: teachGesture = MoveGesture.SwipeVertical; teachCharge = 1; hasTeachGesture = true; break;
                case TutorialGestureTarget.ComboHold: teachGesture = MoveGesture.Hold; teachCharge = 1; hasTeachGesture = true; break;
            }
            if (hasTeachGesture)
            {
                _nextTapTime = Time.unscaledTime + 0.4f;
                _tapCount++;
                inputMgr.SubmitGestureForDebug(teachGesture, teachCharge, front.columnIndex,
                    new Vector2(Screen.width * 0.5f, Screen.height * 0.4f), true, 0f);
                Log("教学手势 #" + _tapCount + " " + teachGesture + "（charge=" + teachCharge + "）");
                return;
            }
        }

        // ⚠️ 技能拍（SkillSlot）期间不点按：**只在这一拍**进行中时不点。
        // 旧写法是「step.kind == SkillSlot 就一直不点」→ 拍结束了驱动还在等 → 最后一只敌人永远不死
        // → **教学关结算不出来**（整个「教学→天赋→招式教学」的链路卡在这里，2026-11-04 实测才发现）。
        bool wantsTap = !(beatRunning && step != null && step.kind == GestureHintKind.SkillSlot)
                        && !(beatRunning && step != null && step.freezeOnCAttack);
        if (wantsTap && front != null && Time.unscaledTime >= _nextTapTime)
        {
            _nextTapTime = Time.unscaledTime + 0.4f;
            _tapCount++;
            var input = InputManager.Instance;
            if (input != null)
            {
                bool ok = input.SubmitGestureForDebug(MoveGesture.Tap, 0, front.columnIndex,
                    new Vector2(Screen.width * 0.5f, Screen.height * 0.4f));
                if (_tapCount % 3 == 0 || ok) Log("点按 #" + _tapCount + " col=" + front.columnIndex + " ok=" + ok + " cFrame=" + cFrame);
            }
        }

        // 4) 阶段变化
        if (_lastPhase != runtime.Phase)
        {
            _lastPhase = runtime.Phase;
            Log("phase → " + runtime.Phase);
        }
    }

    private static void Log(string msg)
    {
        Debug.Log("[TutDriver] " + msg);
        Events.Add(Time.unscaledTime.ToString("F1") + "s " + msg);
        try
        {
            var dir = System.IO.Path.Combine(Application.dataPath, "../Library/Locus/tmp");
            System.IO.Directory.CreateDirectory(dir);
            System.IO.File.AppendAllLines(System.IO.Path.Combine(dir, "tutorial_driver.log"), Events);
            Events.Clear();
        }
        catch { }
    }
}
