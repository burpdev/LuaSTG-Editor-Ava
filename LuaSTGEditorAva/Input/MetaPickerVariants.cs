using Avalonia.Controls;
using LuaSTGEditorSharp.EditorData.Document.Meta;
using LuaSTGEditorSharp.EditorData.Document;
using LuaSTGEditorSharp.EditorData;
using LuaSTGEditorSharp.Services;
using System.Collections.Generic;using Avalonia.Media;using Avalonia.Layout;using Avalonia.Interactivity;using Avalonia.Input;using Avalonia.Data;using Avalonia.Controls.Templates;using Avalonia.Controls.Primitives;using Avalonia;using LuaSTGEditorAva.Services;
namespace LuaSTGEditorAva.Input
{
    public class AudioPickerWindow : DefPickerWindow
    {
        private readonly ManagedAudioPlayer player = new ManagedAudioPlayer();
        private TextBlock statusText;

        public AudioPickerWindow(string title, MetaType type, AttrItem item, bool useDifficulty)
            : base(title, type, item, useDifficulty)
        {
            Closed += (sender, e) => player.Dispose();
        }

        protected override bool ShowPreviewButton() => true;

        protected override void OnPreview()
        {
            MetaModel m = PickList.SelectedItem as MetaModel;
            if (m == null)
            {
                if (statusText != null) statusText.Text = "Select a sound entry first.";
                return;
            }
            string error = player.Play(m.ExInfo1);
            if (error != null)
                EditorAppContext.Dialogs.ShowError(error, "Preview");
        }

        protected override Control CreatePreview()
        {
            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8
            };
            var playBtn = new Button { Content = "Play", MinWidth = 70 };
            playBtn.Click += (sender, e) => OnPreview();
            var stopBtn = new Button { Content = "Stop", MinWidth = 70 };
            stopBtn.Click += (sender, e) => player.Stop();
            buttons.Children.Add(playBtn);
            buttons.Children.Add(stopBtn);

            statusText = new TextBlock
            {
                Margin = new Thickness(0, 6, 0, 0),
                Opacity = 0.75,
                TextWrapping = TextWrapping.Wrap
            };
            player.StatusChanged += message => statusText.Text = message;

            var panel = new StackPanel { Orientation = Orientation.Vertical };
            panel.Children.Add(buttons);
            panel.Children.Add(statusText);
            return panel;
        }
    }

    public sealed class BgmPickerWindow : AudioPickerWindow
    {
        public BgmPickerWindow(string s, AttrItem item)
            : base("BGM", MetaType.BGMLoad, item, false)
        {
            SetInitial(s);
        }
    }

    public sealed class SePickerWindow : AudioPickerWindow
    {
        public SePickerWindow(string s, AttrItem item)
            : base("Sound effect", MetaType.SELoad, item, false)
        {
            SetInitial(s);
        }
    }

    public class TextFilePickerWindow : DefPickerWindow
    {
        private TextBox previewBox;

        public TextFilePickerWindow(string title, MetaType type, AttrItem item)
            : base(title, type, item, false)
        {
        }

        protected override Control CreatePreview()
        {
            previewBox = new TextBox
            {
                IsReadOnly = true,
                AcceptsReturn = true,
                MaxHeight = 150,
                FontFamily = new FontFamily("DejaVu Sans Mono,Consolas,monospace")
            };
            return previewBox;
        }

        protected override void OnSelected(MetaModel m)
        {
            base.OnSelected(m);
            if (previewBox == null || m == null) return;
            try
            {
                string text = System.IO.File.ReadAllText(m.ExInfo1);
                previewBox.Text = text.Length > 4000 ? text.Substring(0, 4000) + "\n..." : text;
            }
            catch (System.Exception ex)
            {
                previewBox.Text = $"Failed to load file \"{m.ExInfo1}\".\n{ex.Message}";
            }
        }
    }

    public sealed class FontPickerWindow : TextFilePickerWindow
    {
        public FontPickerWindow(string s, AttrItem item)
            : base("Font", MetaType.FontLoad, item)
        {
            SetInitial(s);
        }
    }

    public sealed class FxPickerWindow : TextFilePickerWindow
    {
        public FxPickerWindow(string s, AttrItem item)
            : base("Shader", MetaType.FXLoad, item)
        {
            SetInitial(s);
        }
    }

    public sealed class TtfPickerWindow : DefPickerWindow
    {
        public TtfPickerWindow(string s, AttrItem item)
            : base("TTF font", MetaType.TTFLoad, item, false)
        {
            SetInitial(s);
        }

        protected override Control CreatePreview()
        {
            return new TextBlock
            {
                Text = "Archive preview is disabled for TTF fonts.",
                Opacity = 0.6
            };
        }
    }

    public sealed class ModelPickerWindow : DefPickerWindow
    {
        public ModelPickerWindow(string s, AttrItem item)
            : base("3D model", MetaType.ModelLoad, item, false)
        {
            SetInitial(s);
        }
    }

    public sealed class NodeDefPickerWindow : DefPickerWindow
    {
        public NodeDefPickerWindow(string s, AttrItem item)
            : base("Choose node", MetaType.UserDefined, item, true)
        {
            SetInitial(s);
        }
    }

    public sealed class BossDefPickerWindow : DefPickerWindow
    {
        public BossDefPickerWindow(string s, AttrItem item)
            : base("Choose Boss", MetaType.Boss, item, true)
        {
            SetInitial(s);
        }
    }

    public sealed class BossBgPickerWindow : DefPickerWindow
    {
        public BossBgPickerWindow(string s, AttrItem item)
            : base("Choose Boss background", MetaType.BossBG, item, false)
        {
            SetInitial(s);
        }
    }
}
