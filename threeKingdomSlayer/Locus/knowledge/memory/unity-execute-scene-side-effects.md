---
id: kd_66cdb756-b5f2-49fc-a81a-64685f216c38
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# 硬规则：`unity_execute` 里**禁止新建 Camera / Light 等任何 GameObject**

> **同一个错我已经犯了两次**（2026-10-26 上午、上午晚些时候），两次都把用户的卷轴场景与背景图打没。
> 用户原话：**“禁止写摄像机等去截图”**。
> 再犯一次 = 第三次，这个项目就没人敢让我碰 Unity 了。

## ⛔ 硬规则（没有例外）

1. **禁止在 `unity_execute` 里 `new GameObject(...)` 或 `AddComponent<Camera/Light/...>`。**
   包括“在 `LoadPrefabContents` 的回调里建临时相机/灯”这种看起来安全的写法——**它一样落进活动场景**。
2. **禁止用渲染相机做预览/截图。** 需要看画面就用现成的 `unity_capture_viewport(target=game/scene)`；
   需要看几何就用**纯数学投影**（把顶点投到 ndc 统计 / 导出 CSV / 画 ASCII 图），不碰场景。
3. **禁止在 `unity_execute` 里写 `RenderSettings.*`**（ambient/天空盒等）——它会写穿到主场景。
4. 每次在 `unity_execute` 里碰完场景，**必须回读一遍相机/灯光清单**核对（只留场景原有的那些），
   并确认 `Assets/Scenes/*.scene` 的文件时间戳没被写。

## 两次事故的经过（同一根因）

| # | 时间 | 我干了什么 | 后果 |
|---|---|---|---|
| 1 | 2026-10-26 上午 | 渲染投影时建 `LocusPreviewCam`(depth=0, SolidColor 深蓝) + `LocusPreviewLight`，并写了 `RenderSettings.ambientLight` | 进 Play 后**卷轴场景失效、曲率消失、背景图没了**（多一台相机后渲染并清屏）；ambient 变 0.35/0.38/0.45 |
| 2 | 2026-10-26 上午晚 | 又写 `new GameObject("PreviewCam")` + `("PreviewLight")` 渲染入场动画对比 | **同样的症状再次出现**；另外 ambient 又被写穿一次（已还原） |

两次都是**同一个根因**：`new GameObject()` 落到**活动场景**（Battle），不是 `LoadPrefabContents` 的预览场景；
`UnloadPrefabContents` 不管它们 → 它们留在场景里，一台 depth≥0 的相机用纯色清屏，把主相机的天空盒/远景/雾全顶掉。

## 事实

1. **`new GameObject()` 落到活动场景，不是 `LoadPrefabContents` 的预览场景。**
   `LoadPrefabContents` 只把 prefab 本体放进临时预览场景；脚本里另外 `new` 出来的对象仍进主场景。
   `UnloadPrefabContents` 不会清理它们 → 它们留在场景里，还被存进了内存中的场景状态。
2. **`RenderSettings.*` 会写穿到主场景。** 在预览场景回调里设 `RenderSettings.ambientLight`，
   主场景的读数也跟着变了（实测从场景文件里的 0.212/0.227/0.259 变成我在脚本里写的 0.35/0.38/0.45）。
3. **后果的严重程度取决于参数，不是取决于"只读"标签。**
   - 预览相机 `depth=0` > 主相机 `depth=-1` → 我的相机**最后渲染并用 `SolidColor` 清屏** →
     天空盒、远景图、雾效全被一块纯色顶掉（用户看到的"背景图没了/曲率消失"）。
   - 多出来的一盏平行光（与场景自带那盏同角度）→ 亮度叠加。
   - `readonly=true` 只是权限提示，**不是沙箱**；脚本里写了什么就会发生什么。

## 规则

- **只读分析脚本里禁止 `new GameObject()`**（相机、灯光、任何临时对象都不行）。
  需要临时渲染就：复用场景里已有的对象（只改它的参数并**在 finally 里还原**），
  或改用纯数学投影（把顶点投影到 ndc 统计，不实际渲染）。
- **禁止在分析脚本里写 `RenderSettings.*`**。要临时改光照参数，必须记录原值并在同一脚本的 `finally` 里还原。
- 任何"临时改场景"的动作，结束时都要**列一遍相机/灯光清单**核对，只剩场景原有那些。
- 退出 Play Mode 之后**回读磁盘**：`stat -c "%y" Assets/Scenes/Battle.scene` + `git status`，
  确认场景文件没被写；被写了的要立刻报告（场景文件被写 = 用户未保存的改动可能丢）。

## 排查与修复（这次的处理）

1. 症状对照：`GameView` 截图里天空变成**纯色块**而不是渐变 → 一定有第二台相机在抢渲染。
   用 `FindObjectsOfType<Camera>()` 列出**所有**相机（不只 `Camera.main`）并看 `depth` / `clearFlags` / 所属场景。
2. `unity_set_play_mode(mode=edit)` 回到 Edit Mode（Edit Mode 保留用户未保存的场景改动）。
3. `DestroyImmediate` 掉残留对象 + 把 `RenderSettings` 还原成场景文件里的值。
4. 再截一次 Game 视图复核（天空盒渐变回来了 = 修好）。

## 关联

`memory/tool-result-verification.md` 讲的是"回执≠落盘"；本条讲的是"**声明只读≠真的只读**"，
两者都是同一个纪律：工具调用的副作用范围要靠自己核对，不能靠标签推断。
