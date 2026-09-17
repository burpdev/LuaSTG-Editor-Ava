using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using System.Collections.Generic;using Avalonia.Media;using Avalonia.Input;using Avalonia.Data;using Avalonia.Controls.Templates;using Avalonia.Controls.Primitives;using Avalonia;
using System;
namespace LuaSTGEditorAva.Input
{
    public sealed class SizePickerWindow : AvaloniaInputWindow
    {
        private TextBox xBox;
        private TextBox yBox;

        public SizePickerWindow(string s)
        {
            Title = "Size";
            Width = 420;
            Height = 220;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            BuildUi();
            Result = s ?? string.Empty;
            ParseResult();
        }

        private void ParseResult()
        {
            var cs = Separate(Result);
            if (cs.Count >= 1 && double.TryParse(cs[0], out double bx)) xBox.Text = Math.Abs(bx).ToString();
            if (cs.Count >= 2 && double.TryParse(cs[1], out double by)) yBox.Text = Math.Abs(by).ToString();
        }

        private void Combine()
        {
            Result = xBox.Text + "," + yBox.Text;
        }

        private void BuildUi()
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            xBox = new TextBox { Watermark = "Width", Margin = new Thickness(0, 0, 2, 0) };
            xBox.TextChanged += (sender, e) => Combine();
            yBox = new TextBox { Watermark = "Height", Margin = new Thickness(2, 0, 0, 0) };
            yBox.TextChanged += (sender, e) => Combine();
            Grid.SetColumn(xBox, 0);
            Grid.SetColumn(yBox, 1);
            grid.Children.Add(xBox);
            grid.Children.Add(yBox);

            var canvasBtn = new Button { Content = "Canvas...", Margin = new Thickness(0, 8, 0, 0) };
            canvasBtn.Click += Canvas_Click;

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
            panel.Children.Add(canvasBtn);
            DockPanel.SetDock(canvasBtn, Dock.Bottom);
            panel.Children.Add(grid);
            Content = panel;
            Opened += (sender, e) => xBox.Focus();
        }

        private async void Canvas_Click(object sender, RoutedEventArgs e)
        {
            var pe = new PositionCanvasWindow(PositionCanvasWindow.CanvasMode.Point);
            bool? ok = await pe.ShowDialogAsync(this);
            if (ok == true)
            {
                xBox.Text = Math.Abs(pe.SelectedX).ToString();
                yBox.Text = Math.Abs(pe.SelectedY).ToString();
                Combine();
            }
        }
    }
}
