#!/usr/bin/env python3
"""Generate synthetic test data for DINOForge MCP tools."""
from __future__ import annotations
import json, os, random, string
from pathlib import Path

def random_name(length: int = 12) -> str:
    return "".join(random.choices(string.ascii_lowercase, k=length))

def generate_pack_manifest() -> dict:
    return {
        "pack": {"name": random_name(), "version": "1.0.0", "author": "test-generator"},
        "units": [{"id": random_name(8), "name": f"Unit-{i}", "health": random.randint(50, 200)} for i in range(random.randint(3, 10))],
        "buildings": [{"id": random_name(8), "name": f"Bldg-{i}", "health": random.randint(100, 500)} for i in range(random.randint(2, 5))],
    }

def generate_log_data(lines: int = 100) -> str:
    levels = ["INFO", "WARN", "ERROR", "DEBUG"]
    components = ["AssetPipeline", "PackLoader", "GameBridge", "CatalogService"]
    return chr(10).join(f"[{random.choices(levels, weights=[50,20,5,25])[0]}] {random.choice(components)}: {random_name(20)}" for _ in range(lines))

def main() -> None:
    fixtures_dir = os.environ.get("TEST_FIXTURES_DIR", "tests/fixtures/generated")
    packs_path = Path(fixtures_dir + "/packs")
    packs_path.mkdir(parents=True, exist_ok=True)
    for i in range(5):
        (packs_path / f"test-pack-{i}.json").write_text(json.dumps(generate_pack_manifest(), indent=2))
    logs_path = Path(fixtures_dir + "/logs")
    logs_path.mkdir(parents=True, exist_ok=True)
    for i in range(3):
        (logs_path / f"test-log-{i}.txt").write_text(generate_log_data(random.randint(50, 200)))
    print(f"Generated 5 pack fixtures and 3 log fixtures in {fixtures_dir}")

if __name__ == "__main__":
    main()
