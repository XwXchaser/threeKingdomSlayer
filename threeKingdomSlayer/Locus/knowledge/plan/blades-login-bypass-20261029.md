---
id: kd_0bf377e3-934c-4b88-8768-48d320b945d1
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
summary: 《上古卷轴：刀锋》离线化 细节版（2026-10-29）：登录卡点根因（SDK UI 资源类 cn.uc.gamesdk.R$layout 缺失，附 logcat 原文）、FlowTrigger/FlowState 静态字段偏移与认证触发器表定位、IL2CPP ARM32 静态访问形态与编码坑全表、关键函数 RVA 全表、状态机迁移表、v31~v45 逐版补丁实验记录与每版失败根因、复跑方法与环境硬约束、后续候选触发器。
---

# Blades 离线化：登录卡点定位与 v31 补丁（2026-10-29）

> 本文接续 `plan/blades-offline-revival-plan.md`（§6 之前的内容）与
> `memory/blades-reverse-engineering-assets.md`（产物索引）。
> 上一会话因 chat 上下文超限中断，本篇是可直接接续的入口。

---

## 1. 阶段判定（2026-10-29 实测）

| 阶段 | 状态 | 证据 |
|---|---|---|
| 静态逆向（metadata / 257 张设计表 / REST 契约 117 路径 165 DTO） | ✅ | `types.csv` `rest_spec.md` |
| 本地影子后端（TLS 拦截 host_info + MetaManifest） | ✅ | `requests.log`：3 个请求全部 200 |
| Bundle 阶段（本地 bundle 直连 + 美术全加载） | ✅ | 标题画面截图：LOGO+城堡+角色完整渲染 |
| **登录 / 取得会话** | ❌ **唯一阻塞点** | 请求序列停在 `POST /api/game/v1/public/characters//towns/current/rewards/current`（characterId 为空） |
| 角色 / 城镇 / 任务 REST 闭环 | ⬜ 未开始 | 依赖会话 |

客户端**进程是活着的**（pid 正常），画面正常，只是停在标题画面等待。

---

## 2. 登录卡点的新根因（修正旧结论）

> 旧结论「渠道 SDK 服务已 nginx 404 ⇒ 悬浮窗按钮永不出现」**不准确**。

实测（logcat，`run_full_log.sh` 抓全级别日志）：

```
I System.out: GHomeApiBase.initialize() -> gameId=791000460 ...
I System.out: GHomeChannelBase.initializeUI() -> GHome Core SDK Version=4.1.0.14 ...
I System.out: GLPostRequest->request start->url: http://api.mygm.sdo.com/v1/basic/publickey
I System.out: GLPostRequest->request end  ->response->uri: http://api.mygm.sdo.com/v1/basic/publickey
I System.out: GLPostRequest->request start->url: http://api.mygm.sdo.com/v1/basic/handshake
I System.out: GLPostRequest->request end  ->response->uri: http://api.mygm.sdo.com/v1/basic/handshake
I System.out: GLPostRequest->request start->url: http://api.mygm.sdo.com/v1/gchannel/basic/getGameChannelClientCfg?...
W System.err: java.lang.NoClassDefFoundError: Failed resolution of: Lcn/uc/gamesdk/R$layout;
      at cn.uc.gamesdk.e.a.a(Unknown Source)
      at cn.uc.gamesdk.UCGameSdk$3.run(Unknown Source)
Caused by: ClassNotFoundException: Didn't find class "cn.uc.gamesdk.R$layout"
```

结论：

1. **SDK 服务端是活的**（publickey / handshake / getGameChannelClientCfg 都有响应）。
2. **真正卡住的是 SDK 的 Java UI 资源类缺失**：`cn.uc.gamesdk.R$layout` 不在 classpath
   （`DexPathList` 只有 `base.apk` + native libs），悬浮窗 inflate 抛 `NoClassDefFoundError`
   ⇒ 「开始游戏」按钮永远创建不出来 ⇒ 流程无限等待。
3. SDK 的 loader 模块是**运行时下载**的（`/data/user/0/<pkg>/ucgamesdk/modules/updates/cn.uc.gamesdk.loader.apk`，8.2MB，已下载成功），
   其中含 `assets/lib/armeabi/libcn.uc.gamesdk.floater.so` 与 `res/layout/*`，
   但**不含** `cn/uc/gamesdk/R$layout`（该 R 类应在 package=cn.uc.gamesdk 的资源模块里，缺失）。
4. Android 视图树（uiautomator dump）里**只有 Unity 的 Game view**，没有任何 SDK 悬浮窗节点 ⇒ 印证按钮没被创建。

> 备选路线（未做）：自建 package=cn.uc.gamesdk 的资源模块 APK（aapt2 编译 loader APK 的 res/），
> 放进 `modules/update-3/`（该目录已存在但为空）。成本高于 IL2CPP 补丁，故优先做后者。

---

## 3. 静态定位结论（本次新增，全部可复算）

### 3.1 FlowTrigger / FlowState 静态字段偏移表

`dumper_out_v7a/dump.cs` 里 `public static FlowTrigger X; // 0xNN` 给出 90 个触发器的偏移，
关键者：

| 偏移 | 触发器 | 偏移 | 触发器 |
|---|---|---|---|
| 0x10 | AuthenticationPlatformSignIn | 0xA4 | Authenticate |
| **0x18** | **AuthenticationAnonymousLogin** | **0xA8** | **AuthenticateNoUi** |
| 0x1C | AuthenticationCreateAnonymousAccount | 0xB0 | FetchSaveDataFromNoUiAuthentication |
| 0x30 | AuthenticationPlayGame | 0xC0 | StartGame |
| 0x8C | Bootstrap | 0x94 | Proceed |
| 0x124 | SoftLaunchStartNoUi | 0x128 | SoftLaunchStart |

FlowState 状态：0xC Authentication / 0x24 AuthenticationPlatformSignIn /
**0x34 AuthenticationAnonymousLogin** / 0x38 AuthenticationCreateAnonymousAccount /
0x40 AuthenticationPlayGame / 0x48 MainMenu / 0x124 SoftLaunchStartNoUi 等。

### 3.2 「认证触发器表」= FlowTrigger 的同布局副本

`FlowState::StartAuthentication`（**v7a 0x801988**）尾部：

```
0x8019ec  ldr r0, [pc, #0xa8]     ; 池字 V = *(0x801a9c) = 0x2a09e64
0x8019f0  ldr r0, [pc, r0]        ; 槽地址 = (pc+8)+V = 0x2A8B8E5C → 静态字段块
0x8019f4  ldr r0, [r0]            ; 静态单例对象
0x8019fc  ldr r1, [r0, #0x5c]     ; → 「触发器表」对象
0x801a04  ldr r3, [r1, #0xa8]     ; 0xa8
0x801a08  cmp r3, r4              ; 入参触发器 == 表[0xa8] ?
0x801a38  ldr r4, [r1, #4]        ; 否则 → 点火 表[0x4]
0x801a68  ldr r4, [r1]            ; 相等 → 点火 表[0x0]
0x801a88  b  FlowTrigger::Fire    ; 0xA7381C
```

`FlowState::StartSoftLaunchAuthentication`（**0x801BC8**）用同一张表，点火 `表[0x128]`。

表内偏移 **0x0 / 0x4 / 0xA8 / 0x128 与 FlowTrigger 静态字段偏移逐一对应**
（AuthenticationPreFtueStart / AuthenticationPostFtueStart / AuthenticateNoUi / SoftLaunchStart）
⇒ 该对象就是一张 **FlowTrigger 同布局的副本**，因此
`表[0x18]` = **AuthenticationAnonymousLogin**，`表[0xA8]` = AuthenticateNoUi。

### 3.3 IL2CPP ARM32 静态字段访问形态（本次确定）

```
ldr rX,[pc,#A]      ; 池字 V = *(PC+8+A)
ldr rX,[pc,rX]      ; 槽地址 = (PC+8) + V      （PC 指该指令地址+8）
ldr rX,[rX]         ; static_fields[0]
ldr rY,[rX,#imm]    ; 第 imm/4 个静态字段
```
编码：`ldr rT,[rR,#imm]` = `0xE5900000 | (Rn<<16) | (Rt<<12) | (imm/4)`；
`ldr rT,[pc,rM]` = `0xE79F0000`（Rm 在 [3:0]、Rn 在 [19:16]、Rt 在 [15:12]）。

### 3.4 关键函数 RVA（v7a）

| 函数 | RVA |
|---|---|
| FlowTrigger::Fire | 0xA7381C |
| FlowTrigger::GetTriggerByName（全库仅 1 处调用：动画事件） | 0xA76C94 |
| FlowManager::Update（每帧） | 0x7FC978 |
| FlowManager::Fire(trigger) | 0x7FCE68 |
| FlowManager::ReceiveInput | 0x7FD110 |
| FlowState::StartAuthentication | 0x801988 |
| FlowState::StartSoftLaunchAuthentication | 0x801BC8 |
| FlowState::OnSaveDataFetched | 0x801CE8 |
| FlowState::StartupLoadComplete | 0x8024F8 |
| AbstractLoadingMenu::OnInAnimDoneEvent | **0xB188D4**（尾调用在 0xB18910） |
| AbstractLoadingMenu::.ctor | 0xB18954（**旧会话误把它当 OnInAnimDoneEvent，导致 v29 失败**） |
| MainMenu::OnLogoPressed / Update | 0x9DA004 / 0x9DA24C |
| GHomePostManager::LoginResultCallBack | 0xA796A0 |
| AuthenticationServiceImplementation::Authenticate(userID,deviceID,…) | 0x11A1C64 |
| AuthenticationServiceImplementation::Authenticate() | 0x11A340C |
| AuthenticationAndroid::AuthenticateOnCurrentPlatform | 0x119DFC8 |

---

### 3.5 崩溃定位方法（v32 调试时很有效）

原生崩溃信息在 logcat 的 **`CRASH`** 标签下（不在 `Unity` 标签，不加过滤抓不到）：

```bash
adb logcat -d -t 500 | grep -E "Fatal signal|CRASH|fault addr|backtrace" -A 12
```

输出里的 `pc 08a7ba18` 直接给出**我们注入代码的偏移**：
`pc - 注入地址(0xA7B9EC) = 0x2C` → 崩在 `ldr r0,[r0]`（静态字段访问第三步），
配合寄存器快照（`r0 00000000` / `r0 000000a1`）立刻知道是“指针为空”而不是“地址算错”。
另外 `lr` 能反推调用来源（`lr 0864f3b4` ⇒ `FlowManager::Update` 的调用者是 `0x64F3B0`）。

### 3.6 结论：不要自己读 IL2CPP 静态字段（v31~v32 的教训）

v31/v32 连续两版都因为**自行读静态字段**而崩溃：

- v31：漏了元数据惰性初始化前导 → 启动早期读���空指针 → SIGSEGV。
- v32：补上前导仍崩 —— 因为那段前导里的 usage 槽在初始化前只有一个小整数（`0xA1`），
  解引用它必崩；**原代码能跑是因为它所在的位置在游戏自己的初始化之后**。

⇒ **正确做法：不要自己解析静态字段，直接调用游戏自己的函数。**
本轮采用的即此方案（v33）：直接 `bl FlowState::StartAuthentication(0)`
（v7a `0x801988`）—— 它内部完成静态访问并点火认证触发器
（按其内部逻辑：入参触发器 != 表[0xA8] 时点火 `表[0x4]` = AuthenticationPostFtueStart），
我们只负责在正确的时机把它叫起来。

> 附带结论：**“自己读静态字段”这条路成本很高**（池字 PC 相对、需搬前导、需重算 4 个池字），
> 以后优先找“同语义的现成函数”直接调。

### 3.7 状态机迁移表（2026-10-29 静态读出，关键）

从 `FlowState::.cctor`（0x10C0000-0x10E0000）的迁移表里抽出「触发器 → 目标状态」：

| 触发器 | → 目标状态 |
|---|---|
| **AuthenticationPlayGame (0x30)** | **FetchingSaveData** |
| **StartGame (0xC0)** | **FetchingSaveData** |
| AuthenticationPreFtueStart (0x00) | FetchingSaveData / SoftLaunch / SoftLaunchPlayGame |
| FetchSaveDataFromNoUiAuthentication (0xB0) | FetchingSaveData / SoftLaunch |
| RegressionTestAuthenticate (0x120) | FetchingSaveData |

⇒ 进游戏的状态链是：**触发器 → FetchingSaveData → OnSaveDataFetched → 认证 → 角色**。
⇒ **该点火的触发器是 0x30 / 0xC0，不是 0x18**（0x18 在表里不指向任何迁移）。
⇒ 配套钩子条件：`_stateMachine != null`（已过 bundle 阶段）+ 静态对象链逐层非空。
脚本：`build_and_test_v42.sh`（v41 三重保护 + 触发器 0x30/0xC0/0x18/0xA8）。

### 3.8 重入式状态迁移（v43 崩溃根因，v44 的设计依据）

v43 在 `FlowManager::Update` 里调 `FlowState::StartAuthentication(0)` → 崩溃地址
**pc=0xC51D30（StateMachine 泛型 Update 内部）**，调用者 0x77A370 —— **不在注入代码里**。
⇒ 在 dispatcher 正在 tick 时点火 = **重入式状态迁移**，会把状态机搞崩。

⇒ v44 的做法：**先让 dispatcher 正常 tick 完（bl Dispatcher.Update），再在 tick 之外点火**
（钩子挂在 `FlowManager::Update` 的 `pop` 指令 0x7FC99C 上，pop 之后调用 Dispatcher.Update）。
脚本：`build_and_test_v44.sh`。

## 4. v31/v32/v33 补丁设计（本次实施）

**目标**：让流程自己进入匿名登录 → 客户端向本地影子后端发 `POST /asia/api/authentication/v1/public/auth/gplus`
（后端已备好 `/auth/*` 会话响应）→ 拿到会话 → 进入角色/城镇阶段。

**手法**（不需要代码洞，只需要改 1 条指令 + 写 1 段代码）：

1. 在**本局永不调用**的 `GHomeSendManager::SwitchAccount`（0xA7B9EC）写入 **72 字节点火代码**：
   ```
   push {r0,r1,r2,r3,lr}
   ldr r5, [r0, #0x14]        ; this->_stateMachine（保护：为空则跳过点火）
   cmp r5, #0
   beq  .skip
   ldr r0, [pc, #48]          ; 池字（新位置重算）
   ldr r0, [pc, r0]           ; 静态字段块
   ldr r0, [r0]               ; 静态单例
   ldr r1, [r0, #0x5c]        ; 触发器表
   ldr r0, [r1, #0x18]        ; AuthenticationAnonymousLogin
   mov r1, #0
   bl  FlowTrigger::Fire      ; 0xA7381C
   .skip:
   pop {r0,r1,r2,r3,lr}
   ldr r4, [r0, #0x18]        ; this->_dispatcher
   mov r0, r4 ; mov r1, #0
   pop {r4, sl, fp, lr}
   b   Dispatcher.Update      ; 0xC52204（原 Update 的剩余逻辑）
   ```
2. 把 `FlowManager::Update`（0x7FC978）里的 `mov r0, r4`（**0x7FC994**）改成 `b 0xA7B9EC`
   ⇒ **每帧都有一次点火机会**（选它而不是 `OnInAnimDoneEvent` 的原因：
   流程也可能卡在「等加载菜单入场动画事件」上，而 Update 一定每帧跑）。

**池值重算公式**：原指令 `ldr r0,[pc,r0]` 在 0x8019F0 ⇒ 槽地址 = 0x8019F8 + V（V = 池字 @0x801A9C = 0x2A09E64）
⇒ 槽地址 `SLOT = 0x320B85C`（在 .so 内，文件大小 0x32797E4）。
新位置指令在 `CAVE+20` ⇒ PC = CAVE+28 ⇒ 新池字 `V' = SLOT - (CAVE+28)`。

**ARM32 编码坑（本轮踩了三次，构建脚本已内置 assert）**：

| 坑 | 正确写法 |
|---|---|
| `ldr rT,[rR,#imm]` | `0xE5900000｜(Rn<<16)｜(Rt<<12)｜(imm/4)` —— **基址在 [19:16]，目标在 [15:12]**（写反会读错寄存器） |
| `push/pop {.., lr}` | lr 是寄存器列表的 bit14（0x4000），漏掉会把返回地址冲掉 |
| 条件分支 | `beq` = `0x0A000000｜imm24`（cond 在 [31:28]）；`0xEA000000` 是**无条件** `b` |
| `cmp rN,#imm` | `0xE3500000｜(N<<16)｜imm8`（N 在 [19:16]） |
| `strb rT,[pc,rM]` | **STRB 寄存器形式** = `0xE7C00000｜(Rm<<4)｜(Rn<<16)｜(Rt<<12)`；写成 `0xE5CF1000` 会变成 `strb r1,[pc]`（立即数形式，地址=PC+0）⇒ **往只读代码段写 1 → 立刻 SIGSEGV**（v32 第一次跑就是这个坑） |
| `ldrb rT,[rR]` | `0xE5D00000｜(Rn<<16)｜(Rt<<12)`（立即数形式，B 位不同） |

**IL2CPP 静态访问必须带「元数据惰性初始化」前导（v31 崩溃的根因）**：

只写 `ldr r0,[pc,r0] / ldr r0,[r0]` 去读静态字段，在**游戏刚启动**时读到的是**未初始化的空指针**
⇒ SIGSEGV（v31 就是这样把客户端打崩的，只发出 host_info 就死）。
必须连同 `StartAuthentication` 的这段前导一起搬过来（它同时会把对应类的静态字段分配好）：

```
ldr r0, [pc, #A]      ; 池1 → “已初始化”标志字节的 PC 相对地址
add r0, pc, r0
ldrb r0, [r0]
cmp r0, #0
bne  .ok
ldr r0, [pc, #B]      ; 池2 → metadata usage
ldr r0, [pc, r0]
ldr r0, [r0]
bl   il2cpp_codegen_initialize_runtime_metadata   ; 0x7BFFA0
ldr r0, [pc, #C]      ; 池3 → 另一个标志字节
mov r1, #1
strb r1, [pc, r0]
.ok:
```

每个池字存的都是 **PC 相对偏移**，搬动代码时必须重算：
`新池值 = 原目标地址 − 新消费者PC`，其中「原目标地址 = 原消费者PC + 原池值」。

脚本：`Library/Locus/tmp/blades/build_and_test_v32.sh`（含逐条反汇编 assert 自校验）。

**若 v31 无效**：换触发器偏移即可（脚本里 `TRIG_OFF` 一个常量）：
`0xA8`（AuthenticateNoUi）→ `0xA4`（Authenticate）→ `0x30`（AuthenticationPlayGame）→
`0x10`（AuthenticationPlatformSignIn）→ `0x1C`（AuthenticationCreateAnonymousAccount）→ `0x94`（Proceed）。

---

## 4B. 补丁实验记录（v31 ~ v42）与结论

> 全部脚本：`Library/Locus/tmp/blades/build_and_test_v3X.sh`（每个版本一个，含逐条反汇编自校验）。
> 钩子统一用「本局不会被调用」的 `GHomeSendManager::SwitchAccount`(0xA7B9EC) 作为代码洞。

| 版本 | 钩子 | 做法 | 实测结果 |
|---|---|---|---|
| v31 | `FlowManager::Update` | 自己读静态槽取触发器表 → Fire | **启动即崩**（读未初始化空指针） |
| v32 | `FlowManager::Update` | 补上元数据初始化前导后再读 | **仍崩**（usage 槽在初始化前只有 0xA1 这种小整数，解引用必崩） |
| v33 | `FlowManager::Update` | 改成直接 `bl FlowState::StartAuthentication(0)` | **仍崩**（钩子太早，游戏自身初始化未完成） |
| **v34** | **`AbstractLoadingMenu::OnInAnimDoneEvent`（标题动画结束，晚期）** | 自己读静态槽 → Fire → 再跳回原 `Action.Invoke` | ✅ **不崩，请求序列回到健康基线**（host_info → MetaManifest → rewards 轮询，标题画面正常），但**没有 /auth 请求** |
| v35 | `FlowManager::Update` | v31 + “静态槽非 0 才点火”保护 | **又崩**（静态对象在早期是半初始化的，单层 NULL 判断不够） |
| v36 | 同 v34 的晚期钩子 | 一次性点名 4 个触发器（0x18/0xA8/0xA4/0x30） | ✅ 不崩，但**仍无 /auth** ⇒ 钩子可能没触发，或这些触发器当前状态不处理 |
| v37 | `FlowManager::Update` | 完整 NULL 链保护（槽/单例/表/触发器逐层判） | **又崩** ⇒ **每帧方案彻底放弃**（游戏初始化早期静态对象是半初始化的） |
| v39/v40 | 多探针（崩溃地址） | 8 个候选函数入口改成探针 | ✅ v39 命中 **0xDEAD0020 = `FlowState::StartupLoadComplete`（0x8024F8）**；v40 去掉该探针后，客户端在 **MetaManifest 之后**才死（比 v39 晚一步）⇒ **下一个被调用的是 `AbstractLoadingMenu::OnInAnimDoneEvent`（0xB188D4）** ⇒ **v34 的钩子点在 bundle 加载界面上，不是标题画面**（时机太早，触发器被无视） |
| v41 | `FlowManager::Update` | 触发器读取加三重保护（`_stateMachine != null` + 静态槽/单例/表/对象逐层非空） | 未跑完（会话中断），脚本已就绪：`build_and_test_v41.sh` |

### 结论（下一轮直接用）

1. **必须用晚期钩子**（游戏完全初始化之后），`FlowManager::Update` 太早。
2. **不要自己读静态字段**：v31/v32/v35 三次都栽在这条路上；
   优先找「同语义的现成函数」直接调（v33 的思路），或用晚到期的时机（v34）。
3. v34 已排除“崩溃类”问题，剩下的可能性只有两个，需分别验证：
   - **(a) 钩子没触发**：标题画面的入场动画事件可能不是 `OnInAnimDoneEvent`
     （`TitleMenu`/`SplashLoadingMenu` 继承 `AbstractLoadingMenu`，但事件名/绑定对象未知）。
     → 验证方法：把钩子改成“崩溃地址探针”（`ldr pc,[pc,#0]` + 唯一 fault 地址），
       看 logcat 的 `CRASH` 标签里 fault addr 是否等于预期值。
   - **(b) 触发器不对**：当前点的是 `AuthenticationAnonymousLogin`(0x18)，
     当前流程状态可能不处理它。→ 换偏移重试，顺序见下。
4. **候选触发器偏移**（FlowTrigger 静态字段）：
   `0x18` AuthenticationAnonymousLogin → `0xA8` AuthenticateNoUi → `0xA4` Authenticate →
   `0x1C` AuthenticationCreateAnonymousAccount → `0x30` AuthenticationPlayGame →
   `0x10` AuthenticationPlatformSignIn → `0x94` Proceed。
5. **候选钩子点**（都属晚期）：
   - `FlowState::StartupLoadCompleteAndBackendAvailable`(0x8026DC，一次性，bundle 阶段之后)
   - `AbstractLoadingMenu::OnOutAnimDoneEvent`(0xB18914)
   - `FlowManager::ReceiveInput`(0x7FD110)
   - **不要用** `AbstractLoadingMenu::OnInAnimDoneEvent`：它在 **bundle 加载界面**就被调用了（v40 实证）。
   - **每帧钩子（`FlowManager::Update`）必须同时满足**「`_stateMachine != null`」+「静态对象链逐层非空」，
     否则会在游戏初始化早期崩（v35/v37 实证）。
6. **v39 探针表**（一次运行只会有一个命中，其余需要再跑）：
   `AbstractLoadingMenu::OnInAnimDoneEvent` 0xB188D4 → DEAD0004；
   `FlowState::AuthenticationChooseEndingState` 0x7FDC20 → DEAD0008；
   `FlowState::StartAuthentication` 0x801988 → DEAD000C；
   `FlowState::OnSaveDataFetched` 0x801CE8 → DEAD0010；
   `MainMenu::OnLogoPressed` 0x9DA004 → DEAD0014；
   `FlowManager::ReceiveInput` 0x7FD110 → DEAD0018；
   `FlowState::StartSoftLaunchAuthentication` 0x801BC8 → DEAD001C；
   `FlowState::StartupLoadComplete` 0x8024F8 → DEAD0020。
6. **后端已就绪**：影子后端 `/asia/api/authentication/v1/public/auth/*` 已返回
   `Session/UserId/Token/TokenSchema/TokenExpirationSeconds/FeatureStatus/LinkedAccountsStatus`；
   只要客户端发出 `/auth/gplus`，会话立刻到手。

## 5. 复跑方法

```bash
cd Library/Locus/tmp/blades
bash build_and_test_v31.sh      # 打补丁 → 重建 APK → zipalign/签名 → adb install -r
bash run_game.sh                # 起影子后端 + 文件服务器 + adb reverse → 启动客户端 → 抓请求/截图
```

关键约束（老坑，别踩）：

- `adb` 固定 `-s emulator-5554`；先 `adb disconnect 127.0.0.1:5555`；
- **一切必须在同一次 bash 调用内完成**（Locus 每次调用结束会回收整棵进程树）；
- `adb logcat` 不要用全量 `-d`，用 `-s Unity:E Unity:W` 或 `-d -t N`；
- `adb install -r`（**不要 uninstall**）可保留 `/storage/emulated/0/Android/data/<pkg>/` 下的 1.1GB bundle，
  省掉 47 秒重推；
- 每次启动前 `pm grant` 7 个权限，否则卡在系统权限弹窗。

## 6. 本轮新增工具（`Library/Locus/tmp/blades/`）

| 脚本 | 作用 |
|---|---|
| `trigger_scan.py` | 扫 FlowTrigger::Fire 调用点并尝试识别触发的触发器（需 capstone） |
| `find_trigger_reads3.py` | 反推「FlowTrigger 静态字段槽」并全库反查读取点（ARM32 位模式扫描） |
| `run_full_log.sh` | 抓 Unity **全级别**日志 + 视图树/SDK 日志的复现脚本 |

## 7. 后续（拿到会话之后）

1. `POST /api/game/v1/public/characters`（建角）→ `GET /api/game/v1/public/towns/current` 等；
   契约见 `rest_spec.md`（117 路径 / 165 DTO）。
2. 角色进入城镇后才会轮到关卡/战斗闭环，那是另一段工作量。
3. 设计借鉴结论已经在 `design/blades-architecture-benchmark.md`，不受本篇影响。

---

## 8. 增补（2026-10-29 续）：完整表与 v43~v45 记录

> 开机读物 `plan/blades-handoff-20261029.md` 已精简为「最小必要集」（首读体积减 42%），
> 它引用的完整表以**本节**为准。

### 8.1 v43 ~ v45（接 §4B 的 v31~v42；v43 根因见 §3.8）

| 版本 | 钩子 | 做法 | 结果与**根因** |
|---|---|---|---|
| v43 | `FlowManager::Update` | 不读表，直接 `bl StartAuthentication(0)` | ❌ 崩，**崩溃 pc=0xC51D30（StateMachine 泛型 Update 内部）**，调用者 0x77A370 —— **不在注入代码里** ⇒ **在 dispatcher tick 过程中点火 = 重入式状态迁移**，把状态机搞崩 |
| v44 | `FlowManager::Update` 的 `pop` 处 | 先 `bl Dispatcher.Update` 再点火 | ❌ 崩：钩错指令位置，`mov r0,r4` 之后 `this` 已丢 |
| v45 | `FlowManager::Update` 的 `mov r0,r4` 处（此处 `this` 还在 r0） | 同上，但顺序正确 | ❌ 仍崩（**崩溃地址未取**，下一步第一件事就是取） |

**当前设备状态**：装的是会崩的 v45。恢复已知可用版：`bash build_and_test_v34.sh`（能正常跑到标题画面）。

### 8.2 IL2CPP ARM32 编码坑全表（补 §4 的 6 行）

| 坑 | 正确写法 / 结论 |
|---|---|
| `ldr rT,[rR,#imm]` | `0xE5900000｜(Rn<<16)｜(Rt<<12)｜(imm/4)` —— **基址在 [19:16]，目标在 [15:12]**（写反会读错寄存器） |
| `strb rT,[pc,rM]` | **STRB 寄存器形式** `0xE7C00000｜(Rm<<4)｜(Rn<<16)｜(Rt<<12)`；写成 `0xE5CF1000` 会变成 `strb r1,[pc]`（立即数形式，地址=PC+0）⇒ **往只读代码段写 1 → 立刻 SIGSEGV**（v32 第一次跑就是这个坑） |
| `ldrb rT,[rR]` | `0xE5D00000｜(Rn<<16)｜(Rt<<12)`（立即数形式，B 位不同） |
| `push/pop {.., lr}` | lr 是寄存器列表 bit14（0x4000），**漏存会把返回地址冲掉** |
| 条件分支 | `beq`=`0x0A000000｜imm24`（cond 在 [31:28]）；`0xEA000000` 是**无条件** `b` |
| `cmp rN,#imm` | `0xE3500000｜(N<<16)｜imm8`（N 在 [19:16]） |
| 静态字段访问 | `ldr rX,[pc,#A]`→池字 V；`ldr rX,[pc,rX]`→槽地址=(指令地址+8)+V；`ldr rX,[rX]`→static_fields[0] |
| **搬移代码** | 每个池字都是 **PC 相对偏移**，搬动后必须重算：`新池值 = 原目标地址 − 新消费者PC`，其中原目标地址 = 原消费者PC + 原池值 |
| **元数据惰性初始化** | 读静态字段前有 `il2cpp_codegen_initialize_runtime_metadata`(0x7BFFA0) 前导；**但前导本身依赖尚未填充的 usage 槽**，照搬到别处照崩 |
| **不要自己读静态字段** | v31/v32/v35/v37/v42 五次全栽在这条路。优先**调用游戏自己的现成函数**（v33/v43 的思路） |
| **不要在 dispatcher tick 内点火** | 会造成重入式状态迁移，把状态机搞崩（v43 崩溃 pc 在 StateMachine 泛型 Update 内部） |
| 崩溃定位 | logcat `CRASH` 标签给 `pc`；`pc − 注入地址` = 我们代码内的偏移；寄存器快照区分「指针为空」与「地址算错」 |
| 钩子时机 | 同一个类 `AbstractLoadingMenu` 的 `OnInAnimDoneEvent` 在 **bundle 加载界面**就触发（v40 探针实证），不是标题画面 |

### 8.3 复跑方法与环境硬约束全表（补 §5）

```bash
cd Library/Locus/tmp/blades
bash build_and_test_v31.sh      # 打补丁 → 重建 APK → zipalign/签名 → adb install -r
bash run_game.sh                # 起影子后端 + 文件服务器 + adb reverse → 启动客户端 → 抓请求/截图
bash play_offline.sh            # 上一条的增强版（WAIT=300 可延长观察）
```

- 模拟器 `emulator-5554`，Android 7.1.2，**实际跑 ARM32(armeabi-v7a)**（houdini 转译）——补丁必须打在 v7a 库上。
- `adb` 固定 `-s emulator-5554`；先 `adb disconnect 127.0.0.1:5555`（残留 offline 条目会让裸命令报 “more than one device”）。
- **一切必须在同一次 bash 调用内完成**（Locus 每次调用结束会回收整棵进程树）。
- `adb logcat` 不要全量 `-d`（会拖死连接）；用 `-s Unity:E Unity:W` 或 `-d -t N`；**原生崩溃在 `CRASH`/`DEBUG` 标签**。
- 设备侧接线：`adb reverse tcp:443 tcp:8443`（影子后端）、`adb reverse tcp:80 tcp:8080`（文件服务器）；
  `/system/etc/hosts` 需把 6 个游戏域名 + 14 个 `bgs-ping-*.s3.*.amazonaws.com` 指到 127.0.0.1；
  自签 CA 必须装进**系统 CA 库**（targetSdk 26 不信任用户 CA）。
- 每次启动前 `pm grant` 7 个权限，否则卡在系统权限弹窗（「正在初始化…」）。
- `adb install -r`（**不要 uninstall**）可保留 `/storage/emulated/0/Android/data/<pkg>/` 下的 1.1GB bundle，省 47 秒重推；
  但 uninstall 会连带删掉 SDK 下载的模块目录。
- 一轮 build+install+run 约 6-10 分钟（1.2GB APK 流式安装很慢）。

### 8.4 完整偏移表与状态机迁移（补 §3.1 / §3.7）

FlowTrigger 静态字段偏移（`public static FlowTrigger X; // 0xNN`，共 90 个，关键者）：

| 偏移 | 触发器 | 偏移 | 触发器 |
|---|---|---|---|
| 0x00 | AuthenticationPreFtueStart | 0xA4 | Authenticate |
| 0x10 | AuthenticationPlatformSignIn | **0xA8** | **AuthenticateNoUi** |
| **0x18** | **AuthenticationAnonymousLogin** | 0xAC | FetchSaveData |
| 0x1C | AuthenticationCreateAnonymousAccount | **0xB0** | **FetchSaveDataFromNoUiAuthentication** |
| 0x30 | **AuthenticationPlayGame** | 0x94 | Proceed |
| 0x34 | PlayTestStart | 0x8C | Bootstrap |
| 0xC0 | **StartGame** | 0x124 | SoftLaunchStartNoUi |
| 0x128 | SoftLaunchStart | 0x120 | RegressionTestAuthenticate |

FlowState 状态偏移（部分）：`0x0C Authentication` / `0x10 AuthenticationPreFtueStart` / `0x14 AuthenticationPostFtueStart` /
`0x24 AuthenticationPlatformSignIn` / **`0x34 AuthenticationAnonymousLogin`** / `0x38 AuthenticationCreateAnonymousAccount` /
`0x40 AuthenticationPlayGame` / `0x48 MainMenu` / `0x90 PreCharacterSelection` / `0x170 FetchingSaveData` /
`0x190 SoftLaunch` / `0x1C0 SoftLaunchPlayGame`。

状态机迁移表（从 `FlowState::.cctor`，0x10C0000-0x10E0000 读出，共 27 条迁移）：

| 触发器 | → 目标状态 |
|---|---|
| **AuthenticationPlayGame (0x30)** | **FetchingSaveData** / SoftLaunch / SoftLaunchConnectionMenu |
| **StartGame (0xC0)** | **FetchingSaveData** / SoftLaunch |
| AuthenticationPreFtueStart (0x00) | FetchingSaveData / SoftLaunch / SoftLaunchPlayGame / SoftLaunchConnectionMenu / SoftLaunchLogInWithBNetToken |
| FetchSaveDataFromNoUiAuthentication (0xB0) | FetchingSaveData / SoftLaunch |
| RegressionTestAuthenticate (0x120) | FetchingSaveData |

⇒ **进游戏的状态链 = 触发器 → `FetchingSaveData` → `OnSaveDataFetched` → 认证 → 角色**。
⇒ **该点的火是 0x30 / 0xC0 / 0x00 / 0xB0；0x18（AuthenticationAnonymousLogin）在迁移表里不指向任何迁移**
（早几版点的就是它，所以「不崩但也没反应」）。
