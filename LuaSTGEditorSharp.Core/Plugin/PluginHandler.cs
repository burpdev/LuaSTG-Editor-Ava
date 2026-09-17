using System;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using LuaSTGEditorSharp.Util;

namespace LuaSTGEditorSharp.Plugin
{
    public static class PluginHandler
    {
        private static ILogger Logger = EditorLogging.ForContext("PluginHandler");

        public static AbstractPluginEntry DefaultPlugin { get; set; }
        public static AbstractPluginEntry Plugin { get; private set; } = null;

        public static bool LoadPlugin(string PluginPath)
        {
            bool isSuccess;
            Assembly pluginAssembly = null;
            try
            {
                // Normalize Windows-style separators so persisted "lib\X.dll" values also resolve on Linux.
                string normalized = PluginPath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
                string path = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, normalized));
                File.UnblockDll(path);
                pluginAssembly = Assembly.LoadFile(path);
                Plugin = (AbstractPluginEntry)pluginAssembly.CreateInstance("LuaSTGEditorSharp.PluginEntry");
                
            }
            catch (Exception ex)
            {
                Logger.Fatal($"Plugin initialization failed. Reason:\n{ex}");
            }
            if (Plugin == null)
            {
                Plugin = DefaultPlugin;
                if (Plugin == null)
                {
                    Logger.Fatal("Plugin initialization failed and no DefaultPlugin was set.");
                    return false;
                }
                Plugin.NodeTypeCache.Initialize(Assembly.GetExecutingAssembly());
                isSuccess = false;
                Logger.Information("Plugin initialization failed.");
            }
            else
            {
                Plugin.NodeTypeCache.Initialize(AppDomain.CurrentDomain.GetAssemblies());
                isSuccess = true;
                Logger.Information("Plugin initialization successful.");
            }
            return isSuccess;
        }

        /// <summary>
        /// Register an already-constructed plugin entry (headless hosts, tests,
        /// single-file publishes where Assembly.Location is empty and LoadFile
        /// cannot be used). Same cache initialization as <see cref="LoadPlugin"/>.
        /// </summary>
        public static bool RegisterPlugin(AbstractPluginEntry entry)
        {
            try
            {
                if (entry == null) throw new ArgumentNullException(nameof(entry));
                Plugin = entry;
                Plugin.NodeTypeCache.Initialize(AppDomain.CurrentDomain.GetAssemblies());
                Logger.Information("Plugin registration successful.");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Fatal($"Plugin registration failed. Reason:\n{ex}");
                return false;
            }
        }
    }
}
