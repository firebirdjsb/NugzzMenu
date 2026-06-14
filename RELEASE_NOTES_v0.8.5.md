# NugzzMenu v0.8.5

This release focuses on making Build Anywhere reliable for MelonLoader IL2CPP
with S1API.

## Build Anywhere

- Fixed synthetic `Grid.Awake` and `Tile.Awake` null-reference crashes by
  initializing synthetic grids and tiles before native game logic can use them.
- Fixed `BuildUpdate_Grid.Place()` null-reference crashes caused by incomplete
  footprint tile intersections.
- Added safe placement validation so invalid commits are ignored instead of
  crashing or corrupting the active build preview.
- Aligned synthetic grids with each item's actual origin footprint.
- Added precise crosshair floor placement for grid buildables.
- Fixed objects snapping between the floor and a floating position when aiming
  close to the player's feet.
- Filters the player, placement ghost, existing buildables, footprint
  detectors, and synthetic tiles from floor targeting.
- Keeps the most recent valid floor target briefly to prevent blinking when a
  raycast misses for a frame.

## Placed Objects

- Fixed drying racks and other grid buildables becoming invisible after
  placement.
- Prevented property culling from hiding objects placed on Nugzz synthetic
  grids.
- Restores placed object renderers and culling targets after placement.
- Added right-click pickup for objects placed outside purchased property
  bounds.
- Pickup uses the game's native `PickupItem()` path to return the original item
  instance to inventory.
- Checks inventory capacity before pickup so objects are not lost when the
  inventory is full.
- Supports outside objects placed before the current game launch by checking
  their position against owned property bounds.

## Stability

- Added throttled diagnostics for incomplete placement intersections.
- Removed duplicate synthetic grid GUID assignment.
- Preserved the native placement and networking paths for supported buildable
  objects.

## Installation

1. Install MelonLoader IL2CPP and S1API for Schedule I.
2. Place `NugzzMenu.dll` in the game's `Mods` folder.
3. Launch the game and press `F8` to open NugzzMenu.
