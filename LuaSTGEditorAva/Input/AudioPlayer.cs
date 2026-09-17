using System;
using System.Diagnostics;
using System.IO;

namespace LuaSTGEditorAva.Input
{
    public sealed class ManagedAudioPlayer : IDisposable
    {
        private readonly string[] players = new[] { "ffplay", "mpv", "mpg123", "ogg123" };

        private Process process;

        public event Action<string> StatusChanged;

        public bool IsPlaying => process != null && !process.HasExited;

        public string Play(string path)
        {
            if (string.IsNullOrEmpty(path)) return "No file selected.";
            string full;
            try
            {
                full = Path.IsPathRooted(path) ? path : Path.GetFullPath(path);
            }
            catch
            {
                return $"Invalid path: {path}";
            }
            if (!File.Exists(full)) return $"File not found: {full}";

            Stop();
            foreach (string player in players)
            {
                try
                {
                    var psi = new ProcessStartInfo(player)
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    if (player == "ffplay")
                    {
                        foreach (string opt in new[] { "-nodisp", "-autoexit", "-loglevel", "quiet" })
                            psi.ArgumentList.Add(opt);
                    }
                    else if (player == "mpv")
                    {
                        psi.ArgumentList.Add("--no-video");
                    }
                    psi.ArgumentList.Add(full);
                    Process started = Process.Start(psi);
                    if (started == null) continue;
                    process = started;
                    Notify($"Playing: {Path.GetFileName(full)}");
                    return null;
                }
                catch
                {
                }
            }
            Notify("No audio player found (ffplay/mpv/...).");
            return "Could not start playback.";
        }

        public void Stop()
        {
            try
            {
                if (process != null && !process.HasExited)
                {
                    process.Kill();
                }
            }
            catch
            {
            }
            process = null;
            Notify("Stopped.");
        }

        public void Dispose() => Stop();

        private void Notify(string message) => StatusChanged?.Invoke(message);
    }
}
