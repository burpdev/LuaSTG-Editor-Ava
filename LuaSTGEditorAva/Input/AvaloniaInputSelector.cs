using Avalonia.Media;using Avalonia.Layout;using Avalonia.Interactivity;using Avalonia.Input;using Avalonia.Data;using Avalonia.Controls.Templates;using Avalonia.Controls.Primitives;using Avalonia.Controls;using Avalonia;using System.Collections.Generic;
using LuaSTGEditorSharp.EditorData;
using LuaSTGEditorSharp.Util;
using LuaSTGEditorSharp.Windows.Input;
using LuaSTGEditorSharp.Windows;
using System;
namespace LuaSTGEditorAva.Input
{
    public static class AvaloniaInputSelector
    {
        public static readonly string[] NullSelection = Array.Empty<string>();
        public static readonly Func<AttrItem, string, IInputWindow> NullWindow =
            (AttrItem item, string s) => new SingleLineWindow(s);

        private static readonly Dictionary<string, string[]> comboBox = new Dictionary<string, string[]>();
        private static readonly Dictionary<string, Func<AttrItem, string, IInputWindow>> windowGenerator =
            new Dictionary<string, Func<AttrItem, string, IInputWindow>>();
        private static readonly Dictionary<string, string[]> overrides = new Dictionary<string, string[]>();

        public static void Register(IInputWindowSelectorRegister register)
        {
            register.RegisterComboBoxText(comboBox);
            register.RegisterInputWindow(windowGenerator);
            register.RegisterOverrides(overrides);
        }

        public static void AfterRegister()
        {
            List<string> vs = new List<string>(windowGenerator.Keys);
            vs.Add("");
            vs.Sort();
            comboBox["editWindow"] = vs.ToArray();
            windowGenerator["editWindow"] = (src, tar) => new SelectorWindow(tar,
                SelectComboBox("editWindow"), "Input Edit Window");
        }

        public static string[] SelectComboBox(string name)
        {
            return comboBox.GetOrDefault(name, NullSelection);
        }

        public static bool HasWindow(string key) => windowGenerator.ContainsKey(key);

        public static bool HasCombo(string key) => comboBox.ContainsKey(key);

        public static IInputWindow SelectInputWindow(AttrItem source, string name, string toEdit)
        {
            IInputWindow iw = windowGenerator.GetOrDefault(name, NullWindow)(source, toEdit);
            iw.AppendTitle(source.AttrCap);
            return iw;
        }
    }
}
