using System;
using Avalonia;

namespace LuaSTGEditorAva
{
    internal static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                Environment.CurrentDirectory = AppContext.BaseDirectory;
            }
            catch
            {
            }
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();
        }
    }
}
