using System;
using Il2CppScheduleOne.Growing;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.PlayerTasks;

namespace NugzzMenu.Services
{
    /// <summary>Native planting and moisture writes shared by the quick tool actions.</summary>
    internal static class GrowActionService
    {
        internal static bool IsBusy(GrowContainer container)
        {
            TaskManager tasks = TaskManager.Instance;
            return (tasks != null && tasks.CurrentTask != null && tasks.CurrentTask.TaskActive) ||
                container.NPCUserObject != null || container.PlayerUserObject != null;
        }

        internal static bool Plant(Pot pot, SeedDefinition seed, out string reason)
        {
            reason = null;
            if (pot == null || seed == null || string.IsNullOrWhiteSpace(seed.ID))
            {
                reason = "Seed definition not found";
                return false;
            }
            if (GameplayStateGateService.Instance.AreFeatureHotkeysBlocked() || IsBusy(pot))
            {
                reason = "Pot is in use";
                return false;
            }
            if (!pot.IsClientInitialized)
            {
                reason = "Pot is not ready yet";
                return false;
            }
            if (!pot.CanAcceptSeed(out reason)) return false;

            // Vanilla SowSeedTask.Success passes the registry ID, then packs
            // the soil. Unity asset names are not registry identifiers.
            pot.PlantSeed_Server(seed.ID, 0f);
            pot.SetSoilState(Pot.ESoilState.Packed);
            return true;
        }

        internal static bool Water(GrowContainer container, WaterContainerInstance water, out string reason)
        {
            reason = null;
            if (container == null || water == null)
            {
                reason = "Watering target or container is unavailable";
                return false;
            }
            if (GameplayStateGateService.Instance.AreFeatureHotkeysBlocked() || IsBusy(container))
            {
                reason = "Grow container is in use";
                return false;
            }
            // SyncMoistureData silently logs and returns without sending when
            // FishNet has not initialized the client. Check before consuming water.
            if (!container.IsClientInitialized)
            {
                reason = "Grow container is not ready yet";
                return false;
            }
            float capacity = container.MoistureCapacity;
            float normalized = container.NormalizedMoistureAmount;
            float fill = water.CurrentFillAmount;
            if (!float.IsFinite(capacity) || capacity <= 0f || !float.IsFinite(normalized) ||
                !float.IsFinite(fill))
            {
                reason = "Watering data is unavailable";
                return false;
            }
            normalized = Math.Clamp(normalized, 0f, 1f);
            if (normalized >= 0.995f) { reason = "Already fully watered"; return false; }
            if (fill <= 0.001f) { reason = "Water container empty"; return false; }

            // Retain the menu's existing quick-water cost and fill behavior.
            float spend = Math.Min(fill, Math.Max(0.05f, capacity * Math.Clamp(1f - normalized, 0.05f, 0.2f)));
            float previous = capacity * normalized;
            container.SetMoistureAmount(capacity);
            try
            {
                // Do not swallow sync failures and announce a successful watering.
                container.SyncMoistureData();
            }
            catch
            {
                container.SetMoistureAmount(previous);
                throw;
            }
            water.ChangeFillAmount(-spend);
            return true;
        }
    }
}
