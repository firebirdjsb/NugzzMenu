# NugzzMenu

NugzzMenu is a MelonLoader IL2CPP mod menu for Schedule I. It integrates with
S1API and provides player, inventory, vehicle, world, and camera utilities.

Current release: [v0.9.8](RELEASE_NOTES_v0.9.8.md)

## Latest Changes

### v0.9.8

- Added a main-menu Save Manager in Settings for save inspection, backups,
  archive-delete, tutorial flag edits, and local Steam Cloud marker handling.
- Fixed Settings tab sizing with a safe scroll path so larger panels remain
  reachable on-screen.
- Cleaned up repeated missing-variable warning spam from legacy game variables
  such as `cash_balance`, `total_money`, and `player_in_vehicle`.
- Improved vehicle menu work with safer vehicle-camera handling, RV tools,
  expanded spawn support, and current-vehicle tuning controls.
- Added maintainer documentation and formatting rules so the project is easier
  for other coders to understand and extend.

Full notes: [RELEASE_NOTES_v0.9.8.md](RELEASE_NOTES_v0.9.8.md)

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
