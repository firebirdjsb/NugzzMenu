# Motorcycle spawn crash fix — 2026-10-13

Separate Schedule I 0.4.7f12 test build. Startup marker:
`Motorcycle spawn crash fix + safe ground probe 2026-10-13`.

## Cause and evidence

The user's console trace identifies `WheelCollider.GetGroundHit(out WheelHit)`
inside the new `MotorcycleStability.FixedUpdate` as the fatal access-violation
path. Latest.log confirms that the stability/client Never Wanted build and its
patches loaded; that file ends at the reflection fallback messages. The fatal
trace was supplied separately in chat.

The installed menu DLL matches the previous test exactly:
`74e60b9a9feb86a3fd11474f5c1c8defa69b1853831f18fb0b37bcd96d293005`.
This was a bug introduced by the new hill sampling, not an unverified report about
some older mod version.

Read-only binding inspection shows a concrete output-buffer mismatch:

- The native dump defines WheelHit as a value structure. Its collider reference
  lies at byte offset 0x40; that reference is 8 bytes on this x64 target. The native
  structure therefore occupies 72 bytes, including padding and the reference.
- The generated managed WheelHit is an `Il2CppSystem.ValueType` reference wrapper,
  rather than a CLR value structure. GetGroundHit initializes a pointer-sized local
  and passes that local's address to `il2cpp_runtime_invoke` as the output argument.
- That 8-byte slot is insufficient for the native output structure. This explains
  the memory corruption and fatal invocation stack. The existing managed catch did
  not contain the fatal crash.
- RaycastHit, Vector3 and Quaternion are CLR value structures in these generated
  references. Physics.RaycastAll returns a RaycastHit array and uses the existing
  raycast path already used by the mod's placement code.

The previous compile/API audits checked method availability and throwing stubs;
they did not detect this ABI mismatch. The old fixture also modeled WheelHit as a
managed struct. Both gaps are now explicitly covered by call-avoidance checks.

## Repair and scope

`Services/MotorcycleStability.cs` no longer calls GetGroundHit. A small
`Services/MotorcycleGroundProbe.cs` helper samples short downward rays near enabled,
grounded wheels. It excludes the bike, player/rider colliders, triggers and disabled
colliders, uses the closest physical surface, rejects steep/absent/out-of-reach
surfaces, and preserves crest pitch when no suitable contact is available.

The existing 6-degree visual lean limit, centering, steering/grip/spin assistance,
bounded hill pitch, visible-rider repair and stable prompt remain included.
Client Never Wanted remains available. Police vehicle spawning stays reverted.
No appearance, save/config schema or unrelated gameplay change is included.

`Services/LocalAnimationNetworkService.cs` now requires body8, so a client with the
crashing body7 build cannot satisfy the motorcycle matching check. `Core.cs` has a
new startup marker. `tests/LocalAnimationChecks` adds ground filtering and spawn
regressions; its GetGroundHit fixture now throws if that unsafe entry point is
ever used. The compiled-binding audit rejects any GetGroundHit call anywhere in
the menu DLL. It rejects the preserved crashing DLL and accepts the replacement.

## Verification and remaining QA

- Release build: zero warnings/errors.
- 87 animation/motorcycle checks, 27 state/policy/client Never Wanted checks,
  11 prompt checks, 6 picker checks, 9 config checks and 9 accessory checks:
  **149 offline checks passed**.
- Compiled DLL inspection: **one** GetGroundHit reference in the crashing DLL;
  **zero** in the replacement. The new guard demonstrably rejects the old DLL.
- API/Harmony audit against the supplied dump and S1API 3.2.1: 255 types,
  803 members, 85 patches and 12 dynamic patches; zero errors.
- 1011 generated Unity call sites, including nested helpers, avoid throwing
  generated stubs and pass the unsafe-call guard.
- ZIP verification checks archive integrity, payload hashes, dependency provenance
  and preservation of installed DLLs and earlier ZIPs.

No native execution was used to reproduce the access violation again. Offline
fixtures and IL inspection establish removal of the reported fatal call path;
they do not establish in-game physics/rendering/audio or multiplayer correctness.
No raw offsets, executable byte patches, protected-memory writes or injection caves
were introduced. `scripts/verify_offsets.py` is absent and was not claimed as run.

## Install, acceptance test and rollback

Close the game. Back up replaced DLLs outside Mods/Plugins, then install the ZIP's
Mods and Plugins folders. Keep one NugzzMenu DLL. Preserve the existing config;
the example is reference only. Use this same body8 revision on host and clients,
approve clients in Lobby and wait for the matching handshake. Restart the session
and spawn fresh motorcycles.

Using a copied/disposable save, spawn a bike in clear space and leave it parked for
several seconds, then mount, drive/reverse, turn, ride uphill/downhill and over a
crest, exit, remove and spawn again. Repeat with an owning client and check rider
visibility, stationary hover prompt, engine audio, centering and Never Wanted.
These are the remaining manual acceptance gates.

Rollback by restoring your backed-up DLLs with the game closed. The prior
`motorcycle-stability-client-wanted` test contains the reported crash and should
not be used to spawn bikes. Use a known earlier working backup or disable the mod
while diagnosing further. This artifact was not installed or published by the
agent; no game saves, configuration or production data were modified.
