using System;
using System.Reflection;
using Il2CppScheduleOne.Equipping;
using Il2CppScheduleOne.Growing;
using Il2CppScheduleOne.Interaction;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.ObjectScripts.Soil;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.PlayerTasks;
using Il2CppScheduleOne.PlayerTasks.Tasks;
using Il2CppScheduleOne.Trash;
using Il2CppScheduleOne.UI;
using UnityEngine;

namespace NugzzMenu.Services
{
    public sealed class GrowToolFallbackService
    {
        private const float InteractionRange = 3f;
        private const float InteractionRadius = 0.18f;
        private const float FullContainerThreshold = 0.995f;
        private const float EmptyContainerThreshold = 0.001f;

        private static readonly GrowToolFallbackService _instance = new GrowToolFallbackService();
        public static GrowToolFallbackService Instance => _instance;

        private static FieldInfo _equippableItemInstanceField;
        private static bool _equippableItemInstanceFieldSearched;
        private static MethodInfo _sprayBottleCanSprayMethod;
        private static FieldInfo _sprayBottleWaterField;
        private static FieldInfo _sprayBottlePrefabField;
        private float _nextTrimmerDiagnosticTime;
        private float _nextPourableDiagnosticTime;
        private float _nextSeedDiagnosticTime;
        private float _nextMushroomSpawnDiagnosticTime;
        private float _nextActionStatusTime;
        private float _nextPromptScanTime;
        private Equippable _promptTool;
        private GrowContainer _promptContainer;
        private RaycastHit _promptHit;
        private bool _hasPromptTarget;
        private bool _promptFailureReported;

        private GrowToolFallbackService() { }

        public bool RunTrimmersUpdate(Equippable_Trimmers trimmers)
        {
            if (trimmers == null || !IsUsableToolObject(trimmers))
                return false;

            if (!Input.GetMouseButtonDown(0))
            {
                ShowTrimmerPrompt(trimmers);
                return true;
            }

            if (IsTaskActive())
                return true;

            try
            {
                if (!TryGetHoveredGrowContainer(out GrowContainer container, out _))
                {
                    StatusThrottled("No harvest target");
                    return true;
                }

                string reason;
                Pot pot = TryCastComponent<Pot>(container);
                if (pot != null)
                {
                    if (!pot.IsReadyForHarvest(out reason))
                    {
                        StatusThrottled(string.IsNullOrEmpty(reason) ? "Plant not ready" : reason);
                        return true;
                    }

                    TaskManager.Instance.StartTask(
                        new HarvestPlant(pot, trimmers.CanClickAndDrag, trimmers.SoundLoopPrefab));
                    return true;
                }

                MushroomBed bed = TryCastComponent<MushroomBed>(container);
                if (bed != null)
                {
                    if (!bed.IsReadyForHarvest(out reason))
                    {
                        StatusThrottled(string.IsNullOrEmpty(reason) ? "Mushrooms not ready" : reason);
                        return true;
                    }

                    TaskManager.Instance.StartTask(
                        new HarvestMushroomBedTask(bed, trimmers.CanClickAndDrag, trimmers.SoundLoopPrefab));
                    return true;
                }

                StatusThrottled("Target is not harvestable");
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseException("Trimmer fallback failed", ex);
            }

            return true;
        }

        public bool RunPourableUpdate(Equippable_Pourable pourable)
        {
            if (!IsUsableToolObject(pourable))
                return false;

            if (IsWaterContainerTool(pourable))
                return RunWateringCanUpdate(pourable);
            if (IsSoilTool(pourable))
                return RunSoilUpdate(pourable);
            if (IsAdditiveTool(pourable))
                return RunAdditiveUpdate(pourable);

            return false;
        }

        public bool RunSeedUpdate(Equippable_Seed seedTool)
        {
            if (seedTool == null || !IsUsableToolObject(seedTool))
                return false;

            if (!Input.GetMouseButtonDown(0))
            {
                ShowSeedPrompt(seedTool);
                return true;
            }

            if (IsTaskActive())
                return true;

            try
            {
                if (!TryGetHoveredGrowContainer(out GrowContainer container, out _))
                {
                    StatusThrottled("No pot target");
                    return true;
                }

                Pot pot = TryCastComponent<Pot>(container);
                if (pot == null)
                {
                    StatusThrottled("Seeds need a pot");
                    return true;
                }

                string reason;
                if (!pot.CanAcceptSeed(out reason))
                {
                    StatusThrottled(string.IsNullOrEmpty(reason) ? "Pot cannot accept seed" : reason);
                    return true;
                }

                SeedDefinition seed = seedTool.Seed ?? ResolveSeedDefinition(seedTool);
                if (seed == null)
                {
                    StatusThrottled("Seed definition not found");
                    return true;
                }

                if (!CanSpendEquippedStack(seed, seedTool))
                {
                    StatusThrottled("No seed left");
                    return true;
                }

                pot.PlantSeed_Server(seed.name, 0f);
                SpendOneEquippedStack(
                    seed,
                    seedTool,
                    ResolveSeedTrashPrefab(seed));
                NotificationService.Instance.Status("Planted " + SafeName(seed));
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseException("Seed fallback failed", ex);
            }

            return true;
        }

        public bool RunMushroomSpawnUpdate(MushroomSpawnEquipped spawnTool)
        {
            if (spawnTool == null || !IsUsableToolObject(spawnTool))
                return false;

            if (!Input.GetMouseButtonDown(0))
            {
                ShowMushroomSpawnPrompt(spawnTool);
                return true;
            }
            if (IsTaskActive())
                return true;

            try
            {
                if (!TryGetHoveredGrowContainer(out GrowContainer container, out _))
                {
                    StatusThrottled("No mushroom bed target");
                    return true;
                }

                MushroomBed bed = TryCastComponent<MushroomBed>(container);
                if (bed == null)
                {
                    StatusThrottled("Shroom spawn needs a mushroom bed");
                    return true;
                }

                string reason;
                if (!spawnTool.CanApplyToMushroomBed(bed, out reason))
                {
                    StatusThrottled(string.IsNullOrEmpty(reason) ?
                        "Mushroom bed cannot accept spawn" : reason);
                    return true;
                }

                // The native task owns animation, inventory use, and network state.
                spawnTool.StartTask(bed);
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseException("Shroom spawn fallback failed", ex);
            }

            return true;
        }

        public bool RunSprayBottleUpdate(Equippable_SprayBottle sprayBottle)
        {
            if (sprayBottle == null || !IsUsableToolObject(sprayBottle))
                return false;

            if (!Input.GetMouseButtonDown(0))
            {
                ShowSprayBottlePrompt(sprayBottle);
                return true;
            }

            if (IsTaskActive())
                return true;

            try
            {
                if (!TryGetHoveredGrowContainer(out GrowContainer container, out _))
                {
                    StatusThrottled("No mushroom bed target");
                    return true;
                }

                MushroomBed bed = TryCastComponent<MushroomBed>(container);
                if (bed == null)
                {
                    StatusThrottled("Spray bottle needs a mushroom bed");
                    return true;
                }

                WaterContainerInstance water = GetSprayBottleWater(sprayBottle);
                if (water == null || water.CurrentFillAmount <= EmptyContainerThreshold)
                {
                    StatusThrottled("Spray bottle empty");
                    return true;
                }

                string reason;
                if (!CanSpray(sprayBottle, bed, out reason))
                {
                    StatusThrottled(string.IsNullOrEmpty(reason) ?
                        "Mushroom bed cannot be misted" : reason);
                    return true;
                }

                float capacity = bed.MoistureCapacity;
                if (capacity <= 0f)
                    capacity = 1f;

                float missingMoisture = Mathf.Clamp01(
                    1f - GetNormalizedMoistureAmount(bed));
                if (missingMoisture <= 1f - FullContainerThreshold)
                {
                    StatusThrottled("Mushroom bed is already moist");
                    return true;
                }

                float waterUse = capacity * Mathf.Clamp(missingMoisture, 0.05f, 0.2f);
                if (!TrySpendWater(water, waterUse))
                {
                    StatusThrottled("Spray bottle empty");
                    return true;
                }

                bed.SetMoistureAmount(capacity);
                try { bed.SyncMoistureData(); } catch { }

                NotificationService.Instance.Status("Misted mushroom bed");
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseException("Spray bottle fallback failed", ex);
            }

            return true;
        }

        private void ShowTrimmerPrompt(Equippable_Trimmers trimmers)
        {
            try
            {
                if (IsTaskActive() ||
                    !TryGetPromptTarget(trimmers, out GrowContainer container, out RaycastHit hit))
                {
                    return;
                }

                string reason;
                Pot pot = TryCastComponent<Pot>(container);
                if (pot != null && pot.IsReadyForHarvest(out reason))
                {
                    ShowLeftClickPrompt(hit, "Harvest plant");
                    return;
                }

                MushroomBed bed = TryCastComponent<MushroomBed>(container);
                if (bed != null && bed.IsReadyForHarvest(out reason))
                    ShowLeftClickPrompt(hit, "Harvest mushrooms");
            }
            catch (Exception ex)
            {
                ReportPromptFailure(ex);
            }
        }

        private void ShowWateringPrompt(Equippable_Pourable pourable)
        {
            try
            {
                if (IsTaskActive() ||
                    !TryGetPromptTarget(pourable, out GrowContainer container, out RaycastHit hit) ||
                    GetNormalizedMoistureAmount(container) >= FullContainerThreshold ||
                    !HasWater(pourable))
                {
                    return;
                }

                ShowLeftClickPrompt(hit, "Water");
            }
            catch (Exception ex)
            {
                ReportPromptFailure(ex);
            }
        }

        private void ShowSoilPrompt(Equippable_Pourable pourable)
        {
            try
            {
                if (IsTaskActive() ||
                    !TryGetPromptTarget(pourable, out GrowContainer container, out RaycastHit hit))
                {
                    return;
                }

                SoilDefinition soil = ResolveSoilDefinition(pourable) ??
                    ResolveBestSoilDefinition(container);
                string reason;
                if (soil == null ||
                    !CanAcceptSoil(container, soil, out reason) ||
                    !CanSpendEquippedStack(soil, pourable))
                {
                    return;
                }

                ShowLeftClickPrompt(
                    hit,
                    IsMushroomSubstrateTool(pourable) ? "Add substrate" : "Add soil");
            }
            catch (Exception ex)
            {
                ReportPromptFailure(ex);
            }
        }

        private void ShowAdditivePrompt(Equippable_Pourable pourable)
        {
            try
            {
                // The additive's native task state can report busy while the direct
                // fallback remains usable. Keep this display-only gate to targeting;
                // the click path below still performs every authoritative check.
                if (!TryGetPromptTarget(pourable, out _, out RaycastHit hit))
                    return;

                ShowLeftClickPrompt(hit, "Apply additive");
            }
            catch (Exception ex)
            {
                ReportPromptFailure(ex);
            }
        }

        private void ShowSeedPrompt(Equippable_Seed seedTool)
        {
            try
            {
                if (IsTaskActive() ||
                    !TryGetPromptTarget(seedTool, out GrowContainer container, out RaycastHit hit))
                {
                    return;
                }

                Pot pot = TryCastComponent<Pot>(container);
                SeedDefinition seed = seedTool.Seed ?? ResolveSeedDefinition(seedTool);
                string reason;
                if (pot == null ||
                    seed == null ||
                    !pot.CanAcceptSeed(out reason) ||
                    !CanSpendEquippedStack(seed, seedTool))
                {
                    return;
                }

                ShowLeftClickPrompt(hit, "Plant seed");
            }
            catch (Exception ex)
            {
                ReportPromptFailure(ex);
            }
        }

        private void ShowMushroomSpawnPrompt(MushroomSpawnEquipped spawnTool)
        {
            try
            {
                if (IsTaskActive() ||
                    !TryGetPromptTarget(spawnTool, out GrowContainer container, out RaycastHit hit))
                {
                    return;
                }

                MushroomBed bed = TryCastComponent<MushroomBed>(container);
                string reason;
                if (bed != null && spawnTool.CanApplyToMushroomBed(bed, out reason))
                    ShowLeftClickPrompt(hit, "Add shroom spawn");
            }
            catch (Exception ex)
            {
                ReportPromptFailure(ex);
            }
        }

        private void ShowSprayBottlePrompt(Equippable_SprayBottle sprayBottle)
        {
            try
            {
                if (IsTaskActive() ||
                    !TryGetPromptTarget(sprayBottle, out GrowContainer container, out RaycastHit hit))
                {
                    return;
                }

                MushroomBed bed = TryCastComponent<MushroomBed>(container);
                WaterContainerInstance water = GetSprayBottleWater(sprayBottle);
                string reason;
                if (bed != null &&
                    water != null &&
                    water.CurrentFillAmount > EmptyContainerThreshold &&
                    CanSpray(sprayBottle, bed, out reason))
                {
                    ShowLeftClickPrompt(hit, "Mist mushroom bed");
                }
            }
            catch (Exception ex)
            {
                ReportPromptFailure(ex);
            }
        }

        private bool TryGetPromptTarget(
            Equippable tool,
            out GrowContainer container,
            out RaycastHit hit)
        {
            float now = Time.realtimeSinceStartup;
            if (_promptTool != tool || now >= _nextPromptScanTime)
            {
                _promptTool = tool;
                _nextPromptScanTime = now + 0.05f;
                _hasPromptTarget = TryGetHoveredGrowContainer(
                    out _promptContainer,
                    out _promptHit);
            }

            container = _promptContainer;
            hit = _promptHit;
            return _hasPromptTarget && container != null;
        }

        private void ShowLeftClickPrompt(RaycastHit hit, string message)
        {
            try
            {
                InteractionCanvas canvas = InteractionCanvas.Instance;
                if (canvas == null)
                    return;

                Sprite icon = canvas.LeftMouseIcon;
                if (icon == null && InteractionManager.Instance != null)
                    icon = InteractionManager.Instance.icon_LeftMouse;
                if (icon == null)
                    return;

                Vector3 position = hit.point;
                if (hit.collider != null && hit.distance <= 0.001f)
                    position = hit.collider.bounds.center;

                canvas.EnableInteractionDisplay(
                    position,
                    icon,
                    string.Empty,
                    message,
                    canvas.DefaultMessageColor,
                    canvas.DefaultIconColor);
            }
            catch (Exception ex)
            {
                ReportPromptFailure(ex);
            }
        }

        private void ReportPromptFailure(Exception exception)
        {
            if (_promptFailureReported || exception == null)
                return;

            _promptFailureReported = true;
            DebugLogService.Instance.VerboseWarning(
                "Grow interaction prompt failed: " + exception.Message);
        }

        private bool RunWateringCanUpdate(Equippable_Pourable pourable)
        {
            if (!Input.GetMouseButtonDown(0))
            {
                ShowWateringPrompt(pourable);
                return true;
            }

            try
            {
                if (!TryGetHoveredGrowContainer(out GrowContainer container, out _))
                {
                    StatusThrottled("No grow container target");
                    return true;
                }

                float capacity = container.MoistureCapacity;
                if (capacity <= 0f)
                    capacity = 1f;

                float missingMoisture = Mathf.Clamp01(1f - GetNormalizedMoistureAmount(container));
                if (missingMoisture <= 1f - FullContainerThreshold)
                {
                    StatusThrottled("Already fully watered");
                    return true;
                }

                float waterUse = capacity * Mathf.Clamp(missingMoisture, 0.05f, 0.2f);
                if (!TrySpendWater(pourable, waterUse))
                {
                    StatusThrottled("Watering can empty");
                    return true;
                }

                container.SetMoistureAmount(capacity);
                try { container.SyncMoistureData(); } catch { }

                NotificationService.Instance.Status("Watered target");
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseException("Watering fallback failed", ex);
            }

            return true;
        }

        private bool RunSoilUpdate(Equippable_Pourable pourable)
        {
            if (!Input.GetMouseButtonDown(0))
            {
                ShowSoilPrompt(pourable);
                return true;
            }

            try
            {
                if (!TryGetHoveredGrowContainer(out GrowContainer container, out _))
                {
                    StatusThrottled("No grow container target");
                    return true;
                }

                SoilDefinition soil = ResolveSoilDefinition(pourable) ??
                    ResolveBestSoilDefinition(container);
                if (soil == null)
                {
                    StatusThrottled("Soil definition not found");
                    return true;
                }

                string reason;
                if (!CanAcceptSoil(container, soil, out reason))
                {
                    StatusThrottled(string.IsNullOrEmpty(reason) ? "Soil not needed" : reason);
                    return true;
                }

                if (!CanSpendEquippedStack(soil, pourable))
                {
                    StatusThrottled("No soil left");
                    return true;
                }

                if (!ApplySoil(container, soil))
                {
                    StatusThrottled("Soil not allowed here");
                    return true;
                }

                SpendOneEquippedStack(
                    soil,
                    pourable,
                    ResolvePourableTrashPrefab(pourable));
                NotificationService.Instance.Status("Filled soil");
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseException("Soil fallback failed", ex);
            }

            return true;
        }

        private bool RunAdditiveUpdate(Equippable_Pourable pourable)
        {
            if (!Input.GetMouseButtonDown(0))
            {
                ShowAdditivePrompt(pourable);
                return true;
            }

            try
            {
                if (!TryGetHoveredGrowContainer(out GrowContainer container, out _))
                {
                    StatusThrottled("No grow container target");
                    return true;
                }

                AdditiveDefinition additive = ResolveAdditiveDefinition(pourable);
                if (additive == null)
                {
                    StatusThrottled("Additive definition not found");
                    return true;
                }

                if (IsAdditiveAlreadyApplied(container, additive))
                {
                    StatusThrottled("Additive already applied");
                    return true;
                }

                string reason;
                if (!container.CanApplyAdditive(additive, out reason))
                {
                    StatusThrottled(string.IsNullOrEmpty(reason) ? "Cannot apply additive" : reason);
                    return true;
                }

                if (!CanSpendEquippedStack(additive, pourable))
                {
                    StatusThrottled("No additive left");
                    return true;
                }

                container.ApplyAdditive_Server(additive.name);
                SpendOneEquippedStack(
                    additive,
                    pourable,
                    ResolvePourableTrashPrefab(pourable));
                NotificationService.Instance.Status("Applied " + SafeName(additive));
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseException("Additive fallback failed", ex);
            }

            return true;
        }

        private static bool CanAcceptSoil(
            GrowContainer container,
            SoilDefinition soil,
            out string reason)
        {
            reason = null;
            if (container == null || soil == null)
            {
                reason = "Soil target missing";
                return false;
            }

            try
            {
                if (!container.IsSoilAllowed(soil))
                {
                    reason = "Soil not allowed here";
                    return false;
                }
            }
            catch { }

            if (IsContainerFullySoiled(container))
            {
                reason = "Soil already full";
                return false;
            }

            SoilDefinition currentSoil = null;
            try { currentSoil = container.CurrentSoil; } catch { }
            if (currentSoil != null &&
                GetNormalizedSoilAmount(container) > EmptyContainerThreshold &&
                !DoesDefinitionMatch(currentSoil, soil))
            {
                reason = "Different soil already in pot";
                return false;
            }

            return true;
        }

        private static bool IsContainerFullySoiled(GrowContainer container)
        {
            if (container == null)
                return false;

            try
            {
                if (container.IsFullyFilledWithSoil)
                    return true;
            }
            catch { }

            try
            {
                return container.CurrentSoil != null &&
                    container.NormalizedSoilAmount >= FullContainerThreshold;
            }
            catch
            {
                return false;
            }
        }

        private static float GetNormalizedSoilAmount(GrowContainer container)
        {
            try
            {
                return Mathf.Clamp01(container.NormalizedSoilAmount);
            }
            catch
            {
                return 0f;
            }
        }

        private static float GetNormalizedMoistureAmount(GrowContainer container)
        {
            try
            {
                return Mathf.Clamp01(container.NormalizedMoistureAmount);
            }
            catch
            {
                return 0f;
            }
        }

        private static bool IsAdditiveAlreadyApplied(
            GrowContainer container,
            AdditiveDefinition additive)
        {
            if (container == null || additive == null)
                return false;

            string additiveId = GetDefinitionId(additive);
            if (!string.IsNullOrEmpty(additiveId))
            {
                try
                {
                    if (container.IsAdditiveApplied(additiveId))
                        return true;
                }
                catch { }
            }

            try
            {
                var appliedAdditives = container.AppliedAdditives;
                if (appliedAdditives == null)
                    return false;

                for (int i = 0; i < appliedAdditives.Count; i++)
                {
                    if (DoesDefinitionMatch(appliedAdditives[i], additive))
                        return true;
                }
            }
            catch { }

            return false;
        }

        public Exception HandleToolUpdateException(Equippable tool, Exception exception)
        {
            if (exception == null)
                return null;

            bool pourable = tool is Equippable_Pourable;
            bool seed = tool is Equippable_Seed;
            bool mushroomSpawn = tool is MushroomSpawnEquipped;
            float now = Time.realtimeSinceStartup;
            if ((!pourable && !seed && !mushroomSpawn && now >= _nextTrimmerDiagnosticTime) ||
                (pourable && now >= _nextPourableDiagnosticTime) ||
                (seed && now >= _nextSeedDiagnosticTime) ||
                (mushroomSpawn && now >= _nextMushroomSpawnDiagnosticTime))
            {
                if (pourable)
                    _nextPourableDiagnosticTime = now + 5f;
                else if (seed)
                    _nextSeedDiagnosticTime = now + 5f;
                else if (mushroomSpawn)
                    _nextMushroomSpawnDiagnosticTime = now + 5f;
                else
                    _nextTrimmerDiagnosticTime = now + 5f;

                DebugLogService.Instance.VerboseWarning(BuildDiagnostic(tool, exception));
            }

            return null;
        }

        private static bool TryGetHoveredGrowContainer(
            out GrowContainer container,
            out RaycastHit hit)
        {
            container = null;
            hit = default;

            Camera camera = GetCamera();
            if (camera == null)
                return false;

            RaycastHit[] hits = Physics.SphereCastAll(
                camera.transform.position,
                InteractionRadius,
                camera.transform.forward,
                InteractionRange,
                -5,
                QueryTriggerInteraction.Collide);
            if (hits == null)
                return false;

            float bestDistance = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit candidate = hits[i];
                if (candidate.collider == null ||
                    IsLocalPlayerCollider(candidate.collider) ||
                    IsEquippedToolCollider(candidate.collider))
                {
                    continue;
                }

                GrowContainer growContainer = ResolveGrowContainer(candidate.collider);
                if (growContainer == null || candidate.distance >= bestDistance)
                    continue;

                container = growContainer;
                hit = candidate;
                bestDistance = candidate.distance;
            }

            return container != null;
        }

        private static GrowContainer ResolveGrowContainer(Collider collider)
        {
            if (collider == null)
                return null;

            try
            {
                GrowContainer direct = collider.GetComponentInParent<GrowContainer>();
                if (direct != null)
                    return direct;
            }
            catch { }

            try
            {
                MushroomBedInteraction interaction =
                    collider.GetComponentInParent<MushroomBedInteraction>();
                if (interaction != null && interaction._bed != null)
                    return interaction._bed;
            }
            catch { }

            try
            {
                GrowContainer child = collider.GetComponentInChildren<GrowContainer>(true);
                if (child != null)
                    return child;
            }
            catch { }

            try
            {
                MushroomBedInteraction interaction =
                    collider.GetComponentInChildren<MushroomBedInteraction>(true);
                return interaction?._bed;
            }
            catch
            {
                return null;
            }
        }

        private static Camera GetCamera()
        {
            try
            {
                PlayerCamera playerCamera = PlayerCamera.Instance;
                if (playerCamera != null && playerCamera.Camera != null)
                    return playerCamera.Camera;
            }
            catch { }

            return Camera.main;
        }

        private static bool IsWaterContainerTool(Equippable_Pourable pourable)
        {
            if (pourable == null)
                return false;

            try
            {
                if (pourable.TryCast<PourableWaterContainerEquipped>() != null)
                    return true;
            }
            catch { }

            string typeName = pourable.GetType().Name;
            return typeName.IndexOf("WaterContainer", StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.IndexOf("Watering", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsSoilTool(Equippable_Pourable pourable)
        {
            if (pourable == null)
                return false;

            if (IsMushroomSubstrateTool(pourable))
                return true;

            string toolName = SafeName(pourable);
            string typeName = pourable.GetType().Name;
            return toolName.IndexOf("Soil", StringComparison.OrdinalIgnoreCase) >= 0 ||
                toolName.IndexOf("Substrate", StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.IndexOf("Substrate", StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeName.IndexOf("Soil", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsMushroomSubstrateTool(Equippable_Pourable pourable)
        {
            if (pourable == null)
                return false;

            string key = NormalizeKey(SafeName(pourable));
            if (key.StartsWith("msequippable", StringComparison.Ordinal) ||
                key.Contains("mushroomsubstrate"))
            {
                return true;
            }

            try
            {
                string prefabKey = NormalizeKey(SafeName(pourable.PourablePrefab));
                return prefabKey.Contains("mushroomsubstrate") ||
                    prefabKey.StartsWith("mspourable", StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsAdditiveTool(Equippable_Pourable pourable)
        {
            if (pourable == null)
                return false;

            try
            {
                if (pourable.TryCast<Equippable_Additive>() != null)
                    return true;
            }
            catch { }

            string toolName = SafeName(pourable);
            return toolName.IndexOf("Fertilizer", StringComparison.OrdinalIgnoreCase) >= 0 ||
                toolName.IndexOf("PGR", StringComparison.OrdinalIgnoreCase) >= 0 ||
                toolName.IndexOf("SpeedGrow", StringComparison.OrdinalIgnoreCase) >= 0 ||
                toolName.IndexOf("Speed Grow", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsUsableToolObject(Equippable tool)
        {
            try
            {
                return tool != null &&
                    tool.enabled &&
                    tool.gameObject != null &&
                    tool.gameObject.activeInHierarchy;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsTaskActive()
        {
            try
            {
                TaskManager manager = TaskManager.Instance;
                return manager != null &&
                    manager.currentTask != null &&
                    manager.currentTask.TaskActive;
            }
            catch
            {
                return false;
            }
        }

        private static bool HasWater(Equippable_Pourable pourable)
        {
            try
            {
                WaterContainerInstance water =
                    TryCastItemInstance<WaterContainerInstance>(GetHeldItemInstance(pourable));
                if (water == null)
                    water = TryCastItemInstance<WaterContainerInstance>(GetItemInstance(pourable));

                return water != null && water.CurrentFillAmount > 0.001f;
            }
            catch
            {
                return false;
            }
        }

        private static WaterContainerInstance GetSprayBottleWater(
            Equippable_SprayBottle sprayBottle)
        {
            WaterContainerInstance water = TryCastItemInstance<WaterContainerInstance>(
                GetHeldItemInstance(sprayBottle));
            if (water == null)
                water = TryCastItemInstance<WaterContainerInstance>(GetItemInstance(sprayBottle));

            try
            {
                if (_sprayBottleWaterField == null)
                {
                    _sprayBottleWaterField = typeof(Equippable_SprayBottle).GetField(
                        "_waterContainerInstance",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                }

                if (water == null)
                    water = _sprayBottleWaterField?.GetValue(sprayBottle) as WaterContainerInstance;
                else
                    _sprayBottleWaterField?.SetValue(sprayBottle, water);
            }
            catch { }

            return water;
        }

        private static GameObject GetSprayBottlePrefab(Equippable_SprayBottle sprayBottle)
        {
            try
            {
                if (_sprayBottlePrefabField == null)
                {
                    _sprayBottlePrefabField = typeof(Equippable_SprayBottle).GetField(
                        "_sprayablePrefab",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                }

                return _sprayBottlePrefabField?.GetValue(sprayBottle) as GameObject;
            }
            catch
            {
                return null;
            }
        }

        private static bool CanSpray(
            Equippable_SprayBottle sprayBottle,
            MushroomBed bed,
            out string reason)
        {
            reason = string.Empty;
            try
            {
                if (_sprayBottleCanSprayMethod == null)
                {
                    _sprayBottleCanSprayMethod = typeof(Equippable_SprayBottle).GetMethod(
                        "CanSpray",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                }

                if (_sprayBottleCanSprayMethod == null)
                    return bed != null;

                object[] arguments = { bed, null };
                bool canSpray = (bool)_sprayBottleCanSprayMethod.Invoke(
                    sprayBottle,
                    arguments);
                reason = arguments[1] as string ?? string.Empty;
                return canSpray;
            }
            catch
            {
                return bed != null;
            }
        }

        private static bool TrySpendWater(Equippable_Pourable pourable, float amount)
        {
            try
            {
                WaterContainerInstance water =
                    TryCastItemInstance<WaterContainerInstance>(GetHeldItemInstance(pourable));
                if (water == null)
                    water = TryCastItemInstance<WaterContainerInstance>(GetItemInstance(pourable));
                if (water == null || water.CurrentFillAmount <= 0.001f)
                    return false;

                float spend = Mathf.Max(0.05f, amount);
                if (water.CurrentFillAmount < spend)
                    spend = water.CurrentFillAmount;

                water.ChangeFillAmount(-spend);
                ReplicateEquippedSlot();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TrySpendWater(WaterContainerInstance water, float amount)
        {
            try
            {
                if (water == null || water.CurrentFillAmount <= EmptyContainerThreshold)
                    return false;

                float spend = Mathf.Max(0.05f, amount);
                if (water.CurrentFillAmount < spend)
                    spend = water.CurrentFillAmount;

                water.ChangeFillAmount(-spend);
                ReplicateEquippedSlot();
                return true;
            }
            catch
            {
                return false;
            }
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
            container.SetRemainingSoilUses(Mathf.Max(1, soil.Uses));
            container.SetSoilAmount(capacity);
            try { container.SyncSoilData(); } catch { }
            return true;
        }

        private static SoilDefinition ResolveSoilDefinition(Equippable_Pourable pourable)
        {
            try
            {
                PourableSoil soilPourable = pourable?.PourablePrefab?.TryCast<PourableSoil>();
                if (soilPourable != null && soilPourable.SoilDefinition != null)
                    return soilPourable.SoilDefinition;
            }
            catch { }

            SoilDefinition fromItem = TryCastDefinition<SoilDefinition>(
                GetHeldItemInstance(pourable)?.Definition);
            if (fromItem == null)
                fromItem = TryCastDefinition<SoilDefinition>(
                    GetItemInstance(pourable)?.Definition);
            if (fromItem != null)
                return fromItem;

            string key = NormalizeKey(SafeName(pourable));
            if (IsMushroomSubstrateTool(pourable) || key.Contains("substrate"))
                return ResolveDefinition<SoilDefinition>(
                    "mushroomsubstrate",
                    "substrate",
                    "mushroom_substrate");
            if (key.Contains("extralonglife"))
                return ResolveDefinition<SoilDefinition>("extralonglifesoil");
            if (key.Contains("longlife"))
                return ResolveDefinition<SoilDefinition>("longlifesoil");

            return ResolveDefinition<SoilDefinition>("soil");
        }

        private static SoilDefinition ResolveBestSoilDefinition(GrowContainer container)
        {
            SoilDefinition best = ResolveDefinition<SoilDefinition>("extralonglifesoil") ??
                ResolveDefinition<SoilDefinition>("longlifesoil") ??
                ResolveDefinition<SoilDefinition>("soil");

            if (best != null)
                return best;

            try
            {
                var registry = ManagerCacheService.Instance.Registry;
                var items = registry?.GetAllItems();
                if (items == null)
                    return null;

                int bestUses = int.MinValue;
                for (int i = 0; i < items.Count; i++)
                {
                    SoilDefinition soil = TryCastDefinition<SoilDefinition>(items[i]);
                    if (soil == null)
                        continue;
                    if (container != null && !container.IsSoilAllowed(soil))
                        continue;
                    if (soil.Uses <= bestUses)
                        continue;

                    best = soil;
                    bestUses = soil.Uses;
                }
            }
            catch { }

            return best;
        }

        private static AdditiveDefinition ResolveAdditiveDefinition(Equippable_Pourable pourable)
        {
            try
            {
                PourableAdditive additivePourable = pourable?.PourablePrefab?.TryCast<PourableAdditive>();
                if (additivePourable != null && additivePourable.AdditiveDefinition != null)
                    return additivePourable.AdditiveDefinition;
            }
            catch { }

            AdditiveDefinition fromItem = TryCastDefinition<AdditiveDefinition>(
                GetHeldItemInstance(pourable)?.Definition);
            if (fromItem == null)
                fromItem = TryCastDefinition<AdditiveDefinition>(
                    GetItemInstance(pourable)?.Definition);
            if (fromItem != null)
                return fromItem;

            string key = NormalizeKey(SafeName(pourable));
            if (key.Contains("fertilizer"))
                return ResolveDefinition<AdditiveDefinition>("fertilizer");
            if (key.Contains("speedgrow"))
                return ResolveDefinition<AdditiveDefinition>("speedgrow");
            if (key.Contains("pgr"))
                return ResolveDefinition<AdditiveDefinition>("pgr");

            return null;
        }

        private static SeedDefinition ResolveSeedDefinition(Equippable_Seed seedTool)
        {
            SeedDefinition fromItem = TryCastDefinition<SeedDefinition>(
                GetHeldItemInstance(seedTool)?.Definition);
            if (fromItem == null)
                fromItem = TryCastDefinition<SeedDefinition>(
                    GetItemInstance(seedTool)?.Definition);
            if (fromItem != null)
                return fromItem;

            string key = NormalizeKey(SafeName(seedTool));
            if (key.Contains("sourdiesel"))
                return ResolveDefinition<SeedDefinition>("sourdieselseed");
            if (key.Contains("greencrack"))
                return ResolveDefinition<SeedDefinition>("greencrackseed");
            if (key.Contains("granddaddypurple"))
                return ResolveDefinition<SeedDefinition>("granddaddypurpleseed");
            if (key.Contains("coca"))
                return ResolveDefinition<SeedDefinition>("cocaseed");
            if (key.Contains("ogkush") || key.Contains("kush"))
                return ResolveDefinition<SeedDefinition>("ogkushseed");

            return null;
        }

        private static T ResolveDefinition<T>(params string[] ids)
            where T : ItemDefinition
        {
            try
            {
                var registry = ManagerCacheService.Instance.Registry;
                if (registry == null || ids == null)
                    return null;

                for (int i = 0; i < ids.Length; i++)
                {
                    string id = ids[i];
                    if (string.IsNullOrEmpty(id))
                        continue;

                    ItemDefinition definition = registry._GetItem(id, false);
                    T cast = TryCastDefinition<T>(definition);
                    if (cast != null)
                        return cast;
                }
            }
            catch { }

            return null;
        }

        private static bool CanSpendEquippedStack(ItemDefinition expectedDefinition, Equippable tool)
        {
            try
            {
                HotbarSlot slot = GetEquippedSlot();
                ItemInstance item = slot?.ItemInstance ?? GetHeldItemInstance(tool);
                if (item == null || item.Definition == null)
                    return false;
                if (!DoesDefinitionMatch(item.Definition, expectedDefinition))
                    return false;

                if (slot != null)
                    return slot.Quantity > 0;

                try { return item.Quantity > 0; }
                catch { return true; }
            }
            catch
            {
                return false;
            }
        }

        private static bool SpendOneEquippedStack(
            ItemDefinition expectedDefinition,
            Equippable tool,
            TrashItem trashPrefab)
        {
            try
            {
                HotbarSlot slot = GetEquippedSlot();
                ItemInstance item = slot?.ItemInstance ?? GetHeldItemInstance(tool);
                if (item == null || item.Definition == null ||
                    !DoesDefinitionMatch(item.Definition, expectedDefinition))
                {
                    return false;
                }

                if (slot != null)
                {
                    if (slot.Quantity <= 1)
                        slot.ClearStoredInstance(false);
                    else
                        slot.ChangeQuantity(-1, false);

                    try { slot.ReplicateStoredInstance(); } catch { }
                    try { ManagerCacheService.Instance.PlayerInventory?.Reequip(); } catch { }
                    SpawnConsumedItemTrash(trashPrefab);
                    return true;
                }

                Player player = ManagerCacheService.Instance.LocalPlayer;
                string id = GetDefinitionId(expectedDefinition);
                if (player != null && !string.IsNullOrEmpty(id))
                {
                    player.RemoveEquippedItemFromInventory(id, 1);
                    SpawnConsumedItemTrash(trashPrefab);
                    return true;
                }
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning(
                    "Failed to spend equipped grow item: " + ex.Message);
            }

            return false;
        }

        private static TrashItem ResolveSeedTrashPrefab(SeedDefinition seed)
        {
            try { return seed?.FunctionSeedPrefab?.TrashPrefab; }
            catch { return null; }
        }

        private static TrashItem ResolvePourableTrashPrefab(Equippable_Pourable pourable)
        {
            try { return pourable?.PourablePrefab?.TrashItem; }
            catch { return null; }
        }

        private static void SpawnConsumedItemTrash(TrashItem prefab)
        {
            try
            {
                if (prefab == null || string.IsNullOrWhiteSpace(prefab.ID))
                    return;

                TrashManager manager = TrashManager.Instance;
                Player player = ManagerCacheService.Instance.LocalPlayer;
                if (manager == null || player == null || player.transform == null)
                    return;

                Transform origin = player.transform;
                Vector3 position = origin.position +
                    origin.forward * 0.35f +
                    Vector3.up * 0.8f;
                Vector3 velocity = origin.forward * 0.75f + Vector3.up * 0.2f;
                Quaternion rotation = Quaternion.Euler(0f, origin.eulerAngles.y, 0f);

                manager.CreateTrashItem(
                    prefab.ID,
                    position,
                    rotation,
                    velocity,
                    string.Empty,
                    false);
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning(
                    "Failed to create consumed grow-item trash: " + ex.Message);
            }
        }

        private static HotbarSlot GetEquippedSlot()
        {
            try
            {
                PlayerInventory inventory = ManagerCacheService.Instance.PlayerInventory;
                if (inventory != null && inventory.equippedSlot != null)
                    return inventory.equippedSlot;
            }
            catch { }

            return null;
        }

        private static ItemInstance GetHeldItemInstance(Equippable tool)
        {
            try
            {
                HotbarSlot slot = GetEquippedSlot();
                if (slot?.ItemInstance != null)
                    return slot.ItemInstance;
            }
            catch { }

            try
            {
                PlayerInventory inventory = ManagerCacheService.Instance.PlayerInventory;
                if (inventory?.EquippedItem != null)
                    return inventory.EquippedItem;
            }
            catch { }

            return GetItemInstance(tool);
        }

        private static bool DoesDefinitionMatch(
            ItemDefinition actualDefinition,
            ItemDefinition expectedDefinition)
        {
            if (actualDefinition == null || expectedDefinition == null)
                return false;

            if (actualDefinition == expectedDefinition)
                return true;

            string actual = NormalizeKey(GetDefinitionId(actualDefinition));
            string expected = NormalizeKey(GetDefinitionId(expectedDefinition));
            if (string.IsNullOrEmpty(actual))
                actual = NormalizeKey(actualDefinition.name);
            if (string.IsNullOrEmpty(expected))
                expected = NormalizeKey(expectedDefinition.name);

            return !string.IsNullOrEmpty(actual) &&
                !string.IsNullOrEmpty(expected) &&
                actual == expected;
        }

        private static string GetDefinitionId(ItemDefinition definition)
        {
            if (definition == null)
                return string.Empty;

            try
            {
                object value = definition.GetType()
                    .GetProperty("ID", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.GetValue(definition, null);
                string id = value as string;
                if (!string.IsNullOrEmpty(id))
                    return id;
            }
            catch { }

            try
            {
                object value = definition.GetType()
                    .GetField("ID", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.GetValue(definition);
                string id = value as string;
                if (!string.IsNullOrEmpty(id))
                    return id;
            }
            catch { }

            try { return definition.name ?? string.Empty; }
            catch { return string.Empty; }
        }

        private static void ReplicateEquippedSlot()
        {
            try
            {
                HotbarSlot slot = GetEquippedSlot();
                if (slot != null)
                    slot.ReplicateStoredInstance();
            }
            catch { }
        }

        private static T TryCastItemInstance<T>(ItemInstance instance)
            where T : ItemInstance
        {
            if (instance == null)
                return null;

            try
            {
                return instance.TryCast<T>();
            }
            catch
            {
                return instance as T;
            }
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

        private static ItemInstance GetItemInstance(Equippable tool)
        {
            if (tool == null)
                return null;

            try
            {
                FieldInfo field = GetEquippableItemInstanceField();
                return field?.GetValue(tool) as ItemInstance;
            }
            catch
            {
                return null;
            }
        }

        private static FieldInfo GetEquippableItemInstanceField()
        {
            if (_equippableItemInstanceFieldSearched)
                return _equippableItemInstanceField;

            _equippableItemInstanceFieldSearched = true;

            try
            {
                for (Type type = typeof(Equippable);
                     type != null && type != typeof(MonoBehaviour);
                     type = type.BaseType)
                {
                    FieldInfo[] fields = type.GetFields(
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic |
                        BindingFlags.DeclaredOnly);

                    for (int i = 0; i < fields.Length; i++)
                    {
                        FieldInfo field = fields[i];
                        if (field == null)
                            continue;

                        if (typeof(ItemInstance).IsAssignableFrom(field.FieldType) ||
                            field.Name.IndexOf("itemInstance", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            _equippableItemInstanceField = field;
                            return field;
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        private static T TryCastComponent<T>(GrowContainer container)
            where T : GrowContainer
        {
            if (container == null)
                return null;

            try
            {
                return container.TryCast<T>();
            }
            catch
            {
                return container as T;
            }
        }

        private void StatusThrottled(string text)
        {
            float now = Time.realtimeSinceStartup;
            if (now < _nextActionStatusTime)
                return;

            _nextActionStatusTime = now + 1.5f;
            NotificationService.Instance.Status(text);
        }

        private static bool IsLocalPlayerCollider(Collider collider)
        {
            try
            {
                var player = ManagerCacheService.Instance.LocalPlayer;
                return collider != null &&
                    player != null &&
                    collider.transform != null &&
                    player.transform != null &&
                    collider.transform.root == player.transform.root;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsEquippedToolCollider(Collider collider)
        {
            if (collider == null)
                return false;

            try
            {
                if (collider.GetComponentInParent<Equippable>() != null)
                    return true;
            }
            catch { }

            return false;
        }

        private static string BuildDiagnostic(Equippable tool, Exception exception)
        {
            string hovered = "none";
            try
            {
                if (TryGetHoveredGrowContainer(out GrowContainer container, out RaycastHit hit))
                {
                    hovered = SafeName(container) +
                        " collider=" + SafeName(hit.collider) +
                        " distance=" + hit.distance.ToString("0.00");
                }
            }
            catch { }

            ItemInstance item = GetItemInstance(tool);
            return "Grow tool fallback caught " +
                (exception.GetType().Name ?? "Exception") +
                " tool=" + SafeName(tool) +
                " toolType=" + (tool?.GetType().Name ?? "null") +
                " itemType=" + (item?.GetType().Name ?? "null") +
                " itemDef=" + (item?.Definition?.name ?? "null") +
                " hovered=" + hovered;
        }

        private static string SafeName(UnityEngine.Object obj)
        {
            try { return obj != null ? obj.name : "null"; }
            catch { return "unknown"; }
        }

        private static string NormalizeKey(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            char[] chars = new char[value.Length];
            int count = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char c = char.ToLowerInvariant(value[i]);
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                    chars[count++] = c;
            }

            return new string(chars, 0, count);
        }
    }
}
