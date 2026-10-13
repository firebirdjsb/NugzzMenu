using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NugzzMenu.Services
{
    // Preferences only: no save slots, balances, relationships, spawns, or queued
    // world actions are serialized or replayed when loading this file.
    public sealed class MenuConfig
    {
        public int Version { get; set; } = 1;
        public string MenuKeybind { get; set; } = "F8";
        public string StartupTab { get; set; } = "Cheats";
        public bool VerboseDebugLogging { get; set; }
        public bool KeybindOverlay { get; set; } = true;
        public bool DoubleSpaceFlyHotkey { get; set; } = true;
        public bool UseGameStackLogic { get; set; } = true;
        public int SpawnQuantity { get; set; } = 1;
        public int ItemQuality { get; set; } = 2;
        public string ClothingColor { get; set; } = "Native";
        public bool GodMode { get; set; }
        public bool InfiniteStamina { get; set; }
        public bool InfiniteAmmo { get; set; }
        public bool NeverWanted { get; set; }
        public bool BottomlessTrashGrabber { get; set; }
        public bool SpeedBoost { get; set; }
        public float SpeedMultiplier { get; set; } = 2f;
        public float PlayerScale { get; set; } = 1f;
        public float JumpMultiplier { get; set; } = 1f;
        public float GravityMultiplier { get; set; } = 1f;
        public bool FlyEnabled { get; set; }
        public float FlySpeed { get; set; } = 20f;
        public bool ThirdPerson { get; set; }
        public float CameraDistance { get; set; } = 1.9f;
        public float CameraHeight { get; set; } = 0.8f;
        public float CameraShoulder { get; set; } = 0.2f;
        public float LightRangeScale { get; set; } = 1f;
        public float ReflectionInterval { get; set; }
        [JsonExtensionData] public Dictionary<string, JsonElement> Extra { get; set; }

        public void Validate()
        {
            if (Version != 1) throw new InvalidOperationException("Unsupported config version: " + Version);
            string[] keys = { "F6", "F7", "F8", "F9", "F10", "Insert", "Home", "Delete" };
            if (Array.FindIndex(keys, key => string.Equals(key, MenuKeybind, StringComparison.OrdinalIgnoreCase)) < 0)
                throw new InvalidOperationException("Menu key must be F6-F10, Insert, Home, or Delete.");
            string[] tabs = { "Cheats", "Money", "Time", "Vehicles", "Properties", "Items", "Lobby", "Performance", "Relationships", "Quests", "Settings" };
            if (Array.FindIndex(tabs, tab => string.Equals(tab, StartupTab, StringComparison.OrdinalIgnoreCase)) < 0)
                throw new InvalidOperationException("Unknown startup tab.");
            string[] colors = { "Native", "White", "LightGrey", "DarkGrey", "Charcoal", "Black", "LightRed", "Red", "Crimson", "Orange", "Tan", "Brown", "Coral", "Beige", "Yellow", "Lime", "LightGreen", "DarkGreen", "Cyan", "SkyBlue", "Blue", "DeepBlue", "Navy", "DeepPurple", "Purple", "Magenta", "BrightPink", "HotPink" };
            if (Array.FindIndex(colors, color => string.Equals(color, ClothingColor, StringComparison.OrdinalIgnoreCase)) < 0)
                throw new InvalidOperationException("Unknown clothing tint. Use Native for original colors.");
            SpawnQuantity = Math.Clamp(SpawnQuantity, 1, 100);
            ItemQuality = Math.Clamp(ItemQuality, 0, 4);
            SpeedMultiplier = Bound(SpeedMultiplier, 1f, 10f);
            PlayerScale = Bound(PlayerScale, 0.25f, 4f);
            JumpMultiplier = Bound(JumpMultiplier, 0.1f, 6f);
            GravityMultiplier = Bound(GravityMultiplier, 0f, 5f);
            FlySpeed = Bound(FlySpeed, 1f, 500f);
            CameraDistance = Bound(CameraDistance, 1.5f, 6f);
            CameraHeight = Bound(CameraHeight, 0.8f, 2.4f);
            CameraShoulder = Bound(CameraShoulder, -1f, 1f);
            LightRangeScale = Bound(LightRangeScale, 0.35f, 1f);
            ReflectionInterval = Bound(ReflectionInterval, 0f, 2f);
        }

        private static float Bound(float value, float min, float max)
        {
            if (!float.IsFinite(value)) throw new InvalidOperationException("Config numbers must be finite.");
            return Math.Clamp(value, min, max);
        }
    }
}
