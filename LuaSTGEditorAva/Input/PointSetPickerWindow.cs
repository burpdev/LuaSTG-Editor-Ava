using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia;
using System.Collections.Generic;using Avalonia.Media;using Avalonia.Input;using Avalonia.Data;using Avalonia.Controls.Templates;using Avalonia.Controls.Primitives;using System.Linq;
using System.Collections.ObjectModel;
using System.Text;
namespace LuaSTGEditorAva.Input
{
    public sealed class PointSetPickerWindow : AvaloniaInputWindow
    {
        private readonly ObservableCollection<VecRow> pairs = new ObservableCollection<VecRow>();
        private VecRow selected;
        private bool focusIsY;
        private TextBox resultBox;
        private ListBox pairList;
        private TextBox curXBox;
        private TextBox curYBox;

        public PointSetPickerWindow(string s)
        {
            Title = "Point set";
            Width = 560;
            Height = 520;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            BuildUi();
            ResultTXT = s ?? string.Empty;
        }

        public string ResultTXT
        {
            get => Result;
            set
            {
                Result = value;
                RaisePropertyChanged("ResultTXT");
                ReCalcPointSet();
                SyncBoxes();
            }
        }

        public string CurrentX
        {
            get => selected?.X;
            set
            {
                if (selected == null) return;
                selected.X = value;
                ReCalcResult();
                RaisePropertyChanged("CurrentX");
                RefreshList();
                SyncBoxes();
            }
        }

        public string CurrentY
        {
            get => selected?.Y;
            set
            {
                if (selected == null) return;
                selected.Y = value;
                ReCalcResult();
                RaisePropertyChanged("CurrentY");
                RefreshList();
                SyncBoxes();
            }
        }

        private void ReCalcResult()
        {
            var sb = new StringBuilder();
            bool first = true;
            foreach (VecRow sv in pairs)
            {
                if (!first) sb.Append(", ");
                sb.Append(sv.X);
                sb.Append(", ");
                sb.Append(sv.Y);
                first = false;
            }
            Result = sb.ToString();
        }

        private void ReCalcPointSet()
        {
            pairs.Clear();
            var xs = new System.Collections.Generic.List<string>();
            var ys = new System.Collections.Generic.List<string>();
            int id = 0;
            foreach (string part in Separate(ResultTXT))
            {
                string t = part.Trim();
                if (id % 2 == 0 && !string.IsNullOrEmpty(t)) xs.Add(t);
                else if (id % 2 != 0 && !string.IsNullOrEmpty(t)) ys.Add(t);
                id++;
            }
            int max = xs.Count > ys.Count ? xs.Count : ys.Count;
            for (int i = 0; i < max; i++)
                pairs.Add(new VecRow
                {
                    X = i < xs.Count ? xs[i] : "0",
                    Y = i < ys.Count ? ys[i] : "0"
                });
        }

        private void RefreshList()
        {
            object keep = pairList.SelectedItem;
            pairList.ItemsSource = null;
            pairList.ItemsSource = pairs;
            pairList.SelectedItem = keep;
        }

        private void SyncBoxes()
        {
            if (resultBox != null && resultBox.Text != Result) resultBox.Text = Result;
            if (curXBox != null && curXBox.Text != CurrentX) curXBox.Text = CurrentX ?? "";
            if (curYBox != null && curYBox.Text != CurrentY) curYBox.Text = CurrentY ?? "";
        }

        private void BuildUi()
        {
            resultBox = new TextBox { Watermark = "x1, y1, x2, y2, ..." };
            resultBox.TextChanged += (sender, e) =>
            {
                if (resultBox.Text != Result) ResultTXT = resultBox.Text;
            };

            pairList = new ListBox
            {
                ItemsSource = pairs,
                Margin = new Thickness(0, 4, 0, 0),
                ItemTemplate = new FuncDataTemplate<VecRow>((r, ns) =>
                    new TextBlock { [!TextBlock.TextProperty] = new Binding("Display") })
            };
            pairList.SelectionChanged += (sender, e) =>
            {
                selected = pairList.SelectedItem as VecRow;
                RaisePropertyChanged("CurrentX");
                RaisePropertyChanged("CurrentY");
                SyncBoxes();
            };

            curXBox = new TextBox { Watermark = "Pair X", Margin = new Thickness(0, 4, 2, 0) };
            curXBox.TextChanged += (sender, e) => { if (curXBox.Text != CurrentX) CurrentX = curXBox.Text; };
            curXBox.GotFocus += (sender, e) => focusIsY = false;
            curYBox = new TextBox { Watermark = "Pair Y", Margin = new Thickness(2, 4, 0, 0) };
            curYBox.TextChanged += (sender, e) => { if (curYBox.Text != CurrentY) CurrentY = curYBox.Text; };
            curYBox.GotFocus += (sender, e) => focusIsY = true;
            var cur = new Grid();
            cur.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            cur.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            Grid.SetColumn(curXBox, 0);
            Grid.SetColumn(curYBox, 1);
            cur.Children.Add(curXBox);
            cur.Children.Add(curYBox);

            var tools = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 8, 0, 0) };
            var addBtn = new Button { Content = "Add" };
            addBtn.Click += (sender, e) =>
            {
                pairs.Add(new VecRow { X = "0", Y = "0" });
                ReCalcResult();
                SyncBoxes();
            };
            var delBtn = new Button { Content = "Delete" };
            delBtn.Click += (sender, e) =>
            {
                if (pairList.SelectedItem is VecRow row)
                {
                    pairs.Remove(row);
                    ReCalcResult();
                    SyncBoxes();
                }
            };
            var canvasBtn = new Button { Content = "Canvas..." };
            canvasBtn.Click += Canvas_Click;
            var vectorBtn = new Button { Content = "Vector..." };
            vectorBtn.Click += Vector_Click;
            var syncXY = new Button { Content = "Sync X/Y" };
            syncXY.Click += (sender, e) => DoSync(xy: true);
            var syncTri = new Button { Content = "Sync sin/cos" };
            syncTri.Click += (sender, e) => DoSync(xy: false);
            tools.Children.Add(addBtn);
            tools.Children.Add(delBtn);
            tools.Children.Add(canvasBtn);
            tools.Children.Add(vectorBtn);
            tools.Children.Add(syncXY);
            tools.Children.Add(syncTri);

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

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            Grid.SetRow(resultBox, 0);
            Grid.SetRow(pairList, 1);
            Grid.SetRow(cur, 2);
            Grid.SetRow(tools, 3);
            grid.Children.Add(resultBox);
            grid.Children.Add(pairList);
            grid.Children.Add(cur);
            grid.Children.Add(tools);

            var panel = new DockPanel { Margin = new Thickness(12) };
            panel.Children.Add(buttons);
            DockPanel.SetDock(buttons, Dock.Bottom);
            panel.Children.Add(grid);
            Content = panel;
            Opened += (sender, e) => resultBox.Focus();
        }

        private void DoSync(bool xy)
        {
            bool? dir = VecMath.SyncDirection(CurrentX, CurrentY, focusIsY);
            if (dir == null) return;
            if (xy)
            {
                if (dir == true) CurrentX = VecMath.SyncXY(CurrentY, true);
                else CurrentY = VecMath.SyncXY(CurrentX, false);
            }
            else
            {
                if (dir == true) CurrentX = VecMath.SyncTri(CurrentY, true);
                else CurrentY = VecMath.SyncTri(CurrentX, false);
            }
        }

        private async void Canvas_Click(object sender, RoutedEventArgs e)
        {
            var pe = new PositionCanvasWindow(PositionCanvasWindow.CanvasMode.Point);
            bool? ok = await pe.ShowDialogAsync(this);
            if (ok == true) ResultTXT += ", " + pe.SelectedX + ", " + pe.SelectedY;
        }

        private async void Vector_Click(object sender, RoutedEventArgs e)
        {
            var ve = new PositionCanvasWindow(PositionCanvasWindow.CanvasMode.Vector);
            bool? ok = await ve.ShowDialogAsync(this);
            if (ok == true) ResultTXT += ", " + ve.OffsetX + ", " + ve.OffsetY;
        }
    }
}
