// Narrow SDK fixture for state ownership and target-scoped policies. It does
// not simulate Unity rendering, FishNet transport, or native NPC behaviour.
namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class HarmonyPatch : Attribute
    {
        public HarmonyPatch() { }
        public HarmonyPatch(Type type, string method) { }
        public HarmonyPatch(Type type, string method, Type[] args) { }
    }
    public class HarmonyMethod { public HarmonyMethod(System.Reflection.MethodInfo method) { } }
    public class Harmony
    {
        public Harmony(string id) { }
        public void Patch(System.Reflection.MethodInfo method, HarmonyMethod prefix) { }
        public void UnpatchSelf() { }
    }
}
namespace UnityEngine
{
    public static class Time { public static int frameCount; public static float unscaledTime; }
    public enum CursorLockMode { None, Locked }
    public static class Cursor { public static bool visible; public static CursorLockMode lockState = CursorLockMode.Locked; }
    public class Component
    {
        public object Parent;
        public T GetComponentInParent<T>() where T : class => Parent as T;
    }
}
namespace Il2CppFishNet.Object
{
    public class NetworkObject
    {
        public object Component;
        public T GetComponent<T>() where T : class => Component as T;
    }
}
namespace Il2CppFishNet.Connection { public class NetworkConnection { } }
namespace Il2CppScheduleOne.PlayerScripts
{
    public class Player
    {
        public static List<Player> PlayerList=new();
        public bool IsServerInitialized=true,IsClientInitialized=true,IsOwner=true,Approved=true;
        public List<string> Requests=new();public void SendAnimationTrigger(string trigger)=>Requests.Add(trigger);
        public string PlayerCode;
        public PlayerCrimeData CrimeData = new PlayerCrimeData();
    }
    public class PlayerCrimeData
    {
        public enum EPursuitLevel { None, Wanted }
        public Player Player;
        public List<string> Crimes = new List<string>();
        public EPursuitLevel CurrentPursuitLevel;
        public float CurrentArrestProgress, CurrentBodySearchProgress;
        public void AddCrime() => Crimes.Add("Crime");
        public void Escalate() => CurrentPursuitLevel = EPursuitLevel.Wanted;
        public void ClearCrimes() => Crimes.Clear();
        public void SetPursuitLevel(EPursuitLevel level) => CurrentPursuitLevel = level;
        public void SetArrestProgress(float value) => CurrentArrestProgress = value;
        public void SetBodySearchProgress(float value) => CurrentBodySearchProgress = value;
    }
}
namespace Il2CppScheduleOne.Combat
{
    public class Npc
    {
        public T TryCast<T>() where T : class => this as T;
    }
    public class Behaviour
    {
        public bool Enabled = true;
        public Il2CppScheduleOne.PlayerScripts.Player TargetPlayer, Target;
        public int Disabled;
        public void Disable_Server() { Disabled++; Enabled = false; }
    }
    public class CombatTarget { public Il2CppFishNet.Object.NetworkObject NetworkObject; }
    public class CombatBehaviour : Behaviour
    {
        public Npc Npc;
        public new CombatTarget Target;
        public int Requests;
        public void SetTargetAndEnable_Server(Il2CppFishNet.Object.NetworkObject target) => Requests++;
        public void RpcLogic___SetTargetAndEnable_Server_3323014238(Il2CppFishNet.Object.NetworkObject target) => Requests++;
    }
}
namespace Il2CppScheduleOne.Police
{
    public class Investigation { public Il2CppScheduleOne.PlayerScripts.Player Target; }
    public class NpcBehaviour { public Il2CppScheduleOne.Combat.CombatBehaviour CombatBehaviour; }
    public class PoliceOfficer : Il2CppScheduleOne.Combat.Npc
    {
        public static List<PoliceOfficer> Officers = new List<PoliceOfficer>();
        public Il2CppScheduleOne.Combat.Behaviour PursuitBehaviour, VehiclePursuitBehaviour, BodySearchBehaviour;
        public Investigation currentBodySearchInvestigation;
        public NpcBehaviour Behaviour;
        public int Requests;
        public void StopBodySearchInvestigation() => currentBodySearchInvestigation = null;
        public void BeginFootPursuit_Networked(string playerCode) => Requests++;
        public void BeginVehiclePursuit_Networked(string playerCode) => Requests++;
        public void BeginBodySearch_Networked(string playerCode) => Requests++;
        public void ConductBodySearch(Il2CppScheduleOne.PlayerScripts.Player player) => Requests++;
    }
}
namespace Il2CppScheduleOne.UI
{
    public class GameplayMenu
    {
        public enum EGameplayScreen { Phone, Character }
        public static GameplayMenu Instance;
        public bool IsOpen;
        public EGameplayScreen CurrentScreen;
        public void Open() => IsOpen = true;
        public void Close() => IsOpen = false;
        public void SetScreen(EGameplayScreen screen) => CurrentScreen = screen;
    }
    public class PauseMenu
    {
        public static PauseMenu Instance { get; } = new PauseMenu();
        public bool IsPaused;
        public void Pause() => IsPaused = true;
    }
    public class UIScreenManager
    {
        public static UIScreenManager Instance { get; } = new UIScreenManager();
        public bool ScreenActive;
        public bool IsAnyScreenActive() => ScreenActive;
    }
    public class CharacterInterface
    {
        public bool IsOpen, StateActive;
        public int Removed;
        public void Open() => IsOpen = true;
        public void Close() { IsOpen = false; NugzzMenu.Services.NativeActivityGateService.Instance.ClearCharacterInterface(this); }
        public void SetIsActiveGameplayScreen(bool isActive) { StateActive = isActive; if (!isActive) Removed++; }
    }
    public class CharacterDisplay { public bool IsOpen; public void SetOpen(bool open) => IsOpen = open; }
}
namespace Il2CppScheduleOne.UI.Phone
{
    public class Phone
    {
        public static object ActiveApp;
        public bool IsOpen;
        public int CloseRequests;
        public void SetIsOpen(bool o) => IsOpen = o;
        public void RequestCloseApp() { ActiveApp = null; CloseRequests++; }
    }
}
namespace Il2CppScheduleOne.TV
{
    public class TVInterface { public bool IsOpen; public void Open() => IsOpen = true; public void Close() => IsOpen = false; }
}
namespace Il2CppScheduleOne.ObjectScripts
{
    public class JukeboxInterface { public bool IsOpen; public void Open() => IsOpen = true; public void Close() => IsOpen = false; }
}
namespace Il2CppScheduleOne.Casino.UI
{
    public class BlackjackInterface { public object CurrentGame; public void Open() => CurrentGame = new object(); public void Close() => CurrentGame = null; }
    public class RTBInterface { public object CurrentGame; public void Open() => CurrentGame = new object(); public void Close() => CurrentGame = null; }
}
namespace Il2CppScheduleOne.Interaction
{
    public class InteractableObject : UnityEngine.Component { public string Message; public void SetMessage(string text) => Message = text; }
}
namespace Il2CppScheduleOne.Growing
{
    public class GrowContainer
    {
        public GrowContainerInteraction _interactionHandler;
        public bool IsClientInitialized = true, FailSync;
        public Il2CppFishNet.Object.NetworkObject NPCUserObject, PlayerUserObject;
        public float MoistureCapacity = 10f, Moisture;
        public float NormalizedMoistureAmount => Moisture / MoistureCapacity;
        public int Syncs, MoistureChanges;
        public float SentMoisture;
        public void SetMoistureAmount(float amount) { Moisture = amount; MoistureChanges++; }
        public void SyncMoistureData()
        {
            if (FailSync) throw new InvalidOperationException("Fixture moisture sync failed");
            Syncs++; SentMoisture = Moisture;
        }
    }
    public class GrowContainerInteraction { public Il2CppScheduleOne.Interaction.InteractableObject _interactableObject; }
    public class SeedDefinition { public string ID, name; }
}
namespace Il2CppScheduleOne.ItemFramework
{
    public class WaterContainerInstance
    {
        public float CurrentFillAmount = 20f;
        public int Changes;
        public void ChangeFillAmount(float delta) { CurrentFillAmount += delta; Changes++; }
    }
}
namespace Il2CppScheduleOne.ObjectScripts
{
    public class Pot : Il2CppScheduleOne.Growing.GrowContainer
    {
        public enum ESoilState { Flat, Parted, Packed }
        public bool AcceptsSeed = true;
        public List<string> Actions = new List<string>();
        public string PlantedId;
        public float Progress;
        public ESoilState SoilState;
        public bool CanAcceptSeed(out string reason) { reason = AcceptsSeed ? null : "Cannot accept seed"; return AcceptsSeed; }
        public void PlantSeed_Server(string id, float progress) { PlantedId = id; Progress = progress; Actions.Add("plant"); }
        public void SetSoilState(ESoilState state) { SoilState = state; Actions.Add("pack"); }
    }
}
namespace Il2CppScheduleOne.PlayerTasks
{
    public class Task { public bool TaskActive; }
    public class TaskManager
    {
        public static TaskManager Instance { get; } = new TaskManager();
        public Task CurrentTask;
    }
}
namespace Il2CppScheduleOne.CharacterCreator
{
    public class CharacterCreator
    {
        public static CharacterCreator Instance { get; } = new CharacterCreator();
        public bool IsOpen;
    }
}
namespace Il2CppScheduleOne { public class GameInput { } }
namespace NugzzMenu.Services
{
    public class DebugLogService
    {
        public static DebugLogService Instance { get; } = new DebugLogService();
        public readonly List<Exception> Failures = new List<Exception>();
        public void VerboseException(string context, Exception ex) => Failures.Add(ex);
        public void Verbose(string text) { }
        public void VerboseWarning(string text) { }
    }
    public class PlayerCheatService { public static PlayerCheatService Instance { get; } = new PlayerCheatService(); public bool NeverWanted; }
    public class SessionAuthorityService { public static SessionAuthorityService Instance { get; } = new SessionAuthorityService(); public bool FeaturesAllowed = true; public bool IsClientApproved(Il2CppScheduleOne.PlayerScripts.Player player)=>player.Approved; }
    public class LocalAnimationNetworkService { public static LocalAnimationNetworkService Instance { get; }=new();public bool CanSynchronizeBody=true; }
    public class ManagerCacheService { public static ManagerCacheService Instance { get; } = new ManagerCacheService(); public Il2CppScheduleOne.PlayerScripts.Player LocalPlayer; }
    public class LobbyService
    {
        public static LobbyService Instance { get; } = new LobbyService();
        public bool InLobby, Host = true;
        public bool IsInLobby() => InLobby;
        public bool IsHost() => Host;
    }
    public static class GUIFit { public static bool IsTextFieldActive; }
    public class ConsoleAutocompleteService { public static ConsoleAutocompleteService Instance { get; } = new ConsoleAutocompleteService(); public bool IsTyping; }
    public class SaveManagementService { public static SaveManagementService Instance { get; } = new SaveManagementService(); public bool IsMainMenu; }
    public class NotificationService { public static NotificationService Instance { get; } = new NotificationService(); public void Status(string text) { } }
    public class GrowToolFallbackService
    {
        public static GrowToolFallbackService Instance { get; } = new GrowToolFallbackService();
        public Il2CppScheduleOne.Growing.GrowContainer Current;
        public void RefreshPromptForEquippedTool()
        {
            GrowPromptOwnershipService.Clear();
            if (Current != null) GrowPromptOwnershipService.Claim(Current, "Harvest plant");
        }
    }
}
