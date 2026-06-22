# Release Process

This is the maintainer checklist for preparing a NugzzMenu release. Do not push
or publish unless the owner explicitly asks.

## 1. Confirm The Version

Update all version references together:

- `Core.cs`
- `Properties/AssemblyInfo.cs`
- `README.md`
- new `RELEASE_NOTES_vX.Y.Z.md`

Use the exact version requested by the owner. For example, `0.9.6` is not
`v8.5`.

## 2. Build

```powershell
dotnet build SeshMenu.csproj -c Release
```

Expected output:

```text
bin/Release/net6.0/NugzzMenu.dll
```

## 3. Smoke Test

Minimum local checks:

- game reaches main menu with no Nugzz startup errors,
- menu opens/closes in main menu,
- cursor returns correctly in main menu,
- menu opens/closes in-game,
- cursor locks correctly in-game,
- vehicle camera survives opening/closing the menu while driving,
- Settings tab fits all controls,
- Items tab can spawn a quality-aware item,
- Place Anywhere can be toggled by host and warns non-hosts,
- management clipboard still selects and links vanilla targets,
- logs stay quiet during normal use.

## 4. Copy For Testing

Steam path:

```powershell
Copy-Item .\bin\Release\net6.0\NugzzMenu.dll "C:\Program Files (x86)\Steam\steamapps\common\Schedule I\Mods\NugzzMenu.dll" -Force
```

Thunderstore paths depend on the selected profile. Confirm the profile before
copying. If Schedule I is running, the DLL may be locked; close the game before
overwriting.

## 5. Patch Notes

Write notes for players, not just developers. Good notes are grouped like this:

- New
- Improved
- Fixed
- Known Notes

Mention host-only or main-menu-only behavior plainly. Avoid low-level method
names unless they help users understand a limitation.

## 6. GitHub Release

Only after explicit approval:

1. stage the intended files,
2. commit with the release version,
3. tag the version,
4. push branch and tag,
5. create or update the GitHub release,
6. attach `NugzzMenu.dll`,
7. paste the player-facing patch notes.

## 7. After Release

Keep a short note of:

- exact DLL path and timestamp,
- commit hash,
- tag name,
- what was tested,
- anything that was intentionally not changed.
