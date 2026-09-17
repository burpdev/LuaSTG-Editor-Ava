using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

using LuaSTGEditorSharp.EditorData.Document.Meta;

namespace LuaSTGEditorSharp.Plugin
{
    public class SystemMetaLoader
    {
        public static MetaModel[] FromResource(string path)
        {
            MetaModel[] mm = new MetaModel[1];
            try
            {
                string fileName = path.Replace('\\', '/');
                int slash = fileName.LastIndexOf('/');
                if (slash >= 0) fileName = fileName.Substring(slash + 1);

                string asmName = null;
                if (path.StartsWith("pack://", StringComparison.OrdinalIgnoreCase))
                {
                    int semi = path.IndexOf(';');
                    if (semi > 0)
                    {
                        int comma = path.LastIndexOf(',', semi);
                        asmName = path.Substring(comma + 1, semi - comma - 1);
                    }
                }

                Assembly asm = null;
                if (!string.IsNullOrEmpty(asmName))
                {
                    asm = AppDomain.CurrentDomain.GetAssemblies()
                        .FirstOrDefault(a => string.Equals(a.GetName().Name, asmName, StringComparison.OrdinalIgnoreCase));
                    if (asm == null)
                    {
                        try { asm = Assembly.Load(asmName); } catch { asm = null; }
                    }
                }
                if (asm == null) asm = Assembly.GetCallingAssembly();

                string resName = asm.GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
                if (resName == null)
                    throw new FileNotFoundException($"Embedded resource '{fileName}' not found in assembly '{asm.FullName}'.");

                using Stream stream = asm.GetManifestResourceStream(resName);
                using StreamReader sr = new StreamReader(stream);
                string s = sr.ReadToEnd();
                mm = JsonConvert.DeserializeObject<MetaModel[]>(s);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return mm;
        }
    }
}
