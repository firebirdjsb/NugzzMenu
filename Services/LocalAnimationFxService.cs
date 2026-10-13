using System;
using System.Collections.Generic;
using Il2CppScheduleOne.Audio;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.NPCs.Other;
using Il2CppScheduleOne.PlayerScripts;
using UnityEngine;
using UnityEngine.VFX;

namespace NugzzMenu.Services
{
    // Own only temporary effects. Never invoke NPC actions, product-effect
    // coroutines, or the burnout controller's rigidbody/joint creation.
    internal sealed class LocalAnimationFxService
    {
        internal static readonly LocalAnimationFxService Instance = new();
        private sealed class Template
        {
            internal GameObject Source;
            internal Vector3 Position, Scale;
            internal Quaternion Rotation;
        }
        private sealed class Effect
        {
            internal Player Player;
            internal GameObject Root;
            internal Transform Anchor;
            internal Vector3 Origin;
            internal float EndsAt;
            internal int Action;
        }
        private Template _pee, _vomit;
        private readonly Dictionary<int, Effect> _effects = new();
        private readonly List<int> _expired = new();
        internal int Count => _effects.Count;

        // Only an effect-only hierarchy can be instantiated. Audio controllers
        // initialize their own copied AudioSources; NPC/network/physics scripts
        // and arbitrary MonoBehaviours are rejected before Instantiate runs.
        internal static bool SafeTemplate(GameObject source)
        {
            if (source == null) return false;
            foreach (Component c in source.GetComponentsInChildren<Component>(true))
                if (c.TryCast<Transform>() == null && c.TryCast<ParticleSystem>() == null &&
                    c.TryCast<ParticleSystemRenderer>() == null && c.TryCast<AudioSource>() == null &&
                    c.TryCast<AudioSourceController>() == null && c.TryCast<VisualEffect>() == null &&
                    c.TryCast<VFXRenderer>() == null) return false;
            return true;
        }
        private Template Find(string parameter, bool mouth)
        {
            foreach (NPCAuxAction action in Resources.FindObjectsOfTypeAll<NPCAuxAction>())
            {
                if (action == null || action._animBoolName != parameter || !SafeTemplate(action._auxEffect)) continue;
                NPC npc = action._npc ?? action.GetComponentInParent<NPC>(true);
                Transform anchor = mouth ? npc?.Avatar?.Effects?.HeadBone : npc?.Avatar?.AttachmentAnchorProvider?.Hips;
                if (anchor == null || action._auxEffect.GetComponentsInChildren<ParticleSystem>(true).Length == 0) continue;
                Transform source = action._auxEffect.transform;
                Vector3 size = anchor.lossyScale;
                if (Math.Abs(size.x) < 0.00001f || Math.Abs(size.y) < 0.00001f || Math.Abs(size.z) < 0.00001f) continue;
                Vector3 scale = source.lossyScale;
                return new Template { Source = action._auxEffect, Position = anchor.InverseTransformPoint(source.position),
                    Rotation = Quaternion.Inverse(anchor.rotation) * source.rotation,
                    Scale = new Vector3(scale.x / size.x, scale.y / size.y, scale.z / size.z) };
            }
            return null;
        }
        internal bool CanCreate(int action)
        {
            if (action == LocalAnimationCatalog.Pee) return (_pee ??= Find("IsPissing", false))?.Source != null;
            if (action == LocalAnimationCatalog.Vomit) return (_vomit ??= Find("IsVomiting", true))?.Source != null;
            return true;
        }
        internal bool Start(Player player, int action, float duration, Transform prop = null)
        {
            if (action != LocalAnimationCatalog.Pee && action != LocalAnimationCatalog.Vomit && action != LocalAnimationCatalog.Motorcycle) return true;
            Stop(player);
            GameObject root = null;
            try
            {
                if (!CanCreate(action)) return false;
                Transform anchor;
                if (action == LocalAnimationCatalog.Motorcycle)
                {
                    var source = LocalAnimationVisualService.Instance.MotorcycleAsset;
                    var controller = source?.BurnoutController;
                    if (prop == null || controller?.burnoutEffect == null || !SafeTemplate(controller.burnoutEffect.gameObject)) return false;
                    // Copy the complete native VFX-only object, preserving graph
                    // overrides/textures. Keep it at the copied tire contact point.
                    root = UnityEngine.Object.Instantiate(controller.burnoutEffect.gameObject, prop, false);
                    root.transform.localPosition = source.transform.InverseTransformPoint(controller.burnoutEffect.transform.position);
                    root.transform.localRotation = Quaternion.Inverse(source.transform.rotation) * controller.burnoutEffect.transform.rotation;
                    var vfx = root.GetComponent<VisualEffect>();
                    root.SetActive(true); vfx.enabled = true;
                    // Native BeginBurnout uses 36 particles/sec in this build.
                    if (vfx.HasInt("EmissionRate")) vfx.SetInt("EmissionRate", 36);
                    vfx.Play();
                    AudioSource native = controller._burnoutAudio?._audioSource ?? controller._burnoutAudio?.GetComponent<AudioSource>();
                    if (native == null || native.clip == null) throw new InvalidOperationException("Motorcycle audio is not loaded");
                    var audio = root.AddComponent<AudioSource>();
                    audio.clip = native.clip; audio.outputAudioMixerGroup = native.outputAudioMixerGroup;
                    // A live idle bike can have volume zero. Use the controller's
                    // configured playing volume/pitch rather than its current fade.
                    audio.volume = Mathf.Clamp01(controller._burnoutAudio._defaultBaseVolume * controller._burnoutAudio.VolumeMultiplier);
                    audio.pitch = controller._burnoutAudio._defaultBasePitch * controller._burnoutAudio.PitchMultiplier;
                    audio.spatialBlend = native.spatialBlend;
                    audio.minDistance = native.minDistance; audio.maxDistance = native.maxDistance; audio.rolloffMode = native.rolloffMode;
                    audio.playOnAwake = false; audio.loop = true; audio.Play();
                    anchor = prop;
                }
                else
                {
                    bool mouth = action == LocalAnimationCatalog.Vomit;
                    anchor = mouth ? player.Avatar?.Effects?.HeadBone : LocalAnimationPlacementService.Hip(player);
                    if (anchor == null) return false;
                    Template template = mouth ? _vomit : _pee;
                    root = UnityEngine.Object.Instantiate(template.Source, anchor, false);
                    root.transform.localPosition = template.Position; root.transform.localRotation = template.Rotation;
                    root.transform.localScale = template.Scale;
                    root.SetActive(true);
                    foreach (var particles in root.GetComponentsInChildren<ParticleSystem>(true)) particles.Play(true);
                    foreach (var audio in root.GetComponentsInChildren<AudioSource>(true))
                    { if (audio.clip != null) { audio.loop = true; audio.Play(); } }
                }
                root.name = "Nugzz animation FX";
                _effects[player.GetInstanceID()] = new Effect { Player = player, Root = root, Anchor = anchor,
                    Action = action, Origin = player.transform.position, EndsAt = Time.unscaledTime + Math.Min(duration, LocalAnimationCatalog.Duration) };
                return true;
            }
            catch (Exception ex)
            {
                if (root != null) UnityEngine.Object.Destroy(root);
                DebugLogService.Instance.VerboseWarning("Animation FX failed: " + ex.Message);
                return false;
            }
        }
        internal void Stop(Player player)
        {
            if (player != null && _effects.Remove(player.GetInstanceID(), out Effect effect)) Dispose(effect);
        }
        private static void Dispose(Effect effect)
        { if (effect.Root != null) { effect.Root.SetActive(false); UnityEngine.Object.Destroy(effect.Root); } }
        internal void Update()
        {
            _expired.Clear();
            foreach (var entry in _effects)
            {
                Effect e = entry.Value;
                try
                {
                    Transform anchor = e.Action == LocalAnimationCatalog.Vomit ? e.Player?.Avatar?.Effects?.HeadBone :
                        e.Action == LocalAnimationCatalog.Pee ? LocalAnimationPlacementService.Hip(e.Player) : e.Anchor;
                    if (e.Player == null || e.Root == null || anchor == null || anchor != e.Anchor ||
                        !e.Player.Health.IsAlive || e.Player.IsRagdolled || e.Player.CurrentVehicle != null ||
                        !SessionAuthorityService.Instance.FeaturesAllowed || Time.unscaledTime >= e.EndsAt ||
                        (e.Player.transform.position - e.Origin).sqrMagnitude > 0.36f) _expired.Add(entry.Key);
                }
                catch { _expired.Add(entry.Key); }
            }
            foreach (int id in _expired) { Dispose(_effects[id]); _effects.Remove(id); }
        }
        internal void Reset()
        {
            foreach (Effect effect in _effects.Values) { try { Dispose(effect); } catch { } }
            _effects.Clear(); _expired.Clear(); _pee = _vomit = null;
        }
    }
}
