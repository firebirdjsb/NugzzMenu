// Native rendering/physics/transport are deliberately not simulated. These SDK
// fixtures exercise production state transitions, placement math and rollback.
namespace Il2CppInterop.Runtime
{
    public static class DelegateSupport
    {
        public static T ConvertDelegate<T>(Delegate d) where T:class
        {
            if(typeof(T)==typeof(UnityEngine.Camera.CameraCallback)) return (T)(object)new UnityEngine.Camera.CameraCallback(c=>d.DynamicInvoke(c));
            if(typeof(T)==typeof(Il2CppSystem.Action<UnityEngine.Rendering.ScriptableRenderContext,UnityEngine.Camera>))
                return (T)(object)new Il2CppSystem.Action<UnityEngine.Rendering.ScriptableRenderContext,UnityEngine.Camera>((a,b)=>d.DynamicInvoke(a,b));
            throw new InvalidOperationException();
        }
    }
}
namespace Il2CppSystem { public delegate void Action<T,U>(T a,U b); }
namespace UnityEngine.Rendering
{
    public struct ScriptableRenderContext { }
    public static class RenderPipelineManager
    { public static event Il2CppSystem.Action<ScriptableRenderContext,UnityEngine.Camera> beginCameraRendering; public static void Render()=>beginCameraRendering?.Invoke(new(),new()); }
}
namespace UnityEngine
{
    public class Camera : Component { public delegate void CameraCallback(Camera c);public static CameraCallback onPreCull; }
    public class Collider : Component { public bool isTrigger; }
    public class LODGroup : Component { }
    public class BoxCollider : Collider { public Vector3 center,size; }
    public class WheelCollider : Collider
    {
        public Vector3 center;public float radius=0.4f,suspensionDistance=0.3f,mass=20f;public JointSpring suspensionSpring;
        public WheelFrictionCurve forwardFriction=new(){stiffness=1},sidewaysFriction=new(){stiffness=1};
        public bool isGrounded=true;public Vector3? ContactCenter;
        public void GetWorldPose(out Vector3 position,out Quaternion rotation) {position=ContactCenter??transform.position;rotation=transform.rotation;}
        // Any reintroduction of the crashing native entry point fails fixtures.
        public bool GetGroundHit(out WheelHit hit) => throw new InvalidOperationException("Unsafe WheelHit binding was reached");
    }
    public struct WheelFrictionCurve {public float stiffness,extremumSlip,extremumValue,asymptoteSlip,asymptoteValue;}
    public struct WheelHit {public Vector3 normal;}
    public struct JointSpring { public float spring,damper,targetPosition; }
    [Flags] public enum RigidbodyConstraints { None=0,FreezeRotationX=16,FreezeRotationZ=64 }
    public enum ForceMode { Acceleration }
    public class Rigidbody : Component
    {
        public float mass=1500,angularDrag=0.05f;public Vector3 centerOfMass,angularVelocity,velocity,LastAcceleration;
        public RigidbodyConstraints constraints;public bool isKinematic,ClearAngularOnMove;public int Rotations;
        public Quaternion? PhysicsRotation;public Quaternion rotation=>PhysicsRotation??transform.rotation;
        public void MoveRotation(Quaternion rotation) {Rotations++;transform.rotation=rotation;if(ClearAngularOnMove)angularVelocity=Vector3.zero;}
        public void AddForce(Vector3 force,ForceMode mode) {if(mode!=ForceMode.Acceleration)throw new InvalidOperationException();LastAcceleration=force;}
    }
    public enum QueryTriggerInteraction { Ignore }
    public struct RaycastHit { public Collider collider;public float distance;public Vector3 point,normal; }
    public static class Physics
    {
        public static RaycastHit[] Hits; public static bool Blocked;public static int Raycasts;
        public static RaycastHit[] RaycastAll(Vector3 o,Vector3 d,float distance,int layers,QueryTriggerInteraction triggers)
        { Raycasts++;return Hits??new[]{new RaycastHit{collider=new GameObject("Ground").AddComponent<Collider>(),distance=o.y,point=new(o.x,0,o.z),normal=Vector3.up}}; }
        public static bool CheckBox(Vector3 p,Vector3 s,Quaternion r,int l,QueryTriggerInteraction t)=>Blocked;
    }
    public static class Mathf
    {
        public const float Rad2Deg=180f/(float)Math.PI;public static float Clamp01(float v)=>Math.Clamp(v,0,1);
        public static float Clamp(float v,float min,float max)=>Math.Clamp(v,min,max);
        public static float MoveTowards(float current,float target,float step)=>current+Math.Clamp(target-current,-step,step);
    }
}
namespace Il2CppFishNet.Object { public class NetworkObject : UnityEngine.Component { public int ObjectId; } }
namespace Il2CppScheduleOne.AvatarFramework.Animation
{ public class AvatarSeat : UnityEngine.Component { public UnityEngine.Transform SittingPoint; } }
namespace Il2CppScheduleOne.Interaction
{
    public class InteractableObject : UnityEngine.Component
    {
        public enum EInteractableState { Default,Disabled }
        public string Message;public EInteractableState State;
        public string message=>Message;public EInteractableState interactionState=>State;
        public UnityEngine.Transform displayLocationPoint;public UnityEngine.Collider displayLocationCollider;
        public int MessageWrites,StateWrites;
        public void SetMessage(string s){Message=s;MessageWrites++;}
        public void SetInteractableState(EInteractableState s){State=s;StateWrites++;}
    }
    public class InteractionManager {public static InteractionManager Instance;public InteractableObject HoveredInteractableObject;}
}
namespace Il2CppScheduleOne.Vehicles
{
    using UnityEngine;
    using Il2CppScheduleOne.PlayerScripts;
    using Il2CppInterop.Runtime.InteropTypes.Arrays;
    public class Motorbike : Component { public Il2CppScheduleOne.AvatarFramework.Animation.AvatarSeat Seat;public MotorbikeBurnout BurnoutController; }
    public class VehicleSeat : Component { public bool isDriverSeat; }
    public class LandVehicle : Component
    {
        public string VehicleCode="sedan",vehicleName="Sedan";public Rigidbody Rb;public BoxCollider boundingBox;
        public Il2CppReferenceArray<VehicleSeat> Seats;public Il2CppReferenceArray<WheelCollider> steerWheels;
        public float TopSpeed=30,maxSteeringAngle=35,downforce=100,diffGearing=1,Speed_Kmh,CurrentSteerAngle,currentThrottle;
        public float steerRate=120,lastFrameSteerAngle,OverriddenMaxSteerAngle;public bool MaxSteerAngleOverridden;
        public bool IsOwner=true;public int OwnerId=0;public bool LocalPlayerIsDriver;
        public void ResetMaxSteerAngle()=>MaxSteerAngleOverridden=false;
        public void OverrideMaxSteerAngle(float angle){MaxSteerAngleOverridden=true;OverriddenMaxSteerAngle=angle;}
        public float BrakeForceMultiplier=1,handBrakeForce=10000,throttleOverride,steerOverride;
        public bool overrideControls,handbrakeOverride,IsPhysicallySimulated=true;
        public bool IsOccupied,IsVisible=true,BrakesApplied,HandbrakeApplied;public Component Agent,Trunk;public Transform cameraOrigin,centerOfMass;public List<Transform> exitPoints=new();
        public Il2CppScheduleOne.Storage.StorageEntity Storage;
        public Il2CppFishNet.Object.NetworkObject NetworkObject;
        public Il2CppScheduleOne.Interaction.InteractableObject intObj;
        public int Entered;
        // Reproduce the PE-verified native avatar parenting/hiding contract, not
        // Unity wheel dynamics. This previously missing behaviour broke seating.
        public void EnterVehicle()
        {
            Entered++;IsOccupied=true;LocalPlayerIsDriver=true;Player.Local.CurrentVehicle=NetworkObject;
            Player.Local.Avatar.transform.SetParent(transform,false);
            Player.Local.Avatar.transform.localPosition=Seats[0].transform.localPosition+Vector3.up;
            Player.Local.Avatar.SetVisible(false);
        }
        public void ExitVehicle()
        {
            IsOccupied=false;LocalPlayerIsDriver=false;Player.Local.Avatar.transform.SetParent(Player.Local.transform,false);
            Player.Local.Avatar.transform.localPosition=Vector3.zero;Player.Local.Avatar.transform.localRotation=Quaternion.identity;
            Player.Local.Avatar.SetVisible(true);Player.Local.CurrentVehicle=null;
        }
        public void DestroyVehicle() {Destroyed=true;Object.Destroy(gameObject);}
    }
    public class Wheel : Component {public LandVehicle vehicle;public WheelCollider wheelCollider;}
    public class VehicleManager : Component
    {
        public List<LandVehicle> VehiclePrefabs=new(),AllVehicles=new();public bool? LastPlayerOwned;
        public Func<LandVehicle> Factory;public int SpawnCount;
        public LandVehicle SpawnAndReturnVehicle(string code,Vector3 pos,Quaternion rot,bool playerOwned)
        {LastPlayerOwned=playerOwned;SpawnCount++;var v=Factory();v.transform.SetPositionAndRotation(pos,rot);AllVehicles.Add(v);return v;}
    }
}
namespace Il2CppScheduleOne.Vehicles.Sound
{
    public class VehicleSound : UnityEngine.Component
    {
        public Il2CppScheduleOne.Audio.AudioSourceController EngineStartSource,EngineIdleSource,EngineLoopSource;
    }
}
