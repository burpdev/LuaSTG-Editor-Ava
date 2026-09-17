using Avalonia.Input;
using Avalonia.Layout;
using Avalonia;
using System.Collections.Generic;using Avalonia.Media;using Avalonia.Interactivity;using Avalonia.Data;using Avalonia.Controls.Templates;using Avalonia.Controls.Primitives;using Avalonia.Controls;
namespace LuaSTGEditorAva.Input
{
    public sealed class SelectorWindow : AvaloniaInputWindow
    {
        private readonly ComboBox combo;

        public SelectorWindow(string s, string[] items, string title)
        {
            Title = title;
            Width = 430;
            Height = 140;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Result = s;

            combo = new ComboBox
            {
                IsEditable = true,
                ItemsSource = items,
                Text = s,
                Margin = new Thickness(0, 0, 0, 8)
            };
            combo.SelectionChanged += (sender, e) =>
            {
                if (combo.SelectedItem is string picked) Result = picked;
            };
            combo.KeyDown += (sender, e) =>
            {
                if (e.Key == Key.Enter) { Result = combo.Text; Accept(); }
            };

            var ok = new Button { Content = "OK", MinWidth = 90 };
            ok.Click += (sender, e) => { Result = combo.Text; Accept(); };
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
            panel.Children.Add(combo);

            Content = panel;
            Opened += (sender, e) => combo.Focus();
        }
    }
}
