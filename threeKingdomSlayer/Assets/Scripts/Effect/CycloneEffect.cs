using UnityEngine;
using DG.Tweening;

/// <summary>
/// 旋风效果 — 定时被动触发时每个受影响敌人脚下生成一个实例。
///
/// 生命周期：
///   Start → 对敌人造成击飞伤害 → Launch(自定义时长)
///   Update → 读敌人 Y 偏移 → 上升播 cyclone1-6 / 浮空循环 cyclone5-6
///   落地/死亡 → 落地伤害(若解锁) → Destroy
/// </summary>
public class CycloneEffect : MonoBehaviour
{
    [Header("Sprite 序列")]
    public Sprite cyclone1;
    public Sprite cyclone2;
    public Sprite cyclone3;
    public Sprite cyclone4;
    public Sprite cyclone5;
    public Sprite cyclone6;

    [Header("循环帧切换间隔")]
    public float loopFrameInterval = 0.15f;

    [Header("淡出")]
    public float fadeOutDuration = 0.25f;

    private Enemy _target;
    private int _damage;
    private float _landingDamagePercent;
    private SpriteRenderer _sr;

    private Sprite[] _allFrames; // index 0-5 = cyclone1-6
    private bool _wasRising;
    private float _loopTimer;
    private int _loopFrameIndex; // 0=cyclone5, 1=cyclone6
    private bool _landed;
    private bool _fadingOut;

    public void Setup(Enemy target, int damage, float landingDamagePercent, float knockupDuration)
    {
        _target = target;
        _damage = damage;
        _landingDamagePercent = landingDamagePercent;

        if (_target != null)
        {
            // 固定在地面位置（敌人脚下初始位置）
            Vector3 pos = _target.transform.position;
            pos.y = 0f;
            transform.position = pos;

            // 击飞伤害
            _target.TakeDamage(_damage);

            // 击飞（自定义时长）
            _target.Launch(knockupDuration);

            // 监听落地
            _target.OnLaunchedLanded += OnTargetLanded;
        }
    }

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (_sr == null)
            _sr = gameObject.AddComponent<SpriteRenderer>();

        _allFrames = new[] { cyclone1, cyclone2, cyclone3, cyclone4, cyclone5, cyclone6 };
    }

    private void Start()
    {
        // 默认显示第一帧
        if (_sr != null && cyclone1 != null)
            _sr.sprite = cyclone1;
    }

    private void Update()
    {
        // 敌人失效或已落地 → 淡出
        if (_target == null || _landed)
        {
            StartFadeOut();
            return;
        }

        if (_target.state == EnemyState.Dead)
        {
            StartFadeOut();
            return;
        }

        // 敌人不在击飞状态（可能被其他逻辑提前落地）→ 淡出
        if (_target.state != EnemyState.Launched)
        {
            StartFadeOut();
            return;
        }

        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        if (_sr == null || _allFrames == null) return;

        float maxY = _target.CurrentLaunchYHeight;
        if (maxY <= 0f) return;

        float currentY = _target.transform.localPosition.y - _target.LaunchStartLocalPos.y;
        bool rising = _target.IsLaunchRising;

        if (rising)
        {
            // 上升阶段：按高度比例映射 cyclone1-6
            float progress = Mathf.Clamp01(currentY / maxY);
            int frameIndex = Mathf.Min((int)(progress * 6f), 5);
            _sr.sprite = _allFrames[frameIndex];
            _wasRising = true;
        }
        else
        {
            // 浮空/下落阶段：循环 cyclone5-6
            _loopTimer -= Time.deltaTime;
            if (_loopTimer <= 0f)
            {
                _loopTimer = loopFrameInterval;
                _loopFrameIndex = (_loopFrameIndex + 1) % 2;
                _sr.sprite = _allFrames[4 + _loopFrameIndex]; // cyclone5(4) 或 cyclone6(5)
            }
            _wasRising = false;
        }
    }

    private void OnTargetLanded(Enemy enemy)
    {
        if (_landed) return;
        _landed = true;

        // 落地伤害
        if (_landingDamagePercent > 0f && _target != null && _target.state != EnemyState.Dead)
        {
            int landingDmg = Mathf.RoundToInt(_damage * _landingDamagePercent);
            if (landingDmg > 0)
                _target.TakeDamage(landingDmg);
        }

        // 取消监听
        if (_target != null)
            _target.OnLaunchedLanded -= OnTargetLanded;

        StartFadeOut();
    }

    private void StartFadeOut()
    {
        if (_fadingOut) return;
        _fadingOut = true;

        if (_sr != null)
            _sr.DOFade(0f, fadeOutDuration).OnComplete(() => Destroy(gameObject));
        else
            Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (_target != null)
            _target.OnLaunchedLanded -= OnTargetLanded;
    }
}
