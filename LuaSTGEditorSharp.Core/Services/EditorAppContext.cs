using System;

namespace LuaSTGEditorSharp.Services
{
    public static class EditorAppContext
    {
        public static IAppSettings CurrentSettings { get; set; }

        public static IAppDebugSettings CurrentDebugSettings { get; set; }

        public static IMainWindow MainWindow { get; set; }

        public static IMessageNavigator MessageNavigator { get; set; }

        public static IDialogService Dialogs { get; set; } = new NullDialogService();

        public static IFileDialogService FileDialogs { get; set; } = new NullFileDialogService();

        public static bool BatchPacking => CurrentSettings?.BatchPacking ?? false;

        internal sealed class NullDialogService : IDialogService
        {
            public void ShowError(string message, string title = "LuaSTG Editor Ava")
            {
                try
                {
                    Serilog.Log.Error("[{Title}] {Message}", title, message);
                }
                catch
                {
                    Console.Error.WriteLine($"[{title}] {message}");
                }
            }
        }

        internal sealed class NullFileDialogService : IFileDialogService
        {
            public string ShowSaveFileDialog(string initialDirectory, string filter, string fileName)
            {
                return string.Empty;
            }
        }
    }
}
