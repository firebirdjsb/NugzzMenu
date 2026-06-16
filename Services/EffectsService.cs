using System;
using System.Collections.Generic;
using Il2CppScheduleOne;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Product;
using UnityEngine;
using GameEffect = Il2CppScheduleOne.Effects.Effect;

namespace NugzzMenu.Services
{
    public sealed class EffectsService
    {
        private static readonly EffectsService _instance = new EffectsService();
        public static EffectsService Instance => _instance;
        private const string CustomProductPrefix = "nugzz_fx_";
        private const float CustomProductConsumeDelay = 0.75f;
        private const float CustomProductTimeout = 8f;
        private const float LethalKillDelay = 7.5f;

        private readonly string[] _effectIds =
        {
            "AntiGravity", "Athletic", "Balding", "BrightEyed", "Calming",
            "CalorieDense", "Cyclopean", "Disorienting", "Electrifying",
            "Energizing", "Euphoric", "Explosive", "Focused", "Foggy",
            "Gingeritis", "LongFaced", "Glowie", "Jennerising", "Laxative",
            "Lethal", "Munchies", "Paranoia", "Refreshing", "Schizophrenic",
            "Sedating", "Seizure", "Shrinking", "Slippery", "Smelly",
            "Sneaky", "Spicy", "ThoughtProvoking", "Toxic", "TropicThunder",
            "Zombifying"
        };

        private readonly string[] _effectLabels =
        {
            "Anti-Gravity", "Athletic", "Balding", "Bright Eyed", "Calming",
            "Calorie Dense", "Cyclopean", "Disorienting", "Electrifying",
            "Energizing", "Euphoric", "Explosive", "Focused", "Foggy",
            "Gingeritis", "Long Faced", "Glowie", "Jennerising", "Laxative",
            "Lethal", "Munchies", "Paranoia", "Refreshing", "Schizophrenic",
            "Sedating", "Seizure", "Shrinking", "Slippery", "Smelly",
            "Sneaky", "Spicy", "Thought Provoking", "Toxic", "Tropic Thunder",
            "Zombifying"
        };

        private GameEffect[] _cachedEffects = new GameEffect[0];
        private bool _cacheInitialized;
        private readonly List<PendingProductConsume> _pendingConsumes = new List<PendingProductConsume>();
        private readonly List<string> _activeLobbyEffects = new List<string>();
        private float _lethalKillTimer = -1f;

        private EffectsService() { }

        public string[] EffectIds => _effectIds;
        public string[] EffectLabels => _effectLabels;

        public void Update()
        {
            UpdateLethalKill();

            if (_pendingConsumes.Count == 0)
                return;

            for (int i = _pendingConsumes.Count - 1; i >= 0; i--)
            {
                PendingProductConsume pending = _pendingConsumes[i];
                if (pending == null)
                {
                    _pendingConsumes.RemoveAt(i);
                    continue;
                }

                pending.Elapsed += Time.deltaTime;
                if (pending.Elapsed < CustomProductConsumeDelay)
                    continue;

                ProductDefinition productDefinition = FindProductById(pending.ProductId);
                if (productDefinition != null)
                {
                    if (ConsumeProduct(productDefinition))
                    {
                        NotificationService.Instance.Status("Lobby FX stack: " + _activeLobbyEffects.Count);
                        Debug.Log("[Nugzz] Consumed synced custom lobby effect product: " + pending.ProductId);
                        _pendingConsumes.RemoveAt(i);
                    }

                    continue;
                }

                if (pending.Elapsed >= CustomProductTimeout)
                {
                    NotificationService.Instance.Error("Lobby FX sync timed out: " + GetLabel(pending.EffectName));
                    Debug.LogWarning("[Nugzz] Timed out waiting for custom product sync: " + pending.ProductId);
                    _pendingConsumes.RemoveAt(i);
                }
            }
        }

        public void ApplyEffect(string effectName, float duration = 30f)
        {
            try
            {
                var player = Player.Local;
                if (player == null)
                {
                    Debug.LogError("[Nugzz] No local player found");
                    return;
                }

                GameEffect effect = FindEffect(effectName);
                if (effect == null)
                {
                    NotificationService.Instance.Error("Effect not found: " + effectName);
                    Debug.LogError("[Nugzz] Effect not found: " + effectName);
                    return;
                }

                TrackLethal(effectName);

                if (TryApplyVanillaVisibleMultiplayerEffect(effectName, effect))
                {
                    NotificationService.Instance.Status("Lobby FX stack: " + _activeLobbyEffects.Count);
                    Debug.Log("[Nugzz] Applied vanilla-visible lobby effect: " + effectName);
                    return;
                }

                int applied = ApplyEffectToLoadedPlayers(effect);
                NotificationService.Instance.Status("Local FX: " + GetLabel(effectName));
                Debug.Log("[Nugzz] Applied local-visible effect: " + effectName + " to " + applied + " players");
            }
            catch (Exception ex)
            {
                NotificationService.Instance.Error("FX failed: " + effectName);
                Debug.LogError("[Nugzz] Failed to apply effect " + effectName + ": " + ex);
            }
        }

        public void ClearAllEffects()
        {
            try
            {
                var player = Player.Local;
                if (player == null)
                    return;

                _activeLobbyEffects.Clear();
                _pendingConsumes.Clear();
                _lethalKillTimer = -1f;

                int productClears = ClearProductsFromLoadedPlayers();
                TryConsumeClearProduct();

                EnsureEffectCache();
                int cleared = 0;
                for (int i = 0; i < _cachedEffects.Length; i++)
                {
                    try
                    {
                        cleared += ClearEffectFromLoadedPlayers(_cachedEffects[i]);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning("[Nugzz] Failed clearing effect " + SafeEffectName(_cachedEffects[i]) + ": " + ex.Message);
                    }
                }

                NotificationService.Instance.Status("Cleared visible FX");
                Debug.Log("[Nugzz] Cleared visible player effects: effects=" + cleared + " products=" + productClears);
            }
            catch (Exception ex)
            {
                Debug.LogError("[Nugzz] Failed to clear effects: " + ex);
            }
        }

        private int ClearProductsFromLoadedPlayers()
        {
            int cleared = 0;
            try
            {
                var players = Player.PlayerList;
                if (players == null || players.Count == 0)
                {
                    Player local = Player.Local;
                    if (local != null)
                        return ClearProductFromPlayer(local) ? 1 : 0;

                    return 0;
                }

                for (int i = 0; i < players.Count; i++)
                {
                    if (ClearProductFromPlayer(players[i]))
                        cleared++;
                }
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("Clear products from players failed: " + ex.Message);
            }

            return cleared;
        }

        private static bool ClearProductFromPlayer(Player player)
        {
            if (player == null)
                return false;

            try
            {
                ProductItemInstance consumed = player.ConsumedProduct;
                if (consumed != null)
                    consumed.ClearEffectsFromPlayer(player);
            }
            catch { }

            try
            {
                player.ClearProduct();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void TryConsumeClearProduct()
        {
            try
            {
                if (!LobbyService.Instance.IsInLobby())
                    return;

                ProductManager productManager = ManagerCacheService.Instance.ProductManager;
                if (productManager == null)
                    return;

                const string productId = CustomProductPrefix + "clear";
                ProductDefinition existing = FindProductById(productId);
                if (existing == null)
                {
                    var properties = new Il2CppSystem.Collections.Generic.List<string>();
                    var appearance = new MethAppearanceSettings(
                        new Color32(180, 180, 180, 255),
                        new Color32(255, 255, 255, 255));

                    productManager.CreateMeth_Server(
                        "Nugzz FX Clear",
                        productId,
                        EDrugType.Methamphetamine,
                        properties,
                        appearance);
                    TryHideCustomProduct(productManager, productId);
                    QueuePendingConsume("Clear", productId);
                    return;
                }

                ConsumeProduct(existing);
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("Clear FX product sync failed: " + ex.Message);
            }
        }

        private bool TryApplyVanillaVisibleMultiplayerEffect(string effectName, GameEffect effect)
        {
            if (!LobbyService.Instance.IsInLobby() || effect == null)
                return false;

            try
            {
                Player player = Player.Local;
                if (player == null)
                    return false;

                AddActiveLobbyEffect(effectName);
                return TryCreateAndQueueCustomEffectProduct(effectName, effect);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Nugzz] Vanilla-visible lobby effect failed: " + ex.Message);
                return false;
            }
        }

        private bool ConsumeProduct(ProductDefinition productDefinition)
        {
            Player player = Player.Local;
            if (player == null || productDefinition == null)
                return false;

            var product = new ProductItemInstance(
                productDefinition,
                1,
                EQuality.Standard,
                null);
            player.ConsumeProduct(product);
            return true;
        }

        private bool TryCreateAndQueueCustomEffectProduct(string effectName, GameEffect effect)
        {
            try
            {
                ProductManager productManager = ManagerCacheService.Instance.ProductManager;
                if (productManager == null)
                    return false;

                string productId = BuildActiveProductId();
                ProductDefinition existing = FindProductById(productId);
                if (existing != null)
                    return ConsumeProduct(existing);

                var properties = BuildActivePropertyList(effectName, effect);
                if (properties == null || properties.Count == 0)
                    return false;

                var appearance = new MethAppearanceSettings(
                    new Color32(80, 220, 255, 255),
                    new Color32(255, 255, 255, 255));

                productManager.CreateMeth_Server(
                    "Nugzz FX x" + properties.Count,
                    productId,
                    EDrugType.Methamphetamine,
                    properties,
                    appearance);

                TryHideCustomProduct(productManager, productId);
                QueuePendingConsume(effectName, productId);
                NotificationService.Instance.Notify("Syncing lobby FX stack: " + properties.Count);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Nugzz] Custom lobby effect product failed: " + ex.Message);
                return false;
            }
        }

        private void AddActiveLobbyEffect(string effectName)
        {
            string normalized = Normalize(effectName);
            for (int i = 0; i < _activeLobbyEffects.Count; i++)
            {
                if (Normalize(_activeLobbyEffects[i]) == normalized)
                    return;
            }

            _activeLobbyEffects.Add(effectName);
        }

        private Il2CppSystem.Collections.Generic.List<string> BuildActivePropertyList(string fallbackEffectName, GameEffect fallbackEffect)
        {
            var properties = new Il2CppSystem.Collections.Generic.List<string>();
            for (int i = 0; i < _activeLobbyEffects.Count; i++)
            {
                string activeEffectName = _activeLobbyEffects[i];
                GameEffect activeEffect = FindEffect(activeEffectName);
                string propertyId = GetEffectPropertyId(activeEffectName, activeEffect);
                if (!string.IsNullOrEmpty(propertyId))
                    properties.Add(propertyId);
            }

            if (properties.Count == 0)
                properties.Add(GetEffectPropertyId(fallbackEffectName, fallbackEffect));

            return properties;
        }

        private string BuildActiveProductId()
        {
            var normalized = new List<string>();
            for (int i = 0; i < _activeLobbyEffects.Count; i++)
                normalized.Add(Normalize(_activeLobbyEffects[i]));

            normalized.Sort(StringComparer.Ordinal);
            string signature = string.Join("_", normalized);
            if (signature.Length <= 50)
                return CustomProductPrefix + signature;

            return CustomProductPrefix + "stack_" + StableHash(signature).ToString("x8");
        }

        private ProductDefinition FindSingleEffectProductForEffect(string effectName, GameEffect effect)
        {
            try
            {
                Registry registry = Registry.Instance;
                var items = registry != null ? registry.GetAllItems() : null;
                if (items == null)
                    return null;

                string target = Normalize(effectName);
                for (int i = 0; i < items.Count; i++)
                {
                    ProductDefinition productDefinition = TryCastDefinition<ProductDefinition>(items[i]);
                    if (productDefinition?.Properties == null)
                        continue;

                    int propertyCount = productDefinition.Properties.Count;
                    if (propertyCount != 1)
                        continue;

                    for (int propertyIndex = 0; propertyIndex < propertyCount; propertyIndex++)
                    {
                        GameEffect property = productDefinition.Properties[propertyIndex];
                        if (property == null)
                            continue;

                        if (property == effect ||
                            Normalize(property.ID) == target ||
                            Normalize(property.Name) == target ||
                            Normalize(property.name) == target ||
                            Normalize(SafeEffectName(property)) == target)
                        {
                            return productDefinition;
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Nugzz] Product effect lookup failed: " + ex.Message);
                return null;
            }
        }

        private ProductDefinition FindProductById(string productId)
        {
            if (string.IsNullOrEmpty(productId))
                return null;

            ProductDefinition productDefinition = FindProductById(ManagerCacheService.Instance.ProductManager?.AllProducts, productId);
            if (productDefinition != null)
                return productDefinition;

            try
            {
                Registry registry = Registry.Instance;
                var items = registry != null ? registry.GetAllItems() : null;
                if (items == null)
                    return null;

                for (int i = 0; i < items.Count; i++)
                {
                    productDefinition = TryCastDefinition<ProductDefinition>(items[i]);
                    if (IsProductId(productDefinition, productId))
                        return productDefinition;
                }
            }
            catch { }

            return null;
        }

        private static ProductDefinition FindProductById(Il2CppSystem.Collections.Generic.List<ProductDefinition> products, string productId)
        {
            if (products == null)
                return null;

            for (int i = 0; i < products.Count; i++)
            {
                ProductDefinition product = products[i];
                if (IsProductId(product, productId))
                    return product;
            }

            return null;
        }

        private static bool IsProductId(ProductDefinition productDefinition, string productId)
        {
            if (productDefinition == null)
                return false;

            try
            {
                return string.Equals(productDefinition.ID, productId, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private void QueuePendingConsume(string effectName, string productId)
        {
            _pendingConsumes.Clear();
            _pendingConsumes.Add(new PendingProductConsume(effectName, productId));
        }

        private static void TryHideCustomProduct(ProductManager productManager, string productId)
        {
            try { productManager.SetProductListed(productId, false); } catch { }
            try { productManager.SetProductFavourited(productId, false); } catch { }
        }

        private static string GetEffectPropertyId(string effectName, GameEffect effect)
        {
            try
            {
                if (effect != null && !string.IsNullOrEmpty(effect.ID))
                    return effect.ID;
            }
            catch { }

            return effectName;
        }

        private void TrackLethal(string effectName)
        {
            if (Normalize(effectName) != "lethal")
                return;

            _lethalKillTimer = LethalKillDelay;
            NotificationService.Instance.Warning("Lethal will kill in " + LethalKillDelay.ToString("F1") + "s");
        }

        private void UpdateLethalKill()
        {
            if (_lethalKillTimer < 0f)
                return;

            _lethalKillTimer -= Time.deltaTime;
            if (_lethalKillTimer > 0f)
                return;

            _lethalKillTimer = -1f;
            PlayerCheatService.Instance.ForceKillLocalPlayer();
        }

        private int ApplyEffectToLoadedPlayers(GameEffect effect)
        {
            if (effect == null)
                return 0;

            int applied = 0;
            var players = Player.PlayerList;
            if (players == null || players.Count == 0)
            {
                Player local = Player.Local;
                if (local != null)
                {
                    effect.ApplyToPlayer(local);
                    return 1;
                }

                return 0;
            }

            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];
                if (player == null)
                    continue;

                try
                {
                    effect.ApplyToPlayer(player);
                    applied++;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[Nugzz] Failed applying effect to " + SafePlayerName(player) + ": " + ex.Message);
                }
            }

            return applied;
        }

        private int ClearEffectFromLoadedPlayers(GameEffect effect)
        {
            if (effect == null)
                return 0;

            int cleared = 0;
            var players = Player.PlayerList;
            if (players == null || players.Count == 0)
            {
                Player local = Player.Local;
                if (local != null)
                {
                    effect.ClearFromPlayer(local);
                    return 1;
                }

                return 0;
            }

            for (int i = 0; i < players.Count; i++)
            {
                Player player = players[i];
                if (player == null)
                    continue;

                try
                {
                    effect.ClearFromPlayer(player);
                    cleared++;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("[Nugzz] Failed clearing effect from " + SafePlayerName(player) + ": " + ex.Message);
                }
            }

            return cleared;
        }

        private GameEffect FindEffect(string effectName)
        {
            EnsureEffectCache();
            string target = Normalize(effectName);

            for (int i = 0; i < _cachedEffects.Length; i++)
            {
                GameEffect effect = _cachedEffects[i];
                if (effect == null)
                    continue;

                if (Normalize(effect.ID) == target ||
                    Normalize(effect.Name) == target ||
                    Normalize(effect.name) == target ||
                    Normalize(SafeEffectName(effect)) == target)
                {
                    return effect;
                }
            }

            return null;
        }

        private void EnsureEffectCache()
        {
            if (_cacheInitialized)
                return;

            try
            {
                var found = Resources.FindObjectsOfTypeAll<GameEffect>();
                if (found == null)
                {
                    _cachedEffects = new GameEffect[0];
                    _cacheInitialized = true;
                    return;
                }

                _cachedEffects = new GameEffect[found.Length];
                for (int i = 0; i < found.Length; i++)
                    _cachedEffects[i] = found[i];

                Debug.Log("[Nugzz] Cached " + _cachedEffects.Length + " player effects");
            }
            catch (Exception ex)
            {
                _cachedEffects = new GameEffect[0];
                Debug.LogError("[Nugzz] Failed to cache player effects: " + ex);
            }

            _cacheInitialized = true;
        }

        private string GetLabel(string effectName)
        {
            string target = Normalize(effectName);
            for (int i = 0; i < _effectIds.Length; i++)
            {
                if (Normalize(_effectIds[i]) == target)
                    return _effectLabels[i];
            }

            return effectName;
        }

        private static string SafeEffectName(GameEffect effect)
        {
            if (effect == null)
                return "";

            try
            {
                return effect.GetIl2CppType()?.Name ?? "";
            }
            catch
            {
                return effect.name ?? "";
            }
        }

        private static string SafePlayerName(Player player)
        {
            if (player == null)
                return "<null>";

            try
            {
                return !string.IsNullOrEmpty(player.PlayerName)
                    ? player.PlayerName
                    : player.name;
            }
            catch
            {
                return "<player>";
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

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            return value.Replace(" ", "")
                .Replace("-", "")
                .Replace("_", "")
                .ToLowerInvariant();
        }

        private static uint StableHash(string value)
        {
            unchecked
            {
                uint hash = 2166136261u;
                for (int i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= 16777619u;
                }

                return hash;
            }
        }

        private sealed class PendingProductConsume
        {
            public PendingProductConsume(string effectName, string productId)
            {
                EffectName = effectName;
                ProductId = productId;
            }

            public string EffectName { get; }
            public string ProductId { get; }
            public float Elapsed { get; set; }
        }
    }
}
