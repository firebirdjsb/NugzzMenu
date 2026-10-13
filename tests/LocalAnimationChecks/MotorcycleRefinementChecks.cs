using NugzzMenu.Services;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Vehicles;
using UnityEngine;

internal static class MotorcycleRefinementChecks
{
    private sealed class Rig
    {
        internal Transform[] Bones;
        internal Transform LeftPalm, RightPalm;
        internal Action Animate;
    }
    private static Transform Joint(Transform parent, string name, Vector3 position)
    { var joint = new GameObject(name).transform; joint.SetParent(parent, false); joint.localPosition = position; return joint; }
    private static Rig Arms(Player player)
    {
        Transform root = player.Avatar.transform;
        Transform left = Joint(root, "LeftArm", new(-.18f,.65f,0)), right = Joint(root, "RightArm", new(.18f,.65f,0));
        Transform leftLower = Joint(left, "LeftForeArm", new(-.15f,-.18f,.42f)), rightLower = Joint(right, "RightForeArm", new(.15f,-.18f,.42f));
        Transform leftHand = Joint(leftLower, "LeftHand", new(-.12f,0,.4f)), rightHand = Joint(rightLower, "RightHand", new(.12f,0,.4f));
        player.Avatar.AttachmentAnchorProvider = new() { Hips = player.Avatar.HipBone, LeftUpperArm = left, LeftLowerArm = leftLower,
            LeftHand = leftHand, RightUpperArm = right, RightLowerArm = rightLower, RightHand = rightHand };
        var rig = new Rig { Bones = new[]{left,leftLower,leftHand,right,rightLower,rightHand},
            LeftPalm = Joint(leftHand,"LeftPalm",new(0,0,.045f)), RightPalm = Joint(rightHand,"RightPalm",new(0,0,.045f)) };
        player.Avatar.LeftHandAlignmentPoint = rig.LeftPalm; player.Avatar.RightHandAlignmentPoint = rig.RightPalm;
        rig.Animate = () => { foreach (Transform bone in rig.Bones) bone.localRotation = Quaternion.identity; };
        player.Avatar.Animation._animator.OnEvaluate = rig.Animate;
        return rig;
    }
    internal static void Run(Action<string,Action> check, Action<bool,string> require, Func<LandVehicle> chassis, Func<int,bool,Player> player)
    {
        check("Display clearance persists through suspension corrections without changing contact geometry", () =>
        {
            var v = chassis(); require(MotorcycleService.Instance.Attach(v), "Attach failed"); var bike = MotorcycleService.Instance.Bikes.Single();
            var root = v.transform.position; var wheels = v.GetComponentsInChildren<WheelCollider>(true);
            var contacts = wheels.Select(w => w.transform.localPosition).ToArray();
            foreach (var w in wheels) w.ContactCenter = new(0,.15f,0);
            for (int i = 0; i < 30; i++) MotorcycleService.Instance.ApplyRiderPoses();
            var front = bike.Visual.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Wheel_Front");
            float shown = (bike.RearWheel.position.y + front.position.y) * .5f;
            require(Math.Abs(shown - .15f - MotorcycleHandling.VisualClearance) < .00001f, "Suspension canceled or accumulated the clearance");
            require(v.transform.position.Equals(root) && wheels.Select((w,i) => w.transform.localPosition.Equals(contacts[i]) && w.radius == .35f).All(x => x), "Display lift moved the working physics");
        });
        check("Grips belong to the moving handlebars and follow steering, hills and display bank", () =>
        {
            var v = chassis(); require(MotorcycleService.Instance.Attach(v), "Attach failed"); var bike = MotorcycleService.Instance.Bikes.Single();
            require(bike.LeftGrip.parent.name == "Chopper_Handlebars01_LOD0" && bike.RightGrip.parent == bike.LeftGrip.parent, "Targets float independently of bars");
            require(v.transform.InverseTransformPoint(bike.LeftGrip.position).x < v.transform.InverseTransformPoint(bike.RightGrip.position).x, "Left/right grips reversed");
            var local = bike.LeftGrip.localPosition; var before = bike.LeftGrip.position;
            v.IsOccupied = true; v.Speed_Kmh = 20; v.CurrentSteerAngle = 25;
            for (int i = 0; i < 30; i++) MotorcycleService.Instance.Update();
            v.transform.rotation = Quaternion.Euler(-20,75,0);
            require(bike.LeftGrip.localPosition.Equals(local) && (bike.LeftGrip.position - before).sqrMagnitude > .01f &&
                (bike.LeftGrip.position-bike.LeftGrip.parent.TransformPoint(local)).sqrMagnitude<.000001f, "Grip did not follow fork/chassis");
        });
        check("Local and observed riders keep both palms on moving grips after native animation evaluation", () =>
        {
            var local = Arms(Player.Local); var remote = player(2,false); var observed = Arms(remote);
            var v = chassis(); require(MotorcycleService.Instance.Attach(v), "Attach failed"); MotorcycleService.Instance.Ride(v);
            remote.CurrentVehicle = v.NetworkObject; remote.Avatar.transform.SetParent(v.transform,false);
            var bike = MotorcycleService.Instance.Bikes.Single(); v.Speed_Kmh = 20;
            foreach (float yaw in new[]{0f,85f,190f}) foreach(float steer in new[]{-25f,0f,25f})
            {
                Time.unscaledTime += .1f; v.transform.rotation = Quaternion.Euler(-20,yaw,0); v.CurrentSteerAngle = steer;
                for (int i = 0; i < 30; i++) MotorcycleService.Instance.Update();
                MotorcycleService.Instance.ApplyRiderPoses();
                foreach (var rig in new[]{local,observed})
                    require((rig.LeftPalm.position - bike.LeftGrip.position).sqrMagnitude < .00001f &&
                        (rig.RightPalm.position - bike.RightGrip.position).sqrMagnitude < .00001f, "Palm missed a moving grip");
            }
        });
        check("Repeated camera passes neither accumulate hand twist nor change bone positions/scales", () =>
        {
            var rig = Arms(Player.Local); var v = chassis(); require(MotorcycleService.Instance.Attach(v), "Attach failed"); MotorcycleService.Instance.Ride(v);
            var positions = rig.Bones.Select(b => b.localPosition).ToArray(); var scales = rig.Bones.Select(b => b.localScale).ToArray(); var playerRoot = Player.Local.transform.position;
            v.CurrentSteerAngle = 15; v.Speed_Kmh = 20; MotorcycleService.Instance.Update(); MotorcycleService.Instance.ApplyRiderPoses();
            var rotations = rig.Bones.Select(b => b.localRotation).ToArray();
            for (int i = 0; i < 10; i++) { MotorcycleService.Instance.ApplyRiderPoses(); UnityEngine.Rendering.RenderPipelineManager.Render(); }
            require(rig.Bones.Select((b,i) => (b.localRotation * Vector3.up - rotations[i] * Vector3.up).sqrMagnitude < .000001f &&
                b.localPosition.Equals(positions[i]) && b.localScale.Equals(scales[i])).All(x => x), "Camera callbacks accumulated or stretched a pose");
            require(Player.Local.transform.position.Equals(playerRoot), "Hand solver moved network root");
        });
        check("Palm orientation matches its grip frame instead of preserving an arbitrary animated wrist", () =>
        {
            var rig = Arms(Player.Local); var v = chassis(); require(MotorcycleService.Instance.Attach(v), "Attach failed"); MotorcycleService.Instance.Ride(v);
            v.IsOccupied = true; v.CurrentSteerAngle = 25; v.Speed_Kmh = 20;
            for (int i = 0; i < 30; i++) MotorcycleService.Instance.Update(); MotorcycleService.Instance.ApplyRiderPoses();
            var bike = MotorcycleService.Instance.Bikes.Single();
            require((rig.LeftPalm.forward-bike.LeftGrip.forward).sqrMagnitude < .000001f &&
                (rig.RightPalm.up-bike.RightGrip.up).sqrMagnitude < .000001f, "Palm orientation ignored its mirrored grip frame");
        });
        check("Out-of-reach grips keep limb lengths and produce finite joint rotations", () =>
        {
            var rig = Arms(Player.Local); var pose = MotorcycleHandPose.Create(Player.Local, Player.Local.Avatar.transform);
            var left = Joint(nullRoot(),"Far left",new(-100,100,100)); var right = Joint(left.parent,"Far right",new(100,100,100));
            var positions = rig.Bones.Select(b => b.localPosition).ToArray(); pose.Apply(left,right,Quaternion.identity);
            require(rig.Bones.Select((b,i) => b.localPosition.Equals(positions[i]) && float.IsFinite(b.localRotation.x+b.localRotation.y+b.localRotation.z+b.localRotation.w)).All(x => x), "Impossible reach stretched or corrupted rig");
        });
        check("Appearance replacement discards stale hand bones and binds the new rig", () =>
        {
            var old = Arms(Player.Local); var stale = MotorcycleHandPose.Create(Player.Local, Player.Local.Avatar.transform); var current = Arms(Player.Local);
            var oldRotations = old.Bones.Select(b => b.localRotation).ToArray(); require(!stale.IsCurrent, "Stale provider accepted");
            var v = chassis(); require(MotorcycleService.Instance.Attach(v), "Attach failed"); MotorcycleService.Instance.Ride(v); MotorcycleService.Instance.Update(); MotorcycleService.Instance.ApplyRiderPoses();
            var bike = MotorcycleService.Instance.Bikes.Single();
            require((current.LeftPalm.position - bike.LeftGrip.position).sqrMagnitude < .00001f && old.Bones.Select((b,i) => b.localRotation.Equals(oldRotations[i])).All(x => x), "Current clothes used stale bones");
        });
        check("Missing, unrelated and degenerate arm chains fail without moving the avatar", () =>
        {
            require(MotorcycleHandPose.Create(Player.Local, Player.Local.Avatar.transform) == null, "Missing anchors accepted");
            var rig = Arms(Player.Local); var provider = Player.Local.Avatar.AttachmentAnchorProvider; provider.LeftHand = provider.RightHand;
            require(MotorcycleHandPose.Create(Player.Local, Player.Local.Avatar.transform) == null, "Crossed arm chain accepted");
            provider.LeftHand = rig.Bones[2]; rig.Bones[1].localPosition = Vector3.zero;
            var pose = MotorcycleHandPose.Create(Player.Local,Player.Local.Avatar.transform); var grip = Joint(nullRoot(),"Grip",new(0,1,1));
            pose.Apply(grip,grip,Quaternion.identity); require(rig.Bones[0].localRotation.Equals(Quaternion.identity), "Zero-length arm was rotated");
        });
        check("Hand release restores only its current riding pose and preserves native exit/ragdoll", () =>
        {
            var rig = Arms(Player.Local); var pose = MotorcycleHandPose.Create(Player.Local,Player.Local.Avatar.transform);
            var grip = Joint(nullRoot(),"Grip",new(.2f,.7f,.5f)); pose.Apply(grip,grip,Quaternion.identity); pose.Release(true);
            require(rig.Bones.All(b => b.localRotation.Equals(Quaternion.identity)), "Owned render pose survived release");
            pose.Apply(grip,grip,Quaternion.identity); var native = Quaternion.Euler(20,40,60); rig.Bones[0].localRotation = native;
            pose.Release(false); require(rig.Bones[0].localRotation.Equals(native), "Release clobbered native exit");
            pose.Apply(grip,grip,Quaternion.identity); rig.Bones[0].localRotation = native; Player.Local.IsRagdolled = true; pose.Release(true);
            require(rig.Bones[0].localRotation.Equals(native), "Release clobbered ragdoll");
        });
        check("Steering retains ordinary native yaw momentum while a hill rotation clears angular velocity", () =>
        {
            var v = chassis(); require(MotorcycleService.Instance.Attach(v), "Attach failed"); v.Rb.ClearAngularOnMove = true; v.Speed_Kmh = 20;
            foreach (float yaw in new[]{-.5f,0f,.5f})
            { v.Rb.angularVelocity = new(4,yaw,5); MotorcycleService.Instance.Stabilize(v); require(v.Rb.angularVelocity.Equals(Vector3.up * yaw), "Pitch correction swallowed native yaw"); }
            v.Rb.angularVelocity = new(4,50,5); MotorcycleService.Instance.Stabilize(v); require(v.Rb.angularVelocity.y > 0 && v.Rb.angularVelocity.y < 1.5f, "Spin limit was removed");
        });
        check("Steering has useful town-speed range, smooth fork travel and safe high-speed limits", () =>
        {
            require(MotorcycleHandling.SteeringLimit(30) > 18 && MotorcycleHandling.SteeringLimit(60) < 12 && MotorcycleHandling.SteeringLimit(120) <= 10, "Town steering still stiff or highway steering unsafe");
            var v = chassis(); require(MotorcycleService.Instance.Attach(v), "Attach failed"); var bike = MotorcycleService.Instance.Bikes.Single();
            v.IsOccupied = true; v.Speed_Kmh = 0; v.CurrentSteerAngle = 30; float dt = Time.deltaTime; Time.deltaTime = .02f;
            var before = bike.Handling.SteeringDeltaWorld * Vector3.forward; MotorcycleService.Instance.Update(); var first = bike.Handling.SteeringDeltaWorld * Vector3.forward;
            require((first-before).sqrMagnitude > 0 && (first-before).sqrMagnitude < .01f, "Fork jumped straight to lock");
            for (int i = 0; i < 20; i++) MotorcycleService.Instance.Update(); var lockDirection = bike.Handling.SteeringDeltaWorld * Vector3.forward;
            require((lockDirection-first).sqrMagnitude > .1f, "Fork did not reach useful parking lock"); Time.deltaTime = dt;
        });
        check("Engine fades in and out without loop restarts or abrupt throttle pitch jumps", () =>
        {
            var v = chassis(); require(MotorcycleService.Instance.Attach(v), "Attach failed"); var audio = MotorcycleService.Instance.Bikes.Single().Engine.Source; float dt = Time.deltaTime; Time.deltaTime = .02f;
            MotorcycleService.Instance.Ride(v); MotorcycleService.Instance.Update(); float start = audio.volume;
            for (int i = 0; i < 20; i++) MotorcycleService.Instance.Update(); float idle = audio.volume; float pitch = audio.pitch;
            require(start > 0 && start < idle && audio.PlayCalls == 1, "Entry popped or repeatedly restarted");
            v.currentThrottle = 1; v.Speed_Kmh = 35; MotorcycleService.Instance.Update();
            require(audio.pitch > pitch && audio.pitch-pitch < .12f, "Throttle pitch jumped abruptly");
            for (int i = 0; i < 60; i++) MotorcycleService.Instance.Update(); float loud = audio.volume;
            v.ExitVehicle(); MotorcycleService.Instance.Update(); require(audio.isPlaying && audio.volume > 0 && audio.volume < loud, "Exit did not fade");
            MotorcycleService.Instance.Ride(v); MotorcycleService.Instance.Update(); require(audio.PlayCalls == 1, "Re-entering during fade restarted loop");
            v.ExitVehicle(); for (int i = 0; i < 30; i++) MotorcycleService.Instance.Update(); require(!audio.isPlaying && audio.volume == 0, "Exit left loop playing"); Time.deltaTime = dt;
        });
        check("Engine response is frame-rate independent with bounded reverse, coasting and cleanup", () =>
        {
            var v = chassis(); require(MotorcycleService.Instance.Attach(v), "Attach failed"); var engine = MotorcycleService.Instance.Bikes.Single().Engine; var audio = engine.Source;
            float dt = Time.deltaTime; v.IsOccupied = true; v.Speed_Kmh = 35; v.currentThrottle = 1;
            engine.Stop(); Time.deltaTime = .01f; for (int i = 0; i < 100; i++) engine.Update(); float fine = audio.pitch;
            engine.Stop(); Time.deltaTime = .05f; for (int i = 0; i < 20; i++) engine.Update(); require(Math.Abs(audio.pitch-fine) < .012f, "RPM response varies with frame rate");
            v.Speed_Kmh = -5; v.currentThrottle = -1; for (int i = 0; i < 20; i++) engine.Update(); float reverse = audio.pitch;
            v.Speed_Kmh = v.currentThrottle = 0; for (int i = 0; i < 40; i++) engine.Update(); require(audio.pitch < reverse && audio.pitch >= .5f && audio.pitch <= 2.5f, "Reverse/coasting pitch invalid");
            require(audio.spatialBlend == 1 && audio.maxDistance <= 60 && audio.dopplerLevel < .5f, "Engine spatial mix is excessive");
            engine.Stop(); require(!audio.isPlaying && audio.volume == 0, "Cleanup used a lingering fade"); Time.deltaTime = dt;
        });
    }
    private static Transform nullRoot() => new GameObject("Test targets").transform;
}
