# NugzzMenu

NugzzMenu is a MelonLoader IL2CPP mod menu for Schedule I. It integrates with
S1API and provides player, inventory, vehicle, world, and camera utilities.

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

