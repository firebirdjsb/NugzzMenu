# Recorded Harley motorcycle audio test

Separate Schedule I 0.4.7f12 test build, locked S1API 3.2.1. Startup marker:
`Motorcycle recorded Harley audio 2026-10-13`. The existing body12 network
contract is unchanged; use the current revision on all peers to hear this mix.

The previous synthesized exhaust is replaced entirely with real recorded engine
audio. At a stop, a Shovelhead low-idle recording supplies the natural uneven
exhaust beat. Throttle blends smoothly into recorded Harley revs. Idle plays at
0.94 pitch, approximately one semitone lower; the recordings are shifted only
slightly as the engine responds. Bass EQ is +2.5 dB around 95 Hz, with subsonic
rumble removed below 32 Hz. No generated oscillator or game burnout clip is mixed.

Idle source gain is 0.48, raised from the previous primary voice's 0.36. This is
not a promise of a specific in-game loudness difference: the underlying sample
has changed. The processed idle RMS is 0.195, peak below 0.70. The driven loop RMS
is 0.240. Their combined gains stay below 0.755, leaving headroom through the
crossover. Pitch, load, entry/exit and braking responses are smoothed. Exit holds
the current voice blend while fading, avoiding a low-idle swell on dismount.

## Sources and processing

- Idle: **klankbeeld**, *HarleyDavidson Running Idle 130414_03.wav*, sound from
  [klankbeeld on Freesound.org](https://freesound.org/people/klankbeeld/).
  [Recording](https://freesound.org/people/klankbeeld/sounds/185077/),
  [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/).
  The uploader identifies a Shovelhead at low idle, recorded outdoors at 100 m.
  The public high-quality MP3 was used; no account-only original was accessed.
  Trimmed from 20.000 to 28.085 seconds, yielding a 7.965-second loop after joining.
- Driven: **Daniel Simion**, *Harley Davidson*,
  [SoundBible.com](https://soundbible.com/2211-Harley-Davidson.html),
  [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/).
  Public original WAV. Publisher identifies a real Harley idling/revving, without
  specifying its engine model. Trimmed 22.500–25.566 seconds; 2.946-second loop.

Both recordings are converted to mono, resampled to 24 kHz, gently filtered,
phase-matched and crossfaded for looping, then normalized with constant gain.
Their natural dynamics are retained; no synthetic bass/exhaust pulses are added.
The Shovelhead is the documented classic engine source. The driven recording's
exact engine/exhaust specification and RPM are unverified. This is a blend of two
real recordings, not a calibrated recording of one bike across its full RPM range.
Outdoor ambience may remain. No endorsement is implied. Retain audio attribution
with redistribution; these audio adaptations keep their respective CC licenses.

## Changed files

- `Assets/Audio/HarleyIdle.pcm`, `HarleyDrive.pcm`, `provenance.json`,
  `ATTRIBUTION.md`: licensed recordings, processing data, hashes and credits.
- `Services/MotorcycleEngineRecordings.cs`: bounded embedded PCM16 decoder with
  cached managed buffers; no disk path dependency or audio-thread callbacks.
- `Services/MotorcycleEngineAudio.cs`, `MotorcycleEngineNote.cs`: recorded loops,
  gentle pitch range and smooth bounded mixing. Both private clips are released
  on detach, scene reset, expired vehicle and partial conversion failures.
- `Services/MotorcycleExhaustSynth.cs`: retired; the earlier implementation is
  preserved with prior test evidence. It is absent from this Release assembly.
- `SeshMenu.csproj`, `Core.cs`: embedded assets/credits and startup identification.
- `tests/LocalAnimationChecks`: recording hashes, PCM bounds/seam slopes, native
  source isolation, voice lifecycle and second-clip upload failure rollback.

Rider pose, hand/foot alignment, hill handling, steering, stronger brakes,
police vehicle spawning, menu/config/save behavior and network contracts retain
the preceding implementation. `Services/VehicleService.cs` remains exact HEAD.

## Evidence and limits

Release build: zero warnings/errors. 185 offline checks pass, including 123 local
animation/motorcycle checks. API audit: 255 types, 811 members, 85 Harmony targets,
12 dynamic targets, zero errors. All 1,321 Unity call sites inspected in the
animation/motorcycle/Never Wanted modules, including nested types, avoid throwing
generated stubs. Zero `GetGroundHit` references; the earlier fatal output binding
remains excluded. AudioClip construction/upload uses the already inspected intact
native wrappers, not the stripped convenience overload.

Package recording audit includes original/processed hashes, actual source URLs,
licenses, trimming/EQ/crossfade settings, peak/RMS/DC and loop measurements.
PCM and attribution are embedded in the menu DLL. No game recordings are bundled.
Two private clips consume approximately 1.05 MB of PCM per bike, at most four bikes,
plus one shared decoded cache. Offline previews use production voice gain/pitch
math, without native spatial attenuation, Doppler or the game's mixer.

No in-game listening or two-player audio QA was performed by the agent. Test idle
at a stop, repeated throttle/coasting/braking, reversing, mounting/dismounting,
near/far observer sound, existing game volume settings and scene cleanup. Adjust
only the audio mix if listening reveals ambient noise, an obvious repeating rev
or an intrusive idle. Offline tests cannot establish perceived authenticity.

## Installation and rollback

This is a separate test artifact, not an installed/public release. Close the game,
back up replaced DLLs outside Mods/Plugins, then copy the ZIP's Mods and Plugins
folders into the game folder. Keep one NugzzMenu DLL. Preserve the existing config;
the example is reference only. Restart the session and spawn a fresh motorcycle.
Use this same revision on host and clients, with client approval/handshake as before.

Rollback: close the game, restore backed-up DLLs or the preceding deep V-twin/brakes
test on all peers, then restart. Never use the older stability/client-wanted test
that contains the fatal WheelHit binding. No save/config migration. Installed DLLs,
existing packages, game saves/config and production data were not changed.
