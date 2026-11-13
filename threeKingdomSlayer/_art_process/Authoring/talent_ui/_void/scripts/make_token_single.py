"""单个兵符实物放大图 —— v5 的材质锚。

v4 用的锚图是「三个令牌并排」，那个横排本身就在暗示「三列」，导致模型把节点树排成了三条竖链。
v5 改成单个令牌放大居中，去掉列数暗示。
"""
import os

from PIL import Image

ROOT = "C:/threeKingdomSlayer/threeKingdomSlayer"
OUT_DIR = os.path.join(ROOT, "Library/Locus/tmp/talent_ui")
BG = (22, 22, 26)
W, H = 1024, 640
SCALE = 3.0

src = os.path.join(ROOT, "Assets/Sprites/UI/Combat/hud_slot_token.png")
im = Image.open(src).convert("RGBA")
nw, nh = int(im.width * SCALE), int(im.height * SCALE)
im = im.resize((nw, nh), Image.LANCZOS)

canvas = Image.new("RGB", (W, H), BG)
canvas.paste(im, ((W - nw) // 2, (H - nh) // 2), im)
out = os.path.join(OUT_DIR, "hud_token_single_ref.png")
canvas.save(out)
print("saved", out, canvas.size, "token", nw, "x", nh)
