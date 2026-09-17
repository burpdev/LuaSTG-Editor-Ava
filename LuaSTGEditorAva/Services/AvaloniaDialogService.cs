using System;
using Avalonia.Controls;
using LuaSTGEditorSharp.Services;

namespace LuaSTGEditorAva.Services
{
    public sealed class AvaloniaDialogService : IDialogService
    {
        private readonly Func<Window> resolveOwner;

        public AvaloniaDialogService(Func<Window> resolveOwner)
        {
            this.resolveOwner = resolveOwner;
        }

        public void ShowError(string message, string title = "LuaSTG Editor Ava")
        {
            try
            {
                var owner = resolveOwner?.Invoke();
                var dialog = new ErrorDialog(title, message);
                if (owner != null)
                    _ = dialog.ShowDialog<bool>(owner);
                else
                    dialog.Show();
            }
            catch
            {
                Console.Error.WriteLine($"[{title}] {message}");
            }
        }
    }
}
