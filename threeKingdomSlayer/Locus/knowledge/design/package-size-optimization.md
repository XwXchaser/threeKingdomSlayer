---
id: kd_d2fd0160-9f84-4843-809c-379b5aa11ee8
type: design
path: package-size-optimization.md
title: package-size-optimization
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1778402393080
updatedAt: 1778402393081
---

# package-size-optimization

## Summary
Android包体优化方案 — 当前50MB，目标8-12MB

## Content
# Android 包体优化方案

> 当前包体：~50MB | 目标：~8-12MB | 日期：2025-07

---

## 当前资产分布

| 资产 | 源大小 | 预估构建贡献 |
|------|--------|-------------|
| Unity 引擎基础 (IL2CPP/Mono) | - | ~22-27 MB |
| **方正粗黑宋简体 SDF.asset** | 37.1 MB | ~37 MB |
| 方正粗黑宋简体.ttf | 2.8 MB | ~2.8 MB |
| TMP Examples & Extras 整目录 | 9.4 MB | ~8-9 MB |
| TMP LiberationSans SDF.asset | 2.3 MB | ~2.3 MB |
| Sprites (15张PNG/JPG) | 7.3 MB | ~5-7 MB |
| DOTween DLLs + 模块 | 0.8 MB | ~0.5 MB |
| 其余(脚本/场景/Prefab/Material) | <1 MB | <1 MB |

---

## 优化任务清单

### 任务1：重建中文SDF字体（优先级最高，预计节省 36MB+）

**问题**：`Assets/Fonts/方正粗黑宋简体 SDF.asset` 37.1MB，CJK字体全部字符渲染进SDF Atlas。

**方案**：
- 用 TMP Font Asset Creator 重新生成
- Atlas Resolution: 1024×1024 或 2048×2048
- **Character Set: Custom Characters** — 只填入游戏中实际使用的汉字
- 预估字符量：菜单/UI 文本约 100-500 字
- 目标：SDF.asset ≤ 500KB

**验证**：重新打包后确认字体渲染正常，包体下降 35MB+

---

### 任务2：删除 TMP Examples & Extras（优先级高，预计节省 8-9MB）

**问题**：`Assets/TextMesh Pro/Examples & Extras/` 包含 25 个示例场景、5 个示例字体、15 个示例纹理、694KB PDF 文档，全部被打进构建。

**方案**：
- 直接删除整个 `Assets/TextMesh Pro/Examples & Extras/` 目录
- 如果需要参考，移到项目外部或 `Editor/` 目录

**验证**：构建后确认 TMP 基础功能（`Resources/Fonts & Materials/`、`Shaders/`）不受影响

---

### 任务3：纹理压缩优化（优先级高，预计节省 3-5MB）

**问题**：Sprites 目录 7.3MB，大量 RGBA32 未压缩原图。

| 文件 | 大小 | 建议 |
|------|------|------|
| stab.png | 1.24 MB | Max Size 1024, Crunch (ETC2) |
| background.png | 1.02 MB | Max Size 1024, Crunch (ETC2) |
| backgroun_battle.png | 987 KB | Max Size 1024, Crunch (ETC2) |
| slash.png | 823 KB | Max Size 512, Crunch (ETC2) |
| zhangfei_idle_1~4.png | 640KB×4=2.5MB | Max Size 512, Crunch (ETC2) |
| Enemy_1/2.png | 310+283KB | Max Size 512, Crunch (ETC2) |

**方案**：
- 逐张设置合适的 Max Size（特效512、角色512、背景1024）
- Android 平台用 **RGBA Crunched ETC2** 或 **ASTC 6×6**
- 不需要透明通道的图（背景、菜单）改用 RGB Crunched ETC2

---

### 任务4：清理重复/多余字体（优先级中，预计节省 2-3MB）

**问题**：
- `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset` (2.3MB) — 如果游戏只用中文字体，可移除
- `Assets/Fonts/LiberationSans.ttf` (350KB) 与 `Assets/TextMesh Pro/Fonts/LiberationSans.ttf` (350KB) 重复

**方案**：
- 确认英文/数字字体需求，只保留一份
- 如果不使用 LiberationSans，删除两个位置的文件及 SDF

---

### 任务5：DOTween 模块裁剪（优先级低，预计节省 ~200KB）

**问题**：`Assets/Plugins/Demigiant/DOTween/` 有 9 个模块脚本，未全部使用。

**方案**：
- 检查代码中 `using DG.Tweening` 的实际 API 调用
- 删除未使用的模块脚本（如 DOTween.Audio、DOTween.Physics 等）

---

### 任务6：构建设置检查（优先级中）

- **Stripping Level**: 确认 Managed Stripping Level 不是 Disabled
- **IL2CPP**: 如果当前用 Mono，切换 IL2CPP 可减小运行时
- **Engine Code Stripping**: 启用
- **Remove unused shader variants**: 如果 Build Settings 有此选项则开启

---

## 预估效果

| 阶段 | 包体 |
|------|------|
| 当前 | ~50 MB |
| 任务1: 重建中文SDF | ~14 MB |
| + 任务2: 删TMP Examples | ~6 MB |
| + 任务3: 纹理压缩 | ~6 MB |
| + 任务4: 清重复字体 | ~4 MB |
| **预估合计** | **~8-12 MB** |

---

## 注意事项

- 执行任务1后必须在游戏中验证所有中文文本渲染正常
- 任务2删除 TMP Examples 后，TMP 核心 Shaders/Resources 务必保留
- 纹理压缩后检查画质是否符合预期（尤其角色和特效）
- 每次改动后建议出包验证，避免累积问题
