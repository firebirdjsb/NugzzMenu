# NugzzMenu v0.9.1

This is a compatibility patch for players using different S1API builds,
especially older forked versions such as **S1API Forked by Bars v3.0.2r2**.

## Compatibility

- Fixed NugzzMenu failing to load when an older S1API fork is installed.
- Removed the hard dependency on one exact S1API assembly version.
- Nugzz now detects S1API runtime events more gently when they are available,
  and falls back to normal MelonLoader scene hooks when they are not.
- Improved support for Thunderstore profiles that ship a different S1API build
  than the one used while compiling Nugzz.

## Multiplayer Join Safety

- Removed the custom LAN/IP join controls from Settings.
- Removed the custom Steam invite button from Settings.
- Removed menu code that manually touched lobby join, lobby leave, or direct
  scene loading.
- Steam invites and joining friends now stay on the game's normal multiplayer
  path, which should prevent joining into an unsynced duplicate session.

## Stability

- Stopped equipped item handler errors from spamming the log every frame.
- Stopped player scale network sync from retrying forever when the game rejects
  the custom scale variable.
- Cleaned up Settings tab spacing after removing the broken join tools.

## Install

Install MelonLoader IL2CPP and your preferred compatible S1API build for
Schedule I, then place `NugzzMenu.dll` in the game's `Mods` folder.
