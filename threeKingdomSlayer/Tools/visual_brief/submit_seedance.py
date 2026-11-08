"""通用 Seedance 图生视频提交器（接口沿用项目既有链路）。

用法 A：首尾帧工作流（默认）
    python Tools/visual_brief/submit_seedance.py \
        --prompt-file Tools/visual_brief/prompts/1012_attack_arc_E2_full.txt \
        --reference  <参考图.png> \
        --output     C:/Users/steam/Videos/doubaoVideo/<成片名>.mp4 \
        --dry-run
    实际提交加 --approve；任务已建但中断用 --resume（**绝不重新提交**）。

用法 B：**替换视频工作流**（保留旧动作源片的动作、只换形象）
    python Tools/visual_brief/submit_seedance.py \
        --workflow replace \
        --prompt-file <提示词.txt> \
        --reference-image <新身份图.png> \
        --reference-video <旧动作源片.mp4> \
        [--reference-video-extra <表情研究片.mp4>] \
        --output <成片名>.mp4 --approve
    规则见 skill 文档 `Locus/knowledge/skill/reachapi-seedance-video-generation-v2.md` →
    「## 替换视频工作流」：**动作从 reference_video 继承，提示词只写形象/表情层，不得描述动作幅度**。
    `first_frame/last_frame` 不能与 `reference_video` 混用，故本模式身份图挂 `reference_image`。

接口
    POST https://direct.reachapi.ai/v1/vids/create   multipart/form-data
    model=seedance-2-fast / duration_seconds=4 / resolution=720p / aspect_ratio=1:1
    generate_audio=false / watermark=false / seed=-1
    input.content: [ {type:text,text:...}, {type:image_url,role:first_frame,...},
                     {type:image_url,role:last_frame,...} ]
    替换视频工作流时改为：
                     {type:image_url,role:reference_image,...},
                     {type:video_url,role:reference_video,...}
    上传参考素材：POST https://file.reachapi.ai/file/uploads  → data.url（图片与视频均可，单文件 ≤50MiB）
    轮询：GET https://direct.reachapi.ai/v1/tasks/<task_id>
    鉴权：Bearer $REACH_API_KEY

纪律：一个动作只生成一次（叠轮会让边缘锐度逐轮下降）；失败不自动重发（create 响应丢失时任务可能已建）。
"""
from __future__ import annotations

import argparse
import base64
import datetime as dt
import hashlib
import http.client
import json
import mimetypes
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

ENDPOINT = "https://direct.reachapi.ai/v1/vids/create"
UPLOAD = "https://file.reachapi.ai/file/uploads"
MODEL = "seedance-2-fast"
TIMEOUT = 900


def now():
    return dt.datetime.now(dt.timezone.utc).isoformat()


def safe_text(v):
    v = re.sub(r"https?://[^\s\"']+", "<redacted-url>", v)
    v = re.sub(r"Bearer\s+\S+", "Bearer <redacted>", v, flags=re.I)
    return v[:1000]


def redact(v):
    if isinstance(v, dict):
        return {k: ("<redacted-url>" if k.lower() in {"url", "image_url", "video_url"} else redact(i))
                for k, i in v.items()}
    if isinstance(v, list):
        return [redact(i) for i in v]
    return v


def request_json(url, method, body, content_type, timeout, retries=4):
    """retries=0 时**不重试**——用于 create 这种「请求可能已到达服务端」的调用：
    响应丢失时重发会多建任务（重复计费），宁可报错让人工判断。"""
    key = os.environ["REACH_API_KEY"]
    headers = {"Authorization": "Bearer %s" % key, "User-Agent": "curl/8.0"}
    if content_type:
        headers["Content-Type"] = content_type
    req = urllib.request.Request(url, data=body, headers=headers, method=method)
    last = None
    for attempt in range(max(1, retries)):
        try:
            with urllib.request.urlopen(req, timeout=timeout) as r:
                return r.status, json.loads(r.read().decode("utf-8"))
        except urllib.error.HTTPError as e:
            raise RuntimeError("HTTP %s at %s: %s" % (e.code, url, safe_text(e.read().decode("utf-8", "replace"))))
        except (urllib.error.URLError, http.client.RemoteDisconnected, ConnectionError, TimeoutError) as e:
            last = e
            if attempt + 1 < max(1, retries):
                time.sleep(2 + attempt * 3)
    raise RuntimeError("network failed: %s: %s" % (type(last).__name__, last))


def upload(path: pathlib.Path, mime: str = "image/png"):
    boundary = "Locus" + uuid.uuid4().hex
    body = (("--%s\r\n" % boundary).encode()
            + ('Content-Disposition: form-data; name="file"; filename="%s"\r\n' % path.name).encode()
            + ("Content-Type: %s\r\n\r\n" % mime).encode()
            + path.read_bytes()
            + ("\r\n--%s--\r\n" % boundary).encode())
    _s, resp = request_json(UPLOAD, "POST", body, "multipart/form-data; boundary=%s" % boundary, 120)
    url = ((resp.get("data") or {}).get("url")) if isinstance(resp, dict) else None
    if not url:
        raise RuntimeError("upload returned no data.url")
    return url


MIME_BY_EXT = {".png": "image/png", ".jpg": "image/jpeg", ".jpeg": "image/jpeg",
               ".webp": "image/webp", ".gif": "image/gif",
               ".mp4": "video/mp4", ".webm": "video/webm", ".mov": "video/quicktime",
               ".mkv": "video/x-matroska"}


def mime_of(path: pathlib.Path) -> str:
    return MIME_BY_EXT.get(path.suffix.lower(), "application/octet-stream")


def create(prompt, content, spec):
    # model 必须在 payload 顶层，不能放进 input（API 会报 RJ_BAD_REQUEST: Unsupported input fields: model）
    spec_in = {k: v for k, v in spec.items() if k != "model"}
    payload = {"model": spec.get("model", MODEL), "input": {**spec_in, "content": content}}
    # retries=0：请求发出后若响应丢失，服务端可能已建任务，重发会造成重复任务
    _s, resp = request_json(ENDPOINT, "POST", json.dumps(payload, ensure_ascii=False).encode("utf-8"),
                             "application/json", 60, retries=0)
    tid = resp.get("task_id") if isinstance(resp, dict) else None
    if not tid and isinstance(resp, dict) and isinstance(resp.get("data"), dict):
        tid = resp["data"].get("task_id")
    if not tid:
        raise RuntimeError("create returned no task_id")
    return tid, redact(resp)


def poll(task_id, deadline_s=300):
    terminal = {"success", "failed", "error", "cancelled", "canceled"}
    deadline = time.monotonic() + deadline_s
    for attempt in range(40):
        if deadline - time.monotonic() <= 0:
            return None
        _s, resp = request_json("https://direct.reachapi.ai/v1/tasks/" + urllib.parse.quote(task_id, safe=""),
                                "GET", None, None, min(20.0, deadline - time.monotonic()))
        st = resp.get("status") if isinstance(resp, dict) else None
        print(json.dumps({"attempt": attempt + 1, "status": st}, ensure_ascii=False), flush=True)
        if st in terminal:
            return resp
        time.sleep(min(8.0, max(0.0, deadline - time.monotonic())))
    return None


def download(task_id, out: pathlib.Path):
    _s, resp = request_json("https://direct.reachapi.ai/v1/tasks/" + urllib.parse.quote(task_id, safe=""),
                            "GET", None, None, 30)
    data = resp.get("data") if isinstance(resp, dict) else None
    url = None
    if isinstance(data, list) and data and isinstance(data[0], dict):
        url = data[0].get("url")
    if not url:
        raise RuntimeError("no video url in task response")
    out.parent.mkdir(parents=True, exist_ok=True)
    part = pathlib.Path(str(out) + ".part")
    cfg = 'url = "' + url.replace('"', '\\"') + '"\n'
    p = subprocess.run(["curl", "--config", "-", "--silent", "--show-error", "--fail", "--location",
                        "--connect-timeout", "10", "--max-time", "600", "--output", str(part)],
                       input=cfg, text=True, capture_output=True)
    if p.returncode != 0:
        raise RuntimeError("curl failed: %s" % safe_text(p.stderr))
    part.replace(out)
    return out


def main():
    ap = argparse.ArgumentParser(description="通用 Seedance 图生视频提交器")
    ap.add_argument("--prompt-file", required=True)
    ap.add_argument("--reference", default="", help="首尾帧工作流：身份图（挂 first_frame/last_frame）")
    ap.add_argument("--last-frame", default="",
                    help="首尾帧工作流：末帧图（与首帧不同图时用；不传则 first=last=--reference）")
    ap.add_argument("--extra-reference", action="append", default=[],
                    help="首尾帧工作流：额外的参考图（挂 reference_image），可重复；"
                         "用来把「中间关键帧」交给模型，例如射箭的搭箭帧与满弓帧")
    ap.add_argument("--workflow", choices=("frames", "replace"), default="frames",
                    help="frames=首尾帧工作流；replace=替换视频工作流（动作从 reference_video 继承）")
    ap.add_argument("--reference-image", default="", help="替换视频工作流：身份图（挂 reference_image）")
    ap.add_argument("--reference-video", default="", help="替换视频工作流：旧动作源片（挂 reference_video）")
    ap.add_argument("--reference-video-extra", action="append", default=[],
                    help="替换视频工作流：额外的 reference_video（如表情研究片），可重复")
    ap.add_argument("--output", required=True)
    ap.add_argument("--duration", type=int, default=4,
                    help="成片时长秒（替换视频工作流应与 reference_video 的时长一致，否则动作会被压缩）")
    ap.add_argument("--model", default=MODEL,
                    help="视频模型；默认 %s 不支持 1080p，需要更高分辨率时换 seedance-2 / seedance-2-5" % MODEL)
    ap.add_argument("--resolution", default="720p", choices=("480p", "720p", "1080p"),
                    help="成片分辨率档（seedance-2-fast 不支持 1080p）")
    ap.add_argument("--aspect", default="1:1",
                    choices=("21:9", "16:9", "4:3", "1:1", "3:4", "9:16", "adaptive"),
                    help="成片画幅比例；高杆/高道具角色用 3:4 或 9:16 可保住人物像素高度")
    ap.add_argument("--work", default="")
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--approve", action="store_true")
    ap.add_argument("--resume", action="store_true")
    args = ap.parse_args()

    if not os.environ.get("REACH_API_KEY"):
        raise SystemExit("REACH_API_KEY is not configured")
    prompt = pathlib.Path(args.prompt_file).read_text(encoding="utf-8").strip()
    out = pathlib.Path(args.output)

    if args.workflow == "replace":
        if not args.reference_video:
            raise SystemExit("replace 工作流需要 --reference-video")
        vid = pathlib.Path(args.reference_video)
        extra = [pathlib.Path(v) for v in args.reference_video_extra]
        ref = pathlib.Path(args.reference_image) if args.reference_image else None
        for p in ([ref] if ref else []) + [vid] + extra:
            if not p.is_file():
                raise SystemExit("reference missing: %s" % p)
        if len(extra) + 1 > 3:
            raise SystemExit("视频参考上限 3 个")
        refs_desc = ([("image", ref)] if ref else []) + [("video", vid)] + [("video", v) for v in extra]
    else:
        if not args.reference:
            raise SystemExit("frames 工作流需要 --reference")
        ref = pathlib.Path(args.reference)
        vid = None
        extra = []
        if not ref.is_file():
            raise SystemExit("reference missing: %s" % ref)
        refs_desc = [("image", ref)]
        extra_imgs = []
        for e in (args.extra_reference or []):
            p = pathlib.Path(e)
            if not p.is_file():
                raise SystemExit("extra reference missing: %s" % p)
            extra_imgs.append(p)
            refs_desc.append(("image", p))
        if args.last_frame:
            last = pathlib.Path(args.last_frame)
            if not last.is_file():
                raise SystemExit("last frame missing: %s" % last)
            refs_desc.append(("image", last))

    spec = {"model": args.model, "duration_seconds": args.duration,
            "resolution": args.resolution, "aspect_ratio": args.aspect,
            "generate_audio": False, "watermark": False, "seed": -1}
    work = pathlib.Path(args.work) if args.work else pathlib.Path(
        r"C:/Users/steam/Videos/doubaoVideo") / (out.stem + ".work")
    record = work / "runtime.json"

    if args.dry_run:
        print("=== 工作流 ===\n%s" % args.workflow)
        for kind, p in refs_desc:
            print("=== 参考%s ===\n%s  sha256=%s"
                  % ("图" if kind == "image" else "视频", p,
                     hashlib.sha256(p.read_bytes()).hexdigest()))
        print("=== 参数 ===\n%s" % json.dumps(spec, ensure_ascii=False))
        print("=== 提示词 (%d 字符) ===\n%s" % (len(prompt), prompt))
        print("=== 输出 ===\n%s" % out)
        return 0

    if args.resume:
        rec = json.loads(record.read_text(encoding="utf-8"))
        tid = rec["task_id"]
        print("续等任务 %s（不重新提交）" % tid)
        resp = poll(tid)
        if not resp or resp.get("status") != "success":
            print(json.dumps({"task_id": tid, "stage": "failed_or_timeout"}, ensure_ascii=False))
            return 1
        p = download(tid, out)
        print(json.dumps({"task_id": tid, "output": str(p),
                          "sha256": hashlib.sha256(p.read_bytes()).hexdigest()}, ensure_ascii=False, indent=2))
        return 0

    if not args.approve:
        raise SystemExit("未确认批准：加 --approve 才会真的提交（付费，一次机会）")
    if out.exists() or pathlib.Path(str(out) + ".part").exists():
        raise SystemExit("output already exists: %s" % out)
    if record.exists():
        prior = json.loads(record.read_text(encoding="utf-8"))
        if prior.get("stage") == "create_unknown":
            print("注意：上次提交记录停在 create_unknown（请求可能已建任务）。"
                  "确认服务端没有在跑这个任务再重发，否则会重复计费。", file=sys.stderr)
        raise SystemExit("record exists: %s" % record)

    work.mkdir(parents=True, exist_ok=True)
    rec = {"started_utc": now(), "workflow": args.workflow,
           "prompt_file": str(args.prompt_file), "output": str(out),
           "references": [{"kind": k, "path": str(p),
                            "sha256": hashlib.sha256(p.read_bytes()).hexdigest()}
                           for k, p in refs_desc],
           "prompt_sha256": hashlib.sha256(prompt.encode("utf-8")).hexdigest(),
           "prompt_chars": len(prompt), "spec": spec, "stage": "uploading"}
    record.write_text(json.dumps(rec, ensure_ascii=False, indent=2), encoding="utf-8")

    urls = []
    for kind, p in refs_desc:
        urls.append(upload(p, mime_of(p)))
    content = [{"type": "text", "text": prompt}]
    if args.workflow == "replace":
        vid_idx = 0
        if args.reference_image:
            content.append({"type": "image_url", "role": "reference_image", "image_url": {"url": urls[0]}})
            vid_idx = 1
        for u in urls[vid_idx:]:
            content.append({"type": "video_url", "role": "reference_video", "video_url": {"url": u}})
    else:
        # urls 顺序：[首帧, 额外参考图..., 末帧?]；没有末帧时末帧=首帧
        first_url = urls[0]
        mid_urls = urls[1:1 + len(extra_imgs)]
        last_url = urls[1 + len(extra_imgs)] if args.last_frame else first_url
        content.append({"type": "image_url", "role": "first_frame", "image_url": {"url": first_url}})
        for u in mid_urls:
            content.append({"type": "image_url", "role": "reference_image", "image_url": {"url": u}})
        content.append({"type": "image_url", "role": "last_frame", "image_url": {"url": last_url}})
    try:
        tid, resp = create(prompt, content, spec)
    except RuntimeError as e:
        rec.update({"stage": "create_unknown", "error": str(e)})
        record.write_text(json.dumps(rec, ensure_ascii=False, indent=2), encoding="utf-8")
        print(json.dumps({"stage": "create_unknown",
                          "note": "请求可能已到达服务端并建了任务，但响应丢失；task_id 未知。"
                                  "重发会产生重复任务，务必人工确认后再决定。",
                          "error": str(e)}, ensure_ascii=False, indent=2))
        return 1
    rec.update({"task_id": tid, "stage": "created", "create_response": resp})
    record.write_text(json.dumps(rec, ensure_ascii=False, indent=2), encoding="utf-8")

    final = poll(tid)
    if not final or final.get("status") != "success":
        rec["stage"] = "failed_or_timeout"
        record.write_text(json.dumps(rec, ensure_ascii=False, indent=2), encoding="utf-8")
        print(json.dumps({"task_id": tid, "stage": rec["stage"]}, ensure_ascii=False))
        return 1
    p = download(tid, out)
    rec.update({"stage": "video_saved", "usage": final.get("usage"),
                "sha256": hashlib.sha256(p.read_bytes()).hexdigest(),
                "bytes": p.stat().st_size, "finished_utc": now()})
    record.write_text(json.dumps(rec, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"task_id": tid, "output": str(p), "sha256": rec["sha256"],
                      "bytes": rec["bytes"], "usage": rec.get("usage")}, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    sys.exit(main())
