# SkillWeaponsArtSource —— 技能武器美术素材源

**这里只放两类东西：造型参考、画风参考。**
**不要往这里放任何生成的概念图／正交视图板。**

## 目录

| 目录 | 内容 | 是否入库 |
|---|---|---|
| `01_Reference/pbz/` | PBZ《影之刃零》官方立绘（巨凶斧 / 追命锁镰）+ 斧头特写 + 来源台账 | ❌ 第三方素材，只留本地 |
| `01_Reference/style/` | 从本项目实机截图裁出的画风参考（色卡 / 木料铁箍 / 钢刃） | ✅ 自产，入库 |
| `04_Model/` | 3D 源文件（fbx / blend）与贴图（不入 Unity） | 由制作方归档 |

## 唯一需求源

`Locus/knowledge/design/weapon-3d-art-spec-axe-and-chain.md`
—— 交这份给制作方即可，不需要其它上下文。

## 约定

- 造型参考与画风参考**只保留上面这些**；不要再新增 AI 生成样本
  （本项目曾生成 9 版概念图 + 3 版视图板，全部判定不合格并已删除，
  见 `plan/weapon-concept-image-plan-20261114.md` §3）。
- 第三方参考图的来源页 URL 与 sha256 记进 `01_Reference/pbz/references.csv`。
