using Avalonia.Controls.Templates;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia;
using LuaSTGEditorSharp.EditorData.Document.Meta;
using LuaSTGEditorSharp.EditorData.Document;
using LuaSTGEditorSharp.EditorData;
using System.Collections.Generic;using Avalonia.Media;using Avalonia.Interactivity;using Avalonia.Controls.Primitives;using System.Linq;
using System.Collections.ObjectModel;
namespace LuaSTGEditorAva.Input
{
    public class DefPickerWindow : AvaloniaInputWindow
    {
        protected ObservableCollection<MetaModel> AllItems { get; private set; }
        protected ObservableCollection<MetaModel> FilteredItems { get; private set; }

        protected ListBox PickList { get; private set; }
        protected TextBox CodeBox { get; private set; }

        public DefPickerWindow(string title, MetaType type, AttrItem item, bool useDifficulty)
        {
            Title = title;
            Width = 640;
            Height = 480;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            string difficulty = "";
            try { difficulty = useDifficulty ? item.Parent.GetDifficulty() : ""; }
            catch { difficulty = ""; }
            AllItems = item.Parent.parentWorkSpace.Meta.aggregatableMetas[(int)type]
                .GetAllSimpleWithDifficulty(difficulty);
            FilteredItems = new ObservableCollection<MetaModel>(AllItems);

            var filter = new TextBox { Watermark = "Filter...", Margin = new Thickness(0, 0, 0, 8) };
            filter.TextChanged += (sender, e) => ApplyFilter(filter.Text);

            PickList = new ListBox
            {
                ItemsSource = FilteredItems,
                ItemTemplate = new FuncDataTemplate<MetaModel>((m, ns) =>
                    new TextBlock { [!TextBlock.TextProperty] = new Binding("FullName") })
            };
            PickList.SelectionChanged += (sender, e) => OnSelected(PickList.SelectedItem as MetaModel);
            PickList.DoubleTapped += (sender, e) => Accept();

            CodeBox = new TextBox { Margin = new Thickness(0, 8, 0, 0) };
            CodeBox.TextChanged += (sender, e) => Result = CodeBox.Text;

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
            if (ShowPreviewButton())
            {
                var previewBtn = new Button { Content = "Preview", MinWidth = 90 };
                previewBtn.Click += (sender, e) => OnPreview();
                buttons.Children.Add(previewBtn);
            }
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);

            var panel = new DockPanel { Margin = new Thickness(12) };
            panel.Children.Add(buttons);
            DockPanel.SetDock(buttons, Dock.Bottom);
            panel.Children.Add(CodeBox);
            DockPanel.SetDock(CodeBox, Dock.Bottom);
            Control preview = CreatePreview();
            if (preview != null)
            {
                var host = new ContentControl
                {
                    Content = preview,
                    MaxHeight = 170,
                    Margin = new Thickness(0, 8, 0, 0)
                };
                panel.Children.Add(host);
                DockPanel.SetDock(host, Dock.Bottom);
            }
            panel.Children.Add(filter);
            DockPanel.SetDock(filter, Dock.Top);
            panel.Children.Add(PickList);

            Content = panel;
            Opened += (sender, e) => CodeBox.Focus();
        }

        public void SetInitial(string s)
        {
            Result = s;
            CodeBox.Text = Result;
        }

        public DefPickerWindow Init(string s)
        {
            SetInitial(s);
            return this;
        }

        private void ApplyFilter(string text)
        {
            FilteredItems.Clear();
            foreach (MetaModel mm in AllItems.Where(mm => MatchFilter(mm.FullName, text)))
                FilteredItems.Add(mm);
        }

        protected virtual Control CreatePreview()
        {
            return null;
        }

        protected virtual bool ShowPreviewButton()
        {
            return false;
        }

        protected virtual void OnPreview()
        {
        }

        protected virtual void OnSelected(MetaModel m)
        {
            if (m == null) return;
            if (!string.IsNullOrEmpty(m.Result)) Result = m.Result;
            CodeBox.Text = Result;
        }
    }
}
