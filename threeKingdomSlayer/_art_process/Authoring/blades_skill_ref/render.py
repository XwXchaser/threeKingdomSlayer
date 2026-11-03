# -*- coding: utf-8 -*-
import re, os
from PIL import Image, ImageDraw, ImageFont, ImageFilter

src = open('skills.wiki', encoding='utf-8').read()
blocks = re.split(r'^==+\s*(.*?)\s*==+$', src, flags=re.M)
sections = {}
for i in range(1, len(blocks), 2):
    title = blocks[i].strip(); body = blocks[i + 1]
    if title not in ('Spells', 'Perks', 'Abilities') or title in sections:
        continue
    rows = []
    for line in body.split('\n'):
        if not line.startswith('{{Chart |'):
            continue
        head = line.split('boxstyle=background: transparent; |', 1)[1]
        tokmap = {m.group(1): m.group(2) for m in re.finditer(
            r'\|([A-Z]{4,5})=\{\{Image Overlay\|BL-icon-Skill Frame\.png\|BL-icon-([^|]+)\.png', head)}
        lay = re.split(r'\|[A-Z]{4,5}=\{\{Image Overlay', head)[0]
        cells = [c.strip() for c in lay.rstrip('}').strip().split('|')]
        if cells and cells[0] == '':
            cells = cells[1:]
        rows.append((cells, tokmap))
    if rows:
        sections[title] = rows

NAMES = {}
for t, rows in sections.items():
    for cells, tm in rows:
        NAMES.update(tm)
print({k: len(v) for k, v in sections.items()}, 'nodes', len(NAMES))

F = "C:/Windows/Fonts/"
def font(name, size):
    return ImageFont.truetype(F + name, size)

_cache = {}
GOLD = (214, 186, 130)

def load_icon(tok, size):
    """节点 = 金属外框 + 深色底板 + 金色蚀刻剪影（原始 icon 为黑色 alpha 剪影）"""
    key = (tok, size)
    if key in _cache:
        return _cache[key]
    out = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    # 底板：内圈区域（按外框图实测：x 0.27~0.75，y 0.22~0.79）
    plate = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    pd = ImageDraw.Draw(plate)
    box = [int(size * 0.265), int(size * 0.215), int(size * 0.755), int(size * 0.795)]
    for i in range(box[3] - box[1]):
        t = i / max(1, box[3] - box[1] - 1)
        c = (int(46 - 16 * t), int(40 - 14 * t), int(33 - 12 * t), 255)
        pd.line([(box[0] + 2, box[1] + i), (box[2] - 2, box[1] + i)], fill=c)
    pd.rectangle(box, outline=(96, 82, 60, 255), width=max(1, size // 80))
    out.alpha_composite(plate)
    # 剪影
    p = "icons/BL-icon-%s.png" % NAMES[tok].replace(' ', '_')
    if os.path.exists(p):
        ic = Image.open(p).convert("RGBA")
        side = int(size * 0.40)
        ic = ic.resize((side, side), Image.LANCZOS)
        tint = Image.new("RGBA", ic.size, GOLD + (255,))
        tint.putalpha(ic.getchannel("A"))
        out.alpha_composite(tint, (int(size * 0.51 - side / 2), int(size * 0.505 - side / 2)))
    # 金属外框压在最上层
    frame = Image.open("icons/BL-icon-Skill_Frame.png").convert("RGBA").resize((size, size), Image.LANCZOS)
    out.alpha_composite(frame)
    _cache[key] = out
    return out

NODE = 108

def tokens_of(cells):
    return [(i, c) for i, c in enumerate(cells) if c and re.fullmatch(r'[A-Z]{4,5}', c)]

def render_tree(img, rows, ox, oy, step_x, step_y, node, label_font):
    d = ImageDraw.Draw(img)
    pos = {}
    for r, (cells, tm) in enumerate(rows):
        for idx, tok in tokens_of(cells):
            pos[tok] = (ox + idx * step_x, oy + r * step_y, r, idx)
    for tok, (x, y, r, idx) in pos.items():
        for r2 in range(r + 1, len(rows)):
            nxt = tokens_of(rows[r2][0])
            cands = [(abs(i2 - idx), t2) for i2, t2 in nxt if abs(i2 - idx) <= 1]
            if not cands:
                continue
            _, t2 = min(cands)
            x2, y2, _, _ = pos[t2]
            d.line([(x + node / 2, y + node * 0.95), (x2 + node / 2, y2 + node * 0.05)],
                   fill=(122, 103, 74), width=max(3, node // 26))
            break
    for tok, (x, y, r, idx) in pos.items():
        img.alpha_composite(load_icon(tok, node), (int(x), int(y)))
    d = ImageDraw.Draw(img)
    for tok, (x, y, r, idx) in pos.items():
        txt = NAMES[tok]
        w = d.textlength(txt, font=label_font)
        d.text((x + node / 2 - w / 2, y + node + 3), txt, font=label_font, fill=(228, 214, 190),
               stroke_width=2, stroke_fill=(10, 8, 6))
    return pos

def bg(w, h):
    img = Image.new("RGBA", (w, h), (16, 13, 11, 255))
    d = ImageDraw.Draw(img)
    for y in range(h):
        t = y / h
        d.line([(0, y), (w, y)], fill=(int(34 - 18 * t), int(28 - 15 * t), int(23 - 12 * t), 255))
    v = Image.new("L", (w, h), 0)
    ImageDraw.Draw(v).ellipse([-w * 0.35, -h * 0.25, w * 1.35, h * 1.25], fill=90)
    v = v.filter(ImageFilter.GaussianBlur(140)).point(lambda p: min(255, p + 120))
    return Image.composite(img, Image.new("RGBA", (w, h), (0, 0, 0, 255)), v)

W, H = 1080, 1920
img = bg(W, H)
d = ImageDraw.Draw(img)
f_title = font("timesbd.ttf", 58)
f_tab = font("seguisb.ttf", 33)
f_num = font("seguisb.ttf", 40)
f_lab = font("seguisb.ttf", 19)
f_card = font("seguisb.ttf", 26)
f_small = font("seguisb.ttf", 21)

d.text((60, 44), "SKILLS", font=f_title, fill=(226, 206, 168))
sp = Image.open("icons/BL-icon-SkillPoint.png").convert("RGBA").resize((52, 52), Image.LANCZOS)
img.alpha_composite(sp, (W - 200, 50))
d = ImageDraw.Draw(img)
d.text((W - 136, 54), "12", font=f_num, fill=(240, 226, 196))
d.line([(60, 132), (W - 60, 132)], fill=(120, 101, 72), width=3)

tx = 60
for name, i in [("SPELLS", 0), ("PERKS", 1), ("ABILITIES", 2)]:
    tw = int(d.textlength(name, font=f_tab))
    d.text((tx, 158), name, font=f_tab, fill=(240, 220, 180) if i == 0 else (126, 112, 90))
    if i == 0:
        d.line([(tx, 204), (tx + tw, 204)], fill=(196, 160, 104), width=4)
    tx += tw + 54

pos = render_tree(img, sections["Spells"], ox=66, oy=262, step_x=149, step_y=150, node=NODE, label_font=f_lab)
d = ImageDraw.Draw(img)
if "FIREB" in pos:
    x, y, _, _ = pos["FIREB"]
    d.rectangle([x - 6, y - 6, x + NODE + 6, y + NODE + 6], outline=(214, 176, 104), width=4)

card_top = H - 468
d.rectangle([0, card_top, W, H], fill=(24, 20, 16, 255))
d.line([(0, card_top), (W, card_top)], fill=(150, 124, 86), width=3)
d.rectangle([56, card_top + 34, W - 56, H - 40], outline=(150, 124, 86), width=3)
d.rectangle([59, card_top + 37, W - 59, H - 43], outline=(70, 58, 42), width=1)
img.alpha_composite(load_icon("FIREB", 92), (86, card_top + 62))
d = ImageDraw.Draw(img)
d.text((200, card_top + 62), "Fireball", font=font("timesbd.ttf", 42), fill=(238, 222, 188))
d.text((200, card_top + 118), "Spell   ·   Fire Damage   ·   Requires: none", font=f_small, fill=(168, 154, 130))
d.text((86, card_top + 178), "A fiery projectile deals 73.89 fire damage on impact.", font=f_card, fill=(214, 200, 176))

ty = card_top + 228
for name, x in [("RANK", 86), ("LEVEL", 190), ("POINTS", 340), ("MAGNITUDE", 510), ("MAGICKA", 770)]:
    d.text((x, ty), name, font=font("seguisb.ttf", 19), fill=(150, 132, 104))
for i, (rk, lv, pt, mg, mc) in enumerate([(1, 2, 1, "73.89", 90), (2, 11, 1, "108.42", 105),
                                          (3, 20, 2, "150.24", 115), (4, 29, 2, "182.81", 125)]):
    yy = ty + 34 + i * 34
    if i == 0:
        d.rectangle([76, yy - 6, W - 96, yy + 28], fill=(40, 34, 26, 255))
    for x, val in [(86, rk), (190, lv), (340, pt), (510, mg), (770, mc)]:
        d.text((x, yy), str(val), font=f_small, fill=(224, 212, 190))

d.rectangle([76, H - 96, W - 96, H - 46], fill=(74, 58, 34), outline=(196, 160, 104), width=3)
btxt = "LEARN  ·  1 SKILL POINT"
bw = d.textlength(btxt, font=f_tab)
d.text(((W - bw) / 2, H - 88), btxt, font=f_tab, fill=(244, 228, 192))
d.text((60, 14), "结构重建 · 图标与节点框为游戏原素材 · 版面依据 UESP 记录还原，非实机截图",
       font=font("msyh.ttc", 19), fill=(138, 126, 106))
img.convert("RGB").save("blades_skills_portrait_rebuild.png", quality=95)
print("portrait saved", img.size)

TW, TH = 1920, 1180
img2 = bg(TW, TH)
d2 = ImageDraw.Draw(img2)
d2.text((60, 26), "THE ELDER SCROLLS: BLADES — SKILL TREES", font=font("timesbd.ttf", 44), fill=(226, 206, 168))
d2.text((60, 84), "结构重建：三个分页的节点与连线位置（依据 UESP 的 Chart 记录）", font=font("msyh.ttc", 24), fill=(150, 136, 112))
for sec, ox in [("Spells", 40), ("Perks", 670), ("Abilities", 1310)]:
    d2.text((ox + 24, 146), sec.upper(), font=font("seguisb.ttf", 30), fill=(216, 190, 148))
    d2.line([(ox + 24, 188), (ox + 540, 188)], fill=(120, 101, 72), width=3)
    render_tree(img2, sections[sec], ox=ox + 30, oy=214, step_x=82, step_y=100, node=66,
                label_font=font("seguisb.ttf", 15))
    d2 = ImageDraw.Draw(img2)
img2.convert("RGB").save("blades_skill_trees_overview.png", quality=95)
print("overview saved", img2.size)
