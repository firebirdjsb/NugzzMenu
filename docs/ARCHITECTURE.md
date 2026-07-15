# NugzzMenu Architecture

NugzzMenu is a MelonLoader IL2CPP mod menu for Schedule I. It is intentionally
kept as a small C# library instead of a framework-heavy plugin: `Core` handles
MelonLoader lifecycle and menu routing, services own game-facing behavior, and
UI renderers draw tabs.

This document is the first stop for maintainers. For a file-by-file catalog,
read `CODEBASE_MAP.md`. For adding new features safely, read
`FEATURE_PLAYBOOK.md`.

## Design Goals

- Keep gameplay logic out of UI drawing code.
- Keep unsafe work behind services with small public methods.
- Prefer vanilla game paths, RPCs, and managers over custom replicas.
- Make host-only or main-menu-only tools visibly locked instead of silently
  failing.
- Keep the DLL small by avoiding new dependencies unless they remove meaningful
  complexity.
- Make errors diagnosable without creating log spam.

## Top-Level Flow

```text
MelonLoader
  -> Core.OnInitializeMelon()
      -> preferences, GUI setup, S1API event reflection, Harmony patches
  -> Core.OnSceneWasInitialized()
      -> invalidate caches and initialize gameplay services in Main
  -> Core.OnUpdate()
      -> input, notifications, cheats, effects, item queues, vehicles, building
  -> Core.OnLateUpdate()
      -> third-person camera, vehicle menu camera repair, fly post-lock
  -> Core.OnGUI()
      -> dynamic window, tab routing, renderer calls
```

`Core` should stay boring. If a feature needs more than a few lines, it belongs
in a service or renderer.

## Project Shape

- `Core.cs`
  - MelonMod lifecycle.
  - Menu open/close and input locking.
  - Tab selection and tab renderer calls.
  - S1API runtime event subscription by reflection.
- `Services/`
  - Gameplay services, Harmony patches, cache helpers, and GUI infrastructure.
  - Services are singleton classes so state survives between frames.
  - Patch classes currently live beside the service they support.
- `UI/`
  - IMGUI tab renderers.
  - Simple state classes for controls that need to persist between frames.
  - Renderers should call service APIs, not scan the world or edit files.
- `Properties/AssemblyInfo.cs`
  - Assembly version used for release builds.
- `net6.0/`
  - Local reference assemblies for Schedule I, MelonLoader, Unity, FishNet, and
    S1API.
- `docs/`
  - Maintainer guidance and release notes support.

`SeshMenu.csproj` has `EnableDefaultItems=false` but includes
`Services/**/*.cs` and `UI/**/*.cs`, so new C# files under those folders are
compiled automatically.

## Service Pattern

Most services use this shape:

```csharp
public sealed class ExampleService
{
    private static readonly ExampleService _instance = new ExampleService();
    public static ExampleService Instance => _instance;

    private ExampleService() { }

    public void DoThing()
    {
        // Thin public method, guarded internally.
    }
}
```

Keep service public APIs small and intention-revealing. A renderer should be
able to call `VehicleService.Instance.SpawnSelectedVehicle()` or
`SaveManagementService.Instance.BackupSelectedSave()` without knowing the
internals.

## UI Pattern

Each tab renderer receives:

- a `ref float y` cursor,
- available width,
- GUI styles from `GUISystemService`,
- any state object needed by the tab,
- action delegates or service instances.

Renderers should:

- draw labels/buttons/fields,
- update their state object,
- call services or callbacks,
- avoid expensive work every frame.

Renderers should not:

- run broad Unity object scans,
- perform file edits directly,
- patch Harmony methods,
- own multiplayer authority decisions.

## Runtime Compatibility

The mod supports several S1API forks. Directly referencing S1API runtime event
types can make the mod fail to load when a fork has a different assembly
version. `Core.SubscribeS1ApiEvents()` finds event types by name and subscribes
with reflection for that reason.

When adding compatibility work:

- prefer reflection for optional third-party surface area,
- isolate fixes in `CompatibilityService`,
- log once or under verbose debug,
- never let a failed compatibility patch prevent the whole menu from loading.

## Harmony Patch Rules

Harmony patches are powerful but expensive to maintain. Use them when vanilla
code must be intercepted and there is no safer service-level path.

Good patch behavior:

- target exact known methods,
- keep prefix/postfix bodies short,
- delegate real logic into a service,
- catch expected IL2CPP/null-state failures,
- return vanilla execution only when safe.

Bad patch behavior:

- per-frame logging,
- broad exception swallowing without user-visible fallback,
- replacing vanilla multiplayer authority with custom side channels unless
  there is no vanilla path,
- touching unrelated systems from a patch.

## Scene And Authority Boundaries

Some features are intentionally scene-locked or host-locked.

- Save tools are main-menu-only. Editing save files while a world is loaded can
  race active game objects, networking, or save writers.
- Building placement is vanilla-owned. Any future custom placement feature must
  be explicitly host-authoritative and must not intercept normal grid snapping.
- Vehicle spawning and time changes are host-only by game design.
- Client-side visual helpers can run locally, but persistent world changes need
  host authority or a vanilla network path.

## Save Manager Rules

`SaveManagementService` reads the active profile from
`Application.persistentDataPath/Saves`, with a user-profile fallback:

```text
%USERPROFILE%\AppData\LocalLow\TVGS\Schedule I\Saves
```

Safety rules:

- only unlocked in the `Menu` scene,
- uses `System.Text.Json.Nodes` instead of string replacement,
- archive-delete moves slots under `Backups/NugzzDeleted`,
- backups go under `Backups/NugzzManual`,
- Steam Cloud handling only renames the local `steam_autocloud.vdf` marker.

The mod does not rewrite Steam client configuration. Fully disabling Steam Cloud
belongs in Steam's game properties.

## Logging

Use:

- `NotificationService.Notify(...)` for visible user messages,
- `NotificationService.Status(...)` for short status chips,
- `DebugLogService.Verbose(...)` for optional diagnostics,
- rate limits for anything that can happen every frame.

Do not use `Debug.LogError` for expected missing managers while in the main
menu. Expected transient states should either be silent, status-only, or verbose
debug.

## Build

```powershell
dotnet build SeshMenu.csproj -c Release
```

Output:

```text
bin/Release/net6.0/NugzzMenu.dll
```

## Copy Targets

Steam:

```text
C:\Program Files (x86)\Steam\steamapps\common\Schedule I\Mods\NugzzMenu.dll
```

Thunderstore profile paths vary by profile. Confirm the active profile before
overwriting a Thunderstore copy.

## Definition Of Done

A change is ready to hand to testers when:

- `dotnet build SeshMenu.csproj -c Release` succeeds,
- menu open/close still works in main menu and in-game,
- the touched tab does not resize into hidden controls,
- expected host/main-menu locks show clear feedback,
- logs stay quiet unless verbose debug is enabled,
- Steam and Thunderstore copies are only overwritten when requested.
