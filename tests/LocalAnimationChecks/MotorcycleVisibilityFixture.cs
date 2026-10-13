// Models the separate native body/impostor gate. No native rendering is simulated.
namespace Il2CppScheduleOne.Avatar.Impostors
{
    public class AvatarImpostor : UnityEngine.Component
    {
        public int Disabled;
        public void Disable(){Disabled++;gameObject.SetActive(false);}
    }
}
namespace NugzzMenu.Services
{
    public class GameplayStateGateService
    {
        public static readonly GameplayStateGateService Instance=new();public bool Blocked;
        public bool AreFeatureHotkeysBlocked()=>Blocked;
    }
}
