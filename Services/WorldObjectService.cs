using System;
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

        private WorldObjectService() { }

        public int GrowAllPlants()
        {
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

                            // MushroomBed has no public SetGrowthProgress API in the dump. The console
                            // grow command appears to target grow containers internally; for direct API
                            // access we can at least maximize grow speed and moisture so it completes fast.
                            bed.SetMoistureAmount(1f);
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
            int watered = 0;

            watered += WaterContainers<Pot>(FindObjectsOfType<Pot>());
            watered += WaterContainers<MushroomBed>(FindObjectsOfType<MushroomBed>());

            NotificationService.Instance.Status("Watered plants: " + watered);
            return watered;
        }

        public int FillAllPotsWithBestSoil()
        {
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
                if (pots == null)
                    return 0;

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
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[Nugzz] Pot soil scan failed: " + ex.Message);
            }

            NotificationService.Instance.Status("Soil filled: " + filled);
            return filled;
        }

        public int CompleteDryingRacks()
        {
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
