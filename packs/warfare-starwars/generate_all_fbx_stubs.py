#!/usr/bin/env python3
"""
Generate all 24 FBX stub files for DINOForge Star Wars buildings.

Creates minimal but valid FBX files for all 24 buildings (12 archetypes × 2 factions).
These are development/testing placeholders; production should use real Blender exports.

The stubs are structured as:
- Valid FBX binary header
- Minimal node tree
- Metadata tags with building_id, faction, poly count

Usage:
    python generate_all_fbx_stubs.py

Output:
    assets/meshes/buildings/*.fbx (24 files total)

Author: DINOForge Agent
Date: 2026-03-12
"""

import struct
import json
from pathlib import Path
from datetime import datetime
import logging

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
)
logger = logging.getLogger(__name__)

# Buildings configuration
BUILDINGS = [
    # Residential
    ("house_clone_quarters", "Clone Quarters Pod", 320),
    ("house_droid_pod", "Droid Pod", 320),

    # Economy
    ("farm_hydroponic", "Hydroponic Farm", 280),
    ("farm_fuel_harvester", "Fuel Harvester", 280),
    ("farm_moisture_extractor", "Moisture Extractor", 280),
    ("farm_moisture_farm", "Moisture Farm", 280),

    # Storage/Mining
    ("granary_biodome", "Biodome Storage", 300),
    ("granary_ore_vault", "Ore Vault", 300),

    ("stone_mineral_processor", "Mineral Processor", 320),
    ("stone_crystal_mine", "Crystal Mine", 320),

    ("iron_metal_foundry", "Metal Foundry", 350),
    ("iron_forge_station", "Forge Station", 350),

    # Special
    ("soul_meditation_chamber", "Meditation Chamber", 290),
    ("soul_sith_altar", "Sith Altar", 290),

    # Military/Support
    ("builder_construction_bot_factory", "Construction Bot Factory", 380),
    ("builder_engineering_station", "Engineering Station", 380),

    ("guild_merchant_bazaar", "Merchant Bazaar", 310),
    ("guild_trade_hub", "Trade Hub", 310),

    # Defense
    ("gate_repulse_barrier", "Repulse Barrier Gate", 340),
    ("gate_shield_generator", "Shield Generator Gate", 340),

    ("hospital_medical_bay", "Medical Bay", 260),
    ("hospital_clone_bank", "Clone Bank", 260),
]

FACTIONS = ["republic", "cis"]


def create_minimal_fbx(filename: str, building_id: str, faction: str, poly_count: int):
    """
    Create a minimal valid FBX file.

    Structure:
    - FBX binary header (23 bytes: magic + version)
    - Minimal node tree
    - Metadata section
    - Terminator
    """
    with open(filename, "wb") as f:
        # === FBX Header ===
        f.write(b"Kaydara FBX Binary  \0")  # 23 bytes: magic string
        f.write(struct.pack("<I", 7400))  # FBX version 7.4.0

        # === Minimal FBX Node Structure ===
        # Each node: name_len(1) + name + property_count(4) + property_list_len(4) + properties

        # FBXHeaderExtension (empty properties)
        write_fbx_node(f, "FBXHeaderExtension", [], {})

        # FileId (UUID)
        write_fbx_node(
            f,
            "FileId",
            [("bin", bytes.fromhex("284bb00b00a54d40a481fb2de63c1b42"))],
            {}
        )

        # CreationTimeStamp
        write_fbx_node(
            f,
            "CreationTimeStamp",
            [("int", 1741819200)],  # 2026-03-12
            {}
        )

        # Creator
        creator_str = f"DINOForge Star Wars Pack | {building_id} ({faction})"
        write_fbx_node(f, "Creator", [("str", creator_str)], {})

        # GlobalSettings with metadata
        metadata = {
            "building_id": building_id,
            "faction": faction,
            "estimated_poly_count": poly_count,
            "generated": datetime.now().isoformat(),
            "stub_type": "development_placeholder",
            "note": "Replace with real Blender export",
        }
        write_fbx_node(
            f,
            "GlobalSettings",
            [("str", json.dumps(metadata))],
            {}
        )

        # === Terminator ===
        f.write(b"\0" * 13)

        # === Footer ===
        # FBX file ends with padding and version
        f.write(struct.pack("<I", 7400))
        f.write(b"\0" * 120)


def write_fbx_node(f, node_name: str, properties: list, child_nodes: dict):
    """
    Write a simplified FBX node.

    Args:
        f: file handle
        node_name: node identifier
        properties: list of (type, value) tuples
        child_nodes: dict of child nodes (simplified)
    """
    # Node header: name_len(1) + name + num_properties(4) + property_list_len(4)
    name_bytes = node_name.encode("utf-8")

    # Build property data
    prop_data = b""
    for prop_type, prop_value in properties:
        if prop_type == "bin":
            prop_data += prop_value
        elif prop_type == "str":
            if isinstance(prop_value, str):
                prop_value = prop_value.encode("utf-8")
            prop_data += struct.pack("<I", len(prop_value))
            prop_data += prop_value
        elif prop_type == "int":
            prop_data += struct.pack("<i", prop_value)

    # Write header
    f.write(struct.pack("B", len(name_bytes)))
    f.write(name_bytes)
    f.write(struct.pack("<I", len(properties)))
    f.write(struct.pack("<I", len(prop_data)))

    # Write properties
    f.write(prop_data)


def main():
    """Generate all FBX stub files."""
    output_dir = Path(__file__).parent / "assets" / "meshes" / "buildings"
    output_dir.mkdir(parents=True, exist_ok=True)

    logger.info("=" * 80)
    logger.info("DINOForge Building FBX Stub Generator")
    logger.info("=" * 80)
    logger.info(f"Output directory: {output_dir}")
    logger.info("")

    total = len(BUILDINGS) * len(FACTIONS)
    logger.info(f"Generating {total} stub files ({len(BUILDINGS)} buildings × {len(FACTIONS)} factions)...")
    logger.info("")

    count = 0
    for building_id, display_name, poly_count in BUILDINGS:
        for faction in FACTIONS:
            # Faction prefix: rep_ or cis_
            faction_prefix = "rep" if faction == "republic" else "cis"
            filename = f"{faction_prefix}_{building_id}.fbx"
            filepath = output_dir / filename

            create_minimal_fbx(
                str(filepath),
                building_id,
                faction,
                poly_count
            )

            count += 1
            logger.info(f"  [{count:2d}/{total}] {filename:<50} ({poly_count} tris)")

    logger.info("")
    logger.info("=" * 80)
    logger.info(f"Generated {count} stub files in {output_dir}")
    logger.info("")
    logger.info("IMPORTANT:")
    logger.info("  These are placeholder stubs for development and testing.")
    logger.info("  Production assets must be created with Blender using build_all_buildings.py")
    logger.info("")
    logger.info("Next steps:")
    logger.info("  1. Install Blender (if not already installed)")
    logger.info("  2. Run: python build_all_buildings.py --dry-run")
    logger.info("  3. Run: python build_all_buildings.py --parallel 4")
    logger.info("=" * 80)


if __name__ == "__main__":
    main()
