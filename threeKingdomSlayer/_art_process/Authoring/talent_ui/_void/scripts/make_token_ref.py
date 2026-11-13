"""用已部署的战斗 HUD 令牌实物，拼一张「兵符材质与比例」参考板。

这张图是 v4 生图的第 3 张输入（材质锚），完全来自项目实际部署的资产：
  Assets/Sprites/UI/Combat/hud_slot_token.png        拱顶 + 穿绳孔（现役兵符）
  Assets/Sprites/UI/Combat/hud_slot_token_tally.png   切角变体
  Assets/Sprites/UI/Combat/hud_slot_token_plate.png   外张变体
底色用 HUD 面板的近黑 #16161A，与实机一致。
"""
import os

from PIL import Image

ROOT = "C:/threeKingdomSlayer/threeKingdomSlayer"
OUT_DIR = os.path.join(ROOT, "Library/Locus/tmp/talent_ui")
os.makedirs(OUT_DIR, exist_ok=True)

BG = (22, 22, 26)
W, H = 1024, 460
SCALE = 2.15

SOURCES = [
    "Assets/Sprites/UI/Combat/hud_slot_token.png",
    "Assets/Sprites/UI/Combat/hud_slot_token_tally.png",
    "Assets/Sprites/UI/Combat/hud_slot_token_plate.png",
]

img = Image.new("RGB", (W, H), BG)
x = 26
for rel in SOURCES:
    p = os.path.join(ROOT, rel)
    if not os.path.exists(p):
        print("skip (missing):", rel)
        continue
    im = Image.open(p).convert("RGBA")
    nw, nh = int(im.width * SCALE), int(im.height * SCALE)
    im = im.resize((nw, nh), Image.LANCZOS)
    y = (H - nh) // 2
    img.paste(im, (x, y), im)
    print(f"{rel}  -> {nw}x{nh} at x={x}")
    x += nw + 26

out = os.path.join(OUT_DIR, "hud_token_material_ref.png")
img.save(out)
print("saved", out, img.size)
