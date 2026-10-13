using NugzzMenu.Services;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Vehicles;
using UnityEngine;

internal static class MotorcycleAudioBrakeChecks
{
    internal static void Run(Action<string,Action> check,Action<bool,string> require,Func<LandVehicle> chassis)
    {
        check("Embedded original engine banks match authored PCM hashes and remain bounded and DC-free",()=>
        {
            foreach(bool driving in new[]{false,true})
            {
                var data=MotorcycleEngineSamples.Samples(driving);
                using var stream=typeof(MotorcycleEngineSamples).Assembly.GetManifestResourceStream("NugzzMenu.Audio."+(driving?"VTwinLoad":"VTwinIdle")+".pcm");
                using var sha=System.Security.Cryptography.SHA256.Create();
                string hash=Convert.ToHexString(sha.ComputeHash(stream)).ToLowerInvariant();
                require(hash==(driving?"c7d9def6a8b9f1ee9cf49524e061ba8c56bc39574f2af22010f999e6dfe4a222":"bae09a8a9b946a037e2aa820f0f005213429cdc834e01f925e9c1ea4a8fa1e27"),"Embedded bank differs from authored PCM");
                double mean=data.Average(x=>(double)x),rms=Math.Sqrt(data.Average(x=>(double)x*x));
                require(data.Length==144000&&data.All(float.IsFinite)&&ReferenceEquals(data,MotorcycleEngineSamples.Samples(driving)),"Invalid or repeatedly decoded PCM");
                require(Math.Abs(mean)<.00001&&data.Max(x=>Math.Abs(x))<=.721f&&rms>.18&&rms<.24,"Bank clips, lacks body or has DC");
                var steps=data.Skip(1).Select((x,i)=>Math.Abs(x-data[i])).OrderBy(x=>x).ToArray();
                require(Math.Abs(data[0]-data[^1])<=steps[(int)(steps.Length*.999)],"Loop seam exceeds normal waveform slopes");
            }
        });
        check("Stopped occupied motorcycle stays at a deep 1000 RPM idle and a silent loaded layer",()=>
        {
            var v=chassis();require(MotorcycleService.Instance.Attach(v),"Attach failed");MotorcycleService.Instance.Ride(v);var engine=MotorcycleService.Instance.Bikes.Single().Engine;
            float min=10,max=-10;
            for(int i=0;i<250;i++){Time.unscaledTime+=.02f;engine.Update();min=Math.Min(min,engine.Source.pitch);max=Math.Max(max,engine.Source.pitch);}
            require(min>=.83f&&max<=.84f&&max-min<.001f&&engine.Source.isPlaying&&engine.Source.volume>=.49f&&engine.Source.volume<=.51f,"Idle revs rise or gain excessive/inaudible");
            require(engine.Detail.volume==0&&engine.Source.clip.Data.Length==144000&&engine.Source.PlayCalls==1,"Loaded revs leaked into idle or loop restarted");
        });
        check("Both original banks follow the same firing cadence and stay below combined gain limits",()=>
        {
            var idle=new AudioClip{name="Idle_Untuned_Engine_01"};Resources.Assets.Add(idle);var v=chassis();require(MotorcycleService.Instance.Attach(v),"Attach failed");
            MotorcycleService.Instance.Ride(v);var engine=MotorcycleService.Instance.Bikes.Single().Engine;for(int i=0;i<30;i++)engine.Update();
            v.Speed_Kmh=80;v.currentThrottle=1;for(int i=0;i<150;i++)engine.Update();
            require(engine.Detail.clip!=idle&&engine.Source.clip!=idle&&engine.Detail.clip.name=="Nugzz original V-twin load","Native/recorded audio used instead of original bank");
            require(engine.Source.pitch<=2.501f&&engine.Detail.pitch<=1.251f&&engine.Detail.volume>.5f&&engine.Source.volume+engine.Detail.volume<.70f,"Authored voice shifts/gains are excessive");
            require(Math.Abs(engine.Source.pitch*1200-engine.Detail.pitch*2400)<.001,"Banks drift to different firing RPM");
            var resources=typeof(MotorcycleEngineSamples).Assembly.GetManifestResourceNames();
            require(!resources.Any(n=>n.Contains("Harley")||n.Contains("ATTRIBUTION")),"Rejected recorded resources are still embedded");
            MotorcycleService.Instance.Detach(v);require(!idle.Destroyed,"Native game clip was destroyed");
        });
        check("Throttle/reverse rev smoothly and braking suppresses powered revs before returning to idle",()=>
        {
            var note=new MotorcycleEngineNote();for(int i=0;i<80;i++)note.Update(30,1,false,.02f);float power=note.Rpm;
            for(int i=0;i<80;i++)note.Update(30,-1,true,.02f);
            require(note.Rpm<power-500&&note.Load<.01,"Braking input still produces full-throttle sound");
            for(int i=0;i<150;i++)note.Update(0,0,false,.02f);require(Math.Abs(note.Rpm-1000)<2,"Stopped engine never returns to low idle");
            float old=note.Pitch;note.Update(-5,-1,false,.02f);require(note.Pitch>old&&note.Pitch-old<.1,"Reverse note jumps or is missing");
            for(int i=0;i<200;i++)note.Update(300,10,false,.02f);require(note.Pitch<=2.501&&note.Load<=1,"Engine note exceeds bounds");
        });
        check("Both audio layers fade and reuse voices on rapid re-entry; disposed voices cannot restart",()=>
        {
            var v=chassis();require(MotorcycleService.Instance.Attach(v),"Attach failed");MotorcycleService.Instance.Ride(v);var engine=MotorcycleService.Instance.Bikes.Single().Engine;
            v.currentThrottle=1;for(int i=0;i<80;i++)engine.Update();float primary=engine.Source.volume,detail=engine.Detail.volume;
            v.ExitVehicle();engine.Update();require(engine.Source.volume<primary&&engine.Detail.volume<detail&&engine.Source.isPlaying,"Exit does not fade both layers");
            MotorcycleService.Instance.Ride(v);engine.Update();require(engine.Source.PlayCalls==1&&engine.Detail.PlayCalls==1,"Re-entry restarts voices");
            var owned=engine.Source.clip;var driven=engine.Detail.clip;engine.Dispose();engine.Update();engine.Dispose();
            require(!engine.Source.isPlaying&&!engine.Detail.isPlaying&&owned.Destroyed&&driven.Destroyed&&engine.Source.clip==null&&engine.Detail.clip==null,"Disposed audio banks leaked or restarted");
        });
        check("Scene removal and destroyed vehicle cleanup release private PCM without touching native clips",()=>
        {
            var v=chassis();require(MotorcycleService.Instance.Attach(v),"Attach failed");var engine=MotorcycleService.Instance.Bikes.Single().Engine;
            var owned=engine.Source.clip;var driven=engine.Detail.clip;MotorcycleService.Instance.RemoveUnoccupied();
            require(owned.Destroyed&&driven.Destroyed,"Removal leaks audio banks");
            v=chassis();require(MotorcycleService.Instance.Attach(v),"Reattach failed");engine=MotorcycleService.Instance.Bikes.Single().Engine;owned=engine.Source.clip;
            MotorcycleService.Instance.Bikes.Single().Visual=null;MotorcycleService.Instance.Update();require(owned.Destroyed&&MotorcycleService.Instance.Count==0,"Expired vehicle leaks PCM");
        });
        check("PCM creation/upload failures roll back conversion without mute or private clip leaks",()=>
        {
            var v=chassis();AudioClip.FailCreate=true;require(!MotorcycleService.Instance.Attach(v)&&v.vehicleName=="Sedan", "Failed creation left a converted vehicle");AudioClip.FailCreate=false;
            AudioClip.FailSetData=true;require(!MotorcycleService.Instance.Attach(v)&&MotorcycleService.Instance.Count==0&&v.GetComponent<MeshRenderer>().enabled,"Failed upload left a broken bike");AudioClip.FailSetData=false;
            AudioClip.UploadCalls=0;AudioClip.FailSetDataAt=2;AudioClip.Created.Clear();
            require(!MotorcycleService.Instance.Attach(v)&&MotorcycleService.Instance.Count==0&&AudioClip.Created.Count==2&&AudioClip.Created.All(c=>c.Destroyed),"Second bank upload failure leaked one of the private clips");
            AudioClip.FailSetDataAt=0;
        });
        check("Original idle PCM has two strong unevenly spaced exhaust events rather than a continuous drone",()=>
        {
            var data=MotorcycleEngineSamples.Samples(false);
            double Energy(double start,double end)=>data.Skip((int)(start*24000)).Take((int)((end-start)*24000)).Average(x=>(double)x*x);
            require(Energy(.002,.017)>Energy(.032,.042)*1.5&&Energy(.046,.061)>Energy(.086,.096)*1.5,"315/405 pulse pair is not pronounced in PCM");
            var note=new MotorcycleEngineNote();for(int i=0;i<120;i++)note.Update(60,1,false,.02f);
            require(Math.Abs(note.Pitch*1200-note.LoadedPitch*2400)<.001&&note.Rpm<=3000,"Both banks do not share one bounded engine clock");
        });
        check("Stronger stopping keeps native power and controls and restores all brake tuning",()=>
        {
            var v=chassis();float mass=v.Rb.mass,gearing=v.diffGearing,brake=v.BrakeForceMultiplier,hand=v.handBrakeForce;var wheel=v.steerWheels[0];var spring=wheel.suspensionSpring;
            require(MotorcycleService.Instance.Attach(v),"Attach failed");
            float gain=v.BrakeForceMultiplier/(brake*v.Rb.mass/mass);require(gain>=2&&gain<=3,"Regular stopping not strengthened");
            require(v.diffGearing==gearing*v.Rb.mass/mass&&!v.overrideControls&&wheel.suspensionSpring.spring==10000f,"Braking changed torque/input/suspension contract");
            MotorcycleService.Instance.Detach(v);require(v.BrakeForceMultiplier==brake&&v.handBrakeForce==hand&&v.Rb.mass==mass&&wheel.suspensionSpring.Equals(spring),"Detach retained braking changes");
        });
    }
}
