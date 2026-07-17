# NugzzMenu v0.9.9R3

R3 is a focused stability and control release built on the `v0.9.9R2`
checkpoint. It finishes the mushroom grow-tool interaction work, adds the new
Relations editor, expands performance tuning, improves vehicle multiplayer
behavior, and removes another IL2CPP-only UI failure.

Source delta from `v0.9.9R2`: **21 files changed, 2,341 insertions, and 232
deletions**. Most additions are isolated grow-tool, performance, relationship,
vehicle lifecycle, and release-documentation modules.

## Highlights

- Added a searchable NPC and client relationship editor.
- Expanded the FPS tab beyond basic game graphics settings.
- Added visible left-click prompts for patched grow-tool actions.
- Fixed spray bottles so they actually water mushroom beds.
- Improved shroom spawn, substrate, trash-bag, police-siren, and vehicle-HUD
  behavior.
- Replaced Unity's unsupported IL2CPP text editor path.

## Relations Tab

The new Relations tab builds its list from the game's NPC registry and keeps
the scan cached instead of searching the entire scene every frame.

It can:

- Search and page through named NPCs and clients.
- Display relationship, unlock, customer, delivery, and addiction details.
- Set relationship levels from `0` to `5`, with `0.25` adjustments.
- Unlock a selected person and their relationship connections.
- Adjust client addiction from `0%` to `100%`.
- Adjust affinity for weed, meth, cocaine, MDMA, shrooms, and heroin.
- Mark a client as recommended or request a deal offer.

Relationship changes use the game's relationship and customer setters. In a
multiplayer lobby, mutations are host-only. Viewing and searching remain
available to non-host users.

## FPS And Runtime Performance

The FPS tab now provides controls that target repeat runtime work while keeping
textures and primary world lighting intact:

- Frame caps from `60` through `240`, plus uncapped mode and VSync control.
- Decorative-light range budgets using the game's `OptimizedLight` system.
- Reflection refresh throttling using `ReflectionProbeUpdater`.
- Adjustable LOD bias and shadow distance.
- Balanced Performance and Low-Impact Menu presets.
- On-demand NPC, vehicle, optimized-light, and reflection-updater diagnostics.
- A Restore Runtime Defaults action for every new runtime override.

Diagnostics only scan when requested. Decorative-light rescans happen on scene
changes and at a slow maintenance interval while that optimizer is active, so
the performance tab does not create its own continuous scene-scan cost.

## Mushroom And Grow Tools

Patched grow interactions now show a left-mouse action prompt when a valid
target is under the cursor. Prompts cover:

- Watering plants and soil.
- Adding soil or mushroom substrate.
- Applying additives.
- Planting seeds.
- Adding shroom spawn to a mushroom bed.
- Misting a mushroom bed with a spray bottle.
- Harvesting plants and mushrooms.

The prompt follows the same validation as the action. It stays hidden when the
target is full, incompatible, already occupied, or the held item is empty.

### Spray Bottle Fix

The previous fallback could start the mist task without reaching its internal
spray-hit callback. The prompt appeared, but the bed's moisture did not change.

R3 now commits a successful click through the mushroom bed's own moisture and
network synchronization path. It fills the moisture bar, consumes spray-bottle
water, refuses already-moist beds, and synchronizes the resulting bed state.

### Substrate And Shroom Spawn

- Mushroom substrate targeting resolves the complete mushroom-bed interaction
  hierarchy instead of requiring one specific collider.
- Shroom spawn uses the game's own validation and task start path.
- Interaction aim assistance was widened slightly for grow containers without
  changing normal building placement or grid behavior.
- Consumed grow items continue to create their native trash object so related
  quests remain completable.

## Trash Bags And Quest-Safe Interactions

Trash-container lookup now checks the container, collider marker, visuals, and
container-item components through parent and child hierarchies.

The patch runs after the game's lookup, only supplying a fallback when vanilla
did not find a target. Native bag consumption, trash removal, networking, and
quest callbacks remain owned by the game.

## Vehicles And Multiplayer

### Police Sirens

Police lightbars and siren audio now follow the vehicle's synchronized
`HeadlightsOn` state. The native `H` key remains the control. R3 listens for the
normal vehicle-light visual update, RPC, and SyncVar changes and applies the
matching lightbar/audio state on receiving Nugzz clients.

This removes the old per-frame siren toggle state and keeps one networked source
of truth. The custom police lightbar mapping requires Nugzz on the receiving
client; the underlying headlight state still uses the game's network path.

### Vehicle HUD

The vehicle HUD is now cleaned up after the local player has definitively left
their seat. A lightweight recovery check also catches a missed vanilla exit
callback when the canvas, prompts, or current-vehicle reference remain stale.

## UI And IL2CPP Compatibility

Unity's normal `GUI.TextField` reaches `TextEditor.set_text`, which is not
available in this IL2CPP runtime and caused the Relations tab to stop drawing
with `Method unstripping failed`.

R3 uses a small IMGUI-native input control instead. It supports typing,
Backspace, Enter, Escape, focus changes, and independent field identities. Both
the Items and Relations searches now use this path.

## Logging And Compatibility

- Added support for the IL2CPP-object overload of Unity's contextual logger.
- Broadened the known staggered-invoke filter so equivalent messages from
  different logger overloads do not flood the console.
- Kept filtering separate from gameplay methods; these changes do not skip the
  underlying quest, NPC, or interaction calls.
- Retained one-time or throttled diagnostics for actual interaction failures.

## Complete File-Level Diff Inventory

### Entry Point And Release Files

- `Core.cs`: Relations tab routing and runtime performance service update.
- `Properties/AssemblyInfo.cs`: R3 assembly and MelonLoader metadata.
- `README.md`: R3 summary and expanded feature description.
- `CHANGELOG.md`: permanent release history and R3 changelog.
- `RELEASE_NOTES_v0.9.9R3.md`: this transparent release record.
- `docs/CODEBASE_MAP.md`: new service, renderer, and patch ownership.
- `SeshMenu.csproj`: Unity UI reference required by the new interaction paths.

### Existing Services Changed

- `Services/CompatibilityService.cs`: IL2CPP logger overload handling and known
  repetitive staggered-invoke filtering.
- `Services/GUIFit.cs`: IL2CPP-safe manual search input.
- `Services/GrowToolFallbackService.cs`: prompts, mushroom targeting, shroom
  spawn, spray bottle moisture, substrate, resource use, and diagnostics.
- `Services/PerformanceService.cs`: lights, reflections, LOD, shadows,
  diagnostics, presets, restoration, and throttled maintenance.
- `Services/ThirdPersonInteractionPatch.cs`: mushroom spawn and spray-bottle
  update routing plus quieter one-time exception reporting.
- `Services/TrashBagInteractionPatch.cs`: broader fallback target resolution
  while preserving the original game lookup.
- `Services/VehicleService.cs`: police-lightbar state application driven by
  synchronized vehicle lights.

### New Services And Patches

- `Services/PerformanceRuntimePatch.cs`
- `Services/PoliceSirenSyncPatch.cs`
- `Services/RelationshipService.cs`
- `Services/VehicleHudLifecyclePatch.cs`

### UI Changed Or Added

- `UI/ItemsTabRenderer.cs`: stable identity for the safe search control.
- `UI/PerformanceTabRenderer.cs`: expanded runtime controls and diagnostics.
- `UI/RelationshipsTabRenderer.cs`: searchable relationship/customer editor.

## Known Notes

- Relationship mutations are host-only in multiplayer.
- Custom police lightbar/siren mapping requires Nugzz on the receiving client.
- Runtime performance overrides are session controls and can be restored from
  the FPS tab.
- Grow-tool fallbacks intentionally complete valid actions quickly. They retain
  resource consumption, target validation, synchronization, and native trash
  creation, but some actions are faster than the original animation.
- Build Anywhere remains fully removed. Vanilla building still owns placement,
  snapping, validation, and networking.
- No files from unrelated mods are bundled in the release.

## Verification

- Configuration: `Release` / `net6.0`
- Build result: succeeded with `0` warnings and `0` errors
- DLL: `NugzzMenu.dll`
- Size: `403,968` bytes
- File version: `0.9.9.3`
- Product version: `0.9.9R3`
- SHA-256: `ABFF6AD0B0F42DB79D70D280B9CC76E5B4D78C3E25BD058B0078FCF68B7AE985`

The GitHub release asset is this exact verified DLL from the tagged source.
