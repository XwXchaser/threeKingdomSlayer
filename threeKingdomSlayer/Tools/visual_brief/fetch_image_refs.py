#!/usr/bin/env python
"""抓取「游戏实机截图 / 立绘」类动作参考图。

用途：为 1013 旗手的 Walk / 受击 / Dead / 击飞链找参考（用户 2026-11-09 口径：
参考来源走游戏实机截图或立绘，不用实拍照片库）。

做法：Bing 图片异步接口 -> 解析每条结果的 murl(原图) / turl(缩略图) / purl(来源页)
-> 下载缩略图到 00_Reference/<slug>/ -> 写 references.csv 台账 -> 拼一张带编号的
contact sheet 供用户挑选。

用法：
    python Tools/visual_brief/fetch_image_refs.py --query "三国 旗手 立绘" --count 24
    python Tools/visual_brief/fetch_image_refs.py --query "..." --out-dir Enemy1013ArtSource/00_Reference/walk
"""

import argparse
import csv
import html
import http.cookiejar
import json
import os
import re
import sys
import time
import urllib.parse
import urllib.request
from datetime import datetime, timezone

UA = ("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
      "(KHTML, like Gecko) Chrome/122.0 Safari/537.36")
ENDPOINT = ("https://www.bing.com/images/async?q={q}&first=1&count={count}"
            "&relp=35&scenario=ImageBasicHover&datsrc=N_I&layout=RowBased_Landscape")


def fetch_results(query, count, attempts=3):
    q = urllib.parse.quote(query)
    url = ENDPOINT.format(q=q, count=count)
    opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
    last = None
    for attempt in range(attempts):
        req = urllib.request.Request(url, headers={"User-Agent": UA, "Accept": "text/html"})
        try:
            raw = opener.open(req, timeout=40).read().decode("utf-8", "ignore")
        except Exception as exc:  # noqa: BLE001
            last = exc
            time.sleep(2.0)
            continue
        out = []
        for blob in re.findall(r'\sm="(\{&quot;.*?\})"', raw):
            try:
                data = json.loads(html.unescape(blob))
            except Exception:
                continue
            if not data.get("murl"):
                continue
            out.append(data)
        if out:
            return out
        last = RuntimeError("no results parsed")
        time.sleep(2.5)
    print(f"[WARN] fetch gave 0 hits for '{query}': {last}", file=sys.stderr)
    return []


def download(url, dest):
    req = urllib.request.Request(url, headers={"User-Agent": UA, "Referer": "https://www.bing.com/"})
    with urllib.request.urlopen(req, timeout=45) as resp:
        data = resp.read()
    if len(data) < 4096:
        raise ValueError("too small")
    with open(dest, "wb") as fh:
        fh.write(data)
    return len(data)


def ext_of(url):
    path = urllib.parse.urlparse(url).path
    ext = os.path.splitext(path)[1].lower()
    if ext in (".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp"):
        return ".jpg" if ext == ".jpeg" else ext
    return ".jpg"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--query", action="append", required=True, help="可重复")
    ap.add_argument("--count", type=int, default=20)
    ap.add_argument("--out-dir", default="Enemy1013ArtSource/00_Reference")
    ap.add_argument("--ledger", default="")
    args = ap.parse_args()

    root = os.path.abspath(args.out_dir)
    os.makedirs(root, exist_ok=True)
    ledger_path = args.ledger or os.path.join(root, "references.csv")
    now = datetime.now(timezone.utc).isoformat(timespec="seconds")

    rows = []
    idx = 0
    for query in args.query:
        slug = re.sub(r"[^0-9A-Za-z一-鿿]+", "_", query).strip("_")[:48]
        qdir = os.path.join(root, slug)
        os.makedirs(qdir, exist_ok=True)
        try:
            results = fetch_results(query, args.count)
        except Exception as exc:  # noqa: BLE001
            print(f"[WARN] query failed: {query}: {exc}", file=sys.stderr)
            continue
        print(f"[INFO] query '{query}' -> {len(results)} hits")
        for data in results:
            idx += 1
            name = f"{idx:03d}{ext_of(data.get('murl', ''))}"
            dest = os.path.join(qdir, name)
            try:
                size = download(data.get("turl") or data["murl"], dest)
            except Exception:
                try:
                    size = download(data["murl"], dest)
                except Exception as exc:  # noqa: BLE001
                    print(f"[WARN] download failed {data['murl']}: {exc}", file=sys.stderr)
                    continue
            rows.append({
                "index": idx,
                "query": query,
                "local_file": os.path.relpath(dest, os.getcwd()).replace("\\", "/"),
                "bytes": size,
                "source_page": data.get("purl", ""),
                "image_url": data.get("murl", ""),
                "thumb_url": data.get("turl", ""),
                "fetched_utc": now,
            })
            print(f"  [{idx:03d}] {size:>8}B {data.get('purl', '')[:90]}")

    if not rows:
        print("[ERROR] nothing downloaded")
        return 1

    write_header = not os.path.exists(ledger_path)
    with open(ledger_path, "a", newline="", encoding="utf-8-sig") as fh:
        writer = csv.DictWriter(fh, fieldnames=list(rows[0].keys()))
        if write_header:
            writer.writeheader()
        writer.writerows(rows)
    print(f"[OK] {len(rows)} refs -> {root}")
    print(f"[OK] ledger -> {ledger_path}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
