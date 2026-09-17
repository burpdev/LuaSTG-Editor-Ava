using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using LuaSTGEditorSharp.EditorData;

using DanmakuRandomizer.Model;

namespace DanmakuRandomizer.Views
{
    public sealed class RandomizerWindow : Window
    {
        private readonly Slider slider = new Slider
        {
            Minimum = 1,
            Maximum = 50,
            Value = 10,
            Width = 360,
            IsSnapToTickEnabled = true,
            TickFrequency = 1
        };
        private readonly TextBlock valueText = new TextBlock { Text = "10" };
        private readonly TextBlock info = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 8),
            Opacity = 0.75
        };

        public TreeNode ResultNodes { get; private set; }

        public RandomizerWindow()
        {
            Title = "Danmaku Randomizer";
            Width = 460;
            Height = 230;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            CanResize = false;

            slider.ValueChanged += (sender, e) =>
                valueText.Text = ((int)Math.Round(slider.Value)).ToString();

            var depthRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                Margin = new Thickness(0, 0, 0, 8)
            };
            depthRow.Children.Add(new TextBlock { Text = "Depth:" });
            depthRow.Children.Add(slider);
            depthRow.Children.Add(valueText);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            var generate = new Button { Content = "Generate", MinWidth = 100 };
            generate.Click += (sender, e) => Generate();
            var ok = new Button { Content = "OK", MinWidth = 90 };
            ok.Click += (sender, e) =>
            {
                if (ResultNodes == null) Generate();
                Close();
            };
            var cancel = new Button { Content = "Cancel", MinWidth = 90 };
            cancel.Click += (sender, e) => { ResultNodes = null; Close(); };
            buttons.Children.Add(generate);
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);

            var panel = new DockPanel { Margin = new Thickness(12) };
            panel.Children.Add(buttons);
            DockPanel.SetDock(buttons, Dock.Bottom);
            panel.Children.Add(depthRow);
            DockPanel.SetDock(depthRow, Dock.Top);
            panel.Children.Add(info);
            Content = panel;
        }

        public void Generate()
        {
            ResultNodes = new RandomDanmaku
            {
                Depth = (int)Math.Round(slider.Value)
            }.Randomize().GetTreeNodes(null);
            info.Text = $"Generated a boss definition tree with " +
                $"{ResultNodes.Children.Count} top-level children.";
        }
    }
}
