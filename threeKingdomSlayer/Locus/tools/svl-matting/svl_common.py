"""Shared helpers for the SVL matting scripts (self-contained: no project-specific imports).

Configure the Sprite Video Lab checkout with the environment variable
    SPRITE_VIDEO_LAB_ROOT        (default E:\\sprite-video-lab)
and, if you use components that need the work dir (Real-ESRGAN etc. - not used by this workflow),
    SPRITE_VIDEO_LAB_WORK_DIR    (default <checkout>\\work)
"""
from __future__ import annotations

import os
import pathlib
import re
import sys
import types

import numpy as np
from PIL import Image, ImageDraw

# --- the image helpers used by every metric (same semantics as the canonical project recipe) ----
N8 = [(-1, 0), (1, 0), (0, -1), (0, 1), (-1, -1), (-1, 1), (1, -1), (1, 1)]
ALPHA_VISIBLE = 5
PLATE_BACKGROUND_DISTANCE = 14.0
PLATE_LEAK_DISTANCE = 12.0


def sh(mask, dy, dx):
    return np.roll(np.roll(mask, dy, 0), dx, 1)


def dil(mask, n: int = 1):
    out = mask.copy()
    for _ in range(n):
        acc = out.copy()
        for dy, dx in N8:
            acc |= sh(out, dy, dx)
        out = acc
    return out


def ero(mask, n: int = 1):
    out = mask.copy()
    for _ in range(n):
        acc = out.copy()
        for dy, dx in N8:
            acc &= sh(out, dy, dx)
        out = acc
    return out


def reach_border(mask, iterations: int = 64):
    """Pixels of `mask` connected to the image border (used to distinguish holes from outside)."""
    reached = np.zeros_like(mask)
    reached[0, :] = mask[0, :]
    reached[-1, :] = mask[-1, :]
    reached[:, 0] = mask[:, 0]
    reached[:, -1] = mask[:, -1]
    step = 1
    for _ in range(iterations):
        grown = reached.copy()
        grown[step:, :] |= reached[:-step, :]
        grown[:-step, :] |= reached[step:, :]
        grown[:, step:] |= reached[:, :-step]
        grown[:, :-step] |= reached[:, step:]
        grown &= mask
        if grown.sum() == reached.sum():
            break
        reached = grown
        step = min(step * 2, 512)
    return reached


# --- Sprite Video Lab binding -----------------------------------------------------------------
def svl_root() -> pathlib.Path:
    return pathlib.Path(os.environ.get("SPRITE_VIDEO_LAB_ROOT", r"E:\sprite-video-lab"))


def load_server():
    sys.modules.setdefault("cgi", types.ModuleType("cgi"))  # server.py imports cgi (gone in 3.13+)
    root = svl_root()
    if not (root / "server.py").exists():
        raise SystemExit("Sprite Video Lab checkout not found at %s (set SPRITE_VIDEO_LAB_ROOT)" % root)
    if str(root) not in sys.path:
        sys.path.insert(0, str(root))
    import server  # noqa: PLC0415
    return server


# --- plates, keying, metrics ------------------------------------------------------------------
def frame_order(path: pathlib.Path) -> tuple[int, int, str]:
    """Natural animation order: <prefix><index>_f<sourceFrame>.png, else by name."""
    match = re.match(r"^([A-Za-z]+)(\d+)_f(\d+)", path.stem)
    if match:
        return (0, int(match.group(2)), path.name)
    digits = re.search(r"(\d+)", path.stem)
    return (1, int(digits.group(1)) if digits else 0, path.name)


def plate_colour(image: Image.Image, border: int = 5) -> np.ndarray:
    rgb = np.array(image.convert("RGB"), dtype=np.float32)
    w = border
    strip = np.concatenate([rgb[:w].reshape(-1, 3), rgb[-w:].reshape(-1, 3),
                            rgb[:, :w].reshape(-1, 3), rgb[:, -w:].reshape(-1, 3)])
    return np.median(strip, axis=0)


def is_coloured_plate(plate: np.ndarray, threshold: float = 16.0) -> bool:
    return float(max(plate) - min(plate)) > threshold


def key_frame(server, image: Image.Image, threshold: int, softness: int = 16, halo: int = 1,
              harden: bool = True) -> Image.Image:
    key_rgb = server.auto_key_color(image)
    keyed = server.chroma_key_frame(image, key_rgb, threshold, softness, 0.0, halo, None)
    out = server.alpha_aware_despill_frame(image, keyed, server.auto_key_color(image))
    if harden:
        out, _changed = server.semitransparent_to_opaque_image(out)   # 半透明像素转不透明
    return out


def measure(image: Image.Image, source_rgb: np.ndarray, plate: np.ndarray) -> dict:
    arr = np.array(image.convert("RGBA"), dtype=np.float32)
    rgb, alpha = arr[..., :3], arr[..., 3]
    kept = alpha > ALPHA_VISIBLE
    dist = np.sqrt(((source_rgb - plate) ** 2).sum(-1))
    subject = dist > PLATE_BACKGROUND_DISTANCE
    comp = rgb * (alpha / 255.0)[..., None] + 255.0 * (1.0 - alpha / 255.0)[..., None]
    green = comp[..., 1] - np.maximum(comp[..., 0], comp[..., 2])
    boundary = int((kept & dil(~kept, 2)).sum())
    thin = int((kept & dil(~kept, 1)).sum())
    return {
        "visible_px": int(kept.sum()),
        "deleted_subject_pct": round(100.0 * float((subject & ~kept).sum()) / max(1, int(subject.sum())), 2),
        "enclosed_holes_px": int(((~kept) & ~reach_border(~kept, 64)).sum()),
        "plate_leak_px": int((kept & (dist <= PLATE_LEAK_DISTANCE)).sum()),
        "green_gt0": int((green[kept] > 0).sum()),
        "green_gt4": int((green[kept] > 4).sum()),
        "green_gt8": int((green[kept] > 8).sum()),
        "semi_transparent_px": int(((alpha > 0) & (alpha < 255)).sum()),
        "changed_gt8_vs_source": int((kept & (np.abs(rgb - source_rgb).max(-1) > 8)).sum()),
        "perimeter_over_area": round(boundary / max(1, int(kept.sum())), 4),
        "thin_band_share": round(thin / max(1, int(kept.sum())), 4),
    }


def white_composite(image: Image.Image, size: int = 320) -> Image.Image:
    rgba = image.convert("RGBA")
    flat = Image.new("RGB", rgba.size, (255, 255, 255))
    flat.paste(rgba, (0, 0), rgba)
    return flat.resize((size, size), Image.LANCZOS)


def contact_sheet(items: list[tuple[str, pathlib.Path]], out_path: pathlib.Path, cell: int = 320,
                  columns: int = 5) -> pathlib.Path:
    rows = (len(items) + columns - 1) // columns
    label = 20
    sheet = Image.new("RGB", (columns * cell, rows * (cell + label)), (24, 26, 30))
    draw = ImageDraw.Draw(sheet)
    for index, (name, path) in enumerate(items):
        with Image.open(path) as image:
            tile = white_composite(image, cell)
        x, y = (index % columns) * cell, (index // columns) * (cell + label) + label
        sheet.paste(tile, (x, y))
        draw.text((x + 4, y - 15), name[:48], fill=(240, 230, 120))
    out_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(out_path)
    return out_path
