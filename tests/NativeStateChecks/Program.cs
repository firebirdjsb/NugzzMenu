using System.Reflection;
using NugzzMenu.Services;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.UI.Phone;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Police;
using Il2CppScheduleOne.Combat;
using Il2CppFishNet.Object;
using Il2CppScheduleOne.Growing;
using Il2CppScheduleOne.Interaction;

int passed = 0;
void Require(bool value, string text) { if (!value) throw new InvalidOperationException(text); }
void Check(string name, Action action)
{
    UnityEngine.Time.frameCount++;
    SessionAuthorityService.Instance.FeaturesAllowed = true;
    GameplayStateGateService.Instance.SetMenuOpen(false);
    GameplayMenu.Instance = null;
    NativeActivityGateService.Instance.ResetForScene();
    PauseMenu.Instance.IsPaused = false;
    UIScreenManager.Instance.ScreenActive = false;
    UnityEngine.Cursor.visible = false;
    UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.Locked;
    Il2CppScheduleOne.PlayerTasks.TaskManager.Instance.CurrentTask = null;
    LobbyService.Instance.InLobby = false;
    LobbyService.Instance.Host = true;
    NeverWantedNetworkService.Reset();LocalAnimationNetworkService.Instance.CanSynchronizeBody=true;
    if(ManagerCacheService.Instance.LocalPlayer!=null)
    {var player=ManagerCacheService.Instance.LocalPlayer;player.IsServerInitialized=true;player.IsClientInitialized=true;player.IsOwner=true;player.Requests.Clear();}
    foreach(var player in Player.PlayerList)player.Approved=true;
    action(); passed++; Console.WriteLine("PASS: " + name);
}
var gate = NativeActivityGateService.Instance;
bool PatchRuns(Type type, params object[] parameters) =>
    (bool)type.GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, parameters);
void PhoneChanged(Phone phone, bool open) =>
    typeof(NugzzPhoneActivityPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static)
        .Invoke(null, new object[] { phone, open });
void ParentChanged(GameplayMenu parent) =>
    typeof(NugzzGameplayMenuCloseStatePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static)
        .Invoke(null, new object[] { parent });
Check("Phone closing during Character transition preserves clothing and its state", () =>
{
    var parent = new GameplayMenu { IsOpen = true, CurrentScreen = GameplayMenu.EGameplayScreen.Character };
    GameplayMenu.Instance = parent; gate.SetGameplayMenu(parent);
    var character = new CharacterInterface { IsOpen = true, StateActive = true };
    var display = new CharacterDisplay { IsOpen = true };
    gate.SetCharacterInterface(character); gate.SetCharacterDisplay(display);
    var app = new object(); Phone.ActiveApp = app;
    var phone = new Phone { IsOpen = false };
    PhoneChanged(phone, false);
    Require(character.IsOpen && character.StateActive && character.Removed == 0 && display.IsOpen,
        "Transition removed the clothing panel/state");
    Require(Phone.ActiveApp == app && phone.CloseRequests == 0, "Transition closed a retained native app");
    Require(gate.TryGetBlockReason(out _), "World input allowed over Character");
    Require(!GameplayStateGateService.Instance.IsModControlBlocked(out _) &&
        !GameplayStateGateService.Instance.AreMenuHotkeysBlocked(), "Nugzz cannot open over Character");
});
Check("Phone and Character both permit menu hotkeys while blocking world features", () =>
{
    var parent = new GameplayMenu { IsOpen = true }; GameplayMenu.Instance = parent; gate.SetGameplayMenu(parent);
    UIScreenManager.Instance.ScreenActive = true; UnityEngine.Cursor.visible = true;
    UnityEngine.Cursor.lockState = UnityEngine.CursorLockMode.None;
    foreach (var tab in new[] { GameplayMenu.EGameplayScreen.Phone, GameplayMenu.EGameplayScreen.Character })
    {
        parent.CurrentScreen = tab;
        Require(!GameplayStateGateService.Instance.AreMenuHotkeysBlocked(), "Menu hotkey blocked on " + tab);
        Require(GameplayStateGateService.Instance.AreFeatureHotkeysBlocked(), "World hotkeys leaked on " + tab);
        GameplayStateGateService.Instance.SetMenuOpen(true);
        Require(!GameplayStateGateService.Instance.IsModControlBlocked(out _), "Menu closed itself over " + tab);
        GameplayStateGateService.Instance.SetMenuOpen(false);
        Require(gate.IsPhoneCharacterScreenOpen, "Closing Nugzz lost the native cursor owner");
    }
});
Check("Exiting from Character releases the gate even when child visibility lags", () =>
{
    var parent = new GameplayMenu { IsOpen = true, CurrentScreen = GameplayMenu.EGameplayScreen.Character };
    GameplayMenu.Instance = parent; gate.SetGameplayMenu(parent);
    var character = new CharacterInterface { IsOpen = true, StateActive = true };
    gate.SetCharacterInterface(character);
    Require(gate.TryGetBlockReason(out _), "Baseline ownership absent");
    parent.Close(); ParentChanged(parent);
    Require(!gate.TryGetBlockReason(out _) && !gate.IsPhoneCharacterScreenOpen,
        "Parent exit retained the child visibility gate in the same frame");
    Require(!GameplayStateGateService.Instance.AreMenuHotkeysBlocked(), "F8 remains blocked after native exit");
    Require(character.Removed == 0, "Native stack was changed by mod cleanup");
    parent.Open(); gate.SetGameplayMenu(parent);
    Require(gate.IsPhoneCharacterScreenOpen && !GameplayStateGateService.Instance.IsModControlBlocked(out _),
        "Reopening the retained Character screen fails");
});
Check("Phone hotkeys, shortcuts and tab transitions are blocked during Nugzz capture", () =>
{
    GameplayStateGateService.Instance.SetMenuOpen(true);
    Require(!PatchRuns(typeof(NugzzGameplayMenuInputPatch)), "New InputAction polling runs while Nugzz owns input");
    Require(!PatchRuns(typeof(NugzzGameplayMenuOpenPatch)), "Parent phone can open while Nugzz owns input");
    Require(!PatchRuns(typeof(NugzzGameplayMenuTabPatch)), "Native tabs can switch through the overlay");
    Require(!PatchRuns(typeof(NugzzPhoneActivityPatch), true) && PatchRuns(typeof(NugzzPhoneActivityPatch), false),
        "Direct phone open blocked incorrectly or close prevented");
    GameplayStateGateService.Instance.SetMenuOpen(false);
    Require(PatchRuns(typeof(NugzzGameplayMenuInputPatch)) && PatchRuns(typeof(NugzzGameplayMenuOpenPatch)) &&
        PatchRuns(typeof(NugzzGameplayMenuTabPatch)) && PatchRuns(typeof(NugzzPhoneActivityPatch), true),
        "Native input was not restored after Nugzz closes");
});
Check("Other native activities, pause, creation and text retain their menu gates", () =>
{
    GameplayMenu.Instance = new GameplayMenu { IsOpen = true };
    var tv = new Il2CppScheduleOne.TV.TVInterface { IsOpen = true }; gate.SetTV(tv);
    Require(GameplayStateGateService.Instance.IsModControlBlocked(out _), "TV gate removed");
    gate.ClearTV(tv);
    PauseMenu.Instance.IsPaused = true;
    Require(GameplayStateGateService.Instance.AreMenuHotkeysBlocked(), "Pause gate removed");
    PauseMenu.Instance.IsPaused = false;
    Il2CppScheduleOne.CharacterCreator.CharacterCreator.Instance.IsOpen = true;
    Require(GameplayStateGateService.Instance.AreMenuHotkeysBlocked(), "Creator gate removed");
    Il2CppScheduleOne.CharacterCreator.CharacterCreator.Instance.IsOpen = false;
    GUIFit.IsTextFieldActive = true;
    Require(GameplayStateGateService.Instance.AreMenuHotkeysBlocked(), "Text entry gate removed");
    GUIFit.IsTextFieldActive = false;
});
Check("Unrelated exclusive UI remains blocked and scene reset discards previous parent", () =>
{
    UIScreenManager.Instance.ScreenActive = true; UnityEngine.Cursor.visible = true;
    Require(GameplayStateGateService.Instance.IsModControlBlocked(out _), "Unrelated exclusive screen bypassed");
    gate.SetGameplayMenu(new GameplayMenu { IsOpen = true }); gate.ResetForScene();
    Require(!gate.IsPhoneCharacterScreenOpen, "Scene reset kept the old parent");
});


var local = new Player { PlayerCode = "local" };
var other = new Player { PlayerCode = "other" };
local.CrimeData.Player = local; other.CrimeData.Player = other;
ManagerCacheService.Instance.LocalPlayer = local;
Player.PlayerList.Add(local);Player.PlayerList.Add(other);
Check("Never Wanted protects the selected local player for solo, host and client", () =>
{
    PlayerCheatService.Instance.NeverWanted = true;
    Require(NeverWantedService.Protects(local) && !NeverWantedService.Protects(other), "Protection leaked to another player");
    Require(NeverWantedService.ProtectsCode("local") && !NeverWantedService.ProtectsCode("other"), "Code scope failed");
    LobbyService.Instance.InLobby = true; LobbyService.Instance.Host = false;
    local.IsServerInitialized=false;
    Require(NeverWantedService.Protects(local)&&!NeverWantedService.Protects(other), "Client still host-blocked or protected someone else");
});
Check("Turning off or losing authorization restores native police decisions", () =>
{
    PlayerCheatService.Instance.NeverWanted = false;
    Require(!NeverWantedService.Protects(local), "Disabled policy remained active");
    PlayerCheatService.Instance.NeverWanted = true; SessionAuthorityService.Instance.FeaturesAllowed = false;
    Require(!NeverWantedService.Protects(local), "Policy remained active without authorization");
});
Check("Existing crime and police targets are cleared without affecting another player", () =>
{
    PlayerCheatService.Instance.NeverWanted = true;
    local.CrimeData.AddCrime(); local.CrimeData.Escalate();
    local.CrimeData.CurrentArrestProgress = local.CrimeData.CurrentBodySearchProgress = 0.8f;
    other.CrimeData.AddCrime(); other.CrimeData.Escalate();
    var foot = new Behaviour { TargetPlayer = local };
    var vehicle = new Behaviour { Target = local };
    var body = new Behaviour { TargetPlayer = local };
    var otherFoot = new Behaviour { TargetPlayer = other };
    PoliceOfficer.Officers = new List<PoliceOfficer> {
        new PoliceOfficer { PursuitBehaviour = foot, VehiclePursuitBehaviour = vehicle, BodySearchBehaviour = body,
            currentBodySearchInvestigation = new Investigation { Target = local } },
        new PoliceOfficer { PursuitBehaviour = otherFoot } };
    UnityEngine.Time.unscaledTime = 100f;
    NeverWantedService.Update();
    Require(local.CrimeData.Crimes.Count == 0 && local.CrimeData.CurrentPursuitLevel == PlayerCrimeData.EPursuitLevel.None,
        "Existing wanted state remains");
    Require(local.CrimeData.CurrentArrestProgress == 0 && local.CrimeData.CurrentBodySearchProgress == 0, "Search/arrest remains");
    Require(foot.Disabled == 1 && vehicle.Disabled == 1 && body.Disabled == 1 && otherFoot.Disabled == 0,
        "Wrong officer behaviour was changed");
    Require(PoliceOfficer.Officers[0].currentBodySearchInvestigation == null, "Investigation retained");
    Require(other.CrimeData.Crimes.Count == 1 && other.CrimeData.CurrentPursuitLevel == PlayerCrimeData.EPursuitLevel.Wanted,
        "Another player's record changed");
    UnityEngine.Time.unscaledTime += 0.3f;
    NeverWantedService.Update();
    Require(foot.Disabled == 1 && vehicle.Disabled == 1 && body.Disabled == 1,
        "Cleanup repeatedly sent disable RPCs for inactive behaviours");
});
Check("Combat suppression applies to police targeting the local player only", () =>
{
    PlayerCheatService.Instance.NeverWanted = true;
    var target = new NetworkObject { Component = local };
    Require(NeverWantedService.IsProtectedPoliceTarget(new CombatBehaviour { Npc = new PoliceOfficer() }, target), "Police combat not covered");
    Require(!NeverWantedService.IsProtectedPoliceTarget(new CombatBehaviour { Npc = new Npc() }, target), "Non-police NPC combat changed");
    Require(!NeverWantedService.IsProtectedPoliceTarget(new CombatBehaviour { Npc = new PoliceOfficer() }, new NetworkObject { Component = other }),
        "Another player protected");
});

var nativeMessage = typeof(GrowNativeMessagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
bool NativeWriterRuns(InteractableObject target) => (bool)nativeMessage.Invoke(null, new object[] { target });
GrowContainer PromptPot(out InteractableObject target)
{
    target = new InteractableObject();
    var pot = new GrowContainer { _interactionHandler = new GrowContainerInteraction { _interactableObject = target } };
    target.Parent = pot;
    return pot;
}
Check("Native fallback display writer cannot replace the current custom LMB prompt", () =>
{
    var pot = PromptPot(out var target);
    GrowToolFallbackService.Instance.Current = pot;
    GrowPromptOwnershipService.Clear();
    Require(!NativeWriterRuns(target) && target.Message == "[LMB] Harvest plant", "Native fallback won before tool Update");
});
Check("Aim-away and invalid tool restore native display immediately", () =>
{
    var first = PromptPot(out var oldTarget); var second = PromptPot(out var currentTarget);
    GrowPromptOwnershipService.Claim(first, "Harvest plant");
    GrowToolFallbackService.Instance.Current = second;
    Require(NativeWriterRuns(oldTarget) && !NativeWriterRuns(currentTarget), "Old target retained its prompt lease");
    GrowToolFallbackService.Instance.Current = null;
    Require(NativeWriterRuns(currentTarget), "Invalid action still suppressed native display");
});
Check("Native prompts remain available when an interface owns input", () =>
{
    GrowToolFallbackService.Instance.Current = PromptPot(out var target);
    GameplayStateGateService.Instance.SetMenuOpen(true);
    Require(NativeWriterRuns(target), "Hidden menu blocked native prompt writer");
});
// Native writes are checked through the production helper; transport, growth
// timing, native UI rendering and lighting are not simulated by this fixture.
Check("Planting uses canonical ID and native packed soil completion", () =>
{
    var pot = new Il2CppScheduleOne.ObjectScripts.Pot();
    var seed = new SeedDefinition { ID = "ogkushseed", name = "OGKushSeed_Asset" };
    Require(GrowActionService.Plant(pot, seed, out _), "Valid planting refused");
    Require(pot.PlantedId == seed.ID && pot.Progress == 0f &&
        pot.SoilState == Il2CppScheduleOne.ObjectScripts.Pot.ESoilState.Packed &&
        pot.Actions.SequenceEqual(new[] { "plant", "pack" }), "Native completion contract changed");
});
Check("Invalid seeds, busy pots and disconnected pots are not planted", () =>
{
    var pot = new Il2CppScheduleOne.ObjectScripts.Pot();
    var seed = new SeedDefinition { ID = "ogkushseed" };
    Require(!GrowActionService.Plant(pot, new SeedDefinition { name = "AssetOnly" }, out _), "Asset name accepted as ID");
    pot.AcceptsSeed = false; Require(!GrowActionService.Plant(pot, seed, out _), "Native rejection ignored");
    pot.AcceptsSeed = true; pot.PlayerUserObject = new NetworkObject();
    Require(!GrowActionService.Plant(pot, seed, out _), "Another player's pot changed");
    pot.PlayerUserObject = null; pot.IsClientInitialized = false;
    Require(!GrowActionService.Plant(pot, seed, out _) && pot.Actions.Count == 0, "Disconnected pot was planted");
});
Check("Quick watering sends the new moisture before consuming its existing cost", () =>
{
    var pot = new GrowContainer { Moisture = 1f };
    var water = new Il2CppScheduleOne.ItemFramework.WaterContainerInstance();
    Require(GrowActionService.Water(pot, water, out _), "Valid watering refused");
    Require(pot.Moisture == 10f && pot.SentMoisture == 10f && pot.Syncs == 1 && water.CurrentFillAmount == 18f,
        "Moisture was unsynced or quick-water cost changed");
    Require(!GrowActionService.Water(pot, water, out _) && pot.Syncs == 1 && water.Changes == 1,
        "Already watered target consumed more water");
});
Check("Water sync failure restores prior moisture without consumption or success", () =>
{
    var pot = new GrowContainer { Moisture = 3f, FailSync = true };
    var water = new Il2CppScheduleOne.ItemFramework.WaterContainerInstance();
    bool threw = false;
    try { GrowActionService.Water(pot, water, out _); }
    catch (InvalidOperationException ex) when (ex.Message == "Fixture moisture sync failed") { threw = true; }
    Require(threw && pot.Moisture == 3f && water.CurrentFillAmount == 20f && water.Changes == 0,
        "Failed sync silently succeeded or spent water");
});
Check("Native task, NPC, player and menu ownership prevent watering races", () =>
{
    var pot = new GrowContainer(); var water = new Il2CppScheduleOne.ItemFramework.WaterContainerInstance();
    pot.NPCUserObject = new NetworkObject(); Require(!GrowActionService.Water(pot, water, out _), "NPC task was overwritten");
    pot.NPCUserObject = null; pot.PlayerUserObject = new NetworkObject();
    Require(!GrowActionService.Water(pot, water, out _), "Player task was overwritten");
    pot.PlayerUserObject = null;
    Il2CppScheduleOne.PlayerTasks.TaskManager.Instance.CurrentTask = new Il2CppScheduleOne.PlayerTasks.Task { TaskActive = true };
    Require(!GrowActionService.Water(pot, water, out _), "Native task was overwritten");
    Il2CppScheduleOne.PlayerTasks.TaskManager.Instance.CurrentTask = null;
    GameplayStateGateService.Instance.SetMenuOpen(true);
    Require(!GrowActionService.Water(pot, water, out _) && pot.MoistureChanges == 0 && water.Changes == 0,
        "Menu click watered a pot");
});
Check("Uninitialized network and invalid capacity refuse watering before mutation", () =>
{
    var pot = new GrowContainer { IsClientInitialized = false }; var water = new Il2CppScheduleOne.ItemFramework.WaterContainerInstance();
    Require(!GrowActionService.Water(pot, water, out _), "Uninitialized sync accepted");
    pot.IsClientInitialized = true;
    foreach (float value in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
    {
        pot.MoistureCapacity = value;
        Require(!GrowActionService.Water(pot, water, out _), "Invalid capacity accepted");
    }
    Require(pot.MoistureChanges == 0 && water.Changes == 0, "Rejected watering changed resources");
});
Check("Empty and near-empty water containers retain existing quick-water behavior", () =>
{
    var pot = new GrowContainer(); var water = new Il2CppScheduleOne.ItemFramework.WaterContainerInstance { CurrentFillAmount = 0f };
    Require(!GrowActionService.Water(pot, water, out _) && pot.MoistureChanges == 0, "Empty container watered");
    water.CurrentFillAmount = 0.1f;
    Require(GrowActionService.Water(pot, water, out _) && water.CurrentFillAmount == 0f && pot.Syncs == 1,
        "Low-fill quick-water behavior changed");
});
Check("Client Never Wanted clears local state without calling server police cleanup",()=>
{
    local.IsServerInitialized=false;PlayerCheatService.Instance.NeverWanted=true;
    local.CrimeData.AddCrime();local.CrimeData.Escalate();var pursuit=new Behaviour{TargetPlayer=local};
    PoliceOfficer.Officers=new(){new PoliceOfficer{PursuitBehaviour=pursuit}};
    LocalAnimationNetworkService.Instance.CanSynchronizeBody=false;NeverWantedService.Update();
    Require(NeverWantedService.Protects(local)&&local.CrimeData.Crimes.Count==0&&local.CrimeData.CurrentPursuitLevel==PlayerCrimeData.EPursuitLevel.None,"Client policy still host-blocked");
    Require(pursuit.Disabled==0&&local.Requests.Count==0,"Client called a server cleanup or sent a reserved request to an older host");
});
Check("Client sends owner requests with bounded refresh and sends disable once",()=>
{
    local.IsServerInitialized=false;PlayerCheatService.Instance.NeverWanted=true;UnityEngine.Time.unscaledTime=300;
    NeverWantedNetworkService.Update();Require(local.Requests.SequenceEqual(new[]{NeverWantedNetworkService.RequestPrefix+"1"}),"Missing owner enable request");
    UnityEngine.Time.unscaledTime+=1;NeverWantedNetworkService.Update();Require(local.Requests.Count==1,"Per-frame request spam");
    UnityEngine.Time.unscaledTime+=3;NeverWantedNetworkService.Update();Require(local.Requests.Count==2,"No protection refresh");
    PlayerCheatService.Instance.NeverWanted=false;NeverWantedNetworkService.LocalChanged();NeverWantedNetworkService.Update();
    Require(local.Requests.Last()==NeverWantedNetworkService.RequestPrefix+"0","Disable request missing");
    UnityEngine.Time.unscaledTime+=30;NeverWantedNetworkService.Update();Require(local.Requests.Count==3,"Inactive cheat continued sending requests");
});
Check("Unowned or uninitialized client cannot request protection",()=>
{
    local.IsServerInitialized=false;PlayerCheatService.Instance.NeverWanted=true;
    local.IsOwner=false;NeverWantedNetworkService.Update();local.IsOwner=true;local.IsClientInitialized=false;NeverWantedNetworkService.Update();
    Require(local.Requests.Count==0,"Foreign or uninitialized pawn emitted owner request");
});
Check("Host applies client protection with host toggle off and leaves other crime records alone",()=>
{
    PlayerCheatService.Instance.NeverWanted=false;UnityEngine.Time.unscaledTime=400;
    NeverWantedNetworkService.Receive(other,NeverWantedNetworkService.RequestPrefix+"1");
    Require(NeverWantedService.Protects(other)&&!NeverWantedService.Protects(local)&&NeverWantedService.ProtectsCode("other"),"Host's own toggle gates client protection");
    local.CrimeData.ClearCrimes();local.CrimeData.AddCrime();local.CrimeData.Escalate();
    other.CrimeData.AddCrime();other.CrimeData.Escalate();var protectedPursuit=new Behaviour{TargetPlayer=other};var nativePursuit=new Behaviour{TargetPlayer=local};
    PoliceOfficer.Officers=new(){new PoliceOfficer{PursuitBehaviour=protectedPursuit},new PoliceOfficer{PursuitBehaviour=nativePursuit}};
    NeverWantedService.Update();
    Require(other.CrimeData.Crimes.Count==0&&protectedPursuit.Disabled==1,"Host didn't stop police targeting the protected client");
    Require(local.CrimeData.Crimes.Count==1&&nativePursuit.Disabled==0,"Client toggle cleared another player's wanted state");
});
Check("Reserved request parser never leaks malformed controls into Animator or chooses another pawn",()=>
{
    PlayerCheatService.Instance.NeverWanted=false;
    foreach(string value in new[]{"", "2", "1|local", "1|other", "true"})
        Require(!PatchRuns(typeof(NeverWantedRequestPatch),other,NeverWantedNetworkService.RequestPrefix+value),"Reserved message reached native Animator");
    Require(!NeverWantedService.Protects(other)&&!NeverWantedService.Protects(local),"Malformed request enabled protection");
    Require(PatchRuns(typeof(NeverWantedRequestPatch),other,"ThumbsUp"),"Ordinary native animation was intercepted");
    Require(!PatchRuns(typeof(NeverWantedRequestPatch),other,NeverWantedNetworkService.RequestPrefix+"1"),"Enable control reached Animator");
    Require(NeverWantedService.Protects(other)&&!NeverWantedService.Protects(local),"Request selected a different pawn");
});
Check("Client requests require approval and stop immediately on revoke or disable",()=>
{
    PlayerCheatService.Instance.NeverWanted=false;other.Approved=false;
    NeverWantedNetworkService.Receive(other,NeverWantedNetworkService.RequestPrefix+"1");Require(!NeverWantedService.Protects(other),"Unapproved actor gained protection");
    other.Approved=true;other.IsServerInitialized=false;NeverWantedNetworkService.Receive(other,NeverWantedNetworkService.RequestPrefix+"1");Require(!NeverWantedService.Protects(other),"Uninitialized actor gained protection");
    other.IsServerInitialized=true;NeverWantedNetworkService.Receive(other,NeverWantedNetworkService.RequestPrefix+"1");Require(NeverWantedService.Protects(other),"Approved actor could not enable protection");
    other.Approved=false;Require(!NeverWantedService.Protects(other),"Revocation kept protection");NeverWantedNetworkService.Update();other.Approved=true;
    Require(!NeverWantedService.Protects(other),"Reapproval resurrected old toggle");
    NeverWantedNetworkService.Receive(other,NeverWantedNetworkService.RequestPrefix+"1");NeverWantedNetworkService.Receive(other,NeverWantedNetworkService.RequestPrefix+"0");
    Require(!NeverWantedService.Protects(other),"Disable request retained protection");
});
Check("Stale client protection expires and scene reset drops all remote requests",()=>
{
    PlayerCheatService.Instance.NeverWanted=false;UnityEngine.Time.unscaledTime=500;
    NeverWantedNetworkService.Receive(other,NeverWantedNetworkService.RequestPrefix+"1");UnityEngine.Time.unscaledTime+=10.1f;
    Require(!NeverWantedService.Protects(other),"Disconnected client retained protection");NeverWantedNetworkService.Update();
    NeverWantedNetworkService.Receive(other,NeverWantedNetworkService.RequestPrefix+"1");NeverWantedNetworkService.Reset();
    Require(!NeverWantedService.Protects(other),"Scene reset kept remote protection");
});
Require(DebugLogService.Instance.Failures.Count == 0, "Fixture triggered an unexpected exception");
Console.WriteLine($"{passed} state/policy checks passed. Unity display, gameplay, and multiplayer QA remain separate.");
