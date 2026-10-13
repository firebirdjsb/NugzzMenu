using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace NugzzMenu.Services
{
    public sealed class MenuConfigService
    {
        public const string FileName = "nuggzmenu-config.json";
        public static MenuConfigService Instance { get; } = new MenuConfigService();
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        private MenuConfig _loaded;
        public string ConfigPath { get; private set; }
        public string Status { get; private set; } = "Config has not been loaded.";
        private MenuConfigService() { }

        public void Initialize(string assemblyPath)
        {
            string folder = Path.GetDirectoryName(Path.GetFullPath(assemblyPath));
            ConfigPath = Path.Combine(folder, FileName);
            _loaded = null;
        }

        public bool TryLoad(out MenuConfig config)
        {
            config = null;
            try
            {
                if (!File.Exists(ConfigPath)) { Status = "No config file yet. Save defaults to create it."; return false; }
                if (new FileInfo(ConfigPath).Length > 65536) throw new InvalidOperationException("Config exceeds 64 KB.");
                var parsed = JsonSerializer.Deserialize<MenuConfig>(File.ReadAllText(ConfigPath), Options);
                if (parsed == null) throw new InvalidOperationException("Config is empty.");
                parsed.Validate();
                config = _loaded = parsed;
                Status = "Loaded defaults from " + FileName;
                return true;
            }
            catch (Exception ex)
            {
                Status = "Config load failed; current settings retained: " + ex.Message;
                return false;
            }
        }

        public bool Save(MenuConfig config)
        {
            if (string.IsNullOrEmpty(ConfigPath)) { Status = "Config folder is unavailable."; return false; }
            string temporary = ConfigPath + ".tmp";
            try
            {
                config.Validate();
                config.Extra = _loaded?.Extra;
                string json = JsonSerializer.Serialize(config, Options);
                if (Encoding.UTF8.GetByteCount(json) > 65536) throw new InvalidOperationException("Config exceeds 64 KB.");
                File.WriteAllText(temporary, json + Environment.NewLine, new UTF8Encoding(false));
                if (File.Exists(ConfigPath)) File.Replace(temporary, ConfigPath, ConfigPath + ".bak");
                else File.Move(temporary, ConfigPath);
                _loaded = config;
                Status = "Saved current settings as startup defaults.";
                return true;
            }
            catch (Exception ex) { Status = "Config save failed: " + ex.Message; return false; }
            finally { try { if (File.Exists(temporary)) File.Delete(temporary); } catch { } }
        }
    }
}
