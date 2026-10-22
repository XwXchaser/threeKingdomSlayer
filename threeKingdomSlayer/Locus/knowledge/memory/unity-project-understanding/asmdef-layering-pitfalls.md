---
id: kd_38b48a15-e3f0-4323-8bd9-b9ff18345f81
injectMode: inherit
injectAgents:
- '*'
aiEditMode: inherit
---

---
summary: Unity asmdef 分层的可复用做法与坑：.asmref 挂目录不动文件、嵌套 asmref 更深的赢、Editor 特殊目录规则在 asmdef 下失效、程序集名要先查是否被包占用、git mv 脚本必须连 .meta 一起搬、asmdef 程序集看不到 Assembly-CSharp 所以「编译即验收」。
injectMode: inherit
aiEditMode: inherit
---

## 为什么记这条

2026-10-23 把游戏侧从「无 asmdef」切成六层程序集（Data → Services → Combat → Flow → UI → EditorTools），
过程里的规则与坑都是**可复用的 Unity 知识**，下次再切层或新建程序集直接照抄。

## 做法（不动源文件）

- 新程序集建一个**只放 asmdef 的空目录**（如 `Assets/Scripts/Flow/Locus.Flow.asmdef`），
  要入层的目录各放一个 `Locus.Flow.asmref`（内容只有 `{"reference": "Locus.Flow"}`）。
  源文件一个不搬 → **GUID 全保留** → 场景 / prefab 的序列化引用不受影响。
- asmdef 的 `references` 就是**依赖边**：写错层（往上引用）直接编译失败 —— 这就是验收手段。

## 坑（逐条实测过）

1. **`.asmref` 嵌套时更深的赢**：`Player/Locus.Flow.asmref` 与 `Player/Combat/Locus.Combat.asmref`
   并存 → `Player/Combat/*` 仍在 `Locus.Combat`，`Player/InputManager*` 进 `Locus.Flow`。
   ⇒ 不用为了分层去搬文件。
2. **asmdef 放在名为 `Editor` 的目录里照样生效**：特殊目录规则让位给 asmdef
   （`Assets/Scripts/Editor/Locus.EditorTools.asmdef` → 14 个文件进了 editor-only 程序集）。
3. **反过来**：asmdef / asmref 覆盖的目录里，嵌套的 `Editor` 子目录**不再享受特殊规则**
   （证据：`Assets/Scripts/QTE/Editor/*` 在 `Locus.Combat` 里）——
   里面的脚本要用 `UnityEditor`，必须**移出去**或**单独给一个 editor-only asmdef**。
4. **程序集名要先查有没有被包占用**：`CompilationPipeline.GetAssemblies()` 能看到
   `Packages/` 里的程序集 —— 本项目 `Locus.Editor` 已被 Locus 工具包占用，游戏侧改叫 `Locus.EditorTools`。
5. **`git mv <file>.cs` 不会带走 `.meta`** → 新位置没 `.meta` → Unity 重生成 GUID → 引用全断。
   ⇒ 搬脚本要连 `.meta` 一起搬，搬完看 `git status` 里有没有 `.cs.meta` 的 R 行。
6. **asmdef 程序集看不到 `Assembly-CSharp`** ⇒ 切层前必须先用类型级依赖扫描找出越层引用
   （`Tools/asmdef-survey/dep_detail.py`），逐条改成端口。
7. **Unity 序列化不认接口类型的字段** ⇒ 端口只能走「装配点 + 实现类」；
   被场景序列化引用的界面组件（如 `FakeRouteRuntime.choicePanelPrefab`）不能换成接口，只能暂住下层。
8. **`.asmref` 覆盖的目录里若已有 asmdef，二者共存**（本项目 `Assets/Scripts/Combat/` 只有 asmdef，
   `Attack/ Core/ Enemy/ …` 靠 asmref 并入 `Locus.Combat`）—— 这是同一程序集的多目录写法。

## 验收口径

- 编译通过（越层引用会直接报错）；
- 程序集归属实测：`CompilationPipeline.GetAssemblies()` 列出每个文件属于哪个程序集；
- Play Mode 跑一遍既定的阵型基线（布局逐位一致）+ Console 0 error / 0 warn；
- 回归前后备份 / 还原 `PlayerPrefs` 存档。
