#!/usr/bin/env python3
r"""
Prebake lens textures for edge distortion.

Output encoding per pixel (RGBA):
- R,G: direction to edge (encoded from [-1, 1] to [0, 255])
- B: normalized edge distance in [0, 255] where:
     0   = near border
     255 = far from border (or no hit in scan range)
- A: original alpha from source texture

This script reproduces the old shader-style directional scan:
- For each inside-mask pixel, cast rays in N directions.
- First ray hit against outside-mask gives distance sample.
- Direction is weighted average of hit directions.
"""

from __future__ import annotations

import argparse
from concurrent.futures import ProcessPoolExecutor, as_completed
import json
import math
import os
from pathlib import Path
from typing import Iterable, List, Sequence, Tuple
# pip install pillow
# python .\prebake_lens_masks.py --input-dir .\LensTextures --output-dir .\LensTextures_prebaked --num-directions 8 --max-steps 450 --threshold 0.5

def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Prebake lens masks for NVG distortion.")
    parser.add_argument("--input-dir", type=Path, default=Path("LensTextures"))
    parser.add_argument("--output-dir", type=Path, default=Path("LensTextures_prebaked"))
    parser.add_argument(
        "--threshold",
        type=float,
        default=0.5,
        help="Inside-mask threshold in [0,1] after alpha processing.",
    )
    parser.add_argument(
        "--no-invert-alpha",
        action="store_true",
        help="Use alpha directly. Default uses (1-alpha), matching your shader.",
    )
    parser.add_argument(
        "--max-steps",
        type=int,
        default=450,
        help="Raymarch steps (old shader equivalent).",
    )
    parser.add_argument(
        "--num-directions",
        type=int,
        default=16,
        help="Direction count. Use 8 for old behavior, 16+ for higher quality.",
    )
    parser.add_argument(
        "--workers",
        type=int,
        default=1,
        help="Number of worker processes. 1 = sequential.",
    )
    return parser.parse_args()


def clamp01(v: float) -> float:
    if v < 0.0:
        return 0.0
    if v > 1.0:
        return 1.0
    return v


def to_u8(v: float) -> int:
    return int(max(0, min(255, round(v))))


def generate_dirs(count: int) -> List[Tuple[float, float]]:
    dirs: List[Tuple[float, float]] = []
    for i in range(count):
        a = (2.0 * math.pi * i) / count
        dirs.append((math.cos(a), math.sin(a)))
    return dirs


def bilinear_sample(mask: Sequence[float], w: int, h: int, u: float, v: float) -> float:
    u = clamp01(u)
    v = clamp01(v)

    x = u * w - 0.5
    y = v * h - 0.5

    x0 = int(math.floor(x))
    y0 = int(math.floor(y))
    x1 = x0 + 1
    y1 = y0 + 1

    if x0 < 0:
        x0 = 0
    if y0 < 0:
        y0 = 0
    if x1 >= w:
        x1 = w - 1
    if y1 >= h:
        y1 = h - 1

    fx = x - x0
    fy = y - y0

    i00 = y0 * w + x0
    i10 = y0 * w + x1
    i01 = y1 * w + x0
    i11 = y1 * w + x1

    s00 = mask[i00]
    s10 = mask[i10]
    s01 = mask[i01]
    s11 = mask[i11]

    sx0 = s00 + (s10 - s00) * fx
    sx1 = s01 + (s11 - s01) * fx
    return sx0 + (sx1 - sx0) * fy


def prebake_one(
    src_path: Path,
    out_dir: Path,
    threshold: float,
    invert_alpha: bool,
    max_steps: int,
    dirs: Sequence[Tuple[float, float]],
) -> str:
    try:
        from PIL import Image
    except ImportError as exc:
        raise RuntimeError("Missing dependency: Pillow. Install with: pip install pillow") from exc

    img = Image.open(src_path).convert("RGBA")
    w, h = img.size
    src = list(img.getdata())

    alpha: List[int] = [px[3] for px in src]
    mask: List[float] = []
    inside: List[bool] = []

    for a_u8 in alpha:
        a = a_u8 / 255.0
        m = (1.0 - a) if invert_alpha else a
        mask.append(m)
        inside.append(m > threshold)

    du = 1.0 / w
    dv = 1.0 / h
    n = w * h
    out: List[Tuple[int, int, int, int]] = [(0, 0, 0, 0)] * n

    inside_count = 0
    for y in range(h):
        base = y * w
        v0 = (y + 0.5) * dv

        for x in range(w):
            i = base + x
            a_u8 = alpha[i]

            if not inside[i]:
                out[i] = (0, 0, 0, a_u8)
                continue

            inside_count += 1
            u0 = (x + 0.5) * du

            nearest_norm = 1.0
            accum_x = 0.0
            accum_y = 0.0
            accum_w = 0.0

            for dx, dy in dirs:
                hit_norm = 1.0
                ray_u = u0
                ray_v = v0
                step_u = dx * du
                step_v = dy * dv

                hit = False
                for step in range(1, max_steps + 1):
                    ray_u += step_u
                    ray_v += step_v
                    m = bilinear_sample(mask, w, h, ray_u, ray_v)
                    if m <= threshold:
                        hit_norm = step / float(max_steps)
                        hit = True
                        break

                if not hit:
                    continue

                if hit_norm < nearest_norm:
                    nearest_norm = hit_norm

                wdir = 1.0 - hit_norm
                wdir *= wdir
                accum_x += dx * wdir
                accum_y += dy * wdir
                accum_w += wdir

            if accum_w > 1e-8:
                inv = 1.0 / accum_w
                ex = accum_x * inv
                ey = accum_y * inv
                el = math.hypot(ex, ey)
                if el > 1e-8:
                    ex /= el
                    ey /= el
                else:
                    ex = 0.0
                    ey = 0.0
            else:
                ex = 0.0
                ey = 0.0

            r = to_u8((ex * 0.5 + 0.5) * 255.0)
            g = to_u8((ey * 0.5 + 0.5) * 255.0)
            b = to_u8(clamp01(nearest_norm) * 255.0)
            out[i] = (r, g, b, a_u8)

    out_dir.mkdir(parents=True, exist_ok=True)
    out_path = out_dir / f"{src_path.stem}.png"
    out_img = Image.new("RGBA", (w, h))
    out_img.putdata(out)
    out_img.save(out_path)
    """
    meta = {
        "source": str(src_path),
        "output": str(out_path),
        "size": [w, h],
        "inside_threshold": threshold,
        "invert_alpha_used": invert_alpha,
        "max_steps": max_steps,
        "direction_count": dir_count,
        "inside_pixels": inside_count,
        "encoding": {
            "R": "edge direction X encoded from [-1,1] -> [0,255]",
            "G": "edge direction Y encoded from [-1,1] -> [0,255]",
            "B": "nearest edge distance normalized [0,1] by max_steps",
            "A": "original alpha",
        },
    }
    (out_dir / f"{src_path.stem}_prebaked.json").write_text(
        json.dumps(meta, indent=2), encoding="utf-8"
    )
    """
    return str(out_path)


def iter_pngs(folder: Path) -> Iterable[Path]:
    for p in sorted(folder.glob("*.png")):
        if p.is_file():
            yield p


def main() -> int:
    args = parse_args()
    input_dir: Path = args.input_dir
    output_dir: Path = args.output_dir
    threshold: float = float(args.threshold)
    invert_alpha: bool = not args.no_invert_alpha
    max_steps: int = int(args.max_steps)
    num_directions: int = int(args.num_directions)
    workers: int = int(args.workers)

    if not input_dir.exists():
        raise FileNotFoundError(f"Input dir not found: {input_dir}")
    if not (0.0 <= threshold <= 1.0):
        raise ValueError("--threshold must be in [0,1]")
    if max_steps <= 0:
        raise ValueError("--max-steps must be > 0")
    if num_directions < 4:
        raise ValueError("--num-directions must be >= 4")
    if workers <= 0:
        raise ValueError("--workers must be > 0")

    dirs = generate_dirs(num_directions)
    pngs = list(iter_pngs(input_dir))
    if not pngs:
        print(f"No PNG files found in: {input_dir}")
        return 0

    print(
        f"Prebake config: dirs={len(dirs)}, max_steps={max_steps}, "
        f"threshold={threshold}, invert_alpha={invert_alpha}, workers={workers}"
    )

    if workers == 1:
        for src in pngs:
            out_path = prebake_one(
                src_path=src,
                out_dir=output_dir,
                threshold=threshold,
                invert_alpha=invert_alpha,
                max_steps=max_steps,
                dirs=dirs,
            )
            print(f"[OK] {src.name} -> {Path(out_path).name}")
    else:
        max_workers = min(workers, len(pngs), os.cpu_count() or workers)
        failures = 0
        with ProcessPoolExecutor(max_workers=max_workers) as executor:
            future_to_src = {
                executor.submit(
                    prebake_one,
                    src,
                    output_dir,
                    threshold,
                    invert_alpha,
                    max_steps,
                    dirs,
                ): src
                for src in pngs
            }
            for future in as_completed(future_to_src):
                src = future_to_src[future]
                try:
                    out_path = future.result()
                    print(f"[OK] {src.name} -> {Path(out_path).name}")
                except Exception as ex:
                    failures += 1
                    print(f"[ERR] {src.name}: {ex}")
        if failures:
            raise RuntimeError(f"Failed processing {failures} image(s).")

    print(f"Done. Wrote {len(pngs)} textures to: {output_dir}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
