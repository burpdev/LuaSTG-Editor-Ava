using System;

namespace LuaSTGEditorSharp.Services
{
    public interface IDialogService
    {
        void ShowError(string message, string title = "LuaSTG Editor Ava");
    }
}
