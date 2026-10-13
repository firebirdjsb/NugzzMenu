using System.Reflection;
using System.Runtime.Loader;

if (args.Length != 1)
    throw new ArgumentException("Pass the repository root containing the Release build and net6.0 references.");

string root = Path.GetFullPath(args[0]);
AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    string reference = Path.Combine(root, "net6.0", name.Name + ".dll");
    return File.Exists(reference)
        ? AssemblyLoadContext.Default.LoadFromAssemblyPath(reference)
        : null;
};

Assembly menu = AssemblyLoadContext.Default.LoadFromAssemblyPath(
    Path.Combine(root, "bin", "Release", "net6.0", "NugzzMenu.dll"));
Type patch = menu.GetType("NugzzMenu.Services.AvatarApplyAccessorySettingsSafetyPatch", true);
MethodInfo classify = patch.GetMethod("ShouldSuppressAccessoryException",
    BindingFlags.Static | BindingFlags.NonPublic);
if (classify == null)
    throw new MissingMethodException(patch.FullName, "ShouldSuppressAccessoryException");

var cases = new (string Name, Exception Error, bool Crown, bool Expected)[]
{
    ("Successful crown application", null, true, false),
    ("Managed crown null reference", new NullReferenceException(), true, true),
    ("Native wrapped crown null reference",
        new Exception("Il2CppException: System.NullReferenceException: Object reference not set"), true, true),
    ("Other clothing null reference", new NullReferenceException(), false, false),
    ("Other clothing native null reference",
        new Exception("Il2CppException: System.NullReferenceException"), false, false),
    ("Known missing prefab", new ArgumentException("The Object you want to instantiate is null."), false, true),
    ("Crown missing prefab", new ArgumentException("The Object you want to instantiate is null."), true, true),
    ("Unrelated crown error", new InvalidOperationException("Invalid outfit operation"), true, false),
    ("Unrelated clothing error", new InvalidOperationException("Invalid outfit operation"), false, false)
};

foreach (var item in cases)
{
    bool actual = (bool)classify.Invoke(null, new object[] { item.Error, item.Crown });
    if (actual != item.Expected)
        throw new InvalidOperationException(item.Name + ": expected " + item.Expected + ", got " + actual);
    Console.WriteLine("PASS: " + item.Name);
}

Console.WriteLine(cases.Length + " accessory failure checks passed against the built DLL.");
