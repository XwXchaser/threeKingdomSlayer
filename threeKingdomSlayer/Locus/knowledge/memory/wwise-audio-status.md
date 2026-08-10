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
updatedAt: 1786280812030
---

# wwise-audio-status

## Summary
音频系统已从 Wwise 迁移至 Unity 原生 AudioSource + AudioListener.volume。Wwise 已完全移除。迁移中修复了 m_DisableAudio、AudioSource 缺失、preloadAudioData 三个关键问题。

<!-- locus:body:start -->
## 音频系统（已迁移至 Unity 原生）

**迁移完成日期**: 2025-07-09  
**技术栈**: Unity AudioSource + AudioMixer（Wwise 已完全移除）

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
├─ Mixer: Assets/Audio/ThreeKingdomSlayerAudioMixer.mixer
│  └─ Master → BGM（双层 BGM）/ SFX（PlayOneShot）
├─ 音量: Mixer 暴露 MasterVolume / BGMVolume / SFXVolume；PlayerPrefs 为 master_volume / bgm_volume / sfx_volume
└─ 场景切换: Battle unload 时自动 StopBGM
```

AudioListener.volume 在初始化时固定为 1，避免与 Mixer 总线重复衰减。暂停面板的总音量、背景音乐、音效三个 Slider 分别调用 `SetMasterVolume`、`SetBgmVolume`、`SetSfxVolume`；Slider 的 0~1 值经对数换算为 -80~0 dB。旧存档缺少 BGM/SFX 键时默认均为 1。Unity 的 AudioMixer 快照会在首帧覆盖 Awake 写入的参数，因此 `AudioManager.Start()` 会延后一帧再次应用已存档音量。

### AudioManager 序列化字段

| 字段 | 类型 | 内容 |
|------|------|------|
| `_bgmMain` | AudioClip | bgm_fight_stage1_1 (55.7s, CompressedInMemory) |
| `_bgmEnv` | AudioClip | battle_ev (23.2s, CompressedInMemory) |
| `_attackVoices` | AudioClip[] | playerattackvoice1/2/3（随机三选一, DecompressOnLoad） |
| `_attackTiles` | AudioClip[] | slash / stab（随机二选一, DecompressOnLoad） |
| `_parryClip` | AudioClip | parry (DecompressOnLoad) |
| `_slashHitClips` | AudioClip[] | slash_hit1/2（Slash 首次实际命中随机二选一, DecompressOnLoad） |
| `_stabHitClips` | AudioClip[] | stab_hit1/2（Stab 首次实际命中随机二选一, DecompressOnLoad） |

### API

| 方法 | 调用方 |
|------|--------|
| `PlayDefaultBGM()` | StageController.StartStage |
| `StopBGM()` | StageController（胜利/失败/主菜单） |
| `PostEvent("Player_Attack")` | AttackSystem.ExecuteStab/ExecuteSlash |
| `PostEvent("Slash_Hit")` | AttackSystem → Slash 首次实际命中回调 |
| `PostEvent("Stab_Hit")` | AttackSystem → Stab 首次实际命中回调 |
| `PostEvent("Player_Parry")` | AttackSystem.ExecuteParry |
| `SetMasterVolume/GetMasterVolume` | PauseMenuUI |
| `SetBgmVolume/GetBgmVolume` | PauseMenuUI |
| `SetSfxVolume/GetSfxVolume` | PauseMenuUI |

### 播放逻辑

- **BGM**: 两轨同时 Play()，各 loop=true，无 FadeIn
- **Player_Attack**: 随机一条人声 + 随机一种刀剑音，PlayOneShot 叠加
- **Slash_Hit / Stab_Hit**: 分别随机播放对应的两条命中 SFX；仅在本次 Slash/Stab 第一次实际命中敌人时触发，空挥不播放；同时避免同一攻击类型连续重复同一条音频
- **Player_Parry**: 单次 PlayOneShot
- **音量**: AudioMixer 的 Master / BGM / SFX 三档独立控制，参数与 Slider 值持久化。

### Scene 中的 GameObject

- `AudioManager` (挂载 AudioManager 组件，3 个 AudioSource)
- `Main Camera` (AudioListener)

### 待做

- [x] 敌人受击 SFX：已接入 `Enemy_Hit` 事件，随机音频池、0.14 秒全局冷却与连续重复规避均已实现；播放倍率固定为 0.8。
- [ ] UI 点击 SFX
- [x] 基础 Slash/Stab 命中 SFX：分别接入 `slash_hit1/2` 与 `stab_hit1/2`，仅首次实际命中播放；Pierce/Sweep/Launch 仍待单独设计
- [ ] PCM WAV → OGG 压缩（目前约 14MB 未压缩）
- [x] BGM/SFX 独立音量控制：已落地 AudioMixer 三档（总/BGM/SFX）及暂停菜单三 Slider。
<!-- locus:body:end -->
