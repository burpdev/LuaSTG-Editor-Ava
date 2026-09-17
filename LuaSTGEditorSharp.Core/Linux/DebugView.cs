using System;
using System.IO;
using System.Text;
using System.Threading;

namespace LuaSTGEditorSharp.Execution
{
    public class DebugView : IDisposable
    {
        int PID { get; set; }

        public DebugView(Logger writer, int pid)
        {
            if (writer == null) throw new ArgumentNullException("writer");
            this.writer = writer;
            PID = pid;
        }

        public void Start()
        {
            writer("[DebugView] Live debug capture is only supported on Windows; skipping.");
        }

        public void Stop()
        {
        }

        public void Dispose()
        {
            this.Stop();
        }

        private readonly Logger writer;
    }
}
