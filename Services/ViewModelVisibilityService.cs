using Il2CppScheduleOne.Combat;
using Il2CppScheduleOne.PlayerScripts;
using UnityEngine;

namespace NugzzMenu.Services
{
    public sealed class ViewModelVisibilityService
    {
        private static readonly ViewModelVisibilityService _instance =
            new ViewModelVisibilityService();
        public static ViewModelVisibilityService Instance => _instance;

        private enum VisibilityMode
        {
            VanillaFirstPerson,
            NativeSkateboard,
            ThirdPerson,
            NativeAvatarView,
            HiddenInVehicle
        }

        private VisibilityMode _mode = VisibilityMode.VanillaFirstPerson;
        private int _nextHiddenRefreshFrame;
        private int _firstPersonRepairUntilFrame;
        private Player _lastPawnPlayer;
        private bool? _lastPawnVisible;

        private ViewModelVisibilityService() { }

        public bool IsCustomMode => _mode != VisibilityMode.VanillaFirstPerson;

        public void EnterThirdPerson(Player player)
        {
            bool transition = _mode != VisibilityMode.ThirdPerson;
            _mode = VisibilityMode.ThirdPerson;
            _firstPersonRepairUntilFrame = 0;

            if (transition)
            {
                SetPawnVisible(player, true, true);
                SetViewmodelVisible(false);
                _nextHiddenRefreshFrame = Time.frameCount + 12;
                return;
            }

            RefreshHiddenViewmodel();
        }

        public void EnterNativeAvatarView(Player player)
        {
            if (_mode == VisibilityMode.NativeAvatarView)
                return;

            _mode = VisibilityMode.NativeAvatarView;
            _firstPersonRepairUntilFrame = 0;
            SetPawnVisible(player, true, true);
            SetViewmodelVisible(false);
        }

        public void EnterNativeSkateboard(Player player)
        {
            _mode = VisibilityMode.NativeSkateboard;
            _firstPersonRepairUntilFrame = 0;

            // Mounting owns its camera, pawn, and viewmodel state. Nugzz only
            // resumes control after vanilla has completed the dismount.
            _lastPawnPlayer = null;
            _lastPawnVisible = null;
        }

        public void HidePawnForVehicle(Player player)
        {
            _mode = VisibilityMode.HiddenInVehicle;
            _firstPersonRepairUntilFrame = 0;
            SetPawnVisible(player, false, true);
            SetViewmodelVisible(false);
        }

        public void RestoreFirstPerson(Player player)
        {
            bool transition = _mode != VisibilityMode.VanillaFirstPerson;
            _mode = VisibilityMode.VanillaFirstPerson;

            if (transition)
            {
                SetPawnVisible(player, false, true);
                _firstPersonRepairUntilFrame = Time.frameCount + 2;
            }

            SetViewmodelVisible(true);
        }

        public void ReleaseToVanilla(Player player)
        {
            RestoreFirstPerson(player);
        }

        public void EnsureFirstPersonState()
        {
            if (_mode != VisibilityMode.VanillaFirstPerson)
                return;

            SetPawnVisible(ManagerCacheService.Instance.LocalPlayer, false, true);
            SetViewmodelVisible(true);
        }

        public void EnsureFirstPersonViewmodelVisible()
        {
            EnsureFirstPersonState();
        }

        public void PreparePunchViewmodel(PunchController punchController)
        {
            if (_mode != VisibilityMode.VanillaFirstPerson)
                return;

            // PunchController owns the bare-hands animator and offsets.
            SetViewmodelVisible(true);
        }

        public void MaintainFirstPersonRepair()
        {
            if (_mode != VisibilityMode.VanillaFirstPerson ||
                Time.frameCount > _firstPersonRepairUntilFrame)
                return;

            SetViewmodelVisible(true);
        }

        private void RefreshHiddenViewmodel()
        {
            if (Time.frameCount < _nextHiddenRefreshFrame)
                return;

            SetViewmodelVisible(false);
            _nextHiddenRefreshFrame = Time.frameCount + 12;
        }

        private static void SetViewmodelVisible(bool visible)
        {
            try
            {
                PlayerInventory inventory = PlayerInventory.Instance;
                if (inventory != null)
                    inventory.SetViewmodelVisible(visible);
            }
            catch { }
        }

        private void SetPawnVisible(Player player, bool visible, bool force)
        {
            if (player == null)
                return;

            if (!force && _lastPawnPlayer == player && _lastPawnVisible == visible)
                return;

            try { player.SetThirdPersonMeshesVisibility(visible); } catch { }
            try { player.SetVisibleToLocalPlayer(visible); } catch { }
            _lastPawnPlayer = player;
            _lastPawnVisible = visible;
        }
    }
}
