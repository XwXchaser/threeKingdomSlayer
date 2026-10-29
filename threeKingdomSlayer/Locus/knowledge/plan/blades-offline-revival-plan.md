---
id: kd_b859bd71-18c7-4a0d-a65a-116b674dcd14
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# Blades（《上古卷轴：刀锋》）离线化 —— 影子后端实施计划

> **目标**：为已停服的手游《上古卷轴：刀锋》搭建一套**本地影子后端**，让客户端在无官方服务器的情况下自建本地会话继续游玩（私用、不分发）。
> **边界**：不剥离客户端内的授权/付费内容校验；不产出可分发的改包；抽取的美术/资产数据表仅作设计参考，不进本仓库、不进 Unity 项目。
> **状态**：2026-10-28。握手管道已跑通；卡在 `MetaManifest.json` 的内容 schema。
> 相关：`design/blades-architecture-benchmark.md`（架构借鉴）、`memory/blades-reverse-engineering-assets.md`（产物索引与复跑方法）。

---

## 0. 结论

| 阶段 | 状态 | 证据 |
|---|---|---|
| 静态测绘（类型/字段/字符串/数据表） | ✅ 完成 | 14498 类型 / 99958 方法 / 62948 字段 / 15399 字符串常量 / 257 张数据表 |
| APK 装模拟器并启动到标题画面 | ✅ 完成 | 截图 `boot_screen2.png` |
| 定位首个阻塞点 | ✅ 完成 | `Curl error 6: Could not resolve host: bladesasia.jijiagames.com` |
| 模拟器 root + 系统 CA 注入 | ✅ 完成 | `adbd is already running as root`；CA 已进 `/system/etc/security/cacerts` |
| 本地影子后端（TLS + 日志 + 路由） | ✅ 完成 | `server.py`；抓到真实请求 |
| 拦截引导文件 host_info | ✅ 完成 | `GET /china/Android/host_info_cn.json` 命中本地服务 |
| 拦截内容清单 MetaManifest | ✅ 完成 | `GET /Android/MetaManifest.json` 命中本地服务 |
| 反推 `host_info_cn.json` schema | ✅ 完成 | `EndpointsData` / `EndpointsUrlData` DTO |
| 反推 REST 契约（117 路径 + DTO） | ✅ 完成 | `rest_spec.md` / `rest_endpoints.csv` |
| **反推 `MetaManifest.json` schema** | 🔴 **进行中** | 试了 3 种形状均报 `0-10000-7` |
| 本地 REST 业务后端（角色/城镇/任务…） | ⬜ 未开始 | 契约已就绪 |
| 本地实时服（ENet + RMS） | ⬜ 未开始 | 协议已测绘 |

---

## 1. 客户端启动链（已完整还原）

```
① SDK 初始化（GHome/UC）
   api.mygm.sdo.com/v1/basic/handshake  → 成功（这条链还活着！）
② Unity 启动 → 标题画面
③ GET https://bladesasia.jijiagames.com/china/Android/host_info_cn.json
   ← EndpointsData：版本闸门 + 14 个后端端点
④ GET https://bladesasia.jijiagames.com/Android/MetaManifest.json
   ← BundleMetaManifest：版本 → 清单哈希 的映射
⑤ 载入本地清单（assets/Bundles/Manifest bundle 内的 AssetBundleManifest）
   + 本地 ManifestExtraData
⑥ ChooseBundlesToUse / FindDesiredBundleVersion
   → BundleCheckResult: UseBuiltInBundles | UpdateToLatestCompatibleBundles
     | NoCompatibleBundlesFound | BundlesNotReadyForClientVersion | BundleVersionCheckSucceeded
⑦ LoadRemoteManifest → LoadRemoteManifestExtraData
   （端点来自 BladeBundleUrlProvider：{base}/{platform}/Manifests|{platform}/Bundles）
⑧ 需要下载才走 BundleDownloader → ExtractWwiseBundles → EndBundleInitFlow
⑨ 进登录 → 连接 REST 后端（Authentication / Character / Town / …）
```

### 关键类与字段（metadata 实证）

| 类 | 关键字段/方法 |
|---|---|
| `BGS.Game.Bundles.BundleInitFlow` | `LoadLocalManifest` → `DownloadMetaManifest` → `ChooseBundlesToUse` → `FindDesiredBundleVersion` → `LoadRemoteManifest` → `LoadRemoteManifestExtraData` → `RunBundleDownloader` |
| `BGS.Game.Bundles.BladeBundleUrlProvider` | `BUNDLES_DIRECTORY_NAME` / `MANIFESTS_DIRECTORY_NAME` / `META_MANIFESTS_FILE_NAME` / `MANIFEST_EXTRA_DATA_SUFFIX` / `_baseUrl` / `_platformBaseUrl` / `_manifestsBaseUrl` / `_bundlesBaseUrl` |
| `BundleMetaManifest` | `_entries` / `get_Entries` / `GetLatestCompatibleBundleVersion` / `GetPreviousVersion` / `LatestBundleVersion` / `IsVersionAvailable` / `GetVersionManifestHash` / `Deserialize` |
| `BGS.Game.Bundles.BundleMessage` | `MetaManifestDownloadFailed` / `RemoteManifestDownloadFailed` / `BundleDownloadRequired` / `NeedToUpdateApp` / `BundlesNotReady` / `UnexpectedError` |
| `BGS.Shared.BuildSystem.Version` | `_majorVersion` `_minorVersion` `_patchVersion` `_changelistNumber` `_buildNumber`（`SECTION_SEPARATOR`/`SECTION_COUNT`=5 段） |
| `BGS.Shared.BuildSystem.BuildVersionAsset` | `_buildVersion` / `_endpointsType`（本地资产名 `BuildVersion`） |

### 端点表（`BGS.Shared.Rest.Api.BladeServer.Endpoints` 枚举，14 槽）

`Authentication` `Game` `Admin` `Bundles` `Matchmaking` `ClientLog` `Analytics` `Arena` `CreateAccount` `ForgotPassword` `ForgotUserName` `MaintenanceStatus` `Rms`（+`Count` `None`）

### 环境（`EndpointsType` 枚举，17 种）

`Local` `Development` `QualityAssurance` `Integration` `Staging` `Demo` `DemoQualityAssurance` `DevelopmentTokyo` `Production` `Loadtest` `ReleaseQA` `PreRelease` `PreReleaseQA` `Devtest` `DevBlue` `DevGreen` `DevYellow` `DevPurple`

---

## 2. 本地影子后端接线（已验证可跑）

```bash
# 1) 系统 CA（targetSdk 26 → 用户 CA 不可信，必须进系统库）
adb shell "mount -o rw,remount /system"
adb push ca.crt /data/local/tmp/ca.crt
adb shell "cp /data/local/tmp/ca.crt /system/etc/security/cacerts/<openssl subject_hash_old>.0"

# 2) 域名指向本机
adb shell "printf '127.0.0.1 bladesasia.jijiagames.com\n...' >> /system/etc/hosts"

# 3) 443 → 本机 8443
adb reverse tcp:443 tcp:8443

# 4) 起服务 + 启动游戏
python server.py --port 8443 --routes routes.json
bash probe.sh routes.json      # 一键：清数据→授权→启动→等请求静止→打印请求与 Unity 日志→截图
```

> ⚠ 三个坑：① `adb connect 127.0.0.1:5555` 会多挂一个设备，之后所有 adb 命令报 "more than one device"；② `adb reverse` 必须在**同一个 adb 守护进程**里设好再用（跨 bash 调用会丢）；③ `pm clear` 会清掉运行时权限授权，必须重新 `pm grant`，否则卡在权限弹窗。

### 实测到的客户端请求（目前只有 2 个）

```
GET /china/Android/host_info_cn.json      Host: bladesasia.jijiagames.com
GET /Android/MetaManifest.json             Host: bladesasia.jijiagames.com
User-Agent: UnityPlayer/2019.2.16f1 (UnityWebRequest/1.0, libcurl/7.52.0-DEV)
X-Unity-Version: 2019.2.16f1
```

### 已验证的响应行为（错误页 = 客户端自报的错误码 `0-<SERVICE_ID>-<index>`）

| 响应 | 客户端结果 |
|---|---|
| MetaManifest 404 / 空体 | 「更新包下载失败」`0-10000-1` |
| host_info 版本字段写 4 段 `"0.0.0.0"` | 「强制更新」（`Version.FromString` 要 **5 段**，解析失败 → `AppVersionTooLow`） |
| host_info 版本字段改 5 段 `"0.0.0.0.0"` | 错误页变为「未知错误」`0-10000-7` → **版本闸门已过** |
| MetaManifest `{}` / `{"_entries":[]}` / 多版本字典 / 93600 个版本键 | 均为 `0-10000-7` |

→ 错误码来源：`BundleInitController.StandardizedErrorCode { SERVICE_ID = 10000; _httpCode; _errorCode }`。
`BundleMessage` 静态字段顺序（决定语义）：`MetaManifestDownloadFailed`(0) `RemoteManifestDownloadFailed`(1) `BundleDownloadRequired`(2) `CarrierDataDownload`(3) `BundleDownloadFailed`(4) `NetworkError`(5) `OutOfSpace`(6) `NeedToUpdateApp`(7) `BundlesNotReady`(8) `UnexpectedError`(9) `DownloadingStatus` `CheckingForUpdateStatus` `ExtractingAudioBundle` `WarmingUpShaders`。

---

## 2.5 反汇编确认的硬证据（Il2CppDumper + capstone，2026-10-28）

> 工具：`Il2CppDumper v6.7.46`（net6）+ `capstone` + `lief`；产物 `dump.cs` / `script.json` / `stringliteral.json`。
> 辅助脚本：`disasm_il2cpp.py`（按 RVA 反汇编并用 dump.cs 标注调用名）、`disasm_lit.py`（解析字符串字面量引用）。

### A. `host_info_cn.json` 的精确结构

反序列化类 `EndpointsData`（**LitJson** `JsonMapper::ToObject`）：

```csharp
public class EndpointsData {
    public string AppValidMinVersion;      // "1.6.3.1.108" 形式：5 段点分整数
    public string LatestVersion;
    public List<string> PreverVersions;
    public List<string> ReviewVersions;
    public EndpointsUrlData Final;         // 正式环境
    public EndpointsUrlData Review;        // 审核环境
}
public class EndpointsUrlData {            // 16 个槽，全是 string（CheckServerStatus 是 int）
    Authentication, Game, Admin, Bundles, Matchmaking, Analytics, Arena,
    CreateAccount, ForgotPassword, ForgotUserName, MaintenanceStatus, Rms,
    PayAreanId, SSLPublicKey, CheckServerStatus, CheckServerDescription
}
```
- 配置地址是硬编码常量：`EndpointsHandling.EndpointsConfigUrl = "https://bladesasia.jijiagames.com/china/Android/host_info_cn.json"`。
- 失败类型 `EndpointErrorType`：`Success` / `AppVersionTooLow` / `FetchDataFailed`。

### B. `MetaManifest.json` 的精确结构

```csharp
public class BundleMetaManifest {
    private Dictionary<Version, BundleMetaManifest.Entry> _entries;
    public static bool Deserialize(string serializedManifest, Version buildVersion, out BundleMetaManifest m);
}
public class BundleMetaManifest.Entry {
    public const string MINIMUM_GAME_VERSION_ATTRIBUTE = "minGameVersion";
    public const string MANIFEST_HASH_ATTRIBUTE = "manifestHash";
    public const string ENABLED_ATTRIBUTE = "enabled";
    public Version MinimumGameVersion; public Hash128 ManifestHash;
    public bool Enabled; public bool Compatible;
}
```
**外层 key 就是 bundle 版本字符串（5 段）**，value 是这三个字段的对象：

```json
{ "1.6.3.1.108": { "minGameVersion": "0.0.0.0.0",
                   "manifestHash": "11111111111111111111111111111111",
                   "enabled": true } }
```

### C. 语义规则（反汇编逐条确认）

| 规则 | 证据 |
|---|---|
| `Version.FromString` 要求 **正好 5 段**点分整数 | `Split('.')` → `cmp w8, #5` → 每段 `Int32.TryParse` |
| `minGameVersion` 必须是 **5 段** | 同上；写 3/4 段 → `AppVersionTooLow` → 「强制更新」 |
| `Compatible = (buildVersion >= minGameVersion)` | `op_LessThan(buildVersion, minGameVersion)` 取反 |
| `manifestHash` 必须是 **32 位十六进制且非全零** | `Hash128::Parse` + `Hash128::IsValid`（三个 uint32 全 0 → 无效） |
| `enabled` 缺失 → Entry 为 null → 后续空引用异常 | `Dictionary.get_Item` 找不到键会抛 `KeyNotFoundException` |
| 顶层必须是 JSON 对象 | MiniJSON 结果需 cast 成 `Dictionary<string,object>`，否则抛异常 |
| 任何一条 entry 解析失败 → `Deserialize` 返回 false | `OnDownloadMetaManifestSuccess` → `SendMessage(BundleMessage.UnexpectedError)` |
| `FindDesiredBundleVersion` 返回 2/3 时 → 「强制更新」 | `result != 4` → `SendMessage(NeedToUpdateApp)` |
| `updateCondition`：0=BuiltInVersionDeprecated / 1=Always / 2=BundleVersionCheckOnly | `FindDesiredBundleVersion` 内按值分支 |

### D. 远端“清单”是什么

`LoadManifestFromBundle(AssetBundle)` → 远端清单是一个 **UnityFS bundle**（客户端用 `AssetBundle.LoadFromFile` 读，再取其 `AssetBundleManifest` 对象）。
→ 我们直接用 APK 自带的 `assets/Bundles/Manifest`（55901 字节，内含 1480 个 bundle 的 name/hash/依赖）回喂即可，理论上会判定“本地已齐”→ 不下载任何东西。

### E. `ChooseBundlesToUse` 的 5 路跳转表（`BundleCheckResult` switch）

| result | 枚举 | 行为 |
|---|---|---|
| 0 | `UpdateToLatestCompatibleBundles` | 比较 `LatestBundleVersion()==chosenVersion`：相等 → `LoadRemoteManifest`（**会发第 3 个 HTTP 请求**）；不等 → `NeedToUpdateApp`（强制更新） |
| 1 | `UseBuiltInBundles` | 直接 `OnBundlesReady`（不下载） |
| 2 | `NoCompatibleBundlesFound` | 错误提示 |
| 3 | `BundlesNotReadyForClientVersion` | `BundlesNotReady` 消息（**zh-CN 里没有该文案键**，会渲染成空/异常） |
| 4 | `BundleVersionCheckSucceeded` | `OnBundlesReady`（不下载） |

`BundleInitController.UpdateCondition`（编译期固定，决定走哪条）：
`0 = BuiltInVersionDeprecated`（→ result = `IsVersionAvailable(buildVersion)` ? 1 : 0）、`1 = Always`（→ result 0）、`2 = BundleVersionCheckOnly`（→ result 4）。

### F. 错误画面 ⇄ 消息对照（可用 PNG 字节数自动判别）

| 画面 | BundleMessage / 码 | PNG 字节数 |
|---|---|---|
| 「网络错误 / 更新包下载失败…(0-10000-1)」+[重试] | `Bundle.Message.NetworkError` | 2069666 |
| 「未知错误 / 发生未知错误，请重试。(0-10000-7)」+[重试] | `Bundle.Message.UnexpectedError` | 2235060 |
| 「网络错误 / 发生未知错误，请重试」+[可以] | **运行时拼装**（title=`NetworkError.Title`，desc=`UnexpectedError.Description`，label=`Ok`） | 2066418 |
| 「需要软件升级 / 请务必进行更新」+[打开商店] | `Bundle.Message.NeedToUpdateApp` | — |

### G. 矩阵实验结果（7 组，每组一次冷启动）

| host `AppValidMinVersion` | meta `minGameVersion` | `manifestHash` | meta key | 结果 |
|---|---|---|---|---|
| 4 段 | 5 段 | 有效 | 1.6.3.1.108 | **网络错误+可以**（新状态） |
| 5 段 | 5 段 | 有效 | 1.6.3.1.108 | **网络错误+可以** |
| 5 段 | 4 段 | 有效 | 1.6.3.1.108 | **网络错误+可以** |
| 5 段 | 5 段 | **全零** | 1.6.3.1.108 | 强制更新（Hash128 无效 → 条目作废） |
| 5 段 | 5 段 | 有效 | 999.999.999.999.999 | 网络错误+可以 |
| 1.6.3.1.108 | 5 段 | 有效 | 1.6.3.1.108 | 网络错误+可以 |
| 5 段 | 5 段 | 有效 | **0.0.0.0.0** | 未知错误 0-10000-7 |

⇒ 结论：`manifestHash` 必须是**非全零**的 32 位十六进制（否则条目作废→强制更新）；meta key 不能是全零版本；host 的版本字段段数不敏感（4/5 段都能解析）—— **此前的“必须 5 段”结论要修正**。

### H. 已排除的路径
- `BGS.Game.Settings.LocalSettings`（`..\local_settings.txt` + `GetInt/GetBool/SetInt/SetBool`）→ **dump.cs 里零调用点**，本构建未启用（死代码）。
- `AssetBundleManifestExtraData` 并非加密：文件头 `01 00 00 00` = `VERSION`（常量 VERSION=1），其后 `0x05C8=1480` 正好等于 bundle 数 → 自定义二进制格式。
- MiniJSON 解析我们的 body：**已用 Calvin Rien 解析器逐字节复现并验证成功**（4 个条目全部解析出来）→ H1 排除。
- host_info 的 `Bundles` 端点取值（根 / `/asia` / `/Android` / `/asia/Android` / `/Bundles` 5 种全试）→ 结果一致。
- AWS 区域 ping 主机不可达 → 14 个 `bgs-ping-*.s3.*.amazonaws.com` 全重定向后结果不变。
- `pm clear` 抹掉本地缓存 → 保留数据重跑结果一致（且 app 私有目录只有 47MB，**没有解压出任何 bundle**）。

### I. `BundleMessage` 静态字段表（二进制实证）

类型描述符位于 `0x396ED47`，其静态字段偏移：`0x38=NETWORK_ERROR_TITLE` `0x58=NETWORK_ERROR_DESCRIPTION` `0x78=UNEXPECTED_ERROR_TITLE` `0x80=UNEXPECTED_ERROR_DESCRIPTION` `0x88=RETRY_LABEL` `0x98=OK_LABEL`；
`BundleMessage` 实例偏移：`0xB0=MetaManifestDownloadFailed` `0xB8=RemoteManifestDownloadFailed` `0xD0=BundleDownloadFailed` `0xD8=NetworkError` `0xE0=OutOfSpace` `0xE8=NeedToUpdateApp` `0xF0=BundlesNotReady` `0xF8=UnexpectedError`。

### J. 失败是**纯本地**的（无网络参与）

连接跟踪（`/proc/net/tcp` 轮询 30 秒）：全程只有 `127.0.0.1:443`（= 我们影子后端的反向隧道）和 SDK 自己的公网连接；**没有任何新的出站连接**。
→ 说明客户端在 `OnBundlesReady → BundleManager.Setup` 这类**纯本地**步骤里失败，而不是某个 HTTP 请求失败。

### K. `updateCondition` 是**数据驱动**的，默认 = 2

`BundleInitController..ctor(bool enableBundleDownload, DownloadMask downloadMask, BundleUpdateCondition updateCondition)`，调用点在 `FlowState::cctor>b__222_102`（`0x189446C`）：
- 默认分支：`mov w23, #2` → **updateCondition = 2 = `BundleVersionCheckOnly`**，`enableBundleDownload = true`，`downloadMask = 1`；
- 另一分支：从一个列表里找到特定类型元素 → 用它的值。

⇒ 默认构建里客户端 **永远不会走 case 0 去下载远端清单**，而是直接进 `OnBundlesReady`，用**本地清单**装配 bundle。

### L. 失败点已定位到代码行：`_localManifest == null`

`BundleManager.Setup`（`0x198CB5C`）开头：
```
0x198cc28: ldr  x0, [x25, #0x28]     ; _localManifest
0x198cc2c: cbz  x0, #0x198d0f8       ; ← 本地清单为 null → 跳异常分支
0x198cc34: bl   AssetBundleManifest::GetAllAssetBundles
```
⇒ **失败原因 = 本地清单没加载成功**（不是任何 HTTP 请求失败）。

`LoadLocalManifest`（`0x1613D4C`）不是直接读 APK，而是从一个**静态引用**取已预载好的 Manifest bundle → 存在一个**启动期预载步骤**；那一步失败 → 静态为 null。

`AssetBundleInfo` 两种加载方式：
- `LoadType=0` → `LoadBundleFromFile`：`Path.Combine(localBundleDirectory, name)` → `AssetBundle.LoadFromFileAsync`（**Android 上读不了 APK 内 StreamingAssets**）；
- 其它 → `LoadBundleFromCache`：`UnityWebRequestAssetBundle.GetAssetBundle(url, crc)` → `SendWebRequest`（CRC 来自 `AssetBundleManifestExtraData`）。

本地 1480 个 bundle 位于 `assets/Bundles/`（StreamingAssets）。

### M2. 路径模型（完全逆出，二进制实证）

| 用途 | 路径 | 加载方式 |
|---|---|---|
| 本地 bundle 目录 | `Application.streamingAssetsPath` + `/Bundles` | —— |
| 本地清单 | `<上面>/Manifest` | `AssetBundle.LoadFromFileAsync(path)`（`BundleInitFlow.<LoadLocalManifest>b__0` @ `0x16171D8`） |
| 本地附加数据 | `<上面>/ManifestExtraData` | **`UnityWebRequest.Get(path)`**（`0x16141C8`）—— 不是 bundle |
| 附加数据目录 | `Application.persistentDataPath` + `/ManifestExtraData` | `BladeBundles.GetManifestExtraDataDirectory()` @ `0x1612434` |
| Wwise 解包目录 | `Application.persistentDataPath` + `/WwiseExtractedAssets` | `BladeBundles::cctor` |
| CDN 布局 | `{base}/{platform}/MetaManifest.json`、`/{platform}/Manifests/*`、`/{platform}/Bundles/*` | `BladeBundleUrlProvider` |

`BladeBundles::cctor`（`0x16124BC`）用 `Application::get_streamingAssetsPath`（`0x23EED08`）与 `Application::get_persistentDataPath`（`0x23EED3C`）在**运行时**拼出这两个目录 —— 所以最终路径在字符串常量表里查不到，只能从代码推。

### N. 关键异常证据（logcat）

```
E/Unity: KeyNotFoundException: The given key was not present in the dictionary.
  at System.Collections.Generic.Dictionary`2[TKey,TValue].get_Item (TKey key)
  at BundleMetaManifest+Entry.Deserialize (Dictionary`2[TKey,TValue] values, Version buildVersion, Entry& entry)
  at BundleMetaManifest.Deserialize (String serializedManifest, Version buildVersion, out metaManifest)
  at BGS.Game.Bundles.BundleInitFlow.OnDownloadMetaManifestSuccess (UnityWebRequest req)
```
⇒ **`Entry.Deserialize` 对我们下发的 JSON 做 `get_Item`，而那个字典里没有它要的键**。三个属性名（`minGameVersion` / `manifestHash` / `enabled`）已从二进制字面量槽实证无误（槽 `0x3AA2450` / `0x3AE16E8` / `0x3ABC138`）。
⇒ **待解：客户端实际传入的 `values` 字典不是我们给的那一层**。已排除的形状：裸 map（版本→entry）、顶层/内层双写、仅顶层、别名键、`{"Manifests":{...}}`、`{"entries":{...}}`、`_entries` 包裹。

### O. 本轮新建的逆向工具链（可长期复用）
- `Il2CppDumper v6.7.46` → `dump.cs`（完整 C# 头）/ `script.json` / `stringliteral.json`
- `il2cpp_index.py`：**一次性反汇编整个 `il2cpp` 段（26.7MB —— 注意代码段名不是 `.text`！）**，建两个索引：
  - `slot_refs`：全局字面量槽 → 引用它的代码位置（ADRP+[reg,#disp] 解析）
  - `calls`：调用目标 → 调用点
  - 用法：`python il2cpp_index.py slots "Manifest"` / `python il2cpp_index.py call 0x<rva>`
- 通用技巧：**字面量 → 全局槽 → 使用点**
- 踩坑：ADRP 目的寄存器在 bit[4:0]（不是 LDR 的 bit[9:5]）；`bl` 扫描要含 `b`；capstone/lief 建议 `pip install --target ./pylibs`（托管环境 site-packages 会被清理，导致偶发 ImportError）；Unity 的 `Application.streamingAssetsPath` 在 Android 上指向 APK 内，**读不了文件** → 这就是本地清单必然为 null 的原因（已把 1482 个 bundle 铺到 5 个候选目录，均无效）。

### Q. ★ 客户端真实 build 版本 = **1.6.3.1.105**（不是 versionName 的 1.6.3.1.108）

资产位置：`assets/bin/Data/f0fa725693c347447ae43dcca81b1e81`（386 个哈希资产文件之一）。
- 该文件里有一个 `BGS.Shared.BuildSystem.BuildVersionAsset` 实例（raw 68 字节）。
- 解析：m_Name="BuildVersion"（12 字节）之后是 5 个 int32 = **1 / 6 / 3 / 1 / 105** → `_buildVersion = 1.6.3.1.105`，末字段 `_endpointsType = 3`。
- 与 PlayerPrefs 里的 `lastVersion=1.6.3.1.105` 互相印证。

意义：
1. `host_info` 的版本闸门应以 **1.6.3.1.105** 为准（我们之前一直用 1.6.3.1.108）。
2. `MetaManifest` 的外层 key 必须是 **1.6.3.1.105**，`IsVersionAvailable(buildVersion)` 才会为真 → 走 `UseBuiltInBundles`（不需要下载远端清单）。
3. `minGameVersion` 必须 **≥** buildVersion 才算 Compatible。

### R. 为什么本地清单必然加载失败（Android 平台限制）

- 本地清单路径 = `Path.Combine(Application.streamingAssetsPath, "Bundles", "Manifest")`，加载方式 `AssetBundle.LoadFromFileAsync`。
- Android 上 `Application.streamingAssetsPath` 指向 **APK 内部**（`/data/app/<pkg>-1/apk/assets`），Unity 只把 IL2CPP 元数据解到 `/sdcard/Android/data/<pkg>/files/il2cpp/`，**没有把 StreamingAssets 解到可读目录**（已用 lsof / inotify / /proc/pid/maps 全部验证：客户端从未打开任何候选目录下的 bundle 文件，也没有 Unity bundle 缓存）。
- ⇒ `LoadFromFileAsync` 在 APK 内路径上必然失败 → `_localManifest == null` → `BundleManager.Setup` 走异常分支 → 任务框架捕获 → `BundleMessage.UnexpectedError`（即我们看到的「网络错误/发生未知错误」+[可以]）。
- ⇒ **官方形态必然是：先把 bundle 下载/解压到可写目录，再由 Unity 读文件**（这就是"更新包下载"流程存在的原因）。所以离线化的正确解法不是"把 bundle 放哪"，而是**让客户端走 UnityWebRequest/Unity 缓存路径**（`LoadBundleFromCache` 用 `UnityWebRequestAssetBundle.GetAssetBundle(url, crc)`，jar: URL 在 Android 上可直接读 APK 内的 bundle，且 CRC 来自 `AssetBundleManifestExtraData`）。

### P. 下一步的三个具体实验（按性价比排序）
1. **找出 `values` 字典的真实来源**：反汇编 `BundleMetaManifest.Deserialize`（`0x1681C98`）到 `0x1681E54` 的循环，确认 `x21` 取自枚举器的哪个字段、传给 `Entry.Deserialize` 的到底是内层还是外层字典。
2. **确定 Android 上 `streamingAssetsPath` 的真值**：可从运行中进程的内存里 grep 静态字段值（root 可读 `/proc/<pid>/mem`），拿到后把 bundle 铺到该目录。
3. **本地 REST 后端**（契约已完备）：先实现 `/asia/api/authentication/v1/public` 的账号/会话最小实现，让客户端过登录。

### M. 下一步的三个具体实验（按性价比排序）
1. **预置本地清单**：把 `assets/Bundles/Manifest`（56KB）与 `ManifestExtraData`（1.5MB）推到 app 可写目录的候选路径（`/data/user/0/<pkg>/files/bundles/`、`.../files/Bundles/`、`.../files/AssetBundles/`），看错误是否变化 —— 成本极低。
2. **把整个 `assets/Bundles` 解到可写目录**（1.12GB，`adb push` 几分钟）：若客户端走 `LoadType=0`，这正是官方形态。
3. **确认启动期预载 API**：搜 `bl` 到 `UnityWebRequestAssetBundle::GetAssetBundle` / `AssetBundle::LoadFromFileAsync` 的调用方，直接读出 URL 拼接逻辑。

---

## 3. 剩余阻塞点与下一步实验

### 阻塞点 A（当前唯一）：拿到 MetaManifest 后没有第 3 个请求，卡在「网络错误+可以」

**结论（已实证）**：
1. host_info 版本闸门 → **已过**；
2. MetaManifest → **已解析成功**，条目被接受（哈希非全零时）；
3. 客户端走的是 `ChooseBundlesToUse` 的 **case 1 / case 4**（`UseBuiltInBundles` / `BundleVersionCheckSucceeded`）分支 → 直接进 `OnBundlesReady`，**因此永远不会去下载远端清单**（`updateCondition` 默认 = 2 = `BundleVersionCheckOnly`，见 §2.5 K）→ 随后在**本地 bundle 装配**环节失败（见 §2.5 L）；
4. 失败发生在 `OnBundlesReady → BundleManager.Setup` 这段**纯本地**流程（无任何新出站连接）。

**所以卡点已从“网络”收敛为“本地资源装配”**。下一步应直接读 `BundleManager.Setup`（RVA `0x198CB5C`）与 `AssetBundleInfo.LoadBundleFromFile/LoadBundleFromCache` 的失败条件：本地 1480 个 bundle 在 Android 上位于 `assets/Bundles/`（StreamingAssets），`LoadFromFile` 不能直接读 APK 内的文件——**很可能需要把 bundle 从 APK 解到可读目录，或由客户端自行解压**。

**三个待验实验**：
1. `BundleManager.Setup` 里对 localManifest / extraData 的判空与异常点（反汇编）。
2. 把 `assets/Bundles/Manifest`（UnityFS bundle）改名为客户端期望的名字/路径放回 APK（会改动 APK，需先确认客户端到底从哪里读）。
3. 反查 `AssetBundleInfo._bundleDirectory` 的来源（是 `assets/Bundles` 还是缓存目录）。

**辅助信号**：客户端在 release 包里把该阶段 `Debug.Log` 剥掉了，logcat 无业务日志；错误页截图是唯一信号（用 PNG 字节数区分，见 §2.5 F）。

### 阻塞点 B：登录链

`api.mygm.sdo.com` 仍活着 → 九游/GHome 账号链可用（好消息）。
客户端拿到账号后要向**本游戏后端**换会话令牌（`BGS.Game.Rest.Services.AuthenticationService` → `/auth/bnet/login` 或 `/auth/gp` 等）。本地后端需实现这一步（返回 token + userId）。

### 阻塞点 C：实时联机

`Blade.RN` 自研 ENet 框架（可靠 UDP + DiffieHellman/Crypto 握手 + NetData 属性复制）；匹配走 RMS（WebSocket，`RmsMatchmakingEventPayload` 含 ticketId/address/port）。单人对局/城镇玩法可能不需要实时服；地牢/PVP 需要。
客户端内已带权威服务端逻辑（`BGS.Game.Network.Server.PvpServerGameLogic` / `ServerActor` / `ServerEncounter`）+ `PvpEnvironment.DirectServerIp/Port` 直连 + `FightDummy` 木桩 → 可作实现参照。

---

## 4. 里程碑

| # | 里程碑 | 验收口径 |
|---|---|---|
| M0 | 静态测绘（已完成） | 4 张 CSV + 257 张数据表 + 117 路径契约 |
| M1 | 影子后端可拦截（已完成） | 抓到 host_info 与 MetaManifest 两个请求 |
| M1.5 | 反汇编确认契约（已完成） | host_info / MetaManifest 结构、版本与哈希规则、错误码语义全部实证 |
| M2 | 过内容清单闸门（**已完成**） | 客户端已渲染真实游戏场景，越过更新闸门 |
| M2.5 | 绕过远端更新（**已完成**） | 强制 `UseBuiltInBundles`，直接用本地 1482 个 bundle |
| M3 | 本地账号 + 会话 | 登录界面能用本机账号创建角色并拿到 token |
| M4 | 进城镇 | 角色创建成功、城镇数据由本地 REST 返回 |
| M5 | 战斗与任务闭环 | 战斗结算、任务进度、技能/背包状态本地持久化 |
| M6 | （可选）本地实时服 | ENet 房间可进；PvP/地牢联机可用 |

## 5. 不做的事

- 不剥离/绕过客户端的授权、付费内容校验与反作弊。
- 不产出、不分发改包；影子后端仅本机自用。
- 不把抽取的美术/音频/资产数据表并入本仓库或本 Unity 项目。
- 不向已停服的官方后端发送任何伪造请求（域名已在本地 hosts 层面拦截）。

---

## 6. v6 突破：本地 bundle 直连（2026-10-28，本轮最大进展）

### 6.1 结论（已实证）

**客户端本体已能在模拟器上渲染真实游戏场景**（战场背景 + 角色 + 全部美术资源），仅剩「登录 / REST 会话」未闭环。
从「错误弹窗 → 标题+检查更新中 → 本地 extra data KeyNotFound → 真实游戏画面」共跨过 4 道闸门。

### 6.2 补丁清单（v7a 为设备实际架构；arm64 需同步）

| 位置 | 原指令 | 新指令 | 作用 |
|---|---|---|---|
| `0x10D9B68` | `mov r5, #2` | `mov r5, #1` | updateCondition = Always |
| `0x10D9BB0` | `eor r0, r5, #1` | `mov r0, #1` | 同上（强制走下载分支，见 §6.4） |
| `0xD91FC0` | `bl Application.get_streamingAssetsPath` | `bl Application.get_persistentDataPath` | **本地 bundle 目录改道** |
| `0xD94E1C` | `cmp r7, #4` | `mov r7, #1` | **强制 BundleCheckResult = UseBuiltInBundles** |
| `0xD94E20` | `bhi #0xD95104` | `b #0xD94E24` | 去掉 `>4` 默认分支，确保走跳转表 case 1 |

arm64 对应：`0x189441C` / `0x18943E4`（updateCondition）、`0x16124F8`（路径改道）；**arm64 还缺 case1 跳转表补丁**（待补）。
打补丁脚本：`build_and_test_v5.sh`（含自校验：改前反汇编必须与「原指令」一致，否则 assert 失败）。

### 6.3 实测启动链（改后）

```
host_info_cn.json  →  MetaManifest.json
   ↓
LoadLocalManifest        AssetBundle.LoadFromFileAsync(/storage/.../files/Bundles/Manifest)   ← 需预置
   ↓
LoadLocalExtraDataManifest UnityWebRequest.Get(绝对路径) → http://localhost:80/<path>        ← 需 80 端口有纯 HTTP 服务
   ↓
DownloadMetaManifest      → ChooseBundlesToUse → switch(base=0xD94E34)
                              case1 UseBuiltInBundles → _bundleVersion=_buildVersion → OnBundlesReady
   ↓
BundleManager.Setup(bundleVersion, localManifest, localBundleDirectory, remoteManifest, extraData)
   ↓
ExtractWwiseBundles → EndBundleInitFlow → 真实游戏场景渲染 ✅
   ↓
登录 / REST 阶段（当前卡点）
```

### 6.4 三个关键机制（均已实证）

1. **`persistentDataPath` 在 Android = 外部存储**：`/storage/emulated/0/Android/data/<pkg>/files`。
   所以 `LOCAL_BUNDLE_DIRECTORY` = 该路径 + `/Bundles`，bundle 必须预置到**外部存储**（推 `/data/user/0/...` 无效）。
2. **本地 extra data 走 `UnityWebRequest.Get(<无 scheme 绝对路径>)`**：Unity 把它解析成 `http://localhost:80/<path>`。
   - 不处理 → `Curl error 7: Failed to connect to localhost port 80`
   - 转到影子后端（TLS）→ `Curl error 52: Empty reply from server`
   - **必须另开一个纯 HTTP 服务器**（`file_server.py`，端口 8080）并 `adb reverse tcp:80 tcp:8080`。
   实际请求路径：`/storage/emulated/0/Android/data/<pkg>/files/Bundles/ManifestExtraData`（在 Bundles 目录内，不是 files 根下）。
3. **updateCondition / BundleCheckResult 判定表**（`FindDesiredBundleVersion` @ `0xD95160`）：

   | updateCondition | 含义 | 返回 | 后果 |
   |---|---|---|---|
   | 0 | BuiltInVersionDeprecated | 1 (UseBuiltInBundles) / 0 | 内置版本可用就直接用内置 |
   | 1 | Always | 0 (UpdateToLatest) | 总是去下载远端清单 |
   | 2 | BundleVersionCheckOnly | 4 (VersionCheckSucceeded) | 只校验版本 → OnBundlesReady |

   `ChooseBundlesToUse` 的 switch：case0→LoadRemoteManifest，**case1→`_bundleVersion=_buildVersion`→OnBundlesReady**，case2/3→错误，case4→OnBundlesReady。

### 6.5 设备侧接线（全部需要）

```bash
adb reverse tcp:443 tcp:8443   # 影子后端（TLS）
adb reverse tcp:80  tcp:8080   # 本地文件服务器（纯 HTTP，给 UnityWebRequest 用）
# /etc/hosts：6 个游戏域名 + 14 个 bgs-ping-*.s3.*.amazonaws.com → 127.0.0.1
# 模拟器系统 CA 库：7f7fd64e.0（targetSdk 26，用户 CA 不可信）
# 预置 bundle：adb push bundles_src/. /storage/emulated/0/Android/data/<pkg>/files/Bundles/
```

### 6.6 剩余阻塞点：登录闭环

- 客户端是**中国渠道包**，登录走 `cn.uc.gamesdk` / GHome / gundam SDK（Java 层），需要 SDK 侧账号；
  SDK 走真实公网（模拟器有网，`reportsk.sdo.com` 已实测可达）。
- 游戏自身 REST 域名已拦截。登录未完成时客户端在轮询
  `POST /api/game/v1/public/characters//towns/current/rewards/current`（**characterId 为空**），我们返回 404 → 客户端弹「通讯错误 / 重试」。
- 客户端 REST 请求头实测带签名：`Blade-Signature: Hash=SHA1, AccessKey=J28xQldwB6ssZX2Q, Timestamp=..., Signature=...`，
  另有 `Request-Index` / `Client-Version` / `GLD-Version` / `X-Unity-Version`。

### 6.7 下一步（三选一，按性价比）

1. **绕开 SDK 登录**：让游戏认为已登录（IL2CPP 侧补丁或直接构造本地会话/存档）→ 之后所有 REST 由本地后端供数。
2. **实现 SDK 登录闭环**：查 SDK 回调给游戏的 token 形态 + 游戏认证端点（`/asia/api/authentication/v1/public`）的请求/响应契约（`rest_spec.md` 的 Authentication 段）。
3. **直接实现最小 REST**：`characters`（建角）+ `towns/current` 等，让客户端拿到一个本地角色后自行推进。

> **判定**：先做 1（成本最低、收益最大），失败再转 2/3。

### 6.10 v6→v13：登录阶段的定位（2026-10-28 续）

**先修掉三个环境坑**（它们曾把「卡死」伪装成别的问题）：

| 坑 | 现象 | 真因 | 处置 |
|---|---|---|---|
| 系统权限弹窗 | 客户端停在「正在初始化...」，无任何请求 | 重装/重启模拟器后 `READ_PHONE_STATE` 等权限被重置，游戏等弹窗 | 启动前 `pm grant` 7 个权限；再兜底点一下「允许」(1318,608) |
| bundle 没推上 | `Unable to open archive file: .../Bundles/localization` → `CsvLocalizationDatabase.LoadDataFile()` NRE | 脚本用 `adb push` 到暂存目录再 `cp -a`，**`cp -a` 静默失败** | 直接 `adb push bundles_src/. <应用目录>`（外部存储） |
| adb / 后端跨调用失效 | `adb devices` 无输出或「more than one device」 | 工具会回收整棵进程树，adb server 与后端随调用结束而死；残留 `127.0.0.1:5555 offline` 条目导致裸命令歧义 | 一切在**同一次调用**内完成；固定 `-s emulator-5554`；先 `adb disconnect 127.0.0.1:5555`；`adb logcat` 必须用 `-t N` 限量（全量 `-d` 会拖死连接） |

**已确认的运行时事实**：

- 客户端从 bundle 阶段到**标题画面**全程正常（`host_info` → `MetaManifest` → 本地 extra data → 标题画面 ✓）。
- 标题画面**没有菜单按钮**：中国渠道的登录入口是 SDK 的悬浮窗（`cn.uc.gamesdk.floating_button`，logcat 可见 `spm=9game_sdk.floating_button.0.0`），
  而 SDK 服务已 nginx 404 ⇒ 悬浮窗登录入口永远不会出现 ⇒ **必须绕过 SDK 调游戏自身认证**。
- 客户端在未登录时也会后台轮询 `POST /api/game/v1/public/characters//towns/current/rewards/current`（characterId 为空）。

**探针技术（定位流程停在哪一步）**：

- 做法：把被探函数入口改成 `ldr pc,[pc,#0]` + 池字 = 唯一故障地址（`0xDEAD0000 + n*4`）→ 触发 SIGSEGV。
  **houdini 会把跳转目标按 4 字节对齐**（低位丢失），所以目标地址必须 4 字节对齐才能区分。
- 崩溃信息从 `logcat` 的 `Fatal signal 11 (SIGSEGV) ... fault addr 0x...` 读；tombstone 在 `/data/tombstones/`。
- **命中结果**：`fault addr 0xdead0014` ⇒ 第一个命中的是 **`FlowState::StartupLoadComplete`（0x8024F8）**
  ⇒ 流程顺序为：`StartupLoadComplete` → bundle 阶段 → … → 认证阶段（**认证探针全部未命中**）。
- 代价：每个探针包 = 重打包 1.2GB + 签名 + 安装 + 推 1.1GB ≈ 6 分钟，跑一轮 ≈ 3 分钟。
  ⚠ 探针异常若落在 TaskList 回调里会被**静默吞掉**（logcat 无痕），所以改用「崩溃地址」而不是「抛异常」。

**下一刀（明确）**：探针只保留 bundle 尾部与认证阶段（已构建为 `build_and_test_v14.sh`），
定位 `EndBundleInitFlow` 与 `StartAuthentication` 之间的断点；命中后再把该处改道到
`AuthenticationAnonymousLogin` / `AuthenticateNoUi` / `StartSoftLaunchAuthentication`（`0x801BC8`）等无 UI 路径。
同时后端已备好 `/auth/*`（返回 `Session/UserId/Token/TokenSchema/TokenExpirationSeconds/FeatureStatus/LinkedAccountsStatus`），
一旦客户端发出认证请求即可立刻拿到会话。

### 6.11 登录链的完整解剖（2026-10-28 续，最有价值的一节）

**探针结果**（fault addr → 函数）：

| fault addr | 函数 | 含义 |
|---|---|---|
| `0xdead0014`（v13） | `FlowState::StartupLoadComplete` `0x8024F8` | 流程早期就会经过（在 bundle 阶段前后） |
| `0xdead0004`（v14） | `BundleInitFlow::OnBundlesReady` `0xD94A04` | **bundle 阶段整段都能跑完** |

**⇒ 流程真相**：客户端**不是卡死**，而是正常跑到 **MainMenu（标题画面就是主菜单背景）后停下等玩家操作** ✓
——它需要玩家按「开始游戏」，而那个按钮**就是渠道 SDK 的悬浮窗**（`cn.uc.gamesdk.floating_button`，
logcat 实证 `spm=9game_sdk.floating_button.0.0`），SDK 服务已 404 ⇒ 按钮永远不出现 ⇒ 流程永远等下去 ✓✓。

**渠道登录是纯 Java 侧的**（C# 完全不参与）：

- `GHomeSendManager::Login(Action<int>)` `0xA7B5DC` = C# 触发 SDK 登录 ✓
- `GHomePostManager::LoginResultCallBack` `0xA796A0` → `InvokeCallback` → `LoginCallBack` 字段，
  但**该字段在整个二进制里无任何订阅者** ✓；`TicketStr`/`UserStr` 也**无人读取** ✓ ⇒ C# 不响应 SDK 登录结果 ✓
- ⇒ 绕过 SDK 只有两条路：**(a) 自己点火流程的匿名登录分支；(b) 自己造存档里的匿名会话**。

**点火机制**（已定位）：

- `FlowTrigger::Fire()` `0xA7381C` ✓（34 处调用点）
- `FlowTrigger::GetTriggerByName(string)` `0xA76C94` ✓（`AnimEventFlowTriggerSource::OnAnimEvent` 在用 ✓）
- 关键触发器：`AuthenticationAnonymousLogin`、`AuthenticationCreateAnonymousAccount`、
  `PlayTestStartNoUi`、`SoftLaunchStartNoUi`、`AuthenticationPlayGame`、`Bootstrap`、`Proceed`、`LoadingComplete` ✓

**存档机制**（离线单机路线的关键）：

- `SaveService` 的凭据键：`KEY_ANONYMOUS_USER_ID="rlskdfj"`、`KEY_DEVICE_ID="djfhsdf"`、
  `KEY_BNET_TOKEN="a6a5dfs"`、`KEY_USER_NAME="c986daf54"`、`ENCRYPTION_PASS_PHRASE="lkjdfjlskasa…"` ✓
- 存档属性：`IsAnonymousSignedIn` / `IsAcceptedToPlay` / `IsCreateAccountRequired` / `AnonymousSession` ✓
  ⇒ **匿名会话是存档驱动的**：存档里有 userid+token 就能无缝登录（不需要 SDK）✓
- 存储层 `SaveDataLocalStorage`：`FILENAME_SAVE="Save_"`、`FILENAME_SAVE_0`（= "Save_0"）、
  `DataFormat{Binary=0, Json=1, JsonFlatSchema=2}` ✓；**JSON 格式是明文**（存储层无加密 ✓），
  扩展名表 `FileExtensionByDataFormat` ✓ ⇒ 可以手写 JSON 存档 ✓（但默认格式是 Binary）
- `SaveManager::Load(bool isLoadingLocalFile, Action<SyncResult> onLoadDone)` `0xFFF2A0`；
  `SavedDataService.SyncResult{Success=0, BackendCommunicationFailed=1, CharacterNotCreated=2}` ✓

**已写好但尚未生效的补丁（v17）**：

```
0x10EA808  bl SaveManager::Load  ->  b  0x10EA810   （跳过取存档，直接走回调体）
0x10EA848  mov r1, #0           ->  mov r1, #1      （isAuthenticated = true）
0x10EA84C  mov r2, r4           ->  mov r2, #1      （isNoUiAuthentication = true）
```
（`FlowState::<.cctor>b__222_229` 调 `Load`，回调体 `0x10EA810` 里 `OnSaveDataFetched(0x801CE8)` 的实参在 r0/r1/r2 ✓）
效果待验证：需先让流程走到存档步（即先解决"没有开始游戏按钮"）✓。

**踩到的硬约束**：

- **字符串字面量地址在 global-metadata.dat，不在 libil2cpp.so**（`script.json` 的 ScriptString/StringLiteral 的
  `Address` 超出 .so 文件大小 ✓）⇒ **不能用池字直接构造字符串** ⇒ 点火必须拿到「元数据 usage 槽」的静态地址
  （槽地址在 .data 是静态的 ✓，但槽里的值是运行时填的 ✓，需要再 RE 一层）。
- `MainMenu::OnLogoPressed` `0x9DA004` = 「按 LOGO」入口 ✓；在 `OnOpen` 时机太早（委托未就绪 ✗），
  应在 `MainMenu::Update` `0x9DA24C` 里调用 ✓（待试）。
- PlayerPrefs 预置 `AndriodLoginStatus=1` 会被游戏自己覆盖 ✗（无效）。
- 改道 `StartAuthentication → StartSoftLaunchAuthentication` 无效 ✗（`StartAuthentication` 根本没被调用）。
- Wwise 抽取缓存可搬家到 `/sdcard/blades_wcache` 再还原 ✓ ⇒ 迭代省 ~4 分钟。

**下一刀（按顺序）**：

1. 在 `MainMenu::Update` 里调 `OnLogoPressed`（或直接调它内部转调的委托）⇒ 看流程是否进入认证阶段 ✓
2. 若进入 → 配合 v17 的存档绕行补丁 → 触发 `Authenticate()` → 客户端发出 `/auth/gplus` ✓✓
3. 若仍不进 → 用「元数据槽」方案给 `GetTriggerByName` 喂 `"AuthenticationAnonymousLogin"` ✓
4. 拿到会话后实现 `/characters`、`/towns/current` 等端点（`rest_spec.md` §4 有最小集）✓

### 6.8 登录阶段的实测结论（继续推进后追加）

- 客户端已跑进**正式标题画面**（完整 LOGO + 城堡 + 角色），美术资源全部正常加载。
- 「通讯错误」弹窗的来源已定位：**我们后端对 `/api/*` 一律 404**；给 `/api/*` 返回 200 `{}` 后弹窗消失、标题画面正常。
- **渠道 SDK 登录是死路**：`cn.uc.gamesdk`（Java 层）的登录服务已 **nginx 404 下线**；
  且 `GHomePostManager.LoginCallBack` 在整个 libil2cpp.so 里**没有任何订阅者**（渠道回调在本版是空挂的）。
  ⇒ 必须绕过 SDK，让游戏自己发认证请求。
- 认证链（RVA 均为 v7a）：
  `AuthenticationServiceActions::Authenticate` `0x11A1C14`
  → `AuthenticationServiceImplementation::Authenticate` `0x11A1C64`
  → `BladeServerManager::CreateAuthRequest` `0x15B1FA4`
  → `AuthRequest::Setup` `0x15ADE3C`（带 userId）/ `0x15AE328`（仅 deviceId）
  → `POST /auth/gplus?userId=&deviceId=&platform=`（响应 `AuthResponse`，含 `Session`）。
- 状态机里有 **`FlowState.AuthenticationAnonymousLogin`** 与 **`FlowTrigger.AuthenticateNoUi`**
  —— 匿名/无 UI 登录路径，是绕开 SDK 的最佳切入点。
- 响应格式：服务端返回 **JSON**，客户端用 `BGS.Game.Rest.JsonToPersistent` 转 Persistent 树，
  再由各端点的 `*ResponseParser` 解析 ⇒ JSON 字段名必须与 DTO 字段名一致（`rest_spec.md` §3/§4）。
- 客户端 REST 头带签名：`Blade-Signature: Hash=SHA1, AccessKey=J28xQldwB6ssZX2Q, Timestamp=<ms>, Signature=<sha1>`。

### 6.9 本轮新增工具（都在 `Library/Locus/tmp/blades/`）

| 脚本 | 作用 |
|---|---|
| `dis_v7a.py <rva> [size]` | ARM32 反汇编 + `bl` 目标自动标注 `类::方法名` + 字符串字面量解析 |
| `strs_in_func.py` | 只扫函数内字符串常量（穿透 metadata usage 链，最多 4 层） |
| `callsites_v7a.py <rva...>` | **扫描 ARM32 里调用了指定 RVA 的所有调用点**（找「谁调用了某函数」） |
| `iface_dispatch_v7a.py [slot]` | 扫描 IL2CPP **接口派发点**（`ldr r3,[r0,#0x58]` + `ldr r7,[r3,r6,lsl#3]` + `blx r7`），按 slot 分类 |
| `file_server.py` | 纯 HTTP 本地文件服务器（:8080），专供设备 80 端口 |
| `OFFLINE_README.md` | 完整复现步骤、补丁清单、三个坑、剩余工作 |
