using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LuaSTGEditorSharp.Services;

namespace LuaSTGEditorSharp.Lua
{
    public abstract class IndentationGenerator
    {
        private static IndentationGenerator _current;

        public static IndentationGenerator Current
        {
            get
            {
                if (_current == null)
                {
                    var settings = EditorAppContext.CurrentSettings;
                    if (settings != null && settings.SpaceIndentation)
                    {
                        _current = new SpaceIndentation() { NumOfSpaces = settings.IndentationSpaceLength };
                    }
                    else
                    {
                        _current = new TabIndentation();
                    }
                }
                return _current;
            }
            set
            {
                _current = value;
            }
        }

        public abstract string CreateIndentation(int length);
    }
}
