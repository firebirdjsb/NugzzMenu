# NugzzMenu v0.9.9R4

R4 is a large compatibility, multiplayer, control-safety, and feature release
built from the `v0.9.9R3` checkpoint. It updates NugzzMenu for the current
Schedule I IL2CPP runtime and S1API while keeping the menu available both at the
main menu and inside a live save.

This release also consolidates the work completed since R3 across multiplayer
authority, console usability, shapes, skateboards, production helpers, item
spawning, performance, camera recovery, player scaling, and native activity
protection.

## Highlights

- Updated the mod for the current game assemblies, IL2CPP interop surface, and
  S1API references.
- Added host-controlled access for matching NugzzMenu multiplayer clients.
- Added a professional command suggestion layer to the native game console.
- Added interactive static and physics-enabled 3D shapes.
- Added per-skateboard handling and jump tuning.
- Added clothing colors and remembered custom drug mixtures to item spawning.
- Added more grow and production automation.
- Removed the remaining legacy building patch.
- Reduced repeated frame work in several high-frequency systems.

## Current Game Compatibility

- Updated assembly references for the current Schedule I build, Unity modules,
  FishNet, and IL2CPP runtime types used by NugzzMenu.
- Added `link.xml` preservation entries for patched game and Unity types needed
  at runtime.
- Simplified compatibility paths that depended on reflection or methods no
  longer available after the game update.
- Replaced active IMGUI drawing calls that could throw `Method unstripping
  failed` with guarded, compatible rendering paths.
- Kept the main-menu save editor available while restoring full menu access in
  a loaded game.
- Fixed the menu opening in a save and then closing itself because NugzzMenu's
  own unlocked cursor was mistaken for another game screen.

## Multiplayer Access

- Added a session authority service that identifies NugzzMenu clients and their
  exact build version.
- Hosts can allow or deny a compatible client from the Lobby tab in real time.
- Clients receive the host decision and unlock the menu without reconnecting.
- Main-menu save tools remain local and do not require a multiplayer host.
- Added a supported player-value message path for NugzzMenu values that need to
  be shared between matching modded peers.
- Added focused host and client diagnostics for handshakes, approvals, and
  unknown session messages.

Custom NugzzMenu-only features still require compatible mod versions on every
peer that must interpret those custom values. Vanilla game networking remains
the preferred path wherever the game already exposes one.

## Native Activity Protection

- Menu and feature hotkeys are blocked while the game owns input for pause,
  character creation, phone apps, modded phone screens, the console, TV,
  jukebox, casino games, management screens, or production workstations.
- Opening pause while NugzzMenu is active safely closes the menu first.
- NugzzMenu no longer opens while the game is paused or while another protected
  activity owns the controls.
- Game hotkeys such as the phone are suppressed while NugzzMenu owns input.
- Fly and third-person controls are blocked while driving or riding a
  skateboard, preventing camera and movement state conflicts.

## Console Auto-Complete

- Integrated suggestions directly with the native Schedule I console.
- Suggestions appear when the console opens, even before text is entered.
- Added command syntax, descriptions, and source labels.
- Added mouse selection, arrow-key navigation, and Tab completion.
- Corrected row construction for IL2CPP Unity UI and fixed overlapping or
  misaligned text.
- Uses a light cream-green selection color consistent with NugzzMenu.

## Interactive Shapes

- Added cube, sphere, capsule, cylinder, and other simple shape prefabs.
- Shapes support type, size, color, and static or physics-enabled spawning.
- Physics shapes respond to gravity, impacts, players, dropping, and normal
  world collisions.
- Added clear interaction prompts, easier pickup targeting, a dedicated held
  shape indicator, and a placement ghost.
- Static shapes remain fixed after placement while preserving interaction.
- Added shape cleanup and runtime reset handling.

## Skateboards And Vehicles

- Added per-board speed, turn, push, and stop tuning.
- Added unlimited skateboard jumps plus current-board and all-board reset
  actions.
- Disabled incompatible fly and third-person transitions while riding.
- Improved skateboard camera recovery when NugzzMenu opens or closes.
- Reduced repeated vehicle discovery and tuning work while the Vehicles tab is
  visible.
- Improved vehicle collision caching so traffic and parked vehicles are not
  left invisible or with incomplete collider state during save loading.
- Tightened vehicle camera and HUD cleanup around menu and exit transitions.
- Kept police headlight, siren, and lightbar synchronization on the available
  vehicle network state.

## Player And Camera

- Reworked player scale application to avoid writing constant avatar fields.
- Improved foot grounding and reduced repeated vertical correction for scaled
  players.
- Added multiplayer player-value synchronization for matching R4 clients.
- Tightened first-person viewmodel and held-item restoration after third-person,
  vehicle, skateboard, console, and native screen transitions.
- Preserved the game's selected FOV instead of leaving a stale camera value.
- Added a custom FOV control up to 120 in the performance tools.

## Items And Production

- Added native clothing color selection for colorable clothes.
- Added lightweight tracking of mixtures the player creates.
- Custom mixtures are grouped by weed, meth, cocaine, and shrooms.
- The menu shows mixture effects separately for readability and supports
  spawning or deleting the selected saved mixture.
- Added selected-seed auto planting.
- Added completion controls for meth cooks, lab ovens, mixing stations,
  cauldrons, mushroom beds, and related grow cycles.
- Fixed mixing completion so both the process timer and output are finalized.
- Corrected Morning, Noon, Evening, and Midnight time presets.
- Added Bottomless Garbage Picker.

## Effects And Relationships

- Simplified temporary effect application and stopped it from registering
  unwanted `Nugzz FX` products in the product manager.
- Improved relationship list caching and display names.
- Filtered invalid internal NPC entries and Benzies placeholders that are not
  useful relationship targets.
- Preserved customer, addiction, affinity, recommendation, deal, and region
  controls from R3.

## Performance And Stability

- Removed repeated frame-critical work from gravity, stamina, ammo, camera,
  temperature, collision, overlay, and compatibility paths.
- Reduced full vehicle and collider scans and reused cached state where safe.
- Reduced the periodic heartbeat hitch in the Vehicles tab.
- Kept menu list scans throttled and on demand where possible.
- Added safer frame pacing, VSync, LOD, diagnostics, and custom FOV handling.
- Retained known-error filtering without replacing the underlying vanilla game
  methods.

## Vanilla Building Ownership

The remaining legacy `BuildingPatch` source has been removed. NugzzMenu does
not ship Build Anywhere or a replacement placement system in R4. Vanilla game
code owns normal build distance, preview visibility, collision validation, grid
snapping, and placement.

## Reset And Recovery

- Moved Reset All Cheats and Runtime Changes from Cheats to Settings.
- Expanded reset coverage to include newer player, camera, shape, skateboard,
  performance, and activity state.
- Kept Benzies Manor access as a one-time idempotent unlock instead of repeating
  an expensive property repair every frame.
- Preserved main-menu save backup, archive-delete, flag editing, and Steam Cloud
  controls.

## Source Transparency

Compared with `v0.9.9R3`, R4 changes the menu lifecycle and project references;
removes the legacy building patch; adds isolated services for console
auto-complete, activity gating, multiplayer authority, player values, shapes,
and skateboards; and updates the camera, effects, item, relationship, player,
performance, vehicle, grow, time, and UI modules that consume those services.

The complete source diff is available between the public `v0.9.9R3` and
`v0.9.9R4` tags.
