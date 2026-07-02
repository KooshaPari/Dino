#!/usr/bin/env python3
"""Generate the warfare-naval main-menu background.

Cinematic 1920x1080 ocean/fleet at dusk:
  - deep ocean-to-dusk vertical gradient (naval-blue -> steel-grey horizon glow)
  - low sea horizon with a warm dusk sun glow behind a distant fleet
  - layered warship/carrier silhouettes on the horizon (steel-grey)
  - a foreground swell of dark water with subtle specular streaks
  - CENTER-CLEAR: the middle is kept calm so the injected logo + title read.

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

random.seed(1942)  # naval era


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def screen(a, b):
    return ImageChops.screen(a, b)


HORIZON = int(H * 0.60)


def sky():
    """Dusk sky: deep naval-blue at top -> steel/amber near the horizon."""
    top = (10, 18, 34)       # deep naval blue
    mid = (24, 38, 58)       # steel blue
    horiz = (86, 92, 104)    # steel-grey haze
    img = Image.new("RGB", (W, H), top)
    px = img.load()
    for y in range(HORIZON):
        t = y / HORIZON
        if t < 0.7:
            row = lerp(top, mid, t / 0.7)
        else:
            row = lerp(mid, horiz, (t - 0.7) / 0.3)
        for x in range(W):
            px[x, y] = row
    return img


def sun_glow(img):
    """Warm dusk sun low on the horizon, slightly right of center-clear."""
    cx, cy = int(W * 0.72), int(HORIZON - 6)
    glow = Image.new("RGB", (W, H), (0, 0, 0))
    d = ImageDraw.Draw(glow)
    steps = 70
    radius = int(H * 0.42)
    color = (240, 196, 132)  # dusk amber
    for i in range(steps, 0, -1):
        r = radius * i / steps
        a = (1 - i / steps) ** 2 * 0.9
        c = tuple(int(color[k] * a) for k in range(3))
        d.ellipse([cx - r, cy - r * 0.7, cx + r, cy + r * 0.7], fill=c)
    glow = glow.filter(ImageFilter.GaussianBlur(radius * 0.06))
    # bright sun disk
    sd = ImageDraw.Draw(glow)
    sr = int(H * 0.05)
    sd.ellipse([cx - sr, cy - sr, cx + sr, cy + sr], fill=(255, 224, 168))
    glow = glow.filter(ImageFilter.GaussianBlur(3))
    return screen(img, glow)


def ship_silhouette(d, x, base_y, scale, col):
    """Draw a stylized warship/carrier silhouette (steel-grey) at x, base_y."""
    w = int(180 * scale)
    h = int(34 * scale)
    # hull
    hull = [
        (x - w * 0.5, base_y),
        (x + w * 0.5, base_y),
        (x + w * 0.42, base_y + h * 0.5),
        (x - w * 0.40, base_y + h * 0.5),
    ]
    d.polygon(hull, fill=col)
    # superstructure / island
    sw = w * 0.16
    d.rectangle([x - sw, base_y - h * 1.4, x + sw * 0.2, base_y], fill=col)
    # mast
    d.line([(x - sw * 0.4, base_y - h * 1.4),
            (x - sw * 0.4, base_y - h * 2.4)], fill=col, width=max(1, int(scale * 2)))
    # forward gun turret block
    d.rectangle([x + w * 0.18, base_y - h * 0.6, x + w * 0.34, base_y], fill=col)


def fleet(img):
    """Layered fleet silhouettes on / near the horizon."""
    layer = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    far = (70, 80, 94, 200)   # steel-grey haze
    near = (38, 46, 58, 255)  # darker steel
    # far group (silhouetted against the sun glow)
    for (fx, sc) in [(0.55, 0.7), (0.66, 0.55), (0.80, 0.9), (0.90, 0.5),
                     (0.40, 0.6)]:
        ship_silhouette(d, int(W * fx), HORIZON - 2, sc, far)
    layer = layer.filter(ImageFilter.GaussianBlur(0.6))
    img.paste(layer, (0, 0), layer)
    # one near, sharper capital ship to the left of center-clear
    layer2 = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d2 = ImageDraw.Draw(layer2)
    ship_silhouette(d2, int(W * 0.18), HORIZON + 6, 1.5, near)
    img.paste(layer2, (0, 0), layer2)
    return img


def ocean(img):
    """Foreground sea: dark naval water with dusk specular streaks."""
    px = img.load()
    top = (28, 42, 60)
    bottom = (6, 10, 18)
    for y in range(HORIZON, H):
        t = (y - HORIZON) / (H - HORIZON)
        row = lerp(top, bottom, t)
        for x in range(W):
            px[x, y] = row
    # specular sun streak reflecting down from the sun
    spec = Image.new("RGB", (W, H), (0, 0, 0))
    sd = ImageDraw.Draw(spec)
    cx = int(W * 0.72)
    for y in range(HORIZON, H, 3):
        t = (y - HORIZON) / (H - HORIZON)
        width = int(40 + 220 * t)
        a = int(120 * (1 - t))
        jitter = random.randint(-14, 14)
        col = (int(230 * (1 - t * 0.5)), int(180 * (1 - t * 0.4)),
               int(120 * (1 - t * 0.4)))
        sd.line([(cx - width // 2 + jitter, y), (cx + width // 2 + jitter, y)],
                fill=tuple(int(c * a / 255) for c in col), width=2)
    spec = spec.filter(ImageFilter.GaussianBlur(3))
    img = screen(img, spec)
    # subtle horizontal wave streaks
    waves = Image.new("RGB", (W, H), (0, 0, 0))
    wd = ImageDraw.Draw(waves)
    for _ in range(140):
        y = random.randint(HORIZON + 8, H - 4)
        x = random.randint(0, W)
        ln = random.randint(20, 90)
        b = random.randint(12, 40)
        wd.line([(x, y), (x + ln, y)], fill=(b, b + 6, b + 12), width=1)
    waves = waves.filter(ImageFilter.GaussianBlur(1))
    return screen(img, waves)


def vignette(img):
    v = Image.new("L", (W, H), 0)
    d = ImageDraw.Draw(v)
    d.ellipse([-W * 0.30, -H * 0.30, W * 1.30, H * 1.30], fill=255)
    v = v.filter(ImageFilter.GaussianBlur(220))
    dark = Image.new("RGB", (W, H), (0, 0, 0))
    return Image.composite(img, dark, v)


def main():
    img = sky()
    img = sun_glow(img)
    img = ocean(img)
    img = fleet(img)
    img = vignette(img)
    img.save(OUT)
    print("wrote", OUT, img.size)


if __name__ == "__main__":
    main()
