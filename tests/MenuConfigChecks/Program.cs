using NugzzMenu.Services;
using System.Text.Json;

var service = MenuConfigService.Instance;
int passed = 0;
string root = Path.GetFullPath(Path.Combine("trialbuild", "config-checks", Guid.NewGuid().ToString("N")));
Directory.CreateDirectory(root);
void Check(string name, Action action) { action(); passed++; Console.WriteLine("PASS: " + name); }
void Require(bool value, string text) { if (!value) throw new InvalidOperationException(text); }

Check("Uninitialized config never writes or deletes a file", () => Require(!service.Save(new MenuConfig()), "Uninitialized save accepted"));
service.Initialize(Path.Combine(root, "NugzzMenu.dll"));
Check("Config path stays beside the mod", () => Require(service.ConfigPath == Path.Combine(root, "nuggzmenu-config.json"), "Wrong destination"));
Check("Native colors and inactive cheats are the cold-start defaults", () =>
{
    var defaults = new MenuConfig();
    Require(defaults.ClothingColor == "Native" && !defaults.NeverWanted && !defaults.GodMode && !defaults.FlyEnabled,
        "Unsafe or recolored default");
});
Check("Preferences round trip and save a previous-version backup", () =>
{
    Require(service.Save(new MenuConfig { MenuKeybind = "F7", ClothingColor = "Native", NeverWanted = true }), service.Status);
    string first = File.ReadAllText(service.ConfigPath);
    Require(service.TryLoad(out var loaded) && loaded.MenuKeybind == "F7" && loaded.NeverWanted, service.Status);
    loaded.ClothingColor = "Crimson";
    Require(service.Save(loaded), service.Status);
    Require(File.ReadAllText(service.ConfigPath + ".bak") == first, "Previous defaults were not preserved");
    Require(!File.Exists(service.ConfigPath + ".tmp"), "Temporary file leaked");
});
Check("Malformed JSON is retained without applying partial settings", () =>
{
    const string malformed = "{\"godMode\":true,\"menuKeybind\":\"F9\",BAD}";
    File.WriteAllText(service.ConfigPath, malformed);
    Require(!service.TryLoad(out var config) && config == null, "Partial config escaped");
    Require(File.ReadAllText(service.ConfigPath) == malformed, "Bad user file was overwritten");
});
Check("Comments, trailing commas, and unknown fields survive normal editing", () =>
{
    File.WriteAllText(service.ConfigPath, "{ // user preferences\n\"menuKeybind\":\"F10\",\"futureOption\":{\"x\":7},}");
    Require(service.TryLoad(out var config) && config.MenuKeybind == "F10", service.Status);
    Require(service.Save(new MenuConfig()), service.Status);
    using var json = JsonDocument.Parse(File.ReadAllText(service.ConfigPath));
    Require(json.RootElement.GetProperty("futureOption").GetProperty("x").GetInt32() == 7, "Unknown option lost");
});
Check("Numeric bounds match runtime tuning limits", () =>
{
    var config = new MenuConfig { SpawnQuantity = 2000, PlayerScale = -5, ReflectionInterval = 100 };
    config.Validate();
    Require(config.SpawnQuantity == 100 && config.PlayerScale == 0.25f && config.ReflectionInterval == 2f, "Invalid limits");
});
Check("Invalid key, tint, version, and nonfinite numbers are rejected", () =>
{
    var invalid = new[] { new MenuConfig { MenuKeybind = "Tab" }, new MenuConfig { ClothingColor = "FakeColor" },
        new MenuConfig { Version = 99 }, new MenuConfig { FlySpeed = float.NaN } };
    foreach (var config in invalid)
    {
        bool rejected = false;
        try { config.Validate(); } catch (InvalidOperationException) { rejected = true; }
        Require(rejected, "Invalid config accepted");
    }
});
Check("Oversized config cannot replace current settings", () =>
{
    File.WriteAllText(service.ConfigPath, new string(' ', 65537));
    Require(!service.TryLoad(out var config) && config == null, "Oversized file accepted");
});
Console.WriteLine($"{passed} config checks passed. These use isolated files and do not touch game saves.");
