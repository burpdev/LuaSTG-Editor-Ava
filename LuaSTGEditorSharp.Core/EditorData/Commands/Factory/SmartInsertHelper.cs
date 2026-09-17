using System;

namespace LuaSTGEditorSharp.EditorData.Commands.Factory
{
    public static class SmartInsertHelper
    {
        public static InsertCommand CreateSmartInsert(TreeNode selected, TreeNode toInsert, CommandTypeFac preferred)
        {
            if (selected == null || toInsert == null) return null;
            preferred ??= new AfterFac();

            // 1. Default "After" on a container that can take the node as a child:
            //    user clicked a section header expecting the node to go inside.
            //    e.g. On shoot() + Create simple bullet, Task + Sentence (via descendant below).
            //    Only override After (the default mode) so explicit Before/Child/Parent
            //    choices keep working.
            if (preferred is AfterFac)
            {
                try
                {
                    if (selected.ValidateChild(toInsert))
                        return new InsertAsChildCommand(selected, toInsert);
                }
                catch { }
            }

            // 2. Respect the user's chosen mode first.
            try
            {
                InsertCommand primary = preferred.ValidateAndNewInsert(selected, toInsert);
                if (primary != null) return primary;
            }
            catch { }

            // 3. Try direct child of selected.
            if (!(preferred is ChildFac))
            {
                try
                {
                    if (selected.ValidateChild(toInsert))
                        return new InsertAsChildCommand(selected, toInsert);
                }
                catch { }
            }

            // 4. Descendant search: e.g. Dialog selected + Sentence -> insert into
            //    the last Task inside the Dialog. Prefer last/deepest so new nodes
            //    append at the end of a section.
            try
            {
                TreeNode desc = FindSuitableDescendant(selected, toInsert);
                if (desc != null && !ReferenceEquals(desc, selected))
                    return new InsertAsChildCommand(desc, toInsert);
            }
            catch { }

            // 5. Try sibling placements if they weren't the preferred mode.
            if (!(preferred is AfterFac))
            {
                try
                {
                    InsertCommand c = new AfterFac().ValidateAndNewInsert(selected, toInsert);
                    if (c != null) return c;
                }
                catch { }
            }
            if (!(preferred is BeforeFac))
            {
                try
                {
                    InsertCommand c = new BeforeFac().ValidateAndNewInsert(selected, toInsert);
                    if (c != null) return c;
                }
                catch { }
            }

            // 6. Walk up ancestors: a node that belongs higher up (e.g. selected a
            //    deeply nested leaf but new node belongs to an outer section).
            try
            {
                TreeNode onPath = selected;
                TreeNode p = selected.Parent;
                while (p != null)
                {
                    try
                    {
                        if (p.ValidateChild(toInsert))
                            return new InsertAsChildCommand(p, toInsert);
                    }
                    catch { }
                    // Try "after the child on the path" so ordering stays near selection.
                    try
                    {
                        TreeNode childOnPath = onPath;
                        if (childOnPath != null && childOnPath.Parent == p)
                        {
                            InsertCommand c = new AfterFac().ValidateAndNewInsert(childOnPath, toInsert);
                            if (c != null) return c;
                        }
                    }
                    catch { }
                    onPath = p;
                    p = p.Parent;
                }
            }
            catch { }

            return null;
        }

        private static TreeNode FindSuitableDescendant(TreeNode root, TreeNode toInsert)
        {
            if (root == null || toInsert == null) return null;
            // Depth-first, last-child-first so appends land at the end of sections.
            for (int i = root.Children.Count - 1; i >= 0; i--)
            {
                TreeNode child;
                try { child = root.Children[i]; }
                catch { continue; }
                if (child == null) continue;
                try
                {
                    TreeNode deeper = FindSuitableDescendant(child, toInsert);
                    if (deeper != null) return deeper;
                }
                catch { }
                try
                {
                    if (child.ValidateChild(toInsert)) return child;
                }
                catch { }
            }
            return null;
        }
    }
}
