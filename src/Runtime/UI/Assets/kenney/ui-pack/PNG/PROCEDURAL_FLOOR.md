# Procedural-FLOOR UI sprites (placeholder tier)

`panel_background.png` and `button_rectangleFlat.png` in this directory are
**procedurally generated placeholders** (Pillow), produced to clear the
main-menu loading-skeleton caused by missing `UiAssets` pre-warm sprites
(`Plugin.cs` logs "N sprite(s) not found").

- panel_background.png — 64x64 dark rounded 9-slice panel (border 8), teal stroke (#7ebab5), alpha corners
- button_rectangleFlat.png — 64x64 rounded-rect flat button (border 6)

These are the **floor tier**. Rich versions (Blender/Adobe, per the asset
pipeline governance in `docs/asset-pipeline-governance.md`) are a follow-up.
Generator: scratchpad `gen_sprites.py` (design-system colors midnight #090a0c + teal #7ebab5).
