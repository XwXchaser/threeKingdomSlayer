"""天赋界面参考图 —— 第 5 轮（v4 材质已达标，只修节点树）。

v5 = 以 v4 的产出为底图做**定向修改**：
  · 图 1 = v4 产出（材质、版式、页签、面板、通栏朱红按钮全部已正确，保留不动）
  · 图 2 = 单个兵符实物放大图（材质锚；v4 用的「三个令牌并排」本身在暗示三列，是节点树排错的元凶）
  · 唯一改动：把节点树换成我们的 6 个天赋节点，呈菱形（顶 1 / 中左 2 右 2 / 底 1），
    连线由下往上汇聚到顶部节点；多余节点与其标签条一并删掉

同时带上 response_format=b64_json —— 图片 CDN (img.huaruics.com) 从本机不可达，
2026-11-02 起改用 base64 直回，绕开 CDN。
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
# 图 1 = v4 产出（要改的界面）
SRC_BASE = r"C:\Users\steam\Pictures\gptGen\talent_ui_v4\talent_ui_mock_v4.png"
# 图 2 = 单个兵符实物放大（材质锚）
SRC_TOKEN = os.path.join(ROOT, "Library/Locus/tmp/talent_ui/hud_token_single_ref.png")
OUT_DIR = r"C:\Users\steam\Pictures\gptGen\talent_ui_v5"
IMG_OUT = os.path.join(OUT_DIR, "talent_ui_mock_v5.png")
ENDPOINT = "https://api.muskapis.com/v1/images/edits"
MODEL = "gpt-image-2.6-sunburst"
TIMEOUT = 900

PROMPT_EN = """Edit image 1. Keep EVERYTHING else exactly as it is - same background, same header, same title block, same hairline rule, same three plain-text tabs with the underline on the first one, same bottom panel, same vermilion full-width button, same flat near-black tablets with their thin cream hairline outlines, same typography and colour. Do not restyle anything.

CHANGE ONLY THE NODE TREE.

The screen currently shows three vertical columns of tablets. Replace the whole node tree with exactly SIX tablets arranged as a diamond:
- one tablet at the top centre
- two tablets in the row below it, one on the left and one on the right
- two more tablets in the next row down, one on the left and one on the right
- one tablet at the bottom centre
- delete all the other tablets and their label bars, so exactly six remain

The connectors are thin cream hairlines running upward: the bottom-centre tablet links up to the two tablets of the row above it, each of those links up to the two tablets of the next row, and both of those link up and merge into the single top-centre tablet. No other lines.

Keep the tablets looking exactly like image 2 and like the ones already in image 1: flat near-black face (#1A1A1E), a thin cream hairline outline (#F2EDE0) about 3% of the tablet width, a small plain cream ring hole near the top of the crown, a slightly darker black circle in the lower half holding a simple cream pictogram, tall proportion (height about 1.55x the width).

The six tablets must be visually distinguishable by state, by outline colour only, the faces staying black:
- the top-centre tablet = unlocked: bright cream-gold outline (#F0DFB4)
- the two tablets on the right = available: warm bronze outline (#C9A97A)
- the two tablets on the left and the bottom-centre tablet = locked: cold grey outline (#8C8A86)

Under every tablet keep two small flat label bars - the upper, wider one for the name and the lower, narrower one for the price - exactly as they are now, just six pairs instead of ten.

Leave every label area, the panel's text slots and the button as clean empty shapes with no text, letters, numbers or Chinese characters. Do not add any new icons or symbols beyond the small pictograms inside the tablets."""

PROMPT_ZH = """
编辑【图 1】。**其余一切保持原样** —— 同样的背景、同样的顶部、同样的标题块、同样的细金线、
同样的三个纯文字页签（第一个带下划线）、同样的底部面板、同样的朱红通栏按钮、
同样的平面近黑兵符加米白细描边、同样的字体与配色。**不要重新设计任何东西。**

【只改节点树】

现在画面上是三条竖排的节点牌。把整棵节点树换成**正好六个**节点，排成菱形：
- 最上方正中 1 个
- 下一行左右各 1 个，共 2 个
- 再下一行左右各 1 个，共 2 个
- 最下方正中 1 个
- 其余节点牌和它们的标签条全部删掉，最后**只剩六个**

连线是细奶白发丝线，由下往上：最下方正中那个往上连到上一行左右两个，上一行的左右两个
各自往上连到再上一行的左右两个，再上一行的两个汇合到最上方正中那一个。除此之外没有别的线。

兵符的样子保持和【图 2】以及图 1 里的完全一样：平面近黑牌面（#1A1A1E）、
米白细描边（#F2EDE0，约牌宽 3%）、顶部冠上一个小素奶白圆环孔、
下半部一个稍深的黑圆里放一个简单的奶油色小图形、竖长比例（高约为宽的 1.55 倍）。

六个节点要能按状态分开，**只改描边颜色**，牌面始终是黑的：
- 最上方正中那个 = 已解锁：亮奶金描边（#F0DFB4）
- 右侧两个 = 可解锁：暖铜描边（#C9A97A）
- 左侧两个和最下方正中那个 = 锁定：冷灰描边（#8C8A86）

每个牌下面保留两条小小的扁平标签条 —— 上面一条宽的是名称位，下面一条窄的是价格位 ——
和现在一模一样，只是从十对变成六对。

所有标签位置、面板里的文字位、按钮都保持为干净的空形状，不要任何文字、字母、数字、汉字。
不要新增任何图标或符号，牌内已有的小图形保持原样。
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

    inputs = [
        (SRC_BASE, "v4_base_input.png",
         "image 1: the screen to edit - keep everything, change only the node tree"),
        (SRC_TOKEN, "hud_token_single_ref.png",
         "image 2: node material anchor - our DEPLOYED battle-HUD token, enlarged (flat #1A1A1E face, #F2EDE0 hairline, plain ring hole)"),
    ]
    files = []
    for src, name, _role in inputs:
        dst = os.path.join(OUT_DIR, name)
        shutil.copyfile(src, dst)
        files.append(("image[]", name, dst, "image/png"))

    fields = [
        ("model", MODEL),
        ("prompt", PROMPT),
        ("size", "1024x1536"),
        ("quality", "max"),
        ("input_fidelity", "high"),
        ("output_format", "png"),
        ("n", "1"),
        ("response_format", "b64_json"),   # 绕开不可达的图片 CDN
    ]
    body, boundary = build_multipart(fields, files)

    with open(os.path.join(OUT_DIR, "talent_ui_mock_v5.prompt.txt"), "w",
              encoding="utf-8", newline="\n") as fh:
        fh.write(PROMPT)

    req_meta = {
        "version": "talent_ui_mock_v5",
        "endpoint": ENDPOINT,
        "method": "POST",
        "content_type": f"multipart/form-data; boundary=<{boundary}>",
        "fields": {"model": MODEL, "size": "1024x1536", "quality": "max",
                   "input_fidelity": "high", "output_format": "png", "n": "1",
                   "response_format": "b64_json (workaround: image CDN unreachable from this host)"},
        "prompt_sha256": hashlib.sha256(PROMPT.encode("utf-8")).hexdigest(),
        "prompt_chars": len(PROMPT),
        "negative_prompt": None,
        "images": [
            {"order": i, "field": "image[]", "filename": files[i - 1][1],
             "path": files[i - 1][2], "role": inputs[i - 1][2],
             "sha256": sha256_file(files[i - 1][2]),
             "bytes": os.path.getsize(files[i - 1][2])}
            for i in range(1, len(inputs) + 1)
        ],
        "body_bytes": len(body),
        "automatic_retries": 0,
        "read_timeout_seconds": TIMEOUT,
        "submitted_at_local": time.strftime("%Y-%m-%d %H:%M:%S"),
    }
    with open(os.path.join(OUT_DIR, "talent_ui_mock_v5.request.json"), "w", encoding="utf-8") as fh:
        json.dump(req_meta, fh, indent=2, ensure_ascii=False)

    request = urllib.request.Request(
        ENDPOINT, data=body, method="POST",
        headers={"Authorization": f"Bearer {key}",
                 "Content-Type": f"multipart/form-data; boundary={boundary}"})
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
        with open(os.path.join(OUT_DIR, "talent_ui_mock_v5.status.json"), "w", encoding="utf-8") as fh:
            json.dump(status, fh, indent=2, ensure_ascii=False)
        raise SystemExit(status["error"])

    status["seconds"] = round(time.time() - t0, 2)
    payload = json.loads(raw.decode("utf-8"))
    with open(os.path.join(OUT_DIR, "talent_ui_mock_v5.response.json"), "w", encoding="utf-8") as fh:
        json.dump(payload, fh, indent=2, ensure_ascii=False)

    item = payload["data"][0]
    b64 = item.get("b64_json")
    url = item.get("url")
    blob = None
    if b64:
        blob = base64.b64decode(b64)
        status["download"] = "b64_json"
    elif url:
        status["download"] = "url"
        status["image_url"] = url
        raise SystemExit("API returned url again; fetch manually from response.json")
    else:
        status["state"] = "failed"
        status["error"] = "no image payload in response"
        with open(os.path.join(OUT_DIR, "talent_ui_mock_v5.status.json"), "w", encoding="utf-8") as fh:
            json.dump(status, fh, indent=2, ensure_ascii=False)
        raise SystemExit(status["error"])

    with open(IMG_OUT, "wb") as fh:
        fh.write(blob)

    out_bytes = open(IMG_OUT, "rb").read()
    with Image.open(IMG_OUT) as im:
        status["width"], status["height"] = im.size
        status["color_type"] = im.mode
    status["output_bytes"] = len(out_bytes)
    status["output_sha256"] = hashlib.sha256(out_bytes).hexdigest()
    status["png_signature"] = out_bytes[:8] == b"\x89PNG\r\n\x1a\n"
    status["usage"] = payload.get("usage")
    status["echoed_size"] = payload.get("size")
    status["echoed_quality"] = payload.get("quality")
    status["created"] = payload.get("created")
    status["state"] = "image_saved"
    with open(os.path.join(OUT_DIR, "talent_ui_mock_v5.status.json"), "w", encoding="utf-8") as fh:
        json.dump(status, fh, indent=2, ensure_ascii=False)
    print(json.dumps(status, indent=2, ensure_ascii=False))


if __name__ == "__main__":
    main()
