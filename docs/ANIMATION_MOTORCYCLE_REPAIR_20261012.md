# Animation seating/render repairs and rideable motorcycle — 2026-10-12

Historical revision: superseded by the
[solo authority and complete-avatar seating follow-up](SOLO_SEATING_FIXES_20261012.md).
The earlier test ZIP remains available for rollback.

Status: implemented in an isolated test revision. The build, fixtures and API
audits pass. Native visual playback, physics and two-player QA remain pending.
This revision supersedes the custom-body/tent test builds; retain them for rollback.

## What changed

- **Boinking:** the existing clothed solo emote now prefers the current mesh's
  bound joints. It also applies at the built-in camera pre-cull/URP begin-camera
  stage, after Animator/FinalIK updates. The earlier implementation relied on
  LateUpdate and a serialized bone cache. Those are insufficient guarantees that
  the final rendered skeleton receives the pose. Your report confirms the old
  build did not visibly animate; the exact live overwrite has not been captured.
- **Using laptop:** creates a render-only native chair and laptop on a walkable
  surface, aligns the avatar's hips with the chair's sitting point, and restores
  the original offset on Stop. It keeps the player's movement capsule in place.
  It refuses to invent a floating placement when no ground can be found.
- **Revving motorcycle:** copies the whole motorbike visual, grounds it and uses
  its actual seat orientation. The native saddle faces 180 degrees relative to
  the model root; positioning the old visual at the player capsule ignored that
  rotation and its 0.731 m seat height. The pose now aligns to the saddle.
- Failed Local Effects starts now retain their explanation in the panel's status
  area, in addition to the warning notification.

## Riding

Open **Vehicles → Rideable Motorcycle → Spawn Motorcycle** as host. Find clear
space ahead, spawn it, close the menu and interact with **Ride motorcycle**.
Use the game's normal vehicle controls, including controller steering/brakes and
the normal exit control. **Remove Unoccupied** removes only these session bikes.

The game's new motorbike is a stationary NPC prop rather than a player vehicle.
This revision uses its render-only model on a registered native sedan chassis,
with narrow wheel placement, motorcycle-scale collision, mass, seat/camera
alignment and upright support. Native driving, camera, ownership and FishNet
remain in use. The sedan's native prefab code and RPC seat indices are retained;
the custom interaction allows an unoccupied motorcycle to be mounted. No new
network prefab, NPC behaviour, custom keyboard-only controller or asset bundle
is registered. Driving feel is a car-controller-based approximation and requires
runtime tuning/QA; it is not a native two-wheel simulation.

Host spawning is capped at four bikes and requires all current peers to be
approved and running this matching revision. Host and clients share native
spawned-object IDs through a trusted-host message; a bounded pending queue covers
messages arriving before the object. New approved observers receive a snapshot.
An older custom-body build cannot satisfy this revision's readiness handshake.

Bikes are **session only**. Spawn uses the native unowned vehicle option; the
inspected native save routine iterates PlayerOwnedVehicles. No save format,
configuration format or persistence rules were patched. Existing cars, native
NPC motorbikes and registered prefabs are not converted. Failed attachments and
detachment restore the cloned chassis's original settings and renderers.

## Changed files

Added `Services/LocalAnimationRenderService.cs`, `LocalAnimationPlacementService.cs`,
`NativeVisualCloneService.cs`, `MotorcycleService.cs`, `MotorcycleNetworkService.cs`,
`MotorcycleChassisSnapshot.cs` and `UI/MotorcycleRenderer.cs`.

Updated the local body/visual/network/action services, `Core.cs`, the network
value dispatcher in `PlayerCheatService.cs`, its reserved-message allowlist in
`SessionAuthorityService.cs`, `VehicleTabRenderer.cs`, tests and documentation.
`Services/VehicleService.cs` remains identical to HEAD, preserving the requested
police spawning revert. The prior phone/Character, prompt, watering, clothing
picker, content/group, weather and config changes remain included.

## Evidence and limits

- Clean Release build, zero warnings/errors.
- **92 checks:** 37 animation/motorcycle state, render-callback and placement
  checks; 20 native-state/grow checks; 11 prompt frame checks; 6 picker checks;
  9 config checks; 9 accessory failure checks against the built DLL.
- Compiled game bindings and Harmony targets audited against the supplied
  0.4.7f12 dump and pinned stable S1API 3.2.1; zero API errors.
- Generated Unity call inspection covers animation, cloning and motorcycle
  modules, including compiler-generated nested callback/rollback code.
- Read-only game-file inspection checks the native seat orientation/height,
  driver entry, native spawn and PlayerOwnedVehicles save-list boundary.
- ZIP contents, CRC and hashes verified. Only the mod and matching S1API pair
  are bundled as DLLs; no game assemblies or extracted model/animation assets.

Fixtures do not run Unity physics, native Animator/FinalIK, rendering or FishNet.
Actual Boinking visibility, hand/foot alignment, outfit clipping, motorcycle
collision/steering/camera feel and two-player delivery are still unverified.

## Install and focused game QA

Close the game. Back up replaced DLLs **outside Mods/Plugins**, then install the
included menu and matching S1API 3.2.1 DLL/loader pair on **host and clients**.
Preserve your `nuggzmenu-config.json`; the example is reference only. Approve
clients in Lobby and allow two seconds for the new readiness exchange.

Startup marker:
`Test build: Animation render/seat fixes + rideable motorcycle 2026-10-12`.

Use a copied/disposable save:

1. In third person, select Boinking and check motion for several seconds. Test
   Stop, timeout, movement and clothing changes; inspect the persistent status
   if it refuses to start. Verify motion from the other player's view too.
2. Use laptop/revving on level ground. Check chair/saddle contact, facing,
   hands/feet and removal/restoration on Stop, movement, ragdoll and scene exit.
3. Spawn a bike outside near the street. Mount, accelerate, steer, brake,
   reverse and exit with both keyboard and controller. Inspect the native
   vehicle camera, collisions, slopes, upright handling and stuck-pose cleanup.
4. Repeat riding as an approved client; compare visibility and motion from
   both computers. Test a newly approved observer, occupied-bike refusal and
   Remove Unoccupied. Confirm other cars and parked NPC bikes are unaffected.
5. Recheck the phone/Character exit, dark overlay, trimmer prompt, watering and
   expanded clothing picker cases from the earlier test notes.

Rollback: close the game and restore every replaced DLL from its backup. The
agent has not installed this revision or edited game saves/production files.
