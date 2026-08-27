# NugzzMenu v1.0.0

Release date: August 26, 2026

NugzzMenu v1.0.0 is the controller, interface, and usability release. It keeps
the complete v0.9.9R4 feature set while rebuilding the shared menu interaction
and rendering layers for controller, mouse, and keyboard users.

## Highlights

- Full Xbox and PlayStation controller support across every tab and control.
- A redesigned, polished dark-green interface with smooth rounded surfaces,
  custom sliders, persistent focus, controller glyphs, and optimized animation.
- Controller support for flight and third-person camera controls.
- Host-wide R4+ multiplayer authorization with automatic late-join approval.
- Slider-based item quantity, time, movement, flight, camera, and vehicle tuning.
- Stack-safe item spawning and corrected vehicle editor and collision behavior.

## Controls

| Action | Xbox | PlayStation | Keyboard / Mouse |
| --- | --- | --- | --- |
| Open or close menu | `LB + RB + D-pad Up` | `L1 + R1 + D-pad Up` | `F8` |
| Previous / next tab | `LB / RB` | `L1 / R1` | Mouse |
| Navigate | D-pad or left stick | D-pad or left stick | Mouse / scroll wheel |
| Select | `A` | `X` | Left click |
| Close / cancel | `B` | `O` | `F8` |
| Reset runtime changes | `X` | `Square` | Settings action |
| Toggle help | `Y` | `Triangle` | Controller only |
| Toggle third person | `LB + RB + D-pad Down` | `L1 + R1 + D-pad Down` | `G` |
| Third-person camera | Right stick | Right stick | Mouse |
| Toggle fly | Double-tap `A` | Double-tap `X` | Double-tap Space |
| Fly movement | Left stick | Left stick | `WASD` |
| Fly ascend / descend | `A / B` | `X / O` | Space / Ctrl |

Controller input uses Unity's current Input System and falls back to legacy
joystick input when required. Xbox and PlayStation layouts are detected from the
connected device and refresh while the game is running.

## Controller Navigation

- Every button, toggle, slider, and custom text field participates in one shared
  focus system.
- D-pad and left-stick navigation use dead zones, an initial repeat delay, and a
  faster held-direction repeat rate.
- Focus wraps through available controls and dense tabs automatically scroll to
  keep the selected row visible.
- Opening with a controller gives that controller ownership for the complete menu
  session. Incidental mouse movement no longer removes the focus highlight or
  swaps the footer back to keyboard instructions.
- The controller footer uses Xbox or PlayStation glyph chips for tabs,
  navigation, select, close, reset, and help.
- A controller help panel explains the full menu combination and glyph layout.

## Interface Redesign

- Rebuilt the main window with a custom Nugzz logo, dimmed game backdrop, soft
  shadow, layered header, inset content surface, refined tabs, and clearer status
  and notification areas.
- Replaced low-resolution square fills with 64x64 supersampled rounded gradient
  textures, bilinear filtering, and nine-sliced styles. Rounded corners stay
  smooth across dynamic window sizes instead of becoming jagged or stretched.
- Added dedicated styles for the window shadow, header, content area, controller
  focus, sliders, slider thumbs, and controller prompt chips.
- Changed selected and focused states to the Nugzz cream-green palette.
- Centered slider thumbs against their tracks and tightened label alignment.
- Added a smooth unscaled-time slide-and-fade animation: 0.22 seconds when
  opening and 0.15 seconds when closing.
- Optimized animation frames to draw only a lightweight window shell on Repaint
  events. The active tab is no longer recalculated during every fade frame, the
  full menu is no longer matrix-scaled, and redundant scene refreshes were
  removed from the transition path.

## Updated Sliders

- Speed multiplier: `0.25x` to `10.00x`, step `0.25x`.
- Player size: `0.25x` to `4.00x`, step `0.25x`.
- Jump height: `0.25x` to `5.00x`, step `0.25x`.
- Gravity: `0.25x` to `3.00x`, step `0.25x`.
- Flight speed: `5` to `100`, step `5`.
- Third-person distance: `0.5` to `8.0`, step `0.25`.
- Third-person height: `-1.0` to `3.0`, step `0.1`.
- Third-person shoulder offset: `-2.0` to `2.0`, step `0.1`.
- Item spawn quantity: `1` to `100`, step `1`.
- Time selection: minute-accurate range from `06:01` through `04:00` the next
  day, followed by an explicit Apply Time action.
- Vehicle editor values now use the same controller-ready slider implementation.

## Items and Stack Handling

- Replaced fixed item quantity buttons with a continuous `1-100` slider.
- Spawn requests are divided according to the selected item's native stack
  limit rather than creating illegal oversized item instances.
- Destination capacity is checked for the complete incoming stack before an
  insertion is attempted.
- Large quantities continue across legal stacks until the request is complete or
  the inventory is full.
- Partial insertions report both inserted and requested quantities.
- Existing item quality, clothing color, catalog search, and game stack-logic
  behavior remains available.

## Multiplayer Authorization

- Added `Allow All + Late Joiners` for hosts. It approves every currently
  detected compatible player and automatically approves compatible players who
  join later in the same lobby.
- Added `Deny All / Stop Auto-Allow` to revoke bulk approvals and stop automatic
  authorization for future joins.
- Per-player allow and deny controls remain available.
- Compatibility now uses the session protocol introduced in v0.9.9R4 instead of
  requiring an exact DLL build match.
- Host responses echo each client's compatible build token, allowing v0.9.9R4
  clients to recognize approval from a v1.0.0 host.
- Bulk authorization and auto-allow state are cleared when the lobby changes.

## Gameplay Input Protection

- NugzzMenu retains input ownership until the closing animation has completely
  finished, preventing background actions from firing through a fading menu.
- Phone, map, pause, hotbar, UI cycling, UI tabs, map navigation, map zoom,
  scrollbars, and quantity-modifier callbacks are blocked while the menu owns
  input.
- Existing gates remain in place for all phone apps, native and modded screens,
  the console, vehicles, skateboards, workstations, TV, jukebox, casino games,
  pause, and character creation.
- Native cursor and camera state are restored only after NugzzMenu releases its
  input capture.

## Keybind HUD

- Controller combinations are displayed whenever a controller is connected.
- PlayStation names replace Xbox names automatically for DualSense, DualShock,
  Wireless Controller, and other detected Sony layouts.
- Keyboard and mouse instructions are shown only when no controller is connected.
- Removed the large opaque HUD background and replaced it with compact outlined
  text that remains readable without obscuring gameplay.
- HUD width clamps to the current screen and refreshes immediately when the input
  device or controller layout changes.

## Vehicle Fixes

- The vehicle editor now resolves the locally driven vehicle from the active
  player-movement vehicle first, then falls back through current vehicle and
  driver-seat lookups.
- Vehicle collision proxies now remain synchronized with active visible vehicles
  instead of relying on an incomplete proximity snapshot.
- Stale proxies are removed when vehicles unload or become invalid.
- Ignored player-to-vehicle collision pairs are restored when their vehicle proxy
  is removed.
- Fixed cars becoming invisible or allowing players to walk through them after
  vehicle loading, unloading, or runtime collision refreshes.

## Other Fixes

- Fixed controller focus disappearing and mouse/keyboard prompts taking over
  immediately after a controller selection.
- Fixed background keyboard or gamepad actions opening native screens while the
  menu was open or fading out.
- Fixed right-stick camera input missing from third-person mode.
- Fixed controller double-tap fly and controller flight movement.
- Fixed vertically misaligned slider thumbs.
- Fixed clipped controller glyph labels in the footer.
- Fixed the keybind HUD using the wrong instruction set for the connected device.
- Fixed open and close animation hitching caused by full tab work during fades.
- Fixed exact-build checks preventing compatible R4 and v1.0.0 clients from
  sharing host authorization.
- Fixed bulk host approval applying only to current individual players instead
  of all compatible players and late joiners.
- Fixed large item requests producing oversized or partially accepted stacks.

## Preserved Features

All existing tabs remain present with the same names: Cheats, Money, Time,
Vehicles, Properties, Items, Lobby, FPS, Relations, Quests, and Settings. The
complete v0.9.9R4 feature set remains available, including save management,
console autocomplete, shape spawning and placement, skateboard tuning,
production automation, seed tools, custom mixture tools, multiplayer effects,
vehicle tools, teleporting, FPS controls, quest tools, and runtime reset.

Mouse, keyboard, `F8`, text entry, mouse-wheel scrolling, and the main-menu save
editor continue to work alongside controller support.

## Compatibility and Upgrade Notes

- Assembly version: `1.0.0.0`.
- Display and informational version: `1.0.0`.
- Host authorization accepts NugzzMenu v0.9.9R4 and newer protocol-compatible
  clients.
- Players still need Schedule I, MelonLoader, and S1API builds compatible with
  the assembly set used by this release.
- No v0.9.9R4 tab or gameplay feature was intentionally removed.
- Controllers can focus and activate text fields. Entering characters still
  requires a keyboard because this release does not add an on-screen keyboard.

## Verification

- Release configuration builds with zero compiler warnings and zero errors.
- All menu tabs remain registered in the shared renderer.
- Buttons, toggles, sliders, text fields, and vehicle tuning controls route
  through the shared controller-aware GUI layer.
- The built DLL reports assembly version `1.0.0.0`.
