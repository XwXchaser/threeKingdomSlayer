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
