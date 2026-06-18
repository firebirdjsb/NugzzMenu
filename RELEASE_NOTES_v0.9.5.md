# NugzzMenu v0.9.5

This release focuses on fixing the management clipboard so normal management
gameplay works again.

## Big Fix

- Fixed the management clipboard so you can select NPCs, workers, objects,
  lockers, destinations, and placeable management targets again.
- Fixed worker assignment and linking flows, including botanists being assigned
  to plants and NPCs being linked to lockers or destinations.
- Added safer clipboard fallbacks so the game's own management actions still
  fire correctly instead of the menu blocking clicks.

## Camera And Clipboard

- Equipping or opening the management clipboard now turns off third person
  automatically.
- Third person can no longer be toggled from the hotkey or menu while the
  clipboard is equipped.
- This prevents the custom camera from interfering with clipboard targeting.

## Properties And Workers

- Workers can no longer be moved to the RV.
- Workers can no longer be hired for the RV.
- The Properties tab now shows a clear message when the RV is selected instead
  of showing unsupported worker controls.

## Other Fixes Included

- Kept the recent Build Anywhere safety fixes.
- Kept the trimmer and clipboard crash guards.
- Continued cleanup around camera and input state while management tools are
  active.
