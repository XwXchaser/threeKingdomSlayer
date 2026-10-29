---
id: kd_0bf377e3-934c-4b88-8768-48d320b945d1
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
id: kd_blades_login_bypass_20261029
summary: 《上古卷轴：刀锋》离线化 2026-10-29 交接：阶段判定、登录卡点新根因（SDK UI 资源类 cn.uc.gamesdk.R$layout 缺失而非服务器下线）、FlowTrigger 静态字段偏移表与认证触发器表的静态定位结论、v31「流程点火」补丁设计（标题动画结束时点火 AuthenticationAnonymousLogin 让客户端自己发 /auth/gplus）、复跑方法与后续候选触发器。
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
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
