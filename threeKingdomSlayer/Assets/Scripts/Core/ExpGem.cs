using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 经验宝石 — 屏幕空间 UI Image，飞向经验条 Fill 右端后触发经验收集
/// </summary>
public class ExpGem : MonoBehaviour
{
    [System.NonSerialized] public float expAmount;
    [System.NonSerialized] public float speed;
    [System.NonSerialized] public Vector3 targetPosition; // 屏幕空间
    [System.NonSerialized] public System.Action<ExpGem> onArrived;

    private RectTransform _rectTransform;
    private Image _image;
    private bool _collected;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _image = GetComponent<Image>();
    }

    private void Update()
    {
        if (_collected) return;

        Vector3 dir = targetPosition - _rectTransform.position;
        float step = speed * Time.deltaTime;
        if (dir.magnitude <= step)
        {
            _collected = true;
            if (onArrived != null)
                onArrived(this);
            else
                ExpGemManager.Instance?.OnGemArrived(this);
            Destroy(gameObject);
            return;
        }

        _rectTransform.position += dir.normalized * step;
    }

    public void SetVisual(Sprite sprite, Color color)
    {
        if (_image != null)
        {
            _image.sprite = sprite;
            _image.color = color;
        }
    }
}
