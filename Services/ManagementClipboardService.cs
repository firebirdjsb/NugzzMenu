using System;
using Il2CppScheduleOne.EntityFramework;
using Il2CppScheduleOne.Management;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Tools;
using Il2CppScheduleOne.UI.Management;
using UnityEngine;

namespace NugzzMenu.Services
{
    public sealed class ManagementClipboardService
    {
        private static readonly ManagementClipboardService _instance = new ManagementClipboardService();
        public static ManagementClipboardService Instance => _instance;

        private bool _warnedCanvasUpdate;
        private IConfigurable _outlinedConfigurable;
        private BuildableItem _outlinedObject;
        private NPC _outlinedNpc;
        private ITransitEntity _outlinedTransit;
        private int _lastConsumedClickFrame = -1;

        private ManagementClipboardService() { }

        public bool IsActive()
        {
            try
            {
                ManagementClipboard clipboard = ManagementClipboard.Instance;
                return clipboard != null && (clipboard.IsEquipped || clipboard.IsOpen);
            }
            catch
            {
                return false;
            }
        }

        public void PrepareCanvasForVanillaUpdate(ManagementWorldspaceCanvas canvas)
        {
            if (canvas == null || !IsActive())
                return;

            EnsureCanvasLists(canvas);
        }

        public Exception HandleCanvasUpdateException(
            ManagementWorldspaceCanvas canvas,
            Exception exception)
        {
            if (exception == null)
                return null;

            if (!IsActive())
                return exception;

            if (!_warnedCanvasUpdate)
            {
                _warnedCanvasUpdate = true;
                DebugLogService.Instance.VerboseWarning(
                    "Using management clipboard fallback after canvas update error (" +
                    exception.GetType().Name + ")");
            }

            RunCanvasFallback(canvas);
            return null;
        }

        public bool RunObjectSelectorUpdate(ObjectSelector selector)
        {
            if (!IsObjectSelectorActive(selector))
                return false;

            try
            {
                TryGetHoveredObject(selector, out BuildableItem hovered);
                selector.hoveredObj = hovered;
                SetObjectOutline(selector, hovered);

                if (Input.GetMouseButtonDown(0) && hovered != null && TryConsumeClick())
                    selector.ObjectClicked(hovered);

                return true;
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning(
                    "Management object selector fallback failed: " + ex.Message);
                return false;
            }
        }

        public bool RunNpcSelectorUpdate(NPCSelector selector)
        {
            if (!IsNpcSelectorActive(selector))
                return false;

            try
            {
                TryGetHoveredNpc(selector, out NPC hovered);
                selector.hoveredNPC = hovered;
                SetNpcOutline(selector, hovered);

                if (Input.GetMouseButtonDown(0) && hovered != null && TryConsumeClick())
                    selector.NPCClicked(hovered);

                return true;
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning(
                    "Management NPC selector fallback failed: " + ex.Message);
                return false;
            }
        }

        public bool RunTransitSelectorUpdate(TransitEntitySelector selector)
        {
            if (!IsTransitSelectorActive(selector))
                return false;

            try
            {
                TryGetHoveredTransit(selector, out ITransitEntity hovered);
                selector.hoveredObj = hovered;
                SetTransitOutline(selector, hovered);

                if (Input.GetMouseButtonDown(0) && hovered != null && TryConsumeClick())
                    selector.ObjectClicked(hovered);

                return true;
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning(
                    "Management transit selector fallback failed: " + ex.Message);
                return false;
            }
        }

        private void RunCanvasFallback(ManagementWorldspaceCanvas canvas)
        {
            if (canvas == null || !IsActive())
                return;

            try
            {
                EnsureCanvasLists(canvas);
                TryGetHoveredConfigurable(canvas, out IConfigurable hovered);
                canvas.HoveredConfigurable = hovered;
                SetConfigurableOutline(canvas, hovered);

                if (Input.GetMouseButtonDown(0) && hovered != null && TryConsumeClick())
                {
                    canvas.ClearSelection();
                    canvas.AddToSelection(hovered);
                    try
                    {
                        ManagementClipboard clipboard = ManagementClipboard.Instance;
                        clipboard?.SelectionInfo?.Set(canvas.SelectedConfigurables);
                    }
                    catch { }
                }

                try { canvas.UpdateUIs(); } catch { }
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning(
                    "Management clipboard canvas fallback failed: " + ex.Message);
            }
        }

        private static void EnsureCanvasLists(ManagementWorldspaceCanvas canvas)
        {
            try
            {
                if (canvas.SelectedConfigurables == null)
                    canvas.SelectedConfigurables =
                        new Il2CppSystem.Collections.Generic.List<IConfigurable>();
            }
            catch { }

            try
            {
                if (canvas.ShownConfigurables == null)
                {
                    Il2CppSystem.Collections.Generic.List<IConfigurable> current = null;
                    try
                    {
                        ManagementClipboard clipboard = ManagementClipboard.Instance;
                        current = clipboard?.CurrentConfigurables;
                    }
                    catch { }

                    canvas.ShownConfigurables =
                        current ?? new Il2CppSystem.Collections.Generic.List<IConfigurable>();
                }
            }
            catch { }
        }

        private static bool TryGetHoveredConfigurable(
            ManagementWorldspaceCanvas canvas,
            out IConfigurable configurable)
        {
            configurable = null;

            try
            {
                RaycastHit[] hits = GetHits(
                    canvas != null ? canvas.ObjectSelectionLayerMask : new LayerMask { value = -1 },
                    6f);
                float nearest = float.MaxValue;

                for (int i = 0; i < hits.Length; i++)
                {
                    RaycastHit hit = hits[i];
                    if (hit.collider == null || hit.distance >= nearest)
                        continue;

                    IConfigurable candidate = GetConfigurable(hit.collider);
                    if (!IsSelectable(candidate))
                        continue;

                    nearest = hit.distance;
                    configurable = candidate;
                }
            }
            catch { }

            return configurable != null;
        }

        private static bool TryGetHoveredObject(ObjectSelector selector, out BuildableItem item)
        {
            item = null;

            try
            {
                RaycastHit[] hits = GetHits(selector.DetectionMask, ObjectSelector.SELECTION_RANGE);
                float nearest = float.MaxValue;

                for (int i = 0; i < hits.Length; i++)
                {
                    RaycastHit hit = hits[i];
                    if (hit.collider == null || hit.distance >= nearest)
                        continue;

                    BuildableItem candidate = hit.collider.GetComponentInParent<BuildableItem>();
                    if (!IsObjectSelectable(selector, candidate))
                        continue;

                    nearest = hit.distance;
                    item = candidate;
                }
            }
            catch { }

            return item != null;
        }

        private static bool TryGetHoveredNpc(NPCSelector selector, out NPC npc)
        {
            npc = null;

            try
            {
                RaycastHit[] hits = GetHits(selector.DetectionMask, NPCSelector.SELECTION_RANGE);
                float nearest = float.MaxValue;

                for (int i = 0; i < hits.Length; i++)
                {
                    RaycastHit hit = hits[i];
                    if (hit.collider == null || hit.distance >= nearest)
                        continue;

                    NPC candidate = hit.collider.GetComponentInParent<NPC>();
                    if (!IsNpcSelectable(selector, candidate))
                        continue;

                    nearest = hit.distance;
                    npc = candidate;
                }
            }
            catch { }

            return npc != null;
        }

        private static bool TryGetHoveredTransit(
            TransitEntitySelector selector,
            out ITransitEntity entity)
        {
            entity = null;

            try
            {
                RaycastHit[] hits = GetHits(
                    selector.DetectionMask,
                    TransitEntitySelector.SELECTION_RANGE);
                float nearest = float.MaxValue;

                for (int i = 0; i < hits.Length; i++)
                {
                    RaycastHit hit = hits[i];
                    if (hit.collider == null || hit.distance >= nearest)
                        continue;

                    ITransitEntity candidate = GetTransitEntity(hit.collider);
                    if (!IsTransitSelectable(selector, candidate))
                        continue;

                    nearest = hit.distance;
                    entity = candidate;
                }
            }
            catch { }

            return entity != null;
        }

        private static RaycastHit[] GetHits(LayerMask mask, float range)
        {
            Camera camera = null;
            try
            {
                PlayerCamera playerCamera = PlayerCamera.Instance;
                camera = playerCamera?.Camera;
            }
            catch { }

            camera ??= Camera.main;
            if (camera == null)
                return Array.Empty<RaycastHit>();

            return Physics.RaycastAll(
                camera.transform.position,
                camera.transform.forward,
                Mathf.Max(1f, range),
                mask.value,
                QueryTriggerInteraction.Collide);
        }

        private static IConfigurable GetConfigurable(Collider collider)
        {
            if (collider == null)
                return null;

            IConfigurable configurable = TryCastConfigurable(collider);
            if (configurable != null)
                return configurable;

            BuildableItem buildable = collider.GetComponentInParent<BuildableItem>();
            configurable = TryCastConfigurable(buildable);
            if (configurable != null)
                return configurable;

            NPC npc = collider.GetComponentInParent<NPC>();
            configurable = TryCastConfigurable(npc);
            if (configurable != null)
                return configurable;

            Component[] components = collider.GetComponentsInParent<Component>(true);
            if (components == null)
                return null;

            for (int i = 0; i < components.Length; i++)
            {
                configurable = TryCastConfigurable(components[i]);
                if (configurable != null)
                    return configurable;
            }

            return null;
        }

        private static ITransitEntity GetTransitEntity(Collider collider)
        {
            if (collider == null)
                return null;

            ITransitEntity entity = TryCastTransitEntity(collider);
            if (entity != null)
                return entity;

            BuildableItem buildable = collider.GetComponentInParent<BuildableItem>();
            entity = TryCastTransitEntity(buildable);
            if (entity != null)
                return entity;

            Component[] components = collider.GetComponentsInParent<Component>(true);
            if (components == null)
                return null;

            for (int i = 0; i < components.Length; i++)
            {
                entity = TryCastTransitEntity(components[i]);
                if (entity != null)
                    return entity;
            }

            return null;
        }

        private static IConfigurable TryCastConfigurable(UnityEngine.Object obj)
        {
            if (obj == null)
                return null;

            try { return obj.TryCast<IConfigurable>(); }
            catch { return null; }
        }

        private static ITransitEntity TryCastTransitEntity(UnityEngine.Object obj)
        {
            if (obj == null)
                return null;

            try { return obj.TryCast<ITransitEntity>(); }
            catch { return null; }
        }

        private static bool IsSelectable(IConfigurable configurable)
        {
            if (configurable == null)
                return false;

            try { return !configurable.IsDestroyed && configurable.CanBeSelected; }
            catch { return false; }
        }

        private static bool IsObjectSelectable(ObjectSelector selector, BuildableItem item)
        {
            if (selector == null || item == null)
                return false;

            try
            {
                string reason;
                if (!selector.IsObjectTypeValid(item, out reason))
                    return false;

                ObjectSelector.ObjectFilter filter = selector.objectFilter;
                return filter == null || filter.Invoke(item, out reason);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsNpcSelectable(NPCSelector selector, NPC npc)
        {
            if (selector == null || npc == null)
                return false;

            try { return selector.IsNPCTypeValid(npc); }
            catch { return false; }
        }

        private static bool IsTransitSelectable(
            TransitEntitySelector selector,
            ITransitEntity entity)
        {
            if (selector == null || entity == null)
                return false;

            try
            {
                if (!entity.Selectable || entity.IsDestroyed)
                    return false;

                string reason;
                if (!selector.IsObjectTypeValid(entity, out reason))
                    return false;

                TransitEntitySelector.ObjectFilter filter = selector.objectFilter;
                return filter == null || filter.Invoke(entity, out reason);
            }
            catch
            {
                return false;
            }
        }

        private void SetConfigurableOutline(
            ManagementWorldspaceCanvas canvas,
            IConfigurable hovered)
        {
            if (_outlinedConfigurable == hovered)
                return;

            try { _outlinedConfigurable?.HideOutline(); } catch { }
            _outlinedConfigurable = hovered;
            try { hovered?.ShowOutline(canvas.HoveredOutlineColor); } catch { }
        }

        private void SetObjectOutline(ObjectSelector selector, BuildableItem hovered)
        {
            if (_outlinedObject == hovered)
                return;

            try
            {
                if (_outlinedObject != null)
                    selector.SetSelectionOutline(_outlinedObject, false);
            }
            catch { }

            _outlinedObject = hovered;
            try
            {
                if (hovered != null)
                    selector.SetSelectionOutline(hovered, true);
            }
            catch { }
        }

        private void SetNpcOutline(NPCSelector selector, NPC hovered)
        {
            if (_outlinedNpc == hovered)
                return;

            try { _outlinedNpc?.HideOutline(); } catch { }
            _outlinedNpc = hovered;
            try { hovered?.ShowOutline(selector.HoverOutlineColor); } catch { }
        }

        private void SetTransitOutline(
            TransitEntitySelector selector,
            ITransitEntity hovered)
        {
            if (_outlinedTransit == hovered)
                return;

            try
            {
                if (_outlinedTransit != null)
                    selector.SetSelectionOutline(_outlinedTransit, false);
            }
            catch { }

            _outlinedTransit = hovered;
            try
            {
                if (hovered != null)
                    selector.SetSelectionOutline(hovered, true);
            }
            catch { }
        }

        private static bool IsObjectSelectorActive(ObjectSelector selector)
        {
            if (selector == null || !IsOpen(selector))
                return false;

            if (!IsGameObjectActive(selector))
                return false;

            try
            {
                ManagementInterface management = ManagementInterface.Instance;
                return management == null || management.ObjectSelector == selector;
            }
            catch
            {
                return true;
            }
        }

        private static bool IsNpcSelectorActive(NPCSelector selector)
        {
            if (selector == null || !IsOpen(selector))
                return false;

            if (!IsGameObjectActive(selector))
                return false;

            try
            {
                ManagementInterface management = ManagementInterface.Instance;
                return management == null || management.NPCSelector == selector;
            }
            catch
            {
                return true;
            }
        }

        private static bool IsTransitSelectorActive(TransitEntitySelector selector)
        {
            if (selector == null || !IsOpen(selector))
                return false;

            if (!IsGameObjectActive(selector))
                return false;

            try
            {
                ManagementInterface management = ManagementInterface.Instance;
                return management == null || management.TransitEntitySelector == selector;
            }
            catch
            {
                return true;
            }
        }

        private static bool IsGameObjectActive(Component component)
        {
            try
            {
                return component != null &&
                    component.gameObject != null &&
                    component.gameObject.activeInHierarchy;
            }
            catch
            {
                return true;
            }
        }

        private static bool IsOpen(ObjectSelector selector)
        {
            try { return selector.IsOpen; } catch { return false; }
        }

        private static bool IsOpen(NPCSelector selector)
        {
            try { return selector.IsOpen; } catch { return false; }
        }

        private static bool IsOpen(TransitEntitySelector selector)
        {
            try { return selector.IsOpen; } catch { return false; }
        }

        private bool TryConsumeClick()
        {
            int frame = Time.frameCount;
            if (_lastConsumedClickFrame == frame)
                return false;

            _lastConsumedClickFrame = frame;
            return true;
        }
    }
}
