# Changelog

All notable NugzzMenu releases are recorded here. Detailed release notes remain
the authoritative record for each checkpoint.

## v0.9.9R4 - 2026-08-09

### Added

- Clothing color selection in the item spawner. Colorable clothing is created
  with the selected native clothing color, while fixed-color clothing keeps its
  original color.
- Host-controlled multiplayer access for matching NugzzMenu clients. Hosts can
  identify compatible clients and allow or deny their menu access in real time.
- Native console auto-complete with command descriptions, source labels, mouse
  selection, arrow-key navigation, and Tab completion.
- Interactive 3D shape spawning with type, size, color, static/physics modes,
  pickup prompts, a holding indicator, and a placement preview.
- Per-skateboard tuning for speed, steering, push force, stopping, and unlimited
  jumps, including current-board and all-board reset controls.
- Tracking for player-created drug mixtures, separated by product type with
  readable effect lists, spawning, and explicit deletion.
- Selected-seed auto planting and completion tools for meth cooks, lab ovens,
  mixing stations, cauldrons, and mushroom grow cycles.
- Bottomless garbage picker and expanded player-value synchronization support.

### Improved

- Compatibility with the current Schedule I IL2CPP assemblies and S1API. The
  project now preserves required runtime types and avoids fragile unstripping
  calls in active menu rendering paths.
- Menu ownership rules now cover pause, character creation, all phone apps,
  native and modded screens, the console, TV, jukebox, casino games, vehicles,
  skateboards, and active workstations.
- NugzzMenu remains available at the main menu for save management and in live
  saves for normal tools without closing itself after the cursor is unlocked.
- First-person viewmodel, held-item, FOV, and camera restoration were tightened
  around third-person transitions and native activities.
- Player scaling now uses the supported network value path and improved ground
  anchoring instead of writing invalid avatar customization constants.
- Vehicle, relationship, overlay, collision, temperature, camera, gravity,
  stamina, and ammo checks perform less repeated frame work.
- Benzies Manor access is now an idempotent one-time unlock instead of an
  expensive property search and repair running every frame.
- Vehicle/player collision isolation now waits for live vehicles to finish
  loading, discovers new vehicles at a low frequency, and only enables physics
  blockers near players.
- Repeated gravity, ammo, stamina, collision-state, temperature-display, and
  third-person camera work was removed from frame-critical paths.
- Unity log filtering now patches the native logger once instead of wrapping
  every public logging overload.
- Relationship lists filter internal or invalid NPC entries and resolve names
  more consistently.
- Temporary lobby effects no longer register unwanted Nugzz FX products in the
  player's product manager.
- Reset All Cheats and Runtime Changes now lives in Settings and includes the
  newly added runtime systems.

### Fixed

- Repeated `Method unstripping failed` exceptions in the menu and keybind HUD
  after the game update.
- The menu opening only at the main menu, or opening in a save and immediately
  closing again.
- Console suggestion rows being misaligned, blank on first open, or built with
  incompatible Unity UI components.
- Host approval appearing successful while a matching client remained locked
  out of NugzzMenu.
- First-person arms, held items, and weapon models remaining stretched, hidden,
  duplicated, or offset after camera and activity transitions.
- Shape prefabs casting a shadow without a visible model and being difficult to
  pick up or place.
- Skateboard tuning not applying distinctly per board and reset/unlimited-jump
  controls not taking effect.
- Vehicle HUD and camera state persisting after exits or menu transitions.
- Time presets using incorrect minute values and mixing completion leaving the
  timer or output unfinished.
- Frame spikes caused by repeated vehicle-tab scans and incomplete collision
  caches captured while a save was loading.
- Pause and character-creation screens now hard-block both the Nugzz menu and
  third-person controls. Pausing is also blocked while Nugzz owns the cursor.
- Vehicle collision protection no longer retains an incomplete collider set
  captured while a save is still loading, which could leave loaded traffic and
  parked vehicles in an invalid state.
- Removed the remaining legacy building-patch source so vanilla building and
  placement behavior are not intercepted by NugzzMenu.

Full notes: [RELEASE_NOTES_v0.9.9R4.md](RELEASE_NOTES_v0.9.9R4.md)

## v0.9.9R3 - 2026-07-17

### Added

- NPC/client Relations tab with relationship, unlock, addiction, product
  affinity, recommendation, and deal-offer controls.
- Deeper FPS controls for decorative lights, reflection refreshes, LOD, shadow
  distance, on-demand diagnostics, and restoring runtime defaults.
- Native-style left-click prompts for patched grow-tool interactions.
- Multiplayer-aware police lightbar and siren handling driven by the vehicle's
  synchronized headlight state.

### Improved

- Mushroom substrate, shroom spawn, spray bottle, watering, soil, seed,
  additive, and harvesting interaction targeting.
- Trash-container lookup while leaving vanilla bagging and quest progression in
  control.
- Vehicle HUD cleanup after leaving a vehicle.
- Compatibility filtering for repetitive IL2CPP logger and staggered-invoke
  messages.

### Fixed

- Spray bottles now increase and synchronize mushroom-bed moisture and consume
  water.
- Relations and Items search no longer depend on Unity's unsupported IL2CPP
  `TextEditor` path.
- The Relations tab no longer aborts its draw with `Method unstripping failed`.
- Mushroom grow interactions now show a clear left-click action prompt.

Full notes: [RELEASE_NOTES_v0.9.9R3.md](RELEASE_NOTES_v0.9.9R3.md)

## v0.9.9R2

Removed Build Anywhere, restored vanilla building ownership, rebuilt camera and
viewmodel transitions, added FPS and quest tools, and repaired several vanilla
quest and trash interactions.

Full notes: [RELEASE_NOTES_v0.9.9R2.md](RELEASE_NOTES_v0.9.9R2.md)

## Earlier Releases

- [v0.9.9](RELEASE_NOTES_v0.9.9.md)
- [v0.9.8](RELEASE_NOTES_v0.9.8.md)
- [v0.9.6](RELEASE_NOTES_v0.9.6.md)
- [v0.9.5](RELEASE_NOTES_v0.9.5.md)
- [v0.9.2](RELEASE_NOTES_v0.9.2.md)
- [v0.9.1](RELEASE_NOTES_v0.9.1.md)
- [v0.9.0](RELEASE_NOTES_v0.9.0.md)
- [v0.8.5](RELEASE_NOTES_v0.8.5.md)
