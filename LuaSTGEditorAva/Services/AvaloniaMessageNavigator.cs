using LuaSTGEditorSharp.EditorData;
using LuaSTGEditorSharp.Services;

namespace LuaSTGEditorAva.Services
{
    public sealed class AvaloniaMessageNavigator : IMessageNavigator
    {
        private readonly MainWindow mainWindow;

        public AvaloniaMessageNavigator(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        public void Reveal(TreeNode node)
        {
            mainWindow?.Reveal(node);
        }
    }
}
