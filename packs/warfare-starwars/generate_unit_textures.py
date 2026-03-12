#!/usr/bin/env python3
"""
DINOForge Star Wars Pack - Unit Texture Generation Pipeline
============================================================

Generates faction-specific unit textures for all 26 Star Wars units (13×2 factions).
Uses HSV-based color replacement or procedural generation to create distinct
faction appearance (Republic white/blue, CIS dark/orange).

Color Transformation Pipeline:
  1. Create or load base unit texture
  2. Convert RGB to HSV
  3. Apply hue/saturation/value shifts based on target palette
  4. Convert back to RGB
  5. Export as PNG with alpha preservation (sRGB)

Unit Composition:
  - 13 Republic units: Clone Militia → Clone Commando (mixed infantry/vehicles)
  - 13 CIS units: B1 Battle Droid → MagnaGuard (mixed droids/vehicles)
"""

import os
import sys
import json
import argparse
import logging
from pathlib import Path
from typing import Dict, List, Tuple, Optional
from dataclasses import dataclass, asdict
from multiprocessing import Pool, cpu_count
import colorsys

try:
    from PIL import Image, ImageDraw
    import numpy as np
except ImportError:
    print("ERROR: Required packages missing. Install with:")
    print("  pip install Pillow numpy")
    exit(1)

# ============================================================================
# Color Palette Definitions
# ============================================================================

@dataclass
class ColorPalette:
    """Faction-specific color palette for texture generation."""
    name: str
    primary: Tuple[int, int, int]      # Dominant base color
    secondary: Tuple[int, int, int]    # Accent/trim color
    tertiary: Tuple[int, int, int]     # Detail/highlight color
    hue_shift: float                   # Hue rotation in degrees (-180 to 180)
    saturation_multiplier: float       # Saturation adjustment (0.5 to 2.0)
    value_multiplier: float            # Brightness adjustment (0.7 to 1.3)

# Republic Palette: Clean white/blue scheme (high-tech, organized)
REPUBLIC_PALETTE = ColorPalette(
    name="republic",
    primary=(245, 245, 245),           # Pristine white #F5F5F5
    secondary=(26, 58, 107),           # Deep Republic blue #1A3A6B
    tertiary=(100, 160, 220),          # Accent blue #64A0DC
    hue_shift=210,                     # Shift toward cool blues
    saturation_multiplier=0.8,         # Slightly desaturated for clean look
    value_multiplier=1.1               # Brighten for tech aesthetic
)

# CIS Palette: Industrial dark/orange scheme (mechanical, utilitarian)
CIS_PALETTE = ColorPalette(
    name="cis",
    primary=(68, 68, 68),              # Dark grey #444444
    secondary=(179, 90, 0),            # Rust orange #B35A00
    tertiary=(102, 51, 0),             # Dark brown #663300
    hue_shift=30,                      # Shift toward warm oranges
    saturation_multiplier=1.2,         # More saturated for droid appearance
    value_multiplier=0.9               # Slightly darker for industrial look
)

# ============================================================================
# Unit Registry
# ============================================================================

@dataclass
class UnitTextureSpec:
    """Texture specification for a single unit."""
    unit_id: str
    display_name: str
    faction: str                       # 'republic' or 'cis'
    unit_class: str                    # e.g., MilitiaLight, CoreLineInfantry, etc.
    tier: int                          # 1-3
    is_vehicle: bool                   # True for vehicles/droids, False for infantry

# Republic Units (13 total)
REPUBLIC_UNITS = [
    UnitTextureSpec("rep_clone_militia", "Clone Militia", "republic", "MilitiaLight", 1, False),
    UnitTextureSpec("rep_clone_trooper", "Clone Trooper", "republic", "CoreLineInfantry", 1, False),
    UnitTextureSpec("rep_clone_heavy", "Clone Heavy Trooper", "republic", "HeavyInfantry", 2, False),
    UnitTextureSpec("rep_clone_sharpshooter", "Clone Sharpshooter", "republic", "Skirmisher", 2, False),
    UnitTextureSpec("rep_barc_speeder", "BARC Speeder", "republic", "FastVehicle", 2, True),
    UnitTextureSpec("rep_atte_crew", "AT-TE Crew", "republic", "MainBattleVehicle", 3, True),
    UnitTextureSpec("rep_clone_medic", "Clone Medic", "republic", "SupportEngineer", 2, False),
    UnitTextureSpec("rep_arf_trooper", "ARF Trooper", "republic", "Recon", 1, False),
    UnitTextureSpec("rep_arc_trooper", "ARC Trooper", "republic", "EliteLineInfantry", 3, False),
    UnitTextureSpec("rep_jedi_knight", "Jedi Knight", "republic", "HeroCommander", 3, False),
    UnitTextureSpec("rep_clone_wall_guard", "Clone Wall Guard", "republic", "StaticMG", 2, False),
    UnitTextureSpec("rep_clone_sniper", "Clone Sniper", "republic", "Skirmisher", 3, False),
    UnitTextureSpec("rep_clone_commando", "Clone Commando", "republic", "ShieldedElite", 3, False),
]

# CIS Units (13 total)
CIS_UNITS = [
    UnitTextureSpec("cis_b1_battle_droid", "B1 Battle Droid", "cis", "MilitiaLight", 1, True),
    UnitTextureSpec("cis_b1_squad", "B1 Squad", "cis", "CoreLineInfantry", 1, True),
    UnitTextureSpec("cis_b2_super_battle_droid", "B2 Super Battle Droid", "cis", "HeavyInfantry", 2, True),
    UnitTextureSpec("cis_sniper_droid", "Sniper Droid", "cis", "Skirmisher", 2, True),
    UnitTextureSpec("cis_stap_pilot", "STAP Pilot", "cis", "FastVehicle", 1, True),
    UnitTextureSpec("cis_aat_crew", "AAT Crew", "cis", "MainBattleVehicle", 2, True),
    UnitTextureSpec("cis_medical_droid", "Medical Droid", "cis", "SupportEngineer", 1, True),
    UnitTextureSpec("cis_probe_droid", "Probe Droid", "cis", "Recon", 1, True),
    UnitTextureSpec("cis_bx_commando_droid", "BX Commando Droid", "cis", "EliteLineInfantry", 3, True),
    UnitTextureSpec("cis_general_grievous", "General Grievous", "cis", "HeroCommander", 3, True),
    UnitTextureSpec("cis_droideka", "Droideka", "cis", "StaticMG", 3, True),
    UnitTextureSpec("cis_dwarf_spider_droid", "DSD1 Dwarf Spider Droid", "cis", "StaticAT", 2, True),
    UnitTextureSpec("cis_magnaguard", "IG-100 MagnaGuard", "cis", "ShieldedElite", 3, True),
]

ALL_UNITS = REPUBLIC_UNITS + CIS_UNITS

# ============================================================================
# Color Transformation Functions
# ============================================================================

def rgb_to_hsv(rgb: Tuple[int, int, int]) -> Tuple[float, float, float]:
    """Convert RGB (0-255) to HSV (0-1)."""
    r, g, b = rgb[0] / 255.0, rgb[1] / 255.0, rgb[2] / 255.0
    h, s, v = colorsys.rgb_to_hsv(r, g, b)
    return h, s, v


def hsv_to_rgb(h: float, s: float, v: float) -> Tuple[int, int, int]:
    """Convert HSV (0-1) to RGB (0-255)."""
    r, g, b = colorsys.hsv_to_rgb(h, s, v)
    return int(r * 255), int(g * 255), int(b * 255)


def apply_color_transformation(
    pixel: Tuple[int, int, int, int],
    palette: ColorPalette,
    preserve_alpha: bool = True
) -> Tuple[int, int, int, int]:
    """
    Apply faction color palette to a single RGBA pixel.

    Strategy:
    1. Extract alpha
    2. Convert RGB to HSV
    3. Apply hue shift (rotate toward faction color)
    4. Scale saturation for vibrancy control
    5. Adjust value (brightness) for visual depth
    6. Convert back to RGB
    7. Preserve alpha channel
    """
    if preserve_alpha and pixel[3] < 128:
        # Skip fully or mostly transparent pixels
        return pixel

    r, g, b, a = pixel

    # Convert to HSV
    h, s, v = rgb_to_hsv((r, g, b))

    # Apply transformations
    h = (h + (palette.hue_shift / 360.0)) % 1.0  # Rotate hue
    s = min(1.0, s * palette.saturation_multiplier)  # Adjust saturation
    v = min(1.0, v * palette.value_multiplier)  # Adjust brightness

    # Convert back to RGB
    new_r, new_g, new_b = hsv_to_rgb(h, s, v)

    return (new_r, new_g, new_b, a)


# ============================================================================
# Procedural Texture Generation
# ============================================================================

def generate_procedural_unit_texture(
    unit_spec: UnitTextureSpec,
    palette: ColorPalette,
    size: Tuple[int, int] = (512, 512)
) -> Image.Image:
    """
    Generate a procedural texture for a unit with faction colors.

    Creates a base texture with:
    - Primary color as dominant background
    - Secondary accent stripes/panels for mechanical/armor detail
    - Gradient shading for depth
    - Optional geometric patterns based on unit type
    """
    width, height = size
    img = Image.new('RGBA', (width, height), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    # Base color (primary with slight variation)
    base_color = palette.primary
    accent_color = palette.secondary
    detail_color = palette.tertiary

    # Fill background with base color
    draw.rectangle([0, 0, width, height], fill=(*base_color, 255))

    # Add gradient overlay (top to bottom) for depth
    for y in range(height):
        ratio = y / height
        # Interpolate between primary and a darker version
        dark_primary = (
            max(0, int(base_color[0] * (1 - ratio * 0.3))),
            max(0, int(base_color[1] * (1 - ratio * 0.3))),
            max(0, int(base_color[2] * (1 - ratio * 0.3)))
        )
        alpha = int(255 * ratio * 0.15)  # Subtle overlay
        draw.line([(0, y), (width, y)], fill=(*dark_primary, alpha), width=1)

    # Add accent details based on unit type
    if unit_spec.is_vehicle:
        # Vehicles: horizontal accent stripes (armor panels)
        stripe_height = 24
        for y in range(0, height, stripe_height * 3):
            draw.rectangle([0, y + stripe_height, width, y + stripe_height + 8],
                         fill=(*accent_color, 200))

        # Central panel highlight
        panel_margin = width // 8
        draw.rectangle([panel_margin, height // 4, width - panel_margin, 3 * height // 4],
                      outline=(*detail_color, 150), width=4)

    else:
        # Infantry: vertical accent details (armor segments)
        segment_width = width // 6
        for x in range(segment_width, width, segment_width * 2):
            draw.rectangle([x, height // 6, x + segment_width // 2, 5 * height // 6],
                         fill=(*accent_color, 180))

        # Central torso highlight
        torso_margin = width // 4
        draw.ellipse([torso_margin, height // 4, width - torso_margin, 3 * height // 4],
                    outline=(*detail_color, 150), width=3)

    # Add tier-based detail (higher tier = more complexity)
    if unit_spec.tier >= 2:
        # Add geometric patterns
        corner_size = 32
        for corner in [(0, 0), (width - corner_size, 0),
                       (0, height - corner_size), (width - corner_size, height - corner_size)]:
            draw.rectangle([corner[0], corner[1],
                          corner[0] + corner_size, corner[1] + corner_size],
                         fill=(*accent_color, 100))

    if unit_spec.tier >= 3:
        # Elite units: add glowing center
        center_x, center_y = width // 2, height // 2
        radius = width // 6
        draw.ellipse([center_x - radius, center_y - radius,
                     center_x + radius, center_y + radius],
                    fill=(*detail_color, 120))

    # Convert to RGB and apply color transformations to boost faction identity
    img = img.convert('RGB')
    pixels = img.load()

    # Enhance colors via HSV transformation
    for y in range(height):
        for x in range(width):
            pixel = pixels[x, y]
            # Add alpha for transformation
            r, g, b = pixel
            # Convert to HSV and apply palette shift
            h, s, v = rgb_to_hsv((r, g, b))
            h = (h + (palette.hue_shift / 360.0)) % 1.0
            s = min(1.0, s * palette.saturation_multiplier * 0.7)  # Subtle saturation boost
            v = min(1.0, v * palette.value_multiplier * 0.9)
            new_r, new_g, new_b = hsv_to_rgb(h, s, v)
            pixels[x, y] = (new_r, new_g, new_b)

    # Convert back to RGBA
    img = img.convert('RGBA')
    return img


# ============================================================================
# Texture Generation Worker
# ============================================================================

def generate_unit_texture(
    unit_spec: UnitTextureSpec,
    output_dir: Path,
    logger: logging.Logger
) -> Tuple[str, bool]:
    """
    Generate texture for a single unit.

    Returns (unit_id, success)
    """
    try:
        palette = REPUBLIC_PALETTE if unit_spec.faction == "republic" else CIS_PALETTE
        output_path = output_dir / f"{unit_spec.faction}_{unit_spec.unit_id}_albedo.png"

        # Generate procedural texture
        img = generate_procedural_unit_texture(unit_spec, palette, (512, 512))

        # Ensure output directory exists
        os.makedirs(output_dir, exist_ok=True)

        # Save with PNG optimization
        img.save(output_path, "PNG", optimize=True)
        logger.debug(f"Generated: {output_path}")
        return (unit_spec.unit_id, True)

    except Exception as e:
        logger.error(f"Failed to generate texture for {unit_spec.unit_id}: {e}")
        return (unit_spec.unit_id, False)


def generate_unit_texture_worker(args):
    """Wrapper for multiprocessing pool."""
    unit_spec, output_dir, verbose = args
    level = logging.DEBUG if verbose else logging.WARNING
    logging.basicConfig(level=level, format="%(levelname)s: %(message)s")
    logger = logging.getLogger(__name__)
    return generate_unit_texture(unit_spec, output_dir, logger)


# ============================================================================
# Manifest Generation
# ============================================================================

@dataclass
class TextureManifestEntry:
    """Single entry in the unit texture manifest."""
    unit_id: str
    display_name: str
    faction: str
    unit_class: str
    tier: int
    is_vehicle: bool
    albedo_file: str
    color_palette: str
    generated_at: str


def generate_manifest(
    units: List[UnitTextureSpec],
    output_path: Path,
    logger: logging.Logger
) -> bool:
    """Generate UNIT_TEXTURE_MANIFEST.json listing all created textures."""
    try:
        entries = []
        for unit in units:
            palette_name = REPUBLIC_PALETTE.name if unit.faction == "republic" else CIS_PALETTE.name
            entry = TextureManifestEntry(
                unit_id=unit.unit_id,
                display_name=unit.display_name,
                faction=unit.faction,
                unit_class=unit.unit_class,
                tier=unit.tier,
                is_vehicle=unit.is_vehicle,
                albedo_file=f"{unit.faction}_{unit.unit_id}_albedo.png",
                color_palette=palette_name,
                generated_at="2026-03-12"
            )
            entries.append(asdict(entry))

        manifest = {
            "version": "1.0",
            "pack_id": "warfare-starwars",
            "unit_textures": entries,
            "statistics": {
                "total_units": len(units),
                "republic_count": len([u for u in units if u.faction == "republic"]),
                "cis_count": len([u for u in units if u.faction == "cis"]),
                "vehicle_count": len([u for u in units if u.is_vehicle]),
                "infantry_count": len([u for u in units if not u.is_vehicle]),
                "tier_distribution": {
                    "tier_1": len([u for u in units if u.tier == 1]),
                    "tier_2": len([u for u in units if u.tier == 2]),
                    "tier_3": len([u for u in units if u.tier == 3])
                }
            }
        }

        output_path.parent.mkdir(parents=True, exist_ok=True)
        with open(output_path, 'w') as f:
            json.dump(manifest, f, indent=2)

        logger.info(f"Manifest generated: {output_path}")
        return True
    except Exception as e:
        logger.error(f"Failed to generate manifest: {e}")
        return False


# ============================================================================
# Main Pipeline
# ============================================================================

def main():
    parser = argparse.ArgumentParser(
        description="DINOForge Star Wars Unit Texture Generation Pipeline",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  python generate_unit_textures.py --output assets/textures/units/
  python generate_unit_textures.py --faction republic --output assets/textures/units/
  python generate_unit_textures.py --parallel --output assets/textures/units/
  python generate_unit_textures.py --dry-run --verbose
        """
    )

    parser.add_argument(
        "--output",
        type=Path,
        default=Path("assets/textures/units"),
        help="Output directory for generated textures (default: assets/textures/units)"
    )
    parser.add_argument(
        "--faction",
        choices=["republic", "cis", "all"],
        default="all",
        help="Generate textures for specific faction (default: all)"
    )
    parser.add_argument(
        "--parallel",
        action="store_true",
        help="Use multiprocessing for parallel generation"
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Show what would be generated without creating files"
    )
    parser.add_argument(
        "-v", "--verbose",
        action="store_true",
        help="Verbose logging output"
    )

    args = parser.parse_args()

    # Setup logging
    level = logging.DEBUG if args.verbose else logging.INFO
    logging.basicConfig(
        level=level,
        format="%(asctime)s [%(levelname)s] %(message)s",
        datefmt="%Y-%m-%d %H:%M:%S"
    )
    logger = logging.getLogger(__name__)

    # Filter units by faction
    if args.faction == "republic":
        units_to_process = REPUBLIC_UNITS
    elif args.faction == "cis":
        units_to_process = CIS_UNITS
    else:
        units_to_process = ALL_UNITS

    logger.info(f"DINOForge Unit Texture Pipeline")
    logger.info(f"Processing {len(units_to_process)} units (faction: {args.faction})")

    if args.dry_run:
        logger.info("DRY RUN - No files will be created")
        for unit in units_to_process:
            palette = REPUBLIC_PALETTE if unit.faction == "republic" else CIS_PALETTE
            logger.info(f"  Would generate {unit.faction}_{unit.unit_id}_albedo.png "
                       f"({unit.display_name})")
        return

    # Process textures
    if args.parallel:
        logger.info(f"Using multiprocessing with {cpu_count()} workers")
        worker_args = [(unit, args.output, args.verbose) for unit in units_to_process]
        with Pool(cpu_count()) as pool:
            results = pool.map(generate_unit_texture_worker, worker_args)
        success_count = sum(1 for _, success in results if success)
        failed_units = [unit_id for unit_id, success in results if not success]
    else:
        success_count = 0
        failed_units = []
        for unit in units_to_process:
            unit_id, success = generate_unit_texture(unit, args.output, logger)
            if success:
                success_count += 1
            else:
                failed_units.append(unit_id)

    # Generate manifest
    manifest_path = args.output / "UNIT_TEXTURE_MANIFEST.json"
    generate_manifest(units_to_process, manifest_path, logger)

    # Summary
    logger.info("=" * 70)
    logger.info(f"Pipeline complete: {success_count}/{len(units_to_process)} textures generated")
    if failed_units:
        logger.warning(f"Failed units: {', '.join(failed_units)}")
    logger.info(f"Output directory: {args.output.absolute()}")
    logger.info(f"Manifest: {manifest_path.absolute()}")


if __name__ == "__main__":
    main()
