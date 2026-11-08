# -*- coding: utf-8 -*-
"""需求打包器：从 VisualLibrary/index.csv 选出最相关的视觉素材，压成一张可直接贴进网页 GPT 的包。

产出（写到 --out 目录）
    brief.md            需求整合稿骨架（目标/现状/约束/验收/不做/未决问题 + 逐图图注）
    source_requirement.md  需求原文副本（供整合时引用，不进 brief 正文）
    images/NN_<id>.png  裁剪 + 缩放后的图，编号即粘贴顺序
    contact_sheet.png   带编号与图注的拼图（只贴一张时用这个）
    manifest.json       机器可读：选了哪些、为什么选、对应哪个文件
    attach_order.txt    给人的粘贴顺序清单

用法（工程根目录执行）
    python Tools/visual_brief/build_brief.py --req <需求.md> --out VisualLibrary/bundles/<id>
    python Tools/visual_brief/build_brief.py --req <需求.md> --out <dir> --dry-run   # 只看选图结果
    python Tools/visual_brief/build_brief.py --req <需求.md> --out <dir> --shot shot_a,shot_b --max-images 8

设计约束
    · 本脚本只读素材、只写产物目录，不改动任何原始图片。
    · 选图靠可解释的规则打分（见 score_row），不用模型；模型只负责后续"整合需求"。
    · 默认排除 tag 为 void / archive 的条目（作废素材、归档件），需要时用 --include-tags 放行。
"""
import argparse
import csv
import json
import os
import re
import sys
from datetime import datetime

PROJ = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
LIBRARY_DIR = os.path.join(PROJ, "_art_process", "VisualLibrary")
INDEX_CSV = os.path.join(LIBRARY_DIR, "index.csv")
SHOT_DEFS_CSV = os.path.join(LIBRARY_DIR, "shot_defs.csv")

CJK_FONTS = [
    r"C:\Windows\Fonts\msyh.ttc",
    r"C:\Windows\Fonts\simhei.ttf",
    r"C:\Windows\Fonts\msyhbd.ttc",
]

# token 切分：ASCII 词 + 整串（中文标题靠整串命中）
TOKEN_RE = re.compile(r"[A-Za-z0-9_]{2,}")

# 中文需求词 → 索引里的规范标签。需求是中文、文件名是英文，没有这张表就永远对不上。
SYNONYMS = {
    "节点": ["node", "talent"],
    "天赋": ["talent"],
    "技能": ["skill", "active", "qte"],
    "主动": ["active"],
    "大招": ["ultimate", "skill"],
    "图标": ["icon"],
    "界面": ["ui", "hud", "panel"],
    "弹窗": ["popup", "ui", "panel"],
    "三选一": ["choice", "reward", "upgrade"],
    "冷却": ["cooldown"],
    "面板": ["panel", "ui"],
    "按钮": ["btn", "button"],
    "页签": ["tab"],
    "详情": ["detail", "card"],
    "敌人": ["enemy"],
    "怪": ["enemy"],
    "武将": ["player", "hero"],
    "战斗": ["combat", "battle"],
    "场景": ["scene", "environment"],
    "环境": ["environment", "scene"],
    "预览": ["preview", "overview"],
    "整图": ["preview", "overview", "full"],
    "参考": ["reference"],
    "素材": ["asset", "source"],
    "血条": ["hp", "health", "bar"],
    "伤害": ["damage"],
    "跳字": ["damage", "number"],
    "连击": ["combo"],
    "BUFF": ["buff"],
    "QTE": ["qte"],
    "选关": ["stage_select", "stage"],
    "落地页": ["hub", "landing", "menu"],
    "菜单": ["menu", "hub"],
    "预览图": ["preview"],
    "特效": ["effect", "vfx"],
    "格子": ["grid", "slot"],
    "进度": ["progress", "bar"],
    "路线": ["route", "scroll", "node"],
    "卷轴": ["scroll", "route"],
    "草": ["grass", "vegetation"],
    "树": ["tree", "scenery"],
    "风": ["wind"],
    "剑": ["slash", "blade"],
    "枪": ["stab", "spear"],
    "突刺": ["stab", "pierce"],
    "挑飞": ["launch"],
    "格挡": ["parry", "block"],
    "盾": ["shield"],
}

# 需求正文里出现的通用英文词会误命中文件名，剔除
STOPWORDS = {
    "with", "and", "the", "for", "of", "new", "old", "final", "copy", "img",
    "png", "jpg", "image", "file", "src", "raw", "tmp", "temp", "asset",
    "assets", "shot", "shots", "ref", "refs", "srcs", "www", "com", "org",
    "this", "that", "from", "into", "out", "not", "you", "are", "can",
}

# 素材角色的打分权重（整图/参考优先，零件靠标题命中再顶上来）
ROLE_WEIGHT = {
    "preview": 10.0,
    "reference": 6.0,
    "sprite": 3.0,
    "source": 2.0,
    "component": 1.0,
    "ingame": 20.0,
    "archive": -20.0,
    "void": -50.0,
}

# 需求里出现这些词 = 问的是"游戏里实际长什么样" → 实机截图必须压倒离线美术稿
REALITY_KEYWORDS = [
    "实机", "实景", "现状", "真机", "游戏里", "游戏内", "在游戏中", "运行时",
    "实际显示", "真实显示", "当前画面", "实拍", "实机截图", "真实截图",
]


def tokens(text):
    text = (text or "").strip()
    if not text:
        return []
    out = [t.lower() for t in TOKEN_RE.findall(text) if t.lower() not in STOPWORDS]
    whole = text.lower()
    if whole and whole not in out:
        out.append(whole)
    return out


def read_csv(path):
    if not os.path.exists(path):
        return []
    with open(path, "r", encoding="utf-8-sig", newline="") as f:
        return list(csv.DictReader(f))


def load_shot_defs():
    defs = {}
    for r in read_csv(SHOT_DEFS_CSV):
        sid = (r.get("shot_id") or "").strip()
        if sid:
            defs[sid] = r
    return defs


def query_tokens(req_text):
    """从需求正文抽出可匹配的 token：ASCII 词 + 中文同义词展开的规范标签。"""
    low = req_text.lower()
    q = set(t.lower() for t in TOKEN_RE.findall(req_text))
    for zh, canon in SYNONYMS.items():
        if zh.lower() in low:
            q.update(canon)
    return q


def score_row(row, req_text, req_lower, q_tokens, forced, wants_reality=False):
    """给一条索引记录打分。返回 (score, reasons)。"""
    rid = (row.get("id") or "").strip()
    if rid in forced:
        return 1000.0, ["--shot 强制指定"]

    score = 0.0
    reasons = []
    tags = [t.strip() for t in (row.get("tags") or "").split(";") if t.strip()]
    title = (row.get("title") or "").strip()
    desc = (row.get("desc") or "").strip()

    # id 直接出现在需求里
    if rid and rid.lower() in req_lower:
        score += 120
        reasons.append("需求中出现 id")
    short = rid[4:] if rid.startswith("art_") else rid
    if len(short) > 6 and short.lower() in req_lower:
        score += 60
        reasons.append("需求中出现短 id")

    hit_tags = [t for t in tags if t.lower() in req_lower or t.lower() in q_tokens]
    if hit_tags:
        score += 6.0 * len(hit_tags)
        reasons.append("标签命中 " + ",".join(hit_tags[:4]))

    hit_title = [t for t in tokens(title) if len(t) >= 2 and (t in req_lower or t in q_tokens)]
    if hit_title:
        score += 4.0 * min(len(hit_title), 6)
        reasons.append("标题命中 " + ",".join(hit_title[:4]))

    hit_desc = [t for t in tokens(desc) if len(t) >= 2 and (t in req_lower or t in q_tokens)]
    if hit_desc:
        score += 2.0 * min(len(hit_desc), 5)
        reasons.append("描述命中 " + ",".join(hit_desc[:3]))

    role = (row.get("role") or "").strip()
    w = ROLE_WEIGHT.get(role, 0.0)
    if w:
        score += w
        reasons.append("角色=%s(%+.0f)" % (role, w))
    if (row.get("kind") or "") == "shot":
        score += 3.0
        reasons.append("实机截图（现状证据）")

    # 当前成品目录（production / final）优先于历史迭代
    low_path = (row.get("path") or "").replace("\\", "/").lower()
    if "production" in low_path or "_final" in low_path or "/final/" in low_path:
        score += 5.0
        reasons.append("当前成品目录(+5)")

    # 需求问的是"实机/现状" → 实机截图硬优先，不许离线稿冒充实机
    if wants_reality and role == "ingame":
        score += 30.0
        reasons.append("需求问实机/现状 → 实机截图优先(+30)")
    elif wants_reality and role in ("preview", "component", "source", "sprite"):
        score -= 8.0
        reasons.append("需求问实机/现状 → 离线稿降权(-8)")

    # 面积：整图比零件更有信息量（同分时整图优先）
    try:
        area = int(row.get("width") or 0) * int(row.get("height") or 0)
        if area:
            bonus = min(5.0, area / 1_000_000.0)
            score += bonus
            reasons.append("尺寸 %.1fM px(%+.1f)" % (area / 1_000_000.0, bonus))
    except Exception:
        pass

    # 新鲜度：越新越优（指数衰减），同分时新的优先
    cap = (row.get("captured_at") or "")[:10]
    try:
        days = (datetime.now() - datetime.strptime(cap, "%Y-%m-%d")).days
        if days >= 0:
            bonus = 3.0 * (0.5 ** (days / 7.0))
            score += bonus
            reasons.append("新鲜度 %.0f天(%+.1f)" % (days, bonus))
    except Exception:
        pass

    return score, reasons


def readable_name(title, maxlen=42):
    """产物文件名用可读标题（保留 id 在 manifest 里），避免长 slug。"""
    s = re.sub(r"[\\/:*?\"<>|\s]+", "_", (title or "").strip())
    s = s.strip("_")
    return (s[:maxlen] or "image")


def _neg_time(row):
    """同分时新的优先（captured_at 倒序）。"""
    return "".join(chr(255 - ord(c)) for c in (row.get("captured_at") or "")[:19])


def parse_crop(text):
    if not text:
        return None
    try:
        parts = [float(x) for x in text.replace(" ", "").split(",")]
    except Exception:
        return None
    if len(parts) != 4:
        return None
    l, t, r, b = parts
    if not (0 <= l < r <= 1 and 0 <= t < b <= 1):
        return None
    return l, t, r, b


def pick_font(size):
    from PIL import ImageFont
    for p in CJK_FONTS:
        if os.path.exists(p):
            try:
                return ImageFont.truetype(p, size)
            except Exception:
                continue
    return ImageFont.load_default()


def prepare_image(src_abs, out_abs, crop, long_edge, bg, max_pixels=4_000_000):
    from PIL import Image
    im = Image.open(src_abs)
    if im.mode in ("RGBA", "LA", "P"):
        im = im.convert("RGBA")
        bgimg = Image.new("RGBA", im.size, bg)
        im = Image.alpha_composite(bgimg, im)
    im = im.convert("RGB")

    if crop:
        w, h = im.size
        l, t, r, b = crop
        im = im.crop((int(l * w), int(t * h), int(r * w), int(b * h)))

    w, h = im.size
    if long_edge and max(w, h) > long_edge:
        scale = long_edge / float(max(w, h))
        im = im.resize((max(1, int(w * scale)), max(1, int(h * scale))), Image.LANCZOS)

    if im.size[0] * im.size[1] > max_pixels:
        scale = (max_pixels / float(im.size[0] * im.size[1])) ** 0.5
        im = im.resize((max(1, int(im.size[0] * scale)), max(1, int(im.size[1] * scale))), Image.LANCZOS)

    os.makedirs(os.path.dirname(out_abs), exist_ok=True)
    im.save(out_abs, "PNG", optimize=True)
    return im.size


def build_contact_sheet(items, out_abs, cell_w=380, label_h=54, cols=4, bg="#1c1c1c"):
    """items: [(idx, title, caption, pil_image)]"""
    from PIL import Image, ImageDraw
    font = pick_font(17)
    font_b = pick_font(19)
    cells = []
    for idx, title, caption, im in items:
        scale = cell_w / float(im.size[0])
        th = max(1, int(im.size[1] * scale))
        cells.append((idx, title, caption, im.resize((cell_w, th), Image.LANCZOS)))
    rows = (len(cells) + cols - 1) // cols
    row_heights = []
    for r in range(rows):
        row_heights.append(max((c[3].size[1] for c in cells[r * cols:(r + 1) * cols]), default=0) + label_h)
    sheet_w = cols * cell_w + (cols + 1) * 10
    sheet_h = sum(row_heights) + (rows + 1) * 10
    sheet = Image.new("RGB", (sheet_w, sheet_h), bg)
    d = ImageDraw.Draw(sheet)
    y = 10
    for r in range(rows):
        x = 10
        for c in cells[r * cols:(r + 1) * cols]:
            idx, title, caption, im = c
            d.rectangle([x - 1, y - 1, x + cell_w, y + im.size[1]], outline="#4a4a4a")
            sheet.paste(im, (x, y))
            d.text((x + 2, y + im.size[1] + 4), "%02d  %s" % (idx, title[:22]), font=font_b, fill="#ffd98a")
            d.text((x + 2, y + im.size[1] + 26), (caption or "")[:40], font=font, fill="#c8c8c8")
            x += cell_w + 10
        y += row_heights[r] + 10
    sheet.save(out_abs, "PNG", optimize=True)
    return sheet.size


def main(argv):
    ap = argparse.ArgumentParser(description="从视觉库选素材并打成可直接贴给网页 GPT 的包")
    ap.add_argument("--req", action="append", default=[], help="需求文档（可重复），md/txt")
    ap.add_argument("--out", default="", help="产物目录（相对工程根或绝对路径）")
    ap.add_argument("--bundle", default="", help="产物目录名，写到 _art_process/VisualLibrary/bundles/<名>")
    ap.add_argument("--max-images", type=int, default=6, help="最多几张图（默认 6）")
    ap.add_argument("--long-edge", type=int, default=1080, help="缩放后长边像素（默认 1080）")
    ap.add_argument("--mode", choices=["individual", "montage"], default="individual",
                    help="individual=逐张；montage=只贴拼图")
    ap.add_argument("--shot", default="", help="强制指定的 id，逗号分隔")
    ap.add_argument("--tags", default="", help="只要带这些标签的条目，逗号分隔")
    ap.add_argument("--kind", default="", help="只要 shot 或 art，逗号分隔")
    ap.add_argument("--role", default="", help="只要这些角色的条目：preview,reference,component,sprite,source,ingame")
    ap.add_argument("--include-tags", default="", help="放行默认排除的标签（默认 void,archive）")
    ap.add_argument("--crop", action="append", default=[], help="覆盖裁剪：id=l,t,r,b")
    ap.add_argument("--bg", default="#808080", help="透明区域填充色")
    ap.add_argument("--dry-run", action="store_true", help="只打印选图结果，不写文件")
    args = ap.parse_args(argv)

    if not args.req:
        print("错误：至少给一个 --req", file=sys.stderr)
        return 2
    if not args.out and not args.bundle:
        print("错误：需要 --bundle <名> 或 --out <目录>", file=sys.stderr)
        return 2
    if args.bundle and args.out:
        print("错误：--bundle 与 --out 二选一", file=sys.stderr)
        return 2

    req_texts = []
    for rp in args.req:
        if not os.path.exists(rp):
            print("错误：需求文档不存在 %s" % rp, file=sys.stderr)
            return 2
        with open(rp, "r", encoding="utf-8-sig") as f:
            req_texts.append((rp, f.read()))
    req_all = "\n".join(t for _, t in req_texts)
    req_lower = req_all.lower()

    rows = read_csv(INDEX_CSV)
    if not rows:
        print("错误：索引为空，先跑 Tools/visual_brief/index_art_assets.py", file=sys.stderr)
        return 2
    shot_defs = load_shot_defs()

    forced = set(x.strip() for x in args.shot.split(",") if x.strip())
    want_tags = set(x.strip().lower() for x in args.tags.split(",") if x.strip())
    want_kinds = set(x.strip().lower() for x in args.kind.split(",") if x.strip())
    want_roles = set(x.strip().lower() for x in args.role.split(",") if x.strip())
    allow_tags = set(x.strip().lower() for x in args.include_tags.split(",") if x.strip())
    block_tags = ({"void", "archive"} - allow_tags)

    crops = {}
    for c in args.crop:
        if "=" in c:
            k, v = c.split("=", 1)
            crops[k.strip()] = v.strip()

    scored = []
    q_tokens = query_tokens(req_all)
    wants_reality = any(k in req_lower for k in REALITY_KEYWORDS)
    for r in rows:
        if (r.get("status") or "") == "missing":
            continue
        rtags = set(t.strip().lower() for t in (r.get("tags") or "").split(";") if t.strip())
        if rtags & block_tags and (r.get("id") or "") not in forced:
            continue
        if want_kinds and (r.get("kind") or "").lower() not in want_kinds:
            continue
        if want_tags and not (rtags & want_tags):
            continue
        if want_roles and (r.get("role") or "").lower() not in want_roles:
            continue
        s, reasons = score_row(r, req_all, req_lower, q_tokens, forced, wants_reality)
        if s <= 0 and (r.get("id") or "") not in forced:
            continue
        scored.append((s, r, reasons))
    scored.sort(key=lambda x: (-x[0], _neg_time(x[1]), x[1].get("id", "")))

    picked = scored[:max(1, args.max_images)]
    if not picked:
        print("没有匹配到任何素材。--dry-run 也没输出。")
        return 1

    print("候选 %d 条，取前 %d 条：" % (len(scored), len(picked)))
    for s, r, reasons in picked:
        print("  %7.1f  %-52s %s" % (s, r.get("id", "")[:52], (r.get("title") or "")[:24]))
        if reasons:
            print("           └ %s" % "; ".join(reasons[:3]))

    if args.dry_run:
        return 0

    out_dir = args.out if args.out else os.path.join(
        "_art_process", "VisualLibrary", "bundles", args.bundle)
    out_dir = out_dir if os.path.isabs(out_dir) else os.path.join(PROJ, out_dir)
    images_dir = os.path.join(out_dir, "images")
    os.makedirs(images_dir, exist_ok=True)

    manifest = {"bundle": os.path.relpath(out_dir, PROJ).replace("\\", "/"),
                "generated_at": datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
                "requirements": [os.path.relpath(p, PROJ).replace("\\", "/") for p, _ in req_texts],
                "long_edge": args.long_edge, "mode": args.mode, "items": []}

    sheet_items = []
    for i, (s, r, reasons) in enumerate(picked, start=1):
        rid = r.get("id", "")
        rel_path = r.get("path", "")
        src_abs = os.path.join(PROJ, rel_path.replace("/", os.sep))
        if not os.path.exists(src_abs):
            print("  跳过（文件缺失）：%s" % rel_path)
            continue
        crop = None
        if rid in shot_defs:
            crop = parse_crop(shot_defs[rid].get("crop"))
        if rid in crops:
            crop = parse_crop(crops[rid])
        out_name = "%02d_%s.png" % (i, readable_name(r.get("title") or rid))
        out_abs = os.path.join(images_dir, out_name)
        try:
            size = prepare_image(src_abs, out_abs, crop, args.long_edge, args.bg)
        except Exception as e:
            print("  处理失败 %s：%s" % (rel_path, e))
            continue
        caption = (r.get("title") or rid).strip()
        manifest["items"].append({
            "no": i, "id": rid, "kind": r.get("kind", ""), "title": caption,
            "source": rel_path, "out": "images/" + out_name,
            "tags": r.get("tags", ""), "score": round(s, 2),
            "why": reasons[:4], "size": "%dx%d" % size,
        })
        if args.mode == "montage":
            from PIL import Image
            sheet_items.append((i, caption, r.get("desc", "") or r.get("kind", ""),
                                Image.open(out_abs).copy()))
        print("  写出 %s（%dx%d）" % (out_name, size[0], size[1]))

    if args.mode == "montage" and sheet_items:
        cs = os.path.join(out_dir, "contact_sheet.png")
        sz = build_contact_sheet(sheet_items, cs)
        print("  写出 contact_sheet.png（%dx%d，%d 张）" % (sz[0], sz[1], len(sheet_items)))

    with open(os.path.join(out_dir, "manifest.json"), "w", encoding="utf-8") as f:
        json.dump(manifest, f, ensure_ascii=False, indent=2)

    # 需求原文副本
    with open(os.path.join(out_dir, "source_requirement.md"), "w", encoding="utf-8") as f:
        for p, t in req_texts:
            f.write("<!-- 来源：%s -->\n\n" % p)
            f.write(t)
            f.write("\n\n---\n\n")

    # 粘贴顺序
    with open(os.path.join(out_dir, "attach_order.txt"), "w", encoding="utf-8") as f:
        f.write("粘贴顺序（编号与文件名、brief.md 图注一致）\n")
        for it in manifest["items"]:
            f.write("%02d  %s  <- %s\n" % (it["no"], it["out"], it["title"]))
        if args.mode == "montage" and sheet_items:
            f.write("\nmontage 模式：只贴 contact_sheet.png 一张即可，编号见图内黄字。\n")

    # brief 骨架
    with open(os.path.join(out_dir, "brief.md"), "w", encoding="utf-8") as f:
        f.write("# 需求整合稿（自动生成骨架，需复核后再贴给网页 GPT）\n\n")
        f.write("- 生成时间：%s\n" % manifest["generated_at"])
        f.write("- 来源需求：%s\n" % "、".join(manifest["requirements"]))
        f.write("- 素材数量：%d\n\n" % len(manifest["items"]))
        f.write("> 本文件只放**需求**。方案、实现、美术建议一律不在这里出——那是网页 GPT 的活。\n\n")
        f.write("## 1. 目标\n\n（从需求原文提炼：玩家/使用者能感知到的结果）\n\n")
        f.write("## 2. 现状（只写看得见的事实，不推测）\n\n")
        for it in manifest["items"]:
            f.write("- 图 %02d　%s　`%s`\n" % (it["no"], it["title"], it["source"]))
            if it["why"]:
                f.write("  - 关联依据：%s\n" % "；".join(it["why"]))
        f.write("\n## 3. 约束（原文明确写出的硬性条件）\n\n")
        f.write("## 4. 验收（可检查的现象）\n\n")
        f.write("## 5. 不做（原文明确排除的）\n\n")
        f.write("## 6. 未决问题（原文没写、但会影响方案选择的点；给选项，不给推荐）\n\n")
        f.write("## 附：素材清单\n\n")
        f.write("| # | id | 标题 | 类型 | 源文件 | 输出 | 尺寸 |\n")
        f.write("|---|---|---|---|---|---|---|\n")
        for it in manifest["items"]:
            f.write("| %02d | `%s` | %s | %s | `%s` | `%s` | %s |\n" % (
                it["no"], it["id"], it["title"], it["kind"], it["source"], it["out"], it["size"]))

    print("\n产物目录：%s" % os.path.relpath(out_dir, PROJ).replace("\\", "/"))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
