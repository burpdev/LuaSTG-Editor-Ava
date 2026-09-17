using Avalonia.Controls.Templates;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia;
using LuaSTGEditorAva.Services;
using LuaSTGEditorSharp.Plugin;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
namespace LuaSTGEditorAva.Dialogs
{
    public sealed class SettingsDialog : Window
    {
        private readonly LinuxSettings settings = LinuxSettings.Instance;

        public SettingsDialog(int tabIndex = 0)
        {
            Title = "Settings";
            Width = 680;
            Height = 520;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var tabs = new TabControl();
            tabs.Items.Add(BuildGeneralTab());
            tabs.Items.Add(BuildCompilerTab());
            tabs.Items.Add(BuildDebugTab());
            tabs.Items.Add(BuildEditorTab());
            tabs.SelectedIndex = Math.Max(0, Math.Min(3, tabIndex));

            var ok = new Button { Content = "OK", MinWidth = 90 };
            ok.Click += (sender, e) => { settings.Save(); Close(); };
            var cancel = new Button { Content = "Cancel", MinWidth = 90 };
            cancel.Click += (sender, e) => Close();
            var apply = new Button { Content = "Apply", MinWidth = 90 };
            apply.Click += (sender, e) => settings.Save();
            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);
            buttons.Children.Add(apply);

            var panel = new DockPanel { Margin = new Thickness(12) };
            panel.Children.Add(buttons);
            DockPanel.SetDock(buttons, Dock.Bottom);
            panel.Children.Add(tabs);
            Content = panel;
        }

        private TabItem BuildGeneralTab()
        {
            var stack = new StackPanel { Spacing = 10, Margin = new Thickness(8) };
            stack.Children.Add(BoundCheck("Ignore wrong THLib messages", "IgnoreTHLibWarn"));
            stack.Children.Add(ThemeRow());
            stack.Children.Add(BoundCheck("Use Discord Rich Presence (requires restart)", "UseDiscordRpc"));
            stack.Children.Add(BoundCheck("Allow downloading remote templates (New File window)", "UseRemoteTemplates"));
            return new TabItem { Header = "General", Content = new ScrollViewer { Content = stack } };
        }

        private Control ThemeRow()
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            row.Children.Add(new TextBlock { Text = "Editor color theme", VerticalAlignment = VerticalAlignment.Center });
            var combo = new ComboBox
            {
                ItemsSource = ThemeManager.Available,
                SelectedItem = ThemeManager.Normalize(settings.CurrentTheme),
                Width = 160
            };
            combo.SelectionChanged += (sender, e) =>
            {
                if (combo.SelectedItem is string theme)
                {
                    settings.CurrentTheme = theme;
                    ThemeManager.Apply(theme);
                }
            };
            row.Children.Add(combo);
            row.Children.Add(new TextBlock
            {
                Text = "(applied immediately)",
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = 0.6
            });
            return row;
        }

        private TabItem BuildCompilerTab()
        {
            var grid = new Grid { RowSpacing = 10, Margin = new Thickness(0, 0, 12, 0) };
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            int row = 0;

            var batch = BoundCheck("Use batch packing", "BatchPacking");
            Grid.SetRow(batch, row);
            Grid.SetColumnSpan(batch, 3);
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            grid.Children.Add(batch);
            row++;

            AddPathRow(grid, row++, "Zip executable", "ZipExecutablePath", "7z executable|7z*");
            AddLabelRow(grid, row, "LuaSTG executable");
            AddPathRow(grid, row++, null, "LuaSTGExecutablePath", "LuaSTG executable|*");
            AddLabelRow(grid, row, "Game runner (Wine/Proton)");
            var runner = new ComboBox
            {
                SelectedValueBinding = new Binding("Id"),
                VerticalAlignment = VerticalAlignment.Center
            };
            runner.ItemsSource = new List<WineRunner.RunnerOption>
            {
                new WineRunner.RunnerOption { Id = "auto", Label = "auto (recommended)" },
                new WineRunner.RunnerOption { Id = "wine", Label = "wine" },
                new WineRunner.RunnerOption { Id = "wine64", Label = "wine64" },
                new WineRunner.RunnerOption { Id = "proton", Label = "proton" },
                new WineRunner.RunnerOption { Id = "custom", Label = "custom (command below)" },
            };
            runner.SelectedValue = settings.GameRunner;
            runner.SelectionChanged += (sender, e) =>
            {
                if (runner.SelectedValue is string id && !string.IsNullOrEmpty(id))
                    settings.GameRunner = id;
            };
            Opened += (sender, e) => _ = RefreshRunnersAsync(runner);
            Grid.SetRow(runner, row);
            Grid.SetColumn(runner, 1);
            Grid.SetColumnSpan(runner, 2);
            grid.Children.Add(runner);
            row++;
            AddTextRow(grid, row++, "Game Runner command", "CustomRunnerCommand");
            AddPathRow(grid, row++, "Temp path", "TempPath", null, folder: true);
            AddCheckRow(grid, row++, "Update THlib before running", "DebugUpdateLib");
            AddCheckRow(grid, row++, "Save project before running", "DebugSaveProj");
            AddCheckRow(grid, row++, "Pack project", "PackProj");
            AddCheckRow(grid, row++, "Use MD5 resource check in packing", "SaveResMeta");
            AddTextRow(grid, row++, "Editor output file name", "EditorOutputName");
            AddCheckRow(grid, row++, "Use folder packing", "UseFolderPacking");

            return new TabItem { Header = "Compiler", Content = new ScrollViewer { Content = grid } };
        }

        private TabItem BuildDebugTab()
        {
            var stack = new StackPanel { Spacing = 10, Margin = new Thickness(8) };
            var resRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            resRow.Children.Add(new TextBlock { Text = "Resolution", VerticalAlignment = VerticalAlignment.Center });
            var combo = new ComboBox
            {
                IsEditable = true,
                ItemsSource = new[] { "640x480", "800x600", "960x720", "1024x768", "1280x960" },
                Text = $"{settings.DebugResolutionX}x{settings.DebugResolutionY}",
                Width = 160
            };
            combo.SelectionChanged += (sender, e) => ApplyResolution(combo.Text);
            combo.LostFocus += (sender, e) => ApplyResolution(combo.Text);
            resRow.Children.Add(combo);
            stack.Children.Add(resRow);
            stack.Children.Add(BoundCheck("Windowed", "DebugWindowed"));
            stack.Children.Add(BoundCheck("Cheat", "DebugCheat"));
            stack.Children.Add(BoundCheck("Dynamic debug reporting (unstable)", "DynamicDebugReporting"));
            stack.Children.Add(BoundCheck("Enable log window (LuaSTG Sub only)", "SubLogWindow"));
            return new TabItem { Header = "Debug", Content = new ScrollViewer { Content = stack } };
        }

        private async Task RefreshRunnersAsync(ComboBox runner)
        {
            try
            {
                List<WineRunner.RunnerOption> options =
                    await Task.Run(() => WineRunner.DetectRunnerOptions());
                string saved = settings.GameRunner ?? "auto";
                if (!options.Any(o => string.Equals(o.Id, saved, StringComparison.OrdinalIgnoreCase))
                    && !string.IsNullOrEmpty(saved))
                {
                    options.Add(new WineRunner.RunnerOption { Id = saved, Label = $"{saved} (not detected)" });
                }
                runner.ItemsSource = options;
                var match = options.FirstOrDefault(o =>
                    string.Equals(o.Id, saved, StringComparison.OrdinalIgnoreCase));
                runner.SelectedValue = match?.Id;
            }
            catch
            {
            }
        }

        private void ApplyResolution(string text)
        {
            try
            {
                string[] parts = (text ?? "").ToLower().Replace(" ", "").Split('x');
                if (parts.Length == 2 && int.TryParse(parts[0], out int w) && int.TryParse(parts[1], out int h))
                {
                    settings.DebugResolutionX = w;
                    settings.DebugResolutionY = h;
                }
            }
            catch
            {
            }
        }

        private TabItem BuildEditorTab()
        {
            var stack = new StackPanel { Spacing = 10, Margin = new Thickness(8) };
            var authorRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            authorRow.Children.Add(new TextBlock { Text = "Default author name", VerticalAlignment = VerticalAlignment.Center, Width = 190 });
            var author = new TextBox { Width = 300 };
            author.Bind(TextBox.TextProperty, new Binding("AuthorName") { Source = settings, Mode = BindingMode.TwoWay });
            authorRow.Children.Add(author);
            stack.Children.Add(authorRow);
            stack.Children.Add(BoundCheck("Move to new nodes automatically", "AutoMoveToNew"));
            stack.Children.Add(BoundCheck("Auto save projects", "UseAutoSave"));
            var compact = BoundCheck("Compact view (compact nodes and properties)", "CompactView");
            compact.IsCheckedChanged += (sender, e) =>
                (Owner as MainWindow)?.ApplyCompactView(compact.IsChecked == true);
            stack.Children.Add(compact);

            var autoRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            autoRow.Children.Add(new TextBlock { Text = "Autosave interval (minutes)", VerticalAlignment = VerticalAlignment.Center, Width = 190 });
            var timer = new TextBox { Width = 100, Text = settings.AutoSaveTimer.ToString() };
            timer.LostFocus += (sender, e) =>
            {
                if (int.TryParse(timer.Text, out int minutes)) settings.AutoSaveTimer = minutes;
                else timer.Text = settings.AutoSaveTimer.ToString();
            };
            autoRow.Children.Add(timer);
            autoRow.Children.Add(new TextBlock { Text = "(applies from the next cycle)", VerticalAlignment = VerticalAlignment.Center, Opacity = 0.6 });
            stack.Children.Add(autoRow);

            var indent = new StackPanel { Spacing = 4 };
            indent.Children.Add(new TextBlock { Text = "Indentation", FontWeight = FontWeight.Bold });
            var space = new RadioButton { Content = "Space", IsChecked = settings.SpaceIndentation };
            space.Checked += (sender, e) => settings.SpaceIndentation = true;
            var tab = new RadioButton { Content = "Tab", IsChecked = !settings.SpaceIndentation };
            tab.Checked += (sender, e) => settings.SpaceIndentation = false;
            var countRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            countRow.Children.Add(new TextBlock { Text = "Space count:", VerticalAlignment = VerticalAlignment.Center });
            var count = new TextBox { Width = 80, Text = settings.IndentationSpaceLength.ToString() };
            count.LostFocus += (sender, e) =>
            {
                if (int.TryParse(count.Text, out int n)) settings.IndentationSpaceLength = n;
                else count.Text = settings.IndentationSpaceLength.ToString();
            };
            countRow.Children.Add(count);
            indent.Children.Add(space);
            indent.Children.Add(tab);
            indent.Children.Add(countRow);
            stack.Children.Add(indent);

            return new TabItem { Header = "Editor", Content = new ScrollViewer { Content = stack } };
        }

        private CheckBox BoundCheck(string content, string property)
        {
            var check = new CheckBox { Content = content };
            check.Bind(CheckBox.IsCheckedProperty,
                new Binding(property) { Source = settings, Mode = BindingMode.TwoWay });
            return check;
        }

        private void AddLabelRow(Grid grid, int row, string label)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var text = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(text, row);
            Grid.SetColumn(text, 0);
            grid.Children.Add(text);
        }

        private void AddCheckRow(Grid grid, int row, string content, string property)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var check = BoundCheck(content, property);
            Grid.SetRow(check, row);
            Grid.SetColumn(check, 0);
            Grid.SetColumnSpan(check, 3);
            grid.Children.Add(check);
        }

        private void AddTextRow(Grid grid, int row, string label, string property)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            AddLabelRow(grid, row, label);
            var box = new TextBox();
            box.Bind(TextBox.TextProperty,
                new Binding(property) { Source = settings, Mode = BindingMode.TwoWay });
            Grid.SetRow(box, row);
            Grid.SetColumn(box, 1);
            Grid.SetColumnSpan(box, 2);
            grid.Children.Add(box);
        }

        private void AddPathRow(Grid grid, int row, string label, string property, string filterDescription, bool folder = false)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            if (label != null) AddLabelRow(grid, row, label);
            var box = new TextBox();
            box.Bind(TextBox.TextProperty,
                new Binding(property) { Source = settings, Mode = BindingMode.TwoWay });
            Grid.SetRow(box, row);
            Grid.SetColumn(box, 1);
            grid.Children.Add(box);
            var browse = new Button { Content = "...", Width = 32 };
            browse.Click += async (sender, e) =>
            {
                try
                {
                    if (folder)
                    {
                        var folders = await this.StorageProvider.OpenFolderPickerAsync(
                            new FolderPickerOpenOptions { Title = label ?? "Choose folder" });
                        if (folders.Count > 0)
                        {
                            box.Text = folders[0].Path.LocalPath;
                            settings.GetType().GetProperty(property)?.SetValue(settings, box.Text);
                        }
                    }
                    else
                    {
                        var files = await this.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                        {
                            Title = label ?? "Choose file",
                            AllowMultiple = false,
                            FileTypeFilter = new[] { new FilePickerFileType(filterDescription ?? "All files") { Patterns = new[] { "*" } } }
                        });
                        if (files.Count > 0)
                        {
                            box.Text = files[0].Path.LocalPath;
                            settings.GetType().GetProperty(property)?.SetValue(settings, box.Text);
                        }
                    }
                }
                catch
                {
                }
            };
            Grid.SetRow(browse, row);
            Grid.SetColumn(browse, 2);
            grid.Children.Add(browse);
        }
    }
}
