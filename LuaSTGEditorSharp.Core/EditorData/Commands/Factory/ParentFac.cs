using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuaSTGEditorSharp.EditorData.Commands.Factory
{
    public class ParentFac : CommandTypeFac
    {
        public override InsertCommand NewInsert(TreeNode toOp, TreeNode toIns)
        {
            return new InsertAsParentCommand(toOp, toIns);
        }

        public override bool ValidateType(TreeNode toOp, TreeNode toIns)
        {
            if (toOp == null || toIns == null) return false;
            try
            {
                TreeNode toInsP = TreeNode.TryLink(toIns, toOp);
                bool a;
                try
                {
                    a = toIns.ValidateChild(toOp) && (toIns.Parent == null || toIns.Parent.ValidateChild(toIns));
                }
                finally
                {
                    TreeNode.TryUnlink(toIns, toOp, toInsP);
                }
                return a;
            }
            catch
            {
                return false;
            }
        }
    }
}
