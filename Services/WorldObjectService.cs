using System;
using System.Reflection;
using HarmonyLib;
using Il2CppScheduleOne;
using Il2CppScheduleOne.Growing;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;
using UnityEngine;
using static UnityEngine.Object;

namespace NugzzMenu.Services
{
    public sealed class WorldObjectService
    {
        private static readonly WorldObjectService _instance = new WorldObjectService();
        public static WorldObjectService Instance => _instance;
        private static readonly MethodInfo FinalizeChemistryOperation =
            AccessTools.Method(typeof(ChemistryStation), "FinalizeOperation");

        private WorldObjectService() { }

        public int GrowAllPlants()
        {
            if (!CanModifyWorld())
                return 0;

            int changed = 0;

            try
            {
                var pots = FindObjectsOfType<Pot>();
                if (pots != null)
                {
                    for (int i = 0; i < pots.Length; i++)
                    {
                        var pot = pots[i];
                        if (pot == null)
                            continue;

                        try
                        {
                            if (!pot.ContainsGrowable())
                                continue;

                            pot.SetGrowthProgress_Server(0.99f);
                            changed++;
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogWarning("[Nugzz] Pot grow failed: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[Nugzz] Pot scan failed: " + ex.Message);
            }

            try
            {
                var beds = FindObjectsOfType<MushroomBed>();
                if (beds != null)
                {
                    for (int i = 0; i < beds.Length; i++)
                    {
                        var bed = beds[i];
                        if (bed == null)
                            continue;

                        try
                        {
                            if (!bed.ContainsGrowable())
                                continue;

                            ShroomColony colony = bed.CurrentColony;
                            if (colony == null || colony.IsFullyGrown)
                                continue;

                            float capacity = bed.MoistureCapacity > 0f ? bed.MoistureCapacity : 1f;
                            bed.SetMoistureAmount(capacity);
                            try { bed.SyncMoistureData(); } catch { }
                            colony.SetFullyGrown();
                            changed++;
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogWarning("[Nugzz] Mushroom bed grow assist failed: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[Nugzz] Mushroom bed scan failed: " + ex.Message);
            }

            NotificationService.Instance.Status("Grow applied: " + changed);
            return changed;
        }

        public int WaterAllPlants()
        {
            if (!CanModifyWorld())
                return 0;

            int watered = 0;

            watered += WaterContainers<Pot>(FindObjectsOfType<Pot>());
            watered += WaterContainers<MushroomBed>(FindObjectsOfType<MushroomBed>());

            NotificationService.Instance.Status("Watered plants: " + watered);
            return watered;
        }

        public int SeedAllPots(string seedId)
        {
            if (!CanModifyWorld())
                return 0;

            SeedDefinition seed = ResolveDefinition<SeedDefinition>(seedId);
            if (seed == null)
            {
                NotificationService.Instance.Status("Seed not found: " + seedId);
                return 0;
            }

            int planted = 0;
            Pot[] pots = FindObjectsOfType<Pot>();
            for (int i = 0; pots != null && i < pots.Length; i++)
            {
                try
                {
                    Pot pot = pots[i];
                    if (pot == null || pot.ContainsGrowable() || pot.CurrentSoil == null ||
                        pot.NormalizedSoilAmount <= 0.001f)
                    {
                        continue;
                    }

                    string reason;
                    if (!pot.CanAcceptSeed(out reason))
                        continue;

                    pot.PlantSeed_Server(seed.ID, 0f);
                    planted++;
                }
                catch (Exception ex)
                {
                    DebugLogService.Instance.VerboseWarning("Auto-seed failed: " + ex.Message);
                }
            }

            NotificationService.Instance.Status("Pots seeded: " + planted);
            return planted;
        }

        public int FillAllPotsWithBestSoil()
        {
            if (!CanModifyWorld())
                return 0;

            int filled = 0;
            SoilDefinition soil = ResolveBestSoilDefinition();
            if (soil == null)
            {
                NotificationService.Instance.Status("No soil definition found");
                return 0;
            }

            try
            {
                var pots = FindObjectsOfType<Pot>();
                if (pots != null)
                {
                    for (int i = 0; i < pots.Length; i++)
                {
                    Pot pot = pots[i];
                    if (pot == null)
                        continue;

                    try
                    {
                        if (pot.NormalizedSoilAmount >= 0.995f &&
                            pot.CurrentSoil != null)
                        {
                            continue;
                        }

                        if (!ApplySoil(pot, soil))
                            continue;

                        filled++;
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning("[Nugzz] Auto-soil failed: " + ex.Message);
                    }
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[Nugzz] Pot soil scan failed: " + ex.Message);
            }

            SoilDefinition substrate = ResolveDefinition<SoilDefinition>(
                "mushroomsubstrate",
                "substrate",
                "mushroom_substrate") ?? soil;
            try
            {
                var beds = FindObjectsOfType<MushroomBed>();
                if (beds != null && substrate != null)
                {
                    for (int i = 0; i < beds.Length; i++)
                    {
                        MushroomBed bed = beds[i];
                        if (bed == null)
                            continue;

                        try
                        {
                            if (bed.NormalizedSoilAmount >= 0.995f &&
                                bed.CurrentSoil != null)
                            {
                                continue;
                            }

                            if (!ApplySoil(bed, substrate))
                                continue;

                            filled++;
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogWarning("[Nugzz] Auto-substrate failed: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[Nugzz] Mushroom substrate scan failed: " + ex.Message);
            }

            NotificationService.Instance.Status("Soil/substrate filled: " + filled);
            return filled;
        }

        public int CompleteDryingRacks()
        {
            if (!CanModifyWorld())
                return 0;

            int completed = 0;

            try
            {
                var racks = FindObjectsOfType<DryingRack>();
                if (racks == null)
                    return 0;

                for (int r = 0; r < racks.Length; r++)
                {
                    var rack = racks[r];
                    if (rack == null || rack.DryingOperations == null)
                        continue;

                    for (int i = rack.DryingOperations.Count - 1; i >= 0; i--)
                    {
                        try
                        {
                            var op = rack.DryingOperations[i];
                            if (op == null)
                                continue;

                            op.StartQuality = EQuality.Heavenly;
                            op.Time = 999999f;
                            EQuality quality = EQuality.Heavenly;
                            if (rack.GetOutputCapacityForOperation(op, quality) <= 0)
                                continue;

                            rack.TryEndOperation(i, true, quality, UnityEngine.Random.Range(1000, 999999));
                            completed++;
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogWarning("[Nugzz] Drying operation complete failed: " + ex.Message);
                        }
                    }

                    try { rack.RefreshHangingVisuals(); } catch { }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[Nugzz] Drying rack scan failed: " + ex.Message);
            }

            NotificationService.Instance.Status("Drying completed: " + completed);
            return completed;
        }

        public int CompleteChemistryStations()
        {
            if (!CanModifyWorld())
                return 0;

            int completed = 0;
            ChemistryStation[] stations = FindObjectsOfType<ChemistryStation>();
            for (int i = 0; stations != null && i < stations.Length; i++)
            {
                try
                {
                    ChemistryStation station = stations[i];
                    ChemistryCookOperation operation = station?.CurrentCookOperation;
                    if (operation == null || operation.IsComplete() || operation.Recipe == null ||
                        !station.DoesOutputHaveSpace(operation.Recipe))
                    {
                        continue;
                    }

                    operation.Progress(Math.Max(1, operation.Recipe.CookTime_Mins - operation.CurrentTime));
                    if (!operation.IsComplete() || FinalizeChemistryOperation == null)
                        continue;

                    FinalizeChemistryOperation.Invoke(station, null);
                    completed++;
                }
                catch (Exception ex)
                {
                    DebugLogService.Instance.VerboseWarning("Chemistry completion failed: " + ex.Message);
                }
            }

            NotificationService.Instance.Status("Meth cooks completed: " + completed);
            return completed;
        }

        public int CompleteLabOvens()
        {
            if (!CanModifyWorld())
                return 0;

            int completed = 0;
            LabOven[] ovens = FindObjectsOfType<LabOven>();
            for (int i = 0; ovens != null && i < ovens.Length; i++)
            {
                try
                {
                    LabOven oven = ovens[i];
                    OvenCookOperation operation = oven?.CurrentOperation;
                    if (operation == null || operation.IsComplete())
                        continue;

                    operation.UpdateCookProgress(Math.Max(1,
                        operation.GetCookDuration() - operation.CookProgress));
                    oven.SetCookOperation(null, operation, false);
                    oven.SetOvenLit(false);
                    completed++;
                }
                catch (Exception ex)
                {
                    DebugLogService.Instance.VerboseWarning("Lab oven completion failed: " + ex.Message);
                }
            }

            NotificationService.Instance.Status("Lab ovens completed: " + completed);
            return completed;
        }

        public int CompleteMixingStations()
        {
            if (!CanModifyWorld())
                return 0;

            int completed = 0;
            MixingStation[] stations = FindObjectsOfType<MixingStation>();
            for (int i = 0; stations != null && i < stations.Length; i++)
            {
                try
                {
                    MixingStation station = stations[i];
                    if (station == null || station.CurrentMixOperation == null)
                        continue;

                    if (!station.IsMixingDone)
                    {
                        int mixTime = Math.Max(station.CurrentMixTime,
                            station.GetMixTimeForCurrentOperation());
                        station._CurrentMixTime_k__BackingField = mixTime;
                        station.SetMixOperation(null, station.CurrentMixOperation, mixTime);
                        station.MixingDone_Networked();
                    }

                    station.TryCreateOutputItems();
                    completed++;
                }
                catch (Exception ex)
                {
                    DebugLogService.Instance.VerboseWarning("Mixing completion failed: " + ex.Message);
                }
            }

            NotificationService.Instance.Status("Mixing stations completed: " + completed);
            return completed;
        }

        public int CompleteCauldrons()
        {
            if (!CanModifyWorld())
                return 0;

            int completed = 0;
            Cauldron[] cauldrons = FindObjectsOfType<Cauldron>();
            for (int i = 0; cauldrons != null && i < cauldrons.Length; i++)
            {
                try
                {
                    Cauldron cauldron = cauldrons[i];
                    if (cauldron == null || cauldron.GetState() != Cauldron.EState.Cooking)
                        continue;

                    cauldron.RemainingCookTime = 0;
                    cauldron.FinishCookOperation();
                    completed++;
                }
                catch (Exception ex)
                {
                    DebugLogService.Instance.VerboseWarning("Cauldron completion failed: " + ex.Message);
                }
            }

            NotificationService.Instance.Status("Cauldrons completed: " + completed);
            return completed;
        }

        private static bool CanModifyWorld()
        {
            if (!LobbyService.Instance.IsInLobby() || LobbyService.Instance.IsHost())
                return true;

            NotificationService.Instance.Status("World automation is host only");
            return false;
        }

        private static int WaterContainers<T>(T[] containers)
            where T : GrowContainer
        {
            int watered = 0;
            if (containers == null)
                return 0;

            for (int i = 0; i < containers.Length; i++)
            {
                GrowContainer container = containers[i];
                if (container == null)
                    continue;

                try
                {
                    float capacity = container.MoistureCapacity;
                    if (capacity <= 0f)
                        capacity = 1f;

                    if (container.NormalizedMoistureAmount >= 0.995f)
                        continue;

                    container.SetMoistureAmount(capacity);
                    try { container.SyncMoistureData(); } catch { }
                    watered++;
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning("[Nugzz] Auto-water failed: " + ex.Message);
                }
            }

            return watered;
        }

        private static bool ApplySoil(GrowContainer container, SoilDefinition soil)
        {
            if (container == null || soil == null)
                return false;

            try
            {
                if (!container.IsSoilAllowed(soil))
                    return false;
            }
            catch { }

            float capacity = container.SoilCapacity;
            if (capacity <= 0f)
                capacity = 1f;

            container.SetSoil(soil);
            container.SetRemainingSoilUses(soil.Uses > 0 ? soil.Uses : 1);
            container.SetSoilAmount(capacity);
            try { container.SyncSoilData(); } catch { }
            return true;
        }

        private static SoilDefinition ResolveBestSoilDefinition()
        {
            SoilDefinition best = ResolveDefinition<SoilDefinition>("extralonglifesoil") ??
                ResolveDefinition<SoilDefinition>("longlifesoil") ??
                ResolveDefinition<SoilDefinition>("soil");

            if (best != null)
                return best;

            try
            {
                Registry registry = ManagerCacheService.Instance.Registry ??
                    FindObjectOfType<Registry>();
                var items = registry?.GetAllItems();
                if (items == null)
                    return null;

                int bestUses = int.MinValue;
                for (int i = 0; i < items.Count; i++)
                {
                    SoilDefinition soil = TryCastDefinition<SoilDefinition>(items[i]);
                    if (soil == null || soil.Uses <= bestUses)
                        continue;

                    best = soil;
                    bestUses = soil.Uses;
                }
            }
            catch { }

            return best;
        }

        private static T ResolveDefinition<T>(params string[] ids)
            where T : ItemDefinition
        {
            try
            {
                Registry registry = ManagerCacheService.Instance.Registry ??
                    FindObjectOfType<Registry>();
                if (registry == null || ids == null)
                    return null;

                for (int i = 0; i < ids.Length; i++)
                {
                    string id = ids[i];
                    if (string.IsNullOrEmpty(id))
                        continue;

                    T cast = TryCastDefinition<T>(registry._GetItem(id, false));
                    if (cast != null)
                        return cast;
                }
            }
            catch { }

            return null;
        }

        private static T TryCastDefinition<T>(ItemDefinition definition)
            where T : ItemDefinition
        {
            if (definition == null)
                return null;

            try
            {
                return definition.TryCast<T>();
            }
            catch
            {
                return definition as T;
            }
        }
    }
}
