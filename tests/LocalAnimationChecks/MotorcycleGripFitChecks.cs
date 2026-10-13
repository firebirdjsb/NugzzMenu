using NugzzMenu.Services;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Vehicles;
using UnityEngine;

internal static class MotorcycleGripFitChecks
{
    internal sealed class Rig
    {
        internal Transform[] Owned;
        internal Transform LeftPalm, RightPalm, Spine;
        internal Action Animate;
    }
    private static Transform Joint(Transform parent, string name, Vector3 position, Quaternion? rotation = null)
    {
        var t = new GameObject(name).transform; t.SetParent(parent, false);
        t.localPosition = position; t.localRotation = rotation ?? Quaternion.identity; return t;
    }
    internal static Rig NativeArms(Player p)
    {
        // Metre-sized geometry measured from the installed Player avatar's
        // serialized rig: 0.2515m upper arm, 0.2596m forearm, 0.1024m clavicle.
        var hip = p.Avatar.HipBone; var spine = Joint(hip,"mixamorig:Spine",new(0,.1043f,0));
        var left = Joint(spine,"mixamorig:LeftShoulder",new(-.0467f,.4002f,0));
        var right = Joint(spine,"mixamorig:RightShoulder",new(.0467f,.4002f,0));
        var lu = Joint(left,"mixamorig:LeftArm",new(-.0968f,-.0337f,0),Quaternion.Euler(0,0,180));
        var ru = Joint(right,"mixamorig:RightArm",new(.0968f,-.0337f,0),Quaternion.Euler(0,0,180));
        var ll = Joint(lu,"mixamorig:LeftForeArm",new(0,.2515f,0)); var rl = Joint(ru,"mixamorig:RightForeArm",new(0,.2515f,0));
        var lh = Joint(ll,"mixamorig:LeftHand",new(0,.2596f,0)); var rh = Joint(rl,"mixamorig:RightHand",new(0,.2596f,0));
        var owned = new List<Transform> { spine,left,right,lu,ru,ll,rl,lh,rh };
        foreach (var (hand,side) in new[]{(lh,"Left"),(rh,"Right")})
        {
            var index = hand; var thumb = hand;
            for (int i=1;i<=4;i++)
            {
                index = Joint(index,"mixamorig:"+side+"HandIndex"+i,new(0,i==1?.075f:.025f,0));
                thumb = Joint(thumb,"mixamorig:"+side+"HandThumb"+i,new(i==1?.023f:0,i==1?.035f:.025f,0));
                owned.Add(index); owned.Add(thumb);
            }
        }
        var rig = new Rig { Owned=owned.ToArray(), Spine=spine,
            LeftPalm=Joint(lh,"AlignmentPoint",new(0,.0497f,.0138f),Quaternion.Euler(0,0,-90)),
            RightPalm=Joint(rh,"AlignmentPoint",new(0,.0497f,.0138f),Quaternion.Euler(0,0,90)) };
        var rotations = rig.Owned.Select(t=>t.localRotation).ToArray();
        rig.Animate=()=>{ for(int i=0;i<rig.Owned.Length;i++)rig.Owned[i].localRotation=rotations[i]; };
        p.Avatar.AttachmentAnchorProvider=new(){ Hips=hip, LowerSpine=spine, LeftShoulder=left, RightShoulder=right,
            LeftUpperArm=lu,RightUpperArm=ru,LeftLowerArm=ll,RightLowerArm=rl,LeftHand=lh,RightHand=rh };
        p.Avatar.LeftHandAlignmentPoint=rig.LeftPalm; p.Avatar.RightHandAlignmentPoint=rig.RightPalm;
        p.Avatar.Animation._animator.OnEvaluate=rig.Animate; return rig;
    }

    internal static void Run(Action<string,Action> check, Action<bool,string> require, Func<LandVehicle> chassis, Func<int,bool,Player> makePlayer)
    {
        check("Real short arms reproduce the old unreachable-grip gap and now reach in both parking directions",()=>
        {
            var rig=NativeArms(Player.Local); var v=chassis(); require(MotorcycleService.Instance.Attach(v),"Attach failed");
            MotorcycleService.Instance.Ride(v); var bike=MotorcycleService.Instance.Bikes.Single(); var anchors=Player.Local.Avatar.AttachmentAnchorProvider;
            Player.Local.Avatar.transform.rotation=bike.Seat.rotation;LocalAnimationPlacementService.Align(Player.Local.Avatar.transform,Player.Local.Avatar.HipBone,bike.Seat);
            float length=(anchors.LeftLowerArm.position-anchors.LeftUpperArm.position).magnitude+(anchors.LeftHand.position-anchors.LeftLowerArm.position).magnitude;
            require(length>.50f&&length<.52f&&(bike.LeftGrip.position-anchors.LeftUpperArm.position).magnitude>length,"Fixture did not reproduce the actual short-arm boundary");
            foreach(float steer in new[]{-30f,-15f,0f,15f,30f})
            {
                Time.unscaledTime+=.1f;v.CurrentSteerAngle=steer;v.Speed_Kmh=0;
                for(int i=0;i<25;i++)MotorcycleService.Instance.Update();MotorcycleService.Instance.ApplyRiderPoses();
                float left=(rig.LeftPalm.position-bike.LeftGrip.position).magnitude,right=(rig.RightPalm.position-bike.RightGrip.position).magnitude;
                var upper=anchors.RightUpperArm.position-Player.Local.Avatar.HipBone.position;
                var goal=anchors.RightHand.position+(bike.RightGrip.position-rig.RightPalm.position)-Player.Local.Avatar.HipBone.position;
                require(left<.003f&&right<.003f,$"Short arm still misses grip at {steer}: left={left:F4}, right={right:F4}; upper=({upper.x:F3},{upper.y:F3},{upper.z:F3}), wrist=({goal.x:F3},{goal.y:F3},{goal.z:F3})");
                require((Player.Local.Avatar.HipBone.position-bike.Seat.position).sqrMagnitude<.000001f,"Reach correction lifted hips off saddle");
            }
        });
        check("Real hand alignment positions and mirrored frames follow hills, bank and observer riding",()=>
        {
            var local=NativeArms(Player.Local);var remote=makePlayer(2,false);var observed=NativeArms(remote);
            var v=chassis();require(MotorcycleService.Instance.Attach(v),"Attach failed");MotorcycleService.Instance.Ride(v);
            remote.CurrentVehicle=v.NetworkObject;remote.Avatar.transform.SetParent(v.transform,false);var bike=MotorcycleService.Instance.Bikes.Single();
            foreach(float yaw in new[]{0f,95f,210f})foreach(float steer in new[]{-20f,20f})
            {
                Time.unscaledTime+=.1f;v.Speed_Kmh=30;v.CurrentSteerAngle=steer;v.transform.rotation=Quaternion.Euler(-25,yaw,0);
                for(int i=0;i<25;i++)MotorcycleService.Instance.Update();MotorcycleService.Instance.ApplyRiderPoses();
                foreach(var rig in new[]{local,observed})
                    require((rig.LeftPalm.position-bike.LeftGrip.position).magnitude<.003f&&(rig.RightPalm.position-bike.RightGrip.position).magnitude<.003f&&
                        (rig.LeftPalm.forward-bike.LeftGrip.forward).sqrMagnitude<.000001f&&(rig.RightPalm.up-bike.RightGrip.up).sqrMagnitude<.000001f,"Actual palm offset/frame missed moving bars");
            }
        });
        check("Grip pose curls current finger clusters and retains native thumb splay",()=>
        {
            var rig=NativeArms(Player.Local);var v=chassis();require(MotorcycleService.Instance.Attach(v),"Attach failed");MotorcycleService.Instance.Ride(v);
            var thumb=rig.Owned.First(t=>t.name.EndsWith("LeftHandThumb1"));thumb.localRotation=Quaternion.Euler(0,0,-30);
            var fingers=new MotorcycleFingerPose(Player.Local.Avatar.AttachmentAnchorProvider.LeftHand);fingers.Capture();fingers.Apply();
            var index=rig.Owned.First(t=>t.name.EndsWith("LeftHandIndex2"));
            require((index.localRotation*Vector3.up).z>.8f&&(thumb.localRotation*Vector3.up).x>.4f,"Finger curl or thumb splay absent");
            fingers.Restore();require(index.localRotation.Equals(Quaternion.identity)&&(thumb.localRotation*Vector3.up).x>.4f,"Native fingers not restored");
        });
        check("Reach and finger poses stay idempotent without changing limb lengths, network roots or physics",()=>
        {
            var rig=NativeArms(Player.Local);var v=chassis();require(MotorcycleService.Instance.Attach(v),"Attach failed");MotorcycleService.Instance.Ride(v);
            var positions=rig.Owned.Select(t=>t.localPosition).ToArray();var scales=rig.Owned.Select(t=>t.localScale).ToArray();var root=Player.Local.transform.position;var body=v.transform.position;
            v.CurrentSteerAngle=30;for(int i=0;i<25;i++)MotorcycleService.Instance.Update();MotorcycleService.Instance.ApplyRiderPoses();
            var rotations=rig.Owned.Select(t=>t.localRotation).ToArray();
            for(int i=0;i<10;i++){MotorcycleService.Instance.ApplyRiderPoses();UnityEngine.Rendering.RenderPipelineManager.Render();}
            require(rig.Owned.Select((t,i)=>t.localPosition.Equals(positions[i])&&t.localScale.Equals(scales[i])&&
                (t.localRotation*Vector3.up-rotations[i]*Vector3.up).sqrMagnitude<.000001f).All(x=>x),"Reach/curl accumulated or stretched skeleton");
            require(Player.Local.transform.position.Equals(root)&&v.transform.position.Equals(body),"Reach correction moved a network/physics root");
        });
        check("Torso, clavicle and fingers restore owned poses while native dismount keeps its new pose",()=>
        {
            var rig=NativeArms(Player.Local);var baseline=rig.Owned.Select(t=>t.localRotation).ToArray();
            var v=chassis();require(MotorcycleService.Instance.Attach(v),"Attach failed");MotorcycleService.Instance.Ride(v);MotorcycleService.Instance.Update();MotorcycleService.Instance.ApplyRiderPoses();
            MotorcycleService.Instance.ResetForScene();require(rig.Owned.Select((t,i)=>t.localRotation.Equals(baseline[i])).All(x=>x),"Owned upper-body/fingers survived scene cleanup");
            require(MotorcycleService.Instance.Attach(v),"Reattach failed");MotorcycleService.Instance.Update();Time.unscaledTime+=.1f;MotorcycleService.Instance.ApplyRiderPoses();
            v.ExitVehicle();rig.Spine.localRotation=Quaternion.Euler(10,20,30);var native=rig.Spine.localRotation;
            MotorcycleService.Instance.Update();require(rig.Spine.localRotation.Equals(native),"Dismount restored stale seated torso");
        });
        check("Replacing a shoulder/spine anchor invalidates the owned current rig",()=>
        {
            var rig=NativeArms(Player.Local);var pose=MotorcycleHandPose.Create(Player.Local,Player.Local.Avatar.transform);require(pose.IsCurrent,"Current rig rejected");
            Player.Local.Avatar.AttachmentAnchorProvider.LeftShoulder=Joint(Player.Local.Avatar.transform,"Replacement",Vector3.zero);
            require(!pose.IsCurrent,"Stale clavicle accepted");
        });
        check("Additional clearance raises the visible rider and bike together with unchanged saddle alignment",()=>
        {
            NativeArms(Player.Local);var v=chassis();require(MotorcycleService.Instance.Attach(v),"Attach failed");MotorcycleService.Instance.Ride(v);MotorcycleService.Instance.Update();
            var bike=MotorcycleService.Instance.Bikes.Single();foreach(var w in v.GetComponentsInChildren<WheelCollider>(true))w.ContactCenter=new(0,.2f,0);
            for(int i=0;i<25;i++)MotorcycleService.Instance.ApplyRiderPoses();
            var front=bike.Visual.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Wheel_Front");
            require(Math.Abs((front.position.y+bike.RearWheel.position.y)*.5f-.3f)<.00001f,"Additional 5cm clearance was canceled");
            require((Player.Local.Avatar.HipBone.position-bike.Seat.position).sqrMagnitude<.000001f,"Raised bike left rider behind");
        });
    }
}
