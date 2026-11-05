using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 三选一选项卡片 — 显示单个升级选项的名称、描述、稀有度背景
///
/// **选择方式：长按确认**（2026-11-05 用户定：不要“点两下”）。
///   按下 → 高亮并开始计进度 → 按住满 <see cref="holdConfirmSeconds"/> → 确认；
///   提前松手 → 取消（不算选）。
/// </summary>
public class UpgradeCard : MonoBehaviour
{
    [Header("UI 组件")]
    public Image backgroundImage;
    public Image iconFrameImage;
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI cooldownText;
    public TextMeshProUGUI levelStatusText;
    public Image passiveTagImage;
    public Image activeTagImage;
    public Sprite activeSkillFrameSprite;
    public Button button;
    [Header("选中状态")]
    [SerializeField] private Image selectedGlowImage;
    [SerializeField] private float selectedLift = 16f;

    [Header("长按确认")]
    [Tooltip("按住多久算确认（秒）。三选一改成长按选择后，这个值就是“选一张卡”的成本。")]
    public float holdConfirmSeconds = 0.35f;

    private UpgradeDefinition _upgradeDef;
    private Action<UpgradeCard> _onClicked;
    private Vector2 _baseAnchoredPosition;
    private Sprite _baseIconFrameSprite;
    private bool _hasBasePosition;

    // ── 长按确认的运行时状态 ──
    private bool _holding;
    private bool _holdCompleted;
    private float _holdStart;

    public UpgradeDefinition Definition => _upgradeDef;

    public void Setup(UpgradeDefinition def, Action<UpgradeCard> onClicked = null)
    {
        _upgradeDef = def;
        _onClicked = onClicked;
        if (!_hasBasePosition)
        {
            _baseAnchoredPosition = ((RectTransform)transform).anchoredPosition;
            _baseIconFrameSprite = iconFrameImage != null ? iconFrameImage.sprite : null;
            _hasBasePosition = true;
        }
        SetSelected(false);
        if (nameText != null)
        {
            nameText.text = def.displayName;
            nameText.alignment = TextAlignmentOptions.Center;
        }
        if (descriptionText != null)
            descriptionText.text = UpgradeEffectManager.Instance != null
                ? UpgradeEffectManager.Instance.GetUpgradePreviewDescription(def)
                : def.descriptionTemplate;

        int currentLevel = GetCurrentLevel(def);
        int maxLevel = UpgradeEffectManager.Instance != null
            ? UpgradeEffectManager.Instance.GetEffectiveMaxLevel(def)
            : Mathf.Max(1, def != null ? def.maxLevel : 1);
        if (levelStatusText != null)
        {
            if (currentLevel <= 0)
                levelStatusText.text = "新获得";
            else if (currentLevel >= maxLevel)
                levelStatusText.text = "Lv.Max";
            else if (currentLevel + 1 >= maxLevel)
                levelStatusText.text = $"Lv.{currentLevel} → Lv.Max";
            else
                levelStatusText.text = $"Lv.{currentLevel} → Lv.{currentLevel + 1}";
        }

        bool isActiveSkill = def.category == UpgradeCategory.ActiveSkill;
        if (cooldownText != null)
        {
            float nextCooldown = GetDisplayCooldown(def, currentLevel + 1);
            bool hasCooldown = nextCooldown > 0f;
            cooldownText.gameObject.SetActive(hasCooldown);
            if (hasCooldown)
            {
                float currentCooldown = currentLevel > 0 ? GetDisplayCooldown(def, currentLevel) : 0f;
                string cooldownValue = currentLevel > 0 && !Mathf.Approximately(currentCooldown, nextCooldown)
                    ? $"{currentCooldown:0} → <b><color=#F5C542>{nextCooldown:0}</color></b>"
                    : $"<b><color=#F5C542>{nextCooldown:0}</color></b>";
                cooldownText.text = $"冷却时间 {cooldownValue} 秒";
            }
        }
        if (activeTagImage != null)
            activeTagImage.gameObject.SetActive(isActiveSkill);
        if (passiveTagImage != null)
            passiveTagImage.gameObject.SetActive(!isActiveSkill && def.category != UpgradeCategory.Item);
        if (iconFrameImage != null)
            iconFrameImage.sprite = isActiveSkill && activeSkillFrameSprite != null
                ? activeSkillFrameSprite
                : _baseIconFrameSprite;

        if (iconImage != null && def.icon != null)
            iconImage.sprite = def.icon;
        if (button != null)
        {
            // **不用 onClick 确认**（那是“点两下”）—— 长按由本脚本自己轮询指针实现。
            // （Button 的 onPointerDown/onPointerUp UnityEvent 在 Unity 里并不存在，
            //   而本脚本自己实现 IPointerDownHandler 又会被 Button 抢掉事件 —— 组件顺序不可控。）
            button.onClick.RemoveAllListeners();
            button.interactable = true;      // 默认可选（教学会再按需要关掉某些卡）
        }
        SetSelectable(true);
    }

    /// <summary>
    /// 设成不可选（教学的三选1 里只让玩家选指定的那张）：
    /// 按钮不可点 + 整张卡变暗，玩家一眼看出「这张现在不能选」。
    /// </summary>
    public void SetSelectable(bool selectable)
    {
        if (button != null) button.interactable = selectable;
        float a = selectable ? 1f : 0.4f;
        if (backgroundImage != null)
        {
            var c = backgroundImage.color; c.a = a; backgroundImage.color = c;
        }
        if (iconImage != null)
        {
            var c = iconImage.color; c.a = a; iconImage.color = c;
        }
        if (iconFrameImage != null)
        {
            var c = iconFrameImage.color; c.a = a; iconFrameImage.color = c;
        }
        if (nameText != null)
        {
            var c = nameText.color; c.a = a; nameText.color = c;
        }
        if (descriptionText != null)
        {
            var c = descriptionText.color; c.a = a; descriptionText.color = c;
        }
    }

    private static float GetDisplayCooldown(UpgradeDefinition def, int level)
    {
        if (def is ActiveSkillDefinition activeSkill)
            return activeSkill.GetCooldown(level);

        return def.category == UpgradeCategory.TimedPassive
            ? def.GetTriggerInterval(level)
            : 0f;
    }

    private static int GetCurrentLevel(UpgradeDefinition def)
    {
        if (def == null) return 0;
        if (def.category == UpgradeCategory.ActiveSkill)
            return ActiveSkillInventory.Instance != null ? ActiveSkillInventory.Instance.GetLevel(def.upgradeId) : 0;
        return UpgradeEffectManager.Instance != null ? UpgradeEffectManager.Instance.GetUpgradeLevel(def.upgradeId) : 0;
    }

    public void SetSelectedGlow(Image glow)
    {
        selectedGlowImage = glow;
        selectedGlowImage.gameObject.SetActive(false);
    }

    public void SetSelected(bool selected)
    {
        if (selectedGlowImage != null)
            selectedGlowImage.gameObject.SetActive(selected);
        if (_hasBasePosition)
            ((RectTransform)transform).anchoredPosition = _baseAnchoredPosition + (selected ? Vector2.up * selectedLift : Vector2.zero);
    }

    // ── 长按确认 ────────────────────────────────────────────────────────

    /// <summary>当前这张卡是否可选（教学会把非推荐卡置为不可选）。</summary>
    private bool IsSelectable => button == null || button.interactable;

    /// <summary>指针事件（鼠标 / 触摸统一）：返回是否按下 / 抬起，以及屏幕坐标。</summary>
    private static bool ReadPointer(out bool down, out bool up, out Vector2 pos)
    {
        down = false; up = false; pos = Vector2.zero;
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            pos = t.position;
            down = t.phase == TouchPhase.Began;
            up = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
            return true;
        }
        pos = Input.mousePosition;
        down = Input.GetMouseButtonDown(0);
        up = Input.GetMouseButtonUp(0);
        return true;
    }

    private Camera EventCamera
    {
        get
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
            return canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }
    }

    private bool IsPointerOverCard(Vector2 screenPos)
    {
        if (!IsSelectable) return false;
        var rt = transform as RectTransform;
        if (rt == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, EventCamera);
    }

    private void Update()
    {
        bool down, up; Vector2 pos;
        ReadPointer(out down, out up, out pos);

        if (!_holding)
        {
            if (down && IsPointerOverCard(pos))
            {
                _holding = true;
                _holdCompleted = false;
                _holdStart = Time.unscaledTime;   // 弹窗把 timeScale 压成 0 → 计时必须用 unscaled
                SetSelected(true);               // 按下即高亮（"正在选"）
                SetHoldProgress(0f);
            }
            return;
        }

        if (up)
        {
            _holding = false;
            bool completed = _holdCompleted;
            _holdCompleted = false;
            if (completed)
            {
                SetHoldProgress(1f);   // 确认后的收尾由弹窗负责
            }
            else
            {
                SetHoldProgress(0f);
                SetSelected(false);    // 提前松手 → 取消
            }
            return;
        }

        float t = holdConfirmSeconds <= 0f ? 1f : (Time.unscaledTime - _holdStart) / holdConfirmSeconds;
        if (t >= 1f)
        {
            _holdCompleted = true;
            _holding = false;
            SetHoldProgress(1f);
            Debug.Log($"[UpgradeCard] 长按确认 frame={Time.frameCount} upgradeId={_upgradeDef?.upgradeId}");
            _onClicked?.Invoke(this);
        }
        else
        {
            SetHoldProgress(t);
        }
    }

    /// <summary>按住进度表现（不新增美术：选中光圈由暗到亮）。</summary>
    private void SetHoldProgress(float p)
    {
        if (selectedGlowImage == null) return;
        var c = selectedGlowImage.color;
        c.a = Mathf.Lerp(0.25f, 1f, Mathf.Clamp01(p));
        selectedGlowImage.color = c;
        selectedGlowImage.gameObject.SetActive(true);
    }

}
