using System.Reflection;
using NugzzMenu.Services;
using NugzzMenu.UI;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Core.Equipping.Framework;
using Il2CppScheduleOne.Equipping.Framework;
using Il2CppScheduleOne.SpecialCustomers.Misc;
using Il2CppScheduleOne.Vehicles;
using UnityEngine;

int passed=0,failed=0;
void Require(bool condition,string reason) { if(!condition) throw new InvalidOperationException(reason); }
GameObject Model(string name)
{
    var go=new GameObject(name);go.AddComponent<MeshFilter>().sharedMesh=new object();go.AddComponent<MeshRenderer>();return go;
}
Player MakePlayer(int id,bool local)
{
    var go=new GameObject("Player "+id);
    var p=go.AddComponent<Player>();p.IsLocalPlayer=local;p.IsOwner=local;p.Owner=new(){ClientId=id};
    var avatar=new GameObject("Avatar");avatar.transform.SetParent(go.transform,false);
    var animator=avatar.AddComponent<Animator>();
    animator.parameters=LocalAnimationCatalog.Actions.SelectMany(a=>a.EnabledBools.Concat(a.DisabledBools)).Distinct().Select(s=>new AnimatorControllerParameter{name=s,type=AnimatorControllerParameterType.Bool}).ToArray();
    var rig=avatar.AddComponent<Il2CppScheduleOne.AvatarFramework.Animation.AvatarAnimation>();rig._animator=animator;
    rig._bones=new(LocalBodyAnimationService.JointNames.Length);
    for(int i=0;i<rig._bones.Length;i++)
    {
        var joint=new GameObject("mixamorig:"+LocalBodyAnimationService.JointNames[i]).transform;
        joint.SetParent(avatar.transform,false);joint.localPosition=new(0,i*0.01f,0);
        joint.localRotation=Quaternion.AngleAxis(i*3,Vector3.up);rig._bones[i]=joint;
    }
    p.Avatar=avatar.AddComponent<Avatar>();p.Avatar.Animation=rig;p.Avatar.HipBone=rig._bones[0];
    p.Avatar.Effects=avatar.AddComponent<Il2CppScheduleOne.AvatarFramework.AvatarEffects>();p.Avatar.Effects.Avatar=p.Avatar;
    var head=new GameObject("Head").transform;head.SetParent(avatar.transform,false);head.localPosition=new(0,1.7f,0);p.Avatar.Effects.HeadBone=head;
    p.Avatar.Appearance=avatar.AddComponent<Il2CppScheduleOne.Avatar.AvatarAppearance>();
    p.CurrentAppearance=new(){SkinColor=new Color(.42f,.23f,.12f)};
    p.Avatar.Appearance.AppliedNakedAppearance=new(){SkinColor=p.CurrentAppearance.SkinColor};
    go.AddComponent<PlayerNetworkedEquipper>();Player.PlayerList.Add(p);return p;
}
void Reset()
{
    MotorcycleService.Instance.ResetForScene();LocalAnimationRenderService.Shutdown();Physics.Hits=null;Physics.Blocked=false;Physics.Raycasts=0;GameplayStateGateService.Instance.Blocked=false;Il2CppScheduleOne.Interaction.InteractionManager.Instance=null;
    LocalAnimationService.Instance.ResetForScene();Time.unscaledTime+=30;
    Player.PlayerList.Clear();Player.Local=MakePlayer(0,true);
    SessionAuthorityService.Instance.Host=true;SessionAuthorityService.Instance.FeaturesAllowed=true;
    NativeActivityGateService.Instance.Blocked=false;PlayerValueRpcService.Broadcasts.Clear();GUIFit.Buttons.Clear();Animation.FailPlay=false;
    PlayerValueRpcService.TargetMessages.Clear();
    AudioClip.FailCreate=AudioClip.FailSetData=false;
    AudioClip.FailSetDataAt=AudioClip.UploadCalls=0;AudioClip.Created.Clear();
    Resources.Assets.Clear();Resources.Assets.Add(new EquippableData{name="PhoneEquippable"});
    Resources.Assets.Add(Model("Laptop"));
    var bike=Model("Motorbike_Chopper");var burnout=bike.AddComponent<MotorbikeBurnout>();
    var model=new GameObject("Chopper model");model.transform.SetParent(bike.transform,false);model.transform.localRotation=Quaternion.Euler(0,180,0);
    var frame=Model("Chopper_Frame01_LOD0");frame.transform.SetParent(model.transform,false);
    var wheel=Model("RearWheel");wheel.transform.SetParent(model.transform,false);wheel.transform.localPosition=new(0,0.35f,-1f);burnout.rearWheelVisual=wheel.transform;
    var bars=new GameObject("Bars");bars.transform.SetParent(model.transform,false);
    bars.transform.localPosition=new(0,.88f,.5f);bars.transform.localRotation=Quaternion.Euler(-35,0,0);
    var pivot=new GameObject("Pivot");pivot.transform.SetParent(bars.transform,false);
    var handlebars=Model("Chopper_Handlebars01_LOD0");handlebars.transform.SetParent(pivot.transform,false);handlebars.transform.localPosition=new(0,-.67505264f,.04004839f);
    var frontWheel=Model("Wheel_Front");frontWheel.transform.SetParent(pivot.transform,false);frontWheel.transform.position=model.transform.TransformPoint(new(0,0.354f,0.923f));
    var native=bike.AddComponent<Motorbike>();native.BurnoutController=burnout;
    var seat=new GameObject("Seat");seat.transform.SetParent(bike.transform,false);seat.transform.localPosition=new(0,0.731f,0.322f);seat.transform.localRotation=Quaternion.Euler(0,180,0);
    native.Seat=seat.AddComponent<Il2CppScheduleOne.AvatarFramework.Animation.AvatarSeat>();native.Seat.SittingPoint=seat.transform;
    var front=new GameObject("FrontContact").transform;front.SetParent(bike.transform,false);front.localPosition=new(0,0.354f,-0.923f);burnout.frontWheelContact=front;
    Resources.Assets.Add(native);
    var smoke=new GameObject("BurnoutFX");smoke.transform.SetParent(bike.transform,false);
    burnout.burnoutEffect=smoke.AddComponent<UnityEngine.VFX.VisualEffect>();smoke.AddComponent<UnityEngine.VFX.VFXRenderer>();
    burnout._burnoutAudio=bike.AddComponent<Il2CppScheduleOne.Audio.AudioSourceController>();
    burnout._burnoutAudio._audioSource=bike.AddComponent<AudioSource>();burnout._burnoutAudio._audioSource.clip=new AudioClip();
    var npc=new GameObject("Source NPC").AddComponent<Il2CppScheduleOne.NPCs.NPC>();
    npc.Avatar=new GameObject("Source avatar").AddComponent<Avatar>();
    npc.Avatar.AttachmentAnchorProvider=new(){Hips=Player.Local.Avatar.HipBone};npc.Avatar.Effects=Player.Local.Avatar.Effects;
    foreach(var name in new[]{"IsPissing","IsVomiting"})
    {
        var action=new GameObject(name).AddComponent<Il2CppScheduleOne.NPCs.Other.NPCAuxAction>();
        action._animBoolName=name;action._npc=npc;action._auxEffect=new GameObject("Native "+name);
        action._auxEffect.transform.SetParent(name=="IsPissing"?npc.Avatar.AttachmentAnchorProvider.Hips:npc.Avatar.Effects.HeadBone,false);
        action._auxEffect.transform.localPosition=new(0,0,.08f);
        action._auxEffect.AddComponent<ParticleSystem>();action._auxEffect.AddComponent<ParticleSystemRenderer>();
        action._auxEffect.AddComponent<AudioSource>().clip=new AudioClip();Resources.Assets.Add(action);
    }
    var chair=Model("Chair_Business_Sittable1");var chairSeat=chair.AddComponent<Il2CppScheduleOne.AvatarFramework.Animation.AvatarSeat>();
    var sit=new GameObject("SitPoint").transform;sit.SetParent(chair.transform,false);sit.localPosition=new(0,0.475f,0);chairSeat.SittingPoint=sit;Resources.Assets.Add(chairSeat);
}
void Check(string name,Action action)
{
    try { Reset();action();passed++;Console.WriteLine("PASS: "+name); }
    catch(Exception ex) { failed++;Console.WriteLine("FAIL: "+name+" - "+ex.Message); }
}
Dictionary<int,object> Visuals()
{
    var map=(System.Collections.IDictionary)typeof(LocalAnimationVisualService).GetField("_visuals",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(LocalAnimationVisualService.Instance);
    return map.Keys.Cast<int>().ToDictionary(k=>k,k=>map[k]);
}
GameObject Root(object visual)=>(GameObject)visual.GetType().GetField("Root",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(visual);
bool RequestHook(Player p,string trigger)=>(bool)typeof(LocalAnimationRequestPatch).GetMethod("Prefix",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{p,trigger});

Check("All eight actions start and stop on an initialized host",()=>
{
    for(int action=0;action<8;action++)
    {
        Time.unscaledTime+=1;Require(LocalAnimationService.Instance.Play(action),"Action failed: "+action);
        foreach(string name in LocalAnimationCatalog.Actions[action].EnabledBools)Require(Player.Local.Avatar.Animation._animator.GetBool(name),name+" not enabled");
        LocalAnimationService.Instance.Stop(false);
        Require(LocalAnimationService.Instance.ActiveAction==-1&&Visuals().Count==0,"Action did not clean up");
    }
});
Check("All eight actions queue owner requests on an approved client",()=>
{
    Player.Local.Owner.ClientId=1;Player.Local.IsServerInitialized=false;SessionAuthorityService.Instance.Host=false;
    LocalAnimationNetworkService.Instance.TryReceiveNetworkValue(MakePlayer(0,false),LocalAnimationNetworkService.StateVariable,"body12-ready");
    for(int action=0;action<8;action++)
    {
        Time.unscaledTime+=1;Require(LocalAnimationService.Instance.Play(action),"Client action failed");
        Require(Player.Local.Requests.Last()==LocalAnimationNetworkService.RequestPrefix+(action+1),"Wrong owner request");
        LocalAnimationService.Instance.Stop(false);
        Require(Player.Local.Requests.Last().EndsWith("/0"),"Client did not request cleanup");
    }
    Require(PlayerValueRpcService.Broadcasts.Count==0,"Client tried to broadcast observers");
});
Check("Foreign player ownership and uninitialized player cannot send animations",()=>
{
    Player.Local.IsOwner=false;Require(!LocalAnimationService.Instance.Play(3),"Foreign ownership accepted");
    Player.Local.IsOwner=true;Player.Local.IsClientInitialized=false;Require(!LocalAnimationService.Instance.Play(3),"Uninitialized player accepted");
    Require(Player.Local.Bools.Count==0&&Player.Local.Requests.Count==0,"Invalid player emitted RPC");
});
Check("Approval denial and native activities leave the avatar untouched",()=>
{
    SessionAuthorityService.Instance.FeaturesAllowed=false;Require(!LocalAnimationService.Instance.Play(4),"Approval bypass");
    SessionAuthorityService.Instance.FeaturesAllowed=true;NativeActivityGateService.Instance.Blocked=true;
    Require(!LocalAnimationService.Instance.Play(2)&&Player.Local.Bools.Count==0,"Native input ownership bypass");
});
Check("Missing or wrong-type animator parameter fails without Unity log spam",()=>
{
    Player.Local.Avatar.Animation._animator.parameters=new[]{new AnimatorControllerParameter{name="IsDancing1",type=AnimatorControllerParameterType.Trigger}};
    Require(!LocalAnimationService.Instance.Play(3)&&Player.Local.Bools.Count==0,"Sent an invalid bool");
});
Check("Switching poses restores owned parameters and preserves unrelated native state",()=>
{
    var animator=Player.Local.Avatar.Animation._animator;animator.SetBool("IsDancing2",true);animator.SetBool("HandsUp",true);
    Require(LocalAnimationService.Instance.Play(3),"Dance failed");Require(!animator.GetBool("IsDancing2"),"Conflicting dance remained");
    Time.unscaledTime+=1;Require(LocalAnimationService.Instance.Play(5),"Switch failed");
    Require(animator.GetBool("IsDancing2")&&!animator.GetBool("IsDancing1")&&animator.GetBool("HandsUp"),"Prior native state was lost");
    LocalAnimationService.Instance.Stop(false);Require(!animator.GetBool("IsVomiting")&&animator.GetBool("HandsUp"),"Stop cleared unrelated state");
});
Check("Phone cleanup unequips only the temporary networked handler",()=>
{
    var original=new PhoneHandler();Player.Local.Equipped=original;
    Require(LocalAnimationService.Instance.Play(0),"Phone failed");var temporary=Player.Local.Equipped;
    LocalAnimationService.Instance.Stop(false);
    Require(Player.Local.Removed.SequenceEqual(new[]{temporary})&&!Player.Local.Removed.Contains(original),"Inventory item was unequipped");
});
Check("Occupied hand is not displaced to equip a phone",()=>
{
    Player.Local.GetComponent<PlayerNetworkedEquipper>().CanEquipNow=false;
    Require(!LocalAnimationService.Instance.Play(0)&&Player.Local.Removed.Count==0&&Player.Local.Requests.Count==0,"Hand was displaced");
});
Check("Movement, timeout, ragdoll, death and vehicle entry stop a pose",()=>
{
    foreach(string cause in new[]{"movement","timeout","ragdoll","death","vehicle","approval"})
    {
        Time.unscaledTime+=1;Require(LocalAnimationService.Instance.Play(3),"Start failed");
        switch(cause){case "movement":Player.Local.Velocity=new(1,0,0);break;case "timeout":Time.unscaledTime+=21;break;case "ragdoll":Player.Local.IsRagdolled=true;break;case "death":Player.Local.Health.IsAlive=false;break;case "vehicle":Player.Local.CurrentVehicle=new();break;case "approval":SessionAuthorityService.Instance.FeaturesAllowed=false;break;}
        LocalAnimationService.Instance.Update();Require(LocalAnimationService.Instance.ActiveAction==-1&&!Player.Local.Avatar.Animation._animator.GetBool("IsDancing1"),cause+" did not stop");
        Player.Local.Velocity=new();Player.Local.IsRagdolled=false;Player.Local.Health.IsAlive=true;Player.Local.CurrentVehicle=null;SessionAuthorityService.Instance.FeaturesAllowed=true;
    }
});
Check("Scene reset destroys all temporary visuals and restores body parameters",()=>
{
    Require(LocalAnimationService.Instance.Play(7),"Bike failed");var root=Root(Visuals().Values.Single());
    LocalAnimationService.Instance.ResetForScene();Require(root.Destroyed&&Visuals().Count==0&&!Player.Local.Avatar.Animation._animator.GetBool("IsMotorbikeSitting"),"Scene leaked visual or pose");
});
Check("Custom body animation moves the player skeleton without spawning a tent or changing the root",()=>
{
    var hips=Player.Local.Avatar.Animation._bones[0];var original=hips.localPosition;
    Require(LocalAnimationService.Instance.Play(6),"Custom body failed");Time.unscaledTime+=0.3125f;
    LocalBodyAnimationService.Instance.LateUpdate();
    Require(hips.localPosition.z>original.z&&Player.Local.transform.position.sqrMagnitude==0,"Body did not move or root moved");
    Require(Visuals().Count==0&&LocalBodyAnimationService.Instance.ActiveCount==1&&Player.Local.Bools.Count==0,"Spawned a tent or invented a native parameter");
    LocalAnimationService.Instance.Stop(false);Require(hips.localPosition.Equals(original)&&LocalBodyAnimationService.Instance.ActiveCount==0,"Stop did not restore skeleton");
});
Check("Motorcycle visual spins its copied wheel without vehicle behaviours",()=>
{
    Require(LocalAnimationService.Instance.Play(7),"Bike failed");var root=Root(Visuals().Values.Single());
    LocalAnimationService.Instance.Update();Require(root.GetComponentsInChildren<MotorbikeBurnout>(true).Length==0,"Copied physics controller");
    Require(root.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="RearWheel").Rotated>0,"Wheel animation did not advance");
});
Check("Failed native visual and RPC creation unwind partial poses",()=>
{
    var bones=Player.Local.Avatar.Animation._bones;Player.Local.Avatar.Animation._bones=null;
    Require(!LocalAnimationService.Instance.Play(6)&&LocalBodyAnimationService.Instance.ActiveCount==0,"Invalid skeleton accepted");
    Player.Local.Avatar.Animation._bones=bones;Time.unscaledTime+=1;Player.Local.ThrowBool="IsTyping";
    Require(!LocalAnimationService.Instance.Play(1)&&!Player.Local.Avatar.Animation._animator.GetBool("Sitting")&&Visuals().Count==0,"Partial laptop pose persisted");
});
Check("Native request hook filters only reserved messages and rejects malformed payloads",()=>
{
    var remote=MakePlayer(1,false);
    Require(RequestHook(remote,"ThumbsUp"),"Normal native trigger was intercepted");
    foreach(string payload in new[]{"", "99", "-1", "invalid"})
        Require(!RequestHook(remote,LocalAnimationNetworkService.RequestPrefix+payload),"Reserved trigger leaked to Animator");
    Require(PlayerValueRpcService.Broadcasts.Count==0,"Malformed payload accepted");
    Require(!RequestHook(remote,LocalAnimationNetworkService.RequestPrefix+"body12"),"Readiness ack leaked");
    Require(!RequestHook(remote,LocalAnimationNetworkService.RequestPrefix+"7")&&LocalBodyAnimationService.Instance.ActiveCount==1,"Approved remote body request failed");
});
Check("Host refuses denied clients and permits their own cleanup after revocation",()=>
{
    var remote=MakePlayer(1,false);remote.Approved=false;
    LocalAnimationNetworkService.Instance.HandleHostRequest(remote,6);Require(LocalBodyAnimationService.Instance.ActiveCount==0&&PlayerValueRpcService.Broadcasts.Count==0,"Denied client accepted");
    remote.Approved=true;LocalAnimationNetworkService.Instance.HandleHostRequest(remote,-3);
    LocalAnimationNetworkService.Instance.HandleHostRequest(remote,6);remote.Approved=false;
    LocalAnimationNetworkService.Instance.HandleHostRequest(remote,-1);Require(LocalBodyAnimationService.Instance.ActiveCount==0&&PlayerValueRpcService.Broadcasts.Last()=="1|0|0","Revoked client could not clean up");
});
Check("Client observer messages require the known host and cannot overwrite its own action",()=>
{
    Player.Local.Owner.ClientId=2;SessionAuthorityService.Instance.Host=false;
    var host=MakePlayer(0,false);var actor=MakePlayer(1,false);var stranger=MakePlayer(3,false);
    var net=LocalAnimationNetworkService.Instance;
    net.TryReceiveNetworkValue(host,LocalAnimationNetworkService.StateVariable,"body12-ready");
    net.TryReceiveNetworkValue(stranger,LocalAnimationNetworkService.StateVariable,"1|7|20|0");Require(LocalBodyAnimationService.Instance.ActiveCount==0,"Forged host accepted");
    net.TryReceiveNetworkValue(host,LocalAnimationNetworkService.StateVariable,"1|7|20|0");Require(LocalBodyAnimationService.Instance.ActiveCount==1,"Host actor state not applied");
    net.TryReceiveNetworkValue(host,LocalAnimationNetworkService.StateVariable,"2|7|20|0");Require(LocalBodyAnimationService.Instance.ActiveCount==1,"Host state overwrote local action");
    net.TryReceiveNetworkValue(host,LocalAnimationNetworkService.StateVariable,"1|0|0");Require(LocalBodyAnimationService.Instance.ActiveCount==0,"Host stop did not clean remote effect");
});
Check("Network state parser rejects invalid actors/actions/durations and oversized data",()=>
{
    foreach(string bad in new[]{"-1|7|20","1|9|20","1|7|21","1|7|0","1|0|20","1|7|-1","1|7|20|extra","1|7|20","1|7|20|-1","1|7|20|20000","1|7|20|5000",new string('9',100)})
        Require(!LocalAnimationNetworkService.TryParseState(bad,out _,out _,out _),"Accepted "+bad);
});
Check("Host expiry cleans up a remote visual without repeated start messages",()=>
{
    var remote=MakePlayer(1,false);LocalAnimationNetworkService.Instance.HandleHostRequest(remote,7);
    LocalAnimationNetworkService.Instance.HandleHostRequest(remote,7);Require(PlayerValueRpcService.Broadcasts.Count==1,"Duplicate started effect repeatedly");
    Time.unscaledTime+=21;LocalAnimationNetworkService.Instance.Update();Require(Visuals().Count==0&&PlayerValueRpcService.Broadcasts.Contains("1|0|0"),"Remote expiry leaked");
});
Check("Newly approved clients receive the remaining effect and native pose once",()=>
{
    var actor=MakePlayer(1,false);var observer=MakePlayer(2,false);observer.Approved=false;
    LocalAnimationNetworkService.Instance.HandleHostRequest(actor,7);
    Require(PlayerValueRpcService.Broadcasts.Count==1&&actor.ObserverBools.Count==2,"Initial observer snapshot failed");
    observer.Approved=true;Time.unscaledTime+=3;LocalAnimationNetworkService.Instance.Update();
    Require(PlayerValueRpcService.Broadcasts.Last()=="1|8|17"&&actor.ObserverBools.Count==4,"Late approval snapshot failed");
    Time.unscaledTime+=3;LocalAnimationNetworkService.Instance.Update();
    Require(PlayerValueRpcService.Broadcasts.Count(s=>s!="body12-ready")==2&&actor.ObserverBools.Count==4,"Snapshot repeated for existing observers");
    observer.Approved=false;Time.unscaledTime+=2;LocalAnimationNetworkService.Instance.Update();
    observer.Approved=true;Time.unscaledTime+=2;LocalAnimationNetworkService.Instance.Update();
    Require(PlayerValueRpcService.Broadcasts.Last()=="1|8|10"&&PlayerValueRpcService.Broadcasts.Count(s=>s!="body12-ready")==3,"Reapproved observer did not regain the effect");
});
Check("Local Effects layout exposes all eight actions and Stop inside its panel",()=>
{
    float y=100;var style=new GUIStyle();LocalAnimationRenderer.Draw(ref y,700,style,style,style);
    Require(GUIFit.Buttons.Count==9&&y==280,"Wrong action count or scroll height");
    foreach(var button in GUIFit.Buttons)Require(button.Rect.x>=0&&button.Rect.x+button.Rect.width<=700&&button.Rect.y>=120&&button.Rect.y+button.Rect.height<=276,"Clipped button: "+button.Text);
});
Check("Body samples are periodic, bounded and fade smoothly without frame-rate accumulation",()=>
{
    LocalBodyAnimationService.Sample(0,20,out _,out _,out float start);Require(start==0,"Entry did not fade");
    LocalBodyAnimationService.Sample(10,0,out _,out _,out float end);Require(end==0,"Exit did not fade");
    LocalBodyAnimationService.Sample(0.1f,20,out _,out _,out float half);Require(Math.Abs(half-0.5f)<0.00001f,"Fade is not smoothstep");
    LocalBodyAnimationService.Sample(1.3125f,18,out float a,out float b,out _);
    LocalBodyAnimationService.Sample(2.5625f,17,out float c,out float d,out _);
    Require(Math.Abs(a-c)<0.00001f&&Math.Abs(b-d)<0.00001f,"Loop phase drifted");
    for(int i=0;i<2000;i++) { LocalBodyAnimationService.Sample(i/100f,20-i/100f,out float wave,out float bounce,out float weight);
        Require(Math.Abs(wave)<=1&&bounce>=0&&bounce<=1&&weight>=0&&weight<=1,"Unbounded sample"); }
});
Check("Native LateUpdate and fallback apply the same body pose without accumulating offsets",()=>
{
    Require(LocalAnimationService.Instance.Play(6),"Body start failed");Time.unscaledTime+=0.3125f;
    var rig=Player.Local.Avatar.Animation;LocalBodyAnimationService.Instance.LateUpdate();
    var positions=rig._bones.Select(b=>b.localPosition).ToArray();var rotations=rig._bones.Select(b=>b.localRotation).ToArray();
    for(int i=0;i<120;i++) { LocalBodyAnimationService.Instance.Apply(rig);LocalBodyAnimationService.Instance.LateUpdate(); }
    for(int i=0;i<rig._bones.Length;i++)Require(rig._bones[i].localPosition.Equals(positions[i])&&rig._bones[i].localRotation.Equals(rotations[i]),"Repeated overlay drifted");
    var unrelated=MakePlayer(3,false).Avatar.Animation;var native=unrelated._bones[0].localPosition;
    LocalBodyAnimationService.Instance.Apply(unrelated);Require(unrelated._bones[0].localPosition.Equals(native),"Unselected avatar was animated");
});
Check("Body reset restores all owned joints and leaves root/controller/scale intact",()=>
{
    var rig=Player.Local.Avatar.Animation;var controller=rig._animator.runtimeAnimatorController;
    var positions=rig._bones.Select(b=>b.localPosition).ToArray();var rotations=rig._bones.Select(b=>b.localRotation).ToArray();
    Require(LocalAnimationService.Instance.Play(6),"Body start failed");Time.unscaledTime+=0.3125f;LocalBodyAnimationService.Instance.LateUpdate();
    LocalAnimationService.Instance.ResetForScene();Require(LocalBodyAnimationService.Instance.ActiveCount==0,"Body leaked on scene reset");
    for(int i=0;i<rig._bones.Length;i++)Require(rig._bones[i].localPosition.Equals(positions[i])&&rig._bones[i].localRotation.Equals(rotations[i])&&rig._bones[i].localScale.Equals(Vector3.one),"Joint did not restore");
    Require(ReferenceEquals(controller,rig._animator.runtimeAnimatorController)&&Player.Local.transform.position.sqrMagnitude==0,"Changed native root/controller");
});
Check("Ragdoll and replaced rigs retain native transform ownership when the emote ends",()=>
{
    Require(LocalAnimationService.Instance.Play(6),"Body start failed");Time.unscaledTime+=0.3125f;LocalBodyAnimationService.Instance.LateUpdate();
    var hips=Player.Local.Avatar.Animation._bones[0];hips.localPosition=new(5,6,7);Player.Local.IsRagdolled=true;
    LocalAnimationService.Instance.Update();Require(hips.localPosition.Equals(new Vector3(5,6,7))&&LocalBodyAnimationService.Instance.ActiveCount==0,"Ragdoll was snapped to an old pose");
    Player.Local.IsRagdolled=false;Time.unscaledTime+=1;Require(LocalAnimationService.Instance.Play(6),"Restart failed");
    hips.localPosition=new(8,9,10);Player.Local.Avatar.Animation=MakePlayer(3,false).Avatar.Animation;
    LocalBodyAnimationService.Instance.Update();Require(LocalBodyAnimationService.Instance.ActiveCount==0&&hips.localPosition.Equals(new Vector3(8,9,10)),"Replaced rig was modified");
});
Check("Legacy hosts and spoofed readiness cannot enable the custom body request",()=>
{
    Player.Local.Owner.ClientId=1;SessionAuthorityService.Instance.Host=false;var stranger=MakePlayer(3,false);
    Require(!LocalAnimationService.Instance.Play(6),"Legacy host allowed body");
    LocalAnimationNetworkService.Instance.TryReceiveNetworkValue(stranger,LocalAnimationNetworkService.StateVariable,"body12-ready");
    Require(!LocalAnimationService.Instance.Play(6)&&Player.Local.Requests.Count==0,"Forged readiness accepted");
    var host=MakePlayer(0,false);LocalAnimationNetworkService.Instance.TryReceiveNetworkValue(host,LocalAnimationNetworkService.StateVariable,"body12-ready");
    Require(Player.Local.Requests.Single().EndsWith("/body12")&&LocalAnimationService.Instance.Play(6),"Trusted readiness/ack did not enable action");
});
Check("Host custom-body states require matching peers and include elapsed loop phase for new observers",()=>
{
    var actor=MakePlayer(1,false);var observer=MakePlayer(2,false);var net=LocalAnimationNetworkService.Instance;
    net.HandleHostRequest(actor,6);Require(LocalBodyAnimationService.Instance.ActiveCount==0,"Legacy tent request was mistaken for body");
    net.HandleHostRequest(actor,-3);net.HandleHostRequest(actor,6);
    Require(PlayerValueRpcService.TargetMessages.Count==1&&PlayerValueRpcService.TargetMessages[0]==(1,"1|7|20|0"),"Old observer got custom-body state");
    Time.unscaledTime+=3.456f;net.HandleHostRequest(observer,-3);net.Update();
    var packet=PlayerValueRpcService.TargetMessages.Last();
    Require(packet.Recipient==2&&LocalAnimationNetworkService.TryParseState(packet.Value,out int id,out int action,out int seconds,out int elapsed)&&id==1&&action==6&&seconds==17&&Math.Abs(elapsed-3456)<=1,"Missing remaining phase snapshot");
});
Check("Phase resynchronization preserves the original skeleton snapshots",()=>
{
    var rig=Player.Local.Avatar.Animation;var original=rig._bones[0].localPosition;
    Require(LocalAnimationService.Instance.Play(6),"Start failed");Time.unscaledTime+=0.3125f;LocalBodyAnimationService.Instance.LateUpdate();
    LocalBodyAnimationService.Instance.Synchronize(Player.Local,15,5.3125f);LocalBodyAnimationService.Instance.LateUpdate();
    LocalAnimationService.Instance.Stop(false);Require(rig._bones[0].localPosition.Equals(original),"Network sync baked in the old pose");
});
LandVehicle Chassis(int id=100)
{
    var go=Model("Sedan(Clone)");var v=go.AddComponent<LandVehicle>();v.Rb=go.AddComponent<Rigidbody>();v.boundingBox=go.AddComponent<BoxCollider>();
    v.NetworkObject=go.AddComponent<Il2CppFishNet.Object.NetworkObject>();v.NetworkObject.ObjectId=id;
    v.intObj=go.AddComponent<Il2CppScheduleOne.Interaction.InteractableObject>();
    go.AddComponent<LODGroup>();v.Storage=go.AddComponent<Il2CppScheduleOne.Storage.StorageEntity>();
    var trunk=new GameObject("Trunk interaction");trunk.transform.SetParent(go.transform,false);
    trunk.AddComponent<BoxCollider>();trunk.AddComponent<Il2CppScheduleOne.Storage.StorageEntityInteractable>();
    var wheels=new List<WheelCollider>();
    for(int i=0;i<4;i++) {var w=new GameObject("Wheel "+i);w.transform.SetParent(go.transform,false);w.transform.localPosition=new(i%2,0.4f,i<2?1:-1);wheels.Add(w.AddComponent<WheelCollider>());}
    v.steerWheels=new(wheels.Take(2).ToArray());
    var seats=new List<VehicleSeat>();
    for(int i=0;i<4;i++) {var seat=new GameObject("Seat "+i);seat.transform.SetParent(go.transform,false);var s=seat.AddComponent<VehicleSeat>();s.isDriverSeat=i==0;seats.Add(s);} v.Seats=new(seats.ToArray());
    var camera=new GameObject("Camera").transform;camera.SetParent(go.transform,false);v.cameraOrigin=camera;
    var exit=new GameObject("Exit").transform;exit.SetParent(go.transform,false);v.exitPoints.Add(exit);
    return v;
}
VehicleManager Manager()
{
    var manager=new GameObject("VehicleManager").AddComponent<VehicleManager>();var prefab=Chassis();prefab.gameObject.name="Sedan";manager.VehiclePrefabs.Add(prefab);
    manager.Factory=()=>Chassis(100+manager.SpawnCount);Resources.Assets.Add(manager);return manager;
}
Check("Render callback reapplies body motion after a later native pose writer and releases its subscription",()=>
{
    int external=0;Camera.CameraCallback observer=_=>external++;Camera.onPreCull+=observer;
    var hip=Player.Local.Avatar.HipBone;var baseline=hip.localPosition;
    Require(LocalAnimationService.Instance.Play(6),"Start failed");Time.unscaledTime+=0.3125f;
    LocalBodyAnimationService.Instance.LateUpdate();hip.localPosition=baseline;
    UnityEngine.Rendering.RenderPipelineManager.Render();Require(hip.localPosition.z>baseline.z,"Later native writer erased the rendered pose");
    Camera.onPreCull?.Invoke(new());LocalAnimationRenderService.Shutdown();Camera.onPreCull?.Invoke(new());
    Require(external==2,"Shutdown removed someone else's camera callback");Camera.onPreCull-=observer;
});
Check("Body uses current render mesh joints instead of a stale cached skeleton",()=>
{
    var rig=Player.Local.Avatar.Animation;var cached=rig._bones[0];var mesh=Player.Local.Avatar.gameObject.AddComponent<SkinnedMeshRenderer>();
    var hip=new GameObject("mixamorig:Hips").transform;hip.SetParent(Player.Local.Avatar.transform,false);mesh.bones=new(new[]{hip});
    Require(LocalAnimationService.Instance.Play(6),"Body failed");Time.unscaledTime+=0.3125f;UnityEngine.Rendering.RenderPipelineManager.Render();
    Require(hip.localPosition.z>0&&cached.localPosition.z==0,"Dormant skeleton was animated instead of visible joints");
});
Check("Laptop creates a grounded chair, seats the hips, and restores native joint ownership",()=>
{
    Player.Local.transform.position=new(2,1,3);var hip=Player.Local.Avatar.HipBone;var original=hip.localPosition;
    Require(LocalAnimationService.Instance.Play(1),"Laptop failed");UnityEngine.Rendering.RenderPipelineManager.Render();
    Require(Math.Abs(hip.position.y-0.475f)<0.001f&&Root(Visuals().Values.Single()).transform.position.y==0,"Seated on air or prop used capsule-center height");
    Require(Player.Local.transform.position.Equals(new Vector3(2,1,3)),"Moved the capsule");
    Player.Local.Avatar.Animation._animator.SetBool("Sitting",false);LocalAnimationService.Instance.Update();Require(Player.Local.Avatar.Animation._animator.GetBool("Sitting"),"Native frame overwrote the seated flag");
    LocalAnimationService.Instance.Stop(false);Require(hip.localPosition.Equals(original),"Seat offset stuck after Stop");
});
Check("Revving uses the native reversed seat orientation and aligns the avatar to the saddle",()=>
{
    Require(LocalAnimationService.Instance.Play(7),"Revving failed");UnityEngine.Rendering.RenderPipelineManager.Render();
    var hip=Player.Local.Avatar.HipBone;var root=Root(Visuals().Values.Single());
    Require(Math.Abs(hip.position.y-0.731f)<0.001f&&hip.position.z<0&&root.transform.forward.z<-.99f,"Native seat/model orientation was ignored");
});
Check("Ground placement ignores the player's own colliders and fails without a walkable surface",()=>
{
    var ground=new GameObject("Ground").AddComponent<Collider>();var self=Player.Local.gameObject.AddComponent<Collider>();
    Physics.Hits=new[]{new RaycastHit{collider=self,distance=1,point=new(0,9,0),normal=Vector3.up},new RaycastHit{collider=ground,distance=3,point=new(0,0,0),normal=Vector3.up}};
    Require(LocalAnimationPlacementService.Ground(Player.Local,new(),out var p)&&p.y==0,"Own collider used as ground");
    Physics.Hits=Array.Empty<RaycastHit>();Require(!LocalAnimationService.Instance.Play(1)&&Visuals().Count==0,"Floating fallback was allowed");
});
Check("Motorcycle spawn is host gated, avoids occupied space, and remains outside owned/save vehicles",()=>
{
    var manager=Manager();SessionAuthorityService.Instance.Host=false;MotorcycleService.Instance.Spawn();Require(manager.SpawnCount==0,"Client could spawn native chassis");
    SessionAuthorityService.Instance.Host=true;Physics.Blocked=true;MotorcycleService.Instance.Spawn();Require(manager.SpawnCount==0,"Spawn overlapped an obstacle");
    Physics.Blocked=false;MotorcycleService.Instance.Spawn();Require(manager.SpawnCount==1&&manager.LastPlayerOwned==false&&MotorcycleService.Instance.Count==1,"Native session spawn contract changed");
    var prefab=manager.VehiclePrefabs[0];Require(prefab.Rb.mass==1500&&prefab.gameObject.GetComponent<MeshRenderer>().enabled,"Registered prefab was mutated");
    var spawned=manager.AllVehicles.Single();Require(spawned.Rb.mass<500&&spawned.Seats.Length==4,"Conversion changed native RPC seat indices");
    MotorcycleService.Instance.Ride(spawned);Require(spawned.Entered==1&&Player.Local.CurrentVehicle==spawned.NetworkObject,"Native mount/ownership path was bypassed");
});
Check("Motorcycle requires matching peers and bounds the number of session spawns",()=>
{
    var manager=Manager();var peer=MakePlayer(1,false);MotorcycleService.Instance.Spawn();Require(manager.SpawnCount==0,"Mixed build was allowed");
    LocalAnimationNetworkService.Instance.HandleHostRequest(peer,-3);
    for(int i=0;i<6;i++) MotorcycleService.Instance.Spawn();Require(manager.SpawnCount==4&&MotorcycleService.Instance.Count==4,"Spawn bound failed");
});
Check("Failed conversion and detach leave the original cloned chassis usable",()=>
{
    var vehicle=Chassis();var body=vehicle.gameObject.GetComponent<MeshRenderer>();var camera=vehicle.cameraOrigin;
    vehicle.cameraOrigin=null;Require(!MotorcycleService.Instance.Attach(vehicle)&&body.enabled&&vehicle.Rb.mass==1500,"Failed validation partially converted vehicle");
    vehicle.cameraOrigin=camera;Require(MotorcycleService.Instance.Attach(vehicle),"Conversion failed");
    MotorcycleService.Instance.Detach(vehicle);Require(body.enabled&&vehicle.Rb.mass==1500&&vehicle.TopSpeed==30&&vehicle.vehicleName=="Sedan","Detach did not restore chassis");
});
Check("Motorcycle rider pose follows the saddle and exit restores normal native sitting state",()=>
{
    var vehicle=Chassis();Require(MotorcycleService.Instance.Attach(vehicle),"Attach failed");MotorcycleService.Instance.Ride(vehicle);MotorcycleService.Instance.Update();
    UnityEngine.Rendering.RenderPipelineManager.Render();var a=Player.Local.Avatar.Animation._animator;
    Require(a.GetBool("IsMotorbikeSitting")&&!a.GetBool("Sitting"),"Car sitting layer fought motorcycle pose");
    Player.Local.CurrentVehicle=null;MotorcycleService.Instance.Update();Require(!a.GetBool("IsMotorbikeSitting")&&!a.GetBool("Sitting"),"Rider pose stuck after native exit");
});
Check("Only the known ready host can tag a native spawned chassis as a motorcycle",()=>
{
    var manager=Manager();var vehicle=Chassis(101);manager.AllVehicles.Add(vehicle);SessionAuthorityService.Instance.Host=false;var net=MotorcycleNetworkService.Instance;
    LocalAnimationNetworkService.Instance.TryReceiveNetworkValue(MakePlayer(0,false),LocalAnimationNetworkService.StateVariable,"body12-ready");
    net.TryReceiveNetworkValue(MakePlayer(2,false),MotorcycleNetworkService.State,"101|1");net.Update();Require(!MotorcycleService.Instance.IsMotorcycle(vehicle),"Forged peer could convert vehicle");
    net.TryReceiveNetworkValue(Player.PlayerList.First(p=>p.Owner.ClientId==0&&!p.IsLocalPlayer),MotorcycleNetworkService.State,"101|1");Time.unscaledTime+=1;net.Update();Require(MotorcycleService.Instance.IsMotorcycle(vehicle),"Trusted native object snapshot lost");
    foreach(string invalid in new[]{"-1|1","101|2","101|1|3",new string('1',50),"2147483648|1"})Require(!MotorcycleNetworkService.TryParse(invalid,out _,out _),"Invalid object descriptor accepted");
});
Check("Solo native authority permits Boinking and motorcycle spawn without lobby-host status",()=>
{
    SessionAuthorityService.Instance.Host=false;Player.Local.IsServerInitialized=true;
    Require(LocalAnimationNetworkService.IsLocalAuthority&&LocalAnimationService.Instance.Play(6),"Solo body action needed a Steam lobby");
    Require(Player.Local.Requests.Count==0,"Solo action used the client request path");
    LocalAnimationService.Instance.Stop(false);var manager=Manager();MotorcycleService.Instance.Spawn();
    Require(manager.SpawnCount==1&&MotorcycleService.Instance.Count==1,"Solo spawning needed a Steam lobby");
    MotorcycleService.Instance.RemoveUnoccupied();Require(MotorcycleService.Instance.Count==0,"Solo cleanup needed a Steam lobby");
});
Check("Lobby status cannot grant native server authority to an actual client",()=>
{
    Player.Local.IsServerInitialized=false;var manager=Manager();
    Require(!LocalAnimationNetworkService.IsLocalAuthority&&!LocalAnimationService.Instance.Play(6),"Lobby flag bypassed matching body readiness");
    MotorcycleService.Instance.Spawn();Require(manager.SpawnCount==0,"Lobby flag granted native spawning");
});
Il2CppScheduleOne.Core.Avatar.IAttachmentAnchorProvider Anchors(Transform[] b)=>new()
{
    Hips=b[0],LowerSpine=b[1],MiddleSpine=b[2],LeftUpperArm=b[3],RightUpperArm=b[4],
    LeftLowerArm=b[5],RightLowerArm=b[6],LeftUpperLeg=b[7],RightUpperLeg=b[8],LeftLowerLeg=b[9],RightLowerLeg=b[10]
};
Check("Appearance anchor joints take precedence over duplicate cached and mesh bones",()=>
{
    var avatar=Player.Local.Avatar;var cached=avatar.Animation._bones.ToArray();var current=new List<Transform>();
    foreach(string name in LocalBodyAnimationService.JointNames) {var bone=new GameObject(name).transform;bone.SetParent(avatar.transform,false);current.Add(bone);}
    avatar.AttachmentAnchorProvider=Anchors(current.ToArray());
    var skin=avatar.gameObject.AddComponent<SkinnedMeshRenderer>();skin.bones=new(cached);
    Require(LocalAnimationPlacementService.Hip(Player.Local)==current[0],"Used the old serialized hip");
    Require(LocalAnimationService.Instance.Play(6),"Current provider could not animate");Time.unscaledTime+=0.3125f;LocalAnimationRenderService.Apply();
    Require(current[0].localPosition.z>0&&cached[0].localPosition.z==0,"Body animated a different skeleton than attachments");
});
Check("Seat alignment moves the complete avatar and hair while leaving joints and capsule intact",()=>
{
    var avatar=Player.Local.Avatar;var hip=avatar.HipBone;var original=hip.localPosition;
    var hair=new GameObject("Rigid hair").transform;hair.SetParent(avatar.transform,false);hair.localPosition=new(0,1.7f,0);
    Vector3 hairBefore=hair.position,hipBefore=hip.position,rootBefore=avatar.transform.localPosition;
    Require(LocalAnimationService.Instance.Play(7),"Revving failed");LocalAnimationRenderService.Apply();
    Require(((hair.position-hairBefore)-(hip.position-hipBefore)).sqrMagnitude<0.00000001f,"Hair was stranded on the old pose");
    Require(hip.localPosition.Equals(original)&&Player.Local.transform.position.Equals(Vector3.zero),"Seat deformed a joint or moved the capsule");
    LocalAnimationService.Instance.Stop(false);Require(avatar.transform.localPosition.Equals(rootBefore)&&hair.position.Equals(hairBefore),"Complete avatar restoration failed");
});
Check("Changing the appearance's anchor rig ends the body emote without overwriting its replacement",()=>
{
    var avatar=Player.Local.Avatar;var old=avatar.Animation._bones.ToArray();avatar.AttachmentAnchorProvider=Anchors(old);
    Require(LocalAnimationService.Instance.Play(6),"Body failed");Time.unscaledTime+=0.3125f;LocalAnimationRenderService.Apply();
    var hip=new GameObject("Replacement hips").transform;hip.SetParent(avatar.transform,false);hip.localPosition=new(0,1,0);
    var replacement=old.ToArray();replacement[0]=hip;avatar.AttachmentAnchorProvider=Anchors(replacement);
    LocalBodyAnimationService.Instance.Update();LocalAnimationRenderService.Apply();
    Require(LocalBodyAnimationService.Instance.ActiveCount==0&&hip.localPosition.Equals(new Vector3(0,1,0)),"Appearance replacement was overwritten");
});
Check("Seated native pose is evaluated once per frame before contact alignment",()=>
{
    var avatar=Player.Local.Avatar;var a=avatar.Animation._animator;var hip=avatar.HipBone;
    a.OnEvaluate=()=> {Require(a.GetBool("Sitting")&&a.GetBool("IsTyping"),"Evaluated standing flags");hip.localPosition=new(0,0.6f,0);};
    Require(LocalAnimationService.Instance.Play(1),"Laptop failed");a.SetBool("Sitting",false);LocalAnimationRenderService.Apply();
    Require(a.Evaluations==1&&Math.Abs(hip.position.y-0.475f)<0.001f,"Alignment used the pre-seated joint pose");
    UnityEngine.Rendering.RenderPipelineManager.Render();Camera.onPreCull?.Invoke(new());Require(a.Evaluations==1,"Multiple cameras advanced/evaluated the same pose repeatedly");
    Time.unscaledTime+=1;a.SetBool("Sitting",false);avatar.transform.localPosition=Vector3.zero;LocalAnimationRenderService.Apply();
    Require(a.Evaluations==2&&Math.Abs(hip.position.y-0.475f)<0.001f,"Next native frame lost seated contact");
});
Check("An avatar bound directly to the player root fails seating without moving gameplay",()=>
{
    var p=Player.Local;p.Avatar.gameObject=p.gameObject;
    Require(!LocalAnimationService.Instance.Play(1)&&!LocalAnimationService.Instance.Play(7)&&p.transform.position.Equals(Vector3.zero),"Unsafe capsule-root fallback was used");
});
Check("Revving turns the bike and saddle with the player without moving the capsule",()=>
{
    Require(LocalAnimationService.Instance.Play(7),"Revving failed");var root=Root(Visuals().Values.Single());
    var start=root.transform.forward;Player.Local.transform.rotation=Quaternion.Euler(0,90,0);LocalAnimationRenderService.Apply();
    Require((root.transform.forward-Quaternion.Euler(0,90,0)*start).sqrMagnitude<.000001f,"Bike stayed behind on turn");
    Require(Player.Local.transform.position.Equals(Vector3.zero),"Turn moved player capsule");
});
GameObject FxRoot()
{
    var map=(System.Collections.IDictionary)typeof(LocalAnimationFxService).GetField("_effects",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(LocalAnimationFxService.Instance);
    var effect=map.Values.Cast<object>().Single();
    return (GameObject)effect.GetType().GetField("Root",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(effect);
}
Check("Pee and vomit own particles and audio and stop without changing the NPC source",()=>
{
    foreach(int action in new[]{4,5})
    {
        Time.unscaledTime+=1;Require(LocalAnimationService.Instance.Play(action),"FX action failed");var root=FxRoot();
        Require(root.GetComponentsInChildren<ParticleSystem>(true).All(p=>p.Playing)&&root.GetComponentsInChildren<AudioSource>(true).All(a=>a.Playing),"FX/sound not started");
        LocalAnimationService.Instance.Stop(false);Require(root.Destroyed&&!root.activeSelf&&LocalAnimationFxService.Instance.Count==0,"Owned FX leaked");
        var source=Resources.Assets.OfType<Il2CppScheduleOne.NPCs.Other.NPCAuxAction>().First(a=>a._animBoolName==(action==4?"IsPissing":"IsVomiting"))._auxEffect;
        Require(!source.Destroyed&&!source.GetComponent<ParticleSystem>().Playing&&!source.GetComponent<AudioSource>().Playing,"NPC source was played/stopped");
    }
});
Check("Observer FX attach to the actor's current head or hips with the native relative offset",()=>
{
    var actor=MakePlayer(3,false);actor.transform.SetPositionAndRotation(new(10,0,20),Quaternion.Euler(0,90,0));
    foreach(int action in new[]{4,5})
    {
        Require(LocalAnimationVisualService.Instance.Start(actor,action,20),"Observer FX start failed");var root=FxRoot();
        var anchor=action==4?actor.Avatar.HipBone:actor.Avatar.Effects.HeadBone;
        Require(root.transform.parent==anchor&&(root.transform.position-anchor.TransformPoint(new(0,0,.08f))).sqrMagnitude<.000001f,"FX followed source NPC or used the wrong emitter offset");
        LocalAnimationVisualService.Instance.Stop(actor);
    }
});
Check("Unsafe native FX templates fail before NPC gameplay components are cloned",()=>
{
    var native=Resources.Assets.OfType<Il2CppScheduleOne.NPCs.Other.NPCAuxAction>().First(a=>a._animBoolName=="IsPissing");
    native._auxEffect.AddComponent<LandVehicle>();Require(!LocalAnimationService.Instance.Play(4),"Unsafe native effect cloned");
    Require(LocalAnimationFxService.Instance.Count==0&&Player.Local.Requests.Count==0,"Partial unsafe effect was broadcast");
});
Check("Burnout uses a private smoke/audio copy and a configured volume even when native idle audio is muted",()=>
{
    var source=Resources.Assets.OfType<Motorbike>().Single().BurnoutController;source._burnoutAudio._audioSource.volume=0;
    Require(LocalAnimationService.Instance.Play(7),"Revving failed");var root=FxRoot();
    Require(root.GetComponent<UnityEngine.VFX.VisualEffect>().Rate==36&&root.GetComponent<AudioSource>().Playing&&root.GetComponent<AudioSource>().volume>0,"Burnout stayed silent or unstarted");
    Require(!source.burnoutEffect.Playing&&!source._burnoutAudio._audioSource.Playing,"Played source NPC burnout");
});
Check("Remote FX clean up on movement, approval loss, death and scene reset",()=>
{
    var actor=MakePlayer(2,false);
    Require(LocalAnimationVisualService.Instance.Start(actor,4,20),"Observer FX failed");actor.transform.position=new(2,0,0);
    LocalAnimationVisualService.Instance.Update();Require(LocalAnimationFxService.Instance.Count==0,"Moving observer FX leaked");
    Require(LocalAnimationVisualService.Instance.Start(actor,5,20),"Observer vomit failed");SessionAuthorityService.Instance.FeaturesAllowed=false;
    LocalAnimationVisualService.Instance.Update();Require(LocalAnimationFxService.Instance.Count==0,"Denied observer FX leaked");
    SessionAuthorityService.Instance.FeaturesAllowed=true;Require(LocalAnimationVisualService.Instance.Start(actor,4,20),"Restart failed");actor.Health.IsAlive=false;
    LocalAnimationVisualService.Instance.Update();Require(LocalAnimationFxService.Instance.Count==0,"Dead observer FX leaked");
    actor.Health.IsAlive=true;Require(LocalAnimationVisualService.Instance.Start(actor,4,20),"Restart failed");LocalAnimationVisualService.Instance.Reset();
    Require(LocalAnimationFxService.Instance.Count==0,"Scene FX leaked");
});
Check("Motorcycle disables car LODs and suppresses native renderer visibility rewrites",()=>
{
    var vehicle=Chassis();Require(MotorcycleService.Instance.Attach(vehicle),"Attach failed");
    vehicle.GetComponent<MeshRenderer>().enabled=true;MotorcycleService.Instance.RefreshVisibility(vehicle);
    Require(!vehicle.GetComponent<MeshRenderer>().enabled&&!vehicle.GetComponent<LODGroup>().enabled,"Car returned");
    var visual=MotorcycleService.Instance.Bikes.Single().Visual;vehicle.IsVisible=false;MotorcycleService.Instance.RefreshVisibility(vehicle);Require(!visual.activeSelf,"Distant bike ignored native visibility");
    vehicle.IsVisible=true;MotorcycleService.Instance.RefreshVisibility(vehicle);Require(visual.activeSelf&&!vehicle.GetComponent<MeshRenderer>().enabled,"Culling returned the car");
    MotorcycleService.Instance.Detach(vehicle);Require(vehicle.GetComponent<MeshRenderer>().enabled&&vehicle.GetComponent<LODGroup>().enabled,"Detach did not restore chassis rendering");
});
Check("Motorcycle has a live collider on its native interaction object and no storage interaction",()=>
{
    var v=Chassis();Require(MotorcycleService.Instance.Attach(v),"Attach failed");
    var interaction=v.intObj.GetComponentsInChildren<Collider>(true).Where(c=>c.enabled&&!c.Destroyed).ToArray();
    Require(interaction.Any(c=>c.gameObject==v.intObj.gameObject&&c is BoxCollider),"No mount hit target on the native interaction object");
    Require(!v.Storage.enabled&&v.GetComponentsInChildren<Il2CppScheduleOne.Storage.StorageEntityInteractable>(true).All(i=>!i.enabled),"Storage remains usable");
    MotorcycleService.Instance.Ride(v);Require(v.Entered==1&&Player.Local.CurrentVehicle==v.NetworkObject,"Native vehicle entry path not called");
    MotorcycleService.Instance.Detach(v);Require(v.Storage.enabled&&v.GetComponentsInChildren<Il2CppScheduleOne.Storage.StorageEntityInteractable>(true).All(i=>i.enabled),"Storage not restored on detaching clone");
    Require(interaction.Any(c=>c.Destroyed),"Temporary mount/body collider leaked");
});
Check("Clear-effect skin restoration uses chosen appearance and handles cyclic mirrored avatars",()=>
{
    var player=Player.Local;var effects=player.Avatar.Effects;effects.SkinColorSmoother.Default=new(1,1,1);
    effects.SkinColorSmoother.Overrides["sickly"]=new(1.1f,1.1f,1.1f);effects.SkinColorSmoother.Overrides["foreign"]=new(.5f,.5f,.5f);
    var mirror=MakePlayer(3,false).Avatar.Effects;effects.MirrorEffectsTo=new[]{mirror};mirror.MirrorEffectsTo=new[]{effects};
    EffectAppearanceResetService.RestoreSkin(player);
    Require(!effects.SkinColorSmoother.Overrides.ContainsKey("sickly")&&effects.SkinColorSmoother.Overrides.ContainsKey("foreign"),"Removed wrong override");
    Require(effects.SkinColorSmoother.Default.Equals(player.CurrentAppearance.SkinColor)&&effects.SkinColorSmoother._CurrentValue_k__BackingField.Equals(player.CurrentAppearance.SkinColor),"White smoother color survived reset");
    Require(player.Avatar.Appearance.Rendered.Equals(player.CurrentAppearance.SkinColor)&&mirror.Avatar.Appearance.Rendered.Equals(player.CurrentAppearance.SkinColor),"Mirrored character stayed white");
});
Check("Missing chosen appearance uses native naked appearance and never falls back to white",()=>
{
    var p=Player.Local;p.CurrentAppearance=null;var expected=p.Avatar.Appearance.AppliedNakedAppearance.SkinColor;
    EffectAppearanceResetService.RestoreSkin(p);Require(p.Avatar.Appearance.Rendered.Equals(expected),"Naked appearance lost");
    p.Avatar.Appearance.AppliedNakedAppearance=null;p.Avatar.Appearance.Rendered=new(.2f,.3f,.4f);
    EffectAppearanceResetService.RestoreSkin(p);Require(p.Avatar.Appearance.Rendered.Equals(new Color(.2f,.3f,.4f)),"Missing metadata painted white");
});
Check("ClearAllEffects restores healthy skin after Lethal adds a tint and another cleanup throws",()=>
{
    var other=MakePlayer(3,false);other.CurrentAppearance.SkinColor=new(.1f,.2f,.3f);
    Resources.Assets.Add(new Il2CppScheduleOne.Effects.Effect{ID="Lethal",Name="Lethal",Cleanup=p=>p.Avatar.Effects.SkinColorSmoother.Overrides["sickly"]=new(1.1f,1.1f,1.1f)});
    Resources.Assets.Add(new Il2CppScheduleOne.Effects.Effect{ID="Sneaky",Name="Sneaky",Cleanup=p=>throw new InvalidOperationException("Inactive native visibility attribute")});
    EffectsService.Instance.ClearAllEffects();EffectsService.Instance.ClearAllEffects();
    foreach(var player in new[]{Player.Local,other})
        Require(!player.Avatar.Effects.SkinColorSmoother.Overrides.ContainsKey("sickly")&&player.Avatar.Appearance.Rendered.Equals(player.CurrentAppearance.SkinColor),"Cleanup failure/repetition stranded a white avatar");
});
Check("Scaled sedan bounds convert to metre-sized motorcycle bounds",()=>
{
    var v=Chassis();var bounds=new GameObject("Scaled bounds");bounds.transform.SetParent(v.transform,false);
    bounds.transform.localScale=new(1.85f,1.55f,4.85f);v.boundingBox=bounds.AddComponent<BoxCollider>();
    var target=new GameObject("Scaled IntObj");target.transform.SetParent(v.transform,false);target.transform.localScale=new(1.92f,.77843f,1.626f);
    target.transform.localRotation=Quaternion.Euler(0,180,0);v.intObj=target.AddComponent<Il2CppScheduleOne.Interaction.InteractableObject>();
    Require(MotorcycleService.Instance.Attach(v),"Attach failed");
    var box=v.boundingBox;var scale=box.transform.lossyScale;
    Require(Math.Abs(box.size.z*scale.z-2.7f)<.0001f&&Math.Abs(box.size.x*scale.x-.65f)<.0001f,"Car-scaled bounds remained");
    var interaction=target.GetComponent<BoxCollider>();Require(interaction.isTrigger&&Math.Abs(interaction.size.z*target.transform.lossyScale.z-2.5f)<.0001f,"Interaction scale changed physical contacts");
    Require(v.gameObject.GetComponentsInChildren<BoxCollider>(true).Any(c=>c.enabled&&!c.isTrigger&&c.gameObject==v.gameObject&&c.size.z<2f),"No separate bike body");
});
Check("Native axle anchors produce level grounded contacts with a bike wheelbase",()=>
{
    var v=Chassis();Require(MotorcycleService.Instance.Attach(v),"Attach failed");
    var all=v.GetComponentsInChildren<WheelCollider>(true);var front=v.steerWheels;var rear=all.Where(w=>!front.Contains(w)).ToArray();
    Require(Math.Abs(front[0].transform.position.y-rear[0].transform.position.y)<.01f,"Front axle got an extra wheel radius");
    Require(front[0].transform.position.z-rear[0].transform.position.z>1.8f,"Bike axles have the wrong facing/wheelbase");
    Require(all.All(w=>w.radius>.3f&&w.suspensionDistance>.1f),"Wheel contact geometry invalid");
    var constraints=v.Rb.constraints;
    Require(constraints.HasFlag(RigidbodyConstraints.FreezeRotationX)&&constraints.HasFlag(RigidbodyConstraints.FreezeRotationZ),"Sedan airborne turnover can flip bike");
});
Check("Wheel placement works beneath a transformed parent",()=>
{
    var v=Chassis();var parent=new GameObject("Wheel parent");parent.transform.SetParent(v.transform,false);parent.transform.localPosition=new(3,2,4);
    foreach(var w in v.GetComponentsInChildren<WheelCollider>(true))w.transform.SetParent(parent.transform,false);
    Require(MotorcycleService.Instance.Attach(v),"Attach failed");
    Require(v.steerWheels.All(w=>Math.Abs(w.transform.position.x)<.3f&&Math.Abs(w.transform.position.y)<.01f),"Root-relative position was written as parent-relative position");
});
Check("Motorcycle keeps native power scaling with stronger mass-scaled brakes and clears overrides",()=>
{
    var v=Chassis();float gearing=v.diffGearing,brake=v.BrakeForceMultiplier,handbrake=v.handBrakeForce,mass=v.Rb.mass;
    v.overrideControls=true;v.throttleOverride=1;v.steerOverride=-1;v.handbrakeOverride=true;
    Require(MotorcycleService.Instance.Attach(v),"Attach failed");
    float brakeGain=(v.BrakeForceMultiplier/v.Rb.mass)/(brake/mass),handbrakeGain=(v.handBrakeForce/v.Rb.mass)/(handbrake/mass);
    Require(Math.Abs(v.diffGearing/v.Rb.mass-gearing/mass)<.00001f&&brakeGain>=2&&brakeGain<=3&&handbrakeGain>=1.5f&&handbrakeGain<=2.1f,"Native power or stronger brake scaling is incorrect");
    Require(!v.overrideControls&&!v.handbrakeOverride&&v.throttleOverride==0&&v.steerOverride==0,"AI control overrides block driving");
    MotorcycleService.Instance.Detach(v);
    Require(v.Rb.mass==mass&&v.diffGearing==gearing&&v.BrakeForceMultiplier==brake&&v.handBrakeForce==handbrake&&v.overrideControls&&v.handbrakeOverride,"Physics/control settings did not roll back");
});
Check("Native reparented hidden avatar becomes a visible seated rider",()=>
{
    var v=Chassis();Require(MotorcycleService.Instance.Attach(v),"Attach failed");MotorcycleService.Instance.Ride(v);
    Require(!Player.Local.Avatar.gameObject.activeSelf&&LocalAnimationPlacementService.PoseRoot(Player.Local)==null,"Fixture did not reproduce native vehicle entry");
    MotorcycleService.Instance.Update();MotorcycleService.Instance.ApplyRiderPoses();
    Require(Player.Local.Avatar.gameObject.activeSelf&&Player.Local.ThirdPersonMeshesVisibleToLocalPlayer,"Native vehicle hiding remained");
    Require((LocalAnimationPlacementService.Hip(Player.Local).position-MotorcycleService.Instance.Bikes.Single().Seat.position).sqrMagnitude<.00001f,"Hip not on the saddle");
    Require(Player.Local.transform.localPosition.Equals(Vector3.zero),"Seating moved the player network root");
});
Check("Turning and repeated native visibility writes cannot hide or detach rider",()=>
{
    var v=Chassis();Require(MotorcycleService.Instance.Attach(v),"Attach failed");MotorcycleService.Instance.Ride(v);MotorcycleService.Instance.Update();
    foreach(float yaw in new[]{90f,180f,270f})
    {
        v.transform.rotation=Quaternion.Euler(0,yaw,0);Player.Local.Avatar.SetVisible(false);Player.Local.SetThirdPersonMeshesVisibility(false);
        Time.unscaledTime+=.1f;MotorcycleService.Instance.ApplyRiderPoses();
        Require(Player.Local.Avatar.gameObject.activeSelf&&Player.Local.ThirdPersonMeshesVisibleToLocalPlayer,"Turning hid rider");
        Require((LocalAnimationPlacementService.Hip(Player.Local).position-MotorcycleService.Instance.Bikes.Single().Seat.position).sqrMagnitude<.00001f,"Turning separated saddle and rider");
        Require((Player.Local.Avatar.transform.forward-MotorcycleService.Instance.Bikes.Single().Seat.forward).sqrMagnitude<.00001f,"Rider turned independently of bike");
    }
});
Check("Native dismount retains walking transform and restores mesh visibility policy",()=>
{
    var v=Chassis();Require(MotorcycleService.Instance.Attach(v),"Attach failed");MotorcycleService.Instance.Ride(v);MotorcycleService.Instance.Update();MotorcycleService.Instance.ApplyRiderPoses();
    v.ExitVehicle();var position=Player.Local.Avatar.transform.localPosition;var rotation=Player.Local.Avatar.transform.localRotation;
    MotorcycleService.Instance.Update();MotorcycleService.Instance.ApplyRiderPoses();
    Require(Player.Local.Avatar.transform.parent==Player.Local.transform&&Player.Local.Avatar.transform.localPosition.Equals(position)&&Player.Local.Avatar.transform.localRotation.Equals(rotation),"Bike seating overwrote native exit transform");
    Require(Player.Local.Avatar.gameObject.activeSelf&&!Player.Local.ThirdPersonMeshesVisibleToLocalPlayer,"First-person visibility did not return");
});
Check("Remote rider uses native vehicle parenting without moving local player",()=>
{
    var v=Chassis();Require(MotorcycleService.Instance.Attach(v),"Attach failed");var remote=MakePlayer(2,false);
    remote.CurrentVehicle=v.NetworkObject;remote.Avatar.transform.SetParent(v.transform,false);remote.Avatar.SetVisible(false);
    MotorcycleService.Instance.Update();MotorcycleService.Instance.ApplyRiderPoses();
    Require(remote.Avatar.gameObject.activeSelf&&(LocalAnimationPlacementService.Hip(remote).position-MotorcycleService.Instance.Bikes.Single().Seat.position).sqrMagnitude<.00001f,"Remote rider not seated/visible");
    Require(!Player.Local.ThirdPersonMeshesVisibleToLocalPlayer&&Player.Local.transform.localPosition.Equals(Vector3.zero),"Remote rider changed local visibility/movement");
});
Check("Steering leans the visual both ways while preserving the physics root",()=>
{
    var v=Chassis();Require(MotorcycleService.Instance.Attach(v),"Attach failed");var bike=MotorcycleService.Instance.Bikes.Single();
    var physics=v.transform.localRotation;float delta=Time.deltaTime;Time.deltaTime=1f;v.IsOccupied=true;v.Speed_Kmh=30;v.CurrentSteerAngle=28;
    MotorcycleService.Instance.Update();float right=(bike.Visual.transform.rotation*Vector3.up).x;
    v.CurrentSteerAngle=-28;MotorcycleService.Instance.Update();float left=(bike.Visual.transform.rotation*Vector3.up).x;
    Require(right>.1f&&left<-.1f&&v.transform.localRotation.Equals(physics),"Steering did not bank both ways or rotated physics directly");
    v.Speed_Kmh=0;MotorcycleService.Instance.Update();Require(Math.Abs((bike.Visual.transform.rotation*Vector3.up).x)<.0001f,"Parked bike retained riding lean");Time.deltaTime=delta;
});
Check("Suspension display follows ground contact without moving the Rigidbody",()=>
{
    var v=Chassis();Require(MotorcycleService.Instance.Attach(v),"Attach failed");var bike=MotorcycleService.Instance.Bikes.Single();var root=v.transform.position;
    foreach(var w in v.GetComponentsInChildren<WheelCollider>(true))w.ContactCenter=new(0,.1f,0);
    MotorcycleService.Instance.ApplyRiderPoses();var shown=bike.Visual.transform.position;
    MotorcycleService.Instance.ApplyRiderPoses();Require((shown-bike.Visual.transform.position).sqrMagnitude<.0001f&&v.transform.position.Equals(root),"Suspension drifted or moved network physics");
});
Check("Motorcycle engine starts, revs on throttle and reverse, and stops after exit",()=>
{
    var v=Chassis();var sound=v.gameObject.AddComponent<Il2CppScheduleOne.Vehicles.Sound.VehicleSound>();
    var engineObject=new GameObject("Car engine");engineObject.transform.SetParent(v.transform,false);
    sound.EngineLoopSource=engineObject.AddComponent<Il2CppScheduleOne.Audio.AudioSourceController>();sound.EngineLoopSource._audioSource=engineObject.AddComponent<AudioSource>();
    var native=Resources.Assets.OfType<Motorbike>().Single().BurnoutController._burnoutAudio._audioSource;native.volume=0;
    Require(MotorcycleService.Instance.Attach(v),"Attach failed");var source=MotorcycleService.Instance.Bikes.Single().Engine.Source;
    MotorcycleService.Instance.Update();Require(!source.isPlaying&&sound.EngineLoopSource._audioSource.mute,"Car engine audible or empty bike running");
    MotorcycleService.Instance.Ride(v);MotorcycleService.Instance.Update();float idle=source.pitch;
    v.currentThrottle=1;v.Speed_Kmh=30;MotorcycleService.Instance.Update();Require(source.isPlaying&&source.pitch>idle&&source.volume>0&&source.clip.name=="Nugzz original V-twin idle"&&MotorcycleService.Instance.Bikes.Single().Engine.Detail.clip.name=="Nugzz original V-twin load","Throttle did not blend authored engine banks");
    v.currentThrottle=-1;v.Speed_Kmh=-5;MotorcycleService.Instance.Update();Require(source.isPlaying&&source.pitch>idle&&!native.isPlaying,"Reverse did not rev or original prop was changed");
    v.ExitVehicle();for(int i=0;i<20;i++)MotorcycleService.Instance.Update();Require(!source.isPlaying,"Engine remained after exit fade");
    MotorcycleService.Instance.Detach(v);Require(!sound.EngineLoopSource._audioSource.mute,"Car engine mute not restored");
});
Check("Earlier motorcycle revisions cannot satisfy the matching physics handshake",()=>
{
    SessionAuthorityService.Instance.Host=false;var host=MakePlayer(0,false);
    LocalAnimationNetworkService.Instance.TryReceiveNetworkValue(host,LocalAnimationNetworkService.StateVariable,"body11-ready");
    Require(!LocalAnimationNetworkService.Instance.CanSynchronizeBody,"Earlier physics revision accepted");
    LocalAnimationNetworkService.Instance.TryReceiveNetworkValue(host,LocalAnimationNetworkService.StateVariable,"body12-ready");
    Require(LocalAnimationNetworkService.Instance.CanSynchronizeBody,"Matching revision was rejected");
});
Check("Deferred native Start reads the motorcycle COM and detach restores its anchor",()=>
{
    var v=Chassis();var center=new GameObject("Native COM");center.transform.SetParent(v.transform,false);
    center.transform.localPosition=new(0,-.295f,0);v.centerOfMass=center.transform;var original=center.transform.localPosition;
    Require(MotorcycleService.Instance.Attach(v),"Attach failed");var bikeCom=v.Rb.centerOfMass;
    // This is the PE-verified Start calculation, not a simulated Rigidbody.
    v.Rb.centerOfMass=v.transform.InverseTransformPoint(v.centerOfMass.position);
    Require((v.Rb.centerOfMass-bikeCom).sqrMagnitude<.00001f,"Native initialization replaced bike COM");
    MotorcycleService.Instance.Detach(v);Require(center.transform.localPosition.Equals(original),"COM anchor not restored");
});
Check("A delayed client conversion never hides a rider already parented to the vehicle",()=>
{
    var v=Chassis();var player=Player.Local;var clothing=player.Avatar.gameObject.AddComponent<MeshRenderer>();
    var hair=Model("Hair");hair.transform.SetParent(player.Avatar.transform,false);
    var col=player.Avatar.gameObject.AddComponent<Collider>();var lod=player.Avatar.gameObject.AddComponent<LODGroup>();
    v.EnterVehicle();Require(MotorcycleService.Instance.Attach(v),"Late conversion failed");MotorcycleService.Instance.Update();MotorcycleService.Instance.ApplyRiderPoses();
    MotorcycleService.Instance.RefreshVisibility(v);
    Require(clothing.enabled&&hair.GetComponent<MeshRenderer>().enabled&&col.enabled&&lod.enabled&&player.Avatar.gameObject.activeSelf,"Car conversion hid or altered existing rider components");
    Require(!v.GetComponent<MeshRenderer>().enabled,"Late conversion exposed car body");
});
Check("The native body cull, renderer and animator gates all recover before drawing the rider",()=>
{
    var player=Player.Local;var avatar=player.Avatar;var v=Chassis();
    var body=Model("Body");body.transform.SetParent(avatar.transform,false);avatar.BodyContainer=body.transform;
    avatar.HipBone.SetParent(body.transform,false);var hair=Model("Hair");hair.transform.SetParent(avatar.HipBone,false);
    var clothing=Model("Native tie dye shirt");clothing.transform.SetParent(body.transform,false);
    var impostor=Model("Impostor");impostor.transform.SetParent(avatar.transform,false);
    avatar._impostor=impostor.AddComponent<Il2CppScheduleOne.Avatar.Impostors.AvatarImpostor>();
    int unculls=0;avatar.OnAvatarCullingChange=c=>{if(!c)unculls++;};
    var color=player.CurrentAppearance.SkinColor;
    Require(MotorcycleService.Instance.Attach(v),"Attach failed");MotorcycleService.Instance.Ride(v);
    avatar._IsCulled_k__BackingField=true;body.SetActive(false);avatar.Animation._animator.enabled=false;
    foreach(var renderer in avatar.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
    MotorcycleService.Instance.Update();UnityEngine.Rendering.RenderPipelineManager.Render();
    Require(body.activeInHierarchy&&!avatar.IsCulled&&avatar.Animation._animator.enabled&&unculls==1,"Body/animation culling survived");
    Require(hair.GetComponent<MeshRenderer>().enabled&&clothing.GetComponent<MeshRenderer>().enabled,"Rigid hair/clothing stayed hidden");
    Require(!impostor.activeSelf&&!impostor.GetComponent<MeshRenderer>().enabled,"Impostor was displayed with the real body");
    Require(player.CurrentAppearance.SkinColor.Equals(color),"Rider repair rewrote appearance");
    Require((avatar.HipBone.position-MotorcycleService.Instance.Bikes.Single().Seat.position).sqrMagnitude<.00001f,"Body is not on saddle");
});
Check("Repeated native renderer hiding and new clothing cannot remove the local rider",()=>
{
    var v=Chassis();var avatar=Player.Local.Avatar;var shirt=Model("Shirt");shirt.transform.SetParent(avatar.transform,false);
    Require(MotorcycleService.Instance.Attach(v),"Attach failed");MotorcycleService.Instance.Ride(v);MotorcycleService.Instance.Update();
    for(int i=0;i<3;i++)
    {
        shirt.GetComponent<MeshRenderer>().enabled=false;avatar.SetVisible(false);Player.Local.SetThirdPersonMeshesVisibility(false);
        v.transform.rotation=Quaternion.Euler(0,i*90,0);Time.unscaledTime+=.02f;
        MotorcycleService.Instance.ApplyRiderPoses();
        Require(shirt.GetComponent<MeshRenderer>().enabled&&shirt.layer==8&&avatar.gameObject.activeInHierarchy,"Native hide won before render");
    }
    var newHair=Model("Changed hair");newHair.transform.SetParent(avatar.HipBone,false);newHair.layer=12;newHair.GetComponent<MeshRenderer>().enabled=false;
    Time.unscaledTime+=.6f;MotorcycleService.Instance.ApplyRiderPoses();
    Require(newHair.layer==8&&newHair.GetComponent<MeshRenderer>().enabled,"New appearance did not enter the visible layer");
});
Check("Distant remote rider culling remains native and never changes the local layers",()=>
{
    var v=Chassis();Require(MotorcycleService.Instance.Attach(v),"Attach failed");var remote=MakePlayer(2,false);
    var body=Model("Remote body");body.transform.SetParent(remote.Avatar.transform,false);remote.Avatar.BodyContainer=body.transform;
    remote.Avatar._IsCulled_k__BackingField=true;body.SetActive(false);remote.Avatar.Animation._animator.enabled=false;
    remote.CurrentVehicle=v.NetworkObject;remote.Avatar.transform.SetParent(v.transform,false);
    MotorcycleService.Instance.Update();MotorcycleService.Instance.ApplyRiderPoses();
    Require(!body.activeSelf&&!remote.Avatar.Animation._animator.enabled,"Distant observer was force-rendered");
    remote.Avatar._IsCulled_k__BackingField=false;body.GetComponent<MeshRenderer>().enabled=false;
    Time.unscaledTime+=.6f;MotorcycleService.Instance.ApplyRiderPoses();
    Require(body.activeSelf&&body.GetComponent<MeshRenderer>().enabled,"Visible remote rider stayed hidden");
    Require(!Player.Local.ThirdPersonMeshesVisibleToLocalPlayer,"Remote rider changed local layer policy");
});
Check("Ride prompt uses one fixed anchor and avoids repeated native message/state changes",()=>
{
    var v=Chassis();Require(MotorcycleService.Instance.Attach(v),"Attach failed");
    var target=v.intObj;var anchor=target.displayLocationPoint.position;
    Il2CppScheduleOne.Interaction.InteractionManager.Instance=new(){HoveredInteractableObject=target};
    int messages=target.MessageWrites,states=target.StateWrites;
    for(int i=0;i<20;i++)
    {
        Player.Local.transform.position=new(i*.01f,0,0);MotorcyclePromptService.Hover(v);
        Require(MotorcyclePromptService.ShouldShow(target),"Valid hover lost prompt");
        Require(target.displayLocationPoint.position.Equals(anchor)&&target.displayLocationCollider==null,"Anchor wandered to collider closest point");
    }
    Require(target.MessageWrites==messages&&target.StateWrites==states&&target.message=="Ride motorcycle","Prompt rebuilt its native descriptor every frame");
});
Check("Bike prompt disappears when mounted, looking away, opening UI, hidden or unapproved",()=>
{
    var v=Chassis();Require(MotorcycleService.Instance.Attach(v),"Attach failed");var target=v.intObj;
    var other=new GameObject("Other native interaction").AddComponent<Il2CppScheduleOne.Interaction.InteractableObject>();
    var manager=new Il2CppScheduleOne.Interaction.InteractionManager{HoveredInteractableObject=other};
    Il2CppScheduleOne.Interaction.InteractionManager.Instance=manager;
    Require(!MotorcyclePromptService.ShouldShow(target)&&MotorcyclePromptService.ShouldShow(other),"Stale bike replaced another native prompt");
    manager.HoveredInteractableObject=target;Require(MotorcyclePromptService.ShouldShow(target),"Initial hover missing");
    GameplayStateGateService.Instance.Blocked=true;Require(!MotorcyclePromptService.ShouldShow(target),"UI left a bike prompt");GameplayStateGateService.Instance.Blocked=false;
    SessionAuthorityService.Instance.FeaturesAllowed=false;Require(!MotorcyclePromptService.ShouldShow(target),"Denied session displayed prompt");SessionAuthorityService.Instance.FeaturesAllowed=true;
    v.IsVisible=false;Require(!MotorcyclePromptService.ShouldShow(target),"Hidden vehicle displayed prompt");v.IsVisible=true;
    MotorcycleService.Instance.Ride(v);Require(!MotorcyclePromptService.ShouldShow(target),"Own occupied bike prompted while driving");
    v.ExitVehicle();Require(MotorcyclePromptService.ShouldShow(target),"Native exit left mount prompt disabled");
});
Check("Prompt anchors and native descriptor state roll back on detach",()=>
{
    var v=Chassis();var original=new GameObject("Native display location").transform;original.SetParent(v.transform,false);
    v.intObj.displayLocationPoint=original;v.intObj.displayLocationCollider=v.boundingBox;
    v.intObj.SetMessage("Enter vehicle");v.intObj.SetInteractableState(Il2CppScheduleOne.Interaction.InteractableObject.EInteractableState.Disabled);
    Require(MotorcycleService.Instance.Attach(v),"Attach failed");var anchor=v.intObj.displayLocationPoint.gameObject;
    MotorcycleService.Instance.Detach(v);
    Require(anchor.Destroyed&&v.intObj.displayLocationPoint==original&&v.intObj.displayLocationCollider==v.boundingBox,"Temporary prompt leaked");
    Require(v.intObj.message=="Enter vehicle"&&v.intObj.interactionState==Il2CppScheduleOne.Interaction.InteractableObject.EInteractableState.Disabled,"Native descriptor state lost");
});
Check("Steering tightens with speed, centers on release, and lean never exceeds six degrees",()=>
{
    var v=Chassis();Require(MotorcycleService.Instance.Attach(v),"Attach failed");MotorcycleService.Instance.Ride(v);
    var bike=MotorcycleService.Instance.Bikes.Single();float dt=Time.deltaTime;Time.deltaTime=.02f;
    float low=MotorcycleHandling.SteeringLimit(0),high=MotorcycleHandling.SteeringLimit(60);
    Require(low>=28&&low<=32&&high>=9&&high<low*.5f,"Steering lost parking control or high-speed limit");
    v.Speed_Kmh=60;v.lastFrameSteerAngle=50;MotorcycleService.Instance.PrepareSteering(v);
    Require(v.maxSteeringAngle==high&&v.lastFrameSteerAngle<=high&&v.steerRate<100,"Input takes abrupt full-lock steps");
    foreach(float angle in new[]{24f,-24f,100f,-100f,0f})
    {
        v.CurrentSteerAngle=angle;
        for(int i=0;i<30;i++)MotorcycleService.Instance.Update();
        var up=bike.Visual.transform.localRotation*Vector3.up;
        Require(Math.Abs(up.x)<=Math.Sin(MotorcycleHandling.MaxLean*Math.PI/180)+.0001,"Visual lean can topple bike");
    }
    Require(Math.Abs((bike.Visual.transform.localRotation*Vector3.up).x)<.0001,"Released steering never returned upright");
    v.CurrentSteerAngle=24;MotorcycleService.Instance.Update();v.IsOccupied=false;
    for(int i=0;i<20;i++)MotorcycleService.Instance.Update();
    Require(Math.Abs((bike.Visual.transform.localRotation*Vector3.up).x)<.0001,"Unoccupied bike retained cached steering lean");Time.deltaTime=dt;
});
Check("Road normals produce bounded uphill/downhill pitch with no sideways roll",()=>
{
    var v=Chassis();Require(MotorcycleService.Instance.Attach(v),"Attach failed");MotorcycleService.Instance.Ride(v);
    var wheels=v.GetComponentsInChildren<WheelCollider>(true);v.Speed_Kmh=30;
    foreach(float slope in new[]{20f,-20f,0f,55f,-55f})
    {
        float rad=slope*(float)Math.PI/180f;Physics.Hits=new[]{new RaycastHit{collider=new GameObject("Hill").AddComponent<Collider>(),distance=.35f,normal=new(0,(float)Math.Cos(rad),-(float)Math.Sin(rad))}};
        for(int i=0;i<90;i++)MotorcycleService.Instance.Stabilize(v);
        var forward=v.transform.rotation*Vector3.forward;var right=v.transform.rotation*Vector3.right;
        Require(Math.Sign(forward.y)==Math.Sign(slope)&&Math.Abs(right.y)<.0001,"Hill alignment rolled or faced wrong slope");
        Require(Math.Abs(forward.y)<=Math.Sin(40*Math.PI/180)+.0001,"Road alignment pitched past safety limit");
    }
});
Check("Airborne bikes retain safe crest pitch while damping flip and spin velocity",()=>
{
    var v=Chassis();Require(MotorcycleService.Instance.Attach(v),"Attach failed");MotorcycleService.Instance.Ride(v);
    var wheels=v.GetComponentsInChildren<WheelCollider>(true);Physics.Hits=new[]{new RaycastHit{collider=new GameObject("Crest").AddComponent<Collider>(),distance=.35f,normal=new(0,.94f,-.342f)}};
    for(int i=0;i<40;i++)MotorcycleService.Instance.Stabilize(v);var crest=v.transform.rotation;
    foreach(var wheel in wheels)wheel.isGrounded=false;v.Speed_Kmh=50;v.Rb.angularVelocity=new(6,-12,6);v.Rb.velocity=new(0,1,12);
    MotorcycleService.Instance.Stabilize(v);
    Require(v.transform.rotation.Equals(crest)&&v.Rb.angularVelocity.x==0&&v.Rb.angularVelocity.z==0,"Airborne turnover flipped or snapped pitch");
    Require(v.Rb.angularVelocity.y<0&&Math.Abs(v.Rb.angularVelocity.y)<1&&v.Rb.velocity.Equals(new Vector3(0,1,12)),"Spin guard reversed yaw or changed forward motion");
});
Check("Observers never stabilize another driver's Rigidbody, while owning clients and solo do",()=>
{
    var v=Chassis();Require(MotorcycleService.Instance.Attach(v),"Attach failed");v.IsOwner=false;v.OwnerId=2;
    v.Rb.angularVelocity=new(3,4,5);MotorcycleService.Instance.Stabilize(v);
    Require(v.Rb.Rotations==0&&v.Rb.angularVelocity.Equals(new Vector3(3,4,5)),"Observer fought remote physics");
    SessionAuthorityService.Instance.Host=false;v.IsOwner=true;MotorcycleService.Instance.Stabilize(v);
    Require(v.Rb.Rotations==1,"Owning client did not stabilize");
    v.Rb.isKinematic=true;MotorcycleService.Instance.Stabilize(v);Require(v.Rb.Rotations==1,"Kinematic observer was moved");
    v.Rb.isKinematic=false;v.IsOwner=false;v.OwnerId=-1;SessionAuthorityService.Instance.Host=true;MotorcycleService.Instance.Stabilize(v);
    Require(v.Rb.Rotations==2,"Solo/host unowned bike did not stabilize");
});
Check("Grip floors survive weather/handbrake rewrites and all tuning restores on detach",()=>
{
    var v=Chassis();float rate=v.steerRate;v.OverrideMaxSteerAngle(19f);var wheel=v.steerWheels[0];
    var original=wheel.sidewaysFriction;Require(MotorcycleService.Instance.Attach(v),"Attach failed");
    for(int i=0;i<3;i++)
    {
        wheel.sidewaysFriction=new(){stiffness=.1f,extremumValue=1.2f};wheel.forwardFriction=new(){stiffness=.1f,extremumSlip=.4f};
        MotorcycleStability.KeepGrip(wheel);
        Require(wheel.sidewaysFriction.stiffness>=2&&wheel.forwardFriction.stiffness>=1.3f&&wheel.sidewaysFriction.extremumValue==1.2f&&wheel.forwardFriction.extremumSlip==.4f,"Native curve rewrite weakened grip or lost its shape");
    }
    MotorcycleService.Instance.Detach(v);
    Require(wheel.sidewaysFriction.stiffness==original.stiffness&&v.steerRate==rate&&v.MaxSteerAngleOverridden&&v.OverriddenMaxSteerAngle==19,"Tire/steering settings leaked onto detached car");
});
Check("Physics heading survives a lagging interpolated render transform",()=>
{
    var v=Chassis();Require(MotorcycleService.Instance.Attach(v),"Attach failed");
    v.transform.rotation=Quaternion.identity;v.Rb.PhysicsRotation=Quaternion.Euler(0,90,0);
    MotorcycleService.Instance.Stabilize(v);
    Require((v.transform.rotation*Vector3.forward-Vector3.right).sqrMagnitude<.0001,"Stabilizer rolled back current native steering yaw");
});
Check("Ownership transfer on a hill starts from the received pitch rather than snapping level",()=>
{
    var v=Chassis();Require(MotorcycleService.Instance.Attach(v),"Attach failed");v.IsOwner=false;v.OwnerId=2;
    v.transform.rotation=Quaternion.Euler(-25,90,0);MotorcycleService.Instance.Stabilize(v);
    Physics.Hits=new[]{new RaycastHit{collider=new GameObject("Hill").AddComponent<Collider>(),distance=.35f,normal=new(-.422618f,.906308f,0)}};
    v.IsOwner=true;var original=v.transform.rotation*Vector3.forward;MotorcycleService.Instance.Stabilize(v);
    Require((v.transform.rotation*Vector3.forward-original).sqrMagnitude<.001,"Ownership transfer snapped hill pitch to zero");
});
Check("Ground sampling ignores bike, triggers, disabled colliders and mounted or standing players",()=>
{
    var v=Chassis();Require(MotorcycleService.Instance.Attach(v),"Attach failed");var wheel=v.steerWheels[0];
    var own=v.gameObject.AddComponent<BoxCollider>();var standing=Player.Local.gameObject.AddComponent<Collider>();
    var remote=MakePlayer(2,false);var foreign=new GameObject("Other vehicle");remote.Avatar.transform.SetParent(foreign.transform,false);
    var seated=remote.Avatar.gameObject.AddComponent<Collider>();var trigger=new GameObject("Trigger").AddComponent<Collider>();trigger.isTrigger=true;
    var disabled=new GameObject("Disabled").AddComponent<Collider>();disabled.enabled=false;
    var road=new GameObject("Road").AddComponent<Collider>();var slope=new Vector3(0,.94f,-.342f);
    Physics.Hits=new[]{own,standing,seated,trigger,disabled}.Select(c=>new RaycastHit{collider=c,distance=.05f,normal=Vector3.up})
        .Concat(new[]{new RaycastHit{collider=road,distance=.4f,normal=slope}}).ToArray();
    Require(MotorcycleGroundProbe.TryNormal(v,wheel,out var normal)&&normal.Equals(slope),"A rider or owned collider was used as terrain");
});
Check("Ground sampling selects the nearest physical surface without looking through steep obstructions",()=>
{
    var v=Chassis();Require(MotorcycleService.Instance.Attach(v),"Attach failed");var wheel=v.steerWheels[0];
    var road=new GameObject("Road").AddComponent<Collider>();var obstruction=new GameObject("Steep face").AddComponent<Collider>();
    Physics.Hits=new[]{new RaycastHit{collider=road,distance=.5f,normal=Vector3.up},new RaycastHit{collider=obstruction,distance=.2f,normal=new(0,.2f,.98f)}};
    Require(!MotorcycleGroundProbe.TryNormal(v,wheel,out _),"Probe looked through an unsuitable nearer surface");
    Physics.Hits=new[]{new RaycastHit{collider=road,distance=.5f,normal=Vector3.up},new RaycastHit{collider=obstruction,distance=.2f,normal=new(0,.8f,.6f)}};
    Require(MotorcycleGroundProbe.TryNormal(v,wheel,out var normal)&&normal.Equals(new Vector3(0,.8f,.6f)),"Unsorted raycast hits chose a farther floor");
});
Check("Missing or out-of-reach ground keeps crest pitch and adds no contact force",()=>
{
    var v=Chassis();Require(MotorcycleService.Instance.Attach(v),"Attach failed");v.transform.rotation=Quaternion.Euler(-20,0,0);v.IsOccupied=true;
    Physics.Hits=new[]{new RaycastHit{collider=new GameObject("Distant floor").AddComponent<Collider>(),distance=50,normal=Vector3.up}};
    var before=v.transform.rotation*Vector3.forward;MotorcycleService.Instance.Stabilize(v);
    Require((v.transform.rotation*Vector3.forward-before).sqrMagnitude<.00001f&&v.Rb.LastAcceleration.sqrMagnitude==0,"Absent ground snapped pitch or pulled on the bike");
    Physics.Hits=Array.Empty<RaycastHit>();Require(!MotorcycleGroundProbe.TryNormal(v,v.steerWheels[0],out _),"Empty query created a fake contact");
});
Check("Airborne and disabled wheels never invoke ground raycasts",()=>
{
    var v=Chassis();Require(MotorcycleService.Instance.Attach(v),"Attach failed");var wheel=v.steerWheels[0];int before=Physics.Raycasts;
    wheel.isGrounded=false;Require(!MotorcycleGroundProbe.TryNormal(v,wheel,out _),"Airborne contact invented");
    wheel.isGrounded=true;wheel.enabled=false;Require(!MotorcycleGroundProbe.TryNormal(v,wheel,out _),"Disabled wheel contact invented");
    Require(Physics.Raycasts==before,"Inactive wheels allocated/contact-queried each tick");
});
Check("Spawned and newly mounted motorcycles stabilize without the fatal WheelHit entry point",()=>
{
    var v=Chassis();Require(MotorcycleService.Instance.Attach(v),"Attach failed");
    for(int i=0;i<4;i++)MotorcycleService.Instance.Stabilize(v);
    MotorcycleService.Instance.Ride(v);for(int i=0;i<4;i++)MotorcycleService.Instance.Stabilize(v);
    Require(v.Rb.Rotations==8&&Physics.Raycasts==32,"Spawn or mount stability did not reach the safe raycast path");
    // Fixture GetGroundHit throws if reached. Passing establishes call avoidance,
    // not native runtime safety; the compiled DLL is also inspected independently.
});
MotorcycleRefinementChecks.Run(Check,Require,() => Chassis(),MakePlayer);
MotorcycleGripFitChecks.Run(Check,Require,() => Chassis(),MakePlayer);
MotorcycleRiderPostureChecks.Run(Check,Require,() => Chassis(),MakePlayer);
MotorcycleAudioBrakeChecks.Run(Check,Require,() => Chassis());
if(failed!=0)throw new InvalidOperationException($"{failed} checks failed; {passed} passed.");
Console.WriteLine($"{passed} local animation and motorcycle checks passed; native rendering, driving and two-player runtime QA are still required.");
if(args.Length==2&&args[0]=="--audio-preview")
{
    string folder=System.IO.Path.GetFullPath(args[1]);System.IO.Directory.CreateDirectory(folder);
    var reports=new List<object>();
    foreach(bool throttle in new[]{false,true})
    {
        Reset();var v=Chassis();Require(MotorcycleService.Instance.Attach(v),"Preview attach failed");MotorcycleService.Instance.Ride(v);
        var engine=MotorcycleService.Instance.Bikes.Single().Engine;
        string path=System.IO.Path.Combine(folder,throttle?"original-chopper-throttle.wav":"original-chopper-idle.wav");
        reports.Add(MotorcycleAudioPreview.Render(v,engine,path,throttle));Reset();
    }
    MotorcycleAudioPreview.WriteReport(System.IO.Path.Combine(folder,"preview-audit.json"),reports);
    Console.WriteLine("Rendered production-controller idle and throttle previews: "+folder);
}
