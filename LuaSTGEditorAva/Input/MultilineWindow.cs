using Avalonia;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Controls;
using AvaloniaEdit;
namespace LuaSTGEditorAva.Input
{
    public class MultilineWindow : AvaloniaInputWindow
    {
        protected readonly TextEditor box;

        public MultilineWindow(string s, string title = "Input")
        {
            Title = title;
            Width = 560;
            Height = 420;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Result = s;

            box = new TextEditor
            {
                Text = s ?? string.Empty,
                Margin = new Thickness(0, 0, 0, 8),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Options =
                {
                    ShowSpaces = true,
                    WordWrapIndentation = 3
                }
            };
            box.TextChanged += (sender, e) => Result = box.Text;

            var ok = new Button { Content = "OK", MinWidth = 90 };
            ok.Click += (sender, e) => Accept();
            var cancel = new Button { Content = "Cancel", MinWidth = 90 };
            cancel.Click += (sender, e) => Cancel();
            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);

            var panel = new DockPanel { Margin = new Thickness(12) };
            panel.Children.Add(buttons);
            DockPanel.SetDock(buttons, Dock.Bottom);
            panel.Children.Add(box);

            Content = panel;
            Opened += (sender, e) => box.Focus();
        }
    }
}
