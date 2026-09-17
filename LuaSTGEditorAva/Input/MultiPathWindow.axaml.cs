using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using LuaSTGEditorSharp.EditorData;
using LuaSTGEditorSharp.Services;
using LuaSTGEditorSharp;
using System.Collections.Generic;using Avalonia.Media;using Avalonia.Layout;using Avalonia.Data;using Avalonia.Controls.Templates;using Avalonia.Controls.Primitives;using Avalonia;using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System;
namespace LuaSTGEditorAva.Input
{
    public partial class MultiPathWindow : AvaloniaInputWindow
    {
        private readonly string docDirectory;
        private readonly string fileFilter;
        private readonly ObservableCollection<string> items = new ObservableCollection<string>();

        public MultiPathWindow(string s, string wpfFilter, AttrItem owner)
        {
            InitializeComponent();
            fileFilter = wpfFilter;
            try
            {
                docDirectory = Path.GetDirectoryName(owner?.Parent?.parentWorkSpace?.DocPath);
            }
            catch
            {
                docDirectory = "";
            }
            Result = s ?? string.Empty;
            Decompose();
            var list = this.FindControl<ListBox>("FileList");
            list.ItemsSource = items;
            list.KeyDown += List_KeyDown;
            this.FindControl<Button>("AddBtn").Click += Add_Click;
            this.FindControl<Button>("ClearBtn").Click += (sender, e) =>
            {
                items.Clear();
                Combine();
            };
            this.FindControl<Button>("OkBtn").Click += (sender, e) => Accept();
            this.FindControl<Button>("CancelBtn").Click += (sender, e) => Cancel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void Decompose()
        {
            items.Clear();
            foreach (string f in Result.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Distinct())
            {
                string t = f.Trim();
                if (!string.IsNullOrEmpty(t)) items.Add(t);
            }
        }

        private void Combine()
        {
            Result = string.Join("|", items.Distinct());
        }

        private void List_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete) return;
            var list = this.FindControl<ListBox>("FileList");
            var selected = list.SelectedItems.Cast<string>().ToList();
            foreach (string f in selected) items.Remove(f);
            Combine();
        }

        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            IReadOnlyList<IStorageFile> files;
            try
            {
                files = await this.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = Title,
                    AllowMultiple = true,
                    FileTypeFilter = new[]
                    {
                        new FilePickerFileType("File") { Patterns = new[] { "*" } }
                    }
                });
            }
            catch
            {
                return;
            }
            var paths = items.ToList();
            string firstDir = null;
            foreach (IStorageFile f in files)
            {
                string picked = f.Path.LocalPath;
                firstDir ??= Path.GetDirectoryName(picked);
                if (!string.IsNullOrEmpty(docDirectory) && picked.Contains(docDirectory))
                    paths.Add(RelativePathConverter.GetRelativePath(docDirectory, picked));
                else
                    paths.Add(picked);
            }
            items.Clear();
            foreach (string p in paths.Distinct()) items.Add(p);
            Combine();
            try
            {
                if (firstDir != null && EditorAppContext.CurrentSettings != null)
                    EditorAppContext.CurrentSettings.SLDir = firstDir;
            }
            catch
            {
            }
        }
    }
}
