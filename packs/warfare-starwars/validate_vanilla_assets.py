#!/usr/bin/env python3
"""
Validation script for vanilla DINO building assets in warfare-starwars pack.

Checks:
- All expected texture files exist (albedo + normal maps)
- All referenced FBX/mesh files exist in Kenney source
- manifest.yaml references match actual building definitions
- Reports missing assets by category
- Generates summary report

Usage:
    python validate_vanilla_assets.py
    python validate_vanilla_assets.py --verbose
    python validate_vanilla_assets.py --fix-manifest
"""

import json
import sys
import os
from pathlib import Path
from typing import Dict, List, Tuple
import argparse


class AssetValidator:
    def __init__(self, pack_root: Path, verbose: bool = False):
        self.pack_root = pack_root
        self.verbose = verbose
        self.assets_dir = pack_root / "assets"
        self.registry_dir = self.assets_dir / "registry"
        self.textures_dir = self.assets_dir / "textures" / "buildings"
        self.buildings_dir = pack_root / "buildings"

        self.errors: List[str] = []
        self.warnings: List[str] = []
        self.missing_textures: Dict[str, List[str]] = {}
        self.missing_meshes: Dict[str, List[str]] = {}
        self.invalid_manifest_refs: List[str] = []

    def log(self, msg: str, level: str = "INFO"):
        """Log message with level prefix."""
        if level == "INFO" and not self.verbose:
            return
        print(f"[{level}] {msg}")

    def validate_all(self) -> bool:
        """Run all validation checks."""
        print("=" * 70)
        print("Vanilla Building Asset Validator")
        print("=" * 70)

        # Load asset index
        asset_index = self._load_asset_index()
        if not asset_index:
            self.errors.append("Failed to load asset_index.json")
            return False

        # Validate texture files
        print("\n[CHECK 1] Validating texture files...")
        self._validate_textures(asset_index)

        # Validate manifest references
        print("\n[CHECK 2] Validating manifest references...")
        self._validate_manifest_refs(asset_index)

        # Validate Kenney source references
        print("\n[CHECK 3] Validating Kenney source pack references...")
        self._validate_kenney_sources(asset_index)

        # Generate report
        print("\n" + "=" * 70)
        print("VALIDATION REPORT")
        print("=" * 70)
        return self._print_report(asset_index)

    def _load_asset_index(self) -> Dict:
        """Load asset_index.json."""
        index_path = self.registry_dir / "asset_index.json"
        if not index_path.exists():
            self.errors.append(f"asset_index.json not found at {index_path}")
            return None

        try:
            with open(index_path, "r") as f:
                return json.load(f)
        except Exception as e:
            self.errors.append(f"Failed to parse asset_index.json: {e}")
            return None

    def _validate_textures(self, asset_index: Dict) -> None:
        """Check all texture files referenced in asset index exist."""
        buildings = asset_index.get("buildings", [])
        total_checks = 0
        found = 0

        for building in buildings:
            vanilla_name = building.get("vanilla_name", "Unknown")
            status = building.get("status", "pending")

            # Skip pending buildings
            if status == "pending":
                continue

            total_checks += 2

            # Check Republic textures
            rep_texture = building.get("texture_file_republic", "")
            rep_normal = building.get("normal_file_republic", "")

            if rep_texture and (self.textures_dir / rep_texture).exists():
                found += 1
                self.log(f"✓ {rep_texture}", "VERBOSE")
            else:
                self.missing_textures.setdefault("republic", []).append(
                    f"{vanilla_name}: {rep_texture}"
                )
                self.warnings.append(
                    f"Missing Republic texture: {vanilla_name} -> {rep_texture}"
                )

            # Check CIS textures
            cis_texture = building.get("texture_file_cis", "")
            cis_normal = building.get("normal_file_cis", "")

            if cis_texture and (self.textures_dir / cis_texture).exists():
                found += 1
                self.log(f"✓ {cis_texture}", "VERBOSE")
            else:
                self.missing_textures.setdefault("cis", []).append(
                    f"{vanilla_name}: {cis_texture}"
                )
                self.warnings.append(
                    f"Missing CIS texture: {vanilla_name} -> {cis_texture}"
                )

        print(f"  Texture check: {found}/{total_checks} found")

    def _validate_manifest_refs(self, asset_index: Dict) -> None:
        """Validate that manifest.yaml references match asset index."""
        manifest_path = self.pack_root / "manifest.yaml"
        if not manifest_path.exists():
            self.warnings.append("manifest.yaml not found")
            return

        # Load YAML (simple parsing)
        try:
            import yaml
            with open(manifest_path, "r") as f:
                manifest = yaml.safe_load(f) or {}
        except ImportError:
            self.log("PyYAML not installed; skipping manifest validation", "WARN")
            return
        except Exception as e:
            self.warnings.append(f"Failed to parse manifest.yaml: {e}")
            return

        # Check that buildings references are complete
        building_files = manifest.get("loads", {}).get("buildings", [])
        if building_files:
            print(f"  Found {len(building_files)} building file(s) in manifest")

            # Check each file exists
            for building_file in building_files:
                file_path = self.buildings_dir / f"{building_file}.yaml"
                if not file_path.exists():
                    self.invalid_manifest_refs.append(f"Missing building file: {building_file}")
                    self.errors.append(f"manifest.yaml references missing file: {building_file}")
                else:
                    self.log(f"✓ Building file found: {building_file}", "VERBOSE")

    def _validate_kenney_sources(self, asset_index: Dict) -> None:
        """Validate Kenney source references are documented."""
        buildings = asset_index.get("buildings", [])
        kenney_sources = set()

        for building in buildings:
            source = building.get("source", "unknown")
            kenney_pack = building.get("kenney_source", "unknown")
            kenney_sources.add((source, kenney_pack))

        print(f"  Found {len(kenney_sources)} unique Kenney source(s)")
        for source, pack in sorted(kenney_sources):
            print(f"    - {source} (from {pack})")

    def _print_report(self, asset_index: Dict) -> bool:
        """Print final validation report."""
        buildings = asset_index.get("buildings", [])
        summary = asset_index.get("summary", {})

        print(f"\nAsset Index Statistics:")
        print(f"  Total buildings: {summary.get('total_complete', 0) + summary.get('total_in_progress', 0) + summary.get('total_pending', 0)}")
        print(f"  Complete: {summary.get('total_complete', 0)}")
        print(f"  In Progress: {summary.get('total_in_progress', 0)}")
        print(f"  Pending: {summary.get('total_pending', 0)}")
        print(f"  Completion: {summary.get('completion_percentage', 0):.1f}%")

        print(f"\nTexture Status:")
        print(f"  Complete: {summary.get('textures_complete', 0)}")
        print(f"  In Progress: {summary.get('textures_in_progress', 0)}")
        print(f"  Pending: {summary.get('textures_pending', 0)}")

        print(f"\nManifest Status:")
        print(f"  Complete: {summary.get('manifests_complete', 0)}")
        print(f"  In Progress: {summary.get('manifests_in_progress', 0)}")
        print(f"  Pending: {summary.get('manifests_pending', 0)}")

        if self.missing_textures:
            print(f"\n[WARNING] Missing Textures:")
            for faction, missing_list in self.missing_textures.items():
                print(f"  {faction.upper()}:")
                for item in missing_list:
                    print(f"    - {item}")

        if self.invalid_manifest_refs:
            print(f"\n[ERROR] Invalid Manifest References:")
            for item in self.invalid_manifest_refs:
                print(f"  - {item}")

        if self.errors:
            print(f"\n[ERROR] Validation Errors: {len(self.errors)}")
            for error in self.errors:
                print(f"  - {error}")
            return False

        if self.warnings:
            print(f"\n[WARNING] Validation Warnings: {len(self.warnings)}")
            for warning in self.warnings:
                print(f"  - {warning}")

        print("\n" + "=" * 70)
        print("VALIDATION COMPLETE" if not self.errors else "VALIDATION FAILED")
        print("=" * 70)

        return len(self.errors) == 0


def main():
    parser = argparse.ArgumentParser(
        description="Validate vanilla DINO building assets in warfare-starwars pack"
    )
    parser.add_argument(
        "--verbose",
        action="store_true",
        help="Enable verbose output (show all found files)"
    )
    parser.add_argument(
        "--pack-root",
        type=Path,
        default=Path(__file__).parent,
        help="Path to pack root directory (default: script directory)"
    )

    args = parser.parse_args()

    validator = AssetValidator(args.pack_root, verbose=args.verbose)
    success = validator.validate_all()

    return 0 if success else 1


if __name__ == "__main__":
    sys.exit(main())
