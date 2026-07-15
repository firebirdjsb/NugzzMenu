using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.PlayerScripts;
using UnityEngine;

namespace NugzzMenu.Services
{
    public sealed class ViewModelVisibilityService
    {
        private static readonly ViewModelVisibilityService _instance = new ViewModelVisibilityService();
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
            if (_mode != VisibilityMode.ThirdPerson ||
                _lastPawnPlayer != player ||
                _lastPawnVisible != true)
            {
                SetPawnVisible(player, true, true);
                SetViewmodelVisible(false);
                _mode = VisibilityMode.ThirdPerson;
                _nextHiddenRefreshFrame = Time.frameCount + 12;
                return;
            }

            RefreshHiddenViewmodel();
        }

        public void EnterNativeAvatarView(Player player)
        {
            if (_mode == VisibilityMode.NativeAvatarView &&
                _lastPawnPlayer == player &&
                _lastPawnVisible == true)
                return;

            SetPawnVisible(player, true, true);
            SetViewmodelVisible(false);
            _mode = VisibilityMode.NativeAvatarView;
        }

        public void EnterNativeSkateboard(Player player)
        {
            if (_mode == VisibilityMode.NativeSkateboard &&
                _lastPawnPlayer == player &&
                _lastPawnVisible == true)
                return;

            SetPawnVisible(player, true, true);
            SetViewmodelVisible(false);
            _mode = VisibilityMode.NativeSkateboard;
        }

        public void HidePawnForVehicle(Player player)
        {
            if (_mode == VisibilityMode.HiddenInVehicle &&
                _lastPawnPlayer == player &&
                _lastPawnVisible == false)
                return;

            SetPawnVisible(player, false, true);
            SetViewmodelVisible(true);
            _mode = VisibilityMode.HiddenInVehicle;
        }

        public void RestoreFirstPerson(Player player)
        {
            bool startRepairWindow = _mode != VisibilityMode.VanillaFirstPerson ||
                _lastPawnPlayer != player ||
                _lastPawnVisible != false;
            if (!startRepairWindow && Time.frameCount > _firstPersonRepairUntilFrame)
                return;

            SetPawnVisible(player, false, true);
            SetViewmodelVisible(true);
            _mode = VisibilityMode.VanillaFirstPerson;

            if (startRepairWindow)
                _firstPersonRepairUntilFrame = Time.frameCount + 30;
        }

        public void ReleaseToVanilla(Player player)
        {
            RestoreFirstPerson(player);
        }

        public void EnsureFirstPersonViewmodelVisible()
        {
            if (_mode == VisibilityMode.VanillaFirstPerson)
                SetViewmodelVisible(true);
        }

        public void MaintainFirstPersonRepair()
        {
            if (_mode != VisibilityMode.VanillaFirstPerson ||
                Time.frameCount > _firstPersonRepairUntilFrame)
                return;

            SetPawnVisible(ManagerCacheService.Instance.LocalPlayer, false, true);
            SetViewmodelVisible(true);
        }

        private void RefreshHiddenViewmodel()
        {
            int frame = Time.frameCount;
            if (frame < _nextHiddenRefreshFrame)
                return;

            _nextHiddenRefreshFrame = frame + 12;
            SetViewmodelVisible(false);
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

            try
            {
                ViewmodelAvatar viewmodelAvatar = Singleton<ViewmodelAvatar>.Instance;
                if (viewmodelAvatar != null)
                    viewmodelAvatar.SetVisibility(visible);
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
            try
            {
                if (player.Avatar != null)
                    player.Avatar.SetVisible(visible);
            }
            catch { }

            _lastPawnPlayer = player;
            _lastPawnVisible = visible;
        }
    }
}
