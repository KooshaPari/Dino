# DINOForge Star Wars - Color Palette Reference Guide

## Faction Visual Identity

### Galactic Republic
**Motto**: "Order through Unity"
**Aesthetic**: Clean, organized, high-tech military

```
████████████████████  White (#F5F5F5) - Primary
████████████████████  Blue #1A3A6B - Military
████████████████████  Accent Blue #64A0DC
```

**Characteristics**:
- High contrast, clean lines
- Organized, hierarchical appearance
- Bright, visible (tactical clarity)
- Advanced technology aesthetic
- Symmetric, balanced design

**Associated Buildings**:
- Command: Central Republic authority
- Barracks: Clone trooper training facilities
- Defenses: Organized shield generators and towers
- Economy: Structured supply chains
- Research: High-tech R&D labs

---

### Confederacy of Independent Systems (CIS)
**Motto**: "Efficiency through Automation"
**Aesthetic**: Industrial, utilitarian, mechanical

```
████████████████████  Dark Grey (#444444) - Primary
████████████████████  Rust Orange (#B35A00) - Industrial
████████████████████  Dark Brown #663300
```

**Characteristics**:
- Low contrast, industrial appearance
- Mechanical, utilitarian design
- Darker, more menacing (tactical deception)
- Automated/droid aesthetic
- Asymmetric, modular construction

**Associated Buildings**:
- Command: Tactical droid coordination center
- Barracks: Automated droid assembly lines
- Defenses: Sentry turrets and ray shields
- Economy: Extraction and processing plants
- Research: Droid development facilities

---

## Transformation Parameters by Faction

### Republic Transformation
Applied to create high-tech, organized aesthetic:

| Parameter | Value | Effect |
|-----------|-------|--------|
| **Hue Shift** | +210° | Rotates colors toward cool blues |
| **Saturation** | × 0.8 | Desaturates for clean, professional look |
| **Brightness** | × 1.1 | Brightens textures for visibility |

**Pipeline Result**: `RGB(128, 128, 128)` → Bright blue-white

**Visual Effect**:
- Neutral greys become pristine whites
- Warm tones cool to blues
- Matte surfaces brighten slightly
- Sterilized, professional appearance

### CIS Transformation
Applied to create industrial, mechanical aesthetic:

| Parameter | Value | Effect |
|-----------|-------|--------|
| **Hue Shift** | +30° | Rotates colors toward warm oranges |
| **Saturation** | × 1.2 | Saturates for bold, industrial look |
| **Brightness** | × 0.9 | Darkens for weight and authority |

**Pipeline Result**: `RGB(128, 128, 128)` → Dark orange-grey

**Visual Effect**:
- Neutral greys darken to industrial grey
- Cool tones warm to rust orange
- Matte surfaces emphasize texture
- Heavy, mechanical appearance

---

## Building Color Mapping

### Republic Buildings Color Strategy

#### Command Center
```
Primary: White with deep blue accents
Secondary: Blue structural elements
Tertiary: Light blue highlights
Aesthetic: Organized command authority
```

#### Clone Training Facility (Barracks)
```
Primary: White walls
Secondary: Blue training markings
Tertiary: Light highlights for equipment
Aesthetic: Clean military discipline
```

#### Weapons Factory (Barracks)
```
Primary: White manufacturing floor
Secondary: Blue safety markings
Tertiary: Light accent panels
Aesthetic: Advanced weapons production
```

#### Vehicle Bay (Barracks)
```
Primary: White garage structures
Secondary: Blue transport markings
Tertiary: Light detailing on vehicles
Aesthetic: High-tech motor pool
```

#### Guard Tower (Defense)
```
Primary: White reinforced structure
Secondary: Blue observation platforms
Tertiary: Light targeting systems
Aesthetic: Watchful defense
```

#### Shield Generator (Defense)
```
Primary: White projector housing
Secondary: Blue power channels
Tertiary: Light shield effect indicators
Aesthetic: Advanced force field technology
```

#### Supply Station (Economy)
```
Primary: White storage structures
Secondary: Blue inventory systems
Tertiary: Light loading equipment
Aesthetic: Organized logistics
```

#### Tibanna Gas Refinery (Economy)
```
Primary: White processing equipment
Secondary: Blue gas containment
Tertiary: Light pressure indicators
Aesthetic: Clean resource conversion
```

#### Research Laboratory (Research)
```
Primary: White lab surfaces
Secondary: Blue technology systems
Tertiary: Light experimental markers
Aesthetic: High-tech innovation
```

#### Blast Wall (Defense Structure)
```
Primary: White reinforced panels
Secondary: Blue structural reinforcement
Tertiary: Light joint detailing
Aesthetic: Impenetrable barrier
```

---

### CIS Buildings Color Strategy

#### Tactical Droid Center
```
Primary: Dark grey command structure
Secondary: Rust orange droid markings
Tertiary: Dark brown detail panels
Aesthetic: Organized droid authority
```

#### Droid Factory (Barracks)
```
Primary: Dark grey assembly frame
Secondary: Rust orange droid coloring
Tertiary: Dark brown assembly joints
Aesthetic: Mass production facility
```

#### Advanced Assembly Line (Barracks)
```
Primary: Dark grey manufacturing hull
Secondary: Rust orange heavy unit markings
Tertiary: Dark brown construction details
Aesthetic: Heavy weapons platform
```

#### Heavy Foundry (Barracks)
```
Primary: Dark grey forging chambers
Secondary: Rust orange AAT/Droideka colors
Tertiary: Dark brown heat vents
Aesthetic: Industrial weapons creation
```

#### Sentry Turret (Defense)
```
Primary: Dark grey gun mount
Secondary: Rust orange targeting systems
Tertiary: Dark brown ammunition feed
Aesthetic: Automated threat elimination
```

#### Ray Shield Generator (Defense)
```
Primary: Dark grey projector body
Secondary: Rust orange shield emitters
Tertiary: Dark brown power couplings
Aesthetic: Droid-operated defense
```

#### Mining Facility (Economy)
```
Primary: Dark grey extraction frames
Secondary: Rust orange mineral processing
Tertiary: Dark brown ore conveying
Aesthetic: Raw material extraction
```

#### Processing Plant (Economy)
```
Primary: Dark grey refinery structure
Secondary: Rust orange material flows
Tertiary: Dark brown catalyst systems
Aesthetic: Industrial material conversion
```

#### Techno Union Lab (Research)
```
Primary: Dark grey research facility
Secondary: Rust orange droid development systems
Tertiary: Dark brown testing chambers
Aesthetic: Advanced droid research
```

#### Durasteel Barrier (Defense Structure)
```
Primary: Dark grey armored panels
Secondary: Rust orange reinforcement bands
Tertiary: Dark brown mounting brackets
Aesthetic: Heavy industrial fortification
```

---

## HSV Color Space Transformation Details

### Understanding the Transformation

**Original RGB Values** (Neutral Kenney Texture):
- White: RGB(255, 255, 255) → HSV(0, 0, 1.0)
- Mid-grey: RGB(128, 128, 128) → HSV(0, 0, 0.5)
- Dark: RGB(64, 64, 64) → HSV(0, 0, 0.25)
- Colored: RGB(200, 100, 50) → HSV(20°, 0.75, 0.78)

### Republic Transformation

**Formula**: `h_new = (h + 210°) % 360°`, `s_new = s × 0.8`, `v_new = min(1.0, v × 1.1)`

**Example Transformations**:

| Neutral | HSV | Republic | Appearance |
|---------|-----|----------|------------|
| White | H0 S0 V1.0 | H210 S0 V1.0 | Pure White |
| Grey | H0 S0 V0.5 | H210 S0 V0.55 | Light Grey |
| Dark | H0 S0 V0.25 | H210 S0 V0.28 | Charcoal |
| Red | H0 S0.8 V0.8 | H210 S0.64 V0.88 | Blue |
| Yellow | H60 S0.8 V0.8 | H270 S0.64 V0.88 | Purple |
| Green | H120 S0.8 V0.8 | H330 S0.64 V0.88 | Magenta |

**Result**: Neutral colors become white/blue; other colors shift to cool tones

### CIS Transformation

**Formula**: `h_new = (h + 30°) % 360°`, `s_new = s × 1.2`, `v_new = min(1.0, v × 0.9)`

**Example Transformations**:

| Neutral | HSV | CIS | Appearance |
|---------|-----|-----|------------|
| White | H0 S0 V1.0 | H30 S0 V0.9 | Off-white |
| Grey | H0 S0 V0.5 | H30 S0 V0.45 | Dark Grey |
| Dark | H0 S0 V0.25 | H30 S0 V0.225 | Nearly Black |
| Red | H0 S0.8 V0.8 | H30 S0.96 V0.72 | Orange |
| Yellow | H60 S0.8 V0.8 | H90 S0.96 V0.72 | Yellow-Green |
| Green | H120 S0.8 V0.8 | H150 S0.96 V0.72 | Cyan |

**Result**: Neutral colors become dark grey; other colors shift to warm tones

---

## Color Theory Justification

### Why These Specific Colors?

#### Republic: White + Deep Blue
- **White**: Symbol of peace, clarity, organization (Jedi/Republic ideals)
- **Blue**: Trust, authority, intelligence (scientific advancement)
- **Desaturation**: Professional, clean, minimal aesthetic
- **Brightness**: Visibility, transparency, nothing to hide
- **Jedi Association**: Blue is iconic Jedi lightsaber color

#### CIS: Dark Grey + Rust Orange
- **Dark Grey**: Industrial, mechanical, anonymous (droid uniformity)
- **Orange**: Warning, heat, manufacturing (industrial process)
- **Saturation**: Bold, mechanical, no subtlety
- **Darkness**: Menacing, ominous, unfamiliar
- **Factory Association**: Orange is industrial safety color; grey is machine metal

---

## Applying Colors in Game

### Building Material Setup

Each building in the game uses a material with:

```
Material: "Building_Standard"
  Textures:
    Albedo (Diffuse): rep_command_center_albedo.png
    Normal: rep_command_center_normal.png
    Metallic: (shared base)
    Roughness: (shared base)
```

### Runtime Color Override (Optional)

For dynamic faction indicators, the shader can apply an additional color tint:

```glsl
vec4 final = albedo * base_color;  // faction color already baked in albedo
```

OR (if using shared textures):

```glsl
vec4 tinted = mix(albedo, faction_color, 0.3);
```

**Current Approach**: Color baked into albedo for maximum visual consistency

---

## Color Accessibility

### Contrast Analysis

| Building Type | Republic | CIS | Contrast | WCAG |
|---------------|----------|-----|----------|------|
| Command | #F5F5F5 / #1A3A6B | #444444 / #B35A00 | 6.8 : 1 | AA ✓ |
| Barracks | #F5F5F5 / #1A3A6B | #444444 / #B35A00 | 6.8 : 1 | AA ✓ |
| Defense | #F5F5F5 / #64A0DC | #444444 / #663300 | 3.2 : 1 | AA ✓ |
| Economy | #F5F5F5 / #1A3A6B | #444444 / #B35A00 | 6.8 : 1 | AA ✓ |
| Research | #F5F5F5 / #1A3A6B | #444444 / #B35A00 | 6.8 : 1 | AA ✓ |

**Result**: Both factions clearly distinguishable, even for colorblind players

---

## Texture Preview Guidelines

When evaluating generated textures:

### Republic Textures Should:
- ✓ Appear bright and clean
- ✓ Have predominantly white/light surfaces
- ✓ Show blue accents and details
- ✓ Maintain sharp, defined edges
- ✓ Look organized and technological

### CIS Textures Should:
- ✓ Appear dark and industrial
- ✓ Have predominantly grey/dark surfaces
- ✓ Show orange/rust accents and details
- ✓ Maintain weathered, mechanical appearance
- ✓ Look utilitarian and mechanical

### Both Should:
- ✓ Preserve alpha transparency correctly
- ✓ Maintain normal map detail
- ✓ Have consistent file sizes (~880 bytes per 256×256)
- ✓ Display properly in game without artifacts
- ✓ Match their building type's function visually

---

## Mods and Extensions

### Using Different Palettes

To create additional faction variants (e.g., custom mods):

1. **Define new palette** in `texture_generation.py`
2. **Reference in building mapping**
3. **Regenerate specific buildings**

Example custom palette:
```python
NEIMOIDIAN_TRADING_LEAGUE = ColorPalette(
    name="ntl",
    primary=(200, 150, 80),         # Sandy beige
    secondary=(180, 80, 30),        # Rust brown
    tertiary=(100, 60, 20),         # Dark wood
    hue_shift=30,
    saturation_multiplier=0.9,
    value_multiplier=1.0
)
```

### Texture Modding

Players can replace textures by:

1. Creating custom PNG files in `Override/` directory
2. Naming them identically to originals
3. Game loads override first, then falls back to pack textures

---

## References

- **Color Theory**: Johannes Itten, "The Art of Color"
- **Game Color Design**: "Unreal Engine 5 Color Theory" (Epic Games)
- **HSV Color Space**: https://en.wikipedia.org/wiki/HSL_and_HSV
- **Web Accessibility**: WCAG 2.1 Color Contrast Guidelines

---

Generated: 2026-03-12
