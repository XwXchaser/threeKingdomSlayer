using UnityEngine;

/// <summary>
/// 脱手后的独立盾牌：只在生成时读取敌人的姿态，随后自行旋转、下落并销毁。
/// 不作为敌人子物体，不跟随敌人的补齐、击飞或死亡变形；使用缩放时间以配合游戏暂停。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public sealed class ShieldDropEffect : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("独立掉落（世界单位，沿脱手时的精灵平面）")]
    public Vector2 initialVelocity = new Vector2(-2.3f, -0.4f);
    [Min(0f)] public float gravity = 13f;
    public float angularSpeed = -150f;
    [Min(0.05f)] public float lifetime = 0.85f;

    private Vector3 _startPosition;
    private Quaternion _startRotation;
    private Vector3 _right;
    private Vector3 _up;
    private float _elapsed;
    private bool _playing;

    public void Begin(SpriteRenderer source, Vector3 sourceLocalPosition)
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // 作为敌人的同级对象保留战场坐标系，但不继承敌人随后发生的移动和形变。
        Transform sourceTransform = source.transform;
        transform.SetParent(sourceTransform.parent, false);
        transform.localScale = sourceTransform.localScale;
        transform.SetPositionAndRotation(sourceTransform.TransformPoint(sourceLocalPosition), sourceTransform.rotation);
        spriteRenderer.sortingLayerID = source.sortingLayerID;
        spriteRenderer.sortingOrder = source.sortingOrder;
        spriteRenderer.flipX = source.flipX;
        spriteRenderer.flipY = source.flipY;
        Color color = source.color;
        // Enemy 已为本体分配独立材质；读取 sharedMaterial 即可，避免此处再次实例化。
        Material sourceMaterial = source.sharedMaterial;
        if (sourceMaterial != null && sourceMaterial.HasProperty("_Color"))
            color.a *= sourceMaterial.color.a;
        spriteRenderer.color = color;

        _startPosition = transform.position;
        _startRotation = transform.rotation;
        _right = sourceTransform.right;
        _up = sourceTransform.up;
        if (source.flipX)
            _right = -_right;
        _elapsed = 0f;
        _playing = true;
    }

    private void Update()
    {
        if (!_playing)
            return;

        _elapsed += Time.deltaTime;
        if (_elapsed >= lifetime)
        {
            _playing = false;
            Destroy(gameObject);
            return;
        }

        Vector3 velocity = _right * initialVelocity.x + _up * initialVelocity.y;
        transform.position = _startPosition + velocity * _elapsed - _up * (0.5f * gravity * _elapsed * _elapsed);
        transform.rotation = _startRotation * Quaternion.Euler(0f, 0f, angularSpeed * _elapsed);
    }

    private void OnDisable()
    {
        _playing = false;
    }
}
