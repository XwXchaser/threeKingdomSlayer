---
id: kd_bb60439d-5117-49fd-9789-7cc1a60f4bd8
injectMode: inherit
summary: '游戏音频系统从零搭建的完整开发计划。技术栈: Wwise v2025.1.8 + Unity Integration。分6个Phase: 技术栈就绪→Event/State创建→Bank生成→Unity基础设施→Prefab部署→代码挂接→调优。'
aiEditMode: inherit
---

### Phase 3: Unity 集成 — 基础设施

- [x] 挂载 `AkInitializer` + `AkAudioListener` 到 MainCamera
- [x] 创建 `WwiseAudioManager.cs` 单例:
  - Bank 加载/卸载 (`LoadBank` / `UnloadBank`)
  - Event 播放封装 (`PostEvent` / `PlayBGM` / `StopBGM`)
  - BGM 自动循环 (EndOfEvent 回调 + `_bgmStopped` 竞态保护)
  - DontDestroyOnLoad 跨场景持久化
  - `SceneManager.sceneUnloaded` 监听 Battle 卸载自动 StopBGM
- [x] 音量控制: `AkSoundEngine.SetRTPCValue("MasterVolume")` + `PlayerPrefs` 持久化
- [x] `PauseMenuUI` 音量 Slider: 暂停面板拖动调节，重启恢复上次值
- [x] Battle 场景集成: `StageController.StartStage()` 播放 BGM, Victory/Defeat/GoToMainMenu 停止 BGM
- [ ] 场景切换 Bank 管理 (MainMenu ↔ Battle Bank 切换 — 待更多 BGM 后实现)

### 当前进度

| Phase | 状态 |
|-------|------|
| Phase 0: 技术栈就绪 | ✅ 完成 |
| Phase 1: Event & State | ⏳ User (Stage1_Bgm1_Play Event + MasterVolume Game Parameter 已创建) |
| Phase 2: Bank 生成 | ⏳ User (Stage1_Bgm_Play.bnk + Init.bnk 含 MasterVolume 已生成) |
| Phase 3: 基础设施 | ✅ 完成 |
| Phase 4: SFX 接入 | 🔒 等待 Phase 1/2 (User 创建 SFX Event + SoundBank) |
| Phase 5: 调优 | 🔒 等待 Phase 4 |
