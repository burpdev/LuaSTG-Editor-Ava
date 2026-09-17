using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuaSTGEditorAva.Services
{
    public static class WineRunner
    {
        public static readonly IReadOnlyList<string> RunnerChoices =
            new[] { "auto", "wine", "wine64", "proton", "custom" };

        public enum EngineBinaryKind
        {
            Unknown,
            Windows,
            Native
        }

        public static EngineBinaryKind DetectBinaryKind(string path)
        {
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                byte[] magic = new byte[4];
                int read = fs.Read(magic, 0, 4);
                if (read >= 2 && magic[0] == (byte)'M' && magic[1] == (byte)'Z')
                    return EngineBinaryKind.Windows;
                if (read >= 4 && magic[0] == 0x7F && magic[1] == (byte)'E'
                    && magic[2] == (byte)'L' && magic[3] == (byte)'F')
                    return EngineBinaryKind.Native;
                if (read >= 2 && magic[0] == (byte)'#' && magic[1] == (byte)'!')
                    return EngineBinaryKind.Native;
            }
            catch
            {
            }
            if (path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                return EngineBinaryKind.Windows;
            return EngineBinaryKind.Unknown;
        }

        public static List<string> DetectRunners()
        {
            var found = new List<string>();
            foreach (string name in new[] { "wine", "wine64" })
            {
                if (FindOnPath(name) != null && !found.Contains(name))
                    found.Add(name);
            }
            foreach (string dir in FindProtonDirs())
            {
                if (!found.Contains(dir)) found.Add(dir);
            }
            return found;
        }

        public sealed class RunnerOption
        {
            public string Id { get; set; }
            public string Label { get; set; }
            public override string ToString() => Label ?? Id;
        }

        public static List<RunnerOption> DetectRunnerOptions()
        {
            var options = new List<RunnerOption>
            {
                new RunnerOption { Id = "auto", Label = "auto (recommended)" },
            };
            foreach (string name in new[] { "wine", "wine64" })
            {
                string path = FindOnPath(name);
                string version = path != null ? ProbeToolVersion(path) : null;
                options.Add(new RunnerOption
                {
                    Id = name,
                    Label = path == null ? $"{name} (not found)" : $"{name} ({version ?? FindOnPath(name)})"
                });
            }
            List<string> protons = FindProtonDirs();
            options.Add(new RunnerOption
            {
                Id = "proton",
                Label = protons.Count == 0 ? "proton (not found)" : $"proton (auto: {ProtonDisplay(protons[0])})"
            });
            var seenProton = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string dir in protons)
            {
                string key = ProtonDedupeKey(dir);
                if (!seenProton.Add(key)) continue;
                options.Add(new RunnerOption { Id = dir, Label = $"Proton {ProtonDisplay(dir)}" });
            }
            options.Add(new RunnerOption { Id = "custom", Label = "custom (command below)" });
            return options;
        }

        private static string ProtonDedupeKey(string dir)
        {
            string name = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            return (GetProtonVersion(dir) ?? "") + "|" + name;
        }

        private static string ProtonDisplay(string dir)
        {
            string version = GetProtonVersion(dir);
            string name = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (string.IsNullOrEmpty(version)) return name;
            if (name.IndexOf(version, StringComparison.OrdinalIgnoreCase) >= 0
                || version.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                return version;
            return $"{version} — {name}";
        }

        public static string ProbeToolVersion(string exe)
        {
            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo(exe, "--version")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                if (!process.Start()) return null;
                if (!process.WaitForExit(4000))
                {
                    try { process.Kill(); } catch { }
                    return null;
                }
                string line = (process.StandardOutput.ReadLine()
                    ?? process.StandardError.ReadLine() ?? "").Trim();
                if (line.Length > 48) line = line.Substring(0, 48);
                return string.IsNullOrEmpty(line) ? null : line;
            }
            catch
            {
                return null;
            }
        }

        public static string GetProtonVersion(string dir)
        {
            try
            {
                string file = Path.Combine(dir, "version");
                if (!File.Exists(file)) return null;
                string line = File.ReadLines(file).FirstOrDefault()?.Trim();
                if (!string.IsNullOrEmpty(line))
                {
                    // Valve/GE builds prefix a numeric build id ("1786437966 GE-Proton11-5").
                    string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1 && parts[0].All(char.IsDigit))
                        line = string.Join(" ", parts.Skip(1));
                }
                if (line != null && line.Length > 32) line = line.Substring(0, 32);
                return string.IsNullOrEmpty(line) ? null : line;
            }
            catch
            {
                return null;
            }
        }

        public static string Launch(string engineExe, string parameter, LinuxSettings settings)
        {
            var built = BuildLaunchInfo(engineExe, parameter, settings);
            if (built.Error != null) return built.Error;
            try
            {
                Process.Start(built.StartInfo);
                return null;
            }
            catch (Exception ex)
            {
                return $"Failed to launch engine: {ex.Message}";
            }
        }

        public sealed class EngineCapture : IDisposable
        {
            private readonly Process process;
            private readonly Action<string> onLine;
            private readonly Action<int> onExit;
            private volatile bool detached;
            private int exitSignaled;
            private System.Threading.Tasks.Task stdoutReader;
            private System.Threading.Tasks.Task stderrReader;

            public bool HasExited => detached || process?.HasExited == true;

            internal EngineCapture(Process process, Action<string> onLine, Action<int> onExit)
            {
                this.process = process;
                this.onLine = onLine;
                this.onExit = onExit;
                process.Exited += (sender, e) =>
                {
                    if (System.Threading.Interlocked.Exchange(ref exitSignaled, 1) != 0) return;
                    try
                    {
                        System.Threading.Tasks.Task.WaitAll(
                            new[] { stdoutReader, stderrReader }
                                .Where(t => t != null).ToArray(), 5000);
                    }
                    catch
                    {
                    }
                    try { onExit?.Invoke(process.ExitCode); } catch { }
                };
            }

            internal void Begin()
            {
                stdoutReader = System.Threading.Tasks.Task.Run(() => Pump(process.StandardOutput));
                stderrReader = System.Threading.Tasks.Task.Run(() => Pump(process.StandardError));
            }

            private void Pump(StreamReader reader)
            {
                try
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                        Forward(line);
                }
                catch
                {
                }
            }

            private void Forward(string line)
            {
                if (detached || line == null) return;
                try { onLine?.Invoke(line.TrimEnd('\r')); } catch { }
            }

            public void Detach()
            {
                detached = true;
            }

            public void Dispose()
            {
                Detach();
                try { process.Dispose(); } catch { }
            }
        }

        public static (string Error, EngineCapture Capture) LaunchWithLog(
            string engineExe, string parameter, LinuxSettings settings,
            Action<string> onLine, Action<int> onExit)
        {
            var built = BuildLaunchInfo(engineExe, parameter, settings);
            if (built.Error != null) return (built.Error, null);
            built.StartInfo.RedirectStandardOutput = true;
            built.StartInfo.RedirectStandardError = true;
            built.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            built.StartInfo.StandardErrorEncoding = Encoding.UTF8;
            try
            {
                var process = new Process { StartInfo = built.StartInfo, EnableRaisingEvents = true };
                var capture = new EngineCapture(process, onLine, onExit);
                if (!process.Start())
                {
                    capture.Dispose();
                    return ("Failed to launch engine.", null);
                }
                capture.Begin();
                return (null, capture);
            }
            catch (Exception ex)
            {
                return ($"Failed to launch engine: {ex.Message}", null);
            }
        }

        private sealed class LaunchPlan
        {
            public string Error;
            public ProcessStartInfo StartInfo;
            public bool Native;
        }

        private static LaunchPlan BuildLaunchInfo(string engineExe, string parameter, LinuxSettings settings)
        {
            if (string.IsNullOrEmpty(engineExe) || !File.Exists(engineExe))
                return new LaunchPlan { Error = "LuaSTG executable path is not set or missing (Settings/Compiler)." };

            string rawChoice = (settings.GameRunner ?? "auto").Trim();
            string choice = rawChoice.ToLowerInvariant();

            ProcessStartInfo psi = null;
            bool native = false;

            if (Path.IsPathRooted(rawChoice)
                && Directory.Exists(rawChoice)
                && File.Exists(Path.Combine(rawChoice, "proton")))
            {
                psi = ProtonStartInfo(rawChoice, engineExe);
            }

            if (psi == null && choice == "auto" && DetectBinaryKind(engineExe) == EngineBinaryKind.Native)
            {
                psi = new ProcessStartInfo(engineExe)
                {
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetDirectoryName(engineExe)
                };
                native = true;
            }
            else if (psi == null && choice == "custom")
            {
                string template = (settings.CustomRunnerCommand ?? "").Trim();
                if (string.IsNullOrEmpty(template)) template = "%command%";
                LaunchPlan baseline = BuildAutoPlan(engineExe, settings);
                if (baseline.Error != null) return baseline;
                psi = ApplyRunnerTemplate(baseline.StartInfo, template);
                native = baseline.Native;
            }
            else if (psi == null)
            {
                LaunchPlan baseline = BuildAutoPlan(engineExe, settings);
                if (baseline.Error != null) return baseline;
                psi = baseline.StartInfo;
                native = baseline.Native;
            }

            psi.ArgumentList.Add(parameter);
            if (!native)
                SilenceWineNoise(psi);
            return new LaunchPlan { StartInfo = psi, Native = native };
        }

        private static LaunchPlan BuildAutoPlan(string engineExe, LinuxSettings settings)
        {
            string choice = (settings.GameRunner ?? "auto").Trim().ToLowerInvariant();
            if (choice == "auto" && DetectBinaryKind(engineExe) == EngineBinaryKind.Native)
            {
                return new LaunchPlan
                {
                    StartInfo = new ProcessStartInfo(engineExe)
                    {
                        UseShellExecute = false,
                        WorkingDirectory = Path.GetDirectoryName(engineExe)
                    },
                    Native = true
                };
            }
            if (choice == "proton" || (choice == "auto" && FindOnPath("wine") == null && FindOnPath("wine64") == null))
            {
                List<string> protons = FindProtonDirs();
                if (protons.Count == 0)
                    return new LaunchPlan { Error = "No Proton installation found and no wine binary either. " +
                        "Install Wine, or pick a runner in Settings/Compiler." };
                return new LaunchPlan { StartInfo = ProtonStartInfo(protons[0], engineExe) };
            }
            string wine = choice == "wine" || choice == "wine64"
                ? FindOnPath(choice)
                : FindOnPath("wine") ?? FindOnPath("wine64");
            if (wine == null)
                return new LaunchPlan { Error = "Wine is not installed. Install Wine (e.g. your distro's wine package) " +
                    "or pick another runner in Settings/Compiler." };
            var psi = new ProcessStartInfo(wine)
            {
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(engineExe)
            };
            psi.ArgumentList.Add(engineExe);
            return new LaunchPlan { StartInfo = psi };
        }

        public sealed class ExpandedRunnerCommand
        {
            public string FileName;
            public List<string> Arguments = new List<string>();
            public Dictionary<string, string> Environment = new Dictionary<string, string>();
        }

        public static ExpandedRunnerCommand ExpandRunnerTemplate(string template, string baseExe, IEnumerable<string> baseArgs)
        {
            var result = new ExpandedRunnerCommand();
            List<string> tokens = SplitRunnerTemplate(template ?? "");
            bool hasPlaceholder = tokens.Any(t => t.Equals("%command%", StringComparison.OrdinalIgnoreCase));
            var argv = new List<string>();
            foreach (string token in tokens)
            {
                if (token.Equals("%command%", StringComparison.OrdinalIgnoreCase))
                {
                    argv.Add(baseExe);
                    argv.AddRange(baseArgs ?? Enumerable.Empty<string>());
                    continue;
                }
                int eq = token.IndexOf('=');
                if (eq > 0 && IsEnvName(token.Substring(0, eq)))
                {
                    result.Environment[token.Substring(0, eq)] = token.Substring(eq + 1);
                    continue;
                }
                argv.Add(token);
            }
            if (!hasPlaceholder)
            {
                argv.Add(baseExe);
                argv.AddRange(baseArgs ?? Enumerable.Empty<string>());
            }
            result.FileName = argv[0];
            result.Arguments.AddRange(argv.Skip(1));
            return result;
        }

        private static bool IsEnvName(string name)
        {
            if (string.IsNullOrEmpty(name) || !(char.IsLetter(name[0]) || name[0] == '_'))
                return false;
            foreach (char c in name)
            {
                if (!(char.IsLetterOrDigit(c) || c == '_')) return false;
            }
            return true;
        }

        public static List<string> SplitRunnerTemplate(string template)
        {
            var tokens = new List<string>();
            var current = new StringBuilder();
            bool inToken = false;
            char quote = '\0';
            for (int i = 0; i < template.Length; i++)
            {
                char c = template[i];
                if (quote != '\0')
                {
                    if (c == '\\' && i + 1 < template.Length)
                    {
                        current.Append(template[i + 1]);
                        i++;
                    }
                    else if (c == quote)
                    {
                        quote = '\0';
                    }
                    else
                    {
                        current.Append(c);
                    }
                    inToken = true;
                }
                else if (c == '"' || c == '\'')
                {
                    quote = c;
                    inToken = true;
                }
                else if (char.IsWhiteSpace(c))
                {
                    if (inToken)
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                        inToken = false;
                    }
                }
                else
                {
                    current.Append(c);
                    inToken = true;
                }
            }
            if (inToken) tokens.Add(current.ToString());
            return tokens;
        }

        private static ProcessStartInfo ApplyRunnerTemplate(ProcessStartInfo baseline, string template)
        {
            ExpandedRunnerCommand expanded = ExpandRunnerTemplate(template, baseline.FileName, baseline.ArgumentList);
            var psi = new ProcessStartInfo(expanded.FileName)
            {
                UseShellExecute = false,
                WorkingDirectory = baseline.WorkingDirectory
            };
            foreach (var kv in baseline.Environment)
                psi.Environment[kv.Key] = kv.Value;
            foreach (var kv in expanded.Environment)
                psi.Environment[kv.Key] = kv.Value;
            foreach (string arg in expanded.Arguments)
                psi.ArgumentList.Add(arg);
            return psi;
        }

        private static void SilenceWineNoise(ProcessStartInfo psi)
        {
            try
            {
                if (!psi.Environment.ContainsKey("WINEDEBUG"))
                    psi.Environment["WINEDEBUG"] = "fixme-all";
            }
            catch
            {
            }
        }

        public static string BuildParameter(LinuxSettings settings, string modName)
        {
            string Bool(bool b) => b.ToString().ToLowerInvariant();
            return "\"start_game=true is_debug=true setting.nosplash=true setting.windowed=" +
                Bool(settings.DebugWindowed) + " setting.resx=" + settings.DebugResolutionX +
                " setting.resy=" + settings.DebugResolutionY + " cheat=" + Bool(settings.DebugCheat) +
                " updatelib=" + Bool(settings.DebugUpdateLib) + " setting.mod='" + modName + "'\"";
        }

        private static ProcessStartInfo ProtonStartInfo(string protonDir, string engineExe)
        {
            var psi = new ProcessStartInfo(Path.Combine(protonDir, "proton"))
            {
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(engineExe)
            };
            string prefix = Path.Combine(Path.GetDirectoryName(engineExe), "lstg_prefix");
            try
            {
                Directory.CreateDirectory(prefix);
                psi.Environment["STEAM_COMPAT_DATA_PATH"] = prefix;
            }
            catch
            {
            }
            try
            {
                if (!psi.Environment.ContainsKey("STEAM_COMPAT_CLIENT_INSTALL_PATH"))
                {
                    string clientDir = FindSteamClientDir(protonDir);
                    if (!string.IsNullOrEmpty(clientDir))
                        psi.Environment["STEAM_COMPAT_CLIENT_INSTALL_PATH"] = clientDir;
                }
            }
            catch
            {
            }
            psi.ArgumentList.Add("run");
            psi.ArgumentList.Add(engineExe);
            return psi;
        }

        private static string FindSteamClientDir(string protonDir)
        {
            try
            {
                string dir = Path.GetFullPath(protonDir);
                for (int i = 0; i < 4 && !string.IsNullOrEmpty(dir); i++)
                {
                    try
                    {
                        if (Directory.Exists(Path.Combine(dir, "steamapps")))
                            return dir;
                    }
                    catch
                    {
                    }
                    dir = Path.GetDirectoryName(dir);
                }
            }
            catch
            {
            }
            try
            {
                string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                foreach (string candidate in new[]
                {
                    Path.Combine(home, ".steam", "root"),
                    Path.Combine(home, ".local", "share", "Steam"),
                    Path.Combine(home, ".steam", "steam"),
                })
                {
                    try
                    {
                        if (Directory.Exists(candidate)) return candidate;
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
            return null;
        }

        private static string FindOnPath(string name)
        {
            try
            {
                string path = Environment.GetEnvironmentVariable("PATH") ?? "";
                foreach (string dir in path.Split(Path.PathSeparator))
                {
                    if (string.IsNullOrEmpty(dir)) continue;
                    string candidate = Path.Combine(dir, name);
                    if (File.Exists(candidate)) return candidate;
                }
            }
            catch
            {
            }
            return null;
        }

        private static List<string> FindProtonDirs()
        {
            var found = new List<string>();
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string[] roots =
            {
                Path.Combine(home, ".steam", "root", "compatibilitytools.d"),
                Path.Combine(home, ".steam", "steam", "compatibilitytools.d"),
                Path.Combine(home, ".local", "share", "Steam", "compatibilitytools.d"),
                Path.Combine(home, ".local", "share", "Steam", "steamapps", "common"),
                Path.Combine(home, ".steam", "steam", "steamapps", "common"),
            };
            foreach (string root in roots)
            {
                string[] dirs;
                try
                {
                    if (!Directory.Exists(root)) continue;
                    dirs = Directory.GetDirectories(root);
                }
                catch
                {
                    continue;
                }
                foreach (string dir in dirs)
                {
                    if (File.Exists(Path.Combine(dir, "proton")) && !found.Contains(dir))
                        found.Add(dir);
                }
            }
            found.Sort(StringComparer.Ordinal);
            return found;
        }
    }
}
