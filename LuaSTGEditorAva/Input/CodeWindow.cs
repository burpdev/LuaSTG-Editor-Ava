using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using LuaSTGEditorAva.Services;
namespace LuaSTGEditorAva.Input
{
    public sealed class CodeWindow : MultilineWindow
    {
        public CodeWindow(string s)
            : base(s ?? string.Empty, "Code")
        {
            box.FontFamily = new FontFamily("DejaVu Sans Mono,Consolas,monospace");
            try
            {
                var def = LuaHighlighting.Definition;
                if (def != null) box.SyntaxHighlighting = def;
            }
            catch
            {
            }
            box.KeyDown += (sender, e) =>
            {
                if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.Enter)
                {
                    Result = box.Text;
                    Accept();
                }
            };
        }
    }
}
