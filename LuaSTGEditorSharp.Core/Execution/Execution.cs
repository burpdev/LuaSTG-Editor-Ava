using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using LuaSTGEditorSharp.Services;
using Serilog;

namespace LuaSTGEditorSharp.Execution
{
    public delegate void Logger(string s);

    public abstract class LSTGExecution
    {

        private static ILogger Logger = EditorLogging.ForContext("LSTGExecution");
        protected Process LSTGInstance { get; set; }

        protected string Parameter { get; set; }

        protected bool UseShellExecute { get; set; }

        protected bool CreateNoWindow { get; set; }

        protected bool RedirectStandardError { get; set; }

        protected bool RedirectStandardOutput { get; set; }

        protected virtual string WorkingDirectory { get => Path.GetDirectoryName(EditorAppContext.CurrentSettings?.LuaSTGExecutablePath); }

        protected virtual string LuaSTGPath { get => EditorAppContext.CurrentSettings?.LuaSTGExecutablePath; }

        protected abstract string LogFileName { get; }

        public abstract void BeforeRun(ExecutionConfig config);

        public virtual void Run(Logger logger, Action end)
        {
            if (LSTGInstance == null || LSTGInstance.HasExited)
            {
                LSTGInstance = new Process
                {
                    StartInfo = new ProcessStartInfo(LuaSTGPath, Parameter)
                    {
                        UseShellExecute = UseShellExecute,
                        CreateNoWindow = CreateNoWindow,
                        WorkingDirectory = WorkingDirectory,
                        RedirectStandardError = RedirectStandardError,
                        RedirectStandardOutput = RedirectStandardOutput
                    }
                };
                LSTGInstance.EnableRaisingEvents = true;
                LSTGInstance.Start();
                DebugView debugView = null;
                bool useDebugView = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    && string.Equals(Environment.GetEnvironmentVariable("LSTGX_DISABLE_DEBUGVIEW"), "1") != true;
                if (useDebugView)
                {
                    try { debugView = new DebugView(logger, LSTGInstance.Id); }
                    catch { debugView = null; useDebugView = false; }
                }
#if !DEBUG
                if(useDebugView && (EditorAppContext.CurrentDebugSettings?.DynamicDebugReporting ?? false))
                {
                    try { debugView.Start(); } catch { }
                }
#endif
                logger("LuaSTG is Running.\n\n");
                Logger.Information($"LuaSTG is Running.");

                LSTGInstance.Exited += (s, e) => {
                    StringBuilder sb = new();
                    try
                    {
                        using FileStream fs = new(Path.GetFullPath(Path.Combine(
                            Path.GetDirectoryName(LuaSTGPath), LogFileName)), FileMode.Open);
                        using StreamReader sr = new(fs);
                        int i = 0;
                        while (!sr.EndOfStream && i < 8192)
                        {
                            sb.Append(sr.ReadLine());
                            sb.Append("\n");
                            i++;
                        }
                        logger(sb.ToString());
                        end();
                    }
                    catch (Exception exc)
                    {
                        Logger.Error($"LuaSTG instance had an error. Reason:\n{exc}");
                        EditorAppContext.Dialogs.ShowError(exc.ToString());
                    }
                    finally
                    {
#if !DEBUG
                        if (useDebugView && (EditorAppContext.CurrentDebugSettings?.DynamicDebugReporting ?? false)) debugView?.Dispose();
#endif
                    }
                    sb.Append("\nExited with code " + LSTGInstance.ExitCode + ".");
                    logger(sb.ToString());
                };
                
            }
            else
            {
                Logger.Warning("LuaSTG is already running, aborting LuaSTGExecution process.");
                EditorAppContext.Dialogs.ShowError("LuaSTG is already running, please exit first.");
            }
        }
    }
}
