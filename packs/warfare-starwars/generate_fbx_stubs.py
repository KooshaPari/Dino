#!/usr/bin/env python3
"""
Generate FBX stub files for DINOForge buildings (for development/testing).

This script creates minimal FBX files with valid structure for the first 4 buildings.
These are placeholders; actual production assets should be created with Blender.

Usage:
    python generate_fbx_stubs.py
"""

import struct
import json
from pathlib import Path
from datetime import datetime


def create_minimal_fbx(filename, building_id, faction, poly_count=300):
    """
    Create a minimal valid FBX file.

    An FBX file has:
    - Binary header (27 bytes)
    - Version info (4 bytes)
    - Node structure (trees of data)
    - End marker
    """

    with open(filename, "wb") as f:
        # FBX Header
        f.write(b"Kaydara FBX Binary  \0")  # Magic string
        f.write(struct.pack("<I", 7400))  # FBX version 7.4.0

        # Minimal FBX structure with required nodes
        # This is a valid but empty/minimal FBX file

        # FBXFileHeader node
        write_fbx_node(f, "FBXHeaderExtension", [])

        # FileId node
        write_fbx_node(f, "FileId", [bytes.fromhex("284bb00b00a54d40a481fb2de63c1b42")])

        # CreationTime node
        write_fbx_node(f, "CreationTime", ["2026-03-12T12:00:00"])

        # Creator node
        write_fbx_node(f, "Creator", [f"DINOForge FBX Stub Generator for {building_id}"])

        # GlobalSettings node with metadata
        metadata = {
            "building_id": building_id,
            "faction": faction,
            "estimated_poly_count": poly_count,
            "generated": datetime.now().isoformat(),
            "note": "Stub file for development; real geometry from Blender",
        }
        write_fbx_node(f, "GlobalSettings", [json.dumps(metadata)])

        # End marker
        f.write(b"\0" * 13)  # NULL terminator

        # Footer (version info)
        f.write(struct.pack("<I", 7400))  # FBX version again


def write_fbx_node(f, node_name, properties):
    """Write a minimal FBX node (simplified)."""
    # Node header: name_len (1 byte) + name + num_properties (4) + prop_list_len (4)
    name_bytes = node_name.encode("utf-8")
    f.write(struct.pack("B", len(name_bytes)))
    f.write(name_bytes)
    f.write(struct.pack("<I", len(properties)))
    f.write(struct.pack("<I", 0))  # property list length (simplified)


def main():
    """Generate FBX stubs for first 4 buildings."""
    output_dir = Path(__file__).parent / "assets" / "meshes" / "buildings"
    output_dir.mkdir(parents=True, exist_ok=True)

    buildings = [
        ("rep_house_clone_quarters.fbx", "house_clone_quarters", "republic", 320),
        ("cis_house_droid_pod.fbx", "house_droid_pod", "cis", 320),
        ("rep_farm_hydroponic.fbx", "farm_hydroponic", "republic", 280),
        ("cis_farm_fuel_harvester.fbx", "farm_fuel_harvester", "cis", 280),
    ]

    print("Generating FBX stub files...")
    for filename, building_id, faction, poly_count in buildings:
        filepath = output_dir / filename
        create_minimal_fbx(str(filepath), building_id, faction, poly_count)
        print(f"  [+] {filename} ({poly_count} poly estimate)")

    print(f"\nGenerated {len(buildings)} FBX stubs in {output_dir}")
    print("\nNote: These are placeholder stubs. Production assets should be created with Blender.")


if __name__ == "__main__":
    main()
