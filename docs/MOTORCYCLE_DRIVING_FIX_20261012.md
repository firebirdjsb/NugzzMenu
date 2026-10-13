# Motorcycle driving test revision — 2026-10-12

Status: source repair and offline checks complete; new in-game driving/audio and host/client QA remain pending. Schedule I 0.4.7f12, S1API stable 3.2.1.

Startup marker: `Test build: Motorcycle driving + visible rider + engine audio 2026-10-12`.

## Evidence and cause

Reviewed the supplied 26.92-second recording at multiple points. The riding HUD remains active, speed changes, and the motorcycle pitches/tumbles without a visible rider. This establishes the reported visual/physics failure; the recording does not expose the actual input axes.

Read-only inspection of the installed game and assets found:

- `Player.EnterVehicle` reparents Avatar directly under LandVehicle and hides its GameObject. The previous placement guard required Avatar to remain under Player, so it rejected the native seated hierarchy. Local first-person mesh layers also hid the rider.
- The native front-wheel anchor is already at axle height (0.354 m). The previous conversion added another wheel radius, placing front contacts about 0.344 m above the rear contacts. Native sedan turnover torque can then act when wheels lose ground contact. Only roll was constrained previously.
- The sedan bounds object has scale `(1.85, 1.55, 4.85)`. Assigning 2.7 directly to its local Z size produced approximately 13.095 m of bounds and an excessive camera distance. Its scaled interaction object also enlarged the physical box.
- Driving had no motorcycle engine source. The previous audio addition belonged only to the revving emote.

The causes above are code/native-contract findings. Their contribution to the exact acceleration failure is an inference from the geometry and recording, rather than a captured runtime throttle trace.

## Change

The existing registered vehicle provides native input, forward/reverse, brake, driver ownership, camera and FishNet RPCs. The conversion now supplies motorcycle-sized physical geometry, four narrow contacts at the actual axle centres, lower mass with proportional power/braking, a consistent centre-of-mass initializer, and cleared AI control overrides. The camera bounds and mount trigger use dimensions in metres after compensating for their inherited scales.

The physical chassis remains upright, with pitch/roll constrained to prevent the car turnover behavior from launching it. A separate visual lean follows steering and speed; the fork turns and both displayed wheels spin. Suspension display follows grounded wheel poses. This is arcade motorcycle handling, not a simulation of countersteering, wheelies or falls. Slopes, curbs and collisions need runtime evaluation.

The real avatar is allowed under the vehicle, restored to visible layers before rendering, aligned by its hips to the moving saddle, and oriented with the bike's lean/yaw. Hair and clothing remain part of that complete avatar. Native dismount retains control of the walking transform. A delayed conversion excludes already-mounted avatars from chassis visibility, collider and rollback changes.

A separate owned AudioSource uses the loaded native motorcycle rev loop, with lower pitch/volume at idle and increased pitch/volume under throttle or speed, including reverse. Native car engine channels are muted and restored on detach. The motorcycle loop stops on exit/removal/reset. No audio or model assets are distributed.

All multiplayer participants must use this revision (`body6` matching handshake); earlier motorcycle revisions have different geometry. Solo remains supported without a Steam lobby. Motorcycles have no storage and remain session-only, maximum four. Previous animation FX, skin restoration and compatibility fixes remain included.

## Files changed in this revision

- `Services/MotorcycleService.cs`: conversion geometry, current vehicle avatar placement/visibility, late conversion isolation and engine/handling lifecycle.
- `Services/MotorcycleHandling.cs`: wheel layout, proportional tuning, COM initializer, steering lean/fork/wheel visuals and suspension display.
- `Services/MotorcycleEngineAudio.cs`: native motorcycle loop and car engine channel suppression/rollback.
- `Services/MotorcycleChassisSnapshot.cs`: rollback of added control, brake, COM and audio state; exclusion of rider components.
- `Services/LocalAnimationNetworkService.cs`: matching revision handshake.
- `Core.cs`: test-build identifier only in this revision.
- `tests/LocalAnimationChecks/`: native entry fixture and 14 additional geometry, parenting, visibility, steering, audio, initialization and revision checks; SDK fixtures remain separate from native simulation.
- This document. Ignored diagnosis/package helpers live in `trialbuild/motorcycle-driving/`.

Scope excludes police vehicle spawning, game saves, clothing appearance data and unrelated menu behavior. `Services/VehicleService.cs` still matches HEAD exactly.

## Verification

- Release build: zero warnings/errors.
- 69 local animation/motorcycle checks, 20 native state/growing checks, 11 prompt frame checks, 6 clothing picker checks, 9 config checks and 9 accessory failure checks: 124 total.
- Compiled game/S1API binding and all Harmony target audits passed. The package includes the exact current counts/report.
- Generated Unity call inspection passed, including new motorcycle helpers and nested methods; no throwing stripped stubs.
- Native entry/exit/visibility, input/torque/steering, airborne turnover, COM initialization and wheel initialization were inspected read-only against current PE headers and metadata. Native wheel Start does not replace the new radius/layout.
- Diff/whitespace checks passed; the requested police spawning revert remains preserved.
- ZIP CRC, exact payload, per-file hashes and pinned S1API dependency checks passed. Installed DLLs and earlier test ZIPs are preserved.

No process memory patch, injected machine code or memory cave is added. The workspace has no `scripts/verify_offsets.py`; no runtime offset-writing module was introduced. The native inspection uses metadata to resolve methods and validates the PE file before reading it. No runtime native addresses are hardcoded.

Offline fixtures do not simulate Unity physics, rendering/culling, audio mixing or FishNet transport. They do not establish in-game success. The user's recording is baseline evidence, not verification of this revision.

## Install, test and rollback

Close the game. Back up replaced DLLs outside Mods/Plugins, then extract the package's Mods and Plugins folders to the game folder. Keep only one NugzzMenu DLL. Preserve `nuggzmenu-config.json`; the example config is reference only. The packaged S1API 3.2.1 mod/loader pair must be replaced together when upgrading dependencies.

Use a copied/disposable save. Remove any previous session motorcycles or restart before spawning a new one. In Vehicles, spawn a motorcycle on flat clear road; close the menu and interact to ride. Use normal native driving controls: W/S for forward/brake/reverse, A/D steering, Space handbrake and E exit, or the game's configured controller bindings.

Verify:

1. Solo mounting shows the real rider on the saddle, with current skin/clothes/hair and no car body or storage interaction.
2. Forward acceleration works; braking reaches a stop; continued reverse input moves backward.
3. Left/right steering turns the bike, moves the front fork and leans bike/rider smoothly. Repeated turns from different headings do not hide the rider or flip the chassis.
4. Native motorcycle audio is audible at idle, changes under throttle/speed/reverse, and stops on exit. Check the game's audio volume settings too.
5. Exit returns normal on-foot position/appearance/visibility. Remount, remove unoccupied bikes and reload a session without leaving audio or poses behind.
6. Repeat low-speed steering, slopes, curbs and collision checks on a disposable save. Upright arcade stabilization may limit pitch on uneven terrain.
7. With matching builds on both approved peers, check a host-spawned motorcycle driven by the host, then by the client, and observer appearance/sound. Approve/rejoin while the host is already riding to exercise delayed conversion. Real two-player transport is still unverified.
8. Confirm revving/pee/vomit effects and Clear FX skin restoration still work, and an ordinary car still drives/displays normally.

Rollback: close the game and restore every replaced DLL from the backups. No save/config-format migration is needed. The agent did not install this test revision, modify saves, publish a release or touch production. Earlier ZIPs remain available.
