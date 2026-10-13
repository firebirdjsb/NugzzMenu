# Seated rider, relaxed arms and feet on rests — 2026-10-13

Schedule I 0.4.7f12 separate test build; matching host/client revision **body11**.
Startup marker: `Motorcycle seated rider + relaxed elbows + foot rests 2026-10-13`.

## Baseline and scoped repair

The user's top and side screenshots show awkward arm bends, the rider's butt
intersecting the cushion and hanging feet intersecting the road. Installed
NugzzMenu.dll matches the previous grip-fit test exactly (SHA256
`49fc7bd98141a8bc64c2d95452d28d309eb50ea22d0b8629918caf8ca8272f33`), and Latest.log
contains its startup marker and visible-rider diagnostics.

Hand contact alone did not establish a natural-looking arm pose: the previous
solver inherited the native stationary revving elbow plane and permitted a large
clavicle lift. The riding feature also reused that animation's hanging leg pose
without foot targets. The side screenshot establishes that the preceding 6 cm
pelvis offset did not clear this character's cushion/road intersections.

This revision adjusts the **player pose**, keeping the confirmed bike display
clearance, hill/stability logic, native driving controls, engine audio, stable
prompt and session-only/no-storage behavior. Police spawning remains reverted;
client Never Wanted remains available. No save/config schema change, shared
animation edit, added dependency or unrelated gameplay change is included.

## Changed files and behavior

- `Services/MotorcycleRiderTargets.cs`: new authored rider targets. Pelvis height
  is **20 cm above the original copied sitting point**, another **14 cm** above
  the preceding build; the point is 8 cm forward, another 4 cm forward on the
  cushion. The joint lies inside the pelvis, so aligning it close to the cushion
  can leave the buttocks' skin below that surface.
- `Services/MotorcycleService.cs`: use the raised point for the rider, native
  driver seat and camera. Add foot targets and apply the current leg pose after
  native evaluation/saddle alignment and the hand pose. Restore legs alongside
  hands on owned scene cleanup; preserve native dismount/ragdoll ownership.
- `Services/MotorcycleHandPose.cs`: an explicit down/back elbow pole replaces the
  inherited revving pole. Reserve a small bend rather than solve at full arm
  extension. Reduce shoulder aiming from 75 to **35 degrees**, and additional
  torso lean from 45 to **30 degrees**. Palm/finger grip and current-rig checks
  remain. Bone lengths, local positions/scales and network roots are untouched
  by the limb solver.
- `Services/MotorcycleLegPose.cs`: new render-only leg solve. Knees bend forward
  and slightly out around the engine; ankles follow authored foot-rest targets.
  Targets are children of the copied frame and follow suspension, bank, hills
  and heading. They are at mesh-local X +/-0.27 m, Y 0.43 m, Z 0.06 m, with an
  8 cm ankle allowance above a 0.35 m rest level. These are authored pose targets,
  not new rendered pegs or colliders. Native foot orientation is retained.
- `Services/MotorcycleLimbSolver.cs`: shared bounded two-bone rotation math for
  arms and legs. Invalid/degenerate chains fail safely; unreachable limbs stay
  within their lengths. Repeat-camera passes restore that frame's baseline first.
- `Core.cs`, `Services/LocalAnimationNetworkService.cs`: identifying startup
  marker and body11 matching requirement.
- `tests/LocalAnimationChecks`: eight focused posture regressions, including
  installed rig dimensions of approximately 0.4528 m thighs and 0.4290 m shins.

The bike's existing **10 cm display clearance**, wheel placement/radius, springs,
grip/spin safeguards, hill raycasts, steering and audio settings are unchanged.
The copied frame's foot targets do not drive physics or player movement.

## Checks and evidence boundaries

- Release: **zero warnings/errors**.
- **177 offline checks passed**: 115 animation/motorcycle, 27 state/policy,
  11 prompt, 6 picker, 9 config and 9 accessory failure checks.
- New fixtures reproduce hanging shoes below a level road before the leg solve,
  then check both ankles within 3 mm of their targets and representative soles
  clear of the road. A representative butt point 14 cm below the hip clears the
  cushion. These are geometry fixtures, not measurements of runtime skin.
- Arm checks require down/back elbow bend and grip contact at full parking lock.
  Combined hand/foot checks cover hill/bank/heading changes and local/observed
  actors. Other tests cover repeat-camera stability, unchanged limb lengths and
  roots, current appearance rebinding, restoration and invalid-chain handling.
- API/Harmony audit: **255 types, 809 members, 85 patches, 12 dynamic patches**,
  zero errors against the supplied dump and locked S1API 3.2.1.
- **1300 Unity call sites**, including nested helpers, avoid throwing stripped
  stubs. The current built DLL has **zero GetGroundHit references**; the unsafe
  WheelHit binding remains excluded.
- Whitespace check passed; `Services/VehicleService.cs` matches HEAD exactly.
- ZIP verification checks payload/dependency hashes, archive integrity and
  preservation of installed DLLs and all previous test ZIPs.

The screenshots/log identify the preceding build's runtime problem. The new
checks verify pose math and state/lifetime contracts, not Unity skin deformation,
actual visual comfort or FishNet transport. Real butt/sole/clothing clearance,
arm appearance and owning-client/observer rendering remain manual acceptance
gates. No process offsets, executable patches, protected-memory writes or memory
caves were added; the absent verify_offsets.py was not run or claimed as run.

## Installation, manual acceptance and rollback

Close the game. Back up replaced DLLs outside Mods/Plugins, then extract the ZIP's
Mods and Plugins folders into the game folder. Keep one NugzzMenu DLL. Preserve
the existing config; the example is reference only. The locked S1API 3.2.1 mod
and loader are included. Use the same body11 build on host and clients, approve
clients in Lobby, restart the session and spawn a **fresh motorcycle**.

Using a copied/disposable save, inspect the idle rider from the side and above:
butt on the cushion, soles off the road, knees forward around the engine, elbows
relaxed below the hand line and both hands gripping the bars. Drive/reverse,
turn fully both ways, release, ride hills and inspect the local rider, owning
client and observer. Repeat after clothes/body changes, exit/re-entry, ragdoll,
clear effects, removal and respawn. Check for a floating rider, boot/tank/frame
intersections or an unnatural arm/knee plane; these need actual visual QA.

Rollback with the game closed by restoring backups, or the preceding grip-fit
test on all peers, then restarting. The older stability-client-wanted build
contains the reported fatal WheelHit binding and should not be used for bikes.
No save/config migration is required. The agent **did not install or publish**
this revision or modify installed game DLLs, saves, config or production data.
