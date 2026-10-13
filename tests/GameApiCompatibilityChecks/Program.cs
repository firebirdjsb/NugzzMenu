using System.Security.Cryptography;
using System.Text.Json;
using Mono.Cecil;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: GameApiCompatibilityChecks <NugzzMenu.dll> <dummy DLL directory> [--require-special-customers] [--s1api <DLL>] [--report <JSON>]");
    return 1;
}

string menuPath = Path.GetFullPath(args[0]);
string dummyRoot = Path.GetFullPath(args[1]);
bool requireUpdate = args.Contains("--require-special-customers");
var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(dummyRoot);
resolver.AddSearchDirectory(Path.GetDirectoryName(menuPath));
var reader = new ReaderParameters { AssemblyResolver = resolver };
var modules = new Dictionary<string, ModuleDefinition>(StringComparer.Ordinal);
foreach (string assembly in new[] { "Assembly-CSharp", "ScheduleOne.Core" })
    modules[assembly] = ModuleDefinition.ReadModule(Path.Combine(dummyRoot, assembly + ".dll"), reader);

using var menu = ModuleDefinition.ReadModule(menuPath, reader);
var errors = new SortedSet<string>(StringComparer.Ordinal);
var requiredTypes = new[]
{
    "ScheduleOne.Core.Avatar.AvatarObject",
    "ScheduleOne.Core.Avatar.NakedAppearance",
    "ScheduleOne.Core.Avatar.Outfit"
};
var allTypes = modules.Values.SelectMany(module => Flatten(module.Types)).ToList();
var absentUpdateTypes = requiredTypes.Where(name => !allTypes.Any(type => type.FullName == name)).ToArray();
bool hasGroups = allTypes.Any(type => type.FullName.Contains("CustomerGroup", StringComparison.OrdinalIgnoreCase));
bool hasUpdate = absentUpdateTypes.Length == 0 && hasGroups;
if (requireUpdate && !hasUpdate)
    errors.Add("Wrong game dump: the special customers avatar types and customer-group types must be present.");

int checkedTypes = 0;
foreach (TypeReference reference in menu.GetTypeReferences())
{
    if (!IsGameReference(reference)) continue;
    checkedTypes++;
    if (FindType(reference) == null) errors.Add("Missing type: " + NativeName(reference));
}

int checkedMembers = 0;
foreach (MemberReference reference in menu.GetMemberReferences())
{
    if (!IsGameReference(reference.DeclaringType)) continue;
    checkedMembers++;
    var native = FindType(reference.DeclaringType);
    if (native == null) continue; // Already reported by the type check.
    if (reference is MethodReference method && IsInteropGenerated(method)) continue;
    if (!HasMember(native, reference)) errors.Add("Missing or changed member: " + reference.FullName);
}

int checkedPatches = 0;
foreach (TypeDefinition patch in Flatten(menu.Types))
{
    foreach (CustomAttribute attribute in patch.CustomAttributes.Where(attr => attr.AttributeType.FullName == "HarmonyLib.HarmonyPatch"))
    {
        TypeReference targetType = attribute.ConstructorArguments.Select(arg => arg.Value).OfType<TypeReference>().FirstOrDefault();
        string targetName = attribute.ConstructorArguments.Select(arg => arg.Value).OfType<string>().FirstOrDefault();
        if (targetType == null || targetName == null || !IsGameReference(targetType)) continue;
        checkedPatches++;
        TypeDefinition target = FindType(targetType);
        if (target == null || !Hierarchy(target).Any(type => type.Methods.Any(method => MemberName(method.Name) == MemberName(targetName))))
            errors.Add("Missing Harmony target: " + NativeName(targetType) + "." + targetName);
    }
}

string s1Version = "not checked";
int checkedDynamicPatches = 0;
foreach (TypeDefinition patch in Flatten(menu.Types))
{
    MethodDefinition targets = patch.Methods.FirstOrDefault(method => method.Name == "TargetMethods" && method.HasBody);
    if (targets == null || !targets.Body.Instructions.Any(instruction => instruction.Operand is MethodReference call &&
        call.DeclaringType.Name == "NeverWantedService" && call.Name == "Targets")) continue;
    TypeReference targetType = targets.Body.Instructions.Select(instruction => instruction.Operand).OfType<TypeReference>()
        .FirstOrDefault(IsGameReference);
    TypeDefinition native = targetType == null ? null : FindType(targetType);
    foreach (string targetName in targets.Body.Instructions.Where(instruction => instruction.OpCode.Code == Mono.Cecil.Cil.Code.Ldstr)
        .Select(instruction => (string)instruction.Operand))
    {
        checkedDynamicPatches++;
        MethodDefinition method = native == null ? null : Hierarchy(native).SelectMany(type => type.Methods)
            .FirstOrDefault(method => MemberName(method.Name) == MemberName(targetName));
        if (method == null) errors.Add("Missing dynamic Harmony target: " + targetType?.FullName + "." + targetName);
        else
        {
            var prefix = patch.Methods.FirstOrDefault(method => method.Name == "Prefix");
            foreach (var parameter in prefix.Parameters.Where(parameter => !parameter.Name.StartsWith("__")))
                if (!method.Parameters.Any(nativeArg => nativeArg.Name == parameter.Name &&
                    NativeName(nativeArg.ParameterType) == NativeName(parameter.ParameterType)))
                    errors.Add("Harmony prefix parameter mismatch: " + patch.Name + "/" + targetName + "/" + parameter.Name);
        }
    }
}
string s1Path = Option("--s1api");
if (s1Path != null)
{
    using var api = ModuleDefinition.ReadModule(Path.GetFullPath(s1Path), reader);
    var info = api.Assembly.CustomAttributes.FirstOrDefault(attr => attr.AttributeType.FullName == "MelonLoader.MelonInfoAttribute");
    s1Version = info?.ConstructorArguments.ElementAtOrDefault(2).Value as string ?? "unknown";
    foreach (var contract in new[]
    {
        ("S1API.Items.ItemManager", "GetAllItemDefinitions"),
        ("S1API.Entities.NPC", "get_Transform"),
        ("S1API.Entities.NPC", "get_ID"),
        ("S1API.Entities.NPC", "get_FullName"),
        ("S1API.GameTime.TimeManager", "SetTime"),
        ("S1API.Weather.WeatherManager", "get_KnownSequenceIds")
    })
    {
        var type = api.GetType(contract.Item1);
        if (type == null || !type.Methods.Any(method => method.IsPublic && method.Name == contract.Item2))
            errors.Add("Missing public S1API contract: " + contract.Item1 + "." + contract.Item2);
    }
    var npc = api.GetType("S1API.Entities.NPC");
    if (npc == null || !npc.Fields.Any(field => field.IsPublic && field.IsStatic && field.Name == "All"))
        errors.Add("Missing public S1API NPC.All registry");
    var lifecycle = api.GetType("S1API.Lifecycle.GameLifecycle");
    foreach (string name in new[] { "OnLoadComplete", "OnPreSceneChange" })
        if (lifecycle == null || !lifecycle.Events.Any(evt => evt.Name == name))
            errors.Add("Missing S1API lifecycle event: " + name);
}

var report = new
{
    menuPath,
    dummyRoot,
    gameAssemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(dummyRoot, "Assembly-CSharp.dll")))),
    hasSpecialCustomersUpdate = hasUpdate,
    absentUpdateTypes,
    hasCustomerGroupTypes = hasGroups,
    s1ApiVersion = s1Version,
    checkedTypes,
    checkedMembers,
    checkedPatches,
    checkedDynamicPatches,
    errors = errors.ToArray()
};
string output = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
string reportPath = Option("--report");
if (reportPath != null) File.WriteAllText(Path.GetFullPath(reportPath), output);
Console.WriteLine(output);
Console.WriteLine("This checks compiled API bindings and patch targets. It does not execute native code, inspect item/NPC assets, or establish multiplayer behavior.");
foreach (var module in modules.Values) module.Dispose();
return errors.Count == 0 ? 0 : 2;

string Option(string name)
{
    int index = Array.IndexOf(args, name);
    if (index < 0) return null;
    if (index + 1 >= args.Length) throw new ArgumentException("Missing value for " + name);
    return args[index + 1];
}

static IEnumerable<TypeDefinition> Flatten(IEnumerable<TypeDefinition> types)
{
    foreach (var type in types)
    {
        yield return type;
        foreach (var nested in Flatten(type.NestedTypes)) yield return nested;
    }
}

static string Scope(TypeReference type) => type.GetElementType().Scope?.Name?.Replace("Il2CppScheduleOne.Core", "ScheduleOne.Core");
bool IsGameReference(TypeReference type) => type != null && modules.ContainsKey(Scope(type) ?? "");

TypeDefinition FindType(TypeReference type)
{
    if (!modules.TryGetValue(Scope(type) ?? "", out var module)) return null;
    string name = NativeName(type.GetElementType());
    return module.GetType(name);
}

static string NativeName(TypeReference type)
{
    if (type is ByReferenceType byRef) return NativeName(byRef.ElementType) + "&";
    if (type is ArrayType array) return NativeName(array.ElementType) + "[]";
    if (type is GenericInstanceType generic)
    {
        if (generic.ElementType.Namespace == "Il2CppInterop.Runtime.InteropTypes.Arrays")
            return NativeName(generic.GenericArguments[0]) + "[]";
        return NativeName(generic.ElementType) + "<" + string.Join(",", generic.GenericArguments.Select(NativeName)) + ">";
    }
    string name = type.FullName;
    return name.StartsWith("Il2Cpp", StringComparison.Ordinal) ? name.Substring(6) : name;
}

static bool CompatibleType(TypeReference expected, TypeReference actual)
{
    // Generic slots are instantiated by the interop wrapper. Concrete types are
    // checked separately in the type-reference pass, without executing IL2CPP.
    if (expected is GenericParameter || actual is GenericParameter) return true;
    return NativeName(expected) == NativeName(actual);
}

static string MemberName(string name) => new string(name.Select(character =>
    char.IsLetterOrDigit(character) || character == '_' ? character : '_').ToArray());

IEnumerable<TypeDefinition> Hierarchy(TypeDefinition type)
{
    var seen = new HashSet<string>();
    while (type != null && seen.Add(type.FullName))
    {
        yield return type;
        try { type = type.BaseType?.Resolve(); }
        catch { yield break; }
    }
}

bool HasMember(TypeDefinition native, MemberReference reference)
{
    foreach (var type in Hierarchy(native))
    {
        if (reference is FieldReference field)
        {
            if (type.Fields.Any(candidate => MemberName(candidate.Name) == MemberName(field.Name) && CompatibleType(candidate.FieldType, field.FieldType))) return true;
        }
        if (reference is not MethodReference method) continue;
        if (type.Methods.Any(candidate => MemberName(candidate.Name) == MemberName(method.Name)
            && candidate.Parameters.Count == method.Parameters.Count
            && candidate.GenericParameters.Count == method.GenericParameters.Count
            && CompatibleType(candidate.ReturnType, method.ReturnType)
            && candidate.Parameters.Zip(method.Parameters).All(pair => CompatibleType(pair.First.ParameterType, pair.Second.ParameterType)))) return true;

        // IL2CPP exposes native fields as managed getter/setter properties.
        bool getter = method.Name.StartsWith("get_", StringComparison.Ordinal) && method.Parameters.Count == 0;
        bool setter = method.Name.StartsWith("set_", StringComparison.Ordinal) && method.Parameters.Count == 1;
        if ((getter || setter) && type.Fields.Any(candidate => MemberName(candidate.Name) == MemberName(method.Name.Substring(4))
            && CompatibleType(candidate.FieldType, getter ? method.ReturnType : method.Parameters[0].ParameterType))) return true;
    }
    return false;
}

static bool IsInteropGenerated(MethodReference method) =>
    method.Name == ".ctor" && method.Parameters.Count == 1 && method.Parameters[0].ParameterType.FullName == "System.IntPtr";
