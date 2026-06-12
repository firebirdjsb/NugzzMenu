using System;
using System.Linq;
using System.Reflection;
using Il2CppScheduleOne.Audio;
using Il2CppScheduleOne.Money;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.UI.ATM;
using MelonLoader;
using UnityEngine;

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

        public void AddLargeCash(float amount = 999999f)
        {
            AdjustCash(amount);
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

        public float GetCashBalance()
        {
            try
            {
                var moneyManager = ManagerCacheService.Instance.MoneyManager;
                return moneyManager?.cashBalance ?? 0f;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[Nugzz] EconomyService GetCashBalance error: {ex.Message}");
                return 0f;
            }
        }

        public void PayBribe(int turfHeat)
        {
            try
            {
                var player = ManagerCacheService.Instance.LocalPlayer;
                if (player == null) return;

                float bribeAmount = 5000f + turfHeat * 2000f;
                AdjustCash(-bribeAmount, true, true);

                var crimeData = player.CrimeData;
                if (crimeData != null)
                {
                    crimeData.Deescalate();
                    crimeData.SetArrestProgress(0f);
                }

                NotificationService.Instance.Notify($"Bribed -${bribeAmount:N0}");
            }
            catch (Exception ex)
            {
                NotificationService.Instance.Status(ex.Message);
            }
        }
    }
}
