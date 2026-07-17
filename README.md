# NugzzMenu

NugzzMenu is an in-game control suite for Schedule I, built for MelonLoader
IL2CPP and S1API. It combines quality-of-life tools, accessibility controls,
world and inventory utilities, multiplayer-aware vehicle features, quest and
save recovery, performance tuning, and developer-friendly diagnostics in one
menu.

Current release: [v0.9.9R3](RELEASE_NOTES_v0.9.9R3.md)

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

## Latest Changes

### v0.9.9R3

- Added a complete NPC/client Relations tab with relationship, unlock,
  addiction, product-affinity, recommendation, and deal-offer controls.
- Expanded the FPS tab with decorative-light budgets, reflection throttling,
  LOD and shadow controls, on-demand scene diagnostics, and safe restore tools.
- Added clear left-click prompts and reliable interactions for mushroom spawn,
  substrate, spray bottles, watering, soil, seeds, additives, and harvesting.
- Fixed spray bottles so misting a dry mushroom bed fills and synchronizes its
  moisture while consuming bottle water.
- Synchronized police sirens/lightbars from the vehicle's networked headlight
  state for other Nugzz clients and cleaned up the vehicle HUD after exiting.
- Improved trash-container detection and repetitive IL2CPP log filtering without
  replacing the underlying vanilla gameplay methods.
- Replaced the incompatible Unity text editor with an IL2CPP-safe search field
  used by the Items and Relations tabs.

Full transparent diff notes: [RELEASE_NOTES_v0.9.9R3.md](RELEASE_NOTES_v0.9.9R3.md)

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
