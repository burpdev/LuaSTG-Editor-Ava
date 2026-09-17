using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;using Avalonia.Interactivity;using Avalonia.Input;using Avalonia.Data;using Avalonia.Controls.Templates;using Avalonia.Controls.Primitives;using System.Collections.ObjectModel;
using Avalonia;
using LuaSTGEditorSharp.EditorData.Document;
using LuaSTGEditorSharp.EditorData;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
namespace LuaSTGEditorAva.Input
{
    public sealed class ParamRow : INotifyPropertyChanged
    {
        private string name = "";
        private string value = "";

        public string Name
        {
            get => name;
            set { name = value; RaisePropertyChanged("Name"); }
        }

        public string Value
        {
            get => value;
            set { this.value = value; RaisePropertyChanged("Value"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }

    public sealed class ParamEditorWindow : AvaloniaInputWindow
    {
        private readonly ObservableCollection<ParamRow> rows = new ObservableCollection<ParamRow>();
        private bool combining;

        public ParamEditorWindow(AttrItem original, MetaType type, string s)
        {
            Title = "Parameters";
            Width = 560;
            Height = 440;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            List<string> paramNames = new List<string>();
            try
            {
                AbstractMetaData metaData = original.Parent.parentWorkSpace.Meta;
                paramNames = Separate(metaData.aggregatableMetas[(int)type]
                    .FindOfName(original.Parent.NonMacrolize(0).Trim('"'))?.GetParam());
            }
            catch
            {
            }
            Decompose(s ?? string.Empty, paramNames);

            var list = new ListBox { ItemsSource = rows };
            list.ItemTemplate = new FuncDataTemplate<ParamRow>((r, ns) =>
            {
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition(150, GridUnitType.Pixel));
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                var name = new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                name.Bind(TextBlock.TextProperty,
                    new Binding("Name") { Mode = BindingMode.OneWay });
                var value = new TextBox();
                value.Bind(TextBox.TextProperty,
                    new Binding("Value")
                    {
                        Mode = BindingMode.TwoWay,
                        UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
                    });
                value.LostFocus += (sender, e) => Combine();
                Grid.SetColumn(name, 0);
                Grid.SetColumn(value, 1);
                grid.Children.Add(name);
                grid.Children.Add(value);
                return grid;
            });

            var ok = new Button { Content = "OK", MinWidth = 90 };
            ok.Click += (sender, e) => { Combine(); Accept(); };
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
            panel.Children.Add(list);
            Content = panel;
        }

        private void Decompose(string s, List<string> paramNames)
        {
            rows.Clear();
            var parts = Separate(s);
            for (int i = 0; i < parts.Count; i++)
                rows.Add(new ParamRow
                {
                    Name = i < paramNames.Count ? paramNames[i] : "Parameter",
                    Value = parts[i].Trim()
                });
            for (int i = parts.Count; i < paramNames.Count; i++)
                rows.Add(new ParamRow { Name = paramNames[i], Value = "" });
            Combine();
        }

        private void Combine()
        {
            if (combining) return;
            combining = true;
            try
            {
                Result = string.Join(", ", rows.Select(r => r.Value));
            }
            finally
            {
                combining = false;
            }
        }
    }
}
