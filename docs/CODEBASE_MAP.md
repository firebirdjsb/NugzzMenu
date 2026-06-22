# NugzzMenu Codebase Map

This map gives maintainers a quick way to find the right file before changing
behavior. Keep it updated when adding major services, tabs, or patches.

## Entry Point

| File | Responsibility |
| --- | --- |
| `Core.cs` | MelonLoader lifecycle, preferences, menu open/close, input/cursor state, tab routing, S1API event reflection, top-level service update calls. |
| `SeshMenu.csproj` | Build settings and local reference assembly list. |
| `Properties/AssemblyInfo.cs` | Assembly version metadata. |

## UI Renderers

| File | Responsibility |
| --- | --- |
| `UI/CheatsTabRenderer.cs` | God mode, stamina, ammo, wanted status, fly, speed, player scale, third-person camera, teleport helpers. |
| `UI/MoneyTabRenderer.cs` | Cash, online balance, and XP buttons. |
| `UI/TimeTabRenderer.cs` | Time speed, time-of-day, plant growth, drying rack completion. |
| `UI/VehicleTabRenderer.cs` | Vehicle selection, spawning, current vehicle tuning, siren/headlight controls. |
| `UI/PropertiesTabRenderer.cs` | Worker/property management, RV controls, Benzie Manor access. |
| `UI/ItemsTabRenderer.cs` | Item browser, search/filter, quality, quantity, item spawn actions. |
| `UI/LobbyTabRenderer.cs` | Lobby player list, teleport/ragdoll, lobby-visible FX. |
| `UI/SettingsTabRenderer.cs` | Keybinds, item spawner mode, debug toggle, building toggle, save manager. |

UI renderers should remain mostly declarative. If the renderer starts needing
try/catch blocks around game objects or filesystem work, move that logic into a
service.

## Core Services

| File | Responsibility |
| --- | --- |
| `Services/NotificationService.cs` | User-facing notifications and status chips. |
| `Services/DebugLogService.cs` | Verbose debug logging toggle and helpers. |
| `Services/GUISystemService.cs` | Menu textures, colors, fonts, styles, and theme. |
| `Services/GUIFit.cs` | Text-fitting wrappers for IMGUI controls. |
| `Services/TMPHybridService.cs` | Text drawing bridge used by the custom UI. |
| `Services/ManagerCacheService.cs` | Cached access to frequently used game managers and local player references. |
| `Services/CompatibilityService.cs` | Runtime compatibility patches and log-spam filters for game/S1API differences. |

## Gameplay Services

| File | Responsibility |
| --- | --- |
| `Services/PlayerCheatService.cs` | God mode, stamina, ammo, speed boost, player scale, lethal kill handling, related patches. |
| `Services/FlyingService.cs` | Fly mode movement and post-movement lock. |
| `Services/CameraService.cs` | Third-person camera, over-the-shoulder position, visibility restoration, interaction raycasts, punch/melee safety. |
| `Services/VehicleMenuCameraService.cs` | Keeps vanilla vehicle camera stable while opening/closing the menu in a driven vehicle. |
| `Services/VehicleCollisionService.cs` | Player/vehicle collision adjustments so player hitboxes do not push vehicle physics. |
| `Services/VehicleService.cs` | Vehicle cache, vehicle spawn paths, RV repair/blow-up, Benzie access, sirens, current vehicle tuning. |
| `Services/ItemService.cs` | Item cache, filtering, quality-aware spawning, pending spawn queue, inventory helpers. |
| `Services/EconomyService.cs` | Cash and online balance adjustments. |
| `Services/GameManagerService.cs` | Local player, player health, crime data, and XP helpers. |
| `Services/TimeManagerService.cs` | Host-guarded time speed and time-of-day changes. |
| `Services/WorldObjectService.cs` | Plant and drying rack world helpers. |
| `Services/TeleportService.cs` | Save/load position helper. |
| `Services/LobbyService.cs` | Host checks, lobby player list, teleport/ragdoll helpers. |
| `Services/EffectsService.cs` | Lobby-visible FX through vanilla product/effect paths. |
| `Services/PropertyWorkerService.cs` | Owned property labels, employee transfer/hire/fire rules, unsupported worker-capacity checks. |
| `Services/ManagementClipboardService.cs` | Keeps vanilla management clipboard selection/linking working with menu/camera patches. |
| `Services/SaveManagementService.cs` | Main-menu-only save slot inspection, backups, archive deletion, save JSON edits, local Steam Cloud marker handling. |

## Building And Placement

| File | Responsibility |
| --- | --- |
| `Services/BuildingService.cs` | Place Anywhere grids, synthetic tiles, placement precision, outside-item pickup, surface/grid/procedural placement safety. |
| `Services/BuildingPatch.cs` | Harmony patches that route vanilla build update methods into `BuildingService`. |

The building service is intentionally large because it contains several
closely-related placement paths. When changing it, keep the public service API
stable and add small private helpers instead of putting more logic in patches.

## Interaction And Safety Patches

| File | Responsibility |
| --- | --- |
| `Services/ThirdPersonInteractionPatch.cs` | Camera raycast patches, punch/melee hit safety, avatar/NPC/trimmer/management clipboard null-safety. |
| `Services/BuildingPatch.cs` | Placement and outside pickup patches. |
| `Services/VehicleCollisionService.cs` | Vehicle/player collision patches. |
| `Services/PlayerCheatService.cs` | Health/stamina/networked value patches. |
| `Services/CompatibilityService.cs` | Optional runtime patches for spammy or version-sensitive game paths. |

Patch classes should be small routers. If a patch grows complicated, move the
logic into the matching service and call that service from the patch.

## High-Risk Areas

- Building placement and management clipboard both touch vanilla selection and
  interaction paths. Test them together after camera or raycast changes.
- Vehicle camera behavior depends on vanilla camera mode. Avoid forcing camera
  transforms when the player is driving unless the vehicle camera service owns
  that exact transition.
- Player scale is visually fragile in multiplayer. Avoid claiming vanilla-wide
  sync unless unmodded clients have actually been tested.
- Save edits must stay main-menu-only.
- Broad `Resources.FindObjectsOfTypeAll` scans must be cached and rate-limited.

## Where To Add A New Thing

| New thing | Put it here |
| --- | --- |
| New menu tab controls | Existing `UI/*TabRenderer.cs`, or a new renderer if it is a new tab. |
| New gameplay operation | New or existing `Services/*Service.cs`. |
| New Harmony patch | Same service file if tiny, or a nearby patch file if there are multiple related patches. |
| New setting | `SettingsState`, `SettingsTabRenderer`, Melon preferences in `Core` if persistent. |
| New release docs | `RELEASE_NOTES_vX.Y.Z.md` and README current release section. |
| New contributor docs | `docs/`. |
