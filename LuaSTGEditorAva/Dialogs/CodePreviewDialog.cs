using Avalonia.Controls;
using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaEdit;
using LuaSTGEditorAva.Services;
namespace LuaSTGEditorAva.Dialogs
{
    public sealed class CodePreviewDialog : Window
    {
        public CodePreviewDialog(string title, string lua)
        {
            Title = title;
            Width = 700;
            Height = 520;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var text = new TextEditor
            {
                Text = lua ?? string.Empty,
                IsReadOnly = true,
                FontFamily = new FontFamily("DejaVu Sans Mono,Consolas,monospace")
            };
            try
            {
                var def = LuaHighlighting.Definition;
                if (def != null) text.SyntaxHighlighting = def;
            }
            catch
            {
            }

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
