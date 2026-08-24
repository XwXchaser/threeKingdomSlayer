---
id: kd_8564ed43-4dc8-461e-944e-03b1884215ce
injectMode: inherit
summary: DamageNumber TMP 动态创建的常见坑：_FaceColor=black 导致文字全黑、OUTLINE_ON 默认开启、tmp.text 设置顺序影响材质属性、粗体权重配置、colorOverride 传递链。
aiEditMode: inherit
---

# DamageNumber TMP 常见问题排查

## 1. 动态 AddComponent<TMP> 的 _FaceColor=black

**现象**：无论 `tmp.color` 设什么色，文字始终全黑。

**根因**：`AddComponent<TextMeshPro>()` 创建时使用字体默认材质。LibreationSans SDF 的默认材质 `_FaceColor = (0,0,0,1)` 黑色。
TMP Mobile/Distance Field shader 公式：`faceColor = vertexColor × _FaceColor`，黑色乘任何色 = 黑色。

**修复**：`mat.SetColor("_FaceColor", Color.white)`

**关键**：必须在 `tmp.color = displayColor` **之后**设置，因为 `tmp.color` setter 可能触发 `SetMaterialDirty()` 重读共享材质覆盖掉之前的设置。

## 2. 默认材质自带 OUTLINE_ON

**现象**：明明设了 `outlineWidth=0`，文字仍有黑色描边。

**根因**：默认材质可能已启用 `OUTLINE_ON` shader keyword。之前只在 `outlineWidth>0` 时 `EnableKeyword`，从没显式 `DisableKeyword`。

**修复**：
```csharp
if (outlineWidth > 0f)
    mat.EnableKeyword("OUTLINE_ON");
else
    mat.DisableKeyword("OUTLINE_ON");
```

## 3. tmp.text 设置顺序

**关键顺序**（在 `Show()` 中）：
1. `tmp.text = "..."` — 触发 TMP 网格生成
2. `tmp.outlineWidth = ...` — TMP 组件属性
3. `tmp.color = displayColor` — 顶点色
4. `mat.SetColor("_FaceColor", ...)` — **最后**，覆盖可能的材质重置
5. `mat.SetFloat("_WeightBold", ...)` — 粗体权重
6. `EnableKeyword/DisableKeyword("OUTLINE_ON")` — 描边开关

## 4. 粗体

- `tmp.fontStyle = FontStyles.Bold` — 开启粗体标志
- `mat.SetFloat("_WeightBold", value)` — 控制粗体强度（默认 0.75）
- 参数开放为 `[SerializeField] float boldWeight` 方便调整

## 5. colorOverride 传递链

调用链：`AttackWave.Create()` → `damageNumberColor` 参数（默认 `null`）→ `CreateInternal` 赋值 → `HitTarget` → `Enemy.TakeDamage(damageNumberColor)` → `DamageNumberManager.Spawn(colorOverride)` → `DamageNumber.Show(colorOverride)`

`colorOverride ?? textColor`：null 时走默认 `textColor`，非 null 时覆盖。

注意：`AttackWave.Create()` 的所有调用点都没有传 `damageNumberColor`，导致始终为 null。如需按伤害类型着色，在 `CreateInternal` 中加 `damageNumberColor ??= GetColor(damageType)`。
