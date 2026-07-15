using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Boss 常驻血条 — 由 BattleHUD 动态创建和管理。
/// 每帧轮询 Enemy.currentHealth/currentPoise，避免事件订阅时序问题。
/// </summary>
public class BossHealthUI : MonoBehaviour
{
    [Header("UI 元件")]
    public Image healthFill;
    public Image poiseFill;
    public TMP_Text bossNameText;
    public CanvasGroup canvasGroup;

    private Enemy _boss;
    private float _maxHealth;
    private float _maxPoise;
    private int _frameCounter;
    public Enemy BoundBoss => _boss;

    private static Sprite _whiteSprite;

    /// <summary>
    /// 绑定 Boss 并开始显示
    /// </summary>
    public void Bind(Enemy boss)
    {
        _boss = boss;
        _maxHealth = boss.maxHealth;
        _maxPoise = boss.maxPoise;

        // 确保 Fill Image 有源贴图（Filled 类型无 Sprite 时可能不渲染）
        // 优先使用 Inspector 中已拖入的 Sprite，仅在没有时创建默认白色贴图
        EnsureFillSprite(healthFill, new Color(0.85f, 0.15f, 0.15f));
        EnsureFillSprite(poiseFill, new Color(0.9f, 0.7f, 0.15f));

        if (bossNameText != null)
            bossNameText.text = boss.enemyName;

        boss.OnDeath += OnBossDeath;

        gameObject.SetActive(true);
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        Debug.Log($"[BossHealthUI] Bind: {boss.enemyName}, hp={boss.currentHealth}/{_maxHealth}, " +
                  $"poise={boss.currentPoise}/{_maxPoise}");
    }

    private static void EnsureFillSprite(Image img, Color defaultColor)
    {
        if (img == null) return;
        if (img.sprite == null)
        {
            if (_whiteSprite == null)
            {
                var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                var pixels = new Color[16];
                for (int i = 0; i < 16; i++) pixels[i] = Color.white;
                tex.SetPixels(pixels);
                tex.Apply();
                _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
                _whiteSprite.hideFlags = HideFlags.DontSave;
            }
            img.sprite = _whiteSprite;
            img.color = defaultColor;
        }
    }

    /// <summary>
    /// 按 Boss 类型设置血条颜色（变体支持）
    /// </summary>
    public void SetStyle(Color healthColor, Color poiseColor)
    {
        if (healthFill != null) healthFill.color = healthColor;
        if (poiseFill != null) poiseFill.color = poiseColor;
    }

    private void Update()
    {
        if (_boss == null || _boss.state == EnemyState.Dead) return;

        if (healthFill != null)
        {
            float target = _maxHealth > 0f ? _boss.currentHealth / _maxHealth : 0f;
            if (!Mathf.Approximately(healthFill.fillAmount, target))
            {
                healthFill.fillAmount = target;
                healthFill.SetVerticesDirty();
            }
        }

        if (poiseFill != null)
        {
            // 架势恢复中优先用时间进度（不受击飞等状态切换影响），否则用 currentPoise
            float recoveryProgress = _boss.stunRecoveryProgress;
            float target = recoveryProgress < 1f
                ? recoveryProgress
                : (_maxPoise > 0f ? _boss.currentPoise / _maxPoise : 0f);
            if (!Mathf.Approximately(poiseFill.fillAmount, target))
            {
                poiseFill.fillAmount = target;
                poiseFill.SetVerticesDirty();
            }
        }

        // 每秒输出一次诊断日志
        _frameCounter++;
        if (_frameCounter % 60 == 0)
        {
            Debug.Log($"[BossHealthUI] Poll: {_boss.enemyName}, hp={_boss.currentHealth:F0}/{_maxHealth:F0} fill={healthFill?.fillAmount:F3}, " +
                      $"poise={_boss.currentPoise:F0}/{_maxPoise:F0} fill={poiseFill?.fillAmount:F3}, " +
                      $"bossState={_boss.bossState}, EnemyState={_boss.state}");
        }
    }

    private void OnBossDeath(Enemy enemy)
    {
        if (_boss != null)
        {
            _boss.OnDeath -= OnBossDeath;
            _boss = null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0f, 0.5f).SetUpdate(UpdateType.Normal, true).OnComplete(() => Destroy(gameObject));
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (_boss != null)
        {
            _boss.OnDeath -= OnBossDeath;
            _boss = null;
        }
    }
}
