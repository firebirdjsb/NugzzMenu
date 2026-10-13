using System.IO;
using System.Text;
using System.Text.Json;
using Il2CppScheduleOne.Vehicles;
using NugzzMenu.Services;
using UnityEngine;

// Renders the production controller through the SDK fixture. Unity's mixer,
// resampler, Doppler and distance attenuation still require in-game listening.
internal static class MotorcycleAudioPreview
{
    internal static object Render(LandVehicle vehicle, MotorcycleEngineAudio engine, string path, bool throttle)
    {
        const int rate=24000,block=200;
        int seconds=throttle?22:15;
        var samples=new float[seconds*rate];double primaryPhase=0,loadedPhase=0;
        float priorDt=Time.deltaTime;Time.deltaTime=(float)block/rate;
        try
        {
            for(int offset=0;offset<samples.Length;offset+=block)
            {
                float time=(float)offset/rate;
                vehicle.Speed_Kmh=vehicle.currentThrottle=0;vehicle.BrakesApplied=false;
                if(throttle)
                {
                    if(time>=3&&time<4.2f)vehicle.currentThrottle=1;
                    else if(time>=6&&time<12){vehicle.currentThrottle=1;vehicle.Speed_Kmh=Math.Min(60,(time-6)*12);}
                    else if(time>=12&&time<15){vehicle.currentThrottle=-1;vehicle.Speed_Kmh=Math.Max(0,60-(time-12)*20);vehicle.BrakesApplied=true;}
                    else if(time>=17&&time<18.2f){vehicle.currentThrottle=-1;vehicle.Speed_Kmh=-5;}
                }
                if(time>=seconds-.3f)vehicle.IsOccupied=false;
                engine.Update();
                for(int sample=0;sample<block;sample++)
                    samples[offset+sample]=Read(engine.Source,ref primaryPhase)+Read(engine.Detail,ref loadedPhase);
            }
        }
        finally{Time.deltaTime=priorDt;engine.Stop();}
        float peak=samples.Max(x=>Math.Abs(x));double rms=Math.Sqrt(samples.Average(x=>(double)x*x));
        if(!samples.All(float.IsFinite)||peak>=.6f)throw new InvalidOperationException("Preview exceeds finite/headroom bounds");
        using var stream=File.Open(path,FileMode.Create,FileAccess.Write);
        using var writer=new BinaryWriter(stream);
        // Standard PCM WAV: 44-byte RIFF header, mono 16-bit little-endian samples.
        writer.Write(Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+samples.Length*2);
        writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);
        writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);
        writer.Write(Encoding.ASCII.GetBytes("data"));writer.Write(samples.Length*2);
        foreach(float value in samples)writer.Write((short)Math.Round(value*32767));
        return new{file=Path.GetFileName(path),sampleRate=rate,channels=1,durationSeconds=seconds,peak,rms,
            scope="Production PCM/controller rendered with SDK fixture; native mixer/spatial/resampler QA pending"};
    }
    private static float Read(AudioSource source,ref double phase)
    {
        if(!source.isPlaying)return 0;
        float[] data=source.clip.Data;int index=(int)phase;float fraction=(float)(phase-index);
        float sample=data[index]*(1-fraction)+data[(index+1)%data.Length]*fraction;
        phase=(phase+source.pitch)%data.Length;return sample*source.volume;
    }
    internal static void WriteReport(string path,IEnumerable<object> reports)
        =>File.WriteAllText(path,JsonSerializer.Serialize(reports,new JsonSerializerOptions{WriteIndented=true})+"\n");
}
