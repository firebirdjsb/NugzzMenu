# NugzzMenu

NugzzMenu is a MelonLoader IL2CPP mod menu for Schedule I. It integrates with
S1API and provides player, inventory, vehicle, world, and camera utilities.

Current release: [v0.9.9](RELEASE_NOTES_v0.9.9.md)

## Latest Changes

### v0.9.9

- Fixed plant care interactions for watering, soil pouring, seed planting, and
  trimming, with faster quality-of-life fallbacks where vanilla interaction was
  failing.
- Added buttons for auto-watering plants/pots and auto-filling empty pots with
  the best available soil.
- Added unlock buttons for achievements, items/supplies, properties, and
  businesses.
- Fixed item spawner categories, removed broken pseudo/white-square entries,
  and repaired several items that were not spawning properly.
- Fixed Place Anywhere interfering with vanilla floor/grid snapping and improved
  close-to-player placement behavior.
- Added and polished World Teleports in the Cheats tab, with cleaner labels and
  duplicate location filtering.

Full notes: [RELEASE_NOTES_v0.9.9.md](RELEASE_NOTES_v0.9.9.md)

## Requirements

- Schedule I
- MelonLoader IL2CPP
- S1API
- .NET 6 SDK for local builds

## Build

Reference assemblies are expected in `net6.0/`.

```powershell
dotnet build SeshMenu.csproj -c Release
```

The mod DLL is written to `bin/Release/net6.0/NugzzMenu.dll`.

## Install

Place `NugzzMenu.dll` in the game's `Mods` directory.

## Save Manager

The Settings tab includes a main-menu-only save manager. It can refresh save
slots, back up a slot, archive-delete a slot, edit the tutorial flag, edit the
organisation name, and adjust common money fields. Delete is intentionally
recoverable: the save folder is moved under that profile's `Backups` folder.

The Steam Cloud control renames the local `steam_autocloud.vdf` marker inside
the save profile. Fully disabling Steam Cloud still needs to be done in Steam's
game properties.

## Build Anywhere

Enable **Place Anywhere** in the Settings tab to place supported grid, surface,
and procedural buildables outside purchased property bounds.

- Placement follows the floor under the crosshair and remains grounded when
  aiming near the player's feet.
- Placed objects remain visible outside property culling areas.
- Aim at an outside placed object and right-click to return its original item
  to inventory. The object remains in place when inventory space is unavailable.

## Maintainers

Start with [CONTRIBUTING.md](CONTRIBUTING.md) before adding features.

- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) explains the service/UI split,
  scene-safety rules, build flow, and compatibility strategy.
- [docs/CODEBASE_MAP.md](docs/CODEBASE_MAP.md) lists the responsibility of each
  major file.
- [docs/FEATURE_PLAYBOOK.md](docs/FEATURE_PLAYBOOK.md) gives the checklist for
  adding features without breaking multiplayer, saves, camera, or logs.
- [docs/RELEASE_PROCESS.md](docs/RELEASE_PROCESS.md) documents version bumps,
  smoke testing, copy targets, patch notes, and GitHub release steps.
