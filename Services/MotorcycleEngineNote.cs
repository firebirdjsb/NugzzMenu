using System;

namespace NugzzMenu.Services
{
    internal sealed class MotorcycleEngineNote
    {
        internal const float IdleRpm = 1000f, MaximumRpm = 3000f;
        internal float Rpm { get; private set; } = IdleRpm;
        internal float Load { get; private set; }
        internal float Speed { get; private set; }
        internal float Rev => Math.Clamp((Rpm - IdleRpm) / (MaximumRpm - IdleRpm), 0f, 1f);
        // Each bank has an explicit authored RPM. Both voices therefore share
        // the same firing cadence as RPM changes, rather than arbitrary pitches.
        internal float Pitch => Rpm / MotorcycleEngineSamples.IdleBankRpm;
        internal float LoadedPitch => Rpm / MotorcycleEngineSamples.LoadedBankRpm;
        internal void Update(float speedKmh, float throttle, bool braking, float deltaTime)
        {
            float dt = Math.Max(0, deltaTime);
            Speed = Math.Clamp(Math.Abs(speedKmh) / 80f, 0f, 1f);
            float targetLoad = braking ? 0 : Math.Clamp(Math.Abs(throttle), 0f, 1f);
            Load = Respond(Load, targetLoad, dt, 0.07f, 0.20f);
            float targetRpm = Math.Clamp(IdleRpm + Speed * 1000f + Load * (1300f + Speed * 650f), IdleRpm, MaximumRpm);
            Rpm = Respond(Rpm, targetRpm, dt, 0.14f, 0.30f);
        }
        private static float Respond(float current, float target, float dt, float rise, float fall)
        { return current + (target - current) * (1f - (float)Math.Exp(-dt / (target > current ? rise : fall))); }
        internal void Reset() { Rpm = IdleRpm; Load = Speed = 0; }
    }
}
