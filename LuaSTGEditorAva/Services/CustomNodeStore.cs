using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LuaSTGEditorAva.Services
{
    public static class CustomNodeStore
    {
        public static string Root => Path.Combine(AppContext.BaseDirectory, "CustomNodes");

        public static string InitFile => Path.Combine(Root, "Init.lua");

        public static bool HasInitFile => File.Exists(InitFile);

        public static List<string[]> GetRegisteredScripts()
        {
            var result = new List<string[]>();
            if (!HasInitFile) return result;
            try
            {
                foreach (string raw in ReadLines(InitFile))
                {
                    string line = raw.Trim();
                    if (!IsEntry(line)) continue;
                    string name = Unquote(line);
                    if (name == null || name == "_Separator") continue;
                    result.Add(new[] { ReadDisplayName(name), name });
                }
            }
            catch
            {
            }
            return result;
        }

        public static string ReadDisplayName(string fileBase)
        {
            string path = Path.Combine(Root, fileBase + ".lua");
            if (!File.Exists(path)) return fileBase;
            try
            {
                foreach (string raw in ReadLines(path))
                {
                    string line = raw.Trim();
                    if (line.StartsWith("name ="))
                    {
                        string name = Unquote(line.Substring("name =".Length));
                        if (!string.IsNullOrEmpty(name)) return name;
                    }
                }
            }
            catch
            {
            }
            return fileBase;
        }

        public static string CreateFromTemplate(string fileName, string displayName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return "File name is empty.";
            fileName = fileName.Trim();
            foreach (char bad in new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|', ',' })
                if (fileName.IndexOfAny(new[] { bad }) >= 0)
                    return $"Invalid file name: {fileName}";
            if (fileName.Equals("_Separator", StringComparison.OrdinalIgnoreCase))
                return "That name is reserved.";

            string target = Path.Combine(Root, fileName + ".lua");
            if (File.Exists(target)) return $"File already exists: {fileName}.lua";

            string template = Path.Combine(Root, "Example.lua");
            string text;
            if (File.Exists(template))
            {
                text = File.ReadAllText(template);
                text = text.Replace("name = \"Example Node\"",
                    $"name = \"{displayName}\"");
            }
            else
            {
                text = MinimalTemplate(displayName);
            }

            try
            {
                Directory.CreateDirectory(Root);
                File.WriteAllText(target, text, new UTF8Encoding(false));
                EnsureRegistered(fileName);
                return null;
            }
            catch (Exception ex)
            {
                return $"Could not create '{fileName}.lua': {ex.Message}";
            }
        }

        public static string DeleteScript(string fileBase)
        {
            if (string.IsNullOrWhiteSpace(fileBase)) return "Nothing selected.";
            string path = Path.Combine(Root, fileBase + ".lua");
            try
            {
                if (File.Exists(path)) File.Delete(path);
                RemoveRegistered(fileBase);
                return null;
            }
            catch (Exception ex)
            {
                return $"Could not delete '{fileBase}.lua': {ex.Message}";
            }
        }

        private static void EnsureRegistered(string fileBase)
        {
            List<string> lines = HasInitFile
                ? new List<string>(ReadLines(InitFile)) : new List<string>();
            foreach (string raw in lines)
            {
                string line = raw.Trim();
                if (IsEntry(line) && Unquote(line) == fileBase) return;
            }
            int insertAt = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                string trimmed = lines[i].TrimEnd();
                if (trimmed.EndsWith("local Registered_Nodes = {", StringComparison.Ordinal))
                {
                    insertAt = i + 1;
                    break;
                }
            }
            if (insertAt < 0)
            {
                WriteDefaultInit(fileBase);
                return;
            }
            lines.Insert(insertAt, $"\t\"{fileBase}\",");
            File.WriteAllLines(InitFile, lines, new UTF8Encoding(false));
        }

        private static void RemoveRegistered(string fileBase)
        {
            if (!HasInitFile) return;
            var lines = new List<string>(ReadLines(InitFile));
            bool changed = false;
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                string line = lines[i].Trim();
                if (!IsEntry(line) || Unquote(line) != fileBase) continue;
                // merge trailing comma of the previous entry if it becomes last
                string before = i > 0 ? lines[i - 1].TrimEnd() : null;
                if (before != null && before.EndsWith(","))
                    lines[i - 1] = lines[i - 1].TrimEnd().Substring(0, before.Length - 1);
                lines.RemoveAt(i);
                changed = true;
            }
            if (changed)
                File.WriteAllLines(InitFile, lines, new UTF8Encoding(false));
        }

        private static void WriteDefaultInit(string firstEntry)
        {
            Directory.CreateDirectory(Root);
            string text = "function InitNodes()\n" +
                "\t-- Put the name of node's file with or without the .lua at the end of it.\n" +
                "\tlocal Registered_Nodes = {\n" +
                $"\t\t\"{firstEntry}\"\n" +
                "\t}\n\n" +
                "\treturn Registered_Nodes\nend\n";
            File.WriteAllText(InitFile, text, new UTF8Encoding(false));
        }

        private static bool IsEntry(string trimmedLine)
        {
            return trimmedLine.StartsWith("\"") &&
                !trimmedLine.StartsWith("--") &&
                trimmedLine.Contains("\"") &&
                !trimmedLine.Contains("=");
        }

        private static string Unquote(string text)
        {
            int start = text.IndexOf('"');
            if (start < 0) return null;
            int end = text.IndexOf('"', start + 1);
            if (end <= start) return null;
            return text.Substring(start + 1, end - start - 1).Trim();
        }

        private static IReadOnlyList<string> ReadLines(string path)
        {
            return File.ReadAllLines(path);
        }

        private static string MinimalTemplate(string displayName)
        {
            return "function InitNode()\n" +
                "\tlocal properties = {\n" +
                $"\t\tname = \"{displayName}\",\n" +
                "\t\timage = \"\",\n" +
                "\t\tisLeaf = false,\n\n" +
                "\t\tParameters = {\n" +
                "\t\t\t{\"Value\", \"0\", \"value\"},\n" +
                "\t\t}\n" +
                "\t}\n\n" +
                "\treturn properties\nend\n\n" +
                "function ToLuaHead()\n\treturn ''\nend\n\n" +
                "function ToLuaBody()\n\tlocal lua_code = [[-- Put the lua code here.\n{0}\n]]\n\treturn lua_code\nend\n\n" +
                "function ToLuaTail()\n\treturn ''\nend\n\n" +
                "function ToString()\n\tlocal node_description = \"{0}\"\n\treturn node_description\nend\n";
        }
    }
}
