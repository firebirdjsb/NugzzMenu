using System;
using System.Collections.Generic;
using System.Globalization;
using Il2CppScheduleOne.Interaction;
using Il2CppScheduleOne.Networking;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.Variables;
using UnityEngine;
using UnityEngine.Rendering;

namespace NugzzMenu.Services
{
    /// <summary>
    /// Host-owned lightweight primitive spawning. Clients send numeric requests;
    /// the host assigns IDs and broadcasts the resulting transform.
    /// </summary>
    public sealed class ShapePrefabService
    {
        private sealed class ShapeRecord
        {
            public int Id;
            public int Type;
            public int Scale;
            public int Color;
            public bool Physics;
            public GameObject Object;
        }

        private const string Prefix = "Nugzz.Shape.";
        private const string Request = Prefix + "Request";
        private const string SnapshotRequest = Prefix + "SnapshotRequest";
        private const string PickRequest = Prefix + "PickRequest";
        private const string ClearRequest = Prefix + "ClearRequest";
        private const string IdValue = Prefix + "Id";
        private const string TypeValue = Prefix + "Type";
        private const string ScaleValue = Prefix + "Scale";
        private const string ColorValue = Prefix + "Color";
        private const string PhysicsValue = Prefix + "Physics";
        private const string XValue = Prefix + "X";
        private const string YValue = Prefix + "Y";
        private const string ZValue = Prefix + "Z";
        private const string TargetValue = Prefix + "Target";
        private const string CommitValue = Prefix + "Commit";

        private static readonly ShapePrefabService _instance = new ShapePrefabService();
        public static ShapePrefabService Instance => _instance;

        private readonly Dictionary<int, ShapeRecord> _shapes = new Dictionary<int, ShapeRecord>();
        private readonly RaycastHit[] _pickupHits = new RaycastHit[32];
        private int _selectedType;
        private int _selectedScale = 1;
        private int _selectedColor;
        private bool _physicsForNewShapes;
        private int _nextId = 1000;
        private ShapeRecord _carried;
        private ShapeRecord _hovered;
        private RaycastHit _hoveredHit;
        private bool _snapshotRequested;
        private bool _promptFailureReported;
        private float _nextCleanup;
        private float _nextPickupScan;
        private static Material _materialTemplate;

        private int _pendingId;
        private int _pendingType;
        private int _pendingScale;
        private int _pendingColor;
        private bool _pendingPhysics;
        private int _pendingTarget;
        private float _pendingX;
        private float _pendingY;
        private float _pendingZ;

        private ShapePrefabService() { }

        public string SelectedTypeLabel => ShapeNames[_selectedType];
        public string SelectedScaleLabel => ScaleValues[_selectedScale].ToString("0.0", CultureInfo.InvariantCulture) + "x";
        public string SelectedColorLabel => ColorNames[_selectedColor];
        public string PhysicsModeLabel => _physicsForNewShapes ? "ON - movable" : "OFF - frozen";
        public int SpawnedCount => _shapes.Count;
        public bool IsCarrying => _carried != null;

        internal static bool IsNetworkVariable(string variableName)
        {
            return !string.IsNullOrEmpty(variableName) &&
                variableName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
        }

        public void CycleType(int direction) => _selectedType = Wrap(_selectedType + direction, ShapeNames.Length);
        public void CycleScale(int direction) => _selectedScale = Wrap(_selectedScale + direction, ScaleValues.Length);
        public void CycleColor(int direction) => _selectedColor = Wrap(_selectedColor + direction, ColorNames.Length);
        public void TogglePhysicsForNewShapes() => _physicsForNewShapes = !_physicsForNewShapes;
        public void ResetSpawnOptions() => _physicsForNewShapes = false;

        public void SpawnSelected()
        {
            if (!SessionAuthorityService.Instance.FeaturesAllowed)
                return;
            RequestSpawn(_selectedType, _selectedScale, _selectedColor, _physicsForNewShapes);
        }

        public void ClearAll()
        {
            if (LobbyService.Instance.IsInLobby() && !LobbyService.Instance.IsHost())
            {
                SendValue(Player.Local, ClearRequest, 1f);
                NotificationService.Instance.Status("Requested shape cleanup from host");
                return;
            }

            ClearHostShapes(true);
        }

        public void ResetForScene()
        {
            ClearLocalShapes();
            _carried = null;
            _hovered = null;
            _snapshotRequested = false;
            _nextId = 1000;
            _nextCleanup = 0f;
            _nextPickupScan = 0f;
        }

        internal void RequestHostSnapshot()
        {
            if (_snapshotRequested || !LobbyService.Instance.IsInLobby() ||
                LobbyService.Instance.IsHost() || Player.Local == null)
            {
                return;
            }

            _snapshotRequested = true;
            SendValue(Player.Local, SnapshotRequest, 1f);
        }

        public void Update(bool inputBlocked)
        {
            if (Time.unscaledTime >= _nextCleanup)
            {
                _nextCleanup = Time.unscaledTime + 1f;
                RemoveDestroyedRecords();
            }
            if (inputBlocked || !SessionAuthorityService.Instance.FeaturesAllowed)
                return;

            if (_carried != null)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    RequestSpawn(_carried.Type, _carried.Scale, _carried.Color, _carried.Physics);
                    _carried = null;
                    NotificationService.Instance.Status("Shape placed");
                }
                else if (Input.GetMouseButtonDown(1))
                {
                    RequestSpawn(_carried.Type, _carried.Scale, _carried.Color, _carried.Physics);
                    _carried = null;
                    NotificationService.Instance.Status("Shape pickup cancelled");
                }
                return;
            }

            if (_shapes.Count == 0)
            {
                _hovered = null;
                return;
            }

            if (Time.unscaledTime >= _nextPickupScan)
            {
                _nextPickupScan = Time.unscaledTime + 0.05f;
                TryFindPickupTarget(out _hovered, out _hoveredHit);
            }
            if (_hovered?.Object == null)
                return;

            ShowPickupPrompt(_hovered, _hoveredHit);
            if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
            {
                int id = _hovered.Id;
                _hovered = null;
                RequestPickup(id);
            }
        }

        internal bool TryReceiveNetworkValue(Player source, string variableName, string value)
        {
            if (!IsNetworkVariable(variableName))
                return false;
            if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float number))
                return true;

            if (string.Equals(variableName, Request, StringComparison.OrdinalIgnoreCase))
            {
                if (LobbyService.Instance.IsHost() && source != null && !source.IsLocalPlayer &&
                    SessionAuthorityService.Instance.IsClientApproved(source))
                    SpawnForPlayer(source, Mathf.RoundToInt(number));
                return true;
            }
            if (string.Equals(variableName, SnapshotRequest, StringComparison.OrdinalIgnoreCase))
            {
                if (LobbyService.Instance.IsHost() && source != null && !source.IsLocalPlayer &&
                    SessionAuthorityService.Instance.IsClientApproved(source))
                    BroadcastSnapshot();
                return true;
            }
            if (string.Equals(variableName, PickRequest, StringComparison.OrdinalIgnoreCase))
            {
                if (LobbyService.Instance.IsHost() && source != null && !source.IsLocalPlayer &&
                    SessionAuthorityService.Instance.IsClientApproved(source))
                    PickupForPlayer(source, Mathf.RoundToInt(number));
                return true;
            }
            if (string.Equals(variableName, ClearRequest, StringComparison.OrdinalIgnoreCase))
            {
                if (LobbyService.Instance.IsHost() && source != null && !source.IsLocalPlayer &&
                    SessionAuthorityService.Instance.IsClientApproved(source))
                    ClearHostShapes(true);
                return true;
            }

            if (!SessionAuthorityService.IsHostPlayer(source))
                return true;

            if (source != null && source.IsLocalPlayer && LobbyService.Instance.IsHost())
                return true;

            if (variableName == IdValue) _pendingId = Mathf.RoundToInt(number);
            else if (variableName == TypeValue) _pendingType = Mathf.RoundToInt(number);
            else if (variableName == ScaleValue) _pendingScale = Mathf.RoundToInt(number);
            else if (variableName == ColorValue) _pendingColor = Mathf.RoundToInt(number);
            else if (variableName == PhysicsValue) _pendingPhysics = number >= 0.5f;
            else if (variableName == XValue) _pendingX = number;
            else if (variableName == YValue) _pendingY = number;
            else if (variableName == ZValue) _pendingZ = number;
            else if (variableName == TargetValue) _pendingTarget = Mathf.RoundToInt(number);
            else if (variableName == CommitValue) ApplyCommit(Mathf.RoundToInt(number));
            return true;
        }

        private void RequestSpawn(int type, int scale, int color, bool physics)
        {
            int packed = Pack(type, scale, color, physics);
            Player player = Player.Local;
            if (player == null)
                return;

            if (!LobbyService.Instance.IsInLobby() || LobbyService.Instance.IsHost())
                SpawnForPlayer(player, packed);
            else
                SendValue(player, Request, packed);
        }

        private void RequestPickup(int id)
        {
            Player player = Player.Local;
            if (player == null)
                return;
            if (!LobbyService.Instance.IsInLobby() || LobbyService.Instance.IsHost())
                PickupForPlayer(player, id);
            else
                SendValue(player, PickRequest, id);
        }

        private void SpawnForPlayer(Player player, int packed)
        {
            Unpack(packed, out int type, out int scale, out int color, out bool physics);
            Vector3 position = FindSpawnPosition(player, type, scale);
            int id = ++_nextId;
            SpawnLocal(id, type, scale, color, physics, position);
            BroadcastShape(1, id, type, scale, color, physics, position, -1);
            NotificationService.Instance.Status("Spawned " + ShapeNames[type]);
        }

        private void PickupForPlayer(Player player, int id)
        {
            if (!_shapes.TryGetValue(id, out ShapeRecord record) || record == null)
                return;

            int target = GetClientId(player);
            DestroyRecord(record);
            BroadcastShape(2, id, record.Type, record.Scale, record.Color,
                record.Physics, Vector3.zero, target);
            if (player != null && player.IsLocalPlayer)
                SetCarried(record);
        }

        private void ApplyCommit(int operation)
        {
            if (operation == 1)
            {
                SpawnLocal(_pendingId, _pendingType, _pendingScale, _pendingColor,
                    _pendingPhysics,
                    new Vector3(_pendingX, _pendingY, _pendingZ));
                return;
            }

            if (!_shapes.TryGetValue(_pendingId, out ShapeRecord record))
                return;
            DestroyRecord(record);
            if (operation == 2 && _pendingTarget == GetClientId(Player.Local))
                SetCarried(record);
        }

        private void BroadcastShape(int operation, int id, int type, int scale, int color,
            bool physics, Vector3 position, int target)
        {
            if (!LobbyService.Instance.IsInLobby())
                return;
            Player host = Player.Local;
            if (host == null)
                return;

            PlayerValueRpcService.BroadcastToApprovedClients(host, IdValue, id);
            PlayerValueRpcService.BroadcastToApprovedClients(host, TypeValue, type);
            PlayerValueRpcService.BroadcastToApprovedClients(host, ScaleValue, scale);
            PlayerValueRpcService.BroadcastToApprovedClients(host, ColorValue, color);
            PlayerValueRpcService.BroadcastToApprovedClients(host, PhysicsValue, physics ? 1f : 0f);
            PlayerValueRpcService.BroadcastToApprovedClients(host, XValue, position.x);
            PlayerValueRpcService.BroadcastToApprovedClients(host, YValue, position.y);
            PlayerValueRpcService.BroadcastToApprovedClients(host, ZValue, position.z);
            PlayerValueRpcService.BroadcastToApprovedClients(host, TargetValue, target);
            PlayerValueRpcService.BroadcastToApprovedClients(host, CommitValue, operation);
        }

        private void ClearHostShapes(bool broadcast)
        {
            int[] ids = new int[_shapes.Count];
            _shapes.Keys.CopyTo(ids, 0);
            for (int i = 0; i < ids.Length; i++)
            {
                if (!_shapes.TryGetValue(ids[i], out ShapeRecord record))
                    continue;
                DestroyRecord(record);
                if (broadcast)
                    BroadcastShape(3, ids[i], 0, 0, 0, false, Vector3.zero, -1);
            }
            NotificationService.Instance.Status("Cleared spawned shapes");
        }

        private void BroadcastSnapshot()
        {
            foreach (ShapeRecord record in _shapes.Values)
            {
                if (record?.Object == null)
                    continue;
                BroadcastShape(1, record.Id, record.Type, record.Scale, record.Color,
                    record.Physics,
                    record.Object.transform.position, -1);
            }
        }

        private void ClearLocalShapes()
        {
            int[] ids = new int[_shapes.Count];
            _shapes.Keys.CopyTo(ids, 0);
            for (int i = 0; i < ids.Length; i++)
            {
                if (_shapes.TryGetValue(ids[i], out ShapeRecord record))
                    DestroyRecord(record);
            }
        }

        private void SpawnLocal(int id, int type, int scaleIndex, int colorIndex,
            bool physics, Vector3 position)
        {
            if (_shapes.TryGetValue(id, out ShapeRecord existing))
                DestroyRecord(existing);

            type = Mathf.Clamp(type, 0, ShapeNames.Length - 1);
            scaleIndex = Mathf.Clamp(scaleIndex, 0, ScaleValues.Length - 1);
            colorIndex = Mathf.Clamp(colorIndex, 0, Colors.Length - 1);
            GameObject shape = CreateShape(type);
            if (shape == null)
                return;

            shape.name = "NugzzShape_" + id + "_" + ShapeNames[type];
            shape.transform.position = position;
            float size = ScaleValues[scaleIndex];
            shape.transform.localScale = type == 4
                ? new Vector3(size * 0.1f, 1f, size * 0.1f)
                : Vector3.one * size;
            ConfigurePhysics(shape, type, size, physics);

            try
            {
                Material material = CreateVisibleMaterial(Colors[colorIndex]);
                Renderer[] renderers = shape.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].enabled = true;
                    renderers[i].shadowCastingMode = ShadowCastingMode.On;
                    renderers[i].receiveShadows = true;
                    if (material != null)
                        renderers[i].sharedMaterial = material;
                }
            }
            catch { }

            _shapes[id] = new ShapeRecord
            {
                Id = id,
                Type = type,
                Scale = scaleIndex,
                Color = colorIndex,
                Physics = physics,
                Object = shape
            };
        }

        private static void ConfigurePhysics(GameObject shape, int type, float size, bool enabled)
        {
            if (shape == null)
                return;

            Collider collider = shape.GetComponent<Collider>();
            if (type == 4 && enabled && collider is MeshCollider)
            {
                collider.enabled = false;
                BoxCollider box = shape.AddComponent<BoxCollider>();
                box.center = Vector3.zero;
                box.size = new Vector3(10f, 0.08f, 10f);
            }
            if (!enabled)
                return;

            Rigidbody body = shape.AddComponent<Rigidbody>();
            body.useGravity = true;
            body.isKinematic = false;
            body.mass = Mathf.Clamp(size * size * size, 0.25f, 40f);
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.drag = 0.05f;
            body.angularDrag = 0.1f;
        }

        private static Material CreateVisibleMaterial(Color color)
        {
            if (_materialTemplate == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    shader = Shader.Find("Standard");
                if (shader != null)
                    _materialTemplate = new Material(shader) { name = "NugzzShapeMaterial" };
            }

            if (_materialTemplate == null)
                return null;

            Material material = new Material(_materialTemplate);
            material.color = color;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            material.renderQueue = 2000;
            return material;
        }

        private static GameObject CreateShape(int type)
        {
            PrimitiveType primitive = type == 0 ? PrimitiveType.Cube :
                type == 1 ? PrimitiveType.Sphere :
                type == 2 ? PrimitiveType.Capsule :
                type == 3 ? PrimitiveType.Cylinder :
                type == 4 ? PrimitiveType.Plane : PrimitiveType.Cube;
            GameObject shape = GameObject.CreatePrimitive(primitive);
            if (type != 5)
                return shape;

            try
            {
                Mesh mesh = new Mesh { name = "NugzzTrianglePrism" };
                mesh.vertices = new[]
                {
                    new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0f, 0.5f, -0.5f),
                    new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0f, 0.5f, 0.5f)
                };
                mesh.triangles = new[]
                {
                    0,2,1, 3,4,5, 0,1,4, 0,4,3, 1,2,5, 1,5,4, 2,0,3, 2,3,5
                };
                mesh.RecalculateNormals();
                shape.GetComponent<MeshFilter>().mesh = mesh;
            }
            catch { }
            return shape;
        }

        private static Vector3 FindSpawnPosition(Player player, int type, int scaleIndex)
        {
            Vector3 origin = player.transform.position + Vector3.up * 1.2f;
            Vector3 forward = player.transform.forward;
            Camera camera = player.IsLocalPlayer ? Camera.main : null;
            if (camera != null)
                forward = camera.transform.forward;
            Vector3 candidate = origin + forward.normalized * 2.5f;
            if (Physics.Raycast(candidate + Vector3.up * 4f, Vector3.down,
                    out RaycastHit hit, 10f, -5, QueryTriggerInteraction.Ignore))
            {
                float size = ScaleValues[Mathf.Clamp(scaleIndex, 0, ScaleValues.Length - 1)];
                float floorOffset = type == 4 ? 0.02f :
                    (type == 2 || type == 3 ? size : size * 0.5f);
                candidate.y = hit.point.y + floorOffset;
            }
            return candidate;
        }

        private ShapeRecord FindRecord(Transform transform)
        {
            while (transform != null)
            {
                foreach (ShapeRecord record in _shapes.Values)
                {
                    if (record?.Object != null && record.Object.transform == transform)
                        return record;
                }
                transform = transform.parent;
            }
            return null;
        }

        private bool TryFindPickupTarget(out ShapeRecord record, out RaycastHit hit)
        {
            record = null;
            hit = default;
            Camera camera = Camera.main;
            if (camera == null)
                return false;

            Vector3 origin = camera.transform.position;
            Vector3 direction = camera.transform.forward;
            if (Physics.Raycast(origin, direction, out RaycastHit directHit, 6f, -5,
                    QueryTriggerInteraction.Collide))
            {
                ShapeRecord direct = FindRecord(directHit.collider?.transform);
                if (direct != null)
                {
                    record = direct;
                    hit = directHit;
                    return true;
                }
            }

            int hitCount = Physics.SphereCastNonAlloc(origin, 0.3f, direction, _pickupHits, 6f,
                -5, QueryTriggerInteraction.Collide);
            float bestDistance = float.MaxValue;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit candidateHit = _pickupHits[i];
                ShapeRecord candidate = FindRecord(candidateHit.collider?.transform);
                if (candidate == null || candidate.Object == null || candidateHit.distance >= bestDistance)
                    continue;
                if (!HasLineOfSight(camera, candidate, candidateHit))
                    continue;

                record = candidate;
                hit = candidateHit;
                bestDistance = candidateHit.distance;
            }
            return record != null;
        }

        private bool HasLineOfSight(Camera camera, ShapeRecord record, RaycastHit shapeHit)
        {
            Vector3 origin = camera.transform.position + camera.transform.forward * 0.08f;
            Vector3 target = shapeHit.collider != null
                ? shapeHit.collider.bounds.center
                : record.Object.transform.position;
            Vector3 delta = target - origin;
            float distance = delta.magnitude;
            if (distance <= 0.01f || !Physics.Raycast(origin, delta / distance,
                    out RaycastHit blocker, distance + 0.05f, -5,
                    QueryTriggerInteraction.Collide))
                return true;
            return FindRecord(blocker.collider?.transform) == record;
        }

        private void ShowPickupPrompt(ShapeRecord record, RaycastHit hit)
        {
            try
            {
                InteractionCanvas canvas = InteractionCanvas.Instance;
                if (canvas == null)
                    return;

                InteractionManager manager = InteractionManager.Instance;
                Sprite icon = canvas.KeyIcon;
                if (icon == null && manager != null)
                    icon = manager.icon_Key;
                if (icon == null)
                    return;

                string key = manager?.InteractKeyStr;
                if (string.IsNullOrWhiteSpace(key))
                    key = "E";
                Vector3 position = hit.collider != null
                    ? (hit.distance <= 0.001f ? hit.collider.bounds.center : hit.point)
                    : record.Object.transform.position;
                canvas.EnableInteractionDisplay(
                    position,
                    "Pick up " + ShapeNames[record.Type],
                    canvas.DefaultMessageColor,
                    icon,
                    canvas.DefaultKeyColor,
                    key,
                    1f,
                    new Vector2(32f, 32f),
                    true);
            }
            catch (Exception ex)
            {
                if (_promptFailureReported)
                    return;
                _promptFailureReported = true;
                DebugLogService.Instance.VerboseWarning(
                    "Shape interaction prompt failed: " + ex.Message);
            }
        }

        private void SetCarried(ShapeRecord record)
        {
            _carried = new ShapeRecord
            {
                Type = record.Type,
                Scale = record.Scale,
                Color = record.Color,
                Physics = record.Physics
            };
            NotificationService.Instance.Status("Picked up shape - left click to place");
        }

        private void DestroyRecord(ShapeRecord record)
        {
            if (record == null)
                return;
            _shapes.Remove(record.Id);
            try { if (record.Object != null) UnityEngine.Object.Destroy(record.Object); } catch { }
        }

        private void RemoveDestroyedRecords()
        {
            List<int> dead = null;
            foreach (var pair in _shapes)
            {
                if (pair.Value?.Object != null)
                    continue;
                if (dead == null) dead = new List<int>();
                dead.Add(pair.Key);
            }
            if (dead == null)
                return;
            for (int i = 0; i < dead.Count; i++)
                _shapes.Remove(dead[i]);
        }

        private static void SendValue(Player player, string name, float value)
        {
            if (player == null)
                return;
            try
            {
                if (player.GetVariable(name) == null)
                {
                    player.AddVariable(new NumberVariable(name,
                        EVariableReplicationMode.Networked, false,
                        EVariableMode.Player, player, 0f));
                }
                string text = value.ToString("0.###", CultureInfo.InvariantCulture);
                player.SetVariableValue(name, text, false);
                player.SendValue(name, text, true);
            }
            catch (Exception ex)
            {
                DebugLogService.Instance.VerboseWarning("Shape network value failed: " + ex.Message);
            }
        }

        private static int GetClientId(Player player)
        {
            try { return player?.Owner?.ClientId ?? -1; }
            catch { return -1; }
        }

        private static int Pack(int type, int scale, int color, bool physics) =>
            Mathf.Clamp(type, 0, 7) | (Mathf.Clamp(scale, 0, 7) << 3) |
            (Mathf.Clamp(color, 0, 15) << 6) | (physics ? 1 << 10 : 0);

        private static void Unpack(int packed, out int type, out int scale, out int color,
            out bool physics)
        {
            type = Mathf.Clamp(packed & 7, 0, ShapeNames.Length - 1);
            scale = Mathf.Clamp((packed >> 3) & 7, 0, ScaleValues.Length - 1);
            color = Mathf.Clamp((packed >> 6) & 15, 0, Colors.Length - 1);
            physics = (packed & (1 << 10)) != 0;
        }

        private static int Wrap(int value, int count) => (value % count + count) % count;

        private static readonly string[] ShapeNames = { "Cube", "Sphere", "Capsule", "Cylinder", "Plane", "Triangle Prism" };
        private static readonly float[] ScaleValues = { 0.5f, 1f, 2f, 3f };
        private static readonly string[] ColorNames = { "White", "Red", "Green", "Blue", "Yellow", "Orange", "Purple", "Black" };
        private static readonly Color[] Colors =
        {
            Color.white, Color.red, Color.green, Color.blue, Color.yellow,
            new Color(1f, 0.45f, 0.08f), new Color(0.55f, 0.2f, 0.85f), Color.black
        };
    }
}
