"""Author dry V-twin PCM from pressure pulses, pipe modes and combustion noise.

Python + NumPy are authoring tools only; the mod loads the embedded finished PCM.
No recordings, external sound library, audio-thread callbacks or runtime synthesis.
"""
from pathlib import Path
import hashlib
import json
import wave
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / 'Assets/Audio'
EVIDENCE = ROOT / 'trialbuild/motorcycle-authored-audio'
RATE, SECONDS = 24000, 6
FIRING_FRACTION = 315 / 720

def digest(data):
    return hashlib.sha256(data).hexdigest()

def circular_noise(rng, count):
    # Periodic colored noise has no discontinuous random-buffer loop boundary.
    noise = rng.standard_normal(count)
    freq = np.fft.rfftfreq(count, 1 / RATE)
    response = freq / (freq + 180) / np.sqrt(1 + (freq / 2400) ** 4)
    result = np.fft.irfft(np.fft.rfft(noise) * response, count)
    return result / np.sqrt(np.mean(result * result))

def generate(name, rpm, loaded):
    count = RATE * SECONDS
    period = 120 / rpm  # Four-stroke 720-degree cycle, seconds.
    cycles = round(SECONDS / period)
    rng = np.random.default_rng(0x45_315_405 + (1 if loaded else 0))
    noise = circular_noise(rng, count)
    pressure = np.zeros(count)
    texture = np.zeros(count)
    t = np.arange(round(.15 * RATE)) / RATE
    # Air displacement includes the negative tail, rather than an endless bass
    # sine. Several damped pipe modes supply a broad thud and a short bark.
    scale = 2 if loaded else 1
    attack = 1 - np.exp(-t / (.00045 if loaded else .0008))
    blowdown = np.exp(-t / .0038) - .23 * np.exp(-t / .022)
    bass = .68 * np.sin(2 * np.pi * (88 * scale * t - 35 * scale * t*t)) * np.exp(-t / .028)
    body = .29 * np.sin(2 * np.pi * 167 * scale * t) * np.exp(-t / .017)
    bark = (.19 if loaded else .10) * np.sin(2 * np.pi * 510 * scale * t) * np.exp(-t / .006)
    kernel = attack * (.65 * blowdown + bass + body + bark)
    hiss_envelope = attack * np.exp(-t / (.011 if loaded else .007))
    amplitudes = 1 + .045 * np.sin(2*np.pi*np.arange(cycles)/cycles * 7) + rng.uniform(-.035,.035,cycles)
    offsets = []
    for cycle in range(cycles):
        for cylinder, fraction in enumerate([0, FIRING_FRACTION]):
            # The alternating 315/405 gaps stay exact; only pulse strength varies.
            start = round((cycle + fraction) * period * RATE)
            offsets.append(start)
            indices = (start + np.arange(len(t))) % count
            strength = amplitudes[cycle] * (1 if cylinder == 0 else .92)
            pressure[indices] += kernel * strength
            texture[indices] += hiss_envelope * strength
    raw = pressure + noise * texture * (.26 if loaded else .10)
    # Short signed reflections resemble pressure traveling down an exhaust pipe.
    raw = raw - .18 * np.roll(raw, round(.0031*RATE)) + .08 * np.roll(raw, round(.0062*RATE))
    raw = np.tanh(raw * (1.20 if loaded else 1.05))
    f = np.fft.rfftfreq(count,1/RATE)
    # Subsonic rejection and a gentle upper rolloff keep the result deep and dry.
    response = (f / np.sqrt(f*f + 28**2)) / np.sqrt(1+(f/5200)**4)
    raw = np.fft.irfft(np.fft.rfft(raw) * response,count)
    raw -= raw.mean()
    target_rms = .225 if loaded else .215
    gain = min(target_rms / np.sqrt(np.mean(raw*raw)), .72 / abs(raw).max())
    raw *= gain
    pcm = np.round(raw*32767).astype('<i2')
    path = ASSETS / (name+'.pcm'); path.write_bytes(pcm.tobytes())
    samples = pcm.astype(float) / 32768
    with wave.open(str(EVIDENCE/(name+'-loop.wav')),'wb') as out:
        out.setnchannels(1); out.setsampwidth(2); out.setframerate(RATE); out.writeframes(pcm.tobytes())
    power = abs(np.fft.rfft(samples))**2
    steps = abs(np.diff(samples))
    return {
        'asset':'Assets/Audio/'+name+'.pcm','sha256':digest(pcm.tobytes()),
        'sampleRate':RATE,'channels':1,'format':'signed 16-bit little-endian PCM',
        'samples':count,'durationSeconds':SECONDS,'baseRpm':rpm,
        'firingDegrees':[315,405],'firstFiringOffsetsSamples':offsets[:4],
        'firingGapsSamples':np.diff(offsets[:4]).tolist(),
        'peak':float(abs(samples).max()),'rms':float(np.sqrt(np.mean(samples*samples))),
        'dcOffset':float(samples.mean()),'seamDelta':float(abs(samples[0]-samples[-1])),
        'p999SampleDelta':float(np.quantile(steps,.999)),
        'energyBelow300Hz':float(power[f<300].sum()/power.sum()),
        'energyAbove800Hz':float(power[f>800].sum()/power.sum()),
        'gain':float(gain),'pulseCount':len(offsets)
    }

def main():
    ASSETS.mkdir(parents=True,exist_ok=True); EVIDENCE.mkdir(parents=True,exist_ok=True)
    loops=[generate('VTwinIdle',1200,False),generate('VTwinLoad',2400,True)]
    for loop in loops:
        assert loop['peak']<=.721 and abs(loop['dcOffset'])<.00001
        assert loop['seamDelta']<loop['p999SampleDelta']
        assert .18<loop['rms']<.24
    report={'origin':'Original procedural sound authored for NugzzMenu; no recorded samples',
            'generator':'scripts/Generate-MotorcycleAudio.py',
            'generatorSha256':digest(Path(__file__).read_bytes()),
            'idlePlaybackRpm':1000,'maximumPlaybackRpm':3000,
            'firingDegrees':[315,405],'loops':loops,'gameAudioAssetsIncluded':False,
            'externalRecordingsIncluded':False,'runtimeAudioCallbacks':False,
            'perceptualAndInGameQA':'pending'}
    (ASSETS/'design.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    (EVIDENCE/'audio-audit.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(loops,indent=2))

if __name__=='__main__': main()
