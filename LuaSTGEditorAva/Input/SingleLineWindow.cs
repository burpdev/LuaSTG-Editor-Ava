using Avalonia.Input;
using Avalonia.Layout;
using Avalonia;
using System.Collections.Generic;using Avalonia.Media;using Avalonia.Interactivity;using Avalonia.Data;using Avalonia.Controls.Templates;using Avalonia.Controls.Primitives;using Avalonia.Controls;
namespace LuaSTGEditorAva.Input
{
    public sealed class SingleLineWindow : AvaloniaInputWindow
    {
        private readonly TextBox box;

        public SingleLineWindow(string s)
        {
            Title = "Input";
            Width = 430;
            Height = 140;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Result = s;

            box = new TextBox { Text = s, Margin = new Thickness(0, 0, 0, 8) };
            box.TextChanged += (sender, e) => Result = box.Text;
            box.KeyDown += (sender, e) =>
            {
                if (e.Key == Key.Enter) Accept();
            };

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
            Opened += (sender, e) => { box.Focus(); box.SelectAll(); };
        }
    }
}
