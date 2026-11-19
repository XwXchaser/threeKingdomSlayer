# -*- coding: utf-8 -*-
"""追命锁镰概念图 · 提交脚本（2026-11-15；plan §5.11 当前进行项）。

依据：Locus/knowledge/plan/weapon-concept-image-plan-20261114.md §5.11
需求书：Locus/knowledge/design/weapon-3d-art-spec-axe-and-chain.md
接口：POST https://api.muskapis.com/v1/images/edits（multipart，三张 image[]，顺序固定）
模型：gpt-image-2.6-sunburst（发起前用 GET /v1/models 核对）
参数：size=1024x1536 / quality=max / input_fidelity=high / output_format=png / n=1 / 不传 background
纪律：最多提交一次，不自动重试，不覆盖已有产物；四视图闸门未开（等用户确认概念图）

用法
    python Tools/visual_brief/submit_chain_sickle_concept_20261115.py --dry-run
    python Tools/visual_brief/submit_chain_sickle_concept_20261115.py --approve
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


OUT_DIR = _out_root() / "chain_sickle_concept_20261115"
ENDPOINT = "https://api.muskapis.com/v1/images/edits"
GENERATIONS_ENDPOINT = "https://api.muskapis.com/v1/images/generations"
MODELS_ENDPOINT = "https://api.muskapis.com/v1/models"
MODEL = "gpt-image-2.6-sunburst"
SIZE = "1024x1536"
QUALITY = "max"
INPUT_FIDELITY = "high"
TIMEOUT = 900
STEM = "chain_sickle_concept_v1"

# 上传顺序固定（plan §5.11.3）：图1 只管锁镰形制，图2 只管项目配色，图3 只管木料/铁箍材质表达
REFS = [
    (r"SkillWeaponsArtSource/01_Reference/pbz/zhuiming_suolian.png",
     "image 1: only the chain sickle form - inward cutting hook blade, claw-like blade root, forked handle, connector ring, corded hanging pieces, lower collar and end ring"),
    (r"SkillWeaponsArtSource/01_Reference/style/game_palette_board.png",
     "image 2: only the project color relationships - cold blue-gray steel, silver-white edge, warm brown wood, reddish-brown leather, high-contrast color blocks"),
    (r"SkillWeaponsArtSource/01_Reference/style/game_wood_iron_ref.png",
     "image 3: only the material language for warm brown wood, cold blue-gray iron collars and riveted construction"),
]

PROMPT_CN = """请基于上传的三张参考图，设计一把全新的三国战场风格“追命锁镰”概念图。

参考图职责必须严格区分：第一张只负责锁镰的整体形制与部件关系；第二张只负责本项目的配色和高清像素化材质色块；第三张只负责暖棕木料、冷蓝灰铁箍和铆接结构的材质表达。不要复制任何参考图的人物、背景、文字、光照、构图或装饰。

画面展示一把完整的单手追命锁镰，镰头位于上方，握杆向下，整体竖直正立，居中完整显示。镰头是内侧开刃的单刃弯钩镰，镰尖明确朝上。镰刃根部有一个向上扬起、带缺口的爪形头，像兽爪或凶猛的鸟兽头，但必须仍然读成真实兵器结构，不要做成怪物头雕像。镰刃根部连接一个明显的大连接环，链身从这个连接环自然垂下。

握杆必须是清楚的分叉结构，不是普通直柄。握杆使用暖棕偏红褐的旧皮革和皮绳缠绕，皮绳上串着少量暗铁小环与新月形小片，这些挂件应服务于锁镰的身份识别，不要堆成华丽首饰。握杆下端逐渐收细，带数道暗铁金属箍，最末端有一个小挂环。

从镰头根部的大连接环垂下一条自然下垂、略带弧度的铁链，视觉上显示 8 到 9 节闭合椭圆链环。每个链环必须是中空的真实椭圆环，截面细而有厚度，节与节之间有交叠和不同朝向，读成真正的铁链，不是实心粗绳、黑色长条或一团缠绕物。链尾有一个较大的连接环。链条自然下垂，不要笔直绷紧，也不要打结成团。

整体使用项目现有的高清像素化三国战场动作美术：厚重、凶悍、明亮、高对比、清晰的大体块和受控制的像素色簇。镰头主体和金属箍使用冷蓝灰钢，内侧刃缘使用亮银白色作为最亮区域；握杆使用暖棕旧木或被红褐皮革覆盖的暖棕结构；皮革使用暖棕偏红褐。金属必须有清晰受光面和暗面，但不要照片写实。

使用干净的单件武器概念展示构图，纯白或非常浅的中性背景，只有轻微柔和投影。武器完整、不出画、不倾斜，不出现人物、手部、战场、文字、尺寸标注、UI 或其它武器。"""

PROMPT_EN = """Design a new Three Kingdoms battlefield weapon called a chain sickle, based on the three uploaded reference images.

Keep the reference responsibilities separate: image 1 is only for the chain sickle's overall form and component relationships; image 2 is only for this project's color relationships and high-definition pixelated material color blocking; image 3 is only for the warm brown wood, cold blue-gray iron collars, and riveted material language. Do not copy any character, background, text, lighting, composition, or decoration from the references.

Show one complete single-handed chain sickle standing vertically, with the sickle head above and the handle below, centered and fully visible. The head is a single-edged inward-cutting curved hook sickle, with the pointed tip clearly facing upward. At the root of the blade, add an upward-facing notched claw-like head, resembling a fierce animal claw, but keep it as a believable weapon structure rather than a monster statue. Add one clearly visible large connector ring at the blade root, with the chain hanging naturally from that ring.

The handle must have a clearly forked construction, not an ordinary straight handle. Wrap it with warm reddish-brown aged leather and leather cord. Thread a few small dark-iron rings and crescent-shaped metal pieces onto the cord. These pieces should identify the weapon without becoming ornate jewelry. The lower end of the handle tapers narrower, has several dark iron collars, and ends in one small hanging ring.

From the large connector ring at the sickle head, show a natural hanging chain with a slight curve, visually containing 8 to 9 closed oval links. Each link must be a real hollow oval ring with a thin but readable cross-section. The links must overlap and alternate orientation so the result reads as a real chain, not a solid thick rope, black bar, or tangled mass. Add one larger connector ring at the chain tail. The chain hangs naturally; it is not perfectly straight and taut, and it is not knotted.

Use the project's high-definition pixelated Three Kingdoms battlefield action-art direction: heavy, fierce, bright, high contrast, large readable shapes, and controlled pixel-like color clusters. Use cold blue-gray steel for the sickle head and collars, with a bright silver-white inner cutting edge as the brightest area. Use warm brown wood or warm brown structure beneath the reddish-brown leather wraps. Metal must have clear light and shadow planes but must not be photorealistic.

Present the weapon as a clean isolated concept design on a pure white or very light neutral background with only a subtle soft ground shadow. The weapon must be complete, uncropped, upright, and not tilted. Do not include a person, hand, battlefield, text, dimension labels, UI, or any other weapon."""

NEGATIVE = """straight scythe, farming scythe, sickle without chain, ordinary sickle, sword, spear, axe, halberd, hook sword, western fantasy weapon, symmetrical double blade, outward-facing blade, blunt blade, missing inner cutting edge, missing claw-like blade root, missing connector ring, straight handle, single unbranched handle, missing forked grip, solid rope, black bar, beads, tangled chain, chain knot, taut chain, chain with no hollow links, oversized jewelry, ornate gold decoration, gold, brass, bronze, copper, yellow metal, heavy rust, dirty black iron, black wood, black-brown leather, photorealism, sci-fi, plastic toy, magic glow, flames, smoke, character, hand, person, battlefield, scenery, text, watermark, logo, cropped weapon, tilted weapon, multiple weapons, duplicate weapon"""

# ---------------------------------------------------------------------------
# v2（2026-11-15）：v1 被内容安全拦成 400（request id 20261115101035172894036Cr4d9t3W，
# 未生成、未计费）。按本项目既有经验「被拦时先改写措辞再重发」（skill/gpt-image-generation.md），
# v2 保持全部造型要求不变，只做三处改动：
#   ① 删掉 scythe / farming scythe / hook sword / halberd 等“武器名清单”式负面词，
#      换成中性描述（ordinary farming tool / no second weapon）；
#   ② 去掉 “monster statue”“real hollow oval ring…overlap and alternate orientation”
#      这类读起来像制作说明的句子，改成纯外观描述；
#   ③ 首句加入 “fantasy video game prop weapon” 定位，弱化真实武器指向。
# ---------------------------------------------------------------------------
PROMPT_CN_V2 = """请基于上传的三张参考图，为三国战场动作游戏设计一件幻想游戏道具武器：一把单手链钩镰的概念图。

参考图职责必须严格区分：第一张只负责这把链钩武器的整体形制与部件关系；第二张只负责本项目的配色与高清像素化材质色块；第三张只负责暖棕木料、冷蓝灰铁箍和铆接结构的材质表达。不要复制任何参考图的人物、背景、文字、光照、构图或装饰。

画面展示一件完整武器，竖直正立，钩状刃头在上、握柄在下，居中且完整入画。

刃头是一段内侧开刃的弯钩：一条饱满的弧线，锋利尖端朝上，刃口在弧线内侧。弧线根部有一个向上扬起、带缺口的钩形结构，读起来像锻造出来的金属构件，而不是生物雕像。旁边挂着一个明显的大椭圆连接环。

握柄是清楚的分叉结构，不是普通直柄：靠近上端分成两支，往下再合拢。握柄缠着暖棕偏红褐的旧皮革和皮绳，皮绳上串着少量暗铁小环与新月形小片，作为简洁的身份标记。握柄向下逐渐收细，带数道暗铁金属箍，最末端有一个小挂环。

从大连接环垂下一条带自然弧度的链条，画面中可见约八到九枚较大的椭圆链环，每一枚都是有薄壁的中空环，彼此交叠、角度交替，读起来是真正的链，而不是绳、不是黑条、也不是缠成一团。链尾收在一个更大的椭圆环上。链条松垂，既不笔直绷紧，也不打结。

整体使用项目现有的高清像素化三国战场动作美术：厚重、凶悍、明亮、高对比、清晰的大体块和受控制的像素色簇。钩状刃头与金属箍使用冷蓝灰钢，内侧刃缘是亮银白色、也是画面中最亮的区域，握柄是被红褐皮革覆盖的暖棕木。金属要有清晰受光面和暗面，但不照片写实。

使用干净的单件概念展示构图，纯白或非常浅的中性背景，只有轻微柔和投影。整件武器完整入画、不裁切、不倾斜。不出现人物、手部、场景、文字、尺寸标注、UI，也不出现第二件武器。"""

PROMPT_EN_V2 = """Design a fantasy video game prop weapon for a Three Kingdoms battlefield action game: a single-handed chain-hook sickle concept, based on the three uploaded reference images.

Keep the reference responsibilities separate: image 1 is only for the overall form and part relationships of a chain-hook weapon; image 2 is only for this project's color relationships and high-definition pixelated material color blocking; image 3 is only for the material language of warm brown wood, cold blue-gray iron collars and riveted construction. Do not copy any character, background, text, lighting, composition, or decoration from the references.

Show one complete weapon standing vertically, with the hook head above and the grip below, centered and fully inside the frame.

The head is a single-edged inward-cutting curved hook: a broad crescent curve whose sharp tip points upward, with the cutting edge on the inner side of the curve. At the base of that curve sits an upward-facing notched hook that reads as forged metal hardware rather than a creature sculpture. Beside it hangs one clearly visible large oval connector ring.

The grip is clearly forked, not an ordinary straight handle: two branches split near the top and rejoin lower down. It is wrapped in warm reddish-brown aged leather and leather cord, with a few small dark-iron rings and crescent-shaped metal pieces threaded on the cord as simple identity markers. The grip narrows toward the bottom, carries several dark iron collars, and ends in one small hanging ring.

From the large connector ring a chain hangs with a gentle natural curve. It shows about eight or nine large oval links, each a hollow ring with a thin readable wall, overlapping one another and alternating their angle, so it reads as a real chain and not as a rope, a bar, or a tangle. The chain tail ends in one larger oval ring. It hangs loosely, neither perfectly straight and taut nor knotted.

Use the project's high-definition pixelated Three Kingdoms battlefield action-art direction: heavy, fierce, bright, high contrast, large readable shapes, and controlled pixel-like color clusters. The hook head and the collars are cold blue-gray steel, the inner cutting edge is bright silver-white and is the brightest area of the image, and the grip is warm brown wood under reddish-brown leather wraps. Metal shows clear light and shadow planes without looking photorealistic.

Present it as a clean isolated concept design on a pure white or very light neutral background with only a subtle soft ground shadow. The whole weapon is visible, uncropped, upright, and not tilted. No person, no hand, no scenery, no text, no dimension labels, no UI, and no second weapon."""

NEGATIVE_V2 = """ordinary farming tool, blade pointing outward, blunt edge, missing inner cutting edge, missing notched hook at the blade base, missing connector ring, straight unbranched grip, missing forked grip, solid rope, black bar, tangled chain, knotted chain, chain with no hollow links, oversized jewellery, ornate decoration, gold, brass, bronze, copper, yellow metal, heavy rust, dirty black iron, black wood, black-brown leather, photorealism, sci-fi, plastic toy, glowing magic effect, flames, smoke, character, hand, person, battlefield, scenery, text, watermark, logo, cropped weapon, tilted weapon, multiple weapons, duplicate weapon"""

# v3：把 PBZ 参考图从上传列表里去掉后，造型要求必须完全由提示词承担（原文见 §5.11.4 / §5.11.5）
PROMPT_CN_V3 = """请基于上传的两张画风参考图，为三国战场动作游戏设计一件幻想游戏道具武器：一把单手链钩镰（带链钩镰）的概念图。

参考图只负责画风与材质：图1 只负责本项目的配色与高清像素化材质色块，图2 只负责暖棕木料、冷蓝灰铁箍和铆接结构的材质表达。不要复制参考图的任何人物、背景、文字、光照、构图或装饰。武器的造型完全按下面的描述来画。

画面展示一件完整武器，竖直正立，钩状刃头在上、握柄在下，居中且完整入画。

刃头是一段内侧开刃的弯钩：一条饱满的弧线，锋利尖端朝上，刃口在弧线内侧。弧线根部有一个向上扬起、带缺口的爪形钩，读起来像锻造出来的金属构件，而不是生物雕像。刃根旁挂着一个明显的大椭圆连接环。

握柄是清楚的分叉结构，不是普通直柄：靠近上端分成两支，往下再合拢。握柄缠着暖棕偏红褐的旧皮革和皮绳，皮绳上串着少量暗铁小环与新月形小片，作为简洁的身份标记。握柄向下逐渐收细，带数道暗铁金属箍，最末端有一个小挂环。

从大连接环垂下一条带自然弧度的链条，画面中可见约八到九枚较大的椭圆链环，每一枚都是有薄壁的中空环，彼此交叠、角度交替，读起来是真正的链，而不是绳、不是黑条、也不是缠成一团。链尾收在一个更大的椭圆环上。链条松垂，既不笔直绷紧，也不打结。

整体使用项目现有的高清像素化三国战场动作美术：厚重、凶悍、明亮、高对比、清晰的大体块和受控制的像素色簇。钩状刃头与金属箍使用冷蓝灰钢，内侧刃缘是亮银白色、也是画面中最亮的区域，握柄是被红褐皮革覆盖的暖棕木。金属要有清晰受光面和暗面，但不照片写实。

使用干净的单件概念展示构图，纯白或非常浅的中性背景，只有轻微柔和投影。整件武器完整入画、不裁切、不倾斜。不出现人物、手部、场景、文字、尺寸标注、UI，也不出现第二件武器。"""

PROMPT_EN_V3 = """Design a fantasy video game prop weapon for a Three Kingdoms battlefield action game: a single-handed chain sickle, meaning a hook-bladed staff with a chain hanging from its blade root. Base the art direction on the two uploaded reference images.

The uploaded references are style references only: image 1 is only for this project's color relationships and high-definition pixelated material color blocking, and image 2 is only for the material language of warm brown wood, cold blue-gray iron collars and riveted construction. Do not copy any character, background, text, lighting, composition, or decoration from the references. The shape of the weapon must follow the description below exactly.

Show one complete weapon standing vertically, with the hook head above and the grip below, centered and fully inside the frame.

The head is a single-edged inward-cutting curved hook: a broad crescent curve whose sharp tip points upward, with the cutting edge on the inner side of the curve. At the base of that curve sits an upward-facing notched claw hook that reads as forged metal hardware rather than a creature sculpture. Beside it hangs one clearly visible large oval connector ring.

The grip is clearly forked, not an ordinary straight handle: two branches split near the top and rejoin lower down. It is wrapped in warm reddish-brown aged leather and leather cord, with a few small dark-iron rings and crescent-shaped metal pieces threaded on the cord as simple identity markers. The grip narrows toward the bottom, carries several dark iron collars, and ends in one small hanging ring.

From the large connector ring a chain hangs with a gentle natural curve. It shows about eight or nine large oval links, each a hollow ring with a thin readable wall, overlapping one another and alternating their angle, so it reads as a real chain and not as a rope, a bar, or a tangle. The chain tail ends in one larger oval ring. It hangs loosely, neither perfectly straight and taut nor knotted.

Use the project's high-definition pixelated Three Kingdoms battlefield action-art direction: heavy, fierce, bright, high contrast, large readable shapes, and controlled pixel-like color clusters. The hook head and the collars are cold blue-gray steel, the inner cutting edge is bright silver-white and is the brightest area of the image, and the grip is warm brown wood under reddish-brown leather wraps. Metal shows clear light and shadow planes without looking photorealistic.

Present it as a clean isolated concept design on a pure white or very light neutral background with only a subtle soft ground shadow. The whole weapon is visible, uncropped, upright, and not tilted. No person, no hand, no scenery, no text, no dimension labels, no UI, and no second weapon."""

# ---------------------------------------------------------------------------
# v4（2026-11-15）：按 plan §5.13「金属锁钩优先、短柄辅助、链条回收」方案重做。
# 参考图换成三张项目自产画风图（色卡 / 木料铁箍 / 钢刃），不上传会被拦截的 PBZ 锁镰图；
# 造型完全由 §5.13.4 / §5.13.5 的提示词承担。
# ---------------------------------------------------------------------------
PROMPT_CN_V4 = """为一款高清像素化三国战场动作游戏重新设计“追命锁链”的战斗武器概念图。它必须是一件能够被抛出、勾住远处敌人、绷紧铁链并把敌人拉回来的锁钩武器，不是农具镰刀，也不是装饰性长柄武器。

画面只展示一件完整武器，竖直正立，居中且完整入画。武器由上方的紧凑金属锁钩、短分叉握柄和一条自然下垂的铁链组成。锁钩是第一识别点，必须比握柄和链条更醒目。

锁钩头使用厚重的冷蓝灰钢制成，体量紧凑、结构结实。它有一个明确张开的内凹捕捉喉部，喉部空间足以卡住敌人的腰腹、四肢或护甲边缘。锁钩内侧是一条连续、锋利、亮银白的刃缘。钩尖向后朝向链条回收方向弯回，形成明显的止脱倒钩；钩尖、捕捉喉部和链条连接环必须形成一条清晰的拉力路径，让人一眼理解它如何勾住敌人并把敌人拉回。

不要画成长而宽的月牙镰刃。不要让钩口变成浅弧线。不要让链条从侧面像装饰挂件一样垂下。链条必须连接在钩背或钩根的受力连接环上，连接位置要靠近钩头结构中心。

钩头根部保留一个短小、向上扬起、带缺口的钢制止脱爪，作为锁钩结构的一部分，不要做成人脸、兽头或雕像。钩头可以有一到两个厚钢加强块，但不要有大面积铆钉排、徽章、雕花或装饰铁条。

钩头下方是短而结实的分叉握柄：两支短钢骨/木骨从钩头根部向下分开，随后在较短距离内合拢为一个握持段。握柄不是长木杆，木料占比明显低于巨斧。握柄只用少量暖棕木和红褐旧皮革包覆，皮革形成清楚的红褐色块面。只保留一到两个暗铁小环或新月形小片作为身份细节，不要串成首饰，不要垂挂大量布条。

从钩背受力连接环垂下一条自然略弯的铁链，视觉上显示八到九枚闭合椭圆链环。每枚链环都中空、有细但可读的厚度，彼此交叠并交替朝向。链条要读成真实可拉回敌人的锁链，不是绳子、黑色长条或装饰流苏。链尾有一个稍大的连接环，但链条不能抢过锁钩主体。

颜色和巨斧明确区分：不要大面积暖棕木柄，也不要整件灰蒙蒙低饱和。锁钩主体使用明亮的冷蓝灰钢和深蓝青暗面，内刃使用亮银白色高光；链环使用蓝灰钢并有清楚银灰受光边；握柄只使用少量暖棕木，皮革使用醒目的暖红褐色，允许极少量暗朱红或深青色识别块。整体明亮、高对比、色彩分区清楚，但绝不使用金色、黄铜、铜色或鎏金装饰。

使用干净的纯白或非常浅的中性背景，只有轻微柔和投影。不要人物、手部、战场、文字、尺寸标注、UI、其它武器、魔法光效或火焰。武器必须完整、不裁切、不倾斜。"""

PROMPT_EN_V4 = """Redesign the combat weapon called Chain Pull for a high-definition pixelated Three Kingdoms battlefield action game. It must be a weapon that can be thrown, catch a distant enemy, pull the chain taut, and drag the enemy back. It is not a farming scythe and not a decorative long-handled weapon.

Show one complete weapon standing vertically, centered and fully inside the frame. The weapon consists of a compact metal grappling hook head, a short forked grip, and one naturally hanging chain. The grappling hook head is the primary visual identity and must be more prominent than the grip and chain.

The hook head is made from thick, solid cold blue-gray steel. It is compact, heavy, and structurally strong. It has a clearly open inward catching throat with enough cavity to catch an enemy's waist, limb, armor edge, or clothing. The inner edge is one continuous sharp bright silver-white cutting edge. The hook tip curves backward toward the chain and the retrieval direction, forming a clear retaining barb. The hook tip, catching throat, and chain connector must form one readable pulling-force path so the viewer immediately understands how the weapon catches an enemy and pulls the enemy back.

Do not make a long, broad crescent farming blade. Do not make a shallow decorative curve. Do not let the chain hang from the side like jewelry. The chain must attach to a load-bearing connector on the hook back or hook root, close to the structural center of the hook head.

At the hook root, keep one short upward-facing notched steel retaining claw as part of the hook structure. It must not become a face, animal head, or creature sculpture. The hook may have one or two thick steel reinforcement blocks, but no rows of rivets, crest, engraving, decorative metal strips, or oversized ornaments.

Below the hook head is a short, sturdy forked grip. Two short steel-and-wood branches split below the hook root and converge into one hand-grip after a short distance. This is not a long wooden staff; the amount of wood must be visibly much lower than on the battle axe. Use only a small amount of warm brown wood and clear warm reddish-brown leather wraps. Keep only one or two small dark-iron rings or crescent-shaped markers as identity details. Do not turn them into jewelry and do not add hanging cloth strips.

From the load-bearing connector on the hook back, show one naturally curved chain with eight or nine closed oval links. Every link is hollow, with a thin but readable wall, overlapping and alternating orientation. The chain must read as a real chain that can pull an enemy, not a rope, black bar, or decorative tassel. End it with one slightly larger connector ring, but do not let the chain overpower the hook head.

Make this weapon clearly different from the large battle axe: no large warm-brown wooden handle and no uniformly gray low-saturation palette. Use bright cold blue-gray steel and deep blue-teal shadow planes for the hook, bright silver-white inner edge highlights, blue-gray chain links with clean silver-gray highlights, a small amount of warm brown wood, and clearly visible warm reddish-brown leather. A very small amount of dark vermilion or deep teal may be used as an identity accent. The overall image is bright, high contrast, and clearly color-blocked, but contains no gold, brass, copper, or gilded decoration.

Present the weapon on a pure white or very light neutral background with only a subtle soft ground shadow. Do not include a person, hand, battlefield, text, dimension labels, UI, another weapon, magic glow, or flames. Keep the weapon complete, uncropped, and upright."""

NEGATIVE_V4 = """farming scythe, long broad crescent, decorative crescent, ordinary sickle, shallow curve, straight staff, long wooden handle, oversized wood grip, decorative staff, sword, spear, axe, halberd, double blade, outward-facing edge, closed hook with no catching throat, missing retaining barb, missing catching cavity, chain hanging as jewelry, side-mounted chain ornament, chain disconnected from hook root, solid rope, black bar, tassel, tangled chain, knotted chain, perfectly taut chain, missing hollow links, oversized forked handle, excessive leather ornaments, beads, jewelry, hanging cloth strips, creature face, animal head sculpture, gold, brass, bronze, copper, gilded metal, yellow metal, uniformly gray, gray muddy palette, low saturation, heavy rust, dirty black iron, black wood, black-brown leather, photorealism, sci-fi, plastic toy, magic glow, flames, smoke, character, hand, person, battlefield, scenery, text, watermark, logo, cropped weapon, tilted weapon, multiple weapons, duplicate weapon"""

# ---------------------------------------------------------------------------
# v5（2026-11-15）：v4 的定点修正版。
# v4 失败项（§5.13.7）：链条挂在**握柄底端**的箍上，而不是钩背/钩根的受力连接环，
# 导致“钩尖→钩喉→连接环”的拉力路径断开、链条像挂件。
# v5 不改其它要求（钩头形态、配色、短分叉握柄、8–9 环链均保持），
# 只把连接位置写成硬约束：链条连接环位于钩背外弧、明显高于握柄；握柄底端是封死的钢帽，无环无链。
# ---------------------------------------------------------------------------
PROMPT_CN_V5 = PROMPT_CN_V4.replace(
    "链条必须连接在钩背或钩根的受力连接环上，连接位置要靠近钩头结构中心。",
    "链条必须连接在钩背外弧上、明显高于握柄的一个受力连接环上：最上面那一枚链环的高度要与钩喉齐平，"
    "整条链从钩头一侧向下垂落，经过握柄旁边，末端的大连接环停在画面下部。"
    "握柄底端是一个封死的钢帽，没有环、没有链、没有挂钩。",
).replace(
    "从钩背受力连接环垂下一条自然略弯的铁链",
    "从钩背高位受力连接环垂下一条自然略弯的铁链",
).replace(
    "钩头下方是短而结实的分叉握柄：两支短钢骨/木骨从钩头根部向下分开，随后在较短距离内合拢为一个握持段。",
    "钩头下方是短而结实的分叉握柄：两支短钢骨/木骨从钩头根部向下分开，随后在较短距离内合拢为一个握持段。"
    "握柄与钩头之间没有任何链条，链条只从钩背的连接环出来。",
)

PROMPT_EN_V5 = PROMPT_EN_V4.replace(
    "Do not make a long, broad crescent farming blade. Do not make a shallow decorative curve. Do not let the chain hang from the side like jewelry. The chain must attach to a load-bearing connector on the hook back or hook root, close to the structural center of the hook head.",
    "Do not make a long, broad crescent farming blade. Do not make a shallow decorative curve. Do not let the chain hang from the side like jewelry. "
    "The chain must attach to a load-bearing ring mounted on the outer spine of the hook head, clearly higher than the grip: the topmost chain link sits at the same height as the hook throat, "
    "and the whole chain then drapes down beside the grip, ending in one larger connector ring near the bottom of the frame. "
    "The bottom of the grip is a closed steel butt cap with no ring, no chain and no hook on it. No chain may touch the grip.",
).replace(
    "From the load-bearing connector on the hook back, show one naturally curved chain with eight or nine closed oval links.",
    "From the load-bearing ring high on the hook back, show one naturally curved chain with eight or nine closed oval links.",
).replace(
    "Below the hook head is a short, sturdy forked grip.",
    "Below the hook head is a short, sturdy forked grip, and no chain is attached to the grip itself.",
)

NEGATIVE_V5 = NEGATIVE_V4 + ", chain attached to the grip, chain hanging from the handle bottom, ring on the grip butt"

# ---------------------------------------------------------------------------
# v6（2026-11-15）：v5 被内容安全拦掉（request id 20261115104200256054668h6Mc5ryi），
# 推测触发点是 v5 新加的负面词与“禁止链条接触握柄”这类否定式句子。
# v6 改成**最小改动**：以已通过（v4）的文案为底，只把链条连接位置改成中性描述句，
# 负面词完全沿用 v4（不加新词）。
# ---------------------------------------------------------------------------
PROMPT_CN_V6 = PROMPT_CN_V4.replace(
    "不要画成长而宽的月牙镰刃。不要让钩口变成浅弧线。不要让链条从侧面像装饰挂件一样垂下。链条必须连接在钩背或钩根的受力连接环上，连接位置要靠近钩头结构中心。",
    "不要画成长而宽的月牙镰刃。不要让钩口变成浅弧线。链条由钩背外弧上一个明显高于握柄的受力连接环承载，"
    "最上面一枚链环的高度与钩喉齐平，整条链沿握柄一侧向下垂落。握柄底端是一个封死的钢帽。",
).replace(
    "从钩背受力连接环垂下一条自然略弯的铁链",
    "从钩背高处的受力连接环垂下一条自然略弯的铁链",
)

PROMPT_EN_V6 = PROMPT_EN_V4.replace(
    "Do not make a long, broad crescent farming blade. Do not make a shallow decorative curve. Do not let the chain hang from the side like jewelry. The chain must attach to a load-bearing connector on the hook back or hook root, close to the structural center of the hook head.",
    "Do not make a long, broad crescent farming blade. Do not make a shallow decorative curve. "
    "The chain is carried by a load-bearing ring on the outer spine of the hook head, set well above the grip: the uppermost link sits level with the hook throat and the chain falls down alongside the grip. The grip ends in a plain closed steel cap.",
).replace(
    "From the load-bearing connector on the hook back, show one naturally curved chain with eight or nine closed oval links.",
    "From that ring high on the hook back, show one naturally curved chain with eight or nine closed oval links.",
)

# ---------------------------------------------------------------------------
# v7（2026-11-15）：按 plan §5.18「尾部连接环与可读运动轨迹」新方案重做。
# 关键结构变化：链条挂在**整件武器的尾部大连接环**（握柄末端），而不是钩头/钩背；
# 构图从「竖直正立 + 链下垂」改成「轻微三分之四功能姿态」，让前后关系可读。
# 参考图仍是三张项目自产画风图（色卡 / 木料铁箍 / 钢刃），不上传 PBZ 锁镰图（实测会被拦）。
# ---------------------------------------------------------------------------
PROMPT_CN_V7 = """为高清像素化三国战场动作游戏设计一件能够抛出、勾住远处敌人并把敌人拉回的追命锁链武器概念图。

这是一件尾部拖链的投掷锁钩，不是农具镰刀，不是挂链的镰刀，也不是装饰性长柄武器。画面只展示一件完整武器，使用轻微三分之四功能姿态，让武器的前后关系清楚可读。

空间顺序必须明确：锁钩头位于飞行前端，短握柄位于中间，尾部大连接环位于整件武器的最后端，铁链从这个尾部大连接环继续向后延伸，方向朝向玩家手部。大连接环绝不能出现在钩头、钩背、钩根或刃根附近，也不能像饰品一样挂在刃头旁边。

锁钩头是第一视觉主体。它由厚重、结实、具有立体体积的明亮蓝灰钢制成，具有清晰张开的内凹捕捉喉部，能够卡住敌人的腰腹、四肢或护甲边缘。内侧是一条连续锋利的银白刃缘。钩尖向内并向后回弯，形成止脱倒钩，使钩住目标后沿链条收回时不容易脱开。钩尖、捕捉喉部和尾部链环之间要形成清楚的连续受力方向。

短握柄位于锁钩头和尾环之间，不能做成长木杆。握柄由少量暖棕木、钢制承托件和暖红褐皮革组成，木料占比明显低于已经确定的巨斧。只保留少量皮革包覆和一到两个暗铁识别件，不要大量挂件、布条或珠宝。

尾部大连接环必须厚实、明显、位于握柄末端，作为铁链和整件武器的后端受力锚点。铁链从尾环向后拖行，显示八到九枚闭合椭圆链环。每枚链环中空、有细但可读的厚度，彼此交叠并交替朝向，读成真实铁链。链条略带自然弧度，不能笔直绷紧，不能打结，不能像绳子或黑色长条。链条是武器尾部的拖链，不是钩头装饰。

配色必须和巨斧区分：不要长木柄，不要大面积暖棕木，也不要整件灰蒙低级铁色。锁钩使用明亮冷蓝灰钢和深蓝青暗面，内刃和链环受光使用银白到银灰色，短握柄只使用少量暖棕木，皮革使用清楚的暖红褐色，可有极少量暗朱红或深青识别色。整体明亮、高对比、色块清晰，但不使用金色、黄铜、铜色或鎏金，也不做脏黑锈铁。

背景为纯白或非常浅的中性背景，只有轻微柔和投影。不要人物、手部、敌人、战场、文字、箭头、UI、魔法光效、火焰或第二件武器。武器完整入画，不裁切，不让链条和钩头重叠到无法判断连接关系。"""

PROMPT_EN_V7 = """Design a throwable chain-pull weapon for a high-definition pixelated Three Kingdoms battlefield action game. It must be a rear-chain grappling weapon that can fly forward, catch a distant enemy, and pull the enemy back.

This is a grappling hook with a chain trailing from its rear, not a sickle with a chain hanging from the blade, not a farming scythe, and not a decorative long-handled weapon. Show one complete weapon in a slight three-quarter functional pose so its front and rear relationship is unmistakable.

The spatial order is mandatory: the hook head is the forward projectile end, the short grip is in the middle, the large tail connector ring is at the far rear end of the entire weapon, and the chain extends backward from that tail ring toward the player's hand. The large connector ring must not be near the hook head, hook back, hook root, or blade root. It must not look like an ornament hanging beside the blade.

The compact grappling hook head is the primary visual subject. It is made of thick, solid, bright cold blue-gray steel with a clearly open inward catching throat that can catch an enemy's waist, limb, or armor edge. The inner edge is one continuous sharp silver-white cutting edge. The hook tip curves inward and backward, forming a retaining barb so the hook resists slipping out while the chain retrieves it. The hook tip, catching throat, and rear chain ring must form one clear continuous load direction.

Between the hook head and the tail ring is a short grip, never a long wooden staff. Use a small amount of warm brown wood, steel support pieces, and clearly readable warm reddish-brown leather. The wood coverage must be visibly much lower than on the approved battle axe. Keep only minimal leather wrapping and one or two dark-iron identity pieces; no clusters of ornaments, cloth strips, or jewelry.

The large tail connector ring must be thick, clearly visible, and placed at the very end of the short grip. It is the rear load-bearing anchor for the chain and for the entire weapon. From this tail ring, show a chain trailing backward with eight or nine closed oval links. Each link is hollow, with a thin but readable wall, overlapping and alternating orientation. The chain has a slight natural curve, is not perfectly taut, is not knotted, and does not look like rope or a black bar. It is a rear trailing chain, not decoration on the hook head.

Make it visually distinct from the approved battle axe: no long wooden handle, no large warm-brown wood area, and no uniformly gray low-grade palette. Use bright cold blue-gray steel with deep blue-teal shadow planes for the hook, silver-white to silver-gray highlights on the inner edge and chain links, a small amount of warm brown wood, and clearly visible warm reddish-brown leather. A very small amount of dark vermilion or deep teal may be used as an identity accent. Keep the image bright and high contrast with clear color blocks, but use no gold, brass, copper, gilding, dirty black iron, or heavy rust.

Use a pure white or very light neutral background with only a subtle soft ground shadow. Do not include a person, hand, enemy, battlefield, text, arrows, UI, magic glow, flames, or another weapon. Keep the weapon complete and uncropped, and keep the chain and hook separated enough that the connection point is unmistakable."""

NEGATIVE_V7 = """chain attached to blade head, chain beside hook head, chain hanging from hook root, chain hanging from hook back, chain hanging as jewelry, head-mounted chain, blade-mounted chain, farming scythe, long crescent sickle, ordinary sickle, long wooden staff, oversized wood handle, decorative staff, sword, spear, axe, halberd, double blade, shallow hook, missing catching throat, missing retaining barb, missing tail ring, missing rear anchor, grip-end ornament mistaken for tail anchor, solid rope, black bar, tangled chain, knotted chain, perfectly taut chain, missing hollow links, excessive leather ornaments, beads, jewelry, hanging cloth strips, gold, brass, bronze, copper, gilded metal, yellow metal, uniformly gray, muddy low saturation, dirty black iron, heavy rust, black wood, black-brown leather, photorealism, sci-fi, plastic toy, magic glow, flames, smoke, character, hand, enemy, battlefield, scenery, text, arrows, watermark, logo, cropped weapon, tilted weapon, multiple weapons, duplicate weapon"""

# ---------------------------------------------------------------------------
# v7 探针（2026-11-15）：v7 被内容安全拦成 400（request id 20261115123603428019701NHsI5YL9，
# 未生成、未计费）。按项目既定方法做隔离实验：同参考图、同造型描述，只改指令段。
#
#   v7p0 = 造型正文逐字不动，只把负面词换成中性最小集。
#          200 → 触发点在 §5.18.7 的负面词（“chain attached to X / … mistaken for …”类）；
#          400 → 触发点在造型正文里的否定式句子。
#   v7p1 = 在 v7p0 基础上，把正文里的否定式句子改成中性描述（正文其余部分不动），
#          负面词仍是中性最小集。
#
# 依据：§5.14 经验——修正已通过方案时只留中性描述句，不加否定式补强。
# ---------------------------------------------------------------------------
NEGATIVE_V7_MIN = """character, hand, person, enemy, battlefield, scenery, text, arrows, watermark, logo, cropped weapon, tilted weapon, multiple weapons, duplicate weapon"""

# v7p2：p0/p1 都失败（且两者共享“负面词含 tilted weapon、正文却要求三分之四姿态”这一矛盾），
# 因此这里**只改这一个变量**：负面词去掉 tilted weapon，其余逐字沿用 §5.18 正文。
NEGATIVE_V7_MIN_NO_TILT = """character, hand, person, enemy, battlefield, scenery, text, arrows, watermark, logo, cropped weapon, multiple weapons, duplicate weapon"""

PROMPT_EN_V7_SOFT = PROMPT_EN_V7.replace(
    "This is a grappling hook with a chain trailing from its rear, not a sickle with a chain hanging from the blade, not a farming scythe, and not a decorative long-handled weapon.",
    "This is a grappling hook with a chain trailing from its rear. It reads as a thrown tool of war with a compact steel head and a short grip, rather than a farming tool or an ornamental long-handled prop.",
).replace(
    "The large connector ring must not be near the hook head, hook back, hook root, or blade root. It must not look like an ornament hanging beside the blade.",
    "The large connector ring sits at the far rear end of the weapon, clearly separated from the hook head, and it reads as a heavy load anchor rather than a decoration.",
).replace(
    "It is a rear trailing chain, not decoration on the hook head.",
    "The chain reads as a rear trailing chain carried by the weapon's rear anchor ring.",
)

PROMPT_CN_V7_SOFT = PROMPT_CN_V7.replace(
    "这是一件尾部拖链的投掷锁钩，不是农具镰刀，不是挂链的镰刀，也不是装饰性长柄武器。",
    "这是一件尾部拖链的投掷锁钩：紧凑钢制钩头 + 短握柄 + 尾部拖链，读起来是投掷用的战场工具，而不是农具或装饰性长柄道具。",
).replace(
    "大连接环绝不能出现在钩头、钩背、钩根或刃根附近，也不能像饰品一样挂在刃头旁边。",
    "大连接环位于整件武器的最末端，与钩头明显拉开距离，读起来是沉重的受力锚环，而不是装饰件。",
).replace(
    "链条是武器尾部的拖链，不是钩头装饰。",
    "链条读起来是由武器尾部锚环承载的拖链。",
)

# v7p4（2026-11-15）：§5.18 正文整段连续 4 次被拦（v7 / p0 / p1 / p2），
# 因此改为**以已验证可过的 v6 文案为底**，只把链条连接位置改成“握柄末端的尾部大连接环”：
#   ① 展示姿态仍用 v6 的竖直正立（不再要求三分之四，避免与负面词/姿态词冲突）；
#   ② 链条起点 = 握柄最末端的大连接环，位于钩头远端、明显低于钩喉；
#   ③ 负面词沿用 v4/v6 已知可过的集合。
# 仍完整保留：紧凑锁钩头 + 深喉 + 止脱倒钩 + 短分叉握柄 + 8–9 枚中空链环 + 冷蓝灰钢/银白刃缘/红褐皮革配色。
PROMPT_EN_V7_TAIL = PROMPT_EN_V6.replace(
    "Show one complete weapon standing vertically, centered and fully inside the frame. The weapon consists of a compact metal grappling hook head, a short forked grip, and one naturally hanging chain. The grappling hook head is the primary visual identity and must be more prominent than the grip and chain.",
    "Show one complete weapon standing vertically, centered and fully inside the frame. The weapon consists of three separated parts along one line: a compact metal grappling hook head at the top, a short forked grip in the middle, and one chain that continues downward from the large connector ring at the very bottom end of the grip. The grappling hook head is the primary visual identity and must be more prominent than the grip and chain.",
).replace(
    "The chain is carried by a load-bearing ring on the outer spine of the hook head, set well above the grip: the uppermost link sits level with the hook throat and the chain falls down alongside the grip. The grip ends in a plain closed steel cap.",
    "The chain is carried by one large connector ring placed at the very bottom end of the short grip. That ring sits well below the hook head, far under the hook throat, and the whole chain hangs straight down from it, clearly separated from the hook head and from the grip.",
).replace(
    "From that ring high on the hook back, show one naturally curved chain with eight or nine closed oval links.",
    "From the large connector ring at the bottom end of the grip, show one naturally curved chain with eight or nine closed oval links.",
)

PROMPT_CN_V7_TAIL = PROMPT_CN_V6.replace(
    "画面只展示一件完整武器，竖直正立，居中且完整入画。武器由上方的紧凑金属锁钩、短分叉握柄和一条自然下垂的铁链组成。锁钩是第一识别点，必须比握柄和链条更醒目。",
    "画面只展示一件完整武器，竖直正立，居中且完整入画。武器由沿一条直线排开、彼此分离的三个部分组成：最上方的紧凑金属锁钩、中段的短分叉握柄，以及从握柄最末端的大连接环继续向下延伸的铁链。锁钩是第一识别点，必须比握柄和链条更醒目。",
).replace(
    "链条由钩背外弧上一个明显高于握柄的受力连接环承载，最上面一枚链环的高度与钩喉齐平，整条链沿握柄一侧向下垂落。握柄底端是一个封死的钢帽。",
    "铁链由握柄最末端的一个大连接环承载。这个连接环位于钩头远端、明显低于钩喉，整条链从它笔直向下垂落，与钩头和握柄保持清楚的间隔。",
).replace(
    "从钩背高处的受力连接环垂下一条自然略弯的铁链",
    "从握柄末端的大连接环垂下一条自然略弯的铁链",
)

# ---------------------------------------------------------------------------
# v8（2026-11-15）：§5.18 正文在 edits 与 generations 两个通道**连续 6 次**被内容安全拦掉
# （v7 / p0 / p1 / p2 / p4 / generations-v7），且逐字重放今天 18:46 成功过的 v6 提示词
# （v6replay）也被拦 —— 说明是措辞 + 通道当时的拦截状态叠加，不是单一参考图问题。
# v8 改用本项目历史上稳定通过的“幻想游戏道具”框架（见 v2 / probe2 的成功经验），
# 删掉“抛出 / 抓住敌人 / 拉回 / 玩家手部 / 空间顺序必须”等叙事与否定句，
# 只用**几何结构描述**表达 §5.18 的核心要求：
#   钩头（上） → 短分叉握柄（中） → 握柄末端大连接环（下）→ 8–9 枚中空链环继续向下拖出。
# ---------------------------------------------------------------------------
PROMPT_CN_V8 = """为一款高清像素化三国战场动作游戏设计一件幻想游戏道具武器的概念图：一件尾部拖链的锁钩兵器。

画面只展示一件完整武器，竖直正立，居中且完整入画。整件武器沿一条直线排开，清楚地分成前后三段：最上方是紧凑的金属锁钩头，中间是短分叉握柄，最下方是握柄末端的大连接环以及从它继续向下拖出的铁链。三段之间的距离清楚，读起来是同一条连续结构。

锁钩头是第一视觉主体。它由厚重、结实、有立体体积的明亮冷蓝灰钢制成，有一个明确张开的内凹钩口，钩口内侧是一条连续锋利的亮银白刃缘。钩尖向内回弯成一个止脱小倒钩。钩头整体紧凑、有体积，不是宽大的月牙形薄片。

短分叉握柄位于钩头和尾环之间，长度短，结构紧凑。两支短钢骨从钩头根部向下分开一小段，随后合拢成一个握持段。握柄只用少量暖棕木、钢制承托件和清楚的暖红褐色皮革包覆，木料占比明显低于大型战斧。皮革只包一小段，不挂大量布条或首饰。

大连接环位于握柄的最末端，明显低于钩头，是一个厚实、开口很大的金属环，充当铁链的受力锚点。铁链从这一个环开始向下延伸，画面中可见八到九枚闭合的椭圆链环。每枚链环都是有薄壁的中空环，彼此交叠、角度交替，带一点自然弧度，松松地垂下，既不笔直绷紧也不打结。链尾收在一个稍大的椭圆环上。

配色明亮、高对比、色块清晰：锁钩与连接环用明亮的冷蓝灰钢，暗面用深蓝青，钩内刃缘和链环受光用银白到银灰色，握柄只用少量暖棕木，皮革用清楚的暖红褐色。整体不要灰蒙蒙的低饱和，也不要脏黑锈铁，不要金色、黄铜或铜色。

使用干净的纯白背景，只有极轻微的柔和投影。画面只出现这一件武器，不出现人物、手部、场景、文字、尺寸标注、UI、魔法光效或火焰。武器完整入画、不裁切、不倾斜，钩头与链条之间留出清楚的间隔，让连接关系一眼可读。"""

PROMPT_EN_V8 = """Design a fantasy video game prop weapon for a high-definition pixelated Three Kingdoms battlefield action game: a rear-chain grappling weapon.

Show one complete weapon standing vertically, centered and fully inside the frame. The whole weapon is arranged along one line and reads as three clearly separated parts in sequence: a compact metal hook head at the top, a short forked grip in the middle, and at the very bottom a large connector ring at the end of the grip with a chain continuing downward from it. The three parts are spaced apart clearly and read as one continuous structure.

The hook head is the primary visual subject. It is made of thick, solid, bright cold blue-gray steel with real volume, and it has a clearly open inward hook throat whose inner side carries one continuous sharp bright silver-white edge. The hook tip curves inward into a small retaining barb. The head is compact and solid, not a broad thin crescent sheet.

A short forked grip sits between the head and the ring, and it is short and compact. Two short steel branches split a little below the hook root and then close into one gripping section. The grip uses only a small amount of warm brown wood, steel support pieces and clearly readable warm reddish-brown leather, and the wood coverage is visibly much lower than on a large battle axe. The leather covers only a short section, with no clusters of cloth strips or jewelry.

The large connector ring sits at the very end of the grip, clearly below the hook head. It is a thick metal ring with a wide opening and it acts as the load anchor for the chain. The chain begins at that one ring and extends downward, showing eight or nine closed oval links. Every link is a hollow ring with a thin wall, overlapping one another and alternating its angle, with a slight natural curve, hanging loosely, neither perfectly straight and taut nor knotted. The chain tail ends in one slightly larger oval ring.

Keep the image bright, high contrast and clearly color-blocked: the hook and the ring use bright cold blue-gray steel with deep blue-teal shadow planes, the inner edge of the hook and the lit edges of the chain links use silver-white to silver-gray, the grip uses only a small amount of warm brown wood, and the leather is a clear warm reddish-brown. Avoid a dull low-saturation gray look, avoid dirty black rusted iron, and use no gold, brass or copper.

Present the weapon on a clean pure white background with only a very subtle soft ground shadow. The picture contains this one weapon only: no person, no hand, no scenery, no text, no dimension labels, no UI, no magic glow and no flames. Keep the weapon complete, uncropped and upright, and keep clear space between the hook head and the chain so the connection reads at a glance."""

NEGATIVE_V8 = """character, hand, person, scenery, battlefield, text, watermark, logo, cropped weapon, multiple weapons, duplicate weapon, photorealism, plastic toy, magic glow, flames, smoke"""

# ---------------------------------------------------------------------------
# v9（2026-11-15）：按 plan §5.20 新方案逐字提交——“v4 锁镰头回归 + 尾部链环修正”。
#   形制：长而开放的弯钩镰头 + 内侧银白刃缘 + 镰根缺口爪形件 + 分叉握杆 + 握柄末端大连接环 + 8–9 环拖链。
#   参考图：三张项目自产画风参考（色卡 / 木料铁箍 / 钢刃），不上传 PBZ 锁镰图、不上传 v8。
#   通道：edits（带参考图，quality=max / input_fidelity=high / 1024x1536 / n=1）。
# v10（备用）：若 v9 被内容安全拦（§5.19 记录：拦截状态会变，且“否定式补强 + chain attached to …”
#   类负面词是高风险成分），改用 v8 已验证可过的“幻想游戏道具 + 纯几何描述”框架，
#   结构要求与 v9 一致（§5.20.2 / §5.20.8 逐条保留）。
# ---------------------------------------------------------------------------
PROMPT_CN_V9 = """为高清像素化三国战场动作游戏设计一件追命锁镰概念图。它是一把带尾部拖链的弯钩镰，不是闭合圆形抓钩。

最重要的造型身份是锁镰头：镰头必须保持长而开放的弯钩镰剪影，像一段厚重的单刃弯曲镰体。镰尖朝上并向内回弯，内侧是一条连续、清晰、亮银白的锋利刃缘。镰头外侧是厚重的蓝灰钢体，但弯曲开口必须明显，不能收成闭合的 C 形金属环，不能变成圆形抓钩。

镰刃根部保留一个短小、向上扬起、带缺口的爪形钢件。它是镰头结构的一部分，不是兽头雕像。镰根下方必须有清楚的分叉握杆：两支短钢制承托杆从镰根下方分开，随后汇入一段短握持段。分叉结构要像锁镰的握杆，不要画成长木柄。

链条的连接位置必须与镰头分开。整件武器的空间顺序是：前端长弯钩镰头，中间分叉握杆，最后端握柄尾部的大连接环，链条从这个尾部大连接环继续向后下方拖出。尾部大连接环是链条唯一的连接点；它不在镰头、镰背、镰根或刃根附近。镰头旁边不要出现任何链环。

尾部链条显示约八到九枚闭合椭圆链环。每枚链环中空、有细但可读的厚度，彼此交叠并交替朝向，读成真实铁链。链条略带自然弧度并向后拖行，不要笔直悬挂，不要打结，不要画成绳子或黑色长条。链尾有一个稍大的连接环。

使用轻微三分之四的功能展示姿态，让前后关系清楚：长弯钩镰头位于飞行前端，短分叉握杆位于中间，尾环位于最后端，链条拖在尾环后方。不要让链条遮住镰头内刃和弯曲开口。

这件武器必须和已经确定的巨斧明显区分：不要长木柄，不要大面积暖棕木，不要厚板斧头。锁镰主体使用明亮冷蓝灰钢和深蓝青暗面，内侧刃缘和链环受光使用银白到银灰色，握杆只使用少量暖棕木，皮革使用清楚的暖红褐色。整体明亮、高对比、色块清晰，但不使用金色、黄铜、铜色或鎏金，也不做灰蒙低饱和或脏黑锈铁。

背景为纯白或非常浅的中性背景，只有轻微柔和投影。只展示一件完整武器，不要人物、手部、敌人、战场、文字、箭头、UI、魔法光效、火焰或第二件武器。武器完整入画，不裁切。"""

PROMPT_EN_V9 = """Design a chain sickle for a high-definition pixelated Three Kingdoms battlefield action game. It is an open hook-bladed sickle with a chain trailing from its butt end, not a closed circular grappling hook.

The primary identity is the sickle head. Keep a long, open, curved single-edged sickle silhouette, like a substantial forged hook blade. The tip points upward and curves inward. The inner side has one continuous, crisp bright silver-white cutting edge. The outside is thick blue-gray steel, but the opening must remain clearly open. Do not close it into a circular C-shaped metal hook and do not turn it into a round grappling hook.

At the blade root, keep one short upward-facing notched claw made of forged steel. It is part of the sickle construction, not an animal-head sculpture. Directly below the blade root, show a clearly forked sickle handle: two short steel support branches split below the root and then converge into a short grip. It must read as a chain sickle handle, not a long wooden staff.

Keep the chain attachment separate from the sickle head. The spatial order is: the long curved sickle head at the forward end, the forked handle in the middle, a large connector ring at the very bottom butt end of the grip, and the chain trailing backward and downward from that tail ring. The tail connector ring is the only chain attachment point. It is not near the sickle head, blade back, blade root, or cutting edge. No chain links appear beside the sickle head.

Show about eight or nine closed oval chain links from the tail connector ring. Every link is hollow with a thin but readable wall, overlapping and alternating orientation so it reads as a real chain. The chain has a slight natural curve and trails behind the tail ring. It is not a straight hanging rope, not knotted, and not a black bar. End it with one slightly larger connector ring.

Use a slight three-quarter functional presentation so the front-to-rear relationship is clear: the long curved sickle head is the forward projectile end, the short forked grip is in the middle, the tail ring is at the far rear, and the chain trails behind it. Do not let the chain cover the sickle's inner edge or open curve.

Make it clearly different from the approved battle axe: no long wooden handle, no large warm-brown wood area, and no thick plate axe head. Use bright cold blue-gray steel with deep blue-teal shadow planes for the sickle, silver-white to silver-gray highlights on the inner edge and chain links, a small amount of warm brown wood, and clearly visible warm reddish-brown leather. Keep it bright, high contrast, and clearly color-blocked, with no gold, brass, copper, gilding, muddy low saturation, dirty black iron, or heavy rust.

Use a pure white or very light neutral background with only a subtle soft ground shadow. Show one complete weapon only. Do not include a person, hand, enemy, battlefield, text, arrows, UI, magic glow, flames, or another weapon. Keep the weapon complete and uncropped."""

NEGATIVE_V9 = """closed circular grappling hook, round C-shaped hook, compact grappling hook, pure hook, hook-only weapon, long wooden staff, oversized wood handle, broad axe head, farming scythe, straight scythe, double blade, sword, spear, axe, halberd, chain beside sickle head, chain attached to blade root, chain attached to blade back, head-mounted chain, blade-mounted chain, missing open sickle curve, missing inner cutting edge, missing upward sickle tip, missing forked handle, missing tail ring, solid rope, black bar, tangled chain, knotted chain, perfectly taut chain, missing hollow links, excessive ornaments, beads, jewelry, hanging cloth strips, gold, brass, bronze, copper, gilded metal, yellow metal, uniformly gray, muddy low saturation, dirty black iron, heavy rust, black wood, black-brown leather, photorealism, sci-fi, plastic toy, magic glow, flames, smoke, character, hand, enemy, battlefield, scenery, text, watermark, logo, cropped weapon, tilted weapon, multiple weapons, duplicate weapon"""

# v10：§5.20 结构要求（长开放弯钩镰头 / 内侧银白刃缘 / 镰根缺口爪形件 / 分叉握杆 /
# 握柄末端唯一尾环 / 8–9 环拖链 / 冷蓝灰钢配色）逐条保留，但改成“幻想游戏道具 + 纯几何描述”的
# 框架：删掉否定句与叙事句，负面词只留中性最小集（§5.19 / skill 第七轮已验证可过）。
PROMPT_CN_V10 = """为一款高清像素化三国战场动作游戏设计一件幻想游戏道具武器的概念图：一把尾部拖链的弯钩镰。

这把武器沿一条直线排开，清楚地分成前后三段：最前方是长而开放的弯钩镰头，中间是短分叉握杆，最后方是握持段末端的大连接环，以及从它继续向后下方拖出的铁链。三段之间的间隔清楚，读起来是同一条连续结构。

镰头是第一视觉主体。它的剪影长而开放，像一段厚重的单刃弯曲镰体：镰尖朝上并向内回弯，内侧是一条连续、清晰、亮银白的锋利刃缘，外侧是厚重的明亮蓝灰钢体，弯曲的开口贯穿镰体的大部分弧线，钩口保持敞开。

镰刃根部有一个短小、向上扬起、带缺口的爪形钢件，读起来是锻造出来的金属配件。镰根正下方是清楚的分叉握杆：两支短钢制承托杆从镰根下方分开，随后汇入一段短握持段。握杆只用少量暖棕木、钢件和清楚的暖红褐色皮革，木料占比很低。

大连接环位于握持段的最末端，是一个厚实、开口很大的金属环，充当铁链唯一的受力锚点。铁链从这一个环开始向后下方拖出，画面中可见八到九枚闭合椭圆链环。每枚链环都是有薄壁的中空环，彼此交叠、角度交替，带一点自然弧度，链尾收在一个稍大的椭圆环上。

配色明亮、高对比、色块清晰：镰体和连接环用明亮的冷蓝灰钢，暗面用深蓝青，内侧刃缘和链环受光用银白到银灰色，握杆只用少量暖棕木，皮革用清楚的暖红褐色。

使用干净的纯白背景，只有极轻微的柔和投影。画面只出现这一件武器，不出现人物、手部、场景、文字、尺寸标注、UI、魔法光效或火焰。武器完整入画、不裁切，镰头与链条之间留出清楚的间隔。"""

PROMPT_EN_V10 = """Design a fantasy video game prop weapon for a high-definition pixelated Three Kingdoms battlefield action game: an open hook-bladed sickle with a chain trailing from its butt end.

The weapon is arranged along one line and reads as three clearly separated parts in sequence: a long open hook-bladed sickle head at the front, a short forked grip in the middle, and at the far rear a large connector ring at the end of the grip with a chain trailing backward and downward from it. The gaps between the three parts are clear and they read as one continuous structure.

The sickle head is the primary visual subject. Its silhouette is long and open, like a substantial forged hook blade: the tip points upward and curves inward, the inner side carries one continuous crisp bright silver-white cutting edge, the outside is thick bright blue-gray steel, and the curved opening runs through most of the arc so the mouth of the hook stays wide open.

At the blade root there is one short upward-facing notched claw of forged steel that reads as a metal fitting. Directly below the blade root is a clearly forked handle: two short steel support branches split below the root and then converge into a short gripping section. The handle uses only a small amount of warm brown wood, steel pieces and clearly readable warm reddish-brown leather, and the wood coverage stays low.

The large connector ring sits at the very end of the gripping section. It is a thick metal ring with a wide opening and it is the single load anchor for the chain. The chain begins at that one ring and trails backward and downward, showing eight or nine closed oval links. Every link is a hollow ring with a thin but readable wall, overlapping one another and alternating its angle, and the chain tail ends in one slightly larger oval ring.

Keep the image bright, high contrast and clearly color-blocked: the sickle and the ring use bright cold blue-gray steel with deep blue-teal shadow planes, the inner cutting edge and the lit edges of the chain links use silver-white to silver-gray, the grip uses only a small amount of warm brown wood, and the leather is a clear warm reddish-brown.

Present the weapon on a clean pure white background with only a very subtle soft ground shadow. The picture contains this one weapon only: no person, no hand, no scenery, no text, no dimension labels, no UI, no magic glow and no flames. Keep the weapon complete and uncropped, and keep clear space between the sickle head and the chain."""

NEGATIVE_V10 = """character, hand, person, scenery, battlefield, text, watermark, logo, cropped weapon, multiple weapons, duplicate weapon, photorealism, plastic toy, magic glow, flames, smoke"""

# ---------------------------------------------------------------------------
# v11（2026-11-15）：按 plan §5.22「以 v4 原图为造型锁定，仅迁移尾部链环」。
#   image[] 顺序：v4 原图（唯一造型基准）+ 色卡 + 木料铁箍 + 钢刃。
#   接口：edits / quality=max / input_fidelity=high / 1024x1536 / n=1 / 不传 background。
# v12（备用）：仅在 v11 被拦时使用——**输入图不变**（仍以 v4 为首图），
#   只把提示词改写成 §5.21 已验证可过的“幻想游戏道具 + 正向几何描述”框架。
#   （§5.22.2 禁止的是“只上传画风图”这种降级，不是禁止改写措辞。）
# ---------------------------------------------------------------------------
V4_LOCK = "C:/Users/steam/Pictures/gptGen/chain_sickle_concept_20261115/chain_sickle_concept_v1_v4.png"

REFS_V4LOCK = [
    (V4_LOCK, "image 1: the only shape authority - keep the sickle head, head proportions, inner cutting edge, tip direction, notched blade-root claw, forked support branches, short grip, material divisions and overall silhouette"),
    (r"SkillWeaponsArtSource/01_Reference/style/game_palette_board.png",
     "image 2: only the project color relationships"),
    (r"SkillWeaponsArtSource/01_Reference/style/game_wood_iron_ref.png",
     "image 3: only the material language for wood, leather and iron collars"),
    (r"SkillWeaponsArtSource/01_Reference/style/game_saber_ref.png",
     "image 4: only the silver-white cutting edge and clean metal highlight language"),
]

PROMPT_CN_V11 = """请对第一张已提供的锁镰概念图做一次定点结构编辑。第一张图是唯一的造型基准，必须保持它的镰头形状、镰头比例、内侧刃缘、镰尖方向、镰根爪形件、分叉承托杆、短握持段、材质分区和整体轮廓。第二至第四张图只参考项目配色、皮革/木料/铁箍材质和银白金属受光，不参考它们的武器形状。

只做一项结构调整：把铁链的连接位置改到整件武器的尾部。尾部定义为短握持段的最下端、与镰头相反的一端。请在握持段末端新增一个厚实、清楚的大连接环，铁链从这个尾部连接环继续向后下方拖出。链条连接环属于武器后端，不属于镰头、镰背、镰根或刃根。

链条显示约八到九枚闭合椭圆链环，每枚链环中空、有细但可读的厚度，彼此交叠并交替朝向，形成自然略弯的真实铁链，链尾有一个稍大的末端环。链条应从握持段尾部延伸，不要让任何链环出现在镰头旁边。

除链条连接位置和新增尾部连接环外，不得重新设计或重绘武器。必须保留第一张图的长而开放的弯钩镰头：镰尖朝上并向内回弯，内侧连续银白刃缘，镰根上扬缺口爪形钢件，以及镰根下方两支清楚的分叉承托杆。不得把镰头改成闭合圆形 C 钩、纯抓钩、短钩或另一种月牙武器。不得改变分叉握杆的结构，不得把短握柄改成长木柄。

保持第一张图的高清像素化蓝灰钢与银白刃缘风格。尾环和链环使用明亮蓝灰钢与银灰受光，短握柄保留少量暖棕木和暖红褐皮革。不要加入金色、黄铜、铜色、鎏金、灰蒙低饱和或脏黑锈铁。

保持原图的干净浅色背景和完整入画。不要加入人物、手部、敌人、文字、箭头、UI、魔法光效或第二件武器。"""

PROMPT_EN_V11 = """Perform one targeted structural edit on the first uploaded chain-sickle concept image. The first image is the only shape authority. Preserve its sickle-head shape, head proportions, inner cutting edge, tip direction, notched blade-root claw, forked support branches, short grip, material divisions, and overall silhouette. Images 2 to 4 are only for this project's colors, leather/wood/iron material language, and silver-white metal highlights; do not use their weapon shapes.

Make only one structural change: move the chain attachment to the far butt end of the entire weapon. The butt end is the bottom end of the short grip, opposite the sickle head. Add one thick, clearly readable large connector ring at the end of the grip, and let the chain trail backward and downward from that tail ring. The tail connector belongs to the rear of the weapon, not to the sickle head, blade back, blade root, or cutting edge.

Show about eight or nine closed oval chain links. Every link is hollow with a thin but readable wall, overlapping and alternating orientation to form a real chain with a slight natural curve. End it with one slightly larger tail ring. The chain must extend from the butt end of the grip; no chain links appear beside the sickle head.

Except for relocating the chain attachment and adding the tail connector ring, do not redesign or redraw the weapon. Preserve the first image's long, open hook-bladed sickle head: the tip points upward and curves inward, the inner side has one continuous silver-white cutting edge, the blade root has the upward notched claw-like steel fitting, and two clear forked support branches remain below the blade root. Do not turn the sickle head into a closed circular C-hook, a pure grappling hook, a short hook, or a different crescent weapon. Do not change the forked grip into a long wooden staff.

Preserve the first image's high-definition pixelated blue-gray steel and silver-white edge language. Use bright blue-gray steel with silver-gray highlights for the new tail ring and chain links, while keeping the short grip's small amount of warm brown wood and warm reddish-brown leather. Do not add gold, brass, copper, gilding, muddy low saturation, or dirty black rusted iron.

Keep the clean light background and the complete uncropped composition from the first image. Do not add a person, hand, enemy, text, arrows, UI, magic glow, or another weapon."""

NEGATIVE_V11 = """closed circular grappling hook, pure hook, hook-only redesign, changed sickle head, changed blade silhouette, shortened blade, new weapon shape, missing open sickle curve, missing inner cutting edge, missing upward tip, missing blade-root claw, missing forked support branches, chain beside sickle head, chain attached to blade root, chain attached to blade back, head-mounted chain, blade-mounted chain, long wooden staff, oversized wood handle, broad axe head, gold, brass, bronze, copper, gilded metal, yellow metal, uniformly gray, muddy low saturation, dirty black iron, heavy rust, black wood, black-brown leather, excessive ornaments, beads, jewelry, text, watermark, logo, cropped weapon, multiple weapons, duplicate weapon"""

# v12：输入图不变（v4 为首图），只把提示词改成正向几何描述框架（§5.21 已验证可过）。
PROMPT_CN_V12 = """请对已提供的锁镰概念图做一次结构编辑，输入图与参考图的职责分开：第一张图负责造型，后三张图只负责配色、木料皮革铁箍材质和银白金属受光。

以第一张图的镰头为造型基准：长而开放的弯钩镰体、镰尖朝上并向内回弯、内侧一条连续银白刃缘、镰根处向上扬起的缺口爪形钢件、镰根下方两支分叉承托杆、短握持段与红褐皮革包覆。这些部件的形状、比例和位置保持原样。

本轮只调整一处：整件武器沿一条直线排成三段——最上方是镰头，中间是短握持段，最下方是握持段末端的一个厚实大连接环，铁链从这个尾部大连接环向后下方拖出。链条显示八到九枚闭合椭圆链环，每枚链环中空、有细但可读的厚度，彼此交叠并交替朝向，形成自然略弯的真实铁链，链尾收在一个稍大的末端环。

配色保持第一张图的像素化蓝灰钢与银白刃缘，尾环和链环用明亮蓝灰钢与银灰受光，握柄保留少量暖棕木与暖红褐皮革。背景保持干净浅色，武器完整入画。画面里只有这一件武器。

以上几何结构与材质分区按第一张图呈现即可。"""

PROMPT_EN_V12 = """Perform a structural edit on the provided chain-sickle concept image. The inputs have separate jobs: the first image carries the shape, and the following three images only carry this project's colors, the wood/leather/iron-collar material language, and the silver-white metal highlights.

Use the sickle head of the first image as the shape basis: a long open hook-bladed body whose tip points upward and curves inward, one continuous silver-white cutting edge on the inner side, an upward-facing notched claw of steel at the blade root, two forked support branches below the blade root, and a short gripping section with reddish-brown leather wrap. Those parts keep their existing shape, proportions and placement.

This round adjusts one thing: the weapon is arranged along one line as three parts - the sickle head at the top, the short gripping section in the middle, and at the bottom one thick large connector ring at the end of the gripping section, with the chain trailing backward and downward from that tail ring. The chain shows eight or nine closed oval links, every link hollow with a thin but readable wall, overlapping one another and alternating orientation so it reads as a real chain with a slight natural curve, ending in one slightly larger tail ring.

Keep the pixelated blue-gray steel and silver-white edge language of the first image, with bright blue-gray steel and silver-gray highlights on the tail ring and chain links, and a small amount of warm brown wood plus warm reddish-brown leather on the grip. Keep the clean light background and the complete uncropped composition. The picture shows this one weapon only."""

NEGATIVE_V12 = """character, hand, person, enemy, scenery, battlefield, text, arrows, watermark, logo, cropped weapon, multiple weapons, duplicate weapon, photorealism, plastic toy, magic glow, flames, smoke"""

# ---------------------------------------------------------------------------
# v13（2026-11-15）：按 plan §5.24「v4 镰头主体放大 + 紧凑拖链构图」原文逐字提交。
#   变化点：size 改为 1536x1024（宽幅）、构图改为紧凑斜向、镰头放大、链条压缩。
#   输入图不变：v4 原图（首图，唯一造型基准）+ 三张画风参考。
# v14（备用）：若 v13 被内容安全拦，**输入图与尺寸都不变**，只把提示词改成
#   §5.21 / §5.23 已验证可过的“正向几何描述”框架。
# ---------------------------------------------------------------------------
PROMPT_CN_V13 = """对第一张上传的 v4 锁镰概念图进行一次构图重排。第一张图是唯一造型基准，保持它的长弯钩镰头、镰尖方向、内侧连续银白刃缘、镰根上扬缺口爪、分叉承托杆、短握持段和尾部连接环结构。其它三张图只参考项目颜色、皮革/木料/铁箍材质和银白金属受光。

这次只调整构图比例，不重新设计武器。镰头必须是绝对主体，镰头视觉面积超过整条链条，镰头占画面高度约 50% 到 60%。不能把镰头缩小来容纳链条，不能让链条成为画面主体。

采用紧凑的横向或明显斜向功能构图，而不是竖直长条构图。让大而醒目的弯钩镰头位于画面左上或左侧，短握杆向右下延伸，尾部大连接环位于握杆最末端，链条从尾环向右下方短距离拖出并弯回。整件武器应像“镰头主体拖着一小段紧凑锁链”，而不是“长链条末端挂着小镰刀”。

保留约八到九枚真实闭合椭圆链环，但把它们压缩在短而紧凑的弧线或 S 形拖链路径中。链环彼此交叠、方向交替，中空和厚度清楚；不要让链条形成从画面上方一直垂到画面下方的长直线。链条可以沿后方弧线回绕以压缩总长度，但不能遮住镰头内刃、镰根爪件或分叉握杆。

必须保持 v4 的锁镰身份：长而开放的弯钩镰体，镰尖向上并向内回弯，内侧连续刃缘，镰根爪形钢件，镰根下方清楚的分叉承托杆。不要把镰头改成闭合圆钩、纯抓钩或短小钩子。

保持项目高清像素化蓝灰钢与银白刃缘。短握柄只保留少量暖棕木和暖红褐皮革；链环使用明亮蓝灰钢与银灰受光。整体明亮、高对比，不使用金色、黄铜、铜色、鎏金、灰蒙低饱和或脏黑锈铁。

使用宽幅纯白或非常浅的中性背景，轻微柔和投影。只展示一件完整武器，不要人物、手部、敌人、文字、箭头、UI、魔法光效或第二件武器。"""

PROMPT_EN_V13 = """Recompose the first uploaded v4 chain-sickle concept image. The first image is the only shape authority. Preserve its long curved sickle head, tip direction, continuous inner silver-white edge, upward notched blade-root claw, forked support branches, short grip, and rear connector ring. Images 2 to 4 are only for this project's colors and material language.

Change the composition and scale only; do not redesign the weapon. The sickle head must be the absolute primary subject. Its visual area must be larger than the entire chain, and the sickle head should occupy approximately 50 to 60 percent of the canvas height. Do not scale the sickle head down to fit the chain. Do not let the chain become the main subject.

Use a compact horizontal or strongly diagonal functional composition instead of a tall vertical layout. Place the large, dominant curved sickle head at the upper-left or left side. Let the short grip extend toward the lower-right. Place the large tail connector ring at the far end of the grip, and let the chain trail a short distance toward the lower-right before curving back in a compact path. The result must read as a large sickle head with a short trailing chain, not a small sickle hanging from a long chain.

Keep about eight or nine real closed oval chain links, but compress them into a short, compact curved or S-shaped trailing path. The links overlap and alternate orientation, with readable hollow centers and wall thickness. Do not make the chain a long straight line running from the top of the canvas to the bottom. The chain may curve back behind itself to reduce total length, but it must not cover the sickle's inner edge, blade-root claw, or forked grip.

Preserve the v4 chain-sickle identity: a long open curved sickle body, an upward and inward-curving tip, one continuous inner cutting edge, the notched blade-root claw, and clearly visible forked support branches below the root. Do not turn the head into a closed circular hook, a pure grappling hook, or a small short hook.

Preserve the project's pixelated blue-gray steel and silver-white edge language. Keep only a small amount of warm brown wood and warm reddish-brown leather on the short grip. Use bright blue-gray steel and silver-gray highlights on the chain links. Keep the image bright and high contrast with no gold, brass, copper, gilding, muddy low saturation, or dirty black rusted iron.

Use a wide pure white or very light neutral background with only a subtle soft ground shadow. Show one complete weapon only. Do not include a person, hand, enemy, text, arrows, UI, magic glow, flames, or another weapon."""

NEGATIVE_V13 = """small sickle head, tiny blade, long vertical chain dominating the image, chain longer than the weapon body, chain filling most of the canvas, straight hanging chain, small weapon at the end of a long chain, closed circular grappling hook, pure hook, hook-only redesign, changed sickle head, shortened blade, missing open sickle curve, missing inner cutting edge, missing upward tip, missing blade-root claw, missing forked support branches, chain beside sickle head, chain attached to blade root, long wooden staff, oversized wood handle, broad axe head, gold, brass, bronze, copper, gilded metal, yellow metal, uniformly gray, muddy low saturation, dirty black iron, heavy rust, black wood, black-brown leather, excessive ornaments, beads, jewelry, text, watermark, logo, cropped weapon, multiple weapons, duplicate weapon"""

# v14：输入图、尺寸不变（v4 首图 + 三张画风参考，1536x1024），提示词改成正向几何描述框架。
PROMPT_CN_V14 = """对已提供的 v4 锁镰概念图做一次构图重排。输入图职责分开：第一张图负责造型，后三张图只负责项目配色、皮革/木料/铁箍材质和银白金属受光。

第一张图里的镰头造型保持原样：长而开放的弯钩镰体、镰尖向上并向内回弯、内侧一条连续银白刃缘、镰根处向上扬起的缺口爪形钢件、镰根下方两支分叉承托杆、被红褐皮革包覆的短握持段，以及握持段末端的大连接环。这些部件的形状、比例和位置保持原样。

这一轮调整的是画面构图与比例：镰头是绝对主体，它的视觉面积大于整条链条，镰头占画面高度约五成到六成；镰头位于画面左上或左侧，短握杆向右下延伸，大连接环在握杆末端，链条从尾环向右下方拖出一小段后弯回成一条紧凑的弧线。整体是宽幅、斜向的构图，��件武器读起来是“大镰头带着一小段紧凑锁链”。

链条保留八到九枚闭合椭圆链环，环壁薄、中空清楚，彼此交叠、角度交替，沿那条短而紧凑的 S 形弧线排列。链条整体留在画面右下区域，镰头内刃、镰根爪件和分叉握杆保持完整可见。

配色保持第一张图的像素化蓝灰钢与银白刃缘，短握柄保留少量暖棕木与暖红褐皮革，链环用明亮蓝灰钢与银灰受光。背景是宽幅纯白，只有极轻微的柔和投影。画面里只有这一件武器。"""

PROMPT_EN_V14 = """Perform a recomposition of the provided v4 chain-sickle concept image. The inputs have separate jobs: the first image carries the shape, and the following three images carry this project's colors, the wood/leather/iron-collar material language, and the silver-white metal highlights.

Keep the sickle head from the first image: a long open curved sickle body, the tip pointing upward and curving inward, one continuous silver-white cutting edge on the inner side, the upward-facing notched claw of steel at the blade root, the two forked support branches below the root, the short gripping section with its reddish-brown leather wrap, and the large connector ring at the rear end of the grip. Those parts keep their existing shape, proportions and placement.

This round changes the layout and the scale of the picture. The sickle head is the primary subject and carries the largest visual area: it spans about fifty to sixty percent of the canvas height, and its area is larger than the area of the whole chain. The head sits at the upper left of the canvas, the short grip runs toward the lower right, the large connector ring sits at the far end of the grip, and the chain leaves that ring toward the lower right and bends back into a short compact path. The whole layout is wide and diagonal, and the weapon reads as a large sickle head with a short trailing chain.

The chain holds eight or nine closed oval links with thin walls and readable hollow centers, overlapping one another and alternating their angle, arranged along that short compact S-shaped arc. The chain path stays in the lower right region of the canvas, and the sickle's inner edge, the blade-root claw and the forked grip remain fully visible.

Keep the pixelated blue-gray steel and silver-white edge language of the first image, with a small amount of warm brown wood and warm reddish-brown leather on the short grip, and bright blue-gray steel with silver-gray highlights on the chain links. Keep the image bright and high contrast on a wide pure white background with only a very subtle soft ground shadow. The picture shows this one weapon only."""

NEGATIVE_V14 = """character, hand, person, enemy, scenery, battlefield, text, watermark, logo, cropped weapon, multiple weapons, duplicate weapon, photorealism, plastic toy, magic glow, flames, smoke"""

# ---------------------------------------------------------------------------
# v15（2026-11-15）：用户拍板「算了就这样吧，来个正式点的正面立绘图」。
#   ⇒ 停止构图迭代，改出一张**正面立绘**（竖幅 1024x1536，武器竖直正立、正面朝向镜头、完整入画）。
#   造型基准 = v13（用户认可的当前版本，镰头大、尾环在握柄末端、拖链）；
#   另附 v4 原图作为镰头细节的补充基准（§5.24 曾要求不与 v8–v12 混用，这里只补头部形制）。
#   参考图顺序：v13（首图，造型基准）→ v4（镰头细节基准）→ 色卡 → 木料铁箍 → 钢刃。
# ---------------------------------------------------------------------------
V13_LOCK = "C:/Users/steam/Pictures/gptGen/chain_sickle_concept_20261115/chain_sickle_concept_v1_v13_v13.png"

REFS_V15 = [
    (V13_LOCK, "image 1: the shape authority - the approved sickle head, forked support branches, short grip, rear connector ring and the whole weapon design"),
    (V4_LOCK, "image 2: the sickle head detail reference - long open curved blade, upward inward tip, inner silver-white edge, notched blade-root claw, forked branches"),
    (r"SkillWeaponsArtSource/01_Reference/style/game_palette_board.png",
     "image 3: only the project color relationships"),
    (r"SkillWeaponsArtSource/01_Reference/style/game_wood_iron_ref.png",
     "image 4: only the material language for wood, leather and iron collars"),
    (r"SkillWeaponsArtSource/01_Reference/style/game_saber_ref.png",
     "image 5: only the silver-white cutting edge and clean metal highlight language"),
]

PROMPT_CN_V15 = """为高清像素化三国战场动作游戏绘制一张正式的锁镰正面立绘图：第一张图是造型基准，第二张图只补充镰头细节，后三张图只负责项目配色、皮革/木料/铁箍材质和银白金属受光。

武器的设计与第一张图保持一致：长而开放的弯钩镰头，镰尖朝上并向内回弯，内侧一条连续银白刃缘，镰根处向上扬起的缺口爪形钢件，镰根下方两支分叉承托杆，被暖红褐皮革缠握的暖棕木短握持段，钢制环箍，以及握持段末端的大连接环。这些部件的形状、比例和材质分区都保持原样。

画面是一张干净的正立正面视图：武器竖直站在画面中央，正面朝向镜头，镰头在上、握柄居中、链条从握柄末端的大连接环向下垂落。整件武器完整入画，四周留出充足空白，像正式的装备立绘。

链条从尾部连接环自然略弯地向下垂落，可见八到九枚闭合椭圆链环，每枚链环中空、有细但可读的厚度，彼此交叠并交替朝向，链尾收在一个稍大的环上，位置在画面下部。

使用纯白背景，武器下方只有极轻微的柔和投影；光线均匀正式，轮廓清晰，像装备图册一样干净。材质用受控的像素色簇表现，不要照片写实。

配色保持第一张图：锁镰主体用明亮冷蓝灰钢与深蓝青暗面，内侧刃缘是全图最亮的银白，链环用明亮蓝灰钢与银灰受光，握柄用少量暖棕木与暖红褐皮革。整体明亮、高对比、色块清晰。"""

PROMPT_EN_V15 = """Draw a formal front-view presentation illustration of the chain sickle for a high-definition pixelated Three Kingdoms battlefield action game. The first uploaded image is the shape authority, the second image only adds sickle-head detail, and the following three images only carry this project's colors, the wood/leather/iron-collar material language, and the silver-white metal highlights.

Keep the weapon's design as in the first image: the long open curved sickle head whose tip points upward and curves inward, one continuous silver-white cutting edge on the inner side, the upward-facing notched claw of steel at the blade root, the two forked support branches below the root, the short gripping section of warm brown wood wrapped in warm reddish-brown leather, the steel collars, and the large connector ring at the rear end of the grip. Those parts keep their shape, proportions and material divisions.

The picture is a clean upright front elevation: the weapon stands vertically in the center of the canvas, seen straight on, with the sickle head at the top, the grip in the middle, and the chain hanging down from the large connector ring at the end of the grip. The whole weapon fits inside the frame with generous margins on every side, presented like a formal equipment sheet.

The chain hangs from the tail ring in a natural slight curve and shows eight or nine closed oval links, each one hollow with a thin but readable wall, overlapping one another and alternating orientation, ending in one slightly larger ring near the bottom of the frame.

Use a pure white background with only a very subtle soft ground shadow beneath the weapon, even and formal studio lighting, crisp readable silhouettes and controlled pixel-cluster shading rather than photographic rendering.

Keep the palette of the first image: bright cold blue-gray steel with deep blue-teal shadow planes for the sickle, the silver-white cutting edge as the brightest area of the picture, bright blue-gray chain links with silver-gray highlights, and a small amount of warm brown wood with warm reddish-brown leather on the grip. Keep the image bright and high contrast with clear color blocks."""

NEGATIVE_V15 = """character, hand, person, enemy, scenery, battlefield, text, dimension labels, watermark, logo, cropped weapon, multiple weapons, duplicate weapon, photorealism, plastic toy, magic glow, flames, smoke, gold, brass, bronze, copper, muddy low saturation, dirty black iron, heavy rust"""

# v16：v15 被内容安全拦（未生成未计费）。输入图与尺寸不变（v13 + v4 + 三张画风参考），
# 提示词改成“幻想游戏道具 + 正向几何描述”框架（§5.21 / §5.23 已验证可过）。
PROMPT_CN_V16 = """为高清像素化三国战场动作游戏绘制一张锁镰的正式正面立绘图。上传的前两张图用于参考武器设计，后三张图用于参考本项目的配色、皮革/木料/铁箍材质和银白金属受光。

画面里武器竖直站在画面中央，正面朝向镜头，整件完整入画，四周留出充足空白，像一张装备展示立绘。

武器从上到下的结构：最上方是一段长而开放的弯钩镰头，镰尖朝上并向内回弯，内侧沿着一整条连续的银白刃缘，外侧是厚实的蓝灰钢体；镰根处有一个向上扬起的、带缺口的爪形钢件；镰根下方是两支分叉承托杆；接着一段用暖红褐皮革缠握的暖棕木短握柄和几道钢制环箍；握柄最末端是一个厚实的大连接环。

链条从末端那个大连接环自然略弯地向下垂落，可见八到九枚闭合椭圆链环，每枚链环中空、有细但可读的厚度，彼此交叠并交替朝向，链尾收在一个稍大的环上，位置在画面下部。

纯白背景，武器下方只有极轻微的柔和投影；光线均匀正式，轮廓清晰，材质用受控的像素色簇表现。

配色：锁镰主体用明亮冷蓝灰钢与深蓝青暗面，内侧刃缘是全图最亮的银白，链环用明亮蓝灰钢与银灰受光，握柄用少量暖棕木与暖红褐皮革。整体明亮、高对比、色块清晰。"""

PROMPT_EN_V16 = """Create a formal front-view illustration of a chain sickle weapon for a high-definition pixelated Three Kingdoms battlefield action game. The first two uploaded images are the design reference for the weapon, and the last three are the reference for this project's colors, the wood/leather/iron-collar material language, and the silver-white metal highlights.

The weapon stands vertically in the center of the canvas, seen straight on, the whole piece fully inside the frame with generous margins, like an equipment presentation plate.

The weapon reads from the top down like this: a long open hook-bladed sickle head whose tip points upward and curves inward, with one continuous silver-white cutting edge running along its inner side and thick blue-gray steel on the outside; a short upward-facing notched claw of steel at the blade root; two forked support branches below the root; a short gripping section of warm brown wood with a warm reddish-brown leather wrap and a few steel collars; and at the very bottom of the grip one thick large connector ring.

The chain falls from that large connector ring in a natural slight curve and shows eight or nine closed oval links, each one hollow with a thin but readable wall, overlapping one another and alternating their angle, ending in one slightly larger ring near the bottom of the frame.

Pure white background with only a very subtle soft ground shadow under the weapon, even and formal studio lighting, crisp readable silhouettes, and controlled pixel-cluster shading for the materials.

Palette: bright cold blue-gray steel with deep blue-teal shadow planes for the sickle, the silver-white cutting edge as the brightest area of the picture, bright blue-gray chain links with silver-gray highlights, and warm brown wood with warm reddish-brown leather on the grip. Bright, high contrast and clearly color-blocked."""

REFS_V17 = [
    (V13_LOCK, "image 1: the design reference for the weapon - sickle head, forked support branches, short grip, rear connector ring"),
    (r"SkillWeaponsArtSource/01_Reference/style/game_palette_board.png",
     "image 2: only the project color relationships"),
    (r"SkillWeaponsArtSource/01_Reference/style/game_wood_iron_ref.png",
     "image 3: only the material language for wood, leather and iron collars"),
    (r"SkillWeaponsArtSource/01_Reference/style/game_saber_ref.png",
     "image 4: only the silver-white cutting edge and clean metal highlight language"),
]

REFS_V18 = [
    (r"SkillWeaponsArtSource/01_Reference/style/game_palette_board.png",
     "image 1: only the project color relationships"),
    (r"SkillWeaponsArtSource/01_Reference/style/game_wood_iron_ref.png",
     "image 2: only the material language for wood, leather and iron collars"),
    (r"SkillWeaponsArtSource/01_Reference/style/game_saber_ref.png",
     "image 3: only the silver-white cutting edge and clean metal highlight language"),
]

# ---------------------------------------------------------------------------
# v19（2026-11-16）：用户否决 v18 正面立绘（“造型完全被改错了”），
# 指示：回到 v4，先思考怎么把锁链调到尾部，**或者直接先移除**。
#
# 根因分析（写在 §5.27）：
#   每一轮“带链”请求里，链条都在画幅上占一半以上 ⇒ 模型为了塞下链条会重新平衡构图
#   ⇒ 镰头被缩小/重画 ⇒ 造型漂移。**先把链条拿掉，镰头就没有竞争对手，形状才锁得住。**
#   所以这一轮只做一个最小编辑：v4 − 链条，握柄末端改成封死的钢帽。
#   输入只有 v4 一张图（不做多图融合，避免模型重新拼装）。
# ---------------------------------------------------------------------------
REFS_V19 = [
    (V4_LOCK, "image 1: the picture to edit - remove the chain from this weapon and keep every other part as it is"),
]

PROMPT_CN_V19 = """把这张图里的铁链去掉。镰头、镰根处向上扬起的缺口爪、镰根下方的两支分叉承托杆、包着红褐皮革的暖棕木握柄、上下两道铁箍全部留在原位，保持原来的形状和大小。握柄最下端原本挂链的位置改成一枚封死的钢制尾帽。纯白背景，武器竖直正立，完整入画。"""

PROMPT_EN_V19 = """Remove the chain from this weapon. The sickle head, the upward notched claw at the blade root, the two forked support branches below the root, the warm brown wooden grip with its reddish-brown leather wrap and the two iron collars all stay in place, keeping their existing shape and size. The bottom of the grip, where the chain used to hang, now ends in a plain closed steel cap. Pure white background, the weapon standing upright and fully inside the frame."""

NEGATIVE_V19 = """chain, hanging chain, chain links, rope, character, hand, person, text, watermark, logo, cropped weapon, multiple weapons, duplicate weapon"""

# v20（2026-11-16）：在 v19（无链）基础上做**第二个最小编辑**：
#   把握柄末端的封死钢帽换成厚实的大连接环，并从该环向下挂一段短链（8–9 环）。
#   输入只有 v19 一张图 ⇒ 画面里没有“链条抢构图”的竞争对手，镰头形状应当锁得住。
REFS_V20 = [
    ("C:/Users/steam/Pictures/gptGen/chain_sickle_concept_20261115/chain_sickle_concept_v1_v19_v19.png",
     "image 1: the picture to edit - add a tail connector ring at the grip butt and a short chain hanging from it, keeping every other part as it is"),
]

PROMPT_CN_V20 = """在这张图的握柄最下端，把那枚封死的钢帽换成一个厚实的大钢制连接环，并从这个连接环向下挂一段短铁链：八到九枚闭合椭圆链环，每枚中空、有细但可读的厚度，彼此交叠并交替朝向，链尾收在一个稍大的环上。镰头、镰根处向上扬起的缺口爪、镰根下方的两支分叉承托杆、包着红褐皮革的暖棕木握柄和上下两道铁箍都保持原位、原样不动。纯白背景，武器竖直正立，完整入画。"""

PROMPT_EN_V20 = """At the bottom of the grip, replace the closed steel cap with one thick large steel connector ring, and hang a short chain from that ring: eight or nine closed oval links, each one hollow with a thin but readable wall, overlapping one another and alternating orientation, ending in one slightly larger ring. The sickle head, the upward notched claw at the blade root, the two forked support branches below the root, the warm brown wooden grip with its reddish-brown leather wrap and the two iron collars all stay in place, unchanged. Pure white background, the weapon standing upright and fully inside the frame."""

NEGATIVE_V20 = """character, hand, person, enemy, text, watermark, logo, cropped weapon, multiple weapons, duplicate weapon, photorealism, plastic toy, magic glow, flames, smoke"""

# ---------------------------------------------------------------------------
# v21（2026-11-16）：**链环素材图**（需求书 §1.1 缺的 `chain_link.png`）。
#   运行时硬约定（ChainLinkRig.CropLinkSprite）：256² 画布，链节本体落在
#   x 20%~78%、y 18%~58%（y 向上）窗口内；Sprite PPU=100。
#   ⇒ 本图只出一枚“正面看”的单个中空椭圆链环（不画整条链），后面本地合成到 256² 窗口。
#   参考图只给画风（色卡 + 钢刃），不给造型（造型是标准链环，用文字描述）。
# ---------------------------------------------------------------------------
REFS_V21 = [
    (r"SkillWeaponsArtSource/01_Reference/style/game_saber_ref.png",
     "image 1: only the silver-white steel highlight language on a cold blue-gray blade"),
    (r"SkillWeaponsArtSource/01_Reference/style/game_palette_board.png",
     "image 2: only the project color relationships"),
]

PROMPT_CN_V21 = """为一款高清像素化三国战场动作游戏绘制一枚链环的游戏素材图：单独一枚闭合的椭圆铁环，正面朝向镜头，中间的空腔完全可见。环体是锻造钢材，管壁细但轮廓清楚可读；外沿左上方有一道干净的银灰受光边，内侧落到更深的蓝青暗面。沿用本项目的钢材配色：冷蓝灰的金属、干净的银灰高光，没有金色、没有黄铜、没有锈蚀。笔触是清晰的大体块加受控的像素色簇，不要照片写实，不要雕花，不要徽记。画面里只有这一枚链环，居中、放大、完整入画，背景透明。"""

PROMPT_EN_V21 = """Draw a game sprite of one single chain link for a high-definition pixelated Three Kingdoms battlefield action game: one closed oval iron ring alone, seen straight on so the empty center is fully visible. The ring is forged steel with a thin but clearly readable wall; its outer edge catches a clean silver-gray highlight along the upper left, and the inner side falls into a darker blue-teal shadow. Use this project's steel color language: cold blue-gray metal with clean silver-gray highlights, no gold, no brass, no rust. Shading is simple readable pixel clusters, not photographic, with no engraving and no crest. The picture contains this one ring only, centered, large and fully inside the frame, on a transparent background."""

NEGATIVE_V21 = """second ring, multiple rings, chain, chain links, rope, solid bar, character, hand, person, text, watermark, logo, gold, brass, bronze, copper, rust, dirt, photorealism, plastic, glow, flames, smoke, cropped ring, tilted ring"""

VARIANTS = {
    "v1": {"cn": PROMPT_CN, "en": PROMPT_EN, "neg": NEGATIVE},
    "v2": {"cn": PROMPT_CN_V2, "en": PROMPT_EN_V2, "neg": NEGATIVE_V2},
    # v3（2026-11-15）：正式交付候选。
    # 实测结论：probe1（短提示词 + PBZ 锁镰图）400、probe2（同样短提示词、去掉 PBZ 图）200
    # ⇒ 触发点是 PBZ 那张锁镰参考图本身，不是提示词措辞。
    # v3 因此改用：完整造型要求（链身、大连接环、爪形刃根、分叉握杆、挂件、铁箍、末端挂环）
    # 全部写进提示词，参考图只用项目自产的色卡 + 木料铁箍两张（画风/材质职责）。
    "v3": {
        "cn": PROMPT_CN_V3,
        "en": PROMPT_EN_V3,
        "neg": NEGATIVE_V2,
        "refs": [
            (r"SkillWeaponsArtSource/01_Reference/style/game_palette_board.png",
             "image 1: only the project color relationships and high-definition pixelated material color blocking"),
            (r"SkillWeaponsArtSource/01_Reference/style/game_wood_iron_ref.png",
             "image 2: only the material language for warm brown wood, cold blue-gray iron collars and riveted construction"),
        ],
    },
    # v4（2026-11-15）：按 plan §5.13 新方案重做锁镰概念图。
    # 参考图 = 三张项目自产画风图（色卡 / 木料铁箍 / 钢刃），不上传会被内容安全拦的 PBZ 锁镰图。
    "v4": {
        "cn": PROMPT_CN_V4,
        "en": PROMPT_EN_V4,
        "neg": NEGATIVE_V4,
        "refs": [
            (r"SkillWeaponsArtSource/01_Reference/style/game_palette_board.png",
             "image 1: only the overall project color relationships"),
            (r"SkillWeaponsArtSource/01_Reference/style/game_wood_iron_ref.png",
             "image 2: only the material language for wood, leather and iron collars"),
            (r"SkillWeaponsArtSource/01_Reference/style/game_saber_ref.png",
             "image 3: only the silver-white cutting edge and clean metal highlight language"),
        ],
    },
    # v5（2026-11-15）：v4 的定点修正（链条连接点移到钩背高位）。
    "v5": {
        "cn": PROMPT_CN_V5,
        "en": PROMPT_EN_V5,
        "neg": NEGATIVE_V5,
        "refs": [
            (r"SkillWeaponsArtSource/01_Reference/style/game_palette_board.png",
             "image 1: only the overall project color relationships"),
            (r"SkillWeaponsArtSource/01_Reference/style/game_wood_iron_ref.png",
             "image 2: only the material language for wood, leather and iron collars"),
            (r"SkillWeaponsArtSource/01_Reference/style/game_saber_ref.png",
             "image 3: only the silver-white cutting edge and clean metal highlight language"),
        ],
    },
    # v6（2026-11-15）：v5 的措辞收敛版（最小改动，负面词沿用 v4）。
    "v6": {
        "cn": PROMPT_CN_V6,
        "en": PROMPT_EN_V6,
        "neg": NEGATIVE_V4,
        "refs": [
            (r"SkillWeaponsArtSource/01_Reference/style/game_palette_board.png",
             "image 1: only the overall project color relationships"),
            (r"SkillWeaponsArtSource/01_Reference/style/game_wood_iron_ref.png",
             "image 2: only the material language for wood, leather and iron collars"),
            (r"SkillWeaponsArtSource/01_Reference/style/game_saber_ref.png",
             "image 3: only the silver-white cutting edge and clean metal highlight language"),
        ],
    },
    # v7（2026-11-15）：按 plan §5.18「尾部连接环与可读运动轨迹」新方案重做锁链概念图。
    # 结构要点：链条挂在握柄末端的尾部大连接环；三分之四功能姿态表达前后关系。
    "v7": {
        "cn": PROMPT_CN_V7,
        "en": PROMPT_EN_V7,
        "neg": NEGATIVE_V7,
        "plan_ref": "§5.18",
        "refs": [
            (r"SkillWeaponsArtSource/01_Reference/style/game_palette_board.png",
             "image 1: only the overall project color relationships"),
            (r"SkillWeaponsArtSource/01_Reference/style/game_wood_iron_ref.png",
             "image 2: only the material language for wood, leather and iron collars"),
            (r"SkillWeaponsArtSource/01_Reference/style/game_saber_ref.png",
             "image 3: only the silver-white cutting edge and clean metal highlight language"),
        ],
    },
    # v7p0 / v7p1（2026-11-15）：v7 被内容安全拦掉后的隔离实验（见上方注释）。
    "v7p0": {
        "cn": PROMPT_CN_V7,
        "en": PROMPT_EN_V7,
        "neg": NEGATIVE_V7_MIN,
        "plan_ref": "§5.18（隔离实验 p0：正文不动，负面词换中性最小集）",
        "refs": [
            (r"SkillWeaponsArtSource/01_Reference/style/game_palette_board.png",
             "image 1: only the overall project color relationships"),
            (r"SkillWeaponsArtSource/01_Reference/style/game_wood_iron_ref.png",
             "image 2: only the material language for wood, leather and iron collars"),
            (r"SkillWeaponsArtSource/01_Reference/style/game_saber_ref.png",
             "image 3: only the silver-white cutting edge and clean metal highlight language"),
        ],
    },
    "v7p1": {
        "cn": PROMPT_CN_V7_SOFT,
        "en": PROMPT_EN_V7_SOFT,
        "neg": NEGATIVE_V7_MIN,
        "plan_ref": "§5.18（隔离实验 p1：正文否定句改中性描述，负面词仍为中性最小集）",
        "refs": [
            (r"SkillWeaponsArtSource/01_Reference/style/game_palette_board.png",
             "image 1: only the overall project color relationships"),
            (r"SkillWeaponsArtSource/01_Reference/style/game_wood_iron_ref.png",
             "image 2: only the material language for wood, leather and iron collars"),
            (r"SkillWeaponsArtSource/01_Reference/style/game_saber_ref.png",
             "image 3: only the silver-white cutting edge and clean metal highlight language"),
        ],
    },
    # v7p2（2026-11-15）：隔离实验 p2 —— 只去掉负面词里的 tilted weapon（与正文要求的三分之四姿态矛盾）。
    "v7p2": {
        "cn": PROMPT_CN_V7,
        "en": PROMPT_EN_V7,
        "neg": NEGATIVE_V7_MIN_NO_TILT,
        "plan_ref": "§5.18（隔离实验 p2：正文逐字不变，负面词去掉 tilted weapon）",
        "refs": [
            (r"SkillWeaponsArtSource/01_Reference/style/game_palette_board.png",
             "image 1: only the overall project color relationships"),
            (r"SkillWeaponsArtSource/01_Reference/style/game_wood_iron_ref.png",
             "image 2: only the material language for wood, leather and iron collars"),
            (r"SkillWeaponsArtSource/01_Reference/style/game_saber_ref.png",
             "image 3: only the silver-white cutting edge and clean metal highlight language"),
        ],
    },
    # v7p4（2026-11-15）：以已验证可过的 v6 文案为底，只改链条连接位置（尾部大连接环）。
    "v7p4": {
        "cn": PROMPT_CN_V7_TAIL,
        "en": PROMPT_EN_V7_TAIL,
        "neg": NEGATIVE_V4,
        "plan_ref": "§5.18（隔离实验 p4：以 v6 可过文案为底，只改尾部连接环）",
        "refs": [
            (r"SkillWeaponsArtSource/01_Reference/style/game_palette_board.png",
             "image 1: only the overall project color relationships"),
            (r"SkillWeaponsArtSource/01_Reference/style/game_wood_iron_ref.png",
             "image 2: only the material language for wood, leather and iron collars"),
            (r"SkillWeaponsArtSource/01_Reference/style/game_saber_ref.png",
             "image 3: only the silver-white cutting edge and clean metal highlight language"),
        ],
    },
    # v6replay（2026-11-15）：把 v6（当时实测 200 的那条提示词）逐字重放，
    # 用于判断当前 edits 通道的拦截是否与措辞有关（若逐字重放也 400，说明是通道/路由侧的拦截变化）。
    "v6replay": {
        "cn": PROMPT_CN_V6,
        "en": PROMPT_EN_V6,
        "neg": NEGATIVE_V4,
        "plan_ref": "§5.18（诊断：逐字重放已通过的 v6）",
        "refs": [
            (r"SkillWeaponsArtSource/01_Reference/style/game_palette_board.png",
             "image 1: only the overall project color relationships"),
            (r"SkillWeaponsArtSource/01_Reference/style/game_wood_iron_ref.png",
             "image 2: only the material language for wood, leather and iron collars"),
            (r"SkillWeaponsArtSource/01_Reference/style/game_saber_ref.png",
             "image 3: only the silver-white cutting edge and clean metal highlight language"),
        ],
    },
    # v8（2026-11-15）：§5.18 正文在两个通道连续被拦后的“幻想游戏道具”框架重写版。
    "v8": {
        "cn": PROMPT_CN_V8,
        "en": PROMPT_EN_V8,
        "neg": NEGATIVE_V8,
        "plan_ref": "§5.18（重写版：幻想游戏道具框架 + 几何结构描述，避开叙事/否定句）",
        "refs": [
            (r"SkillWeaponsArtSource/01_Reference/style/game_palette_board.png",
             "image 1: only the overall project color relationships"),
            (r"SkillWeaponsArtSource/01_Reference/style/game_wood_iron_ref.png",
             "image 2: only the material language for wood, leather and iron collars"),
            (r"SkillWeaponsArtSource/01_Reference/style/game_saber_ref.png",
             "image 3: only the silver-white cutting edge and clean metal highlight language"),
        ],
    },
    # v9（2026-11-15）：plan §5.20 原文逐字提交。
    "v9": {
        "cn": PROMPT_CN_V9,
        "en": PROMPT_EN_V9,
        "neg": NEGATIVE_V9,
        "plan_ref": "§5.20（v4 锁镰头回归 + 尾部链环修正，原文逐字）",
        "refs": [
            (r"SkillWeaponsArtSource/01_Reference/style/game_palette_board.png",
             "image 1: only the overall project color relationships"),
            (r"SkillWeaponsArtSource/01_Reference/style/game_wood_iron_ref.png",
             "image 2: only the material language for wood, leather and iron collars"),
            (r"SkillWeaponsArtSource/01_Reference/style/game_saber_ref.png",
             "image 3: only the silver-white cutting edge and clean metal highlight language"),
        ],
    },
    # v10（2026-11-15）：§5.20 结构要求改写为“幻想游戏道具 + 纯几何描述”框架（v9 被拦时的备用）。
    "v10": {
        "cn": PROMPT_CN_V10,
        "en": PROMPT_EN_V10,
        "neg": NEGATIVE_V10,
        "plan_ref": "§5.20（几何描述框架改写版，结构要求逐条保留）",
        "refs": [
            (r"SkillWeaponsArtSource/01_Reference/style/game_palette_board.png",
             "image 1: only the overall project color relationships"),
            (r"SkillWeaponsArtSource/01_Reference/style/game_wood_iron_ref.png",
             "image 2: only the material language for wood, leather and iron collars"),
            (r"SkillWeaponsArtSource/01_Reference/style/game_saber_ref.png",
             "image 3: only the silver-white cutting edge and clean metal highlight language"),
        ],
    },
    # v11（2026-11-15）：plan §5.22 原文逐字（v4 原图作为首图锁定造型）。
    "v11": {
        "cn": PROMPT_CN_V11,
        "en": PROMPT_EN_V11,
        "neg": NEGATIVE_V11,
        "plan_ref": "§5.22（v4 原图为造型锁定，原文逐字）",
        "refs": REFS_V4LOCK,
    },
    # v12（2026-11-15）：§5.22 输入图不变，提示词改写为正向几何描述框架（v11 被拦时用）。
    "v12": {
        "cn": PROMPT_CN_V12,
        "en": PROMPT_EN_V12,
        "neg": NEGATIVE_V12,
        "plan_ref": "§5.22（输入图不变，措辞改为正向几何描述）",
        "refs": REFS_V4LOCK,
    },
    # v13（2026-11-15）：plan §5.24 原文逐字（宽幅 1536x1024，镰头放大 + 链条压缩）。
    "v13": {
        "cn": PROMPT_CN_V13,
        "en": PROMPT_EN_V13,
        "neg": NEGATIVE_V13,
        "size": "1536x1024",
        "plan_ref": "§5.24（v4 镰头主体放大 + 紧凑拖链构图，原文逐字）",
        "refs": REFS_V4LOCK,
    },
    # v14（2026-11-15）：§5.24 输入图与尺寸不变，提示词改写为正向几何描述框架。
    "v14": {
        "cn": PROMPT_CN_V14,
        "en": PROMPT_EN_V14,
        "neg": NEGATIVE_V14,
        "size": "1536x1024",
        "plan_ref": "§5.24（输入图与尺寸不变，措辞改为正向几何描述）",
        "refs": REFS_V4LOCK,
    },
    # v15（2026-11-15）：用户拍板要一张正式的正面立绘图（竖幅、正面、直立）。
    "v15": {
        "cn": PROMPT_CN_V15,
        "en": PROMPT_EN_V15,
        "neg": NEGATIVE_V15,
        "size": "1024x1536",
        "plan_ref": "用户拍板：正式正面立绘图（v13 造型锁 + v4 头部细节 + 三张画风参考）",
        "refs": REFS_V15,
    },
    # v16（2026-11-15）：v15 被拦后的改写版（输入图与尺寸不变，措辞改为正向几何描述）。
    "v16": {
        "cn": PROMPT_CN_V16,
        "en": PROMPT_EN_V16,
        "neg": NEGATIVE_V15,
        "size": "1024x1536",
        "plan_ref": "用户拍板的正面立绘图（措辞改为正向几何描述，输入图不变）",
        "refs": REFS_V15,
    },
    # v17（2026-11-15）：v15/v16 都被拦 ⇒ 触发点更可能在**同时上传两张自产武器图**；
    # 去掉 v4 细节图（v13 本身已含镰头），只留 v13 + 三张画风参考，提示词不变。
    "v17": {
        "cn": PROMPT_CN_V16,
        "en": PROMPT_EN_V16,
        "neg": NEGATIVE_V15,
        "size": "1024x1536",
        "plan_ref": "用户拍板的正面立绘图（只上传 v13 + 三张画风参考）",
        "refs": REFS_V17,
    },
    # v18（2026-11-15）：本时点上传任何自产武器图都被拦 ⇒ 只上传三张画风参考，造型完全由文字描述。
    "v18": {
        "cn": PROMPT_CN_V16,
        "en": PROMPT_EN_V16,
        "neg": NEGATIVE_V15,
        "size": "1024x1536",
        "plan_ref": "用户拍板的正面立绘图（不传武器图，只传三张画风参考 + 文字造型描述）",
        "refs": REFS_V18,
    },
    # v19（2026-11-16）：v4 − 链条的最小编辑（只上传 v4 一张图）。
    "v19": {
        "cn": PROMPT_CN_V19,
        "en": PROMPT_EN_V19,
        "neg": NEGATIVE_V19,
        "size": "1024x1536",
        "plan_ref": "用户指示：回到 v4，先把链条移除（最小编辑，v4 单图输入）",
        "refs": REFS_V19,
    },
    # v20（2026-11-16）：在 v19（无链）上加尾部大连接环 + 短链（第二个最小编辑）。
    "v20": {
        "cn": PROMPT_CN_V20,
        "en": PROMPT_EN_V20,
        "neg": NEGATIVE_V20,
        "size": "1024x1536",
        "plan_ref": "v19 基础上加尾部连接环与短链（输入只有 v19 一张图）",
        "refs": REFS_V20,
    },
    # v21（2026-11-16）：链环素材图（需求书 §1.1 的 chain_link.png）。
    "v21": {
        "cn": PROMPT_CN_V21,
        "en": PROMPT_EN_V21,
        "neg": NEGATIVE_V21,
        "size": "1024x1024",
        "plan_ref": "需求书 §1.1 链环贴图：单枚链环素材（只给画风参考，造型由文字）",
        "refs": REFS_V21,
    },
    # probe1（2026-11-15）：隔离诊断。
    # v1 / v2 两次都被内容安全拦成 400（措辞差异很大），因此假设“触发点在参考图或整体框架”。
    # probe1 = 短提示词 + 只传 PBZ 锁镰图与色卡（去掉木料铁箍图），用来分辨：
    #   400 → 触发点与 PBZ 锁镰图或“链/镰/长提示词”有关；
    #   200 → 触发点在长提示词里，短提示词可行（代价是一次生成）。
    "probe1": {
        "cn": "为幻想游戏道具设计一张概念图：单件带钩刃与分叉皮革握柄的兵器，竖直正立，白色背景，形状清晰可读。",
        "en": "Create a concept illustration for a fantasy video game prop: a single hooked bladed weapon with a forked, leather-wrapped grip, standing upright on a plain white background, full object visible, clear readable shapes, simple high-contrast color blocking.",
        "neg": "character, hand, person, scenery, text, watermark, logo, cropped weapon, tilted weapon, multiple weapons",
        "refs": [
            (r"SkillWeaponsArtSource/01_Reference/pbz/zhuiming_suolian.png",
             "image 1: only the overall form and part relationships of a chain-hook weapon"),
            (r"SkillWeaponsArtSource/01_Reference/style/game_palette_board.png",
             "image 2: only the project color relationships and material color blocking"),
        ],
    },
    # probe2（2026-11-15）：去掉 PBZ 锁镰图，只留项目自产的画风参考（色卡 + 木料铁箍），
    # 提示词与 probe1 相同。用来分辨“触发点是不是 PBZ 那张锁镰参考图”：
    #   400 → 触发点不在该图（与文字或画风参考有关）；
    #   200 → PBZ 锁镰图就是触发点，需要换/裁参考图。
    "probe2": {
        "cn": "为幻想游戏道具设计一张概念图：单件带钩刃与分叉皮革握柄的兵器，竖直正立，白色背景，形状清晰可读。",
        "en": "Create a concept illustration for a fantasy video game prop: a single hooked bladed weapon with a forked, leather-wrapped grip, standing upright on a plain white background, full object visible, clear readable shapes, simple high-contrast color blocking.",
        "neg": "character, hand, person, scenery, text, watermark, logo, cropped weapon, tilted weapon, multiple weapons",
        "refs": [
            (r"SkillWeaponsArtSource/01_Reference/style/game_palette_board.png",
             "image 1: only the project color relationships and material color blocking"),
            (r"SkillWeaponsArtSource/01_Reference/style/game_wood_iron_ref.png",
             "image 2: only the material language for warm brown wood, cold blue-gray iron collars and riveted construction"),
        ],
    },
}

PROMPT = PROMPT_EN + "\n\nNegative constraints: " + NEGATIVE


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
    ap = argparse.ArgumentParser(description="提交追命锁镰概念图")
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--approve", action="store_true")
    ap.add_argument("--variant", default="v2", choices=sorted(VARIANTS.keys()))
    ap.add_argument("--model", default=MODEL)
    ap.add_argument("--channel", default="edits", choices=["edits", "generations"],
                    help="edits=带参考图的 multipart 编辑通道；generations=无参考图的文生图通道"
                         "（2026-11-15 实测：edits 通道整体拦截时，generations 通道可通过）")
    ap.add_argument("--tag", default="",
                    help="输出文件后缀（同一变体跑不同通道时区分用，例如 edits / gen）")
    ap.add_argument("--background", default="",
                    help="透明白底等（不传则不发送该字段；例如 transparent）")
    args = ap.parse_args()

    variant = VARIANTS[args.variant]
    prompt_en = variant["en"]
    prompt_cn = variant["cn"]
    negative = variant["neg"]
    prompt = prompt_en + "\n\nNegative constraints: " + negative
    stem = "%s_%s%s" % (STEM, args.variant, ("_" + args.tag) if args.tag else "")
    refs = variant.get("refs", REFS)
    channel = args.channel
    size = variant.get("size", SIZE)

    files = []
    for rel, role in refs:
        p = abs_path(rel)
        if not os.path.isfile(p):
            raise SystemExit("参考图缺失: %s" % p)
        files.append((p, role))

    img_out = OUT_DIR / ("%s.png" % stem)

    if args.dry_run:
        print("=== 变体 %s ===" % args.variant)
        print("=== 模型 ===\n%s" % args.model)
        print("=== 参考图（顺序固定，三张 image[]）===")
        for i, (p, role) in enumerate(files, 1):
            print("image %d: %s\n         sha256=%s\n         %s" % (i, p, sha256_file(p), role))
        print("\n=== 提示词（实际提交用英文版 %d 字符，sha256 %s；中文版同存 .prompt_cn.txt）==="
              % (len(prompt), hashlib.sha256(prompt.encode("utf-8")).hexdigest()))
        print(prompt)
        print("\n=== 参数 ===")
        if channel == "generations":
            print("endpoint=%s（文生图通道：不带参考图，n=1 为数字）" % GENERATIONS_ENDPOINT)
            print("model=%s size=%s quality=%s output_format=png n=1" % (args.model, size, QUALITY))
        else:
            print("endpoint=%s\nmodel=%s size=%s quality=%s input_fidelity=%s output_format=png n=1"
                  % (ENDPOINT, args.model, size, QUALITY, INPUT_FIDELITY))
        print("不传 background；超时 %ds；自动重试 0；最多提交一次" % TIMEOUT)
        print("输出: %s" % img_out)
        return 0

    if not args.approve:
        raise SystemExit("未确认批准：加 --approve 才会真的提交（纪律：一次提交，不自动重试）")

    key = os.environ.get("MUSK_API_KEY")
    if not key:
        raise SystemExit("MUSK_API_KEY missing")
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    if img_out.exists():
        raise SystemExit("refusing to overwrite existing output: %s" % img_out)

    models = check_models(key)
    if args.model not in models["image_models"]:
        raise SystemExit("模型不可用: %s（当前图像模型 %s）" % (args.model, models["image_models"]))

    if channel == "generations":
        fields = {
            "model": args.model,
            "prompt": prompt,
            "size": size,
            "quality": QUALITY,
            "output_format": "png",
            "n": 1,
        }
        if args.background:
            fields["background"] = args.background
        body = json.dumps(fields, ensure_ascii=False).encode("utf-8")
        ctype = "application/json"
        endpoint = GENERATIONS_ENDPOINT
        ref_records = []
    else:
        fields = {
            "model": args.model,
            "prompt": prompt,
            "size": size,
            "quality": QUALITY,
            "input_fidelity": INPUT_FIDELITY,
            "output_format": "png",
            "n": "1",
        }
        if args.background:
            fields["background"] = args.background
        body, ctype = build_multipart(fields, [("image[]", p) for p, _ in files])
        endpoint = ENDPOINT
        ref_records = [
            {"order": i + 1, "path": p, "role": role, "sha256": sha256_file(p),
             "bytes": os.path.getsize(p)}
            for i, (p, role) in enumerate(files)
        ]

    (OUT_DIR / ("%s.prompt.txt" % stem)).write_text(prompt, encoding="utf-8", newline="\n")
    (OUT_DIR / ("%s.prompt_cn.txt" % stem)).write_text(prompt_cn, encoding="utf-8", newline="\n")
    (OUT_DIR / ("%s.prompt_negative.txt" % stem)).write_text(negative, encoding="utf-8", newline="\n")

    with open(OUT_DIR / ("%s.request.json" % stem), "w", encoding="utf-8") as fh:
        json.dump({
            "version": stem,
            "variant": args.variant,
            "plan": "Locus/knowledge/plan/weapon-concept-image-plan-20261114.md %s"
                    % variant.get("plan_ref", "§5.11"),
            "spec": "Locus/knowledge/design/weapon-3d-art-spec-axe-and-chain.md",
            "endpoint": endpoint,
            "channel": channel,
            "method": "POST",
            "content_type": ctype,
            "fields": fields,
            "reference_images": ref_records,
            "models_check": models,
            "prompt_sha256": hashlib.sha256(prompt.encode("utf-8")).hexdigest(),
            "prompt_chars": len(prompt),
            "automatic_retries": 0,
            "read_timeout_seconds": TIMEOUT,
            "gate": "四视图闸门未开：锁镰概念图需用户确认后才能继续",
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
