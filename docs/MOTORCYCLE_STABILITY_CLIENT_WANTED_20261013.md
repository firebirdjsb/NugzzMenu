# Motorcycle stability and client Never Wanted test

Schedule I 0.4.7f12, S1API stable 3.2.1. Separate test artifact; not installed by the agent.

## Reported problems and inspection

The supplied screenshot shows an empty motorcycle saddle. The installed menu DLL's
SHA-256 matches the previous motorcycle-driving test exactly. Its log confirms the
previous startup marker and motorcycle patches loaded. This rules out accidentally
testing an older DLL, but does not identify every runtime visibility writer.

The previous rider repair restored Avatar.activeSelf and the player mesh layer flag.
It did not restore an independently culled BodyContainer, disabled renderers or a
disabled animator. The new repair handles these gates, including hair and clothing,
before rendering, and keeps the real avatar's hips aligned with the saddle. It never
rebuilds appearance, changes skin/clothing colors or moves the player network root.
Render callbacks also initialize on client attachment. Distant remote riders retain
native culling. Verbose diagnostics report missing rigs/errors once per scene.

Native inspection shows InteractableObject.ShowMessage can position labels using
Collider.ClosestPoint. The bike now uses one fixed chassis-relative display point.
Its custom hover handler replaces the car hover label, avoids repeated descriptor
state changes, and rejects stale/native display calls while riding, looking away,
opening UI or losing feature approval. The native interaction key and controller
binding remain in use.

The previous stabilizer froze physical pitch as well as roll, obstructing hill
alignment. The new owner-only physics assist supplies bounded pitch from wheel
contact normals through Rigidbody.MoveRotation while continuing to freeze uncommanded
pitch/roll. Pitch is limited to 40 degrees; visual bank is limited to 6 degrees and
returns upright when steering is released, speed falls or the bike is unoccupied.
Steering angle and rate decrease with speed. Tire grip floors apply after native
weather/handbrake friction updates, and yaw velocity is capped to reduce spin-outs.
A small grounded contact force helps keep the tires on slopes. Airborne bikes retain
their crest pitch. The assist reads Rigidbody.rotation rather than an interpolated
render transform, and ownership transfer starts from the received hill pitch.
Native acceleration, braking, reverse, collision response and steering RPCs remain
in use. This is an arcade stability assist, not a simulation of motorcycle balance.

Never Wanted was explicitly blocked in the UI, setter and protection predicate.
Those host-only restrictions are removed. Approved clients can enable it for their
own player. On a matching host/client build, the existing owner-validated game RPC
forwards the toggle so the host also ignores crime/pursuit/search/combat for that
client, even with the host's own Never Wanted toggle off. Requests contain only an
on/off value; they cannot select another player. Native inspection confirms the
server reader checks OwnerMatches before reaching the patched receiver. Disable,
approval revocation, disconnect expiry and scene reset remove remote protection.
Clients never call server-only police cleanup. An older/unmodified host receives
no reserved request; full server AI protection needs this build on the host too.
The existing general Lobby approval policy remains in place.

## Changed files for this revision

- `Services/MotorcycleHandling.cs`, `MotorcycleStability.cs`: steering, bank, grip,
  hill alignment and spin limiting.
- `Services/MotorcycleRiderVisibility.cs`, `MotorcycleService.cs`: complete rider
  visibility, saddle placement, client render initialization and bounded diagnostics.
- `Services/MotorcyclePromptService.cs`, `MotorcycleChassisSnapshot.cs`: fixed prompt
  anchor, display validation and rollback of prompt/tire/steering settings.
- `Services/LocalAnimationNetworkService.cs`: matching motorcycle protocol is body7.
- `Services/NeverWantedService.cs`, `NeverWantedNetworkService.cs`,
  `PlayerCheatService.cs`, `UI/CheatsTabRenderer.cs`: client toggle and target-scoped
  host enforcement, with host protection independent of the host's own toggle.
- `Core.cs`: scene cleanup and new test marker.
- `tests/LocalAnimationChecks/*`, `tests/NativeStateChecks/*`: focused regressions.
- `trialbuild/animation-diagnosis/InspectReferences.cs`: include Never Wanted in the
  generated Unity-binding audit. Package/inspection evidence is under
  `trialbuild/motorcycle-stability`.

Police vehicle spawning remains byte-identical to HEAD in `VehicleService.cs`.
Prior animation/FX, appearance, phone, watering, hover, item/NPC, color-picker and
config fixes remain included. No save/config schema migration or unrelated refactor.

## Verification and limits

- Release build: zero warnings/errors.
- 82 animation/motorcycle checks and 27 state/policy checks, including client
  Never Wanted requests, approval/expiry, wrong-pawn protection and host-toggle-off.
- 11 prompt frame, 6 color picker, 9 config and 9 accessory checks: 144 total.
- Fresh compiled API/Harmony audit against the supplied 0.4.7 dump and S1API 3.2.1:
  zero errors. Counts and generated binding checks are recorded in ZIP evidence.
- Read-only PE/disassembly validates the inspected native contract. No process
  memory offsets, executable-byte detours or allocated injection caves are used.
  `scripts/verify_offsets.py` is absent; it was not claimed as a completed check.
- Package checks validate included DLL hashes, S1API provenance, archive integrity,
  unchanged installed DLLs and preservation of earlier test ZIPs.

Fixtures validate policy and math, not native Unity physics/rendering/audio or
FishNet transport. No in-game or two-player QA was performed by the agent. Natural
turning feel, extreme slopes/curbs/collisions and actual rider display remain manual
acceptance gates; the build is ready to test, not a verified public release.

## Install, manual QA and rollback

Close the game. Back up replaced DLLs outside Mods/Plugins, then extract the test
ZIP's Mods and Plugins folders into the game folder. Keep only one NugzzMenu DLL.
Replace both S1API files together if upgrading. Preserve the existing config; the
example config is reference only. Install the same revision on host and clients,
approve clients in Lobby, and allow the body7 handshake to complete. Restart the
session and spawn fresh bikes so no earlier conversion is retained.

Test with a disposable/copied save:

1. Hover around a parked bike and move the camera: one stationary Ride motorcycle
   prompt, no label when looking away or driving, and normal prompts on other items.
2. Mount, orbit the camera and steer both ways: body/hair/clothing visible, hips on
   the saddle, body following bike bank and no detached hair. Exit restores walking.
3. Drive/reverse/brake with keyboard and controller; perform slow turns and faster
   S-turns. Release steering: bank returns upright and high-speed full-lock is limited.
4. Ride uphill/downhill, over crests and modest curbs, and test collisions. Check
   contact, smooth pitch changes, no sideways toppling or repeated spin-out.
5. Repeat as an owning client, with the host watching, then transfer the bike between
   riders on a hill. Check consistent visible placement and no competing physics.
6. Client enables Never Wanted while host leaves their own toggle off. Test crime,
   trespass, existing pursuit/search and police combat. Other players keep normal
   police behavior. Disable/revoke/leave/rejoin and confirm native behavior returns.
7. Test removal, death/ragdoll, menu/phone use, scene reload and cleanup; no stranded
   prompt, looping engine, altered appearance or persistent session bike.

Rollback: close the game and restore the replaced DLLs from backups. No save/config
migration. The agent did not deploy this revision, publish it, change game saves or
touch production. Earlier artifacts remain available.
