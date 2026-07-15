# NugzzMenu

NugzzMenu is a MelonLoader IL2CPP mod menu for Schedule I. It integrates with
S1API and provides player, inventory, vehicle, world, and camera utilities.

Current release: [v0.9.9R2](RELEASE_NOTES_v0.9.9R2.md)

## Latest Changes

### v0.9.9R2

- Completely removed Build Anywhere and its synthetic placement grids so
  vanilla building owns placement, snapping, validation, and networking.
- Reworked third-person, first-person viewmodels, skateboard transitions, and
  vehicle-menu camera handling around separate state/visibility services.
- Added FPS and quest-control tabs, a low-cost keybind HUD, jump/gravity tuning,
  optional double-space fly, and driven-vehicle flight.
- Restored native quest variable calls, added targeted Welcome quest recovery,
  and repaired consumed-item trash and trash-bag interactions used by quests.
- Added multiplayer vehicle-tune messages for Nugzz clients and retained native
  vehicle color replication.

Full transparent diff notes: [RELEASE_NOTES_v0.9.9R2.md](RELEASE_NOTES_v0.9.9R2.md)

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
