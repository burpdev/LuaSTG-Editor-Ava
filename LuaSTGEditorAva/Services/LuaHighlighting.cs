using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Highlighting.Xshd;

namespace LuaSTGEditorAva.Services
{
    public static class LuaHighlighting
    {
        private static readonly object loadLock = new object();
        private static bool attempted;
        private static IHighlightingDefinition definition;

        public static IHighlightingDefinition Definition
        {
            get
            {
                lock (loadLock)
                {
                    if (!attempted)
                    {
                        attempted = true;
                        try
                        {
                            definition = LoadCore();
                        }
                        catch
                        {
                            definition = null;
                        }
                    }
                    return definition;
                }
            }
        }

        private static IHighlightingDefinition LoadCore()
        {
            Assembly core = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a =>
                string.Equals(a.GetName().Name, "LuaSTGEditorSharp.Core", StringComparison.OrdinalIgnoreCase));
            if (core == null) return null;
            string resName = core.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("LuaSyntax.xml", StringComparison.OrdinalIgnoreCase));
            if (resName == null) return null;
            using Stream stream = core.GetManifestResourceStream(resName);
            if (stream == null) return null;
            using var reader = new XmlTextReader(stream);
            IHighlightingDefinition def = HighlightingLoader.Load(reader, HighlightingManager.Instance);
            HighlightingManager.Instance.RegisterHighlighting("Lua", new[] { ".lua" }, def);
            return def;
        }
    }
}
