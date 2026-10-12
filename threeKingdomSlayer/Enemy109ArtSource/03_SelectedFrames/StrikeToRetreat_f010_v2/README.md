# Enemy109：f010 共用 Strike 尾帧与 StrikeToRetreat 头帧（已抠图并部署）

用户指定原选帧第02张 `strikeToRetreat02_f010_t0.375.png` 为新的转身头帧和 Strike 尾帧，并授权抠图、替换部署。原12帧目录及MP4不改写，原f001保留作历史来源；本版从f010开始，共11张，源帧顺序为 **f010/027/035/039/043/072/075/079/087/104/145**。本版未补帧、重生成或全图调色，透明成品和游戏播放观感仍待用户目检。

## 最终透明素材与部署地址

960×960 RGBA成品，命名从 `strikeToRetreat01_f010.png` 到 `strikeToRetreat11_f145.png`：

```text
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\03_SelectedFrames\StrikeToRetreat_f010_v2\keyed_ui_t79
```

Unity 1108×1108精灵目录：

```text
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Assets\Sprites\Enemy\Enemy109
```

本轮新增 `Enemy_109_StrikeToRetreat1.png` 至 `Enemy_109_StrikeToRetreat11.png`，并覆盖 `Enemy_109_Attack18.png` 的PNG字节，原meta/GUID保持。Attack18与StrikeToRetreat1文件字节、RGBA像素完全相同；两端的局部衔接颜色和姿势一致，不表示其他既有帧/动作都已统一调色。

## 抠图与画布

- SVL现行配方：Chroma / 逐帧自动取键 / T79 / softness16 / halo1 / 半透明转不透明，不启用平滑或额外后处理。
- 7张代表帧小样及现有MountedIdle1同配方回归通过，批量11张960×960RGBA、Alpha只有0/255、幕布残留与透明隐藏RGB均0。
- 核心掩码共1,359,179像素次，删除0、改RGB0；14,321像素次边缘反混合变色单独记录，不称全图RGB未变。
- 部署按整幅Nearest16/15放大960→1024，放在1108画布左上 `(61,57)`；依据原头帧0.9375倍、(12,59)源空间映射逆变换，恢复原Strike尺寸/位置。未逐帧bbox fit或钉脚。PPU16、Center pivot(554,554)、Point、Uncompressed、无mipmap。
- 原12张RGB及源MP4哈希保持，清单和两份预览顺序回读通过。

## Unity接线与时序

- `Assets/Animations/Enemy_109_StrikeToRetreat.anim`：11键、30fps、0.366667秒源Clip、非Loop；只绑定根SpriteRenderer.m_Sprite，0空引用、0根位移/AnimationEvent。
- 109 Controller新增StrikeToRetreat状态，109 Prefab的CavalryVisualController.strikeToRetreatAnimation直接引用该Clip。RetreatPose改为本版f145末帧，转身完成后保持相同端点。
- 仅成功StrikeRecover→Retreating进入新转身段，按“返航排数×现有moveSpeed”调速播放一次；受阻暂停、恢复续播，HitStop同时冻结动画/计时。提交前打断仍用Interrupted占位，不共用成功转身。
- 现有strikeWindup0.18s、strikeRecover0.22s、moveSpeed0.2s/排及冷却未改，无额外玩法等待；自然背向奔跑Loop尚未部署，本段覆盖现有返航旅行时间，不冒充正式奔跑循环。

## 验证与边界

- 完整C#编译/域重载通过；真实StrikeRoutine为唯一1次32伤害且当帧Attack12，收招结束为已替换Attack18。
- 受阻在第5帧暂停，解除后不重播并到第11帧；提交前打断0伤害、HitStop冻结/续播、落马及对象池复用通过。
- 隔离场景驱动后真实CavalryCycle通过：row3→row1挡路者后一排→唯一伤害→新转身11帧各可见且只进入1次→回row3→TurnToIdle→Idle，恢复speed1。
- 未覆盖自然路线、真实手势、多骑兵和完整波次美术；短返航路径会按现有时长加速，实际速度读感待用户目检。
- 首次完整循环探针被场景UI的timeScale0暂停并超时，未作为通过记录；清理后改用临时停用场景驱动的独立列管理器重测成功。该隔离操作触发过1条DamageNumber停用对象无法StartCoroutine的测试环境日志，未改伤害数字系统，也不宣称Console零错误。针对性测试此前Console零warn/error。
- 最终Edit Mode，临时109探针已清理，Battle/Y场景回读均clean，未保存场景、Git暂存或提交。

## 仅供预览与追溯

```text
H:\Project\threeKingdomSlayer\threeKingdomSlayer\Enemy109ArtSource\03_SelectedFrames\StrikeToRetreat_f010_v2\review
```

- `strikeToRetreat_11_white.png`、`strikeToRetreat_11_neutral.png`、`strikeToRetreat_11_dark.png`：透明版三底总览。
- `strikeToRetreat_11_order_review.webp`：每帧125ms顺序预览，重复仅用于审阅，不是游戏时长。
- `strike_shared_boundary_deployed.png`：同1108画布下Attack17、新Attack18/转身首帧、转身第2帧中性底对照。
- `raw_manifest.json`、`matting_report.json`、`deployment_manifest.json`、`frame_list.csv`：来源、RGB/Alpha诊断、画布、哈希、备份与Unity接线。
- `runtime_validation.json`、`full_cycle_runtime_validation.json`、`unity_verification.json`：实际测试结果及覆盖范围。
- 原部署PNG、meta、Controller/Prefab/视觉控制器快照：`Library/Locus/tmp/backup_enemy109_strike_to_retreat_f010_20261011-232431/`。
