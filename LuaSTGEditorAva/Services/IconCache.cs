using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia.Media.Imaging;
using LuaSTGEditorSharp.Plugin;

namespace LuaSTGEditorAva.Services
{
    public static class IconCache
    {
        private static readonly Dictionary<string, Bitmap> cache =
            new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);

        public static Bitmap GetIcon(string packUri)
        {
            byte[] bytes = GetIconBytes(packUri);
            if (bytes == null) return null;
            lock (cache)
            {
                if (cache.TryGetValue(packUri, out Bitmap hit)) return hit;
                try
                {
                    Bitmap bmp = new Bitmap(new MemoryStream(bytes));
                    cache[packUri] = bmp;
                    return bmp;
                }
                catch
                {
                    return null;
                }
            }
        }

        public static byte[] GetIconBytes(string packUri)
        {
            if (string.IsNullOrEmpty(packUri)) return null;
            try
            {
                return LoadBytesCore(packUri);
            }
            catch
            {
                return null;
            }
        }

        public static Bitmap GetNodeIcon(Type nodeType)
        {
            string uri = GetNodeIconUri(nodeType);
            return uri == null ? null : GetIcon(uri);
        }

        public static string GetNodeIconUri(Type nodeType)
        {
            try
            {
                var plugin = PluginHandler.Plugin;
                if (plugin?.NodeTypeCache?.NodeTypeInfo == null || nodeType == null) return null;
                if (!plugin.NodeTypeCache.NodeTypeInfo.TryGetValue(nodeType, out var data)) return null;
                return data.icon;
            }
            catch
            {
                return null;
            }
        }

        private static byte[] LoadBytesCore(string packUri)
        {
            string uriPath = packUri.Replace('\\', '/');
            string fileName = uriPath;
            int slash = fileName.LastIndexOf('/');
            if (slash >= 0) fileName = fileName.Substring(slash + 1);
            if (string.IsNullOrEmpty(fileName)) return null;

            if (!packUri.StartsWith("/"))
            {
                byte[] disk = TryReadCustomNodeImage(fileName);
                if (disk != null) return disk;
            }

            string asmName = null;
            string relPath = null;
            if (packUri.StartsWith("/"))
            {
                int semi = packUri.IndexOf(';');
                if (semi > 1) asmName = packUri.Substring(1, semi - 1);
                int comp = packUri.IndexOf("component/", StringComparison.OrdinalIgnoreCase);
                if (comp >= 0) relPath = uriPath.Substring(comp + "component/".Length).TrimStart('/');
            }

            IEnumerable<Assembly> assemblies = AppDomain.CurrentDomain.GetAssemblies();
            if (!string.IsNullOrEmpty(asmName))
            {
                Assembly named = assemblies.FirstOrDefault(a =>
                    string.Equals(a.GetName().Name, asmName, StringComparison.OrdinalIgnoreCase));
                if (named != null) assemblies = new[] { named };
            }

            // Tiered matching so unrelated icons sharing a filename suffix can never
            // win (e.g. "task.png" must not resolve "stagetask.png", "stage.png" must
            // not resolve "bgstage.png"):
            // 1. full resource path from the pack URI (e.g. images/16x16/task.png),
            // 2. exact base file name,
            // 3. legacy filename-suffix match (bare custom-node names).
            string fullSuffix = ToManifestSuffix(relPath);

            byte[] best = null;
            foreach (Assembly asm in assemblies)
            {
                string[] resNames;
                try
                {
                    resNames = asm.GetManifestResourceNames();
                }
                catch
                {
                    continue;
                }
                string[] candidates = resNames
                    .Where(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                if (candidates.Length == 0) continue;
                string[] tier = fullSuffix != null
                    ? candidates.Where(n => ManifestPathEquals(n, fullSuffix)).ToArray()
                    : Array.Empty<string>();
                if (tier.Length == 0)
                    tier = candidates.Where(n => BaseNameEquals(n, fileName)).ToArray();
                if (tier.Length == 0)
                    tier = candidates;

                foreach (string resName in tier)
                {
                    byte[] bytes;
                    try
                    {
                        using Stream stream = asm.GetManifestResourceStream(resName);
                        if (stream == null) continue;
                        using var ms = new MemoryStream();
                        stream.CopyTo(ms);
                        bytes = ms.ToArray();
                    }
                    catch
                    {
                        continue;
                    }
                    if (PngArea(bytes) > PngArea(best))
                        best = bytes;
                }
            }
            if (best != null) return best;

            return TryReadCustomNodeImage(fileName);
        }

        private static string ToManifestSuffix(string relPath)
        {
            if (string.IsNullOrEmpty(relPath)) return null;
            string[] segs = relPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segs.Length == 0) return null;
            for (int i = 0; i < segs.Length; i++)
            {
                if (segs[i].Length > 0 && char.IsDigit(segs[i][0]))
                    segs[i] = "_" + segs[i];
            }
            return string.Join(".", segs);
        }

        private static bool ManifestPathEquals(string manifestName, string fullSuffix)
        {
            if (manifestName.Length < fullSuffix.Length) return false;
            if (!manifestName.EndsWith(fullSuffix, StringComparison.OrdinalIgnoreCase)) return false;
            if (manifestName.Length == fullSuffix.Length) return true;
            return manifestName[manifestName.Length - fullSuffix.Length - 1] == '.';
        }

        private static bool BaseNameEquals(string manifestName, string fileName)
        {
            int dot = manifestName.LastIndexOf('.');
            if (dot < 0) return false;
            int prev = manifestName.LastIndexOf('.', dot - 1);
            string baseName = prev < 0 ? manifestName.Substring(0, dot) : manifestName.Substring(prev + 1, dot - prev - 1);
            return string.Equals(baseName, Path.GetFileNameWithoutExtension(fileName), StringComparison.OrdinalIgnoreCase);
        }

        private static int PngArea(byte[] bytes)
        {
            try
            {
                if (bytes == null || bytes.Length < 33) return 0;
                byte[] signature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
                for (int i = 0; i < signature.Length; i++)
                    if (bytes[i] != signature[i]) return 0;
                int width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
                int height = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
                if (width <= 0 || height <= 0 || width > 4096 || height > 4096) return 0;
                return width * height;
            }
            catch
            {
                return 0;
            }
        }

        private static byte[] TryReadCustomNodeImage(string fileName)
        {
            string dir = Path.Combine(AppContext.BaseDirectory, "CustomNodes", "Images");
            foreach (string candidate in new[]
            {
                Path.Combine(dir, fileName),
                Path.Combine(dir, Path.GetFileNameWithoutExtension(fileName) + ".png"),
            })
            {
                if (File.Exists(candidate))
                {
                    try
                    {
                        return File.ReadAllBytes(candidate);
                    }
                    catch
                    {
                    }
                }
            }
            return null;
        }
    }
}
