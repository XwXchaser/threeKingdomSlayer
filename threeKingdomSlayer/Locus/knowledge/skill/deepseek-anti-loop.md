---
id: kd_aeb295d0-6733-4c3b-97ac-18378ffb56c6
injectMode: inherit
aiMaintained: inherit
skillEnabled: true
skillSurface: command
commandTrigger: /anti-loop
---

# deepseek-anti-loop

## Summary
Deepseek 专用的工具调用防死循环规避策略：识别异常输出、切换读取工具、控制单次调用规模

## Content
# 工具调用防死循环规避策略（Deepseek 专用）

> 本 Skill 记录 Deepseek 模型在处理本 Unity 项目时反复出现的「工具输出异常 → 重复读取 → 疑似死循环」问题，以及必须遵守的规避策略。
> 适用场景：一切需要读取/修改代码、Unity 资产、场景、Prefab、Animator Controller 的工作。

## 一、识别「疑似死循环」的信号（遇到立即停止）

以下现象出现**任意一个**，必须立即停止当前读取/修复动作，不得继续用同方式重试：

1. **工具输出出现大段重复行**：例如成片 `}`、`m_TransitionDuration: 0`、`m_Name: XXX` 反复刷屏，且数量远超文件实际应含内容。
2. **`read` 或 `sed` 返回内容与文件语义不符**：单文件几千行 `}` 或整段内容被无限重复。
3. **同一文件同一工具连续多次调用都返回异常**：此时问题在读取路径而非文件本身。
4. **长链工具调用中某一步输出异常，仍继续下一步**：必须先在当前步停下。

**铁律**：出现上述信号 → `停止` → 复盘 → 换工具或换读取方式 → 确认输出正常后再继续。不要「再试一次同一个 sed」。

## 二、读取工具选型优先级

### Unity 资产（.asset / .prefab / .unity / .controller）

1. **`unity_yaml_read`**（首选）：语义化读取，能解析 PrefabInstance、序列化字段、Inspector 视图。
2. **`unity_execute` + Unity API**（次选，适用于状态机/组件遍历等 YAML 难读的结构）：
   - AnimatorController → `UnityEditor.Animations.AnimatorController` 遍历状态机/过渡/trigger。
   - Prefab 组件 → `AssetDatabase.LoadAssetAtPath` + `GetComponent`。
   - Scene 对象 → `SceneManager.GetActiveScene` + 遍历。
3. **`grep -n` 定位行号** → 只读小片段。

### 代码文件（.cs）

1. **`grep -n <pattern>` 拿精确行号**（先确认目标存在）。
2. **`sed -n 'a,bp'` 只读 20-40 行小片段**。
3. **`read` 限 limit**：不要默认读大段，先读函数所在行区间。
4. **`code_symbol_search` / `code_goto_definition` / `code_find_references`**：语义化定位，避免原始文本扫描。

### 禁止项

- **禁止用 `bash sed` 大段读取 Unity YAML 资产**（.controller/.prefab/.scene）：输出异常风险最高。
- **禁止用 `read` 读取 .asset/.prefab/.controller 原始文本**：工具会强制拦截并提示用 YAML 工具。
- **禁止在输出异常后同文件同工具重试**：换工具。

## 三、单次调用规模控制

- 单次 `read` / `sed` 默认控制在 **40 行以内**；确实需要上下文时递增，但仍分块。
- 单次 `bash` 命令尽量**单一职责**：一次 grep 一个模式，一次 sed 一个区间。
- 需要遍历状态机/组件时，优先 `unity_execute` 一次性打印关键字段，而不是多次 YAML 解析。
- 长任务用 `todowrite` 拆分，每步独立可验证。

## 四、排查 Unity 问题时的推荐路径

1. 先 `grep -n` 定位代码/资产行号。
2. 代码逻辑 → `read`/`sed` 小片段；资产结构 → `unity_yaml_read` 或 `unity_execute`。
3. 状态机/过渡/trigger → **`unity_execute` 用 Unity API 遍历 AnimatorController**（不走 YAML）。
4. 每步确认输出正常再推进；输出异常立即停。

## 五、用户沟通约定

- 若连续两次工具输出异常，**主动向用户说明**「读取路径异常，正在换用 X 方式」，而不是默默重试。
- 修复工作一旦出现疑似死循环，**先停**，向用户复盘成因与规避方案，征得同意再继续。
- 用户要求停止时，立即停止一切修复工具调用，只做复盘与方案说明。
