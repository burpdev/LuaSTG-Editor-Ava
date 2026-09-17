using Avalonia.Controls.Templates;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia;
using System.Threading.Tasks;
namespace LuaSTGEditorAva.Dialogs
{
    public sealed class ConfirmDialog : Window
    {
        private int result = -1;

        private ConfirmDialog(string title, string message, string[] buttons)
        {
            Title = title;
            Width = 460;
            Height = 180;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            CanResize = false;

            var text = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap };
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            for (int i = 0; i < buttons.Length; i++)
            {
                int index = i;
                var button = new Button { Content = buttons[i], MinWidth = 90 };
                button.Click += (sender, e) => { result = index; Close(index); };
                row.Children.Add(button);
            }

            var panel = new DockPanel { Margin = new Thickness(12) };
            panel.Children.Add(row);
            DockPanel.SetDock(row, Dock.Bottom);
            panel.Children.Add(text);
            Content = panel;
        }

        public static Task<int> AskAsync(Window owner, string title, string message, params string[] buttons)
        {
            var dialog = new ConfirmDialog(title, message, buttons);
            return dialog.ShowDialog<int>(owner);
        }
    }
}
