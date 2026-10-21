# asmdef 分层依赖实测工具

不依赖 Unity，纯文本扫描 `Assets/Scripts` 下的 C# 源码，产出程序集分层需要的依赖事实。
在**项目根目录**运行（路径是相对项目根的）。

| 脚本 | 作用 |
|---|---|
| `python Tools/asmdef-survey/dep_survey.py` | 目录级依赖矩阵 + 环 + 文件统计（第一张表） |
| `python Tools/asmdef-survey/dep_detail.py` | 跨目录依赖的「文件 → 被引用类型」明细（按类型数排序） |
| `python Tools/asmdef-survey/dep_detail.py Core Enemy` | 只看与指定目录相关的边 |
| `python Tools/asmdef-survey/dep_hub.py` | 被最多目录依赖的类型排名（全局枢纽）+ 各目录文件清单 |
| `python Tools/asmdef-survey/file_deps.py <文件...>` | 单个文件对外部类型的依赖（判断能否独立成程序集） |

判读要点：
- 类型级依赖用**词边界**匹配，已剔除注释与字符串字面量 → 会有少量假阳性（同名标识符），需人工确认。
- 「某文件能否单独成程序集」= 它的外部类型依赖是否全部落在**更低层**的程序集里。
- 方案与批次记录见 `Locus/knowledge/plan/asmdef-layering-plan-20261022.md`。
