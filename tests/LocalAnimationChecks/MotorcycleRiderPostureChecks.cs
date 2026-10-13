using NugzzMenu.Services;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Vehicles;
using UnityEngine;

internal static class MotorcycleRiderPostureChecks
{
    private sealed class Rig
    {
        internal MotorcycleGripFitChecks.Rig Arms;
        internal Transform[] Legs;
        internal Transform LeftSole, RightSole;
        internal Action Animate;
    }
    private static Transform Joint(Transform parent,string name,Vector3 position,Quaternion? rotation=null)
    { var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;t.localRotation=rotation??Quaternion.identity;return t; }
    private static Rig NativeRig(Player p)
    {
        var arms=MotorcycleGripFitChecks.NativeArms(p);var hip=p.Avatar.HipBone;var provider=p.Avatar.AttachmentAnchorProvider;
        // Installed 0.4.7 Player rig: 0.4528m thighs, 0.4290m shins, with hip
        // pivots 0.058m below the pelvis. The baseline hangs both legs down.
        var left=Joint(hip,"mixamorig:LeftUpLeg",new(-.08087f,-.058f,.014f),Quaternion.Euler(0,0,180));
        var right=Joint(hip,"mixamorig:RightUpLeg",new(.08087f,-.058f,.014f),Quaternion.Euler(0,0,180));
        var ll=Joint(left,"mixamorig:LeftLeg",new(0,.4528f,0));var rl=Joint(right,"mixamorig:RightLeg",new(0,.4528f,0));
        var lf=Joint(ll,"mixamorig:LeftFoot",new(0,.429f,0),Quaternion.Euler(0,0,180));
        var rf=Joint(rl,"mixamorig:RightFoot",new(0,.429f,0),Quaternion.Euler(0,0,180));
        provider.LeftUpperLeg=left;provider.LeftLowerLeg=ll;provider.LeftFoot=lf;
        provider.RightUpperLeg=right;provider.RightLowerLeg=rl;provider.RightFoot=rf;
        var rig=new Rig{Arms=arms,Legs=new[]{left,ll,lf,right,rl,rf},
            LeftSole=Joint(lf,"Left shoe sole",Vector3.down*.08f),RightSole=Joint(rf,"Right shoe sole",Vector3.down*.08f)};
        var rotations=rig.Legs.Select(t=>t.localRotation).ToArray();
        rig.Animate=()=>{arms.Animate();for(int i=0;i<rig.Legs.Length;i++)rig.Legs[i].localRotation=rotations[i];};
        p.Avatar.Animation._animator.OnEvaluate=rig.Animate;return rig;
    }
    internal static void Run(Action<string,Action> check,Action<bool,string> require,Func<LandVehicle> chassis,Func<int,bool,Player> makePlayer)
    {
        check("Rider hip clears cushion by raising the player another 14cm without lifting the wheel layout",()=>
        {
            var rig=NativeRig(Player.Local);var v=chassis();require(MotorcycleService.Instance.Attach(v),"Attach failed");
            var bike=MotorcycleService.Instance.Bikes.Single();var wheels=v.GetComponentsInChildren<WheelCollider>(true).Select(w=>w.transform.localPosition).ToArray();
            MotorcycleService.Instance.Ride(v);MotorcycleService.Instance.Update();MotorcycleService.Instance.ApplyRiderPoses();
            var cushion=bike.Seat.position-v.transform.up*MotorcycleRiderTargets.PelvisLift-v.transform.forward*MotorcycleRiderTargets.PelvisForward;
            var butt=Player.Local.Avatar.HipBone.position-bike.Seat.up*.14f;
            require(MotorcycleRiderTargets.PelvisLift==.20f&&Vector3.Dot(butt-cushion,bike.Seat.up)>.04f,"Pelvis or representative butt surface still penetrates cushion");
            require(v.GetComponentsInChildren<WheelCollider>(true).Select((w,i)=>w.transform.localPosition.Equals(wheels[i])).All(x=>x)&&MotorcycleHandling.VisualClearance==.10f,"Player lift changed working bike height/contacts");
        });
        check("Hanging native legs reproduce floor penetration and now bend to both foot rests",()=>
        {
            var rig=NativeRig(Player.Local);var v=chassis();require(MotorcycleService.Instance.Attach(v),"Attach failed");MotorcycleService.Instance.Ride(v);var bike=MotorcycleService.Instance.Bikes.Single();
            Player.Local.Avatar.transform.rotation=bike.Seat.rotation;LocalAnimationPlacementService.Align(Player.Local.Avatar.transform,Player.Local.Avatar.HipBone,bike.Seat);
            require(rig.LeftSole.position.y<0&&rig.RightSole.position.y<0,"Fixture did not reproduce hanging shoes through road");
            MotorcycleService.Instance.Update();MotorcycleService.Instance.ApplyRiderPoses();
            require((rig.Legs[2].position-bike.LeftFoot.position).magnitude<.003f&&(rig.Legs[5].position-bike.RightFoot.position).magnitude<.003f,"Ankles missed foot rests");
            require(rig.LeftSole.position.y>.04f&&rig.RightSole.position.y>.04f,"Soles still pass through flat road");
            require(Vector3.Dot(rig.Legs[1].position-rig.Legs[0].position,bike.Seat.forward)>.1f&&Vector3.Dot(rig.Legs[4].position-rig.Legs[3].position,bike.Seat.forward)>.1f,"Knees do not bend forward around engine");
        });
        check("Elbows bend down/back with gentler shoulder movement while palms stay on grips",()=>
        {
            NativeRig(Player.Local);var v=chassis();require(MotorcycleService.Instance.Attach(v),"Attach failed");MotorcycleService.Instance.Ride(v);var bike=MotorcycleService.Instance.Bikes.Single();
            var a=Player.Local.Avatar.AttachmentAnchorProvider;
            foreach(float steer in new[]{-30f,0f,30f})
            {
                Time.unscaledTime+=.1f;v.CurrentSteerAngle=steer;for(int i=0;i<25;i++)MotorcycleService.Instance.Update();MotorcycleService.Instance.ApplyRiderPoses();
                foreach(var (upper,lower,hand,palm,grip,side) in new[]{(a.LeftUpperArm,a.LeftLowerArm,a.LeftHand,Player.Local.Avatar.LeftHandAlignmentPoint,bike.LeftGrip,-1f),
                    (a.RightUpperArm,a.RightLowerArm,a.RightHand,Player.Local.Avatar.RightHandAlignmentPoint,bike.RightGrip,1f)})
                {
                    var axis=(hand.position-upper.position).normalized;var bend=lower.position-upper.position-axis*Vector3.Dot(lower.position-upper.position,axis);
                    var desired=Player.Local.Avatar.transform.rotation*new Vector3(side*.2f,-1,-.25f);
                    require(Vector3.Dot(bend,desired)>0&&Vector3.Dot((lower.position-upper.position).normalized,(hand.position-lower.position).normalized)<.97f,"Elbow is upward/splayed or rigidly locked");
                    require((palm.position-grip.position).magnitude<.003f,"Relaxed elbows lost grip contact");
                }
            }
        });
        check("Owning and observed riders keep hands and feet on the bike through hill, bank and heading changes",()=>
        {
            var local=NativeRig(Player.Local);var remote=makePlayer(2,false);var observed=NativeRig(remote);
            var v=chassis();require(MotorcycleService.Instance.Attach(v),"Attach failed");MotorcycleService.Instance.Ride(v);
            remote.CurrentVehicle=v.NetworkObject;remote.Avatar.transform.SetParent(v.transform,false);var bike=MotorcycleService.Instance.Bikes.Single();
            foreach(float pitch in new[]{-30f,0f,30f})foreach(float steer in new[]{-20f,20f})
            {
                Time.unscaledTime+=.1f;v.transform.rotation=Quaternion.Euler(pitch,125,0);v.CurrentSteerAngle=steer;v.Speed_Kmh=25;
                for(int i=0;i<25;i++)MotorcycleService.Instance.Update();MotorcycleService.Instance.ApplyRiderPoses();
                foreach(var rig in new[]{local,observed})
                    require((rig.Arms.LeftPalm.position-bike.LeftGrip.position).magnitude<.003f&&(rig.Arms.RightPalm.position-bike.RightGrip.position).magnitude<.003f&&
                        (rig.Legs[2].position-bike.LeftFoot.position).magnitude<.003f&&(rig.Legs[5].position-bike.RightFoot.position).magnitude<.003f,"Hill/bank separated a rider limb from its target");
            }
        });
        check("Repeated camera passes preserve limb lengths and never move network or physics roots",()=>
        {
            var rig=NativeRig(Player.Local);var v=chassis();require(MotorcycleService.Instance.Attach(v),"Attach failed");MotorcycleService.Instance.Ride(v);
            var owned=rig.Arms.Owned.Concat(rig.Legs).ToArray();var pos=owned.Select(t=>t.localPosition).ToArray();var scale=owned.Select(t=>t.localScale).ToArray();
            var player=Player.Local.transform.position;var body=v.transform.position;MotorcycleService.Instance.Update();MotorcycleService.Instance.ApplyRiderPoses();
            var rotations=owned.Select(t=>t.localRotation).ToArray();for(int i=0;i<20;i++){MotorcycleService.Instance.ApplyRiderPoses();UnityEngine.Rendering.RenderPipelineManager.Render();}
            require(owned.Select((t,i)=>t.localPosition.Equals(pos[i])&&t.localScale.Equals(scale[i])&&(t.localRotation*Vector3.up-rotations[i]*Vector3.up).sqrMagnitude<.000001f).All(x=>x),"Riding pose accumulated or stretched limbs");
            require(Player.Local.transform.position.Equals(player)&&v.transform.position.Equals(body),"Riding pose moved a network/physics root");
        });
        check("Leg cleanup restores owned rotations and leaves native exit/ragdoll poses alone",()=>
        {
            var rig=NativeRig(Player.Local);var baseline=rig.Legs.Select(t=>t.localRotation).ToArray();var v=chassis();require(MotorcycleService.Instance.Attach(v),"Attach failed");
            MotorcycleService.Instance.Ride(v);MotorcycleService.Instance.Update();MotorcycleService.Instance.ApplyRiderPoses();MotorcycleService.Instance.ResetForScene();
            require(rig.Legs.Select((t,i)=>t.localRotation.Equals(baseline[i])).All(x=>x),"Scene cleanup left owned leg pose");
            require(MotorcycleService.Instance.Attach(v),"Reattach failed");MotorcycleService.Instance.Update();Time.unscaledTime+=.1f;MotorcycleService.Instance.ApplyRiderPoses();
            v.ExitVehicle();var native=Quaternion.Euler(15,25,35);rig.Legs[0].localRotation=native;MotorcycleService.Instance.Update();
            require(rig.Legs[0].localRotation.Equals(native),"Leg cleanup overwrote native dismount pose");
            var legPose=MotorcycleLegPose.Create(Player.Local,Player.Local.Avatar.transform);var bike=MotorcycleService.Instance.Bikes.Single();legPose.Apply(bike.LeftFoot,bike.RightFoot);
            rig.Legs[0].localRotation=native;Player.Local.IsRagdolled=true;legPose.Release(true);require(rig.Legs[0].localRotation.Equals(native),"Leg cleanup overwrote ragdoll");
        });
        check("Appearance replacement binds current leg anchors and leaves the old rig unchanged",()=>
        {
            var old=NativeRig(Player.Local);var pose=MotorcycleLegPose.Create(Player.Local,Player.Local.Avatar.transform);var current=NativeRig(Player.Local);
            require(!pose.IsCurrent,"Stale leg provider accepted");var rotations=old.Legs.Select(t=>t.localRotation).ToArray();var v=chassis();require(MotorcycleService.Instance.Attach(v),"Attach failed");
            MotorcycleService.Instance.Ride(v);MotorcycleService.Instance.Update();MotorcycleService.Instance.ApplyRiderPoses();var bike=MotorcycleService.Instance.Bikes.Single();
            require((current.Legs[2].position-bike.LeftFoot.position).magnitude<.003f&&old.Legs.Select((t,i)=>t.localRotation.Equals(rotations[i])).All(x=>x),"New clothes used stale leg bones");
        });
        check("Missing, crossed, degenerate and unreachable legs fail safely without skeleton translation",()=>
        {
            require(MotorcycleLegPose.Create(Player.Local,Player.Local.Avatar.transform)==null,"Missing rig accepted");var rig=NativeRig(Player.Local);var provider=Player.Local.Avatar.AttachmentAnchorProvider;
            provider.LeftFoot=provider.RightFoot;require(MotorcycleLegPose.Create(Player.Local,Player.Local.Avatar.transform)==null,"Crossed leg chain accepted");provider.LeftFoot=rig.Legs[2];
            rig.Legs[1].localPosition=Vector3.zero;var pose=MotorcycleLegPose.Create(Player.Local,Player.Local.Avatar.transform);
            var target=Joint(Player.Local.Avatar.transform,"Distant target",new(100,100,100));var pos=rig.Legs.Select(t=>t.localPosition).ToArray();pose.Apply(target,target);
            require(rig.Legs.Select((t,i)=>t.localPosition.Equals(pos[i])&&float.IsFinite(t.localRotation.x+t.localRotation.y+t.localRotation.z+t.localRotation.w)).All(x=>x),"Invalid/unreachable leg was stretched or corrupted");
        });
    }
}
