# NugzzMenu

NugzzMenu is a MelonLoader IL2CPP mod menu for Schedule I. It integrates with
S1API and provides player, inventory, vehicle, world, and camera utilities.

Current release: [v0.9.6](https://github.com/firebirdjsb/NugzzMenu/releases/tag/v0.9.6)

## Latest Changes

### v0.9.6

- Worker controls are now blocked for any owned property with `0/0` worker
  capacity, including Motel Room, Sewer Office, Laundromat, and the RV.
- The Properties tab now shows unsupported-worker messaging instead of hire or
  move controls for those locations.
- Equipping the management clipboard while in third person now exits through
  the normal camera toggle path so first-person visuals restore correctly.
- Clipboard use still blocks third-person toggles while equipped.

Full notes: [RELEASE_NOTES_v0.9.6.md](RELEASE_NOTES_v0.9.6.md)

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
