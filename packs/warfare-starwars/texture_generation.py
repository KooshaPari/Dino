#!/usr/bin/env python3
"""
DINOForge Star Wars Pack - Texture Generation Pipeline
======================================================

Generates faction-specific color variants for all 24 vanilla DINO buildings.
Uses HSV-based color replacement to transform Kenney base textures into
Republic (white/blue) and CIS (dark/orange) variants.

Color Transformation Pipeline:
  1. Load base albedo texture
  2. Convert RGB to HSV
  3. Apply hue/saturation/value shifts based on target palette
  4. Convert back to RGB
  5. Export as PNG with alpha preservation

Building Mapping:
  - 12 Republic buildings: command, 3x barracks, 2x defense, 2x economy, research, wall
  - 12 CIS buildings: command, 3x barracks, 2x defense, 2x economy, research, barrier
"""

import os
import json
import argparse
import logging
from pathlib import Path
from typing import Dict, List, Tuple
from dataclasses import dataclass, asdict

try:
    from PIL import Image
    import colorsys
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
# Building Registry
# ============================================================================

@dataclass
class BuildingTextureSpec:
    """Texture specification for a single building."""
    building_id: str
    display_name: str
    base_texture: str                  # Source Kenney texture filename
    albedo_output: str                 # Output albedo filename
    normal_output: str                 # Output normal map filename (if separate)
    faction: str                       # 'republic' or 'cis'
    building_type: str                 # command, barracks, defense, economy, research, wall/barrier

# Republic Buildings (12 total)
REPUBLIC_BUILDINGS = [
    BuildingTextureSpec(
        building_id="rep_command_center",
        display_name="Republic Command Center",
        base_texture="kenney_structure_c_albedo.png",
        albedo_output="rep_command_center_albedo.png",
        normal_output="rep_command_center_normal.png",
        faction="republic",
        building_type="command"
    ),
    BuildingTextureSpec(
        building_id="rep_clone_facility",
        display_name="Clone Training Facility",
        base_texture="kenney_structure_b_albedo.png",
        albedo_output="rep_clone_facility_albedo.png",
        normal_output="rep_clone_facility_normal.png",
        faction="republic",
        building_type="barracks"
    ),
    BuildingTextureSpec(
        building_id="rep_weapons_factory",
        display_name="Weapons Factory",
        base_texture="kenney_structure_d_albedo.png",
        albedo_output="rep_weapons_factory_albedo.png",
        normal_output="rep_weapons_factory_normal.png",
        faction="republic",
        building_type="barracks"
    ),
    BuildingTextureSpec(
        building_id="rep_vehicle_bay",
        display_name="Vehicle Bay",
        base_texture="kenney_structure_e_albedo.png",
        albedo_output="rep_vehicle_bay_albedo.png",
        normal_output="rep_vehicle_bay_normal.png",
        faction="republic",
        building_type="barracks"
    ),
    BuildingTextureSpec(
        building_id="rep_guard_tower",
        display_name="Guard Tower",
        base_texture="kenney_tower_a_albedo.png",
        albedo_output="rep_guard_tower_albedo.png",
        normal_output="rep_guard_tower_normal.png",
        faction="republic",
        building_type="defense"
    ),
    BuildingTextureSpec(
        building_id="rep_shield_generator",
        display_name="Shield Generator",
        base_texture="kenney_structure_f_albedo.png",
        albedo_output="rep_shield_generator_albedo.png",
        normal_output="rep_shield_generator_normal.png",
        faction="republic",
        building_type="defense"
    ),
    BuildingTextureSpec(
        building_id="rep_supply_station",
        display_name="Supply Station",
        base_texture="kenney_structure_a_albedo.png",
        albedo_output="rep_supply_station_albedo.png",
        normal_output="rep_supply_station_normal.png",
        faction="republic",
        building_type="economy"
    ),
    BuildingTextureSpec(
        building_id="rep_tibanna_refinery",
        display_name="Tibanna Gas Refinery",
        base_texture="kenney_structure_g_albedo.png",
        albedo_output="rep_tibanna_refinery_albedo.png",
        normal_output="rep_tibanna_refinery_normal.png",
        faction="republic",
        building_type="economy"
    ),
    BuildingTextureSpec(
        building_id="rep_research_lab",
        display_name="Research Laboratory",
        base_texture="kenney_structure_h_albedo.png",
        albedo_output="rep_research_lab_albedo.png",
        normal_output="rep_research_lab_normal.png",
        faction="republic",
        building_type="research"
    ),
    BuildingTextureSpec(
        building_id="rep_blast_wall",
        display_name="Blast Wall",
        base_texture="kenney_wall_segment_albedo.png",
        albedo_output="rep_blast_wall_albedo.png",
        normal_output="rep_blast_wall_normal.png",
        faction="republic",
        building_type="wall"
    ),
]

# CIS Buildings (12 total)
CIS_BUILDINGS = [
    BuildingTextureSpec(
        building_id="cis_tactical_center",
        display_name="Tactical Droid Center",
        base_texture="kenney_structure_c_albedo.png",
        albedo_output="cis_tactical_center_albedo.png",
        normal_output="cis_tactical_center_normal.png",
        faction="cis",
        building_type="command"
    ),
    BuildingTextureSpec(
        building_id="cis_droid_factory",
        display_name="Droid Factory",
        base_texture="kenney_structure_b_albedo.png",
        albedo_output="cis_droid_factory_albedo.png",
        normal_output="cis_droid_factory_normal.png",
        faction="cis",
        building_type="barracks"
    ),
    BuildingTextureSpec(
        building_id="cis_assembly_line",
        display_name="Advanced Assembly Line",
        base_texture="kenney_structure_d_albedo.png",
        albedo_output="cis_assembly_line_albedo.png",
        normal_output="cis_assembly_line_normal.png",
        faction="cis",
        building_type="barracks"
    ),
    BuildingTextureSpec(
        building_id="cis_heavy_foundry",
        display_name="Heavy Foundry",
        base_texture="kenney_structure_e_albedo.png",
        albedo_output="cis_heavy_foundry_albedo.png",
        normal_output="cis_heavy_foundry_normal.png",
        faction="cis",
        building_type="barracks"
    ),
    BuildingTextureSpec(
        building_id="cis_sentry_turret",
        display_name="Sentry Turret",
        base_texture="kenney_tower_a_albedo.png",
        albedo_output="cis_sentry_turret_albedo.png",
        normal_output="cis_sentry_turret_normal.png",
        faction="cis",
        building_type="defense"
    ),
    BuildingTextureSpec(
        building_id="cis_ray_shield",
        display_name="Ray Shield Generator",
        base_texture="kenney_structure_f_albedo.png",
        albedo_output="cis_ray_shield_albedo.png",
        normal_output="cis_ray_shield_normal.png",
        faction="cis",
        building_type="defense"
    ),
    BuildingTextureSpec(
        building_id="cis_mining_facility",
        display_name="Mining Facility",
        base_texture="kenney_structure_a_albedo.png",
        albedo_output="cis_mining_facility_albedo.png",
        normal_output="cis_mining_facility_normal.png",
        faction="cis",
        building_type="economy"
    ),
    BuildingTextureSpec(
        building_id="cis_processing_plant",
        display_name="Processing Plant",
        base_texture="kenney_structure_g_albedo.png",
        albedo_output="cis_processing_plant_albedo.png",
        normal_output="cis_processing_plant_normal.png",
        faction="cis",
        building_type="economy"
    ),
    BuildingTextureSpec(
        building_id="cis_tech_union_lab",
        display_name="Techno Union Lab",
        base_texture="kenney_structure_h_albedo.png",
        albedo_output="cis_tech_union_lab_albedo.png",
        normal_output="cis_tech_union_lab_normal.png",
        faction="cis",
        building_type="research"
    ),
    BuildingTextureSpec(
        building_id="cis_durasteel_barrier",
        display_name="Durasteel Barrier",
        base_texture="kenney_wall_segment_albedo.png",
        albedo_output="cis_durasteel_barrier_albedo.png",
        normal_output="cis_durasteel_barrier_normal.png",
        faction="cis",
        building_type="barrier"
    ),
]

ALL_BUILDINGS = REPUBLIC_BUILDINGS + CIS_BUILDINGS

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

def transform_texture(
    input_path: str,
    output_path: str,
    palette: ColorPalette,
    logger: logging.Logger
) -> bool:
    """
    Transform a texture from base Kenney colors to faction-specific colors.

    Returns True if successful, False otherwise.
    """
    try:
        # Load image
        if not os.path.exists(input_path):
            logger.warning(f"Source texture not found: {input_path}")
            return False

        img = Image.open(input_path).convert("RGBA")
        logger.debug(f"Loaded: {input_path} ({img.size})")

        # Apply transformation to each pixel
        pixels = img.load()
        width, height = img.size

        for y in range(height):
            for x in range(width):
                pixel = pixels[x, y]
                pixels[x, y] = apply_color_transformation(pixel, palette)

        # Ensure output directory exists
        os.makedirs(os.path.dirname(output_path), exist_ok=True)

        # Save with PNG optimization
        img.save(output_path, "PNG", optimize=True)
        logger.info(f"Generated: {output_path}")
        return True

    except Exception as e:
        logger.error(f"Failed to transform {input_path}: {e}")
        return False

# ============================================================================
# Manifest Generation
# ============================================================================

@dataclass
class TextureManifestEntry:
    """Single entry in the texture manifest."""
    building_id: str
    display_name: str
    faction: str
    building_type: str
    albedo_file: str
    normal_file: str
    base_texture_source: str
    color_palette: str
    generated_at: str

def generate_manifest(
    buildings: List[BuildingTextureSpec],
    output_path: str,
    logger: logging.Logger
) -> bool:
    """Generate TEXTURE_MANIFEST.json listing all created variants."""
    try:
        entries = []
        for building in buildings:
            palette_name = REPUBLIC_PALETTE.name if building.faction == "republic" else CIS_PALETTE.name
            entry = TextureManifestEntry(
                building_id=building.building_id,
                display_name=building.display_name,
                faction=building.faction,
                building_type=building.building_type,
                albedo_file=building.albedo_output,
                normal_file=building.normal_output,
                base_texture_source=building.base_texture,
                color_palette=palette_name,
                generated_at="2026-03-12"
            )
            entries.append(asdict(entry))

        manifest = {
            "version": "1.0",
            "pack_id": "warfare-starwars",
            "building_variants": entries,
            "statistics": {
                "total_buildings": len(buildings),
                "republic_count": len([b for b in buildings if b.faction == "republic"]),
                "cis_count": len([b for b in buildings if b.faction == "cis"]),
                "building_types": {
                    "command": len([b for b in buildings if b.building_type == "command"]),
                    "barracks": len([b for b in buildings if b.building_type == "barracks"]),
                    "defense": len([b for b in buildings if b.building_type in ("defense", "barrier")]),
                    "economy": len([b for b in buildings if b.building_type == "economy"]),
                    "research": len([b for b in buildings if b.building_type == "research"]),
                    "wall": len([b for b in buildings if b.building_type in ("wall", "barrier")])
                }
            }
        }

        os.makedirs(os.path.dirname(output_path), exist_ok=True)
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
        description="DINOForge Star Wars Texture Generation Pipeline",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  python texture_generation.py --source kenney/ --output assets/textures/buildings/
  python texture_generation.py --faction republic --output assets/textures/buildings/
  python texture_generation.py --dry-run
        """
    )

    parser.add_argument(
        "--source",
        default="source/kenney",
        help="Path to Kenney source textures (default: source/kenney)"
    )
    parser.add_argument(
        "--output",
        default="assets/textures/buildings",
        help="Output directory for generated textures (default: assets/textures/buildings)"
    )
    parser.add_argument(
        "--faction",
        choices=["republic", "cis", "all"],
        default="all",
        help="Generate textures for specific faction (default: all)"
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

    # Filter buildings by faction
    if args.faction == "republic":
        buildings_to_process = REPUBLIC_BUILDINGS
    elif args.faction == "cis":
        buildings_to_process = CIS_BUILDINGS
    else:
        buildings_to_process = ALL_BUILDINGS

    logger.info(f"DINOForge Texture Pipeline")
    logger.info(f"Processing {len(buildings_to_process)} buildings (faction: {args.faction})")

    if args.dry_run:
        logger.info("DRY RUN - No files will be created")
        for building in buildings_to_process:
            palette = REPUBLIC_PALETTE if building.faction == "republic" else CIS_PALETTE
            logger.info(f"  Would transform {building.building_id} using {palette.name} palette")
        return

    # Process textures
    success_count = 0
    failed_buildings = []

    for building in buildings_to_process:
        palette = REPUBLIC_PALETTE if building.faction == "republic" else CIS_PALETTE
        source_path = os.path.join(args.source, building.base_texture)
        output_path = os.path.join(args.output, building.albedo_output)

        logger.info(f"Processing {building.building_id}...")
        if transform_texture(source_path, output_path, palette, logger):
            success_count += 1
        else:
            failed_buildings.append(building.building_id)

    # Generate manifest
    manifest_path = os.path.join(args.output, "TEXTURE_MANIFEST.json")
    generate_manifest(buildings_to_process, manifest_path, logger)

    # Summary
    logger.info("=" * 60)
    logger.info(f"Pipeline complete: {success_count}/{len(buildings_to_process)} textures generated")
    if failed_buildings:
        logger.warning(f"Failed buildings: {', '.join(failed_buildings)}")
    logger.info(f"Output: {os.path.abspath(args.output)}")

if __name__ == "__main__":
    main()
