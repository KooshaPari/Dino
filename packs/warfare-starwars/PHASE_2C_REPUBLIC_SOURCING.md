# Phase 2C-D-A: Missing Republic Units Sourcing Manifest

**Date**: 2026-03-13
**Agent**: Agent-15 (Haiku 4.5)
**Objective**: Identify and source missing Republic units via Sketchfab API to achieve vanilla-dino parity (72 total units)

## Current State

### Existing Republic Units (14/72)
- **MilitiaLight**: Clone Militia
- **CoreLineInfantry**: Clone Trooper
- **HeavyInfantry**: Clone Heavy Trooper
- **Skirmisher**: Clone Sharpshooter, Clone Sniper
- **Recon**: ARF Trooper
- **SupportEngineer**: Clone Medic
- **EliteLineInfantry**: ARC Trooper
- **ShieldedElite**: Clone Commando
- **StaticMG**: Clone Wall Guard
- **FastVehicle**: BARC Speeder
- **MainBattleVehicle**: AT-TE Crew
- **AirstrikeProxy**: V-19 Torrent Starfighter
- **HeroCommander**: Jedi Knight

### Vanilla-Dino Parity Target
```
Target: 72 total units across CIS + Republic

Unit Class Distribution (Vanilla-Dino):
  AntiArmor:           7 units  [MISSING: 0/7 for Republic]
  Artillery:           5 units  [MISSING: 0/5 for Republic]
  CoreLineInfantry:   11 units  [HAVE: 1/11]
  EliteLineInfantry:   4 units  [HAVE: 1/4]
  FastVehicle:         7 units  [HAVE: 1/7]
  HeavyInfantry:       7 units  [HAVE: 1/7]
  HeavySiege:          5 units  [MISSING: 0/5 for Republic]
  MilitiaLight:        7 units  [HAVE: 1/7]
  ShockMelee:          7 units  [HAVE: 0/7]
  Skirmisher:          5 units  [HAVE: 2/5]
  WalkerHeavy:         7 units  [MISSING: 0/7 for Republic]
```

### Republic Unit Gap Analysis (58 missing units)

**Priority Gaps** (classes with ZERO coverage):
1. **AntiArmor** (7 units needed) - Anti-armor specialists, armor-piercing troops
2. **Artillery** (5 units needed) - Clone artillery, cannon platforms
3. **HeavySiege** (5 units needed) - Advanced siege units
4. **WalkerHeavy** (7 units needed) - Heavy walker variants, AT-TE alternatives
5. **ShockMelee** (7 units needed) - Melee specialists, close combat elite

**Secondary Gaps** (classes with <3 units, need 3+ more):
6. **CoreLineInfantry** (10 more needed) - Clone trooper variants
7. **HeavyInfantry** (6 more needed) - Heavy weapons specialists
8. **MilitiaLight** (6 more needed) - Basic clone variants, cheaper units
9. **FastVehicle** (6 more needed) - Speeder variants, reconnaissance
10. **EliteLineInfantry** (3 more needed) - Elite clone variants
11. **Skirmisher** (3 more needed) - Specialized ranged units

---

## Sourcing Plan by Unit Class

### Priority 1: AntiArmor (7 units)
**Role**: Anti-armor specialist, armor-piercing, vehicle killer
**Republic Candidates**: Clone AT-specialists, commando anti-armor, heavy weapons variants

#### Unit 1.1: Clone AT-Specialist (AntiArmor Light)
- **Description**: Clone trooper with anti-armor loadout, portable launcher
- **Search Strategy**: "clone anti-armor", "clone launcher", "AT specialist"
- **Sketchfab Queries**:
  - Query 1: `clone+trooper+launcher` → Clone with shoulder-mounted AT launcher
  - Query 2: `clone+anti+armor+specialist` → Armored clone with AT capability
  - Query 3: `clone+heavy+launcher` → Clone soldier with heavy launcher
- **Model Type**: Armored humanoid (clone trooper base) with launcher/weapon platform
- **Expected Tags**: #clone #trooper #launcher #starwars
- **License Preference**: CC0, CC-BY, CC-BY-SA (must allow commercial)
- **Status**: PENDING SEARCH

#### Unit 1.2: Clone Commando AT (AntiArmor Medium)
- **Description**: Advanced commando with anti-armor focus, thermal detonator platform
- **Search Strategy**: "clone commando anti-armor", "commando specialist", "heavy commando"
- **Sketchfab Queries**:
  - Query 1: `clone+commando+armor+breaker` → Tactical unit with AT capability
  - Query 2: `commando+specialist+heavy+gear` → Specialist with advanced loadout
  - Query 3: `republic+soldier+anti+vehicle` → Republic specialist unit
- **Model Type**: Armored humanoid with tactical gear and heavy weapon
- **Status**: PENDING SEARCH

#### Unit 1.3: Heavy AT Unit (AntiArmor Heavy)
- **Description**: Purpose-built anti-armor unit with guided missile system
- **Search Strategy**: "clone missile trooper", "republic anti-tank", "heavy missile"
- **Sketchfab Queries**:
  - Query 1: `missile+platform+clone` → Clone-based missile unit
  - Query 2: `republic+anti+tank+unit` → Republic AT unit
  - Query 3: `heavy+weapons+specialist+armor` → Heavy unit with AT focus
- **Model Type**: Humanoid or walker-based with missile/launcher platform
- **Status**: PENDING SEARCH

#### Units 1.4-1.7: AntiArmor Variants (4 more)
- Additional variants based on search results
- Can include: Portable AT launcher teams, EMP specialists, advanced commando variants

---

### Priority 2: Artillery (5 units)
**Role**: Long-range fire support, cannon platform, area denial
**Republic Candidates**: Clone gunners, cannon platforms, mortar specialists

#### Unit 2.1: Clone Gunner Platform (Artillery Light)
- **Description**: Clone cannon crew with portable blaster cannon platform
- **Search Strategy**: "clone gunner", "clone cannon crew", "portable artillery"
- **Sketchfab Queries**:
  - Query 1: `clone+gunner+cannon` → Clone with mounted cannon
  - Query 2: `artillery+platform+clone` → Mobile artillery piece
  - Query 3: `heavy+blaster+position` → Fortified blaster emplacement
- **Model Type**: Humanoid or static platform with large cannon/blaster
- **Expected Tags**: #clone #cannon #starwars #artillery
- **Status**: PENDING SEARCH

#### Unit 2.2: Clone Mortar Specialist (Artillery Medium)
- **Description**: Clone with heavy mortar or grenade launcher, area denial
- **Search Strategy**: "clone mortar", "grenade launcher clone", "heavy ordinance"
- **Sketchfab Queries**:
  - Query 1: `clone+mortar+specialist` → Clone with mortar weapon
  - Query 2: `grenade+launcher+trooper` → Heavy launcher variant
  - Query 3: `republic+artillery+crew` → Republic artillery unit
- **Model Type**: Armored humanoid with heavy launcher/mortar
- **Status**: PENDING SEARCH

#### Unit 2.3: Triple Cannon Emplacement (Artillery Heavy)
- **Description**: Static artillery position with triple blaster cannons
- **Search Strategy**: "triple cannon turret", "blaster nest", "cannon emplacement"
- **Sketchfab Queries**:
  - Query 1: `turret+triple+cannon` → Multi-barrel turret
  - Query 2: `artillery+nest+starwars` → Fortified position
  - Query 3: `heavy+blaster+platform` → Heavy weapons platform
- **Model Type**: Static or semi-mobile platform with multiple cannon mounts
- **Status**: PENDING SEARCH

#### Units 2.4-2.5: Artillery Variants (2 more)
- Additional variants based on search results
- Can include: Mortar variants, ion cannon operators, flamethrower specialists

---

### Priority 3: HeavySiege (5 units)
**Role**: Heavy assault, structure destruction, concentrated firepower
**Republic Candidates**: Clone walker variants, walrus tank alternatives, mega-units

#### Unit 3.1: AT-RT Light Walker (HeavySiege Light)
- **Description**: Scout walker with medium firepower, AT-RT style
- **Search Strategy**: "AT-RT walker", "scout walker clone", "light walker"
- **Sketchfab Queries**:
  - Query 1: `atat+walker+scout` → AT-RT style walker
  - Query 2: `walker+light+clone+republic` → Light walker for Republic
  - Query 3: `two+leg+walker+scifi` → Bipedal walker design
- **Model Type**: 2-legged humanoid walker, light-armored
- **Status**: PENDING SEARCH

#### Unit 3.2: Heavy Clone Assault Walker (HeavySiege Medium)
- **Description**: 4-legged heavy siege walker, AT-TE alternative
- **Search Strategy**: "heavy clone walker", "siege walker republic", "walrus tank"
- **Sketchfab Queries**:
  - Query 1: `clone+heavy+walker+siege` → Heavy clone walker
  - Query 2: `quad+walker+republic+tank` → 4-legged heavy walker
  - Query 3: `assault+walker+republic+army` → Republic assault platform
- **Model Type**: 4-legged mechanical walker with heavy turret/weapons
- **Status**: PENDING SEARCH

#### Unit 3.3: Mega Clone Assault Unit (HeavySiege Heavy)
- **Description**: Largest non-hero clone unit, devastating firepower
- **Search Strategy**: "clone mega unit", "giant clone", "super soldier clone"
- **Sketchfab Queries**:
  - Query 1: `clone+mega+soldier+heavy` → Oversized clone warrior
  - Query 2: `giant+clone+trooper+armor` → Enlarged clone unit
  - Query 3: `republic+siege+unit+heavy` → Republic siege specialist
- **Model Type**: Heavily armored giant humanoid or walker-form heavy unit
- **Status**: PENDING SEARCH

#### Units 3.4-3.5: HeavySiege Variants (2 more)
- Additional variants based on search results
- Can include: Ion cannon variants, plasma weapon specialists

---

### Priority 4: WalkerHeavy (7 units)
**Role**: Heavy ground combat, multi-legged tank equivalent, tactical support
**Republic Candidates**: AT-TE variants, AT-AP walkers, heavy support walkers

#### Unit 4.1: AT-TE Standard (WalkerHeavy Light)
- **Description**: Standard six-legged tactical walker, Republic backbone
- **Search Strategy**: "AT-TE walker", "six legged walker clone", "republic walker"
- **Sketchfab Queries**:
  - Query 1: `at+te+walker+clone` → Canonical AT-TE model
  - Query 2: `six+leg+walker+republic+army` → 6-legged walker
  - Query 3: `heavy+walker+starwars+clone` → Clone Wars walker
- **Model Type**: 6-legged tactical walker with rotating head cannon, crew hatches
- **Expected Tags**: #walker #clone #starwars #atte
- **Status**: PENDING SEARCH

#### Unit 4.2: AT-TE Heavy Variant (WalkerHeavy Medium)
- **Description**: Upgraded AT-TE with enhanced armor and firepower
- **Search Strategy**: "AT-TE heavy", "AT-TE variant", "republic heavy walker"
- **Sketchfab Queries**:
  - Query 1: `at+te+heavy+upgrade+armor` → Upgraded AT-TE
  - Query 2: `at+te+variant+cannon` → AT-TE with enhanced weapons
  - Query 3: `walker+heavy+republic+tactical` → Heavy tactical walker
- **Model Type**: AT-TE-derived with heavy armor plates and dual cannon mount
- **Status**: PENDING SEARCH

#### Unit 4.3: AT-AP Walker (WalkerHeavy Medium)
- **Description**: Anti-Personnel walker variant, specialized for infantry support
- **Search Strategy**: "AT-AP walker", "clone anti-personnel walker", "republic AT-AP"
- **Sketchfab Queries**:
  - Query 1: `at+ap+walker+clone` → AT-AP cannon platform
  - Query 2: `walker+anti+personnel+republic` → AP-focused walker
  - Query 3: `cannon+platform+walker+clone` → Cannon-mounted walker
- **Model Type**: 6-legged walker with prominent front-facing cannon
- **Status**: PENDING SEARCH

#### Unit 4.4: Clone Assault Walker (WalkerHeavy Heavy)
- **Description**: Advanced assault walker with integrated shield generator
- **Search Strategy**: "clone assault walker", "republic assault platform", "heavy tactical walker"
- **Sketchfab Queries**:
  - Query 1: `assault+walker+clone+heavy` → Heavy assault platform
  - Query 2: `republic+walker+shield+generator` → Walker with defensive tech
  - Query 3: `walker+heavy+republic+assault` → Assault-class walker
- **Model Type**: Multi-legged walker (6+) with heavy armor and prominent shield housing
- **Status**: PENDING SEARCH

#### Units 4.5-4.7: WalkerHeavy Variants (3 more)
- Additional variants based on search results
- Can include: Support walker, command walker, transport walker variants

---

### Priority 5: ShockMelee (7 units)
**Role**: Close-quarters melee specialists, shock assault, panic breakers
**Republic Candidates**: Elite melee clones, Jedi variant forms, vibroblade specialists

#### Unit 5.1: Clone Vibroblade Specialist (ShockMelee Light)
- **Description**: Clone trooper with melee vibroblade, close combat trainer
- **Search Strategy**: "clone vibroblade", "clone melee", "sword clone"
- **Sketchfab Queries**:
  - Query 1: `clone+trooper+sword+melee` → Clone with close-combat weapon
  - Query 2: `vibroblade+clone+warrior` → Clone swordsman
  - Query 3: `clone+melee+specialist+elite` → Elite melee clone
- **Model Type**: Armored humanoid with melee weapon (blade, staff, etc.)
- **Expected Tags**: #clone #melee #sword #starwars
- **Status**: PENDING SEARCH

#### Unit 5.2: Clone Shock Commando (ShockMelee Medium)
- **Description**: Advanced commando trained in shock melee tactics
- **Search Strategy**: "clone shock commando", "clone elite melee", "commando assault"
- **Sketchfab Queries**:
  - Query 1: `commando+melee+clone+elite` → Elite melee commando
  - Query 2: `clone+assault+specialist+shock` → Shock assault unit
  - Query 3: `commando+blade+warrior+clone` → Commando with melee focus
- **Model Type**: Heavily armored humanoid with advanced melee weapon or dual weapons
- **Status**: PENDING SEARCH

#### Unit 5.3: Jedi Padawan (ShockMelee Heavy)
- **Description**: Younger Jedi warrior with lightsaber, support to Jedi Knight hero
- **Search Strategy**: "jedi padawan", "young jedi", "jedi apprentice"
- **Sketchfab Queries**:
  - Query 1: `jedi+padawan+lightsaber` → Padawan warrior
  - Query 2: `young+jedi+clone+wars+era` → Clone Wars era Jedi
  - Query 3: `jedi+warrior+robe+lightsaber` → Jedi combatant
- **Model Type**: Humanoid robed figure with lightsaber (can be non-physical prop)
- **Status**: PENDING SEARCH

#### Units 5.4-5.7: ShockMelee Variants (4 more)
- Additional variants based on search results
- Can include: Stun staff specialists, force-sensitive variants, elite blade masters

---

### Priority 6: CoreLineInfantry (10 additional units needed, 11 total)
**Role**: General infantry backbone, versatile combat troops
**Republic Candidates**: Clone trooper variants, phase variants, armor markings

#### Unit 6.1: Clone Trooper Phase I Variant (CoreLineInfantry)
- **Description**: Earlier phase clone armor variant, distinct visual identity
- **Search Strategy**: "clone trooper phase 1", "phase 1 armor", "clone old armor"
- **Sketchfab Queries**:
  - Query 1: `clone+trooper+phase+1+armor` → Phase I trooper
  - Query 2: `clone+soldier+old+armor` → Early clone armor
  - Query 3: `clone+phase+one+starwars` → Phase 1 variant
- **Status**: PENDING SEARCH

#### Unit 6.2: Clone Trooper Squad Leader (CoreLineInfantry)
- **Description**: Enhanced clone with markings and additional equipment
- **Search Strategy**: "clone squad leader", "clone sergeant", "command clone"
- **Sketchfab Queries**:
  - Query 1: `clone+squad+leader+sergeant` → Leadership variant
  - Query 2: `clone+markings+commander` → Command variant
  - Query 3: `clone+trooper+enhanced+gear` → Enhanced clone trooper
- **Status**: PENDING SEARCH

#### Units 6.3-6.10: CoreLineInfantry Variants (8 more)
- Additional variants based on search results
- Can include: 501st legion clone, colored armor variants (red, yellow, blue markings), armor configurations, battalion specialists

---

### Priority 7: HeavyInfantry (6 additional units needed, 7 total)
**Role**: Heavy weapons support, suppression, area denial
**Republic Candidates**: Heavy gunner variants, rotary cannon specialists, flamethrower units

#### Unit 7.1: Clone Heavy with Twin Cannons (HeavyInfantry)
- **Description**: Heavy weapons specialist with dual rotary blasters
- **Search Strategy**: "clone heavy twin cannon", "heavy gunner dual", "clone heavy weapons"
- **Sketchfab Queries**:
  - Query 1: `clone+heavy+dual+cannon` → Twin cannon heavy
  - Query 2: `heavy+weapons+clone+dual` → Heavy dual weapons
  - Query 3: `clone+gunner+twin+blaster` → Twin blaster variant
- **Status**: PENDING SEARCH

#### Unit 7.2: Clone Flamethrower Specialist (HeavyInfantry)
- **Description**: Heavy trooper with flamethrower weapon system
- **Search Strategy**: "clone flamethrower", "flame trooper clone", "clone fire specialist"
- **Sketchfab Queries**:
  - Query 1: `clone+flamethrower+specialist` → Flamethrower clone
  - Query 2: `flame+trooper+clone+heavy` → Clone flame unit
  - Query 3: `heavy+fire+weapon+clone` → Heavy fire unit
- **Status**: PENDING SEARCH

#### Units 7.3-7.7: HeavyInfantry Variants (5 more)
- Additional variants based on search results
- Can include: Missile launcher variants, grenade launcher specialists, portable cannon units

---

### Priority 8: MilitiaLight (6 additional units needed, 7 total)
**Role**: Basic infantry cannon fodder, cheap garrison units
**Republic Candidates**: Basic clone variants, conscript clones, support infantry

#### Unit 8.1: Clone Conscript (MilitiaLight)
- **Description**: Cheaper basic clone with minimal equipment, training mission
- **Search Strategy**: "clone conscript", "basic clone trooper", "training clone"
- **Sketchfab Queries**:
  - Query 1: `clone+conscript+basic+trooper` → Basic clone variant
  - Query 2: `clone+soldier+simple+armor` → Simple-armor clone
  - Query 3: `recruit+clone+trooper+basic` → Recruit clone
- **Status**: PENDING SEARCH

#### Units 8.2-8.7: MilitiaLight Variants (5 more)
- Additional variants based on search results
- Can include: Garrison variants, support infantry, guard clones

---

### Priority 9: FastVehicle (6 additional units needed, 7 total)
**Role**: Reconnaissance, fast attack, flanking maneuvers
**Republic Candidates**: Speeder variants, speederbike clones, scout vehicles

#### Unit 9.1: Clone Speederbike Scout (FastVehicle)
- **Description**: Light speederbike reconnaissance variant
- **Search Strategy**: "clone speederbike", "speeder bike clone", "scout speeder"
- **Sketchfab Queries**:
  - Query 1: `clone+speederbike+scout` → Speeder bike
  - Query 2: `speeder+bike+clone+republic` → Republic speeder
  - Query 3: `scout+vehicle+clone+fast` → Fast scout vehicle
- **Status**: PENDING SEARCH

#### Unit 9.2: Clone Floating Transport (FastVehicle)
- **Description**: Hovering transport/attack speeder
- **Search Strategy**: "clone speeder transport", "hover speeder clone", "floating transport"
- **Sketchfab Queries**:
  - Query 1: `clone+speeder+transport+hover` → Transport speeder
  - Query 2: `hover+vehicle+clone+fast` → Hover vehicle
  - Query 3: `speeder+transport+republic+army` → Republic speeder
- **Status**: PENDING SEARCH

#### Units 9.3-9.7: FastVehicle Variants (4 more)
- Additional variants based on search results
- Can include: BARC speeder variants, hover tank variants, light reconnaissance vehicles

---

### Priority 10: EliteLineInfantry (3 additional units needed, 4 total)
**Role**: Elite shock troops, high-value damage dealers
**Republic Candidates**: Elite clone variants, specialized troopers

#### Unit 10.1: Clone Captain Elite (EliteLineInfantry)
- **Description**: Senior clone officer with command presence
- **Search Strategy**: "clone captain", "clone officer elite", "commander clone"
- **Sketchfab Queries**:
  - Query 1: `clone+captain+officer+elite` → Captain variant
  - Query 2: `clone+commander+armor+markings` → Command armor
  - Query 3: `elite+clone+officer+starwars` → Elite officer
- **Status**: PENDING SEARCH

#### Units 10.2-10.4: EliteLineInfantry Variants (2 more)
- Additional variants based on search results
- Can include: 501st elite, specialized commando variants

---

### Priority 11: Skirmisher (3 additional units needed, 5 total)
**Role**: Ranged specialists, harassment, kiting
**Republic Candidates**: Sniper variants, ranged commando, support specialists

#### Unit 11.1: Clone Sniper Specialist (Skirmisher)
- **Description**: Advanced sniper with enhanced precision system
- **Search Strategy**: "clone sniper elite", "sniper clone", "precision marksman"
- **Sketchfab Queries**:
  - Query 1: `clone+sniper+elite+specialist` → Elite sniper
  - Query 2: `sniper+clone+precision+weapon` → Precision sniper
  - Query 3: `clone+marksman+long+range` → Long-range specialist
- **Status**: PENDING SEARCH

#### Units 11.2-11.3: Skirmisher Variants (2 more)
- Additional variants based on search results
- Can include: Ranged commando, support sniper, specialized marksman

---

## API Search Execution

**Token**: `df0764455f124549a58f8a156ad8177d`

**Sketchfab API Endpoint**: `https://api.sketchfab.com/v3/search`

**Global Filters**:
- `downloadable=true` - Must be downloadable
- `licenses=CC0,CC-BY,CC-BY-SA` - License must be permissive
- `type=models` - 3D models only

**Rate Limiting**: ~100 requests/minute; adding 0.5s delay between searches

---

## Search Results

*(To be populated by Agent-15 during execution)*

### Query 1: Clone Trooper Heavy / AT-Specialist
```bash
curl -s -H "Authorization: Token df0764455f124549a58f8a156ad8177d" \
  "https://api.sketchfab.com/v3/search?type=models&query=clone+trooper+heavy&downloadable=true&licenses=CC0,CC-BY,CC-BY-SA" \
  | jq '.results[] | {id, name, license: .license.label, uri, polycount}' | head -30
```

**Status**: PENDING

---

### Query 2: Clone Commando
```bash
curl -s -H "Authorization: Token df0764455f124549a58f8a156ad8177d" \
  "https://api.sketchfab.com/v3/search?type=models&query=clone+commando&downloadable=true&licenses=CC0,CC-BY,CC-BY-SA" \
  | jq '.results[] | {id, name, license: .license.label, uri, polycount}' | head -30
```

**Status**: PENDING

---

### Query 3: Clone Artillery
```bash
curl -s -H "Authorization: Token df0764455f124549a58f8a156ad8177d" \
  "https://api.sketchfab.com/v3/search?type=models&query=clone+artillery&downloadable=true&licenses=CC0,CC-BY,CC-BY-SA" \
  | jq '.results[] | {id, name, license: .license.label, uri, polycount}' | head -30
```

**Status**: PENDING

---

### Query 4: Clone Pilot
```bash
curl -s -H "Authorization: Token df0764455f124549a58f8a156ad8177d" \
  "https://api.sketchfab.com/v3/search?type=models&query=clone+pilot&downloadable=true&licenses=CC0,CC-BY,CC-BY-SA" \
  | jq '.results[] | {id, name, license: .license.label, uri, polycount}' | head -30
```

**Status**: PENDING

---

### Query 5: Clone Walker
```bash
curl -s -H "Authorization: Token df0764455f124549a58f8a156ad8177d" \
  "https://api.sketchfab.com/v3/search?type=models&query=clone+walker&downloadable=true&licenses=CC0,CC-BY,CC-BY-SA" \
  | jq '.results[] | {id, name, license: .license.label, uri, polycount}' | head -30
```

**Status**: PENDING

---

### Query 6: AT-TE Walker
```bash
curl -s -H "Authorization: Token df0764455f124549a58f8a156ad8177d" \
  "https://api.sketchfab.com/v3/search?type=models&query=at+te+walker&downloadable=true&licenses=CC0,CC-BY,CC-BY-SA" \
  | jq '.results[] | {id, name, license: .license.label, uri, polycount}' | head -30
```

**Status**: PENDING

---

### Query 7: Clone Trooper Variants
```bash
curl -s -H "Authorization: Token df0764455f124549a58f8a156ad8177d" \
  "https://api.sketchfab.com/v3/search?type=models&query=clone+trooper+variant&downloadable=true&licenses=CC0,CC-BY,CC-BY-SA" \
  | jq '.results[] | {id, name, license: .license.label, uri, polycount}' | head -30
```

**Status**: PENDING

---

### Query 8: Star Wars Clone Trooper
```bash
curl -s -H "Authorization: Token df0764455f124549a58f8a156ad8177d" \
  "https://api.sketchfab.com/v3/search?type=models&query=star+wars+clone+trooper&downloadable=true&licenses=CC0,CC-BY,CC-BY-SA" \
  | jq '.results[] | {id, name, license: .license.label, uri, polycount}' | head -30
```

**Status**: PENDING

---

## Candidate Models (Found)

*(To be populated during search execution)*

---

## Model Evaluation Criteria

For each candidate model found:

- ✓ **License**: CC0, CC-BY, or CC-BY-SA (no NC, ND restrictions)
- ✓ **Format**: GLB/FBX/OBJ exportable (check available formats)
- ✓ **Polycount**: Estimated at 3K–15K triangles for character units, 1K–5K for details
- ✓ **Aesthetics**: Star Wars Clone Wars theme (not realistic photogrammetry, not grimdark)
- ✓ **Quality**: No obvious broken geometry, UV mapped or solid color
- ✗ **Reject**: Patreon-restricted, non-commercial clause, humanoid rigging required, broken geometry

---

## Success Metrics
- [ ] 8+ Sketchfab API queries executed
- [ ] 15+ candidate models discovered
- [ ] 10+ models passing license and format filters
- [ ] Models populated in manifest with ID, URL, artist, license
- [ ] At least 5 unit classes with candidate models identified
- [ ] Git commit with updated manifest

---

## Next Steps (Post-Agent-15)
1. **Agent-16 (AssetDownloader)**: Download all passing candidates
2. **Agent-17 (Blender)**: Decimate to budget, UV unwrap, export
3. **Agent-18 (Packager)**: Integrate into asset_pipeline.yaml, validate
4. **Agent-19 (Testing)**: Build pack, verify all assets load, run tests

---

**Generated by Agent-15**
**Status**: In Progress
**Last Update**: 2026-03-13 (Manifest creation, searches pending)
