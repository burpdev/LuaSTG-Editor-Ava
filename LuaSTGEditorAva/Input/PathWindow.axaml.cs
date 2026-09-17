using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;using Avalonia.Layout;using Avalonia.Input;using Avalonia.Data;using Avalonia.Controls.Templates;using Avalonia.Controls.Primitives;using Avalonia;using System.Collections.Generic;
using Avalonia.Platform.Storage;
using LuaSTGEditorSharp.EditorData;
using LuaSTGEditorSharp.Services;
using LuaSTGEditorSharp;
using System.IO;
using System.Linq;
using System;
namespace LuaSTGEditorAva.Input
{
    public partial class PathWindow : AvaloniaInputWindow
    {
        private readonly string docDirectory;
        private readonly string fileFilter;

        public PathWindow(string s, string wpfFilter, AttrItem owner)
        {
            InitializeComponent();
            Result = s;
            fileFilter = wpfFilter;
            try
            {
                docDirectory = Path.GetDirectoryName(owner?.Parent?.parentWorkSpace?.DocPath);
            }
            catch
            {
                docDirectory = "";
            }

            var box = this.FindControl<TextBox>("PathBox");
            box.Text = Result;
            box.TextChanged += (sender, e) => Result = box.Text;
            this.FindControl<Button>("BrowseBtn").Click += Browse_Click;
            this.FindControl<Button>("OkBtn").Click += (sender, e) => Accept();
            this.FindControl<Button>("CancelBtn").Click += (sender, e) => Cancel();
            Opened += (sender, e) => box.Focus();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private static List<FilePickerFileType> ParseFilter(string wpfFilter)
        {
            var types = new List<FilePickerFileType>();
            try
            {
                string[] parts = wpfFilter.Split('|');
                for (int i = 0; i + 1 < parts.Length; i += 2)
                {
                    string[] patterns = parts[i + 1].Split(';')
                        .Select(p => p.Trim())
                        .Where(p => p.Length > 0)
                        .ToArray();
                    if (patterns.Length == 0) continue;
                    types.Add(new FilePickerFileType(parts[i].Trim()) { Patterns = patterns });
                }
            }
            catch
            {
            }
            if (types.Count == 0)
                types.Add(new FilePickerFileType("All files") { Patterns = new[] { "*" } });
            return types;
        }

        private async void Browse_Click(object sender, RoutedEventArgs e)
        {
            var box = this.FindControl<TextBox>("PathBox");
            IReadOnlyList<IStorageFile> files;
            try
            {
                files = await this.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = Title,
                    AllowMultiple = false,
                    FileTypeFilter = ParseFilter(fileFilter)
                });
            }
            catch
            {
                return;
            }
            if (files.Count == 0) return;
            string picked = files[0].Path.LocalPath;
            if (!string.IsNullOrEmpty(docDirectory) && picked.Contains(docDirectory))
                box.Text = RelativePathConverter.GetRelativePath(docDirectory, picked);
            else
                box.Text = picked;
            try
            {
                if (EditorAppContext.CurrentSettings != null)
                    EditorAppContext.CurrentSettings.SLDir = Path.GetDirectoryName(picked);
            }
            catch
            {
            }
        }
    }
}
