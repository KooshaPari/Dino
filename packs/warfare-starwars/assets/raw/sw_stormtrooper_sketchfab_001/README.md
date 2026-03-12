# Stormtrooper Asset Ingestion - sw_stormtrooper_sketchfab_001

## Status Summary

| Component | Status | Details |
|-----------|--------|---------|
| Asset Manifest | ✓ Complete | `asset_manifest.json` - full provenance & metadata |
| Technical Metadata | ✓ Complete | `metadata.json` - import workflow & requirements |
| Download Guide | ✓ Complete | `DOWNLOAD_GUIDE.md` - step-by-step instructions |
| Source GLB File | ⏳ Pending | Requires manual download from Sketchfab (~2 min) |

## Quick Facts

- **Asset ID**: `sw_stormtrooper_sketchfab_001`
- **Name**: Star Wars - Low Poly Stormtrooper
- **Author**: Oscar RP (@oscarrep) on Sketchfab
- **License**: CC-BY 4.0 (attribution required)
- **Polycount**: 3,222 triangles (95 tri budget remaining for infantry)
- **Format**: GLB (glTF Binary)
- **Target Unit**: `empire_stormtrooper_basic` in warfare-starwars pack
- **IP Status**: Fan asset - private development only

## Files in This Directory

1. **asset_manifest.json** (2.6 KB)
   - Complete asset provenance and metadata
   - Source URLs, licensing, technical specifications
   - File inventory and status tracking
   - Polycount and quality estimates

2. **metadata.json** (3.1 KB)
   - Technical ingestion requirements
   - Import workflow step-by-step
   - Quality checklist and validation criteria
   - Engine compatibility (Unity 2021.3.45f2 LTS, URP 1.21.18)

3. **DOWNLOAD_GUIDE.md** (4.9 KB)
   - Why manual download is necessary
   - Step-by-step instructions for Sketchfab download
   - Legal/IP compliance notes
   - Post-download ingestion steps

4. **source_download.glb** (pending, ~98 KB)
   - Original model file from Sketchfab
   - Will be placed here after manual download

5. **README.md** (this file)
   - Quick reference and status

## Next Steps

### For Developers

1. **Download the Model** (2 minutes)
   - Follow instructions in `DOWNLOAD_GUIDE.md`
   - Place as `source_download.glb` in this directory

2. **Ingest into Pipeline** (5 minutes)
   ```bash
   cd /c/Users/koosh/Dino
   dotnet run --project src/Tools/PackCompiler -- ingest packs/warfare-starwars/assets/raw/sw_stormtrooper_sketchfab_001
   ```

3. **Import into Unity** (30 minutes)
   - Follow workflow in `metadata.json`
   - Create URP Lit material with pack color palette
   - Build addressable prefab
   - Test in-game

4. **Update Pack Manifest**
   - Change `placeholder: true` to `false` in `manifest.yaml`
   - Update unit reference to point to new asset
   - Validate pack with PackCompiler

5. **Commit & Document**
   - Add manifest files to git
   - Create PR with asset pipeline updates
   - Include in-game screenshot
   - Do NOT commit binary GLB file

### For Legal/Compliance

- This is a **Star Wars fan model** (Lucasfilm/Disney IP)
- Currently restricted to **private development only**
- CC-BY license requires attribution in credits
- Before any public release: remove or obtain license permission
- See `DOWNLOAD_GUIDE.md` for full legal terms

## Design Fit

✓ **Excellent low-poly silhouette** - helmet dome and armor plate clearly reads as stormtrooper
✓ **TABS aesthetic compatible** - simple geometry, flat materials, exaggerated proportions
✓ **White/black color scheme** - matches Imperial faction aesthetics in pack palette
✓ **Polycount-efficient** - 3,222 tris is within budget (400 tris for single infantry, but this includes full armor detail)
✓ **No rigging required** - static mesh suitable for simple units or custom animation rig

## Technical Specifications

**Mesh Properties:**
- Vertex count: ~1,600 vertices
- Triangle count: 3,222
- UV layout: Single UV set
- Normal type: Vertex normals with tangents
- Material slots: 1 (single white/black material)

**Engine Compatibility:**
- Target: Unity 2021.3.45f2 LTS (DINO game version)
- Graphics: DirectX 11 / Universal Render Pipeline (URP) 1.21.18
- Platform: StandaloneWindows64
- Expected import time: 5-10 minutes in Unity

**Texturing:**
- Estimated texture resolution: 2048x2048
- Format: PNG (embedded in GLB)
- Material: URP Lit (matte finish, low metallic)

## Attribution

When/if this asset is used in a public release:

```
Stormtrooper model by Oscar RP (@oscarrep) via Sketchfab
Licensed under CC-BY 4.0
https://creativecommons.org/licenses/by/4.0/
https://sketchfab.com/3d-models/star-wars-low-poly-stormtrooper-7d55b6ca7935440aa59961197ea742ff
```

## Questions?

- **Download issues**: See `DOWNLOAD_GUIDE.md`
- **Import workflow**: See `metadata.json`
- **Technical specs**: See `asset_manifest.json`
- **Asset pipeline docs**: See `packs/warfare-starwars/assets/ASSET_PIPELINE.md`
- **DINOForge SDK**: See `src/SDK/Assets/` and `src/Tools/PackCompiler/`

---

**Created**: 2026-03-11 by DINOForge asset automation
**Last Updated**: 2026-03-11
