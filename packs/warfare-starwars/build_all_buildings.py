#!/usr/bin/env python3
"""
Batch FBX Export Script for DINOForge Star Wars Buildings (All 24)

Generates all 24 building meshes (4 already done, 20 remaining) with:
- Parallel batch processing using multiprocessing
- Faction-specific color variants (Republic + CIS)
- Kenney asset source mapping
- Triangle count optimization (<400 tris target)
- Export metadata logging

Priority buildings:
  house (2 variants) + farm + granary + hospital + stone + iron + soul +
  builder + guild + gate variants (both factions each = 2 versions)

Sources:
  - Kenney sci-fi-rts (structure-a through structure-h)
  - Kenney space-kit
  - Kenney modular-space-kit

Usage:
    python build_all_buildings.py [--dry-run] [--parallel N] [--blender-path PATH]

Usage with Blender (real export):
    blender --background --python blender_batch_export.py -- \\
        --input source/kenney/sci-fi-rts/Models/FBX/structure-c.fbx \\
        --faction republic \\
        --building-id house_clone_quarters \\
        --output assets/meshes/buildings/rep_house_clone_quarters.fbx

Author: DINOForge Agent
Date: 2026-03-12
"""

import json
import subprocess
import argparse
import sys
from pathlib import Path
from typing import List, Dict, Tuple
from multiprocessing import Pool, cpu_count
from datetime import datetime
import logging

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
    handlers=[
        logging.FileHandler("BUILD_ALL_BUILDINGS.log"),
        logging.StreamHandler(),
    ],
)
logger = logging.getLogger(__name__)

# ============================================================================
# BUILDING CONFIGURATION
# ============================================================================

# Kenney source file mappings (structures a-h available)
KENNEY_STRUCTURES = {
    "structure_a": "source/kenney/sci-fi-rts/Models/FBX/structure-a.fbx",
    "structure_b": "source/kenney/sci-fi-rts/Models/FBX/structure-b.fbx",
    "structure_c": "source/kenney/sci-fi-rts/Models/FBX/structure-c.fbx",
    "structure_d": "source/kenney/sci-fi-rts/Models/FBX/structure-d.fbx",
    "structure_e": "source/kenney/sci-fi-rts/Models/FBX/structure-e.fbx",
    "structure_f": "source/kenney/sci-fi-rts/Models/FBX/structure-f.fbx",
    "structure_g": "source/kenney/sci-fi-rts/Models/FBX/structure-g.fbx",
    "structure_h": "source/kenney/sci-fi-rts/Models/FBX/structure-h.fbx",
}

# All 24 buildings (12 archetypes × 2 factions)
# Format: (building_id, display_name, kenney_source_key, estimated_tris)
BUILDINGS = [
    # Already done (4 buildings) - SKIP
    # ("house_clone_quarters", "Clone Quarters Pod", "structure_c", 320),
    # ("house_droid_pod", "Droid Pod", "structure_c", 320),
    # ("farm_hydroponic", "Hydroponic Farm", "structure_e", 280),
    # ("farm_fuel_harvester", "Fuel Harvester", "structure_e", 280),

    # Remaining 20 buildings (10 archetypes × 2 factions)
    # === RESIDENTIAL (HOUSE) ===
    # house_clone_quarters and house_droid_pod already done

    # === ECONOMY (FARMS/HARVESTING) ===
    ("farm_moisture_extractor", "Moisture Extractor", "structure_e", 280),  # CIS variant
    ("farm_moisture_farm", "Moisture Farm", "structure_e", 280),  # Republic variant

    # === RESOURCES (STORAGE/MINING) ===
    ("granary_biodome", "Biodome Storage", "structure_d", 300),  # Republic variant
    ("granary_ore_vault", "Ore Vault", "structure_d", 300),  # CIS variant

    ("stone_mineral_processor", "Mineral Processor", "structure_f", 320),  # CIS variant
    ("stone_crystal_mine", "Crystal Mine", "structure_f", 320),  # Republic variant

    ("iron_metal_foundry", "Metal Foundry", "structure_g", 350),  # CIS variant
    ("iron_forge_station", "Forge Station", "structure_g", 350),  # Republic variant

    # === SPECIAL RESOURCES ===
    ("soul_meditation_chamber", "Meditation Chamber", "structure_b", 290),  # Republic (Jedi)
    ("soul_sith_altar", "Sith Altar", "structure_b", 290),  # CIS (Sith)

    # === MILITARY/SUPPORT ===
    ("builder_construction_bot_factory", "Construction Bot Factory", "structure_h", 380),  # CIS
    ("builder_engineering_station", "Engineering Station", "structure_h", 380),  # Republic

    ("guild_merchant_bazaar", "Merchant Bazaar", "structure_a", 310),  # Republic
    ("guild_trade_hub", "Trade Hub", "structure_a", 310),  # CIS

    # === DEFENSE (GATES) ===
    ("gate_repulse_barrier", "Repulse Barrier Gate", "structure_c", 340),  # CIS
    ("gate_shield_generator", "Shield Generator Gate", "structure_c", 340),  # Republic

    ("hospital_medical_bay", "Medical Bay", "structure_d", 260),  # Republic
    ("hospital_clone_bank", "Clone Bank", "structure_d", 260),  # CIS alternate
]

# Factions
FACTIONS = ["republic", "cis"]

# Pack directory (relative paths from within pack dir)
PACK_ROOT = Path(__file__).parent
ASSETS_DIR = PACK_ROOT / "assets" / "meshes" / "buildings"
EXPORT_LOG = PACK_ROOT / "EXPORT_LOG.txt"

# ============================================================================
# EXPORT CONFIGURATION
# ============================================================================

def get_export_config(building_id: str, faction: str, kenney_source_key: str) -> Dict:
    """Generate export configuration for a single building."""
    kenney_path = KENNEY_STRUCTURES.get(kenney_source_key)
    if not kenney_path:
        raise ValueError(f"Unknown Kenney source: {kenney_source_key}")

    output_file = f"{faction[0:3]}_{building_id}.fbx"  # rep_ or cis_

    return {
        "input": kenney_path,
        "faction": faction,
        "building_id": building_id,
        "output": str(ASSETS_DIR / output_file),
        "log": str(EXPORT_LOG),
    }


def get_all_export_tasks() -> List[Dict]:
    """Generate all export tasks (building × faction combinations)."""
    tasks = []

    for building_id, display_name, kenney_src, tris in BUILDINGS:
        for faction in FACTIONS:
            config = get_export_config(building_id, faction, kenney_src)
            config["display_name"] = display_name
            config["estimated_tris"] = tris
            tasks.append(config)

    return tasks


# ============================================================================
# BLENDER INTEGRATION
# ============================================================================

def run_blender_export(config: Dict, blender_path: str = "blender", dry_run: bool = False) -> Dict:
    """
    Execute a single Blender FBX export.

    Returns dict with status, stdout, stderr.
    """
    cmd = [
        blender_path,
        "--background",
        "--python",
        str(PACK_ROOT / "blender_batch_export.py"),
        "--",
        "--input", config["input"],
        "--faction", config["faction"],
        "--building-id", config["building_id"],
        "--output", config["output"],
        "--log", config["log"],
    ]

    result = {
        "building_id": config["building_id"],
        "faction": config["faction"],
        "display_name": config.get("display_name", config["building_id"]),
        "output": config["output"],
        "status": "pending",
        "command": " ".join(cmd),
    }

    if dry_run:
        result["status"] = "dry_run"
        logger.info(f"[DRY RUN] {config['faction']}_{config['building_id']}")
        return result

    try:
        logger.info(f"Exporting {config['faction']}_{config['building_id']}...")
        process = subprocess.run(
            cmd,
            capture_output=True,
            text=True,
            timeout=300,  # 5 min per export
        )

        if process.returncode == 0:
            result["status"] = "success"
            logger.info(f"✓ {config['faction']}_{config['building_id']}")
        else:
            result["status"] = "failed"
            result["stderr"] = process.stderr
            logger.error(f"✗ {config['faction']}_{config['building_id']}: {process.stderr[:200]}")

        result["stdout"] = process.stdout

    except subprocess.TimeoutExpired:
        result["status"] = "timeout"
        logger.error(f"✗ {config['faction']}_{config['building_id']}: timeout after 300s")
    except Exception as e:
        result["status"] = "error"
        result["error"] = str(e)
        logger.error(f"✗ {config['faction']}_{config['building_id']}: {str(e)}")

    return result


# ============================================================================
# PARALLEL BATCH PROCESSING
# ============================================================================

def worker_export(args: Tuple[Dict, str, bool]) -> Dict:
    """Worker function for multiprocessing pool."""
    config, blender_path, dry_run = args
    return run_blender_export(config, blender_path, dry_run)


def run_parallel_exports(tasks: List[Dict], blender_path: str, num_workers: int, dry_run: bool):
    """Run exports in parallel using multiprocessing pool."""
    logger.info(f"Starting parallel exports ({num_workers} workers, {len(tasks)} tasks)...")

    # Create work items
    work_items = [(task, blender_path, dry_run) for task in tasks]

    results = []
    with Pool(processes=num_workers) as pool:
        results = pool.map(worker_export, work_items)

    return results


# ============================================================================
# REPORTING
# ============================================================================

def print_summary(results: List[Dict]):
    """Print export summary."""
    total = len(results)
    successful = sum(1 for r in results if r["status"] == "success")
    failed = sum(1 for r in results if r["status"] == "failed")
    timeout = sum(1 for r in results if r["status"] == "timeout")
    errors = sum(1 for r in results if r["status"] == "error")
    dry_run = sum(1 for r in results if r["status"] == "dry_run")

    logger.info("\n" + "=" * 80)
    logger.info("EXPORT SUMMARY")
    logger.info("=" * 80)
    logger.info(f"Total:      {total}")
    logger.info(f"Successful: {successful}")
    logger.info(f"Failed:     {failed}")
    logger.info(f"Timeout:    {timeout}")
    logger.info(f"Errors:     {errors}")
    logger.info(f"Dry Run:    {dry_run}")
    logger.info("=" * 80)

    if failed > 0 or errors > 0 or timeout > 0:
        logger.warning("\nFailed/Error tasks:")
        for r in results:
            if r["status"] in ("failed", "error", "timeout"):
                logger.warning(f"  - {r['faction']}_{r['building_id']}: {r['status']}")

    # Write detailed results to JSON
    results_file = PACK_ROOT / "EXPORT_RESULTS.json"
    with open(results_file, "w") as f:
        json.dump(
            {
                "timestamp": datetime.now().isoformat(),
                "summary": {
                    "total": total,
                    "successful": successful,
                    "failed": failed,
                    "timeout": timeout,
                    "errors": errors,
                    "dry_run": dry_run,
                },
                "results": results,
            },
            f,
            indent=2,
        )
    logger.info(f"\nDetailed results written to {results_file}")


# ============================================================================
# VALIDATION
# ============================================================================

def validate_output_files(results: List[Dict]) -> bool:
    """Verify that output FBX files were created."""
    missing = []
    for r in results:
        if r["status"] == "success":
            fbx_file = Path(r["output"])
            if not fbx_file.exists():
                missing.append(f"{r['faction']}_{r['building_id']}")

    if missing:
        logger.error(f"Missing output files: {missing}")
        return False

    logger.info(f"✓ All {len([r for r in results if r['status'] == 'success'])} output files verified")
    return True


# ============================================================================
# MAIN
# ============================================================================

def main():
    parser = argparse.ArgumentParser(
        description="Batch export all DINOForge buildings to FBX"
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Validate config without running Blender",
    )
    parser.add_argument(
        "--parallel",
        type=int,
        default=cpu_count() - 1,
        help=f"Number of parallel workers (default: {cpu_count() - 1})",
    )
    parser.add_argument(
        "--blender-path",
        default="blender",
        help="Path to Blender executable (default: blender in PATH)",
    )

    args = parser.parse_args()

    # Ensure output directory exists
    ASSETS_DIR.mkdir(parents=True, exist_ok=True)

    logger.info(f"DINOForge Building FBX Batch Export")
    logger.info(f"Pack root: {PACK_ROOT}")
    logger.info(f"Output dir: {ASSETS_DIR}")
    logger.info(f"Dry run: {args.dry_run}")
    logger.info("")

    # Generate tasks
    tasks = get_all_export_tasks()
    logger.info(f"Generated {len(tasks)} export tasks ({len(BUILDINGS)} buildings × 2 factions)")

    # Run exports
    results = run_parallel_exports(
        tasks,
        args.blender_path,
        args.parallel,
        args.dry_run,
    )

    # Report
    print_summary(results)

    # Validate
    if not args.dry_run:
        validate_output_files(results)

    # Exit code
    failed_count = sum(1 for r in results if r["status"] in ("failed", "error", "timeout"))
    return 0 if failed_count == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
