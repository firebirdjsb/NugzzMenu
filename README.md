# NugzzMenu

NugzzMenu is an in-game control suite for Schedule I, built for MelonLoader
IL2CPP and S1API. It combines quality-of-life tools, accessibility controls,
world and inventory utilities, multiplayer-aware vehicle features, quest and
save recovery, performance tuning, and developer-friendly diagnostics in one
menu.

Current release: [v1.0.0](RELEASE_NOTES_v1.0.0.md)

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
- Full Xbox and PlayStation controller navigation with a polished animated menu
  shell, visible focus, contextual controls, and automatic scrolling.

## Latest Changes

### v1.0.0

- Added full controller support for Xbox and PlayStation layouts across every
  tab, option, slider, text field, and action in NugzzMenu.
- Redesigned the shared menu shell with smooth open and close animation,
  clearer focus states, contextual controller help, and automatic scrolling.
- Preserved all existing tab names, features, mouse controls, keyboard controls,
  multiplayer safeguards, and main-menu save tools.
- Kept multiplayer authorization compatible with approved NugzzMenu R4 and
  newer clients while advertising the current v1.0.0 build.

#### Maintenance fixes

- Restored the custom left-click interaction prompts and actions for weed,
  mushrooms, watering, soil, substrate, and grow additives. Prompt discovery
  now follows the game's active interactable and held-item definitions instead
  of relying on a narrow physics-layer lookup.
- Preserved the native cooking-station temperature minigame and added a focused
  Bunsen burner input fallback for mouse and controller when the updated game
  does not advance the dial itself.
- Removed the large menu open/close hitch by keeping input patches installed,
  skipping unused IMGUI layout work, caching fitted controls and diagnostics,
  and drawing passive overlays only during repaint events.
- Isolated NugzzMenu's GUI styles so opening the menu no longer replaces the
  global Unity skin or changes the appearance of other mods.
- Improved native-activity blocking, controller detection, console suggestion
  cleanup, multiplayer session discovery, interactive shape prompts, and
  per-skateboard tuning persistence.
- Reworked multiplayer vehicle collision tracking and staggered parked-vehicle
  transform synchronization to keep visible cars and their blocking colliders
  aligned without doing expensive full refreshes every frame.

Full release notes: [RELEASE_NOTES_v1.0.0.md](RELEASE_NOTES_v1.0.0.md)

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
