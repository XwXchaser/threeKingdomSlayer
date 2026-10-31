---
id: kd_7d3216cb-867e-4546-84e4-1fde33316968
injectMode: inherit
injectAgents:
- '*'
summary: 已停服手游《上古卷轴：刀锋》安卓包逆向产物的索引与复跑方法：产物位置（Library/Locus/tmp/blades，types/methods/fields/string_literals CSV、257 张设计数据表、rest_spec.md REST 契约 117 路径/165 DTO、dump 与 build_rest_spec 脚本、本地影子后端 server.py + TLS 证书、probe.sh 一键实验脚本）、global-metadata v24 解析要点（pair 表从偏移 8 开始、type record 92 字节、method_count@68 与 field_count@72、字段记录是 (nameIndex,typeIndex,token)）、雷电模拟器实测环境（Android 7.1.2、adb 已 root、启动固定停在「更新包下载失败 0-10000-1」、根因 Curl error 6 解析 bladesasia.jijiagames.com 失败、SDK 账号链仍活）、本地影子后端接线与还原步骤（/system remount rw、系统 CA 库、hosts、adb reverse）、六个已知坑（adb reverse 需同进程、禁止 adb connect 5555、pm clear 后要重授权等）与待续事项。仅私用研究，产物不入库不分发。
aiEditMode: inherit
---

# Blades（已停服安卓包）逆向产物索引与复跑方法

> **是什么**：对已停服手游《上古卷轴：刀锋》(Blades) 的安卓包做静态逆向 + 模拟器实测后留下的**产物索引与复跑方法**。
> **为什么留**：设计借鉴结论见 `design/blades-architecture-benchmark.md`；本文只存「产物在哪、怎么再生成、实测环境怎么复现」这些**读文档得不到的实操信息**。
> **边界**：抽取的美术/音频/资产表**仅本地私用做设计参考**，不入库、不进 Unity 项目、不对外分发。原始 APK 未做任何修改。

---

## 1. 产物位置与状态

全部在 **`Library/Locus/tmp/blades/`**（Unity 的 `Library` 目录 → **不在 git 里，但可能被 Unity 清理**）。
清理后按 §3 两条命令即可完整重建（除 `server.py` 需手写，见 §5）。

| 文件 | 内容 | 规模 |
|---|---|---|
| `types.csv` | 托管类型清单（namespace / base / 方法数 / 字段数 / 接口标记） | 14498 行 |
| `methods.csv` | 方法清单（类型 + 方法名） | 99958 行 |
| `fields.csv` | 字段清单（所属类型 / 字段名 / 字段类型） | 62948 行 |
| `namespace_summary.csv` | 命名空间统计（类型数 / 方法数） | — |
| `string_literals.txt` | IL2CPP 字符串常量（API 路径、环境名、错误文案） | 15399 行 |
| `tables/*.csv` | 257 张设计数据表（技能/敌人/任务/对话/掉落/配方/挑战…） | 5326 行 |
| `rest_spec.md` / `rest_endpoints.csv` | **REST 实现规格**：117 条路径模板 + 165 个 DTO + 114 个服务类 + 通用请求信封 + 最小可实现集 | — |
| `dump_metadata.py` / `dump_tables.py` / `build_rest_spec.py` | 解析与生成脚本（依赖 `UnityPy`） | — |
| `server.py` + `ca.crt` / `server.crt` / `server.key` / `openssl.cnf` / `server.cnf` | 本地影子后端（TLS 日志服务器 + 路由表 `routes.json`） | — |
| `probe.sh` / `run_handshake.sh` / `full_log.sh` / `probe_full.sh` / `sweep.sh` | 模拟器实验脚本（一键：清数据→授权→启动→等请求静止→抓日志→截图；`sweep.sh` 可批量跑 `shapes/*.json`） | — |
| `make_shapes.py` / `make_gate_variants.py` / `make_manifest.py` / `matrix.py` | 生成 MetaManifest / host_info 候选形状与假设矩阵（`matrix.py` 自动跑一组变体并用截图字节数判别结果） | — |
| `minijson_test.py` | Calvin Rien MiniJSON 解析器的逐字节复现（验证客户端能否解析我们下发的 body） | — |
| `dnstrace.sh` / `probe_full.sh` | 抓全 buffer logcat / DNS 解析轨迹 | — |
| `disasm_il2cpp.py` / `disasm_lit.py` | IL2CPP arm64 反汇编器（按 RVA 标注 C# 方法名 / 解析字符串字面量） | — |
| `il2cpp_index.py` + `il2cpp_index.json` | **反汇编索引工具**：一次扫完 `il2cpp` 段（26.7MB）建立「字面量槽→使用点」与「调用目标→调用点」两张表；查询 `python il2cpp_index.py slots "Manifest"` / `call 0x<rva>` | — |
| `dumper_out/dump.cs` | **Il2CppDumper v6.7.46 完整 C# 头文件（25 MB）**：所有类型/字段/方法签名与 RVA | — |
| `dumper_out/script.json` | 方法地址表（65 MB） | — |
| `dumper_out/stringliteral.json` | 15399 条字面量：地址 → 文本 | — |
| `dumper_in/libil2cpp.so` + `global-metadata.dat` | 反汇编输入（arm64） | — |
| `pylibs/` | 本地 pip 目标目录（capstone/lief）—— 托管环境的 site-packages 会被清理 | — |
| `Il2CppDumper/` | 解压后的 dumper 工具 | — |
| `logcat_boot2.txt` / `logcat_full.txt` / `logcat_p3.txt` / `boot_screen2.png` / `probe_screen.png` | 启动日志与错误页截图 | — |
| `cache/` | 中间解包产物（可删） | 数百 MB |

> ⚠ `cache/` 是 `dump_tables.py` 的中间目录，占空间；不需要时可删，脚本会重建。

---

## 2. 目标 APK 与身份

- 路径：`C:/Users/Administrator/Downloads/shanggujuanzhoudaofeng.apk`
- 大小 1.23 GB，sha256 `c49920c64df257ec8ccd5bee8f21f11aa7efcbb7dd15a6a5dd3a1305698e7ae2`
- 包名 `com.shengqugames.blades.aligames`，versionName 1.6.3.1.108（versionCode 108）
- Unity 2019.2.16f1 / IL2CPP / `global-metadata.dat` v24（type record **92 字节**；int16 count 偏移 **method_count@68 / property_count@70 / field_count@72** —— 这些偏移是本次踩过的坑）

---

## 3. 复跑方法

```bash
# 依赖：pip install UnityPy
cd <workspace>/Library/Locus/tmp/blades

python dump_metadata.py     # → types.csv / methods.csv / fields.csv / namespace_summary.csv
python dump_tables.py       # → tables/*.csv（257 张）+ cache/
python build_rest_spec.py   # → rest_spec.md / rest_endpoints.csv
```

要点：
- `dump_metadata.py` 直接从 APK 读 `assets/bin/Data/Managed/Metadata/global-metadata.dat`，**不依赖 APK 已解包**。
- 解析 global-metadata 的 header 时，**pair 表从文件偏移 8 开始**（0–3 是 sanity，4–7 是 version），每 pair 8 字节 = (offset, size)；`string` 堆在 pair index 2，`typeDefinitions` 在 pair index 19，`methods` 在 5，`fields` 在 11。
- **v24 结构体实测偏移（照抄，别再猜）**：
  - `Il2CppTypeDefinition` = **92 字节**；`nameIndex`@0、`namespaceIndex`@4、`parentIndex`@20、`fieldStart`@36、`methodStart`@40；**`method_count`@68**、`property_count`@70、**`field_count`@72**（均 uint16）。
    自检口径：method_count 与「methods 表按 declaringType 聚合」应 100% 一致（实测 14498/14498）。
  - `Il2CppFieldDefinition` = 12 字节，顺序是 **(nameIndex, typeIndex, token)** —— name 在前！（写反会读到字符串后缀，如 `indowPhone8`）
  - 字段的 `typeIndex` 指向 **Il2CppType 表**（不是 typeDefinition 表），下标可 > 14498，无法直接解析成类型名 —— 只用字段**名**即可。
  - 字符串常量表 = pair0（15399 条 {length,dataIndex}）+ pair1（数据段）。
- `dump_tables.py` 依赖 AssetBundle 内**保留了 type tree**（本包是，828/828 个 MonoBehaviour 全部可读字段）。
- `build_rest_spec.py` 从字符串常量抽路径模板、从 `fields.csv` 抽 DTO 字段，输出 `rest_spec.md` + `rest_endpoints.csv`。

---

## 4. 雷电模拟器实测环境（可复现）

环境：雷电模拟器，Android **7.1.2**（API 25），x86 宿主 + ARM 翻译（`ro.product.cpu.abilist = x86,armeabi-v7a,armeabi`），**adb 已 root**（`adbd is already running as root`）。

安装与启动：
```bash
ADB="$LOCALAPPDATA/Android/Sdk/platform-tools/adb.exe"     # adb 在本机 Android SDK 里，设备号 emulator-5554
adb install -r -t "C:/Users/Administrator/Downloads/shanggujuanzhoudaofeng.apk"
adb shell pm grant com.shengqugames.blades.aligames android.permission.READ_EXTERNAL_STORAGE   # 其余权限按需
adb shell am start -n com.shengqugames.blades.aligames/com.shengqugames.blades.MainActivity
```

**启动表现（固定现象）**：能进到标题画面 → 弹「网络错误 / 更新包下载失败，请确保你的设备网络连接正常。(0-10000-1)」。
根因（logcat 原文）：`E/Unity: Curl error 6: Could not resolve host: bladesasia.jijiagames.com` —— 拉不到引导文件 `https://bladesasia.jijiagames.com/china/Android/host_info_cn.json`。
注意：**SDK 账号链是活的** —— `api.mygm.sdo.com/v1/basic/handshake` 成功、日志 `Ghome init is 0`。

### 本地影子后端（把死域名指到本机）

```bash
# 1) /system 改可写 + 装自签 CA 到系统证书库
adb shell "mount -o rw,remount /system"
adb push ca.crt /data/local/tmp/ca.crt
adb shell "cp /data/local/tmp/ca.crt /system/etc/security/cacerts/<openssl subject_hash_old>.0"
#    （targetSdk 26 → 应用不信任「用户 CA」，必须装进系统 CA 库）
# 2) 改 hosts
adb shell "printf '127.0.0.1 bladesasia.jijiagames.com\n127.0.0.1 bladesqagn.jijiagames.com\n...' >> /system/etc/hosts"
# 3) 设备 443 → 本机 8443
adb reverse tcp:443 tcp:8443
# 4) 启动日志服务器（请求全量落 requests.log，路由由 routes.json 热改）
python server.py --port 8443
```

**还原模拟器**（做过上述改动后）：
```bash
adb shell "rm -f /system/etc/security/cacerts/<hash>.0"
adb shell "sed -i '/jijiagames\|playbladesasia/d' /system/etc/hosts"   # 或直接重写 hosts
```

### 已知坑
- `adb` 服务在两次 bash 调用之间会被回收（每次需自动重连，输出里的 `daemon not running` 是正常现象）；**`adb reverse` 必须在同一次调用内设好并立即使用**，否则隧道丢失。
- 绝不要执行 `adb connect 127.0.0.1:5555`：模拟器本来就以 `emulator-5554` 在线，多挂一个设备后所有 adb 命令会报 "more than one device/emulator"。用 `ANDROID_SERIAL` 指定设备。
- Git Bash 会把 `/data` 之类的参数转成 Windows 路径 → 调 adb shell 时先 `export MSYS_NO_PATHCONV=1`。
- 雷电模拟器上的 `adb` 要用**完整路径**（不在 PATH 里）。
- `pm clear` 会清掉运行时权限授权 → 必须重新 `pm grant`，否则卡在权限弹窗、根本到不了网络层。
- APK 有 1.23 GB → 安装用 streamed install，约 60 秒。
- 启动后的错误页编码在 `0-10000-N` 空间：`-1` = 更新包下载失败；`-7` = 未知错误。

### 操作小技巧（很实用）
- **用错误页截图的 PNG 字节数当断言**（同一分辨率下稳定）：2069666 = 「网络错误/更新包下载失败」、2235060 = 「未知错误 0-10000-7」、2066418 = 「网络错误+发生未知错误」+[可以]、强制更新 = 「需要软件升级」。`matrix.py` 就是用这个做自动判别的。
- 错误画面对应的本地化键在 `cache/localization`（5 种语言）的 `Bundle.Message.*` 里，可直接对照文案反推是哪条消息。
- 模拟器上把域名指到本机时，**把编译进客户端的所有后端主机都加进 hosts**（`bladesasia/bladesqagn/bladesqahw/bladesqahk/qa.playbladesasia/www.playbladesasia` + 14 个 `bgs-ping-*.s3.<region>.amazonaws.com`）。

---

## 5. `server.py` 说明（手工写的，需重建时参考）

- Python 标准库 `ThreadingHTTPServer` + `ssl`，不依赖第三方包。
- `routes.json` 形如 `{"routes":[{"method":"GET","path":"/china/Android/host_info_cn.json","status":200,"json":{...}}]}`；命中则按路由回，否则 404，并尝试回落 `www/<path>` 静态文件。
- 所有请求（方法/路径/请求头/body）追加写入 `requests.log`。
- 证书：自签 CA（`ca.crt`/`ca.key`）+ 服务端证书（`server.crt`/`server.key`，SAN 覆盖 5 个游戏域名 + localhost），SAN 配置在 `server.cnf`。

---

## 6. 待续事项（离线化方向，尚未完成）

1. 跑通「本地服务器 + 客户端」握手，抓到 host_info 与 MetaManifest 两个请求 → **已完成**。
2. 反推 `host_info_cn.json` / `MetaManifest.json` 的精确结构 → **已完成**（见 plan 文档 §2.5）。
3. 卡在 `0-10000-7`（UnexpectedError）：两个存活假设 = ① MiniJSON 顶层解析结果类型不符（用 Python 复刻 Calvin Rien 的 MiniJSON 逐字节比对，并试紧凑无空白输出）；② 客户端已判定无需下载、进入本地资源加载，但 `assets/Bundles/ManifestExtraData`（1.543 MB 疑似加密）解密失败 → 需从 APK 定位 `BuildVersionAsset` 实例的真实 5 段版本值，并找该 blob 的密钥来源。
4. 之后才是 M3–M5：本地账号/会话 → 城镇 → 战斗任务闭环（契约见 `rest_spec.md`）。

---

## 7. 反汇编工具链（关键技巧，已跑通）

```bash
# 1) 取 dumper 输入
python - <<'PY'
import zipfile
z=zipfile.ZipFile(APK)
open('dumper_in/libil2cpp.so','wb').write(z.read('lib/arm64-v8a/libil2cpp.so'))
open('dumper_in/global-metadata.dat','wb').write(z.read('assets/bin/Data/Managed/Metadata/global-metadata.dat'))
PY

# 2) 跑 dumper（需要 .NET；本机 dotnet 6/8/10 均可用）
cd dumper_out && ./Il2CppDumper.exe ../dumper_in/libil2cpp.so ../dumper_in/global-metadata.dat

# 3) 按 RVA 反汇编并用 dump.cs 标注调用名
python ../disasm_il2cpp.py 0x1681C98 "BundleMetaManifest::Deserialize" 0x500
```

**关键技巧：解析字符串字面量引用。**
IL2CPP 把字面量放在一个全局槽表里（`adrp xN, page` + `ldr xN, [xN, #off]`），槽里存的是指向 `Il2CppStringLiteral` 的指针。而这些指针是**运行时初始化**的（.bss），静态读不到 —— 但**全局槽本身**是可读的：
读 `page+off` → 得到指针 `V`；若 `V` 恰好落在 `stringliteral.json` 的地址集合里 → 直接得到该字面量的文本。
（实现见 `disasm_lit.py`；本次用它实证了 `minGameVersion` / `manifestHash` / `enabled` 三个属性名。）

**arm64 指令编码备忘**：`LDR Xt,[Xn,#imm]` 的 32 位编码 = `0xF9400000 | (imm12<<10) | (Rn<<5) | Rt`，**实际偏移 = imm12 × 8**。
（所以「读某个结构体偏移 0xF8 的字段」要搜 `imm12 == 0x1F`，而不是搜 `0xF8`。）

**通用方法表映射**：从 `dump.cs` 扫 `// RVA: 0x...` 注释 + 其下一行签名，构建 `RVA → 类型::方法名`；反汇编时凡 `bl` 到已知 RVA 就直接标出方法名，等于拿到了“伪 C# 调用图”。

---

## 7. v6 工具链（ARM32 侧，比 arm64 更实用）

模拟器实际跑的是 **armeabi-v7a**（houdini 转译），所以真正生效的补丁必须打在 v7a 库上；
arm64 库只用于将来上真机。

| 脚本 | 作用 |
|---|---|
| `dumper_out_v7a/dump.cs` + `script.json` | **ARM32** 版的类/方法/字符串字面量表（与 arm64 同结构） |
| `dis_v7a.py <rva> [size]` | ARM32 反汇编器，**自动解析 `bl` 目标为 `类::方法名`**，`ldr [pc,#imm]` 还能解出字符串字面量 |
| `strs_in_func.py <rva> [size]` | 只扫函数里的字符串常量（穿透 metadata usage 链，**最多 4 层**） |
| `verify_patch.py` | 校验补丁点旧编码与预期一致（ARM32 `BL` 编码 = `0xEB000000 \| (imm24)`，目标 = `pc+8+(imm24<<2)`） |
| `find_bundle_classes.py` | 按类名批量导出方法 RVA（bundle 流程用） |
| `callsites_v7a.py <rva...>` | **ARM32 调用点扫描**：找出所有 `bl` 到指定 RVA 的位置，并归属到调用者函数 |
| `iface_dispatch_v7a.py [slot]` | **ARM32 接口派发扫描**：形态 = `ldr r3,[r0,#0x58]` + `mov r4,#slot` + `uxth r6,r4` + `ldr r7,[r3,r6,lsl #3]` + `blx r7`；按 slot 列出调用点（配合 dump.cs 的 `Slot: N` 注释定位“谁调用了哪个接口方法”） |
| `build_and_test_v5.sh` | 一键：打补丁→重建 APK→zipalign→签名→安装→预置 bundle→起后端→运行→抓日志 |
| `build_and_test_v31.sh` … `v45.sh` | 每版一个的补丁构建脚本（v31~v45），每个含**逐条反汇编自校验**（capstone 断言）；当前设备装的是会崩的 v45，用 v34 恢复干净包 |
| `play_offline.sh` | **一键起服+拉起客户端**（TLS 影子后端 + 文件服务器 + adb reverse + 打印请求序列），`WAIT=300` 可延长观察；`run_full_log.sh` 抓 Unity 全级别日志 |
| `trigger_scan.py` / `find_trigger_reads3.py` | 扫 FlowTrigger::Fire 调用点并识别触发的触发器；反推「FlowTrigger 静态字段槽」并全库反查读取点（ARM32 位模式扫描） |
| `find_cave.py` | 找可安全注入的代码洞（两个函数之间的填充区） |
| `screen_*.png` `requests.log` `unity_all.log` | 各轮实测截图、全量请求日志、Unity 日志 |
| `run_v4.sh` | 只跑运行段（`SKIP_PUSH=1` 跳过 1.1GB 推送，`WAIT=<秒>` 控制观察时长） |
| `file_server.py` | **纯 HTTP** 本地文件服务器（端口 8080），专供设备 80 端口用 |
| `OFFLINE_README.md` | 离线化复现说明（接线、补丁清单、坑、剩余工作） |

**ARM32 编码备忘**：`BL` = `0xEB000000 | imm24`，imm24 = `(target-(pc+8))>>2`；
`MOV Rd,#imm` = `0xE3A00000 | (Rd<<12) | imm`；`B` = `0xEA000000 | imm24`。

**IL2CPP 字符串常量解析（ARM32）**：`ldr rX,[pc,#imm]` → 池地址 → `*池` → usage 指针 → `*usage` → `Il2CppString*`。
**静态读不到最终字符串**：usage 槽是 .bss，运行时才被填成 0xFFFFFFFF 附近的值；
所以「哪个字符串常量对应哪段代码」**无法静态确定**，只能靠运行时行为反推（这也是为什么
「本地 extra data 文件名」最后是用 `adb reverse tcp:80` + 文件服务器实测出来的，而不是静态逆出来的）。

**补丁自校验的写法**（强烈建议沿用）：改之前先用 capstone 反汇编出原指令并 `assert` 等于预期文本，
否则二进制布局一改就静默写坏机器码。

---

## 8. 模拟器 / adb / 进程生命周期的硬规则（踩过很多次）

| 规则 | 原因 |
|---|---|
| **一切要在同一次命令调用内完成**（起后端 + `adb reverse` + 启动客户端 + 抓日志） | Locus 工具会在一次调用结束时**回收整棵进程树**，`nohup`/`Start-Process` 起的服务器与 adb server 也会被杀 |
| **adb 固定 `-s emulator-5554`** | 残留的 `127.0.0.1:5555 offline` 条目会让裸命令报 "more than one device"；先 `adb disconnect 127.0.0.1:5555` |
| **不要用 `adb logcat -d`（全量）** | 会把 adb 连接拖死（客户端挂起、后续命令全超时）；用 `-d -t <N>` 限量，或 `-s Unity:E Unity:W` 过滤后落盘 |
| **⛔ 禁止盲点击（2026-10-30 事故）** | **任何 `input tap` 之前必须确认前台是游戏**（`adb shell dumpsys activity activities \| grep mResumedActivity` ✓）且**坐标来自游戏自己的事件流**（`build_vtap2_cave.py` 的 hook + logcat 打点 ✓）。<br>事故记录：在模拟器桌面（前台不是游戏）时用 96 个盲点扫全屏 ⇒ 打开了无关 APP、并误触系统卸载流程，**卸载了用户设备上的 `com.qy.mtjq.ld`**。<br>⇒ 文档里早就写了「本环境无法读图 ⇒ 找按钮必须走 hook + 打点」✓，盲点击从来不是允许的手段 ✗ |
| **重启模拟器后要重授权限** | `pm grant` READ_PHONE_STATE 等 7 项；否则游戏卡在系统权限弹窗「正在初始化...」，表现为“启动很慢/卡死” |
| **推 bundle 直接推应用目录** | `adb push bundles_src/. /storage/emulated/0/Android/data/<pkg>/files/Bundles/`；先推暂存目录再 `cp -a` 会**静默失败**（`cp` 在 FUSE sdcard 上不可靠） |
| **每次重装后要重推 bundle** | Android 卸载会删掉 `/storage/emulated/0/Android/data/<pkg>/`（含 Wwise 抽取缓存，重抽要几分钟） |

**houdini（ARM 转译）下的两个特性**：

1. ARM 代码跑在 houdini 里，**native backtrace 无法回溯 ARM 帧**（只有 1 帧）→ 想定位函数要用「唯一故障地址」自证。
2. houdini 会把 `ldr pc` 的跳转目标**按 4 字节对齐**（低位丢失）→ 探针目标地址必须 4 字节对齐才能区分。

**「崩溃地址探针」模板**（定位 IL2CPP 函数被调用的最快手段）：

```python
# 把函数入口第 1 条指令改成 ldr pc,[pc,#0]，池字放 4 字节对齐的唯一故障地址
v7a[off:off + 4]     = struct.pack('<I', 0xE59FF000)   # ldr pc, [pc, #0]
v7a[off + 4:off + 8] = struct.pack('<I', 0xE3A00000)   # 占位
v7a[off + 8:off + 12] = struct.pack('<I', 0xDEAD0000 + (i + 1) * 4)
```
读 `logcat` 的 `Fatal signal 11 (SIGSEGV) ... fault addr 0xdead00xx` ⇒ 直接指认命中的是哪个函数。
⚠ 命中即崩溃，只能得到“**最早**被调用的那个探针”；想知道更后面的节点，需去掉靠前的探针重跑。

---

## 9. IL2CPP 字符串与「流程点火」的关键事实（Blades 逆向沉淀）

**字符串字面量的地址不在 .so 里** ✗：`script.json` 的 `ScriptString` / `stringliteral.json` 的 `Address`
是 **global-metadata.dat 内的偏移**（数值大于 libil2cpp.so 文件大小 ✓）⇒
**不能用「池字 + ldr」直接构造一个字符串常量** ⇒ 想给某个 API 传字面量，只能复用**元数据 usage 槽**
（槽地址在 .data 里是静态的 ✓，但槽里存的 `Il2CppString*` 是运行时填的 ✓，需再 RE 一层才能定位到具体字面量）。

**流程状态机（Blades 的 `FlowState`/`FlowTrigger`）**：

- `FlowTrigger::Fire()` 是点火动作 ✓；`FlowTrigger::GetTriggerByName(string)` 可按名字取触发器 ✓
  （由动画事件 `AnimEventFlowTriggerSource::OnAnimEvent` 调用 ✓ ⇒ **动画事件可以驱动流程跳转** ✓）。
- 状态机是**事件驱动的**：菜单/场景的动画事件负责点火下一步 ⇒ 某个动画事件没触发，流程就会静默停住 ✓
  （看起来像“卡死”，实际是在等一个永远不会来的事件）。
- 判断“客户端是否卡住”的可靠方法：**用崩溃地址探针**看它到底走到了哪个节点 ✓；
  若探针全不命中且画面正常 ⇒ 它不是在卡，而是**在等玩家操作** ✓。

**Unity 客户端的「无按钮」陷阱**：渠道包的登录入口常由**渠道 SDK 的悬浮窗**提供（Java 侧 View），
SDK 服务一旦下线，按钮永远不出现 ⇒ 只能靠**补丁**绕过（点火流程 / 造本地存档）✓。

**IL2CPP 存档（SaveService）通常保存登录凭据** ✓：例如匿名 userId / deviceId / token 的键值，
以及 `IsAnonymousSignedIn`、`IsAcceptedToPlay`（EULA）等标志 ⇒
**离线路线常是“写一份本地存档”而不是“改客户端”** ✓；且 JSON 存档格式往往是明文 ✓（便于手工构造 ✓）。

---

## 9. 第二阶段新增：本地自建工具链与实测结论（2026-10-29 晚间）

> 详细过程见 `plan/blades-java-bridge-breakthrough-20261029.md`；本节只留索引与硬事实。

### 9.1 新增脚本（均在 `Library/Locus/tmp/blades/`，纯本地、零下载）

| 脚本 | 作用 |
|---|---|
| `dexlib.py` | 极小 DEX 读写库（表解析 / class_data / code_item / 35c·21c 编码 / map 修复 / 字符串原位覆写） |
| `dex_dump.py` | dex 方法反汇编（`refs <字符串>`、`meth <名字>`） |
| `patch_v49.py` | Java→Unity 伪造登录报文（**必须嵌套 `data{...}`**，平铺无效） |
| `patch_sdk_float.py` | 去掉 SDK 悬浮窗的 `R$layout` 崩溃（唯一引用点 `e/a$a.<init>` co=0xFBE4C unit7） |
| `patch_init_login.py` | 实验：初始化回调里调 `MainActivity.Login()`（⚠ 会阻塞 UI 线程导致进程被杀） |
| `build_v46.sh` | 打包+签名（`JAVA_HOME` 需指向 jdk1.8.0_181） |
| `build_v51.sh` / `build_vA*.sh` / `build_vtap*.sh` | 含原生 cave 钩子的构建（capstone 逐条自校验 + 跳转目标数值核对） |
| `run_live_check.sh` / `run_v46.sh` / `run_v49.sh` / `run_retrigger.sh` | 跑测：起后端 + adb reverse + 拉起 + 抓日志/截图/请求序列（必须同一次 bash 调用内完成） |
| `ucsdk.dex` / `loader.odex` / `loader_v2.apk` | 从设备拉下的 UC SDK 产物（odex 内嵌 dex 已抽出为 `ucsdk.dex`） |

### 9.2 硬事实（均有日志/崩溃地址证据）

- **本环境无法读取图片**：截图 PNG 能抓到，但回传给模型是二进制占位符 ⇒ “看画面找按钮”必须改成
  **hook + logcat 打点 + 坐标扫描**（`build_vtap2_cave.py` 已建好），把屏幕变成事件流。
- **标题页是加载屏 `AbstractLoadingMenu`，不是菜单**：启动只出现 `OnOpen → ClearTip → SupportsTips`，
  之后无事件；42+ 坐标点击零命中；两张截图差异 0.06%（仅 (919,615)-(1000,696) 一小块在动）⇒ 静态 splash，**无可点元素**。
  分辨率 **1920×1080 横屏**。
- **登录入口在 Java 侧**：`MainActivity.Login() → GHome.login() → MainActivity$2.callback(code,msg,map)
  → MainActivity.sendUnityMessage("LoginResultCallBack", …)` ⇒ C# `GHomePostManager.LoginResultCallBack`。
- **报文形状决定成败**：嵌套 `{"code":0,…,"data":{"userid":"1","ticket":"1"}}` 会被 C# 解析并写入
  `TicketStr`/`UserStr`；顶层平铺无效。
- **【已被第三阶段实测推翻】`LoginCallBack` 订阅者为 null**（`[GHomePostManager+0x0c]=0`）⇒ 收到了也没人推进流程。
  → 实测：真正的原因是**流程卡在 `InitializingGame` 状态的加载屏动画**（`PlayInAnim` 从不执行），与 `LoginCallBack` 无关；
  详见 plan/blades-flow-trace-breakthrough-20261029.md
- **【已被第三阶段实测推翻】`FlowTrigger`/`FlowState` 静态至今从未初始化** ⇒ 游戏的流程状态机根本没被创建。
  → 实测：状态机**正常工作**（`FlowManager.Init→Fire→DoFire→DoTransition→InvokeEntered` 跑通 3 次）。
  真正的卡点是 `InitializingGame` 的 transition handler `b__222_223`(0x10E94C4) 以 **null context**
  打开加载屏 ⇒ `AbstractLoadingMenu::OnOpen`(0xB1852C) 在 `0xb18588 popeq` 提前 return
  ⇒ `PlayInAnim`(0xB18790) 不执行 ⇒ 动画完成回调不触发 ⇒ 流程停在初始化阶段。
- **`GHomeSendManager::Login`(0xA7B5DC) 打点实测全程 0 次命中** ⇒ 游戏 C# 侧从不发起渠道 SDK 登录
  ⇒ 「短接 SDK 登录」路线确定无效。
- **`Debug.LogError` 的输出在 logcat 里完全看不到**（0 条 `E Unity`）⇒ 想知道错误文案只能自己从
  `Il2CppString`（chars 在 +12，UTF-16LE）里读。
- **多点 logcat 打点器已建成**（`build_vtrace.py`）：把函数入口换成 `b cave`，cave 里补做原序言→打一条 logcat→原样跳回，
  一次运行拿到 17 个流程节点的事件序列，取代了「一次只能命中一个」的崩溃地址探针。
  含 `('str2',)` 模式（读 Il2CppString 字符）与 `('mem', reg, off)` 模式（读结构体字段）。
- **可靠触发点只有 `GHomePostManager::InitResultCallback`**（引擎就绪那一刻）；Unity 消息只在**引擎就绪前**发出才会被排队投递。
- **ARM `ldr` 的 imm12 是字节偏移**（原函数 `0xE590105C` = `[r0,#0x5c]`）——旧脚本的 `ldr_imm()` 除以 4 是错的，
  历史上 v31–v45 的“点火”全在读错误字段。
- **自行 `bl FlowTrigger::.cctor` 会打断托管执行流**（后续代码不执行，应用不崩但流程无变化）。
- **SDK 登录必然阻塞**：`GHome.login()` 同步访问已下线服务器 → UI 线程挂住 → `Activity pause timeout` → 进程被杀。

---

## 10. 第四阶段新增（2026-10-30 中午）：流程已打通 + ARM32 编码定案

> 详细过程见 `plan/blades-flow-trace-breakthrough-20261029.md` **4E 节**与 `plan/blades-handoff-20261029.md` 顶部「续跑简报」；
> 本节只留**跨会话仍然需要的硬事实与脚本索引**。

### 10.1 新增脚本（均在 `Library/Locus/tmp/blades/`，纯静态、不烧 build 周期）

| 脚本 | 作用 |
|---|---|
| `build_transition_table.py` | **迁移表抽取器**（177 条 → `transition_table.txt`）。要点：`Permit(__this, trigger, destinationState, method)` ⇒ **r1=触发器、r2=目标状态**；带 `StateConfiguration` 参数的 `.cctor` lambda 恰好 86 个，与 86 个 `FlowState` 静态字段**按编号顺序**一一对应 |
| `extract_transitions2.py` | 同上（更稳的前向数据流版） |
| `scan_fire2.py` | **触发器发射点全表**（含尾调用 `b`，共 137 处）—— 用来回答「哪个触发器在启动路径上没人发」 |
| `patch_c_fire.py` | **修法 C**：洞 A（hook `FlowState::SetupGameManagers` 置 `_menuInAnimationCompleted`）+ 洞 B（hook `MainThreadDispatcher::Update` 发 `Authenticate`） |
| `build_c.sh` | 打包修法 C 的 APK |

### 10.2 ★ ARM32 编码定案（之前两轮补丁都栽在这里，务必记住）

| 事实 | 说明 |
|---|---|
| `ldr rX,[pc,#imm]` | 立即数形式 PC = **insn+8** |
| `ldr rX,[pc,rY]` | **寄存器偏移形式 PC 也是 insn+8**（不是 ARM 手册的 +4）。证据：同一函数里 `add r0,pc,r0`(@0x801998) 与 `strb r1,[pc,r0]`(@0x8019C0) 算出同一地址 0x02A8B50F |
| 取类指针链 | **必须原样照抄**（含 `ldr r0,[pc,r0]` 这种 Rd==Rm 的形式）；改成 `ldr r1,[pc,r0]` 会**取到别的类**（实测：发出去的不是触发器，当场崩）。搬运时只需把池字按指令位移差重算：`V' = V + (A2_old - A2_new)`（与 PC 基准无关 ⇒ 有效地址逐位不变） |
| `strb/ldrb` 立即数 | **拆分立即数**编码（bit11=1，imm4 在 bits10-8，imm8 在 bits7-0）⇒ 手写极易错；**直接抄游戏自己的指令字再改寄存器字段** |
| `mov Rd,Rm` | Rd 在 bits15-12：`mov r0,r1` = **0xE1A00001**（写成 0xE1A01000 就是 `mov r1,r0` ⇒ 池字值留在 r0 ⇒ 下一条解引用直接崩，fault addr 恰好是池字值） |
| `ldr Rd,[Rn,#imm]` | Rn 在 bits19-16、Rt 在 bits15-12：`ldr r4,[r0,#0xa4]` = **0xE59040A4**（不是 0xE590A040） |

### 10.4 第四阶段的后端契约（已挖出，2026-10-30）

**★ 客户端把后端响应当作「扁平路径 → 值」来解析，映射表就藏在 APK 里。**

- `JsonFlatSchemaProvider`（`SCHEMA_EXTENSION = ".schema"`）与 `JsonConversionProvider` 读的是 **TextAsset**；
  这些资产在 **`assets/bin/Data/sharedassets0.assets`** 里（共 35 个 TextAsset：
  17 个 `.schema` + 若干 `.conversion`）。**APK 的 `assets/Bundles/*` 与设备上下载的 bundle 里都没有它们** ✗。
- ⚠ **UnityPy 读不出这批资产的正文**（报 `string argument without an encoding`）⇒
  要**手工按 TextAsset 的序列化布局解析**（两个 length-prefixed 字符串）：
  ```python
  n = struct.unpack_from('<i', raw, 0)[0]; name = raw[4:4+n]
  m = struct.unpack_from('<i', raw, 4+n)[0]; script = raw[8+n:8+n+m]
  ```
  脚本：`extract_schemas_apk.py` / `build_api_responses.py`（在 `Library/Locus/tmp/blades/`）。
- **`.schema` 文本** = `[{"path":"town/buildings/{id}/id","type":"{System.String}"}, ...]`
  ⇒ **响应体就是这些 path 组成的嵌套 JSON**；`{id}` 是数组下标占位；`{Object}`⇒{}、`{Array}`⇒[]、其余⇒""。
- **`.conversion` 文本** = 操作树（`UseSchema` / `Read{relativePath}` / `Write{schema,path}`）
  ⇒ 说明 **JSON 路径 → 客户端持久化路径** 的对应关系，例如
  `session/userId → BackendData:Session/UserId`、`session/token → Session/Token`。
- 关键 schema 清单：`session`（userId/token/schema/loginToken/tokenExpirationSeconds/featureStatus/linkedAccountsStatus）、
  `town`（townLevel{level,experiencePoints,levelUp,unlockedDistricts[]}、buildings[]{id,typeId,styleId,level,constructionEnd,isInConstruction,props}）、
  `chests`/`chest`、`Character`/`CharacterData`/`GetCharacter(s)`、`SaveGame`（6189 字节，全量存档同步）、
  `BackendData`（requestIndex/Session/…）、`ErrorData`/`error`、`inventory`、`wallet`、`GetQuests`。
- **响应体没有外层信封** ✓：`{"session": {...}}` 直接就是 session.schema 的形状；
  `requestIndex` 要回显客户端请求头里的 `Request-Index` ✓。
- ⇒ **通用响应策略**：把所有 schema 的根键合并成一个 JSON，对 `/api/*` 一律返回它
  （客户端只读自己 schema 里的 path，多余键无害）✓。路由表生成脚本：`build_routes.py`。
- 客户端侧还有一条中文断言 `'chai: responseRewardData为空'`（responseRewardData 为空时的调试日志）——
  奖励接口响应为空时它会打这条 ⇒ **可用它判断响应是否被正确解析**。

### 10.5 流程侧的最终结论

- 迁移表显示 `LoadingBaseGameContent` 的**唯一出口**是 `--Authenticate--> Authenticating`；
  而 `Authenticate` 触发器在**启动路径上没有任何发射点**（全二进制只 3 处：`LoadFirstLevel` / `IntroCutsceneDone` / `b__222_41`）。
- `FlowState` 的加载屏等待机制（私有静态字段）：0x118 `_waitForMenuInAnimationTaskControl`、
  0x11C `_waitForMenuOutAnimationTaskControl`、**0x120 `_menuInAnimationCompleted`**；
  `WaitForLoadingMenuInAnimation`(0x800D54) 读 0x120，**为 true 就立刻 `TaskControl::SignalCompletion`**。
- ⇒ **修法 C**（已实测跑通）：用 0x120 当「内容已就绪」标志，内容加载完后自己发一次 `Authenticate`：
  流程推进 → 客户端发出 `POST /api/game/v1/public/characters//towns/current/rewards/current`（带 `Blade-Signature` 头）
  ⇒ **影子后端第一次收到业务请求**；无崩溃。**`/auth/gplus` 仍未发出**（认证在取存档之后；而存档响应原本是空的 `{}`）。
  ⇒ 说明客户端**走的是 SDK 登录注入那条路**（`patch_v49.py` 注入的 userid/ticket 直接当会话用）✓。
- ⚠ **补丁洞必须放在 `build_vtrace.py` 预留的 0xA7B9EC..0xA7BABC 区**（探针洞从 0xA7BABC 开始）；
  否则覆盖 TERR 探针 ⇒ `Debug::LogError` 跳进野代码 ⇒ **启动 1 秒后崩**（本轮踩过）。
- ⚠ `FlowTrigger` 的 90 个对象**并非全部相邻分配** ⇒ 不要用「相邻地址」反推触发器名字。

---

## 11. 第五阶段沉淀（2026-10-30 深夜）：AAPCS 变长参数 + 响应的两条解析路径

> 这三条是**可复用**的通用知识，不只对 Blades 有效。

### 11.1 ★ AAPCS 变长参数的落位（本项目栽过一次的坑）

**ARM32 AAPCS**：

| 位置 | 归属 |
|---|---|
| 第 1~4 个参数 | `r0`~`r3` ⇒ **第 4 个参数槽是 r3** |
| 第 5 个及以后的参数 | 栈 ⇒ 调用方在 `bl` **之前**写在 `[sp]`、`[sp+4]`… |
| 被调方读栈参数 | `bl` 已经压了返回地址 ⇒ 它读到的是 **`[sp+4]`、`[sp+8]`…** |

⇒ 所以：**写栈变长参数要写 `[sp]`**（被调方在 `[sp+4]` 读到），
**而 `__android_log_print(prio, tag, fmt, vararg…)` 的第一个 vararg 是 r3**（不是栈！）。

⇒ 本项目后果：洞 B 里写 `str r2,[sp]` ⇒ 日志里打出来的其实是**没赋过值的 r3**（残值 `0x09096484`），
被误读成「被发射的触发器」⇒ 得出「洞 B 是空操作」的错误结论。
**探针器把要打印的值放进 r3（`'reg'` 模式）是对的**；改用 `mov r3,r4` 后立刻打出真触发器。

### 11.2 ★ 后端响应被客户端解析的**两条路径**（要求相反）

| 路径 | 入口 | JSON 值的要求 | 典型异常 |
|---|---|---|---|
| 转换路径 | `.conversion` → `OperationWrite.WriteValue` → `CreateOrUpdatePrimitiveTyped<T>` | **字符串**（它自己按字段类型再解析）⇒ 数值字段要给**合法数字串** | `FormatException: Input string was not in a correct format.`（空串喂给 Int32/UInt32） |
| 直接 cast 路径 | `SequentialParser.Parse` → `PersistentHelper.AssignPersistentValue<T>` | **原生类型**（按 `.schema` 直接 cast）⇒ 数值字段要 JSON number、布尔要 true/false | `InvalidCastException: Unable to cast object of type 'String' to type 'Int64'.` |

⇒ **同一个响应体要同时满足两者**：文本字段给字符串、**数值字段给 number** 最稳
（转换路径那边会 `ToString()`，数字一样能用 ✓）。
⇒ ⚠ **手写 parser（`TownParser` / `ChestParser` / `CharacterParser` …）的真实字段类型可能与 `.schema` 不一致**
  ⇒ `.schema` 对不上不代表没问题 ⇒ 需要**二分定位**（见 11.3）。

### 11.2b ★★ `.schema` 里的 `{id}`：可能是数组下标，也可能是字典键

`{id}` 只是个占位符，**客户端到底要数组还是要字典，只能实测**：

| 集合 | `.schema` 写的是 | 客户端实际要 | 给错的后果 |
|---|---|---|---|
| `characters`（角色列表） | `{Array}` | **字典**（id → 角色） | `InvalidCastException: List\`1 → Dictionary\`2`（每轮上千次） |
| `chests` / `inventory` / `wallet` / `accountCurrencies` / `characterCurrencies` | `{Array}` | **数组** ✓ | 给字典 ⇒ `PersistentObject → PersistentArray` |

⇒ ⇒ **每个 `{id}` 集合都得单独试一遍**，不能靠 schema 推断。
⇒ 排查手法（很有效）：把响应体的**根键**逐批删掉，每轮跑一次对照，
  看异常数怎么变 ⇒ 异常从 2478/轮降到 **2/轮** 就是定位到了大方向，再逐个字段收。

### 11.3 ★ A/B 对照实验必须**彻底杀掉进程**

`adb shell am force-stop <pkg>` + `am start` **有时只是把任务切到后台**，`am start` 会**恢复同一个 pid**
⇒ 上一轮的内存状态（已经被污染的持久化树）跟着进下一轮 ⇒ **对照实验作废**（本轮被骗了一轮）。

⇒ 正确做法：`am force-stop` → `killall -9 <pkg>` → **打印新旧 pid 确认变了**再继续
（`Library/Locus/tmp/blades/run_ca.sh` 现在就这么干）。

### 11.4 ★ SDK 登录结果「没人接」是这类游戏离线化的典型墙

《上古卷轴：刀锋》实测：
- Java 侧把登录结果发到 C#（`UnitySendMessage("GHomePostManager","LoginResultCallBack",json)`）⇒
  C# 的 `GHomePostManager.LoginResultCallBack` **确实收到了**（`TicketStr`/`UserStr` 都写了）；
- 但 **`GHomePostManager.LoginCallBack` 这个回调字段全二进制没有任何 C# 订阅者**
  （dump.cs 里只有字段声明）⇒ **登录结果没人接到「后端认证」上** ⇒
  客户端永远不会去 `POST /auth/gplus` ⇒ 后端会话建不起来 ⇒
  `session._loginType` 一直是 `Session.LoginType.None` ⇒ `IsAuthenticated()` 永远 false
  ⇒ 客户端一进「取存档」状态就判定「没存档」⇒ **连本地存档都不查**。
⇒ ⇒ **排查这类问题要先看「回调字段有没有订阅者」**（grep dump.cs 里的字段名出现次数：
   只有声明 ⇒ 没人订阅）。

⇒ ⇒ 另两条排查经验：
- **否定实验很重要**：预置 PlayerPrefs 的 EULA 键、强制 `IsAuthenticated()` 返回 true，
  **都完全没改变行为** ⇒ 说明卡点比它们更靠前 ⇒ 不要在错的方向上继续加补丁。
- **探针要打在「决策函数」上，而不是「结果函数」上**：
  `OnSaveDataFetched(saveGameFound, isAuthenticated, …)` 的两个 bool 是**硬编码的 0**
  （唯一调用点 `b__222_229` 里写死）⇒ 改它们没意义 ⇒ 要看的是**谁决定了这个分支**。

### 11.5 ★★ Unity TextAsset 的序列化布局（抽 `.schema`/`.conversion` 用）

- TextAsset 的 `m_Name` / `m_Script` 都是**按 4 字节对齐**的 length-prefixed 字符串：
  ```python
  def read_ustr(buf, o):
      ln = struct.unpack_from('<i', buf, o)[0]; o += 4
      s = buf[o:o+ln]; o += ln
      return s, (o + 3) & ~3          # ★ 对齐到 4
  ```
  ⚠ 不对齐就读出垃圾长度（本项目把 `session.conversion` 的长度读成了 109838336）。
- ⚠ **UnityPy 读不出这批资产的正文**（报 `string argument without an encoding`）⇒ 手工按上面的布局解析。
- ★ **只扫 `assets/bin/Data/sharedassets0.assets`**（35 个 TextAsset = 19 schema + 15 conversion）；
  扫 `assets/bin/Data/` 下的 2963 个 bundle 是白费时间（里面一个 TextAsset 都没有）。

### 11.6 ★★ 补丁挂钩点必须先「自证活性」，再谈逻辑

**教训**：连续六轮跑测里，洞 B（挂在 `MainThreadDispatcher::Update`）**一条日志都没打**，
而当时看的是 `run_ca.sh` 的汇总表（它列了 6 个不存在的 tag、又漏了真正在跑的 `TFI`）⇒ 白扫了很多轮。

**低成本活性度量**：给「自己的发射点」或「自己挂的函数」留一个 tag，然后数出现次数：
- 若声称「每帧发」⇒ 140 秒就该有**上千条**；实际只有几位数 ⇒ **挂点死无疑**。
- 命中统计**不要看脚本自带汇总**（tag 表会和探针器的 HOOKS 版脱节），一律：
  `grep -oE " I [A-Z]{3} +: " log.txt | sort | uniq -c`。

**为什么容易挂空**：Unity 侧 `Update` 是**实例方法** ⇒ `SingletonBehaviour<T>` 派生类若没被实例化
（懒加载单例，没人取过 `Instance`），Unity 永远不会调它。
⇒ **首选挂钩点 = 业务层里必定每帧跑的宿主**（本项目：`FlowManager::Update` 0x7FC978，状态机宿主），
而不是“看起来最独立”的基础设施单例。
⇒ 换挂点前先比序言字节：若与旧挂点逐字节相同（本项目都是 `E92D4C10 E28DB008`），
则 cave 复制/回跳逻辑完全不用改，只换一个常量 ⇒ **一次 build 就能验**。

**另：每轮产物必须另存**。本项目 `build/blades_c.apk` 被后续 build 覆盖，
把最后一个能复现目标行为（`TSF=1`）的构建弄丢了 ⇒ 跑测脚本末尾加 `cp … build/history/<轮次>.apk`。

### 11.7 ★★★ 在 x86 模拟器上改 ARM 原生码：三条硬规律（本项目血泪换来的）

《上古卷轴：刀锋》（armeabi-v7a）跑在 LDPlayer（x86 + ARM 转译层）上，改二进制时踩到：

1. **代码段只能 pc-relative 读**。`ldr rX,[pc,#imm]` 的池字由转译层自己解析（能读），
   而**任何计算地址读代码段**（`ldr r2,[r2,r3,lsl #2]` 之类）会真访存 ⇒ **SIGSEGV**
   （实测 `fault addr` 正好是代码段里那张表的首项）。
   ⇒ **常量表不要放代码段**；要么用 `mov`/`add` 立即数算出来，要么把表放到**数据段/堆**。
2. **间接数据可以读**：`ldr rX,[rX,#imm]`、`ldr rX,[rX,Rm]` 读**数据段或堆**都正常
   （游戏自己的静态块/holder 都在堆里，照抄它的寻址方式即可）。
3. **栈对齐**：AAPCS 要求 sp 8 字节对齐。`push {r0-r3,lr}`(20B) + `sub sp,#12` ⇒ **sp ≡ 4 (mod 8)** ⇒
   调托管方法可能踩 VFP 对齐；改成 `push {r0-r3,r4,lr}`（6 个 = 24B）⇒ 始终对齐。

### 11.8 ★★ 写原生补丁的三条纪律（每条都对应一次翻车）

1. **自检要断言「操作数」，不能只断言「助记符」**。
   本轮 `cmp r2,#0` 被编成 `cmp r0,#0`、`mov r0,r4` 被编成 `mov r4,r0` ——
   助记符一样、语义相反，**静默通过**。
   ⇒ 每条关键指令都写 `assert capstone_dis(word) == ('cmp', 'r2, #0')`（还要注意
   CMP 的 Rd 在 **bits[19:16]**、MOV 的 Rd 在 **bits[15:12]**）。
2. **分支的「语义」也要断言**，不只是目标地址。
   本轮把「越界就停」写成**无条件 `b`** ⇒ 补丁每帧都直接跳过、看起来「什么都没发生」。
3. **补丁跑没跑要能自证**：给「自己发射的东西」留一个计数 tag，数出现次数。
   本轮 6 轮白跑，就是因为只看了「汇总表」——而汇总表里列着 6 个**根本不存在**的 tag、漏掉了真正在跑的 5 个。

### 11.9 ★★ 静态对象的「身份反查法」（不知道对象是谁时）

Unity/IL2CPP 里一堆静态字段指针（触发器、状态对象…）都摆在同一个 holder 里，
**按偏移递增排列**，而对象地址与偏移成线性关系：

```
holder+0x00 → obj0        holder+0x04 → obj1        holder+0x08 → obj2 …
object(X) = holder[0] − 4X          （X = 静态字段偏移）
```
⇒ 只要有一个已知偏移做**校准点**（本项目用 `0xa4 = Authenticate`），
就能把运行时打印的**对象地址**换算成**具体字段/触发器名** ⇒ 不用再猜。
（tombstone 的 `memory near r0` 转储能把 holder 的内容整块打出来，校准只需一次。）

### 11.10 ★★★ IL2CPP（Unity 2019 / metadata v24）字面量解码：不用逆向 .so 也能读字符串常量

**metadata 头（`global-metadata.dat`）固定布局**：

| 位置 | 字段 | 内容 |
|---|---|---|
| vals[2]/[3] | `stringLiteralOffset/Size` | 字面量**索引表**（每项 8 字节 `{uint32 length; uint32 dataOffset}`）|
| vals[4]/[5] | `stringLiteralDataOffset/Size` | 字面量**文本段**（**没有 NUL 分隔**，字符串紧挨着存）|
| vals[6]/[7] | `stringOffset/Size` | **名字表**（有 NUL 分隔）|

⚠ **最容易踩的坑**：文本段与名字表长得很像（都是可打印 ASCII），
但**文本段里没有 NUL**、名字表里全是 NUL ⇒ 用「NUL 密度」区分，别认错段
（认错就会解出一大串拼接的乱码 ✓ 本项目第一次就踩了 ✓）。

⇒ 有了索引表就能**按关键词列出全部 15398 条字面量**（游戏里的常量名/文件名/URL 片段全在里面 ✓）。
⇒ 本项目用它定位到了存档文件名零件：`SaveDataLocalStorage.FILENAME_SAVE = "Save_"`、
`FILE_EXTENSION_JSON = ".json"`、`LocalData`、`FullSaveData.json` ✓。
⇒ 局限：`ldstr` 在 ARM32 上**不是** PC-relative 直接取字面量（经由 per-method 的 metadata 结构），
   所以「字面量 → 代码位置」的反查较难 ✓（只做成「文本 → 索引」这一半 ✓）。
工具：`Library/Locus/tmp/blades/find_literals.py` ✓。

### 11.11 ★★ 自签 HTTPS 后端：主机名不匹配 = 请求根本没发出

用自签证书做本地影子后端时，**客户端的 TLS 栈会校验主机名** ⇒ SAN 里没有该主机名 ⇒
握手直接失败（客户端只报 `Curl error 35: Handshake failed`，**请求永远到不了你的 handler**，
后端日志里一片干净 ⇒ 极易误判成“客户端没发请求”✓ 本项目就误判了两轮 ✓）。

⇒ 两条对策：
1. **SAN 写通配符**（`*.jijiagames.com` 等），并用同一张 CA 重签（客户端信任的往往是那张 CA 的固定副本 ✓）；
2. ★ **在 TLS 层打 SNI 日志**（Python：`ctx.sni_callback = lambda s,n,c: print(n)`）⇒
   **握手失败的请求也能看见客户端连的是哪个域名** ✓ —— 这是排查该类问题最便宜的手段。

### 11.12 ★★★ 清理设备/模拟器上的游戏数据目录：**只能删自己写的路径**

本项目在 Android 模拟器（LDPlayer，root 可用）上跑改过二进制的 Unity 游戏，
用 `find <app files dir> -size +5M -delete` 清理自己摆的试验文件 ⇒
**误删了 1525 个 AssetBundle 与 `global-metadata.dat`**（游戏的大文件都在那儿 ✓）
⇒ 客户端直接报 `Unable to open archive file: …/Bundles/…` ⇒ 流程中断 ✗；
恢复办法：从 APK 重新打包推回（2963 个 bundle ≈ 1.07G，tar 传输 ≈ 50s ✓）。

⇒ **纪律**：
- 要删就只删**自己刚写的那些确切路径** ✓；绝不用通配/按大小删 ✓；
- 动手前先 `ls -la` 看清目录里有什么 ✓；
- 知道哪些资源**能从 APK 重建**（bundle、global-metadata.dat ✓）⇒ 删之前先确认可重建 ✓。

⇒ ★ 副产品（值得单独做）：让本地文件服务器**直接从 APK 流式提供任意 AssetBundle**
（用 `bundle_index.json` 做 hash→名字映射 ✓，因为客户端是按 md5 请求的 ✓）
⇒ 以后 bundle 缺失可以随时补 ✓，不必在设备上长期存 1GB ✓。

### 11.13 ★★ 字符串探针（`'str2'`/`'str3'`）会因参数类型不对而直接崩

探针把 `rX+12` 当 UTF-16 字符读 ⇒ **若该参数不是 Il2CppString，读到的就是垃圾指针** ⇒ SIGSEGV。
⇒ 实测：把 `Request.GetRequestJson` 的第三个参数当成字符串来读，r2 实际是个整型 ⇒ 崩在读堆上（fault addr 落在堆区）。
⇒ **纪律**：上字符串探针前，先从 `dump.cs` 确认该方法的参数类型与顺序 ✓
（例：`GetRequestJson(this, int32 type, string url)` ⇒ url 是 **r2**，不是 r1 ✓）。

### 11.14 ★★ 观察“客户端到底读了哪个文件”的两条便宜手段

1. **读 `/proc/<pid>/fd`**：根循环里 `ls -l /proc/$PID/fd` ⇒ 能看到它打开了哪些文件（但**打不开不存在的文件** ⇒ 只能证伪、不能证实 ✓）。
   ⚠ `dd if=/proc/<pid>/mem` 在 Android 上会被 SELinux 拦掉 ⇒ 读不了堆内容 ✓（别指望这条路）。
2. **探“构造请求”的那个方法**：探 `Request::GetRequestForm` / `GetRequestJson` ⇒ 命中就说明请求真的被构造了 ✓
   （本项目就是靠它才发现“请求入队了但没构造”✓）。
