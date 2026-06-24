using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using Il2CppScheduleOne;
using Il2CppScheduleOne.EntityFramework;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.PlayerScripts;
using UnityEngine;

namespace NugzzMenu.Services
{
    public sealed class DebugTestRoomService
    {
        private const string RootName = "Nugzz_DebugTestRoom";
        private const int MaxBuildableDisplays = 120;
        private static readonly Vector3 FallbackRoomOrigin = new Vector3(1800f, 160f, 1800f);

        private static readonly DebugTestRoomService _instance = new DebugTestRoomService();
        public static DebugTestRoomService Instance => _instance;

        private readonly Dictionary<int, NpcSnapshot> _npcSnapshots = new Dictionary<int, NpcSnapshot>();
        private readonly List<Material> _createdMaterials = new List<Material>();
        private GameObject _root;
        private GameObject _displayRoot;
        private GameObject _npcRoot;
        private Vector3 _roomOrigin = FallbackRoomOrigin;
        private string _status = "Test room not loaded.";
        private int _displayedBuildables;
        private int _linedUpNpcs;

        private struct NpcSnapshot
        {
            public NPC Npc;
            public Vector3 Position;
            public Quaternion Rotation;
            public bool MovementEnabled;
        }

        private DebugTestRoomService() { }

        public string StatusMessage => _status;
        public bool IsLoaded => _root != null;
        public int DisplayedBuildables => _displayedBuildables;
        public int LinedUpNpcs => _linedUpNpcs;

        public bool CanMoveWorldNpcs()
        {
            try
            {
                return !LobbyService.Instance.IsInLobby() || LobbyService.Instance.IsHost();
            }
            catch
            {
                return true;
            }
        }

        public void LoadRoom()
        {
            try
            {
                if (IsLoaded || _npcSnapshots.Count > 0)
                    InputLockService.Instance.LockFor(0.85f);

                RestoreNpcs(false);
                DestroyRoomObjects(false);
                _roomOrigin = FallbackRoomOrigin;
                EnsureRoot();
                CreateRoomShell();
                CreateVendorKiosks();
                _displayedBuildables = CreateBuildableDisplays();
                int npcCount = CanMoveWorldNpcs() ? LineUpNpcsInternal(false) : 0;
                TeleportToRoom();

                _status = "Loaded test room: " + _displayedBuildables + " displays, " + npcCount + " NPCs.";
                TeleportService.Instance.MarkCatalogDirty();
                NotificationService.Instance.Status(_status);
            }
            catch (Exception ex)
            {
                _status = "Test room load failed: " + ex.Message;
                UnityEngine.Debug.LogWarning("[Nugzz] " + _status);
                NotificationService.Instance.Warning(_status);
            }
        }

        public void TeleportToRoom()
        {
            try
            {
                if (!IsLoaded)
                {
                    _status = "Load the test room first.";
                    NotificationService.Instance.Warning(_status);
                    return;
                }

                Player player = ManagerCacheService.Instance.LocalPlayer;
                if (player == null)
                {
                    _status = "No local player found.";
                    NotificationService.Instance.Warning(_status);
                    return;
                }

                Vector3 target = _roomOrigin + new Vector3(0f, 1.4f, -16f);
                CharacterController controller = null;
                try { controller = player.GetComponent<CharacterController>(); } catch { }

                try { if (controller != null) controller.enabled = false; } catch { }
                player.transform.position = target;
                player.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                try { if (controller != null) controller.enabled = true; } catch { }

                _status = "Teleported to debug test room.";
                NotificationService.Instance.Status(_status);
            }
            catch (Exception ex)
            {
                _status = "Teleport failed: " + ex.Message;
                UnityEngine.Debug.LogWarning("[Nugzz] " + _status);
                NotificationService.Instance.Warning(_status);
            }
        }

        public void LineUpNpcs()
        {
            LineUpNpcsInternal(true);
        }

        private int LineUpNpcsInternal(bool notify)
        {
            if (!CanMoveWorldNpcs())
            {
                _status = "NPC lineup is host-only in multiplayer.";
                if (notify)
                    NotificationService.Instance.Warning(_status);
                return 0;
            }

            try
            {
                if (!IsLoaded)
                    LoadRoom();

                EnsureRoot();
                if (_npcRoot == null)
                    _npcRoot = CreateEmpty("NPC Lineup", _root.transform);

                NPC[] npcs = UnityEngine.Object.FindObjectsOfType<NPC>(true);
                if (npcs == null || npcs.Length == 0)
                {
                    _status = "No loaded NPCs found to line up.";
                    if (notify)
                        NotificationService.Instance.Warning(_status);
                    return 0;
                }

                int moved = 0;
                int columns = 4;
                Vector3 start = _roomOrigin + new Vector3(35f, 0.08f, 31f);
                for (int i = 0; i < npcs.Length; i++)
                {
                    NPC npc = npcs[i];
                    if (!IsValidNpc(npc))
                        continue;

                    int id = npc.GetInstanceID();
                    if (!_npcSnapshots.ContainsKey(id))
                    {
                        _npcSnapshots[id] = new NpcSnapshot
                        {
                            Npc = npc,
                            Position = npc.transform.position,
                            Rotation = npc.transform.rotation,
                            MovementEnabled = npc.Movement != null && npc.Movement.enabled
                        };
                    }

                    int row = moved / columns;
                    int col = moved % columns;
                    Vector3 position = start + new Vector3(col * 2.2f, 0f, -row * 2.2f);
                    Quaternion rotation = Quaternion.Euler(0f, 180f, 0f);
                    ParkNpc(npc, position, rotation);
                    CreateOrMoveNpcLabel(npc, position + new Vector3(0f, 2.35f, 0f));
                    moved++;
                }

                _linedUpNpcs = moved;
                _status = "Lined up NPCs: " + moved;
                if (notify)
                    NotificationService.Instance.Status(_status);
                return moved;
            }
            catch (Exception ex)
            {
                _status = "NPC lineup failed: " + ex.Message;
                UnityEngine.Debug.LogWarning("[Nugzz] " + _status);
                if (notify)
                    NotificationService.Instance.Warning(_status);
                return 0;
            }
        }

        public void RestoreNpcs()
        {
            RestoreNpcs(true);
        }

        private void RestoreNpcs(bool notify)
        {
            int restored = 0;
            try
            {
                foreach (NpcSnapshot snapshot in _npcSnapshots.Values)
                {
                    if (!IsValidNpc(snapshot.Npc))
                        continue;

                    RestoreNpc(snapshot);
                    restored++;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[Nugzz] NPC restore failed: " + ex.Message);
            }

            _npcSnapshots.Clear();
            _linedUpNpcs = 0;
            _status = "Restored NPCs: " + restored;
            if (notify)
                NotificationService.Instance.Status(_status);
        }

        public void UnlockVendorAccess()
        {
            int changed = UnlockService.Instance.UnlockAllProductsAndItems();
            _status = "Vendor access unlocked/checked: " + changed;
        }

        public void ClearRoom()
        {
            InputLockService.Instance.LockFor(0.85f);
            RestoreNpcs(false);
            DestroyRoomObjects(true);
            _status = "Cleared debug test room.";
            TeleportService.Instance.MarkCatalogDirty();
            NotificationService.Instance.Status(_status);
        }

        public void ResetForScene()
        {
            InputLockService.Instance.LockFor(0.85f);
            RestoreNpcs(false);
            DestroyRoomObjects(false);
            _status = "Test room reset for scene change.";
            TeleportService.Instance.MarkCatalogDirty();
        }

        private void EnsureRoot()
        {
            if (_root != null)
                return;

            _root = new GameObject(RootName);
            _root.transform.position = _roomOrigin;
            _displayRoot = CreateEmpty("Buildable Displays", _root.transform);
            _npcRoot = CreateEmpty("NPC Lineup", _root.transform);
        }

        private static GameObject CreateEmpty(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            if (parent != null)
                obj.transform.SetParent(parent, false);
            return obj;
        }

        private void CreateRoomShell()
        {
            CreateBlock("Skybase Platform", _roomOrigin + new Vector3(0f, -0.12f, 0f), new Vector3(96f, 0.24f, 72f), new Color(0.18f, 0.18f, 0.18f));
            CreateBlock("North Rail", _roomOrigin + new Vector3(0f, 0.7f, 36f), new Vector3(96f, 1.4f, 0.8f), new Color(0.30f, 0.30f, 0.30f));
            CreateBlock("South Rail", _roomOrigin + new Vector3(0f, 0.7f, -36f), new Vector3(96f, 1.4f, 0.8f), new Color(0.30f, 0.30f, 0.30f));
            CreateBlock("East Rail", _roomOrigin + new Vector3(48f, 0.7f, 0f), new Vector3(0.8f, 1.4f, 72f), new Color(0.26f, 0.26f, 0.26f));
            CreateBlock("West Rail", _roomOrigin + new Vector3(-48f, 0.7f, 0f), new Vector3(0.8f, 1.4f, 72f), new Color(0.26f, 0.26f, 0.26f));

            CreateLabel("Nugzz Debug Skybase", _roomOrigin + new Vector3(0f, 3.8f, -34.5f), Quaternion.Euler(0f, 0f, 0f), 0.16f, Color.green, _root.transform);
            CreateLabel("Buildables are spawned as live test objects where possible. Some network-only objects may still need vanilla placement.",
                _roomOrigin + new Vector3(0f, 3.05f, -34.5f), Quaternion.Euler(0f, 0f, 0f), 0.075f, Color.white, _root.transform);

            CreateLight("Skybase Light A", _roomOrigin + new Vector3(-28f, 7f, -18f));
            CreateLight("Skybase Light B", _roomOrigin + new Vector3(28f, 7f, 18f));
            CreateLight("Skybase Light C", _roomOrigin + new Vector3(0f, 7.5f, 0f));
        }

        private void CreateVendorKiosks()
        {
            string[] labels =
            {
                "Hardware",
                "Gas-Mart",
                "Oscar",
                "Boutique",
                "Stan",
                "Shred Shack",
                "Suppliers",
                "Products"
            };

            Vector3 start = _roomOrigin + new Vector3(-38f, 0.55f, -29f);
            for (int i = 0; i < labels.Length; i++)
            {
                Vector3 position = start + new Vector3((i % 4) * 25f, 0f, -(i / 4) * 5f);
                CreateBlock("Vendor Kiosk " + labels[i], position, new Vector3(5f, 1.2f, 3f), new Color(0.08f, 0.25f, 0.12f));
                CreateLabel(labels[i], position + new Vector3(0f, 1.3f, -1.55f), Quaternion.Euler(0f, 0f, 0f), 0.09f, Color.white, _root.transform);
            }
        }

        private int CreateBuildableDisplays()
        {
            int count = 0;
            try
            {
                Registry registry = ManagerCacheService.Instance.Registry ?? UnityEngine.Object.FindObjectOfType<Registry>();
                var items = registry?.GetAllItems();
                if (items == null)
                    return 0;

                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < items.Count && count < MaxBuildableDisplays; i++)
                {
                    ItemDefinition definition = null;
                    try { definition = items[i]; } catch { }
                    BuildableItemDefinition buildableDefinition = TryCastDefinition<BuildableItemDefinition>(definition);
                    BuildableItem builtItem = buildableDefinition?.BuiltItem;
                    if (definition == null || builtItem == null || builtItem.gameObject == null)
                        continue;

                    string name = GetDefinitionLabel(definition);
                    if (!seen.Add(NormalizeKey(name)))
                        continue;

                    Vector3 position = GetDisplayPosition(count);
                    if (!CreateDisplayClone(builtItem.gameObject, name, position))
                        continue;

                    count++;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("[Nugzz] Buildable display creation failed: " + ex.Message);
            }

            return count;
        }

        private bool CreateDisplayClone(GameObject prefab, string label, Vector3 position)
        {
            try
            {
                GameObject clone = UnityEngine.Object.Instantiate(prefab, position, Quaternion.Euler(0f, 180f, 0f));
                if (clone == null)
                    return false;

                clone.name = "Display_" + label;
                clone.transform.SetParent(_displayRoot != null ? _displayRoot.transform : _root.transform, true);
                PrepareDisplayClone(clone);
                FitCloneToDisplay(clone, position, 3.2f);
                CreateLabel(label, position + new Vector3(0f, 2.35f, -1.8f), Quaternion.Euler(0f, 0f, 0f), 0.075f, Color.white, _displayRoot != null ? _displayRoot.transform : _root.transform);
                return true;
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("Display clone skipped for " + label + ": " + ex.Message);
                return false;
            }
        }

        private static void PrepareDisplayClone(GameObject clone)
        {
            try
            {
                Behaviour[] behaviours = clone.GetComponentsInChildren<Behaviour>(true);
                for (int i = 0; i < behaviours.Length; i++)
                {
                    Behaviour behaviour = behaviours[i];
                    if (behaviour == null || behaviour is Light)
                        continue;

                    behaviour.enabled = false;
                }
            }
            catch { }

            try
            {
                Collider[] colliders = clone.GetComponentsInChildren<Collider>(true);
                for (int i = 0; i < colliders.Length; i++)
                    if (colliders[i] != null)
                        colliders[i].enabled = false;
            }
            catch { }

            try
            {
                Rigidbody[] bodies = clone.GetComponentsInChildren<Rigidbody>(true);
                for (int i = 0; i < bodies.Length; i++)
                {
                    Rigidbody body = bodies[i];
                    if (body == null)
                        continue;

                    body.isKinematic = true;
                    body.useGravity = false;
                }
            }
            catch { }

            try { clone.SetActive(true); } catch { }
        }

        private static void FitCloneToDisplay(GameObject clone, Vector3 targetBase, float maxSize)
        {
            try
            {
                Bounds bounds;
                if (!TryGetRendererBounds(clone, out bounds))
                    return;

                float largest = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
                if (largest > maxSize && largest > 0.001f)
                {
                    float scale = maxSize / largest;
                    clone.transform.localScale *= scale;
                }

                if (TryGetRendererBounds(clone, out bounds))
                {
                    Vector3 delta = targetBase - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                    clone.transform.position += delta;
                }
            }
            catch { }
        }

        private static bool TryGetRendererBounds(GameObject obj, out Bounds bounds)
        {
            bounds = default;
            Renderer[] renderers = obj != null ? obj.GetComponentsInChildren<Renderer>(true) : null;
            if (renderers == null || renderers.Length == 0)
                return false;

            bool set = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                if (!set)
                {
                    bounds = renderer.bounds;
                    set = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return set;
        }

        private Vector3 GetDisplayPosition(int index)
        {
            const int columns = 12;
            int row = index / columns;
            int col = index % columns;
            return _roomOrigin + new Vector3(-42f + col * 7.5f, 0.05f, 23f - row * 5.8f);
        }

        private GameObject CreateBlock(string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = "Nugzz_" + name;
            block.transform.SetParent(_root.transform, true);
            block.transform.position = position;
            block.transform.localScale = scale;
            Renderer renderer = block.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material = CreateMaterial(color);
            return block;
        }

        private void CreateLight(string name, Vector3 position)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(_root.transform, true);
            obj.transform.position = position;
            Light light = AddComponentSafe<Light>(obj);
            if (light == null)
                return;

            light.type = LightType.Point;
            light.range = 70f;
            light.intensity = 2.2f;
            light.color = new Color(0.80f, 1f, 0.84f);
        }

        private void CreateOrMoveNpcLabel(NPC npc, Vector3 position)
        {
            string label = GetNpcName(npc);
            CreateLabel(label, position, Quaternion.Euler(0f, 0f, 0f), 0.07f, Color.yellow, _npcRoot != null ? _npcRoot.transform : _root.transform);
        }

        private void CreateLabel(
            string text,
            Vector3 position,
            Quaternion rotation,
            float size,
            Color color,
            Transform parent)
        {
            GameObject obj = new GameObject("Label_" + SanitizeName(text));
            if (parent != null)
                obj.transform.SetParent(parent, true);
            obj.transform.position = position;
            obj.transform.rotation = rotation;

            TextMesh mesh = AddComponentSafe<TextMesh>(obj);
            if (mesh == null)
                return;

            mesh.text = text ?? string.Empty;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = size;
            mesh.fontSize = 64;
            mesh.color = color;
        }

        private Material CreateMaterial(Color color)
        {
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Diffuse");
            Material material = shader != null ? new Material(shader) : new Material(Shader.Find("Hidden/Internal-Colored"));
            material.color = color;
            _createdMaterials.Add(material);
            return material;
        }

        private static T AddComponentSafe<T>(GameObject obj)
            where T : Component
        {
            if (obj == null)
                return null;

            try
            {
                Component component = obj.AddComponent(Il2CppType.Of<T>());
                try { return component?.TryCast<T>(); }
                catch { return component as T; }
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning(
                    "Failed to add component " + typeof(T).Name + ": " + ex.Message);
                return null;
            }
        }

        private static void ParkNpc(NPC npc, Vector3 position, Quaternion rotation)
        {
            if (npc == null)
                return;

            try { npc.Movement?.Stop(); } catch { }
            try { npc.Movement?.SetAgentEnabled(false); } catch { }
            try
            {
                if (npc.Movement != null)
                    npc.Movement.enabled = false;
            }
            catch { }

            try { npc.transform.SetPositionAndRotation(position, rotation); }
            catch
            {
                npc.transform.position = position;
                npc.transform.rotation = rotation;
            }
        }

        private static void RestoreNpc(NpcSnapshot snapshot)
        {
            NPC npc = snapshot.Npc;
            if (npc == null)
                return;

            try { npc.transform.SetPositionAndRotation(snapshot.Position, snapshot.Rotation); }
            catch
            {
                npc.transform.position = snapshot.Position;
                npc.transform.rotation = snapshot.Rotation;
            }

            try
            {
                if (npc.Movement != null)
                {
                    npc.Movement.enabled = snapshot.MovementEnabled;
                    npc.Movement.SetAgentEnabled(snapshot.MovementEnabled);
                }
            }
            catch { }
        }

        private void DestroyRoomObjects(bool notify)
        {
            try
            {
                if (_root != null)
                    UnityEngine.Object.Destroy(_root);
            }
            catch { }

            _root = null;
            _displayRoot = null;
            _npcRoot = null;
            _displayedBuildables = 0;

            for (int i = 0; i < _createdMaterials.Count; i++)
            {
                try
                {
                    if (_createdMaterials[i] != null)
                        UnityEngine.Object.Destroy(_createdMaterials[i]);
                }
                catch { }
            }
            _createdMaterials.Clear();

            if (notify)
                NotificationService.Instance.Status("Debug test room objects cleared.");
        }

        private static bool IsValidNpc(NPC npc)
        {
            try
            {
                return npc != null &&
                    npc.gameObject != null &&
                    npc.gameObject.activeInHierarchy &&
                    npc.gameObject.scene.IsValid();
            }
            catch
            {
                return false;
            }
        }

        private static T TryCastDefinition<T>(ItemDefinition definition)
            where T : ItemDefinition
        {
            if (definition == null)
                return null;

            try { return definition.TryCast<T>(); }
            catch { return definition as T; }
        }

        private static string GetDefinitionLabel(ItemDefinition definition)
        {
            if (definition == null)
                return "Unknown";

            try
            {
                if (!string.IsNullOrEmpty(definition.Name))
                    return definition.Name;
            }
            catch { }

            try { return definition.name ?? "Unknown"; }
            catch { return "Unknown"; }
        }

        private static string GetNpcName(NPC npc)
        {
            if (npc == null)
                return "NPC";

            try
            {
                if (!string.IsNullOrEmpty(npc.fullName))
                    return npc.fullName;
            }
            catch { }

            try
            {
                string first = npc.FirstName ?? string.Empty;
                string last = npc.hasLastName ? npc.LastName ?? string.Empty : string.Empty;
                string full = (first + " " + last).Trim();
                if (!string.IsNullOrEmpty(full))
                    return full;
            }
            catch { }

            try { return npc.name ?? "NPC"; }
            catch { return "NPC"; }
        }

        private static string SanitizeName(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "Unnamed";

            char[] chars = value.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]))
                    chars[i] = '_';
            }

            return new string(chars);
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
