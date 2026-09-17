using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LuaSTGEditorSharp.Services;
using LuaSTGEditorSharp.EditorData.Interfaces;
using LuaSTGEditorSharp.EditorData.Document;

namespace LuaSTGEditorSharp.EditorData.Message
{
    public class RepeatedNameMessage : MessageBase
    {
        public override DocumentData SourceDoc { get => (Source as AbstractMetaData).Parent; }

        public string RepeatedName { get; }
        public int RepeatedGroup { get; }

        public RepeatedNameMessage(string repeatedName, int repeatedGroup, IMessageThrowable source
            , IMessageThrowable manager) : base(1, source, manager)
        {
            RepeatedName = repeatedName;
            RepeatedGroup = repeatedGroup;
        }

        public override string ToString()
        {
            return "Editor class named \"" + RepeatedName + "\" duplicated.";
        }

        public override object Clone()
        {
            return new RepeatedNameMessage(RepeatedName, RepeatedGroup, Source, Manager);
        }

        public override void Invoke()
        {
            TreeNode target =
                ((Source as MetaDataEntity).aggregatableMetas[RepeatedGroup]
                .FindOfName(RepeatedName) as MetaInfo)?.target;
            if (EditorAppContext.MessageNavigator != null)
                EditorAppContext.MessageNavigator.Reveal(target);
            else
                EditorAppContext.MainWindow?.Reveal(target);
        }
    }
}
