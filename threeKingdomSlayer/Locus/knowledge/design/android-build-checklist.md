---
id: kd_7552ac95-7456-4497-9204-ac24173a3b31
type: design
path: android-build-checklist.md
title: android-build-checklist
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1781366098729
updatedAt: 1781406078726
---

# android-build-checklist

## Summary
Android 打包前按优先级分阶段检查清单，涵盖帧率、UI适配、性能、包体、输入优化。每次打包复用。

## Content
# Android 打包检查清单

> 基于 2026 年全项目评估，按优先级分阶段排列。每次打包前按此清单逐项检查。

---

## 阶段 1：帧率 & UI 适配（P0）✅ 已完成

### 1.1 设置帧率
- [x] 在启动脚本中设置 `Application.targetFrameRate = 60`
- 修复文件：`Assets/Scripts/Core/StageConfigManager.cs` → `Awake()` 末尾 `Application.targetFrameRate = 60;`

### 1.2 Canvas Scaler 匹配模式
- [x] `Assets/Scenes/Battle.scene` Canvas Scaler `m_MatchWidthOrHeight` 从 `0` 改为 `0.5`
- MainMenu 场景已是 `0.5`，无需修改

### 1.3 安全区适配
- [x] 添加 `Assets/Scripts/UI/SafeAreaAdapter.cs` 并挂载到 MainMenu+Battle Canvas
- 自动处理刘海屏和底部导航条偏移

---

## 阶段 2：性能优化（P1）✅ 已完成

### 2.1 中文字体 Shader
- [x] `Assets/Fonts/方正粗黑宋简体 SDF.asset` → `TextMeshPro/Mobile/Distance Field`
- [x] `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset` → 同上

### 2.2 OnRenderImage 优化
- [x] `Assets/Scripts/UI/CameraManager.cs` — `currentBlur <= 0.001f` 时 disable CameraManager

### 2.3 关闭纹理 Read/Write
- [x] fire_effect.png, ex_base.png, ex_fill.png, ex_frame.png — `isReadable` 改为 `0`

---

## 阶段 3：包体精简（P2）

### 3.1 排除 TMP Examples ✅
- [x] `Assets/TextMesh Pro/Examples & Extras/` 已删除

### 3.2 音频压缩质量 ✅
- [x] 全部 SFX WAV — Vorbis 质量 80%

### 3.3 大体积纹理 Crunch 压缩 ✅
- [x] stab.png, background.png, backgroun_battle.png, slash.png, zhangfei PNG, Enemy PNG
- 已覆写为 Android ETC2 Crunch

### 3.4 中文字体 SDF Atlas 重建
- [ ] `Assets/Fonts/方正粗黑宋简体 SDF.asset`（~37MB）→ 用实际使用字符集重建，目标 < 500KB
- 需先统计项目实际使用字符集，再通过 Font Asset Creator 重建

---

## 阶段 4：输入 & 交互优化（P3）✅ 已完成

### 4.1 滑动阈值 DPI 感知
- [x] `Assets/Scripts/Player/InputManager.cs` `swipeThreshold` 改为 `Mathf.Max(50f, Screen.dpi * 0.1f)`

### 4.2 震动反馈
- [x] 5 处 `Handheld.Vibrate()` 已添加：InputManager(触屏攻击)、QTEController(判定)、PlayerState(受伤/死亡)、AttackSystem(命中)、UltimateSystem(大招)

---

## 阶段 5：GC 优化（P0-P1）✅ 已完成

> 目标：消除 Android 上 1-2 秒 GC 卡顿

### 5.1 DOTween 生产配置
- [x] `Assets/Resources/DOTweenSettings.asset` → `useSafeMode: 0`, `logBehaviour: 2`, `defaultRecyclable: 1`

### 5.2 Debug.Log 条件编译
- [x] 新建 `Assets/Scripts/DebugLog.cs`（`[Conditional("UNITY_EDITOR")]`）
- [x] 热路径文件 `Debug.Log` → `DebugLog.Info`：ColumnManager.cs, Column.cs, Enemy.cs, InputManager.cs, QTEController.cs

### 5.3 每帧分配消除
- [x] `PlayerState.cs` — 缓存 `_cooldownKeysCache` + `_expiredBuffsCache` List（消除每帧 2 个 List 分配）
- [x] `BattleHUD.cs` — `static readonly AllAttackTypes` 数组（消除每帧 2 个数组分配）

### 5.4 战斗时分配消除
- [x] `ColumnManager.cs` — `GetAllEnemiesInRange` 复用 `_rangeQueryList`（消除每次攻击 1 个 List）
- [x] `ColumnManager.cs` — `RowBasedFillUp` 复用 `_occupiedRowsSet` HashSet（消除每次死亡 1 个 HashSet）
- [x] `Enemy.cs` — `static readonly DefaultRowAlphaFactors`（消除无配置时每帧 1 个 float[]）

### 5.5 ColumnManager 集合复用
- [x] 6 个复用容器字段：`_pushWorkList`, `_pushHitSet`, `_pushByColumn`, `_convOriginalRows`, `_convTargets`, `_convGroups`
- [x] `ApplyPushWave`, `ExecutePush`, `ApplyConvergenceWave` 全部复用

---

## 待完成

- [ ] 3.4 中文字体 SDF Atlas 重建（~37MB → < 500KB）

---

## 代码级架构速查

| 项目 | 说明 |
|------|------|
| Legacy Input | 使用 `Input` 类（非新 Input System），双鼠标/触摸路径，Android 兼容 |
| BIRP | Built-in Render Pipeline，无 SRP 依赖 |
| Shader | BlurEffect/EnemyOutline 用 CGPROGRAM（Unity 6+ 需迁移 HLSL） |
| SaveManager | 实际用 `PlayerPrefs`（注释写 `persistentDataPath` 但未使用） |
| DOTween | safeMode=Off, recyclable=On, logBehaviour=ErrorsOnly |
| 音频 API | 已从 Wwise 迁移至 Unity 原生 AudioSource |
| 字体 Shader | Mobile/Distance Field（不含 bevel/glow/specular） |

---

## 经验教训

- **绝不直接 `edit` .meta 文件**（Tuanjie 引擎加密 GUID，会触发重生成导致引用断裂）。纹理/音频等导入设置的修改必须通过 `unity_execute` API
- Roslyn `code_diagnostics` + `unity_recompile` 双重验证后再 commit
