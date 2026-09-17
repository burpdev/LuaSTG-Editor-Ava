using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using LuaSTGEditorAva.Services;
using LuaSTGEditorSharp.Services;

namespace LuaSTGEditorAva.Dialogs
{
    public sealed class CustomNodesDialog : Window
    {
        private readonly ListBox list = new ListBox();
        private readonly TextBox fileBox = new TextBox { Watermark = "file name (Example2)" };
        private readonly TextBox displayBox = new TextBox { Watermark = "display name" };
        private readonly Func<Task> onChanged;

        public CustomNodesDialog(Func<Task> onChanged)
        {
            this.onChanged = onChanged;
            Title = "Custom Nodes";
            Width = 560;
            Height = 480;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var listScroll = new ScrollViewer
            {
                Content = list,
                Margin = new Thickness(0, 0, 0, 8),
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var createRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Margin = new Thickness(0, 8, 0, 8)
            };
            fileBox.Width = 150;
            displayBox.Width = 170;
            var createButton = new Button { Content = "Create", MinWidth = 70 };
            createButton.Click += CreateButton_Click;
            createRow.Children.Add(fileBox);
            createRow.Children.Add(displayBox);
            createRow.Children.Add(createButton);

            var status = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 4),
                Foreground = Brushes.Gray
            };

            var bottom = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            var reloadButton = new Button { Content = "Reload", MinWidth = 90 };
            reloadButton.Click += ReloadButton_Click;
            var openFolder = new Button { Content = "Open Folder", MinWidth = 110 };
            openFolder.Click += OpenFolder_Click;
            var deleteButton = new Button { Content = "Delete Selected", MinWidth = 130 };
            deleteButton.Click += DeleteButton_Click;
            var closeButton = new Button { Content = "Close", MinWidth = 90 };
            closeButton.Click += (sender, e) => Close();
            bottom.Children.Add(reloadButton);
            bottom.Children.Add(openFolder);
            bottom.Children.Add(deleteButton);
            bottom.Children.Add(closeButton);

            var panel = new DockPanel { Margin = new Thickness(12) };
            panel.Children.Add(bottom);
            DockPanel.SetDock(bottom, Dock.Bottom);
            panel.Children.Add(status);
            DockPanel.SetDock(status, Dock.Bottom);
            panel.Children.Add(createRow);
            DockPanel.SetDock(createRow, Dock.Bottom);
            panel.Children.Add(listScroll);

            Content = panel;
            RefreshList();
        }

        private void RefreshList()
        {
            list.Items.Clear();
            IReadOnlyList<string[]> entries = CustomNodeStore.GetRegisteredScripts();
            if (entries.Count == 0)
            {
                list.Items.Add(new TextBlock
                {
                    Text = "No custom nodes registered. Create one below, or write CustomNodes/Init.lua by hand.",
                    Opacity = 0.6
                });
                return;
            }
            foreach (string[] entry in entries)
            {
                list.Items.Add(new TextBlock
                {
                    Text = $"{entry[0]}   ({entry[1]}.lua)",
                    Margin = new Thickness(2)
                });
            }
        }

        private async void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshList();
            if (onChanged != null) await onChanged();
        }

        private async Task ShowErrorAsync(string message)
        {
            EditorAppContext.Dialogs.ShowError(message, "Custom Nodes");
            await Task.CompletedTask;
        }

        private static void OpenFolder(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch
            {
            }
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(CustomNodeStore.Root);
            OpenFolder(CustomNodeStore.Root);
        }

        private async void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            string fileName = fileBox.Text?.Trim();
            if (string.IsNullOrEmpty(fileName)) return;
            string displayName = displayBox.Text?.Trim();
            if (string.IsNullOrEmpty(displayName)) displayName = fileName;

            string error = CustomNodeStore.CreateFromTemplate(fileName, displayName);
            if (error != null)
            {
                await ShowErrorAsync(error);
                return;
            }
            fileBox.Clear();
            displayBox.Clear();
            RefreshList();
            if (onChanged != null) await onChanged();
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            string selected = SelectedFileBase();
            if (selected == null) return;
            int answer = await ConfirmDialog.AskAsync(this, "Custom Nodes",
                $"Delete CustomNodes/{selected}.lua?", "Delete", "Cancel");
            if (answer != 0) return;
            string error = CustomNodeStore.DeleteScript(selected);
            if (error != null)
            {
                await ShowErrorAsync(error);
                return;
            }
            RefreshList();
            if (onChanged != null) await onChanged();
        }

        private string SelectedFileBase()
        {
            var text = list.SelectedItem as TextBlock;
            if (text == null) return null;
            int open = text.Text.IndexOf('(');
            int dot = text.Text.LastIndexOf(".lua", StringComparison.OrdinalIgnoreCase);
            if (open < 0 || dot < 0 || dot <= open) return null;
            string name = text.Text.Substring(open + 1, dot - open - 1).Trim();
            return string.IsNullOrEmpty(name) ? null : name;
        }
    }
}
