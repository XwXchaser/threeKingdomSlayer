# 音效素材来源与许可

本目录下（`Assets/SFX/` 及子目录）的第三方音频素材统一记录在这里。
**任何新素材入库前必须在本文件登记**：来源 URL、许可类型、下载日期。

---

## defend/defend_grip_lock.wav

| 项 | 内容 |
|---|---|
| 原始名 | Gear fast lock tap |
| 用途 | **玩家启用防御**（举起长枪 → 落防御位）那一刻的音 → `AudioManager._defendReadyClip` |
| 来源 | https://mixkit.co/free-sound-effects/metal/ （素材 id 2857） |
| 直链 | https://assets.mixkit.co/active_storage/sfx/2857/2857.wav |
| 许可 | **Mixkit Free Sound Effects License** — https://mixkit.co/license/#sfxFree |
| 许可要点 | 商业/非商业均可免费使用，**无需署名**；不可作为独立素材再次分发 |
| 下载日期 | 2026-11-05 |
| 规格 | WAV 44.1 kHz / 16 bit / 立体声；全长 1.14s，有效声 0.18s，起音 90ms，峰值 0.26 |
| 为什么是它 | 用户要「**轻微的握持声**」→ 候选：① Gear fast lock tap（0.18s 一声金属扣击）② Gear metallic lock sound（0.31s、起音 0.22s、听着像两声）③ Creaky wooden scrap（有效 1.76s，太长）→ 选 ①：短、轻、像「手扣上枪杆/卡扣到位」 |

> 音量：`_defendReadyVolume` 默认 0.8（素材峰值 0.26 → 实际很轻）。
> 不合意时在 `Battle.scene` 的 **Audio Manager** 组件上换 clip 或调音量，**不用改代码**。

---

## enemy/enemy_swing_metal_whoosh.wav

| 项 | 内容 |
|---|---|
| 原始名 | Metal hit woosh |
| 来源 | https://mixkit.co/free-sound-effects/sword/ （素材 id 1485） |
| 直链 | https://assets.mixkit.co/active_storage/sfx/1485/1485.wav |
| 许可 | **Mixkit Free Sound Effects License** — https://mixkit.co/license/#sfxFree |
| 许可要点 | 商业/非商业均可免费使用，**无需署名**；不可作为独立素材再次分发 |
| 下载日期 | 2026-11-05 |
| 规格 | WAV 44.1 kHz / 24 bit / 立体声；全长 0.48s，有效声 0.36s，起音 80ms |
| 用途 | 敌人攻击起手预警（`AudioManager._enemyAttackSwingClips`） |

## enemy/enemy_swing_dagger_whoosh.wav

| 项 | 内容 |
|---|---|
| 原始名 | Dagger whoosh |
| 来源 | https://mixkit.co/free-sound-effects/sword/ （素材 id 1487） |
| 直链 | https://assets.mixkit.co/active_storage/sfx/1487/1487.wav |
| 许可 | **Mixkit Free Sound Effects License** — https://mixkit.co/license/#sfxFree |
| 许可要点 | 同上 |
| 下载日期 | 2026-11-05 |
| 规格 | WAV 44.1 kHz / 24 bit / 立体声；全长 0.28s，有效声 0.26s，起音 100ms |
| 用途 | 敌人攻击起手预警（轻甲/快速敌人的变体，随机二选一） |

---

## 备注

- 这两条是 2026-11-05 为「敌人起手预警音」补的：原来那个位置借用了 **QTE 答题 UI 音**（`QTE_block1`），
  敌人每次抡招都「叮」一声，和 QTE 答题音一模一样 → 已换掉。
- 选型依据（按波形包络量化比较，避免只凭名字）：
  - `Sword slash swoosh`（id 1476）：有效声 0.64s，但只有 mp3 版本且尾部 1.4s 静音 → 未采用。
  - `Sword blade swish`（id 1506）：起音 0.52s，太拖，像「拖动」而非「挥砍」→ 未采用。
  - `Fast sword whoosh`（id 2792）：有效声仅 0.08s，一闪而过 → 未采用。
- 音量/音色不合意时，直接在 `Battle.scene` 的 **Audio Manager** 组件上换 clip 或调 `_enemyAttackTelegraphVolume`，
  **不需要改代码**。
- **格挡受击音是 `QTE_block1/2`**（用户 2026-11-05 明确指定）→ **不要擅自换成 guard_block 系列**。
