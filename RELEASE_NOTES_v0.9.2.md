# NugzzMenu v0.9.2

This update is focused on making the menu feel cleaner, easier to read, and less noisy while keeping the DLL lightweight.

## New

- Added a **Properties** tab for managing bought properties and workers.
- You can view owned properties, see worker capacity, hire workers, move existing workers to a selected property, and remove/fire workers.

## UI Facelift

- Reworked the whole menu into a darker, more polished green theme.
- Added cleaner window chrome, tab styling, panel cards, status chips, and notification styling.
- Added better runtime font selection so the menu looks less plain without bundling font files.
- Made titles, tabs, buttons, and headers stand out more.
- Fixed dynamic menu sizing so lower options are no longer cut off after the facelift.
- Fixed clipped title text in the header.

## Fixes

- Fixed the Money tab layout so XP controls are visible again.
- Fixed the Cheats tab layout so all Teleport options are visible again.
- Stopped the Vehicles tab from spamming the log when the game has not loaded a VehicleManager yet.
- Vehicle list loading now retries quietly instead of filling the console.

## Notes

- The facelift uses tiny runtime-generated textures and OS fonts where available, so the file size stays low.
- No embedded font files or large UI assets were added.
