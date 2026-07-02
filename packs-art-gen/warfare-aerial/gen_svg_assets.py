#!/usr/bin/env python3
"""Emit SVG sources for the warfare-aerial menu UI (logo + icons + buttons).

Vector-crisp, theme-matched (steel + sky-blue). SVGs written to out-dir;
rasterize to PNG with jsui/render_svg.cjs (resvg).

Run:  python gen_svg_assets.py <out_svg_dir>
"""
import os
import sys

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(__file__)
os.makedirs(OUT, exist_ok=True)

STEEL = "#9aa9bd"
STEEL_D = "#5f6c80"
SKY = "#2f6aa0"
SKY_D = "#13314e"
SUN = "#f0b878"
INK = "#0c1622"


def write(name, svg):
    p = os.path.join(OUT, name)
    with open(p, "w", encoding="utf-8") as f:
        f.write(svg)
    print("svg", p)


# ---- LOGO (1600x600 wordmark: "AERIAL WARFARE") ---------------------------
LOGO = f'''<svg xmlns="http://www.w3.org/2000/svg" width="1600" height="600" viewBox="0 0 1600 600">
  <defs>
    <linearGradient id="steel" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#dbe6f2"/>
      <stop offset="0.5" stop-color="#a6b6c9"/>
      <stop offset="1" stop-color="#5f6c80"/>
    </linearGradient>
    <linearGradient id="bar" x1="0" y1="0" x2="1" y2="0">
      <stop offset="0" stop-color="{SKY_D}" stop-opacity="0"/>
      <stop offset="0.5" stop-color="{SUN}"/>
      <stop offset="1" stop-color="{SKY_D}" stop-opacity="0"/>
    </linearGradient>
  </defs>
  <!-- winged-star air emblem -->
  <g transform="translate(800,150)">
    <g fill="url(#steel)">
      <path d="M 0 -58 L 14 -18 L 56 -18 L 22 8 L 36 50 L 0 24 L -36 50 L -22 8 L -56 -18 L -14 -18 Z"/>
    </g>
    <g stroke="url(#steel)" stroke-width="12" stroke-linecap="round">
      <path d="M -60 28 L -150 8" />
      <path d="M 60 28 L 150 8" />
      <path d="M -50 50 L -120 44" />
      <path d="M 50 50 L 120 44" />
    </g>
  </g>
  <text x="800" y="400" text-anchor="middle" font-family="Arial Black, Arial"
        font-size="180" font-weight="900" letter-spacing="8"
        fill="url(#steel)" stroke="{INK}" stroke-width="3">AERIAL</text>
  <text x="800" y="500" text-anchor="middle" font-family="Arial, sans-serif"
        font-size="66" font-weight="700" letter-spacing="34"
        fill="{SUN}">W A R F A R E</text>
  <rect x="300" y="528" width="1000" height="6" fill="url(#bar)"/>
</svg>'''
write("menu_logo.svg", LOGO)


# ---- ICONS (512x512) -------------------------------------------------------
def icon(inner):
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512" viewBox="0 0 512 512">
  <defs>
    <linearGradient id="g" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#39566f"/>
      <stop offset="1" stop-color="{SKY_D}"/>
    </linearGradient>
    <linearGradient id="m" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#e0e9f4"/>
      <stop offset="1" stop-color="{STEEL_D}"/>
    </linearGradient>
  </defs>
  <rect x="24" y="24" width="464" height="464" rx="84" fill="url(#g)"
        stroke="{STEEL}" stroke-width="8"/>
  {inner}
</svg>'''


# fighter jet (top-down)
write("icon_jet.svg", icon(f'''
  <g fill="url(#m)" transform="translate(256,256)">
    <path d="M 0 -176 L 22 -96 L 30 -24
             L 150 70 L 156 110 L 38 64
             L 30 150 L 70 186 L 70 200
             L 0 174 L -70 200 L -70 186 L -30 150
             L -38 64 L -156 110 L -150 70 L -30 -24
             L -22 -96 Z"/>
  </g>'''))

# wing / chevron rank
write("icon_wing.svg", icon(f'''
  <g fill="url(#m)" transform="translate(256,256)">
    <path d="M 0 -150 L 170 60 L 110 60 L 0 -70 L -110 60 L -170 60 Z"/>
    <path d="M 0 -40 L 130 130 L 78 130 L 0 38 L -78 130 L -130 130 Z" opacity="0.85"/>
  </g>'''))

# radar (air-search)
write("icon_radar.svg", icon(f'''
  <g fill="none" stroke="{STEEL}" stroke-width="8">
    <circle cx="256" cy="256" r="150"/>
    <circle cx="256" cy="256" r="96"/>
    <circle cx="256" cy="256" r="44"/>
  </g>
  <path d="M 256 256 L 256 106 A 150 150 0 0 1 382 180 Z" fill="{SUN}" opacity="0.55"/>
  <circle cx="256" cy="256" r="14" fill="url(#m)"/>
  <circle cx="330" cy="150" r="12" fill="{SUN}"/>'''))


# ---- BUTTONS (256x96) ------------------------------------------------------
def button(fill_a, fill_b, stroke, glow):
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="256" height="96" viewBox="0 0 256 96">
  <defs>
    <linearGradient id="bg" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="{fill_a}"/>
      <stop offset="1" stop-color="{fill_b}"/>
    </linearGradient>
  </defs>
  <rect x="6" y="6" width="244" height="84" rx="10" fill="url(#bg)"
        stroke="{stroke}" stroke-width="3"/>
  <rect x="14" y="13" width="228" height="3" rx="1.5" fill="{glow}" opacity="0.6"/>
</svg>'''


write("btn_normal.svg", button(SKY, SKY_D, STEEL_D, STEEL))
write("btn_hover.svg", button("#3a7ab0", SKY, SUN, SUN))
