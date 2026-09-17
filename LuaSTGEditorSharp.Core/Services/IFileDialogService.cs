using System;

namespace LuaSTGEditorSharp.Services
{
    public interface IFileDialogService
    {
        string ShowSaveFileDialog(string initialDirectory, string filter, string fileName);
    }
}
