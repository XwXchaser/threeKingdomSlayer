---
id: kd_47d3555e-c663-47e8-bcf9-09232714a4e5
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
summary: Blades 离线化 2026-10-29 第二阶段的实测结论：登录链路其实在 Java 层（GHome SDK → MainActivity$2.callback → UnitySendMessage("GHomePostManager","LoginResultCallBack",json)）；已用纯本地工具链（无 Frida、无联网下载）做出可复现的 dex 注入管线并把伪造的 code=0 登录结果送到 C#；剩余唯一缺口是"谁来消费这个登录结果"。含 dex 补丁的全部坑（outs_size、35c 寄存器顺序、map_list、string_ids 排序）与复跑脚本。
---

# Blades 离线化 · Java 桥注入突破（2026-10-29 第二阶段）

> 前置背景看 `plan/blades-handoff-20261029.md`。本篇只记录**这一阶段实测到的新事实**，其中两条与旧交接的根因判断不一致。

## 1. 推翻/修正的旧判断

| 旧判断 | 实测 |
|---|---|
| 渠道 SDK 初始化失败导致流程干等 | **初始化是成功的**：`GHomeApiBase.doCallback() code=0 msg=初始化成功`，C# 侧 `Ghome init is 0`。`NoClassDefFoundError: Lcn/uc/gamesdk/R$layout` 只是被 `W/System.err` 吞掉的悬浮窗异常 |
| 客户端"发过登录但被拒" | 原始包**从未发起过登录**：`logcat_full.txt`/`v5_logcat.txt` 等历史日志里 `GHomeApiBase.login` 出现 0 次 |
| 登录要点 FlowTrigger 点火 | 登录入口在 Java 层，与状态机无关（见 §2） |

补充实测：初始化**之后**调用 `GHome.login()` 会**静默挂住**（无任何回调、无异常日志）；初始化**之前**调用则立刻回调 `code=-10869611 msg=没有初始化`。

## 2. 完整的 Java → Unity 登录契约（dex 实测）

```
MainActivity.Login()
  → GHome.getInstance().login(activity, new MainActivity$2(this))
  → MainActivity$2.callback(int code, String msg, Map data)
      → MainActivity.sendUnityMessage("LoginResultCallBack", code, msg, data)   [invoke-static, 4 参]
          → 拼 {"code":<int>,"message":"<msg>","data":{...}}   [org.json.JSONObject]
          → Log.i("MainActivity","Unity3d %s(%s);", method, json)
          → UnityPlayer.UnitySendMessage("GHomePostManager", method, json)
C# 侧：GHomePostManager.LoginResultCallBack(string) → InvokeCallback(JSON) → 写 TicketStr/UserStr → LoginCallBack(code)
        code != 0 时打 `登录G+失败`(LogError)；code == 0 时**不打任何日志**
```

JS 侧（同文件）另有 8 个入口可复用：`InitResultCallback`（纯数字字符串，如 `"0"`）、`SwitchAccountResultCallBack`、`N2U_GetTicketCallback`、`LogoutResultCallback`、`PayResultCallback`、`GetProductConfigCallBack`、`EscapeGameCallBack`。

## 3. 已建成的工具链（全部本地，无需下载）

| 文件（`Library/Locus/tmp/blades/`） | 作用 |
|---|---|
| `dexlib.py` | 极小 DEX 读写库：表解析、`class_data` 遍历、`code_item` 读写、35c/21c 指令编码、`fix_map`、字符串追加 |
| `dex_dump.py` | dex 方法反汇编（`refs <字符串>` / `meth <名字>`） |
| `patch_v48.py` | 初始化回调内注入（早期版本） |
| `patch_v49.py` | **当前版本**：`onResume → ReportData("0")`，`ReportData` 本体改为发一条成功登录 |
| `build_v46.sh` | v6 bundle 补丁 + 替换 `classes.dex` + zipalign + 签名（需 `JAVA_HOME=jdk1.8.0_181`） |
| `run_v49.sh` / `run_live_check.sh` | 两段式跑法：冷启动 → HOME → 回前台（触发注入）并抓日志/截图/请求 |

**环境事实**：宿主无法下载（~19KB/s，pip 装 frida 超时）→ Frida 路线不可行；本机有 `jdk1.8.0_181` + `build-tools/30.0.3`（d8/dexdump/zipalign/apksigner）→ 纯本地 dex 注入可行。模拟器 `x86 + libnb(houdini)`、`ro.debuggable=1`、API 25。

## 4. dex 补丁的硬坑（每条都踩过）

1. **`outs_size` 必须 ≥ 被调方法的参数个数**，否则 `VerifyError: invalid argument count (4) exceeds outsSize (3)`；改 `code_item+4` 的 u16 即可。
2. **35c 指令的寄存器顺序**：unit2 的 4 个 nibble 是 C/D/E/F = 参数 0..3；第 5 个寄存器在 unit0 的 bits 8-11（G）。写反了会报 `register v1 has type String but expected Integer`。
3. **不要移动 `string_ids` 表**：追加字符串会破坏排序，`dexdump` 报 `Out-of-order string_ids`，且 map_list 必须同步（`Bad offset for section` / `Out of order map item`）。
4. **替代方案（推荐）**：不新增字符串——用现成类型现造数据（`new HashMap` + `put("ticket","1")` + `put("userid","1")`），或复用零引用的长字符串槽位（本 dex 只有 1 条，是 RSA 公钥，别动）。
5. **注入点要挑有富余 code unit 的方法**：`MainActivity$1.callback` 前 22 unit 是死日志，`ReportData` 有 49 unit，`GameEnter/GamePause` 23 unit，`Login/Init/GetTicket` 只有 13 unit。
6. 每次改完必须 `dexdump -f/-d` 校验 + 重算 header 的 adler32(8) 与 SHA-1(12..31)。
7. `install -r` 保留 1.1GB bundle 与 SDK 模块；`pm clear` **不要**用（会删掉已下载的 ucgamesdk 模块而无法重新下载）。

## 5. 现在的状态（v51）

**已达成**：
- `MainActivity.onResume()` 每次前台化都会触发注入 → Java 侧确实打出
  `Unity3d LoginResultCallBack({"code":0,...,"data":{"userid":"1","ticket":"1"}})`
- **投递链路已证明可用**：用 `patch_v49_code1.py`（把 code 改成非 0）跑一次，游戏加载完成后 C# 立刻打 `登录G+失败`
  ⇒ Java→Unity 消息可靠送达；`code=0` 时 C# 成功路径**不打任何日志**，所以"没日志"不等于"没收到"。
- **上一轮"点火无效"的真凶**：旧脚本的 `ldr_imm()` 把立即数除以 4，而 ARM `ldr` 的 imm12 是**字节**偏移
  （原函数里 `0xE590105C` 就是 `[r0,#0x5c]`）。v34 一直在读偏移 `0x17` 的错误字段，`Fire` 收到的是垃圾指针。
  `build_v51.sh` 已修正，并按原机器码逐位复刻函数序言（`push` 寄存器位不同会直接栈失衡）。

**当前 v51 做法**：在 `GHomePostManager::LoginResultCallBack` 入口挂 cave → 读 `[触发器表+0x30]` → **判空 + 只点一次** → `FlowTrigger::Fire`。
结果：注入正常、无崩溃，但仍无 `/auth/gplus`。
（未加判空时游戏会"活着但不响应"，说明该回调在启动早期就被调用、触发器指针当时还没就绪。）

两个待判定：
1. `[表+0x30]` 是否为 null（现在被 guard 静默跳过）——把 guard 改成故意解引用 null，看 `fault addr 0x0` 即可判定（一次构建）。
2. 当前 FlowState 是否接受 `0x30`——没有运行时内省手段时无法确认。

## 5.1 本轮新增的两条硬结论（2026-10-29 15:3x）

1. **`[触发器表 + 0x30]` 在 C# 登录回调时刻是 NULL**。
   做法：cave 里把判空改成故意 `ldr r2,[r0]`，跑一次得到
   `signal 11 (SIGSEGV) SEGV_MAPERR, fault addr 00000000`。
   ⇒ 旧方案（v31~v45 全部）用的"`[singleton+0x5c]` 触发器表 + 偏移"这条路径，
   **在运行时并没有那个触发器**；之前"点火不崩也没反应"的真正原因是读到了空/垃圾值。

2. **把钩子挪到 `FlowManager::Update` 入口（每帧检查、判空、只点一次）后：不崩，但仍无 `/auth/gplus`**。
   同一构建里 Java 侧伪造的 `code=0 + data{userid,ticket}` 已确认送达（`Unity3d LoginResultCallBack` 日志）。
   ⇒ 要么该槽位自始至终为 null（触发器表根本不是这个访问方式），要么流程在当前状态拒绝 `AuthenticationPlayGame`。

3. 附带修正：**ARM `ldr` 的 imm12 是字节偏移**（原函数 `0xE590105C` 即 `[r0,#0x5c]`）。
   `build_v51.sh` 早期的 `ldr_imm()` 除以 4 是错的，这直接导致 v34 系列读错字段。

## 5.2 目前唯一缺的观测手段

现在所有判断都受限于"看不到 C# 侧 FlowState 到底在哪个状态"。宿主网络约 19KB/s，
`pip install frida` / 下载 `frida-server` 都会超时。可行的补法：
- 用 `plan/two-device-handoff-20261009.md` 里提到的另一台主机把
  `frida`(abi3 wheel) + `frida-server-<ver>-android-x86` 拷过来（模拟器是 x86/API25/已 root）；
- 或用真机（arm64）跑，Frida 在 arm64 上兼容性更好。
拿到之后能直接：读 `FlowState` 当前状态、读 `[singleton+0x5c]` 表的真实内容、看 `LoginCallBack` 是否为 null。

## 5.3 若继续纯静态/补丁路线

- 先确认 `[singleton+0x5c]` 到底是哪张表：`FlowState::StartAuthentication`(0x801988) 里读它并取 `+0x0/+0x4/+0xA8/+0x128`，
  说明那是**另一个类**的静态块副本，不一定是 FlowTrigger 表。应当从 `FlowState` 自己的静态字段里取状态，再按迁移表反查。
- 或者换思路：不点触发器，直接把 C# 的 `AuthenticationServiceImplementation.Authenticate` 调起来
  （难点仍是原生侧构造托管字符串——历史上 5 次崩在这）。

## 5.4 2026-10-29 傍晚：原生观测通道打通后的新事实

**观测手段（已可用，复用 `build_vlog2.sh` / `build_vstate.sh` / `build_vfire.sh`）**
在任意 C# 函数入口挂 cave，用 `libil2cpp.so` 已导入的 `__android_log_print`（PLT=0x50F260）
把寄存器/内存值打到 logcat（tag 自定义），格式 `%08x %08x %08x %08x`。
关键坑：cave 里的**字符串指针必须用 pc 反推运行时地址**（写 RVA 会崩在未映射地址）；
`ldr` 的 imm12 是字节偏移；35c 的 Rn/Rt 位别写反；每次改完用 capstone 逐条自校验 + 数值核对跳转目标。

**实测结论**
1. **报文形状决定成败**（A/B 实测）：
   - 嵌套 `{"code":0,...,"data":{"userid":"1","ticket":"1"}}` → C# 确实把
     `TicketStr`(非空 String*) 和 `UserStr`(=1) 写进 `GHomePostManager` 单例；
   - 顶层平铺 `{"code":0,"ticket":"1","userid":"1"}` → 两个字段都不写（值仍为 0）。
     ⇒ `InvokeCallback` 只认 `data{}` 内的键。
   - C# 收到的字符串已用探针核对：长度 `0x24`=36、头 4 字符 `{"co`（`0022007b 006f0063`）。
2. **`[singleton+0x5c]` 不能当触发器表**：同一构建下，冷启动 6s、78s、180s 三次采样该字段都是 0；
   早前 vlog2 那次读到非零（0x9cf7de60）无法复现。⇒ 历史所有"读表取触发器"的补丁建立在一个
   不可靠的字段上，点火路线应当放弃。
3. `LoginCallBack` 委托字段（`+0x0c`）在早期采样时非零，说明**订阅者是存在的**；
   登录成功后没有 `/auth` 的原因应当在"订阅者内部逻辑/它依赖的状态"，不在"没人接"。
4. `set_TicketStr` 打点从不命中——IL2CPP 会把这种私有访问器内联，**不能**用它判断字段是否被写。

**下一步（最短路径）**
委托对象在 `[singleton+0x0c]`，Il2CppDelegate 的 `method_ptr` 通常在对象 +8 处：
把 `[[singleton+0x0c]+8]` 打出来，就是"登录成功时真正会执行的那个 C# 方法"的 RVA，
再用 `dump.cs` 反查函数名，即可知道游戏打算做什么、缺什么前置条件。

## 5.5 收尾结论：真正的堵点在 SDK 侧，不在状态机

**触发器静态的正确访问形态（照抄 `FlowTrigger::.cctor` @0xA76DB4 / `FlowState::StartAuthentication` @0x801988）**
```
ldr r0,[pc,#V]      ; V = FlowTrigger 静态块的 usage 偏移
ldr r5,[pc,r0]      ; r5 = &静态块
ldr r0,[r5]         ; r0 = 静态块
ldr r1,[r0,#0x5c]   ; r1 = 触发器持有者（与 FlowTrigger 静态同布局）
ldr r2,[r1,#0x30]   ; r2 = AuthenticationPlayGame（0x30 才是它；0x18=AnonymousLogin 无效）
```
旧脚本用的链是对的，但**采样时刻太早**：在登录回调、Debug::Log 等所有能采样的点上，
`FlowTrigger` 静态块/持有者都还没初始化（为 0，加保护就跳过；不加保护直接 fault 0x5c）。
⇒ **说明游戏的流程状态机压根没被创建过**——因为它在等渠道 SDK 的登录结果才初始化。

**因此真正的堵点回到 SDK 侧**：`cn.uc.gamesdk.R$layout` 缺失 → 悬浮窗（“开始游戏”按钮）创建不出来 →
SDK 登录回调永不触发 → 游戏的 FlowTrigger/FlowState 静态不初始化 → 客户端停在标题。
我们能伪造的是 *Unity 侧* 的登录结果，但游戏在 **SDK 侧** 等的是一个不会来的回调。

**可行的收尾路线（都在本地、无需下载）**
1. **补齐缺失的 SDK 资源类**：用本地 `build-tools/30.0.3/aapt2 dump resources` 读
   `sdk_loader.apk`（本地已有，8MB）的 resources.arsc，取出 `cn.uc.gamesdk` 包下各 layout 的 id，
   再合成一个 `cn.uc.gamesdk.R$layout` 类（静态 int 字段）注入 APK 重新签名 → 悬浮窗可创建 →
   SDK 登录 UI 出现 → 游戏自己的流程自然启动。
2. 或者：让 SDK **跳过**悬浮窗创建（改 Java 侧 `cn.uc.gamesdk.e.a$a.<init>`），
   再配合我们已经验证可送达的伪造登录结果，让 SDK 回调游戏。
3. 若 SDK 登录 UI 需要真实账号，则退到"用伪造的 SDK 登录回调喂游戏"：
   在 Java 侧补一个 `MainActivity$2.callback` 的直接调用（dex 补丁，无需新增字符串：
   复用现有 `MainActivity$2` 与 `UnitySendMessage`）。

## 5.6 SDK 资源包的实况（决定性）

设备上的 UC SDK 模块（`/data/user/0/<pkg>/ucgamesdk/modules/`）：
- `update-1/cn.uc.gamesdk.loader.apk`（8.2MB）、`update-2/cn.uc.gamesdk.loader.apk`（7.0MB）、
  `odex/cn.uc.gamesdk.loader.dex`（20MB）。
用本地 `aapt2 dump resources` 逐个看：两个 loader 包的 package 都是 `cn.uc.gamesdk.loader`，
layout 只有 4~6 个（`install_permission_prompt_view` / `sdk_state_def_view` / `vp_palyer_controller` /
`vp_player_non_wifi_view` / `activity_permission_desc` / `dialog_layout_confirm`），
**没有任何 `cn.uc.gamesdk` 包自己的 layout**。

⇒ 结论升级：不是"少了一个 R$layout 类"，而是**UC SDK 自己的资源 apk 从来没被下载下来**
（服务端已死）。悬浮窗的 layout 与 R$layout 一起缺失。
所以"补一个 R$layout 类"这条不够——layout 本体也没有，得自己写一套并猜对控件 id，成本高、成功率低。

**更可行的两条**
- **A：让 SDK 跳过悬浮窗**：补丁目标是 SDK dex 里的 `cn.uc.gamesdk.e.a$a.<init>`（就是抛
  `NoClassDefFoundError: Lcn/uc/gamesdk/R$layout;` 的那一处），让它变成空实现；
  SDK 随后继续走登录流程（`GHomeOffline`/`CheckLoginStatus` 都在 dex 里）。
  补丁位置在设备上的 `odex/cn.uc.gamesdk.loader.dex`（可直接 pull → 改 → push）。
- **B：自己初始化游戏的流程**：既然 `FlowTrigger`/`FlowState` 的静态没被创建（cctor 没跑），
  可以由我们的 cave 先 `bl FlowTrigger::.cctor`(0xA76DB4)，再读
  `[静态块+0x5c] → [持有者+0x30]` 得到 AuthenticationPlayGame 并 Fire；
  必要时再调 `FlowState` 的 cctor。这条完全在本地、无需资源。

## 5.7 路线 A 实测结果（不可行，已排除）

尝试：在 cave 里先 `bl FlowTrigger::.cctor`(0xA76DB4) 自行初始化触发器静态，再读持有者取
`AuthenticationPlayGame`(0x30) 并 `FlowTrigger::Fire`。

实测：
- cave 本身**确实执行**（入口标记日志 FE 打出 ✓）；
- 但**调用 `FlowTrigger::.cctor` 之后我们的执行流就断了**（后续日志/点火都不执行，应用不崩溃、
  托管异常从 native 栈穿过去）。把 cctor 与采样拆成两个 cave、先 cctor 后采样，采样侧仍然不执行
  ⇒ 这次调用破坏了后续托管执行，**直接调 cctor 这条路作废**。

顺带确定了两条可复用的规律：
1. **可靠触发点只有一个**：`GHomePostManager::InitResultCallback`（引擎就绪那一刻）。
   Java→Unity 的消息只有在**引擎就绪前**发出才会被排队并于就绪时投递；游戏完全起来之后再发的
   投递不到（推测 GameObject 在场景切换后失效）。所以"冷启动时发 + HOME 回前台"里只有前者有效。
2. cave 里 `add r1,r9,#TAG` / `add r2,r9,#FMT` 的偏移必须与数据区实际布局严格对齐，
   错位会把 prio 当 tag、tag 当格式串，日志就"消失"（踩过一次）。

**因此下一步只剩路线 B**：改 SDK 自己的 dex，把 `cn.uc.gamesdk.e.a$a.<init>`（创建悬浮窗并抛
`NoClassDefFoundError: Lcn/uc/gamesdk/R$layout;` 的那处）改成空实现，让 SDK 不再卡在悬浮窗，
SDK 的登录流程得以继续 → SDK 回调游戏 → 游戏的 FlowTrigger/FlowState 静态才会被创建 → 流程启动。
目标文件在设备上：`/data/user/0/<pkg>/ucgamesdk/modules/update-1/cn.uc.gamesdk.loader.apk`
（= 本地 `sdk_loader.apk`，8.2MB），`update-2/` 是另一个版本（7.0MB），另有一份 20MB 的
`odex/cn.uc.gamesdk.loader.dex`（运行时会用到）。改完需删 odex 让运行时重建，并注意模块完整性校验。

## 5.8 路线 B 实测结果（SDK 侧已打通一半）

**精确定位了崩溃点**（游戏自己的 dex 里，全库仅此一处引用）：
```
Lcn/uc/gamesdk/e/a$a;.<init>(Context)   co=0xFBE4C
  0 invoke RelativeLayout.<init>(Context)
  3 invoke LayoutInflater.from(Context)
  7 sget  v1, Lcn/uc/gamesdk/R$layout;->loading_view:I   ← NoClassDefFoundError 在这里
 10 invoke LayoutInflater.inflate(int,ViewGroup,boolean)
```
`Lcn/uc/gamesdk/R$layout;` 在游戏 dex 里**只有 type 引用、没有 class_def**；它的资源包
（package `cn.uc.gamesdk`）从未下载下来——设备上两个 loader 包的 R 类只有
`cn.uc.gamesdk.lib.R$*` 与 `cn.uc.gamesdk.loader.R$*`。

**补丁**：`patch_sdk_float.py` 把该 `<init>` 的 inflate 段替换为 `return-void`
（保留 super 构造），悬浮窗变成空 View。
实测：**`R$layout` 异常从每次必现降到 0 次** ✓，SDK 初始化照常成功 ✓，客户端稳定运行 ✓。

**但 SDK 自己的登录走不通**：在初始化回调里调 `MainActivity.Login()`（= `GHome.login()`）后，
SDK 的 `loginUI` 会同步访问它已下线的服务器 → UI 线程被阻塞 → 系统报
`Activity pause timeout` 并把进程杀掉（实测进程被 `Killing ... remove task`）。
⇒ **SDK 的真实登录路径不可用（服务端已死）**。

**因此下一步（未做，但路径清晰）**：不要让 SDK 去联网登录，而是**把 SDK 的登录短接成成功**——
在 `GHomeChannelBase.loginUI` / `GHomeOnlineApiBase._login` 这一层直接回调
`doCallback(code=0, msg, data{userid,ticket})`，绕过网络。
这样"游戏自己点开始游戏 → `GHomeSendManager.Login` → Java `MainActivity.Login()` → SDK 立刻成功
→ `MainActivity$2.callback` → C# 收到 SDK 形态的登录结果"，游戏自己的流程才会被创建
（`FlowTrigger`/`FlowState` 静态才会初始化）。
需要先确认游戏标题画面的"开始游戏"入口（`MainMenu::OnLogoPressed` 0x9DA004 是候选；
之前扫点触屏没有触发任何登录调用，可能点偏了或按钮不在那）。

**当前可安装的稳定构建**：`patch_v49.py`（伪造嵌套登录报文）+ `patch_sdk_float.py`（去掉悬浮窗崩溃）
+ `build_v46.sh`。设备上就是这一版：能启动、SDK 初始化正常、无 R$layout 异常。

## 5.9 标题画面到底是什么（机器可读结论）

本环境里截图能抓到（PNG 在本地），但图片回传给模型是二进制占位符，**无法"看图"**；
改用"挂钩 + logcat 打点 + 坐标扫描"把画面变成事件流：

- 分辨率是 **1920×1080 横屏**（先前一次扫点 y 取到 1500，大多落在屏外，已纠正）。
- 两次截图差异只有 **0.06%**，集中在 (919,615)-(1000,696) 一小块 80×80 的动画元素 → **静态 splash**。
- 挂钩 `AbstractLoadingMenu` / `MainMenu` 的交互函数后，启动时只出现三条：
  ```
  OP (OnOpen) → CT (ClearTip) → ST (SupportsTips)
  ```
  之后再无 `PI/IN/PO/OU`，且**全屏 42+ 个坐标点击没有任何按钮处理函数被触发**，
  SDK 登录也未被触发 ⇒ **标题页不是菜单，是加载屏，而且它没有可点元素，它在"等"**。

所以"给一张截图"最多只能确认这是一张静态 loading 图；真正缺的是**加载屏在等什么**：
等 SDK 登录/后端会话。而 SDK 登录必须联网（服务端已死），我们伪造的 Unity 登录消息虽被 C# 接收并写入
`TicketStr/UserStr`，但 `LoginCallBack` 订阅者为 null、`FlowTrigger/FlowState` 静态从未初始化，
**流程状态机根本没被创建**。

**两个明确的下一步候选**
- A：让 **SDK 的登录当场成功**——patch `GHomeApiBase.login(Activity,Callback)`（co=0x13EF18，37 单元）
  直接调用 `doCallback(activity, callback, 0, "ok", HashMap{userid,ticket,isGuest})`
  （5 参 doCallback = method_id 15842；键名 'userid'/'ticket'/'isGuest' 与值 '1'/'true' 均为现成字符串），
  使"SDK 已登录"在 Java 侧成立，看加载屏是否放行。
- B：找出游戏自己"发起登录"的那一次调用——`GHomeSendManager::Login` 在整个 v7a 里没有任何 `bl`
  （ARM/Thumb 都扫过），说明它在接口/委托/异步状态机里被调用，需要换一种方式定位（查 vtable / 状态机类）。


## 6. 下一步（按性价比）

1. **判定 `[表+0x30]` 是否为 null**（一次构建，10 分钟）：把 `build_v51.sh` cave 里的 `beq` 跳过改成故意 `ldr r0,[r0]`（r0==0 时必崩），
   看 logcat `fault addr` 是否为 `0x0`。
2. **确认订阅者**（静态）：`GHomeSendManager::Login`(0xA7B5DC) 里有 `str r4,[r5,#0xc]!`，是全库唯一给 `LoginCallBack` 赋值的地方；
   该方法用 ARM `bl` 与 Thumb `bl` 扫描都是 0 调用点 ⇒ 只能经方法指针表间接调用（`g_MethodPointers` 槽 `0x317fc68`）。
   下一步应查该槽的读者（`ldr`/`add pc` 形式）而不是简单 `bl` 扫描。
3. 若确认无订阅者：**在 C# 侧替游戏调用一次登录发起**。可选：
   - 原生补丁调用 `GHomeSendManager::Login(Action<int>)`（需要拿到游戏自己的委托，风险高）；
   - 或反过来：让 Java 侧在 `MainActivity$2.callback` 之前**先**走 `InitResultCallback` 完成 `IsInit`，再发成功登录（顺序问题）；
   - 或找游戏里真正驱动登录的 UI 入口（`MainActivity.Login` 的 C# 调用者，可能藏在 UnityEvent/UI 绑定里，静态调用图看不到）。
4. 拿到会话后：影子后端按 `rest_spec.md` 补齐 `characters` / `towns/current` 的最小闭环。

## 7. 复跑

```bash
cd H:/Project/threeKingdomSlayer/threeKingdomSlayer/Library/Locus/tmp/blades
python patch_v49.py              # 生成打补丁的 build/classes.dex
bash build_v46.sh                # 打包+签名（约 3-5 分钟）
# Windows 上 apksigner 需要：export JAVA_HOME="C:\Program Files\Java\jdk1.8.0_181"
adb -s emulator-5554 install -r build/blades_v46.apk
bash run_v49.sh                  # 两段式：冷启动 45s → HOME → 回前台 → 抓证据
```

**注意**：Locus 每次 bash 调用结束会回收进程树，后端与 adb server 会一起死，所以"起服务 → 拉游戏 → 等 → 抓日志"必须在同一次调用内完成。
