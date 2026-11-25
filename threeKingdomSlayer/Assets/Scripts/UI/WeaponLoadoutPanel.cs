using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// **主武器与招式面板**（2026-11-24，简易版 UI，后续再换美术与布局）。
///
/// 能做的事（就这两件，先跑通流程）：
///   · **换武器**：点左侧武器条目 → 写入存档 `equippedWeaponId`，**下一次进关生效**
///     （本局内不切武器，见 `design/gameplay-progression-loop.md` §3.1）；
///   · **招式解锁 / 升级**：右侧每条招式显示 Lv / 价格，花铜钱解锁或升级，
///     右侧是**招式树**（多线分支）：父节点解锁后才能买子节点；节点都是招式本身，没有纯数值升级。
///
/// 全部**运行时创建**：不碰场景、不需要美术资源，和 `EndlessHud` 同一套做法。
/// 数据全部来自 `WeaponService`（Inspector 引用链：HeroConfig.weaponCatalog）。
/// </summary>
public class WeaponLoadoutPanel : MonoBehaviour
{
    public static WeaponLoadoutPanel Instance { get; private set; }

    public static WeaponLoadoutPanel GetOrCreate()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("WeaponLoadoutPanel");
        return go.AddComponent<WeaponLoadoutPanel>();
    }

    [Header("配色")]
    public Color panelColor = new Color(0.08f, 0.07f, 0.06f, 0.92f);
    public Color accentColor = new Color(0.95f, 0.82f, 0.42f, 1f);
    public Color textColor = new Color(0.96f, 0.94f, 0.88f, 1f);
    public Color mutedColor = new Color(0.66f, 0.63f, 0.58f, 1f);

    private Canvas _canvas;
    private CanvasGroup _group;
    private TextMeshProUGUI _coinText;
    private RectTransform _weaponList;
    private RectTransform _moveList;
    private TextMeshProUGUI _weaponHeader;
    private MainWeaponConfig _selected;
    private bool _isOpen;

    public bool IsOpen => _isOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Build();
        Hide();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ─────────────────────────── 构建 ───────────────────────────

    private void Build()
    {
        var canvasGo = new GameObject("WeaponPanelCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 110;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(UIResolutionHelper.ReferenceWidth, UIResolutionHelper.ReferenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        _group = canvasGo.AddComponent<CanvasGroup>();

        var root = canvasGo.GetComponent<RectTransform>();

        // 全屏遮罩（挡住背后点击）
        var dim = CreateImage(root, "Dim", new Color(0f, 0f, 0f, 0.62f));
        Stretch(dim.rectTransform);
        dim.raycastTarget = true;

        // 主卡片
        var card = CreateImage(root, "Card", panelColor);
        card.raycastTarget = true;
        var cardRt = card.rectTransform;
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.sizeDelta = new Vector2(1000f, 780f);

        var title = CreateText(cardRt, "Title", "武器与招式", 52, TextAlignmentOptions.Left, accentColor);
        SetTopRect(title.rectTransform, 40f, -32f, 600f, 64f);

        _coinText = CreateText(cardRt, "Coins", "铜钱 0", 34, TextAlignmentOptions.Right, textColor);
        SetTopRect(_coinText.rectTransform, -40f, -36f, 320f, 56f, alignRight: true);

        var hint = CreateText(cardRt, "Hint", "换武器下一次进关生效 · 招式升级只加该招式伤害", 24,
                              TextAlignmentOptions.Left, mutedColor);
        SetTopRect(hint.rectTransform, 40f, -96f, 900f, 34f);

        // 左：武器列表
        _weaponList = CreateColumn(cardRt, "WeaponList", 40f, -150f, 360f, 560f);
        // 右：招式列表
        var right = CreateImage(cardRt, "MoveArea", new Color(0f, 0f, 0f, 0.22f));
        right.raycastTarget = false;
        var rightRt = right.rectTransform;
        rightRt.anchorMin = new Vector2(0f, 1f);
        rightRt.anchorMax = new Vector2(0f, 1f);
        rightRt.pivot = new Vector2(0f, 1f);
        rightRt.anchoredPosition = new Vector2(430f, -150f);
        rightRt.sizeDelta = new Vector2(530f, 560f);

        _weaponHeader = CreateText(rightRt, "MoveHeader", "", 30, TextAlignmentOptions.Left, accentColor);
        SetTopRect(_weaponHeader.rectTransform, 20f, -16f, 480f, 40f);

        _moveList = CreateColumn(rightRt, "MoveList", 20f, -70f, 490f, 470f);

        // 关闭
        var close = CreateButton(cardRt, "CloseButton", "关闭", new Vector2(0f, -330f), new Vector2(300f, 84f));
        close.onClick.AddListener(Hide);
    }

    private RectTransform CreateColumn(RectTransform parent, string name, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    private static void SetTopRect(RectTransform rt, float x, float y, float w, float h, bool alignRight = false)
    {
        rt.anchorMin = new Vector2(alignRight ? 1f : 0f, 1f);
        rt.anchorMax = new Vector2(alignRight ? 1f : 0f, 1f);
        rt.pivot = new Vector2(alignRight ? 1f : 0f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    private Image CreateImage(RectTransform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private TextMeshProUGUI CreateText(RectTransform parent, string name, string content, float size,
                                       TextAlignmentOptions align, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.font = UiFontUtility.ResolveCjkFont();
        text.text = content;
        text.fontSize = size;
        text.alignment = align;
        text.color = color;
        text.raycastTarget = false;
        text.enableWordWrapping = true;
        return text;
    }

    private Button CreateButton(RectTransform parent, string name, string label, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.22f, 0.19f, 0.15f, 1f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = position;

        var text = CreateText(rt, "Label", label, 34, TextAlignmentOptions.Center, textColor);
        Stretch(text.rectTransform);
        return btn;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ─────────────────────────── 开关 ───────────────────────────

    public void Show()
    {
        if (_canvas == null) return;
        _isOpen = true;
        if (_group != null)
        {
            _group.alpha = 1f;
            _group.blocksRaycasts = true;
            _group.interactable = true;
        }
        _canvas.enabled = true;
        Refresh();
    }

    public void Hide()
    {
        _isOpen = false;
        if (_group != null)
        {
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;
        }
        if (_canvas != null) _canvas.enabled = false;
    }

    public void Toggle()
    {
        if (_isOpen) Hide();
        else Show();
    }

    // ─────────────────────────── 内容 ───────────────────────────

    public void Refresh()
    {
        if (_coinText != null) _coinText.text = $"铜钱 {SaveManager.GetCoins()}";

        var catalog = WeaponService.Catalog;
        BuildWeaponList(catalog);
        BuildMoveList();
    }

    private void BuildWeaponList(MainWeaponCatalog catalog)
    {
        Clear(_weaponList);
        if (catalog == null || catalog.weapons == null)
        {
            AddRowText(_weaponList, 0, "没有主武器目录（HeroConfig.weaponCatalog 未配）", mutedColor);
            return;
        }

        var equipped = WeaponService.Equipped;
        if (_selected == null) _selected = equipped;

        float y = 0f;
        for (int i = 0; i < catalog.weapons.Count; i++)
        {
            var weapon = catalog.weapons[i];
            if (weapon == null) continue;

            bool usable = WeaponService.IsWeaponUsable(weapon);
            bool isEquipped = equipped == weapon;
            string state = !usable ? "未实装" : (isEquipped ? "已装备" : "可装备");
            string label = $"{weapon.displayName}\n<size=22><color=#9C958B>{state}</color></size>";

            var btn = CreateRowButton(_weaponList, y, 68f, label, usable, isEquipped ? accentColor : null);
            var captured = weapon;
            btn.onClick.AddListener(() =>
            {
                _selected = captured;
                if (WeaponService.TryEquip(captured)) Debug.Log($"[Weapon] 已装备 {captured.displayName}（下一次进关生效）");
                Refresh();
            });
            y -= 80f;
        }
    }

    private void BuildMoveList()
    {
        Clear(_moveList);
        var weapon = _selected != null ? _selected : WeaponService.Equipped;
        if (weapon == null)
        {
            if (_weaponHeader != null) _weaponHeader.text = "招式";
            AddRowText(_moveList, 0, "先选择一把武器", mutedColor);
            return;
        }

        if (_weaponHeader != null) _weaponHeader.text = $"{weapon.displayName} · 招式";

        if (weapon.moves == null || weapon.moves.Count == 0)
        {
            AddRowText(_moveList, 0, "这把武器还没有配招式", mutedColor);
            return;
        }

        // **招式树**：节点按 treePosition 摆位，父子之间连线（多线分支，像天赋树那样）
        DrawMoveTree(weapon);
    }

    /// <summary>画招式树：先连线，再摆节点（节点画在上层）。</summary>
    private void DrawMoveTree(MainWeaponConfig weapon)
    {
        for (int i = 0; i < weapon.moves.Count; i++)
        {
            var child = weapon.moves[i];
            if (child == null || child.prerequisiteIds == null) continue;
            for (int p = 0; p < child.prerequisiteIds.Count; p++)
            {
                var parent = weapon.FindEntryById(child.prerequisiteIds[p]);
                if (parent == null) continue;
                CreateTreeNodeLink(_moveList, parent.treePosition, child.treePosition);
            }
        }

        for (int i = 0; i < weapon.moves.Count; i++)
        {
            var entry = weapon.moves[i];
            if (entry == null || entry.move == null) continue;
            CreateMoveNode(weapon, entry);
        }
    }

    private void CreateMoveNode(MainWeaponConfig weapon, WeaponMoveEntry entry)
    {
        // 2026-11-25：面板已经有 entry，**直接按 entry 判定**（不依赖 PlayerState / 目录解析）。
        // 旧写法用 IsMoveUnlocked(entry.move)：拿不到 PlayerState / Catalog 时会回退成“未锁 = 已解锁”
        // ⇒ 清档后打开面板可能全部显示“已解锁”（时序相关的假阳性）。
        bool unlocked = WeaponService.IsEntryUnlocked(entry);
        bool prereqMet = WeaponService.ArePrerequisitesMet(weapon, entry);

        string title = string.IsNullOrEmpty(entry.displayName) ? entry.move.name : entry.displayName;
        string state = unlocked
            ? "已解锁"
            : (prereqMet ? $"花 {entry.unlockCoinCost} 解锁" : $"需先解锁：{MissingPrereqNames(weapon, entry)}");

        Color tint = unlocked
            ? new Color(0.26f, 0.21f, 0.13f, 0.95f)
            : (prereqMet ? new Color(0.18f, 0.16f, 0.13f, 0.95f) : new Color(0.12f, 0.11f, 0.10f, 0.85f));

        var btn = CreateTreeNode(_moveList, entry.treePosition, new Vector2(190f, 76f),
                                 $"{title}\n<size=20><color=#9C958B>{state}</color></size>",
                                 tint, !unlocked && prereqMet);

        var captured = entry;
        btn.onClick.AddListener(() => OnMoveAction(weapon, captured));
    }

    /// <summary>列出这条节点「还差哪些前置」（按名字，方便玩家看懂为什么点不了）。</summary>
    private static string MissingPrereqNames(MainWeaponConfig weapon, WeaponMoveEntry entry)
    {
        if (entry == null || entry.prerequisiteIds == null) return "";
        var names = new List<string>();
        for (int i = 0; i < entry.prerequisiteIds.Count; i++)
        {
            var req = weapon != null ? weapon.FindEntryById(entry.prerequisiteIds[i]) : null;
            if (req == null) continue;                       // 配错 id 不卡人
            if (WeaponService.IsEntryUnlocked(req)) continue;
            names.Add(string.IsNullOrEmpty(req.displayName) ? req.EffectiveMoveId : req.displayName);
        }
        return names.Count > 0 ? string.Join("、", names) : "";
    }

    /// <summary>父子节点之间的连线（一条细长 Image 旋转到两点之间）。</summary>
    private void CreateTreeNodeLink(RectTransform parent, Vector2 from, Vector2 to)
    {
        var go = new GameObject("Link", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.55f, 0.48f, 0.33f, 0.55f);
        img.raycastTarget = false;

        Vector2 a = TreeToLocal(from);
        Vector2 b = TreeToLocal(to);
        Vector2 delta = b - a;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = a + delta * 0.5f;
        rt.sizeDelta = new Vector2(delta.magnitude, 4f);
        rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    private Button CreateTreeNode(RectTransform parent, Vector2 treePos, Vector2 size,
                                  string label, Color tint, bool interactable)
    {
        var go = new GameObject("MoveNode", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = tint;
        img.raycastTarget = true;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.interactable = interactable;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = TreeToLocal(treePos);

        var text = CreateText(rt, "Label", label, 26, TextAlignmentOptions.Center, textColor);
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(8f, 6f);
        text.rectTransform.offsetMax = new Vector2(-8f, -6f);
        return btn;
    }

    /// <summary>招式树坐标（原点左下、y 向上）→ 面板局部坐标（留出边距）。</summary>
    private static Vector2 TreeToLocal(Vector2 treePos) => treePos + new Vector2(115f, 70f);

    private void OnMoveAction(MainWeaponConfig weapon, WeaponMoveEntry entry)
    {
        if (entry == null) return;

        var result = WeaponService.TryUnlockMove(weapon, entry);
        switch (result)
        {
            case WeaponMoveActionResult.PrerequisiteLocked:
                Debug.Log("[Weapon] 前置招式还没解锁");
                break;
            case WeaponMoveActionResult.NotEnoughCoins:
                Debug.Log("[Weapon] 铜钱不够");
                break;
            case WeaponMoveActionResult.Success:
                Debug.Log($"[Weapon] 已解锁 {entry.displayName}");
                break;
        }

        Refresh();
    }

    private void AddRowText(RectTransform parent, float y, string content, Color color)
    {
        var text = CreateText(parent, "Row", content, 28, TextAlignmentOptions.TopLeft, color);
        var rt = text.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(parent.sizeDelta.x, 70f);
    }

    private Button CreateRowButton(RectTransform parent, float y, float height, string label,
                                   bool interactable, Color? tint, float width = 0f, float x = 0f)
    {
        var go = new GameObject("Row", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var img = go.AddComponent<Image>();
        img.color = tint ?? new Color(0.16f, 0.14f, 0.12f, 0.9f);
        img.raycastTarget = true;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.interactable = interactable;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(width > 0f ? width : parent.sizeDelta.x, height);
        rt.anchoredPosition = new Vector2(x, y);

        var text = CreateText(rt, "Label", label, 30,
                              width > 0f ? TextAlignmentOptions.Center : TextAlignmentOptions.Left,
                              textColor);
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(width > 0f ? 6f : 16f, 6f);
        text.rectTransform.offsetMax = new Vector2(-10f, -6f);
        return btn;
    }

    private static void Clear(RectTransform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }
}
