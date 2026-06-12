using System;
using System.Reflection;
using Il2CppScheduleOne.Audio;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.UI.ATM;

namespace NugzzMenu.Services
{
    public sealed class EconomyService
    {
        private static readonly EconomyService _instance = new EconomyService();
        public static EconomyService Instance => _instance;
        private EconomyService() { }
        public void AdjustCash(float amount, bool visualizeChange = true, bool playSound = true)
        {
            try
            {
                var moneyManager = ManagerCacheService.Instance.MoneyManager;
                if (moneyManager != null)
                {
                    moneyManager.ChangeCashBalance(amount, visualizeChange, playSound);
                    UnityEngine.Debug.Log($"[Nugzz] Adjusted cash by {amount}");
                }
                else
                {
                    UnityEngine.Debug.LogError("[Nugzz] EconomyService: No MoneyManager found");
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[Nugzz] EconomyService AdjustCash error: {ex.Message}");
            }
        }

        public void AdjustOnlineBalance(float amount)
        {
            try
            {
                var moneyManager = ManagerCacheService.Instance.MoneyManager;
                if (moneyManager != null)
                {
                    moneyManager.CreateOnlineTransaction("Nugzz Mod", amount, 1f, "Added by Nugzz");
                    PlayAtmDepositSound(moneyManager);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[Nugzz] AdjustOnlineBalance error: {ex.Message}");
            }
        }

        private void PlayAtmDepositSound(MoneyManager moneyManager)
        {
            try
            {
                var atms = UnityEngine.Object.FindObjectsOfType<ATMInterface>(true);
                if (atms != null)
                {
                    for (int i = 0; i < atms.Length; i++)
                    {
                        var atm = atms[i];
                        if (atm == null)
                            continue;

                        var field = typeof(ATMInterface).GetField("CompleteSound", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        var sound = field?.GetValue(atm) as AudioSourceController;
                        if (sound != null)
                        {
                            sound.PlayOneShot();
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[Nugzz] ATM deposit sound lookup failed: " + ex.Message);
            }

            try { moneyManager?.PlayCashSound(); } catch { }
        }

    }
}
