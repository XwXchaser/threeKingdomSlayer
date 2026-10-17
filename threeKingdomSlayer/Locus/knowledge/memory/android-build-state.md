---
id: kd_7552ac95-7456-4497-9204-ac24173a3b31
injectMode: inherit
summary: Android 打包前按优先级分阶段检查清单，涵盖帧率、UI适配、性能、包体、输入优化。每次打包复用。
skillEnabled: true
skillSurface: command
aiEditMode: inherit
---

# Android 打包检查清单

> 基于 2026 年全项目评估，按优先级分阶段排列。每次打包前按此清单逐项检查。

---

## 阶段 0.5：打包产物在设备上崩溃（2026-10-15 实测，**已解决**）

### 卷轴背景“变灰/看不全”排查（2026-10-15）

结论链（均有像素/日志证据）：

1. 场景里那张“可编辑天空”不是 Unity 天空盒，而是**屏幕空间全屏 quad**：对象 `Skybox Background - editable` / 材质 `YSampleScenerySkybox_v2` / shader `CurvedScroll/Y Sky Background` / 贴图 `ValleySkybox_v2`（1024×1536）。两个场景的 `RenderSettings.m_SkyboxMaterial` 都只是**内置默认天空盒**。
2. 该 shader 取景：`cover=(min(1,screenAspect/imgAspect), min(1,imgAspect/screenAspect))` → **纵向永远完整（[0,1]）、只横向裁切**；9:16 裁 16%、1:2 裁 25% —— **纵向取景与宽高比无关**。
3. 材质原值 `_VerticalOffset = -0.23` 已改为 **0**（已落盘，git 可见）——但**它不是“背景变灰”的原因**：按第 6 条的正负语义，负值只是切掉贴图底部（本来就被地形遮住），顶部蓝天仍然可见。改它是合理的清理，不要当成修好了“灰”的功臣。
4. **（2026-10-15 复核，推翻旧结论）**先前“HUD 卡片盖住天空”的判断是**错的**：`HeroHUDParent/HeroHUDRoot/HudCard` 实际在屏幕**底部**（自顶 74%~97%），并未遮挡天空。真正让整帧变暗的是 **`BattleHUD(Canvas)/Defeat(panel)/DarkOverlay`（全屏黑，alpha 0.72）**：当时 `StageController.currentState == Defeat`、玩家血量 -22（关卡在无人操作下跑了很久被打死）。只关掉该面板后整帧亮度恢复约 3.5 倍，且顶部与贴图顶行吻合。
5. 该天空 quad 不读 `_YS*` 全局量、不参与投影，也不在 `scenery[]` 数组里，但 `isVisible=True`——从未被剔除。它由 **Main Camera**（cullingMask=-5，Y 场景只是普通已加载场景）渲染，`Sample Camera`（depth=0，clearFlags=SolidColor）与它无关（未参与最终画面）。
6. **`_VerticalOffset` 的正负语义（实测贴图行匹配）**：offset=0 时屏幕顶部 ⇄ 贴图**顶行**（蓝天 90,164,240 区域），屏幕底部 ⇄ 贴图底行，纵向完整、只有横向裁切（cover.y 恒为 1）。**正值会把贴图最深的蓝色顶行推出画面、让可见天空更淡更灰**；负值只是切掉贴图底部（本来就被地形挡住）。⇒ offset=0 是“任何比例都纵向完整”的正确值。
7. 实测工具链结论：判“背景对不对”要用**像素分带 + 与贴图行做颜色匹配**（偏离值 40~60 视为命中），比“看着灰/看着对”可靠得多。注意 Play 模式下关卡会继续跑，**玩家会被打死**，测到的画面常是 Defeat 遮罩态，测量前必须确认 `StageController.currentState` 与玩家血量。

### 设备 vs 编辑器：背景（2026-10-15 晚间，**已解决**，用户真机验收通过）

最终结论：**真机渲染完全正常**（用户截图 864×1920 与编辑器同位置帧逐带吻合：天空均占顶部 ~35%、地面从 35% 起，颜色逐段一致）；差异其实来自两处与包体无关的东西——真机包用 `_VerticalOffset = -0.23`（本任务曾误改为 0，已回退）、以及**卷轴所处位置不同**。模拟器上那张暗暖、静止的画面**未能完全归因**（候选：x86 模拟器软件 GL、我用 MENU 键把它切后台导致帧静止、Defeat 遮罩），但**不阻塞**：同一份包在真机上正常，以此为准；下次要在模拟器上判定，先 `svc power stayon true` + 确认在前台且未进 Defeat，再截图。

排查过程中有效的工具（保留备用）：`adb logcat -s Tuanjie`；像素分带 + 与贴图行颜色匹配；同帧多比例 RT 对比（见上一节）；用 `EditorApplication.isPaused` 冻结关卡再抓图，避免玩家被打死后测到 Defeat 遮罩态。

用户反馈“编辑器里蓝天白云，打包后灰蒙蒙一片”，实测**成立**（不是错觉，也不是模拟器屏幕变暗）：

- 模拟器（同一份今天的包；`full_1338` 与 `diag_bisect_1346` 都试过；已 `svc power stayon true` + `keyevent WAKEUP`，`dumpsys power` 确认 `state=ON/Awake`）：顶部 10% 均色 **(63,55,51)**、(54,46,42)、(60,47,39)… 暖灰，**距蓝天贴图行 220~253**，整帧低对比且 30~140s 内基本静止。
- 编辑器（Play，玩家存活）：顶部 5%~30% 均色 (115,140,193)…(114,150,199)，与 `ValleySkybox_v2` **顶部蓝天行**色距仅 42~56；顶部 40% 里蓝天占 56.6%。
- **已排除**（均有数据）：宽高比（纵向取景与比例无关）、HeroHUD（在屏幕底部 74%~97%）、投影全局量（设备与编辑器完全一致：`_YSAngle=0.7854`、`_YSViewDistance=266.1`、`_YSPose=(0,0,0,0)`、`_YSEnabled=0`）、天空对象状态（设备 `激活/渲染器启用/isVisible` 均 True）、shader 支持（不支持数=0）、shader 编译（Editor.log 里 `CurvedScroll/Y Sky Background` 编译无错）、模拟器熄屏。
- **定量线索**：设备顶部色 ≈ 贴图**底部**暖米行(240,214,181) 的 ~0.26 倍（62/56/47 vs 实测 63/55/51）→ 指向“**打包版把天空贴图上下颠倒采样**（shader 里那行 `o.uv.y=1-o.uv.y` 在 GLES3 上方向与编辑器相反）+ 一层压暗”两个方向。
- （已不存在待定实验：真机已验收，天空与该比例口径均已结案）

### “手机上有天空+山脉、编辑器里地面占比太高”核查（2026-10-15）

- **不是比例问题**：两个相机都是 `fov=60`（垂直视角），且工程内**没有任何按屏幕比例改 FOV/投影的代码**（`ScrollProjection.cginc` 不用 `_ScreenParams`，C# 里只有 `CanvasScalerAdapter`/`SafeAreaAdapter`/`UIResolutionHelper`/OnGUI 在用 `Screen.*`）。⇒ 天空/地面的**上下**比例与屏幕比例无关，改 Game 视图分辨率只改**左右**可视范围。
- 更可能是**卷轴所处位置不同**：刚进关卡时相机正对前方长路（地面占比大）；玩一会儿后卷轴推进、远景山脉进入视野。比对时必须同一位置。

#### 真机截图 vs 编辑器（2026-10-15，已定调）

真机截图（864×1920 = **0.450**）逐带与编辑器同位置帧（0.500）对比：

| 自顶比例 | 真机 | 编辑器 |
|---|---|---|
| 0~35% | 蓝天 (108,151,201)…(167,181,194) | 蓝天 (115,140,193)…(126,146,181) |
| 30~35% | (129,140,159) 远山/雾 | (126,146,181) 同类 |
| 35% 往下 | 暖色地面 (147,102,70)… | 暖色地面 (154,111,85)… |

⇒ **上下分界（天空占顶部 ~35%）两边一致**，而且真机比例 (0.450) 比编辑器 (0.500) 更**窄**却分界相同 ⇒ **纵向构图与宽高比无关**（上面代码结论得到实测印证）。“地面占比高”不是比例问题。

**`_VerticalOffset` 的真实语义（实测标定）**：

- 负值（如真机包的 **-0.23**）= 把可见范围缩到贴图**上部 (1+offset)** 的高度，等于把天空贴图**纵向放大**；实测云带被推到屏幕 23.5~26.6%，与 18%÷0.77≈23.4% 完全吻合。
- `0` = 整张贴图高度刚好铺满屏幕（云带位置更高）。
- **正值会把贴图最深的蓝色顶行推出画面，可见天空变得更淡更灰——是错的方向。**
### 多比例一致性受控实验（2026-10-15，结论：已满足“只裁切不改内容”）

方法：同一时刻、同一位置，只改渲染目标尺寸（把两个相机的 `targetTexture` 指向不同尺寸 RT，`cam.aspect` 随之自动等于 RT 比例），共 4 组：864×1920(0.450)、1080×2160(0.500)、1080×1920(0.5625)、810×1080(0.750)。

| 比例 | 天空结束行（自顶） |
|---|---|
| 0.450 | **16.9%** |
| 0.500 | **16.9%** |
| 0.5625 | **16.9%** |
| 0.750 | 12.8% |

- 三种手机竖屏比例下 **天空结束行完全相同（5% 分段均色也逐段一致）** ⇒ 内容一致，比例只造成**左右**裁切。这符合“任何比例内容一致、顶多被裁掉一部分”的要求。
- **唯一例外**：比例**宽于贴图自身比例（>0.667，平板/横屏）**时，`YSkyBackground.shader` 的 `cover` 拟合会额外上下裁一点（天空结束行 12.8% vs 16.9%），而世界观景物不变 ⇒ 该区间内天空的缩放与世界观不一致。手机竖屏（0.4~0.6）不会触发。
- 本实验口径（同帧多比例 RT + “天空结束行”指标 + 5% 分段均色）可作为以后任何背景/相机改动的验收手段；产物在 `Library/Locus/tmp/asp_<W>x<H>.png`。

#### 修复（已实施并验证）：纵向不再随比例裁切

- 根因：`YSkyBackground.shader` 里 `cover.y = min(1, imageAspect/screenAspect)`；当屏幕比例宽于贴图自身比例（0.667）时该值 < 1，把贴图纵向放大并上下裁切，而世界观景物不变 ⇒ 该区间“内容随比例变化”。
- 改法（1 行）：`float2 cover = float2(min(1, screenAspect/imageAspect), 1);` —— 横向仍按比例裁切，**纵向永远整幅**。
- 验证（同一时刻同一位置，只改渲染目标尺寸）：

| 比例 | 修复前 天空结束行 | 修复后 天空结束行 |
|---|---|---|
| 0.4500 | 16.9% | 16.9% |
| 0.5000 | 16.9% | 16.9% |
| 0.5625 | 16.9% | 16.9% |
| 0.7500 | **12.8%** | **16.9%** |

修复后四种比例的 8 段竖向均色逐段一致（如末段 193,142,82 / 195,143,83），即比例只造成**左右**裁切。产物：`Library/Locus/tmp/fix_*.png`（对照修复前 `asp_*.png`）。
- 已知取舍：屏幕比例宽于贴图（>0.667，平板/横屏）时贴图会被轻微**横向拉伸**而不是裁切（贴图外无内容可露），**内容不变**；手机竖屏（0.4~0.6）不会遇到。
- 未改动（保持不变）：世界观景物的 `fov=60` 垂直投影、`_VerticalOffset = -0.23`（与比例无关的固定纵向标定）。
- 顺带确认：`_VerticalOffset` 是**与比例无关的固定纵向缩放**（-0.23 时可见贴图上部 77%，云带被推到屏幕 23.5~26.6%），不是比例问题；因此“真机与编辑器不一致”靠回退这个值解决，而不是靠改比例。
- **重要副产品**：进关卡后不操作，玩家 **约 8 秒就会死**（实测 100→73 只用了 2 秒；等 11 秒后血量 -22、`StageController.currentState=Defeat`）。一进 Defeat，全屏被 `DarkOverlay`(黑 a=0.72) 压暗 3.5 倍⇒“灰蒙蒙一片”。设备上/浏览器里看到的暗画面极可能就是它，与天空、背景无关。

复现命令（模拟器）：完整包安装后 `adb shell monkey -p com.DefaultCompany.threeKingdomSlayer -c android.intent.category.LAUNCHER 1`，等主菜单出现后 `adb shell input tap 540 716`（继续按钮）即进关卡；截图用 `adb exec-out screencap -p > x.png`。注意 `full` 包是 Release 且剥掉了 `Debug.Log`，要看游戏日志得用带诊断的包。

### 现象

主菜单能进，一旦加载 **Battle 场景**（或把 Battle 作为首场景）就在启动/切换场景时退出：

```text
W/Tuanjie: The referenced script on this Behaviour (Game Object '<null>') is missing!   ×7
E/Tuanjie: The file '<apk>/assets/bin/Data/levelN' is corrupted! Remove it and launch unity again!
E/Tuanjie: [Position out of bounds!]
F/libc   : Fatal signal 5 (SIGTRAP) in tid ... (Loading.Preload)  → abort
```

### 已排除（均有实测证据）

- **与本轮卷轴/假移动路线移植无关**：2026-10-08 的 tombstone_07/08 与今天**同一崩溃**（`Loading.Preload` + `libtuanjie.so` + SIGTRAP），那时还没有本轮代码。
- **与 ABI 无关**：x86 模拟器上用 ARM64|X86 双架构包，`libmain.so` 加载正常（已能进主菜单）。
- **与资源集群打包无关**：`data.tj3d` 集群包与传统 `level*` 分文件包**都崩在同一处**；传统包由菜单 `Tools/打包/Android（ARM64+X86，传统布局）` 产出（`BuildOptions.None`，841 条目、含 level0..2）。
- **文件本身没坏**：`level0/1/2` 均存在、CRC 校验通过、序列化头为 `2022.3.62t7` 与运行时一致；APK 内所有条目 CRC 正常。
- **不是脚本被剥离**：场景里 80 个 MonoBehaviour 类型在 `global-metadata.dat` 里**全部存在**；编辑器内 Battle 场景 0 缺失脚本（仅 `RouteStageTargetsV2` 的 MonoScript 路径为空，疑点待查）。
- **MainMenu 场景数据能读**（level0 多次正常加载），失败只出现在 Battle 场景数据上；把 Battle 打成**唯一场景**后仍崩（现报 `level0`）→ 与“第几张场景”无关。

### 根因与修复（2026-10-15 已定位并修复）

**根因**：`Assets/Scenes/Battle.scene` 的根对象 `RouteStageTargetsV2` 上挂着 **3 个同名脚本组件**，其中 **2 个的 `m_Script` 是坏引用**——写成了场景文件内部的 fileID（`{fileID: 1510856187}` / `{fileID: 885241032}`），而不是规范的 `{fileID: 11500000, guid: <脚本GUID>, type: 3}`：

```yaml
--- !u!114 &745671991
  m_Script: {fileID: 1510856187}   # 坏
--- !u!114 &745671993
  m_Script: {fileID: 885241032}    # 坏
--- !u!114 &745671994
  m_Script: {fileID: 184010192}   # 非规范，但保留了 3 个 target 引用
```

Editor 里它们表现为 2 个 `<null组件>`（本次排查发现 `GameObjectUtility.GetMonoBehavioursWithMissingScriptCount` 对这种“本地 fileID 坏引用”**返回 0**，必须用 `GetComponents<Component>()` 里数 null 才能发现）；玩家构建时引擎预加载到这些组件就解析失败 → `[Position out of bounds!]` → SIGTRAP。

**修复**：删除整个 `RouteStageTargetsV2` 对象后按原 TRS 重建（3 个锤点 `InitialHeadTarget/CombatTarget/TailTarget` 位置保持），新组件的脚本引用是规范 GUID 形式，并重新接上 `RouteStageRuntimeV2.battleTargets`。修复后全场景坏脚本组件 = 0。

**定位方法（可复用）**：用菜单构建不同“根对象子集”的变体场景（我生成 `Assets/Experiments/CurvedScroll/Diagnostics/Bisect.unity`，你点 `Tools/打包/诊断：构建 Bisect 变体`），逐轮对半删根对象 → 装机冷启动看是否还崩；含罪魁的变体必崩、不含的必起。本次：19 个根对象的四轮二分即锁定。

### 工具边界（重要）

- **不能从 Locus 代码片段里驱动构建**：`BuildPipeline.BuildPlayer` 会以引擎内部异常失败（`InvalidCastException @ EditorCompilation.CompleteActiveBuildWhilePumping`，外壳报 “scripts had compiler errors”），复现多次。构建必须走 Unity 菜单：
  - `Tools/打包/Android（ARM64+X86，传统布局）`（推荐，避免集群包，包体也更小：261MB vs 336MB）
  - `Tools/打包/诊断：只打 Battle 单场景`
  - `Tools/打包/诊断：只打空场景 EmptyDiag`（场景：`Assets/Experiments/CurvedScroll/Diagnostics/EmptyDiag.unity`）
- 模拟器**不吃 adb 注入点击**（`input tap` 试过 7 个坐标均无反应），涉及菜单/按钮的验证需要人手点一次。

### 未完成的分支判别

1. **真机（ARM64）测试**：把 `Build/Android/threeKingdomSlayer_20261015_1213.apk` 装到手机。能跑 → 模拟器/其 APK 读取层的问题；也崩 → 构建/引擎问题。
2. **空场景包**：若空场景也崩 → 工程级/引擎级；若空场景正常 → Battle 场景内容特定，需二分场景内容（每次构建需人工点菜单）。
3. 上引擎方（Tuanjie）反馈：tombstone + 最小复现场景 + 引擎版本 `2022.3.62t7 / Tuanjie 1.8.5`。

---

## 阶段 0：构建配置（P0，打包前必须先查）

### 0.1 Target Architectures 必须匹配测试设备

- **事故（2026-10-08）**：两次打包产物只有 `lib/x86/`（`libil2cpp.so` / `libunity.so` / `libtuanjie.so`），真机（arm64-v8a / armeabi-v7a）要么装不上、要么一启动就退；当时 `ProjectSettings.asset` 为 `AndroidTargetArchitectures: 4`（= X86）。
- **最终设置（2026-10-15）**：`PlayerSettings.Android.targetArchitectures = ARM64 | X86`，落盘值 `AndroidTargetArchitectures: 6`（模拟器与真机都能跑；发布包可再只留 ARM64）。
- **编码速查**：`1 = ARMv7`、`2 = ARM64`、`4 = X86`（按位或可多选）。
- **快速自检**：`unzip -l <apk> | grep lib/` 应看到 `lib/arm64-v8a/...`；或直接读 `ProjectSettings/ProjectSettings.asset` 的 `AndroidTargetArchitectures`。
- **兼容取舍**：ARM64 覆盖 2015 年后几乎所有设备且满足商店的 64 位要求；若需兼容极旧 32 位设备再叠加 ARMv7（native 库体积约 +35MB）。`Build/Android/*_x86_*.apk` 不能用于真机验收。

### 0.3 禁止用 Tuanjie 单文件容器模式打包（2026-10-15 实测崩溃）

- **现象**：主菜单能进，点“开始/继续”后进战斗场景时直接退出（“已停止运行”）。
- **日志证据**（adb logcat，两次独立会话均复现）：
  ```text
  [CameraManager] Departure 完成，加载 Battle
  W/Tuanjie: The referenced script on this Behaviour (Game Object '<null>') is missing!   ×7
  E/Tuanjie: The file '<apk>/assets/bin/Data/level1' is corrupted! Remove it and launch unity again!
  E/Tuanjie: [Position out of bounds!]
  F/libc   : Fatal signal 5 (SIGTRAP) in tid ... (Loading.Preload)   → 进程 abort
  ```
- **根因**：该 APK 把全部游戏数据打进了单体容器（`assets/bin/Data/data.tj3d`，共 294MB），**没有 `level*` 场景文件**；引擎运行时按传统布局去读 `assets/bin/Data/level1` → 读越界 → abort。与游戏代码、ABI 都无关。
- **判定方法（构建后先查，再装设备）**：
  ```text
  unzip -l <apk> | wc -l        # 传统布局：600+；tj3d 模式：~35
  unzip -l <apk> | grep level0   # 传统布局：有 level0/level1/...；tj3d：无
  unzip -l <apk> | grep data.tj3d  # tj3d 模式才有
  ```
- **规则**：`EditorUserBuildSettings.compressWithPsArc` 与 `compressFilesInPackage` 必须保持 **False**（当前即为 False）；默认模式打出来的包与传统 `level*` 布局一致。此前的 10-08 包也是传统布局（628 条目，含 `level0`）。
- **另一个坑**：从 Locus 代码片段里直接调 `BuildPipeline.BuildPlayer` 会以引擎内部异常失败（`InvalidCastException @ EditorCompilation.CompleteActiveBuildWhilePumping`，表现为“scripts had compiler errors”）。**构建请用 Unity 的 Build Settings 窗口或编辑器菜单脚本**，不要在片段宿主里驱动。

### 0.4 模拟器 / 真机的 ABI 必须对上（2026-10-15 实测）

- 本机模拟器（x86，Android 7.1.2，模拟 LIO-AN00）能力：`ro.product.cpu.abilist = x86, armeabi-v7a, armeabi`，`ro.enable.native.bridge.exec = 1`（带 ARM32 翻译层 `libnb.so`），**不支持 arm64**。
- 实测失败日志（ARM64-only 包 + x86 模拟器）：
  ```text
  W/System  : ClassLoader referenced unknown path: /data/app/<pkg>-1/lib/x86
  E/libnativebridge: LdInitNativeBridgeDelay open failed! file=.../lib/x86/libmain.so
  E/Tuanjie : Failed to load 'libmain.so'
  E/Tuanjie : java.lang.UnsatisfiedLinkError: ... couldn't find "libmain.so"
      at com.unity3d.player.UnityPlayer.<init>(SourceFile)
      at com.unity3d.player.UnityPlayerActivity.onCreate(UnityPlayerActivity.java:40)
  ```
  表现：能启动、无弹窗、`onCreate` 即终止；`dumpsys package` 的 `primaryCpuAbi=null`（包里没有设备能用的 ABI）。
- 对应关系：**x86 模拟器 → 需 X86（原生）或 ARMv7（翻译层）；真机/发布 → 需 ARM64**。ARM64-only 包在这台模拟器上必然跑不起来。
- 推荐：开发期用 `ARM64 | X86` 胖包（模拟器与真机都能跑，代价是包体变大）；发布包只留 ARM64。改完必须重新构建才生效。
- 日志抓取方式（无需 Unity）：`<AndroidSdk>\platform-tools\adb.exe -s <serial> logcat -c` → `am force-stop <pkg>` → `am start -W -n <pkg>/com.unity3d.player.UnityPlayerActivity` → `logcat -d | grep -iE "Tuanjie|il2cpp|dlopen|UnsatisfiedLink|FATAL"`。

### 0.3 其它构建设置（2026-10-15 核查）
- `scriptingBackend = IL2CPP`、`apiCompatibility = NET Standard 2.0`、`minSdk = 22`、`targetSdk = Auto`、`Managed Stripping = Minimal`、`stripEngineCode = True`：正常。
- `useCustomKeystore = False`（debug 签名）：自测可用；**分发/上架前必须换成自签名**。
- Build Settings 当前只保留：`Assets/Scenes/MainMenu.scene`、`Assets/Scenes/Battle.scene`、`Assets/Experiments/CurvedScroll/YJunctionSample.unity`（长坂坡卷轴关卡必需；已移除 V2 路线关卡场景）。
- 关卡侧只保留长坂坡：`Assets/Scenes/MainMenu.scene` 的 `StageConfigManager.routeStagesV2` 已清空，只留 `fakeRouteStages = [Changbanpo_ScrollRoute]`。
- 包体仍偏大（x86 单架构包 155MB、ARM 双架构包 223MB）：`Assets/Fonts/方正粗黑宋简体 SDF.asset`（~37MB）特重建，见阶段 3.4。

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
