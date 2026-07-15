using System;
using System.Globalization;
using HarmonyLib;
using Il2CppScheduleOne.Dialogue;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Variables;
using UnityEngine;

namespace NugzzMenu.Services
{
    internal static class VanillaQuestVariables
    {
        private const string DanGreetingVariable = "dan_greeting_done";

        public static void EnsurePlayerVariables(Player player)
        {
            if (player == null)
                return;

            VariableDatabase database = GetDatabase();
            if (database?.Creators == null)
                return;

            for (int i = 0; i < database.Creators.Length; i++)
            {
                VariableCreator creator = database.Creators[i];
                if (creator == null ||
                    creator.Mode != EVariableMode.Player ||
                    string.IsNullOrWhiteSpace(creator.Name) ||
                    HasPlayerVariable(player, creator.Name))
                {
                    continue;
                }

                try
                {
                    player.AddVariable(CreatePlayerVariable(player, creator));
                }
                catch (Exception ex)
                {
                    DebugLogService.Instance.VerboseWarning(
                        "Could not restore vanilla player variable " + creator.Name + ": " + ex.Message);
                }
            }

            EnsureDanGreetingVariable();
        }

        public static void EnsureInventoryVariables(PlayerInventory inventory)
        {
            if (inventory?.ItemVariables == null)
                return;

            Player player = ResolvePlayer(inventory);
            if (player == null)
                return;

            for (int i = 0; i < inventory.ItemVariables.Count; i++)
            {
                PlayerInventory.ItemVariable itemVariable = inventory.ItemVariables[i];
                string name = itemVariable?.VariableName;
                if (string.IsNullOrWhiteSpace(name) || HasPlayerVariable(player, name))
                    continue;

                try
                {
                    player.AddVariable(new NumberVariable(
                        name,
                        EVariableReplicationMode.Networked,
                        false,
                        EVariableMode.Player,
                        player,
                        0f));
                }
                catch (Exception ex)
                {
                    DebugLogService.Instance.VerboseWarning(
                        "Could not restore vanilla inventory variable " + name + ": " + ex.Message);
                }
            }
        }

        public static void EnsureDanGreetingVariable()
        {
            VariableDatabase database = GetDatabase();
            if (database == null || HasDatabaseVariable(database, DanGreetingVariable))
                return;

            try
            {
                database.CreateVariable(
                    DanGreetingVariable,
                    VariableDatabase.EVariableType.Bool,
                    bool.FalseString,
                    true,
                    EVariableMode.Global,
                    null,
                    EVariableReplicationMode.Networked);
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning(
                    "Could not restore Dan's greeting variable: " + ex.Message);
            }
        }

        private static BaseVariable CreatePlayerVariable(Player player, VariableCreator creator)
        {
            if (creator.Type == VariableDatabase.EVariableType.Bool)
            {
                bool.TryParse(creator.InitialValue, out bool value);
                return new BoolVariable(
                    creator.Name,
                    EVariableReplicationMode.Networked,
                    creator.Persistent,
                    EVariableMode.Player,
                    player,
                    value);
            }

            float.TryParse(
                creator.InitialValue,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out float number);
            return new NumberVariable(
                creator.Name,
                EVariableReplicationMode.Networked,
                creator.Persistent,
                EVariableMode.Player,
                player,
                number);
        }

        private static Player ResolvePlayer(PlayerInventory inventory)
        {
            Player player = ManagerCacheService.Instance.LocalPlayer;
            if (player != null)
                return player;

            try { return inventory.GetComponentInParent<Player>(); }
            catch { return null; }
        }

        private static VariableDatabase GetDatabase()
        {
            try
            {
                return VariableDatabase.Instance ?? UnityEngine.Object.FindObjectOfType<VariableDatabase>();
            }
            catch
            {
                return null;
            }
        }

        private static bool HasPlayerVariable(Player player, string name)
        {
            try
            {
                if (player.PlayerVariables == null)
                    return false;

                for (int i = 0; i < player.PlayerVariables.Count; i++)
                {
                    BaseVariable variable = player.PlayerVariables[i];
                    if (variable != null &&
                        string.Equals(variable.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        private static bool HasDatabaseVariable(VariableDatabase database, string name)
        {
            try
            {
                if (database.VariableList == null)
                    return false;

                for (int i = 0; i < database.VariableList.Count; i++)
                {
                    BaseVariable variable = database.VariableList[i];
                    if (variable != null &&
                        string.Equals(variable.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

    }

    [HarmonyPatch(typeof(Player), "CreatePlayerVariables")]
    internal static class VanillaPlayerQuestVariablesPatch
    {
        private static void Postfix(Player __instance)
        {
            VanillaQuestVariables.EnsurePlayerVariables(__instance);
        }
    }

    [HarmonyPatch(typeof(PlayerInventory), "Start")]
    internal static class VanillaInventoryQuestVariablesPatch
    {
        private static void Postfix(PlayerInventory __instance)
        {
            VanillaQuestVariables.EnsureInventoryVariables(__instance);
        }
    }

    [HarmonyPatch(typeof(DialogueController_Dan), "Start")]
    internal static class DanGreetingVariableStartPatch
    {
        private static void Prefix()
        {
            VanillaQuestVariables.EnsureDanGreetingVariable();
        }
    }

    [HarmonyPatch(typeof(DialogueController_Dan), nameof(DialogueController_Dan.ModifyDialogueText))]
    internal static class DanGreetingVariableDialoguePatch
    {
        private static void Prefix()
        {
            VanillaQuestVariables.EnsureDanGreetingVariable();
        }
    }
}
