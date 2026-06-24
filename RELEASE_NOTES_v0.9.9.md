# NugzzMenu v0.9.9

This update focuses on making grow tools, item spawning, Build Anywhere, and the
new world teleport tools feel much cleaner in normal play.

## New

- Added auto-water buttons for plants and soil pots.
- Added an auto soil-fill button that fills empty pots with the best soil.
- Added unlock buttons for all achievements, items and supplies, properties, and
  businesses.
- Added World Teleports to the Cheats tab with pages for locations, POIs,
  properties, businesses, suppliers, dead drops, parking, police stations, and
  loaded NPCs.
- Added a debug skybase test room with display clones, vendor markers, and an
  NPC lineup for quick testing.

## Improved

- Watering, seed planting, soil pouring, fertilizing, and trimming now use
  faster quality-of-life fallbacks when vanilla interaction paths fail.
- Auto-watering now works for both planted and empty pots.
- The item spawner categories were cleaned up so items land in more sensible
  groups.
- Teleport labels are cleaner and duplicate/near-duplicate locations are
  filtered out.
- Long teleport names now fit better in the UI.
- The debug room now briefly locks player controls while unloading so the player
  cannot move or look during room cleanup.

## Fixed

- Fixed watering cans, seeds, soil, fertilizer, and trimmers failing or spamming
  errors in some first-person interaction cases.
- Fixed grow tool fallbacks so they do not keep wasting water, soil, or seeds
  when the target is already full or cannot use the item.
- Fixed drying rack completion so completed product can properly reach Heavenly
  quality instead of stopping at Premium.
- Fixed a synthetic grid placement issue where Place Anywhere could take over
  vanilla item snapping.
- Fixed floor-grid snapping and close-to-player placement so items no longer
  jump above the player or fail to sit on the floor grid.
- Fixed several item spawner entries that were missing, miscategorized, or not
  spawning properly.
- Removed broken spawn entries that showed up as white squares or duplicate
  pseudo-quality items.
- Moved World Teleports out of the Lobby tab and into Cheats where it belongs.
- Cleaned up POI names that had awkward line breaks or doubled spacing.

## Notes

- Build Anywhere should only apply custom placement behavior while its toggle is
  enabled. Vanilla building should keep the game's normal snapping behavior.
- Debug skybase buildables are display clones for visibility and testing, not
  fully placed/persistent vanilla buildables.
- No large assets were added.
