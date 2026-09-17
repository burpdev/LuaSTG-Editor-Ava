using System.Collections.Generic;
using System.Threading.Tasks;

using LuaSTGEditorSharp.EditorData;
using LuaSTGEditorSharp.Plugin;

using DanmakuRandomizer.Views;

namespace DanmakuRandomizer
{
    public class DanmakuRandomizerPluginTool : PluginTool
    {
        public override string Name => "Danmaku Randomizer";

        public async Task<PluginToolResult> ExecuteAsync(
            Avalonia.Controls.Window owner, LuaSTGEditorSharp.EditorData.TreeNode selected)
        {
            var window = new RandomizerWindow();
            await window.ShowDialog(owner);
            return new PluginToolResult()
            {
                clipBoard = window.ResultNodes,
                commands = new List<LuaSTGEditorSharp.EditorData.Command>(),
                newDocument = null
            };
        }
    }
}
