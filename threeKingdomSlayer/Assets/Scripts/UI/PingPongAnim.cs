using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 序列帧乒乓动画。默认在两帧间微呼吸，随机触发眨眼走完整循环。
/// 支持 SpriteRenderer（世界空间）和 UI Image（Canvas）。
/// </summary>
public class PingPongAnim : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float fps = 8f;
    [SerializeField] private bool playOnStart = true;
    [Tooltip("每秒触发眨眼的概率")]
    [SerializeField] private float blinkChancePerSecond = 0.3f;

    private SpriteRenderer sr;
    private Image img;
    private float timer;
    private int index;
    private int direction = 1;
    private bool blinking;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        img = GetComponent<Image>();
    }

    private void Start()
    {
        if (!playOnStart) enabled = false;
    }

    private void OnEnable()
    {
        timer = 0f;
        index = 0;
        direction = 1;
        blinking = false;
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0) return;

        timer += Time.deltaTime;
        float interval = 1f / fps;

        // Random blink trigger
        if (!blinking && frames.Length >= 4)
        {
            if (Random.value < blinkChancePerSecond * Time.deltaTime)
            {
                blinking = true;
                index = 1;
                direction = 1;
                timer = 0f;
            }
        }

        while (timer >= interval)
        {
            timer -= interval;
            AdvanceFrame();
        }
    }

    private void AdvanceFrame()
    {
        if (blinking)
            AdvanceBlink();
        else
            AdvanceIdle();
    }

    private void AdvanceIdle()
    {
        index = index == 0 ? 1 : 0;
        ApplySprite(frames[index]);
    }

    private void AdvanceBlink()
    {
        index += direction;

        if (index >= frames.Length - 1)
        {
            index = frames.Length - 1;
            direction = -1;
        }
        else if (index <= 0)
        {
            index = 0;
            direction = 1;
            blinking = false;
        }

        ApplySprite(frames[index]);
    }

    private void ApplySprite(Sprite s)
    {
        if (sr != null) sr.sprite = s;
        if (img != null) img.sprite = s;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (img == null) img = GetComponent<Image>();
        if (frames != null && frames.Length > 0 && index < frames.Length)
            ApplySprite(frames[index]);
    }
#endif
}
