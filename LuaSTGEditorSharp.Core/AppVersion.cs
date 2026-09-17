using System.Reflection;
using System.Runtime.InteropServices;

namespace LuaSTGEditorSharp
{
    public static class AppVersion
    {
        public static string Current
        {
            get
            {
                try
                {
                    var attr = typeof(AppVersion).Assembly
                        .GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                    string v = attr?.InformationalVersion;
                    if (!string.IsNullOrEmpty(v))
                    {
                        // Strip build metadata (e.g. "+githash") if ever present.
                        int plus = v.IndexOf('+');
                        return plus > 0 ? v.Substring(0, plus) : v;
                    }
                }
                catch
                {
                }
                return "0.0.0";
            }
        }

        public static string PlatformSuffix
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return "(Windows)";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return "(MacOS)";
                return "(Linux)";
            }
        }
    }
}
