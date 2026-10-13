using System;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppScheduleOne.Vehicles;
using Il2CppScheduleOne.Vehicles.Sound;
using UnityEngine;

namespace NugzzMenu.Services
{
    internal sealed class MotorcycleEngineAudio
    {
        private readonly LandVehicle _vehicle;
        private readonly AudioSource _source, _detail;
        private AudioClip _idleClip, _driveClip;
        private readonly float _volume;
        private readonly MotorcycleEngineNote _note = new();
        private float _envelope;
        private bool _disposed;
        internal AudioSource Source => _source;
        internal AudioSource Detail => _detail;

        internal MotorcycleEngineAudio(LandVehicle vehicle, GameObject model, Motorbike native, MotorcycleChassisSnapshot snapshot)
        {
            var controller = native.BurnoutController?._burnoutAudio;
            AudioSource template = controller?._audioSource ?? controller?.GetComponent<AudioSource>();
            if (template == null) throw new InvalidOperationException("Native motorcycle audio routing is unavailable");
            _vehicle = vehicle; _volume = Mathf.Clamp01(controller._defaultBaseVolume);
            // Register cleanup before either allocation so an upload failure in
            // the second bank also releases the first clip.
            snapshot.TrackRestore(Dispose);
            CreateBank(ref _idleClip, false, "Nugzz original V-twin idle");
            CreateBank(ref _driveClip, true, "Nugzz original V-twin load");
            var exhaust = new GameObject("Nugzz motorcycle exhaust"); exhaust.transform.SetParent(model.transform, false);
            _source = Voice(exhaust, _idleClip, template); _source.pitch = _note.Pitch;
            var detail = new GameObject("Nugzz motorcycle loaded exhaust"); detail.transform.SetParent(model.transform, false);
            _detail = Voice(detail, _driveClip, template); _detail.pitch = _note.LoadedPitch;
            // Native car audio coroutines can continue on a disabled behaviour.
            // Mute engine channels only; leave collision sounds alone.
            foreach (VehicleSound sound in vehicle.GetComponentsInChildren<VehicleSound>(true))
            {
                Mute(sound.EngineStartSource?._audioSource, snapshot);
                Mute(sound.EngineIdleSource?._audioSource, snapshot);
                Mute(sound.EngineLoopSource?._audioSource, snapshot);
            }
        }
        private static void CreateBank(ref AudioClip clip, bool driving, string label)
        {
            float[] samples = MotorcycleEngineSamples.Samples(driving);
            // AudioClip.Create's convenience overload is stripped in the game's
            // bindings. Use the inspected intact construction/upload wrappers.
            clip = AudioClip.Construct_Internal();
            if (clip == null) throw new InvalidOperationException("Could not create motorcycle audio bank");
            clip.CreateUserSound(label, samples.Length, 1, MotorcycleEngineSamples.SampleRate, false);
            if (!clip.SetData(new Il2CppStructArray<float>(samples), 0))
                throw new InvalidOperationException("Could not upload motorcycle audio bank");
        }
        private static AudioSource Voice(GameObject owner, AudioClip clip, AudioSource template)
        {
            var voice = owner.AddComponent<AudioSource>(); voice.clip = clip;
            voice.outputAudioMixerGroup = template.outputAudioMixerGroup;
            voice.spatialBlend = 1f; voice.minDistance = 2f; voice.maxDistance = 55f;
            voice.rolloffMode = AudioRolloffMode.Logarithmic; voice.dopplerLevel = 0.12f;
            voice.loop = true; voice.playOnAwake = false; voice.volume = 0f;
            return voice;
        }
        private static void Mute(AudioSource audio, MotorcycleChassisSnapshot snapshot)
        {
            if (audio == null) return;
            bool muted = audio.mute;
            snapshot.TrackRestore(() => { if (audio != null) audio.mute = muted; }); audio.mute = true;
        }
        internal void Update()
        {
            if (_disposed || _source == null || _detail == null) return;
            bool running = _vehicle.IsOccupied && _vehicle.IsVisible;
            float dt = Math.Max(0f, Time.deltaTime);
            _envelope = Mathf.MoveTowards(_envelope, running ? 1f : 0f, dt / (running ? 0.18f : 0.25f));
            // Hold the current blend during the exit fade so the low idle cannot
            // swell as the powered layer fades away.
            if (running) _note.Update(_vehicle.Speed_Kmh, _vehicle.currentThrottle,
                _vehicle.BrakesApplied || _vehicle.HandbrakeApplied, dt);
            _source.pitch = _note.Pitch;
            float blend = Mathf.Clamp01(_note.Rev * 1.25f + _note.Load * 0.15f);
            // Leave a low exhaust bed under the loaded bank. Bounded gains
            // and pre-normalized PCM leave headroom even during the crossover.
            _source.volume = _volume * _envelope * 0.50f * (1f - blend * 0.78f);
            _detail.pitch = _note.LoadedPitch;
            _detail.volume = _volume * _envelope * 0.58f * blend;
            if (running)
            { if (!_source.isPlaying) _source.Play(); if (!_detail.isPlaying) _detail.Play(); }
            if (!running && _envelope == 0f) Stop();
        }
        internal void Stop()
        {
            _envelope = 0f; _note.Reset();
            if (_source != null) { _source.volume = 0f; if (_source.isPlaying) _source.Stop(); }
            if (_detail != null) { _detail.volume = 0f; if (_detail.isPlaying) _detail.Stop(); }
        }
        internal void Dispose()
        {
            if (_disposed) return; Stop(); _disposed = true;
            if (_source != null) _source.clip = null;
            if (_detail != null) _detail.clip = null;
            if (_idleClip != null) UnityEngine.Object.Destroy(_idleClip);
            if (_driveClip != null) UnityEngine.Object.Destroy(_driveClip);
        }
    }
}
