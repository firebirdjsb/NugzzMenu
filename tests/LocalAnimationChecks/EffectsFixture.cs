// These fixtures verify lifetime, routing, rollback and source isolation only.
// They do not emulate native particle emission, mixing or Unity render culling.
namespace UnityEngine
{
    public struct Color { public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;} }
    public class ParticleSystem : Component { public bool Playing;public void Play(bool children)=>Playing=true; }
    public class ParticleSystemRenderer : Renderer { }
    public class AudioClip : Object
    {
        public static bool FailCreate,FailSetData;
        public static int FailSetDataAt,UploadCalls;
        public static List<AudioClip> Created=new();
        public float[] Data; public int frequency,channels;
        public static AudioClip Construct_Internal()
        {if(FailCreate)return null;var clip=new AudioClip();Created.Add(clip);return clip;}
        public void CreateUserSound(string label,int length,int count,int rate,bool stream)
        {if(stream||count!=1||length<=0||rate<=0)throw new InvalidOperationException("Invalid PCM initialization");name=label;Data=new float[length];channels=count;frequency=rate;}
        public static AudioClip Create(string name,int length,int channels,int frequency,bool stream)
        {
            if(stream||channels!=1||length<=0||frequency<=0)throw new InvalidOperationException("Invalid PCM clip creation");
            return FailCreate?null:new(){name=name,Data=new float[length],channels=channels,frequency=frequency};
        }
        public bool SetData(Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<float> data,int offset)
        {UploadCalls++;if(FailSetData||UploadCalls==FailSetDataAt)return false;if(offset!=0||data.Count!=Data.Length)throw new InvalidOperationException("PCM buffer length mismatch");Data=data.ToArray();return true;}
    }
    public class AudioLowPassFilter : Component { public float cutoffFrequency; }
    public enum AudioRolloffMode { Logarithmic }
    public class AudioSource : Component
    {
        public AudioClip clip;public object outputAudioMixerGroup;public float volume=1,pitch=1,spatialBlend=1,minDistance=1,maxDistance=30;
        public AudioRolloffMode rolloffMode;public float dopplerLevel;public bool loop,playOnAwake,Playing,mute;public bool isPlaying=>Playing;
        public int PlayCalls,StopCalls;
        public void Play(){Playing=true;PlayCalls++;}
        public void Stop(){Playing=false;StopCalls++;}
    }
}
namespace UnityEngine.VFX
{
    public class VFXRenderer : UnityEngine.Renderer { }
    public class VisualEffect : UnityEngine.Component
    {
        public int Rate;public bool Playing;public bool HasInt(string name)=>name=="EmissionRate";
        public void SetInt(string name,int value)=>Rate=value;public void Play()=>Playing=true;
    }
}
namespace Il2CppScheduleOne.Audio
{ public class AudioSourceController : UnityEngine.Component { public UnityEngine.AudioSource _audioSource;public float _defaultBaseVolume=1,_defaultBasePitch=1,VolumeMultiplier=1,PitchMultiplier=1; } }
namespace Il2CppScheduleOne.NPCs
{ public class NPC : UnityEngine.Component { public Il2CppScheduleOne.PlayerScripts.Avatar Avatar; } }
namespace Il2CppScheduleOne.NPCs.Other
{ public class NPCAuxAction : UnityEngine.Component { public string _animBoolName;public UnityEngine.GameObject _auxEffect;public Il2CppScheduleOne.NPCs.NPC _npc; } }
namespace Il2CppScheduleOne.Storage
{
    public class StorageEntity : UnityEngine.Component { }
    public class StorageEntityInteractable : Il2CppScheduleOne.Interaction.InteractableObject { }
}
namespace Il2CppScheduleOne.Core.Avatar
{ public class NakedAppearance { public UnityEngine.Color SkinColor; } }
namespace Il2CppScheduleOne.Avatar.Player
{ public class PlayerAppearance { public UnityEngine.Color SkinColor; } }
namespace Il2CppScheduleOne.Avatar
{
    public class AvatarAppearance : UnityEngine.Component
    {
        public Il2CppScheduleOne.Core.Avatar.NakedAppearance AppliedNakedAppearance;
        public UnityEngine.Color Rendered;public void SetSkinColor(UnityEngine.Color color)=>Rendered=color;
    }
}
namespace Il2CppScheduleOne.Tools
{
    public class ColorSmoother
    {
        public UnityEngine.Color _CurrentValue_k__BackingField,Default;
        public Dictionary<string,UnityEngine.Color> Overrides=new();public void RemoveOverride(string name)=>Overrides.Remove(name);
    }
}
namespace Il2CppScheduleOne.Effects
{
    public class Effect : UnityEngine.Object
    {
        public string ID,Name;public Action<Il2CppScheduleOne.PlayerScripts.Player> Cleanup;
        public void ApplyToPlayer(Il2CppScheduleOne.PlayerScripts.Player p){ }
        public void ClearFromPlayer(Il2CppScheduleOne.PlayerScripts.Player p)=>Cleanup?.Invoke(p);
        public FakeType GetIl2CppType()=>new(){Name=Name};
        public class FakeType { public string Name; }
    }
}
namespace NugzzMenu.Services
{ public class PlayerCheatService { public static readonly PlayerCheatService Instance=new();public void ForceKillLocalPlayer(){ } } }
namespace Il2CppScheduleOne.AvatarFramework
{
    public class AvatarEffects : UnityEngine.Component
    {
        public UnityEngine.Transform HeadBone;public Il2CppScheduleOne.PlayerScripts.Avatar Avatar;
        public Il2CppScheduleOne.Tools.ColorSmoother SkinColorSmoother=new();
        public AvatarEffects[] MirrorEffectsTo;
        public void SetDefaultSkinColor(UnityEngine.Color color,bool mirror)=>SkinColorSmoother.Default=color;
    }
}
