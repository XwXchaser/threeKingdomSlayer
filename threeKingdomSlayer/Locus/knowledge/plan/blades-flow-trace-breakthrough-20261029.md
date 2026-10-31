---
id: kd_774de6fe-0909-40f7-9582-815b8132bc63
injectMode: inherit
injectAgents:
- '*'
summary: 《上古卷轴：刀锋》离线化 流程追踪日志（最新到 4I 节，2026-10-30 21:10）：记录了从 4A 到 4I 的全部打点证据与结论——★ 洞 B 的三个 bug 已修（无条件 b skip / add 代替 ldr / 栈未 8 字节对齐），洞 B 能跑后客户端首次真的向后端认证（POST /api/authentication/v1/public/auth/gplus 被接受），状态链走到 FetchingSaveData → SaveManager::Load 后因设备上没有本地存档而停住；★★ 硬规律：x86 模拟器的 ARM 转译层里代码段只能 pc-relative 读（计算地址读代码段直接 SIGSEGV），触发器身份可由地址反查 object(X)=holder[0]−4X；后端契约（.schema/.conversion）已挖出且响应 0 异常（两条解析路径 + 集合形状）；附状态名换算手法、洞区布局与 run_ca.sh 汇总 tag 脱节的坑。
aiEditMode: inherit
---

# Blades 离线化 · 第三阶段：流程链打点与卡点定位（2026-10-29 晚）

> 接续 `plan/blades-handoff-20261029.md`（开机读物）与 `plan/blades-java-bridge-breakthrough-20261029.md`（第二阶段）。
> 本篇只记录**这一阶段新拿到的事实**，其中两条直接推翻了旧结论。

---

## 1. 推翻了旧结论

| 旧判断 | 实测 |
|---|---|
| 「路线 A：patch `GHomeApiBase.login` 让 SDK 登录当场成功，就能放行加载屏」 | **前提不成立**：登录入口在 Java 侧（`MainActivity.Login()`），而**游戏 C# 侧没有任何地方调用它**。`GHomeApiBase.login`(0x13EF18) 在整个 v7a 里**零直接调用点**；`GHomeSendManager::Login`(0xA7B5DC) 同样零 `bl` 调用点。⇒ 即使把 SDK 登录短接成成功，也**没有人去调它**，是空操作 |
| 「流程状态机 `FlowTrigger`/`FlowState` 静态从未初始化 ⇒ 状态机根本没被创建」 | **不对**。状态机工作正常：`FlowManager.Init → Fire → DoFire → DoTransition → FlowState.InvokeEntered` 全都跑到了，19:58 那轮一共发生了 **3 次状态迁移**。卡点在状态机**之后**的等待环节 |

---

## 2. 新能力：多点 logcat 打点器（一次运行拿全流程）

历史实验用「崩溃地址探针」，一次运行只能命中一个函数（且命中即崩溃）。
本阶段改成 **cave 探针**：把目标函数入口替换成 `b cave`，cave 里补做原序言 → 打一条 logcat → 原样跳回 `target+4`。**不改变任何行为**，一次运行可观测 17 个节点。

### 2.1 cave 布局（44/53 字节，步长 0x38）

```
target:      b cave                 （替换原函数第一条 STMDB）
cave +0:     <原 STMDB>             （在 cave 里补做，保证栈帧一致）
cave +4:     push {r0-r3,lr}
cave +8:     mov r3, r0|r1          （要打印的入参寄存器）
cave +12:    mov r0, #4             （ANDROID_LOG_INFO）
cave +16:    add r1, pc, #20        → +44 (tag 的运行时地址)
cave +20:    add r2, pc, #20        → +48 (fmt 的运行时地址)
cave +24:    sub sp,sp,#4  /  nop   （AAPCS 8 字节对齐，见下）
cave +28:    bl __android_log_print  （PLT = 0x50F260）
cave +32:    add sp,sp,#4  /  nop
cave +36:    pop {r0-r3,lr}
cave +40:    b target+4             （接着原函数体）
cave +44:    "TAG"\0
cave +48:    "%08x"\0
```

代码洞：`GHomeSendManager::SwitchAccount`(0xA7B9EC) 到 `GHomeSendManager::EscapeGame`(0xA7BDFC) 之间
（本局永不调用）⇒ **可用 0x410 字节 = 17 个探针**。

### 2.2 踩过的坑（都是新的）

1. **`add rX, pc, #imm` 的 imm 只有 8 位有效**（更高位是 rotate 字段）。
   我曾把 tag/fmt 放进**共享字符串区**，偏移最大到 964 → `0xE28F1000|0x3C4` 把 rotate 字段也改了
   ⇒ 算出的 tag 指针是垃圾 ⇒ `__android_log_print` 崩在启动早期，**整个客户端在 Unity 起来前就挂住**
   （症状：只有 `SystemInfo CPU = ...` 两行、没有任何请求、`Timeout while trying to pause the Unity Engine`）。
   ⇒ **字符串必须内联在每个 cave 里**（偏移 ≤ 0xFF），或改用 `movw/movt`（不可行，ASLR）。
2. **AAPCS 栈对齐**：原序言压 n 个寄存器 + cave 自己压 5 个（20 字节）。
   `n` 为偶数时总偏移 `4*(n+5)` 是 4 的倍数但不是 8 的倍数 ⇒ 要再补 4 字节；
   `n` 为奇数时已经 8 对齐 ⇒ **补 4 反而破坏对齐**，必须改成 `nop`。
3. 字符串指针必须用 **pc 相对**算运行时地址（写 RVA 会跳到未映射地址，ASLR）。
4. **logcat 抓取**：`adb logcat -d -t 4000` 会把旧 buffer 一起带出来；
   过滤要用 `I TAG +: value` 的真实格式（tag 后是空格补齐到 8 字符再 `:`），
   写成 `" TAG: "` 会**一条都匹配不到**（本轮就踩了一次，看输出全是 0 误以为探针失效）。

### 2.3 脚本

| 文件 | 作用 |
|---|---|
| `build_vtrace.py` | 打点器生成器（17 探针，逐条 capstone 自校验） |
| `build_vtrace.sh` | 打包 + zipalign + 签名（沿用 v6 bundle 直连补丁 + 当前 `build/classes.dex`） |
| `run_vtrace.sh` / `run_vtrace2.sh` | 起后端 + adb reverse + 拉客户端 + 抓打点日志 |
| `log_vtrace.txt` / `log_vtrace2.txt` | 两轮实测原始日志（证据） |

---

## 3. 实测轨迹（round 2，19:58:41 ~ 19:58:53）

```
19:58:41.922  TMI  a7a5bf00   FlowManager::Init
19:58:41.990  TFI  a7a71960   FlowTrigger::Fire      ┐
19:58:41.990  TMF  a7a5bf00   FlowManager::Fire       │ 3 个触发器
19:58:41.990  TMD  a7a5bf00   FlowManager::DoFire     │
19:58:41.997  TFI  a7a71950   FlowTrigger::Fire      │
19:58:41.997  TMF/TMD                                 │
19:58:42.001  TFI  a7a71970   FlowTrigger::Fire      │
19:58:42.001  TMF/TMD                                 ┘
19:58:42.002  TMT  a7a5bf00   FlowManager::DoTransition   ← 状态迁移 #1
19:58:42.002  TIE  a7a5b578   FlowState::InvokeEntered
19:58:43.549  TSL  994798c4   FlowState::StartupLoadComplete
19:58:43.550  TSA  989edbc0   FlowState::StartupLoadCompleteAndBackendAvailable
19:58:43.550  TMS  a7a5bf00   FlowManager::StartupInit
19:58:43.550  TFI  a7a71ac0   FlowTrigger::Fire
19:58:43.550  TMF/TMD
19:58:43.552  TMT  a7a5bf00   FlowManager::DoTransition   ← 状态迁移 #2
19:58:43.552  TIE  a7a5ba50   FlowState::InvokeEntered
19:58:43.556  TAO  916dd200   AbstractMenu::OnOpen (context = 0x916dd200，非空)
19:58:47.842  TFI  a7a71ab0   FlowTrigger::Fire
19:58:47.842  TMF/TMD
19:58:47.844  TMT  a7a5bf00   FlowManager::DoTransition   ← 状态迁移 #3
19:58:47.844  TIE  a7a5b460   FlowState::InvokeEntered
19:58:47.849  TAO  00000000   AbstractMenu::OnOpen (context = NULL!!)
19:58:47.849  TCT  91d3c828   AbstractLoadingMenu::ClearTip
19:58:47.849  TST  91d3c828   AbstractLoadingMenu::SupportsTips
19:58:53.387  TS2  84058ff0   FlowState::SetupGameManagers
                                    ↓ 之后再无任何打点，客户端静默停住
```

**关键缺失**：`TPI`(PlayInAnim) / `TIN` / `TOU` / `TAU`(Authenticate) / `TOS`(OnSaveDataFetched) **一次都没命中**。

---

## 4. 根因（已定位到具体指令）

`AbstractLoadingMenu::OnOpen`（v7a **0xB1852C**）反汇编：

```
0xb1857c  mov r0, r4
0xb18580  bl  0xb1868c      ; ClearTip(this)        ← TCT
0xb18584  cmp r6, #0        ; r6 = context
0xb18588  popeq {r4,r5,r6,r7,fp,pc}   ; ★ context == null → 直接 return
...
0xb18678  b   0xb18790      ; PlayInAnim()          ← TPI（到不了）
```

实测 `TAO` 打印的 context = **0x00000000** ⇒
**加载屏以 null context 被打开 ⇒ `OnOpen` 提前 return ⇒ `PlayInAnim` 从不执行 ⇒
Animator 不播 ⇒ `onInAnimDone` / `onOutAnimDone` 回调永不触发 ⇒
`FlowState::WaitForLoadingMenuInAnimation`（**0x800D54**）永挂 ⇒
流程走不到认证（`AuthenticationServiceImplementation::Authenticate` 0x11A1C64 从未被调用）
⇒ `/auth/gplus` 从未发出。**

### 4.1 context 的来路（已追到调用点，丢失点待定）

```
FlowState::OpenLoadingMenu(TaskControl)            0x8007C4
  ├─ new Action(onInAnimDone) / new Action(onOutAnimDone)
  ├─ bl 0xB1898C  LoadingMenuContext::.ctor(tip, onInAnimDone, onOutAnimDone)   ← context 在这里被创建（非空）
  └─ 0x800B74  bl MenuRequestManager::OpenMenu(menuInfo=r7, context=r6, onOpen=r4, null, null)
                  └─ 0x9CCB94 MenuRequestManager::OpenMenu
                       ├─ r6 = r2 = context（保存）
                       ├─ bl 0x9E192C  MenuRequest::.ctor(menuInfo, context, onOpen, extra)  ← context 进 request
                       └─ b 0x9E22F8   （把 request 交给菜单管理器处理）
```

⇒ **context 在 `FlowState::OpenLoadingMenu` 里是非空的**，是在「菜单请求 → 真正打开菜单」这一段
（`MenuRequestManager` → `MenuRequest` → 菜单框架 → `LoadingMenu.OnOpen` → `AbstractLoadingMenu.OnOpen`）
**被丢成 null 的**。

---

## 4. 第四~七轮：最终定位（2026-10-29 20:52）

### 4.0 【第七轮·决定性】状态名已识别：流程停在 `LoadingBaseGameContent`

第七轮新加的 `TCO`（`FlowState::.ctor` 入口）打点共 **86 次**，与 dump.cs 里
**86 个 `public static FlowState` 字段一一对应** ⇒ **`.cctor` 的创建顺序 == 静态字段声明顺序**
⇒ 打点序号可以直接换算成状态名。得到：

```
20:51:08.782  TIE a1035578  → TCO#59 = LoadStartupScene        (0x158)
20:51:10.309  TIE a1035a50  → TCO#28 = InitializingBundles     (0x84)
20:51:14.757  TIE a1035460  → TCO#66 = LoadingBaseGameContent  (0x174)   ★ 卡在这里
20:51:14.758  T23           b__222_223 命中
20:51:14.758  TERR          "zhang  InitializingGame show"   ← 开发者自己的标签，与实际状态名不完全一致
20:51:21.987  TLOG          "Wwis"（游戏普通日志）
20:51:22.127  TSG           FlowState::SetupGameManagers
20:51:22.131  TERR          "zhan…"（★ 第二条 LogError，之前没看到）
```

**⇒ 真实流程链：`LoadStartupScene → InitializingBundles → LoadingBaseGameContent → [卡住]`**
（开发者的调试字符串写的是 “InitializingGame”，但 FlowState 里其实没有这个字段，标签是旧的。）

**⇒ `LoadingBaseGameContent` 的下一步应该是 `UninitializingGame`(0x178)，再往���才是
`Authenticating`(0x16C) / `FetchingSaveData`(0x170)。**
候选触发器：`LoadBaseGameContent`(0x110) / `BundlesInitialized`(0x10C) / `LoadingComplete`(0x9C)。

完整 86 个状态名与静态偏移表见 `Library/Locus/tmp/blades/flowstate_fields.md`
（**TCO 打点序号 = 该表行号**）。

### 4.1 菜单请求管线（已实测完整跑通）

```
FlowState::OpenLoadingMenu / 某个 transition handler
  → MenuRequestManager::OpenMenu(info, additionnalData, onOpen, onClose)   0x9CCB94
      → new MenuRequest(info, additionnalData, onOpen, ...)                 0x9E192C
      → MenuRequestManager::AddRequest(request)                            0x9E22F8
          → HandleRequest(request)                                         0x9E326C
              → OpenMenu(MenuRequest)                                      0x9E4570
                  → MenuManager::OpenMenu(menuInfo, context)                0x123DD30
                      → AbstractMenu::Open(context)                        0x15A5F60
                          → AbstractMenu::OnOpen(context)                  0x123B4BC
                              → AbstractLoadingMenu::OnOpen(context)       0xB1852C
```

第三轮实测（round 3）证明 **context 在管线里被忠实传递**：
`TRQ(入口 r2) = TRC(ctor r2) = TMO(MenuManager r2) = TOM(Open r1) = TAO(OnOpen r1)` 全部同值。
⇒ **null 不是菜单框架丢的，是调用方直接传的 null。**

### 4.2 「null context」是硬编码的，不是意外

`FlowState::ShowAuthenticationLoadingScreen`(0x7FDE44) 在 0x7FDF84 处 `mov r2, #0` 硬传 null。
**44 个 `MenuRequestManager::OpenMenu` 调用点里有 44 处都写着 `mov r2,#0`**（静态扫描得到），
其中属于 FlowState 的有 7 个（`b__222_20/142/146/149/152/223/247`）。
⇒ **「context 为 null」本身是游戏的设计，不是 bug**（我第一轮的「null context 就是根因」判断是**误判**）。

### 4.3 真正的卡点（第六轮拿到错误原文）

用「无序言版」cave 直接读 `Il2CppString`（`{klass, monitor, length, chars[]}`，chars 在 +12，UTF-16LE）
读出 `UnityEngine.Debug::LogError`(0x1F9E808) 的消息前几个字符：

| 轮次 | 读到 | 解读 |
|---|---|---|
| 6a | `0x0068007a 0x006e0061` | chars[0..3] = `zhan` |
| 6b | `0x00200067 0x00490020` | chars[4..7] = `g  I` |

⇒ 完整开头 = **`zhang  I…`**。
在 `string_literals.txt` 里匹配到 **开发者调试前缀 `zhang `** 的字面量（这是原作者 zhāng 的标记）：

```
6720   zhang  ShowAuthenticationLoadingScreen
13878  zhang  InitializingGame show
13879  zhang  InitializingGame close
13880  zhang  UninitializingGame show
13881  zhang  UninitializingGame close
```

`chars[7] == 'I'` ⇒ 排除 `ShowAuthenticationLoadingScreen`('S') 与 `UninitializingGame`('U')
⇒ **错误消息 = `zhang  InitializingGame show`**。

而且该池字（`0x02149940`）**全库只出现 2 次**（0x10E95FC 在 `b__222_223` 内，另一处是数据区重复），
`FlowState::ShowAuthenticationLoadingScreen`(0x7FDE44..0x7FE05C) **并不引用它** ⇒ 确认是 `b__222_223` 独有。

### 4.4 最终因果链（全部有打点实证）

```
20:39:46.396  TFI ×3                    FlowTrigger::Fire（3 个启动触发器）
20:39:46.410  TIE  a10de578            FlowState::InvokeEntered（状态 #1）
20:39:48.101  TIE  a10dea50            FlowState::InvokeEntered（状态 #2）
20:39:48.104  TMO                       打开一个普通菜单
20:39:52.504  TIE  a10de460            FlowState::InvokeEntered（状态 #3）
20:39:52.504  T23  a10e3f80            ★ b__222_223 命中（InitializingGame 的 transition handler）
20:39:52.504  TERR  "zhang  InitializingGame show"   ← Debug.LogError
              ... 之后完全静默：TPI(PlayInAnim)/TA1(Authenticate)/TSA(StartAuthentication) 全部 0 次
```

**⇒ 第 3 个进入的状态是 `InitializingGame`（FlowState 静态 0x168）。**
它的 transition handler `b__222_223`(0x10E94C4)：
1. `Debug.LogError("zhang  InitializingGame show")`（**注意用的是 LogError 而不是 Log**），
2. 直接调 `MenuRequestManager::OpenMenu(menuInfo, **null**, null, null)` 打开加载屏。

而 `AbstractLoadingMenu::OnOpen`(0xB1852C) 的开头是：

```
0xb1857c  bl  ClearTip
0xb18584  cmp r6, #0            ; r6 = context
0xb18588  popeq {r4,r5,r6,r7,fp,pc}   ★ context==null → 提前 return
...
0xb18678  b   0xb18790          ; PlayInAnim()   ← 到不了
```

⇒ **context 为 null ⇒ `OnOpen` 提前 return ⇒ `PlayInAnim()` 从不执行 ⇒ Animator 不播 ⇒
加载屏的动画完成回调（`onInAnimDone` / `onOutAnimDone`）永远不触发 ⇒
`InitializingGame` 的初始化任务拿不到信号 ⇒ `zhang  UninitializingGame close` 永不打印 ⇒
流程永远到不了认证（`Authenticate` / `/auth/gplus` 从未发出）。**

### 4.5 顺带确认：SDK 登录路线彻底作废

`GHomeSendManager::Login`(0xA7B5DC) 打点 **全程 0 次命中**（第三/四/五轮都验过）
⇒ **游戏 C# 侧从不发起渠道 SDK 登录** ⇒ 「短接 SDK 登录让会话成功」这条路线**确定无效**，不要再碰。

### 4.6 候选修法（第八~九轮已修正：A 已被证伪，B 是唯一正解）

> **第九轮证伪了「修法 A」**：`OnInAnimDoneEvent`/`OnOutAnimDoneEvent` 在 `_context` 为 null 时会**抛异常**，
> 所以强行让 `PlayInAnim()` 执行并不能走通。
>
> **修法 B（唯一正解）**：让 `b__222_223`(0x10E94C4) 用**带回调的 `LoadingMenuContext`** 打开加载屏，
> 即复制 `FlowState::OpenLoadingMenu`(0x8007C4) 的“造两个 `Action` + `new LoadingMenuContext`”那段。
> - **B1**：把 `b__222_223` 在 0x10E95E0 的 `bl 0x9CCB94` 改成调用 `FlowState::OpenLoadingMenu`。
>   前置：确认 `OpenLoadingMenu` 的 `TaskControl` 参数是否真的被解引用（r5 目前只存了一下）。
> - **B2**：在 cave 里直接复制 `OpenLoadingMenu` 里“造两个 Action + 造 context”的机器码，
>   再自己调 `MenuRequestManager::OpenMenu(manager, menuInfo, context, null, null, null)`。
>   难点：`Action::.ctor` 需要一个 target（`FlowState.<OpenLoadingMenu>b__0`），得先从 dump.cs 找到它。
>
> **修法 C（点火路线，时机已经对了）**：在 `b__222_223` 之后读触发器持有者并 Fire
> `LoadBaseGameContent`(0x110) / `BundlesInitialized`(0x10C) / `LoadingComplete`(0x9C)。
> 静态访问形态（照抄 `FlowState::StartAuthentication` @0x8019EC）：
> ```
> ldr r0,[pc,#V]      ; V = FlowTrigger 静态块的 usage 偏移（池字 @0x801A9C = 0x2A09E64）
> ldr r0,[pc,r0]      ; 槽地址 = 0x8019F8 + V = 0x320B85C
> ldr r0,[r0]         ; 静态块
> ldr r1,[r0,#0x5c]   ; 触发器持有者（里面存的是**指向触发器对象的指针**）
> ldr r2,[r1,#0x110]  ; r2 = LoadBaseGameContent 触发器对象
> bl  FlowTrigger::Fire
> ```
> ⚠ 历史 v31~v45 失败原因不是形态不对，而是**采样时机太早**（登录回调时刻静态块还没初始化）。

### 4.6b 旧候选 A（已证伪，保留供追溯）

**修法 A（最小、最对症）**：让 `AbstractLoadingMenu::OnOpen` 在 context==null 时**也执行 `PlayInAnim()`**。
做法：把 `0xb18588` 的 `popeq` 改成 `b cave`，cave 里 `bl 0xB18790`(PlayInAnim) 后
`pop {r4,r5,r6,r7,fp,pc}`（原函数序言压的就是这 6 个寄存器）。
⚠ 风险 1：`PlayInAnim` 在 `_animator` 为 null 时会抛异常（`0xb187d8 bl 0x7c0060`）。
⚠ 风险 2：context 为 null 时 `_context` 也是 null，`OnInAnimDoneEvent`/`OnOutAnimDoneEvent`
会取 `_context.OnInAnimDone` ⇒ 可能 NRE。**所以 A 只在“动画本身就是等待点”时才成立**，
需要先确认 `LoadingBaseGameContent` 这块屏是不是在等动画完成。

**修法 B（治本）**：让 `b__222_223` 打开加载屏时**带一个真正的 `LoadingMenuContext`**
（带 onInAnimDone / onOutAnimDone 回调），即照 `FlowState::OpenLoadingMenu`(0x8007C4) 的做法构造。
难点：原生侧构造托管对象（历史上 5 次栽在这条路上）。

**修法 C（点火路线，但时机已经对了）**：在 `b__222_223` 之后（**此时 FlowTrigger 静态块已初始化**）
读触发器持有者并 Fire `LoadBaseGameContent`(0x110) / `BundlesInitialized`(0x10C)。
静态访问形态（照抄 `FlowState::StartAuthentication` @0x801988）：
```
ldr r0,[pc,#V]      ; V = FlowTrigger 静态块的 usage 偏移
ldr r5,[pc,r0]      ; r5 = &静态块
ldr r0,[r5]         ; r0 = 静态块
ldr r1,[r0,#0x5c]   ; r1 = 触发器持有者
ldr r2,[r1,#0x110]  ; r2 = LoadBaseGameContent
bl  FlowTrigger::Fire
```
⚠ 历史失败原因（v31~v45）不是这个形态不对，而是**采样时机太早**（在登录回调时刻静态块还没初始化）。
现在知道正确时机 = `b__222_223` 执行之后。

**修法 D（观察用，优先做）**：把 `Debug.LogError` / `Debug.Log` 的消息**多读几个字符**
（当前只读 chars[8..11]），把游戏自己的流程旁白完整抓下来；
同时给 `FlowState::LoadGameDataLibrary`(0x8029A4)、`FlowState::CloseLoadingMenu`(0x800BE0)、
`FlowState::UninitializingGame` 相关方法加打点，确认“关闭”侧到底等的是谁。

### 4.7 新增坑

1. **`Debug.LogError` 的输出在 logcat 里完全看不到**（整轮日志里 0 条 `E Unity`；
   本轮 Unity tag 只有 D/I/W）。⇒ 想知道错误文案只能**自己从 Il2CppString 里读**。
   ⇒ 但**游戏自己的 `Debug.Log`（不是 LogError）是能在 logcat 里看到的**（本轮抓到了 5 条），
   只是内容被 Unity 包了一层 stack trace。
2. **读 Il2CppString 的 cave**（`str2` / `str2r` 模式）：
   `ldr r3,[rN,#12+off*2]`（chars[off..off+1]）+ `ldr r2,[rN,#12+(off+2)*2]`（chars[off+2..off+3]），
   第二个变长参数放 `[sp]`。
   ⚠ **cave 必须先补做原函数的序言（pro）**，否则栈帧不一致会**静默破坏后续执行**
   （第六轮就因为漏了 pro，`b__222_223` 打完 LogError 之后没有继续调 OpenMenu，
    一度看起来像“流程在 LogError 处就断了”）。
   ⚠ **AAPCS 对齐**：pro 压 n 个 → 再压 5 个 → 还要给变长参数留栈空间 ⇒
   `sub sp,#12`（n 偶数）或 `sub sp,#16`（n 奇数）。
   ⚠ **tag 长度必须和 fmt 的偏移联动**（`fmt_off = code_len + len(tag) + 1`）——
   4 字符 tag 配死偏移会�� fmt 变成空串（表现为「tag 打出来了但消息是空的」）。
3. **cave 里 `add rX, pc, #imm` 的 imm 是相对量**，写成 `TAG_OFF - (base + 32)` 会算成负数；
   正确写法是 `TAG_OFF - (指令偏移 + 8)`。
4. **tag 长度断言只对普通 cave 生效**，`str2` 分支在断言之前 return ⇒ 4 字符 tag 会静默通过并撑坏 fmt 偏移。
5. **`FlowState::set_Name`(0x807138) 没有 STMDB 序言**（小 setter），标准 cave 跳不过去；
   状态名改用 `FlowState::.ctor`(0x807150) 的打点序号 × dump.cs 字段顺序来换算。

---

## 4A. 第八~九轮：读到全部旁白，锁定「等待加载屏动画」

### 4A.1 九轮完整时间线（每行都有打点实证）

```
22:51:03.328  TFI  a14f0960        FlowTrigger::Fire
22:51:03.367  TFI  a14f0950
22:51:03.372  TFI  a14f0970
22:51:03.372  TIE  a14da578   → TCO#59 = LoadStartupScene        (0x158)
22:51:03.434  TLOG "Initialia"      游戏旁白：Initializing…
22:51:03.439  TLOG "0"
22:51:03.441  TLOG "Ghome "         Ghome init is 0
22:51:05.091  TLOG " locola"        localization get _l[…]
22:51:05.277  TFI  a14f0ac0
22:51:05.279  TIE  a14daa50   → TCO#28 = InitializingBundles     (0x84)
22:51:14.279  TFI  a14f0ab0
22:51:14.284  TIE  a14da460   → TCO#66 = LoadingBaseGameContent  (0x174)   ★
22:51:14.284  T23  a14dff80        b__222_223
22:51:14.284  TERR "nitial"         = "zhang  InitializingGame show"
22:51:23.228  TLOG "WwiseU"         Wwise 音频初始化
22:51:23.389  TLD  999ef948        FlowState::LoadGameDataLibrary  ★ 启动任务在跑
22:51:23.403  TSG  840e73c0        FlowState::SetupGameManagers
22:51:23.411  TERR "art pu"         = "zhang start push norification"  ★ 也是开发者调试旁白，不是真错误
22:51:23.894  TLOG " locola"
             ← 之后完全静默：TCL(CloseLoadingMenu)=0、TPI/TIN/TOU=0、TAU/TSA/TOS=0
```

### 4A.2 结论修正（重要）

1. **两条 `Debug.LogError` 都是开发者用错 API 的调试旁白**，不是真的错误：
   - 第一条 = `zhang  InitializingGame show`
   - 第二条 = `zhang start push norification`（push 通知流程）
2. **启动任务链其实跑完了**：`LoadGameDataLibrary` → `SetupGameManagers` → push 通知 → 本地化。
   ⇒ **不是“卡在任务里”，而是任务跑完后流程就不动了。**
3. **`FlowState::CloseLoadingMenu`(0x800BE0) 从未执行** ⇒ **加载屏从来没被关掉**。
4. ⇒ **流程在等加载屏的“动画播完”信号**：屏幕是 `b__222_223` 用 **null context** 打开的，
   ⇒ `OnOpen` 提前 return ⇒ `PlayInAnim()` 不执行 ⇒ Animator 不播 ⇒
   `OnOutAnimDoneEvent` 永不被触发 ⇒ 没人去关屏幕 ⇒ 流程停在 `LoadingBaseGameContent`。

### 4A.3 「修法 A」已被证伪（不能再用）

`AbstractLoadingMenu::OnInAnimDoneEvent`(0xB188D4) / `OnOutAnimDoneEvent`(0xB18914)：

```
ldr r4, [r0, #0x3c]     ; _context
cmp r4, #0
bne  ...
mov r0, #0
bl  0x7c0060             ; ★ _context 为 null → 直接抛异常
ldr r4, [r4, #0x8]       ; _context.OnInAnimDone
```

⇒ 即使强行让 `PlayInAnim()` 执行，动画播完时这两个事件也会 **NRE**。
⇒ **唯一正解是修法 B：让 `b__222_223` 用一个带回调的 `LoadingMenuContext` 打开加载屏**
（即 `FlowState::OpenLoadingMenu`(0x8007C4) 里的做法：`new LoadingMenuContext(tip, onInAnimDone, onOutAnimDone)`）。

`OpenLoadingMenu` 的实现要点（0x8007C4~0x800B7C）：
- 参数是 `TaskControl taskControl`（r0→r5，**开头只存了一下，未见解引用**）；
- 内部 `new FlowState`(`bl 0xA72A48`)、`new Tips`(`bl 0xA61684`)、
  造两个 `Action`（`Action::.ctor`）分别当 onInAnimDone / onOutAnimDone；
- `bl 0xB1898C` = `LoadingMenuContext::.ctor(tip, onInAnimDone, onOutAnimDone)`；
- 最后 `bl 0x9CCB94` = `MenuRequestManager::OpenMenu(manager, menuInfo, context, onOpenAction, null, null)`。

⇒ **下一步（B1）**：把 `b__222_223` 在 0x10E95E0 的 `bl 0x9CCB94` 改成走 `OpenLoadingMenu` 的同款序列。
备选（B2）：在 cave 里直接复制 `OpenLoadingMenu` 里“造两个 Action + 造 context”的那段机器码。

### 4A.4 新增工具结论

- **触发器持有者里存的是“指向触发器对象的指针”**，不是对象本身
  ⇒ 把 `Fire` 的 r0（触发器对象地址）减去持有者基址会得到无意义的巨大差值（实测 ~0x2E15B00）。
  要拿触发器名字，得**遍历持有者的指针表**（`ldr rX,[holder+off]` 与 r0 比对）。
- `str3` 模式读 `chars[off..off+5]`：3 个 32 位字 = r3 + 两个栈变长参数。

### 4A.5 第八~九轮新增的坑

1. **`str3`/`str2r` 的字符偏移曾经被忽略**（`sreg, coff = ... if how[0]=='str2r' else (0,0)`），
   导致 TERR 实际读的是 chars[0..5] 而不是 chars[8..13] —— 差 8 个字符，结论会完全错。
   ⇒ 读完务必**把 6 个十六进制字手工解码成字符**再下结论（本轮两次都是靠这一步才读对）。
2. **`ldr rX, [pc, #imm]` 的 imm 是字节偏移**，且**指令地址 + 8 才是 PC 值**；
   池字的位置必须和 `code_len + 数据区布局` 严格对齐（本轮 `off` cave 的 `ldr r2,[pc,#40]`
   实际指向 +60 而不是 +64 ⇒ 读到 tag 字符串当偏移 ⇒ SIGSEGV，客户端启动即死）。
   ⇒ **现在 cave 生成器里有布局自检 assert**（tag/fmt/W 的位置与 pc 相对立即数都要对得上）。
3. **`ldr rX, [pc, rM]` 寄存器形式用的是 rM，不是 rX**：`ldr r2,[pc,r2]` = `0xE79F2002`。
4. **`sub r3, r0, r2` 的机器码不确定**（0xE1803002 实际是 `orr`）⇒ 改成分别打印两个指针、离线相减。

---

## 4B. 第十轮：第一次“打补丁验证”，结果与结论（2026-10-29 23:50）

### 4B.1 先修工具与流程（这一轮开打前先做的）

1. **模拟器生命周期**：用 `ldconsole launch` 在**调用内部**拉起模拟器 ⇒ VM 进程是本次调用的子进程 ⇒
   Locus 每次调用结束回收整棵进程树 ⇒ **模拟器跟着死** ⇒ 每轮都要重开（“反复开关”的成因）。
   ⇒ 改成 `schtasks /create /sc once /st <HH:MM> /tr "…ldconsole.exe launch --index 0" /f`，
   由任务计划服务拉起 ⇒ VM 的父进程变成 `LdVBoxSVC.exe`（服务）⇒ **能活过调用边界**（已实测）。
   ⚠ 不要再在调用内部 `ldconsole launch`。
2. **跑测脚本的两个真 bug**（都造成“假失败”）：
   - 启动 activity 写错了：清单里的启动项是 **activity-alias** `com.shengqugames.blades.MainActivity`，
     真类 `com.shengqugames.blades.aligames.MainActivity` 未直接声明 ⇒ `-n` 指定真类会报
     “Activity class does not exist”。
   - `logcat -d -t 4000` 事后截取会把**启动阶段**的关键打点丢掉（系统噪声多）⇒
     改成**运行期间持续抓** `logcat -v threadtime > log.txt &`，结束时 kill。
   - `adb disconnect 127.0.0.1:5037` 会把 adb server 自己断掉 ⇒ 别把 5037 放进 disconnect 列表。

### 4B.2 修法 B1 的实测结果（**失败，但信息量很大**）

**补丁（两条指令）**：`b__222_223`(0x10E94C4) 末尾
```
0x10E95D8  mov r0, r4        →  mov r0, #0        （TaskControl 传 0）
0x10E95E0  bl  0x9CCB94      →  bl  0x8007C4      （改调 FlowState::OpenLoadingMenu）
```
理由：`FlowState::OpenLoadingMenu`(0x8007C4) 做的正是“正确的事”——造两个 `Action` 当
onInAnimDone/onOutAnimDone，再 `new LoadingMenuContext(tip, onIn, onOut)`，然后用
**非 null** 的 context 打开菜单。而且静态核对发现它**不引用** `TaskControl` 参数。

**实测**：
```
23:47:20.444  T23   a11a3f80      b__222_223 命中
23:47:20.444  TERR  "nitial"      = "zhang  InitializingGame show"
23:47:20.444  TOM   00000000      FlowState::OpenLoadingMenu 被调用（r0 = 我传的 0）
23:47:20.510  TERR  "…coute…"     ← 第二条 LogError
```
Unity 侧栈：
```
E Unity : NullReferenceException: Object reference not set to an instance of an object.
E Unity :   at FlowState.OpenLoadingMenu (TaskControl taskControl)
E Unity :   at FlowState+<>c.<.cctor>b__222_223 (StateMachine`2+Transition[TState,TTrigger] transition)
E Unity :   at Stateless.StateMachine`2+StateRepresentation[TState,TTrigger].ExecuteEntryActions
```
并且 **`SaveManager::GetPlayerLevel` / `GameDataLibrary::get_Tips` / `Tips::GetRandomTip` /
`Action::.ctor` / `LoadingMenuContext::.ctor` 五个探针全部 0 命中**
⇒ **它在第一个真实调用之前就 NRE 了** ⇒ **`TaskControl` 参数确实被解引用**（静态 grep 漏了，
 应该是走了接口/虚调用或我没匹配到的访问形式）。

**⇒ 结论：B1 作废**（造不出 TaskControl）。同时这次 NRE 反而**正面证实了根因**：
流程确实是在等 `OpenLoadingMenu` 造出来的那套“带回调的 context”，而 `b__222_223` 手里没有。

### 4B.3 真正的加载屏链路（三段 .cctor transition handler）

静态定位（`callsites_v7a.py`）：

```
FlowState::PrepareLoadingScreenTaskList (0x8003D4)  ← b__222_189 (0x10E53E8) / b__222_193 (0x10E6AF4)
FlowState::OpenLoadingMenu              (0x8007C4)  ← （由上面那个任务链驱动）
FlowState::CloseLoadingMenu             (0x800BE0)  ← b__222_190 (0x10E618C)
```

⇒ 游戏的**正确**打开加载屏方式是：
`transition → b__222_189/193 → PrepareLoadingScreenTaskList → 任务链 → OpenLoadingMenu(真 TaskControl) →
造带回调的 LoadingMenuContext → 开屏 → 动画完成回调 → 点触发器 → b__222_190 → CloseLoadingMenu`。
而实测 `OpenLoadingMenu` / `CloseLoadingMenu` **都从未执行** ⇒
**流程根本没走到“准备加载屏”那个 transition**。

`b__222_223` 是**另一个** transition 的 handler（它只 log + 用 null context 开一个简易屏，不点火），
所以它不是那条链的一环。

### 4B.4 剩下的三条路（收敛）

| | 做法 | 风险 | 判据 |
|---|---|---|---|
| **C1（现在最对路）** | 找出**离开 `LoadingBaseGameContent`、进入 `b__222_189/193` 那个状态**的触发器，并把它点起来 | 未知是哪个触发器；且**不能在状态机 tick 内点火**（v43 的重入式迁移坑） | pass = `PrepareLoadingScreenTaskList` / `OpenLoadingMenu` / `CloseLoadingMenu` 任一命中 + 出现新的 `TIE`；fail = 崩溃 |
| **C1'** | 先**静态**从 `FlowState::.cctor`(0x80771C，约 39KB) 抽出完整迁移表（触发器 → 目标状态），得到权威答案再动手 | 纯静态分析，无 build 周期 | 得到 27 条迁移 |
| **B2** | 在 cave 里搬 `OpenLoadingMenu` 的“2× `Action::.ctor` + `LoadingMenuContext::.ctor`”那段 | 已知**时机不对**：`OpenLoadingMenu` 在 `b__222_223` 的时刻会 NRE（`get_Instance()` 返回 null，0x800888 抛异常）⇒ 单靠搬代码解决不了“时机”问题 | — |

**⇒ C1' 是下一步**：先把迁移表抽出来（纯静态，不烧 build 周期），拿到“离开 `LoadingBaseGameContent` 的触发器”，
再决定从哪个**非 tick 上下文**（动画事件 / 网络回调）点它。

### 4B.6 迁移表怎么抽（已定位，静态，不烧 build 周期）

`FlowState` 类里带 `StateMachine.StateConfiguration<FlowState, FlowTrigger> config` 参数的
**`.cctor` lambda 恰好 86 个**，与 86 个 `public static FlowState` 字段一一对应 ⇒
**每个状态一个“迁移注册 lambda”**。

已认出的机器码形态（以 `b__222_0` @0x10CC3A8 为例）：
```
ldr r0, [r0, #0x5c]        ; FlowState 静态字段数组
ldr r5, [r0, #0x168]       ; ★ 源状态 = 静态偏移 0x168 = InitializingGame
...
ldr r0, [pc, #V]           ; 池字 → FlowTrigger 静态槽
ldr r0, [pc, r0]
ldr r2, [r0]               ; ★ 触发器（静态偏移 0x00 = AuthenticationPreFtueStart）
mov r0, r4                 ; config
bl  0x1E9F36C              ; config.Add(config, fromState, trigger, ...)
```
⇒ **每处 `bl 0x1E9F36C` 就是一条迁移**；`ldr rX,[r0,#0xNNN]` 给出源/目标状态的静态偏移，
`ldr r2,[r0]` 给出触发器的静态偏移 —— **两者都能直接换成名字**
（FlowState 偏移表见 `flowstate_fields.md`；FlowTrigger 偏移表见本文 §5.4）。

**⇒ 下一步就是这个**：写个脚本遍历 86 个 lambda，把 (源状态偏移, 触发器偏移, 目标状态偏移) 全抽出来，
得到权威的 27 条迁移表，再据此决定在哪个**非 tick 上下文**点哪个触发器。

### 4B.7 本轮踩到的坑（都写进工具了）

- `logcat -d -t N` 事后截取会丢启动阶段（见 4B.1）。
- `Action::.ctor` 的 methodPtr 来自 `ldr r0,[pc,r0]; ldr r2,[r0]` 的**运行时解析链**，
  静态分析只能看到“运行时才填好”，**不能凭静态判断它是不是 null**；
  这次 `TaskControl` 就是这么被漏掉的（静态 grep 不到任何使用）。
  ⇒ **凡是“参数会不会被解引用”的问题，都要用探针实测，不能只靠反汇编。**

---

## 4C. 第七轮（静态抽取 + 修法 N 验证，2026-10-30 00:40）

### 4C.1 迁移表已抽出（纯静态）

脚本：`Library/Locus/tmp/blades/extract_transitions.py`

- 迁移注册点：`FlowState` 里带 `StateConfiguration<FlowState, FlowTrigger>` 参数的 **86 个 `.cctor` lambda**
  （与 86 个 `public static FlowState` 字段 1:1），每处 `bl 0x1E9E9B8` = 一次 `config.Add(...)`，
  **共 180 处注册 → 80 条不同的状态对**。
- 机器码形态（`b__222_0` @0x10CC3A8 实测）：
  ```
  ldr r0, [r0, #0x5c]        ; FlowState 静态字段数组（119 个静态字段）
  ldr r6, [r0, #0x18]        ; ★ 源状态静态偏移
  ldr r0, [pc, #V]            ; ★ 池字（PC 相对）
  mov r1, r5 ; mov r2, r6
  ldr r0, [pc, r0]            ;   → 静态字段存储地址
  ldr r3, [r0]                ; ★ 字段值
  bl  0x1E9E9B8
  ```
- **FlowState 的 119 个静态字段里，找到了“等加载屏”的整套机制**（这是本轮最有价值的产出）：

| 偏移 | 字段 | 类型 |
|---|---|---|
| 0x11C | `_waitForMenuOutAnimationTaskControl` | TaskControl |
| **0x120** | **`_menuInAnimationCompleted`** | **bool** |
| 0x124 | `_loadingMenuControls` | AbstractLoadingMenu.ILoadingMenuControls |
| 0x128 | `_loadingMenuInfo` | MenuInfo |
| 0x140 | `taskControlWaitForAllTheRequestsToFinish` | TaskControl |
| 0x144 | `_initTaskControl` | TaskControl |

### 4C.2 修法 N（实测：不崩，但**无效**）

**假设**：流程等的是 `_menuInAnimationCompleted`；把它置 true 就能放行。
**做法**：hook `MenuRequestManager::OpenMenu`(0x9CCB94)，**仅当 additionnalData(r2)==0**（那次 null context 的加载屏）时，
把 `FlowState._menuInAnimationCompleted`（静态偏移 0x120）置 true。

**读 FlowState 静态块的链**（照抄游戏自己的写法）：
```
ldr r0, [pc, #A]      ; r0 = X'（纯数据字）
ldr r0, [pc, r0]      ; r0 = (本指令地址+8) + X'  → 静态槽地址
ldr r0, [r0]          ; r0 = FlowState 静态块
ldr r0, [r0, #0x5c]   ; r0 = 静态字段数组
mov r1, #1 ; strb r1, [r0, #0x120]
```
静态槽地址 = **0x320B854**（旁边的 0x320B85C 正是 FlowTrigger 的槽，差 8 字节，互相印证）。

**实测结果**：
```
00:38:33.485  T23  b__222_223
00:38:33.485  TERR "nitial"     = "zhang  InitializingGame show"
00:38:33.486  TMO  00000000      ★ OpenMenu 的 additionnalData = NULL（修复分支确实走到了）
00:38:42.542  TLD  LoadGameDataLibrary
00:38:42.556  TSG  SetupGameManagers
00:38:42.564  TERR "r art pu"    = "zhang start push norification"
   ← 之后仍然静默：TPI/TIN/TOU/TCL/TWA/TOL/TAU 全部 0 次；请求序列与基线完全相同
```
⇒ **补丁本身安全（不崩），但流程没有前进 ⇒ 修法 N 证伪。**
⇒ **等待点不是 `_menuInAnimationCompleted` 这个 bool**，而是
`_waitForMenuOutAnimationTaskControl`(0x11C) 那个 **TaskControl 的完成信号**
（由加载屏动画完成回调触发，而该回调因 `_context == null` 根本不存在）。

### 4C.3 结论：只剩一条正解

**修法 B（正解）**：让 `b__222_223` 用**带 onInAnimDone/onOutAnimDone 回调的 `LoadingMenuContext`** 开屏。
构造方式照 `FlowState::OpenLoadingMenu`(0x8007C4)：造两个 `Action`（target 用
`FlowState.<OpenLoadingMenu>b__0` @0xA72A50 等，methodPtr 靠运行时解析的
`ldr r0,[pc,r0]; ldr r2,[r0]` 链）→ `new LoadingMenuContext(tip, onIn, onOut)`。

**难点已经定位清楚**：
- `Action::.ctor(object, IntPtr)` = 0x1A8C62C；
- `Action` 的 target 是 `bl 0x7C0058`（`newobj`）造出来的对象；
- methodPtr 来自 `ldr r0,[pc,#V]; ldr r0,[pc,r0]; ldr r2,[r0]`（运行时解析，搬过去即可，池字要重算）；
- `LoadingMenuContext::.ctor(string, Action, Action)` = 0xB1898C；
- **时机**：`OpenLoadingMenu` 在 `b__222_223` 的时刻会 NRE（`get_Instance()` 返回 null，0x800888 抛异常），
  ⇒ 直接搬代码不行，**要么把它放到管理器就绪之后，要么只搬“造 context”那一段并传 tip=null**。

### 4C.4 本轮又踩到的两个 ARM 编码坑

1. **`ldr rX, [rY, #imm]` 的 Rt 在 bits[15:12]**：`0xE5901000` 是 **`ldr r1,[r0]`** 而不是 `ldr r0,[r0]`（0x1000 是 r1，r0 是 0x0000）。
   ⇒ 写这种指令时**必须反汇编自检**，不能凭印象。
2. **PC 相对池字要算准**：`ldr r0,[pc,r0]` 的目标地址 = (指令地址+8) + 池字值。
   我把 FlowState 静态槽算成了 0x224B854（正确是 0x320B854，**差了整整 0x1000000**），
   症状是 `fault addr e5900004`（执行流跑进了数据区）。
   ⇒ 现在静态槽地址会**从原始 .so 里读出来校验**（值必须落在 .data/.bss 区间）。

---

## 4D. 修法 B 的实现进度（cave 已写完，还差一个 4 字节基准）

**修法 B 的 cave**（`patch_b.py`，205 字节）把游戏自己造 context 的那段机器码（`OpenLoadingMenu` @0x8009A0~0x800A20）
搬了过来：造两个 `Action`（`newobj` + `Action::.ctor(this, null, methodPtr, null)`）+ `new LoadingMenuContext(tip=null, onIn, onOut)`，
然后把新 context 放进 `r2`（替换原来的 null），再原样继续 `MenuRequestManager::OpenMenu` 的函数体。
生成器已带 **44 条指令 + 6 项寄存器方向 + 分支目标**的反汇编自检。

**实测：仍崩溃，差 4 个字节。**
症状：`fault addr` 恒等于某个池字的值（0x0278FBA4/0x0278FBA8…）⇒
说明「读静态槽」那一步算出的地址偏了 4 字节，链路上读到的是相邻的池字。

**根因候选**：`ldr rX, [pc, rY]`（**寄存器偏移**形式）的 PC 基准与 `ldr rX,[pc,#imm]`（立即数形式）不同。
ARM ARM 对寄存器偏移形式的 PC 取值是 **指令地址+4**，立即数形式是 **+8**；
两边混用就会正好差 4 ⇒ 槽地址算错 ⇒ 指针乱飞。
（已用原始 .so 的两个候选槽内容做过对照，但 usage 槽数组太密，静态判别不出来。）

**⇒ 下一轮的干净做法（不再重算池字）**：
1. cave 里 `mov r5, #0`（tip=null），然后 **`bl 0x8009A0`** —— 直接复用游戏自己那段代码，
   让它用自己正确的池字把 context 造出来；
2. 把原函数 `0x800A20` 处的指令改成 `b <我的 cave2>`，这样造完 context 后能回到我的代码；
3. 在 cave2 里 `mov r2, r0`（用新 context 替换 null），恢复寄存器后返回 `OpenMenu` 的函数体。
⇒ **完全绕开 PC 基准问题**，因为池字全部由游戏自己的指令计算。

### 4D.1 本轮又踩到的 ARM 编码坑（都已写进自检）

| 坑 | 说明 |
|---|---|
| `ldr rX,[rY,#imm]` 的 **Rn 在 bits[19:16]、Rt 在 bits[15:12]** | 我把 `ldr r0,[r4]` 写成了 `0xE5904000`，实际是 `ldr r4,[r0]`；正确是 **0xE5940000** |
| `push/pop {…lr}` 的 **lr 是 bit14**（bit10 是 sl） | `push {r0-r7,lr}` = **0xE92D40FF**（我写成 0xE92D04FF ⇒ 存的是 sl 不是 lr ⇒ 函数返回到垃圾地址） |
| `str sb,[sp]`：sb=r9、sp=r13 | Rt=9 ⇒ **0xE58D9000**（我写成 0x58DB000 = `str fp`；0x58D1000 = `str r1`） |
| **`ldr rX,[pc,rY]` 同寄存器形式（Rd==Rm）ARM 规定 UNPREDICTABLE** | 原游戏这么写能跑，但我搬到洞里不稳 ⇒ 改用 `ldr r1,[pc,r0]` + `ldr r2,[r1]` 中转 |
| PC 相对的**池字基准** | 立即数形式 PC=指令+8；**寄存器偏移形式 PC=指令+4** ⇒ 差 4 字节就读飞 |

---

## 4E. 第十一轮（2026-10-30 上午～中午）：静态抽出迁移表 + 修法 C 打通流程 ★★

> **本节是当前最重要的进展**：流程卡点已从「不知道在等什么」变成「明确知道在等 `Authenticate` 触发器」，
> 并用一块小补丁把流程推过去了（客户端开始向后端发业务请求）。

### 4E.1 迁移表（权威版，纯静态抽出）

脚本：`Library/Locus/tmp/blades/build_transition_table.py` → `transition_table.txt`（**177 条**）。

**先纠正一处旧结论**：`Stateless.StateMachine.StateConfiguration<FlowState,FlowTrigger>::Permit`
的签名是 `Permit(__this, FlowTrigger trigger, FlowState destinationState, method)`
⇒ **r1 = 触发器、r2 = 目标状态**（旧文档里写反了，导致之前一直以为「源状态/目标状态」对调）。

**配置 lambda ↔ 状态的对应关系**：带 `StateConfiguration<FlowState,FlowTrigger>` 参数的
`.cctor` lambda 恰好 **86 个**，与 86 个 `public static FlowState` 字段一一对应，**按编号顺序**
第 N 个配置 lambda 归属第 N 个状态（`FlowState` 静态字段表见 `flowstate_fields.md`）。
抽出的表语义自洽，与运行时打点完全吻合：

```
NotStarted            --AppStart-->              LoadStartupScene
LoadStartupScene      --SetBuildVersionGLD-->    InitializingBundles
InitializingBundles   --BundlesInitialized-->    LoadingBaseGameContent
LoadingBaseGameContent --Authenticate-->         Authenticating      ★★唯一的出口
Authenticating        --FetchSaveData-->         FetchingSaveData
FetchingSaveData      --Authenticate-->          Authenticating
Bootstrap             --Authenticate-->          Authenticating
```

⇒ **`LoadingBaseGameContent` 的出边只有 8 条「进 Loading」的 + 1 条 `--Authenticate--> Authenticating`。**

### 4E.2 触发器发射点全表（含尾调用）

`FlowTrigger::Fire`(0xA7381C) 全二进制共 **137 处** `bl`/`b` 引用（早先只数 `bl` 漏了一半）。
把每处的触发器静态偏移换成名字后，启动链的三次发射是：

| 触发器 | 发射点 | 所在函数 |
|---|---|---|
| `AppStart`(0x160) | 0x7FC76C | `FlowManager::Start` |
| `SetBuildVersionGLD`(0x108) | 0x8027C0 | `FlowState::StartupLoadCompleteAndBackendAvailable` |
| `BundlesInitialized`(0x10C) | 0xA76A00 | `FlowState.<>c__DisplayClass48_0.<OnVariantCollectionLoaded>b__0` |
| **`Authenticate`(0xA4)** | 0x8053E8 / 0xA86ABC / 0x10D3388 | `FlowState::LoadFirstLevel` / `GameplayManager::IntroCutsceneDone` / `FlowState.<>c.b__222_41` |

⇒ **`Authenticate` 在启动路径上没有任何发射点** —— 流程就是在一个永远不会来的触发器上停住了。

### 4E.3 ★ PC 基准定案（之前两轮补丁都栽在这里）

**寄存器偏移式 `ldr rX,[pc,rY]` 的 PC = 指令地址 + 8**（不是 ARM 手册常写的 +4）：
同一函数里 `add r0,pc,r0`(@0x801998) 与 `strb r1,[pc,r0]`(@0x8019C0) 算出**同一个**地址 0x02A8B50F，
两者都用 insn+8。`build_vtrace.py` 的 `'off'` 模式（运行时验证过）用的也是 insn+8。

由此推出两个旧结论要翻案：

1. **`patch_b.py`（修法 B）崩在 4 字节**：`slot_of()` 用了 `(use_instr + 4) + V` ⇒ 槽地址偏 4 ⇒ 读飞。
2. **修法 N 其实从未被真正证伪**：`patch_n.py` 里 `FS_SLOT = 0x224B854`（正确值是 **0x0320B854**，
   差了整整 0x1000000）⇒ 那一轮只是往一个随机映射地址写了个字节，**「不崩但流程没动」是必然的**，
   不能据此断定「等待点不是 `_menuInAnimationCompleted`」。

### 4E.4 ★「取类指针链」必须原样照抄

IL2CPP 取类指针的链形如（以 `FlowState::StartAuthentication` 为例）：

```
0x8019ec  ldr r0, [pc, #0xa8]   ; 池字 V = 0x02a09e64
0x8019f0  ldr r0, [pc, r0]       ; ★ Rd==Rm（ARM 规定 UNPREDICTABLE，但游戏就这么写、就这么跑）
0x8019f4  ldr r0, [r0]           ; Il2CppClass*
```

⇒ **搬到补丁洞里必须连 `ldr r0,[pc,r0]` 一起原样搬**。本轮先按旧文档的「安全写法」改成
`ldr r1,[pc,r0]` + `mov r0,r1`，结果**取到了别的类**：发出去的对象是 0x91834900（不是触发器），
当场 SIGSEGV。改回原样形式后一次跑通。

搬运时只需把**池字的值**按指令位移差重算（与 PC 基准无关）：

```
V' = V + (A2_old - A2_new)      # A2 = 链里第二条指令的地址
```

这样有效地址逐位不变，链的行为与原代码完全一致。

### 4E.5 加载屏等待机制（dump.cs 权威字段）

| 静态偏移 | 字段 |
|---|---|
| 0x118 | `_waitForMenuInAnimationTaskControl` |
| 0x11C | `_waitForMenuOutAnimationTaskControl` |
| **0x120** | **`_menuInAnimationCompleted`（bool）** |
| 0x124 / 0x128 | `_loadingMenuControls` / `_loadingMenuInfo` |

`FlowState::WaitForLoadingMenuInAnimation`(0x800D54)：
读 0x120 → **为 true 就立刻 `TaskControl::SignalCompletion`**；为 false 则把 taskControl 存进 0x118 等
`FlowState::OnInAnimDone`(0x8013A0) 置 0x120=true 并 signal。

### 4E.6 修法 C（本轮实施，**已实测通过**）

| 洞 | 挂钩 | 动作 |
|---|---|---|
| A | `FlowState::SetupGameManagers`(0x803414) 入口 | 把 `FlowState._menuInAnimationCompleted`(0x120) 置 true（兼作「内容已就绪」标志） |
| B | `MainThreadDispatcher::Update`(0x1096484) 入口 | 标志为真 ⇒ 取 `FlowTrigger` 静态字段 **0xa4 = `Authenticate`** ⇒ 调 `FlowTrigger::Fire`(0xA7381C) ⇒ **清标志（只发一次）** |

选 `MainThreadDispatcher::Update` 是因为它是 **Unity 主线程的 Update**，
**不在状态机 tick 的调用栈里** ⇒ 不会触发 v43 那个「dispatcher tick 内点火」的重入崩溃。
两个洞里的类指针链都是从游戏自己的代码**原样搬**的（4E.4）。

**实测（13:18 最后一轮，已带「只发一次」修正）**：

```
13:18:21.982  TLD  LoadGameDataLibrary            ← 启动链跑完
13:18:22.457  FIRE : 09096484                     ← ★ 我们发的 Authenticate（**全程只出现一次**）
13:18:22.457  TFI  a1174c50                      ← 状态机收到触发器
13:18:22.459  TIE  a115e4b0                      ← ★ 进入新状态
13:18:22.460  TFI  a1174d70                      ← 状态机接着发的下一个触发器
⇒ 客户端发出 POST /api/game/v1/public/characters//towns/current/rewards/current
   （带 Blade-Signature 头，Host: bladesqahk.jijiagames.com）—— 后端第一次收到业务请求 ✓✓
```

- 无崩溃，进程存活 2 分钟以上；
- **`/auth/gplus` 仍未发出**（`AuthenticationServiceImplementation::Authenticate` 探针 TAU 仍 0 次）：
  状态机是 `Authenticating ⇄ FetchingSaveData` 循环，认证发生在取存档之后，
  而存档请求现在拿到的是空响应 `{}` ⇒ 流程停在等响应。
- 发射出去的对象地址 **0x09096484** 与启动期那五个触发器（0xa1f5c9xx 区）**不在同一块**：
  ⇒ FlowTrigger 的 90 个对象并非全部相邻分配（静态字段的创建顺序/分配顺序与地址顺序不一致），
  **不要用「相邻地址」反推触发器名字**（本轮差点因此误判）。
- 「只发一次」的实现：发完把 `_menuInAnimationCompleted` 清回 false；
  ⚠ 清理必须放在 **Fire 之前**（`FlowTrigger::Fire` 会毁掉 r1，而静态字段数组指针就在 r1）。

### 4E.7 本轮踩到的坑（都已写进工具）

| 坑 | 说明 |
|---|---|
| **洞区重叠** | `build_vtrace.py` 把洞区前 **0xD0** 字节（0xA7B9EC..0xA7BABC）预留给补丁洞，探针洞从 0xA7BABC 开始。补丁洞放错位置 ⇒ 覆盖了 TERR 探针 ⇒ `Debug::LogError` 跳进野代码 ⇒ **启动 1 秒后崩** |
| `strb/ldrb` 立即数 | 是「拆分立即数」编码（bit11=1，imm4 在 bits10-8，imm8 在 bits7-0）⇒ 手写极易错；**直接抄游戏自己的指令字再改寄存器字段** |
| `mov Rd,Rm` | Rd 在 bits15-12：`mov r0,r1` = **0xE1A00001**（写成 0xE1A01000 就是 `mov r1,r0`）⇒ 池字值留在 r0 ⇒ 下一条解引用直接崩（fault addr 恰是池字值） |
| `ldr rX,[rY,#imm]` | Rn 在 bits19-16、Rt 在 bits15-12；`ldr r4,[r0,#0xa4]` = 0xE59040A4（不是 0xE590A040） |

### 4E.8 下一轮

1. **后端侧（新的主战场）**：客户端现在会打 `/api/game/v1/public/characters/...` 家族；
   影子后端目前对未登记路径一律回 `{}` ⇒ 客户端拿不到有效数据就停住。
   按 `rest_spec.md`（117 路径 / 165 DTO）补齐这些响应。
2. 查清 `/auth/gplus` 为什么没发（TAU 0 次）：是 `Authenticating` 状态先取存档、还是需要先有会话。
3. 修法 C 的发射时机可以再往前挪（现在是「SetupGameManagers 之后」才发）。

---

## 4F. 第四阶段后半（2026-10-30 下午）：后端契约挖出来了 ★★

> 上一节把流程推过去了（客户端开始打后端），但它拿到响应后就不动了 ⇒ 这一节把「后端到底要回什么」挖出来。

### 4F.1 响应格式的真相：**客户端把响应当「路径 → 值」来解析，映射表就在 APK 里**

- `JsonFlatSchemaProvider`（`SCHEMA_EXTENSION = ".schema"`）与 `JsonConversionProvider` 读的是 **TextAsset**。
- 这些资产在 **`assets/bin/Data/sharedassets0.assets`** 里（35 个 TextAsset = **17 个 `.schema` + 若干 `.conversion`**）。
  ⚠ **APK 的 `assets/Bundles/*`（2963 个）和设备上下载的 1482 个 bundle 里都没有它们** —— 只在 `assets/bin/Data` 里。
- ⚠ **UnityPy 读不出这批资产的正文**（报 `string argument without an encoding`）⇒ 手工按 TextAsset 的
  序列化布局解析（两个 length-prefixed 字符串）：
  ```python
  n = struct.unpack_from('<i', raw, 0)[0];      name   = raw[4:4+n]
  m = struct.unpack_from('<i', raw, 4+n)[0];     script = raw[8+n:8+n+m]
  ```
  脚本：`extract_schemas_apk.py` / `build_api_responses.py`（在 `Library/Locus/tmp/blades/`）。

### 4F.2 `.schema` = 响应体的形状；`.conversion` = JSON 路径 → 持久化路径

**`.schema` 文本**（`town.schema` 节选）：

```json
[{"path":"","type":"{Object}"},
 {"path":"town","type":"{Object}"},
 {"path":"town/townLevel/level","type":"{System.String}"},
 {"path":"town/buildings","type":"{Array}"},
 {"path":"town/buildings/{id}","type":"{Object}"},
 {"path":"town/buildings/{id}/id","type":"{System.String}"}]
```

⇒ **响应体就是这些 path 组成的嵌套 JSON**；`{id}` 是数组下标占位；`{Object}`⇒{}、`{Array}`⇒[]、其余⇒""。

**`.conversion` 文本**（`session.conversion` 节选）= 操作树：

```json
{"operation":"OperationsContainer","operationsList":[
  {"operation":"UseSchema","schema":"session","operationsList":[
    {"operation":"Read","relativePath":"session/userId",
     "operationsList":[{"operation":"Write","schema":"BackendData","path":"Session/UserId"}]}]}]}
```

⇒ **JSON 路径 → 客户端持久化路径** 的对应关系；`Read` 的 `relativePath` 就是**响应体里的键** ✓。

### 4F.3 关键 schema 清单

| schema | 含义 | 关键路径 |
|---|---|---|
| `session` | 会话（/auth） | session/{userId,token,schema,loginToken,tokenExpirationSeconds,featureStatus,linkedAccountsStatus} |
| `town` | 城镇状态 | town/townLevel/{level,experiencePoints,levelUp,unlockedDistricts[]}、town/buildings[]{id,typeId,styleId,level,constructionEnd,isInConstruction,props} |
| `SaveGame`（100 条） | 全量存档同步 | ChestManager / Settings / Quests / Town / LevelManager / Player / Version / TownStats |
| `BackendData`（25 条） | 每个响应都读的那部分 | requestIndex、Session/{UserId,Token,…}、error/{serviceId,errorCode,details,debugInformation}、BackendCharacterData/{BackendId,Name,TagId,CharacterSave} |
| `Character` / `character` | 角色 | character/{userId,id,name,tagId,level,experience,equippedAbilities[],learnedAbilities{},extraData{}} |
| `GetCharacter(s)` / `CharacterData` | 角色查询 | id / extraData / SelectedUid / ShapeWeight / Scale |
| `chests` / `chest` | 宝箱 | chests[]{id,tier,unlockTime} |
| `inventory` / `GetCharacterWallet` | 背包 / 钱包 | inventory[]{id,count,metadata{}}、{accountCurrencies,characterCurrencies}[]{balance,currencyId} |
| `GetQuests` / `error` / `ErrorData` / `requestIndex` | 任务 / 错误 / 请求序号 | — |

⇒ **响应体没有外层信封** ✓：直接就是 schema 的根键（例如 `{"session":{…}}`）；
`requestIndex` 要回显客户端请求头里的 `Request-Index` ✓。

### 4F.4 通用响应策略（已落地）

把所有 schema 的根键合并成一个 JSON，对 `/api/*` 一律返回它 —— 客户端只读自己 schema 里的 path，
多余键无害 ✓。脚本：`build_api_responses.py`（生成 `shapes/api_universal.json`）→ `build_routes.py`（写 `shapes/R_auth.json`）。

**⚠⚠ 占位值必须按 schema 的类型给（实测踩坑）**：
客户端用 `JsonPersistentDeserializer.CreateOrUpdatePrimitiveTyped<T>` **把 JSON 里的字符串按 schema 类型解析**
⇒ 给空串给数值型路径会抛
`FormatException: Input string was not in a correct format`（栈：`OperationWrite.WriteValue` → `Operation.Exec`）
⇒ 生成规则：
| schema 类型 | 占位值 |
|---|---|
| `{System.Object}` | `{}` |
| `{System.Array}` | `[]`（元素由后续 `{id}` 路径补齐） |
| `{System.Int32/Int64/UInt32/...}` | `"0"` |
| `{System.Single/Double/...}` | `"0"` |
| `{System.Boolean}` | `"false"` |
| `{System.String}` | `""`（但关键字段要填真值） |

**实测（13:34 / 13:38 两轮）**：
- 带结构的响应一给，客户端**不再沉默**：它接着发了 `POST /api/analytics/v1/public/stats/client` ✓（之前是彻底静默）。
- 13:38 那轮：客户端确实进了响应解析，但被上面的 `FormatException` 打断 ⇒ 报错发生在
  `13:37:52`（= 我们 FIRE 的同一秒 ✓）⇒ **错误信息直接在 logcat 的 `E Unity` 里能读到** ✓✓（这是排查响应格式的黄金线索）。
- ⚠ 空占位值会让客户端拿不到可用数据（`BackendCharacterData.BackendId` 为空 ⇒ 下一个请求的
  角色 id 还是空 ⇒ `characters//towns/...` 这种双斜杠路径）⇒ 已把
  `character/{id,name,userId,level,experience}`、`session/*`、`requestIndex` 等填成可用值。
- ⚠ 客户端的**角色 id 不来自这个接口的响应**（`characterToBackendCharacterData.conversion` 只把
  `character/{id,name,tagId}` 写进持久化树 ✓）⇒ 要拿到角色 id，得让客户端走
  「取角色列表 / 建角」那条路（`GET /characters/`、`POST /characters` ✓ 都在契约里）。

### 4F.5 为什么 `/auth/gplus` 一直没发

客户端走的是 **`patch_v49.py` 注入的 SDK 登录**那条路（注入的 userid/ticket 被直接当会话用）✓
⇒ 不需要再走 `/auth/gplus` ✓。会话相关字段请按 `session.schema` 的**小写**键给（`userId`/`token`/…），
旧的 `Session`/`UserId` 那套大写键是早期猜测 ✗。

### 4F.6 ★★ 用 `TCO` 探针给「客户端进入了哪个状态」定名（可复用手法）

`FlowState::.ctor`(0x807150) 加探针后，logcat 里会按**创建顺序**打出每个状态对象的地址，
且地址每 40 字节（0x28）递减 ⇒ **序号 = (首地址 − 目标地址) / 0x28** ✓，
序号即 `flowstate_fields.md` 的行号（与运行时完全对得上：实测 LoadStartupScene=59 ✓、InitializingBundles=28 ✓、LoadingBaseGameContent=66 ✓）。

⇒ 于是能直接回答「客户端现在在哪个状态、它下一步要什么」：

```
TIE a110d578 → #59 LoadStartupScene
TIE a110da50 → #28 InitializingBundles
TIE a110d460 → #66 LoadingBaseGameContent
TIE a110d4b0 → #64 Authenticating   ← 我们发 Authenticate 后的落点（与迁移表预测一致 ✓）
```

⇒ 配合迁移表，`Authenticating` 的出边是 `--FetchSaveData--> FetchingSaveData` ⇒
客户端紧接着就该「取存档」⇒ **后端要回存档数据** ✓。

### 4F.7 ★★ 两个必须遵守的响应规则（实测踩坑）

1. **`Version` 必须给 `74`**：`SaveManager.DATA_VERSION = "Version"`、`SaveManager.CURRENT_VERSION = 74`
   ⇒ 存档版本对不上，客户端会**整份丢弃**存档 ⇒ 表现为「响应收到了但流程不动」✓✓（最难查的一种）。
2. **字符串值必须是合法数字**（除非确定是文本字段）：
   客户端用 `JsonPersistentDeserializer.CreateOrUpdatePrimitiveTyped<T>` 把 JSON 里的**字符串**按持久化字段类型解析
   ⇒ 空串/非数字给到 int 字段就抛
   `FormatException: Input string was not in a correct format`（栈：`OperationWrite.WriteValue` → `Operation.Exec`）
   ⇒ ⚠ **schema 写 `{System.String}` 的字段，持久化目标也可能是 int** ⇒ 只能靠「全是数字最安全」来兜底。
   当前策略：除 `session/{userId,token,schema,loginToken}`、`character/{userId,name}` 这几个确定是文本的键，
   **其余所有字符串叶子一律给数字串**（`build_api_responses.py` 里的 `numerify()` ✓）。

### 4F.8 「在等数据还是在等点击？」——已判定：**在等数据**

`tap_probe.sh`：启动 → 等 45 秒 → 打 5×5 网格 ×2 轮（共 50 次）→ 后端请求数 **0 增长** ⇒ 不是在等玩家点击 ⚠
（⚠⚠ **此实验必须在确认前台是游戏时才能做**；否则会点到桌面图标 —— 见 memory 里的「禁止盲点击」硬规则 ✗）。

### 4F.9 当前的具体阻塞点

- 流程已稳定走到：`LoadingBaseGameContent` →(我们发 Authenticate)→ `Authenticating` →(发 FetchSaveData)→
  客户端请求 `POST /characters//towns/current/rewards/current` ✓ → 拿到响应 ✓ → **不再发下一个业务请求** ✗。
- 异常定位：客户端的 `E Unity` 里在**状态迁移那一瞬**抛 `FormatException`（`OperationWrite.WriteValue`）
  ⇒ 与「持久化字段类型 vs 我们给的值」强相关 ⇒ 已按 4F.7 修正（Version=74 + 数值化），待下一轮验证 ✓。

---

## 4G. 第五阶段（2026-10-30 晚）：★ 洞 B 是承重的（推翻上一轮的结论） + 响应解析的两条路径

> ⚠ **本节标题里的「洞 B 是承重的」已被 4H 打折**：那一结论是在「`PREFS` 未写、客户端自己到不了 `Authenticating`」
> 的条件下测的；写了 `AndriodLoginStatus=1` 之后，客户端**自己**就能进 `Authenticating`（见 4H.3）。
> 本节其余内容（响应两条解析路径、集合形状、认证链测绘）仍然成立。

> 本节全是实测。**上一轮（handoff 续跑简报第 2/3 条）的两个结论都要改**：
> 「洞 B 是空操作、可以摘掉」是**误读**；「FormatException 已经修好」在本轮又换成了另一个异常。

### 4G.1 ★★ 洞 B 不是空操作 —— 上轮读错了探针输出

上一轮的依据是这两行：

```
15:11:00.829  FIRE    : 09096484     ← 洞 B 自己的日志
15:11:00.829  TFI     : 9f2b5c50     ← FlowTrigger::Fire 的探针
15:11:00.830  TIE     : 9f29f4b0     ← 进入 Authenticating
```

于是写成「发出去的对象 `0x09096484` 不是 FlowTrigger（真触发器在 a1f5cxxx 区）⇒ 洞 B 是空操作 ⇒ 可以摘掉」。

**根因：AAPCS 变长参数的落位搞错了。**

| 事实 | 说明 |
|---|---|
| **第 4 个参数槽是 r3**，第一个**栈**变长参数在 **[sp+4]** | `bl` 已经把返回地址压栈了 ⇒ 被调方读 vararg#1 时 sp 指向返回地址 |
| 洞 B 当时写的是 `str r2,[sp]` | ⇒ 被调方读到的 vararg#1 = **[sp+4]** = 洞 B 存的**fmt 地址**；而 **vararg#0 是 r3** |
| **r3 在洞 B 里从头到尾没被赋过值** | ⇒ 日志里那个 `09096484` 是**残留的 r3**，**根本不是被发射的触发器** |
| 探针器 `build_vtrace.py` 的 'reg' 模式把值放在 **r3** | ⇒ 探针器一直是对的（`TFI/TCO/TIE/TERR` 打的都是真值） |

⇒ 真正的触发器是同一毫秒里的 **`TFI 9f2b5c50`**，它和启动期那批触发器
（9f2b5960 / 9f2b5950 / 9f2b5970 / 9f2b5ac0 / 9f2b5ab0）**在同一个分配块里** ⇒ 是真的 FlowTrigger。

**本轮把洞 B 的日志改成 `mov r3,r4`（r4 = 真正取到的触发器）后，它打出的是：**

```
16:23:44.506  FIRE    : a11f4c50      ← ★ 真触发器（和那批启动触发器同一个块）
16:23:44.509  TIE     : a11de4b0      ← 进入 Authenticating
```

⇒ **洞 B 就是把流程从 `LoadingBaseGameContent` 推进到 `Authenticating` 的那一脚。**

### 4G.2 ★ 只留洞 A 的实验结果（用户指定的那一步）：流程停在 LoadingBaseGameContent

`patch_c_aonly.py`（新文件，只含洞 A）+ `build_ca.sh` ⇒ `build/blades_ca.apk`，实测：

| 探针 | 洞 A + 洞 B | **只留洞 A** |
|---|---|---|
| TIE（进入状态） | **4 次**（最后一个 = Authenticating） | **3 次**（停在 LoadingBaseGameContent） |
| TSF / TSD / TSR / TSG / TSC / TSB / TSH / TCH | 0 | 0 |
| TCL / TPO（加载屏关闭 / 出场动画） | 0 | 0 |
| 请求序列 | host_info → MetaManifest → `POST /characters//towns/current/rewards/current` → analytics | 同左 |

⇒ **只置标志（`_menuInAnimationCompleted = true`）不足以推进流程**：
游戏在启动路径上**不会自己**发 `Authenticate`（4E.2 已证：它的三个发射点都不在启动路径上）。
⇒ 下轮要**把洞 B 恢复**（`patch_c_fire.py`，日志已修正为打 r4）。

### 4G.3 ★★ 客户端解析响应有**两条路径**，对值的要求相反

这是本轮最值钱的一条：同一个响应体，客户端用**两种方式**解析，两种方式要的 JSON 类型不一样。

| 路径 | 入口 | 对值的要求 | 踩到的异常 |
|---|---|---|---|
| **A. 转换路径** | `.conversion` → `OperationWrite.WriteValue` → `JsonPersistentDeserializer.CreateOrUpdatePrimitiveTyped<T>` | 把 JSON 里的**字符串**按持久化字段类型再解析 ⇒ 数值字段要给**合法数字串** | `FormatException: Input string was not in a correct format.`（空串喂给 Int32/UInt32） |
| **B. 直接 cast 路径** | `JsonToPersistent.SequentialParser.Parse` → `PersistentHelper.AssignPersistentValue<T>` | 按 `.schema` **直接 cast** ⇒ 数值字段必须是 **JSON number**、布尔字段必须 true/false | `InvalidCastException: Unable to cast object of type 'String' to type 'Int64'.` |

**A 路径的根因（已修）**：`session.conversion` 把 `session/featureStatus`、`session/linkedAccountsStatus`
写进 `BackendData/Session/FeatureStatus`、`.../LinkedAccountsStatus`，而这两个字段在 `BackendData.schema` 里是
**{System.UInt32}** ⇒ 旧版 `build_routes.py` 硬编码的空串 `''` ⇒ **每一次响应都抛 FormatException**
（栈：`OperationWrite.WriteValue` → `Operation.Exec`）⇒ 这就是上一轮「拿到响应就静默」的真正原因。
⇒ 已改成 `'0'`，**FormatException 从此消失**（本轮两轮实测 0 条）。

**B 路径的根因（**尚未定位到具体字段**，但已缩小到「响应体里某个字符串被直接 cast 成 long**）**：

⚠ **本轮踩了一个很贵的坑：对照实验必须**彻底杀进程**。
`am force-stop` + `am start` 有时只是把任务切到后台，**`am start` 会恢复同一个 pid**
⇒ 上一轮的内存状态（已经被污染的持久化树）跟着进下一轮 ⇒ 对照实验作废。
（本轮前半段的「最小 body 仍然抛异常」就是这么被带偏的。）
`run_ca.sh` 现在会 `killall -9` 并**打印新旧 pid**，不一样就别信对照结果。

干净重启后的对照结果（每一行都是**干净重启**后跑的一轮）：

| 响应体 | 异常数 | 说明 |
|---|---|---|
| 全部 `/api/*` 返回 `{}`（`shapes/R_empty.json`） | **0** ✓ | ⇒ **异常确实来自响应体**；且此时 TIE 4 次 ⇒ 流程正常进 `Authenticating` |
| 全部 `/api/*` 返回 `{"requestIndex":"0"}`（`shapes/R_probe.json`） | 38 | body 里仅剩的那个字符串就是 `requestIndex` ⇒ **它被直接 cast 成 long** ⇒ 字符串 ⇒ 崩 ⇒ `requestIndex` 必须是 **JSON number** |
| 完整 body（requestIndex 已改 number） | 2718 | ⇒ **还有第二个祸首** |
| 完整 body（town/chest/wallet/inventory 的时间/计数字段改 number） | 2430 | 没用 |
| 完整 body（**所有看起来像数字的字符串**都改 number） | 2478 | 没用 ⇒ 祸首是一个**不是数字的字符串**（id/uid/name/token 之类） |

⇒ 剩下的嫌疑（下一轮继续）：`character/id`、`characters[0]/id`、`BackendCharacterData/BackendId`、
`Player/Character/SelectedUid`、`extraData` 里的 `SelectedUid`（**`.schema` 说它们是 String，
但手写 parser 可能 cast 成 long**）。
⇒ ⚠ `.schema` 对不上不代表没问题 ⇒ 必须**二分定位**（`shapes/R_probe.json` / `shapes/R_empty.json` 这套对照表就是为此准备的）。

**★ 续：把 id/uid 改成 number 之后，异常变成了另一个 ⇒ 定位前进了一大步**

| 响应体 | 异常 |
|---|---|
| 全部 id/uid 改成 number | `InvalidCastException: Unable to cast object of type 'List\`1' to type 'Dictionary\`2'.` |

⇒ **String→Int64 解决了**（⇒ 祸首确实是那些「被 parser 当成 long 的 id/uid 字符串」）
⇒ 但暴露出**下一个**问题：客户端在某个地方要 **Dictionary**，我们给的是 **List**。
⇒ 二分（干净重启）：
  - 去掉 town/chests/inventory/wallet/accountCurrencies/characterCurrencies/BackendCharacterListData/extraData/id
    ⇒ 异常仍在（2204 次）⇒ 祸首在剩下的根键里；
  - 再看剩下的根键，**唯一还是数组的就是 `characters`** ⇒
    ⇒ 嫌疑：客户端把 `characters`（以及 chests/inventory/wallet 这类 `{id}` 集合）
    **当字典（id → 元素）读**，而 `.schema` 写的是 `{Array}` ⇒ 两者不一致。
⇒ ⇒ **`.schema` 里的 `{id}` 既可能表示「数组下标」也可能表示「字典键」**，
  **光看 schema 猜不出来，必须二分** ⇒ 这也是本轮写 `check_response_types.py` 的原因，
  但它只能按 schema 校验 ⇒ **还需要二分对照表**（`R_probe.json` / `R_empty.json`）。

**★ 再续：把 `{id}` 集合改成「id 为键的字典」后，异常几乎清零**

| 响应体 | 异常数 | 异常 |
|---|---|---|
| `characters`/`chests`/`inventory`/`wallet`/两个 currencies **全改成字典** | **2** | `InvalidCastException: Unable to cast object of type 'PersistentObject' to type 'PersistentArray'.` |
| 只有 `characters` 改字典，其余保持数组 | **2** | 同上 |
| 再去掉 SaveGame 形状的根键 | 2 | 同上 |
| 再去掉 `chest`/两个 currencies | 2 | 同上 |

⇒ **结论**：
1. `characters` 这类 `{id}` 集合，客户端的 parser 要的是 **Dictionary**（id → 元素），
   而 `.schema` 写的是 `{Array}` ⇒ **schema 与真实 parser 不一致，必须靠实测**；
   （`characters` 给数组 ⇒ 抛 `List\`1 → Dictionary\`2`，上千次；给字典 ⇒ 这条错误消失）
2. 但**不是所有** `{id}` 集合都要字典（chests/inventory/wallet… 要 **Array**）⇒ 否则会反过来抛
   `PersistentObject → PersistentArray`。
⇒ ⇒ **每个 `{id}` 集合都得单独试**（这是本轮最花时间的一条经验）。
⇒ **当前状态**：异常从 **2478 条/轮** 降到 **2 条/轮**（重试风暴基本停息），
  剩下的 `PersistentObject → PersistentArray` 还没定位到具体根键
  （嫌疑集中在**对象型根键**：`character` / `town` / `session` / `BackendCharacterData`，
  以及 `characters` 改成字典后客户端仍可能按数组读它）。
  ⚠ 存档链探针（TSD/TSR/TSG/…）**仍是 0** ⇒ 存档回调还没启动 ⇒ 这是下一轮的靶心。

### 4G.4 后端这一轮补的东西

| 文件 | 变化 |
|---|---|
| `extract_contracts.py`（新） | **重写契约抽取**：只读 `sharedassets0.assets`（旧脚本扫 2963 个 bundle 白费时间）；按 **4 字节对齐**解析 TextAsset 的字符串（旧代码对不齐 ⇒ 读出垃圾长度，如 `session.conversion` 读成 109838336）。⇒ 现在能拿到**全部 35 个** TextAsset = **19 个 .schema + 15 个 .conversion**（其中 7 个是 0 字节的空转换） |
| `build_api_responses.py`（重写） | ① 响应体顶层给**字符串**（A 路径要）、**嵌套 JSON（CharacterSave / extraData）给原生类型**（B 路径要）；② `BackendCharacterData.BackendId='1'`、`Name='Hero'`、`TagId='0'`、**`CharacterSave` = 一份按 SaveGame.schema 生成的合法存档 JSON**；③ `character`/`characters` 给真实 id ⇒ 不再出现 `characters//towns/...` 双斜杠；④ `requestIndex` 给 number；⑤ 删掉 `error`（error.conversion 会把 serviceId/errorCode 写进 Int32 字段）与 `quests`（GetQuests.conversion 非空）两个危险根键 |
| `build_routes.py` | **不再硬编码 session**（旧版硬编码的 `featureStatus: ''` 就是 FormatException 的源头） |
| `server.py` | 新增**角色状态**：客户端 `POST /characters` 带来的 `characterSave`（它自己的存档 JSON）会被**存下来并在后续响应里回显** ⇒ `BackendCharacterData.CharacterSave` 里装的是客户端自己的真实存档，不是服务端编的 |
| `check_response_types.py`（新） | 把响应体按每个 `.schema` 逐路径校验 JSON 类型，报出所有不符项 |
| `build_vtrace.py` | 新增 **`'r3'` 探针模式**（打印入口 r0/r1/r2 三个寄存器；给 `OnSaveDataFetched(bool,bool,bool)` 用） |
| `patch_c_fire.py` | 洞 B 的日志改成 `mov r3,r4`（打真触发器，见 4G.1） |
| `patch_c_aonly.py` / `build_ca.sh` / `run_ca.sh`（新） | 只留洞 A 的实验分支 + 一键跑测（`INSTALL=0` 跳过安装、`CLEAR_SAVE=1` 清存档、`ROUTES=` 换路由表） |

### 4G.6 ★★ 后端响应的**集合形状**（`.schema` 完全不可信，全部实测出来）

**背景**：客户端解析响应有两条路（4G.3），B 路径按 `.schema` 直接 cast ⇒ `.schema` 说的形状/类型必须对。
但实测发现 **`.schema` 与手写 parser 不一致**，且 **`{id}` 集合有的要数组、有的要字典**：

| 集合 | `.schema` 写的是 | 客户端实际要 | 给错的后果 |
|---|---|---|---|
| `characters` | `{Array}` | **数组** ✓ | 给字典 ⇒ `PersistentObject → PersistentArray` |
| `chests` / `chest` / `accountCurrencies` / `characterCurrencies` | `{Array}`/`{Object}` | **数组/对象** ✓ | — |
| `inventory` / `wallet` | `{Array}` | **字典**（id → 元素） | 给数组 ⇒ `List\`1 → Dictionary\`2`（每轮 1300+ 条） |
| `BackendCharacterListData` | `{Array}` | **字典**（BackendId → 元素） | 同上 |

⇒ **逐个集合单独试**（每轮 ~90 s），靠异常条数的变化定位（2478 → 38 → 2 → **0**）。
⇒ 定位手法（很有效，值得复用）：
1. 先用 `check_response_types.py`（按 `.schema` 校验）排掉「明显类型不符」；
2. 再用**最小集**逐个加回根键（`shapes/R_probe.json` / `R_empty.json` 两张对照表）；
3. ⚠ **每一轮都要确认 pid 变了**（见 4G.3 的杀进程坑）。

**★ 定位完成后的状态**：完整响应体（23 个根键）**0 异常** ✓（`FormatException` 与 `InvalidCastException` 全部消失）。

### 4G.7 ★ 流程卡在 `Authenticating`：客户端发的触发器**没有迁移**

响应干净之后，用 `TFI`/`TIE` 探针看流程（探针集换成了 TSF/TSD/…/TCL/TPO/TCO/TIE/**TFI**）：

```
18:04:58.919 TFI a7868960   ← AppStart
18:04:58.936 TIE a7852578   ← LoadStartupScene
18:05:00.822 TIE a7852a50   ← InitializingBundles
18:05:06.798 TIE a7852460   ← LoadingBaseGameContent
18:05:15.678 FIRE a7868c50  ← 洞 B 发的 Authenticate
18:05:15.680 TIE a78524b0   ← Authenticating ✓
18:05:15.682 TFI a7868d70   ← ★ 客户端自己发的下一个触发器，**没有 TIE** ⇒ 被 OnUnhandledTrigger 静默忽略
```

⇒ 客户端在 `Authenticating` 里发了一个**该状态下无迁移**的触发器 ⇒ 状态机不动 ⇒
**存档链（`LoadSavedData` 等）一次都没启动**（TSD/TSR/TSG 全 0）。

⇒ **修法 C2（本轮新增）**：让洞 B **连发两个**：
`Authenticate`(0xa4) → 下一帧 `FetchSaveData`(0xac)
（把「下一个要发的触发器偏移」存在 `_menuInAnimationCompleted` 那个字节里：0xa4⇒下一个0xac；0xac⇒下一个0；
  该字节游戏只当 bool 读，非零值安全；只认 0xa4/0xac，被改写就自动停）。
⇒ 目的：**让存档链跑起来**，才能回答「存档回调到底停在哪」。

⚠ 这一步踩的编码坑（都写进自检了）：
| 坑 | 说明 |
|---|---|
| `mov r2,#0xa4` 的立即数在 **bits[11:0]** | 写成 `0xE3A02A04` ⇒ 实际是 `mov r2,#0xA04` ⇒ `strb` 只写低字节 ⇒ 阶段值变成 0x04 ⇒ 洞 B 的守卫拒绝 ⇒ **一个触发器都不发**（实测 TFI 只有启动期那 3 个）⇒ 正确值 `0xE3A020A4` |
| `ldr r4,[r?,r2]`（寄存器偏移形式） | 正确编码：**`ldr r4,[r0,r2]` = `0xE7904002`**（从原二进制里的真例子反推：`ldr r1,[r1,r2]` = `0xE7911002` ⇒ Rn/Rt 在 bits[19:16]/[15:12]、**Rm 在 bits[3:0]**）⇒ 我先按 ARM 手册推成 `0xE1314020`（capstone 解成 `teq`）|
| ★ **触发器要从哪个静态块取** | 触发器指针在 **FlowTrigger** 的静态块（`[TR class + 0x5c]`）里，不在 FlowState 的块里 ⇒ 我第一版写成了 `ldr r4,[r1,r2]`（r1 = FlowState 块）⇒ 取出的是 FlowState 的静态字段 ⇒ Fire() 了一个非触发器对象 ⇒ **两个触发器都白发了**（TFI 有、TIE 没有）⇒ 正确是 `ldr r4,[r0,r2]`（r0 = FlowTrigger 块）|
| 条件执行的助记符 | `cond=0000` 是 **EQ**、`0001` 是 **NE** ⇒ `0x03A02000` 是 `moveq` 不是 `movne` |

### 4G.8 ★★★ 存档回调终于跑起来了：TSF 命中，标志全 false

修好上面两个编码 bug 之后，完整时间线（干净重启、响应体 **0 异常**）：

```
18:27:09.850 TIE a121f578   ① #59 LoadStartupScene
18:27:11.542 TIE a121fa50   ② #28 InitializingBundles
18:27:16.001 TIE a121f460   ③ #66 LoadingBaseGameContent
18:27:22.296 TFI a1235c50      洞 B 发 Authenticate(0xa4)
18:27:22.298 TIE a121f4b0   ④ #64 Authenticating
18:27:22.306 TFI a1235d70      客户端自己发的触发器（**无迁移** ⇒ 被 OnUnhandledTrigger 静默忽略）
18:27:22.348 TFI a1235c30      洞 B 发 FetchSaveData(0xac)
18:27:22.349 TIE a121f488   ⑤ #65 FetchingSaveData     ★ 迁移成功！
18:27:22.350 TSF  00000000 00000000 00000000   ★★ OnSaveDataFetched 被调用
18:27:22.350 TFI a1235ad0
18:27:22.358 TIE a121fa50   ⑥ #28 InitializingBundles  ← 回退重载游戏数据
18:27:26.810 TIE a121f460   ⑦ #66 LoadingBaseGameContent（循环）
```

**状态名对照方法**：TCO 打出 86 个状态对象的创建序（地址每 0x28 递减），
`序号 = (首个地址 − 目标地址)/0x28`，序号即 `flowstate_fields.md` 的行号 ⇒
①~⑦ 依次是 LoadStartupScene / InitializingBundles / LoadingBaseGameContent /
**Authenticating** / **FetchingSaveData** / InitializingBundles / LoadingBaseGameContent。

**⇒ 「存档回调停在哪」的答案**：
`OnSaveDataFetched(saveGameFound, isAuthenticated, isNoUiAuthentication)` **被调用了，但三个标志全是 0**
⇒ 客户端的结论是「**没有存档 + 没有认证**」⇒
它放弃同步、**回退到 `InitializingBundles` 重载游戏数据**（= 走「新游戏/建角」那条路）⇒
然后又进 `LoadingBaseGameContent` ⇒ 下一轮需要再发一次 `FetchSaveData`
（洞 B 的 stage 已用完 ⇒ 只会发 Authenticate ⇒ 卡在 Authenticating ⇒ 循环）。

⇒ **下一轮的三件事**：
1. **让 `saveGameFound` 变成 true**（`isAuthenticated` 同理）——★ 静态已经看清了：
   `OnSaveDataFetched` 在整个二进制里**只有一个调用点**：
   `FlowState.<>c$$<.cctor>b__222_229`（0x10ea854），调用前是

   ```asm
   0x10ea83c  cmp   r4, #0
   0x10ea840  mov   r0, #0        ← saveGameFound = 0（硬编码）
   0x10ea844  movwne r4, #1
   0x10ea848  mov   r1, #0        ← isAuthenticated = 0（硬编码）
   0x10ea84c  mov   r2, r4        ← isNoUiAuthentication = r4
   0x10ea850  mov   r3, #0
   0x10ea854  bl    #0x801CE8     ← OnSaveDataFetched
   ```

   ⇒ **这个 lambda 就是「没有存档」那条分支**（两个标志写死 0）⇒
   客户端能走到它，说明它**根本没去取存档**（`SavedDataServiceImpl::LoadSavedData` 的探针 TSD = 0 ⇒ 确实没调）。
   ⇒ 所以要做的是**让客户端真的去加载一份存档**，而不是改这两个标志
   （设备上现在**没有任何本地存档文件** ⇒ `SaveManager.HasValidSave()` 必然 false）。
   ⇒ 路径：**先让客户端走「新游戏/建角」把存档建出来**（`server.py` 已经会把客户端带来的
   `characterSave` 存下来并在响应里回显 ✓），存档一旦存在，下一轮 `saveGameFound` 就该是 true。
2. **洞 B 改成循环**：发完 `FetchSaveData`(0xac) 后把 stage 置回 `0xa4`（而不是 0）⇒
   每次进 `Authenticating` 都能再触发一次 `FetchSaveData` ⇒ 存档回调反复触发，便于观察。
3. **`isAuthenticated` 的判定**：客户端进入 `FetchingSaveData` 后本应走
   `SavedDataServiceImpl::LoadSavedData`（TSD）⇒ 但它直接跳到了「没存档」分支 ⇒
   要查它判定「已认证 / 有存档」的条件（可能是 `SaveManager.HasValidSave()` + 会话状态）。

### 4G.9 ★ 为什么客户端会走「没存档」分支（静态 + 探针双向确认）

**探针结果**（14 个探针，含 TSL/TSV/TEU/TCN）：

| 探针 | 目标 | 命中 |
|---|---|---|
| TSL | `SaveManager.Load`（加载本地存档，0xFFF2A0） | **0** |
| TSV | `SaveManager.HasValidSave`（0x10001D8） | **0** |
| TEU | `EulaStatus.IsEulaShown`（0x8B2CE4） | **0** |
| TCN | `SaveManager.CreateNewSave`（0xFFFB3C） | **0** |
| TSD/TSR/TSG/TSC | `SavedDataServiceImpl` 的取存档链 | **0** |
| TSF | `OnSaveDataFetched` | ✔（三个 bool 全 0） |

⇒ **客户端一次都没碰过存档系统**：既没加载本地存档、也没去后端同步、也没建新存档、也没查 EULA。

**静态结论**：
- `AuthenticationServiceImplementation.IsAuthenticated()`（0x11A1BE8）
  = `session._loginType == 1` ⇒ `session._loginType` 是 `Session.LoginType`
  （0=None 1=FirstParty 2=BNet 3=Anonymous …）⇒ 客户端的 session 一直是 **None** ⇒ **没认证**。
- `GHomePostManager.LoginCallBack`（字段 0xC）**全二进制没有任何 C# 订阅者**
  （dump.cs 里只有字段声明）⇒ SDK 登录结果虽然被 `LoginResultCallBack`(0xA796A0) 收到
  （TicketStr/UserStr 都写了），**却没人把它接到后端认证上**
  ⇒ 客户端永远不会去调 `/auth/gplus` ⇒ 后端会话建不起来。
  ⇒ **交接文档里「客户端走 SDK 登录注入那条路 ⇒ 不需要 /auth」这个结论是错的。**
- `SavedDataServiceImpl` 里 `SyncResult` 的处理器 `FlowState.<>c$$<.cctor>b__222_77`
  会把 `Success(0)` 变成 **`CharacterNotCreated(2)`**（`orr r0, r4, #2`）⇒
  **「同步成功但没有角色」= CharacterNotCreated** ⇒ 就是「没存档」分支。
- `OnSaveDataFetched` 在整个二进制里**只有一个调用点** `b__222_229`（0x10ea854），
  调用前把 `saveGameFound` / `isAuthenticated` **写死成 0** ⇒ 这是「没有存档」的专用分支。

**两次否定实验（都无效，说明卡点更靠前）**：
1. **预置 PlayerPrefs 的 EULA 键**（18 个候选键全写 1）⇒ 流程**完全不变** ⇒ EULA 不是墙
   （因为 `IsEulaShown` 压根没被调用）。
2. **强制 `IsAuthenticated()` 返回 true**（把 `movwne r0,#0` 改成 `mov r0,#1`）
   ⇒ 流程**完全不变** ⇒ 「没存档」的判定不依赖它（它只是回调参数）。
   ⇒ 该 patch 已改成开关（`BLADES_FORCE_AUTH=1` 才启用），默认关。

⇒ ⇒ **结论**：客户端在**进入 `FetchingSaveData` 状态的那一刻**就判定「没存档」，
  连本地存档都没查 ⇒ ⇒ 判定依据不是存档也不是 EULA，而是**「没有后端角色/会话」**
  ⇒ ⇒ 要往下走，得让客户端**真的拿到后端会话**（`/auth/gplus`）并**建出第一个角色**。

### 4G.11 ★★ 认证链测绘 + 「AndriodLoginStatus」开关 + IL2CPP 运行时 API

#### (1) 认证链完整测绘（`transition_table.txt` + `FlowState.StartAuthentication(trigger)`）

`StartAuthentication(trigger)`（0x801988）是**按 trigger 身份分支**的 ⇒ 我们一直发的 `Authenticate`(0xa4)
**不是认证入口**。完整链（`transition_table.txt` 第 1~9 行）：

```
Authenticating --AuthenticationPostFtueStart(0x04)--> AuthenticationPostFtueStart
AuthenticationPostFtueStart --AuthenticationPostFtueSetup(0x0c)--> AuthenticationPostFtueSetup
AuthenticationPostFtueSetup --AuthenticationPlatformSignIn(0x10)--> AuthenticationPlatformSignIn  ★ 平台登录在这一跳
AuthenticationPlatformSignIn --AuthenticationPlayGame(0x30)--> AuthenticationPlayGame
AuthenticationPlayGame --FetchSaveData(0xac)--> FetchingSaveData
```
（`Authenticating` 的合法出边只有 8 条：`FetchSaveData`(0xac)、`FetchSaveDataFromNoUiAuthentication`(0xb0)、
`AuthenticationPreFtueStart`(0x00)、`AuthenticationPostFtueStart`(0x04)、`SoftLaunchStart*`、`PlayTestStart`、`RegressionTestStart`。）

#### (2) ★★ 平台登录的开关在 Unity PlayerPrefs 里（键名带拼写错误）

`AuthenticationAndroid.GetLoginStatus()`（0x119F330）= **`PlayerPrefs.GetInt("AndriodLoginStatus", 0)`**
（游戏自己的拼写，少了个 d ⇒ `AuthenticationAndroid.LOGIN_STATUS_PLAYER_PREF`）
⇒ 1 = `LoginStatus.Success` ⇒ 客户端走「已登录」快捷路（直接拿 `GHomePostManager` 的 ticket/userid 去后端认证）。

⇒ ⇒ **实测：在 PlayerPrefs 写 `AndriodLoginStatus=1` 之后，客户端自己发出了 `Authenticate` 触发器**
（那一轮洞 A/洞 B 都没参与 ⇒ 客户端**自己**走完了内容加载并自己认证 ⇒ 这是本轮最大的进展）。
⇒ 写入方式：`run_ca.sh` 的 `PREFS=1`（在杀掉进程之后写，因为 Unity 退出时会把 PlayerPrefs 刷回去）。

#### (3) ★ libil2cpp.so **导出了 209 个 `il2cpp_*` 运行时 API**

```
il2cpp_class_from_name        il2cpp_class_get_field_from_name   il2cpp_field_get_value
il2cpp_object_new             il2cpp_runtime_invoke              il2cpp_string_new
il2cpp_domain_get             il2cpp_thread_attach               il2cpp_class_get_method_from_name
```
⇒ ⇒ **可以从 native 里直接调用/读写任意托管成员** ⇒ 这是下一阶段的「 heavyweight 工具」：
用它 `il2cpp_runtime_invoke` 调 `AuthenticationServiceImplementation.Authenticate()`（无参版，用它自己注册的回调），
就不需要自己造 `Action<AuthResponse>` delegate 了。
⇒ 探查脚本：直接读 APK 里 `lib/armeabi-v7a/libil2cpp.so` 的 `.dynsym`（`SHT_DYNSYM`，**注意 Rm 在 bits[3:0]**）。

#### (4) 洞 B 的台阶机制踩到的两个坑（都写进自检了）

| 坑 | 说明 |
|---|---|
| **游戏会覆盖我们的状态字节** | `_menuInAnimationCompleted` 在 **0x120**（1 字节 bool），游戏会写它 ⇒ 台阶计数器被清掉 ⇒ 只走了一步。⇒ 改存 **0x121**（bool 之后的 padding，游戏不写）✓ |
| **ARM 立即数编码的「十六进制加法陷阱」** | `sub r3,r2,#1` 我写成 `0xE2403001` ⇒ 实际是 `sub r3,**r0**,#1` ⇒ 索引恒为 0 ⇒ 永远只发第一级。<br>⇒ Rn/Rd 在 bits[19:16]/[15:12]，**`0x20000` 是 digit4（0x10000 位）不是 digit3** ⇒ 正确值 `0xE2423001`；<br>⇒ 同类：`add r3,r2,#1` = `0xE2823001`、`ldr r2,[r2,r3,lsl #2]` = `0xE7922103`（移位量在 **bits[11:7]**、Rm 在 bits[3:0]）。<br>⇒ **所有这类编码必须用 capstone 逐条自检**（本轮被抓了 4 次）。 |

### 4G.10 本轮的探针集（13 个，换掉了旧的 10 个）

```
TSF  FlowState::OnSaveDataFetched       0x801CE8  ← 靶子（r0/r1/r2 = saveGameFound/isAuthenticated/isNoUi）
TSD  SavedDataServiceImpl::LoadSavedData 0x15964B4
TSR  SavedDataServiceImpl::SyncRequestIndex 0x15964E8
TSG  SavedDataServiceImpl::GetAllCharacters 0x15978A0
TSC  SavedDataServiceImpl::LoadCurrentCharacterAfterGetAllCharacters 0x1597D24
TSB  SavedDataServiceImpl::BackendSyncronizationDone 0x1597BA8
TSH  SavedDataServiceImpl::OnGetCharacter 0x1598698
TCH  CharacterServiceImpl::OnGetAllCharacters 0x16124F0
TCL  FlowState::CloseLoadingMenu        0x800BE0   ← 用户点名
TPO  AbstractLoadingMenu::PlayOutAnim   0xB18858   ← 用户点名
TCO  FlowState::.ctor（状态创建序→名字） 0x807150
TIE  FlowState::InvokeEntered           0x7FD0E4
TERR Debug::LogError 的消息（chars[8..13]）0x1F9E808
```

⚠ 洞区布局：`0xA7B9EC..0xA7BABC` 给补丁洞（洞 A 60B + 洞 B 146B），`0xA7BABC..0xA7BDFC`（832B）给探针。
⚠ **过滤 logcat 要用 `" I (TAG1|TAG2) +: "`**（tag 补齐 8 字符！用 `": "` 过滤会一条都匹配不上——
   本轮就踩了这个坑，害我以为探针全灭）。

---

## 4H. 第五阶段后半（2026-10-30 19:00–20:00）：★ 洞 B 的挂钩点从没跑过 + 客户端自己走到 Authenticating

本轮跑测：`runAA`…`runGG`（七轮，全部是 `build/blades_c.apk`，`PREFS=1` 写入 `AndriodLoginStatus=1`）。

### 4H.1 六轮结果逐字相同（都是同一个「停」状态）

| 轮 | 启动时刻 | TIE | TSF | 后端请求 | 停止位置 |
|---|---|---|---|---|---|
| AA | 19:08 | **7** | **1**（`00000000 00000000 00000000`）| 4 条（多一条 `POST /api/analytics/v1/public/stats/client`；MetaManifest 拿了 2 次 ⇒ 客户端整轮重启过一次）| 进 `FetchingSaveData` 后回退 `InitializingBundles` |
| BB | 19:16 | 4 | 0 | 3 条 | `Authenticating` |
| CC | 19:27 | 4 | 0 | 3 条 | `Authenticating` |
| DD | 19:37 | 4 | 0 | 3 条 | `Authenticating` |
| EE | 19:44 | 4 | 0 | 3 条 | `Authenticating` |
| FF | 19:52 | 4 | 0 | 3 条 | `Authenticating` |
| GG | 19:58 | 4 | 0 | 3 条 | `Authenticating` |

BB–GG 的 3 条后端请求固定是：`GET /Android/MetaManifest.json`、`GET /china/Android/host_info_cn.json`、
`POST /api/game/v1/public/characters//towns/current/rewards/current`（**角色 id 为空**）。

⇒ **AA 是最后一个能复现「进 FetchingSaveData + TSF 命中」的构建**，而这些轮次之间唯一变化的只有洞 B 的实现
⇒ 「台阶 `0xa4`→`0xac`」版本是有效的，「每帧发 `0x04`」版本无效（原因见 4H.2）。
⚠ AA 那版 APK 已被 19:57 的 build 覆盖，**不可复现** ⇒ 以后每轮必须另存一份 APK。

### 4H.2 ★★ 洞 B 挂的 `MainThreadDispatcher::Update` 从未被调用

洞 B 挂在 `MainThreadDispatcher::Update`(0x1096484)，逻辑是「无条件每帧发」。
若函数真被每帧调用，140 秒应产生上千条 TFI；实测 GG 轮**全程只有 7 条 TFI**：

```
19:58:59.041 / .054 / .058   ← 启动期那 3 个触发器（AppStart 等）
19:59:00.626 / 19:59:05.015  ← 客户端自己在流程推进时发的
19:59:11.369  a11a8c50       ← ★ 客户端自己发的 Authenticate（2ms 后进 Authenticating）
19:59:11.372  a11a8d70       ← 客户端在 Authenticating 里发的下一个触发器（无迁移 ⇒ 被静默丢弃）
```

⇒ 结论：**一条都不属于洞 B** ⇒ 挂点死。
静态上吻合的解释：`MainThreadDispatcher : SingletonBehaviour<MainThreadDispatcher>`，
`Update` 是**实例方法** ⇒ 组件没有被创建就永远不会被 Unity 调用；它又是懒加载单例
（`GetOrCreateComponent<MainThreadDispatcher>` 在 dump 里只出现在泛型实例列表中）
⇒ 没人取过 `Instance` 就根本不存在 ⇒ 挂点不可能命中。
⚠ `dump.cs` 只有声明、没有调用点 ⇒ 静态**不能**证伪，必须用探针直接验（下一轮 TMU）。

### 4H.3 ★ 顺带核实：客户端自己就能走到 `Authenticating`

TIE 的 4 条（`序号 = (首个 TCO 地址 − 目标地址)/0x28`）依次是
`#59 LoadStartupScene → #28 InitializingBundles → #66 LoadingBaseGameContent → #64 Authenticating`。
最后那次迁移的触发器 `a11a8c50` 出现在 TIE 前 2ms，且洞 B 已证明是死的 ⇒ **只能由客户端自己发**
⇒ 4G.11(2) 的结论（`PlayerPrefs "AndriodLoginStatus"=1` 让客户端自己认证）**成立**。
⚠ 但它进入 `Authenticating` 之后发的触发器在该状态**无迁移** ⇒ 状态机依旧不动。
⚠ 同时：本轮 `TPA`(AuthenticationAndroid.Authenticate 0x119EFA0)、`TPC`(0x119DFC8)、
`TPB`(0x119F26C)、`TPL`(0x11A3560) **全部 0 命中** ⇒ 客户端从头到尾没调用过平台登录。

### 4H.4 ★ 工具坑：`run_ca.sh` 的汇总 tag 表与 `build_vtrace.py` 的 HOOKS 脱节

`build_vtrace.py` 当前真跑的 13 个探针是：
`TSF TSD TSR TSG TFI TPA TPC TPB TPL TCL TPO TCO TIE`（无 TERR）。
但 `run_ca.sh` 的 `TAGS` 里写着 `TSF TSD TSR TSG TSC TSB TSH TCH TCA TCL TPO TCO TIE TERR`
⇒ 汇总表把真正在跑的 `TFI/TPA/TPC/TPB/TPL` 漏掉，又列出了 6 个不存在的 tag
⇒ **只看汇总表会得出「TFI=0」的错误结论**（本轮差点由此误判）。
⇒ 正确口径：`grep -oE " I [A-Z]{3} +: " log_vtrace2.txt | sort | uniq -c`。

### 4H.5 ★ 下一轮的挂点：`FlowManager::Update`(0x7FC978)（已核实可直接换）

| 目标 | 地址 | 依据 |
|---|---|---|
| `FlowManager::Update`（挂钩点）| **0x7FC978** | 状态机宿主单例的 Update，必定每帧被调；在状态机调用栈**之外** ⇒ 重入安全 |
| 序言 | `E92D4C10 E28DB008` | 与现在的 `MainThreadDispatcher::Update` **逐字节相同** ⇒ cave 与自检全部不用改，只换常量 |
| `FlowTrigger::Fire`（发射）| 0xA7381C | 现有 cave 已用，签名 `void Fire()` |
| `FlowManager::Fire`（公开入口）| 0x7FCE68 | 备选；`_processingTrigger==true` 时只入队 `_nestedTriggers` |
| `FlowManager::get_State` | 0x7FBD60 | 无状态注入用：读当前 FlowState 对象指针 |
| FlowManager 字段 | `_processingTrigger` 0xC / `_nestedTriggers` 0x10 / `_stateMachine` 0x14 / `_debugState` 0x20 | dump.cs FlowManager（TypeDefIndex 6165）|

### 4H.6 ★ 无状态注入（建议尽早落地，取代 stage 字节）

stage 字节方案有两个先天问题：存放位置只能借 FlowState 静态块里的 padding（`0x121`），
且游戏会写它旁边的 `0x120`（已实测被清过一次）。
⇒ 改用：读 `FlowManager::get_State()` 拿到的**当前状态对象指针**，与 FlowState 静态块里
86 个状态对象（`flowstate_fields.md`，偏移 `0xc + 0x4*i`）逐个比对 ⇒ 再决定发哪条出边。
⇒ 完全无状态、支持流程循环、不依赖任何可写内存。

### 4H.7 洞区现状（已变）

| 区域 | 范围 | 大小 |
|---|---|---|
| 洞 A | `0xA7B9EC`..`0xA7BA2F` | 64B |
| 洞 B | `0xA7BA30`..`0xA7BADB` | 172B（含 6 项台阶表）|
| 探针区 | `0xA7BADC`..`0xA7BDFC` | 剩 **8B** ⇒ 再加探针必须先摘 |

⚠ 旧文档里的「探针从 `0xA7BAB4` 起」「洞 B 146B」都已过期（`build_vtrace.py` 现在 `PROBE_BASE = CAVE + 0xF0`）。

---

## 4I. 第六阶段（2026-10-30 20:30–21:05）：★★ 洞 B 修好 + 认证链打通（客户端首次真的认证）

本轮四轮：III 20:42 / IV 20:47 / V 20:56 / VI 21:02。构建：`build/blades_c.apk`（`build/history/libil2cpp_c_*.so` 逐轮归档 ✓）。

### 4I.1 ★★ 洞 B 的三个 bug（19:16–19:59 六轮白跑的真正原因）

| # | bug | 症状 | 修法 |
|---|---|---|---|
| 1 | **控制流被写成无条件 `b skip`**（旧版 `b_code[12] = 0xEA000000`）| 洞 B **每次都直接跳走**，一个触发器都没发过（TFI 只有启动期那几条）| 恢复 `beq`/`bhi` 语义，并给每条分支断言目标 |
| 2 | **取触发器写成 `add r4,r0,r2`** | 算出来是**槽的地址**，不是触发器指针 | 照抄游戏自己的 `ldr r4,[holder,#X]`（holder = 静态块 `[0x5c]`，游戏 0x801A04 就是 `ldr r3,[r1,#0xa8]`）|
| 3 | **栈没 8 字节对齐**（`push 5` + `sub 12` ⇒ sp ≡ 4 mod 8）| 调用托管方法可能踩 VFP 对齐 | 改 `push {r0-r3,r4,lr}`（24B ⇒ 仍 8 对齐）|

⚠ 修 bug 1 的同时发现：**旧挂点 `MainThreadDispatcher::Update` 并没有死** ——
`TMU` 探针实测它 **641 次/150 秒**（约每秒 4 次）✓。之前「挂点死了」的结论是错的（被 bug 1 误导）。
⇒ 洞 B 最终挂在 `FlowManager::Update`(0x7FC978)（状态机宿主单例，必定每帧被调，且在状态机调用栈之外）。

### 4I.2 ★★★ x86 模拟器的 ARM 转译层：**代码段只能 pc-relative 读**

把台阶表放在 `.so` 的代码段（0xA7BAAC）再用 `ldr r2,[r2,r3,lsl #2]` 取 ⇒ **SIGSEGV**：

```
E CRASH : signal 11 (SIGSEGV), code 1 (SEGV_MAPERR), fault addr 00a7baac
   r0 a4e66e60  r1 a4e67e00  r2 00a7baac  r3 00000000
   pc 08a7ba74   #00 pc 00a7ba74 libil2cpp.so
```
⇒ `pc` 就在洞 B 的 `ldr r2,[r2,r3,lsl #2]`，`r2` = 表基址、`r3` = 0 ⇒ 读的正是**代码段里的表首项**。

⇒ **规律**：转译层把 ARM 代码页当作**只执行**，`ldr rX,[pc,#imm]` 里的池字由转译层自己解析（能读），
而**计算地址读代码段**会真去访存 ⇒ 崩。
⇒ **解法**：台阶偏移改用 `mov r2,#0xa4` / `add r2,r2,#8` 直接算；更进一步改成
「**台阶字节直接存触发器静态偏移**」+ `ldr r4,[holder,X]`（holder 在堆里，可读）✓。

### 4I.3 ★★ 触发器身份反查法（以后不用再猜）

TFI 探针（`FlowTrigger::Fire` 0xA7381C）用 `'off'` 模式打印 `(触发器对象地址, holder 地址)`。
游戏自己的 holder 布局（tombstone 实证）：

```
holder+0x00 → a19f5ee0      ⇒ object(X) = holder[0] − 4X   （X = FlowTrigger 静态偏移）
holder+0x04 → a19f5ed0
holder+0x08 → a19f5ec0      （每 4 字节偏移，对象地址降 0x10）
```
⇒ 拿已知的一个偏移（`0xa4` = Authenticate）校准一次，就能把每条 TFI 日志换算成具体触发器：

| 偏移 | 触发器 | 本轮出现时机 |
|---|---|---|
| `0xa4` | Authenticate | 我们发的（进 `Authenticating`）|
| `0x04` | AuthenticationPostFtueStart | 我们发的 |
| `0x0c` | AuthenticationPostFtueSetup | 我们发的 |
| `0x10` | AuthenticationPlatformSignIn | 我们发的（随后客户端去后端认证）|
| `0x1c` | AuthenticationCreateAnonymousAccount | **客户端自己发的** |
| `0x30` | AuthenticationPlayGame | **客户端自己发的**（认证完成后）|
| `0xac` | FetchSaveData | **客户端自己发的** |
| `0x5c` | OpenMenu_BlockingWithPauseAndBlur | 客户端在 `Authenticating` 里发的（想开菜单 ⇒ 卡在这）|
| `0x104`/`0x108`/`0x10c` | UpdateBundles / SetBuildVersionGLD / BundlesInitialized | 启动期 |

### 4I.4 ★★★ 认证链打通（客户端首次真的向后端认证）

洞 B 台阶 `0xa4 → 0x04 → 0x0c → 0x10` 之后，完整时间线（20:42 轮）：

```
20:42:39.458  TIE  #64 Authenticating              ← 我们的 0xa4
20:42:39.503  TIE  #2  AuthenticationPostFtueStart  ← 我们的 0x04
20:42:39.505  TIE  #4  AuthenticationPostFtueSetup  ← 我们的 0x0c
20:42:39.507  TIE  #11 AuthenticationCreateAnonymousAccount  ← 客户端发的 0x1c
20:42:39.542  TFI  0x10                             ← 我们的 0x10（平台登录）
20:42:39.768  TFI  0x30 → TIE #13 AuthenticationPlayGame       ← 客户端（认证完成）
20:42:39.768  TFI  0xac → TIE #65 FetchingSaveData            ← 客户端
20:42:39.771  TSL  SaveManager::Load                        ← 卡在这里
```
认证请求（此前从未出现过）：
```
POST /api/authentication/v1/public/auth/gplus
{"details":{"user_id":1,"ticket":"1","channel":"gplus_cn"},"platform":"gp","language":"cn","deviceId":""}
```
⇒ 来源是 `PlayerPrefs "AndriodLoginStatus"=1` 的快捷路（`AuthenticationAndroid.Authenticate` 等 4 个探针仍为 0 ⇒ 没走 SDK 登录）✓
⇒ 我们的响应 `{"session":{...},"requestIndex":0}` **被接受**（紧接着客户端自己发了 `0x30` PlayGame）✓✓

### 4I.5 ★ 剩余卡点与已排除项

卡点：`FetchingSaveData` → `SaveManager::Load`（0xFFF2A0）调了一次 ⇒ 设备上**没有本地存档**
（`/data/data/com.shengqugames.blades.aligames/files/` 里只有 `.lock`）⇒ 读不到就停住；
十几秒后弹「**网络错误 / 发生未知错误，请重试**」，点「好的」**无效**（游戏不处理输入），
随后进程消失（**无崩溃签名**）。

已排除：
- **域名漏网**：设备 `/etc/hosts` 已把 `bladesasia/bladesqahk/bladesqagn/bladesqahw/qa.playbladesasia.com/…` 全部指向 127.0.0.1 ✓
- **城镇奖励响应的形状**：最小 body（`{requestIndex:0}`）与「客户端自己的 `characterSave` 格式」两种都产生**逐字相同**的行为 ⇒ 不是卡点
  （但顺手发现：`SaveGame.schema` 的根键比我们合并体多一个 **`Quests`**；且客户端自己写的存档里
  `Quests` 是**数组**、`Player.Character.SelectedUid` 与 `BackendCharacterData.BackendId` 是**字符串** ⇒
  合并响应体在存档端点上形状不兼容，已备好 `shapes/R_auth_savegame.json` 备用）
- **bundle 下载**：文件服务器只被请求了 `ManifestExtraData` ✓
- **本地存档缺失**：VII 轮（21:13）在设备上摆了 8 个候选路径的存档文件（内容 = 客户端自己的 `characterSave`）
  ⇒ 行为仍**逐字相同** ⇒ 路径或格式不对。
  存档路径线索：`SavedDataServiceImpl::GetSaveFilePath`(0x1593CE4) → `LocalDataManager::GetSavePath`(0x237B1F8)
  (folder, fileName, out folderPath)，常量在 `LocalDataConstants` / `LocalDataRootConstants`；
  metadata 里的字面量串相邻存放，疑似 `BackendDataJsonFullSaveData` + `.json`，目录名可能带 `Info.` 前缀。

### 4I.6 本轮工具改动

- `patch_c_fire.py`：洞 B 重写（台阶字节存偏移 + `ldr r4,[holder,X]` + 8 字节对齐 + 逐条 `op_str` 断言）；洞 A 改为「台阶字节 = 0xa4」；`CAVE_B` 移到 0xA7BA40
- `build_vtrace.py`：探针集换成 `TSF TFI('off') TPA TPC TPB TPL TCL TPO TCO TIE TMO TSL`（删 TSD/TSR/TSG/TMU，加 TMO/SL）
- `run_ca.sh`：`TAGS` 与真实探针集同步（之前列了 6 个不存在的 tag、漏了 5 个真有的）；
  增加**途中截图**（25/45/70/100/130s）+ 每点打 `free -m`；支持 `TAP="x,y" TAP_AT="25 50"`（点前校验前台是游戏）
- `build_c.sh`：每轮把 `libil2cpp.c.so` 归档到 `build/history/`（以前 APK 被覆盖，丢掉了最后一个能复现 TSF 的构建）

### 4J. 第七阶段续（2026-10-30 21:10–21:45）：★「网络错误」的来源、IL2CPP 字面量解码器、存档路径零件

本轮四轮：VIII 21:33（新探针 + 内置目录存档）、IX 21:37（通配符证书 + SNI 日志）、X 21:41（外置目录存档）。

### 4J.1 ★★「网络错误」= TLS 握手失败（Curl error 35）

VIII 轮第一次在 logcat 里看到：

```
E Unity : Curl error 35: Handshake failed. UnityTls error code: 7
```

⇒ 客户端有一个 HTTPS 请求**在握手阶段就失败** ⇒ 请求根本没进到 HTTP 层 ⇒ 我们的后端日志里自然没有它 ✓（之前一直以为“客户端没发请求”✗）。

原因：我们的自签证书 SAN 只列了几个具体主机名，**主机名不匹配的连接会被客户端的 TLS 栈拒绝** ✓。
⇒ 修法：用同一张 CA（`ca.key` 在手 ✓）**重签** `server.crt`，SAN 换成通配符：
`*.jijiagames.com` / `*.jijiagames.com.cn` / `*.playbladesasia.com` / 具体主机名 / localhost / 127.0.0.1 ✓
⇒ IX 轮起该错误消失 ✓。

★ **顺带给 `server.py` 加了 SNI 日志**（`ctx.sni_callback`）：握手阶段就能打印客户端到底连的是哪个域名
⇒ **握手失败的请求也能被看见** ✓（这是排查 TLS 类问题的最便宜手段）。

### 4J.2 ★★★ 写了 IL2CPP 字面量解码器（`find_literals.py`，可复用）

Unity 2019 / metadata v24 的头布局：

| 字段 | 位置 | 内容 |
|---|---|---|
| `stringLiteralOffset/Size` | vals[2]/[3] | 字面量**索引表**（每项 8 字节 `{uint32 length; uint32 dataOffset}`）|
| `stringLiteralDataOffset/Size` | vals[4]/[5] | 字面量**文本段**（**无 NUL 分隔**，紧挨着存 ⇒ 很容易和名字表混淆）|
| `stringOffset/Size` | vals[6]/[7] | **名字表**（NUL 分隔）|

⇒ 工具能：按关键词列出全部 15398 条字面量（带索引）、按文本反查索引。
⇒ 局限：代码里 `ldstr` 并不是 PC-relative 直接取字面量（而是经由 per-method 的 metadata 结构），
   所以**「字面量 → 代码位置」的反查没做成** ✓（只做了「文本 → 索引」这一半 ✓）。

### 4J.3 ★ 存档路径的字面量零件（已定位到文件名与目录名）

`SaveDataLocalStorage`（TypeDefIndex 9703）的常量与字段：

```
public const string FILENAME_SAVE = "Save_";          // 字面量索引 12080
public const string FILE_EXTENSION_BINARY = ".bin";
public const string FILE_EXTENSION_JSON = ".json";
public const string FILE_EXTENSION_JSON_SCHEMA = ".schema.json";
public static readonly string FILENAME_SAVE_0;        // 运行时初始化 ⇒ 由 "Save_" + "0" 拼
public static readonly Dictionary<int,string> FileExtensionByDataFormat;
private string _cachedPersistentDataPath;
```
⇒ 另找到字面量 `'LocalData'`（索引 8940）与 `'FullSaveData.json'`（索引 8610）
⇒ 最可能的文件名：**`Save_0.FullSaveData.json`**（`FILENAME_SAVE_0` + `FileExtensionByDataFormat[format]`）✓

### 4J.4 ★★ 一个关键认知修正：Unity 在 Android 上的 persistentDataPath 是**外置**目录

设备上的真实布局：

```
/storage/emulated/0/Android/data/com.shengqugames.blades.aligames/files/   ← persistentDataPath（Unity/、il2cpp/、Bundles/ 都在这）
/data/data/com.shengqugames.blades.aligames/files/                         ← 内置目录（只有 SDK 的 .lock 和 dns/xtq）
```
⇒ 之前把存档候选文件摆在内置目录 ✗ ⇒ 必然无效 ✓（VII 轮已证）。
⇒ X 轮把 15 个候选文件摆到外置目录（`files/LocalData/` 与 `files/` 两处，含
`Save_0.FullSaveData.json` / `Save_0.json` / `FullSaveData.json` 等）⇒ **仍然无效** ⇒
**剩下的是目录名或文件格式不对**（格式候选：嵌套 JSON / 扁平 schema JSON / 二进制三种 ✓）。

### 4J.5 ★ 新探针的结论：客户端**从不**向后端要存档

VIII 轮起探针集含 `TSD`（`SavedDataServiceImpl::LoadSavedData` 0x15964B4）与
`TSG`（`GetAllCharacters` 0x15978A0）⇒ **两者始终 0 命中** ✓
⇒ 客户端根本没有发起后端取存档/取角色的请求 ⇒ 卡点确实在**本地存档**这一侧 ✓✓（不是后端数据形状 ✗）。

### 4J.6 本轮工具改动

- `find_literals.py`（新）：IL2CPP 字面量解码器 ✓
- `server.py`：加 `ctx.sni_callback` 打印 TLS SNI 主机名 ✓
- `server.crt` 重签（通配符 SAN，同一 CA ✓；旧证书备份为 `server.crt.bak`/`server.key.bak` ✓）
- `build_vtrace.py`：探针集改为 `TSF TFI(off) TSD TSG TPA TPB TCL TCO TIE TMO TSL`（砍掉 TPO/TPC/TPL 腾位置）

---

## 4K. 第八阶段（2026-10-30 21:45–22:20）：★★★ 模型修正——取存档走**后端**，本地文件那条路从未被调用

### 4K.1 ★★★ 存档不是从本地文件读的（推翻前几轮的整个方向）

给本地存档相关函数加了**字符串探针**（直接读 Il2CppString 的前 6 个 UTF-16 字符 ✓）后：

| 探针 | 目标 | 命中 | 含义 |
|---|---|---|---|
| TSD | `SavedDataServiceImpl::LoadSavedData` (0x15964B4) | **1** | 客户端**确实调用了后端取档** ✓ |
| TSG | `SavedDataServiceImpl::GetAllCharacters` (0x15978A0) | 0 | 没去要角色列表 |
| TSL | `SaveManager::Load` (0xFFF2A0) | 1 | 流程到了这一步 |
| TSL2 | `SaveDataLocalStorage::Load` (0x1594340)（本地读档） | **0** | **本地文件这条路根本没走** ✓ |
| TGP | `JsonManager::GetSavePath` (0x112F990)（拼路径） | **0** | 同上 ✓ |

⇒ ⇒ **VII / X 两轮往设备上摆 32 个存档候选文件之所以无效，不是路径或格式不对，而是客户端压根不读本地文件** ✓✓
⇒ ⇒ 之前「本地存档缺失 ⇒ 卡住」的模型**错了**：真正卡住的是**后端取档这一步**。

### 4K.2 ★ `LoadSavedData` 被调用了，却**一个 HTTP 请求都没发**

时间线（XIII 轮 22:14:49）：
```
22:14:49.153  TSL  SaveManager::Load
22:14:49.153  TSD  SavedDataServiceImpl::LoadSavedData   ← 紧接着
（此后后端再无请求；游戏在 22:16:39 被杀）
```
⇒ 它在**构造请求之前**就退出了 ✓ ⇒ 最可能的原因是**没有角色 id**（客户端刚建完匿名账号 `#11`，
角色还没就绪 ✓ ⇒ 与 `GetAllCharacters` 从未被调用一致 ✓）。

`LoadSavedData` 的结构（反汇编 0x15964B4 起）：
```
0x1596584  遍历 [iface+0x58] 的实现表（16 字节步长），按 key 找槽
0x15965d8  取槽 {funcPtr, arg} → 0x15965f8  blx funcPtr        ← 真正干活的那一步
0x1596624  再取一次槽 → 0x1596688  ldr r2,[r0] / ldr r1,[r0,#4] / bx r2   ← 尾调用
```
⇒ `SaveManager::Load` 里是**同一套槽机制** ✓（“本地 loader / 后端 loader 按 key 选” ✓）⇒
   槽里的函数指针是**运行时表** ✗ ⇒ 静态定位不到 ⇒ 下一轮要用探针打候选函数 ✓。

### 4K.3 ★★★ 事故：清理设备数据目录时误删了 bundle（已完整恢复）

用 `find <files dir> -size +5M -delete` 清理自己摆的试验文件 ⇒ **误删了 1525 个 bundle 与 `global-metadata.dat`**
（游戏的大文件都在那儿 ✓）⇒ 客户端直接报
`Unable to open archive file: .../files/Bundles/wwise_vo…` ⇒ 流程中断 ✗。

⇒ 恢复：从 APK 把 2963 个 bundle 打成 tar 推回设备解包 ✓（1.0G ✓）✓，`global-metadata.dat` 从 APK 补回 ✓，
流程恢复正常 ✓（TIE=9、认证成功 ✓）。

⇒ ★★ **教训**：清理设备上游戏的数据目录**不要按大小删**（bundle 与 IL2CPP metadata 都在那儿 ✓）；
   要删就只删**自己刚写的那些路径** ✓。

⇒ ★ 副产品：`file_server.py` 现在能**直接从 APK 提供任意 bundle** ✓
（先用 `bundle_index.json` 做 hash→名字映射 ✓，客户端按 md5 请求也能命中 ✓）⇒ 以后 bundle 缺失可随时补 ✓。

### 4K.4 本轮探针集（10 个）

```
TSF  FlowState::OnSaveDataFetched              0x801CE8  ('r3')
TFI  FlowTrigger::Fire                         0xA7381C  ('off')  ★ 触发器身份反查
TSD  SavedDataServiceImpl::LoadSavedData        0x15964B4  ('reg')
TSG  SavedDataServiceImpl::GetAllCharacters    0x15978A0  ('reg')
TGP  JsonManager::GetSavePath                  0x112F990  ('str3') ★ r0=目录名
TSL2 SaveDataLocalStorage::Load                0x1594340  ('str3') ★ r2=文件名
TCO  FlowState::.ctor                          0x807150   ('reg')
TIE  FlowState::InvokeEntered                 0x7FD0E4   ('reg')
TMO  MenuRequestManager::OpenMenu              0x9CCB94   ('reg')
TSL  SaveManager::Load                        0xFFF2A0   ('reg')
```

---

## 4K. 第九阶段（2026-10-30 21:10–21:50）：★★★ 请求“入队但不发”的定位 + 认证链打通

### 4K.1 ★★★ 认证链打通（客户端首次真的向后端认证）

洞 B 台阶 `0xa4 → 0x04 → 0x0c → 0x10`（台阶字节直接存触发器静态偏移，取触发器用 `ldr r4,[holder,X]`）⇒ 客户端接着自己发
`0x1c`(CreateAnonymousAccount) → `0x30`(PlayGame) → `0xac`(FetchingSaveData)，并且：

```
POST /api/authentication/v1/public/auth/gplus
{"details":{"user_id":1,"ticket":"1","channel":"gplus_cn"},"platform":"gp","language":"cn","deviceId":""}
```
⇒ 我们的 `{"session":{...},"requestIndex":0}` **被接受** ✓（紧接着客户端就发了 `0x30`）✓

状态链：`#59 LoadStartupScene → #28 InitializingBundles → #66 LoadingBaseGameContent → #64 Authenticating →
#2 PostFtueStart → #4 PostFtueSetup → #11 CreateAnonymousAccount → #13 PlayGame → #65 FetchingSaveData` ✓

### 4K.2 ★★★ 模型修正：取存档走**后端**，本地文件那条路从未被调用

| 探针 | 目标 | 命中 | 含义 |
|---|---|---|---|
| TSD | `SavedDataServiceImpl::LoadSavedData` 0x15964B4 | **1** | 确实调了后端取存档 ✓ |
| TSL | `SaveManager::Load` 0xFFF2A0 | 1 | 流程到了这一步 |
| TSL2 | `SaveDataLocalStorage::Load` 0x1594340（本地读档，'str3' 探针）| **0** | **本地文件根本没读** ✓ |
| TGP | `JsonManager::GetSavePath` 0x112F990（拼路径，'str3'）| **0** | 同上 ✓ |
| TAR | `SerialBackendCommunicationService::AddRequest` 0x159AB70 | **3** | 请求**入队了** |
| TRF | `Request::GetRequestForm` 0x139CD50 | **0** | **HTTP 请求没被构造** |
| TRP | `Request::PerformRequest` 0x139E830 | 2 | 只有两个请求真的跑过协程 |
| TRL | `RestManager::LateUpdate` 0x119378C | 4863 | REST 层每帧正常 ✓ |

⇒ ⇒ 之前两轮“往设备上摆存档文件”的方向整个是错的 ✓（本地文件从不被读）。

### 4K.3 ★ 请求为什么没发：两处候选（第二处仍待验证）

```
① SerialBackendCommunicationService::LateUpdate 0x159B6F0
   0x159b6f0  ldr r1,[r0,#0x2c] ; cmp r1,#0 ; bxne lr      ← “有请求在飞”则不消费
   ⇒ 修法 D：0x159B6F8 改成 nop ⇒ 实测行为不变 ⇒ 不是原因 ✓

② 真正的消费逻辑 0x1589808
   0x15898b4  ldr r7,[r7,#0x70]      ; Request.RequestPriority
   0x15898b8  bl  0x11a49b8          ; get_PriorityFilter()
   0x15898bc  cmp r7,r0
   0x15898c0  bge 0x15898f0          ; ★ priority >= filter ⇒ 跳过该请求
```
⇒ **最可能真因**：取存档请求的 Priority ≥ 当前过滤器值 ⇒ 永远被跳过 ⇒ 队列逐渐堵死。
⇒ **下一轮**：去掉 0x15898C0 的 `bge`（或把 `get_PriorityFilter`(0x11A49B8) 的返回值改大）⇒ 看请求是否发出。

### 4K.4 ★ 本轮工程改动

- **修法 D**（`patch_c_fire.py`）：队列守卫 `bxne lr` → `nop`（0x159B6F8）★ 已证伪，保留无害。
- `server.py`：加 `ctx.sni_callback` 打印 TLS SNI ✓（握手失败也能看见域名）。
- `server.crt`：同一张 CA 重签，SAN 换通配符 ✓（修掉一次 `Curl error 35`）。
- `file_server.py`：可直接从 APK 流式提供任意 bundle（`bundle_index.json` 做 hash→名字映射）✓。
- 新工具 `find_literals.py`：IL2CPP 字面量解码器（15398 条）。
- **事故**：`find -size +5M -delete` 误删设备上 1525 个 bundle 与 `global-metadata.dat` ⇒ 已从 APK 完整还原（2963 个 bundle）✓。
- **教训**：字符串探针（`'str3'`）读的参数若不是字符串 ⇒ **直接 SIGSEGV**（实测 fault addr 落在堆上）⇒ 探字符串前必须先确认参数类型。

---

## 5. 顺带确认的事实（都是硬证据）

### 5.1 Java 侧登录链全貌

```
MainActivity.Login()                       ← Unity 用 UnitySendMessage("MainActivity","Login","") 调用
  → GHome.login(activity, callback)        (method@3d6d；GHome 没声明它，运行时解析到继承来的实现)
      → GHomeApiBase.login(Activity,Callback)   0x13EF18
          → activity.runOnUiThread(new GHomeApiBase$2(this, activity, callback))
              → GHomeApiBase$2.run() → this$0.loginUI(activity, callback)
                  → GHomeChannelBase.loginUI    0x141530
                      ├─ if (!isInitial) doCallback(..., -10869611, "没有初始化", EMPTY_MAP)
                      └─ else → channelLoginUI(activity, callback, new GHomeChannelBase$9(...))
                          → GHome.channelLoginUI   0x13CFEC
                              → UCGameSdk.defaultSdk().login(activity, sdkParams)   ← 联网，已死
```

`MainActivity$2.callback(int code, String msg, Map data)` 读 map 里的
`"userid"` / `"ticket"` / `"isGuest"` 三个键，再交给
`MainActivity.sendUnityMessage(method, code, msg, data)` →
`{"code":<int>,"message":"<msg>","data":{...}}` → `UnityPlayer.UnitySendMessage`。

### 5.2 `patch_v49.py` 的副作用（重要，之前没注意）

`patch_v49.py` 把 `MainActivity.onResume` 的 **unit 3..9** 覆盖成
`const-string "0" + invoke ReportData`，**顺带把原来的
`GHome.getInstance().onResume(activity)` 那条调用（unit 3..9）一起覆盖掉了**。
原版 `onResume` 共 11 unit：

```
0000 invoke-super {v1} UnityPlayerActivity.onResume
0003 invoke-static {} GHome.getInstance
0006 move-result-object v0
0007 invoke-virtual {v0,v1} GHome.onResume(activity)   ← 被 patch_v49 覆盖掉
000a return-void
```

⇒ 用 `patch_v49` 做实验时，SDK 的 `onResume`（悬浮窗/登录态维护）**不再被调用**。
后续做实验时应改成「追加注入」而不是「覆盖 3..9」。

### 5.3 dex 里 static 字段编码的特殊之处

本游戏的 dex 里 **static field 的 encoded_field 只有 2 个 uleb**
（`field_idx_diff` + `access_flags`），**没有 `static_value_off`**；
instance field 也是 2 个 uleb。标准 DEX 的 static field 是 3 个 uleb。
`dexlib.py` 的 `classes()` 按 2 个 uleb 解析是**对的**（照标准写 3 个会把整个 class_data 解析错位）。

### 5.4 FlowTrigger 静态偏移全表（补齐）

`0x00 AuthenticationPreFtueStart / 0x04 AuthenticationPostFtueStart / 0x08 PreFtueSetup /
0x0C PostFtueSetup / 0x10 PlatformSignIn / 0x14 ExternalLogInWithPlatformId /
0x18 AuthenticationAnonymousLogin / 0x1C CreateAnonymousAccount / 0x20 LogInWithBNetToken /
0x24 LogInWithBNetUserData / 0x28 AuthenticationLink / 0x2C SendAnalytics /
**0x30 AuthenticationPlayGame** / 0x34 PlayTestStart / 0x38 PlayTestStartNoUi /
0x3C PlayTestSetup / 0x40 PlayTestClearSaveGame / 0x44 PlayTestLogInWithBNetUserData /
0x48 PlayTestLogInWithBNetToken / 0x4C PlayTestPlayGame / 0x50 PlayTestConnectionMenu /
0x54 OpenMenu_Blocking / 0x58 ...WithPause / 0x5C ...WithPauseAndBlur / 0x60 ExitBlockingMenu /
0x64 StartEncounter / 0x68 PreCombat / 0x6C StartCombat / 0x70 EndCombat / 0x74 EndPostCombat /
0x78 EndEncounter / 0x7C Death / 0x80 StartInteractionWithDestructible / 0x84 StartAutoInteraction… /
0x88 EndInteraction… / **0x8C Bootstrap** / 0x90 Back / **0x94 Proceed** / 0x98 ResetGame /
0x9C LoadingComplete / 0xA0 LoadingFailed / 0xA4 Authenticate / 0xA8 AuthenticateNoUi /
0xAC FetchSaveData / 0xB0 FetchSaveDataFromNoUiAuthentication /
0xB4 WaitForBackendMaintenaceModeToFinish / 0xB8 AnonymousWindowDisplay / 0xBC AnonymousWindowLinkBNet /
**0xC0 StartGame** / 0xC4 LoadTown / 0xC8 LoadVisitingTown / 0xCC SyncVisitingTown /
0xD0 LoadQuest / 0xD4 LoadAbyss / 0xD8 LoadArena / 0xDC LoadTest / 0xE0 LoadStandaloneDungeon /
0xE4 LoadTrainingDungeon / 0xE8 LoadMainMenu / 0xEC CharacterSelection / 0xF0 BlockingCutsceneStarted /
0xF4 BlockingCutsceneFinished / 0xF8 GameOverLoadMainMenu / 0xFC GameOverRestartDungeon /
0x100 GameOverReviveAndContinue / 0x104 UpdateBundles / 0x108 SetBuildVersionGLD /
0x10C BundlesInitialized / 0x110 LoadBaseGameContent / **0x114 StopGameHardwareNotSupported** /
0x118 PvpExploration / 0x11C RegressionTestStart / 0x120 RegressionTestAuthenticate /
0x124 SoftLaunchStartNoUi / **0x128 SoftLaunchStart** / 0x12C SoftLaunchSetup /
0x130 SoftLaunchPlatformSignIn / 0x134 SoftLaunchExternalLogInWithPlatformId /
0x138 SoftLaunchLogInWithBNetUserData / 0x13C SoftLaunchLogInWithBNetToken /
0x140 SoftLaunchLinkPlatformIdWithBnet / 0x144 SoftLaunchLinkPlatformConflictResolution /
0x148 SoftLaunchSendAnalytics / 0x14C SoftLaunchPlayGame / 0x150 SoftLaunchReadAppReceipt /
0x154 SoftLaunchDisplayFinishMessage / 0x158 SoftLaunchConnectionMenu /
0x160 AppStart / 0x164 AppSuspend / 0x168 AppResume`

注意 **0x114 = `StopGameHardwareNotSupported`**：`FlowState::SetupGameManagers`(0x803414) 内部
0x803AAC 处有一条「设备不支持 → Fire(holder[0x114])」的分支（同一函数里还有
`MemPerf::ReportUnsupportedDevice`）。**这条分支本轮没有走**（Fire 之后没有打点），
别把它误读成「启动触发器」。

### 5.5 流程状态机的骨架（补齐旧交接）

| 环节 | RVA |
|---|---|
| `FlowManager::Init`（→ 调 `FlowState.Setup` 建 27 条迁移） | 0x7FBDD8（`FlowState.Setup` 0x7FC3BC） |
| `FlowManager::StartupInit` | 0x7FC784 |
| `FlowManager::Fire` / `DoFire` / `DoTransition` | 0x7FCE68 / 0x7FCF90 / 0x7FD014 |
| `FlowManager::OnUnhandledTrigger`（**只有 4 字节 `bx lr`，是空实现 ⇒ 触发器无匹配迁移时静默忽略**） | 0x7FD010 |
| `FlowState::InvokeEntered` | 0x7FD0E4 |
| `FlowState::StartupLoadComplete` / `…AndBackendAvailable` | 0x8024F8 / 0x8026DC |
| `FlowState::SetupGlobalManagers` / `SetupGameManagers` | 0x802E08 / 0x803414 |
| `FlowState::OpenLoadingMenu` / `CloseLoadingMenu` / `WaitForLoadingMenuInAnimation` | 0x8007C4 / 0x800BE0 / 0x800D54 |
| `FlowState::PrepareLoadingScreenTaskList` | 0x8003D4 |
| `FlowState::StartLoadingContent` / `OpenMenu` | 0x8023B8 / 0x8021B4 |
| `FlowState::OnSaveDataFetched` / `StartAuthentication` | 0x801CE8 / 0x801988 |
| `AuthenticationServiceImplementation::Authenticate` | 0x11A1C64 |
| `MenuRequestManager::OpenMenu` | 0x9CCB94 |
| `AbstractMenu::OnOpen` / `AbstractLoadingMenu::OnOpen` | 0x123B4BC / 0xB1852C |
| `AbstractLoadingMenu::PlayInAnim / PlayOutAnim / OnInAnimDoneEvent / OnOutAnimDoneEvent` | 0xB18790 / 0xB18858 / 0xB188D4 / 0xB18914 |

---

## 6. 下一步（按性价比）

1. **【最高】定位 context 在哪一段被丢成 null**。
   候选探针（下一轮打点器换掉几个低价值节点即可）：
   `MenuRequestManager::OpenMenu`(0x9CCB94, 打印 r2)、
   `MenuRequest::.ctor`(0x9E192C, 打印 r1=context)、
   `0x9E22F8`（MenuRequestManager 把 request 交给菜单管理器的那一步）、
   `LoadingMenu::OnOpen`(0x910D44, 注意它**没有 STMDB 序言**，是 `bl` 直调，要用「无序言版」cave)、
   `AbstractMenuWithDefaultAPI::OnOpen`(0xB0F874, 打印 r1)。
   一旦定位到丢弃点，就有两条修法：
   * **修上游**：让 context 不被丢（例如那个把它置 null 的分支其实应该走「保留 context」）；
   * **修下游**：直接在 `AbstractLoadingMenu::OnOpen` 里，若 context==null 就**直接调 `PlayInAnim()` 并让
     `WaitForLoadingMenuInAnimation` 立刻完成**——但 `onInAnimDone` 回调对象也一起丢了，
     所以更稳的修法是**在 `FlowState::OpenLoadingMenu` 之外重建一个带回调的 context**。
2. **确认 `GHomeSendManager::Login`(0xA7B5DC) 是否真的从未被调用**（打点即可）。
   若确认从未被调用 ⇒ **「短接 SDK 登录」这条路线彻底作废**，不要再在 Java 侧花时间。
3. **改 `patch_v49.py`**：不要覆盖 `onResume` 的 unit 3..9（会连带干掉 `GHome.onResume`），
   改成在 `MainActivity.Init`(0x17defc) 或 `KickOff`(0x17df28) 里追加注入。
4. 拿到 `/auth/gplus` 之后，按 `rest_spec.md` 补齐 `characters` / `towns/current` 闭环。

---

## 7. 复跑

```bash
cd H:/Project/threeKingdomSlayer/threeKingdomSlayer/Library/Locus/tmp/blades
python build_vtrace.py        # 生成带 17 个探针的 libil2cpp.vtrace.so
bash build_vtrace.sh          # 打包 + zipalign + 签名（3-5 分钟）
adb -s emulator-5554 install -r build/blades_vtrace.apk
bash run_vtrace2.sh           # 起后端 + reverse + 拉客户端 + 抓打点（WAIT=110）
grep -aE " I (TAO|TCT|TST|TPI|TIN|TOU|TMI|TMS|TMF|TMD|TMT|TIE|TSL|TSA|TS2|TFI|TAU) +: " log_vtrace2.txt
```

⚠ `adb logcat -d` 会带出旧 buffer；用 `-t 4000` 限量。
⚠ 起后端 + `adb reverse` + 拉客户端 + 抓日志**必须在同一次 bash 调用内**完成。
