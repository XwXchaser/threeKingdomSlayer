---
id: kd_80c00c82-cd7b-4e9e-b248-eb0cbc043555
injectMode: inherit
aiMaintained: inherit
skillEnabled: true
skillSurface: command
---

# overhead-healthbar

## Summary
3D场景中敌人头顶血条（百分比进度条）的Unity实现方案：程序化Quad Mesh + Unlit/Color材质 + 根级独立定位，避免翻转继承和画布遮挡问题。

## Content
# 头顶血条实现方案 (Overhead Health Bar)

## 适用场景
- 3D/2D 场景中需要在角色头顶显示血条
- 血条不跟随角色翻转/旋转
- 需要兼容对象池（GameObject 复用）
- BIRP 渲染管线

## 方案选型

### 推荐：程序化 Quad Mesh + Unlit/Color 材质 + 根级独立定位
- 创建 `MeshFilter` + `MeshRenderer` 子对象，使用程序化生成的 Quad mesh
- 材质使用 `Shader.Find("Unlit/Color")` 或 `Sprites/Default`，不受场景光照影响
- **关键**：血条根对象不挂载为角色子物体，而是场景根级对象，每帧手动设置 `position` 跟随

### 不推荐方案及原因
| 方案 | 问题 |
|---|---|
| SpriteRenderer | 与 Canvas 的排序/层级冲突，BIRP 中常被 UI 背景遮挡 |
| World Space Canvas | 每个敌人一个 Canvas 开销大；仍可能被 ScreenSpaceCamera Canvas 覆盖 |
| 子物体 + 反转 scale | 与 DOTween 动画竞争，每帧符号判断不可靠 |

## 核心设计

### 1. 根级独立定位（避免翻转继承）
```csharp
// barRoot 不 SetParent，直接放根级
barRoot = new GameObject("HealthBar");
barRoot.transform.position = GetHeadWorldPosition();

// Update 每帧跟随
barRoot.transform.position = _enemyTransform.position + offset;
```
优点：完全不受敌人 scale/rotation 影响，DOTween 攻击动画的 `DOScaleX` 翻转不会传递给血条。

### 2. Inspector 可配置参数
- `barWidth` / `barHeight`：世界单位尺寸
- `yOffset`：头顶偏移，0=自动根据 SpriteRenderer.sprite.bounds 计算
- `displayDuration`：受击后显示秒数
- `highColor` / `lowColor` + `lowThreshold`：血量颜色渐变

### 3. 对象池兼容
- `Awake()` 中创建静态材质（共享）
- `EnsureCreated()` 延迟创建子对象，避免池对象 reuse 时重复创建
- `OnDisable()` 隐藏血条（敌人回池时触发）
- `OnDestroy()` 销毁血条根对象（敌人真正销毁时触发）

### 4. 隐藏计时
- `Update()` 中手动 `hideTimer -= Time.deltaTime`
- 超时后 `barRoot.SetActive(false)`
- 不需要 DOTween（零分配，简单可靠）

## 使用方式
1. 将 `EnemyHealthBar.cs` 挂载到敌人 GameObject
2. 代码中受击时调用：
```csharp
var bar = GetComponent<EnemyHealthBar>();
if (bar == null) bar = gameObject.AddComponent<EnemyHealthBar>();
bar.Show(currentHealth / maxHealth);
```

## 已知限制
- Quad mesh 不面向透视摄像机时需要额外 billboard 处理
- 隐藏效果可选加 DOTween fade-out 提升品质
- 网状渲染（MeshRenderer）需确认 layer/cullingMask 不被摄像机裁剪
