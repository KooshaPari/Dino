#!/usr/bin/env python3
"""
Blender FBX Batch Export Tool for DINOForge Star Wars Pack

This script automates FBX export for vanilla DINO buildings using Blender.
It handles texture application, faction-specific color variants, and metadata logging.

Usage:
    blender --background --python blender_batch_export.py -- \\
        --input source/kenney/sci-fi-rts/Models/FBX/structure-c.fbx \\
        --faction republic \\
        --building-id house_clone_quarters \\
        --output assets/meshes/buildings/rep_house_clone_quarters.fbx

Or via Blender as a script:
    blender --background -P blender_batch_export.py -- [args]
"""

import sys
import argparse
import json
import os
from pathlib import Path
from datetime import datetime

# Try to import Blender modules (only available when run within Blender)
try:
    import bpy
    import bmesh
    BLENDER_AVAILABLE = True
except ImportError:
    BLENDER_AVAILABLE = False
    print("Warning: Blender not available. Script can only validate config in standalone mode.")


class BlenderFBXExporter:
    """Manages FBX export workflow for DINOForge buildings."""

    # Faction color palettes (from TEXTURE_MANIFEST.json)
    FACTION_COLORS = {
        "republic": {
            "primary": (0.961, 0.961, 0.961),  # #F5F5F5 (pristine white)
            "secondary": (0.102, 0.227, 0.420),  # #1A3A6B (deep blue)
            "tertiary": (0.392, 0.627, 0.859),  # #64A0DC (accent blue)
            "metallic": 0.1,
            "roughness": 0.8,
        },
        "cis": {
            "primary": (0.267, 0.267, 0.267),  # #444444 (dark grey)
            "secondary": (0.702, 0.353, 0.0),  # #B35A00 (rust orange)
            "tertiary": (0.400, 0.200, 0.0),  # #663300 (dark brown)
            "metallic": 0.2,
            "roughness": 0.7,
        },
    }

    # Building type metadata
    BUILDING_METADATA = {
        "house_clone_quarters": {
            "display_name": "Clone Quarters Pod",
            "building_type": "residential",
            "kenney_source": "structure-c.fbx",
            "estimated_poly_count": 320,
        },
        "farm_hydroponic": {
            "display_name": "Hydroponic Farm",
            "building_type": "economy",
            "kenney_source": "structure-e.fbx",
            "estimated_poly_count": 280,
        },
        "command_center": {
            "display_name": "Command Center",
            "building_type": "command",
            "kenney_source": "structure-c.fbx",
            "estimated_poly_count": 400,
        },
        "barracks": {
            "display_name": "Training Facility",
            "building_type": "military",
            "kenney_source": "structure-b.fbx",
            "estimated_poly_count": 350,
        },
    }

    def __init__(self, input_path, faction, building_id, output_path, log_file=None):
        """
        Initialize exporter.

        Args:
            input_path (str): Path to source Kenney FBX file
            faction (str): Faction identifier ("republic" or "cis")
            building_id (str): DINOForge building ID
            output_path (str): Target FBX export path
            log_file (str): Optional path to write export log
        """
        self.input_path = Path(input_path)
        self.faction = faction
        self.building_id = building_id
        self.output_path = Path(output_path)
        self.log_file = Path(log_file) if log_file else None

        # Validate inputs
        self._validate_inputs()

        # Export metadata
        self.export_metadata = {
            "timestamp": datetime.now().isoformat(),
            "blender_version": bpy.app.version_string if BLENDER_AVAILABLE else "N/A",
            "input_file": str(self.input_path),
            "faction": faction,
            "building_id": building_id,
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

        # Validate building_id (non-strict; allows custom IDs)
        if not self.building_id or len(self.building_id) < 3:
            raise ValueError(f"Invalid building_id: '{self.building_id}'")

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
        obj.name = f"{self.faction}_{self.building_id}"

        # Get faction colors
        colors = self.FACTION_COLORS[self.faction]

        # Create or get material
        mat_name = f"{self.faction}_{self.building_id}_material"
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
        """Optimize mesh poly count."""
        if not BLENDER_AVAILABLE:
            return

        obj = bpy.context.active_object
        if not obj or obj.type != "MESH":
            self.export_metadata["warnings"].append("No mesh object to optimize")
            return

        # Get initial poly count
        initial_tris = len(obj.data.polygons)

        # Apply decimate if needed (target: < 400 tris)
        if initial_tris > 400:
            decimate = obj.modifiers.new(name="Decimate", type="DECIMATE")
            decimate.ratio = 0.8
            bpy.context.view_layer.objects.active = obj
            bpy.ops.object.modifier_apply(modifier=decimate.name)

            final_tris = len(obj.data.polygons)
            self.export_metadata["optimization"] = {
                "method": "decimate",
                "initial_triangles": initial_tris,
                "final_triangles": final_tris,
                "reduction_ratio": final_tris / initial_tris if initial_tris > 0 else 0,
            }
        else:
            self.export_metadata["optimization"] = {
                "method": "none",
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
            print(f"Exporting {self.faction}_{self.building_id}...")

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
        description="Blender FBX batch export tool for DINOForge buildings"
    )
    parser.add_argument(
        "--input",
        required=True,
        help="Path to source Kenney FBX file (e.g., source/kenney/sci-fi-rts/structure-c.fbx)",
    )
    parser.add_argument(
        "--faction", required=True, choices=["republic", "cis"], help="Target faction"
    )
    parser.add_argument(
        "--building-id", required=True, help="DINOForge building ID (e.g., house_clone_quarters)"
    )
    parser.add_argument(
        "--output",
        required=True,
        help="Target FBX output path (e.g., assets/meshes/buildings/rep_house_clone_quarters.fbx)",
    )
    parser.add_argument(
        "--log",
        default="EXPORT_LOG.txt",
        help="Path to export log file (default: EXPORT_LOG.txt)",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Validate config without running Blender operations",
    )

    # Handle Blender's argument parsing
    # Blender adds its own args; we need to extract ours
    if "--" in sys.argv:
        argv = sys.argv[sys.argv.index("--") + 1 :]
    else:
        argv = sys.argv[1:]

    return parser.parse_args(argv)


def main():
    """Main entry point."""
    try:
        args = parse_args()

        # Create exporter
        exporter = BlenderFBXExporter(
            input_path=args.input,
            faction=args.faction,
            building_id=args.building_id,
            output_path=args.output,
            log_file=args.log,
        )

        if args.dry_run:
            print("Dry run mode: validating configuration...")
            print(f"  Input: {args.input}")
            print(f"  Faction: {args.faction}")
            print(f"  Building ID: {args.building_id}")
            print(f"  Output: {args.output}")
            print("  [OK] Configuration valid")
        else:
            # Run export
            exporter.export()

    except Exception as e:
        print(f"Error: {str(e)}", file=sys.stderr)
        sys.exit(1)


# Suppress warning messages if imported
def __suppress_warnings():
    """Suppress encoding warnings in certain environments."""
    import warnings
    warnings.filterwarnings("ignore", category=UnicodeDecodeError)


if __name__ == "__main__":
    main()
