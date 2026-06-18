# NugzzMenu

NugzzMenu is a MelonLoader IL2CPP mod menu for Schedule I. It integrates with
S1API and provides player, inventory, vehicle, world, and camera utilities.

Current release: [v0.9.5](https://github.com/firebirdjsb/NugzzMenu/releases/tag/v0.9.5)

## Latest Changes

### v0.9.5

- Fixed the management clipboard so selecting NPCs, workers, objects, lockers,
  destinations, and management targets works again.
- Restored worker assignment/linking flows such as botanists to plants and
  NPCs to lockers or destinations.
- Disabled third person automatically while the management clipboard is
  equipped, and blocked the third-person hotkey/menu toggle during clipboard
  use so camera overrides do not interfere with targeting.
- Prevented hiring workers for the RV or moving existing workers to the RV,
  since the game does not support that setup.

Full notes: [RELEASE_NOTES_v0.9.5.md](RELEASE_NOTES_v0.9.5.md)

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

## Build Anywhere

Enable **Place Anywhere** in the Settings tab to place supported grid, surface,
and procedural buildables outside purchased property bounds.

- Placement follows the floor under the crosshair and remains grounded when
  aiming near the player's feet.
- Placed objects remain visible outside property culling areas.
- Aim at an outside placed object and right-click to return its original item
  to inventory. The object remains in place when inventory space is unavailable.
