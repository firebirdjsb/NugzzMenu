using HarmonyLib;
using Il2CppScheduleOne.Interaction;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Vehicles;
using UnityEngine;

namespace NugzzMenu.Services
{
    internal static class MotorcyclePromptService
    {
        internal static void Attach(LandVehicle vehicle, MotorcycleChassisSnapshot snapshot)
        {
            InteractableObject target = vehicle.intObj;
            Transform oldPoint = target.displayLocationPoint;
            Collider oldCollider = target.displayLocationCollider;
            string oldMessage = target.message;
            var oldState = target.interactionState;
            var anchor = new GameObject("Nugzz motorcycle prompt anchor");
            anchor.transform.SetParent(vehicle.transform, false);
            anchor.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            snapshot.TrackCreated(anchor);
            snapshot.TrackRestore(() =>
            {
                if (target == null) return;
                target.displayLocationPoint = oldPoint; target.displayLocationCollider = oldCollider;
                target.SetMessage(oldMessage); target.SetInteractableState(oldState);
            });
            // The native collider ClosestPoint varies with camera/player position.
            // One chassis-relative point keeps the label stationary over the bike.
            target.displayLocationPoint = anchor.transform;
            target.displayLocationCollider = null;
            Hover(vehicle);
        }

        internal static bool CanShow(LandVehicle vehicle)
        {
            Player player = Player.Local;
            return vehicle != null && vehicle.IsVisible && player != null && player.CurrentVehicle == null &&
                player.Health != null && player.Health.IsAlive && !player.IsRagdolled &&
                SessionAuthorityService.Instance.FeaturesAllowed &&
                !GameplayStateGateService.Instance.AreFeatureHotkeysBlocked();
        }

        internal static void Hover(LandVehicle vehicle)
        {
            var target = vehicle.intObj;
            string message = vehicle.IsOccupied ? "Motorcycle occupied" : "Ride motorcycle";
            var state = !CanShow(vehicle) || vehicle.IsOccupied
                ? InteractableObject.EInteractableState.Disabled : InteractableObject.EInteractableState.Default;
            if (target.message != message) target.SetMessage(message);
            if (target.interactionState != state) target.SetInteractableState(state);
        }

        internal static bool ShouldShow(InteractableObject target)
        {
            LandVehicle vehicle = target?.GetComponentInParent<LandVehicle>();
            if (!MotorcycleService.Instance.IsMotorcycle(vehicle)) return true;
            // A late native display call must not resurrect a bike after looking
            // away, mounting, opening the menu/phone, despawning or changing scenes.
            var manager = InteractionManager.Instance;
            if (!CanShow(vehicle) || manager == null || manager.HoveredInteractableObject != target) return false;
            Hover(vehicle);
            return true;
        }
    }

    [HarmonyPatch(typeof(InteractableObject), "ShowMessage")]
    internal static class MotorcyclePromptPatch
    {
        private static bool Prefix(InteractableObject __instance)
        { try { return MotorcyclePromptService.ShouldShow(__instance); } catch { return true; } }
    }
}
