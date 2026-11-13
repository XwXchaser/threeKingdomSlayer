"""天赋界面「整体参考图」生成 —— 第 1 轮（单张探针）。

参数口径（项目默认，见 plan/combat-ui-reference-image-generation-plan-20261030.md §6.1）：
  model=gpt-image-2.6-sunburst / endpoint=edits / quality=max / input_fidelity=high
  size=1024x1536 / output_format=png / n=1 / background 不设（整屏稿不需原生透明）

输入图职责：
  image 1 = 程序化版式底图（无文字版）→ 只负责「版式 + 兵符剪影 + 节点位置」

纪律：
  · MUSK_API_KEY 只从环境变量取，绝不写进任何文件
  · 不覆盖已有产物
  · 先落盘图片，再落盘响应 JSON（避免 b64 被覆盖的付费坑）
"""
import base64
import hashlib
import json
import os
import shutil
import time
import urllib.request
import uuid

from PIL import Image

ROOT = r"C:\threeKingdomSlayer\threeKingdomSlayer"
SRC_BASE = os.path.join(ROOT, "Library/Locus/tmp/talent_ui/talent_mock_base_notext.png")
OUT_DIR = r"C:\Users\steam\Pictures\gptGen\talent_ui_v1"
IMG_IN = os.path.join(OUT_DIR, "talent_ui_base_input.png")
IMG_OUT = os.path.join(OUT_DIR, "talent_ui_mock_v1.png")
ENDPOINT = "https://api.muskapis.com/v1/images/edits"
MODEL = "gpt-image-2.6-sunburst"
TIMEOUT = 900

PROMPT_EN = """Redraw this portrait mobile-game UI wireframe as a finished concept screen for a Three Kingdoms action game: the "Talent / Ability" screen. One full screen, vertical, 1024x1536.

KEEP THE LAYOUT EXACTLY as in the input image - same element count, same positions, same hierarchy:
- Top-left: one blank title slot. Top-right: one round coin with a square hole and a blank number slot.
- Below the title: a row of three tabs. The left and middle tabs are dim text blocks. The right tab is a brighter text block with a single gold underline; the other two have no underline.
- Middle: a diamond-shaped branching tree of 6 tall command-tally plaques. One at top center, two on the left and two on the right in the row below, one at bottom center.
- 4 thin brass connector lines run upward and merge into the top plaque.
- Under every plaque there are two short blank bars (the upper, longer bar = label; the lower, shorter bar = price). Keep them blank.
- Bottom: one wide horizontal detail card. Inside on the left a small command-tally thumbnail, then a few blank text lines, and on the far right a gold-outlined button with a blank interior.
- The whole tree area is a near-black ink panel. Nothing else in the screen.

THE PLAQUE SHAPE (an ancient Chinese "bingfu" / tiger-tally bronze command token):
- Tall vertical proportion, height about 1.55x the width.
- Rounded arched crown on top, with ONE round cord hole at the center just below the crown; the hole shows the background through it.
- Straight-sided body, rounded-off bottom.
- Thick outline, about 5% of the plaque width, closed on all four sides.
- The small pictogram already inside each plaque stays a simple pictogram in the lower half of the plaque, below the cord hole.

MATERIAL AND PALETTE - Three Kingdoms battlefield plus ink-and-metal UI language:
- Background: near-black ink (#131318 area) with very light horizontal brush grain. No gradients, no floor, no scenery.
- Plaques: single-tone brass / bronze metal, one soft highlight band from the upper left plus one shadow side. No mirror reflections, no rainbow tint, no rivets, no filigree, no ornamental pattern, no seal stamp, no engraved dragon.
- Three states must read instantly:
  * the top plaque = unlocked: bright golden brass with a warm glow in the cord hole
  * the two right-hand plaques = available: normal brass
  * the two left-hand plaques and the bottom plaque = locked: cold grey dark bronze, visibly dimmed
- Connectors: thin solid brass lines, no glow.
- Detail card: near-black fill with a thin brass outline. Button: dark bronze fill with a bright gold outline.

DO NOT add any new icons or symbols anywhere; the only pictograms are the small ones already inside the plaques. Do not draw any text, letters, numbers or Chinese characters. No characters, no soldiers, no battlefield, no gradients, no neon, no cartoon, no chibi, no ink-wash painting, no scrolls, no checkerboard, no white background."""

PROMPT_ZH = """
【中文补充要求 —— 与上面英文同义，逐条对齐】
把这张竖屏 UI 版式草稿重绘成一张正式的三国题材动作手游「天赋」界面参考图（整屏 1024x1536）。

【必须严格保留版式】与输入图完全一致的元素数量、位置与层级：
· 顶部左上角一个标题字位（留空），右上角一个圆形方孔铜钱和数字位（留空）
· 标题下方一行三个页签：左、中两个是暗色文字块；右边一个是亮色文字块并带一条金色下划线，另外两个不要下划线
· 中部是一棵菱形分叉树：最上方正中 1 个兵符节点，下面左右各 2 个共 4 个，最底部正中 1 个，共 6 个
· 4 条细铜色连线由下往上汇聚到最上方的节点
· 每个兵符节点正下方有两行短横条（上面一行长＝名称，下面一行短＝价格），横条保持留白，不要画任何字
· 底部一条横向详情卡：卡内左侧一个小兵符缩略图，右侧几行留白文字块，最右侧一个金色描边的按钮，按钮内留白
· 整棵树位于近黑墨色面板上，画面里没有别的东西

【兵符造型】中国古代「兵符／虎符」形铜牌：
· 竖长比例，高约为宽的 1.55 倍
· 顶部是圆拱冠，冠下正中一个圆形穿绳孔，孔能透出背景色
· 牌身是直筒，底部圆弧收口
· 轮廓有粗描边，边宽约为牌宽的 5%，四边闭合
· 牌内下半部保留牌面中央那个小图形，它只是占位小图形，不要改造成别的图案

【材质与配色】三国战场 + 墨色金属 UI 语言：
· 背景近黑墨色（#131318 附近），带极轻的横向笔刷噪点，不要渐变，不要地面，不要场景
· 兵符是黄铜／青铜单色金属，只有左上方向一条柔和高光带和一条暗部；不要镜面反射、不要彩虹色、不要铆钉、不要花纹、不要印章、不要雕龙
· 三种状态必须一眼能分：
  - 最上方节点＝已解锁：亮金黄铜，孔内透一点暖光
  - 右侧两个节点＝可解锁：正常黄铜色
  - 左侧两个节点和最底部节点＝锁定：冷灰暗铜，整体压暗
· 连线是细铜色直线，不发光
· 详情卡近黑底 + 一圈细铜色描边；按钮暗铜底 + 亮金描边

【禁止】新增任何图标或符号；不要画任何文字、字母、数字、汉字；不要人物、士兵、战场、渐变、霓虹、卡通、萌系、水墨、卷轴、棋盘格、白底
"""

PROMPT = PROMPT_EN + "\n" + PROMPT_ZH


def sha256_file(path):
    with open(path, "rb") as fh:
        return hashlib.sha256(fh.read()).hexdigest()


def build_multipart(fields, files):
    boundary = "----locus" + uuid.uuid4().hex
    buf = bytearray()
    for name, value in fields:
        buf += f"--{boundary}\r\n".encode()
        buf += f'Content-Disposition: form-data; name="{name}"\r\n\r\n'.encode()
        buf += f"{value}\r\n".encode()
    for name, filename, path, ctype in files:
        buf += f"--{boundary}\r\n".encode()
        buf += (f'Content-Disposition: form-data; name="{name}"; filename="{filename}"\r\n').encode()
        buf += f"Content-Type: {ctype}\r\n\r\n".encode()
        with open(path, "rb") as fh:
            buf += fh.read()
        buf += b"\r\n"
    buf += f"--{boundary}--\r\n".encode()
    return bytes(buf), boundary


def main():
    key = os.environ.get("MUSK_API_KEY")
    if not key:
        raise SystemExit("MUSK_API_KEY missing")
    os.makedirs(OUT_DIR, exist_ok=True)
    if os.path.exists(IMG_OUT):
        raise SystemExit(f"refusing to overwrite existing output: {IMG_OUT}")
    shutil.copyfile(SRC_BASE, IMG_IN)

    fields = [
        ("model", MODEL),
        ("prompt", PROMPT),
        ("size", "1024x1536"),
        ("quality", "max"),
        ("input_fidelity", "high"),
        ("output_format", "png"),
        ("n", "1"),
    ]
    files = [("image[]", os.path.basename(IMG_IN), IMG_IN, "image/png")]
    body, boundary = build_multipart(fields, files)

    with open(os.path.join(OUT_DIR, "talent_ui_mock_v1.prompt.txt"), "w",
              encoding="utf-8", newline="\n") as fh:
        fh.write(PROMPT)

    req_meta = {
        "version": "talent_ui_mock_v1",
        "endpoint": ENDPOINT,
        "method": "POST",
        "content_type": f"multipart/form-data; boundary=<{boundary}>",
        "fields": {
            "model": MODEL,
            "size": "1024x1536",
            "quality": "max",
            "input_fidelity": "high",
            "output_format": "png",
            "n": "1",
            "background": "omitted",
        },
        "prompt_sha256": hashlib.sha256(PROMPT.encode("utf-8")).hexdigest(),
        "prompt_chars": len(PROMPT),
        "negative_prompt": None,
        "images": [
            {
                "order": 1,
                "field": "image[]",
                "filename": os.path.basename(IMG_IN),
                "path": IMG_IN,
                "role": "image 1: procedural layout base (no text) -> LAYOUT + plaque silhouette + node positions",
                "sha256": sha256_file(IMG_IN),
                "bytes": os.path.getsize(IMG_IN),
            }
        ],
        "body_bytes": len(body),
        "automatic_retries": 0,
        "read_timeout_seconds": TIMEOUT,
        "submitted_at_local": time.strftime("%Y-%m-%d %H:%M:%S"),
    }
    with open(os.path.join(OUT_DIR, "talent_ui_mock_v1.request.json"), "w", encoding="utf-8") as fh:
        json.dump(req_meta, fh, indent=2, ensure_ascii=False)

    request = urllib.request.Request(
        ENDPOINT, data=body, method="POST",
        headers={"Authorization": f"Bearer {key}",
                 "Content-Type": f"multipart/form-data; boundary={boundary}"},
    )
    status = {"state": "submitted", "http_status": None, "seconds": None,
              "error": None, "timeout_seconds": TIMEOUT, "output_image": IMG_OUT}
    t0 = time.time()
    try:
        with urllib.request.urlopen(request, timeout=TIMEOUT) as resp:
            raw = resp.read()
            status["http_status"] = resp.status
    except Exception as exc:  # noqa: BLE001
        status["state"] = "failed"
        status["error"] = f"{type(exc).__name__}: {exc}"
        status["seconds"] = round(time.time() - t0, 2)
        with open(os.path.join(OUT_DIR, "talent_ui_mock_v1.status.json"), "w", encoding="utf-8") as fh:
            json.dump(status, fh, indent=2, ensure_ascii=False)
        raise SystemExit(status["error"])

    status["seconds"] = round(time.time() - t0, 2)
    payload = json.loads(raw.decode("utf-8"))

    item = payload["data"][0]
    b64 = item.get("b64_json")
    url = item.get("url")
    if b64:
        blob = base64.b64decode(b64)
    elif url:
        with urllib.request.urlopen(url, timeout=TIMEOUT) as r:
            blob = r.read()
    else:
        status["state"] = "failed"
        status["error"] = "no image payload in response"
        with open(os.path.join(OUT_DIR, "talent_ui_mock_v1.status.json"), "w", encoding="utf-8") as fh:
            json.dump(status, fh, indent=2, ensure_ascii=False)
        raise SystemExit(status["error"])

    # 先落盘图片，再落盘响应
    with open(IMG_OUT, "wb") as fh:
        fh.write(blob)
    with open(os.path.join(OUT_DIR, "talent_ui_mock_v1.response.json"), "w", encoding="utf-8") as fh:
        json.dump(payload, fh, indent=2, ensure_ascii=False)

    out_bytes = open(IMG_OUT, "rb").read()
    with Image.open(IMG_OUT) as im:
        status["width"], status["height"] = im.size
        status["color_type"] = im.mode
    status["output_bytes"] = len(out_bytes)
    status["output_sha256"] = hashlib.sha256(out_bytes).hexdigest()
    status["png_signature"] = out_bytes[:8] == b"\x89PNG\r\n\x1a\n"
    status["usage"] = payload.get("usage")
    status["response_keys"] = sorted(payload.keys())
    status["echoed_size"] = payload.get("size")
    status["echoed_quality"] = payload.get("quality")
    status["model"] = payload.get("model")
    status["created"] = payload.get("created")
    status["state"] = "image_saved"
    with open(os.path.join(OUT_DIR, "talent_ui_mock_v1.status.json"), "w", encoding="utf-8") as fh:
        json.dump(status, fh, indent=2, ensure_ascii=False)
    print(json.dumps(status, indent=2, ensure_ascii=False))


if __name__ == "__main__":
    main()
