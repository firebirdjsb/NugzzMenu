# Original chopper audio test

Separate Schedule I 0.4.7f12 test, S1API 3.2.1. Startup marker:
`Motorcycle original chopper audio 2026-10-13`. Existing body12 network contract.

The user rejected the recorded-engine experiment. This revision replaces it with
original dry V-twin audio. It does not reuse that recording mix or simply revert
to the earlier sine-heavy exhaust loop. The authoring script builds bipolar
pressure pulses, several damped pipe resonances, small combustion-strength
variations, transient colored noise and short signed exhaust reflections.
Soft saturation adds some grit; subsonic rejection and a gentle upper rolloff
keep it bass-heavy. There is no traffic, outdoor ambience or baked-in reverb.

The requested 315/405-degree firing pattern is applied across each 720-degree
cycle. Simulated playback idles at 1000 RPM, with 52.5 ms then 67.5 ms between
shots. A 1200 RPM bank supplies the heavy idle; a sharper 2400 RPM bank supplies
loaded exhaust. Their pitches follow one simulated RPM, keeping the cadence
matched while blending. Idle is 0.8333 pitch; the loaded bank plays silently at
0.4167 pitch until throttle/speed raises its gain. RPM caps at 3000. Transient
load and RPM respond faster to throttle, then settle smoothly after braking.

Idle voice gain is 0.50, up slightly from the preceding test's 0.48. The loaded
voice reaches 0.58 while the idle bed reduces; their sum never exceeds 0.69.
This avoids a large volume jump at the crossover. These values are multiplied by
the native volume routing and entry/exit envelope. The fixture-rendered idle peak
is approximately 0.352 and RMS 0.106; the throttle showcase peak is 0.350 and RMS
0.127. These are measurements, not an in-game perceived-volume guarantee.

## Files changed

- `scripts/Generate-MotorcycleAudio.py`: complete reproducible original authoring
  workflow. Python/NumPy are development tools only, not mod runtime dependencies.
- `Assets/Audio/VTwinIdle.pcm`, `VTwinLoad.pcm`, `design.json`, `AUDIO-NOTES.md`:
  original banks, design/provenance and measured bounds. Six seconds per bank,
  mono PCM16 at 24 kHz. Rejected recording assets are not embedded or packaged.
- `Services/MotorcycleEngineSamples.cs`: cached bounded PCM decoder, replacing
  the recording-specific loader.
- `Services/MotorcycleEngineAudio.cs`, `MotorcycleEngineNote.cs`: original banks,
  matching RPM/cadence, volume blending and quicker throttle response.
- `SeshMenu.csproj`, `Core.cs`: active embedded resources and startup marker.
- `tests/LocalAnimationChecks`: updated original audio tests plus pulse-pair and
  cadence assertions. `MotorcycleAudioPreview.cs` renders the real production
  controller through its SDK fixture, without duplicating the mix math.

Steering, hill stability, height, rider/hand/foot poses, brakes, native driving
controls, police vehicle spawning, phone/clothing handling, Never Wanted, config
and network protocols are outside this change and retain the accepted settings.
`Services/VehicleService.cs` remains identical to HEAD. Prior test ZIPs are retained.

## Checks and limitations

Release: zero warnings/errors. **186 offline checks**, including 124 local
animation/motorcycle checks. PCM hashes, finite/DC/peak bounds, seam slopes,
audible pulse-energy contrast, shared firing cadence, reverse/braking response,
frame-rate response, voice reuse, native-source isolation and partial upload
failure cleanup are checked. These tests do not judge how appealing the sound is.

API audit: 255 types, 811 members, 85 Harmony targets, 12 dynamic targets, zero
errors. The generated-binding audit inspects 1,321 Unity call sites including
nested types. No throwing stripped wrappers, no `GetGroundHit` calls and no
managed audio-thread callbacks. The safe inspected AudioClip construction/upload
wrappers remain in use. Both private clips are released on conversion rollback,
detach, expired vehicles and scene cleanup. PCM cost: approximately 1.15 MB per
bike, maximum four bikes, plus a shared decoded cache.

The Release resource audit confirms exactly the two original banks and their
notes, byte-identical to source, with zero rejected recording resources. Audio
audit includes generator/bank hashes, exact shot offsets, RMS/peak/DC/seam and
frequency-band measurements. Preview WAVs render the production controller at
120 Hz using the SDK fixture; Unity's actual resampler, mixer, distance and Doppler
are not simulated. No in-game listening or two-player sound QA was performed.

Idle preview: 15 seconds. Throttle preview: 22 seconds, including a stationary
blip, acceleration, braking, return to idle, reverse and dismount fade. The result
is an authored game sound, not a recording or a calibrated physical reproduction
of any particular engine/exhaust. Perceived realism and final game volume balance
need user listening feedback, especially at high RPM and from nearby observers.

## Installation and rollback

Separate test artifact; no installation/publication by the agent. Close the game,
back up replaced DLLs outside Mods/Plugins, copy the ZIP's Mods and Plugins folders
into the game directory, and retain one NugzzMenu DLL. Preserve the existing config;
the example is reference only. Restart the session and spawn a fresh bike. Host
and clients should use this same current body12 revision and existing approval/
handshake. Network contract remains unchanged.

Rollback: close the game and restore backed-up DLLs or the earlier deep V-twin/
brakes test, then restart all peers. The rejected recorded test is also preserved
as a historical artifact. Never use the older stability/client-wanted ZIP with
the fatal WheelHit binding. No save/config migration. Installed DLLs, game saves/
config and production data were not modified.
