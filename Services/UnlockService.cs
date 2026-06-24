using System;
using Il2CppScheduleOne;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Property;

namespace NugzzMenu.Services
{
    public sealed class UnlockService
    {
        private static readonly UnlockService _instance = new UnlockService();
        public static UnlockService Instance => _instance;

        private UnlockService() { }

        public int UnlockAllProperties()
        {
            if (!CanChangeWorldOwnership("Property unlocks"))
                return 0;

            int changed = 0;
            try
            {
                var properties = Property.Properties;
                if (properties == null || properties.Count == 0)
                {
                    NotificationService.Instance.Warning("No properties found in the current scene");
                    return 0;
                }

                for (int i = 0; i < properties.Count; i++)
                {
                    Property property = null;
                    try { property = properties[i]; } catch { }
                    if (property == null)
                        continue;

                    if (OwnProperty(property))
                        changed++;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[Nugzz] Unlock all properties failed: " + ex.Message);
            }

            NotificationService.Instance.Status("Unlocked properties: " + changed);
            return changed;
        }

        public int OwnAllBusinesses()
        {
            if (!CanChangeWorldOwnership("Business ownership"))
                return 0;

            int changed = 0;
            try
            {
                var businesses = Business.Businesses;
                if (businesses == null || businesses.Count == 0)
                {
                    NotificationService.Instance.Warning("No businesses found in the current scene");
                    return 0;
                }

                for (int i = 0; i < businesses.Count; i++)
                {
                    Business business = null;
                    try { business = businesses[i]; } catch { }
                    if (business == null)
                        continue;

                    if (OwnBusiness(business))
                        changed++;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[Nugzz] Own all businesses failed: " + ex.Message);
            }

            NotificationService.Instance.Status("Owned businesses: " + changed);
            return changed;
        }

        public int UnlockAllAchievements()
        {
            int changed = 0;
            try
            {
                Array achievements = Enum.GetValues(typeof(AchievementManager.EAchievement));
                for (int i = 0; i < achievements.Length; i++)
                {
                    try
                    {
                        var achievement = (AchievementManager.EAchievement)achievements.GetValue(i);
                        AchievementManager.UnlockAchievement(achievement);
                        changed++;
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[Nugzz] Unlock all achievements failed: " + ex.Message);
            }

            NotificationService.Instance.Status("Unlocked achievements: " + changed);
            return changed;
        }

        public int UnlockAllProductsAndItems()
        {
            int productCount = UnlockAllProducts();
            int itemCount = UnlockAllShopItems();
            NotificationService.Instance.Status("Unlocked products/items: " + productCount + " products, " + itemCount + " items");
            return productCount + itemCount;
        }

        private static bool CanChangeWorldOwnership(string actionName)
        {
            try
            {
                LobbyService lobby = LobbyService.Instance;
                if (lobby != null && lobby.IsInLobby() && !lobby.IsHost())
                {
                    NotificationService.Instance.Warning(actionName + " are host-only in multiplayer");
                    return false;
                }
            }
            catch { }

            return true;
        }

        private static bool OwnProperty(Property property)
        {
            if (property == null)
                return false;

            bool wasOwned = SafeIsOwned(property);
            if (!wasOwned)
            {
                try { property.SetOwned(); } catch (Exception ex) { UnityEngine.Debug.LogWarning("[Nugzz] SetOwned failed: " + ex.Message); }
            }

            try
            {
                if (Property.OwnedProperties != null && !Property.OwnedProperties.Contains(property))
                    Property.OwnedProperties.Add(property);
            }
            catch { }

            bool isOwned = SafeIsOwned(property) ||
                (Property.OwnedProperties != null && Property.OwnedProperties.Contains(property));
            return !wasOwned && isOwned;
        }

        private static bool OwnBusiness(Business business)
        {
            if (business == null)
                return false;

            bool wasOwned = SafeIsOwned(business);
            if (!wasOwned)
            {
                try { business.SetOwned(); } catch (Exception ex) { UnityEngine.Debug.LogWarning("[Nugzz] Business SetOwned failed: " + ex.Message); }
            }

            try
            {
                if (Business.OwnedBusinesses != null && !Business.OwnedBusinesses.Contains(business))
                    Business.OwnedBusinesses.Add(business);
            }
            catch { }

            try
            {
                if (Business.UnownedBusinesses != null && Business.UnownedBusinesses.Contains(business))
                    Business.UnownedBusinesses.Remove(business);
            }
            catch { }

            bool isOwned = SafeIsOwned(business) ||
                (Business.OwnedBusinesses != null && Business.OwnedBusinesses.Contains(business));
            return !wasOwned && isOwned;
        }

        private static bool SafeIsOwned(Property property)
        {
            try { return property != null && property.IsOwned; }
            catch { return false; }
        }

        private static int UnlockAllProducts()
        {
            int changed = 0;
            try
            {
                ProductManager productManager = ManagerCacheService.Instance.ProductManager;
                var products = productManager?.AllProducts;
                if (productManager == null || products == null || products.Count == 0)
                    return 0;

                for (int i = 0; i < products.Count; i++)
                {
                    ProductDefinition product = null;
                    try { product = products[i]; } catch { }
                    if (product == null)
                        continue;

                    bool wasUnlocked = IsProductUnlocked(product);
                    string id = GetProductId(product);
                    if (string.IsNullOrEmpty(id))
                        continue;

                    try { productManager.DiscoverProduct(id); } catch { }
                    try { productManager.SetProductDiscovered(null, id, true); } catch { }
                    try { productManager.SetProductListed(id, true); } catch { }
                    try { productManager.SetProductListed(null, id, true); } catch { }

                    AddProductToList(ProductManager.DiscoveredProducts, product);
                    AddProductToList(ProductManager.ListedProducts, product);

                    if (!wasUnlocked && IsProductUnlocked(product))
                        changed++;
                }

                try { productManager.HasChanged = true; } catch { }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[Nugzz] Unlock all products failed: " + ex.Message);
            }

            return changed;
        }

        private static int UnlockAllShopItems()
        {
            int changed = 0;
            try
            {
                Registry registry = ManagerCacheService.Instance.Registry ?? UnityEngine.Object.FindObjectOfType<Registry>();
                var items = registry?.GetAllItems();
                if (items == null || items.Count == 0)
                    return 0;

                for (int i = 0; i < items.Count; i++)
                {
                    ItemDefinition definition = null;
                    try { definition = items[i]; } catch { }
                    if (definition == null)
                        continue;

                    StorableItemDefinition storable = TryCast<StorableItemDefinition>(definition);
                    if (storable == null)
                        continue;

                    bool wasUnlocked = SafeIsUnlocked(storable);
                    try { storable.RequiresLevelToPurchase = false; } catch { }

                    if (!wasUnlocked && SafeIsUnlocked(storable))
                        changed++;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[Nugzz] Unlock all shop items failed: " + ex.Message);
            }

            return changed;
        }

        private static bool IsProductUnlocked(ProductDefinition product)
        {
            try
            {
                return product != null &&
                    ProductManager.DiscoveredProducts != null &&
                    ProductManager.DiscoveredProducts.Contains(product);
            }
            catch
            {
                return false;
            }
        }

        private static void AddProductToList(Il2CppSystem.Collections.Generic.List<ProductDefinition> list, ProductDefinition product)
        {
            try
            {
                if (list != null && product != null && !list.Contains(product))
                    list.Add(product);
            }
            catch { }
        }

        private static string GetProductId(ProductDefinition product)
        {
            if (product == null)
                return string.Empty;

            try
            {
                if (!string.IsNullOrEmpty(product.ID))
                    return product.ID;
            }
            catch { }

            try { return product.name ?? string.Empty; }
            catch { return string.Empty; }
        }

        private static bool SafeIsUnlocked(StorableItemDefinition definition)
        {
            try { return definition != null && definition.IsUnlocked; }
            catch { return false; }
        }

        private static T TryCast<T>(ItemDefinition definition) where T : ItemDefinition
        {
            if (definition == null)
                return null;

            try { return definition.TryCast<T>(); }
            catch { return definition as T; }
        }
    }
}
