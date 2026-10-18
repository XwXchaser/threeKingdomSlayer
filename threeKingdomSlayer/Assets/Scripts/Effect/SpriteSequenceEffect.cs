using UnityEngine;
using DG.Tweening;

/// <summary>
/// 序列帧播放器：把美术给的帧序列（Sprite 数组）按固定帧率播一遍，可选淡出后自毁。
/// 用法（美术/策划可直接在 Prefab 上配）：
///   1. 新建空物体，挂本组件；
///   2. frames 按顺序拖入序列帧（0 → N）；
///   3. fps 填帧率（破冰建议 12，自然解冻建议 10）；
///   4. 破冰/解冻特效保持 destroyOnFinish = true。
/// 不依赖 Animator，也不需要 AnimationClip，方便逐帧替换。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteSequenceEffect : MonoBehaviour
{
    [Tooltip("序列帧，按播放顺序拖入")]
    public Sprite[] frames;

    [Tooltip("播放帧率（帧/秒）")]
    [Min(1f)] public float fps = 12f;

    [Tooltip("循环播放（一般关闭）")]
    public bool loop;

    [Tooltip("播完自毁")]
    public bool destroyOnFinish = true;

    [Tooltip("播完后整体淡出时长（秒），0 = 不淡出")]
    [Min(0f)] public float fadeOutSeconds = 0.08f;

    [Tooltip("整体缩放倍率")]
    [Min(0.01f)] public float startScale = 1f;

    [Tooltip("是否使用不受 timeScale 影响的时间（暂停/卡肉时仍在播）")]
    public bool useUnscaledTime = false;

    private SpriteRenderer _renderer;
    private float _timer;
    private int _index = -1;
    private bool _finished;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        if (_renderer == null) _renderer = gameObject.AddComponent<SpriteRenderer>();
        transform.localScale = Vector3.one * startScale;
    }

    private void OnEnable()
    {
        _index = -1;
        _timer = 0f;
        _finished = false;
        Advance();
    }

    private void Update()
    {
        if (_finished || frames == null || frames.Length == 0) return;

        _timer += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float frameTime = 1f / Mathf.Max(1f, fps);
        while (_timer >= frameTime)
        {
            _timer -= frameTime;
            if (!Advance()) return;
        }
    }

    private bool Advance()
    {
        if (frames == null || frames.Length == 0)
        {
            Finish();
            return false;
        }

        _index++;

        if (_index >= frames.Length)
        {
            if (loop)
            {
                _index = 0;
            }
            else
            {
                Finish();
                return false;
            }
        }

        _renderer.sprite = frames[_index];
        return true;
    }

    private void Finish()
    {
        if (_finished) return;
        _finished = true;

        if (fadeOutSeconds > 0f && _renderer != null)
        {
            _renderer.DOFade(0f, fadeOutSeconds)
                .SetTarget(gameObject)
                .SetUpdate(useUnscaledTime)
                .OnComplete(() => { if (destroyOnFinish) Destroy(gameObject); });
            if (destroyOnFinish) Destroy(gameObject, fadeOutSeconds + 0.05f);
        }
        else if (destroyOnFinish)
        {
            Destroy(gameObject);
        }
    }
}
