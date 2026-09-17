using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LuaSTGEditorSharp.Services;
using LuaSTGEditorSharp.EditorData.Document;
using LuaSTGEditorSharp.EditorData.Interfaces;

namespace LuaSTGEditorSharp.EditorData.Message
{
    public class InvalidCustomNodeMessage : MessageBase
    {
        public string NodeFileName { get; set; }

        public override string SourceName { get => SourceDoc.RawDocName; }
        public override DocumentData SourceDoc { get => (Source as TreeNode).parentWorkSpace; }

        public InvalidCustomNodeMessage(string nodeFileName, IMessageThrowable source) : base(0, source)
        {
            NodeFileName = nodeFileName;
        }

        public override string ToString()
        {
            return "The custom node \"" + NodeFileName + "\" (CustomNodes/" + NodeFileName + ".lua) doesn't exist.";
        }

        public override object Clone()
        {
            return new InvalidCustomNodeMessage(NodeFileName, Source);
        }

        public override void Invoke()
        {
            TreeNode node = Source as TreeNode;
            if (EditorAppContext.MessageNavigator != null)
                EditorAppContext.MessageNavigator.Reveal(node);
            else
                EditorAppContext.MainWindow?.Reveal(node);
        }
    }
}
