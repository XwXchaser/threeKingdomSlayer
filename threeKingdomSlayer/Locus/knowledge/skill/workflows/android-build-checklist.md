---
id: kd_e0702843-8726-4dee-8b05-1d32a50e295f
injectMode: inherit
summary: Android 出包前逐项确认的检查清单（帧率与适配、性能与 GC、包体、输入与反馈、工程约定），每次打包前使用。
aiEditMode: inherit
skillEnabled: true
skillSurface: command
---

# Android 打包前检查清单

仅列**每次出包前需要逐项确认**的内容。已完成的历史修复与具体文件改动记录在 `memory/android-build-state.md`；包体方案与预估见 `plan/package-size-optimization.md`。

## 1. 帧率与分辨率

- [ ] 启动脚本设置 `Application.targetFrameRate = 60`（当前位于 `StageConfigManager.Awake()` 末尾）
- [ ] Battle 与 MainMenu 两个 Canvas 的 `CanvasScaler.matchWidthOrHeight` 均为 0.5
- [ ] 两个 Canvas 都挂有安全区适配组件（`SafeAreaAdapter`），刘海屏与底部导航条不遮挡 UI
- [ ] 嵌套 Overlay Canvas 都带 CanvasScaler（HeroHUD、Victory 面板是历史易漏点）

## 2. 性能与 GC

- [ ] 中文字体 SDF 材质使用 `TextMeshPro/Mobile/Distance Field`
- [ ] 热路径日志走条件编译封装（`DebugLog`），不直接调用 `Debug.Log`
- [ ] 攻击、补齐、每帧刷新的热路径复用集合，不产生每帧或每次攻击的 List/HashSet 分配
- [ ] `CameraManager` 在无模糊需求时自行关闭，避免空转 `OnRenderImage`
- [ ] 不需读写的纹理关闭 Read/Write

## 3. 包体

- [ ] 纹理按用途设 Max Size 并使用 Android 压缩：特效/角色 512、背景 1024，ETC2 Crunch 或 ASTC
- [ ] 无透明需求的图（背景、菜单）使用 RGB 压缩，避免浪费体积与带宽
- [ ] 中文字体只保留一份 SDF，且已按游戏实际字符集重建
- [ ] 未使用的 DOTween 模块与 TMP 示例资源不在构建内
- [ ] Build Settings 的 Managed Stripping、IL2CPP、Remove unused shader variants 状态符合预期

## 4. 输入与反馈

- [ ] 滑动阈值按 DPI 计算，在高 DPI 设备上不失效
- [ ] 震动反馈调用点齐全（触屏攻击、QTE 判定、受伤/死亡、命中、大招）

## 5. 出包前确认

- [ ] Roslyn `code_diagnostics` 与 `unity_recompile` 双重验证通过后再提交
- [ ] 真机验证中文文本渲染、纹理画质，以及长屏比例（18:9 以上）下 UI 不重叠
- [ ] 每次改动单独出包验证，不累积多个未验证改动

## 6. 工程约定速查

| 项目 | 现状 |
|---|---|
| 输入系统 | Legacy `Input` 类，双鼠标/触摸路径，Android 兼容 |
| 渲染管线 | Built-in（BIRP），无 SRP 依赖 |
| Shader | BlurEffect / EnemyOutline 使用 CGPROGRAM |
| 存档 | `PlayerPrefs`（代码注释提到的 `persistentDataPath` 并未使用） |
| DOTween | safeMode=Off、recyclable=On、logBehaviour=ErrorsOnly |
| 音频 | Unity 原生 AudioSource + AudioMixer（Wwise 已移除） |
| 字体 Shader | Mobile/Distance Field（不含 bevel/glow/specular） |

## 7. 不可违反的约定

- 不直接编辑 `.meta` 文件：Tuanjie 引擎使用加密 GUID，手改会触发重新生成并断裂引用；纹理与音频的导入设置一律通过 `unity_execute` 走 Unity API。
