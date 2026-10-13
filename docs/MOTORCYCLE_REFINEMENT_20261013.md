# Motorcycle clearance, steering, audio and hands — 2026-10-13

Separate Schedule I 0.4.7f12 test build. Startup marker:
`Motorcycle clearance + steering/audio + grip hands 2026-10-13`.
Host and clients require this matching **body9** revision.

## Baseline and scope

The user's latest screenshot shows a visible seated rider, low tires and hands
away from the grips. The user reports that hill handling and rider visibility
work in the preceding spawn-crash-fix build. The installed DLL matches that
artifact (SHA256 `681eb96c07fe483e33292479c46a501eb81d4899e266859f8f67fbc77ce39b9b`),
and Latest.log contains its startup marker and visible-rider diagnostics.
These are user-reported runtime results for the preceding build, not an agent
runtime test of the refinements below.

This revision changes motorcycle display clearance, steering response, engine
mixing and hand contact. The safe raycast hill probe, visible-rider repair,
6-degree visual bank/centering, grip assistance, native driving/ownership/RPCs,
stable prompt and no-storage/session-only motorcycle remain included. Client
Never Wanted remains available. Police vehicle spawning stays reverted; no
appearance, save/config schema or unrelated gameplay changes were introduced.

## Changes and affected files

- `Services/MotorcycleHandling.cs`: a persistent **5 cm display lift**, applied
  after wheel placement and included in suspension alignment. The physical axle
  positions, 0.35 m wheel radius, springs and hill sampling geometry stay intact.
  Read-only installed mesh inspection found approximately 0.36/0.37 m rear/front
  visual tire radii, larger than the physical wheel radius. A plain display offset
  would otherwise be canceled by the existing suspension correction.
- `Services/MotorcycleHandling.cs` and `Services/MotorcycleStability.cs`: increase
  parking steering to 30 degrees, ease town-speed steering reduction, use bounded
  fork travel and reduce chassis angular drag. Native yaw momentum is captured
  **before** the pitch MoveRotation correction, then restored within a speed-based
  spin cap. This avoids relying on angular velocity after a rotation correction.
  The road-normal/crest-pitch algorithm and maximum 40-degree controlled pitch
  are unchanged. Actual steering feel still requires driving QA.
- `Services/MotorcycleEngineAudio.cs`: one continuous native motorcycle clip with
  smoothed engine load/RPM, quieter idle, 180 ms entry and 250 ms exit fades,
  bounded pitch, 3D logarithmic falloff and restrained Doppler. Throttle changes
  and re-entry during a fade do not restart the loop. Removal/reset stops it
  immediately. The native mixer routing and mute rollback remain; collision
  sounds are untouched. No new recorded sound assets are included.
- `Services/MotorcycleGripTargets.cs`: grip targets are children of the copied
  `Chopper_Handlebars01_LOD0` mesh. Coordinates were measured from the installed
  mesh, in metres in mesh-local space: X +/-0.50, Y 1.268, Z 0.003. They follow
  handlebar steering, visual bank, suspension and hill/chassis rotation.
- `Services/MotorcycleHandPose.cs` and `Services/MotorcycleService.cs`: a small
  render-only two-bone arm solver uses the current attachment-provider arm/hand
  bones and hand-alignment points, after native animation evaluation and saddle
  alignment. Wrist orientation follows handlebar steering. Multiple camera
  callbacks restore that frame's native baseline before solving again. Rig
  replacement invalidates stale anchors; missing/invalid chains keep the native
  pose and log once. Unreachable targets are clamped to limb reach: no skeleton
  stretching, bone position/scale changes or network-root movement. Release
  preserves native exit, ragdoll and replacement-rig ownership.
- `Services/LocalAnimationNetworkService.cs` and `Core.cs`: body9 matching check
  and the identifying startup marker.
- `tests/LocalAnimationChecks`: current mesh/arm geometry fixtures and 13 focused
  refinement checks alongside the preceding 87 regressions.

## Verification

- Release build: **zero warnings/errors**.
- **162 offline checks passed**: 100 animation/motorcycle, 27 native state/policy,
  11 prompt, 6 clothing picker, 9 config and 9 accessory failure checks.
- Hand tests cover local/observed palm contact during steering and hills,
  steering orientation, repeat-camera stability, current-rig rebinding,
  impossible reach, invalid chains, rotation-only changes and release ownership.
  Audio tests cover fades, no restart on throttle/re-entry, gradual rev response,
  reverse/coasting, frame-rate comparison and immediate cleanup. Steering tests
  cover native yaw preservation even when the fixture clears angular velocity
  during MoveRotation, spin limits, safe hill pitch and centering.
- API/Harmony audit: **255 types, 807 members, 85 patches, 12 dynamic patches**;
  zero errors against the supplied update dump and locked S1API 3.2.1.
- Generated-binding audit: **1143 Unity call sites**, including nested helpers,
  avoid throwing stripped stubs. The built DLL has **zero GetGroundHit calls**;
  the preserved crashing DLL has one. The unsafe WheelHit binding is not reused.
- `git diff --check` passed; `Services/VehicleService.cs` matches HEAD exactly.
- ZIP verification checks integrity, payload hashes, locked dependency bytes,
  preservation of installed DLLs and all preceding release ZIPs.

These checks establish state/placement/pose math, cleanup and binding availability;
they do not simulate Unity tire dynamics, audio playback, real avatar deformation
or FishNet transport. No new raw offsets, executable patches, protected-memory
writes or injection caves were added. `scripts/verify_offsets.py` is absent and
was not claimed as run.

## Installation, manual acceptance and rollback

Close the game. Back up replaced DLLs **outside Mods/Plugins**, then extract this
ZIP's Mods and Plugins folders into the game folder. Keep one NugzzMenu DLL.
Preserve the existing config; the example is reference only. The included S1API
mod and loader are the locked 3.2.1 release pair. Use the same body9 revision on
host and clients, approve clients in Lobby and wait for the matching handshake.
Restart the session and spawn fresh motorcycles.

Using a copied/disposable save, check:

1. Park/spawn on a level road. Check clearance against tire penetration or an
   excessive visible air gap. Mount, drive forward/reverse and exit.
2. Compare low/town/high-speed steering, release to center, uphill/downhill and
   crests. Check that the confirmed hill and visible-rider behavior remains.
3. In third person, check both palms on the grips through left/right turns and
   hills, including the chosen character body/clothes. If a limb cannot reach,
   its pose safely stops at full reach; actual grip fit remains a manual gate.
4. Listen at idle, throttle, coasting, reverse, exit and rapid re-entry. Check
   smoothness, volume, looping and spatial falloff. Repeat on an owning client
   and observer with the matching build; offline fixtures do not prove sync.
5. Exit, change clothes, enter again, ragdoll/stop effects, remove and respawn.
   Check that walking, appearance and car rendering/audio stay correct.

Rollback with the game closed by restoring backed-up DLLs, or restore the
preceding **motorcycle-spawn-crashfix** test revision on all peers and restart
the session. Do not use the older stability-client-wanted test: it contains the
reported fatal GetGroundHit binding. No save/config migration is required.
This refinement was **not installed or published by the agent**. Installed
game DLLs, saves, config and production data were not modified.
