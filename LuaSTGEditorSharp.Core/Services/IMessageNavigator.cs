using System;
using LuaSTGEditorSharp.EditorData;

namespace LuaSTGEditorSharp.Services
{
    public interface IMessageNavigator
    {
        void Reveal(TreeNode node);
    }
}
