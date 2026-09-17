using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuaSTGEditorSharp.EditorData.Commands.Factory
{
    public class ChildFac : CommandTypeFac
    {
        public override InsertCommand NewInsert(TreeNode toOp, TreeNode toIns)
        {
            return new InsertAsChildCommand(toOp, toIns);
        }

        public override bool ValidateType(TreeNode toOp, TreeNode toIns)
        {
            if (toOp == null || toIns == null) return false;
            try
            {
                return toOp.ValidateChild(toIns);
            }
            catch
            {
                return false;
            }
        }
    }
}
