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
updatedAt: 1780670607626
---

# audio-development-plan

## Summary
游戏音频系统从零搭建的完整开发计划。技术栈: Wwise v2025.1.8 + Unity Integration。分6个Phase: 技术栈就绪→Event/State创建→Bank生成→Unity基础设施→Prefab部署→代码挂接→调优。

## Content
### Phase 3: Unity 集成 — 基础设施

- [x] 挂载 `AkInitializer` + `AkAudioListener` 到 MainCamera
- [x] 创建 `WwiseAudioManager.cs` 单例:
  - Bank 加载/卸载 (`LoadBank` / `UnloadBank`)
  - Event 播放封装 (`PostEvent` / `PlayBGM` / `StopBGM`)
  - BGM 自动循环 (EndOfEvent 回调)
  - DontDestroyOnLoad 跨场景持久化
- [x] Battle 场景集成: `StageController.StartStage()` 播放 BGM, Victory/Defeat/GoToMainMenu 停止 BGM
- [ ] 场景切换逻辑:
  - MainMenu → Battle: 卸载 `BGM.bnk` → 加载 `Battle.bnk` → 切 State=Normal
  - Battle → MainMenu: 卸载 `Battle.bnk` → 加载 `BGM.bnk` → 切 State=Menu

### 当前进度

| Phase | 状态 |
|-------|------|
| Phase 0: 技术栈就绪 | ✅ 完成 |
| Phase 1: Event & State | ⏳ User |
| Phase 2: Bank 生成 | ⏳ User (Stage1_Bgm_Play.bnk 已生成) |
| Phase 3: 基础设施 | ⏳ 进行中 (WwiseAudioManager 已创建, 待 Scene 切换逻辑) |
| Phase 4: Prefab 部署 | 🔒 等待 Phase 3 |
| Phase 5: 代码挂接 | 🔒 等待 Phase 4 |
| Phase 6: 调优 | 🔒 等待 Phase 5 |
