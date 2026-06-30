#!/usr/bin/env python3
"""Generate the warfare-aerial main-menu background.

Cinematic 1920x1080 sky / air-warfare scene at sunrise-storm:
  - sky gradient: stormy steel-grey high -> warm sunrise band -> sky-blue low
  - a sunrise glow on the horizon behind layered cloud banks
  - banked fighter-jet silhouettes (with contrails) crossing the upper sky
  - soft cloud strata + a calm CENTER-CLEAR for the injected logo + title.

Run:  python gen_menu_bg.py [out.png]
Deps: Pillow.  Fully procedural, no network.
"""
import math
import os
import random
import sys

from PIL import Image, ImageChops, ImageDraw, ImageFilter

W, H = 1920, 1080
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(
    os.path.dirname(__file__), "menu_bg.png")

random.seed(1947)  # jet age


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def screen(a, b):
    return ImageChops.screen(a, b)


def sky():
    """Stormy steel high -> sunrise warm band -> sky-blue low."""
    top = (28, 34, 46)        # stormy steel-grey
    mid = (96, 86, 92)        # muted storm
    band = (208, 150, 110)    # sunrise warm band
    low = (96, 132, 168)      # sky-blue
    img = Image.new("RGB", (W, H), top)
    px = img.load()
    b1 = int(H * 0.58)   # start of warm band
    b2 = int(H * 0.74)   # peak of warm band
    for y in range(H):
        if y < b1:
            row = lerp(top, mid, y / b1)
        elif y < b2:
            row = lerp(mid, band, (y - b1) / (b2 - b1))
        else:
            row = lerp(band, low, (y - b2) / (H - b2))
        for x in range(W):
            px[x, y] = row
    return img


def sunrise_glow(img):
    """Warm sunrise glow centered low, left of center-clear."""
    cx, cy = int(W * 0.30), int(H * 0.70)
    glow = Image.new("RGB", (W, H), (0, 0, 0))
    d = ImageDraw.Draw(glow)
    steps = 70
    radius = int(H * 0.50)
    color = (255, 198, 150)
    for i in range(steps, 0, -1):
        r = radius * i / steps
        a = (1 - i / steps) ** 2 * 0.85
        c = tuple(int(color[k] * a) for k in range(3))
        d.ellipse([cx - r, cy - r * 0.65, cx + r, cy + r * 0.65], fill=c)
    glow = glow.filter(ImageFilter.GaussianBlur(radius * 0.07))
    return screen(img, glow)


def cloud_band(img, cy, thickness, col, blur):
    band = Image.new("RGB", (W, H), (0, 0, 0))
    d = ImageDraw.Draw(band)
    for _ in range(18):
        x = random.randint(-200, W + 200)
        w = random.randint(220, 560)
        h = random.randint(int(thickness * 0.5), thickness)
        y = cy + random.randint(-thickness // 2, thickness // 2)
        d.ellipse([x, y, x + w, y + h], fill=col)
    band = band.filter(ImageFilter.GaussianBlur(blur))
    return screen(img, band)


def jet_silhouette(d, x, y, scale, ang, col):
    """Stylized swept-wing fighter-jet (top-down), nose toward +Y, banked `ang`.

    A slender fuselage with sharply rear-swept delta wings and tailplanes —
    reads as a fast jet rather than a symmetric star.
    """
    L = 130 * scale          # fuselage length
    fw = L * 0.07            # fuselage half-width
    pts = [
        (0.0, L * 0.52),                 # nose tip
        (fw, L * 0.30),                  # nose shoulder R
        (fw * 0.9, L * 0.08),
        (L * 0.50, -L * 0.30),           # right wing leading sweep
        (L * 0.54, -L * 0.40),           # right wingtip
        (fw * 1.3, -L * 0.18),           # wing root trailing R
        (fw * 1.2, -L * 0.34),
        (L * 0.22, -L * 0.52),           # right tailplane tip
        (fw, -L * 0.44),
        (fw * 0.7, -L * 0.52),           # tail end R
        (-fw * 0.7, -L * 0.52),          # tail end L
        (-fw, -L * 0.44),
        (-L * 0.22, -L * 0.52),          # left tailplane tip
        (-fw * 1.2, -L * 0.34),
        (-fw * 1.3, -L * 0.18),          # wing root trailing L
        (-L * 0.54, -L * 0.40),          # left wingtip
        (-L * 0.50, -L * 0.30),          # left wing leading sweep
        (-fw * 0.9, L * 0.08),
        (-fw, L * 0.30),                 # nose shoulder L
    ]
    ca, sa = math.cos(ang), math.sin(ang)
    rp = [(x + px * ca - py * sa, y + px * sa + py * ca) for (px, py) in pts]
    d.polygon(rp, fill=col)


def jets(img):
    """A flight of banked jets with contrails crossing the upper sky."""
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    col = (26, 30, 40, 255)   # dark steel silhouette
    # contrails first (behind jets)
    trail = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    td = ImageDraw.Draw(trail)
    flight = [(0.58, 0.24, 1.3), (0.69, 0.32, 1.0),
              (0.79, 0.22, 0.9), (0.50, 0.34, 0.85)]
    # nose points toward +Y locally; heading down-left = nose dir (-1,+1).
    ang = math.atan2(1.0, -1.0) - math.pi / 2  # rotate +Y axis to heading
    hx, hy = -0.7071, 0.7071                    # unit heading (down-left)
    for (fx, fy, sc) in flight:
        x, y = int(W * fx), int(H * fy)
        # twin contrails trailing OPPOSITE the heading (up-right), from wingtips
        wx, wy = -hy, hx   # perpendicular (wing axis)
        for off in (-1.0, 1.0):
            sx = x + int(off * 50 * sc * wx)
            sy = y + int(off * 50 * sc * wy)
            ex = sx - int(500 * sc * hx)
            ey = sy - int(500 * sc * hy)
            td.line([(sx, sy), (ex, ey)],
                    fill=(255, 255, 255, 65), width=max(1, int(sc * 2)))
    trail = trail.filter(ImageFilter.GaussianBlur(2))
    img.paste(trail, (0, 0), trail)
    for (fx, fy, sc) in flight:
        jet_silhouette(d, int(W * fx), int(H * fy), sc, ang, col)
    layer = layer.filter(ImageFilter.GaussianBlur(0.5))
    img.paste(layer, (0, 0), layer)
    return img


def vignette(img):
    v = Image.new("L", (W, H), 0)
    d = ImageDraw.Draw(v)
    d.ellipse([-W * 0.30, -H * 0.30, W * 1.30, H * 1.30], fill=255)
    v = v.filter(ImageFilter.GaussianBlur(220))
    dark = Image.new("RGB", (W, H), (0, 0, 0))
    return Image.composite(img, dark, v)


def main():
    img = sky()
    img = sunrise_glow(img)
    img = cloud_band(img, int(H * 0.66), 150, (210, 178, 168), 60)
    img = cloud_band(img, int(H * 0.80), 200, (150, 150, 170), 80)
    img = cloud_band(img, int(H * 0.40), 90, (70, 76, 92), 50)
    img = jets(img)
    img = vignette(img)
    img.save(OUT)
    print("wrote", OUT, img.size)


if __name__ == "__main__":
    main()
