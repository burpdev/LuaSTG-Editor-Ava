using System;
using System.IO;
using System.Threading.Tasks;
using LuaSTGEditorSharp.Plugin;
using LstgHeadless;

namespace LstgCli
{
    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "-h" || args[0] == "--help")
            {
                Console.WriteLine("Usage: LstgCli <input.lstges|input.lstgproj> [-o output.lua] [--full]");
                Console.WriteLine("  Default (.lstges): single-file lua codegen via SaveCode.");
                Console.WriteLine("  --full: run the full CompileProcess (folder packing, no external zip).");
                return 0;
            }

            string input = Path.GetFullPath(args[0]);
            string output = null;
            bool full = false;
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "-o" && i + 1 < args.Length) output = args[++i];
                else if (args[i] == "--full") full = true;
            }

            DocumentSession session;
            try
            {
                session = await DocumentSession.OpenAsync(input);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Failed to open document: {e.Message}");
                return 2;
            }

            Console.WriteLine($"Plugin loaded: {PluginHandler.Plugin.TargetLSTGVersion}");
            Console.WriteLine($"Opened '{session.Document.DocName}' with {session.GetMessages().Count} message(s).");

            string ext = Path.GetExtension(input);
            if (ext.Equals(".lstges", StringComparison.OrdinalIgnoreCase) && !full)
            {
                output ??= Path.ChangeExtension(input, ".lua");
                try
                {
                    session.SaveCode(output);
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"SaveCode failed: {e}");
                    return 7;
                }
                Console.WriteLine($"Wrote lua: {output} ({new FileInfo(output).Length} bytes)");
                return 0;
            }

            try
            {
                session.Document.CompileProcess.ExecuteProcess(false, false, session.Settings);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"ExecuteProcess failed: {e}");
                return 8;
            }
            Console.WriteLine("Full compile process finished.");
            return 0;
        }
    }
}
