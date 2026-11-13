---
id: kd_b945a87c-de32-4b9a-abc3-075804e59308
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

# Blades 离线化：交接文档现状 + 本机工具缺口（2026-11-13 核实）

> 状态交接的权威文档在 `plan/blades-sync-refusal-handoff-20261112.md`（最新，含第五十三轮），
> 上级文档 `plan/blades-handoff-20261029.md`（开机读物），上一轮堵点复盘
> `plan/blades-sync-response-blocker-20261111.md`。
> 本文只补两件文档里没有的事：**最新进展摘要** 与 **本机（steam 主机）能不能直接接着干**。

## 1. 最新进展摘要（截至第五十三轮）

* 目标始终是：**客户端自己建角**（信号 `TSC` = `FlowState::StartCharacterCustomization`、
  `TQR` = `CreateCharacterRequest::Setup` 命中 ⇒ 客户端自己发 `POST /characters`）。
* 已解决的（都别再走一遍）：
  * **认证风暴**根因 = 会话刷新时间的两个输入都错（`Server-Timestamp` 不发、`tokenExpirationSeconds`
    当时长给）。服务端改成默认发头 + 给绝对 epoch 秒 ⇒ auth 从 349~600 次/轮降到 **1 次/轮**。
  * **空角色 id 路由 bug**（`/characters//…` 命中 `{id}` 模板或兜底 ⇒ 响应体形状错 ⇒ 每帧抛
    `KeyNotFoundException` ⇒ 响应队列卡死）⇒ 给空 id 单独配路由后异常清零。
  * **修法 J**（把 `AddRequestForCompletion` 的静默拒绝改成「用真正响应的那个请求」）
    ⇒ `/sync` 响应能被完成，`/sync → 角色列表 → OnSaveDataFetched` 这条链走通。
  * **FTUE 状态机已锤死**：`0xA72B28` 里那个接口方法就是 `FtueState.get_Status`，
    `cmp r0,#4` 即 `IsAuthenticated(4)` ⇒ ≥4 就 `Fire(ResetGame)`。
* 当前卡点：auth 只发 1 次、无异常、无 ResetGame，但流程在
  `FetchingSaveData → InitializingBundles → LoadingBaseGameContent → Authenticating → …`
  之间**循环**，**没进 FTUE（TutorialForest）、没建角**。
  * 二分结果：**R1/R2/R3 都不是「不进 FTUE」的元凶**（第五十一～五十三轮逐个关掉仍循环）。
  * 新线索：每轮循环都报 `The AssetBundle 'common' can't be loaded because another AssetBundle
    with the same files is already loaded`（次数≈循环轮数）⇒ **第一遍内容加载没走完**，
    流程回到取存档 → 再次 `StartLoadingContent` → 再次加载 'common' ⇒ 冲突 ⇒ 循环。
  * ⚠ **「第三十六轮进过 TutorialForest」不可信**：那版构建探针区溢出洞 C，hook 跳错洞，
    行为与干净构建不同 ⇒ 需用干净构建重做该配置拿到可信基线。
  * 下一步（文档给出的优先级）：① 可信基线（只留 FTUE=3，其余补丁全关）② 探
    `BundleDownloader` / `bundleDownloaderState` / `InitializingBundles` 的状态处理函数
    ③ 读懂 `0xA731D4` 附近的自然分支（决定 `SetFtue(4)` 还是 `SetFtue(3)`）。

## 2. ★ 本机（steam 主机）工具缺口 —— 结论：**不能直接接着跑**

2026-11-13 在 `C:\threeKingdomSlayer\threeKingdomSlayer` 实测：

| 交接文档假设的东西 | 本机现状 |
|---|---|
| 工作目录 `Library/Locus/tmp/blades/`（`build_vtrace.py` / `patch_c_fire.py` / `server.py` / `log_tail.txt` / `dumper_out_v7a/` / `build/libil2cpp.c.so`） | **目录不存在**（`Library` 被清过，只剩 `blades_ref`、`hud_analysis` 等参考图目录） |
| 目标 APK `C:/Users/Administrator/Downloads/shanggujuanzhoudaofeng.apk`（1.23 GB） | **本机没有**（C 盘唯一 apk 是本项目自己的 `threeKingdomSlayer.apk`） |
| 雷电模拟器 `ldconsole.exe`（LDPlayer） | **本机没装**（`C:/LDPlayer` 等均不存在） |
| 文档里的路径 `H:/Project/threeKingdomSlayer/...` | 那是**另一台主机**的盘符，本机是 `C:\threeKingdomSlayer\threeKingdomSlayer` |

⇒ 交接文档里的复跑命令（`python build_vtrace.py && python patch_c_fire.py` + `run_full.sh`）
**在本机跑不起来**。要继续这条线，三选一：

1. **换到原主机**（最省事）：原主机上 `Library/Locus/tmp/blades/` 与 APK 都还在，直接续跑第五十四轮。
2. **把产物搬过来**：从原主机拷 `Library/Locus/tmp/blades/`（不含 `cache/`）+ APK + 雷电模拟器安装包，
   路径按本机调整（注意文档里的 `H:/Project/...` 与 `export PYTHONPATH=...` 都要改）。
3. **从零重建**（最贵）：按 `memory/blades-reverse-engineering-assets.md` §3 的两条命令重跑
   `dump_metadata.py` / `dump_tables.py` 重建 257 张表与 dump（**`server.py` 需手写**，§5），
   再加 v7a 的 `dumper_out_v7a/` 与 `build/libil2cpp.c.so`，并重装模拟器、重做 TLS/hosts/reverse 接线。

⇒ **开工前先确认在哪个主机上做**，否则会白跑（文档已记：每轮都要核对设备 `.so` 与本地
`build/libil2cpp.c.so` 的 md5 一致，否则跑的是旧探针，历史因此作废过 3 轮）。
