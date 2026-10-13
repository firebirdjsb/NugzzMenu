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
| `UI/CheatsTabRenderer.cs` | God mode, stamina, ammo, wanted status, fly, jump/gravity, player scale, third-person camera, teleport helpers. |
| `UI/MoneyTabRenderer.cs` | Cash, online balance, and XP buttons. |
| `UI/TimeTabRenderer.cs` | Time speed, time-of-day, weather controls, plant growth, drying rack completion. |
| `UI/VehicleTabRenderer.cs` | Vehicle selection, spawning, current vehicle tuning, siren/headlight controls. |
| `UI/MotorcycleRenderer.cs` | Host spawn and unoccupied removal controls for session-only rideable motorcycles. |
| `UI/PropertiesTabRenderer.cs` | Worker/property management, RV controls, Benzie Manor access. |
| `UI/ItemsTabRenderer.cs` | Item browser, search/filter, quality, quantity, item spawn actions. |
| `UI/ClothingColorPicker.cs` | Native/default clothing selection, 27 tint swatches, tone shortcuts, and native finish/pattern spawns. |
| `UI/LobbyTabRenderer.cs` | Lobby player list, teleport/ragdoll, lobby-visible FX. |
| `UI/LocalAnimationRenderer.cs` | Eight local-player NPC animation actions, active status and Stop within Local Effect. |
| `UI/PerformanceTabRenderer.cs` | Frame pacing, lights, reflections, LOD, shadows, diagnostics, and performance presets. |
| `UI/RelationshipsTabRenderer.cs` | Searchable NPC/client list and relationship/customer editing controls. |
| `UI/QuestTabRenderer.cs` | Cached quest list, objective details, inspection, and manual quest controls. |
| `UI/SettingsTabRenderer.cs` | Keybinds, item spawner mode, debug/HUD toggles, and main-menu save manager. |

UI renderers should remain mostly declarative. If the renderer starts needing
try/catch blocks around game objects or filesystem work, move that logic into a
service.

## Core Services

| File | Responsibility |
| --- | --- |
| `Services/NotificationService.cs` | User-facing notifications and status chips. |
| `Services/DebugLogService.cs` | Verbose debug logging toggle and helpers. |
| `Services/GUISystemService.cs` | Menu textures, colors, fonts, styles, and theme. |
| `Services/GUIFit.cs` | Text-fitting wrappers and IL2CPP-safe keyboard input for IMGUI controls. |
| `Services/TMPHybridService.cs` | Text drawing bridge used by the custom UI. |
| `Services/ManagerCacheService.cs` | Cached access to frequently used game managers and local player references. |
| `Services/CompatibilityService.cs` | Runtime compatibility patches and log-spam filters for game/S1API differences. |
| `Services/S1ApiService.cs` | Public S1API item/NPC/weather discovery, time changes, runtime version reporting, and optional API lookup. |
| `Services/KeybindOverlayService.cs` | Cached, low-cost in-game keybind HUD. |
| `Services/PerformanceService.cs` | FPS/VSync, decorative lights, reflections, LOD, shadows, diagnostics, and conservative presets. |
| `Services/RelationshipService.cs` | Live NPC/client discovery, including visiting and S1API NPCs, and host-guarded relationship/customer mutations. |
| `Services/SpecialCustomerService.cs` | Native visiting-group rosters, inactive visitor metadata, and host-only next-day scheduling. |
| `Services/MenuConfig.cs`, `Services/MenuConfigService.cs` | Validated startup preferences beside the mod DLL, explicit reload/save, and atomic replacement with backup. |
| `Services/UpdateContentAuditService.cs` | Read-only loaded-save verification of new item, roster, cosmetic, teleport, and business registrations. |

## Gameplay Services

| File | Responsibility |
| --- | --- |
| `Services/PlayerCheatService.cs` | God mode, stamina, ammo, speed boost, player scale, lethal kill handling, related patches. |
| `Services/LocalAnimationCatalog.cs`, `LocalAnimationService.cs` | Verified native pose names, local owner RPCs, parameter snapshots, temporary phone handler and cleanup. |
| `Services/LocalAnimationNetworkService.cs` | Native solo/server authority, owner-checked requests, host approval, matching readiness, trusted observer states and snapshots. |
| `Services/LocalAnimationVisualService.cs` | Temporary native chair/laptop/motorcycle meshes, whole-avatar seat alignment, once-per-frame pose evaluation, wheel spin and cleanup. |
| `Services/LocalBodyAnimationService.cs` | Authored clothed player-joint curves, current appearance attachment bindings, fixed snapshots and pose restoration. |
| `Services/LocalAnimationRenderService.cs` | Built-in and URP camera callbacks applying final body, prop-seat and rider poses; delegate lifecycle cleanup. |
| `Services/LocalAnimationPlacementService.cs` | Ground and current attachment hip discovery, safe child-avatar root alignment and native seat-facing compensation. |
| `Services/NativeVisualCloneService.cs` | Render-only native hierarchy copying and skinned-mesh bone remapping. |
| `Services/MotorcycleService.cs`, `MotorcycleChassisSnapshot.cs` | Session-only native chassis conversion, riding interaction, saddle poses and reversible clone settings. |
| `Services/MotorcycleNetworkService.cs` | Trusted host native-object tags, matching peer readiness, bounded pending attachments and observer snapshots. |
| `Services/NeverWantedService.cs` | Local host-player crime/detection/pursuit/search/combat guards and cleanup of active police AI. |
| `Services/NativeActivityGateService.cs` | Native UI ownership gates and closed-phone Character gameplay-state cleanup. |
| `Services/FlyingService.cs` | Player fly mode, optional double-space hotkey, and driven-vehicle flight. |
| `Services/CameraService.cs` | Public camera facade, interaction raycasts, and camera transition coordination. |
| `Services/ThirdPersonCameraService.cs` | Over-the-shoulder camera ownership, collision, input, and mode guards. |
| `Services/CameraStateRestoreService.cs` | Captures and restores vanilla camera parent, transform, FOV, and look state. |
| `Services/ViewModelVisibilityService.cs` | Mode-specific pawn, first-person viewmodel, held-item, and punch visibility. |
| `Services/VehicleMenuCameraService.cs` | Pauses vehicle input while the menu is open without moving the vanilla vehicle camera. |
| `Services/VehicleCollisionService.cs` | Player/vehicle collision adjustments so player hitboxes do not push vehicle physics. |
| `Services/VehicleService.cs` | Vehicle cache, vehicle spawn paths, RV repair/blow-up, Benzie access, sirens, current vehicle tuning. |
| `Services/ItemService.cs` | Item cache, filtering, quality-aware spawning, pending spawn queue, inventory helpers. |
| `Services/InteractionPromptDisplayService.cs` | Current-frame custom prompt presentation after the native equipped-tool hide pass; preserves native display intent. |
| `Services/GrowPromptOwnershipService.cs` | Current-frame grow-target message ownership and suppression of conflicting native display writes. |
| `Services/EconomyService.cs` | Cash and online balance adjustments. |
| `Services/GameManagerService.cs` | Local player, player health, crime data, and XP helpers. |
| `Services/TimeManagerService.cs` | Host-guarded time speed and time-of-day changes. |
| `Services/WeatherService.cs` | Host-guarded weather presets and confirmed lightning events. |
| `Services/WorldObjectService.cs` | Plant and drying rack world helpers. |
| `Services/TeleportService.cs` | Save/load position helper and native console/property/POI teleport catalog. |
| `Services/LobbyService.cs` | Host checks, lobby player list, teleport/ragdoll helpers. |
| `Services/EffectsService.cs` | Lobby-visible FX through vanilla product/effect paths. |
| `Services/PropertyWorkerService.cs` | Owned property labels, employee transfer/hire/fire rules, unsupported worker-capacity checks. |
| `Services/ManagementClipboardService.cs` | Keeps vanilla management clipboard selection/linking working with menu/camera patches. |
| `Services/SaveManagementService.cs` | Main-menu-only save slot inspection, backups, archive deletion, save JSON edits, local Steam Cloud marker handling. |
| `Services/QuestService.cs` | Cached quest discovery, ordering, details, manual controls, and RV/Welcome quest safeguards. |

## Building And Placement

| File | Responsibility |
| --- | --- |
| `Services/BuildingService.cs` | Grow UI safety handling only. Vanilla building and placement are intentionally left untouched. |
| `Services/BuildingPatch.cs` | Grow-container safety finalizers that route noisy broken widgets into `BuildingService`. |

Building placement should remain vanilla unless a future feature explicitly
reintroduces a host-authoritative placement system.

## Interaction And Safety Patches

| File | Responsibility |
| --- | --- |
| `Services/ThirdPersonInteractionPatch.cs` | Third-person-only camera raycasts plus punch/melee and interaction safety. |
| `Services/ViewModelVisibilityPatch.cs` | Equip, punch, and native avatar-view visibility transitions. |
| `Services/SkateboardCameraPatch.cs` | Forces a clean vanilla camera/viewmodel state around skateboard use. |
| `Services/VehicleCameraPatch.cs` | Vehicle-entry third-person guard and menu input locks. |
| `Services/WelcomeQuestProgressionPatch.cs` | Targeted recovery for the post-Benzies-note Welcome quest transition. |
| `Services/VanillaQuestVariablePatch.cs` | Restores expected native player/inventory variables and Dan's persistent greeting flag. |
| `Services/TrashBagInteractionPatch.cs` | Safe trash-container hover lookup while preserving native bagging behavior. |
| `Services/PerformanceRuntimePatch.cs` | Routes reflection-probe refreshes through the optional performance throttle. |
| `Services/PoliceSirenSyncPatch.cs` | Maps native synchronized vehicle headlight state to police lightbars and siren audio. |
| `Services/VehicleHudLifecyclePatch.cs` | Clears stale vanilla vehicle HUD state after the local player exits. |
| `Services/BuildingPatch.cs` | Grow widget safety and buildable-hover null-safety; it does not alter placement. |
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
| New setting | `SettingsState`, `SettingsTabRenderer`, validated `MenuConfig` startup defaults, and compatibility Melon preferences in `Core`. |
| New release docs | `RELEASE_NOTES_vX.Y.Z.md` and README current release section. |
| New contributor docs | `docs/`. |

## Update Verification

- `scripts/update_s1api.py` verifies the locked upstream ZIP and stages only the
  IL2CPP dependency pair without installing it.
- `scripts/audit_update_assets.py` reads serialized game assets against
  `docs/0.4.7-content.json`; UnityPy is an isolated analysis dependency.
- `tests/GameApiCompatibilityChecks` audits compiled bindings and static/dynamic
  Harmony targets against the supplied metadata dump and public S1API.
- `tests/AccessoryFailureChecks`, `tests/InteractionPromptFrameChecks`,
  `tests/MenuConfigChecks`, and `tests/NativeStateChecks` cover the narrow
  exception, display, config, and phone/police/prompt policies. Fixtures do not
  replace in-game visual or multiplayer QA.
- `tests/ClothingPickerChecks` checks the real generated GUI overload call
  graph and repeated picker rendering/input/tint restoration with a narrow
  fixture, including a before-fix DLL as the failure baseline.

Test packages may include the pinned official S1API mod/loader with its
license, an example config, and small audit reports. Exclude game assets,
dumps, generated interop references, and prior DLL backups.
