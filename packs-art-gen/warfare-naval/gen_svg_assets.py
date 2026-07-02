#!/usr/bin/env python3
"""Emit SVG sources for the warfare-naval menu UI (logo + icons + buttons).

Vector-crisp, theme-matched (steel-grey + naval-blue). SVGs are written to the
given out-dir; rasterize them to PNG with jsui/render_svg.cjs (resvg).

Run:  python gen_svg_assets.py <out_svg_dir>
"""
import os
import sys

OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.dirname(__file__)
os.makedirs(OUT, exist_ok=True)

STEEL = "#8e98a6"
STEEL_D = "#5a6472"
NAVY = "#16314f"
NAVY_D = "#0c1d31"
AMBER = "#e8c489"
INK = "#0a141f"


def write(name, svg):
    p = os.path.join(OUT, name)
    with open(p, "w", encoding="utf-8") as f:
        f.write(svg)
    print("svg", p)


# ---- LOGO (1600x600 wordmark: "NAVAL WARFARE") ----------------------------
LOGO = f'''<svg xmlns="http://www.w3.org/2000/svg" width="1600" height="600" viewBox="0 0 1600 600">
  <defs>
    <linearGradient id="steel" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#cdd6e0"/>
      <stop offset="0.5" stop-color="#9aa6b4"/>
      <stop offset="1" stop-color="#5d6878"/>
    </linearGradient>
    <linearGradient id="bar" x1="0" y1="0" x2="1" y2="0">
      <stop offset="0" stop-color="{NAVY_D}" stop-opacity="0"/>
      <stop offset="0.5" stop-color="{AMBER}"/>
      <stop offset="1" stop-color="{NAVY_D}" stop-opacity="0"/>
    </linearGradient>
  </defs>
  <!-- anchor emblem (compact, above the wordmark) -->
  <g transform="translate(800,128)" fill="#aab4c2" stroke="#5d6878" stroke-width="3" stroke-linejoin="round">
    <!-- ring -->
    <path d="M 0 -94 a 22 22 0 1 0 0.1 0 Z M 0 -82 a 10 10 0 1 1 -0.1 0 Z" fill-rule="evenodd"/>
    <!-- shank -->
    <rect x="-7" y="-58" width="14" height="134" rx="6"/>
    <!-- stock (crossbar) -->
    <rect x="-46" y="-30" width="92" height="13" rx="6"/>
    <!-- flukes: crescent under the shank -->
    <path d="M -78 6 L -64 26
             A 66 66 0 0 0 64 26 L 78 6
             A 84 84 0 0 1 0 60
             A 84 84 0 0 1 -78 6 Z"/>
  </g>
  <text x="800" y="400" text-anchor="middle" font-family="Arial Black, Arial"
        font-size="180" font-weight="900" letter-spacing="10"
        fill="url(#steel)" stroke="{INK}" stroke-width="3">NAVAL</text>
  <text x="800" y="500" text-anchor="middle" font-family="Arial, sans-serif"
        font-size="66" font-weight="700" letter-spacing="34"
        fill="{AMBER}">W A R F A R E</text>
  <rect x="300" y="528" width="1000" height="6" fill="url(#bar)"/>
</svg>'''
write("menu_logo.svg", LOGO)


# ---- ICONS (512x512) -------------------------------------------------------
def icon(inner):
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="512" height="512" viewBox="0 0 512 512">
  <defs>
    <linearGradient id="g" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#3a4658"/>
      <stop offset="1" stop-color="{NAVY_D}"/>
    </linearGradient>
    <linearGradient id="m" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#d7dee7"/>
      <stop offset="1" stop-color="{STEEL_D}"/>
    </linearGradient>
  </defs>
  <rect x="24" y="24" width="464" height="464" rx="84" fill="url(#g)"
        stroke="{STEEL}" stroke-width="8"/>
  {inner}
</svg>'''


# anchor (filled shapes so degenerate-bbox strokes don't vanish under resvg)
write("icon_anchor.svg", icon(f'''
  <g fill="url(#m)" stroke="{STEEL_D}" stroke-width="5" stroke-linejoin="round">
    <path d="M 256 86 a 34 34 0 1 0 0.1 0 Z M 256 104 a 16 16 0 1 1 -0.1 0 Z" fill-rule="evenodd"/>
    <rect x="244" y="150" width="24" height="248" rx="10"/>
    <rect x="184" y="186" width="144" height="22" rx="11"/>
    <path d="M 110 300 L 142 332
             A 116 116 0 0 0 370 332 L 402 300
             A 150 150 0 0 1 256 408
             A 150 150 0 0 1 110 300 Z"/>
  </g>'''))

# warship (silhouette + waterline)
write("icon_ship.svg", icon(f'''
  <g fill="url(#m)">
    <path d="M 96 300 L 416 300 L 388 352 L 124 352 Z"/>
    <rect x="214" y="214" width="84" height="86"/>
    <rect x="300" y="248" width="46" height="52"/>
    <rect x="170" y="262" width="40" height="38"/>
  </g>
  <rect x="250" y="150" width="12" height="68" rx="6" fill="{STEEL}"/>
  <g stroke="{STEEL}" stroke-width="10" stroke-linecap="round" opacity="0.8">
    <line x1="92" y1="384" x2="150" y2="384"/>
    <line x1="200" y1="384" x2="312" y2="384"/>
    <line x1="362" y1="384" x2="420" y2="384"/>
  </g>'''))

# radar (sweep dish)
write("icon_radar.svg", icon(f'''
  <g fill="none" stroke="{STEEL}" stroke-width="8">
    <circle cx="256" cy="256" r="150"/>
    <circle cx="256" cy="256" r="96"/>
    <circle cx="256" cy="256" r="44"/>
  </g>
  <path d="M 256 256 L 256 106 A 150 150 0 0 1 382 180 Z" fill="{AMBER}" opacity="0.55"/>
  <circle cx="256" cy="256" r="14" fill="url(#m)"/>
  <circle cx="330" cy="150" r="12" fill="{AMBER}"/>'''))


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


write("btn_normal.svg", button(NAVY, NAVY_D, STEEL_D, STEEL))
write("btn_hover.svg", button("#1f4368", NAVY, AMBER, AMBER))
