---
id: kd_a55befda-e494-4213-ad89-daca00cee52a
type: memory
path: wwise-audio-status.md
title: wwise-audio-status
inheritInjectMode: true
summaryEnabled: true
commandEnabled: false
readOnly: false
inheritAiConfig: true
createdAt: 1780763484954
updatedAt: 1781010584204
---

# wwise-audio-status

## Summary
音频系统已从 Wwise 迁移至 Unity 原生 AudioSource + AudioListener.volume。Wwise 已完全移除。迁移中修复了 m_DisableAudio、AudioSource 缺失、preloadAudioData 三个关键问题。

<!-- locus:body:start -->
## 音频系统（已迁移至 Unity 原生）

**迁移完成日期**: 2025-07-09  
**技术栈**: Unity AudioSource + AudioListener.volume（Wwise 已完全移除）

### 迁移关键问题与修复

迁移过程中遇到的三个阻塞性 bug：

1. **`ProjectSettings/AudioManager.asset` 中 `m_DisableAudio: 1`** — Wwise 安装时禁用了 Unity 原生音频引擎，导致所有 AudioSource 静默。改为 `0` 后恢复。
2. **AudioSource 组件缺失** — `CreateAudioSources()` 在 `Awake()` 中动态创建不可靠。改为场景中预置 3 个 AudioSource，代码改为检测已有组件。
3. **`preloadAudioData: False`** — WAV 导入设置未预加载音频数据。8 个 AudioClip 全部设为 `preloadAudioData=True`，短 SFX 用 `DecompressOnLoad`，长 BGM 用 `CompressedInMemory`。

### Wwise 残留清理清单

已删除：
- `Assets/Wwise/` (~1.3GB，含 API、MonoBehaviour、Timeline、ProjectDatabase 等)
- `Assets/WwiseSettings.xml`
- `Assets/StreamingAssets/` (Wwise 占位文件)
- `threeKingdomSlayer_WwiseProject/` (Wwise 工程目录)
- 14 个根级 `.csproj` 文件 (AK.Wwise.Unity.*)
- 3 个 `WwiseUnityIntegration_*_Src.zip`
- `logRunSetup.txt`

### 工程结构

```
Assets/Scripts/Managers/AudioManager.cs  — 单例，DontDestroyOnLoad
├─ BGM: 2 个 AudioSource（_bgmMainSource + _bgmEnvSource），双层叠加循环
├─ SFX: 1 个 AudioSource（PlayOneShot）
├─ 音量: AudioListener.volume + PlayerPrefs(key: "master_volume")
└─ 场景切换: Battle unload 时自动 StopBGM
```

### AudioManager 序列化字段

| 字段 | 类型 | 内容 |
|------|------|------|
| `_bgmMain` | AudioClip | bgm_fight_stage1_1 (55.7s, CompressedInMemory) |
| `_bgmEnv` | AudioClip | battle_ev (23.2s, CompressedInMemory) |
| `_attackVoices` | AudioClip[] | playerattackvoice1/2/3（随机三选一, DecompressOnLoad） |
| `_attackTiles` | AudioClip[] | slash / stab（随机二选一, DecompressOnLoad） |
| `_parryClip` | AudioClip | parry (DecompressOnLoad) |

### API（兼容旧 Wwise 调用方）

| 方法 | 调用方 |
|------|--------|
| `PlayDefaultBGM()` | StageController.StartStage |
| `StopBGM()` | StageController（胜利/失败/主菜单） |
| `PostEvent("Player_Attack")` | AttackSystem.ExecuteStab/ExecuteSlash |
| `PostEvent("Player_Parry")` | AttackSystem.ExecuteParry |
| `SetMasterVolume(0~1)` | PauseMenuUI.OnVolumeChanged |
| `GetMasterVolume()` | PauseMenuUI.Start/OnPauseClicked |

### 播放逻辑

- **BGM**: 两轨同时 Play()，各 loop=true，无 FadeIn
- **Player_Attack**: 随机一条人声 + 随机一种刀剑音，PlayOneShot 叠加
- **Player_Parry**: 单次 PlayOneShot
- **音量**: AudioListener.volume 0~1，Scene 卸载时持久化到 PlayerPrefs

### Scene 中的 GameObject

- `AudioManager` (挂载 AudioManager 组件，3 个 AudioSource)
- `Main Camera` (AudioListener)

### 待做

- [ ] 敌人受击 SFX
- [ ] UI 点击 SFX
- [ ] Pierce/Sweep/Launch 攻击 SFX
- [ ] PCM WAV → OGG 压缩（目前约 14MB 未压缩）
<!-- locus:body:end -->
