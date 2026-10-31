---
id: kd_611ff1c7-63b0-41d0-8649-0437525c7d6b
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_tap_hand_frames_gen_20261029
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
summary: tap 手部动作帧的生成方案（4 帧、256×256、指尖对齐），含中英文提示词与负面提示词 —— 待用户批准后才生成。
---

# tap 手部动作帧：生成方案（2026-10-29，待批准）

> 需求来源：用户 10-29「tap 引导手没有动作，只有光圈，需要制作几帧手的动作帧表达点击」。
> 本文 = **方案与提示词**。按 `memory/user-preference.md` 的「图片生成前置审批」规矩，
> **未获明确批准前不调用任何生图接口**。

## 1. 目标与复用

- **要解决**：教学提示的 tap 动作现在只是"手整体缩放 + 光圈扩散"，读不出"点"。
- **一帧两用**：
  1. 教学 tap 拍（点一下 = 戳击）；
  2. **防御键提示**（手移到画面中央的防御按钮上按下去）—— 与用户"防御按键也要教"直接相关。
- **已经先做了一步（不依赖新美术）**：把"缩放"改成**绕指尖 pivot 旋转 14°**
  （`TutorialGestureHint.tapPressAngle`）—— 因为 `Finger` 节点的 pivot 本来就在指尖 (0.26, 0.81)，
  旋转时指尖不动、只有手在压，已经比原来的缩放像"按下去"得多。
  **帧序列到位后自动接管**（`tapFrames` 非空时优先用帧）。

## 2. 素材规格（硬约束）

| 项 | 值 |
|---|---|
| 帧数 | **4 帧**（抬起 / 下压 / 触底 / 回弹） |
| 画布 | **256×256 RGBA**，与现有 `tutorial_gesture_finger_ui.png` 同尺寸 |
| 背景 | **完全透明**（有 alpha 通道） |
| **指尖位置** | 每帧必须落在 **x≈67, y≈208**（图像坐标，y 自上 ≈48）→ 归一化 **(0.262, 0.813)**；prefab 的 `Finger.pivot` 正是 (0.26, 0.81) |
| 朝向 | 与参考图一致：食指指向**左上**，其余指蜷起，手腕有黄铜护腕 |
| 导入 | Sprite Single / PPU 100 / alphaIsTransparency / 无 mipmap / Point / Clamp（与 `hud_orb_circle.png` 对齐） |
| 落地路径 | `Assets/Sprites/UI/Tutorial/tutorial_gesture_tap_f1..f4.png` |

> **指尖对齐是硬指标**：pivot 在指尖 → 帧与帧之间指尖漂一点点，播放时就会看到"手在跳"。

## 3. 生成方式与参数

- 方式：**图生图 / 编辑**（参考图 = 现有 `tutorial_gesture_finger_ui.png`，用途 = **锁定手型/朝向/护腕/光影**）
- `quality=max`、`input_fidelity=high`、`size=1024x1024`、`background=transparent`、`output_format=png`
- **逐帧单独生成**（4 次请求），不要一次出 4 宫格 —— 宫格切出来的帧指尖必然对不齐
- 生成后落地时缩到 256×256，并跑**指尖对齐校验**（见 §6）

## 4. 中文提示词

### 4.1 基准描述（每帧都带）

```
保持参考图中同一只手的手型、手势朝向、肤色、金属护腕样式与整体光影风格完全一致。
画面内容：一只右手，食指伸出并指向画面左上方，其余四指蜷起成半握拳，手腕处戴一圈黄铜色金属护腕，
护腕上有简单的錾刻纹样。手掌与手腕在画面中央偏下，指尖位于画面左上区域，整只手完整可见不被裁切。
纯透明背景，没有任何投影、地面、桌面或环境元素；画面里没有文字、图标、边框或水印。
```

### 4.2 每帧的动作追加语

| 帧 | 追加语（中文） |
|---|---|
| F1 抬起 | `此刻食指伸直、手掌自然抬起，处在按键前的静止姿态。` |
| F2 下压 | `此刻食指以**指尖为支点**向下旋转约 6 度（像按下一个按钮的起始），手掌其余部分保持与参考图一致。` |
| F3 触底 | `此刻食指以**指尖为支点**向下旋转约 13 度（按到底），指尖位置与参考图完全重合，手掌其余部分保持不变。` |
| F4 回弹 | `此刻食指以指尖为支点向下旋转约 6 度并略微抬起的中间姿态（回弹途中），指尖位置与参考图完全重合。` |

### 4.3 负面提示词（中文）

```
不要出现：第二只手、左手、额外的手指、缺失或畸形的手指、手套、卡通描边、扁平矢量风、
背景、投影、地面、桌面、风景、道具、文字、标语、logo、水印、边框、
被裁切的手指、被裁切的手腕、模糊、低分辨率、噪点、彩色描边、夸张的透视变形
```

## 5. English prompts

### 5.1 Base description (same for every frame)

```
Keep the exact same hand shape, gesture orientation, skin tone, brass wrist-guard style and lighting of the reference image.
Content: a right hand with the index finger extended and pointing toward the upper-left of the frame, the other four fingers curled into a loose fist, wearing a brass metal wrist guard with simple engraved patterns. The palm and wrist sit slightly below center, the fingertip is in the upper-left area, and the whole hand is fully visible and uncropped.
Pure transparent background, no shadow, no ground, no desk, no environment elements, no text, no icons, no border, no watermark.
```

### 5.2 Per-frame action suffix

| Frame | Suffix (English) |
|---|---|
| F1 raised | `The index finger is straight and the hand is lifted in a neutral ready pose, before pressing.` |
| F2 pressing | `The index finger is rotated downward by about 6 degrees **around the fingertip as the pivot**, as if starting to press a button; the rest of the hand stays identical to the reference.` |
| F3 pressed | `The index finger is rotated downward by about 13 degrees **around the fingertip as the pivot**, fully pressed; the fingertip stays exactly where it is in the reference and the rest of the hand is unchanged.` |
| F4 rebound | `The index finger is rotated downward by about 6 degrees around the fingertip as the pivot and is starting to lift again (mid rebound); the fingertip stays exactly where it is in the reference.` |

### 5.3 Negative prompt (English)

```
second hand, left hand, extra fingers, missing or deformed fingers, gloves, cartoon outline style, flat vector art,
background, drop shadow, ground, desk, scenery, props, text, captions, logo, watermark, border,
cropped fingers, cropped wrist, blurry, low resolution, noise, colored outline, exaggerated perspective distortion
```

## 6. 落地与验收

1. 生成 4 张 → 落到 `Assets/Sprites/UI/Tutorial/tutorial_gesture_tap_f1..f4.png`
2. **指尖对齐校验**（程序化，不靠肉眼）：
   每帧 alpha 包围盒的**最上端中点**必须落在 x∈[64,70]、y∈[205,211]（图像坐标 y 自上）
   → 与基准帧 `tutorial_gesture_finger_ui.png` 的实测值 (67, 208) 对齐；超差就重做那一帧。
3. 导入设置对齐 `hud_orb_circle.png`（Single / PPU100 / alphaIsTransparency / 无 mipmap / Point）
4. 填进 `TutorialGestureHint.tapFrames`（**预制体 + 场景实例两边都要**，与任务 1 的坑一致）→ `tapFrameInterval` 建议 0.07~0.09
5. **验收**：
   - ① tap 提示能一眼读成"手指按下去"（不是"手在抖"）；
   - ② 播放全程**指尖不漂**；
   - ③ 光圈与指尖**同心**（缺陷 A 已修）；
   - ④ 防御键提示：手落在防御按钮上、反复点击，读成"点这里"；
   - ⑤ 手/光圈尺寸 = 308 / 336 canvas 单位（×1.4 后）。

## 7. 待你确认

- 帧数：**4 帧**（推荐）还是 **3 帧**（去掉"触底"）？
- 生成方式：逐帧单独生成（推荐，指尖能对齐）还是一次出 4 宫格（省事但对不齐）？
- 是否就用现有的手部贴图做参考图（推荐）？

> 批准后我按 §6 的流程执行并回读校验结果。
