using System;
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

                            op.Time = 999999f;
                            EQuality quality = op.GetQuality();
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
    }
}
