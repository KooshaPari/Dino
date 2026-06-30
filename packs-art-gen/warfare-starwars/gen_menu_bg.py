#!/usr/bin/env python3
"""Generate the warfare-starwars Clone Wars main-menu background.

A tasteful, cinematic 1920x1080 space composition:
  - deep space gradient (near-black indigo -> faint nebula glow at edges)
  - a large planet at lower-left whose limb is rim-lit (an "eclipse" crescent)
  - a bright back-lit sun/eclipse glow just past the planet's rim
  - sparse starfield + a couple of soft dust bands
  - CENTER-CLEAR: the middle third is kept dark/quiet so the injected emblem
    + CLONE WARS title read cleanly on top (the themer overlays those).
Edges are the detailed region; center stays calm.

Run:  python gen_menu_bg.py [out.png]
Deps: Pillow (PIL).  No network, no download — fully procedural.
"""
import math
import os
import random
import sys

from PIL import Image, ImageDraw, ImageFilter

W, H = 1920, 1080
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(
    os.path.dirname(__file__), "menu_bg.png")

random.seed(1977)  # A New Hope


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def radial_bg():
    """Deep-space vertical+radial gradient base."""
    top = (6, 9, 20)        # near-black indigo
    bottom = (3, 5, 12)     # darker base
    img = Image.new("RGB", (W, H), top)
    px = img.load()
    for y in range(H):
        t = y / H
        row = lerp(top, bottom, t)
        for x in range(W):
            px[x, y] = row
    return img


def add_glow(img, cx, cy, radius, color, strength=1.0):
    """Soft additive radial glow centered at (cx,cy)."""
    glow = Image.new("RGB", (W, H), (0, 0, 0))
    d = ImageDraw.Draw(glow)
    steps = 60
    for i in range(steps, 0, -1):
        r = radius * i / steps
        a = strength * (1 - i / steps) ** 2
        c = tuple(int(color[k] * a) for k in range(3))
        d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=c)
    glow = glow.filter(ImageFilter.GaussianBlur(radius * 0.08))
    return Image.blend(img, ImageChops_screen(img, glow), 1.0)


def ImageChops_screen(a, b):
    from PIL import ImageChops
    return ImageChops.screen(a, b)


def starfield(img):
    d = ImageDraw.Draw(img)
    for _ in range(900):
        x, y = random.randint(0, W - 1), random.randint(0, H - 1)
        # thin out stars in the center-clear zone
        if 0.30 * W < x < 0.70 * W and 0.10 * H < y < 0.75 * H:
            if random.random() < 0.7:
                continue
        b = random.randint(40, 150)
        if random.random() < 0.04:
            b = random.randint(180, 255)
            d.ellipse([x - 1, y - 1, x + 1, y + 1], fill=(b, b, b))
        else:
            d.point((x, y), fill=(b, b, b))
    return img


def planet(img):
    """Large rim-lit planet at lower-left with an eclipse crescent."""
    pcx, pcy, pr = int(W * 0.20), int(H * 0.92), int(H * 0.62)
    # planet body: a dark sphere (mostly in shadow)
    body = Image.new("RGB", (W, H), (0, 0, 0))
    mask = Image.new("L", (W, H), 0)
    md = ImageDraw.Draw(mask)
    md.ellipse([pcx - pr, pcy - pr, pcx + pr, pcy + pr], fill=255)
    bd = ImageDraw.Draw(body)
    # subtle dark blue surface with faint banding
    for i in range(pr, 0, -2):
        t = i / pr
        col = lerp((10, 16, 30), (4, 6, 14), t)
        bd.ellipse([pcx - i, pcy - i, pcx + i, pcy + i], fill=col)
    body = body.filter(ImageFilter.GaussianBlur(2))
    img.paste(body, (0, 0), mask)

    # rim light: bright crescent along the upper-right limb (the eclipse edge)
    rim = Image.new("RGB", (W, H), (0, 0, 0))
    rd = ImageDraw.Draw(rim)
    # draw a slightly offset bright ring, then mask to the planet disk
    off = int(pr * 0.045)
    rd.ellipse([pcx - pr - off, pcy - pr - off, pcx + pr - off, pcy + pr - off],
               outline=(255, 224, 150), width=max(3, pr // 60))
    rim = rim.filter(ImageFilter.GaussianBlur(6))
    rimmask = Image.new("L", (W, H), 0)
    rmd = ImageDraw.Draw(rimmask)
    rmd.ellipse([pcx - pr, pcy - pr, pcx + pr, pcy + pr], fill=255)
    img.paste(ImageChops_screen(img, rim), (0, 0), rimmask)
    return img, (pcx, pcy, pr)


def dust_bands(img):
    band = Image.new("RGB", (W, H), (0, 0, 0))
    d = ImageDraw.Draw(band)
    for _ in range(3):
        y = random.randint(int(H * 0.15), int(H * 0.85))
        col = random.choice([(20, 18, 34), (28, 16, 18), (14, 22, 30)])
        d.ellipse([-300, y, W + 300, y + random.randint(120, 260)], fill=col)
    band = band.filter(ImageFilter.GaussianBlur(90))
    return ImageChops_screen(img, band)


def vignette(img):
    v = Image.new("L", (W, H), 0)
    d = ImageDraw.Draw(v)
    d.ellipse([-W * 0.35, -H * 0.35, W * 1.35, H * 1.35], fill=255)
    v = v.filter(ImageFilter.GaussianBlur(220))
    dark = Image.new("RGB", (W, H), (0, 0, 0))
    return Image.composite(img, dark, v)


def main():
    img = radial_bg()
    img = dust_bands(img)
    img = starfield(img)
    img, (pcx, pcy, pr) = planet(img)
    # eclipse back-glow: bright sun just past the planet's upper-right rim
    img = add_glow(img, int(pcx + pr * 0.78), int(pcy - pr * 0.74),
                   int(H * 0.34), (255, 232, 170), strength=0.95)
    # faint cool counter-glow upper-right to balance the frame edges
    img = add_glow(img, int(W * 0.93), int(H * 0.12),
                   int(H * 0.30), (90, 130, 200), strength=0.35)
    img = vignette(img)
    img.save(OUT)
    print("wrote", OUT, img.size)


if __name__ == "__main__":
    main()
