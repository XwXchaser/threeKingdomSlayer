---
id: kd_a24a0e9d-517d-4e89-a35a-d3e500b70338
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_blades_sync_blocker_20261111
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
summary: 《上古卷轴：刀锋》离线化的当前堵点复盘（2026-11-11）。本轮拿到一条可复现基线（流程停在 /sync 响应、TGC=0、认证重试 600+ 次），静态读码确认客户端从响应头 Request-Index 取响应索引、错误信息字面量是 PARAMETER_REQUEST_INDEX 提示、还发现 BladeBackendResponseIndex 字段与 requestIndexToBackendSyncRequestIndexData 转换表；实验证伪了「回显 Request-Index 就能修好」的假设。同时修好了三个工具缺陷（后端实例不换导致实验作废、adb 反向隧道后 shell 失效、模拟器 framework 崩溃的恢复流程），并新增三个分析脚本。
---

# Blades 离线化 · `/sync` 响应被丢的堵点（2026-11-11）

> 工作目录：`Library/Locus/tmp/blades/`（**不依赖 Unity**，与 threeKingdomSlayer 主工程无关，不要提交/推送）。
> 上级交接：`plan/blades-handoff-20261029.md`；完整证据链：`plan/blades-flow-trace-breakthrough-20261029.md`。

## 〇、一句话现状

**bundle 初始化、认证、进入「读存档」都通了**；流程稳定停在 **`/sync` 的响应回调**上：
`/sync` 请求确实发出去了、后端也收到了，**但客户端把响应整个丢掉** ⇒
`SavedDataServiceImpl::GetAllCharacters`（TGC）一次都不触发 ⇒
`CharacterService::GetAllCharacters`（TGA）从没创建 ⇒
`FlowState::OnSaveDataFetched`（TSF）不触发 ⇒ 永远到不了选角/建角界面。

**本轮没有推进到建角界面**，但把「为什么丢掉」从猜测推进到了**有反汇编证据的三个具体判定点**，
并**证伪了上一轮排第一的假设**（见 §三）。

---

## 一、本轮可复现基线（2026-11-11 12:05，设备 emulator-5554，探针 10 个）

```
设备: -s emulator-5554        后端实例: pid=43320 echo_index=True server_ts=True
  TSF = 0    TFI = 14   TLS = 2    TBM = 1    TCB = 1
  TGC = 0    TLD = 1    TSR = 1    TGA = 0    TIE = 9
后端请求:
  1 GET  /Android/MetaManifest.json
  1 GET  /china/Android/host_info_cn.json
  1 GET  /api/game/v1/public/sync          ← 发出去了（带 Authorization: local=localtoken0001）
  1 POST /api/game/v1/public/characters//towns/current/rewards/current
  1 POST /api/analytics/v1/public/events
  609 POST /api/authentication/v1/public/auth/gplus   ← ★ 认证重试风暴
客户端发来的 Request-Index: 全部是 0（612/612）
```

★ 关键读法：
* `/sync` 带上了 `Authorization` ⇒ **认证是成功的**，会话被接受过。
* **认证重试风暴 609 次**是**下游症状**（流程反复 ForceReset/重入认证），不是独立问题。
* 客户端每个请求都带 `Request-Index: 0` ⇒ 客户端的**请求索引计数器从未前进**。
* 状态流只进了 9 个状态，`TLD/TSR` 之后**再无状态迁移** ⇒ 流程在**等 `/sync` 的回调**。

---

## 二、静态读码的三个硬事实（v7a `dumper_out_v7a/dump.cs` + `dumper_in_v7a/libil2cpp.so`）

### 2.1 ★ 响应索引来自**响应头** `Request-Index`，缺了就永远是 -1

`Response.ProcessResponseHeaders`（RVA **0x1B77918**）：

```
01b7796c  str  r1,[r7,#0x20]!      ; r1 = -1  ⇒ Response._responseIndex（字段 +0x20）= -1
01b779cc  ldr  r0,[pc,#0x2ac]      ; → 槽 0x320f9e8（字典类型）
01b779d8  ldr  r1,[pc,r1]          ; → 字面量 'Request-Index'
01b779e8  bl   0x2314748           ; Dictionary<string,string>.ContainsKey
01b779ec  cmp  r0,#1
01b779f0  bne  0x1b77b1c           ; ★ 没有这个头 ⇒ 直接跳过，_responseIndex 保持 -1
...
01b77a78  ldr  r0,[pc,r0]          ; → 字面量
          "PARAMETER_REQUEST_INDEX: Wrong value, should be an int: '{0}'"
01b77a2c  bl   0x1c2bcbc           ; int.Parse（解析失败也 bail）
```

⇒ **服务器必须在响应头回 `Request-Index: <整数>`**，否则响应的索引恒为 -1。

### 2.2 ★ 还有第二个索引字段：`BladeBackendResponseIndex`

* 字面量 `BladeBackendResponseIndex`（0x3336A60）被 `RestManager.GetPersistentObjectFromBase64`（RVA **0x1192540**，引用点 0x1192810）读取
  ⇒ 后端下发的数据（base64 解码后的 JSON）里应当有 `BladeBackendResponseIndex` 这个键。
* `/sync` 自己的契约（客户端自带资产 `schemas_out/`）：
  * `requestIndex.schema` = 根键 `requestIndex`，类型 **System.String**；
  * `requestIndexToBackendSyncRequestIndexData.conversion` = 读 `requestIndex` → 写 `BackendData/requestIndex`；
  * `BackendSyncRequestIndexData.CurrentRequestIndex` 是 **int**（`KEY_BACKEND_REQUEST_INDEX = "requestIndex"`）。

### 2.3 ★ 客户端自带「存档形状」清单

`schemas_out/SaveGame.schema` 的根键：
`ChestManager` / `Settings` / `Quests` / `TownStats` / `LevelManager` / `Town` / `Player` /
**`Session{UserId,Token,TokenSchema}`** / **`BackendCharacterData{BackendId,CharacterSave}`** / `Version`。
⚠ 之前为了躲 NRE，把通用体里的 `BackendCharacterData` 等角色相关根键**删掉了**（见 `build_routes_per.py`），
但**存档本身**（`SaveGame.schema`）是带 `Session` 与 `BackendCharacterData` 的。

---

## 三、★ 本轮**证伪**的假设（别再走一遍）

| 假设 | 实测 | 结论 |
|---|---|---|
| 「后端回 `Request-Index` 响应头 ⇒ 响应就能被匹配」 | 加了 `--echo-index --server-timestamp` 后跑测：TGC 仍 0、认证重试 609 次、客户端仍全发 `Request-Index: 0` | **证伪**（缺头确实是 bug，但不是卡点） |
| 「`/sync` 响应用 `requestIndex: -1` 就能触发回调」 | 实测 TGC 仍 0 | **证伪** |
| 「回 `Server-Timestamp` 能修好时间差」 | 同上，行为逐字相同 | **证伪** |

⇒ 说明 `_responseIndex` 只是**附带信息**，响应被丢弃的判定点在**更靠前**的地方（反序列化/响应数据构造阶段）。

---

## 四、本轮修好的**工具**缺陷（都很贵，别再踩）

1. **后端实例没换 ⇒ 实验作废**（踩了一轮）：
   `measure.sh` 杀旧后端不干净时，新实例起不来（8443 被占），跑测实际用的是**上一轮的旧 server.py**。
   ⇒ 已修：`kill_port()` 改成 netstat+taskkill **加 powershell 兜底**；
   `server.py` 新增 **`GET /__health`**（回 pid/开关/路由表/启动时间）；
   `measure.sh` 起完后台**必须**打一次健康检查并打印（以后每轮输出里都能确认是哪个实例）。
2. **`adb server` 重启后，`adb reverse` 会让设备 shell 彻底死掉**（脚本里表现为 `reverse 后 shell: []`、客户端根本没启动、所有探针 0 命中）：
   ⇒ **恢复办法（本轮实测有效）**：`adb reboot` 不够（会触发 system_server watchdog），
   **必须完整重启雷电**：`ldconsole.exe quit --index 0` → 杀 `dnplayer/LdVBoxHeadless` →
   用 **schtasks** 拉起（`ldconsole.exe launch --index 0`，`MSYS_NO_PATHCONV=1` 否则 `/create` 会被 Git Bash 改写成路径）→
   等 90~120 s → `adb devices` 出现 `emulator-5554` → 重新 `adb reverse` 就能活。
3. **反汇编脚本的池字解析**：IL2CPP 的两种取址形式 PC 都是 **insn+8**：
   * `ldr rX,[pc,#imm]` → 池地址 = insn+8+imm，池字是**相对偏移**；
   * `ldr rY,[pc,rX]` → 地址 = **insn+8** + rX（**不是**池地址+8，写错就解不出字符串）。
   ⇒ 已修进 `dis_rva.py`；修之前把 `Request-Index` 误判成 metadata 字符串，修好后才认出来。

---

## 五、本轮新增的工具（都在 `Library/Locus/tmp/blades/`）

| 文件 | 作用 |
|---|---|
| `dis_rva.py` | **按 RVA 反汇编** v7a `libil2cpp.so`（自动 ARM/THUMB），并把 IL2CPP 的池字链解成**可读字符串**；`python dis_rva.py 0x1392480 --n 40 [--arm]` |
| `find_xref.py` | **字面量 → 代码引用点**（`python find_xref.py --str BladeBackendResponseIndex`）：先在数据段找池槽，再在代码段找 `ldr rX,[pc,#imm]` 引用 ⇒ 直接定位「哪条代码用这个字符串」 |
| `server.py --echo-index [--echo-index-default N] --server-timestamp` + `GET /__health` | 影子后端可开关地回 `Request-Index`/`Server-Timestamp`，并能自证是哪个实例在服务 |

---

## 六、下一步（按性价比，建议就按这个顺序做）

### A（首选，约 30 分钟，不需重打 APK）：**用客户端自带契约做 /sync 响应体的枚举实验**
因为 `/sync` 的响应体是**数据**（路由表 `shapes/R_perchar.json`），改完 `python build_routes_per.py` 即可，不必重打 APK。
建议依次试（每轮只改一个变量，跑 `measure.sh` 看 **TGC** 是否 ≥1）：

1. `{"requestIndex": "0"}` —— 字符串（`requestIndex.schema` 写的就是 String）★ 文档说历史上试过，但那是**旧构建/旧后端**，值得用当前环境复测；
2. `{"requestIndex": "0", "BackendData": {"requestIndex": "0"}}`；
3. `{"requestIndex": "0", "Session": {...}, "BackendCharacterData": {...}, "Version": 0}` —— 按 `SaveGame.schema` 给存档根键；
4. `{"requestIndex": "0", "SaveGame": {...}}`（SaveGame 整体塞进去）。

★ 判定标准只看 **TGC**（`/sync` 的响应回调）与随后的 **TGA/TSF**。

### B（若 A 全灭，约 1 小时）：**把探针挪到响应处理链上**，看响应死在哪一步
洞区只剩 ~30 字节 ⇒ 要**腾位置**：bundle 三连探针 `TLS/TBM/TCB` 现在已无价值，可换成：
* `Response.ProcessResponseData`（0x1B77CB8）—— 反序列化入口（打返回值：成功/失败）；
* `Response.CompleteDeserialization`（0x1B786E4）；
* `BladeRequest.ProcessResponse`（0x139E91C）—— 每个响应的总入口（打 httpCode + isError）。
改 `build_vtrace.py` 的 `HOOKS` → `bash build_c.sh`（约 8 分钟）→ 跑测时 `INSTALL=1`。
⚠ 跑前必须确认 `hooks=… / cave 用到 … / 签名 OK` 都打出来，且**设备 `.so` 与本地 `build/libil2cpp.c.so` 的 md5 一致**
（否则跑的是旧探针，历史上因此白跑过 3 轮）。

### C（并行可做）：把「认证重试风暴」这条独立线索钉死
现象：609 次 `POST /auth/gplus`，**每次都带 `Request-Index: 0`** ⇒ 客户端的请求索引计数器不前进。
⇒ 可用 B 的探针位之一打 `BackendCommunicationService.SetCurrentReponseIndex`（0x1191F48）与
`Request.SetRequestIndex`（0x1B75954），直接看索引是谁在写、期望值是多少。
⚠ 别把它当成独立 bug 先修：它极可能只是「/sync 失败 ⇒ 流程重置」的**下游症状**。

---

## 七、复跑（当前可用）

```bash
cd H:/Project/threeKingdomSlayer/threeKingdomSlayer/Library/Locus/tmp/blades
export MSYS_NO_PATHCONV=1
WAIT=100 ROUTES=shapes/R_perchar.json SERVER_ARGS="--echo-index --server-timestamp" bash measure.sh
```
* `measure.sh` = 起后端(schtasks) → 换端口占用 → 建 reverse → 杀 app + 清本地存档 → 起 app → 等 N 秒 → 导 logcat/请求/状态流；
* **看输出里的「后端实例」那行**确认是新实例；`reverse 后 shell` 必须不是 `[]`；
* 探针计数用 `grep -oE " I [A-Z]{3} +: " log_tail.txt | sort | uniq -c` 复核，别只信脚本汇总。

---

# 八、2026-11-11 12:00–13:30 续：响应体枚举 + 探针定位（断点已缩到三步之内）

## 8.1 `/sync` 响应体枚举：**6 种形状全部失败** ⇒ 响应体形状不是卡点

用 `make_sync_variants.py` 生成 6 个变体（`sweep_sync.sh` 逐个跑，判定只看 **TGC**）：

| 变体 | `/sync` 响应体 | 结果 |
|---|---|---|
| v1_str | `{"requestIndex": "0"}`（String，按 schema） | TGC=0 |
| v2_str_bd | `{"requestIndex": "0", "BackendData": {"requestIndex": "0"}}` | TGC=0 |
| v3_save | `{"requestIndex": "0", "Session": {...}, "BackendCharacterData": {...}, "Version": 0}` | TGC=0 |
| v4_savegame | `{"requestIndex": "0", "SaveGame": {...}}` | TGC=0 |
| v5_bbri | `{"requestIndex": "0", "BladeBackendResponseIndex": 0}` | TGC=0 |
| v6_num | `{"requestIndex": 0}`（数字，对照组） | TGC=0 |

⇒ 结论：**`/sync` 的响应体形状不是卡点**（至少不是这 6 种）。

## 8.2 探针实验（每轮只改探针，body/路由不动）

| 轮次 | 探针 | 实测 | 结论 |
|---|---|---|---|
| 13:00 | TRQ=`Response.ProcessResponseData`(基类 0x1B77CB8)<br>TRP=`BackendSyncRequestIndexData.RetrievePersistent`(0xD4EA00) | **各 534 次**（≈认证响应数） | 响应**确实被正常处理**；但两者对**每个**响应都触发 ⇒ 无区分度 |
| 13:05 | TAU=`AuthResponse.GetSessionFromResponse`(0x15AEA54)<br>TPO=`OnPostDeserializationCallback`(0x159A54C)<br>TBD=`BackendSyncronizationDone`(0x1597BA8) | **TAU=1062**（每个认证响应解析 2 次）<br>**TPO=0、TBD=0** | ★★ **认证响应是被接受的**（537 次重试是流程重置的下游症状）<br>★★ **`/sync` 的数据根本没进存档服务** ⇒ 断点在「响应到达」与「回调派发」之间 |

## 8.3 ★★ 踩过的探针坑（会直接导致误判）

* **IL2CPP 会把相同方法体合并成共享实现** ⇒ `BackendSyncRequestIndexData.RetrievePersistent`(0xD4EA00)
  实际是所有 `IBackendData` 的**共用实现** ⇒ 它对**每个**响应都触发（534 次）⇒ **端点专属的直觉会骗人**。
* **虚方法/委托方法没有直接调用点**（`GetAllCharacters`、`OnPostDeserializationCallback` 都是 0 个 `bl` 调用点）
  ⇒ 但**探针照样有效**（改的是函数入口，调用方走虚表/委托也命中）。
* **探针地址必须是「真会被调用」的那个实现**：基类 `Response.ProcessResponseData`(0x1B77CB8) 有 0 个 `bl` 调用点，
  但客户端确实调它（虚表 Slot 8）⇒ **用 `bl` 扫描判断「会不会被调用」会漏掉虚调用**。
* **下游探针没有信息量**：`SyncRequestIndexResponse.GetRequestIndex`(0x1392480) 唯一调用点在 `GetAllCharacters` 内部
  ⇒ TGC=0 时它必然=0。

## 8.4 下一轮探针（已构建，正在跑）

三个**计数可对比**的探针（认证响应约 537 个 ⇒ 多 1 个就是 `/sync`）：

| 探针 | 地址 | 含义 |
|---|---|---|
| TRQ | `Request.ProcessResponseData`(0x1B5B768，重写版) | 响应进入**请求级**处理 |
| TDC | `Response.CompleteDeserialization`(0x1B786E4) | 响应进入**反序列化** |
| TCC | `BackendCommunicationService.CompleteRequest`(0x11A50B4) | 请求被**完成**（回调派发前一步） |

判读树：
* TRQ=537 且 TDC=537 ⇒ `/sync` 响应**没进**请求级处理（HTTP/队列层被丢）；
* TRQ=538 且 TDC=537 ⇒ 进了但**没走到反序列化**（被类型/状态校验拒 ⇒ `Request.ProcessResponseData` 里的
  `'Response has the wrong type'` 判定点 0x1B5B7F4）；
* TDC=538 且 TCC=537 ⇒ 反序列化了但**没完成** ⇒ 回调派发断了（这才是 TGC=0 的直接原因）。

## 8.5 ★★ 工具提速：热替换 `libil2cpp.so`（迭代 15 分钟 → 4 分钟）

重打 1.2GB APK 再 `adb install -r` 要 **10 分钟以上**，且装到一半设备掉线就白装。
**可行办法**：只换 APK 里的 `lib/armeabi-v7a/libil2cpp.so`（53MB），**就地覆盖设备上已解压的那一份**：

```bash
adb push build/libil2cpp.c.so /data/local/tmp/libil2cpp.so        # 实测 1.9 秒
adb shell su -c "cp /data/local/tmp/libil2cpp.so /data/app/<pkg>-N/lib/arm/libil2cpp.so; chmod 755 ..."
adb shell su -c "md5sum /data/app/<pkg>-N/lib/arm/libil2cpp.so"   # 与本地一致才算成功
```
* 包目录后缀会变（`-1`/`-2`）⇒ 用 `pm path <pkg>` 现场取；
* ⚠ **模拟器重启会还原**（package manager reconcile）⇒ 顺序必须是「重启 → 热替换 → 跑测」；
* ⚠ 改完后 `classes.dex` 的补丁（ticket 伪造等）仍在已装的 APK 里，不受影响 ⇒ **不必重装**。

## 8.6 ★★ 新增 `run_full.sh`：一条龙跑测（把环境坑全包了）

```bash
RESTART=1 PUSH_SO=1 INSTALL=0 WAIT=90 ROUTES=shapes/R_perchar.json bash run_full.sh
```
依次做：①（可选）完整重启雷电 → ② 等设备就绪 → ③ `adb reverse` + **shell 自检重试 5 次** →
④ 起影子后端 + `/__health` 自检 → ⑤（可选）热替换 .so / 装 APK → ⑥ 清状态 + 启动客户端 →
⑦ 观察 N 秒 → ⑧ 导出探针计数 / 端点统计 / 状态流。

⇒ **必须一条龙的原因**：Locus 每次 bash 调用结束会回收 **adb server** ⇒ 跨调用的 reverse 隧道全丢；
而 adb server 重启后的**第一次** `adb reverse` 会让 LDPlayer 设备通道 **offline**
（症状：`reverse 后 shell: []`、客户端根本没起、所有探针 0 命中）。

---

## 九、2026-11-11 13:20–13:40 续：回调链探针定位（★ 断点已钉到「请求完成」这一步）

### 9.1 ★ 纠正一个认知：`/sync` 的回调属于 **SessionService**，不是 SavedDataService

```
SessionServiceImplementation
  _onSyncRequestIndexCallback            // 0x10
  SyncRequestIndex()                      // 0x159BB00  ← 创建 /sync 请求
  OnSyncResponse(Response response)       // 0x159BE3C  ← ★★ 真正「调用」回调的方法
  RegisterCallbackOnSyncRequestIndex(...) // 0x159BC94  ← 注册入口
  <>c.<.ctor>b__5_0(SyncRequestIndexResponse _) // 0x159C204 ← 流程侧注册的 lambda
SavedDataServiceImplementation
  GetAllCharacters(SyncRequestIndexResponse response) // 0x15978A0 ← 流程侧真正的回调
```
⇒ 探针实测：**TGR（注册）= 1**（回调注册成功）、**TGB（lambda）= 0**（回调从未被调用）。

### 9.2 ★★★ 读码找到的「丢弃机制」：`_requestCompleted`

`Request` 基类字段（`dumper_out_v7a/dump.cs` 448060 附近）：

| 偏移 | 字段 |
|---|---|
| 0x60 | `private bool _requestCompleted;` ← ★★★ |
| 0x64 | `protected Response _response;` |
| 0x6C | `private RequestRetryData _retryData;` ← 有重试机制 |
| 0x70 | `private Request.Priority _requestPriority;` |

而 `BackendCommunicationService.CompleteRequest`(0x11A50B4) 开头就是：

```
011a5100  ldrb r0, [r4, #0x60]     ; _requestCompleted
011a5104  cmp  r0, #0
011a5108  popne {r4, sl, fp, pc}   ; ★ 已 true ⇒ **直接返回，跳过整个完成流程（回调不触发）**
```

⇒ **一个请求一旦被标记完成，后来才到的响应会被直接丢弃。**
⇒ 这能解释「回调注册了却不调用」：**/sync 请求在响应到达前就被标记完成/销毁了**
（很可能是流程重置时清理请求队列造成的 ⇒ 与认证重试风暴互为因果）。

### 9.3 ★ 探针选择的三条硬规矩（本轮踩全了）

1. **绝不探每帧函数**：`BackendCommunicationService.CompleteRequest`(0x11A50B4) 实测 **8992 次/轮**
   ⇒ 把 logcat 环形缓冲冲光 ⇒ 其他探针命中**全被挤掉（假 0）**。
   ⇒ 每轮都要用「探针命中数 vs 后端请求数」交叉验证。
2. **IL2CPP 会把相同方法体合并成共享实现** ⇒ 端点专属的直觉会骗人：
   `BackendSyncRequestIndexData.RetrievePersistent`(0xD4EA00) 实际是所有 `IBackendData` 的**共用实现**
   ⇒ 它对**每个**响应都触发（534 次）⇒ 无区分度。
3. **用 `bl` 扫描判断「会不会被调用」会漏掉虚调用**：
   `Response.ProcessResponseData`(基类 0x1B77CB8) 有 0 个 `bl` 调用点，但客户端确实调它（虚表 Slot 8）⇒ 实测命中 534 次。
   ⇒ 反过来：`GetAllCharacters`(0x15978A0) 有 0 个 `bl` 调用点（委托调用）⇒ 探针仍有效，但必须**实测验证**。

### 9.4 下一轮探针（已构建）

| 探针 | 地址 | 含义 |
|---|---|---|
| TSY | `SessionServiceImplementation.OnSyncResponse`(0x159BE3C) | /sync 响应处理函数有没有跑 |
| TXD | `BladeRequest.ExecuteActionOnDone`(0x139EC5C) | 每个请求完成时调一次（计数对照） |
| TGC | `SavedDataServiceImpl.GetAllCharacters`(0x15978A0) | 流程侧真正的回调 |

判读：**TXD ≈ 认证数 而 TSY=0** ⇒ /sync 请求在完成前就被标记完成/销毁 ⇒ 响应被丢弃；
**TSY=1 而 TGB=0** ⇒ 回调被调用但内部提前返回。

### 9.5 ★ 并行实验：服务器侧**递增** `Request-Index`

读码确认：客户端 `SetCurrentReponseIndex`(0x1191F48) 的三个调用点都是
`ldr r1,[response,#0x20]` ⇒ 它把**响应头 `Request-Index` 的原值**写回自己的静态计数器
⇒ 服务器必须回**递增**的索引，否则客户端永远停在 0（实测 543 次认证请求全带 `Request-Index: 0`）。
⇒ 已实现 `server.py --echo-index-seq`（服务器侧递增序号），待验证。

---

## 十、★ 最终定位（2026-11-11 13:46）：`/sync` 响应被「已完成」守卫丢弃

### 10.1 ★★ 用**对象地址**做判别（这招是本轮的关键手法）

探针会打印入口 `r0`（对象地址）⇒ 只要两个探针打的是**同一个请求对象**，地址就相同：

| 探针 | 地址 | 打的是什么 |
|---|---|---|
| TSU | `SyncRequestIndexRequest.Setup`(0x1392148) | **/sync 请求对象**的地址（只打一次） |
| TRP | `BladeRequest.ProcessResponse`(0x139E91C) | **每个收到响应的请求**对象地址 |
| TXD | `BladeRequest.ExecuteActionOnDone`(0x139EC5C) | 每个**被完成**的请求 |

实测（2026-11-11 13:46:41）：

```
TSU = 1   (/sync 请求对象 = 8f1c37f8 @ 13:46:41.486)
TRP = 455  ⇒ 8f1c37f8 **在集合里** ⇒ /sync 响应**确实到达了客户端的 HTTP 响应处理层**
TXD = 453  ⇒ 8f1c37f8 **不在集合里** ⇒ /sync 请求**从未被完成**
（TRP 有 454 个不同地址、TXD 只有 452 个 ⇒ 差 2 个，其中一个就是 /sync）
```

⇒ ⇒ **响应到了，但请求没有走完成路径** ⇒ 回调当然不会被调用（TGC=0）。

### 10.2 ★★★ 丢弃机制：`Request._requestCompleted` 守卫

`Request` 基类字段（`dumper_out_v7a/dump.cs` 448060 附近）：

| 偏移 | 字段 |
|---|---|
| 0x60 | `private bool _requestCompleted;` ← ★★★ |
| 0x64 | `protected Response _response;` |
| 0x6C | `private RequestRetryData _retryData;` ← **有重试机制** |
| 0x70 | `private Request.Priority _requestPriority;` |

`BackendCommunicationService.CompleteRequest`(0x11A50B4) 的**第一件事**：

```
011a5100  ldrb r0, [r4, #0x60]      ; _requestCompleted
011a5104  cmp  r0, #0
011a5108  popne {r4, sl, fp, pc}    ; ★ 已 true ⇒ **直接返回，整个完成流程被跳过（回调不触发）**
```

而 `_requestCompleted = true` 是 `BladeRequest.CompleteRequest`(0x139DA90) 末尾写的（0x139dad0..0x139dadc）。

⇒ ⇒ **结论：`/sync` 请求在响应到达之前就已经被标记为「已完成」**
（最可能是**重试/失败路径**先把它完成了一次：`_retryData` 字段说明客户端有重试机制；
第一次尝试失败→请求被标记完成→重试发出（服务端只看到 1 次 /sync）→ 响应回来时被守卫丢弃）。
⇒ 也可能是流程重置时销毁了待处理请求。

### 10.3 ★ 顺带验证成功的一件事：服务器侧递增 `Request-Index`

`--echo-index-seq` 生效后，**客户端发来的 `Request-Index` 开始递增**（0 → 94 → … → 349），
证明机制正确：`SetCurrentReponseIndex`(0x1191F48) 的三个调用点都是 `ldr r1,[response,#0x20]`
⇒ 客户端把**响应头 `Request-Index` 的原值**写回自己的静态计数器 ⇒ 服务器必须回递增值。
（但它**没有**修好 `/sync` ⇒ 索引不是本次的卡点。）

### 10.4 ★ 本轮还纠正的一个「假 0」

`build_vtrace.py` 对**没有 STMDB 序言**的函数会「跳过」（输出 `跳过（非 STMDB 序言）`）
⇒ 该探针**根本没装上** ⇒ 计数恒为 0（**假 0**）。
本轮 `TGR@0x159BC94` 就是这样被跳过的；而上一轮用**有**序言的包装层
`SessionServiceCallbacks.RegisterCallbackOnSyncRequestIndex`(0x159BC64) 实测 **=1**
⇒ **回调注册确实发生**（结论不变）。
⇒ **纪律：每次跑完先看构建输出里有没有「跳过」，被跳过的 tag 不算证据。**

### 10.5 下一轮的具体动作（给接手的人）

1. **找「谁提前把 `_requestCompleted` 置 true」**：探
   * `Request.Destroy`(0x1B76118)（它不写 +0x60，只写 +0x80 ⇒ 可排除）
   * `SerialBackendCommunicationService.CancelRequest`(0x159B558 → `CleanCurrentRequest` 0x1589798)
   * `SerialBackendCommunicationService.SoftSync`(0x159B560)
   * **重试路径**：找 `_retryData`(+0x6C) 的写入点 ⇒ 哪条路径会「失败后重发同一个请求对象」
   ⇒ 目标：让第一次失败**不**把请求标记完成（或让重试换一个请求对象）。
2. **备选思路（更省事）**：既然 `/sync` 请求「第一次尝试失败 → 被标记完成 → 重试被丢弃」，
   那**让第一次尝试就成功**即可：查 `RequestRetryData` 的构造条件（什么错误会触发重试），
   看看是不是我们某个响应形状/状态码让第一次尝试被判失败。
3. **探针预算**：洞区只放得下 **10 个**（'reg' 72B / 'off' 96B），
   上限 `0xA7B9EC..0xA7BDFC`；每次改 `build_vtrace.py` 的 `HOOKS` 后
   `bash build_c.sh`（约 1 分钟）→ `PUSH_SO=1` 热替换设备上的 .so（**不要重装 1.2GB APK**）。



