// Focused fake SDK: exercises production ownership/cleanup code. It does not
// simulate Unity rendering, native animation playback, or FishNet packet delivery.
namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class)] public class HarmonyPatch : Attribute
    { public HarmonyPatch(Type type, string method, Type[] args) { } public HarmonyPatch(Type type,string method) { } }
}
namespace Il2CppInterop.Runtime.InteropTypes.Arrays
{
    public class Il2CppStructArray<T> : System.Collections.Generic.List<T> where T:struct
    { public Il2CppStructArray(T[] data):base(data) { } }
    public class Il2CppReferenceArray<T> : System.Collections.Generic.List<T> where T : class
    { public Il2CppReferenceArray(int size) { for(int i=0;i<size;i++) Add(null); } public Il2CppReferenceArray(T[] data):base(data){} public int Length=>Count; }
}
namespace UnityEngine
{
    public class Object
    {
        private static int _next; private readonly int _id=++_next;
        public string name; public bool Destroyed;
        public int GetInstanceID()=>_id;
        public static void Destroy(Object o) { o.Destroyed=true; }
        public static T FindObjectOfType<T>() where T:Object=>Resources.Assets.OfType<T>().FirstOrDefault();
        public static GameObject Instantiate(GameObject original,Transform parent,bool worldPositionStays)=>original.Copy(parent);
        public T TryCast<T>() where T:Object=>this as T;
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform=>gameObject.transform;
        public bool enabled=true;
        public T GetComponent<T>() where T:Component=>gameObject.GetComponent<T>();
        public T GetComponentInParent<T>(bool includeInactive=false) where T:Component => GetComponent<T>() ?? transform.parent?.GetComponentInParent<T>(includeInactive);
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T:Component=>gameObject.GetComponentsInChildren<T>(includeInactive);
    }
    public class GameObject : Object
    {
        private readonly List<Component> _components=new();
        public readonly Transform transform;
        public bool activeSelf=true;
        public int layer;
        public bool activeInHierarchy=>activeSelf&&(transform.parent?.gameObject.activeInHierarchy??true);
        public void SetActive(bool active)=>activeSelf=active;
        public GameObject(string label="") { name=label; transform=new Transform { gameObject=this }; _components.Add(transform); }
        public T AddComponent<T>() where T:Component,new() { var c=new T { gameObject=this }; _components.Add(c); return c; }
        public T GetComponent<T>() where T:Component => _components.OfType<T>().FirstOrDefault();
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T:Component => _components.OfType<T>().Concat(transform.Children.SelectMany(c=>c.gameObject.GetComponentsInChildren<T>(includeInactive))).ToArray();
        internal GameObject Copy(Transform parent)
        {
            var copy=new GameObject(name);copy.transform.SetParent(parent,false);
            copy.transform.localPosition=transform.localPosition;copy.transform.localRotation=transform.localRotation;copy.transform.localScale=transform.localScale;
            foreach(var c in _components.Where(c=>c is not Transform))
            {
                var clone=(Component)Activator.CreateInstance(c.GetType());clone.gameObject=copy;copy._components.Add(clone);
                foreach(var f in c.GetType().GetFields())if(f.Name!="gameObject")f.SetValue(clone,f.GetValue(c));
            }
            foreach(var child in transform.Children)child.gameObject.Copy(copy.transform);
            return copy;
        }
    }
    public class Transform : Component
    {
        public Transform Parent; public List<Transform> Children=new();
        public Transform parent=>Parent;
        public Vector3 localPosition,localScale=Vector3.one;
        public Vector3 position { get=>Parent==null?localPosition:Parent.TransformPoint(localPosition);set=>localPosition=Parent==null?value:Parent.InverseTransformPoint(value); }
        public Quaternion localRotation=Quaternion.identity;
        public Quaternion rotation {get=>Parent==null?localRotation:Parent.rotation*localRotation;set=>localRotation=Parent==null?value:Quaternion.Inverse(Parent.rotation)*value;}
        public Vector3 lossyScale=>Parent==null?localScale:Scale(Parent.lossyScale,localScale);
        static Vector3 Scale(Vector3 a,Vector3 b)=>new(a.x*b.x,a.y*b.y,a.z*b.z);
        public Vector3 TransformPoint(Vector3 value)=>position+rotation*Scale(value,lossyScale);
        public Vector3 InverseTransformPoint(Vector3 value)=>InverseTransformVector(value-position);
        public Vector3 forward=>rotation*new Vector3(0,0,1); public Vector3 right=>rotation*new Vector3(1,0,0);
        public Vector3 eulerAngles=>new();
        public Vector3 up=>rotation*Vector3.up;
        public Vector3 InverseTransformDirection(Vector3 value)=>Quaternion.Inverse(rotation)*value;
        public Vector3 InverseTransformVector(Vector3 value) {var d=InverseTransformDirection(value);var scale=lossyScale;return new(d.x/scale.x,d.y/scale.y,d.z/scale.z);}
        public bool IsChildOf(Transform other)=>this==other||(Parent?.IsChildOf(other)??false);
        public new string name=>gameObject.name;
        public int childCount=>Children.Count; public Transform GetChild(int index)=>Children[index];
        public void SetParent(Transform parent,bool worldPositionStays) { Parent?.Children.Remove(this); Parent=parent; parent.Children.Add(this); }
        public void SetPositionAndRotation(Vector3 p,Quaternion q) { position=p;rotation=q; }
        public float Rotated; public void Rotate(Vector3 axis,float degrees,Space space) { Rotated+=degrees; }
    }
    public struct Vector3
    {
        public float x,y,z; public Vector3(float a,float b,float c) {x=a;y=b;z=c;}
        public float sqrMagnitude=>x*x+y*y+z*z;
        public float magnitude=>(float)Math.Sqrt(sqrMagnitude);
        public Vector3 normalized=>sqrMagnitude<.000001f?zero:this*(1f/(float)Math.Sqrt(sqrMagnitude));
        public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
        public static Vector3 Cross(Vector3 a,Vector3 b)=>new(a.y*b.z-a.z*b.y,a.z*b.x-a.x*b.z,a.x*b.y-a.y*b.x);
        public static Vector3 Scale(Vector3 a,Vector3 b)=>new(a.x*b.x,a.y*b.y,a.z*b.z);
        public static Vector3 forward=>new(0,0,1);
        public static Vector3 one=>new(1,1,1); public static Vector3 up=>new(0,1,0); public static Vector3 right=>new(1,0,0);
        public static Vector3 zero=>new(); public static Vector3 down=>new(0,-1,0);
        public static Vector3 operator +(Vector3 a,Vector3 b)=>new(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a,Vector3 b)=>new(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator *(Vector3 a,float b)=>new(a.x*b,a.y*b,a.z*b);
    }
    public struct Quaternion
    {
        public float x,y,z,w; public Quaternion(float a,float b,float c,float d){x=a;y=b;z=c;w=d;}
        public static Quaternion identity=>new(0,0,0,1);
        private System.Numerics.Quaternion Native=>new(x,y,z,w);
        private static Quaternion Wrap(System.Numerics.Quaternion q)=>new(q.X,q.Y,q.Z,q.W);
        public static Quaternion AngleAxis(float degrees,Vector3 axis)=>Wrap(System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.Normalize(new(axis.x,axis.y,axis.z)),degrees*(float)Math.PI/180f));
        public static Quaternion operator *(Quaternion a,Quaternion b)=>Wrap(a.Native*b.Native);
        public static Vector3 operator *(Quaternion a,Vector3 b) {var v=System.Numerics.Vector3.Transform(new(b.x,b.y,b.z),a.Native);return new(v.X,v.Y,v.Z);}
        public static Quaternion Inverse(Quaternion q)=>Wrap(System.Numerics.Quaternion.Inverse(q.Native));
        public static Quaternion FromToRotation(Vector3 from,Vector3 to)
        {
            from=from.normalized;to=to.normalized;float dot=Vector3.Dot(from,to);
            if(dot<-.999999f)
            {var axis=Vector3.Cross(from,Vector3.up);if(axis.sqrMagnitude<.000001f)axis=Vector3.Cross(from,Vector3.right);return AngleAxis(180,axis);}
            var cross=Vector3.Cross(from,to);return Wrap(System.Numerics.Quaternion.Normalize(new(cross.x,cross.y,cross.z,1+dot)));
        }
        public static Quaternion Euler(float x,float y,float z)=>Wrap(System.Numerics.Quaternion.CreateFromYawPitchRoll(y*(float)Math.PI/180,x*(float)Math.PI/180,z*(float)Math.PI/180));
    }
    public enum Space { Self }
    public static class Time { public static float unscaledTime,deltaTime=0.02f,fixedDeltaTime=.02f; public static int frameCount=>(int)(unscaledTime/0.02f); }
    public enum AnimatorControllerParameterType { Bool,Float,Trigger }
    public class AnimatorControllerParameter { public string name; public AnimatorControllerParameterType type; }
    public class Animator : Component
    {
        public object runtimeAnimatorController=new();
        public AnimatorControllerParameter[] parameters=Array.Empty<AnimatorControllerParameter>();
        public readonly Dictionary<string,bool> Bools=new();
        public int Evaluations; public Action OnEvaluate;
        public void Update(float delta) { if(delta!=0f) throw new InvalidOperationException("Manual evaluation advanced time"); Evaluations++;OnEvaluate?.Invoke(); }
        public bool GetBool(string key)=>Bools.TryGetValue(key,out bool v)&&v;
        public void SetBool(string key,bool value)=>Bools[key]=value;
    }
    public class Renderer : Component { public object[] sharedMaterials=Array.Empty<object>(); }
    public class MeshRenderer : Renderer { }
    public class MeshFilter : Component { public object sharedMesh; }
    public class SkinnedMeshRenderer : Renderer
    {
        public object sharedMesh,localBounds; public Transform rootBone;
        public Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Transform> bones=new(0);
    }
    public class AnimationClip : Object { }
    public enum WrapMode { Loop }
    public class Animation : Component
    {
        public Dictionary<string,AnimationClip> Clips=new(); public WrapMode wrapMode; public bool Played;
        public static bool FailPlay;
        public AnimationClip GetClip(string key)=>Clips.TryGetValue(key,out var c)?c:null;
        public void AddClip(AnimationClip clip,string key)=>Clips[key]=clip;
        public bool Play(string name) { Played=!FailPlay&&Clips.ContainsKey(name);return Played; }
    }
    public static class Resources
    { public static List<Object> Assets=new(); public static T[] FindObjectsOfTypeAll<T>() where T:Object=>Assets.OfType<T>().ToArray(); }
    public class GUIStyle { }
    public struct Rect { public float x,y,width,height; public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;} }
    public enum TextAnchor { MiddleLeft }
}
namespace Il2CppFishNet.Connection { public class NetworkConnection { public int ClientId; } }
namespace Il2CppScheduleOne.Core.Equipping.Framework
{
    public class EquippableData : UnityEngine.Object { }
    public interface IEquippedItemHandler { }
    public class PhoneHandler : IEquippedItemHandler { }
}
namespace Il2CppScheduleOne.Equipping.Framework
{
    public class PlayerNetworkedEquipper : UnityEngine.Component
    { public bool CanEquipNow=true; public bool CanEquip(Il2CppScheduleOne.Core.Equipping.Framework.EquippableData data)=>CanEquipNow; }
}
namespace Il2CppScheduleOne.SpecialCustomers.Misc
{ public class OrgyTent : UnityEngine.Component { public UnityEngine.Animation _animation; } }
namespace Il2CppScheduleOne.Vehicles
{ public class MotorbikeBurnout : UnityEngine.Component { public UnityEngine.Transform rearWheelVisual,frontWheelContact; public float rearWheelRPM=100f; public UnityEngine.VFX.VisualEffect burnoutEffect;public Il2CppScheduleOne.Audio.AudioSourceController _burnoutAudio; } }
namespace Il2CppScheduleOne.PlayerScripts
{
    public class Avatar : UnityEngine.Component { public Il2CppScheduleOne.AvatarFramework.Animation.AvatarAnimation Animation; public UnityEngine.Transform HipBone;
        public UnityEngine.Transform LeftHandAlignmentPoint,RightHandAlignmentPoint;
        public Il2CppScheduleOne.Core.Avatar.IAttachmentAnchorProvider AttachmentAnchorProvider;
        public Il2CppScheduleOne.AvatarFramework.AvatarEffects Effects;public Il2CppScheduleOne.Avatar.AvatarAppearance Appearance;
        public UnityEngine.Transform BodyContainer;public bool _IsCulled_k__BackingField;public bool IsCulled=>_IsCulled_k__BackingField;
        public Action<bool> OnAvatarCullingChange;public Il2CppScheduleOne.Avatar.Impostors.AvatarImpostor _impostor;
        public int CullingUpdates;public Action OnCullingUpdate;
        public void UpdateAnimationActive(){CullingUpdates++;OnCullingUpdate?.Invoke();BodyContainer?.gameObject.SetActive(!IsCulled);}
        public void SetVisible(bool visible)=>gameObject.SetActive(visible); }
    public class Health { public bool IsAlive=true; }
    public class Player : UnityEngine.Component
    {
        public static Player Local; public static List<Player> PlayerList=new();
        public bool IsLocalPlayer,IsOwner,IsClientInitialized=true,IsServerInitialized=true,IsRagdolled,Approved=true;
        public Il2CppFishNet.Object.NetworkObject CurrentVehicle; public Health Health=new(); public Avatar Avatar;
        public UnityEngine.Vector3 Velocity; public Il2CppFishNet.Connection.NetworkConnection Owner;
        public bool ThirdPersonMeshesVisibleToLocalPlayer;
        public void SetThirdPersonMeshesVisibility(bool visible)
        {ThirdPersonMeshesVisibleToLocalPlayer=visible;foreach(var t in Avatar.GetComponentsInChildren<UnityEngine.Transform>(true))t.gameObject.layer=visible?8:12;}
        public Il2CppScheduleOne.Avatar.Player.PlayerAppearance CurrentAppearance;
        public List<(string Name,bool Value)> Bools=new(); public List<string> Requests=new();
        public List<Il2CppScheduleOne.Core.Equipping.Framework.IEquippedItemHandler> Removed=new();
        public Il2CppScheduleOne.Core.Equipping.Framework.IEquippedItemHandler Equipped;
        public string ThrowBool;
        public void SendAnimationBool(string name,bool value)
        { if(name==ThrowBool) throw new InvalidOperationException("Injected RPC failure"); Bools.Add((name,value));Avatar.Animation._animator.SetBool(name,value); }
        public void SendAnimationTrigger(string trigger)=>Requests.Add(trigger);
        public List<(string Name,bool Value)> ObserverBools=new();
        public void SetAnimationBool(string name,bool value)=>ObserverBools.Add((name,value));
        public Il2CppScheduleOne.Core.Equipping.Framework.IEquippedItemHandler Equip_Networked(Il2CppScheduleOne.Core.Equipping.Framework.EquippableData data)
            =>Equipped=new Il2CppScheduleOne.Core.Equipping.Framework.PhoneHandler();
        public void Unequip(Il2CppScheduleOne.Core.Equipping.Framework.IEquippedItemHandler handler) { Removed.Add(handler); if(ReferenceEquals(Equipped,handler))Equipped=null; }
    }
}
namespace Il2CppScheduleOne.AvatarFramework.Animation
{
    public class AvatarAnimation : UnityEngine.Component
    {
        public UnityEngine.Animator _animator;
        public Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<UnityEngine.Transform> _bones;
    }
}
namespace NugzzMenu.Services
{
    using Il2CppScheduleOne.PlayerScripts;
    public class SessionAuthorityService
    {
        public static readonly SessionAuthorityService Instance=new(); public bool FeaturesAllowed=true; private bool _host=true;
        public bool Host {get=>_host;set { _host=value; if(Player.Local!=null)Player.Local.IsServerInitialized=value; } }
        public bool IsClientApproved(Player p)=>p.Approved;
        public static bool IsHostPlayer(Player p)=>p!=null&&(p.IsLocalPlayer?Instance.Host:p.Owner.ClientId==0);
    }
    public class GameManagerService { public static readonly GameManagerService Instance=new(); public Player GetLocalPlayer()=>Player.Local; }
    public class NativeActivityGateService
    { public static readonly NativeActivityGateService Instance=new(); public bool Blocked; public bool TryGetBlockReason(out string reason){reason="using native UI";return Blocked;} }
    public class NotificationService
    { public static readonly NotificationService Instance=new();public string Message;public void Status(string s)=>Message=s; public void Warning(string s)=>Message=s; public void Notify(string s)=>Message=s; public void Error(string s)=>Message=s; }
    public class DebugLogService { public static readonly DebugLogService Instance=new(); public void VerboseWarning(string s) { } public void Verbose(string s) { } }
    public static class PlayerValueRpcService
    { public static List<string> Broadcasts=new(); public static void BroadcastToApprovedClients(Player p,string name,string value)=>Broadcasts.Add(value);
      public static List<(int Recipient,string Value)> TargetMessages=new();
      public static bool SendToClient(Player p,Player recipient,string name,string value){Broadcasts.Add(value);TargetMessages.Add((recipient.Owner.ClientId,value));return true;} }
    public enum LabelCategory { Header,Status,Label }
    public class GUISystemService
    {
        public static readonly GUISystemService Instance=new(); public object GetColorForCategory(LabelCategory c)=>null;
        public int GetFontSizeForCategory(LabelCategory c)=>14; public UnityEngine.TextAnchor GetAlignmentForCategory(LabelCategory c)=>UnityEngine.TextAnchor.MiddleLeft;
        public object GetStyleForCategory(LabelCategory c)=>null;
    }
    public class TMPHybridService
    { public static readonly TMPHybridService Instance=new();public void Label(float x,float y,float w,float h,string t,object color,int size,UnityEngine.TextAnchor align,object style=null){} }
    public static class GUIFit
    {
        public static List<(UnityEngine.Rect Rect,string Text)> Buttons=new();
        public static void Panel(UnityEngine.Rect r,UnityEngine.GUIStyle s) { }
        public static bool Button(UnityEngine.Rect r,string text,UnityEngine.GUIStyle s){ Buttons.Add((r,text));return false; }
    }
}
namespace Il2CppScheduleOne.Core.Avatar
{
    public class IAttachmentAnchorProvider
    {
        public UnityEngine.Transform Hips,LowerSpine,MiddleSpine,LeftShoulder,RightShoulder,LeftUpperArm,RightUpperArm,LeftLowerArm,RightLowerArm,LeftHand,RightHand,
            LeftUpperLeg,RightUpperLeg,LeftLowerLeg,RightLowerLeg,LeftFoot,RightFoot;
    }
}
