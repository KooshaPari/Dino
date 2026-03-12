# Stormtrooper Model - Download & Ingestion Guide

## Asset Details
- **Model**: Star Wars - Low Poly Stormtrooper
- **Source**: Sketchfab by Oscar RP (@oscarrep)
- **Model ID**: `7d55b6ca7935440aa59961197ea742ff`
- **URL**: https://sketchfab.com/3d-models/star-wars-low-poly-stormtrooper-7d55b6ca7935440aa59961197ea742ff
- **License**: CC-BY (requires attribution)
- **Polycount**: 3,222 triangles (within budget for infantry unit)

## Manifest Status
✓ Asset manifest created: `asset_manifest.json`
✓ Technical metadata created: `metadata.json`
✓ Directory structure ready: `sw_stormtrooper_sketchfab_001/`
⏳ Source file pending: `source_download.glb` (requires manual download)

## Why Manual Download?

Sketchfab requires user authentication to download models. The model is publicly available under CC-BY license but requires:
1. Free Sketchfab account (or existing login)
2. Manual click-through to accept terms
3. Format selection (GLB recommended)

This 2-minute manual step ensures legal compliance and proper attribution tracking.

## Download Instructions

### Step 1: Visit Sketchfab
Open this URL in your browser:
```
https://sketchfab.com/3d-models/star-wars-low-poly-stormtrooper-7d55b6ca7935440aa59961197ea742ff
```

### Step 2: Click Download Button
- Look for the **Download** button (top right of model viewer)
- If prompted to sign in, create a free account or use existing login

### Step 3: Select GLB Format
- Choose **glTF Binary (.glb)** from the format options
- This format preserves geometry, materials, and textures in one file
- File size: ~98 KB (small, loads quickly in Unity)

### Step 4: Save File
- Save the downloaded file as **`source_download.glb`** in this directory
- Full path: `packs/warfare-starwars/assets/raw/sw_stormtrooper_sketchfab_001/source_download.glb`

### Step 5: Verify Download
```bash
ls -lh packs/warfare-starwars/assets/raw/sw_stormtrooper_sketchfab_001/source_download.glb
# Should show ~98 KB file size
file packs/warfare-starwars/assets/raw/sw_stormtrooper_sketchfab_001/source_download.glb
# Should report: "glTF binary data, 3D model container"
```

### Step 6: Ingest into Asset Pipeline
Once the GLB file is in place, run:
```bash
dotnet run --project src/Tools/PackCompiler -- ingest packs/warfare-starwars/assets/raw/sw_stormtrooper_sketchfab_001
```

This will:
- Validate the GLB structure
- Extract mesh and texture data
- Create import-ready FBX for Unity
- Update status in `asset_manifest.json`

## Legal Compliance

**CC-BY Attribution Required**

This asset is licensed under Creative Commons Attribution 4.0 International (CC-BY).
Attribution must be provided:

**In-game credits** (when/if public release occurs):
> Stormtrooper model by Oscar RP (@oscarrep) via Sketchfab
> Licensed under CC-BY 4.0 (https://creativecommons.org/licenses/by/4.0/)

**In repository**: Add to `packs/warfare-starwars/assets/ATTRIBUTION.md`:
```markdown
### 3D Models

- **sw_stormtrooper_sketchfab_001**: Star Wars Low Poly Stormtrooper by Oscar RP (@oscarrep)
  - Source: https://sketchfab.com/3d-models/star-wars-low-poly-stormtrooper-7d55b6ca7935440aa59961197ea742ff
  - License: CC-BY 4.0
  - Polycount: 3,222 triangles
```

## IP Restrictions

⚠️ **IMPORTANT: Fan Asset - Not for Public Distribution**

This is a **Star Wars fan model**. The underlying IP (characters, universe, designs) is owned by:
- **Lucasfilm Ltd. / Disney**

**Current Status**: Private development only
- ✓ Permitted: Internal testing, private dev builds, personal mods
- ✗ NOT Permitted: Public release, distribution, commercial use, mod platforms

**Before any public release** of DINOForge containing this asset:
1. Remove all Star Wars fan models
2. Replace with original-licensed (CC0/CC-BY) assets
3. Obtain explicit legal permission from Lucasfilm if keeping SW IP

For more on fan asset policy, see: `docs/FAN_ASSETS_POLICY.md`

## Import Quality Checklist

Once the GLB is downloaded, verify:

- [ ] File is valid GLB (binary glTF format)
- [ ] Silhouette is clear stormtrooper shape
- [ ] White/black armor coloring is visible
- [ ] Polycount reported as ~3222 triangles
- [ ] Mesh is centered at origin (0, 0, 0)
- [ ] No obvious visual artifacts (flipped normals, missing faces, etc.)
- [ ] Textures are embedded and loading correctly

## Next Steps

1. **Download**: Follow steps above to get `source_download.glb`
2. **Ingest**: Run PackCompiler ingest command
3. **Import**: Open in Unity, create material and prefab (see ASSET_PIPELINE.md)
4. **Normalize**: Apply warfare-starwars color palette and styling
5. **Test**: Verify in-game with faction unit preview
6. **Commit**: Add to git with manifest files only (no binary GLB in repo)

## Questions?

- Sketchfab download help: https://help.sketchfab.com/hc/en-us/articles/204010828-Downloading-models
- DINOForge asset pipeline: See `ASSET_PIPELINE.md` in this directory
- Legal/IP questions: Contact project lead
