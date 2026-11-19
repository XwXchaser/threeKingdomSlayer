---
id: kd_f66afdff-49f2-4d18-af74-e4dba02f391d
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_blades_sync_refusal_handoff_20261112
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
summary: 《上古卷轴：刀锋》离线化 —— ★续跑入口是本文件顶部「续跑简报（2026-11-19）」，§一到§二十一只是过程记录（不少结论已被推翻）。当前进度：客户端能自然认证 → 认下后端给的占位角色 → 拉齐全套状态 → 进入 Loading 加载屏、5 个加载任务各跑 9～10 次、0 托管异常（R11 消掉 CharacterSetting NRE）；但 FlowState::LoadFirstLevel(0x80510C) 在 0x805254 那道分派上落回出口② Fire(Authenticate) ⇒ 从不 LoadLevel(2,0) ⇒ 城镇关卡不加载。闸门已完整解码：X = 该接口 slot 23 的返回值，X=3 走 Fire、4/5 走 StartFTUEQuest、6 走 EnterTownPostFtue+LoadLevel、7 走 ValidateConsistency，由探针表现反推现值为 3；最便宜两条路：0x805258 的 e2400001(sub r0,r0,#1) 改成 e3a00005(mov r0,#5) 即强制出口③，或先把该处改成 b cave 把 X 打出来。已排除的闸门：FTUE=7、玩家等级30、CurrentLevelType=6、加载任务卡住、类型层。运维变更：计划任务 BladesOffline 与 blades_offline_server.cmd 自愈循环已删除（会把后端每 20 秒拉起来、关都关不掉），后端只在同一次 bash 调用内 nohup 起；开跑前先把 run_full.sh:172 的探针标签表改成 TSF TSQ TLF TLV TLD。
---

# Blades 离线化 · `/sync` 被「静默拒绝」定位 + 交接（2026-11-11 夜 ~ 2026-11-12）

> 上级交接：`plan/blades-handoff-20261029.md`（开机读物）、`plan/blades-sync-response-blocker-20261111.md`（上一轮堵点复盘）。
> 工作目录：`Library/Locus/tmp/blades/`（不依赖 Unity 主工程，**不要提交/推送**）。

## ★★★★★ 续跑简报（2026-11-19）——**新一轮从这一节接**

> 本文 2600+ 行，§一到§二十一是过程记录（不少中间结论后来被推翻）。**本节是当前唯一权威状态**，
> 只有要追证据链时才往下翻。逆向全程在 `Library/Locus/tmp/blades/` 里做，**与 Unity 主工程无关**（不需要开编辑器）。

### A. 一句话

客户端能：自然认证 → 认下后端给的占位角色 → 拉齐全套状态 → 进 `Loading` 加载屏、5 个加载任务各跑 9～10 次，
**0 托管异常**；但 `FlowState::LoadFirstLevel`(0x80510C) 在**门口那道分派**上落回
**出口② `FlowTrigger::Fire(Authenticate)`** ⇒ 永远不 `LoadLevel(2,0)` ⇒ **城镇关卡不加载**。

### B. 已经排除的闸门（别重复验证）

| 曾怀疑 | 实测 | 结论 |
|---|---|---|
| FTUE 状态 | `SetFtue` 每轮 9 次、每次都是 **7** | 不是它 |
| 玩家等级 | `PLAYER_LEVEL=30` 后 `TLV` 仍 0 | 不是它 |
| `LevelManager.CurrentLevelType=6` | 试过，无效 | 不是这个字段 |
| 加载任务卡住 | 5 个 task 各 9～10 次全跑 | 不是它 |
| 类型层（浮点 / Double→Int64） | 响应体浮点 0 处 | 已解决 |
| `CharacterSetting::RetrievePersistent` NRE | R11 后托管异常 0 条 | 已解决 |

### C. ★★★ 闸门已完整解码（2026-11-19 静态读码，本节已逐条复核）

第一道闸门在 **`0x805254`**（在跳转表分派之前）：

```
00805238  add r0, r3, r5, lsl #3     ; 接口偏移表
0080523c  ldr r0, [r0, #4]
00805240  add r0, ip, r0, lsl #3
00805244  add r0, r0, #0xbc          ; ★ 虚方法槽：4 + 23*8 ⇒ slot 23（0 基）
00805248  ldr r2, [r0]               ; methodPtr
0080524c  ldr r1, [r0, #4]           ; adjustor
00805250  mov r0, r4                 ; this
00805254  blx r2                     ; ★★ X = 存档服务接口第 23 号方法的返回值
00805258  sub r0, r0, #1
0080525c  cmp r0, #6
00805260  bhi #0x80538c              ; X∉[1..7] ⇒ 直接 pop 返回（什么也不做）
00805264  add r1, pc, #8
00805268  lsl r0, r0, #2
0080526c  ldr r0, [r0, r1]
00805270  add pc, r0, r1              ; 跳转表 @0x805274
```

跳转表（`0x805274` 起 7 个字，用 `struct` 直接解出，**已核对**）：

| X | 目标 | 做的事（已读码） | 探针表现 |
|---|---|---|---|
| 1、2 | 0x805290 | 走**第二道闸门** `0x805644`（接口 **slot 51**，`+0x19C`）；非 0 ⇒ `0x8057B4` = `GameplayManager::StartFtue`（教程路） | `TSF` 会命中 |
| **3** | 0x805390 | **出口②**：`b FlowTrigger::Fire`(0xA7381C, r1=0) = `Authenticate` | 4 个出口探针**全 0** ← **我们现在就是这条** |
| 4、5 | 0x805330 | **出口①**：`GameplayManager::StartFTUEQuest`(0xA86AD8) | `TSQ` 会命中 |
| **6** | 0x8053EC | ★★ **出口③**：`GameplayManager::EnterTownPostFtue`(0xA87128) → **`FlowState::LoadLevel(2,0)`**(0x804CD0) → `TelemetryManager::TrackFTUEQuestCompleted` | `TLV` 会命中 |
| 7 | 0x8054AC | **出口④**：`RestManager::set_IsCommunicationEnabled(true)` + `GameQuestManager::ValidateConsistency` | 无对应探针 |
| 其他 | 0x80538C | `pop {r4,r5,r6,sl,fp,pc}` 直接返回 | — |

**推论（重要）**：实跑里 `TSF=0 / TSQ=0 / TLV=0 / TLD=0` 且流程回到 `Fire(Authenticate)`
⇒ **X 只可能是 3**（1、2 走 StartFtue，4、5 走 StartFTUEQuest，6 走 LoadLevel，7 走 ValidateConsistency）。
⇒ 也就是说：**后端/客户端当前给出的那个枚举值是 3，而我们要它 = 6。**

### D. 下一轮（三步，都很便宜）

**第 1 步（先量，不改行为）**：把 `X` 打出来，确认真的是 3。
`0x805258` 是一条独立 4 字节指令（`sub r0,r0,#1` = `e2400001`），可以就地改成 `b cave`，
cave 里打印 r0 → 补做 `sub r0,r0,#1` → `b 0x80525c`。用 `build_vtrace.py` 现有洞区 + 4 字节 `b` 模式即可，
**不需要新洞**。

**第 2 步（要结果，一行立即数）**：直接强制 X=6 ——
把 `0x805258` 的 `e2400001`（`sub r0,r0,#1`）改成 **`e3a00005`（`mov r0,#5`）**，
于是 `cmp r0,#6` 通过、索引 = 5 ⇒ 跳转表第 6 项 ⇒ **出口③**（`EnterTownPostFtue` + `LoadLevel(2,0)`）。
★ 纯原地 4 字节改写：不占洞区、不动栈、无 cave ⇒ 最不容易自伤的一条。
★ 但它**绕过判据**：进得去城镇 ≠ 存档自洽 ⇒ 后面大概率立刻暴露新问题（这是预期，不是回退理由）。

**第 3 步（顺手做，有长期价值）**：认出接口 **slot 23** 是谁。
在 `dumper_out_v7a/dump.cs` 里找 `FlowState` 那个静态 `+0x184` 存档服务字段的**声明类型**，
按声明顺序数到第 23 个方法（0 基）——拿到名字才知道 X 是哪个枚举
（`LevelType` / `GameMode` / `TownState` 之类），也才知道该改后端哪个字段，而不是永久靠改指令绕过。

### E. 环境与运维现状（2026-11-19 变了，务必按新的来）

* **计划任务 `\BladesOffline` 与自愈循环 `blades_offline_server.cmd` 已删除**（原因与清除步骤见 §二十二）。
  它每 ~20 秒把 8443/8080 重新拉起，用户手动关都关不掉 ⇒ **一律不要重建、不要 `schtasks /run`**。
* 后端只在**同一次 bash 调用内** `nohup` 起（`run_full.sh` 已这么做），跑完随调用回收——这是**预期行为**，不是故障。
* 需要隧道就在**同一次调用**里 `adb reverse`，不要靠后台守护。
* 跑测命令（当前这套）：
  ```bash
  RESTART=0 PUSH_SO=1 WAIT=95 ROUTES=shapes/R_withchar.json bash run_full.sh
  ```

### F. ⚠ 下次开跑前必须先改的一处

`run_full.sh:172` 的探针标签表还是**旧的 18 个**（`TSF TFI TGC TLD TSR …`），
而当前构建只有 5 个钩子：**`TSF TSQ TLF TLV TLD`** ⇒ 上一轮跑出来的「全 0」有一半是**统计口径错**。
⇒ 开跑前先把那一行改成 `for t in TSF TSQ TLF TLV TLD; do`。

### G. 最后一次跑测的有效性（11-17 22:44，设备 .so = `0008cc01a6f9f731de42e85da6b81a80`）

不依赖那行坏掉的统计、直接从 `log_tail.txt` 手工数：
**`TLF`=9、`TLV`=0、`TSF`=0、`TSQ`=0、`TLD`=0**
⇒ 与 §21.21 结论一致：`LoadFirstLevel` 跑了 9 次、一次都没进城镇 ⇒ 走出口②。
（本轮没有新增跑测：模拟器与后端都没动，只有静态读码。）

## ★★★ 最新进展（2026-11-13 第三十五～五十轮）【历史，已被顶部「续跑简报」取代】

**一句话**：**认证风暴已彻底修掉**（根因不是客户端 bug，而是我们给的两个字段语义错了）；
**「/characters//…空 id」路由 bug 已修**；**ResetGame 判定点已钉死并用补丁绕过**；
**认证链的重新武装也做出来了**（洞 F）。
⇒ 客户端现在**不再重置、不再抛异常、只认证一次**，但**仍在「取存档 ↔ 加载内容」之间循环**，
**还没进到 FTUE（TutorialForest）、更没到建角**（见 §十二 的「剩下的卡点」）。

| 轮次 | 改动 | 结果 |
|---|---|---|
| 35 | 服务端：**默认发 `Server-Timestamp` 头** + `tokenExpirationSeconds` 改成**绝对 epoch 秒** | ★★ **认证风暴消失**：auth 请求 349 次/轮 → **1 次/轮**；探针 TRF 打出刷新时间戳 `0x6af7bb7c`（真实时间）而不再是 `57841`（1970 年） |
| 36 | PlayerPrefs 的 FTUE 状态 6 → **3**（FTUECutsceneDone）；加探针 TFW/TXX/TFT | 客户端**进了 FTUE**（analytics 载荷出现 `menu=TutorialForest`）；FTUE 任务 accept 请求回来了。⚠ 探针日志被 Unity 异常刷屏顶掉（logcat 环形缓冲） |
| 37 | 补 FTUE 任务的**目标 id**（从 APK 抽出的设计表拿到）；logcat 改**分段追加 dump** | 探针恢复可见：**TXX=4、TFT=4** ⇒ 与 4 比较的那个返回值 X=4 |
| 38 | 给「**空角色 id**」的端点单独配路由（`/characters//…`）；分析/统计端点换成**无数组**响应体；路由顺序改成「具体在前、兜底在后」 | ★★ 流程不再卡在 Authenticating，能走到 `OnSaveDataFetched`（TSF） |
| 39 | 客户端补丁 **修法 R1**：`bge → Fire(ResetGame)` 改成无条件 `b → StartLoadingContent` | ★★ **不再 ForceReset**；流程 `FetchingSaveData → InitializingBundles`；**Unity 异常 0 条**；卡在 InitializingBundles |

### 四个根因（都有实测/读码证据）

1. **认证风暴 = 会话刷新时间的两个输入都错**（§十一.1）
   `Session::CalculateSessionRefreshTime(0xE6EBD8)`：
   `now = Server-Timestamp(ms)/1000`；`remaining = tokenExpirationSeconds − now`；`refresh = ceil(remaining*0.67+1)`；
   `_sessionRefreshServerTimestamp = now + refresh`。
   我们之前**不发** `Server-Timestamp`（⇒ now=0）且把 `tokenExpirationSeconds` 当**时长**给 86400（⇒ remaining 巨负）
   ⇒ 刷新时间落在 1970/1989 ⇒ **客户端认为会话已过期 ⇒ 每 ~130ms 重发一次认证** ⇒ 流程空等 46s 才重置。
   ⇒ 现在：`Server-Timestamp` 默认打开 + `tokenExpirationSeconds = now+86400`（**绝对 epoch 秒**）⇒ 刷新时间在 ~16 小时后 ⇒ 风暴消失。

2. **`/characters//…`（空角色 id）落到通用兜底 ⇒ 响应体形状错 ⇒ 响应队列卡死**（§十一.2）
   客户端还没角色时请求路径中间是**空段**（`/characters//towns/current/rewards/current`），
   路由表里的 `{id}` 模板**能匹配空段**（`server.py::_match_template` 明写「占位符：任意值（含空）」）
   ⇒ 命中的是 `{id}` 路由/兜底 `/api/*` ⇒ 客户端拿到「全 schema 合并体」⇒
   `SearchArrayToDictionaryParser` 把数组转成「按 id 索引的字典」后按具体 id 取 ⇒ 取不到 ⇒
   **`KeyNotFoundException` 每帧抛一次** ⇒ 响应**永远反序列化不完** ⇒
   **排在它后面的认证响应也完不成** ⇒ 流程停在 `Authenticating`。
   ⇒ 现在：给空 id 形式单独配路由（响应体只放该端点要的东西）⇒ 异常清零、流程推进。

3. **ResetGame 的判定点**（§十一.3）
   `FlowState.<>c__DisplayClass144_0.<OnSaveDataFetched>b__0`（0xA72B28）尾部：
   ```
   0xA73610  blx r2                              ; 接口调用，返回值 X
   0xA73614  cmp r0, #4
   0xA73618  bge 0xA73650                        ; ★ X >= 4 ⇒ Fire(FlowTrigger.ResetGame)
   0xA73648  bl  FlowState::StartLoadingContent  ;   X < 4  ⇒ 正常路径
   0xA73688  ldr r4, [r0, #0x98]                 ; ← FlowTrigger.ResetGame（静态槽 0x98）
   0xA736A0  bl  FlowTrigger::Fire
   ```
   探针 TXX 实测 **X = 4** ⇒ 每轮都走 ResetGame ⇒ 无限重置循环。
   ⇒ 修法 **R1**（`patch_c_fire.py`，开关 `BLADES_FIX_R1=0` 可关）：
   把 `bge` 改成无条件 `b 0xA7361C`（`0xAA00000C` → `0xEAFFFFFF`）⇒ 无存档时继续「加载内容」而不是判成状态不一致去重置。

4. **FTUE 任务数据**（§十一.4）：任务 `73ab7435-021e-451a-9944-29e32e72382c`（MQ01_ComingHome）
   目标 `7e4ceb81-85d9-4536-9b9a-d04e728e7859`（DiscoverAreaObjective, quota 1）
   —— 出自 `tables/BGS.Game.Quests.DungeonQuestHolderScriptableObject.csv`（APK 抽出的 257 张设计表之一）。

### 现在的卡点（下一轮从这里接）

客户端进到 **`InitializingBundles`**（= `StartLoadingContent` 之后）后**静停**：不再发请求、不再进新状态、Unity 异常 0 条。
设备上 bundle 是全的（`/storage/emulated/0/Android/data/<pkg>/files/Bundles/` 2963 个 / 1.0G ✓），
文件服务器只被请求过 `ManifestExtraData`（2 次，200 ✓）⇒ **它没在下载 bundle，而是卡在「等某个本地事件」**。
下一步建议（按性价比）：
① 探 `FlowState::InitializingBundles` 相关状态处理函数 + `BundleDownloader`/加载完成回调（看它到底在等什么）；
② 查 `BundleMessage.RemoteManifestDownloadFailed`（字符串常量在 dump 里，客户端有「远端 manifest 下载失败」消息）是否被触发；
③ 探 `FlowState::LoadGameDataLibrary`（0x8029A4）/ `SetupGlobalManagers`（0x802E08）看内容加载走到哪一步；
④ 若确认是「动画事件没触发」，参考 `memory/blades-reverse-engineering-assets.md` §9：状态机靠动画事件点火 ⇒ 需要补事件或改走对应触发器。

### 本轮新增工具（都在 `Library/Locus/tmp/blades/`，纯本地）

| 文件 | 作用 |
|---|---|
| `disasm.py` | v7a 反汇编 + 方法名/池字标注（`python disasm.py 0xA72B28`） |
| `xref.py` | 扫 `bl` 调用点 / 池字引用（`--call` / `--pool`） |
| `poolscan.py` / `dump_poolrefs.py` | 扫常量池引用（**dump_poolrefs.py 一次导出 82 万条到 `poolrefs.txt`，之后离线 grep**） |
| `fire_which.py` | ★ 反查「哪个 `FlowTrigger::Fire` 调用点点了哪个触发器」（holder 偏移 → 名字） |
| `fix_quest_routes.py` / `fix_empty_id_routes.py` | 修任务响应形状 / 补空 id 专用路由 |
| `build_vtrace.py`（HOOKS 段） | 本轮探针集：TIE/TSF/TCA/TSC/TQR/**TFW**(触发器名)/**TXX**(比较值)/**TFT**(FTUE 状态) |
| `patch_c_fire.py`（新增修法 R1） | 无存档时不要 ResetGame |
| `run_full.sh`（改） | ★ logcat **分段追加 dump**（Unity 刷屏会把早期探针日志挤掉） |

> ⚠ 两条踩坑（务必记住）：
> ① **logcat 环形缓冲**：Unity 每帧抛异常时，早期的探针日志会被挤掉 ⇒ 看起来像「探针没生效」；
>    判读前先 `grep -a -oE " I [A-Z]{3} " log_tail.txt | sort | uniq -c` 看是不是 0。
> ② **IL2CPP 会合并相同方法体**：`SetFtue` 的入口地址可能被别的函数共用 ⇒ 探针打在它上面会打出别的函数的参数
>    （本轮 TFT=4 就是这么来的：所有 `bl SetFtue` 的实参只有 1/2/3/5/6/7，**没有 4**）。

---

## 〇、一句话现状（★以下是 2026-11-12 的旧结论，已被上面第三十五～三十九轮更新）

**上一轮的方向性结论全部作废**：不是响应体形状、不是 `Request-Index`、不是反序列化失败。
真正卡点是**请求完成阶段被静默拒绝**：

```
SerialBackendCommunicationService::AddRequestForCompletion(0x159B2BC)
  0159b344  ldr r4,[r5,#0x2c]!   ; r4 = this->_currentRequest
  0159b348  cmp r4, r7           ; r7 = 本该完成的请求
  0159b34c  popne {r4,r5,r6,r7,r8,sl,fp,pc}   ; ★ 不等 ⇒ 直接 return（不报错、不回调）
⇒ /sync 的响应回来时 `_currentRequest` 已经不是 /sync 请求 ⇒ 请求被丢弃
⇒ 回调（GetAllCharacters）永不触发 ⇒ 流程卡在「读存档」⇒ 认证风暴（每轮 300~600 次 auth）
```

**已排除**（本轮有据可查，别再走一遍）：
- 错误路径（`response._isError != 0`）：探针 TER = 0 ⇒ 没走。
- 「进待完成队列但没冲刷」（`_requestsAwaitingCompletion`）：探针 TAD 对 /sync = 0 ⇒ 没进。
- 「已完成但回调没触发」：探针 TCR 对 /sync = 0 ⇒ 根本没完成。

## 一、机理全图（静态读码，已核对 v7a `libil2cpp.so`）

```
Request.ProcessResponse(0x139E91C)                     ← 响应到达
  → ProcessResponseData → ResponseDeserializer.Deserialize → 反序列化
  → response._isError = isError | 非2xx | !ProcessResponseData返回值     (0x139EB38)
  → _completionHandler.AddRequestForCompletion(request)                (0x139EC1C)
      → SerialBackendCommunicationService::AddRequestForCompletion(0x159B2BC)
          ① r4(=_currentRequest) != r7(=请求) ⇒ popne 静默拒绝            (0x159b34c)  ★★本轮定位
          ② response._isError != 0 ⇒ 错误路径(HttpErrorsHandling)          (0x159b3c8)
          ③ response._deserializationComplete(+0x40)==0 ⇒ 加入待完成队列   (0x159b4a0)
             （等 RestManager::HandleRequestCompletions 冲刷；它只有两个调用点：
                SaveManager::HandleRequestCompletions / SavedDataServiceImplementation::BackendSyncronizationDone）
          ④ 否则 ⇒ 立刻 CompleteRequest(0x139DA90)
```

**为什么 /sync 会被换人**：串行语义**只由 `LateUpdate` 的守卫保证**
（`SerialBackendCommunicationService::LateUpdate(0x159B6F0)`：`ldr r1,[r0,#0x2c]; cmp r1,#0; bxne lr`），
而 `StartNextRequest(0x1589808)` **自己不检查 `_currentRequest`**。

## 二、三轮探针实测（关键数据）

| 轮次 | 探针 | 结果 | 结论 |
|---|---|---|---|
| 7（21:38） | TSU/TAR/TER/TAD/TCR/TGC/TBD/TLD/TSR/TGA | TSU=1、TAR=314、TER=0、TAD=312、TCR=313、**TSU 不在任何集合** | /sync 只在「入口命中」出现 ⇒ 被静默拒绝 |
| 8（21:51） | +TPS(串行 PerformRequest)、TCU(0x159b344 打 `_currentRequest`) | `TPS /sync` 在 T+0 命中；**T+11~19ms 又一个请求被 TPS 执行**；随后 `TAR /sync` + `TCU=那个别的请求` | ★ **两个请求几乎同时在飞 ⇒ `_currentRequest` 被换人** |
| 9（23:25） | 洞 C 打 `TIC: this / _currentRequest`（钩 StartNextRequest 中段 0x1589844） | **TIC = 0**，但 TPS = 385 | ★★ 矛盾未解（见 §四） |

## 三、本轮新增/改动的工具与补丁（都已落盘）

### 3.1 补丁 [I]（`patch_c_fire.py`，默认启用）
把「同时只允许一个请求在飞」下沉到 `StartNextRequest`：
- 钩点 `HOOK_C = 0x1589844`（`ldr r5,[r4,#0x18]`），洞 `CAVE_C = 0xA7BDA0`（探针区尾部之后）。
- 洞逻辑：`ldr r5,[r4,#0x2c]` → `cmp r5,#0` → 非 0 则**打日志 `TIC: <this> <_currentRequest>` 并直接返回**（不消费队列）；否则继续原流程。
- 旧诊断补丁 **[D]**（把 `LateUpdate` 的队列守卫 `bxne lr` 改成 nop）已**关闭**：它恰恰是**破坏**串行语义的原因
  （允许同时在飞多个请求 ⇒ 换人 ⇒ 拒绝完成 ⇒ 认证风暴）。开关：`BLADES_NO_QUEUE_GUARD=1` 保留守卫。
- **[E] StartNextRequest 优先级过滤 bge→b**、**[G] IsOmittedContext 恒 false** 仍默认开启（历史诊断补丁，优先级低，可关）。

### 3.2 新增工具
| 文件 | 作用 |
|---|---|
| `which_method.py`（副本 `_wm.py`） | **RVA → 方法名**解析（用 `dumper_out_v7a/script.json`）★ 静态分析主力 |
| `find_stores.py` | capstone 全量扫「对某字段偏移的写指令」（噪声大，配合方法名过滤才有用） |
| `scan_field_writers.py` | 半成品（按 imm12 扫，会漏掉「基址预加寄存器」的情况，**别用**） |

**★ 环境坑（会浪费很多时间）**：bash 里跑 `python xxx.py` 时**经常**报 `ModuleNotFoundError: capstone`。
两个有效解法：① `export PYTHONPATH="H:\\locus\\data\\managed-python\\windows-x64\\python-3.13\\site-packages"`；
② 用 Locus 的 `python` 工具（不受影响）。`cp` 出来的副本有时能跑、有时不能，**别在那上面浪费时间**。

### 3.3 探针系统现状（`build_vtrace.py`）
- 洞区：`PROBE_BASE=0xA7BADC` 起，'reg'/'nopro' 每探针 0x48 字节 ⇒ **最多 11 个**（现用 9 个）。
- `'nopro'` 模式只能打**一个寄存器**；要打两个需自己加 `'reg2'` 模式（本次未加）。
- ⚠ 'nopro' 钩在 **会改 flags 的指令之后**要小心：log 调用会破坏 flags。本轮只钩了 `ldr`（不改 flags）⇒ 安全。
- 每次 build 后**必须看输出里有没有「跳过」**（非 STMDB 序言的函数会被跳过 ⇒ 那个 tag 恒 0，是**假 0**）。

## 四、★★ 复核结论（2026-11-12）：「矛盾」是**误读 + 空补丁**，真正卡点已定位到两处

> ⚠ 本节**推翻了**上一版的 §四 结论。下一步不再是把 `TIC` 挪位置，而是先做 §九 的三项探针。

### 4.1 `TIC = 0` **不是**「洞一次都没执行」——是探针只打在一条分支上

洞 C（`patch_c_fire.py`）的代码是：

```
0x00a7bda0  ldr r5, [r4, #0x2c]     ; r5 = this->_currentRequest
0x00a7bda4  cmp r5, #0
0x00a7bda8  bne #0xa7bdb4           ; ★ 只有 != 0 才打日志
0x00a7bdac  ldr r5, [r4, #0x18]      ; == 0 ⇒ 静默走原流程（**不打任何日志**）
0x00a7bdb0  b  #0x1589848
0x00a7bdb4  push {r0,r1,r2,r3,lr}   ; ← 唯一会打 TIC 的分支
```

⇒ **`TIC=0` 的真实含义是「每次进 `StartNextRequest` 时 `_currentRequest` 都是 0」**，
而不是「洞没执行」。上一版 §四 的推断（「洞没被走到 / 走了别的分支」）**作废**。

已做的排除性核实（全部有据）：
* 设备 `.so` md5 `9f464205d573d2865af8e31ff065a5a9` == 本地 `build/libil2cpp.c.so` ⇒ **跑的确实是带洞的版本**（不是旧 .so）。
* Capstone 逐条反汇编：`0x1589844: b #0xa7bda0` → 洞首指令 `ldr r5,[r4,#0x2c]` ⇒ **洞装得对**。
* `StartNextRequest`(0x1589808) 的**两条**入口路径（`bne 0x1589844` 与「一次性初始化块」末尾）
  **都到得了钩子**，函数体前面**没有**提前 return ⇒ 「洞没被走到」不成立。

### 4.2 ★ 补丁 [I] 在结构上**不可能生效**（空补丁）

`StartNextRequest` **只会在槽位空时被进入**：

* `LateUpdate`(0x159B6F0)：`ldr r1,[r0,#0x2c]; cmp r1,#0; bxne lr` ⇒ **有请求在飞就不进来**；
* `ContinueWithNextRequest`(0x159B538)：先 `CleanCurrentRequest`（把 `_currentRequest` 清 0）再进。

⇒ 洞里的守卫**永远看不到非 0 的 `_currentRequest`** ⇒ 永远不触发 ⇒ **`TIC` 永远打不出来**。
⇒ 「补丁 [I] 生效后 TIC=0 而 TPS=385」**不是矛盾**，而是「一个永远不触发的守卫」。
⇒ **原 §四 的修法方向（在 `StartNextRequest` 补守卫）必须废弃**。

### 4.3 ★ 换人机制找到了：`Serial::PerformRequest` 会**无条件覆盖** `_currentRequest`

```
0159aedc  Serial::PerformRequest 入口（探针 TPS 打 r1=被执行的请求）
0159af28  bl  0x11a4c88            ; 基类 PerformRequest：request._completionHandler = this
0159af2c  str r5, [r4, #0x2c]!     ; ★★ this->_currentRequest = request（无条件覆盖！）
```

⇒ **任何在别的请求还在飞时启动的请求，都会把 `_currentRequest` 抢走** ⇒
迟到的那个请求回来时 `cmp r4,r7` 必然不等 ⇒ 被静默拒绝。

日志里两处独立实证（`log_tail.txt`）：

```
31.681 TPS 8f255660   (A 起飞)
31.775 TPS 8f255330   (B 起飞 —— A 还在飞)
31.883 TAR 8f255660 ; TCU 8f255330  ⇒ A 被拒
31.957 TAR 8f255330 ; TCU 8f255330  ⇒ B 通过
32.019 TPS 8edd2c38   (/sync 起飞)
32.038 TPS 8f255330   (B 又起飞 —— /sync 还在飞)
32.057 TAR 8edd2c38 ; TCU 8f255330  ⇒ ★ /sync 被拒
```
后端侧同时看到 **391 次 `POST /auth/gplus`** 对 **1 次 `GET /sync`** ⇒
**认证风暴与 `/sync` 交错**，正是抢槽位的来源。

### 4.4 ★ 还有一条**没人想过**的拒绝路径：`r7` 可能被**类型校验**清零

`AddRequestForCompletion` 在做身份比较**之前**，先拿请求做了一次类型/注册表校验：

```
0159b2c4  mov r8, r0          ; r8 = this
0159b2cc  mov r6, r1          ; r6 = 请求
0159b2fc  mov r7, #0          ; r7 = 「通过校验的请求」，默认 0
0159b308  ldr r0,[pc,#0x204]   ; 全局对象
0159b30c  ldr r1,[r6]         ; r1 = 请求->klass
0159b314  ldrb r3,[r1,#0xb4]   ; 请求的 class id
0159b31c  ldrb r2,[r0,#0xb4]   ; 全局的 class id
0159b320  cmp r3,r2
0159b324  blo 0x159b340        ; ★ 请求 class id 更小 ⇒ r7 保持 0
0159b328  ldr r1,[r1,#0x64]    ; 请求 klass 的方法表
0159b32c  mov r7, r6          ; r7 = 请求
0159b334  ldr r1,[r1,#-4]
0159b338  cmp r1, r0
0159b33c  movne r7, #0         ; ★ 不匹配 ⇒ r7 = 0
0159b348  cmp r4, r7           ; ★★ r4 = _currentRequest，r7 = 请求(可能已被清成 0)
0159b34c  popne {...}          ; 不等 ⇒ 静默 return
```

⇒ **若类型校验不过，`r7` 就是 0**，此时 `cmp r4,r7` 变成「拿 `_currentRequest` 和 0 比」⇒
**照样拒绝，但根本没发生「换人」**。
⇒ 上一版 §四 的结论（「被换人」）隐含假设了 `r7 == /sync`，而 **`r7` 从来没被观测过**
（探针 `TCU` 只打 `r4`）⇒ **两种可能都还没排除**。

### 4.5 下一步（已改，见 §十）

1. **加 `'reg2'` 探针模式**（同一探针同时打两个寄存器）——上一版 §四 提到但没做。
2. 在 `0x159b344`（`ldr r4,[r5,#0x2c]!`，**不改 flags**，安全）打 `r4`(=_currentRequest) + `r7`(=校验后的请求)
   ⇒ 一次跑测即可判定「换人」还是「类型校验清零」。
3. **洞 C 挪到 `StartNextRequest` 入口 `0x1589808`** 并**无条件**打日志（不再只在 !=0 分支打）
   ⇒ 变成真正的计数器（`TIC`=进入次数、`TIR`=入口时的 `_currentRequest`）。
4. 目标信号不变：`TGC`≥1 ⇒ `TGA`≥1 ⇒ 后端看到 `GET /api/game/v1/public/characters`
   ⇒ 我们回 `{"requestIndex":0,"characters":[]}` ⇒ 客户端自己走**建角**流程（用户要的「不绕建角」）。

## 五、复跑命令（当前设备上的 .so 已是带洞 C + 9 探针的版本）

```bash
cd H:/Project/threeKingdomSlayer/threeKingdomSlayer/Library/Locus/tmp/blades
export MSYS_NO_PATHCONV=1
export PYTHONPATH="H:\\locus\\data\\managed-python\\windows-x64\\python-3.13\\site-packages"

# ① 重建（改 build_vtrace.py 的 HOOKS / patch_c_fire.py 的洞）
export BLADES_NO_QUEUE_GUARD=1      # ★ 必须：保留 LateUpdate 的串行守卫
python build_vtrace.py && python patch_c_fire.py
#    ⇒ build/libil2cpp.c.so（**不必重打 1.2GB APK**，热替换即可）

# ② 跑测（adb server 每次 bash 调用会被回收 ⇒ reverse 隧道必须同一次调用内完成）
RESTART=1 PUSH_SO=1 INSTALL=0 WAIT=75 ROUTES=shapes/R_perchar.json bash run_full.sh
#    RESTART=1 必用：adb server 重启后第一次 adb reverse 会让雷电设备通道 offline
#    （症状：reverse 后 shell: []），必须**完整重启雷电**（ldconsole quit → schtasks launch → 等 120s）

# ③ 分析
python - <<'PY'
import re
lines=open('log_tail.txt',encoding='utf-8',errors='replace').read().splitlines()
ev=[(m.group(1),m.group(2)) for m in
    (re.search(r'I (TSU|TAR|TCU|TCR|TPS|TIC|TGC|TBD|TLD|TGA) +: ([0-9a-f]+)',l) for l in lines) if m]
sets={}
for t,a in ev: sets.setdefault(t,[]).append(a)
for k in sorted(sets): print('%-4s n=%-5d unique=%-5d'%(k,len(sets[k]),len(set(sets[k]))))
tsu=set(sets.get('TSU',[]))
for k in ('TAR','TCU','TCR','TPS','TIC'): print(' TSU in',k,'?',bool(tsu&set(sets.get(k,[]))))
PY
```

## 六、关键地址表（v7a `libil2cpp.so`）

| 地址 | 含义 |
|---|---|
| 0x159B2BC | `SerialBackendCommunicationService::AddRequestForCompletion`（★拒绝点） |
| 0x159b344 | `ldr r4,[r5,#0x2c]`（读 `_currentRequest`）—— 探针 TCU |
| 0x159b34c | ★★ `cmp r4,r7 ; popne`（**静默拒绝**） |
| 0x159b3c8 | 错误路径（`_isError!=0` ⇒ HttpErrorsHandling）—— 探针 TER |
| 0x159b4a0 | 「加入 `_requestsAwaitingCompletion`」—— 探针 TAD |
| 0x1589808 | `SerialBackendCommunicationService::StartNextRequest`（洞 C 目标） |
| 0x1589844 | StartNextRequest 里取队列那条指令（洞 C 当前钩点） |
| 0x159B6F0 | `LateUpdate`（串行守卫 `bxne lr` @0x159B6F8，旧补丁 [D] 曾把它 nop 掉） |
| 0x159AEDC | `SerialBackendCommunicationService::PerformRequest` —— 探针 TPS |
| 0x1588BD0 | `ParallelBackendCommunicationService::PerformRequest` —— 探针 TPP |
| 0x139DA90 | `Request.CompleteRequest` —— 探针 TCR |
| 0x1392148 | `SyncRequestIndexRequest.Setup` —— 探针 TSU（/sync 请求对象地址） |
| 0x15978A0 | `SavedDataServiceImpl.GetAllCharacters` —— 探针 TGC（目标信号） |
| 0x1597BA8 | `SavedDataServiceImplementation.BackendSyncronizationDone` —— 探针 TBD |
| 0x11A50B4 | `BackendCommunicationService::CompleteRequest`（`_requestCompleted` 守卫 @0x11A5100） |
| 0x11A4C88 | 基类 `PerformRequest`：**只设 `request._completionHandler = this`**（写在 `Request+0x5c`），不启动请求 |
| 0x159AF2C | ★★ `Serial::PerformRequest` **无条件写** `this->_currentRequest = request`（换人机制本体） |
| 0x159B308–0x159B33C | ★ `AddRequestForCompletion` 的**类型/注册表校验**：不过就把 `r7` 清零（另一种拒绝路径） |
| 0x159B344 | `ldr r4,[r5,#0x2c]!`（读 `_currentRequest`）—— 探针 TCU **改在这里打 r4+r7**（不改 flags，安全） |
| 0x159B348 | ★★ `cmp r4, r7`（身份比较）——`r7`=校验后的请求（可能被清 0） |
| 0x1589798 | `Serial::CleanCurrentRequest`（**清 `_currentRequest`**；`r1==1` 表示还要走完成流程） |
| 0x159B538 | `Serial::ContinueWithNextRequest`（先 Clean 再 `StartNextRequest`） |
| 0x1B77CB8 | `Response.ProcessResponseData`（反序列化入口） |
| 0x1189084 | `ResponseDeserializer.Deserialize`（MiniJSON→PersistentObject→转换文件） |
| 0x1589798 | `SerialBackendCommunicationService::CleanCurrentRequest`（会清 `_currentRequest`） |
| 0x159B538 | `SerialBackendCommunicationService::ContinueWithNextRequest`（先清当前请求再 StartNextRequest） |

**`Request` 基类字段偏移**（dump.cs TypeDefIndex 8735）：`_currentWebRequest`0x28 / `_currentWebCoroutine`0x2C /
`_wasCancelled`0x44 / `_requestCompleted`0x60 / `_response`0x64 / `_roundTripTimer`0x68 / `_retryData`0x6C /
`_requestPriority`0x70。`Response`（TypeDefIndex 9003）：`_responseData`0x1C / `_responseIndex`0x20 /
`_backendData`0x3C / `_deserializationComplete`0x40。

## 七、踩坑清单（本轮新增）

1. **`Request-Index` 只在响应头里有意义**；客户端 `SetCurrentReponseIndex(0x1191F48)` 对 **-1 直接忽略**
   （`cmn r5,#1 ; popeq`）⇒ 服务器不给这个头 ⇒ 客户端计数器停在 0（实测 612 个请求全带 `Request-Index: 0`）。
   ⇒ 服务器**必须回递增**的 `Request-Index`（`server.py --echo-index-seq` 已实现，实测能让客户端计数前进）。
2. **`/sync` 请求本身不带 `Request-Index` 头**（`REQUEST_TYPE_REQUIRE_REQUEST_INDEX` 决定），但它的**内部索引**
   仍是服务端的静态计数器值。
3. 探针「命中 0」有两种：真 0 和**假 0**（探针没装上：build 输出里出现「跳过（非 STMDB 序言）」）。
4. `nopro` 钩在 `popne`/`cmp` 之后会**破坏 flags**（log 调用改标志位）⇒ 只能钩不改 flags 的指令。
5. `patch_c_fire.py` 里 `struct.pack_into('<I', so, ADDR, ...)` 用的是 **RVA 当文件偏移**；
   代码段 vaddr==offset（可），**RW 段差 0x1000（不可）**。
6. `/sync` 响应体形状**已排除**（6 种变体全试过：str/num/带 Session/带 SaveGame/带 BladeBackendResponseIndex/嵌套 BackendData）。
   客户端自带的 schema：`schemas_out/requestIndex.schema` = 根键 `requestIndex`（**String**）；
   `schemas_out/BackendData.schema` = 根键 `requestIndex`（Int32）+ Session + BackendCharacterData + BackendCharacterListData。
7. **`.so` 热替换比重装 APK 快 10 倍**：`adb push build/libil2cpp.c.so /data/local/tmp/` → `su -c cp` 到
   `/data/app/<pkg>-N/lib/arm/`（包目录后缀会变，用 `pm path` 现取）→ `md5sum` 校验两边一致；**模拟器重启会还原**。
8. `TIC`/`TPS` 这类**同一函数被多个类共用**的情况要注意（IL2CPP 会合并相同方法体）。
   ⚠ 但 `0x159AEDC`（`Serial::PerformRequest`）经 `script.json` 核对**只挂一个方法** ⇒ 不是合并体。
9. ★★ **探针「命中 0」有第三种含义：探针只打在一条分支上**（本轮 `TIC` 就是）。
   ⇒ 判读纪律：先看探针**代码本身**在什么条件下才打印，再下「函数没被调用」的结论。
   ⇒ 同 §七.3（探针没装上＝假 0）、§10.4（没 STMDB 序言被跳过＝假 0）并列，是第三类「假 0」。

## 八、本轮没做/没验证的

- 没有验证「修好串行语义后流程真的能走到建角」——**这是最终目标**（用户要的是客户端自己建角，不要后端代建）。
- 响应体形状那一路**已证伪**，但**没有**重新验证过「修好拒绝之后 body 是否还需要调」——
  §四 第 3 步拿到 TGC≥1 之后再回头调 body。
- 旧补丁 [E]/[G] 一直没关（理论上无害）；若修好后仍不通，先把它们关掉再试。

## 九、★★★★ 2026-11-12 第十一轮：**突破**——修法 J 让 `/sync` 响应完成它自己的请求

### 9.1 一句话

**`/sync` 这个卡点已经解开。** 客户端现在能：
`/sync` 响应到达 → **完成 /sync 请求** → `SavedDataServiceImpl::GetAllCharacters`(TGC) 触发
→ `CharacterService::GetAllCharacters`(TGA) 触发 → 后端收到 `GET /api/game/v1/public/characters`
⇒ 流程已进入「读角色列表」阶段（**距建角只差一步**）。

### 9.2 修法 J 是什么（代码已落盘：`patch_c_fire.py`，开关 `BLADES_FIX_J=0` 可关）

`Serial::AddRequestForCompletion`(0x159B2BC) 里那句静默拒绝：

```
0159b348  cmp r4, r7          ; r4 = this->_currentRequest, r7 = 真正响应的那个请求
0159b34c  popne {..}          ; ★ 不等 ⇒ 直接 return（不报错、不回调）
0159b360  ldr r4, [r4, #0x64] ; 后续把 r4 当作「当前请求」用（取它的 _response）
```

⇒ **修法 J**：把「拒绝」改成「**用真正响应的那个请求**」：

```
cave J @0xA7BE60:
  cmp  r7, #0
  beq  L_pop          ; r7==0（类型校验不过）⇒ 保持原行为（弹栈返回），不引入崩溃点
  mov  r4, r7         ; ★ 用真正响应的请求（后续 ldr r4,[r4,#0x64] 取的是它自己的 _response）
  b    0x159b350
L_pop:
  pop  {r4,r5,r6,r7,r8,sl,fp,lr}
```

**为什么这样是对的**：客户端的串行语义假设「同一时刻只有一个请求在飞」；
影子后端 + 模拟器延迟会让两个请求重叠，槽位被后来者覆盖 ⇒ 先到者的响应被丢。
**把响应交回它自己的请求**才是这个函数本来的语义（`r7` 已经过类型/注册表校验）。

### 9.3 第十轮/第十一轮的实测证据

| 观测 | 结论 |
|---|---|
| `TCU` 第二个数（`r7`）**从来不是 0**（348/348） | **排除**「类型/注册表校验清零」路径 |
| `TCU`（/sync 那次）= `r4=8fa202a8`、`r7=8f4a4a18` | **确认是「换人」**：另一个请求抢走了 `_currentRequest` |
| `TIC` 进入 **347** 次，入口时 `_currentRequest` **恒为 0**（347/347） | **实证**「StartNextRequest 只在槽位空时被进入」⇒ 上一版补丁 [I] 确实是**空补丁** |
| `TCL`（`CleanCurrentRequest`）**0 命中** | `_currentRequest` 不是被「取消/清理」路径清掉的 ⇒ 纯粹是**被覆盖** |
| 时间线 | `42.416 TPS /sync` → `42.426 TPS 另一个请求` → `42.434 TAR /sync`（被拒） |
| 修法 J 生效后 | `TGC=2`、`TGA=2`；后端收到 `GET /api/game/v1/public/characters` ×2 |

### 9.4 当前新的堵点（第十一轮末尾）

客户端拿到**空角色列表**（我们回 `{"requestIndex":0,"characters":[]}`）后：
* **没有**发 `POST /characters`（建角）；
* 反而发了 `POST /api/game/v1/public/characters//quests/<uuid>/accept`（**角色 id 为空**）；
* 00:37:50.639 抛 **NRE**：`BGS.Shared.Bundles.DependencyHandle.<OnLoaded>b__23_0`
  → 反汇编定位到抛点 `0x121e51c`：**`this+0x18` 的依赖列表为空**（bundle 依赖缺失）；
  这个 NRE 在**所有历史日志里从未出现过**（log_v46/v50/v51/v53/log_tap 全为 0）⇒ 是客户端走到新阶段才暴露的；
* 然后 10s 后流程重置：`/sync` → 角色列表 → …… 循环。

### 9.5 第十二～十三轮补充（2026-11-12 晚）

| 轮次 | 探针 | 结论 |
|---|---|---|
| 十二 | `TSF`(FlowState::OnSaveDataFetched)、`TCS`(OnGetAllCharacters)、`TGM`(OpenCharacterCustomizationMenu)、`TCR`(CreateCharacterRequest::Setup) | `TGM`=0、`TCR`=0 ⇒ **流程没去开建角界面，也没发起建角请求** |
| 十二 | `TSF` | ⚠ **参数读反了**：`r0`=saveGameFound=**0**（无存档 ✓）、`r1`=isAuthenticated=**1** ⇒ 客户端**正确知道没有存档** |
| 十三 | `TEL`(FlowState::ShouldShowEula) | 返回值 **0** ⇒ **本来就不弹用户协议**（不是卡点） |
| 十三 | 截图 | 客户端停在「**网络错误 / 发生未知错误，请重试。**」弹窗（见 `screen_now.png`） |

⚠ **存档不在平铺文件里**：`/data/data/<pkg>/files/{SaveGame,LocalData,Info}/` 是**目录**（且当前为空），
跑测脚本里的 `rm -f SaveGame.json SaveData.json BackendDataJsonFullSaveData.json` **删了个空**。

⚠ **客户端在启动瞬间就带着空角色 id 发请求**：
```
POST /characters//towns/current/rewards/current      ← 角色 id 为空
POST /characters//quests/<uuid>/accept                ← 角色 id 为空
GET  /sync
GET  /characters
```
⇒ 它**没有角色却进了游戏流程**（而不是去选角/建角）。

★ **EULA 的真实机制（已查清，供以后用）**：
`FlowState::ShouldShowEula` → `EulaStatus::IsEulaShown` → `PlayerPrefs.GetInt("acceptedLegalDocumentIds", 0)`
（`PlayerPrefs` 文件名 = `<pkg>.v2.playerprefs.xml`；写入 = `SetInt` + `Save`）。
已实现 `seed_prefs.py` / `seed_prefs.sh`（并接进 `run_full.sh`）可预置该键=1；
但实测 `ShouldShowEula` 本来就返回 0 ⇒ **它不是当前卡点**（保留该脚本无害）。

### 9.8 第十四～十六轮：认证风暴的真正形态

| 轮次 | 改动 | 结果 |
|---|---|---|
| 十四 | 加 `TER`(`_isError` 错误分支) 与 `TAD`(待完成队列) 探针 | **`TER`=0、`TAD`=533** ⇒ auth 响应**既没报错、也没完成**，全部进了待完成队列 |
| 十五 | 服务端：响应体 `requestIndex` 回填**请求头里的 Request-Index** | 无变化（TAD 仍 533、auth 仍 536） |
| 十六 | 服务端：响应体 `requestIndex` 与 `session/*` 改成**字符串**（客户端 schema 全是 String） | 无变化（TAD 仍 533、auth 仍 535） |

⇒ **认证风暴 = 每个 auth 响应都进了「待完成队列」而从未完成** ⇒ 客户端不断重发认证。
⇒ 队列的冲刷点只有两个：`RestManager::HandleRequestCompletions`(0x01192168)、
`SavedDataServiceImplementation::BackendSyncronizationDone`(0x1597BA8) ⇒ **第十���轮探这两个**。

⚠ 认证风暴**可能是下游症状**：每轮 `/sync` + `GET /characters` 只发生 **1 次**，
说明流程**确实走过了认证**、走到了「读角色列表」；风暴在后台反复刷。
⇒ **客户端真正的卡点是拿到「空角色列表」之后没有去开选角/建角界面**（`TGM`=0、`TCR`=0），
   而屏幕上是「**网络错误**」框（`screen_now.png`）。

### 9.9 第十七轮：认证响应的完整生命周期（已用探针逐环节钉死）

`AddRequestForCompletion`(0x159B2BC) 里的判定链：

```
0159b344  ldr r4,[r5,#0x2c]!    ; r4 = this->_currentRequest（修法 J 之后 = 真正响应的那个请求）
0159b360  ldr r4,[r4,#0x64]     ; r4 = request._response
0159b364  cmp r4,#0 / beq ③     ; 没有响应 ⇒ ③
0159b36c..039c  响应类型校验      ; 不通过 ⇒ ③
0159b39c  beq ④                  ; 通过 ⇒ ④ 立刻完成
0159b3a0  ③ 路径 …
0159b408  ldrb r0,[r4,#0x40]     ; ★ response._deserializationComplete
0159b410  beq 0x159b4a0          ; ★ 还没反序列化完 ⇒ 入队（③）
0159b4a0  入队 + 清 _currentRequest + 尾调 StartNextRequest
```

**实测（第十七轮）**：

| 探针 | 结果 | 含义 |
|---|---|---|
| `TRS`(0x159b360) | 532，**指针非 null** | 响应**确实挂在了请求上** ✓ |
| `TDE`(0x159b408) | 532，**全为 0** | 到达完成判定时，**反序列化还没完** |
| `TDC`(0x1B786E4) | 535，**比 TDE 晚 ~9ms，且对象地址与 TRS 相同** | 反序列化**随后完成了** ✓ |
| `TAD`(0x159b4a0) | 532 | ⇒ 响应全部**进了待完成队列** |
| `TIC`(洞 C) | 532（与 TAD 一一对应） | 每个入队响应后面都跟着一次 StartNextRequest ✓ |

⇒ **根因**：反序列化是**异步**的（晚 9ms），而完成判定在它之前 ⇒ 响应入队；
**队列必须由 `RestManager::HandleRequestCompletions` 冲刷**，而它**从没被调用**。

**冲刷链（静态读码）**：
```
RestManager::HandleRequestCompletions (0x1192168)          ← 全库只有 2 个调用点
  └→ Base::HandleRequestCompletions (0x1192214)
       └→ Base::CallCompletionOnResponses (0x11A4FA8)
            └→ Serial::CallCompletionOnResponses (0x159B6D0)
                 └→ Base::CallCompleteDeserializationOnResponses (0x11A4E00)
                      └→ Response::CompleteDeserialization (0x1B786E4)
```
两个调用点：
1. `SaveManager::HandleRequestCompletions` (0x00fff8fc) —— 需要一个**本地存档对象**（无则 `mov r0,#0; bl 0x7c0060` 抛异常），
   且它自己的调用点是**虚调用**（全库无直接 `bl`）。
2. `SavedDataServiceImpl::BackendSyncronizationDone` (0x1597cc0) —— **只在 `response == null` 的分支里**！

⇒ **要让队列被冲刷，要么有本地存档，要么让 `/sync` 走「response == null」分支。**
   `SessionServiceImplementation::OnSyncResponse`(0x159BE3C) 决定传什么：
   * 成功路径（`_isError == 0`）⇒ 回调收到 `response._responseData`（**非 null**）⇒ 不冲刷；
   * 错误路径（`_isError != 0`）⇒ 类型校验不过时 `r6 = 0` ⇒ 回调收到 **null** ⇒ **冲刷** ✓。

⇒ 候选修法（待验证）：
* **服务端**：把 `/sync` 的响应做成让客户端走「无数据」路径（形状/字段不对 ⇒ 反序列化结果为空）；
* **客户端补丁（修法 K）**：在 `OnSyncResponse` 的错误路径把 `r6` 强制为 0（0x159c140 `mov r6,sl` → `mov r6,#0`），
  让回调收到 null ⇒ 队列被冲刷 ⇒ 认证完成、风暴停止。

### 9.10 第十八～十九轮：★ **重要纠正**（认证风暴并不是「卡住」）

> ⚠⚠⚠ **第 19、20 两轮的结论全部作废**：那两轮**构建其实失败了**
> （探针 tag 写成了 4 字符 `TCS3` ⇒ `build_vtrace.py` 的 `assert len(tag)==3` 抛异常），
> 而我当时用的 shell 是 `python build_vtrace.py | grep ... ; python patch_c_fire.py && run_full.sh`
> —— **`;` 让 pipeline 在构建失败后继续**，于是 `patch_c_fire.py` 拿的是**上一轮的** `libil2cpp.vtrace.so`
> ⇒ 设备上跑的是**旧探针** ⇒ 那两轮的「0 命中」**全是假零**。
>
> ★ **纪律**：① 探针 tag 必须**正好 3 个字符**（探针日志按 3 字符对齐，4 字符会被解析器漏掉）；
> ② 构建与跑测必须用 **`&&`** 串起来（`;` 会在失败后继续）；
> ③ 每次跑完先核对 **.so 里洞区的 tag 列表**（或 build 日志的 `hooks=`）与本轮 HOOKS 一致。

第十八轮（这轮构建是好的）把认证请求的整条生命周期量了一遍（`TAR` 收到响应 / `TAD` 入队 / `TDC` 反序列化完成 / `TCR` 请求完成 / `TAU` 解析会话）：

| 探针 | 次数 | 结论 |
|---|---|---|
| `TAR` | 532，**只有 6 个不同请求对象** | 客户端**反复复用同一个请求对象**重发 |
| `TCR` | 535，**与 TAR 的集合完全相同** | ⇒ ★ **每个收到响应的请求都真的完成了**（TAR ⊆ TCR） |
| `TAU` | 1056（每个 auth 响应 2 次） | 认证会话**确实被解析** ✓ |

⇒ **「auth 响应永不完成」的结论是错的**：响应确实完成了，只是客户端在**每 ~140ms 重发一次**
   （重试机制 `Request._retryData` +0x6C）⇒ **认证风暴是重试循环，不是卡死**。

第十九轮的探针（TEF/TEM/TED/TCS2/TCM/TER）**因构建失败而是假零** ⇒ 重新验证时已合并到第二十轮。

### 9.11 目前的完整图景（★ 第二十轮已把卡点钉死）

```
认证（每 ~140ms 重发一次，共 ~530 次；每个请求其实**都完成了**）
   ↓ 成功（会话被解析 TAU=1056）
GET /sync  → 响应完成（**修法 J** 的效果）→ TGC/TGA → GET /characters → 空列表
   ↓
FlowState::OnSaveDataFetched(saveGameFound=0, isAuthenticated=1)      ← 01:55:52.629
   ↓
FlowState::WaitForAllTheRequestsToFinish                              ← ★★ 01:55:52.648 / .727 命中 2 次
   ↓  ★★ 卡在这里出不来
   ├─ StartCharacterCustomization(TSC) = 0
   ├─ CharacterSelectionMenu::OnOpen/OnInit(TCS/TCM) = 0
   └─ 屏幕停在「网络错误」框（但 TEX/TEF/TER 都是 0 ⇒ **不是**异常处理、也不是 REST 错误路径）
```

★★ **结论**：流程**已经知道「没有存档」**（saveGameFound=0）、也**成功读到了空角色列表**，
   但它在 `WaitForAllTheRequestsToFinish` 等「所有请求结束」，
   而**认证在无限重发** ⇒ 请求队列**永远不空** ⇒ 流程**永远走不到建角**。

⇒ **下一步的三个方向**（按性价比）：
1. **让认证不再重发**（服务端：让会话/时间戳看起来「还有效」；
   客户端：`BLADES_FORCE_AUTH=1` 强制 `IsAuthenticated()` 返回 true）；
2. **让流程不等**（客户端补丁：`WaitForAllTheRequestsToFinish` 提前返回）；
3. **查清客户端为何重发认证**：`AuthResponse::CalculateSessionRefreshTime`(0x15AECB4)
   = `Session._sessionRefreshServerTimestamp`(0x28，来自响应头 `Server-Timestamp`)
   + `0.67 * Session._authorizationTokenExpirationSeconds`(0x20，来自会话里的 `TokenExpirationSeconds`)
   ⇒ 两者任一为 0/失真 ⇒ 刷新时间就在过去 ⇒ 立刻重认证。

### 9.12 第二十一～二十四轮：三个实验的结果

| 轮次 | 改动 | 结果 | 结论 |
|---|---|---|---|
| 21 | `BLADES_FORCE_AUTH=1`（强制 `IsAuthenticated()`=true）+ 补 `POST .../towns/current/rewards/current` 路由 | auth 仍 537 次 | 认证重发**与 `IsAuthenticated` 无关** |
| 22 | **修法 K2**：`WaitForAllTheRequestsToFinish` 里 `0x8011E0 beq 0x80122c` → 无条件 `b`（**不等所有请求**） | `TWA` 仍 2 次、后续仍 0 | ⇒ **卡点不在「等请求结束」这一步本身**（补丁本身无害，开关 `BLADES_FIX_K2=0`） |
| 23 | `/sync` 响应体改成 `{}` | ★★ **auth 从 ~530 降到 2**；但 `TSF`=0、流程不再往下走 | ★ `/sync` 体**必须带 `requestIndex`**（否则响应不完成）；★★ **认证风暴是「sync 之后失败 → 整体重置 → 重新认证」的下游现象** |
| 24 | `/sync` 响应体改成完整 BackendData 形状（requestIndex + Session + BackendCharacterListData:[]） | 与第 22 轮完全相同 | 响应体形状（在带 requestIndex 的前提下）**不影响这个卡点** |

★★ **最重要的推论**：**不要去修认证风暴**（它是结果不是原因）——
   第 23 轮已经证明：只要流程不进「sync 之后」那一步，认证风暴就完全消失。
⇒ **要修的是「`OnSaveDataFetched` 之后、选角/建角之前」的那一步。**

### 9.13 ★ 下一轮该探什么（具体建议）

1. **流程状态机**（当前最大的盲区）：`FlowState` 的状态字段（见 `flowstate_fields.md` 的静态偏移表）
   与**当前状态**指针；旧版有 `TCO`/`TIE` 两个探针（现版本已丢失，需重建）⇒
   探「状态切换」的那一处 + 把状态 id 映射回 `flowstate_fields.md` 的名字 ⇒ 直接看到它卡在哪个状态。
2. **`OnSaveDataFetched` 尾部的尾调** `0x1A8C62C`（`Action<object>.Invoke`）⇒ 那是流程注册的**下一个状态处理函数**；
   探它 + 它调用的状态处理函数 ⇒ 就能看到「为什么不去开选角界面」。
3. **「网络错误」框的来源**：已排除 `HttpErrorsHandling`（TEM/TEF/TER=0）与 `FlowState::OnExceptionEncountered`（TEX=0）；
   还剩 `BGS.Game.Network.Shared.*`（UnityWebRequest 层）与 `SoftLaunch.*`（登录界面）两条线。

### 9.14 ★★★ 第二十六轮：**流程状态机轨迹可见**（新探针模式 `regstr3`）

★ **新探针模式 `'regstr3'`**：从 `[r{regA}+off]` 取一个 `Il2CppString*`，再打印它的 **UTF-16 前 6 个字符**（3 个字）。
   用法：`('regstr3', 0, 0x18)` ⇒ 探 `FlowState::InvokeEntered` 就能**直接读到状态名**
   （每个状态对象在 `+0x18` 存着自己的名字字符串；.cctor 里 `str r1,[r0,#0x18]!` 赋名）。
   ⇒ **不再需要「创建顺序 → 状态名」的映射表**（旧 `TCO` 探针方案失败：`FlowState::.ctor` 实际没被调到，0 命中）。

**实测状态轨迹（一轮）**：
```
02:46:55.661  LoadSt   LoadStartupScene
02:46:56.906  Initia  InitializingBundles
02:47:01.819  Loadin  LoadingBaseGameContent
02:47:07.937  Authen  Authentication…   （连进 5 个认证相关状态）
02:47:08.132  Fetchi  FetchingSaveData
02:47:08.375  ── TSF：OnSaveDataFetched(saveGameFound=0, isAuthenticated=1) ──
02:47:08.380  ForceR  ★★ ForceReset            ← 读完存档后立刻强制重置
02:47:08.381  Loadin  LoadingBaseGameContent
02:47:08.386  ── TWA ×2：WaitForAllTheRequestsToFinish ──
02:47:08.431  Uninit  UninitializingGame
02:47:09.267  Bootst  Bootstrap
02:47:09.269  Initia  InitializingBundles      → 回到 LoadingBaseGameContent → 循环 ∞
```

★★ **两个关键结论**：
1. **流程知道「没有存档」（saveGameFound=0），却选了 `ForceReset` 而不是 `PreCharacterSelection`(0x90) → `CharacterSelection`(0x94)**
   ⇒ **这才是最终卡点**（此前所有轮次都在它上游）。
2. **认证风暴的机制清楚了**：`ForceReset` → `LoadingBaseGameContent` → `UninitializingGame` → `Bootstrap` → … → 再来一遍
   ⇒ 每轮循环都重新认证 ⇒ **~530 次 auth/轮**。所以认证风暴是**重置循环的结果**，不是原因（与第 23 轮「/sync 不完成 ⇒ 风暴消失」互相印证）。

⚠ NRE（`GameQuestManager.<LoadQuest>b__1`）发生在 **ForceReset 之后 2 秒** ⇒ 是重置的**结果**，不是触发者。

⇒ **下一步**：查 `FetchingSaveData` 状态的处理函数为何选 `ForceReset`：
   已排除「异常触发」（NRE 在后）、「认证失败」（isAuthenticated=1）。
   候选：① 角色列表被解析成 **null**（而不是空数组）⇒ 流程无法判断；
        ② 客户端认为「有当前角色但 id 为空」⇒ 状态不一致 ⇒ 重置（与「带空角色 id 的 rewards/quest 请求」吻合）。
   ⇒ 第二十七轮探 `CharacterService::get_CharactersList`(0x0160F5DC) 与 `get_CurrentCharacterId`(0x0160F5D4) 来判定。

### 9.15 第二十七轮：状态轨迹**可复现**；角色列表/当前角色**根本没被问**

* 状态轨迹与第 26 轮**逐字一致**（`LoadSt → Initia → Loadin → Authen×5 → Fetchi → ForceR → Loadin → Uninit → Bootst → Initia` ⇒ 循环）。
* `TCL`(`get_CharactersList`) = **0**、`TCC`(`get_CurrentCharacterId`) = **0**
  ⇒ 流程在决定重置前**根本没去读角色列表/当前角色** ⇒ 不是「列表解析成 null」这条路。
* ⇒ 重置决策发生在 `FetchingSaveData` 的状态处理函数里（`OnSaveDataFetched` 尾部尾调 `0x1A8C62C`）。
  ⇒ 第二十八轮探 `FlowState::GetResetGameState`(0x00801860) 与
  `FlowState::ResetLoginCredentialsAndFtueStatus`(0x00806508)，
  外加 `SavedDataService::LoadSavedData`(0x15964B4)（本地存档什么时候读的）。

### 9.17 第二十八～二十九轮：**重置闭环**完全确定，客户端从不尝试建角

实测（探针命中）：
| 探针 | 次数 | 时序 | 含义 |
|---|---|---|---|
| `TLD`(`SavedDataService::LoadSavedData`) | 1 | 紧跟 FetchingSaveData | 流程确实去**读本地存档** |
| `TSF`(`OnSaveDataFetched`) | 1 | saveGameFound=**0**、isAuthenticated=**1** | **本地没有存档** |
| `TIE`→`ForceR` | 1 | 紧接 TSF | ⇒ **立刻进入 ForceReset** |
| `TRS`(`FlowState::GetResetGameState`) | 1 | 紧跟 ForceReset | 重置目标由它选出 |
| `TRC`(`ResetLoginCredentialsAndFtueStatus`) | **0** | — | ★ **登录凭据没有被重置** ⇒ 认证风暴只是「循环重跑启动流程」的副产物 |
| `TIE`→`Loadin` | 1 | 紧跟 | ★ **重置目标 = LoadingBaseGameContent** ⇒ 回到加载 ⇒ 重新认证 ⇒ 循环 ∞ |
| `TRP`/`TCR2`/`TSS`/`TSC`/`TEC`/`TCS`/`TQR`（各种建角 API） | **全 0** | — | ★ **客户端从不尝试建角/软同步/自定义** |

★★ **完整闭环**：
```
FetchingSaveData → LoadSavedData（本地无存档 ⇒ saveGameFound=0）
   → OnSaveDataFetched(0,1) → **ForceReset** → GetResetGameState
   → **LoadingBaseGameContent** → Authentication → FetchingSaveData → … ∞
```

后端请求顺序（同一轮的相对顺序）：
```
POST /characters//towns/current/rewards/current   ← 空角色 id
POST /characters//quests/<uuid>/accept             ← 空角色 id
POST /auth/gplus
GET  /sync                                          ← 修法 J 让它完成
POST /auth/gplus
GET  /characters                                    ← ★ 在 OnSaveDataFetched 之后才发
```
⇒ **角色列表请求是在 ForceReset 之前发出的** ⇒ 流程「先问列表、再重置」，
   ⇒ 「空列表 ⇒ 触发重置」这条假设还没被排除（角色列表响应形状/内容可能才是最后一环）。

⚠ 设备时钟比宿主慢约 30 秒 ⇒ 跨设备/宿主的日志**只能比相对顺序**，不能比绝对时间。

### 9.19 第三十～三十二轮：**两个对照实验把最后一环拆开了**

| 实验 | 改动 | `ForceReset` | 流程终点 |
|---|---|---|---|
| 第三十轮 | `GET /characters` 返回**一个假角色** | **不再命中** ✓ | 停在 `FetchingSaveData`（等角色的存档数据） |
| 第三十一轮 | 列表恢复**空** + 会话补上**权益位** `featureStatus=15`、`linkedAccountsStatus=2` | **不再命中** ✓ | `OnSaveDataFetched(0,1)` 之后**不再进任何新状态**（停住） |

⇒ ★★ 两个结论：
1. **「角色列表为空」与「会话没有权益」** 都是会触发 `ForceReset` 的条件；
   去掉**任何一个**都能让重置消失 ⇒ ⇒ **`ForceReset` 是客户端的「异常恢复」路径**，
   而真正该走的是「新玩家 ⇒ 选角/建角」。
2. 第三十一轮（空列表 + 有权益）流程**不再重置、也不再推进** ⇒ 它在等某个还没满足的条件
   ⇒ 第三十二轮探 `CharacterService::OnGetAllCharacters` / `get_CharactersList`
   （判断列表回调是否到了、列表是 null 还是空数组）。

⚠ 假角色只是**诊断**用（真实目标仍是「客户端自己建角」）；按 §9.4 的结论，
   喂占位角色会让客户端**以为角色已存在 ⇒ 跳过建角** ⇒ 实验后已把列表**恢复成空**。

### 9.21 第三十四轮：★★ **FTUE（新手教程）才是最终那一环**

★ **从 dump.cs 里找到了状态机的语义**：

`BGS.Game.Gameplay.FtueState` 用 PlayerPrefs 键 **`PLAYER_PREF_FTUE_STATUS`** 存一个枚举值：
```
FTUEStatus: Unknown=0 Starting=1 Started=2 FTUECutsceneDone=3
            **IsAuthenticated=4** FTUEQuest=5 FTUEQuestCompleted=6 **CharacterCreationDone=7**
```
⇒ **设备上这个键的值正好是 `4`（IsAuthenticated）** ⇒ 客户端认为「只过完了认证」，
   **卡在 FTUE 中段**，而 **建角正是 FTUE 的最后一步**（`CharacterCreationDone`）！

★★ 之前那些「怪现象」现在全部对上了：
* `POST /characters//quests/73ab7435-021e-451a-9944-29e32e72382c/accept` ⇒ **这就是 FTUE 任务**
  （角色 id 为空是**正常**的：FTUE 任务在建角之前）。
* `GameQuestManager.<LoadQuest>b__1` 的 **NRE** ⇒ 客户端本地没有这个**服务端任务 id** 的数据
  ⇒ 任务加载失败 ⇒ **FTUE 推不动** ⇒ 永远到不了最后一步的建角 ⇒ 也就永远没有 `CreateCharacterRequest`。
* `POST /characters//towns/current/rewards/current`（空角色 id）⇒ 同样是 FTUE/初始化阶段的行为。

⇒ **本轮的三个改动**（都是服务端/配置侧，**不改客户端**）：
1. `seed_prefs.py` 现在同时预置 **`PLAYER_PREF_FTUE_STATUS = 6`**（FTUEQuestCompleted）
   ⇒ 客户端的下一步直接就是「建角」（`CharacterCreationDone`）。
2. `/sync` 响应的 `Quests` 里带上 **FTUE 任务**（`CurrentQuestId` + 该 id 的 `QuestVersion/Status/Seed`）。
3. 任务相关端点返回**任务数据**（`GetQuests.schema` 形状：`{id,version,seed,status,objectives}`）：
   * `GET  /characters/{id}/quests` ⇒ `{"quests":[…]}`
   * `POST /characters/{id}/quests/{qid}/accept` ⇒ `{"requestIndex":0,"quests":[…]}`

⇒ 目标信号仍然是：**`TQR`(CreateCharacterRequest::Setup) ≥ 1** ⇒ 客户端**自己**发 `POST /characters`
   ⇒ 后端的 `capture_character` 会把它记下来（`server.py` 已有这个机制）⇒ 用户要的「客户端自己建角」达成。

### 9.22 第三十四轮结果（FTUE=6 + 任务数据）

| 观测 | 结果 | 含义 |
|---|---|---|
| `POST /characters//quests/<uuid>/accept` | **消失** ✓ | FTUE 任务被跳过（FTUE 状态=6 生效） |
| auth 请求数 | **530 → 349** | 少了一轮循环 |
| `TQR`(CreateCharacterRequest::Setup) | 仍 **0** | 还是没建角 |
| 流程 | `FetchingSaveData` → `TSF(0,1)` → **空等 46 秒** → `ForceReset` → `UninitializingGame` → `Bootstrap` | ★ 那 46 秒是**超时等待** |
| 应用进程 | 03:46:07 **died**（重启循环中途退出） | 重置循环里进程死掉 |

⇒ 流程在 `OnSaveDataFetched` 之后**等某个东西直到超时**（46s）才重置。
⇒ 「等什么」是下一个要查的（大概率是某个本该到的请求/回调没到）。

### 9.23 服务端本轮的改动

* **会话的权益位**（`Session.FeatureStatus` 是位掩码：
  `None=0 / GameplayUnlocked=1 / GuildAccessAllowed=2 / PVPAccessAllowed=4 / E3SignupBonusAllowed=8`）
  ⇒ 我们之前给的是 **0**（等于「没解锁游戏」）⇒ 已改成 **15**；
  `linkedAccountsStatus` 从 0 改成 **2**（`GOOGLE_PLAY`，客户端是 gplus 登录）。
* **响应体的 `requestIndex` 按端点区分类型**（`server.py`）：
  * `/sync` ⇒ **字符串**（客户端 `requestIndex.schema` 写的是 System.String，
    `requestIndexToBackendSyncRequestIndexData.conversion` 读的就是它）；
  * 其它 `/api/*` ⇒ **整数**（`BackendData.schema` 里 `BackendData/requestIndex` 是 Int32）。
  ⇒ 客户端反序列化是**严格类型检查**（见 §八 的注），类型不对会整个解析失败 ⇒ 不能一律字符串化。
* `session/*` 的值统一成字符串（`session.schema` 里全是 System.String）。
* 新增 `POST /api/game/v1/public/characters/{id}/towns/current/rewards/current` 路由
  （原先只有 GET，客户端发的是 POST ⇒ 掉到通用兜底，拿到的是不对的形状）。

### 9.6 第十二轮探针方案（**已跑完**，见 §9.5）

| 探针 | 位置 | 作用 |
|---|---|---|
| `TSF` | `FlowState::OnSaveDataFetched`(0x00801CE8) | 流程回调；`r1`=`saveGameFound` 标志 |
| `TCS` | `CharacterServiceImplementation::OnGetAllCharacters`(0x016124F0) | 角色列表回调 |
| `TGM` | `FlowState::OpenCharacterCustomizationMenu`(0x007FEAC4) | ★ 流程到底有没有去开建角界面 |
| `TCR` | `CreateCharacterRequest::Setup`(0x015B46E4) | ★★ 客户端自己发起的建角请求（用户要的就是这个） |
| `TNC` | `CharacterServiceImplementation::OnCreateCharacter`(0x01612C7C) | 建角完成回调 |
| `TDP` | `DependencyHandle.<OnLoaded>b__23_0`(0x0121E4C8) | NRE 现场（哪个 bundle 依赖为空） |
| `TGA`/`TGC`/`TLD`/`TSU` | 同上轮 | 链路对照 |

### 9.7 本轮新增的工具与坑

* **新增探针模式**（`build_vtrace.py`）：
  * `'reg2'`：无序言版 + 同时打**两个寄存器**（例：`('reg2',4,7)` 打 `r4`+`r7`）；
    ⚠ 只能钩**不改 flags** 的指令。
  * `'reg2s'`：标准序言版 + 同时打两个寄存器（★ `('reg2s',1,14)` 打 **lr ⇒ 直接反查调用者**）。
  * `'regmem'`：标准序言版 + 打「一个寄存器 + 一个内存值」。
* ⚠ **洞区容量**：`PROBE_BASE=0xA7BADC` 起，探针每个 `0x48` 字节；洞 C 在 `0xA7BDB0`（74B）⇒
  **探针最多 10 个**（10 个的最后一个 blob 写到 `0xA7BDA6`，与洞 C 不重叠；再往前挪 1 字节就会重叠）。
* ⚠ **文件服务器的真实日志在 stderr**（`file_server_err.log`），`file_server.log` 只有启动行 ⇒
  「文件服务器没日志」≠「没请求」。
  ⚠ 本轮实测：客户端确实向文件服务器要了 `ManifestExtraData`（本地副本与 APK **逐字节一致**，md5 `898a18f3…`），
  且 `file_server.py` 本身**没有** bug（`with open(target,'rb') as fh` 正常、`Content-Type` 拼写正确）——
  ⚠ 之前怀疑过这两处是误读，已排除。

## 十、2026-11-12 复核轮（静态读码）：推翻上一版结论 + 下一轮探针方案

> ★★★ 本节（第十轮·静态读码）的结论已被**第十一轮实测**验证并升级为「修法 J」，见 §九；本节保留作为推理链。

### 10.1 本轮做了什么（**没有跑模拟器**，全是静态读码 + 离线核对）

| 检查 | 结果 |
|---|---|
| 复现上一轮基线 | `TPS=385 / TAR=385 / TCU=385 / TCR=384 / TSU=1`；`/sync` 请求对象 `8edd2c38` **在** TAR/TPS、**不在** TCU/TCR —— 与上一版完全一致 |
| 设备 `.so` 是否就是带洞版本 | 设备 `md5=9f464205d573d2865af8e31ff065a5a9` == 本地 `build/libil2cpp.c.so` ⇒ **是**（排除「跑了旧 .so」） |
| 洞 C 是否装对 | Capstone：`0x1589844: b #0xa7bda0` → 洞首指令 `ldr r5,[r4,#0x2c]` ⇒ **装对了** |
| 洞 C 是否可达 | `StartNextRequest` 的两条入口路径都到 `0x1589844`，前面无提前 return ⇒ **可达** |
| `0x159AEDC` 是不是合并体 | `script.json` 里只挂 `SerialBackendCommunicationService$$PerformRequest` 一个 ⇒ **不是**（排除 §四 可能性 2） |
| `_currentRequest` 谁写 | 只有两处：`0x159AF2C`（`PerformRequest` 写入）、`0x15897F0`（`CleanCurrentRequest` 清零）；**没有任何地方做「只在空时写」的保护** |

### 10.2 结论（详见 §四）

1. `TIC=0` 是**探针只打一条分支**造成的（§七.9）⇒ 上一版 §四 的「洞没执行」**作废**。
2. 补丁 [I] **结构上不可能生效**（`StartNextRequest` 只在槽位空时被进入）⇒ 上一版 §四 的**修法方向作废**。
3. 换人机制 = `PerformRequest` 在 `0x159AF2C` **无条件覆盖** `_currentRequest`（§4.3，有两处日志实证）。
4. **另一种拒绝路径尚未排除**：`r7` 被类型/注册表校验清零（§4.4）⇒ **必须观测 `r7`**。

### 10.3 下一轮探针方案（**一次构建 + 一次跑测**）

| 探针 | 位置 | 作用 | 实现要点 |
|---|---|---|---|
| `TCU` | `0x159b344` | 打 `r4`(=`_currentRequest`) + `r7`(=校验后的请求) | ★ **新增 `'reg2'` 模式**（同探针打两个寄存器）；钩这条 `ldr` **不改 flags** ⇒ 安全 |
| `TCR` | `0x1589798` | `CleanCurrentRequest` 入口：打 `r1`(标志) + `[r0+0x2c]`(被清掉的那个请求) | 新增 `'regmem'` 模式（打寄存器 + 内存） |
| `TIC` | `0x1589808` | `StartNextRequest` **入口**，**无条件**打日志 | 洞 C 改钩入口；洞里补做原序言 `push {r4-r7,fp,lr}` + `add fp,sp,#0x10`，打完跳 `0x1589810` |
| `TSU` | `0x1392148` | `/sync` 请求对象地址 | 保留 |
| `TPS` | `0x159AEDC` | 谁被起飞（`r1`=请求） | 保留 |
| `TAR` | `0x159B2BC` | 响应到达（`r1`=请求） | 保留 |
| `TCR` | `0x139DA90` | 请求完成（`r0`=请求） | 保留（与上表 `TCR` 改名区分） |

⚠ 洞区容量：`PROBE_BASE=0xA7BADC` 起，`reg`/`nopro`/`reg2`/`regmem` 每探针 `0x48` 字节；
洞 C 在 `0xA7BDA0`（~74B）⇒ **探针最多 10 个**（用到 `0xA7BDAC`）。

### 10.4 判读树（拿到这轮日志后按顺序看）

* `TCU` 的第二个数（`r7`）**= 0** ⇒ 走的是**类型校验清零**路径 ⇒ 与「换人」无关
  ⇒ 修法转向「让 `/sync` 请求通过 `0x159B308` 的类型校验」（查 `Request` 的 klass 与全局 class id 的对应关系）。
* `TCU` 的第二个数 **= `/sync` 请求地址**、第一个数 **≠ `/sync`** ⇒ 确认是**换人**
  ⇒ 修法二选一：
  ① **队列化**：让 `PerformRequest` 在已有请求在飞时**不覆盖** `_currentRequest`、把新请求排回队列
     （改 `0x159AF2C` 附近：写 `_currentRequest` 前先判空）；
  ② **换判据**：`AddRequestForCompletion` 不拿易变的 `_currentRequest` 比身份，改用
     `request._completionHandler`（基类 `PerformRequest` 写在 `Request+0x5c`）—— 那个才是「谁负责完成这个请求」。
* `TCR`（`CleanCurrentRequest`）命中且第二个数 == `/sync` ⇒ **`/sync` 被清掉/取消了**
  ⇒ 说明 `/sync` 起飞后被取消，槽位被清空 ⇒ 认证请求才能插进来 ⇒ 优先查「谁取消 `/sync`」。
* 全部命中但 `TGC` 仍 0 ⇒ 回到「响应到了但完成路径没走」，再查 `0x159B360` 之后的分支（`[r4+0x64]`= `_response` 为空则跳过）。

### 10.5 本轮复跑命令（同上一版）

```bash
cd H:/Project/threeKingdomSlayer/threeKingdomSlayer/Library/Locus/tmp/blades
export MSYS_NO_PATHCONV=1
export PYTHONPATH="H:\\locus\\data\\managed-python\\windows-x64\\python-3.13\\site-packages"
export BLADES_NO_QUEUE_GUARD=1      # ★ 必须：保留 LateUpdate 的串行守卫
python build_vtrace.py && python patch_c_fire.py
RESTART=1 PUSH_SO=1 INSTALL=0 WAIT=75 ROUTES=shapes/R_perchar.json bash run_full.sh
```
⚠ 分析脚本的正则要加新标签：`I (TSU|TAR|TCU|TCR|TPS|TIC|TGC|TBD|TLD|TGA|TCR) +: ([0-9a-f]+)`
⇒ `TCU`/`TCR` 这类现在**打两个数**，正则要改成能吃两个十六进制数（见下轮脚本）。

---

# 十一、第三十五～三十九轮（2026-11-13）：认证风暴根因 + 空 id 路由 + ResetGame 判定点

> 本节是**当前最新**的进展；§〇～§十保留作推理链。凡与本节冲突的旧结论，以本节为准。

## 十一.1 ★★ 认证风暴的真正根因：会话刷新时间的两个输入都错了

### 机理（静态读码，v7a）

```
Response::ProcessResponseHeaders(0x1B77918)
    0x1B77B24  str r5, [r7, #0x28]!   ; Response._timeStampMiliseconds = 0
    …若响应头里有 "Server-Timestamp" ⇒ Int64.TryParse ⇒ 写进去（毫秒）

AuthResponse::CalculateSessionRefreshTime(Session) (0x15AECB4)
    └→ Session::CalculateSessionRefreshTime(long requestTimeStampMiliseconds) (0xE6EBD8)
         now       = requestTimeStampMiliseconds / 1000            （epoch 秒）
         remaining = _authorizationTokenExpirationSeconds − now     （0x20 字段）
         refresh   = Mathf.CeilToInt(remaining * 0.67f + 1f)        （0.67f 池字 0x3F2B851F）
         _sessionRefreshServerTimestamp = now + refresh             （写到 0x28）
```

客户端用这个时间戳判断「会话是否已过期 ⇒ 要不要再认证」。我们之前：
* **不发** `Server-Timestamp` 头（`server.py` 的 `SERVER_TIMESTAMP` 默认 False）⇒ `now = 0`
* 把 `tokenExpirationSeconds` 当**时长**给 `86400` ⇒ `remaining = 86400 − 0`（看起来对），
  但一旦 `now` 变成真实 epoch（1.79e9），`remaining` 就是 **−1.78e9** ⇒ 刷新时间落在 1989 年

⇒ 两种情况都让客户端认为「会话已过期」⇒ **每 ~130ms 重发一次 `POST /auth/gplus`**（第三十四轮实测 349 次/轮）
⇒ 流程在 `FetchingSaveData` 里空等 ~46s（349 × 134ms）才 ForceReset。

### 修法（服务端，两处一起改，缺一不可）

```python
# server.py
SERVER_TIMESTAMP = True          # ★ 默认加 Server-Timestamp 头（毫秒）
# 响应体里（小写 session / 大写 Session 都处理）：
_body[...]["tokenExpirationSeconds"] = str(int(time.time()) + 86400)   # ★ 绝对 epoch 秒
```

### 效果（第三十五轮）

| 指标 | 第三十四轮 | 第三十五轮 |
|---|---|---|
| `POST /auth/gplus` 次数 | **349** | **1** |
| 探针 TRF（会话刷新时间戳） | — | `0x6af7bb7c`（真实时间；故障值是 `57841`＝1970 年） |
| 流程 | 空等 46s → ForceReset → 循环 | FetchingSaveData → ForceReset（立刻）→ 循环 |

## 十一.2 ★★ 空角色 id 的路由 bug：响应队列被一个响应卡死

### 现象（第三十七轮）

* 客户端没有角色时请求的路径中间是**空段**：
  `POST /api/game/v1/public/characters//towns/current/rewards/current`
* `server.py::_match_template` 明确写着「占位符：任意值（含空）」⇒ `{id}` 路由**能匹配空段**
* 命中的是 `{id}` 路由或兜底 `/api/*` ⇒ 客户端收到「全 schema 合并体」
* 客户端把里面的数组（`accountCurrencies` / `chests` / `quests` / `town.buildings` …）转成
  「按 id 索引的字典」，再按**具体 id**去取 ⇒ 取不到 ⇒
  ```
  KeyNotFoundException: The given key was not present in the dictionary.
    at BGS.Game.Rest.JsonToPersistent.SearchArrayToDictionaryParser.Parse
    at BGS.Game.Rest.ResponseDeserializer.CompleteDeserialization
  ```
  **每帧抛一次**（第三十七轮 2373 次）⇒ 该响应**永远反序列化不完**
  ⇒ 排在它后面的**认证响应**也完不成 ⇒ 流程停在 `Authenticating`（第三十七轮只发了 5 个请求）。

### 修法（第三十八轮）

1. 给**空 id 形式**单独配路由（响应体只放该端点需要的东西）：
   `POST/GET /api/game/v1/public/characters//towns/current/rewards/current`、
   `…/characters//quests/<qid>/accept`、`…/characters//quests`、`…//towns/current`、
   `…//inventories/current`、`…//chests`、`…//wallets/current`
2. 分析/统计端点（`/api/analytics/v1/public/events`、`/api/analytics/v1/public/stats/client`）改用
   **无数组**的响应体 `{"requestIndex":0,"session":{…}}`。
3. **路由顺序**：`_match_template` 是**先匹配先赢** ⇒ 必须把
   「精确路径 → 空 id 路径 → `{id}` 模板 → `/api/*` 兜底」排好序（用 `fix_empty_id_routes.py` 后再重排）。

### 效果

第三十八轮：流程不再卡在 `Authenticating`，能走到 `OnSaveDataFetched`（TSF 命中），
异常从 2373/轮降到 950/轮；第三十九轮（配合修法 R1 + 分析端点不再触发）**异常 0 条**。

## 十一.3 ★★ ResetGame 判定点（第三十七～三十九轮）

`<OnSaveDataFetched>b__0`（0xA72B28）尾部：

```
0xA73610  blx r2                              ; 接口调用，返回值 X
0xA73614  cmp r0, #4
0xA73618  bge 0xA73650                        ; ★ X >= 4 ⇒ 走重置
0xA73648  bl  FlowState::StartLoadingContent  ;   X < 4  ⇒ 正常路径
0xA73688  ldr r4, [r0, #0x98]                 ; ← FlowTrigger.ResetGame（静态槽 0x98）
0xA736A0  bl  FlowTrigger::Fire
```

* 探针 **TXX**（钩在 `0xA73610` 的 `blx r2` 之后，打返回值）⇒ **X = 4**（第三十七～三十九轮都是 4）。
* 反查 `FlowTrigger` 静态槽：`ResetGame = 0x98` ✔（`flowstate_fields.md` 同表），
  `fire_which.py` 能列出「哪个 Fire 调用点点了哪个触发器」——
  **唯一点 `CharacterSelection`（槽 0xEC）的地方是 `FlowState.<>c.<.cctor>b__222_155`（0x10E0930）**，
  而迁移表显示 `Gameplay --CharacterSelection--> PreCharacterSelection` ⇒
  **进角色创建界面必须先到 `Gameplay` 状态**。
* `X` 到底是「FTUE 状态」还是别的，**尚未最终确认**：FTUE 的取值链是
  `GameplayManager::StartFtue→2 / IntroCutsceneDone→3 / StartFTUEQuest→5 / EndFTUEQuest→6 / EndFtue→7`
  （`b__222_41`（认证路径）→3）——**没有任何 `bl SetFtue` 传 4**，
  而设备上的 `PLAYER_PREF_FTUE_STATUS` 跑完却是 **4** ⇒ 有人绕过了 `SetFtue`（或函数体被合并）。
  ⇒ **X 的语义仍未钉死**，这是下一轮要查的第一件事（可从「哪个接口方法返回 4」入手：
  调用点是 holder+0x5c 的服务对象的**接口第 0 个方法**，返回值与 4 比较）。

### 修法 R1（第三十九轮，默认开）

```python
# patch_c_fire.py
FIX_R1_AT = 0x00A73618
assert u32(orig, FIX_R1_AT) == 0xAA00000C      # bge 0xA73650
struct.pack_into('<I', so, FIX_R1_AT, 0xEAFFFFFF)   # b 0xA7361C（无条件走 StartLoadingContent）
```

效果：第三十九轮流程 `FetchingSaveData → InitializingBundles`，**不再 ForceReset**。

## 十一.4 FTUE 任务数据（从 APK 设计表拿）

`tables/BGS.Game.Quests.DungeonQuestHolderScriptableObject.csv` 里：

| 字段 | 值 |
|---|---|
| 任务 id | `73ab7435-021e-451a-9944-29e32e72382c`（`MQ01_ComingHome`） |
| 目标 id | `7e4ceb81-85d9-4536-9b9a-d04e728e7859`（`DiscoverAreaObjective`, quota 1） |

⇒ 任务响应体（`GetQuests.schema`）：

```json
{"requestIndex": 0,
 "quests": [{"id": "73ab7435-021e-451a-9944-29e32e72382c",
             "version": "1", "seed": "1", "status": "0",
             "objectives": [{"id": "7e4ceb81-85d9-4536-9b9a-d04e728e7859", "progress": "0"}]}]}
```

`/sync` 里的 `Quests` 段（`SaveGame.schema`）：

```json
"Quests": {"CurrentQuestId": "73ab7435-…",
           "73ab7435-…": {"QuestVersion":1,"Status":0,"Seed":1,
                           "7e4ceb81-…": {"Progress":0.0,"Status":0}}}
```

## 十一.5 本轮跑测命令与判读

```bash
cd H:/Project/threeKingdomSlayer/threeKingdomSlayer/Library/Locus/tmp/blades
export MSYS_NO_PATHCONV=1
export PYTHONPATH="H:\\locus\\data\\managed-python\\windows-x64\\python-3.13\\site-packages"
export BLADES_NO_QUEUE_GUARD=1
python build_vtrace.py && python patch_c_fire.py        # 洞区 0xA7BADC 起，探针最多 10 个
RESTART=1 PUSH_SO=1 INSTALL=0 WAIT=150 ROUTES=shapes/R_perchar.json bash run_full.sh
```

判读要点：
* 探针统计一律用 `grep -a -oE " I [A-Z]{3} " log_tail.txt | sort | uniq -c`（**别用脚本自带汇总**，它的 tag 表已过时）。
* `TFW`（FlowTrigger::Fire 的名字）打印的是 **UTF-16 前 6 个字符、32 位小端**：
  `00700041 00530070 00730075` ⇒ 低 16 位在前 ⇒ **`Appsus` = AppSuspend**（不是 "pASpsu"）。
* `TIE` 同理：`006f004c 00640061 00740053` ⇒ **`LoadSt` = LoadStartupScene**。

---

# 十二、第四十～五十轮（2026-11-13 晚）：FTUE 语义锤死 + 三个新补丁 + 一个探针洞的坑

> 本节接在 §十一 之后，是**当前最新**的状态。结论均有探针/读码证据。

## 十二.1 ★★★ FTUE 状态机彻底锤死（探针实证）

`<OnSaveDataFetched>b__0`（0xA72B28）里**同一个接口方法**（探针 TMN 打出方法名 = **`get_Status`**）
被调用多次，分别与常量 **1 / 2 / 4 / 5 / 7** 比较 ⇒ 它就是 **`FtueState.get_Status`**（FTUEStatus）。

| 位置 | 比较 | 含义 |
|---|---|---|
| 0xA732A4 | `cmp r0,#7` | CharacterCreationDone(7) |
| 0xA730D8 | `cmp r1,#5` | FTUEQuest(5) |
| 0xA73614 | `cmp r0,#4` | ★ IsAuthenticated(4) ⇒ **≥4 就 Fire(ResetGame)**，<4 就 `StartLoadingContent` |
| 0xA733A4 | `cmp r1,#2` | Started(2) |

**游戏自己在这个函数里设 FTUE 状态**（探针 TFT 打 lr 定位到调用点）：
| 调用点 | 设为 | 场景 |
|---|---|---|
| 0xA72CA8 | **7** CharacterCreationDone | 找到本地存档 ⇒ 已经建过角色 |
| 0xA731EC | **4** IsAuthenticated | ★ **我们走的就是这条**（无存档 + 已认证）⇒ 紧接着 4≥4 ⇒ ResetGame |
| 0xA73250 | 6 FTUEQuestCompleted | |
| 0xA733C4 | 3 FTUECutsceneDone | 新玩家路径 |
| 0xA736C4 | 1 Starting | |

FTUEStatus 全链：`GameplayManager::StartFtue→2 / IntroCutsceneDone→3 / StartFTUEQuest→5 /
EndFTUEQuest→6 / EndFtue→7`；`FlowState.<>c.<.cctor>b__222_41`（认证完成）→ SetFtue(3)+Fire(Authenticate)。

## 十二.2 新增三个补丁（`patch_c_fire.py`，都有开关）

| 补丁 | 位置 | 作用 |
|---|---|---|
| **R1** | 0xA73618 `bge`→`b` | 无存档时**不再 ResetGame**，改走 StartLoadingContent（0xAA00000C → 0xEAFFFFFF） |
| **R2** | 0x800DC8 `ldrb`→`mov r2,#1` | 加载菜单「入场动画完成」一律当作已完成（老墙：菜单以 null context 打开时动画不跑 ⇒ 流程静默停住） |
| **R3** | 0xA731EC `mov r0,#4`→`mov r0,#3` | 把 FTUE 状态从 4(IsAuthenticated) 改成 3(FTUECutsceneDone) ⇒ 状态<4 ⇒ 客户端继续走教程而不是重置 |
| **洞 F** | 钩 `FlowTrigger::Fire`(0xA7381C) | 点「**BundlesInitialized**」时把认证链台阶字节重新武装（见 §十二.4） |

## 十二.3 ★★ 认证链（洞 B）的机制与时序（卡了两轮的地方）

洞 B 钩 `FlowManager::Update`(0x7FC978)，用 FlowState 静态块的 **0x121**（bool 后的 padding，游戏不写）
当「台阶字节」，依次发：0xa4(Authenticate) → 0x04(PreFtueStart) → 0x0c(PostFtueSetup) → 0x10(PlatformSignIn) → 0(停)。

**这 4 级必须在流程处于 LoadingBaseGameContent（正要发 Authenticate）时消耗**，否则会被白白烧掉：
* 第四十一～四十三轮：洞 A 只在启动时武装一次 ⇒ 第二遍没人再发 Authenticate ⇒ 卡在 LoadingBaseGameContent。
* 第四十五轮：改在 `StartLoadingContent` 武装 ⇒ **太早**（流程还在 InitializingBundles）⇒ 链在 01:05 就烧完，
  而流程 01:11 才进 LoadingBaseGameContent ⇒ 照样卡住。
* 第四十六～四十九轮：洞 F 在 `FlowTrigger::Fire` 里判定「点的是 BundlesInitialized」才武装 ⇒ 时机对了 ✓
  （第五十轮实测：auth 请求 1 次、流程能反复走 Authenticating ⇒ 链能再走一轮 ✓）。

## 十二.4 洞 F 的实现要点（写得坑很多，留档）

洞 F 钩 `FlowTrigger::Fire`（替换函数入口），结构：
```
push {r4,sl,fp,lr}          ; ★ 原序言第 1 条（必须照抄）
push {r0-r3,lr}
ldr  r1,[r0,#8]              ; 触发器的 <Name>（Il2CppString*，FlowTrigger 的实例字段 +0x8）
ldrb r2,[r1,#0xc]            ; chars[0] = 'B'
cmp  r2,#0x42
bne  skip
ldrb r3,[r1,#0x10]           ; chars[2] = 'n'
cmp  r3,#0x6e
bne  skip
ldr  r0,[pc,#imm] / ldr r0,[pc,r0] / ldr r0,[r0]   ; ★ FlowState 静态块链（照抄 FS_CHAIN）
ldr  r1,[r0,#0x5c]           ; FlowState 静态块
ldrb r2,[r1,#0x121]          ; 台阶字节
cmp  r2,#0
bne  skip                    ; ★ 只有链已耗尽时才重新武装
mov  r2,#0xa4
strb r2,[r1,#0x121]
skip:
pop  {r0-r3,lr}
add  fp,sp,#8                ; ★ 原序言第 2 条（漏了 ⇒ 帧指针错）
b    0xA73824                ; ★ 尾调用原函数体（原函数的 pop{...,lr} 会回到原调用者）
```

**三个坑（都真的卡住过）**：
1. **ARM32 的立即数字段在 bits[11:0]**（8 位值 + 4 位旋转），**不是 A64 的 imm12<<12** ⇒
   `cmp r2,#0x7542` 那种写法会编出**别的指令**（当时把客户端直接搞挂了）。
   而且 **CMP 的 Rd 字段必须是 0xF**（表示「无目的寄存器」）。
2. **分支条件码**：`0x0A` 是 **BEQ**、BNE 是 **0x1A** ⇒ 写反了 ⇒ 「名字不匹配时才武装」
   ⇒ 每次点火都把链拨回第 1 级 ⇒ **流程卡在 Authenticating**（第四十七轮的现象）。
3. **代码表变长后池字索引要跟着改**（f_code[18] → f_code[21]）⇒ 池字没写进去 ⇒
   `ldr r0,[pc,r0]` 读到垃圾 ⇒ **客户端启动即挂**（第四十八～四十九轮）。

## 十二.5 ★ 探针洞区的硬上限（踩过：客户端直接卡死）

* 探针区 = `0xA7BADC` 起，**到 `0xA7BDA0` 止**（**洞 C 从 0xA7BDA0 开始，再往后是活代码**）。
* 每个探针 0x48 字节（`regstr3` 0x60）⇒ **最多 9~10 个**。
* ⚠ 第四十轮放了 10 个探针 ⇒ 最后一个探针的洞**盖住了洞 C** ⇒ 某个 hook 跳进错误的洞
  ⇒ **客户端直接卡死**（看起来像游戏 bug）。
* ⇒ `build_vtrace.py` 已加硬断言（`PROBE_LIMIT = 0xA7BDA0`），超了直接报错。

## 十二.6 ★ 剩下的卡点（下一轮从这里接）

**现状（第五十轮）**：auth 只发 1 次、无异常、无 ResetGame，
但流程在 `FetchingSaveData → StartLoadingContent → InitializingBundles → LoadingBaseGameContent
→ Authenticating → FetchSaveData → …` 之间**循环 17 次**，
**没有进 FTUE（TutorialForest）、没有建角**（`TSC`/`TQR` 仍为 0）。

**最值得先试的两件事**：
1. **回归定位**：第三十六轮（**没有** R1/R2/R3/洞 F，只把 FTUE 预置成 3）时，客户端**确实进过 TutorialForest**
   （证据：analytics 载荷里有 `"menu":"TutorialForest"`），并且**发了 FTUE 任务的 accept 请求**。
   ⇒ 现在是哪个补丁让它不再进 FTUE？**逐个关掉 R1/R2/R3/洞 F 跑一轮**即可二分定位
   （R2 最可疑：强制「入场动画完成」可能让内容加载过早完成 ⇒ 教程起不来 ⇒ 回到取存档）。
2. **自然路径**：客户端本来有一条「无存档 + 新玩家 ⇒ SetFtue(3) ⇒ 继续教程」的分支（0xA733C4）。
   我们是用 R3 硬改状态才走上它的；**那条分支的判定条件还没读懂**（0xA731D4 的 `cmp r2,#0` 附近），
   读懂它就能走「无补丁的自然路径」，比硬改稳。

**目标信号（一直没变）**：`TSC`（`FlowState::StartCharacterCustomization`）与
`TQR`（`CreateCharacterRequest::Setup`）命中 ⇒ 客户端自己发 `POST /api/game/v1/public/characters`。

## 十二.7 本轮新增的工具/探针模式

| 名称 | 作用 |
|---|---|
| `build_vtrace.py` 的 **`('cstr', regA, off)`** 模式 | 读 `[rX+off]` 当 **char\*** 打印（MethodInfo 的方法名在 +0x8 ⇒ 能直接读出接口方法名） |
| `disasm.py` / `xref.py` / `poolscan.py` / `dump_poolrefs.py` | 反汇编 / 调用点 / 常量池扫描（`poolrefs.txt` 里 82 万条池引用可离线 grep） |
| `scan_offset.py` | 扫「访问某结构体偏移」的所有指令（capstone 版，比手写位模式可靠） |
| `fire_which.py` | 反查「哪个 `FlowTrigger::Fire` 调用点点了哪个触发器」（**注意：只按偏移匹配会误报**，holder 的取法要照游戏写法） |
| `fix_quest_routes.py` / `fix_empty_id_routes.py` | 任务响应形状 / 空 id 专用路由 |

## 十二.8 第五十一～五十三轮：回归二分（排除法）

| 轮次 | 客户端补丁 | 结果 |
|---|---|---|
| 51 | R1+R3+洞F，**R2 关** | 循环 304 次（更快），**无** TutorialForest |
| 52 | 同上 + 给所有 tailored 响应**补回 session** | 循环 294 次，无变化 ⇒ 不是 session 缺失 |
| 53 | **R1/R2/R3 全关**，只留洞F | 循环 288 次（≈0.6s/轮的重置循环），无 TutorialForest |

⇒ **R1/R2/R3 都不是「不进 FTUE」的元凶**；「补 session」也不是。

### ★★ 新线索：内容加载的 AssetBundle 冲突

```
272 ×  E Unity : The AssetBundle 'common' can't be loaded because another AssetBundle with the same files is already loaded.
```
（次数 ≈ 循环轮数 ⇒ 是循环的**症状**）⇒ 每轮循环都会**重新加载** 'common' bundle，而它已经在内存里 ⇒ 加载失败。

⇒ 真正的因果链大概是：**第一遍内容加载没走完**（或走完后又被判失败）
⇒ 流程回到取存档 ⇒ 再次 StartLoadingContent ⇒ 再次加载 'common' ⇒ 冲突 ⇒ 再次回取存档 ⇒ ……

### ⚠ 第三十六轮「进过 TutorialForest」这条证据**不可信**

第三十六轮那次构建的探针区**溢出了洞 C**（正是 §十二.5 那个坑）⇒
那版 .so 里某个 hook 跳进了错误的洞 ⇒ 行为与干净构建不同 ⇒
「第三十六轮能进 TutorialForest」**不能当作基线**。
⇒ 建议**用当前干净构建重跑一次「第三十六轮配置」**（只把 FTUE 预置成 3、其余补丁全关），
拿到一个可信基线，再逐个打开补丁。

### 下一步（按性价比）

1. **可信基线**：当前构建 + 只留 FTUE=3（无 R1/R2/R3/洞F）跑一轮，看是否进 TutorialForest。
2. **探内容加载**：探 `BundleDownloader` / `BundleManager` 的状态与回调（`bundleDownloaderState`），
   以及 `FlowState::InitializingBundles` 的状态处理函数 ⇒ 看第一遍到底卡在哪。
3. **读懂自然分支**：`0xA731D4` 附近的 `cmp r2,#0`（决定 SetFtue(4) 还是 SetFtue(3) 的那个判定）。
4. 目标信号仍是 `TSC` / `TQR`。

---

# 十三、第五十四～五十八轮（2026-11-14）：★ 重置根因锤死 + 一个把客户端打崩的构建 bug

> 本节是**当前最新**的状态（§〇～§十二保留作推理链）。本轮没有解决「客户端自己建角」，
> 但把**卡点从「不知道」变成了「一条可判定的条件」**，并且修掉了一个会污染结果的构建 bug。

## 十三.1 ★★★ 重置的真正判据：**FTUE 状态 ≥ 4**

`FlowState.<>c__DisplayClass144_0.<OnSaveDataFetched>b__0`（0xA72B28）的**尾巴**才是重置的判定点：

```
0xA73610  blx  r2              ; 接口调用 ⇒ FtueState.get_Status()  ⇒ r0 = FTUE 状态
0xA73614  cmp  r0, #4
0xA73618  bge  #0xA73650       ; ★★ 状态 >= 4 ⇒ Fire(FlowTrigger.ResetGame)
0xA73648  bl   #0x8023B8       ;   状态 < 4  ⇒ FlowState::StartLoadingContent（正常路径）
0xA73688  ldr  r4, [r0, #0x98] ; FlowTrigger.ResetGame（静态槽 0x98）
0xA736A0  bl   #0xA7381C       ; FlowTrigger::Fire
```

⇒ **只要回调结束时的 FTUE 状态 ≥ 4，就必然 ResetGame ⇒ 必然进 `Bootstrap → InitializingBundles → …` 死循环。**
⇒ 而状态**来自 PlayerPrefs 的 `PLAYER_PREF_FTUE_STATUS`**（实测：预置 7 ⇒ 探针 TMA 先打出 7；
预置 6 ⇒ TXX 打出 6；预置 3 ⇒ 客户端把它改成 4 后 TXX 打出 4）。
⇒ 现在 `seed_prefs.py` 支持用环境变量 `FTUE_VAL` 预置任意状态（默认 3）。

**实测对照（同一构建，只改预置值）**：

| 预置 FTUE 状态 | 第一个接口调用（TMA） | 最终比较（TXX） | 结果 |
|---|---|---|---|
| 3（FTUECutsceneDone） | 3（≤5） | **4** | SetFtue(4) ⇒ ResetGame |
| 6（FTUEQuestCompleted） | 6 | **6** | 走 ≥6 那条路 ⇒ 仍是 SetFtue(6) ⇒ ResetGame |
| 7（CharacterCreationDone） | 7 | **6** | 同上 ⇒ ResetGame |

⇒ **任何状态都会落到「≥4 ⇒ 重置」** ⇒ 光改预置值没用。

## 十三.2 ★★★ 这条 lambda 的分支结构（静态读码，控制流已理清）

它是一条 **40 来个 `if (条件) { SetFtue(X); return; }` 串成的链**，
每个「块」的形状都一样：取一个服务 → 在它的接口表里找实现 → `blx` 调它 → 用返回值做判定。
**第一个命中的块决定结果**。

三个关键 `SetFtue` 调用点（`FlowState::FtueState::SetFtue` = 0xC975B8）：

| 调用点 | 设定值 | 走到它的条件 |
|---|---|---|
| 0xA72CA8 | 7 = CharacterCreationDone | 「找到本地存档 ⇒ 已经建过角色」 |
| 0xA72BFC…→0xA733C4 | 3 = FTUECutsceneDone | ① `isNoUiAuthentication == 0` 且标志位 bit9 == 0；② 0xA73258 那条路（**接口返回 != 1**） |
| 0xA731EC | 4 = IsAuthenticated | 第一个接口调用（0xA730CC）**返回值 ≤ 5**（`cmp r1,#5; ble`） |
| 0xA73250 | 6 = FTUEQuestCompleted | 0xA7321C 的接口调用**返回值 == 1** |
| 0xA736C4 | 1 = Starting | 兜底 |

★ **只有 `SetFtue(3)` 那一支能让最终状态 < 4 ⇒ 不重置** ⇒ 它是唯一的逃生门。

### ⚠️ 一个必须记住的 ARM32 陷阱（本轮踩过）

**`and r1, r1, #0x200` 不带 S 位 ⇒ 不改标志位**（ARM 数据处理里 S 是 **bit20**，不是 bit24）。
⇒ 之前把 `cmp r2,#0` 当成「死代码」是**错的**：它后面夹着的 `and`（无 S）不覆盖标志位，
所以 `beq` 测的仍然是那句 `cmp`。
⇒ **判读 IL2CPP ARM32 的条件分支时，先确认中间指令是否带 S 位**，否则整条推理链会错。

### ★★ 当前最可疑的堵点：`[X+0x184]` 这个服务是 **null**

0xA72E14 那一块：

```
0xA72E14  ldr  r1, [r0, #0x5c]     ; r1 = [Obj+0x5c] = X
0xA72E18  ldr  r4, [r1, #0x184]    ; r4 = [X+0x184]  ← ★ 服务
0xA72E1C  cmp  r4, #0
0xA72E20  beq  #0xA73228          ; ★★ r4 == null ⇒ 直接 SetFtue(6) ⇒ 重置
0xA72E24  ldrb r1, [r0, #0xbb]    ; 标志位 bit1：置位则改走「另一个静态」的同一条路
0xA72E40  ...  ldr r4,[r0,#0x184] ; 兜底（**解析后发现兜底与主路是同一个静态对象 ⇒ 兜底无意义**）
0xA72E58  beq  #0xA7370C          ; 还是 null ⇒ NullReferenceException
0xA72E5C  ...  blx r2             ; 0xA7321C：接口调用 ⇒ 返回 1 ⇒ SetFtue(6)；!= 1 ⇒ SetFtue(3) ✔
```

实测：**探针 TMB（钩在 0xA7321C）命中 0 次** ⇒ 这个接口调用**从未发生** ⇒ `[X+0x184]` 确实是 null
⇒ 客户端每次都走「服务缺失 ⇒ SetFtue(6) ⇒ 重置」。

⇒ **下一步要弄清 `[X+0x184]` 是什么服务**（静态解析不出来：`X` 是运行期指针）。
　候选做法：
* 运行时探针打印 `[X+0x184]` 的值 + `X` 的类名（Il2CppClass 的 name 在 +0x8）；
* 或找出「谁给 `[X+0x184]` 赋值」——扫全库对偏移 0x184 的写指令并按方法名过滤；
* 或对照 `FlowState::SetupGlobalManagers`(0x802E08)：**全库只有一处**写 `FlowState._saveService`
  （静态槽 0x184，赋值点 0x802F10）⇒ **这条线索指向「存档服务」**，
  而 `TSG` 探针实测该方法**每轮跑一次**（第 58 轮 12 次）且**早于** OnSaveDataFetched 约 9 秒
  ⇒ ⇒ **`_saveService` 按理已经非 null** ⇒ 说明 `[X+0x184]` **不是** `FlowState._saveService`，
  是另一个（更靠后创建的）服务。

## 十三.3 ★★★ 构建 bug：探针区与洞 F 重叠（会崩、也会污染结果）

洞区真实布局（`patch_c_fire.py`）：

```
0xA7B9EC 洞A(68B) | 0xA7BA40 洞B(148B) | 0xA7BADC 探针区 |
0xA7BD04 洞F(88B) | 0xA7BD4C 洞E(停用，与洞F重叠) | 0xA7BDA0 洞C | 0xA7BE60 洞J | 0xA7BE90 洞K2
```

★ `build_vtrace.py` 的 `PROBE_LIMIT` 之前写成 **0xA7BDA0** ⇒ **第 9 个探针起就压住洞 F**：
* **第 56 轮**（9 个探针）⇒ 洞 F 被覆盖 ⇒ 认证链的「重新武装」失效 ⇒ **那轮结论不可信**（要重跑）；
* **第 57 轮**（8 个探针）⇒ 洞 F 前 32 字节被覆盖 ⇒ **客户端启动 1 秒后崩**
  （logcat：`E CRASH … #00 pc 00a7bd0c` ⇒ 正好是洞 F 的指令被顶掉）。

⇒ **已修**：`PROBE_LIMIT = 0xA7BD04`（洞 F 起点）⇒ 探针区 = 0xA7BADC..0xA7BD04 = 652 字节
　⇒ 最多 13 个 0x48 探针（含两个 0x60 的 `regstr3` 时最多 11 个）。
⇒ 洞 E（0xA7BD4C）与洞 F 重叠 ⇒ **洞 E 必须保持停用**。

## 十三.4 ★★ 环境坑：`adb reverse` 只有 SDK 版 adb 支持

* **雷电自带的 adb（1.0.31）不支持 `adb reverse`**（只会打印帮助）⇒ 隧道建不起来 ⇒
  客户端直接 `Curl error 7: Connection refused`（第 54 轮第一次跑就踩了，白跑 4 分钟）。
* **两套 adb 不能同时在跑**：SDK 版一启动就 `adb server version (31) doesn't match this client (41); killing...`
  把雷电的 server 踢掉 ⇒ 设备变 offline ⇒ 必须**完整重启雷电**才能恢复（`ldconsole quit` → `schtasks launch` → 等 120s）。
* **adb server 重启后的第一次 `adb reverse` 会让设备通道掉线**（脚本里表现为「reverse 后 shell 掉线」），
  重试 5 次仍失败 ⇒ 直接用 `RESTART=1` 重启模拟器（`run_full.sh` 已支持）。
⇒ `run_full.sh` 已固定使用 SDK 版 adb（并加了注释说明为什么不能用雷电版）。

## 十三.5 本轮改动清单（都在 `Library/Locus/tmp/blades/`）

| 文件 | 改动 |
|---|---|
| `build_vtrace.py` | **PROBE_LIMIT 0xA7BDA0 → 0xA7BD04**（关键修复）；探针集改为 TIE/TSF/TSC/TQR/TXX/TMA/TSG |
| `patch_c_fire.py` | 新增 **修法 R4**（0xA72FC4 `and r1,r1,#0x200` → `mov r1,#0`，开关 `BLADES_FIX_R4`）——**实测无效**（那个分支不是我们走的路） |
| `seed_prefs.py` | `FTUE_VAL` 改为可用环境变量覆盖 |
| `run_full.sh` | 固定用 SDK 版 adb + 注释说明 reverse 的坑 |

**修法 R4 的结论**：它改的是「另一个块」里的标志位判定，**不在我们这条路径上**
⇒ 重置依旧（实测 TXX 仍为 4）⇒ 证明「同一个标志位 bit9」不是重置的判据（判据是 FTUE 状态本身）。

## 十三.6 当前构建与探针

* 构建：`export BLADES_NO_QUEUE_GUARD=1 BLADES_FIX_R1=0 BLADES_FIX_R2=0 BLADES_FIX_R3=0 BLADES_FIX_R4=0`
  → `python build_vtrace.py && python patch_c_fire.py`（`build/libil2cpp.c.so`）。
  生效补丁：**洞 F**（认证链重新武装）、**J**（响应交回自己的请求）、**K2**（不等所有请求）、**E/G**（历史诊断）、
  **D 关闭**（保留串行守卫）；R1/R2/R3/R4 全关。
* 探针（7 个）：`TIE` 状态名 / `TSF` OnSaveDataFetched 参数 / `TSC` 建角入口 / `TQR` 建角请求 /
  `TXX` 最终比较值 / `TMA` 第一个接口返回值 / `TSG` SetupGlobalManagers。
* 跑测：`RESTART=1 PUSH_SO=1 INSTALL=0 WAIT=120 ROUTES=shapes/R_perchar.json bash run_full.sh`
  （`FTUE_VAL` 可覆盖预置值）。

## 十三.7 下一步（按性价比）

1. **弄清 `[X+0x184]` 是什么服务**：运行时探针（`regmem` 模式打印 `[r1+0x184]` + `r1`），
   或全库扫「对偏移 0x184 的写指令」按方法名过滤。
   ★ 判据：**只要这个服务非 null 且它的查询返回 != 1，客户端就会走 `SetFtue(3)` ⇒ 状态 < 4 ⇒ 不再重置**。
2. **重跑第 56 轮那个配置**（FTUE=7 + 9 探针）——在修好洞 F 之后那轮结论会变。
3. 若 1 成功 ⇒ 观察 FTUE 是否启动（`StartFtue` 0xA86898 / `IntroCutsceneDone` 0xA869FC，
   两者都**没有直接 `bl` 调用点** ⇒ 是接口/动画事件点火 ⇒ 需要另找触发方式）。
4. 目标信号不变：`TSC`（`FlowState::StartCharacterCustomization`）与 `TQR`（`CreateCharacterRequest::Setup`）命中
   ⇒ 客户端自己发 `POST /api/game/v1/public/characters`。

---

## 十四、第五十九～六十轮（2026-11-14 晚）：★★ 修法 R6 生效——重置消失，卡点前移到「FTUE 入口没被触发」

### 十四.1 ★★★ 修法 R6：把 FTUE 状态从 4(IsAuthenticated) 改成 1(Starting)

**依据**（静态读码 + 探针）：
* 重置判据在 `OnSaveDataFetched` 尾部：`cmp r0,#4 / bge ⇒ Fire(ResetGame)`（状态 ≥4 ⇒ 必重置）。
* 状态 ≤5 时，`0xA730D8 cmp r1,#5; ble` 会命中 ⇒ 把状态改成 **4** ⇒ 下一句比较必然 ≥4 ⇒ 重置。
  ⇒ **任何初始状态 ≤5 都会被改成 4**（实测预置 3/6/7 三种都重置）。
* FTUE 状态机的真实链条（`GameplayManager`，dump.cs）：
  `StartFtue()→2(Started)` → `IntroCutsceneDone→3(FTUECutsceneDone)` → `StartFTUEQuest→5`
  → `EndFTUEQuest→6` → `EndFtue→7(CharacterCreationDone)`。
  ⇒ **新玩家应当处于 1(Starting)**，而 4=IsAuthenticated 是「已认证却没有存档」的异常态。

**补丁**（`patch_c_fire.py`，开关 `BLADES_FIX_R6`，与 R3 互斥）：
```
FIX_R6_AT = 0x00A731EC        # mov r0, #4  →  mov r0, #1
```

**实测（第五十九轮，FTUE 预置 3）**：
| 探针 | 结果 | 含义 |
|---|---|---|
| `TXX`（与 4 比较的值） | **1** | 状态已被改成 1（Starting）⇒ **< 4 ⇒ 不再重置** ✓ |
| 状态轨迹 | `… Authen×4 → Fetchi → **Initia** → Loadin → Authen → Fetchi → Initia …` | ★ **ForceReset / Bootstrap 从轨迹里消失了** ✓ |
| 每轮耗时 | ~5s | 变成「重载内容 ↔ 取存档」的循环 |

⇒ **R6 证明：重置循环的根因就是 FTUE 状态被推成 4**；修掉之后客户端不再重置。

### 十四.2 ★★ 新的卡点：教程入口从没被触发

加了两个探针（第六十轮）：
| 探针 | 位置 | 命中 | 含义 |
|---|---|---|---|
| `TSD` | `GameplayManager::StartFtue` (0xA86898) | **0** | ★ 教程从没开始 |
| `TSI` | `GameplayManager::IntroCutsceneDone` (0xA869FC) | **0** | ★ 开场动画从没结束/触发 |

⇒ 状态虽然变成 1(Starting)，但**没有任何代码去调 `StartFtue()`** ⇒ 教程链永远起不来 ⇒ 建角自然无从谈起。

**为什么调不到**：`StartFtue` / `IntroCutsceneDone` 在整个 .so 里**没有任何代码引用**
（全文件搜索方法地址，只命中数据段里的「方法指针表」0x317E464/0x317E468 ⇒ 只有表项，没有调用点）
⇒ 它们是**被场景/关卡数据按名字回调**的（`IntroCutsceneDone(LevelEventInstance)` 的参数类型就是关卡事件）
⇒ 也就是**必须真的去播那段开场关卡**才会触发。

### 十四.3 ★★ 路线图（现在基本清楚了）

```
新玩家 ⇒ (播开场关卡) ⇒ IntroCutsceneDone ⇒ StartFtue ⇒ FTUE 任务 ⇒ EndFtue(7)
      ⇒ Gameplay --CharacterSelection--> PreCharacterSelection --Proceed--> CharacterSelection
      ⇒ ★ 建角界面 ⇒ 客户端自己 POST /api/game/v1/public/characters
```
* `Gameplay`（FlowState 静态槽 0xD0）**在迁移表里没有任何 Permit 入口** ⇒ 它同样靠**动画事件**点火
  （`AnimEventFlowTriggerSource::OnAnimEvent`）⇒ 那个动画在**加载屏/主菜单场景**里。
* ⇒ 也就是说：**必须让内容加载真正走完并停在加载屏上，动画事件才会点火 → 进 Gameplay → 教程 → 建角。**
* 我们现在的循环是 `取存档 → StartLoadingContent → InitializingBundles → LoadingBaseGameContent → Authenticating → 取存档`，
  **每轮都重新加载内容**，于是：
  ① 加载屏的动画事件很可能来不及/被反复打断；
  ② 每轮重载都会触发 **`The AssetBundle 'common' can't be loaded because another AssetBundle with the same files is already loaded`**
     （第一轮加载的 'common' 还在内存里）⇒ 内容加载其实一直在失败。

⇒ **下一轮的两个候选方向**（还没验证）：
1. 让内容加载**只做一次**（不每轮重载）—— 找 `StartLoadingContent`(0x8023B8) 的调用条件，或在 `FetchingSaveData` 之后阻止再次 `LoadBaseGameContent`。
2. 解决 'common' 冲突（卸载后再加载 / 只加载一次）—— 需要看 `BundleSceneLoader` 的卸载路径。

### 十四.4 当前构建/探针/命令（第六十轮状态）

* 构建：`BLADES_NO_QUEUE_GUARD=1 BLADES_FIX_R1=0 BLADES_FIX_R2=0 BLADES_FIX_R3=0 BLADES_FIX_R4=0 BLADES_FIX_R6=1`
  → `python build_vtrace.py && python patch_c_fire.py`。
  生效补丁：**R6**（4→1）、**洞 F**、**J**、**K2**、**E/G**；**D 关闭**。
* 探针（7 个，上限 8）：`TIE` 状态名 / `TSF` OnSaveDataFetched / `TSC` 建角入口 / `TQR` 建角请求 /
  `TXX` 最终比较的状态值 / `TSD` StartFtue / `TSI` IntroCutsceneDone。
* 跑测：`RESTART=1 PUSH_SO=1 INSTALL=0 WAIT=120 ROUTES=shapes/R_perchar.json bash run_full.sh`（`FTUE_VAL` 可覆盖预置）。
* ⚠ 探针区上限 0xA7BD04（洞 F 起点）⇒ **最多 7～8 个探针**（TIE 是 0x60 的 `regstr3`）。

---

## 十五、第六十一～六十二轮（2026-11-14 晚）：★ 建角入口能被触发 + 定位到「缺的那个管理器」

### 十五.1 「重复 bundle」假设被证伪（第六十一轮）

* 假设：APK 的 `assets/Bundles/*`（**STORED 未压缩** ⇒ Unity 能直接从 APK 加载）
  与我们推到 `/files/Bundles/` 的副本重复 ⇒ 同一 bundle 被加载两次 ⇒ 触发
  `The AssetBundle 'common' can't be loaded because another AssetBundle with the same files is already loaded`。
* 实测：把设备 `/files/Bundles/` 里的 bundle 二进制挪走（只留 Manifest/ManifestExtraData/*.manifest）后，
  客户端改从 APK 归档里读 ⇒ **直接失败**（`CsvLocalizationDatabase:LoadDataFile()` +
  `ArchiveStorageReader.cpp:587` 报错）⇒ **客户端就是要用 /files/Bundles 那份** ⇒ 假设不成立。
  ⇒ 已把设备上的 2963 个文件**完整还原**（bundle + 1480 个 .manifest + Manifest + ManifestExtraData）。
* 顺带确认：`common` 与 `common_client` 的**内部资源名不重叠**
  （UnityPy 读出 `Assets/BGS/Scenes/common.unity` vs `Assets/BGS/Scenes/common_client.unity`）
  ⇒ 冲突不是它俩造成的，而是**同一个 'common' 在进程里被加载了两次**（第二次必然冲突）。

### 十五.2 ★★ 内容加载失败是**首轮**就有的（不是循环的后果）

第 60 轮时间线：
```
12:19:55.620  InitializingBundles（第一次内容加载）
12:20:05.604  GameDataLibrary.Setup → InitData NRE ×17   ← ★ 第一次加载里就抛
12:20:07.704  第一次 'common' 冲突
```
⇒ `GameDataLibrary.InitData(entry)` 是个按 `entry._entryType` 的 switch，
   NRE 说明 **`entry._data`（数据资产）是 null**；
   17 次 NRE ≈ 17 个 library entry 全部失败 ⇒ **承载游戏数据的 bundle（'common' 里有 129 张数据表）
   在 Setup() 跑的时候还没加载好** ⇒ **这是一个「加载顺序/竞态」问题**，不是缺文件。
⇒ ⇒ 我们的洞 B（一进 LoadingBaseGameContent 就发认证）让流程**跑在内容加载前面**：
   客户端在数据还没就绪时就去建数据库 ⇒ NRE ⇒ 后续 level/场景也就没法加载。

### 十五.3 ★★★ 第六十二轮：**客户端自己的建角入口被触发了**（洞 G）

* 新补丁 **洞 G**（`patch_c_fire.py`，`BLADES_HOLE_G` 开关）：
  钩 `CharacterServiceImplementation::OnGetAllCharacters`（0x16124F0，角色列表回调 = 客户端拿到列表的时刻）
  ⇒ 调一次 **`FlowState::StartCharacterCustomization`（0x7FE7E8）**。
  静态读码确认 **r0（controller）在该方法里根本没被使用**（只 `mov r5,r0`），依赖全从静态取
  ⇒ 可以安全地用 `(null, null)` 调用。
* 实测：**探针 `TSC` 命中 22 次** ⇒ 客户端真的走进了建角入口；
  而且 logcat 的 NRE 堆栈显示 **游戏自己**（`GHomeSendManager.EscapeGame`）也在调它 ⇒
  **「没有角色 ⇒ 逃到建角」本来就是游戏的自然路径**（我们只是又触发了一次）。
* 但方法内部 NRE：`at FlowState.StartCharacterCustomization(...)` ⇒ 它依赖的一个对象是 null。

### 十五.4 ★★★ 定位到「缺的那个管理器」＝ 0x810E18

`StartCharacterCustomization` 里连续三次 `bl 0x810E18`，返回值判空后使用 ⇒ 0x810E18 是**取管理器的 getter**。
反汇编它发现：

```
0x810E18  push {r4, r5, fp, lr}
0x810E24  ldr  r5, [r4, #0xc]        ; 取 this+0xC 的对象
0x810E28  ldrb r0, [r5, #0xba]       ; ★★ 读那个对象的 0xBA 标志字节
0x810E2C  tst  r0, #1
...
0x810E5C  ldrh r0, [r5, #0xba]       ; ★★ 同一处标志的 0x200 位
0x810E60  beq  ...
```

⇒ **0x810E18 操作的正是我们一路在追的那个「0xBA 标志位」管理器对象**
（§十三.2 里的 Obj1/Obj2 同款：+0xBA 标志、+0x5C/+0x74 字段）。
⇒ ⇒ **它返回 null ⇒ 建角入口拿不到控制器 ⇒ NRE。**
⇒ ⇒ 而这个管理器只会在**进入 Gameplay/UI 层**时创建 —— 我们卡在 Gameplay 之前，所以它不存在。
⇒ ⇒ **闭环**：内容加载没走完 ⇒ 进不了 Gameplay ⇒ 管理器不存在 ⇒ 建角入口必然 NRE。

### 十五.5 ★★ 一个新的工具能力：现在能「看屏幕」了

`run_full.sh` 已加截图（`SHOT_AT="25 70 115"` ⇒ `screen_*.png`）。
★ **agent 可以直接用 `read` 工具读 PNG**（工具支持图片附件）
⇒ 一旦建角/其它界面真的弹出来，**就能看着画面找按钮并点击**
（不必再像以前那样靠 hook + 坐标扫描硬猜）。
⇒ 第 62 轮三张截图都是 2,066,418 字节 ＝ 内存文档里记的
「网络错误 + 发生未知错误」那一帧 ⇒ 当时界面还没弹出来。

### 十五.6 下一轮该做什么（收敛后的唯一主线）

**主线：让内容加载真正走完一次**（而不是每轮重载、也不是在数据就绪前就认证）。
1. **探 `FlowState::OnLoadingTaskListComplete`（0x8015B4）** ⇒ 它就是「加载任务列表完成」的回调
   ⇒ 探它跑没跑，就知道内容加载有没有走完。
2. **把洞 B 的认证链改成「内容加载完成后才发」**：
   新增一个洞，钩 0x8015B4 ⇒ 置一个「内容已就绪」标志；
   洞 B 改成「标志为真才发认证链」⇒ 客户端就不会再跑在内容加载前面
   ⇒ 预期：`GameDataLibrary.Setup` 不再 NRE ⇒ 'common' 不再重复加载 ⇒ 能进 level/Gameplay。
3. 若 0x8015B4 从不触发 ⇒ 退而求其次：在洞 B 里加**延迟**（例如等 N 帧或等 'common' 加载完成的信号）。
4. 目标信号不变：`TSC`/`TQR` 命中（且**不 NRE**）⇒ 客户端自己发 `POST /api/game/v1/public/characters`。

### 十五.7 本轮新增/修改（都在 `Library/Locus/tmp/blades/`）

| 文件 | 改动 |
|---|---|
| `patch_c_fire.py` | 新增 **洞 G**（调客户端自己的建角入口，守卫字节 0x122）；新增 **修法 R6**（FTUE 4→1） |
| `build_vtrace.py` | `PROBE_LIMIT = 0xA7BD04`（洞 F 起点，防探针压洞）；探针集 TIE/TSF/TSC/TQR/TXX/TSD/TSI |
| `run_full.sh` | 固定用 SDK 版 adb；新增**截图**（`SHOT_AT`） |
| `seed_prefs.py` | `FTUE_VAL` 环境变量可覆盖预置状态 |

⚠ **写洞的硬规矩（这轮踩到）**：**洞地址必须 4 字节对齐**（ARM 指令不能落在奇地址）。
第一版洞 G 放在 0xA7BDEE（未对齐）⇒ `b`/`bl` 的相对位移算出来差 2 字节 ⇒ 跳飞到错误地址。
⇒ 洞区可用空闲区：`0xA7BDF0..0xA7BE60`（112B，已放洞 G 80B）。

---

## 十六、第六十三～六十五轮（2026-11-14 晚）：★ 自然流程已通，卡点收敛为「建角入口 NRE」

### 十六.1 ★★★ 关键结论：**洞 F（提前武装认证链）就是竞态的元凶**

| 轮次 | 配置 | GameDataLibrary NRE | 'common' 冲突 | 状态轨迹 |
|---|---|---|---|---|
| 62 | 洞 F **开**（BundlesInitialized 就武装认证） | 17 | 有 | 加载↔取存档循环 |
| 63 | 洞 F **关** + 洞 H（OnLoadingTaskListComplete 才武装） | **0** | **0** | `LoadSt→Initia→Loadin→**Authen×5**→Fetchi` |
| 64 | 63 + R2 开 | 0 | 0 | 同上 |
| 65 | 64 + 洞 I（放行加载菜单退场等待） | 0 | 0 | 同上 |

⇒ ★ **关掉洞 F 后，NRE 与 bundle 冲突全部消失** ⇒ 证实了「**流程跑在内容加载前面**」的竞态假设：
　洞 F 在「BundlesInitialized」（内容加载**刚开始**）就武装认证链 ⇒ 客户端在数据还没就绪时
　就去建数据库（`GameDataLibrary.Setup` → 17 条 entry 全 NRE）⇒ 内容/level 加载不完整。
⇒ ★ **而且客户端自己完成了认证**（第 63～65 轮轨迹里 `Authen×5` 出现在 `Loadin` 之后 6 秒，
　**没有任何补丁参与**）⇒ **游戏的自然认证流程是通的**（`b__222_41` 在内容加载完成后发 Authenticate）。
⇒ ⇒ **我们其实不需要洞 B/F 那套强行发触发器的机制了** ⇒ 洞 F 建议**永久关闭**。

### 十六.2 剩下的唯一堵点：建角入口里的 NRE

* 轨迹停在 `FetchingSaveData`（不再前进）；`TSC`=11 ⇒ 客户端**确实进了** `FlowState::StartCharacterCustomization`
  （而且 logcat 显示是**游戏自己** `GHomeSendManager.EscapeGame` 调的 ⇒ 「没有角色 ⇒ 逃去建角」是游戏的自然分支 ✓）。
* NRE 位置：`FlowState.StartCharacterCustomization(CustomizationController controller, TaskControl taskControl)`。
  它三次调用 **0x810E18** 取依赖并判空 ⇒ **0x810E18 返回 null** ⇒ NRE。
* 0x810E18 读的对象带 **0xBA 标志位**（bit0 / bit9）⇒ 与我们一路追的「0xBA 管理器」是同一个。
* ★ `CustomizationController` 存在哪：dump.cs 里 `public CustomizationController controller; // 0x8`
  属于 **`CustomizationFlowStateContext`**，而 FlowState 有一个静态字段
  `_customizationContext`（静态偏移 **0x88**）⇒
  **建角控制器是「角色选择流程」里创建的**，而「角色选择流程」要先到 `Gameplay` 状态才会跑。
* ⇒ ⇒ **闭环收敛**：`Gameplay` 进不去 ⇒ `CustomizationFlowStateContext` 没建 ⇒ 控制器为 null ⇒ 建角入口 NRE。
  而 `Gameplay` 在迁移表里**没有 Permit 入口** ⇒ 它靠**动画事件**点火
  ⇒ **必须真的加载并播出一个关卡（level scene）**（新玩家就是那段开场关卡）。

### 十六.3 两个否定实验（避免以后重复走）

* **R2（加载菜单「入场动画完成」一律当作已完成）开/关没有区别**（第 63 vs 64 轮轨迹完全一致）。
* **洞 I（`PlayLoadingMenuOutAnimation` 里立刻调 `OnOutAnimDone` 放行退场等待）没有效果**
  （第 64 vs 65 轮完全一致）⇒ ⇒ **加载菜单的动画不是当前卡点**（探针 `TLD`=0 说明
  `OnLoadingTaskListComplete` 从没触发，但那不是我们等的那个完成点）。

### 十六.4 当前推荐配置（第六十五轮那套）

```bash
export BLADES_NO_QUEUE_GUARD=1 BLADES_FIX_R1=0 BLADES_FIX_R2=1 BLADES_FIX_R3=0 \
       BLADES_FIX_R4=0 BLADES_FIX_R6=1 BLADES_HOLE_G=1 BLADES_HOLE_H=1 \
       BLADES_HOLE_I=1 BLADES_NO_HOLE_F=1
python build_vtrace.py && python patch_c_fire.py
RESTART=1 PUSH_SO=1 INSTALL=0 WAIT=120 ROUTES=shapes/R_perchar.json bash run_full.sh
```
* **R6**（FTUE 4→1）必开（否则回到 ResetGame 死循环）。
* **洞 F 关**（它是竞态元凶）；**洞 H**（内容加载完成才武装）开着。
* 洞 G（直接调建角入口）现在**可以留着**（它没造成伤害，而且将来一旦控制器存在就能立刻开界面），
  但要注意：游戏自己也会调 ⇒ 若它开始 NRE，优先怀疑洞 G 的空参数调用（先关掉 `BLADES_HOLE_G=0` 对照）。

### 十六.5 下一步（唯一主线：让客户端加载并播出一个关卡 ⇒ 进 Gameplay）

1. **找「第一个关卡」的加载入口**：新玩家开场要播的那段 level scene。
   线索：`GameplayManager::StartFtue` / `IntroCutsceneDone` 在 .so 里**只有方法指针表、没有代码引用**
   ⇒ 它们是被**关卡数据按名字回调**的 ⇒ 必须有关卡被加载。
   可查：`FlowState::LoadGameDataLibrary`（0x8029A4）、`FlowState::LoadLevelOnCharacterCreationDone`（0x805B08 附近）、
   `LevelManager` 的方法；以及 `LevelEventInstance` 这种参数类型对应的关卡事件系统。
2. **找出「Gameplay」是靠哪个动画事件点火的**：迁移表里 `Gameplay` 无 Permit 入口 ⇒ 动画事件
   （`AnimEventFlowTriggerSource::OnAnimEvent`，按**名字**点 FlowTrigger）⇒ 那个动画在某个已加载的场景里。
3. 有了关卡加载 ⇒ 动画事件点火 ⇒ Gameplay ⇒ `Gameplay --CharacterSelection--> PreCharacterSelection
   --Proceed--> CharacterSelection` ⇒ **建角界面弹出** ⇒ 届时用截图（`SHOT_AT` + `read` 读 PNG）
   看着界面点击 ⇒ 客户端自己 `POST /api/game/v1/public/characters` ⇒ **目标达成**。

---

## 十七、第六十六～八十六轮（2026-11-14 晚）：★★ 换路径 B——后端给占位角色，客户端**认下角色并拉全套状态**

> 用户反馈「还是进不去游戏」⇒ 决定换策略：
> **路径 A**（纯逆向：让客户端自己建角）卡在 Gameplay/UI 层 ⇒ 暂时搁置；
> **路径 B**（后端给一个占位角色 ⇒ 客户端跳过建角 ⇒ 去加载城镇）⇒ 目标是「先能玩」。
> 路径 A 的成果不丢：客户端确实在走它自己的建角分支（已留证）。

### 17.1 客户端要的角色契约（全部来自 APK 的 .schema/.conversion，不是猜的）

| 端点 | 形状 | 依据 |
|---|---|---|
| `GET /characters` | `{"characters": [**数组**], "BackendCharacterListData":[{"BackendCharacterData":{BackendId,Name,TagId,CharacterSave}}]}` | `charactersListToBackendCharactersListData.conversion`（ReadArrayElements + GetOrCreateArrayElementWithId）+ `BackendData.schema` |
| 角色元素 | `id/name/tagId/level/experience/equippedAbilities/abilities` + **`extraData` = URL 编码的 CharacterData JSON**（`SelectedUid/ShapeWeight/Scale`） | `character.conversion`（UrlDecode → DeserializePersistentJson） |
| `GET /characters/{id}` | 同上（该端点返回**该角色的完整存档**） | `GetCharacter.conversion` |
| `GET /sync` | 整份 SaveGame（Player/Town/Quests/Settings…）+ `BackendCharacterData` | `SaveGame.schema` |

★ **踩过的三个坑（都会静默毁掉一整轮）**：
1. **`characters` 必须是数组**（记忆文档里「客户端要字典」那条在这个端点是错的）⇒ 给字典报
   `InvalidCastException: PersistentObject → PersistentArray`。
2. **路由顺序**：服务端「先匹配先赢」⇒ 新加的具体路由必须排在兜底 `/api/*` **之前**
   （之前 `/characters/1` 命中的是通用合并体，len=1163 ⇒ 客户端拿不到数据块 ⇒ KeyNotFound）。
   ⇒ `{id}` 模板路由也要删掉，否则它会挡在具体路由前面。
3. **数值字段的类型**：客户端有两条解析路径，**直接 cast 路径**（`SequentialParser`）会把 JSON 值直接转成
   模型类型 ⇒ 字符串数字会炸 `InvalidCastException: 'String'→'Int64'`。
   实测踩到的具体点：
   * `session.tokenExpirationSeconds/featureStatus/linkedAccountsStatus` ⇒ **要数字**（不是字符串）
   * `Character.ShapeWeight` ⇒ **整数 45~55**（`CHARACTER_SHAPE_WEIGHT_MIN/MAX`），
     发 `1.0`(Double) ⇒ `'Double'→'Int64'`
   * 存档串（`characterSave` 是 JSON **字符串**）内部的 `0.0/1.0` 也要整数化（字符串里的不会被批量转换处理到）
   ⇒ 已写 `check_types.py`：按 .schema 逐字段核对路由响应体的类型，一次性列全部不匹配。

### 17.2 ★★★ 现在的实测状态（第六十五～八十六轮）

```
客户端启动 → host_info / MetaManifest → 认证(自然发生) → /sync → /characters
   → ★ 认下我们的角色 ★（此前从不发生）
   → GET /characters/1
   → GET /characters/1/towns/current
   → GET /characters/1/wallets/current
   → GET /characters/1/inventories/current（注意是单数 character/1/…）
   → GET /characters/1/dungeons / abysses/current / crafts
   → GET /characters/1/globalshops/current / globalgifts
   → POST /characters/1/quests / challenges
```
⇒ **这就是当年（第三十轮）那 16 个端点** ⇒ 客户端已经进入「加载我的存档/城镇」的阶段。

### 17.3 当前唯一剩下的报错：`GET /characters/1` ⇒ `ResponseData.GetData(2)` KeyNotFound

* 堆栈：`CharacterServiceImplementation.OnGetCharacter` → `ResponseData.GetData(int key)`
  ⇒ OnGetCharacter 里 `0x1612B80: mov r1, #2` ⇒ **它要「数据键 2」**。
* `ResponseData` = { `_rootObject`(0x8), `_dataDictionary`(0xC): Dictionary<int, BackendDataInformation> }
  ⇒ 数据块是**转换管线**按「键」塞进去的（`AddBackendData(int dataKey, IBackendData, bool)`）。
* 我们试过：最小体 / 完整存档 / 带列表形状 / 带 character / SaveGame / BackendData / CharacterData …
  ⇒ 仍然取不到键 2 ⇒ **键 2 对应的块没被产出**（键的含义还没定位：
  可能是某个 schema 的注册 id，或某个 `DataKey` 枚举值）。
* ⇒ 下一步：用 `ResponseDeserializer`(0x1B786E4) / `AddBackendData` 的调用点反查「键 2 是什么」，
  或者反查 `OnGetCharacter` 后续代码怎么用这个块（它拿到后要做的事会告诉我们它想要什么）。

### 17.4 复跑方式

```bash
cd Library/Locus/tmp/blades
python make_withchar.py                     # 生成 shapes/R_withchar.json（带角色 + 逐端点正确形状）
export MSYS_NO_PATHCONV=1 FTUE_VAL=3
PUSH_SO=0 INSTALL=0 WAIT=120 ROUTES=shapes/R_withchar.json bash run_full.sh
```
* `make_withchar.py` 会自动：把 `/characters` 改成数组+BackendCharacterListData、把 `/sync` 覆盖成整份存档、
  给 10 个角色相关端点各自补**schema 正确**的最小响应（插在兜底之前）、统一数字/字符串类型、
  生成 URL 编码的 `extraData`。
* 客户端补丁现状：**R6 开**（FTUE 4→1，不重置）、**洞 F 关**（它是竞态元凶）、**洞 H 开**（内容加载完成才装配认证链）、
  **洞 G 关**（★ 它自己用 null 参数调建角入口 ⇒ 会制造假的 NRE，误导判断）、R2 开。

---

## 十八、第八十七～九十六轮（2026-11-14 晚～深夜）：★★ 客户端认下角色 → 状态到 7 → 卡在「世界加载」

### 18.1 三个数据层硬规则（踩出来的，务必保留）

| 规则 | 症状 | 修法 |
|---|---|---|
| **数组元素要带端点自己的 id 字段** | `KeyNotFoundException` @ `SearchArrayToDictionaryParser.Parse` | 探针实测：客户端用 **`questId`** 给任务数组建字典（`SearchArrayToDictionaryParser._fieldtoMatchInJson`）⇒ 元素必须带 `questId`（其它端点同理：`challengeId`/`dungeonId`/…）⇒ 已在 `make_withchar.py` 里统一补别名 |
| **数值字段不能给字符串**（直接 cast 路径） | `InvalidCastException: 'String'→'Int64'` | `session.tokenExpirationSeconds/featureStatus/linkedAccountsStatus` **要数字**；`ShapeWeight` 是**整数 45~55**（`CHARACTER_SHAPE_WEIGHT_MIN/MAX`）；**存档 JSON 字符串内部**的 `0.0/1.0` 也要整数化（批量转换处理不到字符串内部） |
| **路由顺序：具体路径必须在兜底 `/api/*` 之前** | 客户端拿到的是通用合并体（len=1163）⇒ 数据块取不到 | `make_withchar.py` 统一把具体路由排前、兜底排尾，并**删掉 `{id}` 模板路由**（它们会挡在具体路由前面） |

⇒ 工具：`check_types.py`（按 .schema 逐字段核对响应体类型）、`make_withchar.py`（生成整份「带角色」路由表）。

### 18.2 ★★★ 现在的实际状态（第九十六轮）

```
LoadSt → Initia → Loadin → Authen×5 → Fetchi → Initia → Loadin → …（停在加载）
```
* ✅ 认证**自然发生**（洞 F 关掉之后）
* ✅ 客户端**认下我们的角色** ⇒ 主动拉 `/characters/1` + 10 个状态端点（城镇/钱包/背包/地下城/深渊/打造/商店/礼物/任务/挑战）
* ✅ `TXX` = **7 = CharacterCreationDone** ⇒ **客户端认为角色已建好**
* ✅ **0 异常、0 崩溃**（R1 开 ⇒ 不再 ForceReset，也就不在拆解时空指针崩）
* ❌ **卡在 `LoadingBaseGameContent`**：`OnLoadingTaskListComplete`（探针 TLD）**从不触发** ⇒
  「加载任务列表」永远不完成 ⇒ **世界内容（关卡/场景/bundle）没加载完** ⇒ 进不了城镇。
* 画面：游戏的**加载屏**（城堡剪影 + 玩法提示「不同的哥布尔种类有不同的弱点和抗性」+ 转圈），
  偶尔叠着游戏的通用「网络错误」框。

### 18.3 本轮修掉的两个自伤

* **洞 I 第一版**（在 `PlayLoadingMenuOutAnimation` 里无条件调 `OnOutAnimDone`）⇒ 菜单还没准备好时
  `FlowState::_loadingMenuControls`（静态 0x124）为 null ⇒ `OnOutAnimDone` 抛 NRE ⇒ 加载被打断
  ⇒ 重置 ⇒ 拆解时空指针崩溃。⇒ 现在先查 0x124 非空再调用（洞 I 挪到洞 G 的空间 0xA7BDF0）。
* **TKEY 探针**（打印 `_fieldToMatchInJson`）**自己会把客户端搞崩**（该字段为 null 时读它 ⇒ SIGSEGV fault 0x0）
  ⇒ 已移除（字段名已拿到：questId）。

### 18.4 ★ 当前最佳配置（可直接续跑）

```bash
cd Library/Locus/tmp/blades
export PYTHONPATH="H:\\locus\\data\\managed-python\\windows-x64\\python-3.13\\site-packages"
export BLADES_NO_QUEUE_GUARD=1 BLADES_FIX_R1=1 BLADES_FIX_R2=1 BLADES_FIX_R3=0 \
       BLADES_FIX_R4=0 BLADES_FIX_R6=1 BLADES_HOLE_G=0 BLADES_HOLE_H=1 BLADES_HOLE_I=1 BLADES_NO_HOLE_F=1
python build_vtrace.py && python patch_c_fire.py          # → build/libil2cpp.c.so
CHAR_MODE=char python make_withchar.py                    # → shapes/R_withchar.json
export MSYS_NO_PATHCONV=1 FTUE_VAL=3
RESTART=1 PUSH_SO=1 INSTALL=0 WAIT=120 ROUTES=shapes/R_withchar.json bash run_full.sh
```
* 补丁含义：**R1** 不再 ForceReset（★ 不然会崩）、**R2** 加载菜单入场动画当作已完成、
  **R6** FTUE 状态 4→1、**洞 H** 内容加载完成才武装认证链、**洞 I** 放行退场等待、
  **洞 F 关**（它是「提前认证」的竞态元凶）、**洞 G 关**（它自己用 null 参数调建角入口会造假 NRE）。
* 探针（6 个）：TIE 状态名 / TSF OnSaveDataFetched / TSC 建角入口 / TQR 建角请求 / TXX FTUE 状态值 / TLD 加载完成。

### 18.5 下一步（唯一主线：让「加载任务列表」完成）

1. **找出是哪个加载任务不完成**：`BGS.Shared.TaskList.TaskListRunner` 的任务列表；
   探 `GameDataLibrary.Setup` / `BundleSceneLoader` / Wwise 初始化 / 关卡加载。
2. **重点怀疑**：
   * `'common' bundle 冲突`（第一遍加载成功、之后重复加载必失败 ⇒ 第二遍起内容是坏的）
   * **关卡场景没加载**：城镇关卡要从 bundle 里加载，而我们的存档里 `Town.BuildingSiteGroups` 是空的
     ⇒ 需要填**真实城镇数据**（'common' bundle 里的 `TownData`/`BuildingSiteGroups` ⇒ 可用
     `tables/*.csv` 抽取的数据来造）
   * 音频（Wwise）初始化在模拟器上失败也会挂住加载列表
3. 一旦 `TLD` 能触发，流程就会进 `Gameplay` ⇒ 城镇 ⇒ 然后才轮到 `Gameplay --CharacterSelection-->
   PreCharacterSelection`（**这才是「客户端自己建角」那一段**，路径 A 可以接回来了）。

---

## 十九、运维修复（2026-11-15）：★「更新包下载失败」的真正原因 = **反向隧道没了**

### 19.1 现象与原因

用户反馈：**手动从模拟器启动游戏**时，弹出**原始症状**
「网络错误 / 更新包下载失败，请确保你的设备网络连接正常。(0-10000-1)」。

排查（`adb reverse --list` / `netstat` / 设备 hosts）：

| 检查项 | 状态 |
|---|---|
| 设备 `/system/etc/hosts`：`bladesasia.jijiagames.com → 127.0.0.1` | ✅ 在 |
| 宿主后端 8443 / 8080（schtasks 起） | ✅ 在 |
| **`adb reverse tcp:443 tcp:8443`** | ❌ **空的** |

⇒ **根因**：游戏走 https(443)，设备的 hosts 把域名指向 127.0.0.1，
   但**设备本地的 443 没有隧道**（`adb reverse` 需要 adb server 存活，
   而 adb server 每次工具调用结束都会被**回收进程树**⇒ 隧道随之消失）⇒
   客户端连 `host_info` / `MetaManifest` 都拿不到 ⇒ 报 0-10000-1。
⇒ **不是游戏的问题，也不是后端的问题，是隧道只在跑测脚本期间存在。**

### 19.2 修法：常驻守护 `blades_offline_server.cmd` + 计划任务 `BladesOffline`

> 🚫 **本节已作废（2026-11-19）——不要照着做。**
> 这个「自愈循环 + 登录触发器」组合会让后端在两次跑测之间每 20 秒自己回来，
> 用户手动关都关不掉（事故与清除步骤见 §二十二）。它已被**删除**，
> 且 `run_full.sh` 会顺手再删一次。现行做法：后端只在**同一次 bash 调用内** `nohup` 起。
> 以下保留作“当时为什么这么做”的历史记录。

`Library/Locus/tmp/blades/blades_offline_server.cmd`（循环，每 ~27 秒自愈）：
1. 后端 8443 / 8080 没跑就 `start /min` 拉起来
2. `adb start-server` 保证 adb server 在
3. 重建 `tcp:443→8443`、`tcp:80→8080` 两条反向隧道

注册为**登录时自启**的计划任务：
```bash
export MSYS_NO_PATHCONV=1
schtasks /create /tn BladesOffline /sc ONLOGON \
  /tr "cmd /c \"H:\Project\threeKingdomSlayer\threeKingdomSlayer\Library\Locus\tmp\blades\blades_offline_server.cmd\"" /f
schtasks /run /tn BladesOffline      # 立即生效
```
★ 坑：脚本里**不能用 `timeout /t`**（非交互控制台会直接退出进程）⇒ 改用
   `ping -n N 127.0.0.1 >nul` 延时。
★ 坑：Git Bash 下 `schtasks /create` 会被当成路径 ⇒ 必须 `export MSYS_NO_PATHCONV=1`。

### 19.3 验证（手动启动游戏，非跑测脚本）

```
$ adb shell am force-stop <pkg>; adb shell am start -n <pkg>/.MainActivity
后端收到：host_info → MetaManifest → POST auth/gplus → GET /sync → GET /characters
          → GET /characters/1 → wallets/towns/inventories/abysses/dungeons/…
```
⇒ **「更新包下载失败」消失**；画面从「原始症状」变成游戏标题画面 + 游戏自己的
   通用错误框（「网络错误 / 发生未知错误，请重试。」）⇒ 剩下的就是 §十八 那个
   「世界加载任务列表不完成」的问题（见 §18.5 的下一步）。
⇒ ⚠ **不要点那个「好的」按钮**：点了会把游戏送进后台并被系统杀掉（早期事故记录）。

---

## 二十、★ 续跑交接（2026-11-14 深夜）——**新对话从这一节接**

### 20.1 一句话现状

客户端现在能：自然认证 → 认下我们给的占位角色 → 拉齐全套状态（城镇/钱包/背包/地下城/深渊/打造/商店/礼物/任务/挑战），
FTUE 状态到 **7 = CharacterCreationDone**，**0 异常 0 崩溃**；
**仍然玩不到内容**，因为流程卡在「重载内容 ↔ 取存档」的循环里 ⇒
**世界内容（关卡/场景）加载不完成** ⇒ 进不了 `Gameplay` ⇒ 到不了建角界面。

### 20.2 本轮解决的三个真凶（都有探针/读码证据）

| # | 现象 | 根因 | 修法 |
|---|---|---|---|
| 1 | 每轮 `ForceReset` ⇒ 无限重置 | `OnSaveDataFetched` 会把 FTUE 状态改成 **4(IsAuthenticated)**，而尾巴 `cmp r0,#4 / bge` ⇒ 状态 ≥4 必重置。**任何**初始状态（3/6/7 实测过）都逃不掉 | **R6**：0xA731EC `mov r0,#4` → `mov r0,#1`（Starting）⇒ 状态 <4 ⇒ 不再重置 | `BLADES_FIX_R6=1`（默认开） |
| 2 | 后端给了角色，客户端不认 | `OnSaveDataFetched` 的**第一个分支**就是 `saveGameFound`（0xA72B74），它问存档服务 `get_IsAcceptedToPlay`（探针 TQM 打出方法名）**必须 == 1**，否则掉回重置 | **R8**：`SaveService.get_IsAcceptedToPlay`(0x8220EC) 的 `ldrb r0,[r0,#0x25]` → `mov r0,#1` | `BLADES_FIX_R8=1`（默认开） |
| 3 | 角色列表被无视 / KeyNotFoundException | ① `characters` 必须是**数组**；② 数组元素要带**端点自己的 id 字段**（实测任务数组用 **`questId`**）；③ 数值字段不能给字符串（`session.tokenExpirationSeconds`、`ShapeWeight` 是整数 45~55、存档 JSON 字符串内部的 `0.0` 也要整数化）；④ **具体路由必须排在兜底 `/api/*` 之前**（否则客户端拿到通用合并体 len=1163） | 全部落在 `make_withchar.py` + `check_types.py` | — |

### 20.3 现在的实测轨迹（第 102 轮）

```
LoadSt → Initia → Loadin → Authen×5 → Fetchi → Initia → Loadin → …（每轮约 5 秒）
```
* 探针：`TIE` 状态名 / `TSF`=OnSaveDataFetched 参数（`(1,1)`＝有存档且已认证）/ `TRV`=`get_IsAcceptedToPlay` 返回值（**1** ✓）/ `TQM` 方法名 / `TLD`=OnLoadingTaskListComplete（**0** ✗）/ `TSC`/`TQR` 建角信号（0）。
* `TLD = 0` ⇒ **「加载任务列表」永远不完成**。
* `StartFtue`(0xA86898) / `IntroCutsceneDone`(0xA869FC) 探针 **0 命中** ⇒ **开场关卡从没播**。
* `Gameplay` 状态（FlowState 静态槽 0xD0）在迁移表里**没有 Permit 入口** ⇒ 只能靠**动画事件**点火
  （`AnimEventFlowTriggerSource::OnAnimEvent`）⇒ **必须真的加载出一个场景**。
* logcat 每轮刷 `The AssetBundle 'common' can't be loaded because another AssetBundle with the same files is already loaded`
  ⇒ **次生症状**（第二轮加载必然失败），不是根因。
  ★ 已验证：设备上的 bundle 之间**没有重名冲突**（UnityPy 逐个列出内部序列化文件比对过）；
  也验证过 `common` 与 `common_client` 内部名不同（`BuildPlayer-common` vs `BuildPlayer-common_client`）。

### 20.4 ★ 当前最佳配置（直接复制即可续跑）

```bash
cd H:/Project/threeKingdomSlayer/threeKingdomSlayer/Library/Locus/tmp/blades
export PYTHONPATH="H:\\locus\\data\\managed-python\\windows-x64\\python-3.13\\site-packages"

# ① 构建（当前 .so md5 = bb1ab716abcfc31864b954ba065ac430，设备上已是同一份）
export BLADES_NO_QUEUE_GUARD=1 BLADES_FIX_R1=1 BLADES_FIX_R2=1 BLADES_FIX_R3=0 BLADES_FIX_R4=0 \
       BLADES_FIX_R6=1 BLADES_FIX_R8=1 BLADES_HOLE_G=0 BLADES_HOLE_H=1 BLADES_HOLE_I=1 BLADES_NO_HOLE_F=1
python build_vtrace.py && python patch_c_fire.py

# ② 生成「带角色」的路由表
CHAR_MODE=char python make_withchar.py            # → shapes/R_withchar.json

# ③ 跑一轮
export MSYS_NO_PATHCONV=1 FTUE_VAL=3
RESTART=1 PUSH_SO=1 INSTALL=0 WAIT=120 ROUTES=shapes/R_withchar.json bash run_full.sh
```

### 20.5 补丁全表（`patch_c_fire.py`）

| 补丁 | 位置 | 作用 | 开关 |
|---|---|---|---|
| **R1** | 0xA73618 `bge`→`b` | 无存档时**不 ForceReset**，改走 StartLoadingContent（否则重置→拆解时空指针崩溃） | `BLADES_FIX_R1` |
| **R2** | 0x800DC8 | 加载菜单「入场动画完成」一律当作已完成 | `BLADES_FIX_R2` |
| **R6** | 0xA731EC `mov r0,#4`→`#1` | FTUE 状态 4→1（Starting）⇒ 不重置 | `BLADES_FIX_R6` |
| **R8** | 0x8220EC `ldrb r0,[r0,#0x25]`→`mov r0,#1` | `get_IsAcceptedToPlay` 恒真 | `BLADES_FIX_R8` |
| **洞 H** | 0xA7BD5C，钩 0x8015B4 | **内容加载完成**（OnLoadingTaskListComplete）才装配认证链 | `BLADES_HOLE_H` |
| **洞 I** | 0xA7BDF0，钩 0x801268 | `_loadingMenuControls`(静态 0x124) 非空时放行**退场**等待 | `BLADES_HOLE_I` |
| 洞 F | 0xA7BD04，钩 0xA7381C | 在「BundlesInitialized」时装配认证链 ⇒ **竞态元凶，保持关** | `BLADES_NO_HOLE_F` |
| 洞 G | 0xA7BDF0（已挪给洞 I） | 直接调建角入口 ⇒ **它自己用 null 参数调用会造假 NRE，保持关** | `BLADES_HOLE_G` |

### 20.6 探针与洞区（硬约束，踩过）

```
洞区 0xA7B9EC..0xA7BFDC：
  0xA7B9EC 洞A(68B) | 0xA7BA40 洞B(148B) | 0xA7BADC **探针区** | 0xA7BD04 洞F(88B)
  0xA7BD5C 洞H(56B) | 0xA7BD4C 洞E(停用，与洞F重叠) | 0xA7BDA0 洞C | 0xA7BE40 洞I(68B)
  0xA7BE60 洞J | 0xA7BE90 洞K2 | 之后是活代码（没有更多空间）
```
* **探针区上限必须写成 `0xA7BD04`（洞 F 起点）**，`build_vtrace.py` 里已改。
  写错成 0xA7BDA0 ⇒ 第 9 个探针起压住洞 F ⇒ 第 56 轮洞 F 被静默覆盖（结论不可信）、第 57 轮客户端直接崩。
* 探针上限：普通 0x48/个、`regstr3` 0x60/个 ⇒ **探针区 552 字节 ⇒ 最多 7 个普通 + 1 个 regstr3（当前正好 7 个）**。
* **洞地址必须 4 字节对齐**（第一版洞 G 放 0xA7BDEE ⇒ `b/bl` 位移差 2 字节 ⇒ 跳飞）。
* 探针模式：`reg`/`reg2`/`reg2s`/`regstr3`/`cstr`（读 [rX+off] 当 char*，可打印 MethodInfo+0x8 的方法名）。
  ★ `cstr` 在字段为 null 时会**把客户端搞崩**（SIGSEGV fault 0x0）⇒ 用完要撤。
* `build_vtrace.py` 的 HOOKS 里 `TSD`（StartFtue 0xA86898）、`TSI`（IntroCutsceneDone 0xA869FC）、
  `TSV`（0xA72BB8 读 _saveService）、`TRV`（0xA72C6C 读接口返回值）、`TLD`（0x8015B4）都可随时换回来。

### 20.7 本轮新工具/产物（`Library/Locus/tmp/blades/`）

| 文件 | 作用 |
|---|---|
| `make_withchar.py` | 生成 `shapes/R_withchar.json`：角色列表（数组）+ `BackendCharacterListData` + 10 个状态端点的**逐端点 schema 正确**响应 + 统一类型 + URL 编码 extraData；**自动把具体路由排到兜底之前**；`CHAR_MODE=sync|char|full|empty` 切换形状 |
| `check_types.py` | 按 `schemas_out/*.schema` 逐字段核对路由响应体类型，一次列全部不匹配 |
| `blades_offline_server.cmd` + `launch_blades_offline.vbs` | **常驻守护**：后端 + adb 反向隧道自愈（VBS 隐藏启动 ⇒ 开机不弹窗） |
| `schemas_out2/` | 用 UnityPy 从 `sharedassets0.assets` 重新精确抽取的 35 个 TextAsset（schema/conversion 原文） |
| `osdf.asm` / `flowstate.asm` | `OnSaveDataFetched` lambda 与 FlowState 的完整反汇编（控制流分析用） |

### 20.8 下一步（三步，按顺序）

1. **查是哪个加载任务不完成**（TLD=0 ⇒ 任务列表不完成）：
   * 探 `BGS.Shared.TaskList.TaskListRunner::Update` / `Task::SignalCompletion`；
   * 重点怀疑：bundle 加载（'common' 重复加载）、`GameDataLibrary.Setup`、Wwise 音频初始化、关卡场景加载。
2. **让内容加载只走一次**（当前每轮都 `Initia` 重载 ⇒ 第二轮起 'common' 必失败）：
   * 找 `FlowState::StartLoadingContent`(0x8023B8) 的调用条件，或在 `FetchingSaveData` 之后阻止再次 `LoadBaseGameContent`；
   * 洞 I 放行退场等待后可试。
3. **加载完成 → `Gameplay` → 建角界面**：
   * `Gameplay --CharacterSelection--> PreCharacterSelection --Proceed--> CharacterSelection`（迁移表）；
   * 界面弹出后**用截图驱动**（`read` 能读 PNG ⇒ 可看着点；`run_full.sh` 已加 `SHOT_AT` 截图）；
   * 目标信号：`TSC`（`FlowState::StartCharacterCustomization` 0x7FE7E8）/ `TQR`（`CreateCharacterRequest::Setup` 0x15B46E4）命中 ⇒ 客户端自己 `POST /api/game/v1/public/characters`。

### 20.9 会浪费时间的坑（速查）

* **别点游戏里的「好的」按钮**（网络错误框）⇒ 会把游戏送进后台并被系统杀掉。
* `adb reverse` **只有 Android SDK 版 adb 支持**（雷电自带 1.0.31 会打印帮助）；
  两套 adb 不能同时跑（SDK 版会把雷电的 server 踢掉 ⇒ 设备 offline）；
  adb server 重启后**第一次 reverse 会掉线** ⇒ 直接用 `RESTART=1` 重启模拟器。
* Git Bash 下 `schtasks /create|/run` 会被当路径 ⇒ 先 `export MSYS_NO_PATHCONV=1`。
* 计划任务里**别用 `timeout /t`**（非交互控制台直接退出进程）⇒ 用 `ping -n N 127.0.0.1 >nul`。
* ARM32：`and Rd,Rn,#imm` **默认不带 S 位 ⇒ 不改标志位**（S 在 bit20）⇒ 判读分支时先确认中间指令有没有覆盖标志位。
* ARM32 单数据传送：**字形式 imm12 就是字节偏移**（`ldr r1,[r0,#0x124]` 的编码 0xE5911124，Rt 在 bits15-12）。
* 探针/洞区容量见 §17.5；**先看洞区地图再放任何东西**。
* **客户端不会读 APK 归档里的 bundle**（它从 `/files/Bundles/` 读）⇒ 设备上的 2963 个文件必须齐全。

---

## 二十一、★ 第一百零三轮～一百零六轮（2026-11-16）：★★ 卡点彻底改写——「加载任务列表不完成」是**果**，不是因

> 本节是**当前最新**的状态（§〇～§二十保留作推理链）。
> 本轮没有让客户端进到游戏，但**把「为什么进不去」从「不知道」变成了一条可判定的指令链**，
> 并给出**三个立即数级别的补丁方案**（无需新增代码洞）。

### 21.1 一句话现状

客户端**能自然认证、认下占位角色、拉齐全套状态**（后端契约这层已通），
但**流程永远停在 `FetchingSaveData`**：
`InitializingBundles → LoadingBaseGameContent → (LoadFirstLevel) → Fire(Authenticate) → Authenticating
→ FetchSaveData → FetchingSaveData → OnSaveDataFetched → (R1) UpdateBundles → InitializingBundles → ∞`

**城镇关卡从头到尾没被加载过**（本轮新探针实测，见 21.4）。

### 21.2 ★★ 循环的真正机理（读码 + 探针双证）

`FlowState.<>c__DisplayClass144_0.<OnSaveDataFetched>b__0`(0xA72B28) 的**尾巴**：

```
0xA73588  ldr r0,[r0,#0x5c]      ; FtueState 的 static_fields
0xA7358C  ldr r4,[r0]           ; r4 = FtueState._instance（静态槽 0x0）
0xA73610  blx r2                ; IFtueState.get_Status() ⇒ FTUE 状态
0xA73614  cmp r0,#4
0xA73618  bge 0xA73650          ; ★ 状态 ≥4 ⇒ Fire(ResetGame) → ForceReset
0xA73648  bl  FlowState::StartLoadingContent   ; 状态 <4 ⇒ Fire(UpdateBundles) → InitializingBundles
0xA73688  ldr r4,[r0,#0x98]     ; FlowTrigger.ResetGame（静态槽 0x98）
0xA736A0  bl  FlowTrigger::Fire
```

⇒ **两条分支都会回到「重载内容」**：状态 ≥4 走 ResetGame（ForceReset→Bootstrap→InitializingBundles），
状态 <4 走 UpdateBundles（直接 InitializingBundles）。
⇒ **修法 R1（把 `bge` 改成无条件 `b`）只是把「重置循环」换成了「重载循环」，并没有跳出循环。**
⇒ 而 `LoadingBaseGameContent` 的动作里 `LoadFirstLevel` 会 `Fire(Authenticate)` ⇒ 又回到取存档 ⇒ 闭环。

### 21.3 ★★ `FetchingSaveData` 的 5 条 permit（旧迁移表漏了一条）

| 触发器（静态槽） | 目标状态 |
|---|---|
| Authenticate (0xA4) | Authenticating |
| AuthenticateNoUi (0xA8) | Authenticating |
| UpdateBundles (0x104) | InitializingBundles |
| LoadBaseGameContent (0x110) | LoadingBaseGameContent |
| **ResetGame (0x98)** | **ForceReset** ← 尾调用 `b Permit`（0x10EA3EC） |

⚠ **旧脚本 `build_transition_table.py` 只认 `bl Permit`、不认尾调用 `b Permit` ⇒ 漏了最后一条**；
新脚本 **`build_transition_table2.py`**（本轮新增）已修正「尾调用 + 触发器/目标判定串位」，
输出 `transition_table2.txt`（部分行仍有误读，用前请复核）。

### 21.4 ★★★ 本轮实测（探针 TIE/TSF/**TLF**/**TLV**/**TGD**/**TRU**/TLD，设备 .so md5 `bb1ab716…`）

| 探针 | 位置 | 命中 | 结论 |
|---|---|---|---|
| TLF | `FlowState::LoadFirstLevel` (0x80510C) | **10** | ★ **「第一关卡加载决策点」每轮循环都跑** |
| TLV | `FlowState::LoadLevel` (0x804CD0) | **0** | ★★ **城镇关卡从未加载** |
| TGD | `FlowState::GameplayLoadingDone` (0x7FF8AC) | **0** | 关卡没加载完 ⇒ 不会 Fire(LoadTown) |
| TRU | `TaskListRunner::Run` (0x13A8964) | **0** | ★ **「加载任务列表」从未启动** |
| TLD | `FlowState::OnLoadingTaskListComplete` (0x8015B4) | **0** | 同上（§20.8 第 1 步的假设被推翻） |
| TSF | `OnSaveDataFetched` (0x801CE8) | 20 | 实测参数 **(1,1)**＝有存档且已认证 |

### 21.5 ★★★ 关键结构（读码结论）

```
FlowState::PrepareLoadingScreenTaskList (0x8003D4)
   └─ new SequentialTaskList() + AddTask ×5        ← 「加载屏任务列表」
FlowState.<>c.<.cctor>b__222_189 (0x10E44E8)
   └─ PrepareLoadingScreenTaskList()
   └─ TaskListRunner::Run(taskList, OnLoadingTaskListComplete)   ← 只有它会启动那个列表
```

* ★ **`Loading` 状态是唯一的关卡加载入口**：
  只有 `LoadingBaseGameContent / GameplayWaitForLoadingDone / LevelLoaded / Exploration / SyncVisitingTown`
  这些状态 permit 了 `LoadTown / LoadMainMenu / LoadQuest …` → `Loading`。
  ⇒ **TLD=0 是「从没进过 `Loading`」的后果，不是原因**（修正 §20.8 第 1 步）。
* ★ `FlowState::LoadFirstLevel`(0x80510C) 与 `FlowState::LoadLevel`(0x804CD0) **都没有直接调用点**
  ⇒ 它们是 **TaskDelegate**（任务列表里的 task）。
  但实测 TLF=10 而 TRU=0 ⇒ **LoadFirstLevel 跑的是 `SetupGameManagers`(0x803414) 建的那张表**
  （0x803D70 建表、0x803E94 AddTask），**不是** `Loading` 那张。

### 21.6 ★★ `LoadFirstLevel` 的分支（决定「加载城镇」还是「重新认证」）

```
0x8052C4  ldr r4,[r0,#0x184]      ; r4 = FlowState._saveService（静态 0x184）
0x8052DC  取 ISaveService 的第 0 个方法（接口槽 0x19C）= get_ClearSaveGame
0x805644  blx r2
0x805648  cmp r0,#0
0x80564C  bne 0x8057B4            ; ClearSaveGame != 0 ⇒ GameplayManager::StartFtue (0xA86898)
0x805650  取另一个接口的第 0 个方法（接口槽 0x10C）⇒ 返回字符串
0x805714  bl String::IsNullOrEmpty
0x80571C  bne 0x8057B4            ; 字符串非空 ⇒ 同样 StartFtue
0x805720  置一个静态标志位
0x8057A8  …  ⇒ 落到 switch 跳转表
```

`LoadFirstLevel` 的四个出口（**彼此之间是 switch 跳转表的间接跳转，不是顺序 fall-through**）：

| 出口 | 地址 | 内容 |
|---|---|---|
| ① | 0x805388 | `GameplayManager::StartFTUEQuest` (0xA86AD8) |
| ② | 0x8053E8 | `FlowTrigger::Fire(Authenticate)` ← **我们走的这条（循环）** |
| ③ | 0x805474 | `GameplayManager::EnterTownPostFtue` + **`FlowState::LoadLevel(levelType=2, loadType=0)`** ← **想要的那条** |
| ④ | 0x805624 | `GameQuestManager::ValidateConsistency` |

### 21.7 ★★ 下一轮的三个补丁方案（按性价比，全部是「改立即数」，不需要新洞）

> 三个方案都基于「让流程进 `Loading` 状态 ⇒ 那张加载任务列表才会跑 ⇒ 城镇关卡才会被加载」。

**方案 C（推荐，最小改动）**：把 `FetchingSaveData` 的第 5 条 permit 改成 `Permit(LoadTown → Loading)`，
同时让 `OnSaveDataFetched` 的尾巴 Fire 的是 `LoadTown`：

| 补丁 | 位置 | 原值 → 新值 | 作用 |
|---|---|---|---|
| **C1** | 0x00A73688 | `ldr r4,[r0,#0x98]` → `#0xC4` | Fire 的触发器：ResetGame → **LoadTown** |
| **C2** | 0x010EA3BC | `ldr r5,[r0,#0x164]` → `#0x134` | 目标状态：ForceReset → **`Loading`** |
| **C3** | 0x010EA3C0 | `ldr r6,[r1,#0x98]` → `#0xC4` | 触发器：ResetGame → **LoadTown** |
| 构建 | — | `BLADES_FIX_R1=0` | R1 必须关：否则尾巴永远走不到 Fire（0xA73650） |

⚠ **风险**：`FetchingSaveData` 的父状态是 `InitializingGame`（静态 0x168），`Loading` 的父状态是 `Master`（0x154）
⇒ 这是**跨分支跳转**，Stateless 会先退出 `InitializingGame`（可能触发清理动作）再进 `Loading`。
⇒ 第一轮建议同时保留 TIE（状态轨迹）探针，看是否出现 `UninitializingGame` 之类的意外状态。

**方案 B**：直接改 `LoadFirstLevel` 的 switch 跳转表，让它落到出口③（0x8053EC 那块）。
需要先定位那张跳转表（IL2CPP 编译 `switch` 的形式），比方案 C 麻烦。

**方案 A**：改存档服务的返回值（`get_ClearSaveGame` / 那个字符串）——
⚠ **不可行**：非空/true 走的是出口①（StartFtue 教程路），不是出口③。

### 21.8 本轮新增/修改（都在 `Library/Locus/tmp/blades/`）

| 文件 | 改动 |
|---|---|
| `build_vtrace.py` | 探针集换成 **TIE / TSF / TLF / TLV / TGD / TRU / TLD**（7 个，仍在 0xA7BADC..0xA7BCEC，不碰洞 F） |
| `build_transition_table2.py` | **新增**：迁移表 v2（认尾调用 `b Permit`；用「偏移表归属」消歧触发器/目标）→ `transition_table2.txt` |
| `fire_which.py`（用法） | 全库 137 处 `FlowTrigger::Fire` 调用点 → 触发器名 + 调用者（**含尾调用**）的完整对照表 |

### 21.9 本轮顺便钉死的其它事实

* **FlowTrigger 静态偏移全表**（`public static FlowTrigger` 字段）：0x94=Proceed、0x98=ResetGame、
  **0x9C=LoadingComplete**、0xA0=LoadingFailed、0xA4=Authenticate、0xA8=AuthenticateNoUi、0xAC=FetchSaveData、
  0xC4=LoadTown、0xC0=StartGame、0xE8=LoadMainMenu、0xEC=CharacterSelection、**0x104=UpdateBundles**、
  0x10C=BundlesInitialized、0x110=LoadBaseGameContent …
* `OnSaveDataFetched` 的两个 bool 在 **b__222_229 的调用点上是硬编码 0**（0x10EA840 `mov r0,#0`、0x10EA848 `mov r1,#0`）；
  实测 TSF=(1,1) ⇒ **真正带 (1,1) 的调用来自 `SaveManager::Load`(0xFFF2A0) 的回调路径**（0x10EA808）。
* 设备现状：`PLAYER_PREF_FTUE_STATUS=7`、`acceptedLegalDocumentIds=1`；
  **本地存档目录 `files/{SaveGame,LocalData,Info}/` 全空** ⇒ 客户端认为「有存档」但本地并没有存档文件。
* 后端契约这层已通（本轮 13:18:46→13:18:49 的请求序列）：
  `POST /auth/gplus ×1` → `GET /sync ×1` → `GET /characters ×1` → `GET /characters/1` + 10 个状态端点各 ×1
  ⇒ **认证风暴彻底解决**；剩下的全是客户端流程问题。
* 一次 `Curl error 52: Empty reply from server`（13:18:50，线程 2425），该请求**没出现在 requests.log**
  ⇒ 疑似打到了 hosts 没覆盖的域名（`s3.amazonaws.com` / `www.bethesda.net` 未在设备 hosts 里）。

### 21.10 下一轮的三步

1. **上方案 C**（3 个立即数补丁 + `BLADES_FIX_R1=0`），跑一轮看：
   `TIE` 有没有出现 `Loadin`→`Load`（`Loading` 状态）、`TRU`/`TLD` 有没有命中、`TLV` 有没有命中。
2. 若 `TRU` 命中但 `TLV` 仍 0 ⇒ 说明 `LoadFirstLevel` 在任务列表里走的还是出口②
   ⇒ 改走**方案 B**（改 switch 跳转表）。
3. 若 `TLV` 命中 ⇒ 城镇关卡开始加载 ⇒ 接下来盯 `TGD`（GameplayLoadingDone → Fire(LoadTown)）与
   `Gameplay` 状态（动画事件点火）⇒ 才能走到建角界面（路径 A）。

### 21.11 ★★★★ 第一百零七轮：**方案 C（修法 R10）实测生效**——加载屏任务列表第一次真的跑起来了

**做法**：在 `patch_c_fire.py` 里新增 **修法 R10**（3 处立即数，默认开，`BLADES_FIX_R10=0` 可关）：

| 补丁 | 位置 | 原值 → 新值 | 作用 |
|---|---|---|---|
| C1 | 0x00A73688 | `ldr r4,[r0,#0x98]` → `#0xC4` | `OnSaveDataFetched` 尾巴 Fire 的触发器：ResetGame → **LoadTown** |
| C2 | 0x010EA3BC | `ldr r5,[r0,#0x164]` → `#0x134` | `FetchingSaveData` 第 5 条 permit 的目标状态：ForceReset → **`Loading`** |
| C3 | 0x010EA3C0 | `ldr r6,[r1,#0x98]` → `#0xC4` | 同一条 permit 的触发器：ResetGame → **LoadTown** |
| 构建 | — | **`BLADES_FIX_R1=0`** | R1 必须关（否则尾巴永远走不到 Fire） |

**构建命令**（当前最佳配置）：
```bash
export BLADES_NO_QUEUE_GUARD=1 BLADES_FIX_R1=0 BLADES_FIX_R2=1 BLADES_FIX_R3=0 BLADES_FIX_R4=0 \
       BLADES_FIX_R6=1 BLADES_FIX_R8=1 BLADES_FIX_R10=1 BLADES_HOLE_G=0 BLADES_HOLE_H=1 \
       BLADES_HOLE_I=1 BLADES_NO_HOLE_F=1
python build_vtrace.py && python patch_c_fire.py
CHAR_MODE=char python make_withchar.py
RESTART=0 PUSH_SO=1 INSTALL=0 WAIT=120 ROUTES=shapes/R_withchar.json bash run_full.sh
```

**实测对比（同一套探针）**：

| 探针 | R10 关 | **R10 开** | 含义 |
|---|---|---|---|
| TRU `TaskListRunner::Run` | 0 | **20** | ★★★ **加载屏任务列表第一次真的启动了** |
| TLD `OnLoadingTaskListComplete` | 0 | 0 | 列表**没有跑完**（某个 task 卡住/失败） |
| TLV `FlowState::LoadLevel` | 0 | 0 | 城镇关卡仍未加载 |
| TLF `FlowState::LoadFirstLevel` | 10 | 10 | 决策点照旧每轮都跑 |
| TSF `OnSaveDataFetched` | 20 | 10 | 循环轮数减半（不再「重载内容」那么频繁） |

**状态轨迹（R10 开）**：
```
LoadSt → Initia → Loadin → Authen → (TLF) → Authen → Fetchi → (TSF=1,1)
      → Loadin(=Loading) → (TRU ★任务列表启动) → … 仍会回到 Authen/Fetchi
```

⚠ **命名陷阱**：`Loading` 与 `LoadingBaseGameContent` 的**前 6 个字符都是 "Loadin"**
⇒ TIE 探针（只打 6 字符）分不清这两个状态；要看清楚得换探针（打更多字符或直接探 `Loading` 的动作）。

**新出现的异常**：R10 开后有一次 `NullReferenceException`（13:57:10，Unity 线程 3704）。

**⇒ 现在的卡点又前移了一步**：
1. 任务列表启动了但**没跑完**（TLD=0）⇒ 需要探 `Task::SignalCompletion` / `TaskListRunner::Update`
   与 `PrepareLoadingScreenTaskList` 里那 **5 个 task** 各自的状态（哪个没完成）。
2. `LoadFirstLevel` 作为 task 跑起来后仍走出口②（Fire Authenticate）⇒
   它的 switch 跳转表要改（**方案 B**），让它落到出口③（`EnterTownPostFtue` + `LoadLevel(2,0)`）。
3. 注意：`LoadFirstLevel` 现在**有两个来源**（`SetupGameManagers` 的表 + `Loading` 的表），
   探针分不清是哪一个 ⇒ 下一轮建议按调用者再分两个 tag。

### 21.12 ★★★★★ 第一百零八轮（2026-11-16）：★★ **关卡其实已经加载了**——卡点变成「角色持久化数据缺失」

**入口**：R10 跑完那轮 logcat 里有一条 Unity NRE，堆栈是决定性的：

```
NullReferenceException: Object reference not set to an instance of an object.
  at BGS.Game.Uids.Uid.RetrievePersistent (PersistentObject persistentNode)
  at BGS.Game.Character.CharacterSetting.RetrievePersistent ()
  at PlayerActor.RetrievePersistent ()
  at Actor.Init (ActorConfig config, Action onActorReady)
  at BGS.Game.LevelGeneration.ActorInitializer.SetupInstance (GameObject instance, ...)
  at BGS.Game.Level.SpawnedInstance.Spawn (GameObject spawnedInstancesRoot, LevelChunk levelChunk)
```

⇒ ★★★ **城镇关卡已经加载、并且进入「生成角色（Actor）」阶段**（`LevelChunk` / `SpawnedInstance.Spawn`）。
   ⇒ **§18.5 的「关卡场景没加载」判断被推翻**：关卡加载没问题，R10 之后流程真的走到关卡里去了。

#### 21.12.1 NRE 的确切位置与含义（读码钉死）

`BGS.Game.Character.CharacterSetting.RetrievePersistent` = **0xDBD618**：

```
00dbd660  ldr r5, [r8, #0x104]   ; r5 = this->_persistentNode
00dbd668  cmp r5, #0
00dbd66c  bne 0xdbd678
00dbd670  mov r0, #0
00dbd674  bl  0x7c0060          ; ★★ 抛 NRE：_persistentNode == null
```

`CharacterSetting` 的字段表（dump.cs）末尾：

```
0x104  private PersistentObject _persistentNode;
```

⇒ **NRE 的根因 = 玩家的 CharacterSetting 从来没拿到自己的持久化节点。**

`IPersistent`（TypeDefIndex 9462）的定义：
```
public abstract PersistentObject PersistentNode { get; set; }   // Slot 0/1（接口派发）
```
⇒ `set_PersistentNode` **没有直接 `bl` 调用点**是正常的（接口虚调用），**不是死代码**。

#### 21.12.2 为什么我们的数据里没有这个节点

`character.conversion` / `GetCharacter.conversion` 里：

```
character/extraData → UrlDecode → DeserializePersistentJson(schema=CharacterData)
   → Read SelectedUid / ShapeWeight / Scale
   → Write SaveGame: Player/Character/{SelectedUid,ShapeWeight,Scale}
```

* `CharacterData.schema` 只有 3 个字段：`SelectedUid(String)` / `ShapeWeight(Single)` / `Scale(Single)`。
* **但 schema 只描述「客户端会读哪些字段」，不代表 extraData 里只有这些**：
  `DeserializePersistentJson` 是把 extraData **整个反序列化成持久化树**，
  角色的 **CharacterSetting 节点（CharacterUID / CompoundUID / NPCActorType / Name / TagId /
  ScaleFactor / Morphs / Weapon / Shield / Armors / 各种 PresetUid / Tint…）**
  就该在这棵树里。
* 我们现在给的 extraData 只有 `{"SelectedUid":"1","ShapeWeight":1,"Scale":1}`
  （`make_withchar.py` 第 53~60 行构造），`CharacterSave` 也是照抄 SaveGame
  ⇒ **两处都没有 CharacterSetting 节点** ⇒ 客户端建不出角色的 CharacterSetting 持久化节点
  ⇒ 关卡生成玩家 Actor 时 `RetrievePersistent()` 直接 NRE。

#### 21.12.3 下一轮怎么做（三条，按性价比）

1. **先定位「CharacterSetting 节点在持久化树里的路径/键名」**：
   * 用 `iface_dispatch_v7a.py` 找 `IPersistent.set_PersistentNode`（Slot 1）的**接口派发调用点**
     ⇒ 谁在物化节点时调用它 ⇒ 那条路径就是节点在树里的位置；
   * 或在 `string_literals.txt` 里找 `"CharacterSetting"` / `"Character/"` 之类的持久化路径字面量
     （本轮 grep 没搜到，说明路径名可能不在字面量表里，要走上面的接口派发路线）。
2. **同时探一下「角色是从哪来的」**：给 `CharacterSetting.RetrievePersistent`(0xDBD618) 加探针，
   再给「创建玩家 CharacterSetting」的地方加探针 ⇒ 确认它读的是 extraData 还是 CharacterSave。
3. **兜底路线：让游戏自己写一份存档当样本**：
   设备上的 `files/{SaveGame,LocalData,Info}/` **到现在还是空的**（客户端没走到存档点），
   但只要流程再往前一步（角色生成成功 → 进城镇 → 触发存档），
   客户端就会把**它自己期望的持久化树**写进本地文件 ⇒ 直接读那个文件，就能得到
   CharacterSetting 节点的准确形状，再回填给后端（比纯猜键名快得多）。

> 附：R10 之后新增的 NRE 目前**只出现 1 次**（13:57:10，Unity 线程 3704），不是刷屏 ⇒
> 是「生成玩家 Actor 时的一次性失败」，不是每帧异常。

### 21.13 ★★★ 第一百零九轮（2026-11-16）：空角色列表实验 + ★ **「给 CharacterSetting 挂持久化节点」的完整配方**

#### 21.13.1 空角色列表实验（`CHAR_MODE=nolist`）

给 `make_withchar.py` 加了 `nolist` 模式（`GET /characters` → **空数组** + 空 `BackendCharacterListData`），
探针换成建角信号（TIE/TSF/TSC/TQR/TLD/TRU/TLV），配置沿用 R10。

**实测**：

| 观测 | 结果 | 结论 |
|---|---|---|
| `TSF`（OnSaveDataFetched 参数） | **(0,1)**（有角色时是 (1,1)） | ★ 客户端**正确知道「没有存档/角色」** ⇒ 后端响应形状是对的 |
| `TSC` / `TQR` | **0 / 0** | ★ **「没有角色 ⇒ 去建角」这条自然分支没有触发**（§15.3 说的 `GHomeSendManager.EscapeGame` 路径这轮没走到） |
| `TRU` | 24 | R10 仍生效（加载任务列表照常启动） |
| 后端请求 | auth ×1、/sync ×1、GET /characters ×1，**没有 POST /characters** | 客户端没建角 |
| 新 NRE | `BGS.Shared.Bundles.DependencyHandle.<OnLoaded>b__23_0`（bundle 依赖为空） | 没有角色 ⇒ 关卡里没有玩家资产 ⇒ 依赖缺失 |

⇒ **结论**：空角色列表**不能**把流程推进到建角界面；建角分支需要 UI/Gameplay 层先起来，
   而内容加载仍以 NRE 收场 ⇒ **路径 A（客户端自己建角）还得先解决「内容加载不完成」**。

#### 21.13.2 ★★★ 关键配方（读码得到）：**怎么给 CharacterSetting 挂上持久化节点**

`CharacterServiceImplementation.CreateRandomPlayerCharacter()`（**0x1610C30**）是游戏自己造角色的代码，
反汇编后就是完整答案：

```
0x1610C68  bl  GetDefaultCharacterSetting()          ; 取默认 CharacterSetting
0x1610C88  bl  PersistentObject::.ctor                 ; new 一个持久化节点
0x1610CB8  bl  PersistentObject::GetOrCreateMember<object>(key)   ; ★ 取/建「成员」（带一个键）
0x1610CD0  str r7, [r6, #0x104]!                      ; ★★ 把它写进 CharacterSetting._persistentNode（偏移 0x104 ✓）
0x1610D0C  bl  CharacterSetting::UpdatePersistent      ; 把设置写回节点
0x1610D58  bl  BackendCharacterData::.ctor             ; 造 BackendCharacterData
```

要点：
1. **`GetOrCreateMember` 永远不会返回 null** ⇒ 正常情况下 `_persistentNode` 一定有值
   ⇒ **我们这次 NRE 的真正含义是：玩家角色的 CharacterSetting 从来没走过这条「挂节点」流程**，
   而不是「键写错了」。
2. 那个 `key`（成员名）是**元数据里的字符串字面量**，静态读不出来
   （池字链落到 .bss 的 MethodInfo 槽，`il2str()` 解不开）⇒ **必须运行时取**。
3. ⇒ **下一轮探针**：钩 `PersistentObject::GetOrCreateMember`（**0x1054FA8**），
   用 `'str2r'` 模式打印 r1（Il2CppString*）⇒ 直接得到「CharacterSetting 节点在树里的成员名」。
4. 顺带得到两个后端语义（之前只猜过）：
   * `CharacterServiceImplementation.UpdateCharacterData(string dataKey, PersistentObject persistentObject)`（0x1610DA0）
     ⇒ 客户端**按 dataKey 上报持久化数据**（这就是 §17.3 里「数据键 2」的同一套键）。
   * `CharacterServiceImplementation.CreateRandomPlayerCharacter()`（0x1610C30）
     ⇒ **游戏本地就能造出一个完整角色**（含 CharacterSave）⇒ 这是「一份真实 CharacterSave 长什么样」的权威模板。

#### 21.13.3 下一轮的三件事（按性价比）

1. **探 `GetOrCreateMember`(0x1054FA8) 拿键名**（'str2r' 模式，探针预算够），
   再探 `CharacterServiceImplementation.OnGetCharacter` ⇒ 确认玩家的 CharacterSetting 是不是
   **应该**由「角色服务回调」来挂节点。
2. **把 `CreateRandomPlayerCharacter()` 当模板**：如果能在客户端侧调用它（或读它的产物），
   就能得到一份**游戏自己认可的 CharacterSave**，直接回填后端 ⇒ 解决 §21.12 的数据缺口。
3. `GET /characters/{id}` 的响应要能产出 **数据键 2**（§17.3 的老墙）：
   候选形状已知 = `GetCharacter.schema` + `GetCharacter.conversion`
   （`extraData` → UrlDecode → DeserializePersistentJson → 写 SaveGame 的 `Player/Character/*`）。

### 21.14 ★★★★ 第一百一十轮（2026-11-16）：★★ **运行时拿到了持久化树的「成员名词表」**（数据缺口的形状基本对上了）

**做法**：钩 `PersistentObject::GetOrCreateMember<T>`（**0x1054FA8**），用 `build_vtrace.py` 的
**`'str3'` 模式**（读 r1 的 Il2CppString，打印前 6 个 UTF-16 字符）——
因为 `GetOrCreateMember<T>(string name)` 的 r1 就是成员名，而它是元数据字面量、静态读不出来。

**实测（一次跑测 7321 次调用，去重后 477 个不同键名，按频次）**：

| 次数 | 键名前 6 字符 | 推测全名 |
|---|---|---|
| 856 | `Static` | **StaticData / Static…** |
| 856 | `Dynami` | **DynamicData / Dynamic…**（与 Static 成对出现，次数完全相同 ⇒ 一对） |
| 440 | `LootOw` | LootOwner… |
| 198 | `Buildi` | Building… |
| 176 | `LotsCl` | LotsCl…（资源批次？） |
| 45 | `Player` | Player… |
| 42 | `Conden` | Condensed… |
| 40 | `Constr` | Constructor/Const… |
| 33 | `Items` | Items ✓（与 SaveGame.schema 对得上） |
| 32 | `ChestS` | **ChestSlots ✓**（SaveGame.schema 里就有） |
| 30 | `Invent` | **Inventory ✓** |
| 24 | `Abilit` | **Abilities ✓** |
| 各 18 | `73ab74…` `6d4c98…` `574e5c…` | **UID/实体键**（`73ab7435-…` 正是 FTUE 任务 id ✓） |

#### 21.14.1 ★★ 这解释了「数据缺口」到底缺什么

**我们现在的 `CharacterSave` 是照抄 `SaveGame.schema` 的转换形状**
（`ChestManager/Settings/Quests/TownStats/LevelManager/Town/Player/…`），
**而客户端真正读的是「原生持久化树」**——它的顶层成员是
**`Static…` / `Dynamic…` 这一对**，下面挂着 `Player / ChestSlots / Inventory / Abilities / Items …`
以及大量 **UID 键**（实体/任务/建筑……）。

⇒ **⇒ 我们的 CharacterSave 整棵树的结构就是错的**（不只是「少了一个 CharacterSetting 节点」）。
⇒ 这也解释了 §21.12 的 NRE：客户端在树里找不到「角色的那个成员」
   ⇒ `_persistentNode` 没挂上 ⇒ `Uid.RetrievePersistent(null)` 抛 NRE。

#### 21.14.2 下一轮（按性价比）

1. **抓一份真实的 CharacterSave 当模板**（最省事）：
   * 探 `CharacterServiceImplementation.UpdateCharacterData(string dataKey, PersistentObject)`（**0x1610DA0**）
     —— 客户端把持久化数据序列化后按 dataKey 上报 ⇒ 打出来的就是**原生树 JSON**；
   * 或探 `CreateRandomPlayerCharacter`（**0x1610C30**）的产物（`BackendCharacterData.CharacterSave`）；
   * 或探 `JsonPersistentSerializer` 的序列化出口 —— 任何一次序列化都能拿到原生树。
2. **拿到原生树后**：把 `make_withchar.py` 的 `CharacterSave` 改成原生形状
   （`Static…`/`Dynamic…` + `Player` + UID 键），再跑一轮 ⇒ 预期 `_persistentNode` 挂上 ⇒ 玩家 Actor 生成成功 ⇒ 进城镇。
3. 顺带：`GetOrCreateMember` 的**嵌套关系**还没拿到（现在只知道成员名，不知道父子）。
   要还原结构，可以在探针里同时打「返回的成员节点指针」与「父节点指针」，运行时拼树
   （`GetOrCreateMember` 是嵌套调用，调用顺序≈建树顺序）。

### 21.15 ★★★★★ 第一百一十一～一百一十四轮（2026-11-16 夜）：★★ **原生持久化树的形状找到了 —— CharacterSetting 的 NRE 消失**

**突破口**：字面量表里相邻的两条 **`"Static"` / `"Dynamic"`**（第 5345/5347 行）
＋ 运行时探针打出的高频成员名 `Static` / `Dynami`（各 856 次，成对出现）
⇒ **原生持久化树的顶层两个成员就叫 "Static" 与 "Dynamic"**
（另有 `ClutterStatic` / `ClutterDynamic` 是关卡杂物那对）。

**后端改动**（`make_withchar.py`，新增 `SAVE_WRAP=1` 开关）：
把**整份存档**（`cs`）包成 `{"Static": <存档>, "Dynamic": <存档>}`，
⇒ 同时影响 `/sync` 响应体、`/characters/{id}` 响应体、`CharacterSave` 字符串。
另加 `_intify()`：把整数值 float 一律转 int（客户端是直接 cast 成 Int64）。

**逐轮实测**：

| 轮次 | 后端形状 | 结果 |
|---|---|---|
| 110 | 原始（SaveGame 转换形状） | ❌ `NullReferenceException` @ `CharacterSetting.RetrievePersistent`（`_persistentNode` 为 null） |
| 111 | 只把 `CharacterSave` 包成 Static/Dynamic | ❌ NRE 依旧 |
| 112 | **整份存档都包** | ✅ **NRE 消失**（只余 `InvalidCastException: Double→Int64`） |
| 113 | 同上 + `_intify`（cs 与 /sync、/characters 响应体） | NRE 仍无 ✅；Double→Int64 仍在（**说明还有别处是 float**） |

**⇒ 两条硬结论**：
1. **客户端读的是原生持久化树（顶层 Static/Dynamic），不是 SaveGame 的转换形状**；
   `/sync` 与 `/characters/{id}` 的响应体里的那份**才是**它读的树（只包 CharacterSave 无效 ⇒ 已实证）。
2. `CharacterSetting._persistentNode` 为 null 的根因就是这个形状不对；形状对了 ⇒ 节点挂得上 ⇒ NRE 消失。

**下一步（一步就能做完）**：
1. **把剩下的 float 找出来**：`make_withchar.py` 末尾加一个「对 `routes` 里所有响应体统一 `_intify`」的循环
   （现在只整了 `/sync`、`/characters`、`/characters/{id}` 三处，
   **城镇/背包/宝箱等端点的响应体仍来自原始 routes 文件、里面很可能带 1.0 这种 float**）⇒ 这是 `Double→Int64` 的最可能来源。
2. 之后重跑，预期 `TLV`（`FlowState::LoadLevel`）/`TLD`（`OnLoadingTaskListComplete`）开始命中
   ⇒ 城镇关卡真正加载 ⇒ 玩家 Actor 生成成功 ⇒ 进城镇 ⇒ 再谈建角（路径 A）。
3. 若 `Static`/`Dynamic` 两份内容需要分开（哪半放玩家状态、哪半放静态配置），
   再按客户端读到的成员名分流（`TGM` 探针能打出成员名，配合调用顺序可还原嵌套）。

**本轮新增/修改**：
| 文件 | 改动 |
|---|---|
| `run_full.sh` | **不再创建/启动计划任务**（用户要求）：后端与文件服务器改为本次调用内 `nohup` 后台起；旧的 `blades_srv`/`blades_filesrv` 一次性任务已删除 |
| `make_withchar.py` | 新增 `SAVE_WRAP=1`（Static/Dynamic 包裹）、`nolist` 模式（空角色列表）、`_intify()`（数值整数化） |
| `build_vtrace.py` | 探针集现为 TIE / TGM（`str3` 打印 `GetOrCreateMember` 的成员名）/ TSC / TQR / TLD / TLV |

### 21.16 ⚠ 第一百一十五轮（2026-11-16 夜）：整份 routes 整数化后 NRE 回来了 ⇒ 两个改动互相影响，需做干净 A/B

* 112 轮（**只**包 Static/Dynamic，未动数值）：**NRE 消失**，只剩 `InvalidCastException: Double→Int64`。
* 113 轮（+ `_intify` 只作用于 cs / `/sync` / `/characters`）：NRE 仍无，Double 仍在。
* 115 轮（+ **所有** routes 响应体统一 `_intify`，含城镇/背包/宝箱等端点）：
  **Double→Int64 消失，但 NRE 又出现了**（12:08:50）⇒ **两处改动互相影响**（很可能是整数化改动了城镇/关卡数据，
  使客户端找到了另一个节点、却缺 CharacterSetting 需要的字段）。

⇒ **下一轮必须做一次干净 A/B**（别再叠加改动）：
1. 只开 `SAVE_WRAP=1`（不动数值）→ 记录 NRE/Double 现状；
2. 再单独找出 Double 的确切来源：给 `check_types.py` 加一条「扫描所有 routes 响应体里仍是 float 的字段」，
   逐字段列出来（**只**整这一处需要的，别全局整）；
3. A/B 结果确定后再看 `TLV`/`TLD` 是否命中 ⇒ 城镇关卡加载 ⇒ 玩家 Actor 生成 ⇒ 进城镇。

### 21.17 ★★★★ 第一百一十六～一百一十八轮（2026-11-16 夜～17 日晨）：★ 类型层彻底干净 + ★★ 拿到客户端「期望的树成员名表」

#### 21.17.1 类型层：改成**按 .schema 驱动**，异常清零

`check_types.py` 的输出里有一批「**schema 声明 {System.String}、我们却发了数字**」的字段
（`character/level`、`character/experience`、`town/townLevel/*`、`chests/*/id`、
`quests/*/version|seed|status|progress`、`BackendId`、`TagId` …）。
生成器原本有个「数字样字符串统一转成数字」的兜底（`_coerce`）⇒ 把 `"0.0"` 变成了 `0.0`
⇒ 客户端 DirectCast 路径抛 `InvalidCastException: Double→Int64`（栈：`PrimitiveParserIntToFloat.Parse`）。

**修法**：把这些 schema 声明为 String 的键加进 `_SKIP_KEYS`（保持字符串），
并把 `character/level`/`character/experience` 显式写成 int（客户端按 Int64 解析），
`extraData` 的 `ShapeWeight` 写 int、`Scale` 保持 float（试过也整数化 ⇒ CharacterSetting 建不出来）。

⇒ **结果**：`/sync` 等响应体里**浮点 0 处、转型异常 0 条**（新增 `scan_floats.py` 可随时复查）。

#### 21.17.2 ★★★ 客户端「期望的持久化树成员名」拿到了

用 `TGM`（`GetOrCreateMember` 的成员名，`'str3'` 模式）+ `TPM`（`PlayerActor::RetrievePersistent` 标记）
关联，拿到客户端在玩家持久化时**自己创建**的成员名序列：

```
Stats → Level → Experience → LearnedAbilities → Abilities → Wallet → Currency
     → **NPCActorType** → **CompoundUID** → LocalName… → Request… → Condensed…×7
     → ChestManager → ChestC… → LocalD… → Building… → Recipe… → NewRec… → NewTem…
     → Building… → Inventory → Inventory → EmoteN… → QuestN… → NewQue…
```

⇒ ★ **`NPCActorType` 与 `CompoundUID` 正是 `CharacterSetting` 自己的键**
（dump.cs：`KEY_NPC_ACTOR_TYPE_UID="NPCActorType"`、`KEY_COMPOUND_UID="CompoundUID"`）
⇒ **客户端会自己在树里创建 CharacterSetting 的成员**（含 `CharacterUID`/`Name`/`TagId`/`Morphs`/`Weapon`…）
⇒ ⇒ 我们要做的不是「提供 CharacterSetting 节点」，而是**把整棵树的骨架搭对**
（顶层 `Static`/`Dynamic` + 客户端期望的那批成员名）。

#### 21.17.3 剩下的唯一堵点（已定位到指令）

`CharacterSetting.RetrievePersistent`(0xDBD618) 第 4 条指令就抛 NRE：

```
00dbd660  ldr r5,[r8,#0x104]   ; r5 = this->_persistentNode
00dbd66c  bne 0xdbd678
00dbd674  bl  0x7c0060         ; ★ _persistentNode == null ⇒ NRE
```

而**挂节点的人**是 `PlayerActor::RetrievePersistent`(0xAC6D14) 里对 `GetOrCreateMember` 的调用
（该方法里有 4 处 `str [rX,#0x104]`）⇒ **要查的是：它把哪个成员节点挂给了 CharacterSetting**
（静态扫描已定位：`GetOrCreateMember` 调用点 437 个，其中 69 个后面跟着写 +0x104；
`CharacterSetting::RetrievePersistent` 自己的两处调用在 0xDBD694/0xDBD6F0，**在判空之后**）。

#### 21.17.4 下一轮（按性价比）

1. **把 21.17.2 的成员名表变成「我们要发的键」**：用 TGM 抓到的名字（477 个去重）
   在我们的 `Static`/`Dynamic` 里补齐同名成员（空对象即可）⇒ 让客户端的 `GetOrCreateMember`
   全部命中现成节点，而不是临时造。
   ⚠ 探针只打 6 个字符 ⇒ 建议给 `build_vtrace.py` 加一个 `regstr6`（12 字符）模式。
2. **定位「挂给 CharacterSetting 的那个节点名」**：用 `TPM` 标记 + TGM 名字序列，
   找紧挨在 `NPCActorType` **之前**的那个成员名（= CharacterSetting 节点自身的名字）。
3. 顺手可试：`CHAR_MODE=nolist`（空角色列表）现在**异常全 0**，只是流程停在取存档循环
   ⇒ 若 1/2 把树搭对，空列表路径可能直接进「没有角色 ⇒ 建角」分支（**路径 A 的正解**）。

### 21.18 ★★★★★ 第一百一十九～一百二十二轮（2026-11-16 夜～17 日）：★★ 「玩家 Actor 永远生成不出来」的根因链彻底读通

#### 21.18.1 ★★★ 决定性代码（`PlayerActor::RetrievePersistent`，0xAC6D14）

```
0xAC731C  接口调用（r0 = 某个状态值）
0xAC733C  cmp  r0, #7
0xAC7340  blt  0xAC7434          ; ★★ 返回值 < 7 ⇒ **整段 Character 分支被跳过**
0xAC7344  ldr  r5, [sb, #0x10C]   ; r5 = 玩家自己的持久化节点（PersistentObject）
0xAC736C  bl   PersistentObject::HasMember      (0x11332F0)  ← ★ 键名 = **"Character"**
0xAC7374  bne  0xAC7434          ; ★ 没有 "Character" 成员 ⇒ 也跳过
0xAC7390  bl   CharacterSetting::.ctor
0xAC73E0  bl   PersistentObject::GetMember<object>
0xAC73F8  str  r7, [r6, #0x104]! ; ★★ 挂成 CharacterSetting._persistentNode
0xAC7424  bl   CharacterSetting::RetrievePersistent  (0xDBD618)
           └─ 0xDBD660 ldr r5,[r8,#0x104]; cmp r5,#0; bl 0x7c0060  ← 节点为 null 就抛 NRE
```

⇒ **⇒ 玩家 Actor 生成失败 = 上面三道门任意一道没过**：
① 状态值 < 7；② 那个持久化节点里没有 `"Character"` 成员；③ 成员有了但节点没挂上。

#### 21.18.2 ★★ 客户端自己期望的树成员（12 字符全名，str6 探针实测）

```
Stats → Level → Experience → LearnedAbilities → Abilities → Wallet → Currencies
     → [HasMember("Character")] → NPCActorType → CompoundUID → LocalNotifications
     → RequestedNotifications → CondensedCra/Con ×3 → ChestManager → ChestCycles
     → LocalData → BuildingNewFields → RecipeNewMarket → NewRecipes → NewTemperLevels
     → BuildingLevels → InventoryNew…
```
⇒ **NPCActorType / CompoundUID 正是 `CharacterSetting` 自己的键**
⇒ **⇒⇒ 我们存档里 `Player/Character` 只有 SelectedUid/ShapeWeight/Scale ⇒ 缺 CharacterSetting 的字段。**

#### 21.18.3 已做的后端修正（`make_withchar.py`）

1. `SAVE_WRAP=1`：整份存档包成 `{"Static":…, "Dynamic":…}`（原生持久化树的顶层，见 §21.14）。
2. `Player/Character` 补上 CharacterSetting 字段：`CharacterUID / CompoundUID / NPCActorType / Name / TagId`（字符串型，已加进 `_SKIP_KEYS` 防止被自动转数字）。
3. **类型层改成按 .schema 驱动**（`_SKIP_KEYS` 扩充 + `scan_floats.py` 复查）⇒ **浮点 0 处、转型异常 0 条**。
4. `INTIFY=0|key|all` 开关；`CHAR_MODE=nolist`（空角色列表 ⇒ 异常全 0，但流程停在取存档循环）。

#### 21.18.4 ★ 下一轮：**一行补丁**就能绕过整棵树的形状问题

把 0xAC736C 的调用从 **`HasMember`(0x11332F0)** 换成 **`GetOrCreateMember<object>`(0x1054FA8)**：

```
0xAC736C  bl 0x11332F0   ⇒   bl 0x1054FA8
```

（ARM32 `BL` = `0xEB000000 | (imm24)`，imm24 = (target-(pc+8))>>2 ⇒ 改 4 个字节即可。）

⇒ 客户端就**总会拿到一个（哪怕空的）"Character" 节点** ⇒ `CharacterSetting._persistentNode` 非 null
⇒ `RetrievePersistent()` 不再 NRE ⇒ **玩家 Actor 能生成** ⇒ 关卡加载能走完 ⇒ 进城镇。
⇒ 配合 `BLADES_FTUE_SET=7`（把 R6 的目标值改成 7，让 0xAC733C 的 `cmp r0,#7` 闸门成立）。

**⇒ 建议下一轮的完整配置**：
```bash
export BLADES_NO_QUEUE_GUARD=1 BLADES_FIX_R1=0 BLADES_FIX_R2=1 BLADES_FIX_R3=0 BLADES_FIX_R4=0 \
       BLADES_FIX_R6=1 BLADES_FTUE_SET=7 BLADES_FIX_R8=1 BLADES_FIX_R10=1 BLADES_HOLE_G=0 \
       BLADES_HOLE_H=1 BLADES_HOLE_I=1 BLADES_NO_HOLE_F=1
python build_vtrace.py && python patch_c_fire.py
SAVE_WRAP=1 INTIFY=0 CHAR_MODE=char python make_withchar.py shapes/R_perchar.json shapes/R_withchar.json
RESTART=0 PUSH_SO=1 INSTALL=0 WAIT=95 ROUTES=shapes/R_withchar.json bash run_full.sh
```
（探针：TKY=HasMember 键名 / TPM=PlayerActor::RetrievePersistent 标记 / TGM=GetOrCreateMember 成员名 / TLV=LoadLevel / TLD=OnLoadingTaskListComplete）

#### 21.18.5 本轮新增工具

| 文件 | 作用 |
|---|---|
| `build_vtrace.py` 新模式 **`str6`** | 从**寄存器**里的 Il2CppString 打 **12 个字符**（6 个字）⇒ 成员名不再被 6 字符截断 |
| `build_vtrace.py` 新模式 **`regstr6`** | 从**字段**（[rX+off]）里的 Il2CppString 打 12 字符 |
| `scan_floats.py` | 扫描路由响应体里仍是 float 的字段（含内嵌 JSON 字符串）⇒ 一行命令复查类型层 |
| `make_withchar.py` | `SAVE_WRAP` / `CHAR_MODE=nolist` / `INTIFY` 三个开关 + `_SKIP_KEYS`（按 schema 保类型） |
| `patch_c_fire.py` | `BLADES_FTUE_SET`（R6 的目标 FTUE 状态可配：1=Starting / 7=CharacterCreationDone） |

### 21.19 ★★★★★★ 第一百二十三轮（2026-11-16 17:00）：★★ **修法 R11 生效——托管异常清零**，但暴露出一个原生崩溃

**修法 R11**（`patch_c_fire.py`，`BLADES_FIX_R11=0` 可关，**默认开**）：
把 `PlayerActor::RetrievePersistent` 里查成员的那句
`bl PersistentObject::HasMember`(0x11332F0) 改成 **`bl PersistentObject::GetOrCreateMember<object>`(0x1054FA8)**：

```
0xAC736C  EB19AFDF (bl 0x11332F0)  ⇒  EB16370D (bl 0x1054FA8)
```

⇒ 客户端**永远拿得到一个（哪怕空的）"Character" 节点** ⇒ `CharacterSetting._persistentNode` 不再为 null
⇒ **`CharacterSetting::RetrievePersistent()` 的 NRE 彻底消失**。

**实测（同时开 `BLADES_FTUE_SET=7`）**：

| 指标 | 之前 | **R11 之后** |
|---|---|---|
| 托管异常（`E Unity : …Exception`） | 1 条 NRE（每轮） | **0 条** ✓ |
| `TKY`（HasMember 调用数） | 13753 | **21848**（⇒ 持久化操作明显走得更深了） |
| `TLV` / `TLD` | 0 / 0 | 0 / 0（流程仍在 FetchingSaveData↔Loading 循环） |
| 进程 | 存活 | **原生崩溃**：`E CRASH`，pc **0x1055068**（frame#0）、lr 0x2316CB0（frame#1） |

⇒ **⇒⇒ 新的堵点变成原生崩溃**，位置在 `PersistentObject` 的成员函数区
（`GetOrCreateMember` 0x1054FA8 / `GetMember` 0x1054EC0 之后不远处 ⇒ 0x1055068 就在这一片）。

**下一轮（按顺序）**：
1. **反汇编 0x1055068 附近**，确认它在 `GetOrCreateMember`/`GetMember` 的哪一段、崩在哪条指令
   （`python disasm.py 0x1055068`；同时看 logcat 里 CRASH 段的 `signal` 行 ⇒ 是 SIGSEGV 还是 SIGABRT）。
2. 若 0x1055068 落在 `GetOrCreateMember` 内部 ⇒ 说明**该函数不能在这个调用点用**
   （调用约定/隐含 this 不同？）⇒ 换成更稳的写法：
   * 方案①：不动 R11，改为**把 "Character" 成员真的塞进树**（我们已塞进 `Player/Character`，
     但客户端查的那个树不是我们的 `Player` 成员 ⇒ 需要再定位 `this->[+0x10C]` 的来源）；
   * 方案②：在 R11 的位置**先调 GetOrCreateMember、再把返回值丢掉**，只用它的副作用建出节点
     （需要再改 1 条指令把 r0 改成常量 1，让 `cmp r0,#1` 恒真）；
   * 方案③：崩溃若在 `GetMember`（0x1054EC0）⇒ 说明节点虽建出来了但 `GetMember` 返回 null ⇒ 改用
     `GetOrCreateMember` 的返回值直接挂上（方案②的变体）。
3. 崩溃解决后，`TLV`（`FlowState::LoadLevel`）/`TLD`（`OnLoadingTaskListComplete`）是否命中
   才是「关卡真正加载完」的判据 ⇒ 命中即进城镇。

### 21.20 ★★ 第一百一十二～一百一十三轮（2026-11-16 19:50～20:10）：任务级探针 + 探针器新模式

**本轮把「加载屏任务列表」这条路彻底铺好了**：

| 探针 | 位置 | 作用 | 实测 |
|---|---|---|---|
| TAD | `AbstractTaskList::AddTask` (0x13A81C0) | 读 r1（Task/TaskDelegate）+ [r1+0x10] | **命中 20 次**，但 [r1+0x10] 读到 0 ⇒ **Delegate.m_methodPtr 不在 +0x10**（r1 可能是 `Task`，其 `_method` 在 +0x18） |
| TKN | `Task::Run` (0x13A9A4C) | 读 [this+0xC]=`_name`（12 字符） | **0 命中** ⇒ 任务不经过 `Task::Run` 执行（走的是 delegate 直调 / RunAsyncOperation） |
| TLD / TLV / TSC | OnLoadingTaskListComplete / LoadLevel / StartCharacterCustomization | 判据 | 仍 0 |

⇒ **下一轮就两件事**：
1. 认 task：探 `SequentialTaskList::DoRunTask`(0x13A9ED8) 或把 TAD 改成读 **`[r1+0x18]`（Task._method）**，
   再在离线侧对 `Delegate.m_methodPtr` 做二级解析（`Task._method` → `Delegate.m_methodPtr`）；
2. 找到「第一个没 SignalCompletion 的 task」⇒ 那就是关卡加载卡住的确切位置。

#### 21.20.1 探针器新增模式（本轮写的，附两个坑）

* **`str6`**：从**寄存器**里的 Il2CppString 打 **12 个字符**（6 个字）⇒ 成员名不再被 6 字符截断
  （`NPCAct` → `NPCActorType`）。用法：`('str6', sreg, coff)`。
* **`regstr6`**：从**字段**（`[rX+off]` 的 Il2CppString*）打 12 个字符。用法：`('regstr6', reg, off)`。
* ⚠ **坑 1（踩了）**：`regstr6` 的代码段有 **88 字节**，所以 `TAG_OFF` 必须 ≥ 88（我先写 72 ⇒
  「tag 位置错」断言挂掉 ⇒ **构建失败 ⇒ 设备上还是旧 .so ⇒ 探针一个都不命中、看起来像「探针失效」**）。
  ⇒ **教训：构建失败时 `&&` 会短路，但后面用 `;` 串的命令照跑 ⇒ 会拿旧 .so 去跑测**；
  **每轮必须核对 `build_rt.log` 里的 `hooks=` 与本轮探针集一致 + 核对设备 .so 的 md5**。
* ⚠ **坑 2**：`regstr6` 的步长必须 ≥ 88+4+49 ⇒ 用 **0x90**（我先写 0x80 ⇒ blob 放不下）。

#### 21.20.2 当前最佳配置（已验证：0 托管异常）

```bash
export BLADES_NO_QUEUE_GUARD=1 BLADES_FIX_R1=0 BLADES_FIX_R2=1 BLADES_FIX_R3=0 BLADES_FIX_R4=0 \
       BLADES_FIX_R6=1 BLADES_FTUE_SET=7 BLADES_FIX_R8=1 BLADES_FIX_R10=1 BLADES_FIX_R11=1 \
       BLADES_HOLE_G=0 BLADES_HOLE_H=1 BLADES_HOLE_I=1 BLADES_NO_HOLE_F=1
python build_vtrace.py && python patch_c_fire.py          # 探针区 0xA7BADC..0xA7BC8C（不碰洞 F/H）
SAVE_WRAP=1 INTIFY=0 CHAR_MODE=char python make_withchar.py shapes/R_perchar.json shapes/R_withchar.json
RESTART=0 PUSH_SO=1 INSTALL=0 WAIT=95 ROUTES=shapes/R_withchar.json bash run_full.sh
```

| 补丁 | 位置 | 作用 |
|---|---|---|
| R1 关 | 0xA73618 | 恢复 `bge`（状态 ≥4 才走 Fire） |
| R6 + FTUE_SET=7 | 0xA731EC | FTUE 状态 4→**7**（`PlayerActor::RetrievePersistent` 的 `cmp r0,#7` 闸门要成立） |
| R8 | 0x8220EC | `get_IsAcceptedToPlay` 恒真 |
| **R10** | 0xA73688 / 0x10EA3BC / 0x10EA3C0 | Fire(LoadTown) + `FetchingSaveData` permit 改成 **LoadTown → `Loading`** ⇒ 真正进 `Loading` 状态 ⇒ 加载屏任务列表启动（TRU 0→20） |
| **R11** | 0xAC736C | `HasMember("Character")` → **`GetOrCreateMember<object>`** ⇒ **CharacterSetting 的 `_persistentNode` 永远非 null ⇒ 玩家 Actor 生成时的 NRE 消失（0 托管异常）** |
| 洞 H / 洞 I | 0xA7BD5C / 0xA7BDF0 | 内容加载完成才装认证链 / 放行加载菜单退场 |

### 21.21 ★★★ 第一百一十四～一百一十七轮（2026-11-16 20:00～21:00）：**加载任务全都跑了，卡点收窄到 `LoadFirstLevel` 内部的一道分支**

**本轮结论（全部有探针/读码实证）**：

| 观测 | 结果 | 含义 |
|---|---|---|
| `TGA` LoadGameDataLibrary / `TGM` SetupGlobalManagers / `TIL` LoadInitialLevel / `TOL` OpenLoadingMenu / `TLF` LoadFirstLevel | **各 9～10 次，全部跑了** | ★★ **加载屏任务列表的 5 个 task 全部执行了**（不是 task 卡住） |
| `TLV` `FlowState::LoadLevel` | **0** | ★★ **城镇关卡仍然没被加载** ⇒ 卡在 `LoadFirstLevel` 内部，**没走到出口③** |
| `TFU` `FtueState::SetFtue` | 9 次，**每次都是 7** | FTUE 状态已正确设为 7（`BLADES_FTUE_SET=7` 生效）⇒ **闸门不是 FTUE 状态** |
| 玩家等级抬到 30（`PLAYER_LEVEL=30`） | `TLV` 仍 0 | **闸门也不是玩家等级** |
| 托管异常 | **0 条** | R11（HasMember→GetOrCreateMember）持续有效 |

#### 21.21.1 `LoadFirstLevel`（0x80510C）的四个出口（读码）

```
出口① 0x805388  b  GameplayManager::StartFTUEQuest (0xA86AD8)
出口② 0x8053E8  b  FlowTrigger::Fire(Authenticate)            ← ★ 我们每轮都走这条（循环的来源）
出口③ 0x805474  bl FlowState::LoadLevel(levelType=2, loadType=0) + EnterTownPostFtue   ← ★★ 想要的那条
出口④ 0x805624  b  GameQuestManager::ValidateConsistency
```
分派方式：第一道闸门在 **`0x805254`**（`blx r2`，接口虚方法槽 `+0xBC` ⇒ slot 23），
后面还有一道 `0x805644`（槽 `+0x19C` ⇒ slot 51，只从 X∈{1,2} 分支才能到达；非 0 ⇒ `0x8057B4` 走 `StartFtue`）。
★ 2026-11-19 已把整张跳转表解码并核对完毕（含每个 X 的真实去向）⇒ **见顶部「续跑简报」§C，以那里为准**。
原记录（部分已过时，仅供追溯）：
（`0x805644 blx r2`；结果在 `0x805648 cmp r0,#0 / bne 0x8057B4`）：
* **非 0** ⇒ 跳 `0x8057B4` ⇒ 走 `GameplayManager::StartFtue`（**当新玩家开教程** ⇒ 教程路，永远不加载城镇）
* **= 0** ⇒ 继续往下（0x805650…0x805600 的分支链还没逐条读）⇒ 最终落到出口②/③/④ 之一。

⇒ **⇒ 下一轮就一件事：把 `LoadFirstLevel` 里 0x805650..0x805600 这段分支链逐条读出来**
（`python disasm.py 0x80510C`），找出「什么条件下走出口③（EnterTownPostFtue + LoadLevel）」——
它很可能是「玩家等级 / 剧情进度 / 城镇解锁条件」这类判定（我们存档里缺相应字段 ⇒ 一直落回出口②）。

#### 21.21.2 本轮新踩的坑

* `FtueState::get_Status`(0xC97050) **不是 STMDB 序言** ⇒ 探针器直接「跳过」⇒ 该点探不了
  ⇒ 想知道「某个接口方法叫什么名字」，得用 `'cstr'` 模式打 MethodInfo+0x8（**需要另加一个无序言版**）。
* `AbstractTaskList::AddTask`(0x13A81C0) 的 r1 **不是委托对象**：读 `[r1+0xC]` 与 `[r1+0x10]` **都是 0**
  ⇒ 委托的字段布局与预想不同（IL2CPP 的 Delegate 在 32 位下的偏移要重新核）⇒ **别再靠猜委托布局**，
  改成直接探「任务要调的方法本体」（本轮就是这么做的，一轮就看清 5 个 task 全跑了）。

---

## 二十二、事故记录：常驻守护让用户关不掉后端（2026-11-19 已清除）

**现象**：不跑逆向时 `server.py`(8443) / `file_server.py`(8080) 一直活着，手动 kill 后约 20 秒自己回来，
用户明确反馈「关都关不掉」。

**根因**（不是 `run_full.sh`，它 2026-11-16 就改成同调用内 `nohup` 了）：

1. 计划任务 `\BladesOffline`（2026-11-16 13:04 创建，`LogonTrigger` 登录即启）
   → `wscript.exe //b launch_blades_offline.vbs` → `cmd /c blades_offline_server.cmd`；
2. `blades_offline_server.cmd` 是个 **`:loop … ping -n 21 … goto loop` 自愈循环**：
   每 ~20 秒 `netstat | findstr ":8443 "`，端口一空就 `start "blades_srv" … python server.py`，8080 同理；
3. 该循环实例从 **11-18 23:53 起一直挂着**（父进程 wscript 早已退出，成了孤儿 cmd）
   ⇒ 只杀 python 没用，必须**先杀循环再杀进程树**。

**已做的清除**（顺序很重要：先摘任务，否则 `schtasks /end` 之外还会被 `RestartOnIdle`/触发器拉回）：

```bash
export MSYS_NO_PATHCONV=1              # Git Bash 会把 /tn 当路径改写
taskkill /PID <blades_offline_server.cmd 的 PID> /T /F   # ★ 先杀自愈循环（/T 连带子进程树）
schtasks /delete /tn "\BladesOffline" /f
```

`run_full.sh` 已在删除 `blades_srv` / `blades_filesrv` 的同一处加了 `schtasks /delete /tn BladesOffline /f`，
并在注释里写明「一律不要恢复」。`blades_offline_server.cmd` 与 `launch_blades_offline.vbs` 文件保留（默认无人调用），
**不要再把它们接回任何触发器**。跨调用需要后端时，用同一次 bash 调用里的 `nohup` 起，跑完随调用回收。
