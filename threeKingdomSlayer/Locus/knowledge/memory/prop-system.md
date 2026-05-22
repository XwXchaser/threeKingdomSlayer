---
id: kd_5e3205b5-6d89-4fba-9242-51fe0ec7d1eb
type: memory
path: prop-system.md
title: prop-system
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1779348485423
updatedAt: 1779348485424
---

# prop-system

## Summary
道具系统基础架构：PropType 枚举、PropData 结构、SaveData 道具列表、旧档迁移、SaveManager 通用道具 API、StageController 结算规则

<!-- locus:body:start -->
## 道具系统（PropSystem）

### 文件
- `Assets/Scripts/Core/PropSystem.cs` — `PropType` 枚举、`PropData` 类、`PropUtils` 扩展方法
- `Assets/Scripts/Core/SaveManager.cs` — `SaveData.props` 列表、通用道具 API、旧档迁移

### PropType 枚举
```csharp
public enum PropType { Coin = 0, /* 后续扩展: Key, Gem, ... */ }
```

### 核心 API

**SaveManager（持久层）**：
- `GetCoins()` / `SetCoins(int)` / `AddCoins(int)` — 铜钱便利方法
- `GetProp(PropType)` / `SetProp(PropType, int)` / `AddProp(PropType, int)` — 通用道具方法
- `SaveData.GetCoinCount()` — 从 props 列表读取铜钱，兼容旧 coinCount 字段
- `SaveData.MigrateIfNeeded()` — 自动将旧存档 coinCount 迁移到 props 列表

**PlayerState（会话层）**：
- `coinCount` — 本局铜钱计数器（会话级）
- `GetSessionProp(PropType)` — 获取本局道具数量

**PropUtils（工具扩展）**：
- `List<PropData>.GetPropAmount(type)` / `.SetPropAmount(type, amount)` / `.AddPropAmount(type, delta)`

### 结算规则
- `StageController.SettleCoins()` 在通关胜利和「返回主菜单」时调用
- `_coinsSettled` 标记防重复结算
- 「重新开始」不结算，强退游戏不结算

### 扩展新道具
1. 在 `PropType` 枚举添加新类型
2. 使用 `SaveManager.SetProp(newType, amount)` 存取
3. 可选在 `PlayerState` 添加对应的会话计数器
<!-- locus:body:end -->
