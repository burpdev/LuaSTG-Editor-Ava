using Avalonia.Controls.Templates;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia;
using LuaSTGEditorAva.Services;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System;
namespace LuaSTGEditorAva.Dialogs
{
    public sealed class NewDialog : Window
    {
        private sealed class TemplateEntry
        {
            public string Text { get; set; }
            public string FullPath { get; set; }
            public bool IsRemote { get; set; }
            public string Display => IsRemote ? Text + "  (remote)" : Text;
        }

        private sealed class RemoteTemplate
        {
            public string name { get; set; }
            public string download_url { get; set; }
        }

        private readonly List<TemplateEntry> templates = new List<TemplateEntry>();
        private readonly Dictionary<string, string> remoteDescCache =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly ListBox list;
        private readonly TextBlock description;
        private readonly TextBox nameBox;
        private readonly TextBox authorBox;
        private readonly CheckBox allowPractice;
        private readonly CheckBox allowSCPractice;

        public string SelectedPath { get; private set; }
        public string SelectedName { get; private set; }
        public string Author { get; private set; }
        public bool AllowPractice { get; private set; } = true;
        public bool AllowSCPractice { get; private set; } = true;

        public NewDialog()
        {
            Title = "New File...";
            Width = 680;
            Height = 520;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            LoadLocalTemplates();

            list = new ListBox
            {
                ItemTemplate = new FuncDataTemplate<TemplateEntry>((t, ns) =>
                    new TextBlock { [!TextBlock.TextProperty] = new Binding("Display") })
            };
            list.ItemsSource = templates;
            list.SelectionChanged += (sender, e) => RefreshDescription();
            list.DoubleTapped += (sender, e) => Accept();

            var descHeader = new TextBlock { Text = "Description", FontWeight = FontWeight.Bold };
            description = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.85,
                Margin = new Thickness(0, 4, 0, 0)
            };
            var descPanel = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
            descPanel.Children.Add(descHeader);
            descPanel.Children.Add(description);

            var top = new Grid();
            top.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            top.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            Grid.SetColumn(list, 0);
            Grid.SetColumn(descPanel, 1);
            top.Children.Add(list);
            top.Children.Add(descPanel);

            nameBox = new TextBox { Text = "Untitled" };
            authorBox = new TextBox { Text = LinuxSettings.Instance.AuthorName };
            allowPractice = new CheckBox { Content = "Allow Practice", IsChecked = true };
            allowSCPractice = new CheckBox { Content = "Allow SC Practice", IsChecked = true };
            var checks = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 16 };
            checks.Children.Add(allowPractice);
            checks.Children.Add(allowSCPractice);

            var form = new Grid { Margin = new Thickness(0, 8, 0, 0) };
            form.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            form.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            AddFormRow(form, 0, "Name:", nameBox);
            AddFormRow(form, 1, "Author:", authorBox);
            Grid.SetRow(checks, 2);
            Grid.SetColumn(checks, 1);
            form.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            form.Children.Add(checks);

            var ok = new Button { Content = "OK", MinWidth = 90 };
            ok.Click += (sender, e) => Accept();
            var cancel = new Button { Content = "Cancel", MinWidth = 90 };
            cancel.Click += (sender, e) => { SelectedPath = null; Close(false); };
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
            panel.Children.Add(form);
            DockPanel.SetDock(form, Dock.Bottom);
            panel.Children.Add(top);
            Content = panel;

            if (templates.Count > 0)
                list.SelectedIndex = 0;

            Opened += (sender, e) => _ = LoadRemoteTemplatesAsync();
        }

        private static void AddFormRow(Grid grid, int row, string label, Control control)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var text = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(text, row);
            Grid.SetColumn(text, 0);
            grid.Children.Add(text);
            Grid.SetRow(control, row);
            Grid.SetColumn(control, 1);
            control.Margin = new Thickness(8, 2, 0, 2);
            grid.Children.Add(control);
        }

        private void LoadLocalTemplates()
        {
            try
            {
                string dir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates"));
                if (!Directory.Exists(dir)) return;
                var files = new DirectoryInfo(dir).GetFiles("*.lstges").Cast<FileInfo>()
                    .Concat(new DirectoryInfo(dir).GetFiles("*.lstgproj"));
                foreach (FileInfo fi in files)
                    templates.Add(new TemplateEntry
                    {
                        Text = Path.GetFileNameWithoutExtension(fi.Name),
                        FullPath = fi.FullName
                    });
            }
            catch
            {
            }
        }

        private async Task LoadRemoteTemplatesAsync()
        {
            if (!LinuxSettings.Instance.UseRemoteTemplates) return;
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Sharp-X Editor (Linux)");
                client.Timeout = TimeSpan.FromSeconds(15);
                string json = await client.GetStringAsync(
                    "https://api.github.com/repos/Sharp-X-Team/Sharp-X-templates/contents/templates");
                var remote = JsonConvert.DeserializeObject<List<RemoteTemplate>>(json);
                if (remote == null) return;
                bool added = false;
                foreach (RemoteTemplate rt in remote)
                {
                    string ext = Path.GetExtension(rt.name ?? "");
                    if (ext != ".lstges" && ext != ".lstgproj") continue;
                    if (templates.Any(t => t.Text == Path.GetFileNameWithoutExtension(rt.name))) continue;
                    templates.Add(new TemplateEntry
                    {
                        Text = Path.GetFileNameWithoutExtension(rt.name),
                        FullPath = rt.download_url,
                        IsRemote = true
                    });
                    added = true;
                }
                if (added)
                {
                    int selected = list.SelectedIndex;
                    list.ItemsSource = null;
                    list.ItemsSource = templates;
                    list.SelectedIndex = selected;
                }
            }
            catch
            {
            }
        }

        private void RefreshDescription()
        {
            if (list.SelectedItem is not TemplateEntry entry)
            {
                description.Text = "";
                return;
            }
            if (entry.IsRemote)
            {
                description.Text = "Fetching description...";
                _ = RefreshRemoteDescriptionAsync(entry);
                return;
            }
            try
            {
                string descPath = Path.Combine(
                    Path.GetDirectoryName(entry.FullPath) ?? "",
                    entry.Text + ".txt");
                using var sr = new StreamReader(descPath);
                description.Text = sr.ReadLine() ?? $"No description for \"{entry.Text}\".";
            }
            catch
            {
                description.Text = $"No description for \"{entry.Text}\".";
            }
        }

        private async Task RefreshRemoteDescriptionAsync(TemplateEntry entry)
        {
            try
            {
                if (!remoteDescCache.TryGetValue(entry.Text, out string contents))
                {
                    string url = Path.ChangeExtension(entry.FullPath, "txt") ?? "";
                    int seg = url.LastIndexOf("/templates/", StringComparison.OrdinalIgnoreCase);
                    if (seg >= 0)
                        url = url.Substring(0, seg) + "/descriptions/" + url.Substring(seg + "/templates/".Length);
                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Sharp-X Editor (Linux)");
                    client.Timeout = TimeSpan.FromSeconds(15);
                    contents = (await client.GetStringAsync(url))?.Split('\n')[0] ?? "";
                    remoteDescCache[entry.Text] = contents;
                }
                if (ReferenceEquals(list.SelectedItem, entry))
                    description.Text = string.IsNullOrEmpty(contents)
                        ? $"No description for \"{entry.Text}\"."
                        : contents;
            }
            catch
            {
                if (ReferenceEquals(list.SelectedItem, entry))
                    description.Text = $"No description for \"{entry.Text}\".";
            }
        }

        private void Accept()
        {
            if (list.SelectedItem is TemplateEntry entry)
            {
                SelectedPath = entry.FullPath;
                SelectedName = string.IsNullOrWhiteSpace(nameBox.Text) ? "Untitled" : nameBox.Text.Trim();
                Author = authorBox.Text ?? "";
                AllowPractice = allowPractice.IsChecked != false;
                AllowSCPractice = allowSCPractice.IsChecked != false;
                Close(true);
            }
        }
    }
}
