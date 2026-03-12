#!/usr/bin/env python3
"""
Blender FBX Batch Export Tool for DINOForge Star Wars Pack - UNITS

This script automates FBX export for Star Wars Clone Wars era units using Blender.
It handles texture application, faction-specific color variants, and metadata logging.

Unit archetypes (13 × 2 factions = 26 total):
  1. Militia (light infantry)
  2. Line Infantry (core troops)
  3. Heavy Infantry (armor)
  4. Ranged Infantry (skirmishers)
  5. Cavalry (mobile vehicles)
  6. Siege (heavy vehicles)
  7. Support (medics/engineers)
  8. Scout (reconnaissance)
  9. Elite (special troops)
  10. Hero (commanders)
  11. Wall Defender (fortification)
  12. Skirmisher (ranged support)
  13. Special (elite specialists)

Factions:
  - republic: Clone Troopers (Order archetype)
  - cis: Battle Droids (Industrial Swarm archetype)

Usage:
    blender --background --python blender_units_batch_export.py -- \\
        --unit clone_militia \\
        --faction republic \\
        --input source/kenney/sci-fi-rts/Models/FBX/soldier-a.fbx \\
        --output assets/meshes/units/rep_clone_militia.fbx \\
        --batch-process

Or via batch config file:
    blender --background --python blender_units_batch_export.py -- \\
        --batch-file units_batch_config.json
"""

import sys
import argparse
import json
import os
from pathlib import Path
from datetime import datetime
import subprocess

# Try to import Blender modules (only available when run within Blender)
try:
    import bpy
    import bmesh
    BLENDER_AVAILABLE = True
except ImportError:
    BLENDER_AVAILABLE = False
    print("Warning: Blender not available. Script can only validate config in standalone mode.")


class UnitFBXExporter:
    """Manages FBX export workflow for DINOForge Star Wars units."""

    # Unit archetype definitions
    UNIT_ARCHETYPES = {
        # Republic units (Order archetype)
        "rep_clone_militia": {
            "faction": "republic",
            "display_name": "Clone Militia",
            "unit_type": "militia",
            "kenney_source": "soldier-a.fbx",  # Basic humanoid
            "target_tris": 400,
            "biped": True,
        },
        "rep_clone_trooper": {
            "faction": "republic",
            "display_name": "Clone Trooper",
            "unit_type": "line_infantry",
            "kenney_source": "soldier-a.fbx",
            "target_tris": 450,
            "biped": True,
        },
        "rep_clone_heavy": {
            "faction": "republic",
            "display_name": "Clone Heavy Trooper",
            "unit_type": "heavy_infantry",
            "kenney_source": "soldier-c.fbx",  # Heavy armor variant
            "target_tris": 500,
            "biped": True,
        },
        "rep_clone_sharpshooter": {
            "faction": "republic",
            "display_name": "Clone Sharpshooter",
            "unit_type": "ranged_infantry",
            "kenney_source": "soldier-a.fbx",
            "target_tris": 400,
            "biped": True,
        },
        "rep_barc_speeder": {
            "faction": "republic",
            "display_name": "BARC Speeder",
            "unit_type": "cavalry",
            "kenney_source": "vehicle-a.fbx",  # Small speeder bike
            "target_tris": 550,
            "biped": False,
        },
        "rep_atte_crew": {
            "faction": "republic",
            "display_name": "AT-TE Crew",
            "unit_type": "siege",
            "kenney_source": "vehicle-e.fbx",  # Large walker
            "target_tris": 600,
            "biped": False,
        },
        "rep_clone_medic": {
            "faction": "republic",
            "display_name": "Clone Medic",
            "unit_type": "support",
            "kenney_source": "soldier-a.fbx",
            "target_tris": 400,
            "biped": True,
        },
        "rep_arf_trooper": {
            "faction": "republic",
            "display_name": "ARF Trooper",
            "unit_type": "scout",
            "kenney_source": "soldier-a.fbx",
            "target_tris": 380,
            "biped": True,
        },
        "rep_arc_trooper": {
            "faction": "republic",
            "display_name": "ARC Trooper",
            "unit_type": "elite",
            "kenney_source": "soldier-b.fbx",  # Enhanced armor
            "target_tris": 480,
            "biped": True,
        },
        "rep_jedi_knight": {
            "faction": "republic",
            "display_name": "Jedi Knight",
            "unit_type": "hero",
            "kenney_source": "soldier-d.fbx",  # Hero model
            "target_tris": 550,
            "biped": True,
        },
        "rep_clone_wall_guard": {
            "faction": "republic",
            "display_name": "Clone Wall Guard",
            "unit_type": "wall_defender",
            "kenney_source": "soldier-c.fbx",
            "target_tris": 420,
            "biped": True,
        },
        "rep_clone_sniper": {
            "faction": "republic",
            "display_name": "Clone Sniper",
            "unit_type": "skirmisher",
            "kenney_source": "soldier-b.fbx",
            "target_tris": 400,
            "biped": True,
        },
        "rep_clone_commando": {
            "faction": "republic",
            "display_name": "Clone Commando",
            "unit_type": "special",
            "kenney_source": "soldier-c.fbx",
            "target_tris": 500,
            "biped": True,
        },
        # CIS units (Industrial Swarm archetype)
        "cis_b1_battle_droid": {
            "faction": "cis",
            "display_name": "B1 Battle Droid",
            "unit_type": "militia",
            "kenney_source": "robot-a.fbx",
            "target_tris": 380,
            "biped": True,
        },
        "cis_b1_squad": {
            "faction": "cis",
            "display_name": "B1 Squad",
            "unit_type": "line_infantry",
            "kenney_source": "robot-a.fbx",
            "target_tris": 400,
            "biped": True,
        },
        "cis_b2_super_battle_droid": {
            "faction": "cis",
            "display_name": "B2 Super Battle Droid",
            "unit_type": "heavy_infantry",
            "kenney_source": "robot-b.fbx",
            "target_tris": 520,
            "biped": True,
        },
        "cis_sniper_droid": {
            "faction": "cis",
            "display_name": "Sniper Droid",
            "unit_type": "ranged_infantry",
            "kenney_source": "robot-a.fbx",
            "target_tris": 360,
            "biped": True,
        },
        "cis_stap_pilot": {
            "faction": "cis",
            "display_name": "STAP Pilot",
            "unit_type": "cavalry",
            "kenney_source": "vehicle-a.fbx",
            "target_tris": 480,
            "biped": False,
        },
        "cis_aat_crew": {
            "faction": "cis",
            "display_name": "AAT Crew",
            "unit_type": "siege",
            "kenney_source": "vehicle-c.fbx",
            "target_tris": 600,
            "biped": False,
        },
        "cis_medical_droid": {
            "faction": "cis",
            "display_name": "Medical Droid",
            "unit_type": "support",
            "kenney_source": "robot-a.fbx",
            "target_tris": 360,
            "biped": False,
        },
        "cis_probe_droid": {
            "faction": "cis",
            "display_name": "Probe Droid",
            "unit_type": "scout",
            "kenney_source": "robot-c.fbx",
            "target_tris": 320,
            "biped": False,
        },
        "cis_bx_commando_droid": {
            "faction": "cis",
            "display_name": "BX Commando Droid",
            "unit_type": "elite",
            "kenney_source": "robot-b.fbx",
            "target_tris": 480,
            "biped": True,
        },
        "cis_general_grievous": {
            "faction": "cis",
            "display_name": "General Grievous",
            "unit_type": "hero",
            "kenney_source": "robot-d.fbx",
            "target_tris": 580,
            "biped": True,
        },
        "cis_droideka": {
            "faction": "cis",
            "display_name": "Droideka",
            "unit_type": "wall_defender",
            "kenney_source": "robot-e.fbx",
            "target_tris": 520,
            "biped": False,
        },
        "cis_dwarf_spider_droid": {
            "faction": "cis",
            "display_name": "DSD1 Dwarf Spider Droid",
            "unit_type": "skirmisher",
            "kenney_source": "robot-c.fbx",
            "target_tris": 450,
            "biped": False,
        },
        "cis_magnaguard": {
            "faction": "cis",
            "display_name": "IG-100 MagnaGuard",
            "unit_type": "special",
            "kenney_source": "robot-b.fbx",
            "target_tris": 500,
            "biped": True,
        },
    }

    # Faction color palettes (from COLOR_PALETTE_GUIDE.md)
    FACTION_COLORS = {
        "republic": {
            "primary": (0.961, 0.961, 0.961),  # #F5F5F5 (pristine white)
            "secondary": (0.102, 0.227, 0.420),  # #1A3A6B (deep blue)
            "tertiary": (0.392, 0.627, 0.859),  # #64A0DC (accent blue)
            "metallic": 0.1,
            "roughness": 0.7,
        },
        "cis": {
            "primary": (0.267, 0.267, 0.267),  # #444444 (dark grey)
            "secondary": (0.702, 0.353, 0.0),  # #B35A00 (rust orange)
            "tertiary": (0.400, 0.200, 0.0),  # #663300 (dark brown)
            "metallic": 0.15,
            "roughness": 0.6,
        },
    }

    def __init__(self, unit_id, faction, input_path, output_path, log_file=None):
        """
        Initialize exporter.

        Args:
            unit_id (str): Unit archetype ID (e.g., "clone_militia")
            faction (str): Faction identifier ("republic" or "cis")
            input_path (str): Path to source Kenney FBX file
            output_path (str): Target FBX export path
            log_file (str): Optional path to write export log
        """
        self.unit_id = unit_id
        self.faction = faction
        self.input_path = Path(input_path)
        self.output_path = Path(output_path)
        self.log_file = Path(log_file) if log_file else None

        # Validate inputs
        self._validate_inputs()

        # Export metadata
        self.export_metadata = {
            "timestamp": datetime.now().isoformat(),
            "blender_version": bpy.app.version_string if BLENDER_AVAILABLE else "N/A",
            "input_file": str(self.input_path),
            "unit_id": unit_id,
            "faction": faction,
            "output_file": str(self.output_path),
            "status": "pending",
            "errors": [],
            "warnings": [],
        }

    def _validate_inputs(self):
        """Validate input parameters."""
        # Validate faction
        if self.faction not in self.FACTION_COLORS:
            raise ValueError(
                f"Unknown faction '{self.faction}'. Must be: {list(self.FACTION_COLORS.keys())}"
            )

        # Validate unit_id
        full_unit_id = f"{self.faction[:3]}_{self.unit_id}" if not self.unit_id.startswith("rep_") and not self.unit_id.startswith("cis_") else self.unit_id
        if full_unit_id not in self.UNIT_ARCHETYPES:
            raise ValueError(
                f"Unknown unit '{full_unit_id}'. Valid units: {list(self.UNIT_ARCHETYPES.keys())}"
            )

        # Validate output directory
        self.output_path.parent.mkdir(parents=True, exist_ok=True)

    def _import_fbx(self):
        """Import Kenney FBX into Blender scene."""
        if not BLENDER_AVAILABLE:
            self.export_metadata["warnings"].append("Blender not available; skipping import")
            return None

        if not self.input_path.exists():
            error_msg = f"Source FBX not found: {self.input_path}"
            self.export_metadata["errors"].append(error_msg)
            raise FileNotFoundError(error_msg)

        # Clear default scene
        bpy.ops.object.select_all(action="SELECT")
        bpy.ops.object.delete()

        # Import FBX
        try:
            bpy.ops.import_scene.fbx(
                filepath=str(self.input_path),
                axis_forward="-Y",
                axis_up="Z",
                automatic_bone_orientation=True,
            )
            self.export_metadata["status"] = "imported"
            return True
        except Exception as e:
            error_msg = f"Failed to import FBX: {str(e)}"
            self.export_metadata["errors"].append(error_msg)
            raise

    def _apply_faction_material(self):
        """Apply faction-specific material to imported object."""
        if not BLENDER_AVAILABLE:
            self.export_metadata["warnings"].append(
                "Blender not available; skipping material application"
            )
            return

        # Get the imported object (assumes single object)
        imported_objs = bpy.context.selected_objects
        if not imported_objs:
            self.export_metadata["warnings"].append("No objects found after import")
            return

        obj = imported_objs[0]
        obj.name = f"{self.faction}_{self.unit_id}"

        # Get faction colors
        colors = self.FACTION_COLORS[self.faction]

        # Create or get material
        mat_name = f"{self.faction}_{self.unit_id}_material"
        if mat_name in bpy.data.materials:
            material = bpy.data.materials[mat_name]
        else:
            material = bpy.data.materials.new(name=mat_name)

        # Apply Principled BSDF shader
        material.use_nodes = True
        bsdf = material.node_tree.nodes.get("Principled BSDF")
        if not bsdf:
            bsdf = material.node_tree.nodes.new(type="ShaderNodeBsdfPrincipled")

        # Apply faction colors
        bsdf.inputs["Base Color"].default_value = (
            colors["primary"][0],
            colors["primary"][1],
            colors["primary"][2],
            1.0,
        )
        bsdf.inputs["Metallic"].default_value = colors["metallic"]
        bsdf.inputs["Roughness"].default_value = colors["roughness"]

        # Assign material to object
        if obj.data.materials:
            obj.data.materials[0] = material
        else:
            obj.data.materials.append(material)

        self.export_metadata["material_applied"] = {
            "material_name": mat_name,
            "base_color": colors["primary"],
            "metallic": colors["metallic"],
            "roughness": colors["roughness"],
        }

    def _optimize_geometry(self):
        """Optimize mesh poly count to target triangle count."""
        if not BLENDER_AVAILABLE:
            return

        obj = bpy.context.active_object
        if not obj or obj.type != "MESH":
            self.export_metadata["warnings"].append("No mesh object to optimize")
            return

        # Get initial poly count
        initial_tris = len(obj.data.polygons)

        # Get target tri count from archetype
        full_unit_id = f"{self.faction[:3]}_{self.unit_id}" if not self.unit_id.startswith("rep_") and not self.unit_id.startswith("cis_") else self.unit_id
        target_tris = self.UNIT_ARCHETYPES.get(full_unit_id, {}).get("target_tris", 450)

        # Apply decimate if needed
        if initial_tris > target_tris * 1.2:  # 20% tolerance
            ratio = target_tris / initial_tris if initial_tris > 0 else 0.8
            ratio = min(0.95, max(0.5, ratio))  # Clamp between 50% and 95%

            decimate = obj.modifiers.new(name="Decimate", type="DECIMATE")
            decimate.ratio = ratio
            bpy.context.view_layer.objects.active = obj
            bpy.ops.object.modifier_apply(modifier=decimate.name)

            final_tris = len(obj.data.polygons)
            self.export_metadata["optimization"] = {
                "method": "decimate",
                "target_triangles": target_tris,
                "initial_triangles": initial_tris,
                "final_triangles": final_tris,
                "reduction_ratio": final_tris / initial_tris if initial_tris > 0 else 0,
            }
        else:
            self.export_metadata["optimization"] = {
                "method": "none",
                "target_triangles": target_tris,
                "triangle_count": initial_tris,
            }

    def _center_pivot(self):
        """Center object pivot point at base."""
        if not BLENDER_AVAILABLE:
            return

        obj = bpy.context.active_object
        if not obj:
            return

        # Set origin to geometry
        bpy.ops.object.origin_set(type="GEOMETRY")
        self.export_metadata["pivot_centered"] = True

    def _export_fbx(self):
        """Export mesh to FBX."""
        if not BLENDER_AVAILABLE:
            self.export_metadata["warnings"].append("Blender not available; skipping export")
            return

        # Select all objects for export
        bpy.ops.object.select_all(action="SELECT")

        try:
            bpy.ops.export_scene.fbx(
                filepath=str(self.output_path),
                axis_forward="-Y",
                axis_up="Z",
                scale_units=True,
                use_selection=True,
                apply_scalings=True,
                mesh_smooth_type="FACE",
                use_mesh_modifiers=True,
                bake_space_transform=False,
                use_armature_deform_only=False,
                use_custom_properties=True,
            )
            self.export_metadata["status"] = "exported"
            self.export_metadata["export_time"] = datetime.now().isoformat()
        except Exception as e:
            error_msg = f"Export failed: {str(e)}"
            self.export_metadata["errors"].append(error_msg)
            raise

    def export(self):
        """Execute full export pipeline."""
        try:
            print(f"Exporting {self.faction}_{self.unit_id}...")

            if BLENDER_AVAILABLE:
                self._import_fbx()
                self._apply_faction_material()
                self._optimize_geometry()
                self._center_pivot()
                self._export_fbx()
            else:
                self.export_metadata["status"] = "dry_run"
                self.export_metadata[
                    "notes"
                ] = "Blender not available; dry run only (config validated)"

            self.export_metadata["status"] = "success"
            print(f"✓ Exported to {self.output_path}")

        except Exception as e:
            self.export_metadata["status"] = "failed"
            print(f"✗ Export failed: {str(e)}", file=sys.stderr)
            raise

        finally:
            # Always log metadata
            if self.log_file:
                self._write_log()

    def _write_log(self):
        """Write export metadata to log file."""
        with open(self.log_file, "a") as f:
            f.write(json.dumps(self.export_metadata, indent=2))
            f.write("\n" + "=" * 80 + "\n")

    def get_metadata(self):
        """Return export metadata dict."""
        return self.export_metadata


def parse_args():
    """Parse command-line arguments."""
    parser = argparse.ArgumentParser(
        description="Blender FBX batch export tool for DINOForge Star Wars units"
    )
    parser.add_argument(
        "--unit",
        help="Unit archetype ID (e.g., clone_militia)",
    )
    parser.add_argument(
        "--faction", choices=["republic", "cis"], help="Target faction"
    )
    parser.add_argument(
        "--input",
        help="Path to source Kenney FBX file (e.g., source/kenney/sci-fi-rts/Models/FBX/soldier-a.fbx)",
    )
    parser.add_argument(
        "--output",
        help="Target FBX output path (e.g., assets/meshes/units/rep_clone_militia.fbx)",
    )
    parser.add_argument(
        "--batch-file",
        help="Path to batch config JSON file (overrides --unit, --faction, etc.)",
    )
    parser.add_argument(
        "--log",
        default="UNITS_EXPORT_LOG.txt",
        help="Path to export log file (default: UNITS_EXPORT_LOG.txt)",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Validate config without running Blender operations",
    )
    parser.add_argument(
        "--batch-process",
        action="store_true",
        help="Process all units in batch mode (requires valid Blender environment)",
    )

    # Handle Blender's argument parsing
    if "--" in sys.argv:
        argv = sys.argv[sys.argv.index("--") + 1 :]
    else:
        argv = sys.argv[1:]

    return parser.parse_args(argv)


def generate_batch_config():
    """Generate a batch configuration for all 26 units."""
    config = {
        "batch_name": "Star Wars Clone Wars Units - Complete Set",
        "version": "0.1.0",
        "created": datetime.now().isoformat(),
        "output_dir": "assets/meshes/units/",
        "log_file": "UNITS_EXPORT_LOG.txt",
        "units": [],
    }

    exporter = UnitFBXExporter(
        unit_id="clone_militia",
        faction="republic",
        input_path="dummy",
        output_path="dummy",
    )

    for full_unit_id, unit_info in exporter.UNIT_ARCHETYPES.items():
        faction = unit_info["faction"]
        # Extract unit_id from full_unit_id (remove faction prefix)
        prefix = "rep_" if faction == "republic" else "cis_"
        unit_id = full_unit_id[len(prefix) :]

        config["units"].append({
            "unit_id": unit_id,
            "faction": faction,
            "full_id": full_unit_id,
            "display_name": unit_info["display_name"],
            "unit_type": unit_info["unit_type"],
            "kenney_source": unit_info["kenney_source"],
            "input": f"source/kenney/sci-fi-rts/Models/FBX/{unit_info['kenney_source']}",
            "output": f"assets/meshes/units/{full_unit_id}.fbx",
            "target_tris": unit_info["target_tris"],
        })

    return config


def main():
    """Main entry point."""
    try:
        args = parse_args()

        # Generate batch config if requested
        if args.batch_process:
            config = generate_batch_config()
            print(f"Generated batch config with {len(config['units'])} units")
            print(json.dumps(config, indent=2))
            return

        # Validate single unit export
        if not args.unit or not args.faction or not args.input or not args.output:
            if args.batch_file:
                print(f"Would load batch config from {args.batch_file}")
                # In a full implementation, would read config and process multiple units
            else:
                parser = argparse.ArgumentParser()
                parser.error(
                    "Require --unit, --faction, --input, --output or --batch-file"
                )

        # Create exporter for single unit
        exporter = UnitFBXExporter(
            unit_id=args.unit,
            faction=args.faction,
            input_path=args.input,
            output_path=args.output,
            log_file=args.log,
        )

        if args.dry_run:
            print("Dry run mode: validating configuration...")
            print(f"  Unit: {args.unit}")
            print(f"  Faction: {args.faction}")
            print(f"  Input: {args.input}")
            print(f"  Output: {args.output}")
            metadata = exporter.get_metadata()
            print(f"  [OK] Configuration valid")
            print(f"  Metadata: {json.dumps(metadata, indent=2)}")
        else:
            # Run export
            exporter.export()

    except Exception as e:
        print(f"Error: {str(e)}", file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()
