using Avalonia.Controls.Shapes;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System.Collections.Generic;using Avalonia.Interactivity;using Avalonia.Data;using Avalonia.Controls.Templates;using Avalonia.Controls.Primitives;using Avalonia;
using System;
using LuaSTGEditorAva.Services;
namespace LuaSTGEditorAva.Input
{
    public sealed class PositionCanvasWindow : AvaloniaInputWindow
    {
        public enum CanvasMode { Point, Vector }

        private readonly CanvasMode mode;
        private readonly Canvas canvas;
        private readonly Ellipse cursor;
        private readonly Line vectorLine;
        private readonly TextBox readoutX;
        private readonly TextBox readoutY;
        private TextBox vecFromX;
        private TextBox vecFromY;
        private TextBox vecToX;
        private TextBox vecToY;
        private TextBox vecOffX;
        private TextBox vecOffY;
        private TextBox vecRadius;
        private TextBox vecTheta;
        private bool? clipTo10;
        private bool dragging;

        public double SelectedX { get; private set; }
        public double SelectedY { get; private set; }
        public double OffsetX { get; private set; }
        public double OffsetY { get; private set; }

        public PositionCanvasWindow(CanvasMode mode = CanvasMode.Point, double startX = 0, double startY = 0)
        {
            this.mode = mode;
            Title = mode == CanvasMode.Point ? "Position Editor" : "Vector Editor";
            Width = 668;
            Height = mode == CanvasMode.Vector ? 760 : 640;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            SelectedX = startX;
            SelectedY = startY;
            if (mode == CanvasMode.Vector)
            {
                OffsetX = startX;
                OffsetY = startY;
            }

            canvas = new Canvas
            {
                Width = 640,
                Height = 480,
                Background = new SolidColorBrush(Color.FromRgb(32, 32, 40)),
                ClipToBounds = true
            };
            DrawMockBackdrop();
            DrawStageGrid();

            cursor = new Ellipse
            {
                Width = 11,
                Height = 11,
                Stroke = new SolidColorBrush(Colors.AliceBlue),
                StrokeThickness = 2
            };
            canvas.Children.Add(cursor);
            vectorLine = new Line
            {
                Stroke = new SolidColorBrush(Colors.Yellow),
                StrokeThickness = 2,
                IsVisible = mode == CanvasMode.Vector
            };
            canvas.Children.Add(vectorLine);
            if (mode == CanvasMode.Vector)
            {
                var origin = new Ellipse
                {
                    Width = 11,
                    Height = 11,
                    Fill = new SolidColorBrush(Colors.AliceBlue),
                    Stroke = new SolidColorBrush(Colors.Black),
                    StrokeThickness = 1
                };
                Canvas.SetLeft(origin, 224 - origin.Width / 2);
                Canvas.SetTop(origin, 240 - origin.Height / 2);
                canvas.Children.Add(origin);
            }

            canvas.PointerPressed += Canvas_Pressed;
            canvas.PointerMoved += Canvas_Moved;
            canvas.PointerReleased += (sender, e) => dragging = false;

            readoutX = new TextBox { IsReadOnly = true, Width = 110 };
            readoutY = new TextBox { IsReadOnly = true, Width = 110 };

            var none = new RadioButton { Content = "No clip", IsChecked = true, Margin = new Thickness(0, 0, 8, 0) };
            var intClip = new RadioButton { Content = "Int", Margin = new Thickness(0, 0, 8, 0) };
            var tenClip = new RadioButton { Content = "10", Margin = new Thickness(0, 0, 8, 0) };
            none.Checked += (sender, e) => clipTo10 = null;
            intClip.Checked += (sender, e) => clipTo10 = false;
            tenClip.Checked += (sender, e) => clipTo10 = true;
            var clips = new StackPanel { Orientation = Orientation.Horizontal };
            clips.Children.Add(none);
            clips.Children.Add(intClip);
            clips.Children.Add(tenClip);

            var readoutRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 8, 0, 0) };
            readoutRow.Children.Add(new TextBlock { Text = "X:", VerticalAlignment = VerticalAlignment.Center });
            readoutRow.Children.Add(readoutX);
            readoutRow.Children.Add(new TextBlock { Text = "Y:", VerticalAlignment = VerticalAlignment.Center });
            readoutRow.Children.Add(readoutY);

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
            if (mode == CanvasMode.Vector)
            {
                Control vecPanel = BuildVectorPanel();
                panel.Children.Add(vecPanel);
                DockPanel.SetDock(vecPanel, Dock.Bottom);
            }
            else
            {
                panel.Children.Add(readoutRow);
                DockPanel.SetDock(readoutRow, Dock.Bottom);
            }
            panel.Children.Add(clips);
            DockPanel.SetDock(clips, Dock.Bottom);
            panel.Children.Add(canvas);

            Content = panel;
            RefreshCursor();
        }

        private static TextBox ReadoutBox()
        {
            return new TextBox { IsReadOnly = true };
        }

        private static TextBlock ReadoutLabel(string text)
        {
            return new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center };
        }

        private Control BuildVectorPanel()
        {
            vecFromX = ReadoutBox();
            vecFromY = ReadoutBox();
            vecToX = ReadoutBox();
            vecToY = ReadoutBox();
            vecOffX = ReadoutBox();
            vecOffY = ReadoutBox();
            vecRadius = ReadoutBox();
            vecTheta = ReadoutBox();

            var toRow = new Grid { Margin = new Thickness(0, 8, 0, 0) };
            toRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            toRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            toRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            toRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            toRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            toRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            toRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            AddToGrid(toRow, vecFromX, 0);
            AddToGrid(toRow, ReadoutLabel(","), 1);
            AddToGrid(toRow, vecFromY, 2);
            AddToGrid(toRow, ReadoutLabel("To"), 3);
            AddToGrid(toRow, vecToX, 4);
            AddToGrid(toRow, ReadoutLabel(","), 5);
            AddToGrid(toRow, vecToY, 6);

            var offRow = new Grid { Margin = new Thickness(0, 4, 0, 0) };
            offRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            offRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            offRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            offRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            AddToGrid(offRow, ReadoutLabel("Offset"), 0);
            AddToGrid(offRow, vecOffX, 1);
            AddToGrid(offRow, ReadoutLabel(","), 2);
            AddToGrid(offRow, vecOffY, 3);

            var polarRow = new Grid { Margin = new Thickness(0, 4, 0, 0) };
            polarRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            polarRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            polarRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            polarRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            AddToGrid(polarRow, ReadoutLabel("Radius"), 0);
            AddToGrid(polarRow, vecRadius, 1);
            AddToGrid(polarRow, ReadoutLabel("Theta"), 2);
            AddToGrid(polarRow, vecTheta, 3);

            var stack = new StackPanel();
            stack.Children.Add(toRow);
            stack.Children.Add(offRow);
            stack.Children.Add(polarRow);
            return stack;
        }

        private static void AddToGrid(Grid grid, Control control, int column)
        {
            Grid.SetColumn(control, column);
            if (control is TextBlock label)
                label.Margin = new Thickness(6, 0, 6, 0);
            grid.Children.Add(control);
        }

        private void DrawMockBackdrop()
        {
            try
            {
                Bitmap backdrop = IconCache.GetIcon("/LuaSTGNode.Legacy;component/images/ref/levelRef.png");
                if (backdrop == null) return;
                canvas.Children.Add(new Image
                {
                    Source = backdrop,
                    Width = 640,
                    Height = 480
                });
            }
            catch
            {
            }
        }

        private void DrawStageGrid()
        {
            const double left = 32;
            const double top = 16;
            const double right = 416;
            const double bottom = 464;
            var grid = new SolidColorBrush(Color.FromArgb(70, 110, 110, 140));
            for (double x = left + 32; x < right; x += 32)
            {
                canvas.Children.Add(new Line
                {
                    StartPoint = new Point(x, top),
                    EndPoint = new Point(x, bottom),
                    Stroke = grid,
                    StrokeThickness = 1
                });
            }
            for (double y = top + 32; y < bottom; y += 32)
            {
                canvas.Children.Add(new Line
                {
                    StartPoint = new Point(left, y),
                    EndPoint = new Point(right, y),
                    Stroke = grid,
                    StrokeThickness = 1
                });
            }
            var axis = new SolidColorBrush(Color.FromRgb(120, 120, 150));
            var border = new SolidColorBrush(Color.FromRgb(160, 160, 180));
            canvas.Children.Add(new Line
            {
                StartPoint = new Point(224, top),
                EndPoint = new Point(224, bottom),
                Stroke = axis,
                StrokeThickness = 1
            });
            canvas.Children.Add(new Line
            {
                StartPoint = new Point(left, 240),
                EndPoint = new Point(right, 240),
                Stroke = axis,
                StrokeThickness = 1
            });
            var frame = new Rectangle
            {
                Width = right - left,
                Height = bottom - top,
                Stroke = border,
                StrokeThickness = 1,
                Fill = null
            };
            Canvas.SetLeft(frame, left);
            Canvas.SetTop(frame, top);
            canvas.Children.Add(frame);
        }

        private void Canvas_Pressed(object sender, PointerPressedEventArgs e)
        {
            Point p = e.GetPosition(canvas);
            dragging = true;
            if (mode == CanvasMode.Vector)
            {
                SelectedX = VecMath.ScrXToLstgX(p.X, clipTo10);
                SelectedY = VecMath.ScrYToLstgY(p.Y, clipTo10);
                OffsetX = SelectedX;
                OffsetY = SelectedY;
            }
            else
            {
                SelectedX = VecMath.ScrXToLstgX(p.X, clipTo10);
                SelectedY = VecMath.ScrYToLstgY(p.Y, clipTo10);
            }
            RefreshCursor();
        }

        private void Canvas_Moved(object sender, PointerEventArgs e)
        {
            if (!dragging) return;
            Point p = e.GetPosition(canvas);
            if (mode == CanvasMode.Vector)
            {
                SelectedX = VecMath.ScrXToLstgX(p.X, clipTo10);
                SelectedY = VecMath.ScrYToLstgY(p.Y, clipTo10);
                OffsetX = SelectedX;
                OffsetY = SelectedY;
                RefreshCursor();
            }
            else
            {
                SelectedX = VecMath.ScrXToLstgX(p.X, clipTo10);
                SelectedY = VecMath.ScrYToLstgY(p.Y, clipTo10);
                RefreshCursor();
            }
        }

        private void RefreshCursor()
        {
            double sx = VecMath.LstgXToScrX(SelectedX);
            double sy = VecMath.LstgYToScrY(SelectedY);
            Canvas.SetLeft(cursor, sx - cursor.Width / 2);
            Canvas.SetTop(cursor, sy - cursor.Height / 2);
            readoutX.Text = SelectedX.ToString();
            readoutY.Text = SelectedY.ToString();
            if (mode == CanvasMode.Vector)
            {
                vectorLine.StartPoint = new Point(224, 240);
                vectorLine.EndPoint = new Point(sx, sy);
                RefreshVectorBoxes();
            }
        }

        private void RefreshVectorBoxes()
        {
            if (vecFromX == null) return;
            vecFromX.Text = "0";
            vecFromY.Text = "0";
            vecToX.Text = SelectedX.ToString();
            vecToY.Text = SelectedY.ToString();
            vecOffX.Text = OffsetX.ToString();
            vecOffY.Text = OffsetY.ToString();
            vecRadius.Text = Math.Sqrt(OffsetX * OffsetX + OffsetY * OffsetY).ToString();
            vecTheta.Text = (Math.Atan2(OffsetY, OffsetX) / Math.PI * 180).ToString();
        }
    }
}
