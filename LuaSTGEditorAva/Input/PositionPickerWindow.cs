using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia;
using System.Collections.Generic;using Avalonia.Media;using Avalonia.Input;using Avalonia.Data;using Avalonia.Controls.Templates;using Avalonia.Controls.Primitives;using System.Linq;
using System.Collections.ObjectModel;
namespace LuaSTGEditorAva.Input
{
    public class PositionPickerWindow : AvaloniaInputWindow
    {
        private string decomposedX = "";
        private string decomposedY = "";
        private VecRow selectedVec;
        private readonly ObservableCollection<VecRow> decomposedVectors = new ObservableCollection<VecRow>();

        private TextBox resultBox;
        private TextBox xBox;
        private TextBox yBox;
        private ListBox vecList;
        private TextBox curXBox;
        private TextBox curYBox;

        public PositionPickerWindow(string s)
        {
            Title = "Position";
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
                ReCalcDecomposition();
                ReCalcPolynomial();
                RaisePropertyChanged("DecomposedX");
                RaisePropertyChanged("DecomposedY");
                SyncBoxes();
            }
        }

        public string DecomposedX
        {
            get => decomposedX;
            set
            {
                decomposedX = value;
                RaisePropertyChanged("DecomposedX");
                ReCalcResult();
                RaisePropertyChanged("ResultTXT");
                ReCalcPolynomial();
                SyncBoxes();
            }
        }

        public string DecomposedY
        {
            get => decomposedY;
            set
            {
                decomposedY = value;
                RaisePropertyChanged("DecomposedY");
                ReCalcResult();
                RaisePropertyChanged("ResultTXT");
                ReCalcPolynomial();
                SyncBoxes();
            }
        }

        public string CurrentX
        {
            get => selectedVec?.X;
            set
            {
                if (selectedVec == null) return;
                selectedVec.X = value;
                decomposedX = VecMath.MergeComponent(decomposedVectors, true);
                ReCalcResult();
                RaisePropertyChanged("CurrentX");
                RefreshVecList();
                RaisePropertyChanged("DecomposedX");
                RaisePropertyChanged("ResultTXT");
                SyncBoxes();
            }
        }

        public string CurrentY
        {
            get => selectedVec?.Y;
            set
            {
                if (selectedVec == null) return;
                selectedVec.Y = value;
                decomposedY = VecMath.MergeComponent(decomposedVectors, false);
                ReCalcResult();
                RaisePropertyChanged("CurrentY");
                RefreshVecList();
                RaisePropertyChanged("DecomposedY");
                RaisePropertyChanged("ResultTXT");
                SyncBoxes();
            }
        }

        private void ReCalcResult()
        {
            Result = decomposedX + "," + decomposedY;
        }

        private void ReCalcDecomposition()
        {
            var pos = Separate(Result);
            decomposedX = pos.Count > 0 ? pos[0] : "";
            decomposedY = pos.Count > 1 ? pos[1] : "";
        }

        private void ReCalcPolynomial()
        {
            decomposedVectors.Clear();
            var xPoly = VecMath.SeparatePolynomial(decomposedX ?? "");
            var yPoly = VecMath.SeparatePolynomial(decomposedY ?? "");
            int maxTerm = xPoly.Count > yPoly.Count ? xPoly.Count : yPoly.Count;
            for (int i = 0; i < maxTerm; i++)
                decomposedVectors.Add(new VecRow
                {
                    X = i < xPoly.Count ? xPoly[i] : "0",
                    Y = i < yPoly.Count ? yPoly[i] : "0"
                });
        }

        private void RefreshVecList()
        {
            object keep = vecList.SelectedItem;
            vecList.ItemsSource = null;
            vecList.ItemsSource = decomposedVectors;
            vecList.SelectedItem = keep;
        }

        private void SyncBoxes()
        {
            if (resultBox != null && resultBox.Text != Result) resultBox.Text = Result;
            if (xBox != null && xBox.Text != decomposedX) xBox.Text = decomposedX;
            if (yBox != null && yBox.Text != decomposedY) yBox.Text = decomposedY;
            if (curXBox != null && curXBox.Text != CurrentX) curXBox.Text = CurrentX ?? "";
            if (curYBox != null && curYBox.Text != CurrentY) curYBox.Text = CurrentY ?? "";
        }

        private void BuildUi()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            resultBox = new TextBox { Watermark = "x, y" };
            resultBox.TextChanged += (sender, e) =>
            {
                if (resultBox.Text != Result) ResultTXT = resultBox.Text;
            };
            Grid.SetRow(resultBox, 0);
            grid.Children.Add(resultBox);

            var xy = new Grid();
            xy.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            xy.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            xBox = new TextBox { Watermark = "X", Margin = new Thickness(0, 4, 2, 0) };
            xBox.TextChanged += (sender, e) => { if (xBox.Text != decomposedX) DecomposedX = xBox.Text; };
            yBox = new TextBox { Watermark = "Y", Margin = new Thickness(2, 4, 0, 0) };
            yBox.TextChanged += (sender, e) => { if (yBox.Text != decomposedY) DecomposedY = yBox.Text; };
            Grid.SetColumn(xBox, 0);
            Grid.SetColumn(yBox, 1);
            xy.Children.Add(xBox);
            xy.Children.Add(yBox);
            Grid.SetRow(xy, 1);
            grid.Children.Add(xy);

            vecList = new ListBox
            {
                ItemsSource = decomposedVectors,
                Margin = new Thickness(0, 4, 0, 0),
                ItemTemplate = new FuncDataTemplate<VecRow>((r, ns) =>
                    new TextBlock { [!TextBlock.TextProperty] = new Binding("Display") })
            };
            vecList.SelectionChanged += (sender, e) =>
            {
                selectedVec = vecList.SelectedItem as VecRow;
                RaisePropertyChanged("CurrentX");
                RaisePropertyChanged("CurrentY");
                SyncBoxes();
            };
            Grid.SetRow(vecList, 2);
            grid.Children.Add(vecList);

            var cur = new Grid();
            cur.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            cur.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            curXBox = new TextBox { Watermark = "Term X", Margin = new Thickness(0, 4, 2, 0), Tag = "X" };
            curXBox.TextChanged += (sender, e) => { if (curXBox.Text != CurrentX) CurrentX = curXBox.Text; };
            curYBox = new TextBox { Watermark = "Term Y", Margin = new Thickness(2, 4, 0, 0), Tag = "Y" };
            curYBox.TextChanged += (sender, e) => { if (curYBox.Text != CurrentY) CurrentY = curYBox.Text; };
            Grid.SetColumn(curXBox, 0);
            Grid.SetColumn(curYBox, 1);
            cur.Children.Add(curXBox);
            cur.Children.Add(curYBox);
            Grid.SetRow(cur, 3);
            grid.Children.Add(cur);

            var tools = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 8, 0, 0) };
            var canvasBtn = new Button { Content = "Canvas..." };
            canvasBtn.Click += Canvas_Click;
            var vectorBtn = new Button { Content = "Vector..." };
            vectorBtn.Click += Vector_Click;
            tools.Children.Add(canvasBtn);
            tools.Children.Add(vectorBtn);
            Grid.SetRow(tools, 4);
            grid.Children.Add(tools);

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
            panel.Children.Add(grid);
            Content = panel;
            Opened += (sender, e) => resultBox.Focus();
        }

        private async void Canvas_Click(object sender, RoutedEventArgs e)
        {
            var pe = new PositionCanvasWindow(PositionCanvasWindow.CanvasMode.Point);
            bool? ok = await pe.ShowDialogAsync(this);
            if (ok == true)
            {
                DecomposedX += (pe.SelectedX < 0 || (DecomposedX ?? "").Trim().Length <= 0
                    ? pe.SelectedX.ToString() : "+" + pe.SelectedX);
                DecomposedY += (pe.SelectedY < 0 || (DecomposedY ?? "").Trim().Length <= 0
                    ? pe.SelectedY.ToString() : "+" + pe.SelectedY);
            }
        }

        private async void Vector_Click(object sender, RoutedEventArgs e)
        {
            var ve = new PositionCanvasWindow(PositionCanvasWindow.CanvasMode.Vector);
            bool? ok = await ve.ShowDialogAsync(this);
            if (ok == true)
            {
                DecomposedX += (ve.OffsetX < 0 || (DecomposedX ?? "").Trim().Length <= 0
                    ? ve.OffsetX.ToString() : "+" + ve.OffsetX);
                DecomposedY += (ve.OffsetY < 0 || (DecomposedY ?? "").Trim().Length <= 0
                    ? ve.OffsetY.ToString() : "+" + ve.OffsetY);
            }
        }
    }

    public sealed class VectorPickerWindow : PositionPickerWindow
    {
        public VectorPickerWindow(string s)
            : base(s)
        {
            Title = "Vector";
        }
    }
}
