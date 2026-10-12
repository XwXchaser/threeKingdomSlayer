# Stab 长枪美术参考与三视图准备包

绝对目录：`H:\Project\threeKingdomSlayer\threeKingdomSlayer\StabWeaponArtSource\`

此目录位于 Unity 项目根目录、`Assets/` 之外，Unity 不导入。它保存既有生成素材的副本与后续制作草案。当前阶段：V01 六张三视图已生成并导出，最终用户验收没有在可用记录中确认；未找到已交付的长枪 3D 模型。

## 当前任务

刚性枪体V02已获用户验收，记录见 `04_BodyV02/user_acceptance.json`。红缨V02已在用户批准后单次生成，HTTP 200返回1536×1024 RGB PNG，完成技术校验并归档于 `05_TasselV02/stab_tassel_orthographic_v02.png`，待最终美术验收。

最新四视图交付位于 `12_AssembledFourViewV06/`：前／左／右／后四张384×1024单图、1536×1024无字合图、1856×1440中文标注版和trace。前／右／后三张逐字节沿用V5，新增左侧一次生成后整体等比对齐；左／右枪尖与枪尾高度差0px，左侧红缨展开和远侧遮挡待验收。V5五视图保留在 `11_AssembledFiveViewV05/`，旧分组件V04在 `09_FiveViewV04/`，V03失败资料在 `07_TopBottomV03_Failed/`。

## 查阅顺序

1. `01_Reference/assembly_reference.png`：当前下游选用的完整武器母图，1280×1280 RGB PNG。枪体和飘带的相对比例、正面挂点以此为依据。
2. `02_OrthographicV01/body/front.png`、`right.png`、`back.png`：枪体三视图，512×1024 RGB PNG，白底，无布料。
3. `02_OrthographicV01/ribbon/front.png`、`right.png`、`back.png`：独立红飘带三视图，规格同上。
4. `00_Index/source_manifest.json`：17 个来源文件的原始路径、SHA-256、尺寸和复制校验。
5. `00_Index/tassel_v02_specification.json`：红缨三视图合图规格与结果状态；`00_Index/body_v02_specification.json` 记录已验收枪体；`00_Index/five_view_v03_specification.json` 记录新增顶视／底视任务和说明板排版；`00_Index/assembly_v02_specification.json` 为完整装配历史草案。
6. `../Locus/knowledge/plan/stab-3d-three-view-production-handoff.md`：项目调查结论、历史版本、已知缺陷、中英文提示词及后续待办。

## 候选素材与追溯资料

枪体验收版位于 `04_BodyV02/`，红缨三视图候选位于 `05_TasselV02/`；最新带红缨前／左／右／后四视图在 `12_AssembledFourViewV06/`，此前完整五视图在 `11_AssembledFiveViewV05/`，分组件评审版在 `09_FiveViewV04/`。`06_FiveViewPreparation/` 是历史占位草稿。各输出目录的 `trace/` 和 `source_manifest.json` 仅供追溯。`02_OrthographicV01/` 为历史六张组件参考；`01_Reference/` 为完整造型参考；`03_TraceV01/` 包含历史源板和请求/校验。

两个组件分别放大展示，**不可因为都为512×1024就按同像素尺度装配**。完整母图负责组件相对比例。V01 枪刃折肩相对母图较明显；飘带三视图根/尾高度与局部折面有少量差异，侧背是二维参考上的补定。

历史 `approved_specification.json` 的 `prepared_for_user_approval_not_submitted` 是生成前状态；实际两次 HTTP 200、成图和导出记录证明 V01 已执行。历史 JSON 原样归档，其绝对输出路径仍指向原始 `C:\Users\Administrator\Pictures\gptGen\stab_orthographic_refs_v01\`，并未重写为本包路径。

母图 SHA-256：`6475df45a489976d0de95f042235d4080e1adf36c18c54f639dbd231ec46c9ae`。

枪体V02和红缨V02各提交一次生成；红缨原始输出在 `C:\Users\Administrator\Pictures\gptGen\stab_tassel_orthographic_v02\`，项目内合图及8份追溯文件归档于 `05_TasselV02/`，逐字节一致。红缨主轮廓完整，但正面右边距约6px、背面左边距约14px，未达到32px目标；记录如实保留。五视图V03和V04各两次生成，完整装配V05一次，V06左侧一次；均分别获批后提交。当前最新交付为前／左／右／后四视图，左侧待最终美术验收。所有旧版本保留，本包未自动提交或推送，没有建模、绑定或Unity部署。
