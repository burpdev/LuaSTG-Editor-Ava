using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace LuaSTGEditorAva.Services
{
    //TODO: change this to my own AppID once i setup the discord app for it
    //also modify the asset strings to the assets i add
    public sealed class DiscordPresence : IDisposable
    {
        public const string ApplicationId = "1263260551153319996";

        private readonly object gate = new object();
        private Socket socket;
        private bool disposed;

        public bool Connected
        {
            get
            {
                lock (gate) return socket != null && socket.Connected;
            }
        }

        public async Task ConnectAsync()
        {
            if (disposed || Connected) return;
            Socket candidate = null;
            try
            {
                candidate = await Task.Run(() => ConnectSocket());
            }
            catch
            {
                candidate = null;
            }
            if (candidate == null) return;
            lock (gate)
            {
                if (disposed)
                {
                    candidate.Dispose();
                    return;
                }
                socket = candidate;
            }
            if (!await Task.Run(() => Handshake()))
            {
                Disconnect();
                return;
            }
            Reset();
        }

        public void Reset()
        {
            SetActivity(null, "Idle", DateTime.UtcNow,
                "sharpxicon", "Not doing anything", null, null);
        }

        public void SetEditing(string docName, DateTime startedUtc)
        {
            SetActivity(docName, "Editing Project", startedUtc,
                "sharpxicon", null, "icon", docName);
        }

        public void SetActivity(string details, string state, DateTime? startUtc,
            string largeImage, string largeText, string smallImage, string smallText)
        {
            Socket current;
            lock (gate) current = socket;
            if (current == null || disposed) return;
            try
            {
                var activity = new JObject();
                if (!string.IsNullOrEmpty(details)) activity["details"] = details;
                if (!string.IsNullOrEmpty(state)) activity["state"] = state;
                var assets = new JObject();
                if (!string.IsNullOrEmpty(largeImage)) assets["large_image"] = largeImage;
                if (!string.IsNullOrEmpty(largeText)) assets["large_text"] = largeText;
                if (!string.IsNullOrEmpty(smallImage)) assets["small_image"] = smallImage;
                if (!string.IsNullOrEmpty(smallText)) assets["small_text"] = smallText;
                if (assets.Count > 0) activity["assets"] = assets;
                if (startUtc.HasValue)
                {
                    long unix = new DateTimeOffset(startUtc.Value).ToUnixTimeSeconds();
                    activity["timestamps"] = new JObject { ["start"] = unix };
                }
                var payload = new JObject
                {
                    ["cmd"] = "SET_ACTIVITY",
                    ["args"] = new JObject
                    {
                        ["pid"] = Process.GetCurrentProcess().Id,
                        ["activity"] = activity
                    },
                    ["nonce"] = Guid.NewGuid().ToString()
                };
                SendFrame(current, 1, payload.ToString(Newtonsoft.Json.Formatting.None));
            }
            catch
            {
                Disconnect();
            }
        }

        public void Dispose()
        {
            Disconnect();
            disposed = true;
        }

        private void Disconnect()
        {
            lock (gate)
            {
                try
                {
                    socket?.Shutdown(SocketShutdown.Both);
                }
                catch
                {
                }
                try
                {
                    socket?.Dispose();
                }
                catch
                {
                }
                socket = null;
            }
        }

        private static Socket ConnectSocket()
        {
            string runtime = Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
            int uid = UnixUid();
            var dirs = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrEmpty(runtime)) dirs.Add(runtime);
            if (uid >= 0) dirs.Add($"/run/user/{uid}");
            dirs.Add(Path.GetTempPath());
            for (int n = 0; n < 10; n++)
            {
                foreach (string dir in dirs)
                {
                    string path = Path.Combine(dir, $"discord-ipc-{n}");
                    try
                    {
                        var sock = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                        sock.Connect(new UnixDomainSocketEndPoint(path));
                        if (sock.Connected) return sock;
                        sock.Dispose();
                    }
                    catch
                    {
                    }
                }
            }
            return null;
        }

        private static int UnixUid()
        {
            // parse /proc/self/status Uid line
            try
            {
                foreach (string line in File.ReadAllLines("/proc/self/status"))
                {
                    if (!line.StartsWith("Uid:")) continue;
                    string[] parts = line.Split(new[] { ' ', '\t' },
                        StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int uid)) return uid;
                }
            }
            catch
            {
            }
            return -1;
        }

        private bool Handshake()
        {
            Socket current;
            lock (gate) current = socket;
            if (current == null) return false;
            try
            {
                var hello = new JObject { ["v"] = 1, ["client_id"] = ApplicationId };
                SendFrame(current, 0, hello.ToString(Newtonsoft.Json.Formatting.None));
                current.ReceiveTimeout = 3000;
                byte[] header = ReceiveExact(current, 8);
                if (header == null) return false;
                int opcode = BitConverter.ToInt32(header, 0);
                int length = BitConverter.ToInt32(header, 4);
                if (opcode != 1 || length <= 0 || length > 65536) return false;
                byte[] body = ReceiveExact(current, length);
                if (body == null) return false;
                var reply = JObject.Parse(Encoding.UTF8.GetString(body));
                return string.Equals(reply.Value<string>("cmd"), "DISPATCH", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(reply.Value<string>("evt"), "READY", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static void SendFrame(Socket sock, int opcode, string payload)
        {
            byte[] data = Encoding.UTF8.GetBytes(payload);
            byte[] header = new byte[8];
            BitConverter.GetBytes(opcode).CopyTo(header, 0);
            BitConverter.GetBytes(data.Length).CopyTo(header, 4);
            SendAll(sock, header);
            SendAll(sock, data);
        }

        private static void SendAll(Socket sock, byte[] buffer)
        {
            int sent = 0;
            while (sent < buffer.Length)
            {
                int n = sock.Send(buffer, sent, buffer.Length - sent, SocketFlags.None);
                if (n <= 0) throw new IOException("Socket closed while sending.");
                sent += n;
            }
        }

        private static byte[] ReceiveExact(Socket sock, int count)
        {
            var buffer = new byte[count];
            int received = 0;
            while (received < count)
            {
                int n;
                try
                {
                    n = sock.Receive(buffer, received, count - received, SocketFlags.None);
                }
                catch (SocketException)
                {
                    return null;
                }
                if (n <= 0) return null;
                received += n;
            }
            return buffer;
        }
    }
}
