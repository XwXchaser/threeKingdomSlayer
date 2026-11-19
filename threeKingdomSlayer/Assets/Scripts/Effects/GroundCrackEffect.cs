using DG.Tweening;
using UnityEngine;

/// <summary>
/// **砸地裂纹特效**（抡斧用）：命中帧在斧刃入地点生成，裂纹**从中心向外扩散**后淡出。
///
/// 结构：SpriteRenderer（贴图中心 = 命中点）+ 材质 `Custom/GroundCrackReveal`（_Reveal 径向显现）。
/// 时序：显现 0.16s（OutCubic）→ 顶住 0.12s → 淡出 0.22s（InQuad），结束自毁。
/// 规格与美术需求见 `design/axe-ground-crack-vfx-spec-20261115.md`。
/// </summary>
[DisallowMultipleComponent]
public sealed class GroundCrackEffect : MonoBehaviour
{
    [SerializeField] private SpriteRenderer crackRenderer;
    [SerializeField] private Material revealMaterial;

    [Header("时序")]
    [SerializeField] private float revealSeconds = 0.16f;
    [SerializeField] private float holdSeconds = 0.12f;
    [SerializeField] private float fadeSeconds = 0.22f;

    [Header("形态")]
    [Tooltip("2.5D 地面上平铺的裂纹要竖向压扁（相机俯角 18° ⇒ sin18°≈0.31）。")]
    [SerializeField] private float verticalSquash = 0.35f;
    [SerializeField] private float randomRotationDegrees = 8f;

    /// <summary>在指定世界位置生成并播放。</summary>
    /// <param name="worldPosition">命中点（贴图中心）。</param>
    /// <param name="worldDiameter">裂纹的世界直径（贴图覆盖的地面宽度）。</param>
    public void Play(Vector3 worldPosition, float worldDiameter)
    {
        if (crackRenderer == null) crackRenderer = GetComponentInChildren<SpriteRenderer>();
        if (crackRenderer == null || crackRenderer.sprite == null)
        {
            // ★ 贴图还没到位 ⇒ 什么都不显示。
            //   否则材质的 _MainTex 为空 ⇒ Unity 绑定默认白贴图 ⇒ 命中点会出现一块**白盘**，
            //   看起来像“武器材质丢失变灰模”（2026-11-16 实测踩到）。
            Debug.LogWarning("[GroundCrackEffect] 缺少裂纹贴图（Sprite 未赋值）⇒ 本次不显示");
            crackRenderer.enabled = false;   // 先关渲染器（Destroy 要到帧末才生效，避免闪出一帧）
            Destroy(gameObject);
            return;
        }

        transform.position = worldPosition;
        transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-randomRotationDegrees, randomRotationDegrees));

        // 材质实例（每个特效一份，避免互相污染 _Reveal/_Fade）
        Material mat = revealMaterial != null ? revealMaterial : crackRenderer.material;
        if (mat == null) { Destroy(gameObject); return; }
        crackRenderer.material = mat;
        mat.SetFloat("_Reveal", 0f);
        mat.SetFloat("_Fade", 1f);

        // 尺寸：sprite 原生尺寸（像素/PPU）→ 目标世界直径；竖向再压扁
        float nativeWidth = crackRenderer.sprite != null ? crackRenderer.sprite.bounds.size.x : 1f;
        float s = nativeWidth > 0.0001f ? worldDiameter / nativeWidth : 1f;
        transform.localScale = new Vector3(s, s * verticalSquash, s);

        var seq = GameplayPause.Bind(DOTween.Sequence());
        seq.SetTarget(this);
        seq.Append(DOTween.To(() => mat.GetFloat("_Reveal"), v => mat.SetFloat("_Reveal", v),
            1f, revealSeconds).SetEase(Ease.OutCubic));
        seq.AppendInterval(holdSeconds);
        seq.Append(DOTween.To(() => mat.GetFloat("_Fade"), v => mat.SetFloat("_Fade", v),
            0f, fadeSeconds).SetEase(Ease.InQuad));
        seq.OnComplete(() => Destroy(gameObject));
        seq.OnKill(() => Destroy(gameObject));
    }
}
