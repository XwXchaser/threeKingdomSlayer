"""天赋页整体参考图 · 第 6 轮（按 2026-11-02 交接文档发起）。

依据：`Locus/knowledge/plan/talent-page-reference-generation-handoff-20261102.md`
  · §4 中文正向提示词、§5 English positive prompt、§6 负面词 —— 三者按原文使用
  · §7 参数意图：1 张整屏候选 / PNG / 1024x1536 / quality=max / 不输入任何项目现有美术作参考

接口沿用项目既有 GPT Image 链路（skill/gpt-image-generation.md）：
  POST https://api.muskapis.com/v1/images/generations
  model=gpt-image-2.6-sunburst  size=1024x1536  quality=max  output_format=png  n=1
  response_format=b64_json —— 图片 CDN 从本机不可达（见 talent_ui_v5 记录），改 base64 直回

纪律：只提交一次，不自动重试、不批量生成（交接 §7）。
"""

import base64
import hashlib
import json
import os
import struct
import time
import urllib.error
import urllib.request

ROOT = r"C:\threeKingdomSlayer\threeKingdomSlayer"
OUT_DIR = r"C:/Users/steam/Pictures/gptGen/talent_ui_ref_v6"
IMG_OUT = os.path.join(OUT_DIR, "talent_page_ref_v6.png")
PROMPT_TXT = os.path.join(OUT_DIR, "talent_page_ref_v6.prompt.txt")
REQ_JSON = os.path.join(OUT_DIR, "talent_page_ref_v6.request.json")
RESP_JSON = os.path.join(OUT_DIR, "talent_page_ref_v6.response.json")
STATUS_JSON = os.path.join(OUT_DIR, "talent_page_ref_v6.status.json")

ENDPOINT = "https://api.muskapis.com/v1/images/generations"
MODEL = "gpt-image-2.6-sunburst"
SIZE = "1024x1536"
QUALITY = "max"
TIMEOUT = 900

# ---------------------------------------------------------------- prompts
# §5 English positive prompt（原文）
PROMPT_EN = """Create one complete portrait mobile action-game talent-page concept in a high-definition pixel-art style for a Three Kingdoms war-themed game. It must read as a usable game interface, not as a character illustration or promotional poster.

Take inspiration from the clean information hierarchy associated with The Elder Scrolls: Blades skill UI: clear title, category tabs, talent tree, readable connecting lines and a selected-talent detail area, with generous spacing. Do not copy its node shapes, fantasy ornament or exact interface.

Use layered dark surfaces, carefully designed pixel-stepped contours, deliberate hard-edged color clusters and restrained light-facing planes. Give the interface the finish and visual weight of production game art without photorealism, painterly rendering, excessive wear or ornament. Avoid flat vector boxes, generic software dashboards and placeholder-looking UI.

Reserve space for a title and coin counter at the top, followed by three category tabs with the third category selected. In the central viewport, show distinct charge, displacement and fire talent branches. Use a consistent node-frame family and distinguish locked, available and unlocked states with brightness, edge treatment and limited color accents. Connecting lines must express the actual branches; do not invent cross-branch connections.

Design original high-definition pixel skill symbols for charging, defense, impact, water, cyclone and fire. Each icon has one clear focal subject and remains recognizable at mobile size. Allow restrained functional colors in the icons without making the screen oversaturated.

At the bottom, include a clear selected-talent detail area with reserved spaces for a thumbnail, name, description, state, coin cost and confirmation button. Keep the primary action visually clear without excessive glow.

Make the whole page feel like a finished interface from one coherent pixel-art action game. Leave all title, tab, node-name, price, description and button-text areas blank for later typesetting. No pseudo-text. No character portrait, battlefield backdrop or scenery outside the interface."""

# §4 中文正向提示词（原文）
PROMPT_ZH = """设计一张竖屏手机动作游戏的天赋页面整体参考图，三国战争题材的高清像素游戏美术。画面是一个可阅读、可操作的游戏 UI，而不是角色插画或宣传海报。

学习《上古卷轴：刀锋》技能界面给人的整洁感：明确的标题、分类、天赋树和详情层级，分支之间有留白，连线清楚，装饰服从信息。不要复制其节点形状、奇幻纹样或具体界面。

整体使用有层次的深色承托，界面边缘和节点采用精细的像素阶梯轮廓、经过设计的硬边色块和克制的受光面。材质应有游戏美术的完成度和适度分量，但不做照片写实、厚涂、满屏磨损或繁复装饰。不要画成单层纯色矢量框、通用软件面板或程序占位界面。

顶部预留标题和铜钱数量位置，下方有三个分类页签，第三个分类为选中状态。中部展示三选一技能解锁页：蓄力、位移、火系是不同分支；节点形状统一，锁定、可买和已解锁通过亮度、边缘层次和少量颜色变化区分。使用清楚的细连接线表达真实的分支关系，不画跨流派的虚假连线。

节点内部使用新设计的高清像素技能符号：聚力与防御、冲击、水浪、旋风和火焰等，每个图标只有一个明确主体，在手机尺寸下仍可辨识。图标可以有少量功能性的色彩，但不能让整页饱和杂乱。

底部是一块清楚的选中节点详情区，预留图标、名称、说明、状态、铜钱花费及确认按钮的位置。主要按钮突出但不刺眼。

保持整个画面像同一款完整游戏的正式界面：高清像素色块、清楚的形体、克制的细节和可读的信息层级。所有标题、页签、节点名称、价格、描述和按钮文字均留空，由后期添加；不要生成伪文字。不要出现人物立绘、战场背景或 UI 外的场景。"""

# §7 参数意图落到画面的一条技术附录：画布 2:3，设备 9:16，界面收在中央安全区
PROMPT_CANVAS = """Canvas: the image is 1024x1536 (2:3) while the target device screen is 9:16. Compose the whole interface inside the central 9:16 safe area (about 864x1536); the narrow left and right margins continue as plain dark background only. Keep every meaningful element clear of the left and right edges.

画布尺寸 1024x1536（2:3），目标设备是 9:16 竖屏：整个界面收在中央 9:16 安全区（约 864x1536）内，左右窄边只保留纯深色底延伸，任何重要元素都不要贴到左右边缘。"""

# §6 负面提示词（原文，接口没有 negative_prompt 字段，合并进正文）
PROMPT_NEG = """Avoid all of the following: placeholder UI, wireframe, flat vector dashboard, corporate icons, generic app interface, oversized geometric blocks, crude low-resolution pixel art, mosaic filter, random pixel noise, blurry pixel edges, photorealistic, painterly, soft glow, bloom, excessive bevels, ornate engraved metal, excessive weathering, parchment scroll, western fantasy ornament, character illustration, background scenery, text, letters, numbers, gibberish.

不要出现：占位线框 UI、扁平矢量仪表盘、通用软件图标、巨大几何色块、粗糙低清像素、马赛克滤镜、随机噪点、模糊像素边缘、照片写实、厚涂、柔光与辉光、过量倒角、繁复錾刻金属、过度做旧、羊皮卷、西方奇幻纹样、人物立绘、背景场景，以及任何文字、字母、数字、伪文字。"""

PROMPT = "\n\n".join([PROMPT_EN, PROMPT_ZH, PROMPT_CANVAS, PROMPT_NEG])


def png_info(path):
    """不依赖 PIL 读 PNG IHDR。"""
    with open(path, "rb") as fh:
        head = fh.read(33)
    if head[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError("not a PNG")
    if head[12:16] != b"IHDR":
        raise ValueError("no IHDR")
    width, height = struct.unpack(">II", head[16:24])
    depth, color_type = head[24], head[25]
    return {"width": width, "height": height, "bit_depth": depth, "color_type": color_type}


def save_payload(item, out_path):
    """先落盘图片，再清洗响应 JSON（避免自己把 base64 覆盖掉）。"""
    b64 = item.get("b64_json")
    url = item.get("url")
    if b64:
        blob = base64.b64decode(b64)
        with open(out_path, "wb") as fh:
            fh.write(blob)
        return {"download": "b64_json", "bytes": len(blob)}
    if url:
        with urllib.request.urlopen(url, timeout=300) as resp:
            blob = resp.read()
        with open(out_path, "wb") as fh:
            fh.write(blob)
        return {"download": "url", "url": url, "bytes": len(blob)}
    raise SystemExit("no image payload (neither b64_json nor url)")


def main():
    key = os.environ.get("MUSK_API_KEY")
    if not key:
        raise SystemExit("MUSK_API_KEY missing")
    os.makedirs(OUT_DIR, exist_ok=True)
    if os.path.exists(IMG_OUT):
        raise SystemExit(f"refusing to overwrite existing output: {IMG_OUT}")

    fields = {
        "model": MODEL,
        "prompt": PROMPT,
        "size": SIZE,
        "quality": QUALITY,
        "output_format": "png",
        "n": 1,
        "response_format": "b64_json",  # 图片 CDN 从本机不可达，改 base64 直回
    }
    body = json.dumps(fields, ensure_ascii=False).encode("utf-8")

    with open(PROMPT_TXT, "w", encoding="utf-8", newline="\n") as fh:
        fh.write(PROMPT)
    with open(REQ_JSON, "w", encoding="utf-8") as fh:
        json.dump({
            "version": "talent_page_ref_v6",
            "source": "plan/talent-page-reference-generation-handoff-20261102.md",
            "endpoint": ENDPOINT,
            "method": "POST",
            "content_type": "application/json",
            "fields": fields,
            "prompt_sha256": hashlib.sha256(PROMPT.encode("utf-8")).hexdigest(),
            "prompt_chars": len(PROMPT),
            "reference_images": [],
            "reference_images_note": "交接 §2.2/§7：不输入任何项目现有美术作参考；《刀锋》仅以文字传达信息组织原则",
            "automatic_retries": 0,
            "read_timeout_seconds": TIMEOUT,
            "submitted_at_local": time.strftime("%Y-%m-%d %H:%M:%S"),
        }, fh, indent=2, ensure_ascii=False)

    request = urllib.request.Request(
        ENDPOINT, data=body, method="POST",
        headers={"Authorization": f"Bearer {key}", "Content-Type": "application/json"})

    status = {"state": "submitted", "http_status": None, "seconds": None,
              "error": None, "output_image": IMG_OUT}
    t0 = time.time()
    try:
        with urllib.request.urlopen(request, timeout=TIMEOUT) as resp:
            raw = resp.read()
            status["http_status"] = resp.status
    except urllib.error.HTTPError as exc:
        status["state"] = "failed"
        status["error"] = f"HTTPError {exc.code}: {exc.read().decode('utf-8', errors='replace')[:800]}"
        status["seconds"] = round(time.time() - t0, 2)
        with open(STATUS_JSON, "w", encoding="utf-8") as fh:
            json.dump(status, fh, indent=2, ensure_ascii=False)
        raise SystemExit(status["error"])
    except Exception as exc:  # noqa: BLE001
        status["state"] = "failed"
        status["error"] = f"{type(exc).__name__}: {exc}"
        status["seconds"] = round(time.time() - t0, 2)
        with open(STATUS_JSON, "w", encoding="utf-8") as fh:
            json.dump(status, fh, indent=2, ensure_ascii=False)
        raise SystemExit(status["error"])

    status["seconds"] = round(time.time() - t0, 2)
    payload = json.loads(raw.decode("utf-8"))

    # 顺序：先存图，再清洗响应
    saved = save_payload(payload["data"][0], IMG_OUT)
    status.update(saved)

    cleaned = json.loads(raw.decode("utf-8"))
    for item in cleaned.get("data", []):
        if item.get("b64_json"):
            item["b64_json"] = "<base64 omitted; image saved to %s>" % IMG_OUT
    with open(RESP_JSON, "w", encoding="utf-8") as fh:
        json.dump(cleaned, fh, indent=2, ensure_ascii=False)

    status.update(png_info(IMG_OUT))
    status["output_bytes"] = os.path.getsize(IMG_OUT)
    status["output_sha256"] = hashlib.sha256(open(IMG_OUT, "rb").read()).hexdigest()
    status["png_signature"] = True
    status["usage"] = payload.get("usage")
    status["echoed_size"] = payload.get("size")
    status["echoed_quality"] = payload.get("quality")
    status["created"] = payload.get("created")
    status["state"] = "image_saved"
    with open(STATUS_JSON, "w", encoding="utf-8") as fh:
        json.dump(status, fh, indent=2, ensure_ascii=False)
    print(json.dumps(status, indent=2, ensure_ascii=False))


if __name__ == "__main__":
    main()
