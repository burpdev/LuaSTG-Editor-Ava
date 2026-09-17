using Avalonia.Controls;
using Avalonia.Interactivity;using Avalonia.Input;using Avalonia.Data;using Avalonia.Controls.Templates;using Avalonia.Controls.Primitives;using System.Collections.Generic;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia;
using System;
namespace LuaSTGEditorAva.Input
{
    public class ArgbPickerWindow : AvaloniaInputWindow
    {
        public struct HsvColor
        {
            public float H;
            public float S;
            public float V;

            public HsvColor(float h, float s, float v) { H = h; S = s; V = v; }
        }

        private byte a = 255;
        private float h;
        private float s = 100f;
        private float v = 100f;
        private string aStr = "";
        private string rStr = "";
        private string gStr = "";
        private string bStr = "";
        private Border preview;

        public bool AlphaUsed { get; set; }

        public ArgbPickerWindow(string s, bool alpha = true)
        {
            Title = alpha ? "ARGB color" : "RGB color";
            Width = 420;
            Height = alpha ? 380 : 340;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            AlphaUsed = alpha;
            Result = s;
            BuildUi();
            UpdateState();
        }

        public byte A
        {
            get => a;
            set
            {
                a = value;
                aStr = value.ToString();
                UpdateState();
                CombineResult();
                RaisePropertyChanged("A");
                RaisePropertyChanged("AValue");
                RaisePropertyChanged("AStr");
            }
        }

        public double AValue
        {
            get => a;
            set => A = (byte)Math.Max(0, Math.Min(255, Math.Round(value)));
        }

        public double HValue
        {
            get => h;
            set => H = (float)value;
        }

        public double SValue
        {
            get => s;
            set => S = (float)value;
        }

        public double VValue
        {
            get => v;
            set => V = (float)value;
        }

        public byte R
        {
            get => HsvToRgb(new HsvColor(h, s, v)).R;
            set
            {
                HsvColor hsv = RgbToHsv(Color.FromRgb(value, G, B));
                h = hsv.H;
                s = hsv.S;
                v = hsv.V;
                UpdateState();
                rStr = value.ToString();
                CombineResult();
            }
        }

        public byte G
        {
            get => HsvToRgb(new HsvColor(h, s, v)).G;
            set
            {
                HsvColor hsv = RgbToHsv(Color.FromRgb(R, value, B));
                h = hsv.H;
                s = hsv.S;
                v = hsv.V;
                UpdateState();
                gStr = value.ToString();
                CombineResult();
            }
        }

        public byte B
        {
            get => HsvToRgb(new HsvColor(h, s, v)).B;
            set
            {
                HsvColor hsv = RgbToHsv(Color.FromRgb(R, G, value));
                h = hsv.H;
                s = hsv.S;
                v = hsv.V;
                UpdateState();
                bStr = value.ToString();
                CombineResult();
            }
        }

        public float H
        {
            get => h;
            set
            {
                h = value;
                UpdateState();
                CombineResult();
            }
        }

        public float S
        {
            get => s;
            set
            {
                s = value;
                UpdateState();
                CombineResult();
            }
        }

        public float V
        {
            get => v;
            set
            {
                v = value;
                UpdateState();
                CombineResult();
            }
        }

        public string AStr
        {
            get => byte.TryParse(aStr, out byte _) || string.IsNullOrWhiteSpace(aStr) ? A.ToString() : aStr;
            set
            {
                if (byte.TryParse(value, out byte parsed)) A = parsed;
                else { aStr = value; CombineResult(); RaisePropertyChanged("AStr"); }
            }
        }

        public string RStr
        {
            get => byte.TryParse(rStr, out byte _) || string.IsNullOrWhiteSpace(rStr) ? R.ToString() : rStr;
            set
            {
                if (byte.TryParse(value, out byte parsed)) R = parsed;
                else { rStr = value; CombineResult(); RaisePropertyChanged("RStr"); }
            }
        }

        public string GStr
        {
            get => byte.TryParse(gStr, out byte _) || string.IsNullOrWhiteSpace(gStr) ? G.ToString() : gStr;
            set
            {
                if (byte.TryParse(value, out byte parsed)) G = parsed;
                else { gStr = value; CombineResult(); RaisePropertyChanged("GStr"); }
            }
        }

        public string BStr
        {
            get => byte.TryParse(bStr, out byte _) || string.IsNullOrWhiteSpace(bStr) ? B.ToString() : bStr;
            set
            {
                if (byte.TryParse(value, out byte parsed)) B = parsed;
                else { bStr = value; CombineResult(); RaisePropertyChanged("BStr"); }
            }
        }

        public override string Result
        {
            get => base.Result;
            set
            {
                base.Result = value;
                List<string> cs = Separate(Result);
                if (AlphaUsed)
                {
                    if (cs.Count >= 1) AStr = cs[0];
                    if (cs.Count >= 2) RStr = cs[1];
                    if (cs.Count >= 3) GStr = cs[2];
                    if (cs.Count >= 4) BStr = cs[3];
                }
                else
                {
                    if (cs.Count >= 1) RStr = cs[0];
                    if (cs.Count >= 2) GStr = cs[1];
                    if (cs.Count >= 3) BStr = cs[2];
                }
            }
        }

        public void CombineResult()
        {
            result = AlphaUsed
                ? AStr + "," + RStr + "," + GStr + "," + BStr
                : RStr + "," + GStr + "," + BStr;
            RaisePropertyChanged("Result");
        }

        public static Color HsvToRgb(HsvColor hsv)
        {
            hsv.H -= Convert.ToSingle(Math.Floor(hsv.H / 360) * 360);
            hsv.S /= 100;
            hsv.V /= 100;
            byte v = Convert.ToByte(hsv.V * 255);
            if (hsv.S == 0) return Color.FromArgb(255, v, v, v);
            int hh = Convert.ToInt32(Math.Floor(hsv.H / 60)) % 6;
            float f = hsv.H / 60 - hh;
            byte a = Convert.ToByte(v * (1 - hsv.S));
            byte b = Convert.ToByte(v * (1 - hsv.S * f));
            byte c = Convert.ToByte(v * (1 - hsv.S * (1 - f)));
            switch (hh)
            {
                case 0: return Color.FromArgb(255, v, c, a);
                case 1: return Color.FromArgb(255, b, v, a);
                case 2: return Color.FromArgb(255, a, v, c);
                case 3: return Color.FromArgb(255, a, b, v);
                case 4: return Color.FromArgb(255, c, a, v);
                default: return Color.FromArgb(255, v, a, b);
            }
        }

        public static HsvColor RgbToHsv(Color rgb)
        {
            HsvColor hsv = new HsvColor();
            byte max = Math.Max(rgb.R, rgb.G);
            max = Math.Max(max, rgb.B);
            byte min = Math.Min(rgb.R, rgb.G);
            min = Math.Min(min, rgb.B);
            hsv.V = max / 255f;
            int mm = max - min;
            hsv.S = max == 0 ? 0 : mm / (float)max;
            if (mm == 0) hsv.H = 0;
            else if (rgb.R == max) hsv.H = (rgb.G - rgb.B) / (float)mm * 60;
            else if (rgb.G == max) hsv.H = 120 + (rgb.B - rgb.R) / (float)mm * 60;
            else hsv.H = 240 + (rgb.R - rgb.G) / (float)mm * 60;
            if (hsv.H < 0) hsv.H += 360;
            hsv.S *= 100;
            hsv.V *= 100;
            return hsv;
        }

        private void UpdateState()
        {
            Color current = HsvToRgb(new HsvColor(h, s, v));
            current = Color.FromArgb(a, current.R, current.G, current.B);
            if (preview != null)
                preview.Background = new SolidColorBrush(current);
            RaisePropertyChanged("H");
            RaisePropertyChanged("S");
            RaisePropertyChanged("V");
            RaisePropertyChanged("HValue");
            RaisePropertyChanged("SValue");
            RaisePropertyChanged("VValue");
            RaisePropertyChanged("AValue");
            RaisePropertyChanged("RStr");
            RaisePropertyChanged("GStr");
            RaisePropertyChanged("BStr");
            RaisePropertyChanged("AStr");
        }

        private void BuildUi()
        {
            var grid = new Grid { Margin = new Thickness(4) };
            grid.ColumnDefinitions.Add(new ColumnDefinition(70, GridUnitType.Pixel));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            grid.ColumnDefinitions.Add(new ColumnDefinition(70, GridUnitType.Pixel));

            int row = 0;
            if (AlphaUsed) AddSliderRow(grid, row++, "Alpha", 0, 255, "AValue");
            AddSliderRow(grid, row++, "Hue", 0, 360, "HValue");
            AddSliderRow(grid, row++, "Saturation", 0, 100, "SValue");
            AddSliderRow(grid, row++, "Value", 0, 100, "VValue");
            AddChannelRow(grid, row++, "R", "RStr");
            AddChannelRow(grid, row++, "G", "GStr");
            AddChannelRow(grid, row++, "B", "BStr");
            if (AlphaUsed) AddChannelRow(grid, row++, "A", "AStr");

            preview = new Border
            {
                Height = 36,
                Margin = new Thickness(0, 8, 0, 0),
                BorderBrush = new SolidColorBrush(Color.FromArgb(255, 128, 128, 128)),
                BorderThickness = new Thickness(1)
            };
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            Grid.SetRow(preview, row);
            Grid.SetColumnSpan(preview, 3);
            grid.Children.Add(preview);

            var resultBox = new TextBox
            {
                Margin = new Thickness(0, 8, 0, 0),
                IsReadOnly = true
            };
            resultBox.Bind(TextBox.TextProperty,
                new Binding("Result") { Source = this, Mode = BindingMode.OneWay });
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            Grid.SetRow(resultBox, row + 1);
            Grid.SetColumnSpan(resultBox, 3);
            grid.Children.Add(resultBox);

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
        }

        private void AddSliderRow(Grid grid, int row, string label, double min, double max, string property)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var caption = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(caption, row);
            Grid.SetColumn(caption, 0);
            grid.Children.Add(caption);
            var slider = new Slider { Minimum = min, Maximum = max, Margin = new Thickness(4, 2) };
            slider.Bind(Slider.ValueProperty,
                new Binding(property) { Source = this, Mode = BindingMode.TwoWay });
            Grid.SetRow(slider, row);
            Grid.SetColumn(slider, 1);
            Grid.SetColumnSpan(slider, 2);
            grid.Children.Add(slider);
        }

        private void AddChannelRow(Grid grid, int row, string label, string property)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var caption = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(caption, row);
            Grid.SetColumn(caption, 0);
            grid.Children.Add(caption);
            var box = new TextBox { Margin = new Thickness(4, 2) };
            box.Bind(TextBox.TextProperty,
                new Binding(property)
                {
                    Source = this,
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.LostFocus
                });
            Grid.SetRow(box, row);
            Grid.SetColumn(box, 1);
            Grid.SetColumnSpan(box, 2);
            grid.Children.Add(box);
        }
    }
}
