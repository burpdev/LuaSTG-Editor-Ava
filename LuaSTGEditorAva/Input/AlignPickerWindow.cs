using Avalonia.Interactivity;
using Avalonia;
using System.Collections.Generic;using Avalonia.Media;using Avalonia.Layout;using Avalonia.Input;using Avalonia.Data;using Avalonia.Controls.Templates;using Avalonia.Controls.Primitives;using Avalonia.Controls;
namespace LuaSTGEditorAva.Input
{
    public sealed class AlignPickerWindow : AvaloniaInputWindow
    {
        private static readonly string[] Tags = { "0", "1", "2", "4", "5", "6", "8", "9", "10" };

        public AlignPickerWindow(string s)
        {
            Title = "Text alignment";
            Width = 280;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Result = s;

            var grid = new Grid { Margin = new Thickness(4) };
            for (int i = 0; i < 3; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            }
            for (int i = 0; i < Tags.Length; i++)
            {
                var button = new Button
                {
                    Tag = Tags[i],
                    Content = Tags[i],
                    Margin = new Thickness(2),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                button.Click += Option_Click;
                Grid.SetRow(button, i / 3);
                Grid.SetColumn(button, i % 3);
                grid.Children.Add(button);
            }

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
            panel.Children.Add(grid);

            Content = panel;
        }

        private void Option_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is string tag)
            {
                Result = tag;
                Accept();
            }
        }
    }
}
