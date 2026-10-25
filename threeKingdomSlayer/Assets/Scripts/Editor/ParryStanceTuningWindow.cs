using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// **招架姿态调参窗口**（Tools ▸ 三国杀戮 ▸ 招架姿态调参）。
///
/// 为什么需要它：持枪姿态的构图必须由**相机几何**解算，而不是靠截图目测——
/// 本画幅（1080×2160、aspect 0.5）很窄，枪长 10.8 在任何前景深度都必然出画，
/// 所以只能解算「矛头落在画面哪、杆身从哪穿出、起手时整杆是否在画外」。
///
/// 窗口给出这些**读数**（0% = 画面中心，100% = 画面边缘），调参数时实时更新。
/// 改完直接进 Battle 场景 Play 就能看，不需要截图来回试。
/// </summary>
public class ParryStanceTuningWindow : EditorWindow
{
    private const string AssetPath = "Assets/Prefabs/UI/Skills/Zhangfei_Parry.asset";
    private const float WeaponLength = 10.8f;   // FantasySpear3D 整杆世界长度（prefab 原尺寸）
    private const float TipOffset = WeaponLength * 0.5f;   // 根节点在枪身中段 → 矛头距根 = 长度/2

    private AttackSkillConfig _cfg;

    private Vector3 _anchor;
    private Vector3 _entryOffset;
    private float _tipDeg, _entryDeg, _defendDrop;
    private float _swingIn, _overshoot, _settle, _hold, _exitFast, _exitNormal;

    private string _readout = "";

    [MenuItem("Tools/三国杀戮/招架姿态调参")]
    private static void Open()
    {
        var win = GetWindow<ParryStanceTuningWindow>("招架姿态");
        win.minSize = new Vector2(380f, 420f);
        win.Load();
    }

    private void OnEnable() { Load(); }

    private void Load()
    {
        _cfg = AssetDatabase.LoadAssetAtPath<AttackSkillConfig>(AssetPath);
        if (_cfg == null) return;
        _anchor = _cfg.parryStanceHeadAnchor;
        _entryOffset = _cfg.parryStanceEntryRootOffset;
        _tipDeg = _cfg.parryStanceTipDegrees;
        _entryDeg = _cfg.parryStanceEntryAngleDegrees;
        _defendDrop = _cfg.parryStanceDefendAnchorDrop;
        _swingIn = _cfg.parryStanceSwingInSeconds;
        _overshoot = _cfg.parryStanceOvershootDegrees;
        _settle = _cfg.parryStanceSettleSeconds;
        _hold = _cfg.parryStanceHoldSeconds;
        _exitFast = _cfg.parryStanceExitFastSeconds;
        _exitNormal = _cfg.parryStanceExitNormalSeconds;
        Compute();
    }

    private void Apply()
    {
        if (_cfg == null) return;
        Undo.RecordObject(_cfg, "招架姿态调参");
        _cfg.parryStanceHeadAnchor = _anchor;
        _cfg.parryStanceEntryRootOffset = _entryOffset;
        _cfg.parryStanceTipDegrees = _tipDeg;
        _cfg.parryStanceEntryAngleDegrees = _entryDeg;
        _cfg.parryStanceDefendAnchorDrop = _defendDrop;
        _cfg.parryStanceSwingInSeconds = _swingIn;
        _cfg.parryStanceOvershootDegrees = _overshoot;
        _cfg.parryStanceSettleSeconds = _settle;
        _cfg.parryStanceHoldSeconds = _hold;
        _cfg.parryStanceExitFastSeconds = _exitFast;
        _cfg.parryStanceExitNormalSeconds = _exitNormal;
        EditorUtility.SetDirty(_cfg);
        AssetDatabase.SaveAssets();
        Compute();
    }

    private void OnGUI()
    {
        if (_cfg == null)
        {
            EditorGUILayout.HelpBox("找不到 " + AssetPath, MessageType.Error);
            if (GUILayout.Button("重试")) Load();
            return;
        }

        EditorGUILayout.LabelField("构图（相机几何解算）", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        _anchor = EditorGUILayout.Vector3Field("矛头锚点 (x,y,z)", _anchor);
        _tipDeg = EditorGUILayout.Slider("斜举角 (度)", _tipDeg, -90f, 180f);
        _entryDeg = EditorGUILayout.Slider("起手指向 (度)", _entryDeg, -90f, 180f);
        _entryOffset = EditorGUILayout.Vector3Field("起手枪尾偏移 (x,y,z)", _entryOffset);
        _defendDrop = EditorGUILayout.Slider("防御位下沉", _defendDrop, 0f, 1f);
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("时序", EditorStyles.boldLabel);
        _swingIn = EditorGUILayout.Slider("挥入时长 (s)", _swingIn, 0.05f, 1f);
        _overshoot = EditorGUILayout.Slider("过冲角 (度)", _overshoot, 0f, 30f);
        _settle = EditorGUILayout.Slider("回弹时长 (s)", _settle, 0.02f, 0.5f);
        _hold = EditorGUILayout.Slider("定格时长 (s)", _hold, 0f, 0.5f);
        _exitFast = EditorGUILayout.Slider("收枪·格挡成功 (s)", _exitFast, 0.05f, 1f);
        _exitNormal = EditorGUILayout.Slider("收枪·未格挡 (s)", _exitNormal, 0.05f, 2f);
        if (EditorGUI.EndChangeCheck()) Apply();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("读数（0% = 画面中心，100% = 画面边缘）", EditorStyles.boldLabel);
        EditorGUILayout.TextArea(_readout, GUILayout.Height(170f));

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("从资产重读")) Load();
            if (GUILayout.Button("复制当前值为文本"))
                EditorGUIUtility.systemCopyBuffer = BuildCopyText();
        }
        EditorGUILayout.HelpBox(
            "· 尺寸固定为 prefab 原尺寸（不可调）——武器大小是武器的属性，与动作无关。\n" +
            "· 整杆枪在任何前景深度都必然出画 → 只能解算“矛头在哪、杆身从哪穿出”。\n" +
            "· 改完直接进 Battle 场景 Play 看效果，不需要截图目测。", MessageType.Info);
    }

    /// <summary>几何解算：把关键点投影到屏幕百分比。</summary>
    private void Compute()
    {
        var cam = Camera.main;
        if (cam == null) { _readout = "场景里没有主相机。"; return; }

        float tanV = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float tanH = tanV * cam.aspect;
        Vector3 camPos = cam.transform.position, up = cam.transform.up, fwd = cam.transform.forward, right = cam.transform.right;

        System.Func<Vector3, Vector2> pct = p =>
        {
            Vector3 rel = p - camPos;
            float v = Vector3.Dot(rel, up), f = Vector3.Dot(rel, fwd), h = Vector3.Dot(rel, right);
            if (Mathf.Abs(f) < 0.01f) return Vector2.zero;
            return new Vector2(h / (f * tanH), v / (f * tanV));
        };

        Vector3 dirIdle = new Vector3(Mathf.Cos(_tipDeg * Mathf.Deg2Rad), Mathf.Sin(_tipDeg * Mathf.Deg2Rad), 0f);
        Vector3 dirEntry = new Vector3(Mathf.Cos(_entryDeg * Mathf.Deg2Rad), Mathf.Sin(_entryDeg * Mathf.Deg2Rad), 0f);
        Vector3 headIdle = _anchor;
        Vector3 rootIdle = headIdle - dirIdle * TipOffset;
        Vector3 rootEntry = rootIdle + _entryOffset;
        Vector3 headEntry = rootEntry + dirEntry * TipOffset;

        Vector2 pHead = pct(headIdle), pRoot = pct(rootIdle);
        Vector2 pEntryHead = pct(headEntry), pEntryRoot = pct(rootEntry);

        // 杆身从画面左缘穿出的点
        float exitT = -1f; Vector2 pExit = Vector2.zero;
        for (float s = 0f; s <= WeaponLength; s += 0.05f)
        {
            Vector2 p = pct(headIdle - dirIdle * s);
            if (p.x <= -1f) { exitT = s; pExit = p; break; }
        }

        var sb = new StringBuilder();
        sb.AppendLine($"矛头（斜举位）  右 {pHead.x * 100f:F0}%  上 {pHead.y * 100f:F0}%");
        sb.AppendLine($"枪尾（斜举位）  右 {pRoot.x * 100f:F0}%  上 {pRoot.y * 100f:F0}%");
        sb.AppendLine($"矛头（起手位）  右 {pEntryHead.x * 100f:F0}%  上 {pEntryHead.y * 100f:F0}%");
        sb.AppendLine($"枪尾（起手位）  右 {pEntryRoot.x * 100f:F0}%  上 {pEntryRoot.y * 100f:F0}%");
        bool entryOffFrame = pEntryHead.x > 1f || pEntryHead.x < -1f || pEntryHead.y > 1f || pEntryHead.y < -1f
                            || pEntryRoot.x > 1f || pEntryRoot.x < -1f || pEntryRoot.y > 1f || pEntryRoot.y < -1f;
        sb.AppendLine($"起手是否整杆出画：{(entryOffFrame ? "是 ✓" : "否 ✗（武器会在画面里凭空出现）")}");
        if (exitT >= 0f)
            sb.AppendLine($"杆身从左缘穿出：距矛头 {exitT:F2} 单位（占整杆 {exitT / WeaponLength * 100f:F0}%）位置 右 {pExit.x * 100f:F0}% 上 {pExit.y * 100f:F0}%");
        else
            sb.AppendLine("杆身未从左缘穿出（整杆都在画面里 → 违背进出画面的准则）");
        float d = Mathf.Abs(camPos.z - _anchor.z);
        sb.AppendLine($"深度 z={_anchor.z:F1}（距相机 {d:F1}）→ 可见半宽 {d * tanH:F2} 半高 {d * tanV:F2}；整杆 {WeaponLength:F1} → 必然出画");
        _readout = sb.ToString();
    }

    private string BuildCopyText()
    {
        return $"矛头锚点={_anchor} 斜举角={_tipDeg} 起手指向={_entryDeg} 起手枪尾偏移={_entryOffset} 防御下沉={_defendDrop}";
    }
}
