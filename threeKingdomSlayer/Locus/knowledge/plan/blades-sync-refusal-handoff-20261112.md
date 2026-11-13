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
summary: 《上古卷轴：刀锋》离线化 —— /sync 回调不触发的根因定位（本轮突破）：不是响应体形状、不是索引、不是反序列化，而是 SerialBackendCommunicationService::AddRequestForCompletion(0x159B2BC) 里的 `cmp r4,r7 ; popne`(0x159b34c) 静默拒绝 —— 响应回来时 `_currentRequest` 已被换成别的请求。已用三轮探针+静态读码钉死，并落地补丁 [I]（把「同时只允许一个请求在飞」下沉到 StartNextRequest，洞 C @0xA7BDA0）。唯一未解的矛盾：补丁生效后 TIC(StartNextRequest 入口日志)=0 而 TPS(串行 PerformRequest)=385，即串行 PerformRequest 不是从 StartNextRequest 进来的。附全部工具、地址表、复跑命令与踩坑。
---

# Blades 离线化 · `/sync` 被「静默拒绝」定位 + 交接（2026-11-11 夜 ~ 2026-11-12）

> 上级交接：`plan/blades-handoff-20261029.md`（开机读物）、`plan/blades-sync-response-blocker-20261111.md`（上一轮堵点复盘）。
> 工作目录：`Library/Locus/tmp/blades/`（不依赖 Unity 主工程，**不要提交/推送**）。

## ★★★ 最新进展（2026-11-13 第三十五～五十轮）——**先看这一节再往下读**

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
