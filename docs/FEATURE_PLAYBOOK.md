# Feature Playbook

Use this checklist when adding or repairing a NugzzMenu feature. It keeps work
predictable and makes future bugs easier to trace.

## 1. Classify The Feature

Before coding, decide which category it belongs to:

- UI-only: style, layout, labels, button grouping.
- Local player helper: camera, movement, stamina, inventory, visuals.
- World mutation: spawning, building, save editing, time, property state.
- Multiplayer-visible action: anything other players can see or that the host
  must authorize.
- Compatibility patch: workarounds for game/S1API/fork differences.

The category decides how strict the guards need to be.

## 2. Pick The Right Owner

- UI-only work goes in a tab renderer or `GUISystemService`.
- Gameplay logic goes in a service.
- Persistent settings go through Melon preferences in `Core`.
- Runtime compatibility work goes in `CompatibilityService`.
- Vanilla method interception goes in a Harmony patch that delegates to a
  service.

Do not put long gameplay routines inside a renderer. Renderers are called every
GUI frame and should stay cheap.

## 3. Use Vanilla Paths First

For Schedule I features, vanilla systems are usually safer than custom
shortcuts:

- use managers and registered prefabs where possible,
- use host-authorized paths for persistent multiplayer changes,
- use existing item/vehicle/building/network APIs before custom RPCs,
- prefer vanilla camera state restoration over directly setting transforms.

Custom logic is fine when vanilla has no path, but document why.

## 4. Guard By Scene And Authority

Ask these questions:

- Does it only make sense in `Main`?
- Does it only make sense in `Menu`?
- Does the host need to run it?
- Should non-hosts see a warning instead of trying it?
- Can it run while paused?
- Can it run while the mod menu is open?
- Can it run while third-person camera is active?

Common examples:

- Save manager: `Menu` scene only.
- Building placement: leave vanilla snapping untouched unless a feature is
  explicitly host-authoritative and multiplayer-tested.
- Time and vehicle spawning: host-only.
- Camera helpers: local-only, but must respect vehicles and clipboard tools.

## 5. Keep Logging Useful

Use this order:

1. `NotificationService.Notify(...)` when the player needs to see it.
2. `NotificationService.Status(...)` for short feedback.
3. `DebugLogService.Verbose(...)` for maintainer diagnostics.
4. `UnityEngine.Debug.LogWarning(...)` only for unexpected but survivable
   failures.
5. `UnityEngine.Debug.LogError(...)` only when the feature truly failed.

Anything that can happen every frame must be silent, verbose-only, or
rate-limited.

## 6. UI Checklist

- The tab still fits after dynamic window sizing.
- Long labels are shortened or fitted through `GUIFit`.
- Buttons give status feedback.
- Dangerous actions require confirmation.
- Host-only/main-menu-only actions are visibly locked.
- Text fields are not reset every frame by an automatic refresh.

## 7. Multiplayer Checklist

- Test as host.
- Test as non-host.
- Test with both players using NugzzMenu if the feature is mod-to-mod.
- Test with a player not using NugzzMenu if the feature is claimed to be
  vanilla-visible.
- Watch for packet parse errors, observer RPC exceptions, and mismatched
  authority.

Do not describe a feature as working for unmodded clients unless it has been
tested that way.

## 8. Save/Data Checklist

- Only edit disk saves from the main menu.
- Make a backup or archive before destructive-looking changes.
- Use JSON parsing, not string replacement.
- Preserve unknown fields.
- Write clear UI text about what will and will not happen.

## 9. Build And Copy

Build:

```powershell
dotnet build SeshMenu.csproj -c Release
```

Copy to Steam only when requested:

```powershell
Copy-Item .\bin\Release\net6.0\NugzzMenu.dll "C:\Program Files (x86)\Steam\steamapps\common\Schedule I\Mods\NugzzMenu.dll" -Force
```

Thunderstore profile paths vary. Confirm the active profile before overwriting.

## 10. Hand-Off Notes

When handing a change to another maintainer, include:

- what changed,
- which files changed,
- build result,
- copied DLL path if copied,
- what was not tested,
- any expected limitations.
