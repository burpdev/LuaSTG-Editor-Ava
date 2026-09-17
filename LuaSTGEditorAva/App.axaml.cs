using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LuaSTGEditorAva.Input;
using LuaSTGEditorAva.Services;
using LuaSTGEditorSharp.Services;
using LstgHeadless;

namespace LuaSTGEditorAva
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            EditorAppContext.CurrentSettings = LinuxSettings.Instance;
            EditorAppContext.CurrentDebugSettings = LinuxSettings.Instance;
            DocumentSession.EnsureInitialized();
            AvaloniaInputSelector.Register(new AvaloniaInputRegistry());
            AvaloniaInputSelector.AfterRegister();
            ThemeManager.Apply(LinuxSettings.Instance.CurrentTheme);

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var main = new MainWindow();
                EditorAppContext.Dialogs = new AvaloniaDialogService(() => main);
                EditorAppContext.FileDialogs = new AvaloniaFileDialogs();
                EditorAppContext.MainWindow = main;
                EditorAppContext.MessageNavigator = new AvaloniaMessageNavigator(main);
                desktop.MainWindow = main;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
