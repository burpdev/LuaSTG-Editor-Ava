using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LuaSTGEditorAva.Services
{
    [JsonSerializable(typeof(LinuxSettings))]
    internal partial class LinuxSettingsJsonContext : JsonSerializerContext
    {
    }

    public sealed class LinuxSettings : LuaSTGEditorSharp.IAppSettings,
        LuaSTGEditorSharp.IAppDebugSettings, INotifyPropertyChanged
    {
        private static readonly string ConfigDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "lstg-ava");
        private static readonly string ConfigPath = Path.Combine(ConfigDir, "settings.json");

        private static readonly string LegacyConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LuaSTGEditorSharp", "settings.json");

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            TypeInfoResolver = LinuxSettingsJsonContext.Default
        };

        public static LinuxSettings Instance { get; } = Load();

        public static string ConfigFilePath => ConfigPath;

        public static bool LoadedOk { get; private set; }

        public static string LastLoadError { get; private set; }

        public bool UseFolderPacking { get; set; } = true;
        public bool IgnoreTHLibWarn { get; set; }
        public string ZipExecutablePath { get; set; } = "7zz";
        public string LuaSTGExecutablePath { get; set; } = "";
        public string GameRunner { get; set; } = "auto";
        public string CustomRunnerCommand { get; set; } = "%command%";
        public string EditorOutputName { get; set; } = "_editor_output.lua";
        public bool IsEXEPathSet =>
            !(BatchPacking && string.IsNullOrEmpty(ZipExecutablePath)) && !string.IsNullOrEmpty(LuaSTGExecutablePath);
        public string TempPath { get; set; } = Path.GetTempPath();
        public string SLDir { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public bool SaveResMeta { get; set; }
        public bool PackProj { get; set; }
        public bool BatchPacking { get; set; }
        public bool SpaceIndentation { get; set; } = true;
        public int IndentationSpaceLength { get; set; } = 4;

        public bool DebugWindowed { get; set; } = true;
        public int DebugResolutionX { get; set; } = 640;
        public int DebugResolutionY { get; set; } = 480;
        public bool DebugCheat { get; set; }
        public bool SubLogWindow { get; set; }
        public bool DebugUpdateLib { get; set; }
        public bool DynamicDebugReporting { get; set; }

        public string AuthorName { get; set; } = "LuaSTG User";
        public bool AutoMoveToNew { get; set; } = true;
        public bool UseAutoSave { get; set; }
        public int AutoSaveTimer { get; set; } = 5;
        public bool DebugSaveProj { get; set; }
        public bool UseDiscordRpc { get; set; }
        public bool UseRemoteTemplates { get; set; } = true;
        public string CurrentTheme { get; set; } = "Gray";
        public bool CompactView { get; set; }
        public List<string> RecentlyOpened { get; set; } = new List<string>();

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaiseChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(ConfigDir);
                if (!LoadedOk && File.Exists(ConfigPath))
                {
                    try { File.Copy(ConfigPath, ConfigPath + ".bak", true); }
                    catch
                    {
                    }
                }
                string json = JsonSerializer.Serialize(this, JsonOptions);
                File.WriteAllText(ConfigPath, json);
                LoadedOk = true;
            }
            catch
            {
            }
        }

        private static LinuxSettings Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    LinuxSettings loaded = JsonSerializer.Deserialize<LinuxSettings>(json, JsonOptions);
                    if (loaded != null)
                    {
                        LoadedOk = true;
                        return loaded;
                    }
                    LastLoadError = $"Could not parse {ConfigPath}.";
                }
                else if (File.Exists(LegacyConfigPath))
                {
                    // One-time migration from the old directory.
                    string json = File.ReadAllText(LegacyConfigPath);
                    LinuxSettings migrated = JsonSerializer.Deserialize<LinuxSettings>(json, JsonOptions);
                    if (migrated != null)
                    {
                        migrated.Save();
                        LoadedOk = true;
                        return migrated;
                    }
                    LastLoadError = $"Could not parse {LegacyConfigPath}.";
                }
            }
            catch (Exception ex)
            {
                LastLoadError = $"Could not load {ConfigPath}: {ex.Message}";
            }
            return new LinuxSettings();
        }
    }
}
