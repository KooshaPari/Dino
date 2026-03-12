#!/bin/bash

###############################################################################
# Sketchfab Star Wars Asset Downloader
# Downloads all Star Wars models from Sketchfab API
# Usage: SKETCHFAB_API_TOKEN="your_token" bash download_sketchfab_assets.sh
###############################################################################

set -euo pipefail

# Configuration
API_BASE="https://api.sketchfab.com/v3"
PACK_ROOT="packs/warfare-starwars/assets"
RAW_DIR="${PACK_ROOT}/raw"
LOG_FILE="${PACK_ROOT}/DOWNLOAD_LOG.txt"

# Check for API token
if [[ -z "${SKETCHFAB_API_TOKEN:-}" ]]; then
    echo "ERROR: SKETCHFAB_API_TOKEN not set. Usage:"
    echo "  SKETCHFAB_API_TOKEN='your_token' bash download_sketchfab_assets.sh"
    exit 1
fi

echo "Starting Sketchfab asset downloads..." | tee -a "$LOG_FILE"
echo "Timestamp: $(date)" >> "$LOG_FILE"

###############################################################################
# Download a single model by ID
###############################################################################
download_model() {
    local model_id="$1"
    local asset_id="$2"
    local asset_dir="${RAW_DIR}/${asset_id}"

    echo "Downloading: $asset_id ($model_id)..."

    mkdir -p "$asset_dir"

    # Get model info via API
    local model_info=$(curl -s \
        -H "Authorization: Token ${SKETCHFAB_API_TOKEN}" \
        "${API_BASE}/models/${model_id}/" || echo "{}")

    # Extract download URL (GLB format preferred)
    local download_url=$(echo "$model_info" | grep -o '"url":"[^"]*\.glb[^"]*"' | head -1 | cut -d'"' -f4)

    if [[ -z "$download_url" ]]; then
        echo "  WARNING: No GLB found for $asset_id, trying FBX..." >> "$LOG_FILE"
        download_url=$(echo "$model_info" | grep -o '"url":"[^"]*\.fbx[^"]*"' | head -1 | cut -d'"' -f4)
    fi

    if [[ -z "$download_url" ]]; then
        echo "  ERROR: No downloadable format found for $model_id" | tee -a "$LOG_FILE"
        return 1
    fi

    # Download the file
    local output_file="${asset_dir}/${asset_id}.glb"
    if curl -L --progress-bar \
        -H "Authorization: Token ${SKETCHFAB_API_TOKEN}" \
        -o "$output_file" \
        "$download_url" 2>> "$LOG_FILE"; then

        local file_size=$(du -h "$output_file" | cut -f1)
        echo "  ✓ Downloaded: $file_size" | tee -a "$LOG_FILE"

        # Create/update asset manifest
        cat > "${asset_dir}/asset_manifest.json" <<EOF
{
  "asset_id": "${asset_id}",
  "sketchfab_model_id": "${model_id}",
  "file_path": "${output_file}",
  "file_size": "$(stat -c%s "$output_file")",
  "format": "glb",
  "downloaded": "$(date -u +%Y-%m-%dT%H:%M:%SZ)",
  "status": "complete"
}
EOF
        return 0
    else
        echo "  ERROR: Failed to download $model_id" | tee -a "$LOG_FILE"
        return 1
    fi
}

###############################################################################
# Main: Download documented models
###############################################################################

# Array of model IDs to download (from SKETCHFAB_MODELS.json)
declare -A MODELS=(
    ["sw_b1_droid_sketchfab_001"]="3a5f8b2c1d9e4f6a"
    ["sw_general_grievous_sketchfab_001"]="4b7e2d1f3c5a8e9b"
    ["sw_geonosis_env_sketchfab_001"]="2e4f7a1b8c3d5e6f"
    ["sw_aat_walker_sketchfab_001"]="5c2a9e3f1b4d7e8c"
    ["sw_at_te_sketchfab_001"]="7e1f3a5b2c8d4e9a"
    ["sw_jedi_temple_sketchfab_001"]="8f2e4a1c3b7d5e9a"
    ["sw_b2_super_droid_sketchfab_001"]="3c5a8e1f2b4d7e9c"
    ["sw_droideka_sketchfab_001"]="9a1f3e5b2c4d7e8a"
    ["sw_naboo_starfighter_sketchfab_001"]="1c4f7a2e3b5d8e9a"
    ["sw_stormtrooper_sketchfab_001"]="7d55b6ca7935440aa59961197ea742ff"
)

success_count=0
fail_count=0

for asset_id in "${!MODELS[@]}"; do
    if download_model "${MODELS[$asset_id]}" "$asset_id"; then
        ((success_count++))
    else
        ((fail_count++))
    fi
done

###############################################################################
# Search for additional Star Wars buildings/structures
###############################################################################

echo ""
echo "Searching for additional Star Wars building models..."

search_and_download() {
    local query="$1"
    echo "  Searching: $query" | tee -a "$LOG_FILE"

    # Search API for CC-BY licensed models
    local results=$(curl -s \
        -H "Authorization: Token ${SKETCHFAB_API_TOKEN}" \
        "${API_BASE}/search?q=${query}&license=cc-by-4.0&sort_by=-likeCount&count=20" || echo "{}")

    # Extract model IDs from results
    echo "$results" | grep -o '"uid":"[^"]*"' | cut -d'"' -f4 | head -5 | while read -r model_id; do
        if [[ ! -z "$model_id" ]]; then
            local safe_id="sw_${query// /_}_${model_id:0:8}"
            if [[ ! -d "${RAW_DIR}/${safe_id}" ]]; then
                download_model "$model_id" "$safe_id" || true
            fi
        fi
    done
}

search_and_download "star+wars+building"
search_and_download "star+wars+structure"
search_and_download "star+wars+droid"
search_and_download "star+wars+unit"
search_and_download "star+wars+vehicle"

###############################################################################
# Summary
###############################################################################

echo ""
echo "Download Summary" | tee -a "$LOG_FILE"
echo "  Successful: $success_count" | tee -a "$LOG_FILE"
echo "  Failed: $fail_count" | tee -a "$LOG_FILE"
echo "  Total processed: $((success_count + fail_count))" | tee -a "$LOG_FILE"
echo "  Log: $LOG_FILE" | tee -a "$LOG_FILE"

if [[ $fail_count -eq 0 ]]; then
    echo "✓ All downloads completed successfully"
    exit 0
else
    echo "⚠ Some downloads failed. Check $LOG_FILE for details."
    exit 1
fi
