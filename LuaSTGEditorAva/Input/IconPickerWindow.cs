using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.Media;using Avalonia.Input;using Avalonia.Data;using Avalonia.Controls.Templates;using Avalonia.Controls.Primitives;using System.Collections.Generic;
using Avalonia;
using LuaSTGEditorAva.Services;
using System;
namespace LuaSTGEditorAva.Input
{
    public class IconPickerWindow : AvaloniaInputWindow
    {
        protected IconPickerWindow(string title, IEnumerable<(string tag, string imageUri)> options,
            int imageSize = 32, int columns = 8)
        {
            Title = title;
            Width = 640;
            Height = 480;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var grid = new Grid { Margin = new Thickness(4) };
            for (int i = 0; i < columns; i++)
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            int index = 0;
            foreach ((string tag, string imageUri) in options)
            {
                Bitmap bmp = IconCache.GetIcon(imageUri);
                Control content = bmp != null
                    ? (Control)new Image { Source = bmp, Width = imageSize, Height = imageSize }
                    : (Control)new TextBlock { Text = tag, TextWrapping = TextWrapping.Wrap };
                var button = new Button
                {
                    Tag = tag,
                    Content = content,
                    Padding = new Thickness(4),
                    Margin = new Thickness(2),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Center
                };
                ToolTip.SetTip(button, tag);
                button.Click += Option_Click;
                Grid.SetRow(button, index / columns);
                if (index % columns == 0)
                    grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                Grid.SetColumn(button, index % columns);
                grid.Children.Add(button);
                index++;
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
            var scroll = new ScrollViewer { Content = grid };
            panel.Children.Add(scroll);

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
