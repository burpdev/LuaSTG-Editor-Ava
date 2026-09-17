using System;
using System.IO;

namespace LstgHeadless
{
    public sealed class HeadlessAppSettings : LuaSTGEditorSharp.IAppSettings, LuaSTGEditorSharp.IAppDebugSettings
    {
        public bool UseFolderPacking => true;
        public bool IgnoreTHLibWarn => true;
        public string ZipExecutablePath => "7zz";
        public string LuaSTGExecutablePath => Path.Combine(Path.GetTempPath(), "luastg", "luastg.exe");
        public string EditorOutputName => "_editor_output.lua";
        public bool IsEXEPathSet => true;
        public string TempPath => Path.GetTempPath();
        public string SLDir { get; set; } = Directory.GetCurrentDirectory();
        public bool SaveResMeta => false;
        public bool PackProj => false;
        public bool BatchPacking => false;
        public bool SpaceIndentation => true;
        public int IndentationSpaceLength => 4;

        public bool DebugWindowed => true;
        public int DebugResolutionX => 640;
        public int DebugResolutionY => 480;
        public bool DebugCheat => false;
        public bool SubLogWindow => false;
        public bool DebugUpdateLib => false;
        public bool DynamicDebugReporting => false;
    }
}
