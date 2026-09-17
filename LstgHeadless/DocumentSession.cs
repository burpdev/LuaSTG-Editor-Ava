using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LuaSTGEditorSharp;
using LuaSTGEditorSharp.EditorData;
using LuaSTGEditorSharp.EditorData.Document;
using LuaSTGEditorSharp.EditorData.Node;
using LuaSTGEditorSharp.Plugin;
using LuaSTGEditorSharp.Services;

namespace LstgHeadless
{
    public class DocumentSession
    {
        private static bool loggingInitialized;

        public DocumentData Document { get; private set; }

        public DocumentCollection Collection { get; } = new DocumentCollection();

        public string FilePath { get; private set; }

        public HeadlessAppSettings Settings { get; } = new HeadlessAppSettings();

        public static void EnsureInitialized()
        {
            if (!loggingInitialized)
            {
                EditorLogging.Initialize();
                loggingInitialized = true;
            }
            if (EditorAppContext.CurrentSettings == null)
            {
                var settings = new HeadlessAppSettings();
                EditorAppContext.CurrentSettings = settings;
                EditorAppContext.CurrentDebugSettings = settings;
            }
            if (PluginHandler.Plugin == null)
            {
                // Single-file publishes have an empty Assembly.Location, so LoadFile-based loading cannot work there.
                if (!PluginHandler.RegisterPlugin(new PluginEntry()))
                    throw new InvalidOperationException("Failed to load headless plugin.");
            }
        }

        public static async Task<DocumentSession> OpenAsync(string path)
        {
            EnsureInitialized();
            var session = new DocumentSession();
            await session.LoadAsync(Path.GetFullPath(path));
            return session;
        }

        public static async Task<DocumentSession> NewFromTemplateAsync(string templatePath, string displayName)
        {
            EnsureInitialized();
            var session = new DocumentSession();
            await session.LoadAsync(Path.GetFullPath(templatePath));
            session.Document.DocPath = "";
            session.Document.DocName = displayName;
            session.FilePath = "";
            StampEditorVersion(session.Document);
            return session;
        }

        private static void StampEditorVersion(DocumentData document)
        {
            if (document?.TreeNodes == null) return;
            var queue = new Queue<TreeNode>(document.TreeNodes);
            while (queue.Count > 0)
            {
                TreeNode node = queue.Dequeue();
                if (node is EditorVersion stamp)
                    stamp.Version = AppVersion.Current;
                if (node.Children != null)
                    foreach (TreeNode child in node.Children)
                        queue.Enqueue(child);
            }
        }

        private async Task LoadAsync(string fullPath)
        {
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("Input file not found.", fullPath);

            string ext = Path.GetExtension(fullPath);
            Document = DocumentData.GetNewByExtension(ext, Collection.MaxHash, Path.GetFileName(fullPath), fullPath);
            Collection.AddAndAllocHash(Document);

            TreeNode root = await DocumentData.CreateNodeFromFileAsync(fullPath, Document);
            if (root == null)
                throw new InvalidDataException($"Failed to parse document '{fullPath}' (empty root).");
            Document.TreeNodes.Add(root);
            root.RaiseCreate(new OnCreateEventArgs() { parent = null });
            Document.OnOpening();
            Document.GatherCompileInfo(Settings);
            FilePath = fullPath;
        }

        public string GenerateLua()
        {
            if (Document == null) throw new InvalidOperationException("No document open.");
            return string.Concat(Document.TreeNodes[0].ToLua(0));
        }

        public void SaveCode(string outPath)
        {
            if (Document == null) throw new InvalidOperationException("No document open.");
            Document.SaveCode(outPath);
        }

        public bool Save(bool saveAs = false)
        {
            if (Document == null) throw new InvalidOperationException("No document open.");
            return Document.Save(Settings, saveAs);
        }

        public IReadOnlyList<string> GetMessages()
        {
            return MessageContainer.Messages.Select(m => m.ToString()).ToList();
        }
    }
}
