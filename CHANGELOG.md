# Changelog

All notable NugzzMenu releases are recorded here. Detailed release notes remain
the authoritative record for each checkpoint.

## v1.0.0 - 2026-08-26

v1.0.0 is the controller, interface, and usability release. It preserves the
complete v0.9.9R4 feature set while rebuilding the shared interaction and
rendering layers for controllers, mouse, and keyboard.

### Added - Controller Support

- Full Xbox and PlayStation controller support across Cheats, Money, Time,
  Vehicles, Properties, Items, Lobby, FPS, Relations, Quests, and Settings.
- Automatic Xbox and PlayStation layout detection through Unity's Input System,
  with a legacy joystick-input fallback for compatible controllers.
- `LB + RB + D-pad Up` on Xbox or `L1 + R1 + D-pad Up` on PlayStation opens and
  closes NugzzMenu.
- D-pad and left-stick menu navigation with dead-zone handling, initial delay,
  held-direction repeat, wraparound focus, and automatic focused-row scrolling.
- `LB/RB` or `L1/R1` changes tabs, `A/X` selects, `B/O` closes,
  `X/Square` runs Reset All Cheats and Runtime Changes, and `Y/Triangle` toggles
  the controller help panel.
- `LB + RB + D-pad Down` or `L1 + R1 + D-pad Down` toggles third-person mode.
- Right-stick camera orbit in third person with independent yaw and pitch rates.
- Controller flight support: double-tap `A/X` to toggle, move with the left
  stick, ascend with `A/X`, and descend with `B/O`.
- Device-aware menu footer and keybind HUD. Xbox, PlayStation, and keyboard/mouse
  prompts are shown only when relevant to the connected input device.

### Added - Interface

- A redesigned dark-green menu shell with a custom Nugzz logo, dimmed backdrop,
  soft shadow, layered header, content surfaces, refined tabs, and clearer
  notification and status areas.
- Smooth 0.22-second opening and 0.15-second closing slide-and-fade animations
  driven by unscaled time.
- A persistent controller focus outline that remains active after the menu is
  opened with a controller and is not stolen by incidental mouse movement.
- Contextual controller help and compact Xbox or PlayStation button glyph chips
  for tabs, navigation, selection, closing, reset, and help.
- Shared polished slider tracks and centered slider thumbs for all converted
  numeric controls.
- Supersampled, bilinear-filtered rounded textures and nine-sliced GUI styles so
  curved panels, buttons, tabs, focus rings, and backdrops remain smooth when
  menu dimensions change.

### Changed - Menu Controls

- Speed multiplier is now a slider from `0.25x` to `10.00x` in `0.25x` steps.
- Player size is now a slider from `0.25x` to `4.00x` in `0.25x` steps.
- Jump height is now a slider from `0.25x` to `5.00x` in `0.25x` steps.
- Gravity is now a slider from `0.25x` to `3.00x` in `0.25x` steps.
- Flight speed is now a slider from `5` to `100` in steps of `5`.
- Third-person distance, height, and shoulder offset now use controller-ready
  sliders instead of separate increment and decrement buttons.
- Vehicle tuning sliders now use the same mouse and controller interaction path
  as the rest of the menu.
- Item spawn quantity is now one slider covering `1` through `100` instead of a
  row of fixed quantity buttons.
- The four fixed time presets were replaced by a minute-accurate slider covering
  `06:01` through `04:00` the next day, with a separate Apply Time action.
- Dynamic tab sizing, explicit scroll controls, mouse-wheel scrolling, and
  controller auto-scroll now share the same measured content area.

### Changed - Items and Stacks

- Spawned quantities are split using each item's native stack limit instead of
  creating oversized item instances.
- Inventory insertion now verifies the destination slot can accept the complete
  incoming stack before placing it.
- Large requests continue across legal stacks until the requested quantity is
  inserted or inventory capacity is exhausted.
- Spawn status now reports the inserted and requested quantities when only part
  of a request fits.
- Existing quality, clothing-color, and game stack-logic options remain active
  with the new quantity slider.

### Changed - Multiplayer Authorization

- Hosts can authorize every compatible player at once with `Allow All + Late
  Joiners`.
- Auto-allow remains active for compatible players who join after the bulk
  approval action and resets when the lobby session changes.
- `Deny All / Stop Auto-Allow` revokes current bulk approvals and disables
  automatic approval for later joins.
- Per-player allow and deny controls remain available.
- The handshake now accepts compatible NugzzMenu v0.9.9R4 and newer clients
  instead of requiring an exact assembly build match.
- Host responses use each client's compatible protocol token so R4 clients can
  recognize and accept authorization from a v1.0.0 host.

### Improved

- Buttons, toggles, sliders, text fields, tab controls, and scroll controls now
  share one focus and input-registration layer for consistent mouse, keyboard,
  Xbox, and PlayStation behavior.
- Controller ownership persists for the entire controller-opened menu session;
  focus styling and controller instructions no longer disappear after a click
  or minor mouse movement.
- The menu retains input ownership throughout the closing animation, preventing
  phone, map, pause, hotbar, and other gameplay actions from firing underneath
  the fading window.
- Additional native UI cycle, tab, map, scrollbar, and quantity callbacks are
  blocked while NugzzMenu owns input.
- The keybind HUD no longer uses an oversized background panel. It uses outlined
  text, clamps to the current screen width, and refreshes immediately when the
  connected controller layout changes.
- Menu labels adapt to the active device for flight, third person, navigation,
  selection, and reset instructions.
- Main-menu save tools, `F8`, mouse navigation, keyboard hotkeys, text entry,
  `G` third person, and keyboard flight controls remain supported.

### Performance

- Animation frames render a lightweight shell instead of executing the active
  tab renderer and registering every interactive control during each fade.
- Transition rendering now skips non-Repaint IMGUI events and avoids scaling the
  full menu through `GUI.matrix`, eliminating open and close animation hitching.
- Redundant scene-state refreshes were removed from the animated OnGUI path.
- Vehicle collision proxies now track active visible vehicles, synchronize
  consistently, remove stale proxies, and restore ignored collision pairs when
  a vehicle disappears.

### Fixed

- Controller focus highlighting disappearing immediately and reverting to
  keyboard/mouse instructions after controller selection.
- Background gamepad and keyboard actions opening the phone, map, or other game
  screens while NugzzMenu is open or closing.
- Third-person camera rotation not responding to the controller right stick.
- Double-tap flight and active flight movement not responding to controllers.
- Slider thumbs appearing vertically off-center.
- Jagged rounded corners on the window, panels, buttons, tabs, and backdrop.
- Controller glyph labels being clipped or hidden in the footer.
- Keybind HUD showing controller instructions without a connected controller,
  or keyboard instructions while a controller layout is active.
- Vehicle editor lookup failing while the local player is driving. Vehicle
  detection now checks the active movement vehicle before seat-based fallbacks.
- Vehicle collision optimization leaving some cars invisible or allowing
  players to pass through them after vehicles loaded, unloaded, or changed.
- Exact-build multiplayer checks preventing R4 and v1.0.0 players from being
  authorized together.
- Host bulk authorization affecting only one current player instead of every
  compatible player and future late joiners.
- Item quantities creating oversized or partially inserted stacks that could
  split incorrectly after spawning.

### Compatibility

- Version metadata, assembly metadata, UI labels, and lobby advertisements now
  identify the build as `1.0.0`.
- Built for the current Schedule I IL2CPP assemblies, MelonLoader, and S1API
  reference set used by v0.9.9R4.
- Host authorization remains protocol-compatible with NugzzMenu v0.9.9R4 and
  newer clients. Both players still need compatible Schedule I and S1API builds.
- No tab or v0.9.9R4 gameplay feature was removed for the controller redesign.
- Controllers can focus and activate text fields; entering text still uses the
  keyboard because v1.0.0 does not add an on-screen keyboard.

### Verification

- Release configuration builds successfully with zero compiler warnings and
  zero compiler errors.
- The deployed DLL reports assembly version `1.0.0.0`.
- Every interactive menu renderer routes through the shared controller-aware GUI
  layer, including vehicle tuning and item, time, camera, movement, and flight
  sliders.

Full notes: [RELEASE_NOTES_v1.0.0.md](RELEASE_NOTES_v1.0.0.md)

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
