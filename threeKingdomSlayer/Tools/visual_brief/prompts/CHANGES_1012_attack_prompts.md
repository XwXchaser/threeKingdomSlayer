# 1012 attack 提示词修订记录

> 对应交接文档：`Locus/knowledge/plan/enemy1012-art-replacement-handoff.md` §5
> 比对基准（**已过闸门的原词**）：`Library/Locus/tmp/enemy1012_E2_expression_arc.py`（E2 全身）
> 与 `Library/Locus/tmp/enemy1012_E1_expression_arc.py`（E1 头部研究片）

---

## 2026-11-05 · E2' v1beat 版三处「钉脸」缺陷修正

**依据**：`enemy1012-bandit-idle-animation-handoff.md` §0.4.2（v3 失败根因）/ §0.4.3（v4 成功三改法）

v3 失败的第一根因是「两处完全保持把脸锁成静态」，第二根因是「一串针对脸的『绝不』等于告诉模型脸不要动」。
E2' v1beat 版虽然按 §5 声明「负向词只留四条」，**但正文里实际留了六条**，且保留列表里有一处把脸整体打包。

### 缺陷与修正

| # | 缺陷 | 原文（缺陷版） | 修正后 | 依据 |
|---|---|---|---|---|
| 1 | 第五条面部禁令 | `and he must never droop his eyelids` | **删除** | §0.4.3 ②：v4 成功词**明确删掉**这一条，改为「眼睛全程保持窄而冷」；已过闸门的 E1/E2 原词均无此句 |
| 2 | keep 列表把脸整体打包 | `...the leather chin strap, his face, the hemp tunic, ... exactly as they are in image 1` | 改为 `Keep everything else exactly as in image 1: the iron helmet..., the narrow long bandit face, the high cheekbones, the hollow cheeks, the pointed forward chin, the heavy brows, the sallow yellow-olive skin, the sparse crooked wispy mustache, the stray chin stubble, the same skin brightness, the hemp tunic, ...` | §0.4.2 ①：成功版 E1/E2 的 keep 列表**逐项列举物理特征**，从不说 `his face` 整体 |
| 3 | 结尾又一条面部禁令 | `no new equipment and no change to his face or helmet` | `no new equipment and no change to his helmet, his clothing or his weapon` | 同 #1：面部「绝不」一律不进负向词 |

### 修正后与已过闸门的 E2 原词逐句比对

```
成功版 E2（闸门通过）:
  • He must never look tired, sleepy, drowsy, weary, exhausted, bored, sad or blank.
  • Keep everything else exactly as in image 1: the iron helmet with the short red plume, ...
修正后 E2':
  • He must never look tired, sleepy, drowsy, weary, exhausted, bored, sad or blank.
  • Keep everything else exactly as in image 1: the iron helmet with the short red plume, ...
```

面部约束句**结构与成功版完全一致**；差异只剩 attack 特有的身体动作与刀身描述。

### 校验

- 残留检查：`grep -c "droop his eyelids\|no change to his face\|strap, his face,"` → **0**
- 提示词长度：3942 → **4149** 字符
- `submit_seedance.py --dry-run` 通过（参考图 sha `75c780df…`）

### E1' 头部片复查结论：**无需修改**

`1012_attack_arc_E1_head.txt` 的负向词止于 `bored, sad or blank`，keep 列表逐项列举物理特征，结尾无「脸不许变」句 —— **与已过闸门的 E1 原词同构**，可直接提交。

---

## 参考：两条 v3 失败写法（不要回退）

```
✗ Keep his expression exactly as in image 1 and keep it cold and calculating for the whole loop
✗ Keep his iron helmet ..., his face, ... exactly as they are in image 1
✗ ... no change to his face or helmet
✗ never droop his eyelids
```

> 实测：`enemy1012_bandit_idle_v6.mp4` 用第一条生成 → 弧线 rise 仅 369、面部全程冻结浅笑；
> `enemy1012_bandit_idle_v3.mp4` 用「保持并让坏笑加深」生成 → 嘴部序列 18–39 无趋势、峰值在 f0。
