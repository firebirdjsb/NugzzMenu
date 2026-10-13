# Deep V-twin idle and easier braking — 2026-10-13

Schedule I 0.4.7f12 separate test build. Matching host/client revision **body12**.
Startup marker: `Motorcycle deep V-twin idle + easier braking 2026-10-13`.

## Problem and scope

The user confirms the preceding rider-posture build works and requests deeper,
lower-pitched Harley-style idle thumping and easier stopping. The installed DLL
matches that build (SHA256
`d9d59899ea2d25ee74144b458f60180a87484b984ab1dd718fc6690e128d68b6`), and Latest.log
contains its marker and visible-rider diagnostics.

Read-only game asset inspection shows the previous primary clip is **BurnoutSFX**,
a 35.53-second streaming recording with rev changes. Pitching that whole clip
cannot make a steady input-driven idle. The game also contains an
`Idle_Untuned_Engine_01` clip; the new mix borrows it if Unity has loaded it.

This revision changes motorcycle audio and brake tuning only. The user-confirmed
rider pose, saddle/foot targets, height, steering, hill/stability safeguards and
native driving/reverse controls remain. Police spawning stays reverted; client
Never Wanted remains. No save/config schema change, new dependency or deployment.

## Changed files

- `Services/MotorcycleExhaustSynth.cs`: original four-second, mono 24 kHz PCM
  exhaust layer with bass-heavy paired firing pulses and continuous tails at the
  loop seam. The 315/405-degree timing model is informed by the technical
  description in [US7023214B2](https://patents.google.com/patent/US7023214B2/en).
  This is an authored V-twin-style sound, not a recorded Harley-Davidson engine
  sample. No downloaded or extracted game recordings are bundled.
- `Services/MotorcycleEngineNote.cs`: low **850 RPM idle** design setting,
  smoothed throttle/load/road-speed response and bounded revs. Native braking
  suppresses powered revs, so the engine settles as the bike stops.
- `Services/MotorcycleEngineAudio.cs`: the new exhaust is the primary voice,
  pitched to about 0.71 at idle. A lower-pitched, filtered native idle texture
  plays if available. Otherwise the recorded burnout is a quiet throttle-only
  detail layer and is silent at idle. Both voices keep entry/exit fades, 3D
  falloff, native mixer routing and no restart on throttle or quick re-entry.
  Generated PCM is created once per bike on the game thread; no managed audio
  callback runs on Unity's audio thread. Owned clips are explicitly released.
- `Services/MotorcycleHandling.cs`: mass-scaled regular brakes **2.4x** the
  preceding bike setting and handbrake **1.8x**. Keep native brake/reverse input
  and curves; do not inject throttle or add a separate force controller.
- `Services/MotorcycleService.cs`: release the owned PCM on detach, removal,
  scene reset and expired vehicles. Borrowed native clips remain untouched.
- `Core.cs`, `Services/LocalAnimationNetworkService.cs`: startup marker and body12
  match requirement; all peers need the same physical brake revision.
- `tests/LocalAnimationChecks`: eight new audio/brake regressions and updates to
  the existing audio checks to cover generated exhaust plus borrowed texture.

## Verification and limits

- Release: **zero warnings/errors**; **185 offline checks passed** (123 animation/
  motorcycle, 27 state/policy, 11 prompt, 6 picker, 9 config, 9 accessory).
- New tests cover finite/bounded/DC-free PCM, unequal pulse placement, loop seam,
  stable idle, native texture borrowing, pitch-down, braking/reverse transitions,
  both voice fades, no restart, cleanup, PCM failure rollback and brake restore.
- API/Harmony audit: **255 types, 811 members, 85 patches, 12 dynamic patches**,
  zero errors against the supplied dump and locked S1API 3.2.1.
- The binding audit caught `AudioClip.Create` resolving to a throwing stripped
  wrapper. The final code uses the inspected intact `Construct_Internal` and
  `CreateUserSound` wrappers plus SetData with a mono float array. **1327 Unity
  call sites** avoid throwing stubs; the DLL has **zero GetGroundHit calls**.
- Original idle-layer preview: eight seconds at the design idle, mono 24 kHz;
  peak 0.507, dominant frequency 63.75 Hz and over 99.9% of energy below 300 Hz.
  The preview excludes the game's texture voice, spatial mixing and Doppler.
- Diff whitespace and exact HEAD police-spawn checks pass. Archive verification
  checks payload/dependency hashes and preserves installed DLLs/earlier ZIPs.

The user's report is runtime acceptance of the previous posture revision. These
checks establish sound-data/control/lifetime behavior and binding availability,
not subjective realism, the final in-game mix or actual stopping distance.
An owning-client/observer session and driving/listening QA remain required.
No raw process offsets, executable patches, protected-memory writes or memory
caves were added; the absent verify_offsets.py was not run or claimed as run.

## Install, acceptance and rollback

Close the game. Back up replaced DLLs outside Mods/Plugins and extract the ZIP's
Mods/Plugins folders into the game directory. Keep one NugzzMenu DLL; preserve
the existing config (the example is reference only). The locked S1API 3.2.1 pair
is included. Host and clients need this same body12 revision; approve clients in
Lobby, restart the session and spawn a **fresh motorcycle**.

Using a copied/disposable save: listen stopped at idle for 20 seconds, blip the
throttle, drive/coast, brake to a stop, reverse, exit and quickly re-enter. Check
that idle stays deep without recorded burnout revs, the mix is comfortable and
stopping is easier without wheel lock or an abrupt reverse transition. Test
handbrake, hills and owning-client/observer sound; recheck the accepted rider pose.
Remove/respawn and restart the session to check cleanup. The original preview is
in `preview/idle-thump-preview.wav`; it is not a capture of the game's full mix.

Rollback with the game closed by restoring backups or the preceding rider-posture
test on all peers, then restarting. Do not use the older stability-client-wanted
test, which contains the fatal WheelHit binding. No save/config migration is
needed. This artifact was **not installed or published by the agent**; installed
game DLLs, saves, config and production data were not modified.
