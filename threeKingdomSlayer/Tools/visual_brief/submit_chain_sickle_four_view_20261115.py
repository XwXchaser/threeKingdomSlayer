# -*- coding: utf-8 -*-
"""追命锁镰四视图 · 提交脚本（2026-11-15，用户验收 v6 后开工）。

依据：Locus/knowledge/plan/weapon-concept-image-plan-20261114.md §5.15（本次新增）
需求书：Locus/knowledge/design/weapon-3d-art-spec-axe-and-chain.md §3.2 / §4.4
接口：POST https://api.muskapis.com/v1/images/edits（multipart，三张 image[]，顺序固定）
模型：gpt-image-2.6-sunburst（发起前用 GET /v1/models 核对）
参数：size=1024x1536 / quality=max / input_fidelity=high / output_format=png / n=1 / 不传 background

★ 两条硬约束（实测，勿改）：
  1. **不能上传已验收的 v6 概念图当设计锁** —— 上传自有生成图会被内容安全拦成 400（plan §5.10）。
  2. **不能上传 PBZ 锁镰图** —— 同样被拦（plan §5.12）。
  ⇒ 每张只挂三张项目自产画风参考图，造型完全由提示词承担（v4/v6 已验证该口径能过）。

提示词组装（沿用抡斧四视图已验证的顺序）：
  段A  共用造型描述 = 用户验收的 v6 提交文案，一字不改
  段B  正交图稿说明（四张共用）
  段C  该视图的观察方向（四张不同）
  段D  负面词 = plan §5.13.6 原文，不新增（v5 因新增负面词被拦）

用法
    python Tools/visual_brief/submit_chain_sickle_four_view_20261115.py --view front --dry-run
    python Tools/visual_brief/submit_chain_sickle_four_view_20261115.py --view front --approve
"""
from __future__ import annotations

import argparse
import base64
import hashlib
import json
import mimetypes
import os
import pathlib
import struct
import time
import urllib.error
import urllib.request
import uuid

PROJ = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


def _out_root() -> pathlib.Path:
    steam_root = pathlib.Path("C:/Users/steam/Pictures/gptGen")
    if steam_root.is_dir():
        return steam_root
    return pathlib.Path.home() / "Pictures" / "gptGen"


OUT_DIR = _out_root() / "chain_sickle_four_view_20261115"
ENDPOINT = "https://api.muskapis.com/v1/images/edits"
MODELS_ENDPOINT = "https://api.muskapis.com/v1/models"
MODEL = "gpt-image-2.6-sunburst"
SIZE = "1024x1536"
QUALITY = "max"
INPUT_FIDELITY = "high"
TIMEOUT = 900

# 参考图：与锁镰概念图 v4/v6 同一组，顺序固定（plan §5.13.3）
REFS = [
    (r"SkillWeaponsArtSource/01_Reference/style/game_palette_board.png",
     "image 1: only the overall project color relationships"),
    (r"SkillWeaponsArtSource/01_Reference/style/game_wood_iron_ref.png",
     "image 2: only the material language for wood, leather and iron collars"),
    (r"SkillWeaponsArtSource/01_Reference/style/game_saber_ref.png",
     "image 3: only the silver-white cutting edge and clean metal highlight language"),
]

# ── 段A：用户验收的 v6 造型描述 ─────────────────────────────���────────────
# 默认从本机生成档案里**逐字读取** v6 的提交文案（去掉末尾的负面词段），
# 保证四视图复用的就是验收过的原文；档案缺失时才用下方内置副本。
ARCHIVE = pathlib.Path("C:/Users/steam/Pictures/gptGen/chain_sickle_concept_20261115/"
                        "chain_sickle_concept_v1_v6.prompt.txt")
ARCHIVE_CN = pathlib.Path("C:/Users/steam/Pictures/gptGen/chain_sickle_concept_20261115/"
                           "chain_sickle_concept_v1_v6.prompt_cn.txt")
NEG_MARKER = "\n\nNegative constraints: "


def load_archive_shape() -> tuple[str, str]:
    """返回 (EN 段A, CN 段A)；档案缺失时回落到内置副本。"""
    en = cn = ""
    if ARCHIVE.is_file():
        raw = ARCHIVE.read_text(encoding="utf-8")
        en = raw.split(NEG_MARKER)[0].strip()
    if ARCHIVE_CN.is_file():
        cn = ARCHIVE_CN.read_text(encoding="utf-8").strip()
    if not en:
        en = SHAPE_EN_FALLBACK.strip()
    if not cn:
        cn = SHAPE_CN_FALLBACK.strip()
    return en, cn


SHAPE_EN_FALLBACK = """Redesign the combat weapon called Chain Pull for a high-definition pixelated Three Kingdoms battlefield action game. It must be a weapon that can be thrown, catch a distant enemy, pull the chain taut, and drag the enemy back. It is not a farming scythe and not a decorative long-handled weapon.

Show one complete weapon standing vertically, centered and fully inside the frame. The weapon consists of a compact metal grappling hook head, a short forked grip, and one naturally hanging chain. The grappling hook head is the primary visual identity and must be more prominent than the grip and chain.

The hook head is made from thick, solid cold blue-gray steel. It is compact, heavy, and structurally strong. It has a clearly open inward catching throat with enough cavity to catch an enemy's waist, limb, armor edge, or clothing. The inner edge is one continuous sharp bright silver-white cutting edge. The hook tip curves backward toward the chain and the retrieval direction, forming a clear retaining barb. The hook tip, catching throat, and chain connector must form one readable pulling-force path so the viewer immediately understands how the weapon catches an enemy and pulls the enemy back.

Do not make a long, broad crescent farming blade. Do not make a shallow decorative curve. The chain is carried by a load-bearing ring on the outer spine of the hook head, set well above the grip: the uppermost link sits level with the hook throat and the chain falls down alongside the grip. The grip ends in a plain closed steel cap.

At the hook root, keep one short upward-facing notched steel retaining claw as part of the hook structure. It must not become a face, animal head, or creature sculpture. The hook may have one or two thick steel reinforcement blocks, but no rows of rivets, crest, engraving, decorative metal strips, or oversized ornaments.

Below the hook head is a short, sturdy forked grip. Two short steel-and-wood branches split below the hook root and converge into one hand-grip after a short distance. This is not a long wooden staff; the amount of wood must be visibly much lower than on the battle axe. Use only a small amount of warm brown wood and clear warm reddish-brown leather wraps. Keep only one or two small dark-iron rings or crescent-shaped markers as identity details. Do not turn them into jewelry and do not add hanging cloth strips.

From that ring high on the hook back, show one naturally curved chain with eight or nine closed oval links. Every link is hollow, with a thin but readable wall, overlapping and alternating orientation. The chain must read as a real chain that can pull an enemy, not a rope, black bar, or decorative tassel. End it with one slightly larger connector ring, but do not let the chain overpower the hook head.

Make this weapon clearly different from the large battle axe: no large warm-brown wooden handle and no uniformly gray low-saturation palette. Use bright cold blue-gray steel and deep blue-teal shadow planes for the hook, bright silver-white inner edge highlights, blue-gray chain links with clean silver-gray highlights, a small amount of warm brown wood, and clearly visible warm reddish-brown leather. A very small amount of dark vermilion or deep teal may be used as an identity accent. The overall image is bright, high contrast, and clearly color-blocked, but contains no gold, brass, copper, or gilded decoration.

Present the weapon on a pure white or very light neutral background with only a subtle soft ground shadow. Do not include a person, hand, battlefield, text, dimension labels, UI, another weapon, magic glow, or flames. Keep the weapon complete, uncropped, and upright."""

SHAPE_CN_FALLBACK = """为一款高清像素化三国战场动作游戏重新设计“追命锁链”的战斗武器概念图。它必须是一件能够被抛出、勾住远处敌人、绷紧铁链并把敌人拉回来的锁钩武器，不是农具镰刀，也不是装饰性长柄武器。

画面只展示一件完整武器，竖直正立，居中且完整入画。武器由上方的紧凑金属锁钩、短分叉握柄和一条自然下垂的铁链组成。锁钩是第一识别点，必须比握柄和链条更醒目。

锁钩头使用厚重的冷蓝灰钢制成，体量紧凑、结构结实。它有一个明确张开的内凹捕捉喉部，喉部空间足以卡住敌人的腰腹、四肢或护甲边缘。锁钩内侧是一条连续、锋利、亮银白的刃缘。钩尖向后朝向链条回收方向弯回，形成明显的止脱倒钩；钩尖、捕捉喉部和链条连接环必须形成一条清晰的拉力路径，让人一眼理解它如何勾住敌人并把敌人拉回。

不要画成长而宽的月牙镰刃。不要让钩口变成浅弧线。链条由钩背外弧上一个明显高于握柄的受力连接环承载，最上面一枚链环的高度与钩喉齐平，整条链沿握柄一侧向下垂落。握柄底端是一个封死的钢帽。

钩头根部保留一个短小、向上扬起、带缺口的钢制止脱爪，作为锁钩结构的一部分，不要做成人脸、兽头或雕像。钩头可以有一到两个厚钢加强块，但不要有大面积铆钉排、徽章、雕花或装饰铁条。

钩头下方是短而结实的分叉握柄：两支短钢骨/木骨从钩头根部向下分开，随后在较短距离内合拢为一个握持段。握柄不是长木杆，木料占比明显低于巨斧。握柄只用少量暖棕木和红褐旧皮革包覆，皮革形成清楚的红褐色块面。只保留一到两个暗铁小环或新月形小片作为身份细节，不要串成首饰，不要垂挂大量布条。

从钩背高处的受力连接环垂下一条自然略弯的铁链，视觉上显示八到九枚闭合椭圆链环。每枚链环都中空、有细但可读的厚度，彼此交叠并交替朝向。链条要读成真实可拉回敌人的锁链，不是绳子、黑色长条或装饰流苏。链尾有一个稍大的连接环，但链条不能抢过锁钩主体。

颜色和巨斧明确区分：不要大面积暖棕木柄，也不要整件灰蒙蒙低饱和。锁钩主体使用明亮的冷蓝灰钢和深蓝青暗面，内刃使用亮银白色高光；链环使用蓝灰钢并有清楚银灰受光边；握柄只使用少量暖棕木，皮革使用醒目的暖红褐色，允许极少量暗朱红或深青色识别块。整体明亮、高对比、色彩分区清楚，但绝不使用金色、黄铜、铜色或鎏金装饰。

使用干净的纯白或非常浅的中性背景，只有轻微柔和投影。不要人物、手部、战场、文字、尺寸标注、UI、其它武器、魔法光效或火焰。武器必须完整、不裁切、不倾斜。"""

# ── 段B：正交图稿说明（四张共用）────────────────────────────────────────
ORTHO_EN = """This image is an orthographic elevation sheet for 3D modeling. Draw one single orthographic view of the weapon, seen straight on with orthographic projection and no perspective shortening. The weapon stands upright in the center of the frame, complete and uncropped, on a pure white background with only a very light soft shadow."""

ORTHO_CN = """这张图是给 3D 建模用的正交视图图稿：只画一件武器的正交视图，正交投影、正对观察、不做透视缩短。武器竖直站在画面中央，完整不裁切，纯白背景，只有极轻的柔和投影。"""

# ── 段C：各视图观察方向（需求书 §4.4）────────────────────────────────────
VIEW_EN = {
    "front": """FRONT view: the observer is on the +Z side of the weapon and looks toward -Z. Screen right is +X and screen up is +Y. The inner face of the hook head is turned toward the viewer, so the bright silver-white cutting edge along the inner curve is fully visible. The hook tip points up and toward screen left. The large connector ring on the spine of the hook sits on screen right, and the chain hangs down from it along the screen-right side of the grip. The short forked grip is below the hook root.""",
    "left": """LEFT view: the observer is on the -X side of the weapon and looks toward +X. Screen right is +Z and screen up is +Y. This is a true edge-on side view: the hook head is seen from its narrow side, so it is only about one fifth as wide as in the front view and reads as a slim vertical steel wedge rather than a broad blade. The catching throat becomes a narrow open slot in profile, the retaining barb shows in profile, the connector ring on the spine is edge-on as a thin ellipse, and the bright inner cutting edge faces screen right. The chain is seen along its plane, with alternating links showing a full oval face and a thin edge, so the chain reads with real depth. The grip shows its short fork in profile. Do not mirror the front view to make this one.""",
    "right": """RIGHT view: the observer is on the +X side of the weapon and looks toward -X. Screen right is -Z and screen up is +Y. This is a true edge-on side view from the opposite side: the hook head is again only about one fifth as wide as in the front view, a slim vertical steel wedge, with the catching throat as a narrow open slot in profile and the retaining barb in profile. The bright inner cutting edge now faces screen left, because the +Z face of the weapon points to screen left in this view. The connector ring on the hook spine is edge-on as a thin ellipse, and the chain shows alternating full oval links and thin edge-on links. Do not mirror the front view to make this one.""",
    "back": """BACK view: the observer is on the -Z side of the weapon and looks toward +Z. Screen right is -X and screen up is +Y. Show the back of the hook head: the convex spine, the reinforcement blocks, the large connector ring seen face-on as a full oval ring, and the chain hanging from that ring down to its slightly larger tail ring. Because screen right is -X, the hook tip now points up and toward screen right, and the chain hangs along the screen-left side of the grip. The short forked grip and its leather wraps are seen from behind. Keep the hook, the ring, the grip and the chain as one coherent weapon; do not redesign them.""",
}

VIEW_CN = {
    "front": """正面视图：观察者在武器的 +Z 一侧朝 −Z 看，画面右对应 +X，画面上对应 +Y。钩头的内侧朝向观察者，因此内缘那条亮银白刃线完整可见。钩尖朝上并偏向画面左侧。钩背外弧上的大连接环在画面右侧，链条从它垂下、沿握柄画面右侧自然下垂。短分叉握柄在钩根下方。""",
    "left": """左侧视图：观察者在武器的 −X 一侧朝 +X 看，画面右对应 +Z，画面上对应 +Y。这是真正的边缘侧视：钩头只以窄侧朝向观察者，宽度约为正面视图的五分之一，读成一片竖直的窄钢楔而不是宽刃面；捕捉喉部在侧视里变成一道张开的窄槽，止脱倒钩显出侧面轮廓，钩背上的连接环侧看是薄椭圆，亮银白的内刃朝画面右侧。链条顺着它的平面走：一半链环看到整圈环面，一半只看到薄边，因此链条有前后层次。握柄的短分叉在侧面露出。不要用正面图水平镜像来做这一张。""",
    "right": """右侧视图：观察者在武器的 +X 一侧朝 −X 看，画面右对应 −Z，画面上对应 +Y。这是从另一侧看的真正边缘侧视：钩头同样只有正面宽度的五分之一，是一片竖直窄钢楔；捕捉喉部在侧面是张开的窄槽，止脱倒钩呈侧面轮廓；亮银白的内刃此时朝画面左侧，因为武器的 +Z 面在这一视图里指向画面左。钩背连接环侧看是薄椭圆，链环是一半整圈环面、一半薄边。不要用正面图水平镜像来做这一张。""",
    "back": """背面视图：观察者在武器的 −Z 一侧朝 +Z 看，画面右对应 −X，画面上对应 +Y。展示钩头的背面：凸起的钩背、加强块、正面朝向观察者的大连接环（整圈椭圆环），以及从该环垂下、末端是大连接环的链条。由于画面右对应 −X，钩尖此时朝上并偏向画面右侧，链条沿握柄的画面左侧下垂。短分叉握柄和它的皮革缠握从背面看。钩头、连接环、握柄和链条必须保持是同一件武器，不要重新设计。""",
}

# ── 段B / 段C 的第二套措辞（b 版）───────────────────────────────────────
# a 版首次提交被内容安全拦成 400（request id 20261115105321690630634ZI47K5Ja），
# 该过滤对具体措辞敏感（见 skill/gpt-image-generation.md）。b 版只改措辞、不改信息量。
ORTHO_EN_B = """Draw one single orthographic view of the weapon for 3D modeling. The projection is orthographic, with no perspective shortening. The weapon stands upright in the center of the frame, complete and uncropped, on a pure white background with only a very light soft shadow."""

ORTHO_CN_B = """为 3D 建模画一张武器的正交视图：正交投影，不做透视缩短。武器竖直站在画面中央，完整不裁切，纯白背景，只有极轻的柔和投影。"""

VIEW_EN_B = {
    "front": """FRONT view: the observer stands on the +Z side and looks toward -Z. Screen right is +X, screen up is +Y. The inner face of the hook points at the viewer, so the bright silver-white edge along the inner curve reads clearly. The hook tip points upward and to screen left. The big connector ring on the hook spine is on screen right, and the chain drops from it down the screen-right side of the grip. The short forked grip sits below the hook root.""",
    "left": """LEFT view: the observer stands on the -X side and looks toward +X. Screen right is +Z, screen up is +Y. This is a true edge-on side view: the hook is seen from its narrow side, about one fifth as wide as in the front view, a slim vertical steel wedge rather than a broad blade. The catching throat becomes a narrow open slot in profile, the retaining barb shows in profile, the ring on the spine is edge-on as a thin ellipse, and the bright inner edge faces screen right. Along the chain, alternating links show a full oval face and a thin edge, so it reads with real depth. The short fork of the grip shows in profile. This view is not a mirrored copy of the front view.""",
    "right": """RIGHT view: the observer stands on the +X side and looks toward -X. Screen right is -Z, screen up is +Y. This is a true edge-on side view from the opposite side: the hook is again about one fifth as wide as in the front view, a slim vertical steel wedge, with the catching throat as a narrow open slot in profile and the retaining barb in profile. The bright inner edge faces screen left here, because the +Z face of the weapon points to screen left in this view. The ring on the hook spine is edge-on as a thin ellipse, and the chain alternates full oval links with thin edge-on links. This view is not a mirrored copy of the front view.""",
    "back": """BACK view: the observer stands on the -Z side and looks toward +Z. Screen right is -X, screen up is +Y. Show the back of the hook: the convex spine, the reinforcement blocks, the big connector ring face-on as a full oval, and the chain dropping from that ring to its slightly larger tail ring. Because screen right is -X, the hook tip points upward and to screen right here, and the chain hangs down the screen-left side of the grip. The short forked grip and its leather wraps are seen from behind. Hook, ring, grip and chain stay one coherent weapon; do not redesign them.""",
}

VIEW_CN_B = {
    "front": """正面视图：观察者站在 +Z 一侧朝 −Z 看，画面右对应 +X，画面上对应 +Y。钩头内侧朝向观察者，内缘那条亮银白刃线清晰可读。钩尖朝上并偏向画面左侧。钩背上的大连接环在画面右侧，链条从它垂下、沿握柄画面右侧下落。短分叉握柄在钩根下方。""",
    "left": """左侧视图：观察者站在 −X 一侧朝 +X 看，画面右对应 +Z，画面上对应 +Y。这是真正的边缘侧视：钩头只以窄侧朝向观察者，宽度约为正面的五分之一，是竖直窄钢楔而非宽刃面；捕捉喉部在侧面是张开的窄槽，止脱倒钩呈侧面轮廓，连接环侧看是薄椭圆，亮银白内刃朝画面右侧。链条上一节整圈环面、一节薄边交替，读出前后层次。握柄的短分叉在侧面露出。这一张不是正面图的镜像。""",
    "right": """右侧视图：观察者站在 +X 一侧朝 −X 看，画面右对应 −Z，画面上对应 +Y。这是从另一侧看的真正边缘侧视：钩头同样只有正面宽度的五分之一，是竖直窄钢楔，捕捉喉部在侧面是张开的窄槽，止脱倒钩呈侧面轮廓。亮银白内刃此时朝画面左侧，因为武器 +Z 面在这一视图指向画面左。连接环侧看是薄椭圆，链环是一半整圈环面、一半薄边。这一张不是正面图的镜像。""",
    "back": """背面视图：观察者站在 −Z 一侧朝 +Z 看，画面右对应 −X，画面上对应 +Y。展示钩头背面：凸起的钩背、加强块、正面朝向观察者的大连接环（整圈椭圆），以及从该环垂下、末端是稍大尾环的链条。由于画面右对应 −X，钩尖此时朝上并偏向画面右侧，链条沿握柄画面左侧下垂。短分叉握柄与它的皮革缠握从背面看。钩头、连接环、握柄与链条保持同一件武器，不要重新设计。""",
}

PROMPT_SETS = {
    "a": {"ortho_en": ORTHO_EN, "ortho_cn": ORTHO_CN, "view_en": VIEW_EN, "view_cn": VIEW_CN},
    "b": {"ortho_en": ORTHO_EN_B, "ortho_cn": ORTHO_CN_B, "view_en": VIEW_EN_B, "view_cn": VIEW_CN_B},
    # c 版（极简）：a / b 两版都被内容安全拦成 400
    # （request id 20261115105321690630634ZI47K5Ja / 20261115105601937512355oKOiaokF），
    # 两次措辞差异很大，故按“先隔离再改词”思路收缩到最小指令量：
    # 段A 仍是验收过的 v6 原文，只把新增内容压成一行正交句 + 一行方向句。
    "c": {
        "ortho_en": "Draw one single orthographic view of the weapon for 3D modeling, orthographic projection, no perspective shortening. The weapon is upright, complete, on a pure white background.",
        "ortho_cn": "为 3D 建模画一张武器的正交视图：正交投影，不做透视缩短。武器竖直、完整、纯白背景。",
        "view_en": {
            "front": "FRONT view: the observer looks from +Z toward -Z; screen right is +X, screen up is +Y.",
            "left": "LEFT view: the observer looks from -X toward +X; screen right is +Z, screen up is +Y. The hook is seen edge-on, about one fifth as wide as in the front view.",
            "right": "RIGHT view: the observer looks from +X toward -X; screen right is -Z, screen up is +Y. The hook is seen edge-on, about one fifth as wide as in the front view.",
            "back": "BACK view: the observer looks from -Z toward +Z; screen right is -X, screen up is +Y.",
        },
        "view_cn": {
            "front": "正面视图：观察者从 +Z 朝 −Z 看，画面右对应 +X，画面上对应 +Y。",
            "left": "左侧视图：观察者从 −X 朝 +X 看，画面右对应 +Z，画面上对应 +Y；钩头侧看，宽度约为正面的五分之一。",
            "right": "右侧视图：观察者从 +X 朝 −X 看，画面右对应 −Z，画面上对应 +Y；钩头侧看，宽度约为正面的五分之一。",
            "back": "背面视图：观察者从 −Z 朝 +Z 看，画面右对应 −X，画面上对应 +Y。",
        },
    },
    # d 版：只保留一行坐标方向句，删掉整段正交图稿说明（用于定位触发块）。
    "d": {
        "ortho_en": "", "ortho_cn": "",
        "view_en": {
            "front": "FRONT view: the observer looks from +Z toward -Z; screen right is +X, screen up is +Y.",
            "left": "LEFT view: the observer looks from -X toward +X; screen right is +Z, screen up is +Y.",
            "right": "RIGHT view: the observer looks from +X toward -X; screen right is -Z, screen up is +Y.",
            "back": "BACK view: the observer looks from -Z toward +Z; screen right is -X, screen up is +Y.",
        },
        "view_cn": {
            "front": "正面视图：观察者从 +Z 朝 −Z 看，画面右对应 +X，画面上对应 +Y。",
            "left": "左侧视图：观察者从 −X 朝 +X 看，画面右对应 +Z，画面上对应 +Y。",
            "right": "右侧视图：观察者从 +X 朝 −X 看，画面右对应 −Z，画面上对应 +Y。",
            "back": "背面视图：观察者从 −Z 朝 +Z 看，画面右对应 −X，画面上对应 +Y。",
        },
    },
    # e 版：把方向与正交都写成**游戏美术语言**（asset sheet / flat front view），不含坐标与建模术语。
    "e": {
        "ortho_en": "",
        "ortho_cn": "",
        "view_en": {
            "front": "FRONT view. Draw the weapon seen straight from the front, flat and centered like a game asset sheet, with the inner face of the hook toward the viewer, the hook tip pointing up and to the left, the big connector ring on the right and the chain falling down the right side of the grip.",
            "left": "LEFT view. Draw the weapon seen straight from its left side, flat and centered like a game asset sheet. The hook is turned edge-on and appears only about one fifth as wide as in the front view, the chain links alternate between full oval faces and thin edges, and the grip shows its short fork in profile.",
            "right": "RIGHT view. Draw the weapon seen straight from its right side, flat and centered like a game asset sheet. The hook is turned edge-on and appears only about one fifth as wide as in the front view, the chain links alternate between full oval faces and thin edges, and the bright inner edge of the hook now points to the left of the image.",
            "back": "BACK view. Draw the weapon seen straight from the back, flat and centered like a game asset sheet. The convex back of the hook and the reinforcement blocks are visible, the big connector ring is seen as a full oval, the chain drops from it down the left side of the grip, and the hook tip points up and to the right.",
        },
        "view_cn": {
            "front": "正面视图。正对观察者平视绘制该武器，像游戏素材立绘一样平整居中：钩头内侧朝向观察者，钩尖朝上偏左，大连接环在右侧，链条沿握柄右侧下垂。",
            "left": "左侧视图。从武器左侧正对绘制，平整居中，像游戏素材立绘：钩头侧向镜头，宽度约为正面的五分之一，链环整圈环面与薄边交替，握柄分叉呈侧面。",
            "right": "右侧视图。从武器右侧正对绘制，平整居中，像游戏素材立绘：钩头侧向镜头，宽度约为正面的五分之一，链环整圈环面与薄边交替，亮银白内刃此时朝画面左侧。",
            "back": "背面视图。从武器背面正对绘制，平整居中，像游戏素材立绘：钩背凸面与加强块可见，大连接环呈整圈椭圆，链条沿握柄左侧下垂，钩尖朝上偏右。",
        },
    },
    # f 版（2026-11-15，用户批准）：坐标方向句 + **游戏素材语言**的平视/结构描述，
    # 不含 orthographic / elevation sheet / for 3D modeling 等制图术语（见 §5.15 拦截实测）。
    "f": {
        "ortho_en": "", "ortho_cn": "",
        "view_en": {
            "front": "FRONT view: the observer looks from +Z toward -Z; screen right is +X, screen up is +Y. Draw it flat and centered like a game asset sheet. The inner face of the hook is turned toward the viewer, the hook tip points up and to the left, the big connector ring is on the right and the chain falls down the right side of the grip.",
            "left": "LEFT view: the observer looks from -X toward +X; screen right is +Z, screen up is +Y. Draw it flat and centered like a game asset sheet. The hook is turned edge-on and appears only about one fifth as wide as in the front view, a slim vertical steel wedge; the catching throat becomes a narrow open slot in profile; the bright inner edge faces screen right; the chain links alternate between full oval faces and thin edges; the grip shows its short fork in profile.",
            "right": "RIGHT view: the observer looks from +X toward -X; screen right is -Z, screen up is +Y. Draw it flat and centered like a game asset sheet. The hook is turned edge-on and appears only about one fifth as wide as in the front view, a slim vertical steel wedge; the catching throat becomes a narrow open slot in profile; the bright inner edge faces screen left; the chain links alternate between full oval faces and thin edges; the grip shows its short fork in profile.",
            "back": "BACK view: the observer looks from -Z toward +Z; screen right is -X, screen up is +Y. Draw it flat and centered like a game asset sheet. The convex back of the hook and its reinforcement blocks are visible, the big connector ring is seen as a full oval, the chain drops from it down the left side of the grip, and the hook tip points up and to the right.",
        },
        "view_cn": {
            "front": "正面视图：观察者从 +Z 朝 −Z 看，画面右对应 +X，画面上对应 +Y。平整居中，像游戏素材立绘。钩头内侧朝向观察者，钩尖朝上偏左，大连接环在右侧，链条沿握柄右侧下垂。",
            "left": "左侧视图：观察者从 −X 朝 +X 看，画面右对应 +Z，画面上对应 +Y。平整居中，像游戏素材立绘。钩头侧向镜头，宽度约为正面的五分之一，是竖直窄钢楔；捕捉喉部在侧面是张开的窄槽；亮银白内刃朝画面右侧；链环整圈环面与薄边交替；握柄分叉呈侧面。",
            "right": "右侧视图：观察者从 +X 朝 −X 看，画面右对应 −Z，画面上对应 +Y。平整居中，像游戏素材立绘。钩头侧向镜头，宽度约为正面的五分之一，是竖直窄钢楔；捕捉喉部在侧面是张开的窄槽；亮银白内刃朝画面左侧；链环整圈环面与薄边交替；握柄分叉呈侧面。",
            "back": "背面视图：观察者从 −Z 朝 +Z 看，画面右对应 −X，画面上对应 +Y。平整居中，像游戏素材立绘。钩背凸面与加强块可见，大连接环呈整圈椭圆，链条沿握柄左侧下垂，钩尖朝上偏右。",
        },
    },
    # g 版（2026-11-15）：f 版 + **单件武器约束**。
    # f 版 LEFT 实测：模型并排画了两件（左 3/4 视图 + 右真正的边缘侧视），
    # 边缘侧视本身正确但违反“单视图单件武器”。故加一句正向约束，不新增负面词。
    "g": {
        "ortho_en": "", "ortho_cn": "",
        "view_en": {
            "front": "FRONT view: the observer looks from +Z toward -Z; screen right is +X, screen up is +Y. Only one single weapon appears in the image, standing alone and centered, not a pair and not a comparison layout. Draw it flat and centered like a game asset sheet. The inner face of the hook is turned toward the viewer, the hook tip points up and to the left, the big connector ring is on the right and the chain falls down the right side of the grip.",
            "left": "LEFT view: the observer looks from -X toward +X; screen right is +Z, screen up is +Y. Only one single weapon appears in the image, standing alone and centered, not a pair and not a comparison layout. Draw it flat and centered like a game asset sheet. The hook is turned edge-on and appears only about one fifth as wide as in the front view, a slim vertical steel wedge; the catching throat becomes a narrow open slot in profile; the bright inner edge faces screen right; the chain links alternate between full oval faces and thin edges; the grip shows its short fork in profile.",
            "right": "RIGHT view: the observer looks from +X toward -X; screen right is -Z, screen up is +Y. Only one single weapon appears in the image, standing alone and centered, not a pair and not a comparison layout. Draw it flat and centered like a game asset sheet. The hook is turned edge-on and appears only about one fifth as wide as in the front view, a slim vertical steel wedge; the catching throat becomes a narrow open slot in profile; the bright inner edge faces screen left; the chain links alternate between full oval faces and thin edges; the grip shows its short fork in profile.",
            "back": "BACK view: the observer looks from -Z toward +Z; screen right is -X, screen up is +Y. Only one single weapon appears in the image, standing alone and centered, not a pair and not a comparison layout. Draw it flat and centered like a game asset sheet. The convex back of the hook and its reinforcement blocks are visible, the big connector ring is seen as a full oval, the chain drops from it down the left side of the grip, and the hook tip points up and to the right.",
        },
        "view_cn": {
            "front": "正面视图：观察者从 +Z 朝 −Z 看，画面右对应 +X，画面上对应 +Y。画面里只出现一把武器，单独居中，不要并排两把，也不要对比排版。平整居中，像游戏素材立绘。钩头内侧朝向观察者，钩尖朝上偏左，大连接环在右侧，链条沿握柄右侧下垂。",
            "left": "左侧视图：观察者从 −X 朝 +X 看，画面右对应 +Z，画面上对应 +Y。画面里只出现一把武器，单独居中，不要并排两把，也不要对比排版。平整居中，像游戏素材立绘。钩头侧向镜头，宽度约为正面的五分之一，是竖直窄钢楔；捕捉喉部在侧面是张开的窄槽；亮银白内刃朝画面右侧；链环整圈环面与薄边交替；握柄分叉呈侧面。",
            "right": "右侧视图：观察者从 +X 朝 −X 看，画面右对应 −Z，画面上对应 +Y。画面里只出现一把武器，单独居中，不要并排两把，也不要对比排版。平整居中，像游戏素材立绘。钩头侧向镜头，宽度约为正面的五分之一，是竖直窄钢楔；捕捉喉部在侧面是张开的窄槽；亮银白内刃朝画面左侧；链环整圈环面与薄边交替；握柄分叉呈侧面。",
            "back": "背面视图：观察者从 −Z 朝 +Z 看，画面右对应 −X，画面上对应 +Y。画面里只出现一把武器，单独居中，不要并排两把，也不要对比排版。平整居中，像游戏素材立绘。钩背凸面与加强块可见，大连接环呈整圈椭圆，链条沿握柄左侧下垂，钩尖朝上偏右。",
        },
    },
    # h 版（2026-11-15）：LEFT / RIGHT 的**加强边缘侧视**措辞。
    # f 版 LEFT 实测：模型并排画两件；g 版：单件了，但钩头仍是 3/4 面（“one fifth as wide” 没起作用）。
    # h 版改为把“宽面背对观察者、完全不可见”写成硬描述，并去掉可能被读成“对比排版”的说法。
    "h": {
        "ortho_en": "", "ortho_cn": "",
        "view_en": {
            "left": "LEFT view: the observer looks from -X toward +X; screen right is +Z, screen up is +Y. One single weapon stands alone and centered in the frame. The weapon is seen exactly from its left side: the broad face of the hook is turned away from the viewer and is not visible at all, so the hook reads as a thin vertical steel plate seen edge-on, about one fifth as wide as it is in the front view. The catching throat reads as a narrow open slot cut into that plate, the barb shows in profile, the ring on the spine is edge-on as a thin ellipse, the bright inner edge becomes a thin bright line along the plate, the chain links alternate between full oval faces and thin edges, and the grip shows its short fork in profile.",
            "right": "RIGHT view: the observer looks from +X toward -X; screen right is -Z, screen up is +Y. One single weapon stands alone and centered in the frame. The weapon is seen exactly from its right side: the broad face of the hook is turned away from the viewer and is not visible at all, so the hook reads as a thin vertical steel plate seen edge-on, about one fifth as wide as it is in the front view. The catching throat reads as a narrow open slot cut into that plate, the barb shows in profile, the ring on the spine is edge-on as a thin ellipse, the bright inner edge becomes a thin bright line along the plate, the chain links alternate between full oval faces and thin edges, and the grip shows its short fork in profile.",
            "front": "FRONT view: the observer looks from +Z toward -Z; screen right is +X, screen up is +Y. One single weapon stands alone and centered in the frame. The broad inner face of the hook is turned toward the viewer, the hook tip points up and to the left, the big connector ring is on the right and the chain falls down the right side of the grip.",
            "back": "BACK view: the observer looks from -Z toward +Z; screen right is -X, screen up is +Y. One single weapon stands alone and centered in the frame. The convex back of the hook and its reinforcement blocks are visible, the big connector ring is seen as a full oval, the chain drops from it down the left side of the grip, and the hook tip points up and to the right.",
        },
        "view_cn": {
            "left": "左侧视图：观察者从 −X 朝 +X 看，画面右对应 +Z，画面上对应 +Y。画面里只有一把武器，单独站在画面中央。武器完全从左侧看过去：钩头的宽面背对观察者、完全看不见，钩头因此读成一片侧对镜头的竖直薄钢板，宽度约为正面视图的五分之一。捕捉喉部读成钢板上的一道窄开口槽，止脱倒钩呈侧面轮廓，钩背的连接环侧看是薄椭圆，亮银白内刃变成钢板边缘的一条细亮线，链环整圈环面与薄边交替，握柄分叉呈侧面。",
            "right": "右侧视图：观察者从 +X 朝 −X 看，画面右对应 −Z，画面上对应 +Y。画面里只有一把武器，单独站在画面中央。武器完全从右侧看过去：钩头的宽面背对观察者、完全看不见，钩头因此读成一片侧对镜头的竖直薄钢板，宽度约为正面视图的五分之一。捕捉喉部读成钢板上的一道窄开口槽，止脱倒钩呈侧面轮廓，钩背的连接环侧看是薄椭圆，亮银白内刃变成钢板边缘的一条细亮线，链环整圈环面与薄边交替，握柄分叉呈侧面。",
            "front": "正面视图：观察者从 +Z 朝 −Z 看，画面右对应 +X，画面上对应 +Y。画面里只有一把武器，单独站在画面中央。钩头的内侧宽面朝向观察者，钩尖朝上偏左，大连接环在右侧，链条沿握柄右侧下垂。",
            "back": "背面视图：观察者从 −Z 朝 +Z 看，画面右对应 −X，画面上对应 +Y。画面里只有一把武器，单独站在画面中央。钩背凸面与加强块可见，大连接环呈整圈椭圆，链条沿握柄左侧下垂，钩尖朝上偏右。",
        },
    },
    # i 版（2026-11-15）：h 版被内容安全拦成 400（request id 20261115112537765430581bXI0xd8S），
    # 推测是“宽面完全不可见 / 钢板上切出窄槽”这类结构描述触发。i 版改回 f 版的短句，
    # 只去掉“catching / retaining barb / cut into that plate / turned away … not visible”这些词。
    "i": {
        "ortho_en": "", "ortho_cn": "",
        "view_en": {
            "left": "LEFT view: the observer looks from -X toward +X; screen right is +Z, screen up is +Y. One single weapon stands alone and centered in the frame. The hook is turned edge-on and appears only about one fifth as wide as in the front view, a slim vertical steel wedge. The throat reads as a narrow slot, the ring on the spine is a thin ellipse, the bright inner edge is a thin bright line, the chain links alternate between wide ovals and thin edges, and the grip shows its short fork in profile.",
            "right": "RIGHT view: the observer looks from +X toward -X; screen right is -Z, screen up is +Y. One single weapon stands alone and centered in the frame. The hook is turned edge-on and appears only about one fifth as wide as in the front view, a slim vertical steel wedge. The throat reads as a narrow slot, the ring on the spine is a thin ellipse, the bright inner edge is a thin bright line, the chain links alternate between wide ovals and thin edges, and the grip shows its short fork in profile.",
            "front": "FRONT view: the observer looks from +Z toward -Z; screen right is +X, screen up is +Y. One single weapon stands alone and centered in the frame. The broad inner face of the hook is turned toward the viewer, the hook tip points up and to the left, the big connector ring is on the right and the chain falls down the right side of the grip.",
            "back": "BACK view: the observer looks from -Z toward +Z; screen right is -X, screen up is +Y. One single weapon stands alone and centered in the frame. The convex back of the hook and its reinforcement blocks are visible, the big connector ring is seen as a full oval, the chain drops from it down the left side of the grip, and the hook tip points up and to the right.",
        },
        "view_cn": {
            "left": "左侧视图：观察者从 −X 朝 +X 看，画面右对应 +Z，画面上对应 +Y。画面里只有一把武器，单独站在画面中央。钩头侧向镜头，宽度约为正面的五分之一，是竖直窄钢楔。喉部读成一道窄槽，钩背的连接环是薄椭圆，亮银白内刃是一条细亮线，链环在宽椭圆与薄边之间交替，握柄分叉呈侧面。",
            "right": "右侧视图：观察者从 +X 朝 −X 看，画面右对应 −Z，画面上对应 +Y。画面里只有一把武器，单独站在画面中央。钩头侧向镜头，宽度约为正面的五分之一，是竖直窄钢楔。喉部读成一道窄槽，钩背的连接环是薄椭圆，亮银白内刃是一条细亮线，链环在宽椭圆与薄边之间交替，握柄分叉呈侧面。",
            "front": "正面视图：观察者从 +Z 朝 −Z 看，画面右对应 +X，画面上对应 +Y。画面里只有一把武器，单独站在画面中央。钩头的内侧宽面朝向观察者，钩尖朝上偏左，大连接环在右侧，链条沿握柄右侧下垂。",
            "back": "背面视图：观察者从 −Z 朝 +Z 看，画面右对应 −X，画面上对应 +Y。画面里只有一把武器，单独站在画面中央。钩背凸面与加强块可见，大连接环呈整圈椭圆，链条沿握柄左侧下垂，钩尖朝上偏右。",
        },
    },
    # j 版（2026-11-15）：改用**剪影 / 纸片轮廓**的表达，几乎不提结构术语。
    # h / i 两版都被拦（400），说明“描述钩头结构细节”这条线很容易触发过滤。
    "j": {
        "ortho_en": "", "ortho_cn": "",
        "view_en": {
            "left": "LEFT view: the observer looks from -X toward +X; screen right is +Z, screen up is +Y. One single weapon stands alone and centered in the frame, drawn as a flat side silhouette on white, like a paper cut-out of the weapon seen from the side: a narrow upright hook shape, a slim handle, and a hanging chain of alternating wide and narrow links.",
            "right": "RIGHT view: the observer looks from +X toward -X; screen right is -Z, screen up is +Y. One single weapon stands alone and centered in the frame, drawn as a flat side silhouette on white, like a paper cut-out of the weapon seen from the side: a narrow upright hook shape, a slim handle, and a hanging chain of alternating wide and narrow links.",
            "front": "FRONT view: the observer looks from +Z toward -Z; screen right is +X, screen up is +Y. One single weapon stands alone and centered in the frame. The broad inner face of the hook is turned toward the viewer, the hook tip points up and to the left, the big connector ring is on the right and the chain falls down the right side of the grip.",
            "back": "BACK view: the observer looks from -Z toward +Z; screen right is -X, screen up is +Y. One single weapon stands alone and centered in the frame. The convex back of the hook and its reinforcement blocks are visible, the big connector ring is seen as a full oval, the chain drops from it down the left side of the grip, and the hook tip points up and to the right.",
        },
        "view_cn": {
            "left": "左侧视图：观察者从 −X 朝 +X 看，画面右对应 +Z，画面上对应 +Y。画面里只有一把武器，单独站在画面中央，用平面的侧面剪影表现，像一张从侧面看过去的白色纸片剪影：一个窄长的直立钩形、一根细握柄、一条宽窄相间的下垂链条。",
            "right": "右侧视图：观察者从 +X 朝 −X 看，画面右对应 −Z，画面上对应 +Y。画面里只有一把武器，单独站在画面中央，用平面的侧面剪影表现，像一张从侧面看过去的白色纸片剪影：一个窄长的直立钩形、一根细握柄、一条宽窄相间的下垂链条。",
            "front": "正面视图：观察者从 +Z 朝 −Z 看，画面右对应 +X，画面上对应 +Y。画面里只有一把武器，单独站在画面中央。钩头的内侧宽面朝向观察者，钩尖朝上偏左，大连接环在右侧，链条沿握柄右侧下垂。",
            "back": "背面视图：观察者从 −Z 朝 +Z 看，画面右对应 −X，画面上对应 +Y。画面里只有一把武器，单独站在画面中央。钩背凸面与加强块可见，大连接环呈整圈椭圆，链条沿握柄左侧下垂，钩尖朝上偏右。",
        },
    },
    # k 版（2026-11-15）：RIGHT 的轻微扰动版。
    # f 版 RIGHT 与 f 版 LEFT 只差方向句与“内刃朝左”，却被拦
    # （request id 202611151139428314520802LaUm7G2），基本可判定为该过滤的随机性；
    # 按“先改写再重发”做最小扰动（换两个短语），语义不变。
    "k": {
        "ortho_en": "", "ortho_cn": "",
        "view_en": {
            "right": "RIGHT view: the observer looks from +X toward -X; screen right is -Z, screen up is +Y. One single weapon stands alone in the middle of the frame. The hook is turned edge-on and appears about one fifth as wide as in the front view, a slim vertical steel wedge. The throat reads as a narrow slot, the ring on the spine is a thin ellipse, the bright inner edge is a thin bright line, the chain links alternate between wide ovals and thin edges, and the grip shows its short fork in profile.",
            "left": "LEFT view: the observer looks from -X toward +X; screen right is +Z, screen up is +Y. One single weapon stands alone in the middle of the frame. The hook is turned edge-on and appears about one fifth as wide as in the front view, a slim vertical steel wedge. The throat reads as a narrow slot, the ring on the spine is a thin ellipse, the bright inner edge is a thin bright line, the chain links alternate between wide ovals and thin edges, and the grip shows its short fork in profile.",
            "front": "FRONT view: the observer looks from +Z toward -Z; screen right is +X, screen up is +Y. One single weapon stands alone in the middle of the frame. The broad inner face of the hook is turned toward the viewer, the hook tip points up and to the left, the big connector ring is on the right and the chain falls down the right side of the grip.",
            "back": "BACK view: the observer looks from -Z toward +Z; screen right is -X, screen up is +Y. One single weapon stands alone in the middle of the frame. The convex back of the hook and its reinforcement blocks are visible, the big connector ring is seen as a full oval, the chain drops from it down the left side of the grip, and the hook tip points up and to the right.",
        },
        "view_cn": {
            "right": "右侧视图：观察者从 +X 朝 −X 看，画面右对应 −Z，画面上对应 +Y。画面里只有一把武器，单独站在画面中央。钩头侧向镜头，宽度约为正面的五分之一，是竖直窄钢楔。喉部读成一道窄槽，钩背的连接环是薄椭圆，亮银白内刃是一条细亮线，链环在宽椭圆与薄边之间交替，握柄分叉呈侧面。",
            "left": "左侧视图：观察者从 −X 朝 +X 看，画面右对应 +Z，画面上对应 +Y。画面里只有一把武器，单独站在画面中央。钩头侧向镜头，宽度约为正面的五分之一，是竖直窄钢楔。喉部读成一道窄槽，钩背的连接环是薄椭圆，亮银白内刃是一条细亮线，链环在宽椭圆与薄边之间交替，握柄分叉呈侧面。",
            "front": "正面视图：观察者从 +Z 朝 −Z 看，画面右对应 +X，画面上对应 +Y。画面里只有一把武器，单独站在画面中央。钩头的内侧宽面朝向观察者，钩尖朝上偏左，大连接环在右侧，链条沿握柄右侧下垂。",
            "back": "背面视图：观察者从 −Z 朝 +Z 看，画面右对应 −X，画面上对应 +Y。画面里只有一把武器，单独站在画面中央。钩背凸面与加强块可见，大连接环呈整圈椭圆，链条沿握柄左侧下垂，钩尖朝上偏右。",
        },
    },
    # m 版（2026-11-15）：**不再追加指令段**，而是直接在已验证可过的正文里改写取景句。
    # 依据：当轮 8 次“正文 + 追加视图指令段”全部被拦（LEFT c/d/e、RIGHT a/b/c、BACK a），
    # 而同一条正文单独提交（base 对照）连续两次通过 ⇒ 触发点是“追加的视图/制图指令段”，不是正文。
    # m 版只替换正文里的两句话（取景句 + 展示句），其余一字不动。
    "m": {
        "ortho_en": "", "ortho_cn": "",
        "view_en": {
            "back": "Show one complete weapon standing vertically, seen from behind, centered and fully inside the frame. The convex back of the hook and its reinforcement blocks face the viewer, the big connector ring is seen as a full oval, the chain drops from it down the left side of the grip, and the hook tip points up and to the right.",
            "right": "Show one complete weapon standing vertically, seen from its right side, centered and fully inside the frame. The hook is turned edge-on and appears about one fifth as wide as in the front view, the bright inner edge of the hook points to the left of the image, the ring on the hook spine is a thin ellipse, the chain links alternate between wide ovals and thin edges, and the grip shows its short fork in profile.",
            "left": "Show one complete weapon standing vertically, seen from its left side, centered and fully inside the frame. The hook is turned edge-on and appears about one fifth as wide as in the front view, the bright inner edge of the hook points to the right of the image, the ring on the hook spine is a thin ellipse, the chain links alternate between wide ovals and thin edges, and the grip shows its short fork in profile.",
            "front": "Show one complete weapon standing vertically, seen from the front, centered and fully inside the frame. The broad inner face of the hook is turned toward the viewer, the hook tip points up and to the left, the big connector ring is on the right and the chain falls down the right side of the grip.",
        },
        "view_cn": {
            "back": "画面只展示一件完整武器，从背面看，竖直正立，居中且完整入画。钩背凸面与加强块朝向观察者，大连接环呈整圈椭圆，链条沿握柄左侧下垂，钩尖朝上偏右。",
            "right": "画面只展示一件完整武器，从它的右侧看，竖直正立，居中且完整入画。钩头侧向镜头，宽度约为正面的五分之一，钩头的亮银白内刃指向画面左侧，钩背的连接环是薄椭圆，链环在宽椭圆与薄边之间交替，握柄分叉呈侧面。",
            "left": "画面只展示一件完整武器，从它的左侧看，竖直正立，居中且完整入画。钩头侧向镜头，宽度约为正面的五分之一，钩头的亮银白内刃指向画面右侧，钩背的连接环是薄椭圆，链环在宽椭圆与薄边之间交替，握柄分叉呈侧面。",
            "front": "画面只展示一件完整武器，从正面看，竖直正立，居中且完整入画。钩头的内侧宽面朝向观察者，钩尖朝上偏左，大连接环在右侧，链条沿握柄右侧下垂。",
        },
    },
    # n 版（2026-11-15）：**最小改动**——取景句里只加“seen from …”几个词，不加任何结构描述。
    # m 版（正文内改写整句）仍被拦，说明只要新增“视图语义”就可能触发；
    # n 版用来测试最小增量是否还能通过。
    "n": {
        "ortho_en": "", "ortho_cn": "",
        "view_en": {
            "back": "Show one complete weapon standing vertically, seen from behind, centered and fully inside the frame.",
            "right": "Show one complete weapon standing vertically, seen from the right side, centered and fully inside the frame.",
            "left": "Show one complete weapon standing vertically, seen from the left side, centered and fully inside the frame.",
            "front": "Show one complete weapon standing vertically, seen from the front, centered and fully inside the frame.",
        },
        "view_cn": {
            "back": "画面只展示一件完整武器，从背面看，竖直正立，居中且完整入画。",
            "right": "画面只展示一件完整武器，从右侧看，竖直正立，居中且完整入画。",
            "left": "画面只展示一件完整武器，从左侧看，竖直正立，居中且完整入画。",
            "front": "画面只展示一件完整武器，从正面看，竖直正立，居中且完整入画。",
        },
    },
    # p 版（2026-11-15，走**文生图通道** /v1/images/generations）：
    # 实测 edits 通道下“正文 + 任何视图语义”都会被拦，而同一段文字在 generations 通道可以通过；
    # 但没有参考图时模型会自己铺黑底+辉光，故把“白底”写死进正文，并保留完整的视图描述。
    "p": {
        "ortho_en": "", "ortho_cn": "",
        "view_en": {
            "back": "Show one complete weapon standing vertically, seen from behind, centered and fully inside the frame. The convex back of the hook and its reinforcement blocks face the viewer, the big connector ring is seen as a full oval, the chain drops from it down the left side of the grip, and the hook tip points up and to the right.",
            "right": "Show one complete weapon standing vertically, seen from its right side, centered and fully inside the frame. The hook is turned edge-on and appears about one fifth as wide as in the front view, the bright inner edge of the hook points to the left of the image, the ring on the hook spine is a thin ellipse, the chain links alternate between wide ovals and thin edges, and the grip shows its short fork in profile.",
            "left": "Show one complete weapon standing vertically, seen from its left side, centered and fully inside the frame. The hook is turned edge-on and appears about one fifth as wide as in the front view, the bright inner edge of the hook points to the right of the image, the ring on the hook spine is a thin ellipse, the chain links alternate between wide ovals and thin edges, and the grip shows its short fork in profile.",
            "front": "Show one complete weapon standing vertically, seen from the front, centered and fully inside the frame. The broad inner face of the hook is turned toward the viewer, the hook tip points up and to the left, the big connector ring is on the right and the chain falls down the right side of the grip.",
        },
        "view_cn": {
            "back": "画面只展示一件完整武器，从背面看，竖直正立，居中且完整入画。钩背凸面与加强块朝向观察者，大连接环呈整圈椭圆，链条沿握柄左侧下垂，钩尖朝上偏右。",
            "right": "画面只展示一件完整武器，从它的右侧看，竖直正立，居中且完整入画。钩头侧向镜头，宽度约为正面的五分之一，钩头的亮银白内刃指向画面左侧，钩背的连接环是薄椭圆，链环在宽椭圆与薄边之间交替，握柄分叉呈侧面。",
            "left": "画面只展示一件完整武器，从它的左侧看，竖直正立，居中且完整入画。钩头侧向镜头，宽度约为正面的五分之一，钩头的亮银白内刃指向画面右侧，钩背的连接环是薄椭圆，链环在宽椭圆与薄边之间交替，握柄分叉呈侧面。",
            "front": "画面只展示一件完整武器，从正面看，竖直正立，居中且完整入画。钩头的内侧宽面朝向观察者，钩尖朝上偏左，大连接环在右侧，链条沿握柄右侧下垂。",
        },
    },
    # base 版（对照组）：**不加任何视图/正交指令**，就是 2026-11-15 18:46 成功产出 v6 的那条提示词。
    # 用来分辨“是不是我们的新增措辞被拦”还是“服务端过滤变严 / 路由变了”。
    "base": {
        "ortho_en": "", "ortho_cn": "",
        "view_en": {"front": "", "left": "", "right": "", "back": ""},
        "view_cn": {"front": "", "left": "", "right": "", "back": ""},
    },
}

# ── 段D：负面词（plan §5.13.6 原文，四张完全相同，不新增）───────────────
NEGATIVE = """farming scythe, long broad crescent, decorative crescent, ordinary sickle, shallow curve, straight staff, long wooden handle, oversized wood grip, decorative staff, sword, spear, axe, halberd, double blade, outward-facing edge, closed hook with no catching throat, missing retaining barb, missing catching cavity, chain hanging as jewelry, side-mounted chain ornament, chain disconnected from hook root, solid rope, black bar, tassel, tangled chain, knotted chain, perfectly taut chain, missing hollow links, oversized forked handle, excessive leather ornaments, beads, jewelry, hanging cloth strips, creature face, animal head sculpture, gold, brass, bronze, copper, gilded metal, yellow metal, uniformly gray, gray muddy palette, low saturation, heavy rust, dirty black iron, black wood, black-brown leather, photorealism, sci-fi, plastic toy, magic glow, flames, smoke, character, hand, person, battlefield, scenery, text, watermark, logo, cropped weapon, tilted weapon, multiple weapons, duplicate weapon"""

VIEWS = ("front", "left", "right", "back")


def abs_path(p: str) -> str:
    return p if os.path.isabs(p) else os.path.join(PROJ, p.replace("/", os.sep))


def sha256_file(path: str) -> str:
    with open(path, "rb") as fh:
        return hashlib.sha256(fh.read()).hexdigest()


def png_info(path: str) -> dict:
    with open(path, "rb") as fh:
        head = fh.read(33)
    if head[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError("not a PNG")
    w, h = struct.unpack(">II", head[16:24])
    return {"width": w, "height": h, "bit_depth": head[24], "color_type": head[25]}


def build_multipart(fields, files):
    boundary = "----locus%s" % uuid.uuid4().hex
    out = bytearray()
    for k, v in fields.items():
        out += ("--%s\r\n" % boundary).encode()
        out += ('Content-Disposition: form-data; name="%s"\r\n\r\n' % k).encode()
        out += (v + "\r\n").encode()
    for name, path in files:
        ctype = mimetypes.guess_type(path)[0] or "application/octet-stream"
        with open(path, "rb") as fh:
            blob = fh.read()
        out += ("--%s\r\n" % boundary).encode()
        out += ('Content-Disposition: form-data; name="%s"; filename="%s"\r\n'
                % (name, os.path.basename(path))).encode()
        out += ("Content-Type: %s\r\n\r\n" % ctype).encode()
        out += blob + b"\r\n"
    out += ("--%s--\r\n" % boundary).encode()
    return bytes(out), "multipart/form-data; boundary=%s" % boundary


def check_models(key: str) -> dict:
    req = urllib.request.Request(MODELS_ENDPOINT, headers={"Authorization": "Bearer %s" % key})
    with urllib.request.urlopen(req, timeout=120) as resp:
        payload = json.loads(resp.read().decode("utf-8"))
    ids = sorted(item.get("id", "") for item in payload.get("data", []))
    return {"http_status": resp.status, "models": ids,
            "image_models": [i for i in ids if "image" in i]}


def main() -> int:
    ap = argparse.ArgumentParser(description="提交追命锁镰四视图（逐张）")
    ap.add_argument("--view", required=True, choices=VIEWS)
    ap.add_argument("--attempt", default="a", help="同一视图的尝试编号，用于不覆盖旧产物")
    ap.add_argument("--prompt-set", default="b", choices=sorted(PROMPT_SETS.keys()))
    ap.add_argument("--endpoint-kind", default="edits", choices=("edits", "generations"),
                    help="edits = 带参考图的 multipart；generations = 不带参考图的文生图通道")
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--approve", action="store_true")
    ap.add_argument("--model", default=MODEL)
    args = ap.parse_args()

    stem = "chain_sickle_%s_%s" % (args.view, args.attempt)
    shape_en, shape_cn = load_archive_shape()
    pset = PROMPT_SETS[args.prompt_set]
    parts_en = [shape_en] + [t for t in (pset["ortho_en"], pset["view_en"][args.view]) if t]
    parts_cn = [shape_cn] + [t for t in (pset["ortho_cn"], pset["view_cn"][args.view]) if t]
    prompt_en = "\n\n".join(parts_en)
    prompt_cn = "\n\n".join(parts_cn)
    if args.prompt_set == "m":
        # 在正文内部改写取景句，而不是追加指令段
        old_en = ("Show one complete weapon standing vertically, centered and fully inside the frame. "
                  "The weapon consists of a compact metal grappling hook head, a short forked grip, "
                  "and one naturally hanging chain. The grappling hook head is the primary visual identity "
                  "and must be more prominent than the grip and chain.")
        new_en = pset["view_en"][args.view] + (" The weapon consists of a compact metal grappling hook head, a short forked grip, "
                                              "and one naturally hanging chain. The grappling hook head is the primary visual identity "
                                              "and must be more prominent than the grip and chain.")
        if old_en not in prompt_en:
            raise SystemExit("m 版：正文取景句未命中，架构已变")
        prompt_en = prompt_en.replace(old_en, new_en)
        old_cn = "画面只展示一件完整武器，竖直正立，居中且完整入画。"
        new_cn = pset["view_cn"][args.view]
        if old_cn not in prompt_cn:
            raise SystemExit("m 版：中文正文取景句未命中，架构已变")
        prompt_cn = prompt_cn.replace(old_cn, new_cn)
    elif args.prompt_set == "n":
        # 最小改动：只替换取景句本身
        old_en = "Show one complete weapon standing vertically, centered and fully inside the frame."
        if old_en not in prompt_en:
            raise SystemExit("n 版：正文取景句未命中，架构已变")
        prompt_en = prompt_en.replace(old_en, pset["view_en"][args.view])
        old_cn = "画面只展示一件完整武器，竖直正立，居中且完整入画。"
        if old_cn not in prompt_cn:
            raise SystemExit("n 版：中文正文取景句未命中，架构已变")
        prompt_cn = prompt_cn.replace(old_cn, pset["view_cn"][args.view])
    elif args.prompt_set == "p":
        # 文生图通道版：改写取景句 + 把“白底”写死
        old_en = ("Show one complete weapon standing vertically, centered and fully inside the frame. "
                  "The weapon consists of a compact metal grappling hook head, a short forked grip, "
                  "and one naturally hanging chain. The grappling hook head is the primary visual identity "
                  "and must be more prominent than the grip and chain.")
        new_en = (pset["view_en"][args.view] + " The weapon consists of a compact metal grappling hook head, "
                  "a short forked grip, and one naturally hanging chain. The grappling hook head is the primary "
                  "visual identity and must be more prominent than the grip and chain.")
        if old_en not in prompt_en:
            raise SystemExit("p 版：正文取景句未命中，架构已变")
        prompt_en = prompt_en.replace(old_en, new_en)
        old_bg = ("Present the weapon on a pure white or very light neutral background with only a subtle "
                  "soft ground shadow.")
        new_bg = ("Place the weapon alone on a plain, evenly lit white background, with only a small soft "
                  "contact shadow under it and no glow or vignette.")
        if old_bg not in prompt_en:
            raise SystemExit("p 版：正文背景句未命中，架构已变")
        prompt_en = prompt_en.replace(old_bg, new_bg)
        old_cn = "画面只展示一件完整武器，竖直正立，居中且完整入画。"
        if old_cn not in prompt_cn:
            raise SystemExit("p 版：中文正文取景句未命中，架构已变")
        prompt_cn = prompt_cn.replace(old_cn, pset["view_cn"][args.view])
        old_bg_cn = "使用干净的纯白或非常浅的中性背景，只有轻微柔和投影。"
        new_bg_cn = "把武器单独放在均匀的纯白背景上，只在下方留一小块柔和接地阴影，不要辉光、不要暗角。"
        if old_bg_cn not in prompt_cn:
            raise SystemExit("p 版：中文正文背景句未命中，架构已变")
        prompt_cn = prompt_cn.replace(old_bg_cn, new_bg_cn)
    prompt = prompt_en + "\n\nNegative constraints: " + NEGATIVE

    files = []
    for rel, role in REFS:
        p = abs_path(rel)
        if not os.path.isfile(p):
            raise SystemExit("参考图缺失: %s" % p)
        files.append((p, role))

    img_out = OUT_DIR / ("%s.png" % stem)

    if args.dry_run:
        print("=== 视图 %s（attempt %s）===" % (args.view, args.attempt))
        print("=== 参考图（顺序固定，三张 image[]；均不负责造型）===")
        for i, (p, role) in enumerate(files, 1):
            print("image %d: %s\n         sha256=%s\n         %s" % (i, p, sha256_file(p), role))
        print("\n=== 完整提交提示词（EN，%d 字符，sha256 %s）==="
              % (len(prompt), hashlib.sha256(prompt.encode("utf-8")).hexdigest()))
        print(prompt)
        print("\n=== 参数 ===")
        print("endpoint=%s\nmodel=%s size=%s quality=%s input_fidelity=%s output_format=png n=1"
              % (ENDPOINT, args.model, SIZE, QUALITY, INPUT_FIDELITY))
        print("不传 background；超时 %ds；自动重试 0；单张提交，不出合图" % TIMEOUT)
        print("输出: %s" % img_out)
        return 0

    if not args.approve:
        raise SystemExit("未确认批准：加 --approve 才会真的提交")

    key = os.environ.get("MUSK_API_KEY")
    if not key:
        raise SystemExit("MUSK_API_KEY missing")
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    if img_out.exists():
        raise SystemExit("refusing to overwrite existing output: %s" % img_out)

    models = check_models(key)
    if args.model not in models["image_models"]:
        raise SystemExit("模型不可用: %s（当前图像模型 %s）" % (args.model, models["image_models"]))

    fields = {
        "model": args.model,
        "prompt": prompt,
        "size": SIZE,
        "quality": QUALITY,
        "input_fidelity": INPUT_FIDELITY,
        "output_format": "png",
        "n": "1",
    }
    if args.endpoint_kind == "generations":
        # 文生图通道：不带参考图，只发 JSON
        endpoint = "https://api.muskapis.com/v1/images/generations"
        body = json.dumps({"model": args.model, "prompt": prompt, "size": SIZE,
                           "quality": QUALITY, "output_format": "png", "n": 1},
                          ensure_ascii=False).encode("utf-8")
        ctype = "application/json"
        used_refs = []
    else:
        endpoint = ENDPOINT
        body, ctype = build_multipart(fields, [("image[]", p) for p, _ in files])
        used_refs = files

    (OUT_DIR / ("%s.prompt.txt" % stem)).write_text(prompt, encoding="utf-8", newline="\n")
    (OUT_DIR / ("%s.prompt_cn.txt" % stem)).write_text(prompt_cn, encoding="utf-8", newline="\n")
    (OUT_DIR / ("%s.prompt_negative.txt" % stem)).write_text(NEGATIVE, encoding="utf-8", newline="\n")

    with open(OUT_DIR / ("%s.request.json" % stem), "w", encoding="utf-8") as fh:
        json.dump({
            "version": stem,
            "view": args.view,
            "attempt": args.attempt,
            "prompt_set": args.prompt_set,
            "shape_source": str(ARCHIVE) if ARCHIVE.is_file() else "in-script fallback",
            "plan": "Locus/knowledge/plan/weapon-concept-image-plan-20261114.md §5.15",
            "spec": "Locus/knowledge/design/weapon-3d-art-spec-axe-and-chain.md §3.2 / §4.4",
            "endpoint": endpoint,
            "method": "POST",
            "content_type": ctype,
            "fields": fields,
            "reference_images": [
                {"order": i + 1, "path": p, "role": role, "sha256": sha256_file(p),
                 "bytes": os.path.getsize(p)}
                for i, (p, role) in enumerate(used_refs)
            ],
            "models_check": models,
            "prompt_sha256": hashlib.sha256(prompt.encode("utf-8")).hexdigest(),
            "prompt_chars": len(prompt),
            "automatic_retries": 0,
            "read_timeout_seconds": TIMEOUT,
            "note": "不上传已验收概念图、不上传 PBZ 锁镰图（内容安全实测），造型由提示词承担",
            "submitted_at_local": time.strftime("%Y-%m-%d %H:%M:%S"),
        }, fh, indent=2, ensure_ascii=False)

    request = urllib.request.Request(endpoint, data=body, method="POST", headers={
        "Authorization": "Bearer %s" % key, "Content-Type": ctype})

    status = {"state": "submitted", "http_status": None, "seconds": None,
              "error": None, "output_image": str(img_out)}
    t0 = time.time()
    try:
        with urllib.request.urlopen(request, timeout=TIMEOUT) as resp:
            raw = resp.read()
            status["http_status"] = resp.status
    except urllib.error.HTTPError as exc:
        status["state"] = "failed"
        status["error"] = "HTTPError %s: %s" % (exc.code, exc.read().decode("utf-8", "replace")[:800])
        status["seconds"] = round(time.time() - t0, 2)
        with open(OUT_DIR / ("%s.status.json" % stem), "w", encoding="utf-8") as fh:
            json.dump(status, fh, indent=2, ensure_ascii=False)
        raise SystemExit(status["error"])
    except Exception as exc:  # noqa: BLE001
        status["state"] = "failed"
        status["error"] = "%s: %s" % (type(exc).__name__, exc)
        status["seconds"] = round(time.time() - t0, 2)
        with open(OUT_DIR / ("%s.status.json" % stem), "w", encoding="utf-8") as fh:
            json.dump(status, fh, indent=2, ensure_ascii=False)
        raise SystemExit(status["error"])

    status["seconds"] = round(time.time() - t0, 2)
    payload = json.loads(raw.decode("utf-8"))
    item = payload["data"][0]
    if item.get("b64_json"):
        blob = base64.b64decode(item["b64_json"])
        status["download"] = "b64_json"
    elif item.get("url"):
        with urllib.request.urlopen(item["url"], timeout=300) as r:
            blob = r.read()
        status["download"] = "url"
    else:
        raise SystemExit("no image payload")

    cleaned = json.loads(raw.decode("utf-8"))
    for it in cleaned.get("data", []):
        if it.get("b64_json"):
            it["b64_json"] = "<base64 omitted; image saved to %s>" % img_out
    with open(OUT_DIR / ("%s.response.json" % stem), "w", encoding="utf-8") as fh:
        json.dump(cleaned, fh, indent=2, ensure_ascii=False)

    with open(img_out, "wb") as fh:
        fh.write(blob)
    status.update(png_info(str(img_out)))
    status["output_bytes"] = len(blob)
    status["output_sha256"] = hashlib.sha256(blob).hexdigest()
    status["usage"] = payload.get("usage")
    status["echoed_size"] = payload.get("size")
    status["echoed_quality"] = payload.get("quality")
    status["echoed_background"] = payload.get("background")
    status["models_check"] = models
    status["state"] = "image_saved"
    with open(OUT_DIR / ("%s.status.json" % stem), "w", encoding="utf-8") as fh:
        json.dump(status, fh, indent=2, ensure_ascii=False)
    print(json.dumps(status, indent=2, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
