#!/bin/bash
# Parallel FBX batch export for Star Wars Clone Wars units
# Exports all 26 unit FBX files with faction-specific colors
#
# Usage:
#   ./run_units_batch_export.sh [--dry-run] [--parallel N] [--faction FACTION]
#
# Requirements:
#   - Blender 3.0+ installed and in PATH
#   - Python 3.7+ with jq (JSON query)
#   - GNU Parallel (optional, for parallelization)
#   - Kenney assets in source/kenney/sci-fi-rts/Models/FBX/

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BATCH_CONFIG="$SCRIPT_DIR/units_batch_config.json"
LOG_FILE="$SCRIPT_DIR/UNITS_EXPORT_LOG.txt"
DRY_RUN=false
PARALLEL_JOBS=4
TARGET_FACTION=""

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --dry-run)
            DRY_RUN=true
            shift
            ;;
        --parallel)
            PARALLEL_JOBS="$2"
            shift 2
            ;;
        --faction)
            TARGET_FACTION="$2"
            shift 2
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Check prerequisites
check_prerequisites() {
    local missing=0

    if ! command -v blender &> /dev/null; then
        echo -e "${RED}✗ Blender not found in PATH${NC}"
        missing=1
    fi

    if ! command -v jq &> /dev/null; then
        echo -e "${RED}✗ jq not found in PATH (install via: apt-get install jq)${NC}"
        missing=1
    fi

    if ! command -v python3 &> /dev/null; then
        echo -e "${RED}✗ Python 3 not found in PATH${NC}"
        missing=1
    fi

    if [[ ! -f "$BATCH_CONFIG" ]]; then
        echo -e "${RED}✗ Batch config not found: $BATCH_CONFIG${NC}"
        missing=1
    fi

    if [[ $missing -eq 1 ]]; then
        echo "Please install missing dependencies and try again."
        exit 1
    fi

    echo -e "${GREEN}✓ All prerequisites met${NC}"
}

# Export single unit FBX
export_unit() {
    local unit_id="$1"
    local faction="$2"
    local input="$3"
    local output="$4"

    if [[ "$DRY_RUN" == "true" ]]; then
        echo "[DRY RUN] $faction - $unit_id"
        return 0
    fi

    # Check if source FBX exists
    if [[ ! -f "$input" ]]; then
        echo -e "${YELLOW}⚠ Source not found: $input (skipping $unit_id)${NC}"
        return 1
    fi

    # Ensure output directory exists
    mkdir -p "$(dirname "$output")"

    # Run Blender export
    echo "Exporting $faction - $unit_id..."
    blender --background --python blender_units_batch_export.py -- \
        --unit "$unit_id" \
        --faction "$faction" \
        --input "$input" \
        --output "$output" \
        --log "$LOG_FILE" 2>&1 | grep -E "(✓|✗|Exporting)" || true

    if [[ -f "$output" ]]; then
        echo -e "${GREEN}✓ $unit_id exported${NC}"
        return 0
    else
        echo -e "${RED}✗ Export failed: $unit_id${NC}"
        return 1
    fi
}

# Export all units (sequential)
export_sequential() {
    local total=0
    local success=0

    echo "Exporting units (sequential mode)..."
    echo "======================================="

    # Clear log file
    > "$LOG_FILE"

    # Extract units from batch config
    local units=$(jq -r '.units[] | "\(.unit_id),\(.faction),\(.input),\(.output)"' "$BATCH_CONFIG")

    while IFS=',' read -r unit_id faction input output; do
        # Filter by faction if specified
        if [[ -n "$TARGET_FACTION" && "$faction" != "$TARGET_FACTION" ]]; then
            continue
        fi

        ((total++))
        if export_unit "$unit_id" "$faction" "$input" "$output"; then
            ((success++))
        fi
    done <<< "$units"

    echo "======================================="
    echo "Exported: $success/$total units"
    return $((total - success))
}

# Export all units (parallel using xargs)
export_parallel() {
    local total=$(jq '.units | length' "$BATCH_CONFIG")

    if [[ -n "$TARGET_FACTION" ]]; then
        total=$(jq ".units | map(select(.faction == \"$TARGET_FACTION\")) | length" "$BATCH_CONFIG")
    fi

    echo "Exporting units (parallel mode, $PARALLEL_JOBS jobs)..."
    echo "======================================="

    # Clear log file
    > "$LOG_FILE"

    # Use xargs for parallel processing
    jq -r '.units[] | "\(.unit_id),\(.faction),\(.input),\(.output)"' "$BATCH_CONFIG" | \
        grep -E "($(echo "$TARGET_FACTION" | sed 's/,/|/g'))" || echo "" | \
        xargs -P "$PARALLEL_JOBS" -I {} bash -c '
            IFS="," read -r unit_id faction input output <<< "{}"
            export_unit "$unit_id" "$faction" "$input" "$output"
        ' 2>&1

    echo "======================================="
    echo "Batch export complete!"
}

# Main
main() {
    echo "DINOForge Star Wars Units - FBX Batch Export"
    echo "=============================================="
    echo ""

    check_prerequisites

    echo "Configuration:"
    echo "  Batch config: $BATCH_CONFIG"
    echo "  Log file: $LOG_FILE"
    echo "  Dry run: $DRY_RUN"
    echo "  Parallel jobs: $PARALLEL_JOBS"
    echo "  Target faction: ${TARGET_FACTION:-all}"
    echo ""

    # Determine if parallel processing is available
    if command -v parallel &> /dev/null; then
        echo "GNU Parallel detected. Using parallel mode."
        export_parallel
    else
        echo "GNU Parallel not available. Using sequential mode."
        export_sequential
    fi

    echo ""
    echo "Export results:"
    tail -10 "$LOG_FILE" || echo "(no log entries)"
}

# Run main function
main
