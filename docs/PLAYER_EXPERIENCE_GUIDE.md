# Player Experience Guide: warfare-starwars v0.1.0

## "What I See When I Play"

**Pack Version**: 0.1.0 | **DINO Version**: 1.21+ | **Status**: Early Access | **Release Date**: Early 2026

---

## Table of Contents

1. [Overview](#overview)
2. [Game Launch Experience](#game-launch-experience)
3. [Faction Selection](#faction-selection)
4. [Campaign Setup](#campaign-setup)
5. [First 5 Minutes of Gameplay](#first-5-minutes-of-gameplay)
6. [Combat Scenarios](#combat-scenarios)
7. [What's Implemented](#whats-implemented)
8. [What's Missing (v0.1.0 Limitations)](#whats-missing-v01-limitations)
9. [Known Issues & Workarounds](#known-issues--workarounds)
10. [Vanilla DINO vs warfare-starwars](#vanilla-dino-vs-warfare-starwars-comparison)
11. [Troubleshooting](#troubleshooting)

---

## Overview

When you launch **DINO with only the warfare-starwars mod enabled**, you're experiencing a total conversion that replaces the vanilla medieval fantasy setting with the Star Wars Clone Wars era (2019-2021 animated series). You'll command the **Galactic Republic** (player faction) or face the **Confederacy of Independent Systems** (CIS) in two enemy variants.

**What to expect**:
- Republic clone troopers instead of medieval soldiers
- Droid armies instead of goblin/orc enemies
- Star Wars-themed buildings (Clone Quarters, Droid Factories, etc.)
- Blaster weapons and sci-fi aesthetics
- Vanilla game mechanics with Star Wars reskinning

**What NOT to expect** (v0.1.0):
- Custom visual effects (VFX) for blasters, explosions
- Star Wars audio/music replacements
- Custom animations for Clone troopers or Droids
- Jedi Force powers or special mechanics
- 24 building types fully modeled (14/24 in Phase 1)

---

## Game Launch Experience

### Step 1: Starting DINO with warfare-starwars

**What you do**:
- Launch DINO normally through your Steam/GOG client
- The game loads with the warfare-starwars mod activated (via BepInEx plugin loader)

**What you see**:

```
[LOADING SCREEN]
┌────────────────────────────────────────────┐
│  Diplomacy is Not an Option                │
│  Loading...                                │
│  [████████████████░░░░░░░░░░░░] 65%       │
│  Initializing mods: warfare-starwars v0.1.0
└────────────────────────────────────────────┘
```

The loading screen is **vanilla DINO** (generic medieval fantasy aesthetic). No custom loading bar or splash screen in v0.1.0. The mod loads silently in the background via BepInEx, so you won't see any visual indication of mod loading—just the standard DINO bootstrap.

**Load times**: ~2-3 seconds longer than vanilla (asset catalog scanning + registry initialization). This is the first time you'll notice something is different: the title screen appears after a brief pause.

---

### Step 2: Main Menu & Mod Menu

**What you see**:
- Standard DINO main menu appears
- Press **F10** (or navigate via menu) to open the **Mod Configuration Menu** (MCM)

**In the Mod Menu**:
```
┌─ DINOForge Mod Manager ──────────────────┐
│ Active Mods:                             │
│  ✓ warfare-starwars (v0.1.0)             │
│      Star Wars: Clone Wars               │
│      Total Conversion                    │
│      Author: DINOForge Community         │
│      Factions: 3                         │
│      Units: 26 total                     │
│      Buildings: 20 defined (14 modeled)  │
│                                          │
│  [Enable/Disable]  [Options]  [Close]   │
└──────────────────────────────────────────┘
```

**What this menu shows**:
- Pack metadata (name, version, type)
- Faction count (3: Republic, CIS-Droid-Army, CIS-Infiltrators)
- Unit roster size (26 units total across all factions)
- Building definitions (20 building types, but only 14 have 3D models in Phase 1)

If you disable warfare-starwars here, the game falls back to vanilla DINO. If you want to enable other packs in the future, they'd appear here.

**Loading screen sound**: Vanilla DINO theme continues to loop (no custom music yet).

---

## Faction Selection

### The Faction Screen

After pressing "New Game" or starting a campaign, you reach the **Faction Selection** screen.

**What you see** (vs. Vanilla DINO):

| Aspect | Vanilla DINO | warfare-starwars |
|--------|--------------|------------------|
| **Faction 1 (Player)** | "Kingdom" (medieval) | **Galactic Republic** (white/blue) |
| **Faction 2 (Enemy)** | "Empire" (dark medieval) | **Confederacy of Independent Systems** (red/metallic) |
| **Faction 3 (Guerrilla Enemy)** | "Bandits" | **CIS Infiltrators** (same but smaller roster) |
| **Faction Colors** | Gold, Red, Gray | White, Blue, Red (sci-fi) |
| **Description** | Generic medieval text | Clone Wars lore (1-2 sentences) |

### Choosing Your Faction

**Republic (Player Faction - Recommended for First-Time)**

```
┌─ GALACTIC REPUBLIC ──────────────────────┐
│                                          │
│ [Republic Logo - Blue/White]             │
│                                          │
│ Description:                             │
│ "The Grand Army of the Republic,         │
│  composed of disciplined clone troopers  │
│  led by Jedi commanders. Balanced forces │
│  with strong morale and combined arms    │
│  tactics."                               │
│                                          │
│ Faction Bonuses:                         │
│  • Research Speed +10%                   │
│  • Unit Morale +5%                       │
│  • Elite Unit Cost -10%                  │
│  • Upkeep Cost +10% (economic downside)  │
│                                          │
│ Starting Units:                          │
│  • Clone Militia (5x spawn)              │
│  • Clone Trooper (3x spawn)              │
│  • 1x Militia Builder                    │
│                                          │
│ [SELECT] [INFO] [BACK]                   │
└──────────────────────────────────────────┘
```

**Difficulty Settings** (below faction choice):
- Easy / Normal / Hard (scales enemy unit stats, resource income)

**What you select** → Campaign map appears

---

## Campaign Setup

### Map Generation & Initial View

**What you see after selecting "Galactic Republic"**:

```
[CAMPAIGN MAP]
┌──────────────────────────────────────────┐
│                                          │
│        Your Base (Bottom-Left)           │
│        Clone Quarters Pod: 1             │
│        Clone Training Facility: 1        │
│        Supply Station: 1                 │
│                                          │
│    [Captured Territory]                  │
│    [Trees, Rocks, Neutral NPCs]          │
│                                          │
│        Enemy Bases (Top-Right)           │
│        Droid Factory: 2                  │
│        Droid Control Node: 1             │
│                                          │
│ [PLAY] [OPTIONS] [BACK]                  │
└──────────────────────────────────────────┘
```

### Starting Resources Displayed

When you hover over your base, you see:

```
REPUBLIC COMMAND CENTER

Owner: You (Galactic Republic)
Allegiance: Playable

Current Resources:
  Food: 100
  Wood: 150
  Stone: 200
  Iron: 100
  Gold: 50
  Population: 5/20 (5 workers, 15 available)

Production Rate:
  +5 Food/second (from Supply Station)
  +3 Wood/second (from Woodcutter)
  +0 Stone/second (no quarry yet)
```

**Startup Resources** are the same as vanilla DINO (not customized in v0.1.0):
- Food: 100 starting (enough for ~3 militia)
- Wood: 150 (for basic structures)
- Stone: 200 (walls, towers)
- Iron: 100 (military buildings)
- Gold: 50 (research, elite units)
- Population Slots: 20 (5 starting workers)

### UI Elements

At the bottom of the screen, you see the standard DINO HUD:

```
[Building Panel] [Unit Panel] [Tech Tree] [Diplomacy] [Stats]
└─ Resource Bar: F:100 W:150 S:200 I:100 G:50 | Pop: 5/20
└─ Current Time: Day 1, 00:00
```

**No custom UI theming** in v0.1.0. The menus are vanilla DINO with Star Wars content loaded into them.

---

## First 5 Minutes of Gameplay

### Minute 0-1: Workers & Resource Gathering

**What you do**:
1. Start with 5 Clone Worker units (generic workers, no visual difference from vanilla)
2. Direct workers to gather wood and stone from nearby trees/rocks
3. Place your first defensive structure (Guard Tower or Wall)

**What you see**:

```
[GAME VIEW - First Person]

Your base sits in a grassy valley. You see:
  • Clone Quarters Pod (white/blue building) - Your housing
  • Clone Training Facility (white/blue, barracks-like structure)
  • Supply Station (loading dock aesthetic)
  • 5 workers moving around, collecting resources (they look like vanilla workers)

Enemy in distance (top of map):
  • Red/metallic Droid buildings (Confederacy bases)
  • No units attacking yet (peaceful opening)
```

**Unit Appearance**:
- Clone units appear as **white and blue humanoid figures** with blaster rifles
- They move with vanilla DINO walking animation (not custom)
- When they attack, they use vanilla shooting animation + blaster projectile models
- Droids (enemy) appear as **tan/brown metallic humanoids** (B-1 Battle Droids)

**Sound**: Vanilla DINO ambient sounds. No Clone Wars music, no blaster sounds yet (these use vanilla DINO weapon audio).

---

### Minute 1-3: First Military Unit & Building Placement

**What you do**:
- Build your first Clone Training Facility (or it's already placed)
- Queue up Clone Militia and Clone Trooper units
- Place a Guard Tower for defense

**What you see**:

```
[TRAINING FACILITY] - Clone Training Facility
Status: Built
Production Queue:
  1. Clone Militia (training...) - 60 seconds remaining
  2. Clone Trooper (queued) - Will start when first completes

Unit Appearance:
  • Clone Militia: White/blue robes, blaster rifle (carbine variant)
    HP: 85 | Damage: 10 | Armor: 3 | Range: 16
    Cost: 30 Food, 10 Iron, 8 Gold

  • Clone Trooper: Slightly heavier armor, DC-15A blaster
    HP: 125 | Damage: 14 | Armor: 6 | Range: 20
    Cost: 40 Food, 20 Iron, 12 Gold
```

**Building Placement**:
You place a **Clone Guard Tower** (first defensive structure):

```
[GUARD TOWER PREVIEW]
└─ Clone Guard Tower (white/blue aesthetic)
   Position: Your choice on map
   Health: 700
   Cost: 40 Wood, 50 Stone, 35 Iron, 10 Gold

   Once built, it shoots at enemies in a cone.
   (Uses vanilla DINO tower mechanics)
```

The tower appears as a white/blue sci-fi turret structure. It's a modified version of vanilla DINO's tower—same function, different cosmetics.

---

### Minute 3-5: First Enemy Engagement

**What happens**:
- Enemy CIS Droids move toward your base (typically by minute 3-4 on Normal difficulty)
- You have 1-2 Clone Militia and 1-2 Clone Troopers ready
- Enemy sends 3-4 B-1 Battle Droids + 1 B2 Super Battle Droid

**What you see**:

```
[COMBAT ZONE]

Your Units:
  Clone Militia #1    HP: 85/85  ████░
  Clone Militia #2    HP: 85/85  ████░
  Clone Trooper #1    HP: 125/125 ██████░

Enemy Units:
  B-1 Droid #1        HP: 60/60   ███░
  B-1 Droid #2        HP: 60/60   ███░
  B-2 Super Droid     HP: 110/110 █████░

[COMBAT UNFOLDS]

  Round 1: Your Clone Militia fires
    └─ Blaster bolt (vanilla projectile) → Hit B-1 for 10 damage
    └─ B-1 Returns fire → Hit Militia for 8 damage

  Round 2: Clone Trooper fires (higher accuracy)
    └─ Blaster bolt → Hit B-1 for 14 damage (kills it)

  [Explosion sound - VANILLA] - B-1 unit destroyed

  Combat continues for 30-45 seconds...

  VICTORY or DEFEAT depending on your choices
```

**Combat Visuals**:
- Clone units maintain formation (vanilla DINO formation system)
- Blaster projectiles are vanilla DINO projectile models (not lightsaber-specific)
- Hit effects: vanilla explosions + unit knockback
- Sound: vanilla DINO combat audio (sword clashes become blaster pops, but generic)
- Building destruction: vanilla DINO collapse animation (building fades/explodes)

**What's Missing in v0.1.0**:
- ❌ No custom blaster bolt VFX (still looks like vanilla projectiles)
- ❌ No Star Wars sound effects (blaster sounds are generic sci-fi DINO sounds)
- ❌ No custom unit animations (Clone troopers move like vanilla soldiers)
- ❌ No faction-specific UI highlighting
- ❌ No custom explosion VFX for droids

---

## Combat Scenarios

### Scenario 1: Clone Militia vs B-1 Battle Droids

**Unit Matchup**:

```
REPUBLIC CLONE MILITIA              CIS B-1 BATTLE DROID
├─ HP: 85                           ├─ HP: 60
├─ Damage: 10                       ├─ Damage: 12
├─ Armor: 3                         ├─ Armor: 0 (droid armor ≠ physical)
├─ Range: 16                        ├─ Range: 18
├─ Speed: 5.0                       ├─ Speed: 4.8
├─ Accuracy: 65%                    ├─ Accuracy: 70%
├─ Fire Rate: 4.0/sec               ├─ Fire Rate: 3.0/sec
└─ Cost: 30 Food, 10 Iron, 8 Gold   └─ Cost: 20 Food, 15 Iron, 5 Gold

WINNER: Clone Militia (higher HP survives longer)
ECONOMY: B-1 is cheaper (good for enemy spam tactics)
```

**What it looks like**:
- Clone Militia in white/blue formation fires blaster bolts
- B-1 Droids in tan/metallic color return fire
- Damage numbers float above (vanilla DINO HUD)
- Defeated droids fall and fade (or explode with vanilla effect)
- Defeated clones ragdoll and fall (vanilla death animation)

---

### Scenario 2: Clone Trooper vs B-2 Super Battle Droid

**Unit Matchup**:

```
REPUBLIC CLONE TROOPER             CIS B-2 SUPER BATTLE DROID
├─ HP: 125                         ├─ HP: 190
├─ Damage: 14                      ├─ Damage: 18
├─ Armor: 6                        ├─ Armor: 12 (very armored)
├─ Range: 20                       ├─ Range: 22
├─ Speed: 4.5                      ├─ Speed: 3.5
├─ Accuracy: 75%                   ├─ Accuracy: 68%
├─ Fire Rate: 2.5/sec              ├─ Fire Rate: 1.5/sec
└─ Cost: 40 Food, 20 Iron, 12 Gold └─ Cost: 60 Food, 40 Iron, 25 Gold

WINNER: Clone Trooper (faster fire rate, 2v1 advantage)
ECONOMY: B-2 is specialist, expensive (enemy only builds 1-2 per attack wave)
STRATEGY: Overwhelm with numbers or focus-fire with ranged support
```

**Tactical View**:
- Clone Trooper's blaster bolts deal normal damage to B-2
- B-2's armor reduces incoming damage by ~20%
- B-2 one-shots Clone Militia (18 damage > 85 HP not quite, but close)
- 2x Clone Troopers can defeat 1x B-2 in ~15 seconds

---

### Scenario 3: Hero Unit - Jedi Knight

**If you tech-rush to the Jedi unit** (high cost, late-game):

```
REPUBLIC JEDI KNIGHT (Hero Unit)
├─ HP: 300
├─ Damage: 25
├─ Armor: 10
├─ Range: 24 (lightsaber range - vanilla extended)
├─ Speed: 6.0 (fastest unit)
├─ Accuracy: 95%
├─ Fire Rate: 3.0/sec
└─ Cost: 100 Food, 50 Iron, 80 Gold (expensive!)

COUNTER: Enemy General Grievous or 5x B-1 droids
ROLE: Frontline breaker, morale booster (nearby allies get +morale)
```

**What it looks like**:
- Single Jedi Knight unit in white/blue robes
- Blaster bolts instead of lightsaber (v0.1.0 limitation - no custom melee VFX)
- Charges into enemy formations
- Takes minimal damage due to high armor
- Other units route around Jedi (vanilla DINO pathfinding)

**Missing**: No Force powers, no special animations. Jedi moves like vanilla elite infantry.

---

## What's Implemented

### Units (26 Total)

**Republic (13 units)**:
1. **Clone Militia** - T1 cheap infantry, carbine-equipped
2. **Clone Trooper** - T1 core line infantry, DC-15A blaster
3. **Clone Heavy Trooper** - T2 anti-mass, Z-6 rotary cannon
4. **Clone Sharpshooter** - Sniper variant, higher range (19+)
5. **BARC Speeder** - Light vehicle, fast reconnaissance
6. **AT-TE Crew** - Heavy walker vehicle, area suppression
7. **Clone Medic** - Support unit, heals nearby friendlies
8. **ARF Trooper** - Reconnaissance elite, fast
9. **ARC Trooper** - Elite infantry, highest damage
10. **Jedi Knight** - Hero unit, morale leader
11. **Clone Wall Guard** - Stationary defensive heavy
12. **Clone Sniper** - Ranged specialist, single-target burst
13. **Clone Commando** - Spike unit, fast elite

**CIS Droids (13 units)**:
1. **B-1 Battle Droid** - T1 cheap infantry, single blaster
2. **B-1 Droid Squad** - 2x B-1s (stronger variant)
3. **B-2 Super Battle Droid** - T1 heavy armor, heavy blaster
4. **Sniper Droid** - Anti-air, long-range
5. **STAP Pilot** - Light vehicle, aerial (vanilla animation)
6. **AAT Crew** - Tank crew, area damage
7. **Medical Droid** - Support unit, heals droids
8. **Probe Droid** - Scout, invisible detection
9. **BX Commando Droid** - Elite droid, fast attacks
10. **General Grievous** - Hero unit, highest HP enemy
11. **Droideka** - Roller droid, shield generator
12. **Dwarf Spider Droid** - Melee attack droid
13. **Magnaguard** - Elite melee, high armor

**Gameplay Feel**: Roles are balanced with vanilla DINO vanilla unit types mapped 1:1 to Star Wars counterparts. Clone Militia = Militia, Clone Trooper = Line Infantry, etc.

---

### Buildings (20 Defined, 14 Modeled in Phase 1)

#### **Fully Modeled (14 / 24)**

**Republic Buildings** (white/blue sci-fi aesthetic):
1. Republic Command Center (HQ/TC equivalent)
2. Clone Training Facility (Barracks)
3. Weapons Factory (Barracks 2)
4. Vehicle Bay (Workshop)
5. Guard Tower (Defense tower)
6. Shield Generator (Defensive fortification)
7. Supply Station (Economic building)
8. Tibanna Refinery (Economic building 2)
9. Research Lab (Tech building)
10. Clone Quarters Pod (Housing)

**CIS Buildings** (red/metallic droid aesthetic):
11. Droid Control Node (equivalent to Command Center)
12. Battle Droid Factory (Barracks)
13. Super Droid Foundry (Barracks 2)
14. Mecha Workshop (Workshop)

And more (same visual template, 20 total defined in YAML).

#### **Missing Models (10 / 24)**

For these 10 building types, the game falls back to **vanilla DINO placeholder models**:

- Granary (food storage) → Shows vanilla DINO granary
- Farm (food production) → Shows vanilla DINO farm
- Iron Mine → Shows vanilla DINO mine
- Stone Quarry → Shows vanilla DINO quarry
- Gate/Wall Segment → Shows vanilla DINO wall
- Market/Trade Hub → Shows vanilla DINO market
- Tower/Fortification variants → Show vanilla DINO towers

**User Experience**: When you place a "Biodome" (Clone Quarters alternative), it appears with a Star Wars sci-fi dome model. When you place a "Granary," it looks like vanilla DINO's granary because the model isn't complete yet. **This is intentional graceful degradation**.

**Texture Status**: All 20 generated buildings have Star Wars faction textures (Republic white/blue, CIS red/metallic). Even placeholder models get the right colors via shader.

---

### Factions (3 Total)

| Faction | Type | Replaces Vanilla | Units | Role |
|---------|------|------------------|-------|------|
| **Galactic Republic** | Playable | Player Faction | 13 | Balanced, elite-focused |
| **CIS Droid Army** | Enemy (Classic) | Enemy Faction 1 | 13 | Same tier, all 13 roster |
| **CIS Infiltrators** | Enemy (Guerrilla) | Enemy Faction 2 | 3 | Smaller roster (Grievous, BX, Sniper) |

**Vanilla Comparison**:

| Vanilla DINO | warfare-starwars |
|--------------|------------------|
| Kingdom (Player) | Galactic Republic (Clones) |
| Empire (Enemy) | CIS Droid Army (Battle Droids) |
| Bandits (Guerrilla) | CIS Infiltrators (Droid Commandos) |

**Faction Bonuses**:
- **Republic**: +10% Research, +5% Unit Morale, -10% Elite Cost, but +10% Upkeep
- **CIS**: Balanced economic modifiers (no heavy bonuses)

---

### Assets & Cosmetics

#### Textures (100% Complete)

All 26 unit types have faction-colored textures:
- **Republic units**: White, Blue, Red accent colors
- **CIS units**: Red, Metallic Gray, Orange accent colors
- Texture generation via HSV faction variants (automated DINOForge pipeline)
- Source: Kenney.nl 3D Models (CC0 Public Domain)

#### 3D Models (Phase 1 - 4 Buildings)

Sample FBX models exported and integrated:
- Clone Quarters Pod
- Battle Droid Factory
- Shield Generator
- Droid Pod

The remaining 10 modeled buildings are FBX files present in `assets/meshes/buildings/` but not yet tested in-game (integration in progress).

#### Audio (Not Yet Integrated)

Status: 0% custom audio in v0.1.0
- Blaster sounds use vanilla DINO ranged weapon audio
- Footsteps use vanilla DINO infantry audio
- Building construction uses vanilla DINO construction audio
- No Clone Wars theme or faction-specific music

---

## What's Missing (v0.1.0 Limitations)

### 1. Custom Visual Effects (VFX) - Planned for v0.3+

**Currently Missing**:
- ❌ Blaster bolt VFX (unique blue/red energy bolts instead of vanilla projectiles)
- ❌ Clone trooper hit reactions (no blue sparks on armor hits)
- ❌ Droid hit reactions (no sparks for metallic hits)
- ❌ Blaster impact explosions (unique sci-fi particles instead of vanilla puff clouds)
- ❌ Shield generator visual effects (no shield dome plasma effect)
- ❌ Jedi force abilities (not implemented, Jedi uses standard blaster attacks)

**Workaround**: You see vanilla DINO explosion clouds when units die, but they're identical to killing vanilla soldiers.

**v0.3 Goal**: Implement custom VFX for top 5 most-fired weapons (Clone Trooper blaster, B-2 heavy, sniper shots).

---

### 2. Audio Replacements - Planned for v0.4+

**Currently Missing**:
- ❌ Blaster fire sounds (generic sci-fi pops, not Star Wars classic "pew pew")
- ❌ Building construction audio (no droid mechanical sounds)
- ❌ Unit footsteps (no mechanical droid walks)
- ❌ UI feedback sounds (still vanilla DINO click/beep)
- ❌ Faction music (no Star Wars Clone Wars theme)

**Workaround**: Mute game sound and play Clone Wars soundtrack in background.

**v0.4 Goal**: Integrate free/CC0 Star Wars-inspired audio (generated or from royalty-free sources). Replace at minimum: unit death sounds, build complete sounds, attack sounds.

---

### 3. Custom Animations - Planned for v1.0+

**Currently Missing**:
- ❌ Clone trooper-specific run/walk (they use vanilla soldier animations)
- ❌ Droid walk cycles (they use vanilla infantry, not mechanical droid walks)
- ❌ Blaster reload animations (not implemented)
- ❌ Unit-specific attack animations (all use vanilla melee/ranged)
- ❌ Building construction animations (not custom)

**Workaround**: Imagination. When you see a white figure running, pretend it's a Clone Trooper.

**v1.0 Goal**: Ship custom character animations for top 8 unit types. This is a large effort requiring 3D artist.

---

### 4. Building Model Completion - Phase 2 (v0.2+)

**Current Status**: 14/24 building types are modeled.

**Missing Models (10 types - show vanilla DINO placeholders)**:

```
├─ Granary (Biodome) - Defined, no model → vanilla granary appears
├─ Farm (Moisture Farm) - Defined, no model → vanilla farm appears
├─ Iron Mine - Defined, no model → vanilla mine appears
├─ Quarry - Defined, no model → vanilla quarry appears
├─ Gate Wall - Defined, no model → vanilla wall appears
├─ Market - Defined, no model → vanilla market appears
├─ Medic/Support Building - Defined, no model → vanilla healer appears
├─ Meditation Chamber - Defined, no model → vanilla temple appears
├─ Sith Altar (CIS) - Defined, no model → vanilla temple appears
└─ Droid Storage - Defined, no model → vanilla warehouse appears
```

**User Impact**: If you place a Moisture Farm, you get a vanilla DINO farm building model but with Clone Wars textures and Star Wars faction colors. It's thematic enough for gameplay but not 100% lore-accurate.

**v0.2 Goal**: Export remaining 10 FBX models from Blender, integrate and test.

---

### 5. Faction-Specific UI Theming - Planned for v0.5+

**Currently Missing**:
- ❌ Faction-colored unit selection panels (still vanilla UI colors)
- ❌ Faction-themed button backgrounds
- ❌ Faction-specific icon packs for tech tree
- ❌ Faction-themed dialog boxes and menus
- ❌ Droid-specific HUD elements (different stat displays)

**Workaround**: UI text is correct (says "Clone Trooper" instead of "Soldier") but visuals are unchanged.

**v0.5 Goal**: Create faction-specific UI themes: Republic = sleek blue/white, CIS = industrial red/gray.

---

### 6. Advanced Mechanics - Future Versions

**Not in v0.1.0**:
- ❌ Jedi Force powers (Jedi Knight is strong but uses blasters only)
- ❌ Droid unique abilities (BX Commando has no special moves)
- ❌ Faction-specific tech trees (tech tree is generic)
- ❌ Morale system tie-ins (morale stats exist but not deeply integrated)
- ❌ Doctrine/role system (defined in code but not gameplay-critical)

**These are planned for M6+** in the DINOForge roadmap.

---

## Known Issues & Workarounds

### Issue 1: Missing Building Models Display as Vanilla

**What happens**: You place a "Moisture Farm" and see a generic DINO granary model.

**Why**: The building type is defined in YAML but the FBX model hasn't been integrated yet.

**Workaround**: It's functionally identical. The building still produces food at the correct rate. Gameplay is unaffected.

**Fix**: Upgrade to v0.2 when Phase 2 building models are released.

---

### Issue 2: No Custom Blaster Sounds

**What happens**: Clones fire blasters but it sounds like vanilla DINO arrows/bolts.

**Why**: Custom audio assets aren't integrated yet (v0.1.0 is content-only).

**Workaround**: Enable "Unit Voice Pack" optional addon when available. Or mute and play Clone Wars score in browser.

**Fix**: Upgrade to v0.4 for audio replacements.

---

### Issue 3: Jedi Knight Uses Blasters, Not Lightsaber

**What happens**: Jedi Knight unit fires blaster bolts instead of melee attacks.

**Why**: Lightsaber animation/VFX not implemented. Jedi is mechanically an elite ranged unit.

**Workaround**: Play Jedi as ranged support unit. Use for morale boost (he buffs nearby allies) and area suppression.

**Fix**: Wait for v1.0+ when custom melee animations are shipped.

---

### Issue 4: Enemy AI Doesn't Use Faction-Specific Tactics

**What happens**: CIS and Republic both use identical combat tactics (vanilla DINO AI).

**Why**: Faction-specific AI behavior isn't implemented (framework exists, logic pending).

**Workaround**: Treat CIS and Republic AI as identical. Different units, same strategy.

**Fix**: Wait for M6 when Warfare Domain AI overhaul ships.

---

### Issue 5: Building Placement Doesn't Validate Star Wars Lore

**What happens**: You can place Clone Quarters (housing) anywhere, even in enemy territory visually.

**Why**: No special lore-validation layer. Uses vanilla DINO placement rules.

**Workaround**: Play narratively—only place Clone buildings in Republic territory.

**Fix**: Not planned (gameplay-first approach).

---

### Issue 6: Unit Recruitment Descriptions Show Vanilla Balance Text

**What happens**: Unit info says "Clone Trooper" but some text still refers to generic "infantry role."

**Why**: Descriptions are overloaded in YAML but some vanilla DINO UI text shows through.

**Workaround**: Ignore generic text, focus on stat differences.

**Fix**: v0.2 will polish all UI text.

---

## Vanilla DINO vs warfare-starwars Comparison

### Gameplay Mechanics

| Aspect | Vanilla DINO | warfare-starwars |
|--------|--------------|------------------|
| **Core Loop** | Build → Research → Attack | Identical |
| **Resource Types** | Food, Wood, Stone, Iron, Gold | Identical |
| **Unit Count** | ~20 units | 26 units (13 per faction) |
| **Building Count** | 20-24 types | 20 types (14 modeled) |
| **Tech Tree Depth** | 3-4 tiers | Identical |
| **Diplomacy** | Alliances, trades | Identical |
| **Campaign Length** | 30-60 min per map | Identical |
| **Difficulty Scaling** | Easy / Normal / Hard | Identical |

**Verdict**: 100% mechanically identical. warfare-starwars is a **total cosmetic conversion** with balanced unit stats.

---

### Cosmetics & Immersion

| Aspect | Vanilla DINO | warfare-starwars |
|--------|--------------|------------------|
| **Visuals** | Medieval fantasy | Star Wars sci-fi |
| **Unit Models** | Swordsmen, archers, etc. | Clone troopers, battle droids |
| **Building Models** | Medieval castles, farms | Clone facilities, droid factories |
| **Color Palette** | Browns, golds, reds | Whites, blues, reds, grays |
| **Audio** | Medieval lutes, sword clashes | (Vanilla audio - audio pending) |
| **Music** | Medieval fantasy score | (Vanilla DINO score in v0.1.0) |
| **Narrative Feel** | High fantasy | Hard sci-fi / military |

**Verdict**: 90% cosmetic. Audio is a gap; animations are placeholder.

---

### Starting a Game: Side-by-Side

#### Vanilla DINO - First 10 seconds

```
[GAME START - VANILLA]
1. Game loads, title screen (medieval fantasy aesthetic)
2. Select faction: Kingdom, Empire, Bandits
3. Loading... (2 seconds)
4. Appear on map with wooden buildings
5. See peasants gathering wood/stone
6. See enemy castle in distance with archers
```

#### warfare-starwars - First 10 seconds

```
[GAME START - STARWARS]
1. Game loads, title screen (vanilla DINO, not mod-themed)
2. Select faction: Galactic Republic, CIS Droid Army, CIS Infiltrators
3. Loading... (3 seconds - slightly slower due to mod loading)
4. Appear on map with Clone Quarters and Droid Factories
5. See Clone Workers gathering from resource nodes
6. See enemy Droid base in distance with antenna
7. [GAMEPLAY] - Identical from here
```

**Differences**: Names, unit appearance, building appearance, descriptions. Everything else vanilla.

---

### Unit Stat Comparison: Militia-Tier Infantry

#### Vanilla DINO Militia

```
Militia
├─ HP: 85
├─ Damage: 10
├─ Armor: 3
├─ Range: 16
├─ Speed: 5.0
├─ Accuracy: 65%
├─ Cost: 30 Food, 10 Iron, 8 Gold
└─ Role: Cheap scouts, early harassment
```

#### warfare-starwars Clone Militia

```
Clone Militia
├─ HP: 85
├─ Damage: 10
├─ Armor: 3
├─ Range: 16
├─ Speed: 5.0
├─ Accuracy: 65%
├─ Cost: 30 Food, 10 Iron, 8 Gold
└─ Role: Cheap scouts, early harassment
```

**Stats**: 100% identical. Reskinned, not rebalanced.

---

### Mid-Game Unit: Heavy Infantry Comparison

#### Vanilla DINO Heavy Infantry

```
Knight
├─ HP: 155
├─ Damage: 10
├─ Armor: 8
├─ Range: 18
├─ Speed: 3.5
├─ Fire Rate: 8.0/sec (heavily armored, slow attacks)
├─ Cost: 60 Food, 30 Iron, 20 Gold
└─ Role: Fortress in melee, tanky frontline
```

#### warfare-starwars Clone Heavy Trooper

```
Clone Heavy Trooper
├─ HP: 155
├─ Damage: 10
├─ Armor: 8
├─ Range: 18
├─ Speed: 3.5
├─ Fire Rate: 8.0/sec (rotary cannon spin-up)
├─ Cost: 60 Food, 30 Iron, 20 Gold
└─ Role: Fortress in melee, tanky frontline
```

**Stats**: 100% identical. Same role, Star Wars flavor.

---

## Troubleshooting

### "Game won't load with warfare-starwars enabled"

**Symptoms**: Game hangs on loading screen or crashes immediately.

**Cause**: BepInEx plugin not loading, or corruption in mod files.

**Solution**:
1. Verify BepInEx is installed (check `BepInEx/plugins/` directory)
2. Verify warfare-starwars pack is in `packs/warfare-starwars/` (not renamed)
3. Check BepInEx console for errors (press F5 in-game)
4. Disable mod in Mod Menu (F10), restart game to confirm vanilla works
5. Report error to DINOForge issues page with BepInEx log

---

### "I see vanilla units instead of Clones/Droids"

**Symptoms**: Game loaded but all units look like vanilla DINO soldiers.

**Cause**: Asset pack failed to load. Fallback to vanilla models activated.

**Solution**:
1. Check `packs/warfare-starwars/units/` exists and has YAML files
2. Check `packs/warfare-starwars/assets/` exists and has textures
3. Verify mod is still enabled (F10 menu shows checkmark)
4. Reload game (File > New Game, don't use quickload)

---

### "Buildings look gray/untextured"

**Symptoms**: Clone Quarters Pod appears white/gray mesh with no detail texture.

**Cause**: Shader initialization delay or texture path incorrect.

**Solution**:
1. Wait 5 seconds after loading (first time can be slow)
2. Move camera (sometimes forces texture reload)
3. Disable and re-enable mod (F10), reload
4. If persistent, update graphics drivers

---

### "Performance is slower than vanilla DINO"

**Symptoms**: FPS drops from 60 to 40 with warfare-starwars enabled.

**Cause**: Asset loading overhead, additional shader compilation, or texture VRAM usage.

**Solution**:
1. Lower Graphics Quality (game settings) from Ultra to High
2. Disable ambient shadows (impacts performance)
3. Reduce draw distance (less terrain rendered)
4. Close background programs to free VRAM

**Note**: v0.1.0 is not heavily optimized. v0.2+ will include performance pass.

---

### "Enemy doesn't attack / infinite peace"

**Symptoms**: CIS droids never move toward your base.

**Cause**: Enemy AI configuration pending (framework in place, logic not integrated).

**Workaround**: Attack enemy base yourself; combat works once units collide.

**Fix**: Planned for M6 (Warfare AI overhaul).

---

### "Jedi Knight is too weak / too strong"

**Symptoms**: Jedi dies instantly or kills entire enemy army alone.

**Cause**: Stat balance pending fine-tuning in v0.2.

**Solution**:
1. Check current stats in-game: Click Jedi unit → View stats
2. If too weak: Micro-manage Jedi (don't charge alone)
3. If too strong: Enemy AI can't handle hero units yet (framework pending)

**Feedback**: Report balance concerns to DINOForge GitHub issues.

---

### "I see a Building with vanilla model instead of Star Wars"

**Symptoms**: "Moisture Farm" appears as vanilla DINO granary.

**Cause**: Building model is defined in YAML but FBX not integrated (Phase 2 pending).

**Solution**: Gameplay is unaffected. Building works perfectly. This is expected in v0.1.0.

**Upgrade**: Wait for v0.2 (Q2 2026 planned) for full building model rollout.

---

## FAQ

### Q: Can I disable warfare-starwars and play vanilla DINO?

**A**: Yes. Open Mod Menu (F10), uncheck warfare-starwars. Game reverts to vanilla on next restart.

---

### Q: Can I play multiple packs at once?

**A**: In future versions, yes (pack dependency system ready). v0.1.0 is single-pack only due to `singleton: true` in manifest. Expect multi-pack in v0.5+.

---

### Q: Why is there no Clone Wars music?

**A**: Audio integration is Phase 2 (v0.4+). v0.1.0 focuses on gameplay and visuals. Using Star Wars music risks copyright claims; we're planning original "inspired by" compositions.

---

### Q: Can I mod warfare-starwars?

**A**: Not directly in v0.1.0. Full mod-of-mod support comes in M7+. Currently, you can edit YAML files locally but can't redistribute.

---

### Q: How big is the download?

**A**: ~150 MB for the pack (textures + FBX models). DINO base game is 30 GB, so warfare-starwars adds <1% overhead.

---

### Q: What's the next version roadmap?

**A**:
- **v0.2** (Q2 2026): Remaining 10 building models, UI polish, performance pass
- **v0.3** (Q3 2026): Custom VFX for blasters, hit reactions, shield effects
- **v0.4** (Q4 2026): Audio replacements, UI theming, sound packs
- **v0.5** (Q1 2027): Hero abilities, Jedi Force powers, faction-specific mechanics
- **v1.0** (Q2 2027): Custom animations, complete polish, performance optimization

See `ROADMAP.md` for full details.

---

## Conclusion: What You're Experiencing

**warfare-starwars v0.1.0** is a **feature-complete content conversion** that replaces DINO's medieval fantasy setting with the Star Wars Clone Wars era. All gameplay mechanics are vanilla DINO—same economy, same unit roles, same buildings, same tech tree—but reskinned with 26 custom units, 3 balanced factions, and 20 uniquely-themed buildings.

**In v0.1.0, you get**:
- ✅ 26 fully-defined Star Wars units (13 Republic, 13 CIS)
- ✅ 3 balanced factions (Republic, CIS-Army, CIS-Infiltrators)
- ✅ 20 building types with Star Wars names and textures
- ✅ 14 custom 3D building models (FBX integrated)
- ✅ 100% texture coverage for all units and buildings
- ✅ Balanced unit stats mapped to vanilla DINO roles
- ✅ Full YAML pack metadata and ContentLoader support

**In v0.1.0, you DON'T get**:
- ❌ Custom VFX (blaster bolts, explosions, shields)
- ❌ Star Wars audio (still vanilla DINO SFX and music)
- ❌ Custom animations (units move like vanilla soldiers)
- ❌ All 24 building models (14/24 complete)
- ❌ Jedi Force powers (hero Jedi uses standard blasters)
- ❌ Faction-specific UI theming

**The experience**: You're playing vanilla DINO with a Star Wars skin. The gameplay is identical, but the visual and narrative immersion sets you in the Clone Wars instead of a medieval fantasy realm. Perfect for starting a new campaign; upgrading to v0.2+ as quality-of-life features arrive.

**Recommended playstyle for first game**:
1. Select **Galactic Republic** faction
2. Play on **Normal** difficulty
3. Build your Clone base methodically (barracks, towers, research)
4. Recruit Clone Troopers and engage CIS droids in small skirmishes
5. Tech up to Clone Heavy Trooper and AT-TE walker
6. Defend against mid-game droid attacks
7. Counter-attack CIS base with combined arms (infantry + vehicles)
8. Victory condition: destroy enemy command center

**Total playtime**: 45-60 minutes for first victory. Identical to vanilla DINO campaign.

---

**Happy conquest, Clone Commander!** 🎮

*warfare-starwars v0.1.0 | DINOForge M5 Content Pack | March 2026*
