using LuaSTGEditorSharp.Windows.Input;
using System.Collections.Generic;using Avalonia.Media;using Avalonia.Layout;using Avalonia.Interactivity;using Avalonia.Input;using Avalonia.Data;using Avalonia.Controls.Templates;using Avalonia.Controls.Primitives;using Avalonia;using Avalonia.Controls;
using System.Threading.Tasks;
namespace LuaSTGEditorAva.Input
{
    public interface IAvaloniaInputWindow : IInputWindow
    {
        Task<bool?> ShowDialogAsync(Window owner);
    }
}
