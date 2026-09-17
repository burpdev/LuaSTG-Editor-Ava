using System;
using LuaSTGEditorSharp.EditorData;
using LuaSTGEditorSharp.EditorData.Document;
using LuaSTGEditorSharp.EditorData.Document.Meta;
using LuaSTGEditorSharp.EditorData.Interfaces;
using LuaSTGEditorSharp.EditorData.Node.Boss;
using LuaSTGEditorSharp.EditorData.Node.Stage;
using LuaSTGEditorSharp.Execution;
using LuaSTGEditorSharp.Plugin;
using LuaSTGEditorSharp.Windows;

namespace LuaSTGEditorSharp
{
    public class PluginEntry : AbstractPluginEntry
    {
        public PluginEntry() : base()
        {
            NodeTypeCache = new LuaSTGEditorSharp.EditorData.Node.NodeTypeCache();
            Execution = new HeadlessExecution();
        }

        public override string NodeAssemblyName => "LuaSTGNode.Legacy";

        public override IInputWindowSelectorRegister GetInputWindowSelectorRegister() => null;

        public override AbstractMetaData GetMetaData() => new MetaData();

        public override AbstractMetaData GetMetaData(IMetaInfoCollection[] meta) => new MetaData(meta);

        public override AbstractToolbox GetToolbox(IMainWindow mw) => null;

        public override IViewDefinition GetViewDefinitionWindow(DocumentData document) => null;

        public override Type[] StageNodeType => new Type[] { typeof(Stage) };

        public override Type[] BossSCNodeType => new Type[] { typeof(BossSpellCard) };

        public override int MetaInfoCollectionTypeCount => (int)MetaType.__max;

        public override int[][] MetaInfoCollectionWatchDict => new int[][]{
                new int[]{ (int)MetaType.UserDefined },
                new int[]{ (int)MetaType.StageGroup },
                new int[]{ (int)MetaType.Boss, (int)MetaType.Bullet, (int)MetaType.BossBG
                    , (int)MetaType.Laser, (int)MetaType.BentLaser, (int)MetaType.Object },
                new int[]{ (int)MetaType.Task },
                new int[]{ (int)MetaType.ImageLoad },
                new int[]{ (int)MetaType.ImageGroupLoad },
                new int[]{ (int)MetaType.BGMLoad },
                new int[]{ (int)MetaType.FXLoad },
                new int[]{ (int)MetaType.FontLoad },
                new int[]{ (int)MetaType.TTFLoad },
                new int[]{ (int)MetaType.Item}
            };

        public override string TargetLSTGVersion => "Headless (Linux)";

        private sealed class HeadlessExecution : LSTGExecution
        {
            public override void BeforeRun(ExecutionConfig config) { }
            public override void Run(Logger logger, Action end)
            {
                logger("Launching the game is not supported in headless mode.");
                end();
            }
            protected override string LogFileName => "log.txt";
        }
    }
}
