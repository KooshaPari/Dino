# Addressables Integration for warfare-starwars

This directory contains the design specification for integrating Unity Addressables v1.21.18 asset loading into the DINOForge platform, specifically for the warfare-starwars content pack.

## Files

### Primary Design Document
- **ADDRESSABLES_INTEGRATION_DESIGN.md** - Complete specification (600+ lines)
  - Architecture overview
  - Bundle layout & naming conventions
  - ContentLoader integration patterns
  - Cache strategies
  - Windows build automation
  - Runtime asset loading patterns
  - Debugging & diagnostics
  - Migration & compatibility
  - Reference implementation code
  - Unit test suite
  - Troubleshooting guide
  - Implementation checklist

### Summary & Quick Reference
- **ADDRESSABLES_DESIGN_SUMMARY.md** - Executive summary
  - Design overview
  - Deliverables checklist
  - Key design decisions
  - Integration path (5 phases)
  - Impact analysis
  - Success criteria

### Implementation Guide
- **ADDRESSABLES_README.md** - This file

## Quick Links

### For Designers
- Read: ADDRESSABLES_DESIGN_SUMMARY.md (this gives you the full picture in 5 minutes)

### For Implementers (Phase 2+)
- Read: ADDRESSABLES_INTEGRATION_DESIGN.md (reference implementation section)
- Follow: Implementation checklist (end of main document)
- Code: See reference code blocks in main document

### For Testing
- Unit test suite: Section 11 of ADDRESSABLES_INTEGRATION_DESIGN.md
- Test data: Provided in example test class
- Validation: CLI tools documented for pre-deployment checks

### For Debugging
- Overlay integration: Section 9 of main document
- Logging: Comprehensive logging patterns provided
- Inspector tool: CLI command specification

## Implementation Phases

1. **Phase 1: Core SDK** ✓ DONE
   - `IAssetLoader` interface defined in `src/SDK/Assets/IAssetLoader.cs`
   - Bundle layout designed
   - Addressing convention specified

2. **Phase 2: Runtime Implementation** (Next)
   - Implement `AddressablesAssetLoader` in `src/Runtime/Assets/`
   - Reference code provided in main document

3. **Phase 3: ContentLoader Integration**
   - Add asset loader to ContentLoader
   - Test with warfare-starwars pack

4. **Phase 4: Build Automation**
   - Set up Unity project
   - Configure Addressables groups
   - Test build scripts

5. **Phase 5: Debugging**
   - Implement debug overlay
   - Add CLI inspection tools

## Key Takeaways

### Bundle Structure
- 3 bundles: units, buildings, configs
- ~50 assets total (26 unit FBX + 26 unit textures + 24 building FBX + 24 building textures)

### Address Naming
```
warfare-starwars/units/rep_clone_trooper.fbx
warfare-starwars/textures/units/rep_clone_trooper_base.png
warfare-starwars/buildings/rep_house_clone_quarters.fbx
```

### Memory Budget
- Persistent cache: 500 MB
- LRU cache: 200 MB
- Bundle-scoped: 1000-1500 MB
- Total asset footprint: 972-2211 MB

### Integration Points
- SDK: `IAssetLoader` interface (platform-agnostic)
- Runtime: `AddressablesAssetLoader` implementation (Unity-dependent)
- ContentLoader: Asset orchestration
- PackCompiler: Catalog validation

## Next Steps

1. **Phase 2 Lead**: Create `AddressablesAssetLoader` in Runtime/Assets/
2. **Phase 3 Lead**: Integrate with ContentLoader
3. **Phase 4 Lead**: Set up Unity project + build scripts
4. **Phase 5 Lead**: Implement debug tools

Detailed checklist in main document (40+ tasks with dependencies).

---

**Status**: Design Complete, Ready for Implementation
**Questions?**: See troubleshooting guide in main document
