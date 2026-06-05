---
id: kd_bb60439d-5117-49fd-9789-7cc1a60f4bd8
type: design
path: audio-development-plan.md
title: audio-development-plan
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1780581294944
updatedAt: 1780581294944
---

# audio-development-plan

## Summary
游戏音频系统从零搭建的完整开发计划。技术栈: Wwise v2025.1.8 + Unity Integration。分6个Phase: 技术栈就绪→Event/State创建→Bank生成→Unity基础设施→Prefab部署→代码挂接→调优。

## Content
### Phase 0: 技术栈就绪 ✅

- [x] Wwise Unity Integration 安装 (v2025.1.8 Build 9170)
- [x] Wwise Authoring 工程创建 (`threeKingdomSlayer_WwiseProject/`)

---

### Phase 1: Wwise Authoring — Event & State 创建 (User)

#### 1.1 BGM Event
- `BGM_Menu` — 主菜单背景音乐 (2D)
- `BGM_Battle_Normal` — 战斗常规阶段 (2D)
- `BGM_Battle_Boss` — BOSS 战阶段 (2D)
- `BGM_Victory` — 胜利 (2D)
- `BGM_Defeat` — 失败 (2D)

#### 1.2 Player SFX Event
- `SFX_Player_Attack_Light1~4` — 普攻 1-4 段
- `SFX_Player_Attack_Heavy` — C技/重攻击
- `SFX_Player_Ultimate` — 大招释放
- `SFX_Player_Ultimate_Charge` — 大招蓄力
- `SFX_Player_Dash` — 闪避
- `SFX_Player_Hit` — 受击
- `SFX_Player_Deflect` — 格挡/弹反
- `SFX_Player_Footstep` — 脚步声 (3D)

#### 1.3 Enemy SFX Event
- `SFX_Enemy_Attack_Melee` — 近战敌人攻击 (3D)
- `SFX_Enemy_Attack_Ranged` — 远程敌人射击 (3D)
- `SFX_Enemy_Arrow_Fly` — 箭矢飞行 (3D, 带 Doppler)
- `SFX_Enemy_Arrow_Impact` — 箭矢命中 (3D)
- `SFX_Enemy_Hit` — 受击 (3D)
- `SFX_Enemy_Death` — 死亡 (3D)
- `SFX_Enemy_Spawn` — 出场 (3D)
- `SFX_Enemy_Boss_Appear` — BOSS 出场
- `SFX_Enemy_Boss_Roar` — BOSS 咆哮

#### 1.4 Displacement SFX Event
- `SFX_Displacement_PushWave` — 击退波
- `SFX_Displacement_ConvergenceWave` — 聚拢波
- `SFX_Displacement_CycloneWave` — 回旋波
- `SFX_Displacement_ChainBounce` — 连锁弹射

#### 1.5 Skill (被动) SFX Event
- `SFX_Skill_Activate` — 被动技能触发 (通用)
- `SFX_Skill_Lightning` — 落雷类被动
- `SFX_Skill_Explosion` — 爆炸类被动

#### 1.6 QTE SFX Event
- `SFX_QTE_Trigger` — QTE 触发
- `SFX_QTE_Success` — QTE 成功
- `SFX_QTE_Fail` — QTE 失败
- `SFX_QTE_Tick` — QTE 倒计时/节奏点

#### 1.7 UI SFX Event
- `SFX_UI_Click` — 按钮点击
- `SFX_UI_Hover` — 悬停
- `SFX_UI_Coin` — 铜钱获取
- `SFX_UI_Combo` — 连击数提升
- `SFX_UI_VictoryStamp` — 胜利印章
- `SFX_UI_WaveClear` — 波次清空
- `SFX_UI_ExpCard_Select` — 经验三选一

#### 1.8 BattleState States
- State Group: `BattleState`
  - `Menu` — 主菜单
  - `Normal` — 常规战斗
  - `Boss` — BOSS 战
  - `Victory` — 胜利
  - `Defeat` — 失败

#### 1.9 3D 衰减配置
- 击退波/聚拢波/回旋波: 大范围 (50-80m 最大衰减距离)
- 近战敌人攻击/受击: 中等范围 (20-30m)
- 箭矢飞行: 直线衰减 (30-50m, 带 Doppler)
- 脚步声: 近距离 (5-10m)

---

### Phase 2: Wwise Authoring — Bank 生成 (User)

#### Bank 策略
| Bank | 内容 | 加载时机 | 预估大小 |
|------|------|----------|----------|
| `Init.bnk` | Wwise 初始化 | 游戏启动 | ~300KB |
| `BGM.bnk` | 所有 BGM | 主菜单 | ~2-3MB |
| `SFX_Common.bnk` | UI + QTE + 公共 SFX | 主菜单 | ~500KB-1MB |
| `SFX_Battle.bnk` | Player + Enemy + Displacement + Skill SFX | 进入战斗 | ~3-5MB |
| | **总计** | | **~5.8-9.3MB** |

---

### Phase 3: Unity 集成 — 基础设施 (Dev)

- [ ] 挂载 `AkInitializer` + `AkAudioListener` 到 MainCamera
- [ ] 创建 `WwiseAudioManager.cs` 单例:
  - Bank 加载/卸载 (`LoadBank` / `UnloadBank`)
  - Event 播放封装 (`PostEvent`)
  - BattleState State 切换 (`SetBattleState`)
  - 场景切换 Bank 管理
- [ ] 场景切换逻辑:
  - MainMenu → Battle: 卸载 `BGM.bnk` → 加载 `Battle.bnk` → 切 State=Normal
  - Battle → MainMenu: 卸载 `Battle.bnk` → 加载 `BGM.bnk` → 切 State=Menu

---

### Phase 4: Unity 集成 — Prefab 部署 (Dev)

- [ ] Player Prefab: 挂载 `AkGameObj`
- [ ] Enemy Prefab (通用): 挂载 `AkGameObj`
- [ ] Boss Prefab: 挂载 `AkGameObj`
- [ ] Arrow Prefab (Enemy_105 箭矢): 挂载 `AkGameObj`
- [ ] Displacement Wave Prefabs (击退波/聚拢波/回旋波): 挂载 `AkGameObj`

---

### Phase 5: Unity 集成 — 代码挂接 (Dev)

#### 5.1 BGM 切换
- [ ] `MainMenuUI.Awake()` — 加载 `BGM.bnk`, Post `BGM_Menu`, SetState=`Menu`
- [ ] `StageController` 进入战斗 — SetState=`Normal`
- [ ] BOSS 出现时 — SetState=`Boss`
- [ ] Victory/Defeat — SetState=`Victory`/`Defeat`

#### 5.2 Player 音频
- [ ] 攻击动画事件 → `SFX_Player_Attack_Light1~4`
- [ ] C技动画事件 → `SFX_Player_Attack_Heavy`
- [ ] 大招释放 → `SFX_Player_Ultimate`
- [ ] 闪避 → `SFX_Player_Dash`
- [ ] 受击 → `SFX_Player_Hit`
- [ ] 弹反 → `SFX_Player_Deflect`

#### 5.3 Enemy 音频
- [ ] 敌人攻击时机 → `SFX_Enemy_Attack_Melee` / `SFX_Enemy_Attack_Ranged`
- [ ] 箭矢生成 → `SFX_Enemy_Arrow_Fly`
- [ ] 箭矢命中 → `SFX_Enemy_Arrow_Impact`
- [ ] 敌人受击 → `SFX_Enemy_Hit`
- [ ] 敌人死亡 → `SFX_Enemy_Death`
- [ ] 敌人出场 → `SFX_Enemy_Spawn`
- [ ] BOSS 出场/咆哮 → `SFX_Enemy_Boss_Appear` / `SFX_Enemy_Boss_Roar`

#### 5.4 Displacement 音频
- [ ] 击退波触发 → `SFX_Displacement_PushWave`
- [ ] 聚拢波触发 → `SFX_Displacement_ConvergenceWave`
- [ ] 回旋波触发 → `SFX_Displacement_CycloneWave`
- [ ] 连锁弹射触发 → `SFX_Displacement_ChainBounce`

#### 5.5 Skill 音频
- [ ] PassiveTriggerModule 触发时 → 按技能类型 Post 对应 Event

#### 5.6 QTE 音频
- [ ] QTE 触发 → `SFX_QTE_Trigger`
- [ ] QTE 成功 → `SFX_QTE_Success`
- [ ] QTE 失败 → `SFX_QTE_Fail`
- [ ] QTE 节奏点 → `SFX_QTE_Tick`

#### 5.7 UI 音频
- [ ] 按钮点击 → `SFX_UI_Click`
- [ ] 铜钱UI更新 → `SFX_UI_Coin`
- [ ] 连击数变化 → `SFX_UI_Combo`
- [ ] 胜利印章 → `SFX_UI_VictoryStamp`
- [ ] 波次清空 → `SFX_UI_WaveClear`
- [ ] 经验卡选择 → `SFX_UI_ExpCard_Select`

---

### Phase 6: 调优 (Both)

- [ ] 真机验证 Bank 加载/卸载性能
- [ ] 3D 衰减实际效果调优
- [ ] BGM 过渡平滑性 (State 切换时 Crossfade)
- [ ] 包体重新评估 (上调至 13-22MB 包体预算)
- [ ] 音频优先级和 Voice 数量限制配置

---

### 当前进度

| Phase | 状态 |
|-------|------|
| Phase 0: 技术栈就绪 | ✅ 完成 |
| Phase 1: Event & State | ⏳ User |
| Phase 2: Bank 生成 | ⏳ User |
| Phase 3: 基础设施 | 🔒 等待 Phase 2 |
| Phase 4: Prefab 部署 | 🔒 等待 Phase 2 |
| Phase 5: 代码挂接 | 🔒 等待 Phase 2 |
| Phase 6: 调优 | 🔒 等待 Phase 5 |
