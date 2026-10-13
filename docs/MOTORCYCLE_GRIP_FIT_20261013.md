# Raised motorcycle/rider and real-arm grip fit — 2026-10-13

Separate Schedule I 0.4.7f12 test build; matching host/client protocol **body10**.
Startup marker: `Motorcycle raised seat + real-arm grip fit 2026-10-13`.

## Observed problem and cause

The user's new front-view screenshot shows both hands below the handlebars and
requests more bike/rider height. Installed NugzzMenu.dll matches the preceding
refinement test (SHA256 `75bbdc2c7a365edf69009d7c4ee466cb0de2117e4870be468b9cbb885bc3cb9a`),
and Latest.log contains that revision's startup marker.

Read-only inspection of the installed Player avatar found approximately
**0.2515 m upper arms and 0.2596 m forearms**. The preceding hand fixture used
longer arms; its successful placement tests did not establish reach for the real
character. The two-bone solver safely clamped unreachable targets, leaving a
visible gap. It also preserved the animated wrist orientation instead of aligning
the palm's frame to the bar and did not author a closed finger grip.

The native AlignmentPoint is about 0.0497 m along the hand and 0.0138 m toward
the palm, with mirrored +/-90-degree local rolls. Mesh inspection also shows the
inner grip tube centre differs from the outer end-cap centre used previously.
These serialized asset measurements are not runtime pointers or process offsets.

## Scoped changes and files

- `Services/MotorcycleHandling.cs`: display clearance is **10 cm total**, another
  **5 cm** above the preceding test. The suspension target retains the lift.
  Physical wheel placement/radius, springs, safe hill sampling, grip/spin limits,
  steering and engine tuning remain the preceding revision's settings.
- `Services/MotorcycleService.cs`: the copied rider point moves **6 cm up and
  4 cm forward** on the cushion. The rider follows the raised saddle point, and
  native driver-seat/camera setup uses the same point. No physical/network root
  or shared prefab is moved by these display changes.
- `Services/MotorcycleGripTargets.cs`: targets sit on measured inner tube centres
  (mesh-local metres: X +/-0.44, Y 1.285, Z 0.0484). Their mirrored frames face the
  palms down onto the grips and account for the tubes' approximately 10-degree
  outward slope. They remain children of the turning handlebar mesh.
- `Services/MotorcycleHandPose.cs`: align the complete native palm frame to each
  grip, use current clavicle anchors and bounded seated torso lean/turn when arm
  reach needs it, then solve the arms. Hips remain aligned to the saddle point;
  bone lengths/positions/scales, player movement roots and wheel physics are
  unchanged by this solver. Lean is bounded to 45 degrees, torso turn to 20
  degrees, and clavicle aiming to 75 degrees. Invalid/unreachable rigs remain
  safely bounded rather than stretching. Frame baselines, current-rig validation,
  dismount/ragdoll ownership and repeat-render stability remain in place.
- New `Services/MotorcycleFingerPose.cs`: curl the installed rig's Index/Thumb
  clusters around the grips, retaining thumb splay and restoring native finger
  rotations with the owned riding pose. No shared animation clips are edited.
- `Core.cs` and `Services/LocalAnimationNetworkService.cs`: startup marker and
  body10 matching check, preventing an older motorcycle revision from matching.
- `tests/LocalAnimationChecks`: seven new real-proportion, palm-frame, finger,
  cleanup, rig-replacement and combined-height checks; existing checks retained.

The preceding native hill/rider fix and audio/steering changes remain included.
No police spawning change, other gameplay refactor, dependency addition or
save/config schema change is in scope. Client Never Wanted remains available.

## Verification and limits

- Release build: **zero warnings/errors**.
- **169 offline checks passed**: 107 animation/motorcycle, 27 state/policy,
  11 prompt, 6 picker, 9 config and 9 accessory failure checks.
- The new short-arm fixture reproduces the unreachable-grip condition and checks
  both palms within 3 mm of targets at full +/-30-degree parking lock. It also
  checks mirrored palm orientation, hill/bank/observer poses, curled fingers,
  repeat cameras, restored torso/clavicles/fingers and unchanged limb lengths.
- API/Harmony audit against the supplied update dump and locked S1API 3.2.1:
  **255 types, 809 members, 85 patches, 12 dynamic patches**, zero errors.
- **1254 Unity call sites**, including nested helpers, avoid throwing generated
  stubs. The current built DLL still has **zero GetGroundHit references**.
- Diff whitespace checks pass; `Services/VehicleService.cs` still matches HEAD.
- The test ZIP checks payload/dependency hashes, archive integrity and preservation
  of installed DLLs and all earlier test ZIPs.

The screenshot and logs are user runtime evidence for the preceding build.
The new rig geometry fixtures exercise pose math, not Unity rendering, real skin
deformation, audio or multiplayer transport. This new revision still needs manual
in-game acceptance, particularly hand fit for different character body shapes,
visual torso movement and ground clearance. No raw memory writes, executable
patches or memory caves were added; the absent verify_offsets.py was not run.

## Install, acceptance and rollback

Close the game and back up replaced DLLs outside Mods/Plugins. Extract this ZIP's
Mods and Plugins folders into the game folder; keep one NugzzMenu DLL. Preserve
the existing config (the example is reference only). The package includes the
locked S1API 3.2.1 mod/loader pair. Install this same body10 revision on all peers,
approve clients in Lobby, restart the session and spawn a **fresh bike**.

With a copied/disposable save, check the raised bike and rider from the front and
side at idle; both palms should grasp the grips with fingers closed. Turn fully
left/right, release steering, drive/reverse, ride hills and inspect hand contact
on an owning client and observer. Check the new saddle position against the
cushion, foot position and excessive tire air gap. Change clothes/body appearance,
exit, re-enter, ragdoll/clear effects, remove and respawn; check restoration.

Rollback with the game closed by restoring your backups, or use the preceding
motorcycle-refinement test on all peers and restart. Do not use the older
stability-client-wanted build, which contains the reported fatal WheelHit binding.
No save/config migration is needed. The agent did **not install or publish** this
revision or modify installed DLLs, saves, config or production data.
