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
updatedAt: 1780763484956
---

# wwise-audio-status

## Summary
Wwise 音频集成状态 — Bank清单、Event列表、触发规则、音量系统 (updated 2025-06-07)

<!-- locus:body:start -->
## 当前 SoundBanks

| Bank | 路径 | 用途 |
|---|---|---|
| Init.bnk | Windows/Android | Wwise 初始化 |
| Stage1_Bgm_Play.bnk | Windows/Android | Stage1 战斗 BGM（循环） |
| Player_Attack.bnk | Windows/Android | 玩家攻击 SFX |
| Player_Parry.bnk | Windows/Android | 玩家格挡 SFX |

## Event 触发规则

| Event | 触发位置 | 条件 |
|---|---|---|
| Stage1_Bgm1_Play | WwiseAudioManager.PlayDefaultBGM() | Stage1 进入 Battle |
| Player_Attack | AttackSystem.ExecuteStab/ExecuteSlash | 命中目标后 |
| Player_Parry | AttackSystem.ExecuteParry | 反弹飞行物 or 命中敌人 |

## Volume 音量系统

- RTPC: `MasterVolume` (0~100)
- 持久化: `PlayerPrefs` key `wwise_master_volume`
- 管理: `WwiseAudioManager.SetMasterVolume/GetMasterVolume/ApplySavedVolume`
- UI: `PauseMenuUI.volumeSlider`

## Bank 加载

- `WwiseAudioManager.Start()` 中同步加载所有 Bank
- 场景切换时 BGM Bank 不卸载（DontDestroyOnLoad）
- Battle → MainMenu 时 StopBGM + StopAll
<!-- locus:body:end -->
