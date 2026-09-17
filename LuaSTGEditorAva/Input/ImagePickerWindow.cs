using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia;
using LuaSTGEditorAva.Services;
using LuaSTGEditorSharp;
using LuaSTGEditorSharp.EditorData.Document.Meta;
using LuaSTGEditorSharp.EditorData.Document;
using LuaSTGEditorSharp.EditorData;
using System.Collections.Generic;using Avalonia.Media;using Avalonia.Interactivity;using Avalonia.Input;using Avalonia.Data;using Avalonia.Controls.Templates;using Avalonia.Controls.Primitives;using System.Collections.ObjectModel;
using System.Linq;
using System;
namespace LuaSTGEditorAva.Input
{
    public sealed class ImagePickerWindow : AvaloniaInputWindow
    {
        private TextBox codeBox;
        private int groupIndex = 1;
        private int groupCols = 1;
        private int groupRows = 1;

        private sealed class Entry
        {
            public MetaModel Model;
            public bool Internal;
            public string Display => Internal ? $"{Model.FullName} (Internal)" : Model.FullName;
        }

        public ImagePickerWindow(string s, AttrItem item, bool allowAnimation, bool allowParticle)
        {
            Title = "Image";
            Width = 760;
            Height = 540;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var images = item.Parent.parentWorkSpace.Meta.aggregatableMetas[(int)MetaType.ImageLoad]
                .GetAllSimpleWithDifficulty();
            var groups = item.Parent.parentWorkSpace.Meta.aggregatableMetas[(int)MetaType.ImageGroupLoad]
                .GetAllSimpleWithDifficulty();
            var particles = item.Parent.parentWorkSpace.Meta.aggregatableMetas[(int)MetaType.ParticleLoad]
                .GetAllSimpleWithDifficulty();
            var animations = item.Parent.parentWorkSpace.Meta.aggregatableMetas[(int)MetaType.AnimationLoad]
                .GetAllSimpleWithDifficulty();
            var sysImages = new ObservableCollection<MetaModel>(NodesConfig.SysImage);
            var sysGroups = new ObservableCollection<MetaModel>(NodesConfig.SysImageGroup);

            codeBox = new TextBox { Margin = new Thickness(0, 8, 0, 0) };
            codeBox.TextChanged += (sender, e) => Result = codeBox.Text;
            Result = s;
            codeBox.Text = Result;

            var tabs = new TabControl();
            tabs.Items.Add(BuildListTab("Image", images, sysImages, s, withIndex: false));
            tabs.Items.Add(BuildListTab("Image Group", groups, sysGroups, s, withIndex: true));
            if (allowParticle)
                tabs.Items.Add(BuildListTab("Particle", particles,
                    new ObservableCollection<MetaModel>(), s, withIndex: false));
            if (allowAnimation)
                tabs.Items.Add(BuildListTab("Animation", animations,
                    new ObservableCollection<MetaModel>(), s, withIndex: false));

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
            panel.Children.Add(codeBox);
            DockPanel.SetDock(codeBox, Dock.Bottom);
            panel.Children.Add(tabs);

            Content = panel;
        }

        private TabItem BuildListTab(string header, ObservableCollection<MetaModel> items,
            ObservableCollection<MetaModel> sysItems, string initial, bool withIndex)
        {
            List<Entry> all = new List<Entry>(items.Count + sysItems.Count);
            foreach (MetaModel m in items) all.Add(new Entry { Model = m, Internal = false });
            foreach (MetaModel m in sysItems) all.Add(new Entry { Model = m, Internal = true });

            var filtered = new ObservableCollection<Entry>(all);

            var filter = new TextBox { Watermark = "Filter...", Margin = new Thickness(0, 0, 0, 4) };
            filter.TextChanged += (sender, e) =>
            {
                string f = filter.Text ?? "";
                filtered.Clear();
                foreach (Entry en in all)
                {
                    if (en.Model.FullName != null && en.Model.FullName.Contains(f))
                        filtered.Add(en);
                }
            };

            var list = new ListBox
            {
                ItemsSource = filtered,
                ItemTemplate = new FuncDataTemplate<Entry>((m, ns) =>
                    new TextBlock { [!TextBlock.TextProperty] = new Binding("Display") })
            };

            var preview = new Image
            {
                MaxWidth = 280,
                MaxHeight = 280,
                Stretch = Stretch.Uniform
            };
            var info = new TextBlock { Opacity = 0.7, TextWrapping = TextWrapping.Wrap };
            var previewPanel = new StackPanel { Spacing = 4, MinWidth = 200 };
            previewPanel.Children.Add(preview);
            previewPanel.Children.Add(info);

            NumericUpDown indexBox = null;
            StackPanel indexRow = null;
            if (withIndex)
            {
                indexRow = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 4, 0, 0)
                };
                indexBox = new NumericUpDown
                {
                    Minimum = 1,
                    Maximum = 4096,
                    Increment = 1,
                    FormatString = "0",
                    Value = 1,
                    Width = 140,
                    FontFamily = new FontFamily("DejaVu Sans Mono,Consolas,monospace"),
                    HorizontalContentAlignment = HorizontalAlignment.Center
                };
                indexRow.Children.Add(indexBox);
                indexBox.ValueChanged += (sender, e) =>
                {
                    groupIndex = Math.Max(1, (int)(indexBox.Value ?? 1));
                    RefreshGroupResult((list.SelectedItem as Entry)?.Model);
                    RefreshPreview((list.SelectedItem as Entry)?.Model);
                };
            }

            void Refresh(MetaModel m, bool isInternal)
            {
                if (m == null) return;
                if (!string.IsNullOrEmpty(m.Result)) Result = m.Result;
                if (codeBox != null) codeBox.Text = Result;
                if (withIndex)
                {
                    int cols = 1;
                    int rows = 1;
                    try
                    {
                        string[] colrow = (m.ExInfo2 ?? "").Split(',');
                        if (colrow.Length > 1)
                        {
                            if (int.TryParse(colrow[0], out int cx) && cx > 0) cols = cx;
                            if (int.TryParse(colrow[1], out int rx) && rx > 0) rows = rx;
                        }
                    }
                    catch
                    {
                    }
                    groupCols = cols;
                    groupRows = rows;
                    if (indexBox != null)
                    {
                        indexBox.Maximum = Math.Max(1, cols * rows);
                        groupIndex = Math.Min(groupIndex, cols * rows);
                        indexBox.Value = groupIndex;
                    }
                    RefreshGroupResult(m);
                }
                RefreshPreview(m, isInternal);
            }

            void RefreshPreview(MetaModel m, bool isInternal = false)
            {
                if (m == null) return;
                Bitmap full = LoadPreview(m.ExInfo1);
                IImage shown = full;
                if (withIndex && full != null)
                    shown = CropCell(full, groupIndex, groupCols, groupRows) ?? (IImage)full;
                preview.Source = shown;
                info.Text = Describe(m, isInternal, full, withIndex, groupIndex);
            }

            list.SelectionChanged += (sender, e) =>
            {
                Refresh((list.SelectedItem as Entry)?.Model, (list.SelectedItem as Entry)?.Internal == true);
            };
            list.DoubleTapped += (sender, e) => Accept();

            var body = new Grid();
            body.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            body.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            Grid.SetColumn(list, 0);
            Grid.SetColumn(previewPanel, 1);
            body.Children.Add(list);
            body.Children.Add(previewPanel);

            var tabPanel = new DockPanel();
            tabPanel.Children.Add(filter);
            DockPanel.SetDock(filter, Dock.Top);
            if (withIndex && indexRow != null)
            {
                tabPanel.Children.Add(indexRow);
                DockPanel.SetDock(indexRow, Dock.Bottom);
            }
            tabPanel.Children.Add(body);

            Entry match = FindInitial(all, initial, withIndex);
            if (match != null)
            {
                list.SelectedItem = match;
                Refresh(match.Model, match.Internal);
            }

            return new TabItem { Header = header, Content = tabPanel };
        }

        private Entry FindInitial(List<Entry> all, string initial, bool withIndex)
        {
            if (all == null || all.Count == 0 || string.IsNullOrEmpty(initial)) return null;
            foreach (Entry en in all)
            {
                if (en.Model.Result == initial) return en;
            }
            if (withIndex)
            {
                //Group results append the cell index: "\"image:name12\""
                //Strip trailing digits to match, and restore the index so the preview/cell stay in sync
                string stripped = initial.Trim().Trim('"');
                int k = stripped.Length;
                while (k > 0 && char.IsDigit(stripped[k - 1])) k--;
                if (k < stripped.Length && int.TryParse(stripped.Substring(k), out int idx) && idx >= 1)
                    groupIndex = idx;
                string prefix = k < stripped.Length ? stripped.Substring(0, k) : stripped;
                foreach (Entry en in all)
                {
                    string r = (en.Model.Result ?? "").Trim().Trim('"');
                    if (r == prefix) return en;
                }
            }
            return null;
        }

        private void RefreshGroupResult(MetaModel m)
        {
            if (m == null || string.IsNullOrEmpty(m.Result)) return;
            Result = "\"" + m.Result.Trim('"') + groupIndex + "\"";
            if (codeBox != null) codeBox.Text = Result;
        }

        private static string Describe(MetaModel m, bool isInternal, Bitmap bmp, bool withIndex, int index)
        {
            string source = isInternal ? "(Internal)" : "(Document)";
            string size = "";
            if (bmp != null)
                size = $"\n{bmp.PixelSize.Width} x {bmp.PixelSize.Height}";
            if (withIndex)
                size += $"\n{Math.Max(1, GetCols(m))} x {Math.Max(1, GetRows(m))} cells, cell {Math.Max(1, index)}";
            return $"{m.FullName} {source}{size}";
        }

        private static int GetCols(MetaModel m)
        {
            try
            {
                string[] colrow = (m.ExInfo2 ?? "").Split(',');
                if (colrow.Length > 1 && int.TryParse(colrow[0], out int cx) && cx > 0) return cx;
            }
            catch { }
            return 1;
        }

        private static int GetRows(MetaModel m)
        {
            try
            {
                string[] colrow = (m.ExInfo2 ?? "").Split(',');
                if (colrow.Length > 1 && int.TryParse(colrow[1], out int rx) && rx > 0) return rx;
            }
            catch { }
            return 1;
        }

        private static IImage CropCell(Bitmap full, int index, int cols, int rows)
        {
            try
            {
                cols = Math.Max(1, cols);
                rows = Math.Max(1, rows);
                index = Math.Max(1, Math.Min(index, cols * rows));
                int w = full.PixelSize.Width;
                int h = full.PixelSize.Height;
                int cellW = Math.Max(1, w / cols);
                int cellH = Math.Max(1, h / rows);
                int x = ((index - 1) % cols) * cellW;
                int y = ((index - 1) / cols) * cellH;
                x = Math.Max(0, Math.Min(x, w - 1));
                y = Math.Max(0, Math.Min(y, h - 1));
                cellW = Math.Min(cellW, w - x);
                cellH = Math.Min(cellH, h - y);
                if (cellW <= 0 || cellH <= 0) return null;
                return new CroppedBitmap(full, new PixelRect(x, y, cellW, cellH));
            }
            catch
            {
                return null;
            }
        }

        private static Bitmap LoadPreview(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            try
            {
                string p = path.Trim().Trim('"');
                if (p.StartsWith("pack:", StringComparison.OrdinalIgnoreCase))
                    return IconCache.GetIcon(p);
                if (System.IO.File.Exists(p))
                    return new Bitmap(p);
                if (System.IO.File.Exists(path))
                    return new Bitmap(path);
            }
            catch
            {
            }
            return null;
        }
    }
}
