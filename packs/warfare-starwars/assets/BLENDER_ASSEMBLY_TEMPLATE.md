# Blender Assembly Template: Clone Quarters Pod

**Difficulty**: Beginner / Intermediate
**Estimated Time**: 2 hours (first time; 45 min with practice)
**Target Building**: `rep_house_clone_quarters` (Republic) / `cis_house_droid_pod` (CIS)
**Blender Version**: 3.6+

---

## Step-by-Step Assembly Process

### Phase 1: Project Setup (10 minutes)

#### 1.1 Create New Blender Project

1. Open Blender 3.6 (or newer)
2. Create a new **General** project
3. Delete the default cube (select → X → Confirm)

**Expected State**:
- Empty scene with only camera and light
- Grid visible
- Ready to import

---

#### 1.2 Import Kenney Source Model

**File to Import**: `assets/source/kenney/space-kit/Models/FBX format/structure.fbx`

**Steps**:
1. Go to **File → Import → FBX** (`.fbx`)
2. Navigate to Kenney source folder
3. Select `structure.fbx`
4. Click **Import FBX**

**Import Settings** (use defaults unless noted):
- Scale: 1.0
- Forward axis: -Y
- Up axis: Z
- Automatic bone orientation: ON

**Expected Result**:
```
Scene contents (after import):
  └── structure (object)
      └── Material (auto-imported)
```

---

#### 1.3 Inspect & Rename

After import, you'll see a generic "structure" object. Rename it for clarity:

1. In **Outliner** (right panel), right-click the imported object
2. Select **Rename** (F2)
3. Type: `rep_house_base` (or `cis_house_base` for CIS variant)

**Expected State**:
- Object renamed in outliner
- Object visible in 3D viewport
- Ready for texturing

---

### Phase 2: Texture Application (30 minutes)

#### 2.1 Create Material Slots

The imported model may or may not have materials. We need to create faction-specific materials.

**Steps**:
1. Select the imported object in viewport (click on it)
2. Go to **Shading** workspace (top menu bar)
3. In **Shader Editor** (bottom panel), look for existing materials or click **New**
4. Create a new material: click **New** button (if no material exists)

**Material Node Setup**:

For **Republic Variant** (`rep_house_clone_quarters`):
- Create nodes: `Principled BSDF` (usually auto-created) + `Image Texture`

```
[Image Texture]
     ↓
  Base Color
     ↓
[Principled BSDF] ← Color
     ↓
[Material Output]
```

For **CIS Variant** (`cis_house_droid_pod`):
- Same node structure, different texture

---

#### 2.2 Load Faction Texture

**For Republic**:

1. In **Shader Editor**, select the **Image Texture** node (or create one: Shift+A → Texture → Image Texture)
2. Click **Open** (folder icon)
3. Navigate to: `assets/textures/buildings/rep_house_clone_quarters_albedo.png`
4. Click **Open Image**
5. Connect output pin to **Principled BSDF** > **Base Color**

**For CIS**:

1. Same steps, but load: `assets/textures/buildings/cis_house_droid_pod_albedo.png`

**Expected Viewport** (after texture load):
```
Before:
  [Grey/white generic object]

After:
  [Object with faction-color texture applied]
  Republic: Bright white with blue accents
  CIS: Dark grey with orange/rust accents
```

---

#### 2.3 Enable Texture Preview in Viewport

1. In viewport top-right, change shading mode from **Solid** to **Material Preview** (third circle icon)
   - OR press **Z** and select **Material Preview**

**Expected Change**:
- Texture now visible in viewport (not just in renders)
- Lighting improves visibility
- Colors appear as intended

---

### Phase 3: Add Faction-Specific Details (45 minutes)

#### 3.1 Add Detail Layer: Accents & Stripes

For this template, we'll add a simple **decal stripe** using a second material.

**Steps** (Republic example):

1. Go back to **Modeling** workspace
2. In outliner, add a new object for the stripe:
   - **Add → Mesh → Cube**
   - Scale it to be a thin stripe: `S` (scale) → `Z` (z-axis only) → `0.05` (thin)
   - Move it to the side of the house: `G` (grab/move) → `X` (x-axis only) → position

3. Create a new material for the stripe:
   - Select stripe cube
   - In **Shading**, create new material
   - Set color to pure blue (`#1A3A6B` = RGB 26, 58, 107)
   - In **Principled BSDF**, set:
     - Base Color: navy blue
     - Metallic: 0.2 (slight sheen)
     - Roughness: 0.7

**Expected Visual**:
```
Before:
  [White rounded structure]

After:
  [White structure]
  [Navy blue accent stripe on side]
```

---

#### 3.2 Add Custom Detail: Emblem or Glow Emitter

**Option A: Static Emblem Decal (Simple)**

1. Add another small cube as emblem base
2. Scale it: `S` → `0.1` (small)
3. Position it on front face
4. Create material: bright gold (`#FFD700` = RGB 255, 215, 0)
5. Apply to emblem

**Option B: Glow Emitter Point (Advanced, for future VFX)**

1. Add an empty object (for reference):
   - **Add → Empty → Sphere**
   - Scale tiny: `S` → `0.05`
   - Position at top center of building
2. Rename in outliner: `glow_emitter_top`
3. Note this location for VFX team

**Expected Result**:
```
[White building structure]
  + Navy stripe
  + Gold emblem (front, small)
  + [Glow emitter point marked]
```

---

#### 3.3 CIS Variant: Rust & Mechanical Details

For CIS, follow same steps but:

- **Stripe color**: Rust orange (`#B35A00` = RGB 179, 90, 0)
- **Emblem**: Replace with tech insignia (small + orange-grey mix)
- **Add details**: Tiny geometric "panels" (cubes scaled thin) showing mechanical nature
- **Accent trim**: Add dark recesses using shadow-colored stripes

---

### Phase 4: Optimization & Cleanup (20 minutes)

#### 4.1 Optimize Polygon Count

Current target: **300-400 triangles**

**Check current count**:
1. In viewport, press **N** (show stats)
2. Look for "Triangles: X"
3. Compare to budget

**If over budget, decimate**:
1. Select the main building object
2. Add modifier: **Modifiers panel** (wrench icon right) → **Add Modifier** → **Decimate**
3. Set **Ratio**: 0.8 (keeps 80% of geometry)
4. Check stats again; repeat if needed

**Expected Result**: `< 400 tris` (verify in stats)

---

#### 4.2 Merge Materials (Optional)

If you added multiple objects (stripe, emblem), you can merge them for export:

1. Select all objects (A key)
2. **Object → Join** (Ctrl+J)
3. Result: Single object with combined materials

**Skip this if keeping separate** — both methods export fine.

---

#### 4.3 Center Pivot Point

Ensure the building's origin (pivot) is at its base:

1. Select building object
2. **Object → Set Origin → Origin to Geometry**
3. Then **Object → Set Origin → Origin to 3D Cursor** (if you want to adjust further)

**Why**: Game engine expects buildings to sit on ground; pivot at base prevents floating.

---

### Phase 5: Export to FBX (15 minutes)

#### 5.1 Prepare for Export

1. Select the building object (click on it in viewport or outliner)
2. Go to **File → Export → FBX** (`.fbx`)

#### 5.2 Export Settings

In the **FBX Export** dialog, set:

| Setting | Value | Reason |
|---------|-------|--------|
| **Scale** | 1.0 | Maintain Kenney scale |
| **Forward** | -Y Forward | Standard FBX convention |
| **Up** | Z Up | Consistent with import |
| **Include → Mesh** | ✓ ON | Export geometry |
| **Include → Materials** | ✓ ON | Export textures/colors |
| **Include → Smoothing Groups** | ✓ ON | Smooth shading |
| **Smoothing** | ON | Face smoothing |
| **Apply Scalings** | FBX Units | Standard scaling |

**Example**:
```
Scale: 1.0
Forward Axis: -Y Forward
Up Axis: Z Up
☑ Apply Scalings: FBX Units
☑ Include Mesh
☑ Include Materials
☑ Smoothing Groups
```

---

#### 5.3 Choose Output Path

**For Republic**:
```
packs/warfare-starwars/assets/meshes/buildings/rep_house_clone_quarters.fbx
```

**For CIS**:
```
packs/warfare-starwars/assets/meshes/buildings/cis_house_droid_pod.fbx
```

1. Type filename in the text box
2. Click **Export FBX**

**Expected Result**:
```
✓ File created at path
✓ Console message: "Exported [filename]"
```

---

### Phase 6: Validate & Iterate (30 minutes)

#### 6.1 Visual Inspection

Before testing in game, verify the export:

1. Close Blender project (save as `rep_house_clone_quarters.blend` for future edits)
2. Re-open Blender
3. **File → Open → rep_house_clone_quarters.blend**
4. **File → Import → FBX** (import the exported FBX)
5. Compare side-by-side in viewport

**Checklist**:
- [ ] Texture still applied
- [ ] Colors correct (white+blue for Rep, grey+orange for CIS)
- [ ] Stripes/details intact
- [ ] Poly count acceptable
- [ ] No missing faces or "flipped" normals (dark patches)

---

#### 6.2 Scale & Proportions Check

The Kenney model should be roughly:
- **Height**: ~6-8 game units
- **Width/Depth**: ~4-6 game units (typical for house)

Visually, it should look like a **small residential pod** or **droid storage**, not a full building.

If scale looks wrong:
1. Select object
2. Press `S` (scale)
3. Type `1.5` (or `0.7`, etc.) to adjust
4. Re-export

---

#### 6.3 Test in Game (Optional but Recommended)

1. Copy exported FBX to game mod folder
2. Launch game
3. Check:
   - [ ] Building renders without errors
   - [ ] Texture visible (correct faction colors)
   - [ ] Scale matches vanilla building size
   - [ ] No Z-fighting (flickering surfaces)
   - [ ] FPS stable (no performance drops)

---

## Before/After Visual Reference

### ASCII Art Representation

**BEFORE** (vanilla structure.fbx imported):
```
    ╭─────────╮
    │         │
    │ Generic │  ← Plain grey/white
    │ Structure│  ← No faction color
    │         │
    ╰─────────╯
```

**AFTER** (Republic variant with details):
```
    ╭─────────────╮
    │  ███░░░████ │  ← Gold emblem (top-center)
    │ ░███░░░░░██░│  ← White primary (light grey)
    │ ░███░░░░░██░│  ← Navy stripe (right side)
    │ ░███░░░░░██░│  ← Republic blue accent
    ╰─────────────╯
         ↑
       ████  ← Glow emitter point (invisible, for VFX)
```

**AFTER** (CIS variant with details):
```
    ╭─────────────╮
    │ ██░░░░░░██░ │  ← Rust orange stripe (left)
    │ ██░░░░░░██░ │  ← Dark grey primary
    │ ██░░[⚙]░██░ │  ← Mechanical detail (center)
    │ ██░░░░░░██░ │  ← Shadow recesses
    ╰─────────────╯
```

---

## Workflow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│ START: Blender Assembly for rep_house_clone_quarters        │
└────────────────────────┬────────────────────────────────────┘
                         │
                    (10 min)
                         ↓
              ┌──────────────────────┐
              │ 1. Setup Project     │
              │    - New project     │
              │    - Delete cube     │
              │    - Import FBX      │
              └────────────┬─────────┘
                           │
                      (30 min)
                           ↓
              ┌──────────────────────┐
              │ 2. Apply Texture     │
              │    - Create material │
              │    - Load PNG        │
              │    - Enable preview  │
              └────────────┬─────────┘
                           │
                      (45 min)
                           ↓
              ┌──────────────────────┐
              │ 3. Add Details       │
              │    - Stripe decal    │
              │    - Emblem icon     │
              │    - Faction colors  │
              └────────────┬─────────┘
                           │
                      (20 min)
                           ↓
              ┌──────────────────────┐
              │ 4. Optimize          │
              │    - Decimate poly   │
              │    - Check tri count │
              │    - Merge materials │
              └────────────┬─────────┘
                           │
                      (15 min)
                           ↓
              ┌──────────────────────┐
              │ 5. Export FBX        │
              │    - FBX settings    │
              │    - Save to path    │
              │    - Verify export   │
              └────────────┬─────────┘
                           │
                      (30 min)
                           ↓
              ┌──────────────────────┐
              │ 6. Validate & Test   │
              │    - Reimport & check│
              │    - Test in game    │
              │    - Document notes  │
              └────────────┬─────────┘
                           │
                           ↓
              ┌──────────────────────┐
              │ ✓ DONE               │
              │ Save .blend source   │
              │ FBX ready for pack   │
              └──────────────────────┘
```

---

## Texture Assignment: Step-by-Step Detail

### Republic (white + navy)

**Texture File**: `rep_house_clone_quarters_albedo.png`

**Shader Node Walkthrough**:

```
1. Shade Editor (bottom panel):

   Image Texture Node:
   ┌──────────────────┐
   │ Image Texture    │ ← Double-click to load PNG
   │ [rep_house...] 📁│
   └────┬─────────────┘
        │ Color
        ↓
   Principled BSDF:
   ┌──────────────────┐
   │ Principled BSDF  │
   │ Base Color ← ●   │ (connected from Image)
   │ Metallic: 0.1    │
   │ Roughness: 0.8   │
   └────┬─────────────┘
        │ BSDF
        ↓
   Material Output:
   ┌──────────────────┐
   │ Material Output  │
   │ Surface ← ●      │
   └──────────────────┘

Expected result in Material Preview:
  → Texture appears: bright white + blue accents
  → Object reflects light naturally
```

---

### CIS (grey + orange)

**Texture File**: `cis_house_droid_pod_albedo.png`

**Same node structure**, but:
- Load `cis_house_droid_pod_albedo.png` instead
- Set Metallic slightly higher (0.2) for industrial look
- Set Roughness: 0.7 (more matte, less shiny)

**Expected result in Material Preview**:
- Dark grey primary
- Orange/rust accents
- Mechanical appearance (less reflective than Republic)

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| **Texture won't load** | Check file path; ensure PNG exists in `assets/textures/buildings/` |
| **Imported object is tiny** | Check Blender scale units (File → Project Settings → Units); may need S-scale |
| **Colors look wrong** | Switch to Material Preview mode (Z key); may still be in Solid view |
| **Export fails** | Ensure filename has no spaces; use underscores (e.g., `rep_house_clone_quarters.fbx`) |
| **Polygon count way over** | Use Decimate modifier (target 0.5 ratio to cut poly count in half) |
| **Shiny/reflective** | Increase Roughness in Principled BSDF (closer to 1.0) |
| **Stripe won't stick to model** | Join objects before export (Ctrl+J) |

---

## Advanced Options (Optional)

### Custom Glow Emitter

For buildings with glowing elements (farms, extractors), create emission:

1. Select a face or small cube detail
2. In Shader Editor, add **Emission** shader
3. Set color (e.g., blue for Republic, orange for CIS)
4. Increase strength (1.0-2.0)
5. Mix with **Mix Shader** between Principled and Emission

**Result**: Subtle glow that shows in game (looks like active machinery).

### Animated Rotor (Advanced)

For rotating details (claw, drill):

1. Create small object (e.g., cylinder)
2. Apply **Armature** modifier + bone
3. In **Dope Sheet**, add rotation keyframes
4. Export with armature

**Note**: Requires game support for skeletal animation (consult team).

---

## Checklist for Completion

- [ ] Blender project created
- [ ] Kenney FBX imported
- [ ] Texture applied (PNG loaded in shader)
- [ ] Faction accent stripe added
- [ ] Emblem or detail layer added
- [ ] Poly count optimized (< 400 tris)
- [ ] Pivot centered at base
- [ ] Exported to FBX in correct path
- [ ] Reimported and visually verified
- [ ] Tested in game (optional but recommended)
- [ ] Source `.blend` file saved for future edits
- [ ] Notes documented in BUILD_CHECKLIST.md

---

## Next Steps After Assembly

1. **Repeat for other 22 buildings** using this template
2. **Refine faction details** based on pilot feedback
3. **Batch-process** using Blender Python script (see BATCH_ASSEMBLY_PLAN.md)
4. **Validate all 24 in game**
5. **Document final workflow** for team reproduction

---

## References

- **Kenney Assets**: `assets/source/kenney/space-kit/`
- **Texture Outputs**: `assets/textures/buildings/`
- **FBX Outputs**: `assets/meshes/buildings/`
- **Build Checklist**: `BUILD_CHECKLIST.md`
- **Batch Plan**: `BATCH_ASSEMBLY_PLAN.md`
- **Blender Docs**: https://docs.blender.org/manual/en/latest/

---

**Created**: 2026-03-12
**Template Difficulty**: Intermediate
**Estimated Time**: 2 hours (first time)
**Reuse Time**: 45 minutes (with practice)
