# NugzzMenu

NugzzMenu is an in-game control suite for Schedule I, built for MelonLoader
IL2CPP and S1API. It combines quality-of-life tools, accessibility controls,
world and inventory utilities, multiplayer-aware vehicle features, quest and
save recovery, performance tuning, and developer-friendly diagnostics in one
menu.

Current release: [v0.9.9R4](RELEASE_NOTES_v0.9.9R4.md)

## Features

- Player cheats, movement tuning, fly mode, and an adjustable third-person
  camera.
- Quality-aware item spawning, grow helpers, money/XP tools, teleports, time,
  weather, property, business, and achievement controls.
- Vehicle spawning, tuning, police sirens, vehicle flight, and multiplayer-aware
  synchronization where the game exposes a suitable network path.
- Quest inspection and recovery controls, RV story safeguards, and a
  main-menu-only save manager with recoverable deletion and backups.
- NPC and customer relationship editing, including unlocks, addiction, product
  affinity, recommendations, and deal offers.
- FPS controls for frame pacing, decorative lights, reflections, LOD, shadows,
  scene diagnostics, and low-impact menu operation.
- Compatibility and logging guards designed to preserve vanilla gameplay calls
  while filtering repetitive known errors.
- Host-controlled multiplayer access for matching NugzzMenu clients, native
  console command suggestions, per-board skateboard tuning, and interactive
  static or physics-enabled 3D shapes.
- Clothing color selection, remembered custom mixtures, production automation,
  and selected-seed grow helpers.

## Latest Changes

### v0.9.9R4

- Updated NugzzMenu for the current Schedule I IL2CPP assemblies and S1API,
  including safer GUI rendering and preserved patch targets.
- Added host-controlled Nugzz access for matching multiplayer clients, with
  live allow and deny controls in the Lobby tab.
- Added native console auto-complete, interactive 3D shapes, per-skateboard
  tuning, clothing colors, and remembered custom drug mixtures.
- Added selected-seed auto planting plus one-click completion for meth cooks,
  ovens, mixing stations, cauldrons, and mushroom grow cycles.
- Added stricter pause, phone, console, TV, jukebox, casino, workstation,
  vehicle, skateboard, and character-creation input protection.
- Reduced repeated frame work in vehicle collision, camera, player, overlay,
  relationship, and menu systems while keeping the gameplay tools available.
- Reworked first-person view restoration, player scale synchronization, shape
  interaction, vehicle HUD cleanup, and menu availability in live saves.
- Removed the remaining legacy building patch source so vanilla placement owns
  building behavior.

Full transparent diff notes: [RELEASE_NOTES_v0.9.9R4.md](RELEASE_NOTES_v0.9.9R4.md)

Release history: [CHANGELOG.md](CHANGELOG.md)

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
