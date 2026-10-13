using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace NugzzMenu.Services
{
    // Use public S1API APIs through reflection so an unavailable API feature
    // cannot prevent MelonLoader from loading the rest of the menu.
    public sealed class S1ApiService
    {
        public const string ReferenceVersion = "3.2.1";
        public static S1ApiService Instance { get; } = new S1ApiService();

        private readonly HashSet<string> _reportedFailures = new HashSet<string>();
        private readonly Dictionary<string, Type> _types = new Dictionary<string, Type>();

        public struct NpcInfo
        {
            public GameObject GameObject;
            public string Id;
            public string Name;
        }

        private S1ApiService() { }

        public string GetLoadedVersion()
        {
            try
            {
                Type type = FindLoadedType("S1API.S1API");
                if (type == null) return "unavailable";
                foreach (CustomAttributeData attribute in type.Assembly.GetCustomAttributesData())
                {
                    if (attribute.AttributeType.FullName == "MelonLoader.MelonInfoAttribute" &&
                        attribute.ConstructorArguments.Count >= 3)
                    {
                        return attribute.ConstructorArguments[2].Value as string ?? "unknown";
                    }
                }
                return type.Assembly.GetName().Version?.ToString() ?? "unknown";
            }
            catch { return "unknown"; }
        }

        public List<string> GetRegisteredItemIds()
        {
            var ids = new List<string>();
            try
            {
                Type type = FindLoadedType("S1API.Items.ItemManager");
                MethodInfo method = type?.GetMethod("GetAllItemDefinitions", BindingFlags.Static | BindingFlags.Public,
                    null, Type.EmptyTypes, null);
                if (method?.Invoke(null, null) is IEnumerable definitions)
                {
                    foreach (object definition in definitions)
                    {
                        try
                        {
                            string id = ReadPublicMember(definition, "ID") as string;
                            if (!string.IsNullOrWhiteSpace(id)) ids.Add(id);
                        }
                        catch (Exception ex) { ReportOnce("item definition", ex); }
                    }
                }
            }
            catch (Exception ex) { ReportOnce("item catalog", ex); }
            return ids;
        }

        public List<NpcInfo> GetNpcs()
        {
            var result = new List<NpcInfo>();
            try
            {
                Type type = FindLoadedType("S1API.Entities.NPC");
                object all = type?.GetField("All", BindingFlags.Static | BindingFlags.Public)?.GetValue(null)
                    ?? type?.GetProperty("All", BindingFlags.Static | BindingFlags.Public)?.GetValue(null);
                if (all is IEnumerable npcs)
                {
                    foreach (object npc in npcs)
                    {
                        try
                        {
                            var transform = ReadPublicMember(npc, "Transform") as Transform;
                            if (transform == null) continue;
                            result.Add(new NpcInfo
                            {
                                GameObject = transform.gameObject,
                                Id = ReadPublicMember(npc, "ID") as string,
                                Name = ReadPublicMember(npc, "FullName") as string
                            });
                        }
                        catch (Exception ex) { ReportOnce("NPC identity", ex); }
                    }
                }
            }
            catch (Exception ex) { ReportOnce("NPC catalog", ex); }
            return result;
        }

        public List<string> GetWeatherSequenceIds()
        {
            var result = new List<string>();
            try
            {
                Type type = FindLoadedType("S1API.Weather.WeatherManager");
                if (type?.GetProperty("KnownSequenceIds", BindingFlags.Static | BindingFlags.Public)
                    ?.GetValue(null) is IEnumerable ids)
                {
                    foreach (object id in ids)
                        if (id is string value && !string.IsNullOrWhiteSpace(value)) result.Add(value);
                }
            }
            catch (Exception ex) { ReportOnce("weather catalog", ex); }
            return result;
        }

        public bool TrySetTime(int clockTime)
        {
            Type type = FindLoadedType("S1API.GameTime.TimeManager");
            MethodInfo method = type?.GetMethod("SetTime", BindingFlags.Static | BindingFlags.Public,
                null, new[] { typeof(int) }, null);
            if (method == null) return false;
            // Propagate a failed operation to the caller. Repeating it through a
            // native fallback could apply the same time change twice.
            method.Invoke(null, new object[] { clockTime });
            return true;
        }

        internal Type FindLoadedType(string fullName)
        {
            if (_types.TryGetValue(fullName, out Type cached)) return cached;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type type = assembly.GetType(fullName, false);
                    if (type == null) continue;
                    _types[fullName] = type;
                    return type;
                }
                catch { }
            }
            // Do not cache misses: S1API can be loaded after NugzzMenu.
            return null;
        }

        internal static object ReadPublicMember(object instance, string name)
        {
            if (instance == null) return null;
            Type type = instance.GetType();
            PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            return property != null ? property.GetValue(instance)
                : type.GetField(name, BindingFlags.Instance | BindingFlags.Public)?.GetValue(instance);
        }

        private void ReportOnce(string operation, Exception exception)
        {
            if (_reportedFailures.Add(operation))
                DebugLogService.Instance.VerboseException("S1API " + operation + " unavailable; using native discovery", exception);
        }
    }
}
