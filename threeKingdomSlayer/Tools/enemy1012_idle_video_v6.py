# -*- coding: utf-8 -*-
"""1012 贼兵 idle 视频生成 · v6（以已验收的 v5 立绘为身份源）。

背景
    现有 `Enemy_1012_idle1..6.png` 来自旧身份图（stageA5b）生成的
    `enemy1012_bandit_idle_v2.mp4`。v5 立绘验收后，身份图与比例都变了，
    必须重做视频 → 抠图 → 选帧 → 落位 → 接入。

输入
    身份图 `C:/Users/steam/Pictures/gptGen/enemy1012_idle_v6/input_identity_v12_bandit_idle_v6_1024.png`
      （由 v5 经 SVL 抠图后按管线规格重排：1024² / 绿 (0,159,62) / 主体高 696 / 脚底 895 / cx 474.5）

接口（沿用项目既有链路，见 Library/Locus/tmp/create_enemy1021_idle_video_v1.py）
    上传   POST https://file.reachapi.ai/file/uploads        → data.url
    建任务 POST https://direct.reachapi.ai/v1/vids/create      → task_id
    轮询   GET  https://direct.reachapi.ai/v1/tasks/<task_id>
    下载   任务响应 data[0].url → C:/Users/steam/Videos/doubaoVideo/
    鉴权   Bearer $REACH_API_KEY

参数（不可改：叠轮会让边缘锐度逐轮下降，一个动作只允许生成一次）
    seedance-2-fast / 4 s / 720p / 1:1 / 无音频 / 无水印 / seed −1 / 不传 background
    first_frame 与 last_frame 用同一张身份图，保证循环无缝

用法
    python Tools/enemy1012_idle_video_v6.py --dry-run    # 只打印请求，不提交
    python Tools/enemy1012_idle_video_v6.py --approve    # 实际提交（付费，一次机会）
"""
from __future__ import annotations

import datetime as dt
import hashlib
import http.client
import json
import os
import pathlib
import re
import subprocess
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid

ROOT = pathlib.Path(r"C:/threeKingdomSlayer/threeKingdomSlayer")
REFERENCE = pathlib.Path(r"C:/Users/steam/Pictures/gptGen/enemy1012_idle_v6/input_identity_v12_bandit_idle_v6_1024.png")
OUTPUT = pathlib.Path(r"C:/Users/steam/Videos/doubaoVideo/enemy1012_bandit_idle_v6.mp4")
WORK = ROOT / "Library/Locus/tmp/enemy1012_idle_v6"
RECORD = WORK / "runtime.json"

# 已验收的 v2 视频词（用户验收：v4 表情合格、6 帧、1.8s 周期）。
# v5 保留了全部身份特征（半球顶兜鍪+短红缨、络腮胡、麻绳腰带、红布条、护腕、环首刀、垂持），
# 表情沿用 option C（冷而算计），因此词可以原样复用，只换参考图。
PROMPT = r"""Game sprite idle loop animation of the single armored bandit swordsman shown in image 1, rendered in the same high-definition pixel-art style with the same colors. Locked camera, fixed framing, fixed position and fixed scale: the character stays in exactly the same spot in the frame, at exactly the same size, in exactly the same pose, with both feet planted on the same ground line and his entire body fully visible at all times. The motion is a restrained breathing idle cycle: his chest rises and falls slightly, his weight shifts by a hair between his front foot and his back foot, the arm holding the dao trembles a little without lifting the blade, the short red horsehair plume on his helmet and the red cloth at his belt sway softly, and his head bobs by a fraction. He never steps, never turns, never raises the blade and never attacks.

Keep his expression exactly as in image 1 and keep it cold and calculating for the whole loop: the face itself stays quiet and controlled, the eyes narrow and bright, angled slightly to one side as if he is studying the viewer, one brow a little higher than the other, the jaw set and the cheekbones sharp, the lips closed with one corner lifted just a little in a cold half-smirk that never reaches his eyes. He must never look tired, sleepy, drowsy, weary, exhausted, bored, sad or blank, and he must never droop his eyelids or turn his mouth corners downward.

Keep his iron helmet with the short red plume, the forehead band, the neck guard, the leather chin strap, his face, the hemp tunic, the chest plate, the rope belt, the bracers, the trousers, the leg wraps, the boots and the single ring-pommel dao exactly as they are in image 1. Keep the whole motion subtle and make the last frame return to exactly the same pose as the first frame so the loop is seamless. Uniform green screen background, no scenery, no floor, no shadow, no text, no other characters, no camera movement, no zoom, no cut, no dust, no new equipment and no change to his face or helmet."""

SPEC = {
    "model": "seedance-2-fast",
    "duration_seconds": 4,
    "resolution": "720p",
    "aspect_ratio": "1:1",
    "generate_audio": False,
    "seed": -1,
    "watermark": False,
}


def now() -> str:
    return dt.datetime.now(dt.timezone.utc).isoformat()


def safe_text(value: str) -> str:
    value = re.sub(r"https?://[^\s\"']+", "<redacted-url>", value)
    value = re.sub(r"Bearer\s+\S+", "Bearer <redacted>", value, flags=re.I)
    return value[:1000]


def redact(value):
    if isinstance(value, dict):
        result = {}
        for key, item in value.items():
            if key.lower() in {"url", "image_url", "video_url", "audio_url"}:
                result[key] = "<redacted-url>"
            else:
                result[key] = redact(item)
        return result
    if isinstance(value, list):
        return [redact(item) for item in value]
    return value


def save(record: dict) -> None:
    WORK.mkdir(parents=True, exist_ok=True)
    RECORD.write_text(json.dumps(record, ensure_ascii=False, indent=2), encoding="utf-8")


def request_json(url: str, method: str, body: bytes | None, content_type: str | None, timeout: float):
    api_key = os.environ["REACH_API_KEY"]
    headers = {"Authorization": f"Bearer {api_key}", "User-Agent": "curl/8.0"}
    if content_type:
        headers["Content-Type"] = content_type
    request = urllib.request.Request(url, data=body, headers=headers, method=method)
    last = None
    for attempt in range(4):                       # 连接被断是常态，重试即可
        try:
            with urllib.request.urlopen(request, timeout=timeout) as response:
                return response.status, json.loads(response.read().decode("utf-8"))
        except urllib.error.HTTPError as exc:
            raw = exc.read().decode("utf-8", errors="replace")
            raise RuntimeError(f"HTTP {exc.code} at {url}: {safe_text(raw)}") from exc
        except (urllib.error.URLError, http.client.RemoteDisconnected, ConnectionError, TimeoutError) as exc:
            last = exc
            time.sleep(2 + attempt * 3)
    raise RuntimeError(f"network failed after retries at {url}: {type(last).__name__}: {last}")


def upload_reference(record: dict) -> str:
    boundary = "Locus" + uuid.uuid4().hex
    body = (
        f"--{boundary}\r\n"
        f"Content-Disposition: form-data; name=\"file\"; filename=\"{REFERENCE.name}\"\r\n"
        "Content-Type: image/png\r\n\r\n"
    ).encode("utf-8") + REFERENCE.read_bytes() + f"\r\n--{boundary}--\r\n".encode("utf-8")
    started = time.monotonic()
    status, response = request_json(
        "https://file.reachapi.ai/file/uploads", "POST", body,
        f"multipart/form-data; boundary={boundary}", 60)
    data = response.get("data") if isinstance(response, dict) else None
    url = data.get("url") if isinstance(data, dict) else None
    if not url:
        raise RuntimeError("Upload returned no data.url")
    record["upload"] = {"http_status": status, "seconds": round(time.monotonic() - started, 3),
                        "file": REFERENCE.as_posix(), "host": urllib.parse.urlsplit(url).netloc}
    record["stage"] = "creating"
    save(record)
    return url


def create_task(record: dict, reference_url: str) -> str:
    content = [
        {"type": "text", "text": PROMPT},
        {"type": "image_url", "role": "first_frame", "image_url": {"url": reference_url}},
        {"type": "image_url", "role": "last_frame", "image_url": {"url": reference_url}},
    ]
    payload = {"model": SPEC["model"],
               "input": {**{k: v for k, v in SPEC.items() if k != "model"}, "content": content}}
    started = time.monotonic()
    status, response = request_json(
        "https://direct.reachapi.ai/v1/vids/create", "POST",
        json.dumps(payload, ensure_ascii=False).encode("utf-8"), "application/json", 60)
    data = response.get("data") if isinstance(response, dict) else None
    task_id = response.get("task_id") if isinstance(response, dict) else None
    if not task_id and isinstance(data, dict):
        task_id = data.get("task_id")
    if not task_id:
        raise RuntimeError("Create returned no task_id")
    record.update({"create_http_status": status, "create_seconds": round(time.monotonic() - started, 3),
                   "task_id": task_id, "accepted_utc": now(), "stage": "created",
                   "create_response_sanitized": redact(response)})
    save(record)
    return task_id


def poll_task(record: dict, task_id: str) -> bool:
    deadline = time.monotonic() + 300.0
    terminal = {"success", "failed", "error", "cancelled", "canceled"}
    for attempt in range(40):
        remaining = deadline - time.monotonic()
        if remaining <= 0:
            break
        status, response = request_json(
            "https://direct.reachapi.ai/v1/tasks/" + urllib.parse.quote(task_id, safe=""),
            "GET", None, None, min(20.0, remaining))
        state = response.get("status") if isinstance(response, dict) else None
        item = {"attempt": attempt + 1, "utc": now(), "http_status": status, "status": state}
        if state in terminal:
            item["usage"] = response.get("usage")
            item["cost"] = response.get("cost")
        if state in {"failed", "error", "cancelled", "canceled"}:
            item["error"] = redact(response.get("error") or response.get("message"))
        history = record.get("poll_history", [])
        history.append(item)
        record["poll_history"] = history
        record["last_status"] = state
        if state == "success":
            record["success_observed_utc"] = now()
            record["usage"] = response.get("usage")
            record["cost"] = response.get("cost")
            record["task_response_sanitized"] = redact(response)
        elif state in terminal:
            record["task_response_sanitized"] = redact(response)
        save(record)
        print(json.dumps({"attempt": attempt + 1, "status": state}, ensure_ascii=False), flush=True)
        if state in terminal:
            return state == "success"
        time.sleep(min(8.0, max(0.0, deadline - time.monotonic())))
    record["stage"] = "poll_timeout"
    save(record)
    return False


def download(record: dict) -> str:
    """从任务接口重新取一次真实 URL（记录里的 url 是脱敏过的，不能直接用）。"""
    task_id = record.get("task_id")
    if not task_id:
        raise RuntimeError("record has no task_id")
    _status, response = request_json(
        "https://direct.reachapi.ai/v1/tasks/" + urllib.parse.quote(task_id, safe=""),
        "GET", None, None, 30)
    data = response.get("data")
    url = None
    if isinstance(data, list) and data and isinstance(data[0], dict):
        url = data[0].get("url")
    if not url:
        raise RuntimeError("No video url in task response: %s" % safe_text(json.dumps(response)[:400]))
    part = pathlib.Path(str(OUTPUT) + ".part")
    part.parent.mkdir(parents=True, exist_ok=True)
    cfg = 'url = "' + url.replace('"', '\\"') + '"\n'
    args = ["curl", "--config", "-", "--silent", "--show-error", "--fail", "--location",
            "--connect-timeout", "10", "--max-time", "600", "--output", str(part)]
    proc = subprocess.run(args, input=cfg, text=True, capture_output=True)
    if proc.returncode != 0:
        raise RuntimeError(f"curl failed: {safe_text(proc.stderr)}")
    part.replace(OUTPUT)
    blob = OUTPUT.read_bytes()
    record["download"] = {"output": OUTPUT.as_posix(), "bytes": len(blob),
                          "sha256": hashlib.sha256(blob).hexdigest(),
                          "downloaded_at_utc": now()}
    record["stage"] = "video_saved"
    save(record)
    return record["download"]["sha256"]


def main() -> int:
    dry = "--dry-run" in sys.argv
    approve = "--approve" in sys.argv
    resume = "--resume" in sys.argv

    if not os.environ.get("REACH_API_KEY"):
        raise SystemExit("REACH_API_KEY is not configured")
    if not REFERENCE.is_file():
        raise SystemExit(f"Reference does not exist: {REFERENCE}")

    # 续等：任务已创建，只继续轮询 + 下载，绝不重新提交
    if resume:
        if not RECORD.exists():
            raise SystemExit(f"No record to resume: {RECORD}")
        record = json.loads(RECORD.read_text(encoding="utf-8"))
        task_id = record.get("task_id")
        if not task_id:
            raise SystemExit("Record has no task_id")
        print(f"续等任务 {task_id}（不重新提交）")
        ok = poll_task(record, task_id)
        if not ok:
            print(json.dumps({"task_id": task_id, "stage": "failed_or_timeout"}, ensure_ascii=False))
            return 1
        sha = download(record)
        print(json.dumps({"task_id": task_id, "output": OUTPUT.as_posix(), "sha256": sha,
                          "record": RECORD.as_posix()}, ensure_ascii=False, indent=2))
        return 0

    ref_sha = hashlib.sha256(REFERENCE.read_bytes()).hexdigest()
    payload_preview = {
        "model": SPEC["model"],
        "input": {**{k: v for k, v in SPEC.items() if k != "model"},
                  "content": [{"type": "text", "text": PROMPT},
                              {"type": "image_url", "role": "first_frame", "image_url": {"url": "<uploaded>"}},
                              {"type": "image_url", "role": "last_frame", "image_url": {"url": "<uploaded>"}}]},
    }
    if dry:
        print("=== 参考图 ===")
        print(REFERENCE, ref_sha)
        print("\n=== 参数 ===")
        print(json.dumps(SPEC, ensure_ascii=False, indent=2))
        print("\n=== 提示词 (%d 字符, sha256 %s) ===" % (
            len(PROMPT), hashlib.sha256(PROMPT.encode("utf-8")).hexdigest()))
        print(PROMPT)
        print("\n=== 请求体 ===")
        print(json.dumps(payload_preview, ensure_ascii=False, indent=2)[:1200])
        print("\n=== 输出 ===", OUTPUT)
        return 0

    if not approve:
        raise SystemExit("未确认批准：加 --approve 才会真的提交（付费生成，一次机会）")
    if OUTPUT.exists() or pathlib.Path(str(OUTPUT) + ".part").exists():
        raise SystemExit(f"Output or .part already exists: {OUTPUT}")
    if RECORD.exists():
        raise SystemExit(f"Record already exists: {RECORD}")

    record = {"started_utc": now(), "stage": "uploading", "reference": REFERENCE.as_posix(),
              "reference_sha256": ref_sha,
              "reference_role": "v6 identity image (from approved v5 art), used once as first_frame and once as last_frame",
              "output": OUTPUT.as_posix(), "spec": SPEC, "prompt": PROMPT,
              "prompt_sha256": hashlib.sha256(PROMPT.encode("utf-8")).hexdigest(),
              "authorization": "user approved generation in conversation"}
    save(record)
    reference_url = upload_reference(record)
    task_id = create_task(record, reference_url)
    ok = poll_task(record, task_id)
    if not ok:
        print(json.dumps({"task_id": task_id, "stage": "failed_or_timeout"}, ensure_ascii=False))
        return 1
    sha = download(record)
    print(json.dumps({"task_id": task_id, "output": OUTPUT.as_posix(), "sha256": sha,
                      "record": RECORD.as_posix()}, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    sys.exit(main())
