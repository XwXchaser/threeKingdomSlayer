> **位置**：本目录就是它的家 —— `Locus/tools/svl-matting/`（**受版本控制、随仓库分发**）。目标主机只要 `git pull` 就能拿到，不需要改任何代码。
> 本机的工作副本在 `Library/Locus/tmp/svl_run/`（`Library/` 被 .gitignore 忽略，**不随仓库同步**）。

# Sprite Video Lab 抠图工作流脚本（v2，参数化）

一份**可以直接用到任何新主机**的抠图工作流：把 AI 生成的**纯色底**视频抽帧，抠成透明精灵并入库 Unity。
脚本全部**命令行参数化**，不需要改任何代码（只要设一个环境变量指向 checkout）。

## 内容

```
knowledge/
  svl-chroma-rim-fix.md                       ← 【主文档】从零部署与使用手册（先读这个）
  sprite-video-lab-matting-evaluation.md      ← 实测台账：失败模式、根因与量化依据
scripts/
  svl_common.py            公共库（键控/度量/出图；度量 helper 已内联，无外部依赖）
  svl_matte_batch.py       批量抠图（自动按底材选容差）+ 报告 + 总览图
  svl_scan_tolerance.py    容差扫描与推荐值（彩色幕布给默认；中性底算最优）
  svl_build_source.py      建新角色的素材源文件夹（重命名 + sha256 清单 + 拷已部署参照）
  svl_deploy_to_unity.py   入库 1108 画布精灵（**默认干跑**；打印偏移核对；替换自动备份）
  green_guarantee.py       单点去绿工具（不改 alpha，带备份与报告）
```

## 三步用起来

1. **装**：按主文档第 1 章在新主机部署 Sprite Video Lab（**服务端必须 Python 3.10**；ffmpeg 可选；**不要装 Real-ESRGAN**，该工序已弃用）。
   ```powershell
   set SPRITE_VIDEO_LAB_ROOT=<SVL_ROOT>       # 含 server.py 的 checkout 目录（任意盘，例 C:\sprite-video-lab）
   set SPRITE_VIDEO_LAB_WORK_DIR=<SVL_WORK>   # 工作目录（任意盘，例 C:\sprite-video-lab-work）
   ```
   `<SVL_ROOT>` 不设时脚本会**自动探测**（`E:\sprite-video-lab` → `C:\sprite-video-lab` → 当前目录及其上级）；建议还是显式设，报错更少。
2. **抠**（也可直接用网页版，见主文档第 2 章）：
   ```powershell
   $PY  = "<SVL_MODELS>\venv\Scripts\python.exe"   # 例 C:\sprite-video-lab-models\venv\Scripts\python.exe
   $RUN = "Locus\tools\svl-matting"
   $PY $RUN\svl_matte_batch.py --src "<ArtSource>\03_SelectedFrames"      # 全部动作
   $PY $RUN\svl_matte_batch.py --src "<某帧目录>" --label Attack --sheet   # 单个动作 + 总览图
   $PY $RUN\svl_scan_tolerance.py --src "<某动作目录>" --tolerances 55,70,79
   ```
3. **入库**：
   ```powershell
   python $RUN\svl_deploy_to_unity.py --src "<ArtSource>\03_SelectedFrames" `
       --dest "<工程>\Assets\Sprites\Enemy\EnemyXXX" `
       --name-template "Enemy_XXX_{Action}{i}.png"          # 先干跑，看“逐帧所需偏移及跨度”
   python $RUN\svl_deploy_to_unity.py ... --apply            # 确认后写入（替换自动备份到 Assets 之外）
   ```
   写完后按主文档 §5.3（新建精灵设导入设置）与 §5.4（Unity 验证）收尾。

## 配方（一句话）

`Chroma` + `自动取背景色` + **容差按底材标定**（彩色幕布 79；中性底 14，或用 6 + 中性幕布清理）+ `softness 16` + `halo 1` + **只勾「半透明像素转不透明」**。
**不要**：`softness = 0`、`先做平滑处理`、全源统一容差、旧的脚本后处理链（描边环替换/删源绿/中性化/硬绿保证——默认关闭）。

## 依赖

- 服务端：Python **3.10** + `Pillow`（3.13+ 不可用：`cgi` 被移除）。
- 脚本：`numpy` + `Pillow`（不在基础依赖里时 `pip install numpy pillow`）；脚本已内置 3.13+ 的 `cgi` 兼容处理。

## 已知限制（详见主文档第 6/7 章）

- 只适用于**纯色平底**；复杂底需要语义模型（BiRefNet / CorridorKey，含许可边界）。
- 产物**硬边**（半透明=0），边缘抗锯齿带被裁 1–3px → “被删主体”10–27% 是正常量级；残留 `gEx>8` 约每帧 1–2 千像素也是正常量级。
- 容差**必须按底材标定**，否则会把暗部角色吃掉（灰底用 79 → 实测被删 66%）。
- 量化口径**不跨轮比较**；**不建动画 clip、不改 controller**（入库后的精灵是孤立的）。
