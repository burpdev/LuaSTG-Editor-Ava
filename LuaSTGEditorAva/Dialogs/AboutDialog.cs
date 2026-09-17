using Avalonia.Controls.Templates;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia;
using LuaSTGEditorSharp;
namespace LuaSTGEditorAva.Dialogs
{
    public sealed class AboutDialog : Window
    {
        public AboutDialog()
        {
            Title = "About";
            Width = 480;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            CanResize = false;

            var text = new TextBlock
            {
                Text = $"LuaSTG Editor Ava v{AppVersion.Current}\n" +
                    "A code generator for the LuaSTG engine, based on THlib.\n\n" +
                    "Original Editor Sharp by czh098tom.\n" +
                    "Sharp X by zinoLath, RyannThi & Rūl Hōlos. (Sharp X Team)\n" +
                    "Avalonia port by burpdev.",
                TextWrapping = TextWrapping.Wrap
            };
            var close = new Button { Content = "Close", MinWidth = 90 };
            close.Click += (sender, e) => Close();

            var panel = new DockPanel { Margin = new Thickness(12) };
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            row.Children.Add(close);
            panel.Children.Add(row);
            DockPanel.SetDock(row, Dock.Bottom);
            panel.Children.Add(text);
            Content = panel;
        }
    }
}
