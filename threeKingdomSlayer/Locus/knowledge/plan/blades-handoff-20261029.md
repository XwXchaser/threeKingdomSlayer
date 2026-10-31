---
id: kd_30d752d1-ab9a-4929-93d3-558006ff8e0e
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
summary: 《上古卷轴：刀锋》安卓包逆向/离线化 的开机读物（2026-10-30 21:10 更新，先读顶部续跑简报）：洞 B 的三个 bug 已修（控制流被写成无条件 b skip / 取触发器写成 add 而非 ldr / 栈没 8 字节对齐），洞 B 现在能跑；客户端已能自己完成平台认证（我们的后端首次真的收到 POST /api/authentication/v1/public/auth/gplus 并被接受），一路走到 FetchingSaveData → SaveManager::Load；剩下卡点是设备上没有本地存档文件，客户端读不到就停住并弹「网络错误」。关键事实：x86 模拟器的 ARM 转译层里代码段只能 pc-relative 读（计算地址读代码段直接 SIGSEGV）；触发器身份可由地址反查 object(X)=holder[0]−4X；认证链 = 0xa4→0x04→0x0c→0x10 后客户端自己发 0x1c/0x30/0xac。
---

> **2026-10-29 第三阶段修订（最重要，请先读这一段）**
> 本文第 1 / 2 / 6 节的结论**又变了一次**，行动前请先看
> **`plan/blades-flow-trace-breakthrough-20261029.md`**：
> ① **「路线 A（短接 SDK 登录）」彻底作废**：游戏 C# 侧从不调用 `GHomeSendManager.Login`
>    （打点实测全程 0 次命中），把它短接成成功也没人会去调。
> ② **「FlowTrigger/FlowState 静态从未初始化 ⇒ 状态机没被创建」是错的**：状态机工作正常，
>    实测发生 3 次 `DoTransition` + `InvokeEntered`。
> ③ **真正的卡点（已读到全部旁白）**：流程链是
>    **`LoadStartupScene → InitializingBundles → LoadingBaseGameContent`**，卡在第三个。
>    其 transition handler `b__222_223`(0x10E94C4) 打出 `Debug.LogError("zhang  InitializingGame show")`
>    并以 **null context** 打开加载屏 ⇒ `AbstractLoadingMenu::OnOpen`(0xB1852C) 在
>    `0xb18588 popeq` 处提前 return ⇒ **`PlayInAnim()` 从不执行** ⇒ Animator 不播
>    ⇒ `OnOutAnimDoneEvent` 永不被触发 ⇒ **`FlowState::CloseLoadingMenu`(0x800BE0) 从未执行**
>    ⇒ 加载屏永远不关 ⇒ 流程停在 `LoadingBaseGameContent`、到不了认证
>    （`Authenticate` / `/auth/gplus` 从未发出）。
>    ⚠ 两条 `Debug.LogError` 都是开发者用错 API 的调试旁白（`zhang  InitializingGame show` 与
>    `zhang start push norification`），**不是真的错误**；启动任务链其实跑完了
>    （`LoadGameDataLibrary` → `SetupGameManagers` → push 通知 → 本地化）。
>    ⚠ 「强行让 `PlayInAnim()` 执行」这条**已被证伪**：`_context` 为 null 时
>    `OnInAnimDoneEvent`/`OnOutAnimDoneEvent` 会直接抛异常。
>    ⇒ **唯一正解是让 `b__222_223` 改传一个带回调的 `LoadingMenuContext`**
>    （照 `FlowState::OpenLoadingMenu`(0x8007C4) 构造）。
>    ⚠ 状态名是用「`FlowState::.ctor` 打点序号」×「dump.cs 的 86 个 `public static FlowState` 字段顺序」
>    换算出来的（两者一一对应，已实测验证）。完整表见 `Library/Locus/tmp/blades/flowstate_fields.md`。
> ④ **修法 N 已实测证伪**：把 `FlowState._menuInAnimationCompleted`（静态偏移 0x120）置 true **不奏效**（补丁不崩，但流程与基线完全一样）。
>    ⇒ 真正的等待点是 `_waitForMenuOutAnimationTaskControl`（0x11C）那个 **TaskControl 的完成信号**。
> ⑤ **只剩修法 B**：让 `b__222_223` 用**带 onInAnimDone/onOutAnimDone 回调的 `LoadingMenuContext`** 开屏
>    （照 `FlowState::OpenLoadingMenu`(0x8007C4) 造；难点是 `Action::.ctor` 的 target/methodPtr 来源，且**时机**要避开
>    `OpenLoadingMenu` 在 manager 未就绪时的 NRE）。
> ⑥ **迁移表已抽出**（静态，脚本 `extract_transitions.py`）：180 处 `config.Add` → 80 条状态对；
>    顺带发现 `FlowState` 的 119 个静态字段里有整套“等加载屏”机制
>    （`_menuInAnimationCompleted` 0x120 / `_waitForMenuOutAnimationTaskControl` 0x11C /
>     `_loadingMenuControls` 0x124 / `_loadingMenuInfo` 0x128）。
> ⑤ 新增能力：**多点 logcat 打点器**（`build_vtrace.py`，一次运行拿 17 个流程节点的事件序列），
>    取代了「一次只能命中一个」的崩溃地址探针；另有「无序言版 cave」可直读 `Il2CppString`。

> **2026-10-29 晚间修订说明（重要）**
> 本文第 2 节的"根因"描述已被实测修正，**不要再照旧结论行动**：
> ① SDK 初始化其实是**成功**的；`NoClassDefFoundError: Lcn/uc/gamesdk/R$layout;` 只是悬浮窗那一处崩溃，
>    且该类在游戏 dex 里"只有引用没有定义"（其资源包从未下载）；
> ② 登录入口在 **Java 侧**（`MainActivity.Login() → GHome.login() → MainActivity$2.callback → UnitySendMessage`），
>    客户端**从未主动发起过登录**，因此 `/auth` 一次都没出现；
> ③ 已建成**纯本地**（无需下载）的两条工具链：Java→Unity 报文注入、原生 logcat 打点 + 坐标扫描。
> 详细结论、证据与下一步见 **`plan/blades-java-bridge-breakthrough-20261029.md`（第 2 阶段）**。

# Blades 离线化 · 开机读物（2026-10-29）

> ## ★★★ 续跑简报（2026-10-30 21:50 最新 · 先读这段）★★★
>
> **一句话**：**洞 B 已经修好并被验证**，客户端**首次真的完成了平台认证**
> （`POST /api/authentication/v1/public/auth/gplus` 我们的 session 响应被接受 ✓），
> 一路走到 `FetchingSaveData → SaveManager::Load`；**剩下的卡点已从「本地存档」修正为「请求没被发出」**：
> 请求**入队了**（`AddRequest` 命中 3 次）却**没有构造 HTTP 请求**（`GetRequestForm`/`GetRequestJson` 0 命中）。
>
> ### 一、★★ 本轮最重要的一条模型修正（推翻 21:10 版结论）
> | 事实 | 证据 |
> |---|---|
> | 客户端**走后端**取存档，不是读本地文件 | `TSD`(`SavedDataServiceImpl::LoadSavedData` 0x15964B4) **命中 1 次**；而 `SaveDataLocalStorage::Load`(0x1594340) 与 `JsonManager::GetSavePath`(0x112F990) 的字符串探针**全程 0 命中** ⇒ **本地文件那条路根本没走** |
> | 请求**入队但没发出** | `TAR`(`SerialBackendCommunicationService::AddRequest` 0x159AB70) **=3**；`TRF`(`Request::GetRequestForm` 0x139CD50) = **0**；`TRP`(`Request::PerformRequest` 0x139E830) = 2 |
> | REST 层**每帧都在跑** | `TRL`(`RestManager::LateUpdate` 0x119378C) **=4863** |
>
> ⇒ ⇒ **之前 20:30–21:10 那两轮「往设备上摆存档文件」的方向整个是错的**（本地文件从不被读）。
> ⇒ ⇒ 21:10 版交接里「卡点是设备上没有本地存档」**作废**。
>
> ### 二、★ 请求为什么没被发出（已定位到一处过滤，仍差最后一步确认）
> 队列消费在 `SerialBackendCommunicationService::LateUpdate`(0x159B6F0)：
> ```
> 0x159b6f0  ldr r1, [r0, #0x2c]   ; field_0x2c（“有请求在飞”）
> 0x159b6f4  cmp r1, #0
> 0x159b6f8  bxne lr                ; ★ 非 0 就直接返回，队列不被消费
> ```
> ⇒ **修法 D（已写进 `patch_c_fire.py`，启用中）**：把 0x159B6F8 的 `bxne lr` 改成 `nop`。
> ⇒ 实测：**行为完全不变**（请求依旧不发）⇒ **这个守卫不是原因**。
>
> 真正的过滤在 **0x1589808**（`LateUpdate` 的下一段）：
> ```
> 0x15898ac  mov  r0, r4            ; the service
> 0x15898b4  ldr  r7, [r7, #0x70]   ; r7 = Request.RequestPriority
> 0x15898b8  bl   0x11a49b8         ; get_PriorityFilter()
> 0x15898bc  cmp  r7, r0
> 0x15898c0  bge  0x15898f0         ; ★ priority >= filter ⇒ 跳过这个请求
> ```
> ⇒ **最可能的真因**：取存档请求的 `Priority` ≥ 当前 `PriorityFilter` ⇒ 永远被跳过 ⇒ 队列里越排越死。
> ⇒ ★ **下一轮第一个动作**：把 0x15898C0 的 `bge` 改成无条件分支（或把 `get_PriorityFilter`(0x11A49B8) 的返回值改成足够大）
>   ⇒ 看请求是否终于发出、我们的后端会不会收到 `/characters` 之类的新请求 ✓。
>   （`Request.Priority` 字段在 Request 对象 +0x70 ✓；枚举定义在 dump.cs 的 `Request.Priority` ✓。）
>
> ### 三、★ 洞 B 的最终形态（`patch_c_fire.py`，已验证可跑）
> * 挂点：**`FlowManager::Update`(0x7FC978)**（状态机宿主单例，每帧被调；序言 `E92D4C10` 与旧挂点逐字节相同 ✓）。
> * 台阶：**台阶字节（FlowState 静态块 +0x121）直接存触发器静态偏移**（0xa4=Authenticate、0x04=PostFtueStart、0x0c=PostFtueSetup、0x10=PlatformSignIn），
>   取触发器用 `ldr r4,[holder,X]`（holder = FlowTrigger 静态块[0x5c]）✓。
> * ★ **台阶表不能放 .so 代码段**：x86 模拟器的 ARM 转译层只允许 pc-relative 读代码段，
>   `ldr r2,[r2,r3,lsl #2]` 读代码段 ⇒ **SIGSEGV**（实测 fault addr 0xA7BAAC = 表首项）✓ 已避开。
> * 洞 A 改为把台阶字节置 **0xa4**（不再置 1）✓。
> * 洞 B 字节布局：洞 A `0xA7B9EC`(68B) / 洞 B `0xA7BA40`(148B) / 探针区 `0xA7BADC..0xA7BDFC`（剩 0x30B）✓。
>
> ### 四、★ 认证链已打通（可复现）
> 洞 B 推 `0xa4 → 0x04 → 0x0c → 0x10` 之后，客户端自己发 `0x1c`(CreateAnonymousAccount) → `0x30`(PlayGame) → `0xac`(FetchSaveData)：
> ```
> POST /api/authentication/v1/public/auth/gplus
> {"details":{"user_id":1,"ticket":"1","channel":"gplus_cn"},"platform":"gp","language":"cn","deviceId":""}
> ```
> ⇒ 我们的响应 `{"session":{...},"requestIndex":0}` 被接受 ✓（紧接着客户端就发了 `0x30`）✓
> ⇒ 状态链：`#59 → #28 → #66 → #64 Authenticating → #2 PostFtueStart → #4 PostFtueSetup → #11 CreateAnonymousAccount → #13 PlayGame → #65 FetchingSaveData` ✓
>
> ### 五、★ 本轮新增/修好的工具与事实
> * **修法 D**（`patch_c_fire.py` 内）：队列守卫 `bxne lr` → `nop`（0x159B6F8）★ 当前**启用中**，已证伪 ⇒ 可以保留（无害）或撤掉。
> * **`server.py` 加 TLS SNI 日志**（`ctx.sni_callback`）⇒ 握手失败也能看见客户端连的域名 ✓
> * **`server.crt` 已重签**：同一张 CA，SAN 换通配符（`*.jijiagames.com` / `*.playbladesasia.com` / `*.s3.us-east-1.amazonaws.com` 等）✓ 修掉一次 `Curl error 35` ✓
> * **`file_server.py` 现在能直接从 APK 流式提供任意 bundle**（用 `bundle_index.json` 做 hash→名字映射，因为客户端是按 md5 请求的）✓
> * **`find_literals.py`**：IL2CPP 字面量解码器（15398 条字面量可按关键词列出）✓
> * **`run_ca.sh`**：支持 `TAP="x,y" TAP_AT=45`（点之前校验前台是游戏）、途中 5 张截图、每点打印 `free -m` ✓
> * **触发器身份换算**：探针打印 `(对象地址, holder)`，而 **object(X) = holder[0] − 4X** ⇒ 用已知偏移（`0xa4`）校准一次即可把每条 TFI 换算成具体触发器 ✓
>   （已确认：`0xa4` Authenticate、`0xac` FetchSaveData、`0x5c` OpenMenu_BlockingWithPauseAndBlur、`0x30` PlayGame、`0x1c` CreateAnonymousAccount、`0x104` UpdateBundles、`0x10c` BundlesInitialized ✓）
> * **事故与恢复**：曾用 `find -size +5M -delete` 清理设备 ⇒ 误删 1525 个 bundle 与 `global-metadata.dat` ⇒
>   已从 APK 完整还原（2963 个 bundle，tar 推送 ~50s）✓ ⚠ **清理游戏数据目录只能删自己写过的确切路径**。
> * 设备现状：bundle 完整 ✓；试验用存档候选文件已清理 ✓；装的仍是 21:22 那版 APK ✓（探针集 = TSF TFI TSD TAR TRF TRP TRL TCO TIE TMO TSL ✓）。
>
> ### 六、下一轮建议顺序
> 1. **把 0x15898C0 的 `bge` 去掉**（或改 `get_PriorityFilter` 返回值）⇒ 验证「优先级过滤」假设 ⇒ 期待后端收到新的 `/characters` 类请求 ✓。
> 2. 若仍不发 ⇒ 给 `Request` 对象 +0x70 的 Priority 打一个探针/'mem' 读值，看它到底多大 ✓（并读 `get_PriorityFilter` 的返回值 ✓）。
> 3. 请求一旦发出 ⇒ 观察后端需要什么响应体 ⇒ 大概率是 `BackendCharacterListData`/`characters`（形状已实测：字典 ✓）⇒ 让客户端**建角**并落盘本地存档 ✓。
> 4. `TSF`（`OnSaveDataFetched`）仍为 0 ⇒ 拿到存档后它应该命中 ✓（这是终点信号）。
>
> ### 七、最小阅读清单
> 本文 → `plan/blades-flow-trace-breakthrough-20261029.md` **4K/4J 节**（本轮与上轮完整证据）
> → `memory/blades-reverse-engineering-assets.md` 第 10/11 节。工作目录 `Library/Locus/tmp/blades/`；
> 跑测 `run_ca.sh`（`INSTALL/CLEAR_SAVE/PREFS/ROUTES/WAIT/TAP/TAP_AT`）。
>
> ### 八、★ 新对话怎么接手（2026-10-30 21:55 补）
> **A. 知识是自动带过去的**：本文 frontmatter 是 `injectMode: inherit` + `injectAgents: ['*']`
> ⇒ 新会话启动时会把这份交接**自动注入**给 agent ✓（不需要人工复制上下文）。
> 唯一要做的是在开场白里点一句“读 handoff 顶部 21:50 简报”✓（上面的建议话术即可）。
>
> **B. 磁盘上已经就绪的东西**（全部在 `Library/Locus/tmp/blades/`，**不需要重建**）：
> | 文件 | 作用 |
> |---|---|
> | `patch_c_fire.py` | 洞 A + 洞 B（含**修法 D**：队列守卫 `bxne lr`→`nop`，已证伪、无害）|
> | `build_vtrace.py` | 探针器，当前集：`TSF TFI(off) TSD TAR TRF TRP TRL TCO TIE TMO TSL` |
> | `build_c.sh` | 打包（build_vtrace → patch_c_fire → 重打包 → 签名 → 归档 `.so` 到 `build/history/`）|
> | `run_ca.sh` | 一键跑测（起后端 + adb reverse + 强杀进程 + 写 PlayerPrefs + 跑 + 截图 + 汇总）|
> | `server.py` / `file_server.py` | 影子后端（TLS 8443）/ bundle 文件服务器（8080，**可直接从 APK 流式供给 bundle**）|
> | `shapes/R_auth*.json` | 三种响应体（合并体 / 城镇奖励=最小 / 城镇奖励=客户端自己的存档格式）|
> | `find_literals.py` | IL2CPP 字面量解码器 |
> | `log_vtrace2.txt`、`run*.log`、`build/history/` | 逐轮原始日志与归档 |
>
> **C. 设备侧现状**：模拟器 `emulator-5554`（LDPlayer）**正在运行**；设备上的
> `libil2cpp.so` md5 = `fae4d303133c21b608a7d2fea6af7a73` = 本地 `build/libil2cpp.c.so` ✓ ⇒ **设备跑的正是当前源码** ✓
> ⇒ 新会话可以**直接跑** `run_ca.sh`（`INSTALL=0` 即可）✓。
>
> **D. 两个必须知道的坑**：
> * **adb server 与后端进程活不过一次 bash 调用**（Locus 每次调用结束会回收进程树）⇒
>   所以每轮都要**在同一次调用里**把 `server.py`/`file_server.py`/`adb reverse`/启动客户端/抓 logcat 全部做完 —— `run_ca.sh` 已经这么写了 ✓。
> * **模拟器不能用 `ldconsole launch` 在调用内拉起**（会成为子进程被回收）⇒ 用 `schtasks` 或手动启动：
>   `schtasks /create /tn LdLaunch /sc once /st <HH:MM> /tr '"E:\leidian\LDPlayer4\ldconsole.exe" launch --index 0' /f`
>   （若模拟器已如现在这样在跑，**什么都不用做** ✓。）
>
> **E. 想把这次进展固化到 git**：`Locus/knowledge/plan/blades-handoff-20261029.md`、
> `plan/blades-flow-trace-breakthrough-20261029.md`、`memory/blades-reverse-engineering-assets.md`
> 三个文件是本轮的知识沉淀，可单独 `git add` + `commit` ✓（工作区里还有其它项目的改动，别一起提交）。
>
> ---
>
> ## ⚠【已被上面 21:50 版取代，以下保留作历史】续跑简报（2026-10-30 21:10 版）

> ## ⚠【已被 21:50 版取代】续跑简报（2026-10-30 21:10 版 · 先读这段）★★★
>
> **一句话**：**洞 B 已经修好并跑起来了**，客户端现在能**自己完成平台认证**（我们的后端第一次真的收到
> `POST /api/authentication/v1/public/auth/gplus` 并被接受），一路走到 `FetchingSaveData` → `SaveManager.Load`；
> **剩下的卡点是「设备上没有本地存档文件」** ⇒ 客户端读不到存档就停住，十几秒后弹「网络错误」。
>
> ### 一、本轮（20:30–21:05）四轮实测：洞 B 修好后发生的事
> | 轮 | 改动 | 结果 |
> |---|---|---|
> | III 20:42 | 洞 B 台阶改为 `0xa4→0x04→0x0c→0x10`，挂钩点 `FlowManager::Update`(0x7FC978) | **状态链走通**：`#59→#28→#66→#64 Authenticating→#2 PostFtueStart→#4 PostFtueSetup→#11 CreateAnonymousAccount→#13 PlayGame→#65 FetchingSaveData`；**客户端自己发了 `/auth/gplus`** ⇒ 会话建立 ✓；`SaveManager::Load` 命中 1 次；再无请求 |
> | IV 20:47 | 同上 + 点掉「网络错误」弹窗 | 点击后 45 秒进程消失（**无崩溃签名**），疑被杀/自杀 |
> | V 20:56 | 城镇奖励端点只回最小 body + 25/50/75s 三次点击 | 行为**逐字相同** ⇒ 响应形状不是卡点；点击无效（游戏不处理输入） |
> | VI 21:02 | 城镇奖励端点改用**客户端自己的 `characterSave` 格式** | 行为**逐字相同** ⇒ 同上 |
> | VII 21:13 | 在设备上摆 8 个候选路径的本地存档文件 | 行为**逐字相同** ⇒ 路径/格式至少有一个不对（存档路径见第五节） |
>
> ### 二、★ 洞 B 的三个 bug（本轮修掉，19:16–19:59 六轮白跑的原因就在这）
> 1. **控制流被写成了无条件 `b skip`**：旧版本 +48 处是 `b_code[12] = 0xEA000000`（无条件跳走）⇒ 洞 B **每次都直接跳过**，从来没发过任何触发器。
>    （不是挂点死了：本轮用 `TMU` 探针实测 `MainThreadDispatcher::Update` **一直在被调用**。）
> 2. **取触发器写成了 `add r4,r0,r2`**（算出的是**槽地址**）⇒ 正确写法是照抄游戏自己的 `ldr r4,[holder,#offset]`
>    （游戏在 `FlowState::StartAuthentication` 0x801A04 就是 `ldr r3,[r1,#0xa8]`；holder = 静态块 `[0x5c]`）。
> 3. **栈没 8 字节对齐**：`push {r0-r3,lr}`(20B) + `sub sp,#12` ⇒ sp ≡ 4 (mod 8) ⇒ 改成 `push {r0-r3,r4,lr}`(24B)。
>
> ### 三、★★ 本轮最硬的两个技术事实
> 1. **x86 模拟器上的 ARM 转译层：代码段只能 pc-relative 读**。
>    把台阶表放进 `.so` 的代码段再用 `ldr r2,[r2,r3,lsl #2]` 取 ⇒ **直接 SIGSEGV**
>    （实测 20:24:37：`fault addr 00a7baac` = 表首项，`pc 0xA7BA74`）。
>    ⇒ 改成「台阶字节直接存触发器偏移 + `ldr r4,[holder,X]`」，全程不读代码段 ✓。
> 2. **触发器身份可以由地址反查**：TFI 探针打印 `(触发器对象地址, holder 地址)`，而
>    **对象地址(X) = holder[0] − 4X**（X = FlowTrigger 静态偏移）⇒ 拿 `0xa4` 校准一次就能把每条日志换算成
>    具体触发器（`0xa4` Authenticate / `0xac` FetchSaveData / `0x5c` OpenMenu_BlockingWithPauseAndBlur / `0x30` PlayGame / `0x1c` CreateAnonymousAccount …）。
>    ⇒ **不需要再猜客户端发的是哪条触发器**。
>
> ### 四、认证链已打通（可复现）
> 洞 B 台阶 `0xa4`（Authenticate）→ `0x04`（PostFtueStart）→ `0x0c`（PostFtueSetup）→ `0x10`（PlatformSignIn）
> ⇒ 客户端接着自己发：`0x1c` CreateAnonymousAccount → `0x30` PlayGame → `0xac` FetchSaveData。
> 认证请求体：`{"details":{"user_id":1,"ticket":"1","channel":"gplus_cn"},"platform":"gp","language":"cn","deviceId":""}`
> （来自 `PlayerPrefs "AndriodLoginStatus"=1` 的快捷路；`AuthenticationAndroid.Authenticate` 等 4 个探针仍为 0 ⇒ 它没走 SDK 登录）。
> 我们的响应 `{"session":{...},"requestIndex":0}` 被接受 ✓（随后客户端自己发了 `0x30`）。
>
> ### 五、★ 剩下的卡点（下一轮从这里继续）
> - 状态机停在 `#65 FetchingSaveData` → `SaveManager::Load`(0xFFF2A0) → `SavedDataServiceImpl::LoadSavedData`(0x15964B4)（**TSD 命中 1 次** ✓）
>   ⇒ **取存档走的是后端** ✓；但**一个 HTTP 请求都没发** ⇒ 它在构造请求前就退出 ✓（最可能：**还没有角色 id** ✓ —— `GetAllCharacters`(TSG) 全程 0 命中 ✓）。
> - ★ **本地文件那条路从未被调用**：`SaveDataLocalStorage::Load`(0x1594340) 与 `JsonManager::GetSavePath`(0x112F990) **全程 0 命中** ✓
>   ⇒ 之前「往设备上摆存档文件」的方向整个是错的 ✓（VII / X 两轮的 32 个候选文件无效是**符合预期**的 ✓）⇒ **不用再纠结本地存档路径/格式** ✓。
> - 「网络错误」的来源：VIII 轮 logcat 出现 `Curl error 35: Handshake failed`（自签证书主机名不匹配 ✓）
>   ⇒ 已用同一张 CA 重签 `server.crt`（SAN 换通配符 ✓）；`server.py` 已加 **SNI 日志** ✓。
> - ⇒ **下一轮的两个抓手**：
>   ① **找出 `LoadSavedData` 早退的原因**：它在 0x1596584 按 key 从**接口实现表**（`[iface+0x58]`，16 字节步长）里找槽，
>      命中就 `blx` 槽里的函数指针（0x15965f8）⇒ 槽是**运行时表** ✗ 静态定位不到 ⇒ **用探针打候选函数**
>      （`CharacterService` / `BackendCommunicationService` 的请求入口 ✓）。
>   ② **让后端把角色准备好**：客户端已经建过匿名账号（状态 `#11` ✓）但没去要角色列表 ⇒ 可能在等角色 id ✓ ⇒
>      看 `CreateAnonymousAccount` 那条路本该发哪个请求（metadata 里有 `CreateNewSaveData` / `OnNewSaveCreated` ✓），
>      我们的 `/api/*` catch-all 已经能应答 ✓（但该请求**没来** ✓ ⇒ 要先知道它为什么没发 ✓）。
> - 已排除：域名漏网 ✓；城镇奖励响应形状 ✓；bundle 下载 ✓（文件服务器现在还能直接从 APK 提供任意 bundle ✓）。
>
> ### 六、别再踩的坑（本轮新增）
> * **脚本自检必须逐条断言 `op_str`，不能只断言助记符**：本轮 `cmp r2,#0` 被编成 `cmp r0,#0`、`mov r0,r4` 被编成 `mov r4,r0`，
>   两次都**静默**通过（助记符对、操作数错）⇒ 现在每条关键指令都有 `assert dis(...) == ('cmp','r2, #0')`。
> * **分支目标的语义也要断言**：旧版本把「越界就停」写成无条件 `b` ⇒ 洞 B 永远跳过。
> * 每轮跑完**先看截图**：本轮是截图才发现「已经到标题画面 + 网络错误弹窗」，日志里完全看不出来。
> * 跑测脚本现在会在每个截图点打 `free -m`、支持 `TAP="x,y" TAP_AT="25 50 75"`（点之前会确认前台是游戏）。
>
> ### 六之二、当前设备与构建状态（可直接接着跑，不用重新 build）
> * 设备上的 `libil2cpp.so` md5 = `3fd4e7c2e111204050e575590e39def4` = 本地 `build/libil2cpp.c.so` ✓
>   ⇒ 设备跑的**就是当前源码**（洞 B 台阶 `0xa4→0x04→0x0c→0x10`、挂 `FlowManager::Update`、10 个探针）。
> * 逐轮归档在 `build/history/libil2cpp_c_*.so`（20:23 / 20:31 / 20:41 / 21:22 / 21:59 / 22:0x）✓。
> * 后端可用形态：`ROUTES=shapes/R_auth_savegame.json`（城镇奖励端点用客户端自己的存档格式，其余用合并体）；
>   `shapes/R_auth_townmin.json`（城镇奖励只回 `{requestIndex:0}`）；`shapes/R_auth.json`（原合并体）。
> * ✅ 设备上的 bundle 已**完整还原**（2963 个 ✓）；试验用的存档候选文件**已清理** ✓。
> * ★ Unity 在 Android 上的 **persistentDataPath 是外置目录**
>   `/storage/emulated/0/Android/data/<pkg>/files/` ✓（`Unity/`、`il2cpp/`、`Bundles/` 都在这）。
> * ⚠ **别在游戏的数据目录里按大小删文件**（bundle 与 `global-metadata.dat` 都在那儿 ✓ 本轮误删过一次 ✓）。
>
> ### 七、最小阅读清单
> 本文 → `plan/blades-flow-trace-breakthrough-20261029.md` **4I 节（本轮四轮完整证据）+ 4G/4H 节**
> → `memory/blades-reverse-engineering-assets.md` 第 10/11 节。工作目录 `Library/Locus/tmp/blades/`；
> 跑测 `run_ca.sh`（`INSTALL/CLEAR_SAVE/PREFS/ROUTES/WAIT/TAP/TAP_AT`）。
>
> ---
>
> ## ⚠【已被上面 21:10 版取代，以下保留作历史】续跑简报（2026-10-30 20:20 版）
>
> **一句话**：客户端现在**不需要洞 B 也能自己走到 `Authenticating`**（靠 PlayerPrefs `AndriodLoginStatus=1`），
> 然后就停在那儿；而**洞 B 在当前构建里没有任何执行证据** ⇒ 下一轮第一件事是「证明挂钩点活着」，不是继续加逻辑。
>
> ### 一、本轮七轮实测（AA→GG，19:08–19:59）：后六轮逐字相同
> | 轮 | 条件 | TIE | TSF | 后端请求 | 停在哪 |
> |---|---|---|---|---|---|
> | AA 19:08 | 老洞 B（台阶 `0xa4`→`0xac`）| **7** | **1**（三个 flag 全 0）| 4 条（多一条 `analytics/stats/client`）| FetchingSaveData 后回退 InitializingBundles |
> | BB–GG 19:16/19:27/19:37/19:44/19:52/19:58 | 当前洞 B（每帧发 `0x04`）| **4** | 0 | 3 条：MetaManifest、host_info、`POST /characters//towns/current/rewards/current` | **Authenticating** |
>
> TIE 的 4 次（用 TCO 序号换算，`序号=(首地址−地址)/0x28`）= `#59 LoadStartupScene → #28 InitializingBundles → #66 LoadingBaseGameContent → #64 Authenticating`。
> 19:59:11.369 那条 `TFI a11a8c50` 是**客户端自己**发出的 `Authenticate`（洞 B 死了，只能它自己；这就是 4G.11(2) 那条 `AndriodLoginStatus=1` 的效果 ✓）；
> 19:59:11.372 `TFI a11a8d70` 是客户端进 `Authenticating` 后发的下一个触发器，**在该状态无迁移** ⇒ 被 `OnUnhandledTrigger` 静默丢弃（4G.7 的老现象，换了触发者后重现）。
>
> ### 二、★ 本轮唯一的新结论：洞 B 的挂钩点 90% 是死的
> 洞 B 挂 `MainThreadDispatcher::Update`(0x1096484)：若每帧被调，140 秒该打出**上千条** TFI；实测**全程 7 条**，且每条都能对上启动期/客户端自己的发射 ⇒ **一条都没跑**。
> 静态解释（吻合但未证实）：`MainThreadDispatcher` 是 `SingletonBehaviour<MainThreadDispatcher>`，`Update` 是**实例方法** ⇒ 组件没被创建就不会被 Unity 调用；它又是懒加载单例 ⇒ 没人取过 `Instance` 就不存在。
> ⚠ `dump.cs` 只给声明不给调用点 ⇒ **静态无法证伪**，必须用探针验。
> ⚠ 因此「只留洞 A ⇒ TIE 3」「洞 B 是承重的」这两条旧结论**必须在同一条件下重做**（当时 `PREFS` 变量没控制干净）。
> ⚠ 同时核实：本轮 `TPA/TPC/TPB/TPL`（平台登录/后端认证 4 个探针）**全 0** ⇒ 客户端从头到尾没调过 `AuthenticationAndroid.Authenticate`。
>
> ### 三、下一轮（一次 build 拿三个答案）
> 1. **洞 B 换挂点**：`FlowManager::Update`(**0x7FC978**，状态机宿主单例，必定每帧被调，且在状态机调用栈**之外** ⇒ 不会重入崩溃）。
>    ★ 好消息：它的序言是 `E92D4C10`，与现在的 `MainThreadDispatcher::Update` **逐字节相同** ⇒ 只需改 `HOOK_B` 一个常量，cave 结构与自检全部不用动。
>    `bl` 的目标仍是 `FlowTrigger::Fire`(**0xA7381C**，签名已确认 `void Fire()`）。
> 2. **腾探针位**（洞区只剩 8B）：删掉长期为 0 的 `TSD/TSR/TSG`（和 `TPA/TPC/TPB/TPL`），
>    加 **TMU = `MainThreadDispatcher::Update` 0x1096484**（回答「老挂点到底有没有跑」）。新挂点活性由洞 B 的发射日志自证（TFI 应变成每帧一条）。
> 3. **台阶回到 `[0xa4, 0xac]`**（`Authenticate` → `FetchSaveData`），stage 字节仍用 **0x121**；
>    若又只走一步 ⇒ 直接上第四节的无状态注入。
>
> ### 四、备用方案（推荐尽早做）：无状态注入，不再依赖 stage 字节
> `FlowManager` 字段：`_processingTrigger`(0xC)、`_nestedTriggers` 队列(0x10)、`get_State()`(**0x7FBD60**)。
> ⇒ 洞 B 可以直接读**当前状态对象指针**，和 `FlowState` 静态块里那 86 个状态对象比一比，再决定发哪条出边
> ⇒ **不需要任何自存状态**，天然支持循环，也不会被游戏覆盖字节。状态对象地址表见 `flowstate_fields.md`（TCO 序号 = 行号）。
> （另：在 `_processingTrigger==true` 期间调 `FlowManager::Fire` 只会进 `_nestedTriggers` 队列 ⇒ 这是游戏自己的安全重入机制。）
>
> ### 五、本轮新踩/新确认的坑
> * **A/B 对照必须彻底杀进程**（`am force-stop` + `killall -9`，并打印新旧 pid）——`am start` 会复活同一 pid 带着旧内存状态。
> * **logcat 过滤必须用 `" I (TAG1|TAG2) +: "`**（tag 补齐 8 字符），用 `": "` 会一条都匹配不到。
> * ★ **`run_ca.sh` 的 `TAGS` 汇总表已与 `build_vtrace.py` 的 `HOOKS` 脱节**：汇总里列了 6 个**不存在**的 tag（TSC/TSB/TSH/TCH/TCA/TERR），
>   却漏掉真正在跑的 `TFI/TPA/TPC/TPB/TPL` ⇒ **看汇总表会得出「TFI=0」的错误印象**。看命中一律以 `grep -oE " I [A-Z]{3} +: " log_vtrace2.txt` 为准。
> * 每轮 **APK 不要覆盖**：`build/blades_c.apk` 被 19:57 那次 build 盖掉了 runAA（最后一个能复现 TSF 的版本）。
>   ⇒ 以后在 `build_c.sh` 末尾加 `cp build/blades_c.apk build/history/blades_c_<轮次>.apk`。
> * 洞区布局（现状）：洞 A `0xA7B9EC`(64B) / 洞 B `0xA7BA30`(172B，止于 `0xA7BADC`) / 探针区 `0xA7BADC..0xA7BDFC`，**只剩 8B**。
>
> ### 六、最小阅读清单
> 本文 → `plan/blades-flow-trace-breakthrough-20261029.md` **4H 节（本轮证据）+ 4G 节（认证链 / 后端契约）**
> → `memory/blades-reverse-engineering-assets.md` 第 10/11 节。工作目录 `Library/Locus/tmp/blades/`；跑测 `run_ca.sh`（`INSTALL/CLEAR_SAVE/PREFS/ROUTES/WAIT`）。
>
> ---
>
> ## ⚠【已被上面 20:20 版取代，以下保留作历史】续跑简报（2026-10-30 深夜 19:0x 版）
>
> **一句话现状**：流程已能走到 **FetchingSaveData** 并**触发存档回调**
> （`TSF` = `OnSaveDataFetched` 已命中）；**后端响应现在 0 异常**（格式/类型问题全部解决）。
> **当前卡点**：存档回调的三个标志全 false ⇒ 客户端在**进入 FetchingSaveData 的那一瞬**就判定「没存档」，
> **连本地存档都没查过**（`SaveManager.Load` / `HasValidSave` / `CreateNewSave` 探针全是 0）。
> ⇒ 根因：**客户端没有后端会话**（`session._loginType` 一直是 `None`）。
> ⇒ ★ **本轮的最大进展**：认证链已完整测绘，且找到了平台登录的开关
> —— `AuthenticationAndroid.GetLoginStatus()` = `PlayerPrefs.GetInt("AndriodLoginStatus", 0)`
> （游戏自己拼错的名字）⇒ **写 1 = Success** ⇒ 实测客户端**自己**发出了 `Authenticate` 触发器。
>
> ### ★ 本轮（2026-10-30 深夜）推翻的两条旧结论
> 1. **「洞 B 是空操作、可以摘掉」是误读** ⇒ **洞 B 是承重的**。
>    AAPCS 里**第 4 个参数槽是 r3**、第一个栈变长参数在 **[sp+4]** ⇒ 旧洞 B 写的 `str r2,[sp]`
>    并没有把触发器送出去，**日志里那个 `0x09096484` 是没被赋过值的 r3**（残值）。
>    把洞 B 的日志改成 `mov r3,r4` 后，它打出的是**真触发器**（和启动期那批触发器同一个分配块）✓
>    ⇒ 洞 B 就是把流程推进到 `Authenticating` 的那一脚。
>    **实测「只留洞 A」：TIE 只有 3 次 ⇒ 流程停在 `LoadingBaseGameContent`，到不了 `Authenticating`** ⇒ 洞 B 要留着。
> 2. **「FormatException 已修好」只是换了个异常**（现已全部解决）：
>    旧：session/featureStatus（UInt32）给空串 ⇒ FormatException（已修，0 条）；
>    再：id/uid/requestIndex 被直接 cast 成 long ⇒ `InvalidCastException: String → Int64`（已修）；
>    再：`characters` 这类 `{id}` 集合的**形状**不对（要数组还是要字典）⇒ `List → Dictionary` / `Object → Array`（已修）。
>    ⇒ **现在的完整响应体：0 异常** ✓
>
> ### ★★ 客户端解析响应的两条路（要求相反，.schema 不可信）
> | 路径 | 入口 | 值的要求 | 异常 |
> |---|---|---|---|
> | 转换路径 | `.conversion` → `OperationWrite.WriteValue` → `CreateOrUpdatePrimitiveTyped<T>` | **字符串**（它自己按字段类型再解析）⇒ 数值字段给**合法数字串** | `FormatException` |
> | 直接 cast 路径 | `SequentialParser.Parse` → `AssignPersistentValue<T>` | **原生类型**（数值要 JSON number、布尔要 true/false） | `InvalidCastException: String → Int64` |
> ⇒ 顶层用字符串、**嵌套 JSON（CharacterSave/extraData）用原生类型**；
> ⇒ **`{id}` 集合逐个试**：`characters`/`chests`/currencies 要**数组**，`inventory`/`wallet`/`BackendCharacterListData` 要**字典**。
>
> ### 已经成立的事实（都已实测）
>
> | # | 事实 |
> |---|---|
> | 1 | 流程卡点的**唯一出口**是 `LoadingBaseGameContent --Authenticate--> Authenticating`；`Authenticate` 在启动路径上**没有任何发射点** ⇒ **必须由我们发**（洞 B）|
> | 2 | ★ **置 `FlowState._menuInAnimationCompleted`(0x120)=true`（洞 A）单独不足以推进流程**（实测：只留洞 A ⇒ TIE 停在 3 次）|
> | 3 | ★ **洞 B 发的是真触发器**（修好日志后打出 `a11f4c50`）⇒ 洞 A + 洞 B 合用才能进 `Authenticating` |
> | 4 | ★ **客户端解析响应有两条路，对值的要求相反**：<br>① **转换路径**（`.conversion` → `OperationWrite.WriteValue`）把**字符串**按字段类型解析 ⇒ 数值字段要给**合法数字串**（空串 ⇒ `FormatException`）；<br>② **直接 cast 路径**（`SequentialParser.Parse` → `AssignPersistentValue<T>`）按 `.schema` 直接 cast ⇒ 数值字段必须是 **JSON number**（字符串 ⇒ `InvalidCastException`）|
> | 5 | 状态名可用 `TCO`(`FlowState::.ctor`) 探针定：地址每 **0x28** 递减，**序号 = (首地址 − 地址)/0x28**，序号即 `flowstate_fields.md` 行号（实测 59/28/66/64 全对上）|
> | 6 | **PC 基准：两种 `ldr` 形式都是 insn+8**（`patch_b.py` 的 +4、`patch_n.py` 的 0x224B854 都错）|
> | 7 | **取类指针链必须原样照抄**（含 `ldr r0,[pc,r0]`）；搬运只重算池字 `V' = V + (A2_old - A2_new)` |
> | 8 | 补丁洞必须在 `build_vtrace.py` 预留的 `0xA7B9EC..0xA7BABC` 区（洞 A 60B + 洞 B 146B，探针从 0xA7BABC 起）|
> | 9 | ⚠ **过滤 logcat 打点要用 `" I (TAG1|TAG2) +: "`**（tag 补齐 8 字符！）用 `": "` 过滤会一条都匹配不上 |
>
> ### 后端契约（已挖出，见 flow-trace 4F/4G 节）
>
> - 响应体是「**路径 → 值**」的扁平 JSON，**无外层信封**；映射表是 **TextAsset**：
>   **19 个 `.schema` + 15 个 `.conversion`**（7 个是 0 字节的空转换），
>   只存在于 **`assets/bin/Data/sharedassets0.assets`**。
>   ⚠ **抽取必须按 4 字节对齐解析 TextAsset 的字符串**（`extract_contracts.py`，旧脚本读不对齐 ⇒ 长度是垃圾）。
> - **转换链**：`extract_contracts.py` → `build_api_responses.py` → `build_routes.py` → `server.py`（支持 `{0}` 模板路由）。
> - **类型校验器**：`check_response_types.py`（把响应体按每个 `.schema` 逐路径校验 JSON 类型）。
> - **二分工具**：`shapes/R_probe.json`（最小 body）/ `shapes/R_empty.json`（空 body）⇒ 换 `ROUTES=` 即可跑对照。
> - 调试黄金通道：**客户端的异常在 logcat 的 `E Unity` 里**（Debug.Log 不进 logcat，但异常进）。
>
> ### ⛔ 硬规则（本会话踩出来的事故）
> **禁止盲点击**。曾在前台不是游戏时用 96 个盲点扫全屏 ⇒ 打开无关 APP 并**误卸载了用户设备上的 `com.qy.mtjq.ld`**。
> 任何 `input tap` 前必须确认前台是游戏，且坐标只能来自游戏自己的事件流（`build_vtap2_cave.py`）。
>
> ### 下一步（按性价比）
> 1. **让客户端走完认证链**：认证链是
>    `Authenticating --0x04--> AuthenticationPostFtueStart --0x0c--> ...PostFtueSetup --0x10--> AuthenticationPlatformSignIn
>    --0x30--> AuthenticationPlayGame --0xac--> FetchingSaveData`
>    ⇒ **每帧发 `AuthenticationPostFtueStart`(0x04)**（`Authenticating` 的合法出边）就能把它推进去；
>    ★ 注意：洞 B 现在**一次都没发过** ⇒ 连洞 A（`SetupGameManagers`）都没跑到 ⇒
>    ⇒ 先用探针确认 `SetupGameManagers` 是否被调用；必要时把挂钩点换成「客户端进 `Authenticating` 的那一刻」
>    （可以用 `InvokeEntered`(TIE) 探针探测状态变化）。
> 2. **用 IL2CPP 运行时 API 直调认证**（`il2cpp_runtime_invoke` 调 `AuthenticationServiceImplementation.Authenticate()`，
>    无参版用它自己注册的回调 ⇒ 不用自己造 delegate）—— `libil2cpp.so` **导出了 209 个 `il2cpp_*` API** ✓。
> 3. 拿到会话后客户端会去取角色列表（`GET /characters`）⇒ 响应里角色已给；
>    ⚠ `b__222_77` 会把「同步成功但没角色」判成 `CharacterNotCreated` ⇒ 要确保它真拿到角色。
> 4. `BLADES_FORCE_AUTH=1` 可临时强制 `IsAuthenticated()` 返回 true（实测**目前无效**，只作复查用）。
>
> **最小阅读清单**：本文 → `plan/blades-flow-trace-breakthrough-20261029.md` 的 **4E/4F/4G 节** → `memory/blades-reverse-engineering-assets.md` 第 10 节。
>
> ### ⚠ 本轮定案的三个编码事实（之前两轮都栽在这里）
> 1. **寄存器偏移式 `ldr rX,[pc,rY]` 的 PC 基准 = 指令+8**（不是 +4）。
>    ⇒ `patch_b.py`（修法 B）差 4 字节；`patch_n.py` 的 FlowState 槽写成 0x224B854（正确 **0x0320B854**）
>    ⇒ **修法 N 其实从未被真正证伪**（那一轮只是往随机地址写了个字节）。
> 2. **「取类指针链」必须原样照抄**（含 `ldr r0,[pc,r0]` 这种 Rd==Rm 的形式）；
>    改成「安全写法」`ldr r1,[pc,r0]` 会取到**别的类**（本轮实测：发出去的对象不是触发器，当场崩）。
>    搬运时只需把池字按指令位移差重算：`V' = V + (A2_old - A2_new)`（与 PC 基准无关）。
> 3. **补丁洞必须放在 `build_vtrace.py` 预留的 0xA7B9EC..0xA7BABC 区**（探针洞从 0xA7BABC 开始），
>    否则会覆盖 TERR 探针 ⇒ `Debug::LogError` 跳进野代码 ⇒ **启动 1 秒后崩**。
>
> ### 现在的战场：后端契约（已挖出 ✓）
>
> **★ 后端要回什么，已经从 APK 里挖出来了**（详见 flow-trace 文档 4F 节）：
> - 客户端把响应当「**路径 → 值**」解析，映射表是 **TextAsset**：17 个 `.schema`（响应体形状）
>   + 若干 `.conversion`（JSON 路径 → 持久化路径），**只存在于 `assets/bin/Data/sharedassets0.assets`** ✓
>   （`assets/Bundles/*` 与设备上下载的 1482 个 bundle 里都没有 ✗）。
> - ⚠ **UnityPy 读不出这批资产的正文** ⇒ 手工按 TextAsset 布局解析（两个 length-prefixed 字符串）✓；
>   脚本 `extract_schemas_apk.py` / `build_api_responses.py`（在 `Library/Locus/tmp/blades/`）。
> - **响应体没有外层信封** ✓，直接就是 schema 的根键（例：`{"session":{…}}`）；`requestIndex` 要回显请求头 ✓。
> - ⚠ **占位值必须按 schema 类型给**（数值型给 `""` 会抛 `FormatException`，
>   栈：`OperationWrite.WriteValue` → `Operation.Exec` ⇒ **logcat 的 `E Unity` 里能直接看到这类错** ✓✓）。
> - **通用响应策略**：把所有 schema 的根键合并成一个 JSON 对 `/api/*` 一律返回 ✓
>   （`build_api_responses.py` → `build_routes.py`）。
> - ⇒ **当前阻塞**：客户端在 `Authenticating` 里请求 `POST …/characters//towns/current/rewards/current`（**角色 id 为空**），
>   拿到响应后只发一条 `POST /api/analytics/v1/public/stats/client` 就静默；
>   `OnSaveDataFetched`/`CloseLoadingMenu`/`PlayOutAnim` 探针**全 0 命中** ⇒ **存档回调没跑**。
>
> ### 新增工具（全部纯静态，不烧 build 周期）
> `build_transition_table.py`（迁移表，177 条）、`extract_transitions2.py`、`scan_fire2.py`（触发器发射点全表）、
> `patch_c_fire.py`（修法 C）、`build_c.sh`（打包）、
> **`extract_schemas_apk.py` + `build_api_responses.py` + `build_routes.py`（后端响应生成链）**、
> `tap_probe.sh`（「是在等数据还是在等点击」的判定实验）。
>
> ---
> ### 🕰 旧续跑简报（上下文被压缩后、或换新对话时，直接看这里 ★★★）
>
> **新对话开场白建议**（2026-10-30 21:50 更新）：
> 「继续《上古卷轴：刀锋》离线化逆向。
> 先读 `Locus/knowledge/plan/blades-handoff-20261029.md` **顶部的 21:50 续跑简报**（七节：现状 / 模型修正 / 过滤定位 / 认证链 / 工具与事故 / 下一步顺序 / 阅读清单），
> 再读 `plan/blades-flow-trace-breakthrough-20261029.md` 的 **4K 节**（本轮证据）与 **4J 节**（洞 B 修好那轮）。
> 工作目录：`Library/Locus/tmp/blades/`（脚本、构建、日志都在里面，**不要重建，直接跑** `run_ca.sh`）。」
>
> **任务**：让已停服手游《上古卷轴：刀锋》(Blades, Unity 2019.2.16f1 / IL2CPP / ARM32) 的安卓包离线可玩。
> 只改 `Library/Locus/tmp/blades/` 里的脚本与产物；**不要动 `Assets/`**；Unity 完全不相关。
>
> **已确立的根因（九轮打点实证）**：
> 流程链 = `LoadStartupScene → InitializingBundles → **LoadingBaseGameContent**`（第三个卡住）。
> 其 transition handler `b__222_223`(0x10E94C4) 打出 `Debug.LogError("zhang  InitializingGame show")`，
> 然后以 **additionnalData = null** 调 `MenuRequestManager::OpenMenu`(0x9CCB94)：
> ```
> 0x10E95D0  mov r2, #0
> 0x10E95E0  bl  0x9CCB94
> ```
> ⇒ `AbstractLoadingMenu::OnOpen`(0xB1852C) 在 `0xb18588 popeq` 提前 return
> ⇒ `PlayInAnim()` 不执行 ⇒ Animator 不播
> ⇒ `OnInAnimDoneEvent`/`OnOutAnimDoneEvent`(0xB188D4/0xB18914) 因 `_context==null` **抛异常**
> ⇒ `FlowState._waitForMenuOutAnimationTaskControl`(静态 0x11C) 的完成信号永不到达
> ⇒ 流程到不了认证（`Authenticate` 0x11A1C64 从未被调用，`/auth/gplus` 从未发出）。
>
> **三条修法的状态**：
> | | 做法 | 状态 |
> |---|---|---|
> | N | 把 `_menuInAnimationCompleted`(静态 0x120) 置 true | **已实测证伪**（不崩但流程不动）⇒ 等待点是 TaskControl 的完成信号 |
> | B | 自己造带回调的 `LoadingMenuContext` | cave 写完（`patch_b.py`，205B，带 44 条指令 + 6 项寄存器方向 + 分支目标自检），**卡在 4 字节 PC 基准**：寄存器偏移形式 `ldr Rx,[pc,Ry]` 的 PC=**指令+4**，立即数形式 `ldr Rx,[pc,#imm]` 的 PC=**指令+8** |
> | B2 | **原地复用**：`b__222_223` 的 `bl OpenMenu` 改成 `bl FlowState::OpenLoadingMenu`(0x8007C4) | 方向正确（池字全由原函数算），但 `OpenLoadingMenu` 在那个时刻 NRE（0x800888 `get_Instance()` 返回 null） |
>
> **下一轮的确切动作（一轮 build 拿两个答案）**：
> 1. 把 `0x10E95E0` 的 `bl 0x9CCB94` 改成 `bl 0x8007C4`（原地复用）；
> 2. 在 `OpenLoadingMenu` 内部的 `0x800874`（那个 `get_Instance()` 调用点）挂 cave 打 r0 → **告诉我们是哪个单例为 null**；
> 3. 分支：若那个单例只是“当时还没就绪” ⇒ 把调用挪到 `FlowState::SetupGameManagers`(0x803414, :42.5 所有管理器就绪) 那个时点；
>    若是 TaskControl ⇒ 它只能由任务链调用 ⇒ 回到“哪个触发器能进那个 transition”；
> 4. **关键线索**：`FlowState::PrepareLoadingScreenTaskList`(0x8003D4) 只被 `b__222_189`/`b__222_193` 调用，
>    而这两个 transition handler **从未被触发** ⇒ 游戏本来有一条“准备加载屏”的任务链，但它所在的 transition 从未发生。
>    迁移表已抽出（`extract_transitions.py`：180 处 `config.Add` → 80 条状态对），**只差把 (状态对) 与 (触发器) 对上**。
>
> **关键文件**（`H:/Project/threeKingdomSlayer/threeKingdomSlayer/Library/Locus/tmp/blades/`）：
> `build_vtrace.py`（多点打点器，六种取值模式 + 布局自检）、`run_round.sh`（标准跑测）、
> `patch_b.py`（修法 B 的 cave）、`extract_transitions.py`（迁移表抽取）、`build_b.sh`、
> `flowstate_fields.md`（86 个 FlowState 静态字段；**TCO 打点序号 = 行号**）、
> `log_vtrace2.txt`（最近一轮原始日志）、`classes.dex`（已含 patch_v49 + patch_sdk_float）。
> 详细证据见 `plan/blades-flow-trace-breakthrough-20261029.md` 的 4A/4B/4C/4D 节。
>
> **环境坑（每条都踩过）**：
> * **模拟器必须用 `schtasks` 拉起**（或手动）：在 bash 调用内部 `ldconsole launch` 会让 VM 进程成为本次调用的子进程，
>   Locus 每次调用结束回收整棵进程树 ⇒ 模拟器跟着死 ⇒ 每轮重开。
>   `schtasks /create /tn LdLaunch /sc once /st <HH:MM> /tr '"E:\leidian\LDPlayer4\ldconsole.exe" launch --index 0' /f`
>   ⇒ VM 的父进程变成 `LdVBoxSVC.exe`（服务），能活过调用边界。
> * **adb server 也会被回收** ⇒ 一切（起服 + `adb reverse` + 拉客户端 + 抓日志）必须在**同一次调用**内完成。
> * **logcat 要持续抓**（`logcat -v threadtime > file &`），事后 `logcat -d -t N` 会把启动阶段丢掉（噪声多）。
>   打点格式是 `I TAG     : value`（tag 补齐 8 字符），过滤用 `" I (TAG1|TAG2) +: "`。
> * 启动组件是 **activity-alias** `com.shengqugames.blades.MainActivity`；`-n` 指定真类会报 does not exist。
> * 模拟器重启后**运行时授权会丢**，跑测脚本里已 `pm grant` 7 项。
> * Unity 的 `Debug.Log/LogError` **不进 logcat**（本构建），只能自己从 `Il2CppString`（chars 在 +12，UTF-16LE）读。
> * ARM 编码：`ldr Rx,[Ry,#imm]` 的 **Rt 在 bits[15:12]、Rn 在 bits[19:16]**；`push/pop {…lr}` 的 **lr 是 bit14**；
>   `str sb,[sp]` 的 sb=r9 ⇒ `0xE58D9000`。
>
> **别再犯的错（我的）**：能静态回答的不要烧一轮 build 周期；机器码写完先反汇编自检再打包；
> 观测够了就切到打补丁；不要把“自包含 cave”当成默认形态——**先问能不能让游戏自己的代码原样跑**。


> **这份文档是给下一个会话的「开机读物」**：读完即可接续，不需要回看任何聊天记录。
>
> **2026-10-29 精简说明**：原文 20.8KB ≈ 14.5k tokens，一次 `read` 就会把请求推到 ~26k，小上下文模型容易报错。本篇只留**最小必要集**，完整表下沉到细节版 **`plan/blades-login-bypass-20261029.md`**（§3 偏移/RVA、§4B+§8.1 补丁全记录、§8.2 编码坑、§8.3 复跑硬约束）。

---

## 0. 一句话任务

让已停服的手游 **《上古卷轴：刀锋》(Blades)** 的安卓包在模拟器/真机上**离线可玩**（最终产出可安装的 APK），同时把其中值得借鉴的架构整理成文档（已产出 `design/blades-architecture-benchmark.md`）。

**边界**：仅个人研究用；抽取的美术/音频/资产表**不入库、不进 Unity 工程、不对外分发**；原始 APK 未被修改（sha256 `c49920c6…5698e7ae2`）。前期影子后端实现细节见 `plan/blades-offline-revival-plan.md`。

---

## 1. 现状阶段表（2026-10-29 实测）

| 阶段 | 状态 | 证据 |
|---|---|---|
| 静态逆向（metadata / 257 张设计表 / REST 契约 117 路径 165 DTO） | ✅ | `types.csv` `rest_spec.md` |
| 本地影子后端（TLS 拦截 host_info + MetaManifest） | ✅ | `requests.log`：3 个请求全 200 |
| Bundle 阶段（本地 bundle 直连 + 美术全加载） | ✅ | 标题画面截图（LOGO+城堡+角色）完整渲染 |
| **登录 / 取得会话** | ❌ **唯一阻塞点（第三阶段已定位到指令级 + 读到全部旁白 + 识别状态名）** | 打点实测：流程链 **`LoadStartupScene → InitializingBundles → LoadingBaseGameContent`**（第三个卡住）；其 handler `b__222_223` 打出 `Debug.LogError("zhang  InitializingGame show")` 并以 **null context** 打开加载屏 ⇒ `AbstractLoadingMenu::OnOpen` 提前 return ⇒ `PlayInAnim` 不执行 ⇒ 加载屏动画永不完成 ⇒ `CloseLoadingMenu` 从未执行 ⇒ 无 `/auth`。详见 plan/blades-flow-trace-breakthrough-20261029.md |
| 角色 / 城镇 / 任务 REST 闭环 | ⬜ 未开始 | 依赖会话；契约见 `rest_spec.md` |

客户端**进程活着、画面正常**，只是停在标题状态等一个永远不会来的触发器。

---

## 2. 为什么进不去（2026-10-29 晚间按实测修正）

**实测链条（每一条都有 logcat / 崩溃地址证据）**：

```
游戏启动 → AbstractLoadingMenu.OnOpen → ClearTip → SupportsTips → 【停住】
   ↳ 标题页没有可点元素（42+ 坐标扫描零命中），它是在“等”
   ↳ 等的是 SDK 登录 / 后端会话
        ↳ SDK 登录必须联网 → GHome.login() 同步请求已下线的服务器 → UI 线程阻塞 → “Activity pause timeout” 进程被杀
        ↳ 我们伪造的 Unity 登录消息：已送达、C# 已解析并写入 TicketStr/UserStr，
          但 GHomePostManager.LoginCallBack 订阅者为 null；FlowTrigger/FlowState 静态从未初始化
          → 游戏的流程状态机根本没被创建
```

**已修正的三点**：
- ① SDK 初始化是**成功**的（`code=0 msg=初始化成功`，C# `Ghome init is 0`）。`NoClassDefFoundError: Lcn/uc/gamesdk/R$layout;`
  只是悬浮窗创建那一处崩溃，已定位到唯一引用点
  `Lcn/uc/gamesdk/e/a$a;.<init>(Context)`（co=0xFBE4C，unit 7 `sget v1, R$layout;->loading_view:I`），
  **把它改成空实现后该异常降为 0 次** ✓。
- ② 该类在游戏 dex 里**只有引用没有定义**（其资源包 package `cn.uc.gamesdk` 从未下载；设备上两个 loader 包只有
  `cn.uc.gamesdk.lib.R$*` / `cn.uc.gamesdk.loader.R$*`）⇒ 不是“少一个类”，而是“少一整个资源包”。
- ③ 客户端**从未主动发起过登录**（历史日志里 `GHomeApiBase.login` 出现 0 次），所以服务器侧给什么响应都无关：
  `200 {}` / `401` / 业务错误码实测行为一致，永远不会请求 `/auth`。

- 「开始游戏」按钮是渠道 SDK 的 **Java 悬浮窗**（`cn.uc.gamesdk.floating_button`），不是游戏内 UI。
- `uiautomator dump` 证实：视图树里**只有 Unity 的 Game view**，没有任何 SDK 悬浮窗节点。
- **SDK 服务端其实还活着**（旧结论「SDK 已 404 下线」是错的）：`/v1/basic/publickey`、`/v1/basic/handshake`、`/v1/gchannel/basic/getGameChannelClientCfg?channelCode=G15` 全部有响应。
- SDK loader 模块运行时下载成功（8.2MB，含 `libcn.uc.gamesdk.floater.so` 与 `res/layout/*`），但**不含** `cn/uc/gamesdk/R$layout`（该 R 类属 package=cn.uc.gamesdk 的资源模块，缺失）。
- 完整 logcat 原文与排查过程见 `plan/blades-login-bypass-20261029.md` §2。

### 2.1 绕行方向（选定）

绕过 SDK，让**游戏自己**发起认证：游戏自身有匿名/设备登录路径
`AuthenticationServiceImplementation::Authenticate(userID, deviceID, …)` →
`POST /asia/api/authentication/v1/public/auth/gplus?userId=&deviceId=&platform=`。
本地影子后端**早已备好** `/auth/*` 会话响应，只要客户端发出这个请求，会话立刻到手。

---

## 3. 逆向成果索引（都在 `Library/Locus/tmp/blades/`，Unity 临时目录，不进 git）

| 产物 | 说明 |
|---|---|
| `dumper_out_v7a/dump.cs` + `script.json` | **ARM32**（设备实际跑的架构）完整 C# 头文件与地址表，25MB |
| `types.csv` `methods.csv` `fields.csv` / `tables/*.csv` | 14498 / 99958 / 62948 行 + **257 张设计数据表**（技能/敌人/任务/对话/掉落/配方/挑战…，做设计借鉴用） |
| `rest_spec.md` `rest_endpoints.csv` | REST 实现规格：117 路径模板 + 165 DTO + 最小可实现集 |
| `server.py` + `ca.crt`/`server.crt` / `file_server.py` / `shapes/R_auth.json` | 影子后端（TLS，日志型，路由表 `routes.json`）＋ 文件服务器（:8080）＋ **当前路由集**（含 `/auth/*` 会话响应） |
| `bundles_src/` | 本地 bundle 镜像（1482 个文件、1.1GB），推设备用 |
| `build_and_test_v*.sh` / `play_offline.sh` / `run_full_log.sh` | 补丁构建脚本（v5→v45）／一键起服+拉起客户端／抓全级别日志复现脚本 |

（`string_literals.txt`、`dis_v7a.py` 等扫描工具链、各轮截图与日志 `screen_*.png` `requests.log` `unity_all.log` 的完整清单见 `memory/blades-reverse-engineering-assets.md`）

### 3.1 第二阶段新增产物（2026-10-29 晚间，全部本地、无需下载）

| 文件 | 作用 |
|---|---|
| `dexlib.py` | 极小 DEX 读写库：表解析、class_data 遍历、code_item 读写、35c/21c 指令编码、`fix_map`、字符串原位覆写 |
| `dex_dump.py` | dex 方法反汇编（`refs <字符串>` / `meth <名字>`） |
| `patch_v49.py` | 在 `MainActivity.onResume` → `ReportData` 里构造嵌套登录报文发给 C#（**报文必须是嵌套 `data{...}`，平铺无效**） |
| `patch_sdk_float.py` | 把 `cn/uc/gamesdk/e/a$a.<init>` 的 inflate 段改为 `return-void`，**消除 `R$layout` 崩溃** |
| `patch_init_login.py` | 实验用：初始化回调里调 `MainActivity.Login()`（⚠ 会因 SDK 同步联网阻塞 UI 线程，不建议启用） |
| `build_v46.sh` / `build_v51.sh` / `build_vA*.sh` / `build_vtap*.sh` | 各类构建（dex 注入 / 原生 cave 钩子），均含 capstone 逐条自校验 + 跳转目标数值核对 |
| `run_v46.sh` / `run_v49.sh` / `run_live_check.sh` / `run_retrigger.sh` | 跑测脚本（起后端 + adb reverse + 拉起客户端 + 抓日志/截图/请求序列） |
| `ucsdk.dex`、`loader.odex`、`loader_v2.apk` | 从设备拉下来的 UC SDK 产物（20MB odex 的内嵌 dex 在 `ucsdk.dex`） |

### 3.2 本地自建的两条观测/注入能力（无网络依赖）

1. **Java→Unity 报文注入**：在游戏 `classes.dex` 里改写 `MainActivity.onResume` / `ReportData`，
   用现成字符串（`'userid'`/`'ticket'`/`'isGuest'`/`'1'`/`'true'`）拼出 JSON 并调用
   `MainActivity.sendUnityMessage(...)` / `UnityPlayer.UnitySendMessage(...)`。
2. **原生 logcat 打点**：在 `libil2cpp.so` 的 `GHomeSendManager::SwitchAccount`（0xA7B9EC，本局永不调用）当 code cave，
   用 libil2cpp 已导入的 `__android_log_print`（PLT=0x50F260）打印寄存器/内存值；
   配合 `input tap` 坐标扫描，可**机器可读地**回答“屏幕上有什么、点哪里有用”。
   ⚠ 坑：cave 内字符串指针必须用 `pc` 反推运行时地址（写 RVA 会跳到未映射地址）；`ldr` 的 imm12 是**字节**偏移。

---

## 4. 必记的偏移与结论（最小集）

### 4.1 触发器与状态（最小集）

- **首选触发器 `0x30 AuthenticationPlayGame`** → 迁移到 `FetchingSaveData`；次选 `0xC0 StartGame`、`0x00 AuthenticationPreFtueStart`、`0xB0 FetchSaveDataFromNoUiAuthentication`。
- **不要点 `0x18 AuthenticationAnonymousLogin`** —— 迁移表里它不指向任何迁移（前几版点的就是它，所以「不崩但也没反应」）。
- **进游戏的状态链 = 触发器 → `FetchingSaveData` → `OnSaveDataFetched` → 认证 → 角色**。
- 完整表（90 个 FlowTrigger 偏移 / FlowState 状态偏移 / 27 条状态机迁移）见 `plan/blades-login-bypass-20261029.md` §3.1、§3.7、§8.4。

### 4.2 关键函数 RVA（v7a，常用）

| 函数 | RVA |
|---|---|
| FlowTrigger::Fire | 0xA7381C |
| FlowState::StartAuthentication | 0x801988 |
| FlowState::StartupLoadComplete | 0x8024F8（探针实证：流程里第一个被调用的函数） |
| FlowState::StartupLoadCompleteAndBackendAvailable | 0x8026DC（晚期钩子候选） |
| FlowManager::Update | 0x7FC978（`mov r0,r4`@0x7FC994；`pop`@0x7FC99C） |
| FlowManager::ReceiveInput | 0x7FD110（晚期钩子候选） |
| MainMenu::OnLogoPressed / Update | 0x9DA004 / 0x9DA24C |
| AbstractLoadingMenu::OnInAnimDoneEvent | 0xB188D4 —— ⚠ **bundle 加载界面就触发，不是标题画面**（v40 实证），别在这下钩子 |
| AbstractLoadingMenu::OnOutAnimDoneEvent | 0xB18914（晚期钩子候选） |
| AuthenticationServiceImplementation::Authenticate(user,device,…) | 0x11A1C64 |

其余（`AbstractLoadingMenu::.ctor` 0xB18954 曾被误认成 OnInAnimDoneEvent 导致 v29 失败等）见 `plan/blades-login-bypass-20261029.md` §3.4。

### 4.3 「认证触发器表」= FlowTrigger 的同布局副本（要点）

`FlowState::StartAuthentication`（0x801988）尾部读出「触发器表」对象（`[this+0x5c]`），其中取 0x0 / 0x4 / 0xA8 / 0x128 四项 —— 与 FlowTrigger 静态字段偏移逐一对应 ⇒ 它是一张 **FlowTrigger 同布局副本**，所以 `[表+0x18]` = AuthenticationAnonymousLogin。逐条指令链见细节版 §3.2、§3.3。

---

## 5. 补丁实验（v31~v45）：三类结论

> 脚本 `build_and_test_v3X.sh`（含逐条反汇编自校验）；代码洞统一用本局永不调用的 `GHomeSendManager::SwitchAccount`(0xA7B9EC)。

- ✅ **已证实**：客户端能一路跑到标题画面（崩溃地址探针法，v39/v40 定位到首个被调用的流程函数）；v34 起补丁**不崩**，请求序列回健康基线；影子后端 `/auth/*` 已能返回完整会话体；bundle 与美术全量加载正常。
- ❌ **已排除**：崩溃类问题；**`/auth` 没发出不是后端问题**（请求序列根本没走到那一步）。
- 💥 **已踩死的三条路**（别再走）：
  1. **自己读 IL2CPP 静态字段** —— v31/v32/v35/v37/v42 五次全崩（漏元数据惰性初始化前导；usage 槽在初始化前只有小整数；静态对象早期半初始化）。
  2. **在 dispatcher tick 内点火** —— v43 崩在 StateMachine 泛型 Update 内部（pc=0xC51D30，调用者 0x77A370，不在注入代码里）= 重入式状态迁移。
  3. **在 bundle 阶段下钩子** —— v40 实证 `OnInAnimDoneEvent` 在 bundle 加载界面就触发，时机太早。
- 逐版失败原因表（v31~v45）见 `plan/blades-login-bypass-20261029.md` §4B、§8.1。

### 5.1 第二阶段对上述路线的**修正**（2026-10-29 晚间）

- **旧 cave 的 `ldr_imm()` 是错的**：ARM `ldr` 的 imm12 是**字节**偏移（原函数 `0xE590105C` 即 `[r0,#0x5c]`），
  旧 helper 除以 4 ⇒ v34 系列一直在读偏移 `0x17` 的错误字段。已修正。
- **点火用的槽位本身是空的**：`[触发器表+0x30]`（= AuthenticationPlayGame）在运行时 **NULL**
  （故意解引用实测 `SIGSEGV, fault addr 0x00000000`）⇒ 历史“点火无效”不只是触发器选错（0x18 本就无效），
  而是读的表/槽位不可用。
- **自行 `bl FlowTrigger::.cctor` 不可行**：会打断托管执行流（后续代码不执行，应用不崩但流程无变化）。
- **可靠触发点只有一个**：`GHomePostManager::InitResultCallback`（引擎就绪那一刻）。
  Java→Unity 的消息只有在**引擎就绪前**发出才会被排队投递；游戏起来后再发投递不到。
- **当前设备装的是 v46**（= v6 bundle 补丁 + 悬浮窗崩溃已修 + 伪造登录报文注入），
  **不崩、可启动、SDK 初始化正常、无 `R$layout` 异常**。

---

## 6. 下一步（2026-10-29 晚间更新；旧建议已被实测推翻）

> **第三阶段已把「点触发器」与「短接 SDK 登录」两条路线都排空**：
> 触发器路线：状态机本来就正常，缺的不是触发而是**加载屏的动画回调**；
> SDK 登录路线：`GHomeSendManager.Login` 打点实测全程 0 次，改了也是空操作。
> **下一步只剩一条**：让 `InitializingGame` 的加载屏把动画播完。
>
> 1. **【最高】修法 A（一个指令位）**：把 `AbstractLoadingMenu::OnOpen` 的 `0xb18588 popeq`
>    改成 `b cave`，cave 里 `bl 0xB18790`(PlayInAnim) 后 `pop {r4,r5,r6,r7,fp,pc}`。
>    同时给 `PlayInAnim` / `OnInAnimDoneEvent` / `OnOutAnimDoneEvent` 加打点，验证「动画完成回调」是不是等待点。
>    ⚠ 若加载屏 prefab 没有 Animator，PlayInAnim 会抛异常 ⇒ 改走修法 B。
> 2. **【备选】修法 B**：`b__222_223` 打开加载屏时改传一个带回调的 `LoadingMenuContext`
>    （照 `FlowState::OpenLoadingMenu`(0x8007C4) 构造）；难点是原生侧构造托管对象。
> 3. **顺带**：`patch_v49.py` 覆盖了 `MainActivity.onResume` 的 unit 3..9，
>    **连带干掉了 `GHome.getInstance().onResume(activity)`**；改成追加注入到 `Init`/`KickOff`。
> 4. 拿到会话后：`POST /api/game/v1/public/characters`（建角）→ `towns/current`，契约见 `rest_spec.md`。
>
> 工具：`build_vtrace.py`（多点 logcat 打点器，含读 Il2CppString 的 `str2` 模式）+ `build_vtrace.sh` + `run_vtrace2.sh`。

> ⚠️ **以下 A / B / C 是第二阶段的旧建议，已被第三阶段实测推翻，保留仅供追溯**：

1. **A（已作废）**：让 **SDK 的登录当场成功**——
   patch `GHomeApiBase.login(Activity,Callback)`（co=0x13EF18，37 单元），改为直接调
   `doCallback(activity, callback, 0, "ok", HashMap{userid,ticket,isGuest})`
   （5 参 doCallback = method_id 15842；`'userid'`/`'ticket'`/`'isGuest'`/`'1'`/`'true'` 都是现成字符串）。
   目的：让“SDK 已登录”在 Java 侧成立，看加载屏是否放行。
   ⚠ 注意：直接在初始化回调里调 `MainActivity.Login()` 会因 SDK 同步联网阻塞 UI 线程（进程被杀），必须**短接**而不是真登录。
2. **B**：找出游戏自己“发起登录”的那一次调用——`GHomeSendManager::Login` 在整个 v7a 里**没有任何 `bl`**
   （ARM/Thumb 都扫过）⇒ 在接口派发/委托/异步状态机里，需换查法（vtable / 状态机类 / UnityEvent 绑定）。
3. **C（工具侧）**：本环境**图片无法被模型读取**（截图只能做像素级分析）。需要“看画面找元素”时，
   用已建好的 **hook + logcat 打点 + 坐标扫描**（`build_vtap2_cave.py`）：把屏幕变成事件流。
4. 拿到会话后：`POST /api/game/v1/public/characters`（建角）→ `towns/current` 等，契约见 `rest_spec.md`。

---

## 7. 复跑命令与脚本索引

```bash
cd H:/Project/threeKingdomSlayer/threeKingdomSlayer/Library/Locus/tmp/blades
bash play_offline.sh            # 一键起服+拉起客户端（TLS 后端 + 文件服 + adb reverse + 请求序列），WAIT=300 可延长观察
bash build_and_test_v45.sh      # 打补丁 → 重建 1.2GB APK → zipalign/签名 → adb install -r
bash run_game.sh                # 只跑不重打包（旧版）

# —— 第二阶段（推荐用这套）——
python patch_v49.py              # 1) 生成带伪造登录报文的 build/classes.dex
python patch_sdk_float.py        # 2) 同上文件上再去掉 SDK 悬浮窗的 R$layout 崩溃
bash build_v46.sh                # 3) 打包 + zipalign + 签名（apksigner 需 JAVA_HOME=jdk1.8.0_181）
adb -s emulator-5554 install -r build/blades_v46.apk
bash run_live_check.sh           # 4) 起后端 + 拉起 + 抓日志/截图/请求序列（一次调用内完成）
```
⚠ 不跑 `play_offline.sh` 直接点图标 → 客户端连不上已停服域名 →「更新包下载失败」(0-10000-1)。
⚠ `build_v46.sh` 里的 `apksigner` 需要 `JAVA_HOME="C:\Program Files\Java\jdk1.8.0_181"`（系统默认指向不存在的 jdk-16）。
⚠ 每轮重建 1.2GB APK ≈ 3–5 分钟；`install -r` 会保留 1.1GB bundle 与 SDK 模块（**不要 `pm clear`**）。

**环境硬约束**（每次都会踩；完整版见 `plan/blades-login-bypass-20261029.md` §8.3）：

- 模拟器 `emulator-5554`，Android 7.1.2，**实际跑 ARM32(armeabi-v7a)**（houdini 转译）——补丁必须打在 v7a 库上。
- `adb` 固定 `-s emulator-5554`；先 `adb disconnect 127.0.0.1:5555`（残留 offline 条目会让裸命令报 “more than one device”）。
- **一切必须在同一次 bash 调用内完成**：Locus 每次调用结束会回收整棵进程树，adb server 与后端会一起死。
- `adb logcat` 不要全量 `-d`（会拖死连接）；用 `-d -t N` 或 `-s Unity:E Unity:W`；**原生崩溃在 `CRASH`/`DEBUG` 标签**。
- 设备侧接线（`adb reverse`）、`/system/etc/hosts` 改写、系统 CA 安装、`pm grant` 7 权限、`install -r` 保留 1.1GB bundle：见细节版 §8.3。
- 一轮 build+install+run 约 6-10 分钟。

---

## 8. 经验与踩坑（本轮最贵的学费）

### 8.1 IL2CPP ARM32 逆向/打补丁

完整编码坑表（`ldr`/`strb`/`ldrb` 的立即数与寄存器形式、`push/pop` 的 lr 位、条件分支、`cmp`、静态字段访问形态、搬移代码时的池字重算公式、元数据惰性初始化前导）见 **`plan/blades-login-bypass-20261029.md` §8.2**。三条最贵的结论：

- **不要自己读 IL2CPP 静态字段**（v31/v32/v35/v37/v42 五次全栽）—— 优先**调用游戏自己的现成函数**（v33/v43 的思路）。
- **不要在 dispatcher tick 内点火** —— 重入式状态迁移会把状态机搞崩（v43 崩溃 pc 在 StateMachine 泛型 Update 内部）。
- **不要在 bundle 阶段下钩子** —— `AbstractLoadingMenu::OnInAnimDoneEvent` 在 bundle 加载界面就触发（v40 探针实证），不是标题画面。

### 8.2 工作流

- **每版补丁都写「逐条反汇编自校验」**（capstone 断言），否则 ARM32 编码错误会以「游戏莫名崩了」的形式出现；断言**只警告不中断**构建。
- **用崩溃地址探针做「函数是否被调用」的判定**：把函数入口改成 `ldr pc,[pc,#0]` + 唯一 fault 地址，看 logcat `fault addr` 是哪个 → 一次运行只能命中一个，要连续问就连续跑。
- 补丁流水线：`build_and_test_vNN.sh` + `play_offline.sh`，一轮 6-10 分钟；**先写断言再打包**。长会话务必把进度写进 `Locus/knowledge/plan/*.md`，新增文档带 `blades-` 前缀。

### 8.3 Unity 侧边界

- 本线**没有改动任何 `Assets/` 文件**；产物只在 `Library/Locus/tmp/blades/`（临时目录）与 `Locus/knowledge/plan/blades-*.md`。
- Unity 主线（战斗 UI / 教学关 / 落地页 / 天赋树）由**别的会话**推进，交接见 `plan/weekly-plan-combat-ui-tutorial-20261027.md`、`plan/hub-ui-v9-assembly-handoff-20261025.md`。
- `Locus/knowledge/` 是多会话共享目录：新增文档请**带 `blades-` 前缀**，避免与主线互相覆盖。

---

## 9. 结论摘要（给下一个会话的三句话）

1. **为什么进不去**：渠道 SDK 的 UI 资源类缺失 → 悬浮窗「开始游戏」按钮出不来 → 流程在标题状态干等；SDK 服务端是活的，游戏自身后端已死（由本地影子后端接管）。
2. **怎么进**：让游戏自己发 `/auth/gplus`（deviceId 即可），会话由本地后端给；触发器 `0x30 AuthenticationPlayGame` / `0xC0 StartGame`（迁移表指向 `FetchingSaveData`），必须**晚期**（bundle 之后）且**不在 dispatcher tick 内**点火。
3. **最大的坑**：不要自己读 IL2CPP 静态字段、不要在状态机 tick 里点火、不要在 bundle 阶段下钩子。
