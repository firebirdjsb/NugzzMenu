using System;
using System.IO;

namespace NugzzMenu.Services
{
    internal static class MotorcycleEngineSamples
    {
        internal const int SampleRate = 24000;
        internal const float IdleBankRpm = 1200f, LoadedBankRpm = 2400f;
        private static readonly Lazy<float[]> Idle = new(() => Read("VTwinIdle"));
        private static readonly Lazy<float[]> Drive = new(() => Read("VTwinLoad"));
        internal static float[] Samples(bool driving) => driving ? Drive.Value : Idle.Value;

        private static float[] Read(string name)
        {
            using Stream stream = typeof(MotorcycleEngineSamples).Assembly
                .GetManifestResourceStream("NugzzMenu.Audio." + name + ".pcm");
            // Headerless signed PCM16, mono at 24 kHz. Validate byte count before
            // conversion; no unmanaged pointers, file paths or audio callbacks.
            if (stream == null || stream.Length < SampleRate || stream.Length > SampleRate * 2 * 12 || stream.Length % 2 != 0)
                throw new InvalidDataException("Invalid embedded motorcycle audio bank: " + name);
            using var reader = new BinaryReader(stream);
            var samples = new float[(int)stream.Length / 2];
            for (int i = 0; i < samples.Length; i++) samples[i] = reader.ReadInt16() / 32768f;
            return samples;
        }
    }
}
