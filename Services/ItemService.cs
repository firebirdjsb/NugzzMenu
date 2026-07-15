using System;
using System.Collections.Generic;
using Il2CppScheduleOne;
using Il2CppScheduleOne.Clothing;
using Il2CppScheduleOne.Equipping.Framework;
using Il2CppScheduleOne.Growing;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Product;
using Il2CppScheduleOne.Product.Packaging;
using Il2CppScheduleOne.Storage;
using UnityEngine;

namespace NugzzMenu.Services
{
    public sealed class ItemService
    {
        private static readonly ItemService _instance = new ItemService();
        public static ItemService Instance => _instance;
        private string[] _itemIds = new string[0];
        private string[] _itemNames = new string[0];
        private string[] _itemCategories = new string[0];
        private ItemDefinition[] _itemDefinitions = new ItemDefinition[0];
        private int _itemCount = 0;
        private bool _isCached = false;
        private int[] _filteredIndices = new int[0];
        private int _filteredCount = 0;
        private int _pageIndex = 0;
        private int _currentFilter = 0;
        private int _itemsPerPage = 15;
        private string _searchText = "";
        private int _qualityIndex = 2;
        private readonly Queue<SpawnRequest> _pendingSpawns = new Queue<SpawnRequest>();
        public bool UseGameStackLogic { get; set; } = true;

        private struct SpawnRequest
        {
            public string ItemId;
            public int Quantity;
            public int QualityIndex;
        }

        private struct SlotSnapshot
        {
            public bool HasItem;
            public string ItemKey;
            public int Quantity;
            public bool HasQuality;
            public EQuality Quality;
        }

        private struct KnownCatalogItem
        {
            public string Id;
            public string Name;
            public string Category;

            public KnownCatalogItem(string id, string name, string category)
            {
                Id = id;
                Name = name;
                Category = category;
            }
        }

        private static readonly KnownCatalogItem[] KnownCatalogItems =
        {
            new KnownCatalogItem("suspensionrack", "Suspension Rack", "Equipment"),
            new KnownCatalogItem("mushroombed", "Mushroom Bed", "Grow"),
            new KnownCatalogItem("mushroomspawnstation", "Mushroom Spawn Station", "Grow"),
            new KnownCatalogItem("mushroomsubstrate", "Mushroom Substrate", "Grow"),
        };

        private static readonly string[] Categories =
        {
            "All",
            "Drugs",
            "Seeds",
            "Mixers",
            "Grow",
            "Tools",
            "Packaging",
            "Equipment",
            "Furniture",
            "Storage",
            "Lights",
            "Weapons",
            "Skateboards",
            "Clothes",
            "Decor",
            "Misc",
        };

        private static readonly string[] WeaponKeys =
        {
            "baseballbat", "fryingpan", "machete", "revolver", "m1911",
            "pump shotgun", "pumpshotgun", "shotgunshell", "cylinder", "magazine",
            "goldenm1911"
        };

        private static readonly string[] DrugKeys =
        {
            "ogkush", "sourdiesel", "greencrack", "granddaddypurple",
            "weed", "meth", "liquidmeth", "cocaine", "cocainebase",
            "shroom", "pseudo", "liquidbabyblue", "liquidbikercrank",
            "liquidglass", "cocaleaf"
        };

        private static readonly string[] IngredientKeys =
        {
            "acid", "addy", "banana", "battery", "chili", "cuke", "donut",
            "energydrink", "flumedicine", "gasoline", "horsesemen", "iodine",
            "megabean", "motoroil", "mouthwash", "paracetamol", "phosphorus",
            "viagor", "viagra"
        };

        private static readonly string[] ToolKeys =
        {
            "wateringcan", "spraybottle", "trimmer", "spraypaint",
            "graffiticleaner", "grafitticleaner", "trashbag", "trashgrabber",
            "flashlight", "managementclipboard"
        };

        private static readonly string[] GrowKeys =
        {
            "soil", "fertilizer", "pgr", "speedgrow", "growtent", "pot",
            "mushroombed", "mushroomspawnstation", "grainbag", "sporesyringe",
            "substrate", "mushroomsubstrate", "shroomspawn", "acunit",
            "soilpourer", "potsprinkler", "sprinkler", "bigsprinkler"
        };

        private static readonly string[] EquipmentKeys =
        {
            "chemistrystation", "laboven", "cauldron", "mixingstation",
            "brickpress", "dryingrack", "packagingstation", "suspensionrack",
            "launderingstation"
        };

        private static readonly string[] StorageKeys =
        {
            "storagerack", "storagecloset", "displaycabinet", "filingcabinet",
            "locker", "safe"
        };

        private static readonly string[] LightKeys =
        {
            "lamp", "light", "halogen", "led", "fullspectrum"
        };

        private static readonly string[] SkateboardKeys =
        {
            "skateboard", "cruiser"
        };

        private static readonly string[] DecorKeys =
        {
            "artwork", "clock", "sign", "goldbar", "goldchain", "silverchain",
            "goldwatch", "silverwatch", "chateaulapeepee", "brutdugloop",
            "oldmanjimmyswhiskey", "garbagethrone", "trashcrown", "wallshelf",
            "wallmountedshelf", "metalsign", "woodensign", "woodsign"
        };

        private ItemService() { }

        public static int CategoryCount => Categories?.Length ?? 1;
        public static string GetCategoryLabel(int idx)
        {
            if (Categories == null) return "All";
            if (idx < 0 || idx >= Categories.Length) return "Other";
            return Categories[idx];
        }

        private static bool IsBlockedCatalogItem(string itemId)
        {
            string key = NormalizeItemKey(itemId);
            return key == "cash" ||
                key == "defaultweed" ||
                key == "offer" ||
                key == "stolendeaddrop" ||
                key == "lowqualitypseudo" ||
                key == "lowqualitypseudoproduct" ||
                key == "pseudolowquality" ||
                key == "pseudolowqualityproduct" ||
                key == "highqualitypseudo" ||
                key == "highqualitypseudoproduct" ||
                key == "pseudohighquality" ||
                key == "pseudohighqualityproduct" ||
                key == "poorqualitypseudo" ||
                key == "poorqualitypseudoproduct" ||
                key == "pseudopoorquality" ||
                key == "pseudopoorqualityproduct" ||
                key == "premiumqualitypseudo" ||
                key == "premiumqualitypseudoproduct" ||
                key == "pseudopremiumquality" ||
                key == "pseudopremiumqualityproduct" ||
                key == "cukepseudo" ||
                key == "cukepseudoproduct" ||
                key == "energydrinkpseudo" ||
                key == "energydrinkpseudoproduct" ||
                key == "brick";
        }

        private static string NormalizeItemKey(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            var chars = new char[value.Length];
            int count = 0;
            for (int i = 0; i < value.Length; i++)
            {
                char c = char.ToLowerInvariant(value[i]);
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                    chars[count++] = c;
            }
            return new string(chars, 0, count);
        }

        public void InitializeCache()
        {
            if (_isCached)
            {
                UnityEngine.Debug.Log("[Nugzz] Cache already initialized, skipping");
                return;
            }

            try
            {
                var registry = ManagerCacheService.Instance.Registry;
                if (registry == null)
                {
                    registry = UnityEngine.Object.FindObjectOfType<Registry>();
                }
                if (registry == null)
                {
                    UnityEngine.Debug.LogError("[Nugzz] Registry not found - item spawner will not work");
                    return;
                }

                var allItems = registry.GetAllItems();
                UnityEngine.Debug.Log($"[Nugzz] GetAllItems returned: {(allItems == null ? "null" : allItems.Count + " items")}");
                if (allItems == null || allItems.Count == 0)
                {
                    UnityEngine.Debug.LogWarning("[Nugzz] Item registry empty");
                    return;
                }

                var products = ManagerCacheService.Instance.ProductManager?.AllProducts;
                int productCount = products != null ? products.Count : 0;
                int knownCount = KnownCatalogItems != null ? KnownCatalogItems.Length : 0;
                int count = allItems.Count + productCount + knownCount;
                _itemIds = new string[count];
                _itemNames = new string[count];
                _itemCategories = new string[count];
                _itemDefinitions = new ItemDefinition[count];
                _itemCount = 0;
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < count; i++)
                {
                    if (i >= allItems.Count)
                        break;

                    try
                    {
                        AddCatalogItem(allItems[i], seen);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError("[Nugzz] Cache error " + i + ": " + ex.Message);
                    }
                }

                if (products != null)
                {
                    for (int i = 0; i < products.Count; i++)
                    {
                        try
                        {
                            AddCatalogItem(products[i], seen);
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogError("[Nugzz] Product cache error " + i + ": " + ex.Message);
                        }
                    }
                }

                AddKnownCatalogItems(seen);

                SortItemCache();
                _isCached = true;
                ApplyFilter();
                UnityEngine.Debug.Log("[Nugzz] Loaded " + _itemCount + " items (" + allItems.Count + " registry, " + productCount + " products, " + knownCount + " known)");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[Nugzz] InitCache fail: " + ex.Message);
            }
        }

        private void AddKnownCatalogItems(HashSet<string> seen)
        {
            if (KnownCatalogItems == null || seen == null)
                return;

            for (int i = 0; i < KnownCatalogItems.Length; i++)
            {
                KnownCatalogItem item = KnownCatalogItems[i];
                if (string.IsNullOrEmpty(item.Id) || IsBlockedCatalogItem(item.Id))
                    continue;

                string key = NormalizeAliasKey(item.Id);
                string alias = GetKnownAlias(key);
                if (!string.IsNullOrEmpty(alias))
                    key = alias;

                if (!seen.Add(key))
                    continue;
                if (_itemCount >= _itemIds.Length)
                    return;

                _itemIds[_itemCount] = item.Id;
                _itemNames[_itemCount] = string.IsNullOrEmpty(item.Name)
                    ? GetDisplayName(item.Id, null)
                    : item.Name;
                _itemCategories[_itemCount] = string.IsNullOrEmpty(item.Category)
                    ? GetCatalogCategory(item.Id, null)
                    : item.Category;
                _itemDefinitions[_itemCount] = null;
                _itemCount++;
            }
        }

        private void AddCatalogItem(ItemDefinition definition, HashSet<string> seen)
        {
            string id = definition?.name;
            if (string.IsNullOrEmpty(id) || IsBlockedCatalogItem(id) ||
                !CanCatalogDefinitionSpawn(definition))
            {
                return;
            }

            string key = NormalizeAliasKey(id);
            string alias = GetKnownAlias(key);
            if (!string.IsNullOrEmpty(alias))
                key = alias;

            if (!seen.Add(key))
                return;

            if (_itemCount >= _itemIds.Length)
                return;

            _itemIds[_itemCount] = id;
            _itemNames[_itemCount] = GetDisplayName(id, definition);
            _itemCategories[_itemCount] = GetCatalogCategory(id, definition);
            _itemDefinitions[_itemCount] = definition;
            _itemCount++;
        }

        private static bool CanCatalogDefinitionSpawn(ItemDefinition definition)
        {
            if (definition == null)
                return false;

            string key = NormalizeAliasKey(definition.name);
            string alias = GetKnownAlias(key);
            if (!string.IsNullOrEmpty(alias))
                key = alias;
            if (key == "suspensionrack")
                return true;

            if (TryCastDefinition<ProductDefinition>(definition) != null)
                return true;
            if (TryCastDefinition<StorableItemDefinition>(definition) != null)
                return true;
            if (TryCastDefinition<BuildableItemDefinition>(definition) != null)
                return true;

            return false;
        }

        private void SortItemCache()
        {
            for (int i = 1; i < _itemCount; i++)
            {
                string name = _itemNames[i];
                string id = _itemIds[i];
                string category = _itemCategories[i];
                ItemDefinition def = _itemDefinitions[i];
                int j = i - 1;

                while (j >= 0 && string.Compare(_itemNames[j], name, StringComparison.OrdinalIgnoreCase) > 0)
                {
                    _itemNames[j + 1] = _itemNames[j];
                    _itemIds[j + 1] = _itemIds[j];
                    _itemCategories[j + 1] = _itemCategories[j];
                    _itemDefinitions[j + 1] = _itemDefinitions[j];
                    j--;
                }
                _itemNames[j + 1] = name;
                _itemIds[j + 1] = id;
                _itemCategories[j + 1] = category;
                _itemDefinitions[j + 1] = def;
            }
        }

        public void SetFilter(int filterIndex)
        {
            if (filterIndex < 0 || filterIndex >= Categories.Length) filterIndex = 0;
            if (_currentFilter != filterIndex)
            {
                _currentFilter = filterIndex;
                ApplyFilter();
            }
        }

        public void SetSearchText(string text)
        {
            _searchText = text ?? "";
            ApplyFilter();
        }

        public string GetSearchText() => _searchText;

        public void SetQualityIndex(int index)
        {
            if (index < 0 || index > 4) index = 2;
            _qualityIndex = index;
            DebugLogService.Instance.Verbose("Selected item quality: " + GetQuality(index));
        }

        public int GetQualityIndex() => _qualityIndex;

        public void ApplyFilter()
        {
            if (!_isCached) return;

            _filteredIndices = new int[_itemCount];
            _filteredCount = 0;
            _pageIndex = 0;

            string searchLower = _searchText?.ToLowerInvariant() ?? "";
            bool hasSearch = !string.IsNullOrEmpty(_searchText);

            if (_currentFilter == 0)
            {
                for (int i = 0; i < _itemCount; i++)
                {
                    if (IsBlockedCatalogItem(_itemIds[i]))
                        continue;

                    if (hasSearch)
                    {
                        string idLower = _itemIds[i].ToLowerInvariant();
                        string nameLower = _itemNames[i].ToLowerInvariant();
                        if (!DoesSearchMatch(idLower, nameLower, searchLower))
                            continue;
                    }
                    _filteredIndices[_filteredCount++] = i;
                }
                return;
            }

            string selectedCategory = GetCategoryLabel(_currentFilter);

            for (int i = 0; i < _itemCount; i++)
            {
                if (IsBlockedCatalogItem(_itemIds[i]))
                    continue;

                string idLower = _itemIds[i].ToLowerInvariant();
                string nameLower = _itemNames[i].ToLowerInvariant();

                if (!string.Equals(_itemCategories[i], selectedCategory, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (hasSearch)
                {
                    if (!DoesSearchMatch(idLower, nameLower, searchLower))
                        continue;
                }

                _filteredIndices[_filteredCount++] = i;
            }
        }

        private static bool DoesSearchMatch(string idLower, string nameLower, string searchLower)
        {
            if (string.IsNullOrEmpty(searchLower))
                return true;

            if ((!string.IsNullOrEmpty(idLower) && idLower.Contains(searchLower)) ||
                (!string.IsNullOrEmpty(nameLower) && nameLower.Contains(searchLower)))
                return true;

            string normalizedSearch = NormalizeItemKey(searchLower);
            if (string.IsNullOrEmpty(normalizedSearch))
                return true;

            return NormalizeItemKey(idLower).Contains(normalizedSearch) ||
                NormalizeItemKey(nameLower).Contains(normalizedSearch);
        }

        public void SpawnItem(string itemId, int quantity, int qualityIndex = -1)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0)
                return;

            if (IsBlockedCatalogItem(itemId))
            {
                NotificationService.Instance.Status("Item blocked: " + HumanizeItemId(itemId));
                return;
            }

            if (qualityIndex < 0 || qualityIndex > 4)
                qualityIndex = _qualityIndex;

            if (UseGameStackLogic && quantity > 1)
            {
                for (int i = 0; i < quantity; i++)
                {
                    _pendingSpawns.Enqueue(new SpawnRequest
                    {
                        ItemId = itemId,
                        Quantity = 1,
                        QualityIndex = qualityIndex
                    });
                }

                DebugLogService.Instance.Verbose("Queued item spawn " + quantity + "x " + itemId + " as game-stack inserts quality=" + GetQuality(qualityIndex));
                return;
            }

            _pendingSpawns.Enqueue(new SpawnRequest
            {
                ItemId = itemId,
                Quantity = quantity,
                QualityIndex = qualityIndex
            });
            DebugLogService.Instance.Verbose("Queued item spawn " + quantity + "x " + itemId + " quality=" + GetQuality(qualityIndex) + " mode=" + (UseGameStackLogic ? "game" : "stackmod"));
        }

        public void ProcessPendingSpawns()
        {
            const int maxPerFrame = 4;
            int processed = 0;

            while (_pendingSpawns.Count > 0 && processed < maxPerFrame)
            {
                SpawnRequest request = _pendingSpawns.Dequeue();
                SpawnItemImmediate(request.ItemId, request.Quantity, request.QualityIndex);
                processed++;
            }
        }

        private void SpawnItemImmediate(string itemId, int quantity, int qualityIndex = 2)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0)
                return;

            try
            {
                var registry = ManagerCacheService.Instance.Registry;
                if (registry == null)
                {
                    registry = UnityEngine.Object.FindObjectOfType<Registry>();
                }
                if (registry == null)
                {
                    UnityEngine.Debug.LogError("[Nugzz] Registry not found for item spawn");
                    return;
                }

                var itemDefinition = ResolveItemDefinition(registry, itemId, out string resolvedItemId);
                if (itemDefinition == null)
                {
                    UnityEngine.Debug.LogError("[Nugzz] Item definition not found for '" + itemId + "'");
                    return;
                }
                itemId = resolvedItemId;

                var playerInventory = ManagerCacheService.Instance.PlayerInventory;
                if (playerInventory == null)
                {
                    playerInventory = UnityEngine.Object.FindObjectOfType<PlayerInventory>();
                }
                if (playerInventory == null)
                {
                    UnityEngine.Debug.LogError("[Nugzz] Player inventory not found for item spawn");
                    return;
                }

                EQuality requestedQuality = GetQuality(qualityIndex);
                DebugLogService.Instance.Verbose("Spawn diagnose: requested=" + itemId + " resolved=" + resolvedItemId + " quantity=" + quantity + " quality=" + requestedQuality + " defType=" + itemDefinition.GetType().Name);

                SlotSnapshot[] beforeSlots = CaptureSlotSnapshot(playerInventory);

                ItemInstance instance = CreateItemInstance(itemDefinition, itemId, quantity, qualityIndex);
                if (instance == null)
                {
                    UnityEngine.Debug.LogError("[Nugzz] Failed to create item instance for '" + itemId + "'");
                    NotificationService.Instance.Status("Item create failed: " + itemId);
                    return;
                }

                LogInstanceQuality("pre-insert", instance);

                if (!TryAddItemToInventory(playerInventory, instance, requestedQuality))
                {
                    UnityEngine.Debug.LogError("[Nugzz] Failed to insert item into inventory for '" + itemId + "'");
                    NotificationService.Instance.Status("Item insert failed: " + itemId);
                    return;
                }

                ApplyQualityToChangedInventorySlots(playerInventory, itemId, beforeSlots, requestedQuality);
                LogMatchingInventoryQuality(playerInventory, itemId, "post-insert");

                DebugLogService.Instance.Verbose("Spawned " + quantity + "x " + itemId + " via direct item instance");
                NotificationService.Instance.Status("Spawned " + quantity + "x " + itemId);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[Nugzz] Failed to spawn item '" + itemId + "': " + ex);
                NotificationService.Instance.Status("Item spawn failed: " + itemId);
            }
        }

        private bool TryAddItemToInventory(PlayerInventory playerInventory, ItemInstance instance, EQuality requestedQuality)
        {
            if (playerInventory == null || instance == null)
                return false;

            if (TryGetQualityInstance(instance) != null)
            {
                ForceInstanceQuality(instance, requestedQuality, "pre-insert");
                if (!UseGameStackLogic)
                    return TryInsertQualityItemToInventory(playerInventory, instance, requestedQuality);
            }

            try
            {
                playerInventory.AddItemToInventory(instance);
                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[Nugzz] AddItemToInventory failed, trying direct slot insert: " + ex.Message);
            }

            try
            {
                var slots = playerInventory.GetAllInventorySlots();
                if (slots != null && ItemSlot.TryInsertItemIntoSet(slots, instance))
                    return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[Nugzz] Direct slot insert failed: " + ex.Message);
            }

            return false;
        }

        private bool TryInsertQualityItemToInventory(PlayerInventory playerInventory, ItemInstance instance, EQuality requestedQuality)
        {
            try
            {
                if (!ForceInstanceQuality(instance, requestedQuality, "quality-insert-start"))
                    return false;

                var slots = playerInventory.GetAllInventorySlots();
                if (slots == null)
                    return false;

                string wanted = NormalizeItemKey(instance.Definition?.name);

                for (int i = 0; i < slots.Count; i++)
                {
                    var slot = slots[i];
                    var existing = slot?.ItemInstance;
                    if (slot == null || existing == null)
                        continue;

                    string existingId = null;
                    try { existingId = existing.Definition?.name; } catch { }
                    if (NormalizeItemKey(existingId) != wanted)
                        continue;

                    QualityItemInstance existingQuality = TryGetQualityInstance(existing);
                    if (existingQuality == null || existingQuality.Quality != requestedQuality)
                        continue;

                    try
                    {
                        if (slot.IsAtCapacity || slot.IsAddLocked)
                            continue;
                    }
                    catch { }

                    try
                    {
                        if (!existing.CanStackWith(instance, true))
                            continue;
                    }
                    catch { }

                    ForceInstanceQuality(instance, requestedQuality, "before-stack-add");
                    slot.AddItem(instance, false);
                    ForceInstanceQuality(slot.ItemInstance, requestedQuality, "after-stack-add");
                    try { slot.ReplicateStoredInstance(); } catch { }
                    DebugLogService.Instance.Verbose("Quality insert merged into matching " + requestedQuality + " stack for " + wanted + " slot=" + i);
                    return true;
                }

                for (int i = 0; i < slots.Count; i++)
                {
                    var slot = slots[i];
                    if (slot == null || slot.ItemInstance != null)
                        continue;

                    try
                    {
                        if (slot.IsAddLocked)
                            continue;
                    }
                    catch { }

                    try
                    {
                        int capacity = slot.GetCapacityForItem(instance, true);
                        if (capacity <= 0)
                            continue;
                    }
                    catch { }

                    ForceInstanceQuality(instance, requestedQuality, "before-empty-slot-set");
                    slot.SetStoredItem(instance, false);
                    ForceInstanceQuality(slot.ItemInstance, requestedQuality, "after-empty-slot-set");
                    try { slot.ReplicateStoredInstance(); } catch { }
                    DebugLogService.Instance.Verbose("Quality insert placed separate " + requestedQuality + " stack for " + wanted + " slot=" + i);
                    return true;
                }

                DebugLogService.Instance.VerboseWarning("No direct quality slot available for " + wanted + " quality=" + requestedQuality);
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("Direct quality insert failed: " + ex.Message);
            }

            return false;
        }

        private bool ForceInstanceQuality(ItemInstance instance, EQuality quality, string context)
        {
            try
            {
                QualityItemInstance qi = TryGetQualityInstance(instance);
                if (qi == null)
                    return false;

                try { qi.SetQuality(quality); } catch { }
                try { qi.Quality = quality; } catch { }

                bool ok = qi.Quality == quality;
                DebugLogService.Instance.Verbose("Quality force " + context + " type=" + instance.GetType().Name + " def=" + (instance.Definition?.name ?? "null") + " requested=" + quality + " actual=" + qi.Quality + " ok=" + ok);
                return ok;
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("Quality force failed " + context + ": " + ex.Message);
                return false;
            }
        }

        private ItemDefinition ResolveItemDefinition(Registry registry, string requestedId, out string resolvedId)
        {
            resolvedId = requestedId;
            if (registry == null || string.IsNullOrEmpty(requestedId))
                return null;

            string wanted = NormalizeAliasKey(requestedId);
            string alias = GetKnownAlias(wanted);
            if (!string.IsNullOrEmpty(alias))
                wanted = alias;

            ProductDefinition product = ResolveProductDefinition(wanted, out string productId);
            if (product != null)
            {
                resolvedId = productId;
                return product;
            }

            try
            {
                var direct = registry._GetItem(requestedId, false);
                if (direct != null)
                    return direct;
            }
            catch { }

            for (int i = 0; i < _itemCount; i++)
            {
                string id = _itemIds[i];
                if (string.IsNullOrEmpty(id))
                    continue;

                string key = NormalizeAliasKey(id);
                string candidateAlias = GetKnownAlias(key);
                if (!string.IsNullOrEmpty(candidateAlias))
                    key = candidateAlias;
                if (key != wanted)
                    continue;

                if (i >= 0 && i < _itemDefinitions.Length && _itemDefinitions[i] != null)
                {
                    resolvedId = id;
                    return _itemDefinitions[i];
                }

                try
                {
                    var def = registry._GetItem(id, false);
                    if (def != null)
                    {
                        resolvedId = id;
                        return def;
                    }
                }
                catch { }
            }

            return null;
        }

        private ProductDefinition ResolveProductDefinition(string wanted, out string resolvedId)
        {
            resolvedId = null;

            try
            {
                var products = ManagerCacheService.Instance.ProductManager?.AllProducts;
                if (products == null)
                    return null;

                for (int i = 0; i < products.Count; i++)
                {
                    ProductDefinition product = products[i];
                    if (product == null || string.IsNullOrEmpty(product.name))
                        continue;

                    string key = NormalizeAliasKey(product.name);
                    string alias = GetKnownAlias(key);
                    if (!string.IsNullOrEmpty(alias))
                        key = alias;

                    if (key != wanted)
                        continue;

                    resolvedId = product.name;
                    DebugLogService.Instance.Verbose("Resolved product definition from ProductManager: " + resolvedId + " type=" + product.GetType().Name);
                    return product;
                }
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("Product definition lookup failed: " + ex.Message);
            }

            return null;
        }

        private static string NormalizeAliasKey(string value)
        {
            string key = NormalizeItemKey(value);
            if (key.StartsWith("the")) key = key.Substring(3);
            return key;
        }

        private static string GetDisplayName(string itemId, ItemDefinition definition)
        {
            string known = GetKnownDisplayName(NormalizeAliasKey(itemId));
            if (!string.IsNullOrEmpty(known))
                return known;

            try
            {
                string definitionName = definition?.name;
                known = GetKnownDisplayName(NormalizeAliasKey(definitionName));
                if (!string.IsNullOrEmpty(known))
                    return known;
            }
            catch { }

            return HumanizeItemId(itemId);
        }

        private static string GetCatalogCategory(string itemId, ItemDefinition definition)
        {
            string key = NormalizeAliasKey(itemId);
            string alias = GetKnownAlias(key);
            if (!string.IsNullOrEmpty(alias))
                key = alias;

            if (IsSeedKey(key) || TryCastDefinition<SeedDefinition>(definition) != null)
                return "Seeds";

            if (key == "mushroombed" ||
                key == "mushroomspawnstation" ||
                key == "mushroomsubstrate" ||
                key == "substratebag")
            {
                return "Grow";
            }

            if (key == "suspensionrack")
                return "Equipment";

            if (key == "bomb" || key == "rdx")
                return "Misc";
            if (key == "mushroomhat")
                return "Clothes";
            if (key == "cocaleaf")
                return "Drugs";

            if (ContainsAny(key, WeaponKeys))
                return "Weapons";
            if (TryCastDefinition<ProductDefinition>(definition) != null || ContainsAny(key, DrugKeys))
                return "Drugs";
            if (TryCastDefinition<PackagingDefinition>(definition) != null)
                return "Packaging";
            if (TryCastDefinition<ClothingDefinition>(definition) != null)
                return "Clothes";
            if (ContainsAny(key, IngredientKeys))
                return "Mixers";
            if (ContainsAny(key, SkateboardKeys))
                return "Skateboards";
            if (ContainsAny(key, StorageKeys))
                return "Storage";
            if (ContainsAny(key, EquipmentKeys))
                return "Equipment";
            if (ContainsAny(key, ToolKeys))
                return "Tools";
            if (ContainsAny(key, GrowKeys) ||
                TryCastDefinition<SoilDefinition>(definition) != null ||
                TryCastDefinition<AdditiveDefinition>(definition) != null ||
                TryCastDefinition<SporeSyringeDefinition>(definition) != null ||
                TryCastDefinition<ShroomSpawnDefinition>(definition) != null)
            {
                return "Grow";
            }

            if (ContainsAny(key, LightKeys))
                return "Lights";
            if (ContainsAny(key, DecorKeys))
                return "Decor";
            if (TryCastDefinition<BuildableItemDefinition>(definition) != null)
                return "Furniture";
            if (TryCastDefinition<EquippableItemDefinition>(definition) != null ||
                TryCastDefinition<WaterContainerDefinition>(definition) != null)
            {
                return "Tools";
            }

            return "Misc";
        }

        private static bool IsSeedKey(string key)
        {
            return key == "ogkushseed" ||
                key == "greencrackseed" ||
                key == "granddaddypurpleseed" ||
                key == "sourdieselseed" ||
                key == "cocaseed";
        }

        private static bool ContainsAny(string key, string[] needles)
        {
            if (string.IsNullOrEmpty(key) || needles == null)
                return false;

            for (int i = 0; i < needles.Length; i++)
            {
                string needle = needles[i];
                if (!string.IsNullOrEmpty(needle) && key.Contains(needle))
                    return true;
            }

            return false;
        }

        private static string HumanizeItemId(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return string.Empty;

            string text = itemId
                .Replace("_", " ")
                .Replace("-", " ")
                .Replace("(Clone)", string.Empty)
                .Replace("Built", string.Empty);

            var chars = new List<char>(text.Length + 8);
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                char previous = i > 0 ? text[i - 1] : '\0';
                char next = i + 1 < text.Length ? text[i + 1] : '\0';

                bool startsWord = i > 0 &&
                    !char.IsWhiteSpace(previous) &&
                    char.IsUpper(c) &&
                    (char.IsLower(previous) || char.IsDigit(previous) ||
                     (char.IsUpper(previous) && char.IsLower(next)));

                if (startsWord)
                    chars.Add(' ');
                chars.Add(c);
            }

            text = new string(chars.ToArray()).Trim();
            text = text
                .Replace("M 1911", "M1911")
                .Replace("P G R", "PGR")
                .Replace("R D X", "RDX")
                .Replace("O G ", "OG ")
                .Replace("A C ", "AC ")
                .Replace("T V", "TV")
                .Replace("L E D", "LED")
                .Replace("Mk Ii", "Mk II")
                .Replace("Mk 2", "Mk 2");

            return text;
        }

        private static string GetKnownDisplayName(string normalized)
        {
            switch (normalized)
            {
                case "acunit": return "AC Unit";
                case "bigsprinkler": return "Big Sprinkler";
                case "brickpress": return "Brick Press";
                case "brutdugloop": return "Brut du Gloop";
                case "chateaulapeepee": return "Chateau La Peepee";
                case "cocaleaf": return "Coca Leaf";
                case "cocaseed": return "Coca Seed";
                case "cocainebase": return "Cocaine Base";
                case "displaycabinet": return "Display Cabinet";
                case "electricplanttrimmers": return "Electric Plant Trimmers";
                case "extralonglifesoil": return "Extra Long-Life Soil";
                case "flumedicine": return "Flu Medicine";
                case "fullspectrumgrowlight": return "Full Spectrum Grow Light";
                case "graffiticleaner":
                case "grafitticleaner": return "Graffiti Cleaner";
                case "granddaddypurple": return "Granddaddy Purple";
                case "granddaddypurpleseed": return "Granddaddy Purple Seed";
                case "greencrack": return "Green Crack";
                case "greencrackseed": return "Green Crack Seed";
                case "growtent": return "Grow Tent";
                case "halogengrowlight": return "Halogen Grow Light";
                case "horsesemen": return "Horse Semen";
                case "laboven": return "Lab Oven";
                case "ledgrowlight": return "LED Grow Light";
                case "longlifesoil": return "Long-Life Soil";
                case "megabean": return "Mega Bean";
                case "mixingstationmk2":
                case "mixingstationmkii": return "Mixing Station Mk 2";
                case "moisturepreservingpot": return "Moisture-Preserving Pot";
                case "motoroil": return "Motor Oil";
                case "mouthwash": return "Mouth Wash";
                case "mushroombed": return "Mushroom Bed";
                case "mushroomhat": return "Mushroom Hat";
                case "mushroomspawnstation": return "Mushroom Spawn Station";
                case "mushroomsubstrate": return "Mushroom Substrate";
                case "substratebag": return "Mushroom Substrate";
                case "ogkush": return "OG Kush";
                case "ogkushseed": return "OG Kush Seed";
                case "oldmanjimmyswhiskey": return "Ol' Man Jimmy's Whiskey";
                case "packagingstationmk2":
                case "packagingstationmkii": return "Packaging Station Mk 2";
                case "planttrimmers": return "Plant Trimmers";
                case "potsprinkler": return "Pot Sprinkler";
                case "pseudo":
                case "pseudoproduct": return "Pseudo";
                case "pumpshotgun": return "Pump Shotgun";
                case "revolvercylinder": return "Revolver Cylinder";
                case "shroomspawn": return "Shroom Spawn";
                case "soilpourer": return "Soil Pourer";
                case "sourdiesel": return "Sour Diesel";
                case "sourdieselseed": return "Sour Diesel Seed";
                case "speedgrow": return "Speed Grow";
                case "sporesyringe": return "Spore Syringe";
                case "floorrack":
                case "suspensionrack":
                case "suspensionrackbuilt": return "Suspension Rack";
                case "trashgrabber": return "Trash Grabber";
                case "viagra":
                case "viagor": return "Viagor";
                case "wallmountedshelf": return "Wall-Mounted Shelf";
                default: return null;
            }
        }

        private static string GetKnownAlias(string normalized)
        {
            switch (normalized)
            {
                case "okkush": return "ogkush";
                case "ogkush": return "ogkush";
                case "granddaddypurp": return "granddaddypurple";
                case "granddaddypurps": return "granddaddypurple";
                case "sourdiesel": return "sourdiesel";
                case "greencrack": return "greencrack";
                case "granddaddypurpleseed": return "granddaddypurpleseed";
                case "greencrackseed": return "greencrackseed";
                case "ogkushseed": return "ogkushseed";
                case "sourdieselseed": return "sourdieselseed";
                case "buttonuprolled": return "rolledbuttonup";
                case "tshirt": return "tshirt";
                case "goldwristwatch": return "goldwatch";
                case "silverwristwatch": return "silverwatch";
                case "graffiticleaner": return "graffiticleaner";
                case "grafitticleaner": return "graffiticleaner";
                case "viagra": return "viagor";
                case "viagor": return "viagor";
                case "packagingstationmkii": return "packagingstationmk2";
                case "mixingstationmkii": return "mixingstationmk2";
                case "pseudoproduct": return "pseudo";
                case "floorrack": return "suspensionrack";
                case "suspensionrackbuilt": return "suspensionrack";
                case "substratebag": return "mushroomsubstrate";
                case "chateaulepeepee": return "chateaulapeepee";
                case "chateaulapeepee": return "chateaulapeepee";
                default: return null;
            }
        }

        private SlotSnapshot[] CaptureSlotSnapshot(PlayerInventory playerInventory)
        {
            try
            {
                var slots = playerInventory?.GetAllInventorySlots();
                if (slots == null)
                    return new SlotSnapshot[0];

                var snapshot = new SlotSnapshot[slots.Count];
                for (int i = 0; i < slots.Count; i++)
                {
                    try
                    {
                        var instance = slots[i]?.ItemInstance;
                        if (instance == null)
                            continue;

                        snapshot[i].HasItem = true;
                        snapshot[i].ItemKey = NormalizeItemKey(instance.Definition?.name);
                        snapshot[i].Quantity = slots[i].Quantity;
                        QualityItemInstance qi = TryGetQualityInstance(instance);
                        if (qi != null)
                        {
                            snapshot[i].HasQuality = true;
                            snapshot[i].Quality = qi.Quality;
                        }
                    }
                    catch { }
                }

                return snapshot;
            }
            catch
            {
                return new SlotSnapshot[0];
            }
        }

        private void ApplyQualityToChangedInventorySlots(PlayerInventory playerInventory, string itemId, SlotSnapshot[] beforeSlots, EQuality quality)
        {
            try
            {
                var slots = playerInventory?.GetAllInventorySlots();
                if (slots == null)
                    return;

                string wanted = NormalizeItemKey(itemId);
                int fixedSlots = 0;

                for (int i = 0; i < slots.Count; i++)
                {
                    try
                    {
                        var instance = slots[i]?.ItemInstance;
                        QualityItemInstance qi = TryGetQualityInstance(instance);
                        if (qi == null)
                            continue;

                        string slotKey = NormalizeItemKey(instance.Definition?.name);
                        if (slotKey != wanted)
                            continue;

                        bool changed = i >= beforeSlots.Length || !beforeSlots[i].HasItem;
                        if (!changed && beforeSlots[i].ItemKey == wanted)
                        {
                            int nowQty = 0;
                            try { nowQty = slots[i].Quantity; } catch { }
                            changed = nowQty > beforeSlots[i].Quantity ||
                                (beforeSlots[i].HasQuality && beforeSlots[i].Quality != qi.Quality);
                        }

                        if (!changed)
                            continue;

                        try { qi.Quality = quality; } catch { }
                        try { qi.SetQuality(quality); } catch { }
                        try { slots[i].SetStoredItem(instance, false); } catch { }
                        try { slots[i].ReplicateStoredInstance(); } catch { }
                        fixedSlots++;
                    }
                    catch { }
                }

                DebugLogService.Instance.Verbose("Quality enforcement changedSlots=" + fixedSlots + " item=" + itemId + " quality=" + quality);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[Nugzz] Changed-slot quality enforcement failed: " + ex.Message);
            }
        }

        public int ClearInventoryItemsOnly()
        {
            int cleared = 0;
            try
            {
                var inv = ManagerCacheService.Instance.PlayerInventory ?? UnityEngine.Object.FindObjectOfType<PlayerInventory>();
                var slots = inv?.GetAllInventorySlots();
                if (inv == null || slots == null)
                    return 0;

                for (int i = 0; i < slots.Count; i++)
                {
                    var slot = slots[i];
                    if (slot == null || slot.ItemInstance == null)
                        continue;

                    try
                    {
                        if (inv.cashSlot != null && object.ReferenceEquals(slot, inv.cashSlot))
                            continue;
                    }
                    catch { }

                    try
                    {
                        slot.ClearStoredInstance(false);
                        cleared++;
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogWarning("[Nugzz] Failed to clear inventory slot: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[Nugzz] Clear inventory failed: " + ex.Message);
            }

            NotificationService.Instance.Status("Cleared inventory items: " + cleared);
            return cleared;
        }

        private ItemInstance CreateItemInstance(ItemDefinition definition, string itemId, int quantity, int qualityIndex)
        {
            if (definition == null || quantity <= 0)
                return null;

            ItemInstance instance = null;
            try
            {
                EQuality requestedQuality = GetQuality(qualityIndex);

                // Product and quality item constructors in the assembly dump accept EQuality.
                // Build with selected quality up front instead of creating Standard defaults and
                // trying to mutate quality afterward.
                try
                {
                    instance = CreateQualityAwareInstance(definition, quantity, requestedQuality);
                    if (instance != null)
                    {
                        DebugLogService.Instance.Verbose($"Created selected-quality instance: {requestedQuality} for {itemId}");
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning("[Nugzz] Direct selected-quality instance create failed, falling back to default instance: " + ex.Message);
                    instance = null;
                }

                // GetDefaultInstance is a virtual method - must catch IL2CPP exceptions
                if (instance == null)
                {
                    try
                    {
                        instance = GetDefaultInstanceWithTemporaryQuality(definition, quantity, requestedQuality);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError("[Nugzz] GetDefaultInstance failed for '" + itemId + "': " + ex.Message);
                        return null;
                    }
                }

                if (instance == null)
                {
                    UnityEngine.Debug.LogWarning("[Nugzz] GetDefaultInstance returned null for '" + itemId + "' - item may not be spawnable");
                    instance = CreateFallbackStorableInstance(definition, quantity);
                    if (instance == null)
                        return null;
                }

                DebugLogService.Instance.Verbose($"Created instance: {instance?.GetType().Name} for {itemId}");

                if (IsQualityCapableDefinition(definition) && TryGetQualityInstance(instance) == null)
                {
                    UnityEngine.Debug.LogWarning("[Nugzz] Created instance for quality-capable item is not quality-capable: " + itemId + " type=" + instance.GetType().Name);
                    return null;
                }

                // Set quality on quality-capable items via direct field access
                // Use IL2CPP-safe approach
                try
                {
                    QualityItemInstance qi = TryGetQualityInstance(instance);
                    if (qi != null)
                    {
                        ForceInstanceQuality(qi, requestedQuality, "factory-finalize");
                        DebugLogService.Instance.Verbose($"Set quality {qi.Quality} on {itemId}");
                    }
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogWarning("[Nugzz] Quality setting skipped: " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("[Nugzz] Safe item factory failed for '" + itemId + "': " + ex);
                return null;
            }

            return instance;
        }

        private static bool IsQualityCapableDefinition(ItemDefinition definition)
        {
            return TryCastDefinition<ProductDefinition>(definition) != null ||
                TryCastDefinition<QualityItemDefinition>(definition) != null;
        }

        private ItemInstance CreateFallbackStorableInstance(ItemDefinition definition, int quantity)
        {
            try
            {
                StorableItemDefinition storableDefinition = TryCastDefinition<StorableItemDefinition>(definition);
                if (storableDefinition != null)
                {
                    UnityEngine.Debug.LogWarning("[Nugzz] Using fallback StorableItemInstance for " + definition.name);
                    return new StorableItemInstance(storableDefinition, quantity);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[Nugzz] Fallback storable instance failed: " + ex.Message);
            }

            return null;
        }

        private ItemInstance CreateQualityAwareInstance(ItemDefinition definition, int quantity, EQuality quality)
        {
            if (definition == null || quantity <= 0)
                return null;

            try
            {
                CocaineDefinition cocaineDefinition = TryCastDefinition<CocaineDefinition>(definition);
                if (cocaineDefinition != null)
                    return new CocaineInstance(cocaineDefinition, quantity, quality, null);
            }
            catch (Exception ex) { UnityEngine.Debug.LogWarning("[Nugzz] CocaineInstance create failed: " + ex.Message); }

            try
            {
                MethDefinition methDefinition = TryCastDefinition<MethDefinition>(definition);
                if (methDefinition != null)
                    return new MethInstance(methDefinition, quantity, quality, null);
            }
            catch (Exception ex) { UnityEngine.Debug.LogWarning("[Nugzz] MethInstance create failed: " + ex.Message); }

            try
            {
                WeedDefinition weedDefinition = TryCastDefinition<WeedDefinition>(definition);
                if (weedDefinition != null)
                    return new WeedInstance(weedDefinition, quantity, quality, null);
            }
            catch (Exception ex) { UnityEngine.Debug.LogWarning("[Nugzz] WeedInstance create failed: " + ex.Message); }

            try
            {
                ShroomDefinition shroomDefinition = TryCastDefinition<ShroomDefinition>(definition);
                if (shroomDefinition != null)
                    return new ShroomInstance(shroomDefinition, quantity, quality, null);
            }
            catch (Exception ex) { UnityEngine.Debug.LogWarning("[Nugzz] ShroomInstance create failed: " + ex.Message); }

            try
            {
                ProductDefinition productDefinition = TryCastDefinition<ProductDefinition>(definition);
                if (productDefinition != null)
                    return new ProductItemInstance(productDefinition, quantity, quality, null);
            }
            catch (Exception ex) { UnityEngine.Debug.LogWarning("[Nugzz] ProductItemInstance create failed: " + ex.Message); }

            try
            {
                QualityItemDefinition qualityDefinition = TryCastDefinition<QualityItemDefinition>(definition);
                if (qualityDefinition != null)
                    return new QualityItemInstance(qualityDefinition, quantity, quality);
            }
            catch (Exception ex) { UnityEngine.Debug.LogWarning("[Nugzz] QualityItemInstance create failed: " + ex.Message); }

            return null;
        }

        private ItemInstance GetDefaultInstanceWithTemporaryQuality(ItemDefinition definition, int quantity, EQuality quality)
        {
            QualityItemDefinition qualityDefinition = TryCastDefinition<QualityItemDefinition>(definition);
            if (qualityDefinition != null)
            {
                EQuality previous = qualityDefinition.DefaultQuality;
                try
                {
                    qualityDefinition.DefaultQuality = quality;
                    return definition.GetDefaultInstance(quantity);
                }
                finally
                {
                    try { qualityDefinition.DefaultQuality = previous; } catch { }
                }
            }

            return definition.GetDefaultInstance(quantity);
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

        private static QualityItemInstance TryGetQualityInstance(ItemInstance instance)
        {
            if (instance == null)
                return null;

            try
            {
                return instance.TryCast<QualityItemInstance>();
            }
            catch
            {
                return instance as QualityItemInstance;
            }
        }

        private static EQuality GetQuality(int qualityIndex)
        {
            switch (qualityIndex)
            {
                case 0: return EQuality.Trash;
                case 1: return EQuality.Poor;
                case 3: return EQuality.Premium;
                case 4: return EQuality.Heavenly;
                default: return EQuality.Standard;
            }
        }

        public int GetPageCount()
        {
            if (_filteredCount == 0) return 1;
            int p = _filteredCount / _itemsPerPage;
            if (_filteredCount % _itemsPerPage > 0) p++;
            return p;
        }

        public int GetPageIndex() => _pageIndex;

        public void SetPageIndex(int idx)
        {
            int max = GetPageCount();
            if (max <= 1) { _pageIndex = 0; return; }
            if (idx < 0) idx = 0;
            if (idx >= max) idx = max - 1;
            _pageIndex = idx;
        }

        public void PreviousPage() => SetPageIndex(_pageIndex - 1);
        public void NextPage() => SetPageIndex(_pageIndex + 1);

        public int GetCurrentPageItemCount()
        {
            int start = _pageIndex * _itemsPerPage;
            if (start >= _filteredCount) return 0;
            int end = start + _itemsPerPage;
            if (end > _filteredCount) end = _filteredCount;
            return end - start;
        }

        public int GetFilteredCount() => _filteredCount;

        public string GetItemIdAt(int idx)
        {
            if (idx < 0 || idx >= _filteredCount) return null;
            int actual = _filteredIndices[idx];
            if (actual < 0 || actual >= _itemCount) return null;
            return _itemIds[actual];
        }

        private void LogInstanceQuality(string stage, ItemInstance instance)
        {
            try
            {
                if (!DebugLogService.Instance.VerboseEnabled || instance == null)
                    return;

                QualityItemInstance qi = TryGetQualityInstance(instance);
                string quality = qi != null ? qi.Quality.ToString() : "n/a";
                DebugLogService.Instance.Verbose(stage + ": instanceType=" + instance.GetType().Name + " def=" + (instance.Definition?.name ?? "null") + " quality=" + quality);
            }
            catch { }
        }

        private void LogMatchingInventoryQuality(PlayerInventory playerInventory, string itemId, string stage)
        {
            try
            {
                if (!DebugLogService.Instance.VerboseEnabled)
                    return;

                var slots = playerInventory?.GetAllInventorySlots();
                if (slots == null)
                    return;

                string wanted = NormalizeItemKey(itemId);
                for (int i = 0; i < slots.Count; i++)
                {
                    var instance = slots[i]?.ItemInstance;
                    if (instance == null)
                        continue;

                    string slotId = null;
                    try { slotId = instance.Definition?.name; } catch { }
                    if (NormalizeItemKey(slotId) != wanted)
                        continue;

                    QualityItemInstance qi = TryGetQualityInstance(instance);
                    string quality = qi != null ? qi.Quality.ToString() : "n/a";
                    DebugLogService.Instance.Verbose(stage + ": slot=" + i + " id=" + slotId + " qty=" + slots[i].Quantity + " quality=" + quality + " type=" + instance.GetType().Name);
                }
            }
            catch { }
        }

        public string GetCurrentPageItemIdAt(int pageSlot)
        {
            if (pageSlot < 0 || pageSlot >= _itemsPerPage) return null;
            return GetItemIdAt((_pageIndex * _itemsPerPage) + pageSlot);
        }

        public string GetItemNameAt(int idx)
        {
            if (idx < 0 || idx >= _filteredCount) return null;
            int actual = _filteredIndices[idx];
            if (actual < 0 || actual >= _itemCount) return null;
            return _itemNames[actual];
        }

        public string GetCurrentPageItemNameAt(int pageSlot)
        {
            if (pageSlot < 0 || pageSlot >= _itemsPerPage) return null;
            return GetItemNameAt((_pageIndex * _itemsPerPage) + pageSlot);
        }

        public bool IsCached => _isCached;
        public int ItemCount => _itemCount;

        public void ClearCache()
        {
            _itemIds = new string[0];
            _itemNames = new string[0];
            _itemCategories = new string[0];
            _itemDefinitions = new ItemDefinition[0];
            _itemCount = 0;
            _isCached = false;
            _filteredIndices = new int[0];
            _filteredCount = 0;
            _searchText = "";
        }
    }
}
