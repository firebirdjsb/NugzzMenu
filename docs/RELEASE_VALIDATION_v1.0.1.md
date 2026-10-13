# v1.0.1 release validation

Release prepared from a separate checkout based on the published v1.0.0 commit
`2412c7c02da9dbc247374dfb59be4da743013fa7`. The existing development checkout and
its historical test artifacts were preserved. Player notes are in
`RELEASE_NOTES_v1.0.1.md`; `CHANGELOG.md` includes the full v1.0.1 entry.

## Version and scope

All menu, MelonLoader, file, informational and multiplayer version labels are
1.0.1; assembly version is 1.0.1.0. The previous test-build startup marker is
replaced with the v1.0.1 release marker. Existing body12 animation/motorcycle
wire behavior is unchanged. Rider/handling/braking and all earlier accepted
fixes retain the final preceding implementation.

The release covers the Special Customers compatibility work, current-frame
interaction prompts, phone/Character input ownership, planting/watering,
Never Wanted including approved clients, config, clothing picker, local player
animations/effects and the session motorcycle. Police spawning is exact v1.0.0
`Services/VehicleService.cs`. The rejected downloaded audio is excluded.

## Build and automated results

- Clean Release build: zero warnings and errors.
- 124 animation/motorcycle checks, 27 state/policy checks, 11 prompt checks,
  6 clothing-picker checks, 9 config checks, 9 accessory-failure checks: **186**.
- Current game dump audit: 255 types, 811 members, 85 static patch targets and
  12 dynamic targets; no errors; required Special Customers types present.
- Generated Unity call inspection: 1,321 call sites including nested types;
  no throwing generated stubs in the inspected modules.
- Zero `WheelCollider.GetGroundHit` references. The earlier fatal binding
  remains excluded; safe ground raycasts are retained.
- Release metadata/resource inspection verifies every 1.0.1 label and exactly
  the two original V-twin banks plus their notes, byte-identical to source.
- Final menu DLL SHA-256:
  `82e1df7b5c22d60fe2f139c9e1cb6e948eb1d0400649832e359e33fee8bcfea1`.
- S1API 3.2.1 dependency ZIP and both packaged IL2CPP files are checked against
  `S1API.lock.json` and the upstream archive. Dependency MIT license included.
- Package CRC, complete file list and every packaged file hash are verified.
  Package source commit is recorded in its manifest and must be a clean checkout.
- Whitespace checks and police-source equality check pass. Publication source
  excludes build references, dumps, game assets, local logs, test ZIPs and rejected
  recording assets. Only original authored PCM is committed.

## Runtime evidence and limits

Earlier user feedback confirmed motorcycle hill handling, visible rider and
subsequently accepted rider posture. That feedback is not a complete live test
of this version. The original sound preview was produced from the real audio
controller through its SDK fixture; it does not emulate Unity's audio mixer,
resampler or distance effects.

No in-game smoke test or final two-player playtest was performed by the agent.
The shelf-crown cause remains unconfirmed; a separate intermittent dark-screen
report outside the demonstrated Phone/Character path is not claimed fixed.
The public notes retain these limits. Verify those cases, all eight animations,
the color picker, grow prompts, watering, host/client protection, visitor queue,
new content spawning, motorcycle behavior and audio in live sessions.

No new raw memory writes, native detours or allocation caves are introduced by
the release/version work. PE offset and JMP/CALL patch-integrity checks do not
apply. Compiled API/binding/resource audits are the relevant static checks here.

## Publication and rollback

The owner explicitly requested GitHub publication. Source and v1.0.1 release
assets are prepared for that authorized action. Exact published commit and
artifact hashes are captured in the package/publication evidence.

No game installation, save files or existing user config were modified. Install
manually using the package instructions. Close the game and restore backed-up
DLLs to roll back; preserve the config. Prior release/test assets are untouched.
