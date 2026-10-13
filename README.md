# NugzzMenu

NugzzMenu is an in-game control suite for Schedule I, built for MelonLoader
IL2CPP and S1API. It combines quality-of-life tools, accessibility controls,
world and inventory utilities, multiplayer-aware vehicle features, quest and
save recovery, performance tuning, and developer-friendly diagnostics in one
menu.

Current release: [v1.0.1](RELEASE_NOTES_v1.0.1.md)

## Features

- Player cheats, movement tuning, fly mode, and an adjustable third-person
  camera.
- Quality-aware item spawning, grow helpers, money/XP tools, teleports, time,
  on-demand weather presets and lightning, property, business, and achievement
  controls.
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
- Full Xbox and PlayStation controller navigation with a polished animated menu
  shell, visible focus, contextual controls, and automatic scrolling.

## Latest Changes — v1.0.1

- Updated for Schedule I 0.4.7f12 and the Special Customers update, with S1API 3.2.1 included.
- Added all four visiting groups and 31 visitors to Relations, with next-day arrival controls.
- Added rideable session motorcycles with visible riders, smoother steering, better hill stability, easier stopping and original chopper audio.
- Added eight player animations under Lobby > Local Effects, with temporary props and effects.
- Added clear, fog, light rain, storm/heavy rain and lightning controls under Time.
- Added saved startup preferences in `Mods/nuggzmenu-config.json` and a clearer clothing color picker with Native / No color defaults.
- Improved Never Wanted for solo players, hosts and approved clients.
- Addressed Phone/Character lockouts, conflicting grow prompts, quick planting/watering, color-picker errors and white skin after clearing effects.

[Full player-friendly v1.0.1 changelog](RELEASE_NOTES_v1.0.1.md) includes new items,
all visitor names, multiplayer limits, upgrade guidance and remaining playtests.
Earlier releases remain in [CHANGELOG.md](CHANGELOG.md).

## Requirements

- Schedule I 0.4.7f12
- MelonLoader IL2CPP
- S1API 3.2.1 (included with the release)
- .NET 6 SDK for local builds

## Build

Reference assemblies are expected in `net6.0/`.

The special customers compatibility pass pins S1API `3.2.1` in
`S1API.lock.json`. Run `python scripts/update_s1api.py` to verify and stage that
dependency and refresh the local IL2CPP build reference. The game installation
is not modified by this command. See
[the compatibility checklist](docs/SPECIAL_CUSTOMERS_COMPATIBILITY.md) for
validation status and the required updated game assemblies.

```powershell
dotnet build SeshMenu.csproj -c Release
```

The mod DLL is written to `bin/Release/net6.0/NugzzMenu.dll`.

## Install

Close the game and back up the DLLs you replace outside Mods/Plugins. Extract the
release ZIP's `Mods` and `Plugins` folders into the game folder, keeping one
NugzzMenu DLL. Preserve your existing `nuggzmenu-config.json`; the example is
reference only. Host and clients should use v1.0.1 for the new bikes and animations.
Restore the backed-up DLLs with the game closed to roll back.

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
