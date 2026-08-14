---
id: kd_6c6ceb9f-a422-4524-969d-06f8a432857d
injectMode: inherit
summary: 三国无双割草游戏的完整机制介绍文档，涵盖攻击系统、大招系统、阵型系统、敌人系统、关卡系统、铜钱经济、配置资产体系和技术架构。供与其他 AI 讨论游戏内容制作时使用。
aiMaintained: inherit
---

## 7. 铜钱经济（道具系统）

### 7.1 道具系统

铜钱是道具系统（`PropSystem`）的第一个道具类型。`PropType` 枚举定义所有道具类型：

```csharp
public enum PropType { Coin = 0, /* 后续扩展: Key, Gem, ... */ }
```

`PropData` 为「类型 + 数量」结构，`SaveData.props` 列表统一管理所有道具。

### 7.2 铜钱获取

- 击杀敌人获得铜钱（Enemy 组件 `coinReward` 字段）
- 通关时获得通关奖励（`StageConfig.clearCoinReward`）

### 7.3 铜钱结算

- 本局铜钱仅记录在 `PlayerState.coinCount`，新关卡从 0 开始
- **结算时机**（`StageController.SettleCoins()`）：
  - **通关胜利**：全部波次清空后结算
  - **返回主菜单**：点击「返回主菜单」按钮时结算（含失败后返回）
- `_coinsSettled` 标记保证同一局只结算一次（幂等）
- 「重新开始」不结算（铜钱丢失）
- 强退游戏（Alt+F4、杀进程）无法触发结算，铜钱丢失

### 7.4 存档

- `SaveData.props` 列表存储所有道具（`List<PropData>`）
- `coinCount` 字段保留向后兼容，`MigrateIfNeeded()` 自动将旧存档迁移到 props 列表
- `SaveManager` 提供通用道具 API：`GetProp(type)` / `SetProp(type, amount)` / `AddProp(type, delta)`
- 便利方法：`GetCoins()` / `SetCoins(amount)` / `AddCoins(delta)` 直接操作 PropType.Coin

### 7.5 数据流

```
Enemy死亡 → PlayerState.AddCoins(enemy.coinReward) → UI更新
通关      → PlayerState.AddCoins(clearCoinReward) → SettleCoins() → SaveManager.SetCoins()
返回主菜单 → SettleCoins() → SaveManager.SetCoins()
```

### 7.6 UI

- Battle 场景显示本局铜钱（`CoinCounterUI`，含跳动+金色飘字动画）
- MainMenu 显示总持有铜钱（`MainMenuUI.coinText`，调用 `SaveManager.GetCoins()`）
- 胜负面板显示本局获得铜钱（`BattleHUD.resultCoinText`）
