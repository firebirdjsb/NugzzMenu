# NugzzMenu v0.9.9R2

This is a checkpoint release built from the full working diff since `v0.9.9`.
It records the camera, quest, building, performance, vehicle, grow-tool, UI, and
compatibility work without presenting the checkpoint as a finished `v1.0`.

Source delta from `v0.9.9`: **41 files changed, 6,033 insertions, and 2,374
deletions**. The unusually large deletion count is primarily the removed custom
building/placement implementation.

## Headline Changes

- Completely removed Build Anywhere, Place Anywhere, synthetic placement grids,
  outside-placement pickup handling, and every custom placement validation path.
- Rebuilt third-person camera handling around separate camera-state and
  viewmodel-visibility services.
- Added FPS and quest-control tabs.
- Restored native quest-variable execution that older compatibility patches
  could suppress.
- Added targeted quest recovery, consumed-item trash, and trash-bag interaction
  fixes for vanilla progression.
- Added jump and gravity controls, optional double-space fly, and vehicle flight.
- Added Nugzz-to-Nugzz vehicle tuning sync while retaining the game's native
  owned-vehicle color replication.

## Building: Removed Custom Placement Code

The entire Build Anywhere/Place Anywhere feature has been removed, not merely
disabled in the UI.

Removed behavior includes:

- Synthetic grids, tiles, GUID encoding, and network grid reconstruction.
- Custom floor, grid, surface, and procedural placement validation.
- Custom placement raycasts, distance handling, ghost positioning, and culling.
- Host-only outside placement and custom outside-object pickup/return handling.
- The Settings-tab placement toggle and saved placement state.
- Building update patches that changed vanilla placement or preview behavior.

What remains in the building files is narrowly scoped null-safety for broken grow
widgets and a safe hovered-buildable lookup. Vanilla code now owns placement,
grid snapping, preview visibility, collision checks, placement distance, and
networking.

## Camera And Viewmodels

- Added a dedicated over-the-shoulder third-person camera service.
- Added camera state capture/restore for the original camera parent, local
  position, local rotation, FOV, and look state.
- Added mode-specific viewmodel handling instead of globally hiding held items.
- Restored first-person arms, equipped items, punching arms, and pawn visibility
  after camera transitions.
- Preserved the game's native `V` avatar-view behavior.
- Third person now refuses or exits cleanly during vehicle use, skateboard use,
  management clipboard use, and active building placement.
- Skateboard mount/dismount and camera-mode transitions now return camera and
  viewmodel ownership to vanilla.
- Third-person interaction raycasts only run while the custom third-person
  override is genuinely active.
- Removed the custom `LookRaycast_ExcludeBuildables` interception so native
  building and management raycasts keep their original behavior.
- Vehicle-menu handling no longer writes private camera offsets or forces camera
  transforms. It blocks vehicle input/camera input while the menu is open and
  then releases the untouched camera back to vanilla.

Default third-person settings are now:

- Distance: `1.90`
- Height: `0.80`
- Shoulder offset: `0.20`

## Quest And Progression Repairs

The earlier compatibility layer contained Harmony prefixes that could skip
native `Player` and `VariableDatabase` get/set calls for selected missing
variables. Hiding a warning by preventing the game method from running could
also prevent native quest conditions and callbacks from running. Those
method-blocking patches are removed.

R2 instead:

- Recreates expected player and inventory variables from the game's own
  `VariableCreator` definitions where available.
- Restores commonly expected inventory counters without replacing native quest
  methods.
- Restores Dan's exact persistent greeting variable so his one-time free
  additive conversation does not reset every visit.
- Adds targeted recovery for the Welcome quest transition after reading the
  Benzies note.
- Adds a cached, ordered quest list with objective states and dynamic detail
  height.
- Adds Start, Complete, End, and Reset controls for the selected quest.
- Adds a read-only Welcome/RV quest-state inspector.

RV story safeguards were also changed:

- Normal Blow Up RV and Repair/Respawn RV actions refuse to alter the RV while
  the Welcome quest is active.
- The separate `Force RV + Complete Welcome (Skips Story)` action is the only
  intentional story-skip route and is labeled as such.
- Repairing or respawning the RV does not rewind completed quest state.

## Grow Tools, Trash, And Vanilla Quest Events

- Faster watering, soil, substrate, seed, and additive fallbacks now spawn the
  exact native trash prefab when a consumed item is exhausted.
- This preserves garbage-related progression such as `Keeping It Fresh` instead
  of silently deleting the empty container.
- Added mushroom substrate handling to soil-fill helpers and mushroom beds.
- Added a safe trash-container hover lookup for trash bags.
- Native trash bag consumption, container bagging, networking, and quest counter
  updates remain in the original game method.
- Existing guards still avoid consuming water, soil, seeds, or additives when a
  target is already full or cannot accept that item.

## Performance And UI

- Added an FPS tab with frame cap, VSync, a conservative smooth-visual preset,
  and a low-impact menu preset.
- Added a cached keybind HUD that can be disabled in Settings.
- Font application now runs only while the menu or a Nugzz notification is
  visible instead of being reapplied continuously.
- Quest discovery is cached and refreshed on demand rather than scanned every
  frame.
- The main-menu save manager is hidden during gameplay and replaced by a short
  explanation.
- Added an inline `SOLO/HOST`, `HOST`, or `NON-HOST` status beside the version.
- Added dynamic text fitting for controls that previously clipped.
- Added dedicated wide sizing for the FPS and Quest tabs.
- Removed duplicate Save Pos/Load Pos buttons from the Lobby tab.
- Removed Nugzz's backquote developer-console override; the vanilla console owns
  its input again.

## Cheats And Movement

- Added adjustable jump height from `0.1x` to `6x`.
- Added adjustable gravity from `0x` to `5x`.
- Added a persistent toggle for the double-space fly hotkey.
- Added optional driven-vehicle flight using the normal fly movement keys.
- Vehicle gravity is restored when vehicle flight stops.
- Moved Infinite Ammo directly below God Mode.

## Vehicles And Multiplayer

- Vehicle tuning ranges were reduced to more usable values instead of extreme
  multipliers.
- Tune payloads now include traction, steering, speed, brake strength,
  handbrake bite, headlight brightness/color, and body color.
- Nugzz clients broadcast and apply those tune payloads to the matching vehicle
  using a stable key plus vehicle code/position fallback.
- Duplicate payloads are suppressed and broadcasts are rate-limited.
- Owned body color still uses the game's native `SendOwnedColor` path.
- Vehicle input is blocked while the menu is open so a car cannot drive away
  behind the UI.

Multiplayer boundary: custom tuning values are interpreted only by another
client running this Nugzz build. Unmodded clients can receive effects sent by a
vanilla RPC, such as owned vehicle color, but they cannot interpret the custom
`Nugzz.VehicleTune` player value.

## Items, World Helpers, And Teleports

- Mushroom Bed, Mushroom Spawn Station, and Mushroom Substrate are explicitly
  categorized under Grow.
- The auto soil helper can fill mushroom beds with substrate.
- Removed the broad Parking Lot teleport scan that generated many duplicate
  destinations.
- Generic `POI Main Text` placeholders are filtered from World Teleports.
- Existing near-duplicate teleport filtering remains in place.

## Compatibility And Logging

- Runtime compatibility patches are installed once; Core no longer calls
  `PatchAll` a second time after MelonLoader has already installed attribute
  patches.
- Version-sensitive runtime patches use resolved overloads rather than assuming
  one Unity logger signature.
- Added a guarded Temperature Display update path for zero-length look vectors.
- Known missing-variable, pathfinding, collider, and other repetitive warnings
  can be filtered without skipping the underlying gameplay method.
- Removed the old compatibility prefixes that returned early from native
  variable get/set calls.

## Maintainer Changes

- Updated architecture, codebase map, feature playbook, and release-process
  documentation to state that building placement is vanilla-owned.
- Split camera ownership, state restoration, viewmodel visibility, performance,
  quest UI, and transition patches into dedicated files.
- `QuestService.cs` is intentionally called out as a large/high-risk checkpoint
  module. It is cached and functional, but should be split by discovery,
  mutation, and presentation responsibilities before `v1.0`.

## Known Limits At This Checkpoint

- The Welcome-note recovery is a compatibility patch. On affected saves the
  note may need to be read a second time before the next objective activates.
- Manual quest controls can skip story content and are scene/host sensitive.
  Back up a save before using them.
- Existing saves that never persisted Dan's greeting flag may receive his offer
  once more before R2 stores the corrected flag.
- The newest trash-bag hover and Dan persistence changes compile successfully
  but still need broad runtime testing across solo, host, and non-host saves.
- Custom vehicle tuning sync requires Nugzz on the receiving client.
- Build Anywhere and outside-placement pickup are intentionally unavailable.
- This checkpoint does not claim that every game-version or S1API-fork
  combination has been runtime tested.

## Complete File-Level Diff Inventory

### Entry Point And Metadata

- `Core.cs`: tab routing, preferences, HUD, host label, cursor/input handling,
  camera guards, performance/quest rendering, and duplicate patch removal.
- `Properties/AssemblyInfo.cs`: R2 assembly and MelonLoader version metadata.
- `README.md`: current release and checkpoint summary.
- `RELEASE_NOTES_v0.9.9R2.md`: this transparent checkpoint record.

### Existing Services Changed

- `Services/BuildingPatch.cs`: removed placement patches; retained grow and hover
  safety only.
- `Services/BuildingService.cs`: removed the synthetic placement system.
- `Services/CameraService.cs`: converted to a camera facade and transition
  coordinator.
- `Services/CompatibilityService.cs`: safe runtime patching/log filters and
  removal of gameplay-blocking variable patches.
- `Services/FlyingService.cs`: optional double-space hotkey and vehicle flight.
- `Services/GUIFit.cs`: additional fitted text/control helpers.
- `Services/GrowToolFallbackService.cs`: native trash generation and substrate
  handling.
- `Services/ItemService.cs`: mushroom Grow categorization and aliases.
- `Services/PlayerCheatService.cs`: jump/gravity tuning and vehicle-tune receive
  hook.
- `Services/TeleportService.cs`: placeholder/duplicate POI filtering.
- `Services/ThirdPersonInteractionPatch.cs`: third-person-only raycast scope.
- `Services/VehicleMenuCameraService.cs`: input lock without camera transform
  ownership.
- `Services/VehicleService.cs`: network tune payloads and quest-aware RV actions.
- `Services/WorldObjectService.cs`: mushroom substrate autofill.

### New Services And Patches

- `Services/CameraStateRestoreService.cs`
- `Services/KeybindOverlayService.cs`
- `Services/PerformanceService.cs`
- `Services/QuestService.cs`
- `Services/SkateboardCameraPatch.cs`
- `Services/ThirdPersonCameraService.cs`
- `Services/TrashBagInteractionPatch.cs`
- `Services/VanillaQuestVariablePatch.cs`
- `Services/VehicleCameraPatch.cs`
- `Services/ViewModelVisibilityPatch.cs`
- `Services/ViewModelVisibilityService.cs`
- `Services/WelcomeQuestProgressionPatch.cs`

### UI Changed Or Added

- `UI/CheatsTabRenderer.cs`: movement, fly, and control ordering.
- `UI/LobbyTabRenderer.cs`: removed duplicate position buttons.
- `UI/PropertiesTabRenderer.cs`: quest-aware and explicitly labeled RV actions.
- `UI/SettingsTabRenderer.cs`: keybind HUD toggle, no placement toggle, and
  main-menu-only save tools.
- `UI/VehicleTabRenderer.cs`: conservative tuning ranges.
- `UI/PerformanceTabRenderer.cs`: new FPS tab.
- `UI/QuestTabRenderer.cs`: new quest tab.

### Maintainer Documentation Changed

- `docs/ARCHITECTURE.md`
- `docs/CODEBASE_MAP.md`
- `docs/FEATURE_PLAYBOOK.md`
- `docs/RELEASE_PROCESS.md`

## Verification

- Configuration: `Release` / `net6.0`
- Build result: succeeded with `0` warnings and `0` errors
- DLL: `NugzzMenu.dll`
- Size: `375,808` bytes
- File version: `0.9.9.2`
- Product version: `0.9.9R2`
- SHA-256: `D5E565FAF7ABA620097D876A196E78AD902924B2B98A18092F743A057540DB7C`

The GitHub release asset is this exact verified DLL from the tagged source.
