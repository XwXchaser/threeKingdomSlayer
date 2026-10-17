---
id: kd_b1d72e4c-6490-4b44-9952-1e6e0ba8a458
injectMode: inherit
injectAgents:
- unity
aiEditMode: inherit
---

# 卷轴路线投影 Shader 的硬约束

适用于 `Assets/Experiments/CurvedScroll/Shaders/` 下所有读取 `_YSEnabled` 的路线投影 Shader（`YScrollSceneryE0Route`、`YScrollScenery`、`YScrollSample Scenery`、`YOrientedSceneryE0Route`、`YGrass`、`YGrassE0Route`）。2026-10-16 修 N1 路灯旗悬空/方向时实测确认；约束 0 的修复已由用户游戏验收（Scene 与实际游戏方向一致）。

## 0. 负缩放会让 Scene 与 Game 出现镜像差异

这是约束 1 的可见后果，也是最容易误判成「单纯方向不对」的那个。同一个对象因为两条分支对负号处理不同，**编辑画面与游戏画面互为镜像**：

| | `_YSEnabled` | 负 `scale.x` 是否生效 |
|---|---|---|
| Scene 编辑画面（`viewCamera` 为空） | 0 → 走默认 `mul(objectToWorld, v)` | **生效**（会镜像） |
| Game / Play（已绑定相机） | 1 → 走投影重建分支 | **被丢弃** |

实测（`scaleX=-1.68`、`flipX=1`）：Scene 路径 vs Game 路径 `direct=122.91 / mirrored=0.00` → 两张图严格互为镜像。把 `scale.x` 改成正数后 `direct=0.00`，两条路径一致。

**排查口诀**：编辑画面和游戏画面方向相反，先看 `scale.x` 是不是负数，而不是先去调 `flipX`。
**验证方法**：同一份 scene 数据分别按 `_YSEnabled=0/1` 渲染两张图，比对 `direct` 与 `mirrored` 哪个接近 0。

## 1. 负缩放会丢符号，不能用负 Scale 做镜像

这些 Shader 在投影分支里用基向量**长度**重建 XY，不是用带符号的轴：

```hlsl
float sx = length(unity_ObjectToWorld._m00_m10_m20);   // 永远 ≥ 0
float sy = length(unity_ObjectToWorld._m01_m11_m21);
anchor.xy += v.xy * float2(sx, sy);                    // 只用长度，符号被丢弃
```

后果：`scale.x = -2.4` 与 `+2.4` 渲染结果**逐像素完全相同**。非投影材质（内建 `Sprites/Default`）会正常按负缩放镜像，所以把对象从 `Sprites/Default` 换到路线材质时，镜像会**静默消失**。

**规则**：路线投影材质上的景物，朝向必须用 `SpriteRenderer.flipX` / `flipY` 表达，不能用负 `scale.x`。换材质前先确认该对象是否靠负缩放做朝向。

注：`UnityFlipSprite(vertex, _Flip)` 处理的是 `_Flip`，Unity 只把 `flipX/flipY` 写进去，**不会**把 Transform 负缩放折算成 flip。所以负缩放在 `length()` 重建下必然丢失。

`YOrientedSceneryE0Route` 同样用 `length()`，但它的横向方向由 `authoredRight` 重新推导，只取方向不取符号；负缩放同样无效。

### 附：改为正缩放会让 `Renderer.bounds` 位移，但旗杆不一定动

`Renderer.bounds` 是世界空间未投影包围盒，它**会**随符号翻转而移到另一边（实测 `min.x` 从 `-5.71` 变 `-8.58`）。这**不等于**画面里的物体移动了：实际渲染位置由 Shader 决定，投影分支会丢弃符号，所以像素级比对是 `0.00`。判断「物体是否位移」要看渲染像素，不能看 `bounds`。同理，贴图 pivot 在旗杆上时，翻符号不移动旗杆本身。

## 2. `YScrollSample` 是 `[ExecuteAlways]`，会主动清掉 `_YSEnabled`

`YScrollSample.Before(Camera cam)` 每次相机 `onPreCull` 都会先 `Restore()`：

```csharp
void Before(Camera cam) {
    Restore();                       // <-- 无条件把 _YSEnabled 设回 0
    if (cam != viewCamera) return;   // 非 viewCamera 到此为止
    ...
    Shader.SetGlobalFloat("_YSEnabled", 1);
}
```

后果：**任何**离屏渲染/探针相机（甚至 SceneView）在渲染时都会触发 `Restore()`，把 `_YSEnabled` 清零。于是探针前设的全局量在 `Render()` 期间被覆盖，渲染走的是**非投影**分支，测出的是错的代码路径。这会让镜像/位置对比得出自相矛盾的结论（本项目曾因此连续两轮读到相反结果）。

**规则**：要渲染路线投影效果，二选一：
- 让探针相机就是 `route.viewCamera`（生产路径）；或
- 探针期间临时 `route.enabled = false`，用完恢复。

**不能**只设全局量就 `Render()` —— 会被 `Before()` 覆盖。

## 3. 探针必须隔离，否则被已加载场景污染

`cullingMask = ~0` 的探针相机会把已 Additive 加载的 Battle/Y 场景几何一起渲染进来，像素统计（不透明像素数、包围盒、质心）全部失真。

**规则**：探针用 `EditorSceneManager.NewScene(EmptyScene, Additive)` + 专用 `layer`（如 31）+ `cullingMask = 1 << layer`。用完 `CloseScene` 并检查无残留对象。

## 4. 部署检查清单

- 换材质前后用像素级 before/after 对比确认外观（`delta = 0` 才算保持外观）。
- 共享材质实例被多个对象引用时，改材质会**同时影响全部引用者**；只想改其中几个必须逐 Renderer 指定材质资源，而不是改那个共享实例。
- `scene.isDirty` 不能当作「是否已保存」的依据（本项目出现过保存前 `isDirty=False` 但改动确实落盘、以及 dirty 状态与磁盘不一致的情况）。验证落盘用磁盘回读 + 引用计数（例如某 GUID 引用数 `379→383`）+ 关闭重载复核。
- 路线景物换材质后，`scenery` 数组数量、空项、重复项需复核不变。
- 在 Hierarchy 里删除已被 `scenery` 引用的对象，**Unity 只把数组槽置空、不会自动压缩**，会留下 null 槽。运行时无害（`YScrollSample` 遍历都带 `if(scenery[i])` 判空），但会积累脏数据；可用运行时重写数组压缩（`scenery` 只被 `YScrollSample` 自己读取，顺序无语义）。注意只压缩 null，不要删非空项。
- 一次性遍历修正某组对象时要同时处理**已停用但带同样缺陷**的成员，否则下次被启用时问题重现。

## 5. 权威惯例（新部署按此执行）

- 路线投影材质上的景物一律 **`scale.x > 0`**，左右朝向用 `flipX`。
- 内嵌 `Sprites/Default` 上的景物不受约束 0/1 影响（无投影分支，Scene 与 Game 一致），但若计划将来换到路线材质，也应提前改为正缩放 + `flipX`。
