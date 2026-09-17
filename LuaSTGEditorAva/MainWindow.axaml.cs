using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DanmakuRandomizer;
using LstgHeadless;
using LuaSTGEditorAva.Dialogs;
using LuaSTGEditorAva.Input;
using LuaSTGEditorAva.Services;
using LuaSTGEditorSharp;
using LuaSTGEditorSharp.EditorData;
using LuaSTGEditorSharp.EditorData.Commands;
using LuaSTGEditorSharp.EditorData.Commands.Factory;
using LuaSTGEditorSharp.EditorData.Document;
using LuaSTGEditorSharp.EditorData.Node;
using LuaSTGEditorSharp.EditorData.Node.Advanced;
using LuaSTGEditorSharp.Plugin;
using LuaSTGEditorSharp.Services;

namespace LuaSTGEditorAva
{
    public sealed class ToolboxTabView
    {
        public string Header { get; set; }
        public List<ToolboxItemData> Items { get; set; }
    }

    public partial class MainWindow : Window, IMainWindow
    {
        private static readonly string VersionName = $"LuaSTG Editor Ava v{AppVersion.Current} {AppVersion.PlatformSuffix}";

        private DocumentSession session;
        private readonly List<DocumentSession> sessions = new List<DocumentSession>();
        private DispatcherTimer autoSaveTimer;
        private readonly DiscordPresence discord = new DiscordPresence();        private PluginToolbox toolbox;
        private TreeNode clipBoard;
        private CommandTypeFac insertState = new AfterFac();
        private Button insAfterButton;
        private Button insBeforeButton;
        private Button insChildButton;
        private Button insParentButton;
        private bool suppressDocStrip;
        private Point? dragStartPoint;
        private TreeNode dragNode;
        private WineRunner.EngineCapture activeEngine;
        private readonly object engineLogLock = new object();
        private readonly List<string> engineLogPending = new List<string>();
        private DispatcherTimer engineLogTimer;
        private bool debugLogStickToBottom = true;
        private bool debugLogScrollHooked;
        private int lastToolboxTab;

        public MainWindow()
        {
            InitializeComponent();
            Title = VersionName;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            this.FindControl<MenuItem>("NewMenu").Click += New_Click;
            this.FindControl<MenuItem>("OpenMenu").Click += Open_Click;
            this.FindControl<MenuItem>("SaveMenu").Click += Save_Click;
            this.FindControl<MenuItem>("SaveAsMenu").Click += SaveAs_Click;
            this.FindControl<MenuItem>("SaveLuaMenu").Click += SaveLua_Click;
            this.FindControl<MenuItem>("CloseMenu").Click += Close_Click;
            this.FindControl<MenuItem>("ExitMenu").Click += Exit_Click;
            this.FindControl<MenuItem>("EditNodeMenu").Click += EditNode_Click;
            this.FindControl<MenuItem>("UndoMenu").Click += Undo_Click;
            this.FindControl<MenuItem>("RedoMenu").Click += Redo_Click;
            this.FindControl<MenuItem>("CutMenu").Click += Cut_Click;
            this.FindControl<MenuItem>("CopyMenu").Click += Copy_Click;
            this.FindControl<MenuItem>("PasteMenu").Click += Paste_Click;
            this.FindControl<MenuItem>("DeleteMenu").Click += Delete_Click;
            this.FindControl<MenuItem>("BanMenu").Click += Ban_Click;
            this.FindControl<MenuItem>("FoldMenu").Click += Fold_Click;
            this.FindControl<MenuItem>("UnfoldMenu").Click += Unfold_Click;
            this.FindControl<MenuItem>("FoldRegionMenu").Click += FoldRegion_Click;
            this.FindControl<MenuItem>("UnfoldRegionMenu").Click += UnfoldRegion_Click;
            this.FindControl<MenuItem>("GoToDefMenu").Click += GoToDef_Click;
            this.FindControl<MenuItem>("FixAttrsMenu").Click += FixAttrs_Click;
            this.FindControl<MenuItem>("CustomNodesMenu").Click += CustomNodes_Click;
            RefreshPluginTools();
            this.FindControl<MenuItem>("InsBeforeMenu").Click += (sender, args) => SetInsertState(new BeforeFac());
            this.FindControl<MenuItem>("InsAfterMenu").Click += (sender, args) => SetInsertState(new AfterFac());
            this.FindControl<MenuItem>("InsChildMenu").Click += (sender, args) => SetInsertState(new ChildFac());
            this.FindControl<MenuItem>("InsParentMenu").Click += (sender, args) => SetInsertState(new ParentFac());
            this.FindControl<MenuItem>("SavePresetMenu").Click += SavePreset_Click;
            this.FindControl<MenuItem>("RefreshPresetMenu").Click += (sender, args) => RefreshPresetList();
            this.FindControl<MenuItem>("RunMenu").Click += (sender, args) => PackProject(run: true);
            this.FindControl<MenuItem>("ScDebugMenu").Click += ScDebug_Click;
            this.FindControl<MenuItem>("StageDebugMenu").Click += StageDebug_Click;
            this.FindControl<MenuItem>("PackMenu").Click += (sender, args) => PackProject(run: false);
            this.FindControl<MenuItem>("FileFolderMenu").Click += FileFolder_Click;
            this.FindControl<MenuItem>("ModFolderMenu").Click += ModFolder_Click;
            this.FindControl<MenuItem>("ViewCodeMenu").Click += ViewCode_Click;
            this.FindControl<MenuItem>("ExportCodeMenu").Click += SaveLua_Click;
            this.FindControl<MenuItem>("DefinitionsMenu").Click += Definitions_Click;
            this.FindControl<MenuItem>("GeneralSettingsMenu").Click += (sender, args) => OpenSettings(0);
            this.FindControl<MenuItem>("CompilerSettingsMenu").Click += (sender, args) => OpenSettings(1);
            this.FindControl<MenuItem>("DebugSettingsMenu").Click += (sender, args) => OpenSettings(2);
            this.FindControl<MenuItem>("EditorSettingsMenu").Click += (sender, args) => OpenSettings(3);
            this.FindControl<MenuItem>("AboutMenu").Click += About_Click;
            BuildToolbar();
            try
            {
                var asm = typeof(MainWindow).Assembly;
                string res = asm.GetManifestResourceNames().FirstOrDefault(n =>
                    n.IndexOf("AppIcon.png", StringComparison.OrdinalIgnoreCase) >= 0);
                if (res != null)
                {
                    using var src = asm.GetManifestResourceStream(res);
                    if (src != null)
                    {
                        var copy = new MemoryStream();
                        src.CopyTo(copy);
                        copy.Position = 0;
                        Icon = new WindowIcon(copy);
                    }
                }
            }
            catch
            {
            }
            this.FindControl<TreeView>("DocTree").SelectionChanged += DocTree_Changed;
            this.FindControl<ListBox>("MessageList").ItemsSource =
                LuaSTGEditorSharp.EditorData.MessageContainer.Messages;
            this.FindControl<ListBox>("DocStrip").SelectionChanged += DocStrip_Changed;
            this.FindControl<TextBox>("SearchBox").TextChanged += SearchBox_Changed;
            var tree = this.FindControl<TreeView>("DocTree");
            tree.AddHandler(DragDrop.DropEvent, DocTree_Drop);
            tree.AddHandler(DragDrop.DragOverEvent, DocTree_DragOver);
            tree.PointerPressed += DocTree_PointerPressed;
            tree.PointerMoved += DocTree_PointerMoved;
            tree.PointerReleased += (sender, e) => { dragNode = null; dragStartPoint = null; };
            DragDrop.SetAllowDrop(tree, true);
            DragDrop.SetAllowDrop(this, true);
            AddHandler(DragDrop.DropEvent, Window_Drop);
            AddHandler(InputElement.KeyDownEvent, Window_KeyDown, RoutingStrategies.Bubble);
            autoSaveTimer = new DispatcherTimer { Interval = AutoSaveInterval() };
            autoSaveTimer.Tick += (sender, e) =>
            {
                autoSaveTimer.Interval = AutoSaveInterval();
                RunAutoSave();
            };
            autoSaveTimer.Start();

            engineLogTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
            engineLogTimer.Tick += (sender, e) => FlushEngineLog();
            engineLogTimer.Start();
            EnsureToolbox();
            SetInsertState(new AfterFac());
            ApplyCompactView();
            RefreshPresetList();
            RefreshRecentList();
            RefreshAll();
            if (!LinuxSettings.LoadedOk && LinuxSettings.LastLoadError != null)
            {
                AppendDebugLog("Settings load failed: " + LinuxSettings.LastLoadError);
                SetStatus("Settings file could not be loaded; using defaults. See Debug Log.");
            }
            else
            {
                AppendDebugLog("Settings loaded from " + LinuxSettings.ConfigFilePath);
            }
            if (LinuxSettings.Instance.UseDiscordRpc)
                _ = discord.ConnectAsync();
            Closed += (sender, e) => discord.Dispose();
        }

        public DocumentData ActivatedWorkSpaceData => session?.Document;

        private TreeNode SelectedNode => this.FindControl<TreeView>("DocTree").SelectedItem as TreeNode;

        public void Insert(TreeNode node, bool isInvoke = true)
        {
            try
            {
                TreeNode selected = SelectedNode;
                if (session?.Document == null || node == null || selected == null)
                    return;

                Command c = SmartInsertHelper.CreateSmartInsert(selected, node, insertState);
                if (c == null)
                {
                    SetStatus($"Cannot insert '{node.GetType().Name}' here. Try selecting a different node or changing insert mode.");
                    return;
                }
                if (session.Document.AddAndExecuteCommand(c))
                {
                    if (LinuxSettings.Instance.AutoMoveToNew) Reveal(node);
                    if (isInvoke)
                    {
                        node.CheckMessage(null, new PropertyChangedEventArgs(""));
                        _ = CreateInvokeAsync(node);
                    }
                }
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError("Insert failed.\n" + ex);
            }
            RefreshAll();
        }

        public void Insert(TreeNode node, bool isInvoke, string[] supportedVers)
        {
            try
            {
                if (!LinuxSettings.Instance.IgnoreTHLibWarn && supportedVers != null &&
                    !supportedVers.Contains(PluginHandler.Plugin?.TargetLSTGVersion))
                {
                    EditorAppContext.Dialogs.ShowError("This node is not supported by the current THLib.", "Warning");
                }
            }
            catch
            {
            }
            Insert(node, isInvoke);
        }

        public void Reveal(TreeNode node)
        {
            if (node == null) return;
            try
            {
                TreeNode temp = node.Parent;
                node.parentWorkSpace.IsSelected = true;
                node.parentWorkSpace.TreeNodes[0].ClearChildSelection();
                var stack = new Stack<TreeNode>();
                while (temp != null)
                {
                    stack.Push(temp);
                    temp = temp.Parent;
                }
                while (stack.Count > 0) stack.Pop().IsExpanded = true;
                node.IsSelected = true;
                this.FindControl<TreeView>("DocTree").SelectedItem = node;
            }
            catch
            {
            }
        }

        private async Task CreateInvokeAsync(TreeNode node)
        {
            try
            {
                AttrItem ai = node.GetCreateInvoke();
                if (ai == null) return;
                var iw = AvaloniaInputSelector.SelectInputWindow(ai, ai.EditWindow, ai.AttrInput);
                if (iw is IAvaloniaInputWindow win && await win.ShowDialogAsync(this) == true)
                {
                    session.Document.AddAndExecuteCommand(new EditAttrCommand(ai, ai.AttrInput, win.Result));
                    RefreshAll();
                }
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError("Insert setup failed.\n" + ex);
            }
        }


        private async void New_Click(object sender, RoutedEventArgs e)
        {
            if (!await ConfirmDiscardUnsaved()) return;
            var dialog = new NewDialog();
            bool? ok = await dialog.ShowDialog<bool?>(this);
            if (ok != true || string.IsNullOrEmpty(dialog.SelectedPath)) return;
            try
            {
                string path = dialog.SelectedPath;
                if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    path = await DownloadToTempAsync(path, dialog.SelectedName);
                session = await DocumentSession.NewFromTemplateAsync(path, dialog.SelectedName);
                ApplyNewDocSettings(session.Document, dialog);
                AfterDocumentOpened($"New '{dialog.SelectedName}'");
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError("Failed to create document.\n" + ex);
            }
        }

        private static void ApplyNewDocSettings(DocumentData document, Dialogs.NewDialog dialog)
        {
            try
            {
                if (document?.TreeNodes == null || document.TreeNodes.Count == 0) return;
                var queue = new Queue<TreeNode>();
                queue.Enqueue(document.TreeNodes[0]);
                while (queue.Count > 0)
                {
                    TreeNode node = queue.Dequeue();
                    if (node is ProjSettings settings)
                    {
                        settings.Author = dialog.Author;
                        settings.AllowPractice = dialog.AllowPractice ? "true" : "false";
                        settings.AllowSCPractice = dialog.AllowSCPractice ? "true" : "false";
                    }
                    foreach (TreeNode child in node.Children) queue.Enqueue(child);
                }
            }
            catch
            {
            }
        }

        private static async Task<string> DownloadToTempAsync(string url, string name)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Sharp-X Editor (Linux)");
            client.Timeout = TimeSpan.FromSeconds(60);
            string text = await client.GetStringAsync(url);
            string path = Path.Combine(Path.GetTempPath(), name + ".lstges");
            File.WriteAllText(path, text);
            return path;
        }

        private async void Open_Click(object sender, RoutedEventArgs e)
        {
            var files = await this.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open LuaSTG project",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("LuaSTG project") { Patterns = new[] { "*.lstges", "*.lstgproj" } },
                    new FilePickerFileType("All files") { Patterns = new[] { "*" } }
                }
            });
            if (files.Count == 0) return;
            await OpenDocumentAsync(files[0].Path.LocalPath);
        }

        private async Task OpenDocumentAsync(string path)
        {
            if (!await ConfirmDiscardUnsaved()) return;
            try
            {
                DocumentSession existing = sessions.FirstOrDefault(s =>
                    !string.IsNullOrEmpty(s.Document.DocPath) &&
                    string.Equals(s.Document.DocPath, Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    ActivateSession(existing);
                    SetStatus($"Already open: '{existing.Document.DocName}'");
                    return;
                }
                session = await DocumentSession.OpenAsync(path);
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError("Failed to open document.\n" + ex);
                return;
            }
            RegisterSession(session);
            SetStatus($"Opened '{session.Document.DocName}' | {MessageContainer.Messages.Count} message(s)");
            AddRecentlyOpened(path);
        }

        private void RegisterSession(DocumentSession opened)
        {
            if (!sessions.Contains(opened))
                sessions.Add(opened);
            ActivateSession(opened);
        }

        private void ActivateSession(DocumentSession target)
        {
            session = target;
            var tree = this.FindControl<TreeView>("DocTree");
            tree.SelectedItem = null;
            tree.ItemsSource = session?.Document.TreeNodes;
            this.FindControl<ListBox>("AttrList").ItemsSource = null;
            RefreshDocStrip();
            RefreshAll();
            Title = session?.Document == null
                ? VersionName
                : $"{VersionName} - {session.Document.RawDocName}";
            UpdateDiscordPresence();
        }

        private void UpdateDiscordPresence()
        {
            if (!LinuxSettings.Instance.UseDiscordRpc) return;
            try
            {
                if (session?.Document != null)
                    discord.SetEditing(session.Document.RawDocName, session.Document.StartedTimestamp);
                else
                    discord.Reset();
            }
            catch
            {
            }
        }

        private void RefreshDocStrip()
        {
            suppressDocStrip = true;
            try
            {
                var strip = this.FindControl<ListBox>("DocStrip");
                strip.ItemsSource = sessions.ToList();
                strip.SelectedIndex = session == null ? -1 : sessions.IndexOf(session);
            }
            finally
            {
                suppressDocStrip = false;
            }
        }

        private void DocStrip_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (suppressDocStrip) return;
            int index = this.FindControl<ListBox>("DocStrip").SelectedIndex;
            if (index >= 0 && index < sessions.Count && sessions[index] != session)
                ActivateSession(sessions[index]);
        }

        private void AfterDocumentOpened(string status)
        {
            RegisterSession(session);
            SetStatus($"{status} | {MessageContainer.Messages.Count} message(s)");
        }

        private void AddRecentlyOpened(string path)
        {
            try
            {
                var list = LinuxSettings.Instance.RecentlyOpened;
                list.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
                list.Insert(0, path);
                while (list.Count > 12) list.RemoveAt(list.Count - 1);
                LinuxSettings.Instance.Save();
                RefreshRecentList();
            }
            catch
            {
            }
        }

        private void RefreshRecentList()
        {
            var menu = this.FindControl<MenuItem>("RecentMenu");
            menu.Items.Clear();
            foreach (string path in LinuxSettings.Instance.RecentlyOpened)
            {
                if (string.IsNullOrEmpty(path)) continue;
                var item = new MenuItem { Header = path };
                item.Click += async (sender, e) =>
                {
                    if (File.Exists(path))
                        await OpenDocumentAsync(path);
                    else
                    {
                        LinuxSettings.Instance.RecentlyOpened.Remove(path);
                        LinuxSettings.Instance.Save();
                        RefreshRecentList();
                    }
                };
                menu.Items.Add(item);
            }
            if (menu.Items.Count == 0)
                menu.Items.Add(new MenuItem { Header = "(empty)", IsEnabled = false });
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (session?.Document == null)
            {
                SetStatus("No document open.");
                return;
            }
            if (string.IsNullOrEmpty(session.Document.DocPath))
            {
                await SaveAsFlow();
                return;
            }
            try
            {
                bool ok = session.Save(false);
                SetStatus(ok ? $"Saved project: {session.Document.DocPath}" : "Save failed.");
                RefreshAll();
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError("Save failed.\n" + ex);
            }
        }

        private async void SaveAs_Click(object sender, RoutedEventArgs e)
        {
            if (session?.Document == null)
            {
                SetStatus("No document open.");
                return;
            }
            await SaveAsFlow();
        }

        private async Task SaveAsFlow()
        {
            var file = await this.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save project",
                SuggestedFileName = session.Document.DocName,
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("LuaSTG project") { Patterns = new[] { "*.lstges", "*.lstgproj" } },
                    new FilePickerFileType("All files") { Patterns = new[] { "*" } }
                }
            });
            if (file == null) return;
            try
            {
                string path = file.Path.LocalPath;
                session.Document.DocPath = path;
                session.Document.DocName = Path.GetFileName(path);
                bool ok = session.Save(false);
                SetStatus(ok ? $"Saved project: {path}" : "Save failed.");
                AddRecentlyOpened(path);
                RefreshAll();
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError("Save failed.\n" + ex);
            }
        }

        private async void SaveLua_Click(object sender, RoutedEventArgs e)
        {
            if (session?.Document == null)
            {
                SetStatus("No document open.");
                return;
            }
            var file = await this.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save compiled lua",
                SuggestedFileName = Path.ChangeExtension(session.Document.DocName, ".lua"),
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Lua") { Patterns = new[] { "*.lua" } },
                    new FilePickerFileType("All files") { Patterns = new[] { "*" } }
                }
            });
            if (file == null) return;
            try
            {
                if (TestError()) return;
                session.Document.GatherCompileInfo(LinuxSettings.Instance);
                session.SaveCode(file.Path.LocalPath);
                SetStatus($"Wrote lua: {file.Path.LocalPath}");
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError("SaveCode failed.\n" + ex);
            }
        }

        private async void Close_Click(object sender, RoutedEventArgs e)
        {
            if (session?.Document == null) return;
            await CloseSessionAsync(session);
        }

        private void DocTabClose_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is DocumentSession target)
                _ = CloseSessionAsync(target);
        }

        private async System.Threading.Tasks.Task CloseSessionAsync(DocumentSession target)
        {
            if (target?.Document == null) return;
            if (target != session) ActivateSession(target);
            if (!await ConfirmDiscardUnsaved()) return;
            try
            {
                session.Document.OnClosing();
            }
            catch
            {
            }
            sessions.Remove(session);
            ActivateSession(sessions.Count > 0 ? sessions[sessions.Count - 1] : null);
            SetStatus(session == null ? "No document open." : $"Switched to '{session.Document.DocName}'");
        }

        private async Task<bool> ConfirmDiscardUnsaved()
        {
            if (session?.Document == null || !session.Document.IsUnsaved) return true;
            int choice = await ConfirmDialog.AskAsync(this, "LuaSTG Editor Ava",
                $"Do you want to save \"{session.Document.RawDocName}\"?", "Yes", "No", "Cancel");
            if (choice == 2 || choice < 0) return false;
            if (choice == 0)
            {
                if (string.IsNullOrEmpty(session.Document.DocPath))
                    await SaveAsFlow();
                else
                    session.Save(false);
                return !session.Document.IsUnsaved;
            }
            return true;
        }

        private static TimeSpan AutoSaveInterval()
        {
            int minutes = LinuxSettings.Instance.AutoSaveTimer;
            if (minutes < 1) minutes = 1;
            if (minutes > 1440) minutes = 1440;
            return TimeSpan.FromMinutes(minutes);
        }

        private void RunAutoSave()
        {
            if (!LinuxSettings.Instance.UseAutoSave) return;
            int saved = 0;
            foreach (DocumentSession candidate in sessions.ToList())
            {
                try
                {
                    if (candidate?.Document == null || !candidate.Document.IsUnsaved) continue;
                    if (string.IsNullOrEmpty(candidate.Document.DocPath)) continue;
                    if (candidate.Save(false)) saved++;
                }
                catch
                {
                }
            }
            if (saved > 0)
            {
                SetStatus($"Auto-saved {saved} project(s).");
                RefreshAll();
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private bool allowClose;

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            base.OnClosing(e);
            if (allowClose) return;
            e.Cancel = true;
            _ = ConfirmQuitAsync();
        }

        private async System.Threading.Tasks.Task ConfirmQuitAsync()
        {
            foreach (DocumentSession candidate in sessions.ToList())
            {
                if (candidate?.Document == null || !candidate.Document.IsUnsaved) continue;
                ActivateSession(candidate);
                int choice = await ConfirmDialog.AskAsync(this, "LuaSTG Editor Ava",
                    $"Do you want to save \"{candidate.Document.RawDocName}\"?",
                    "Yes", "No", "Cancel");
                if (choice == 2 || choice < 0) return;
                if (choice == 0)
                {
                    if (string.IsNullOrEmpty(candidate.Document.DocPath))
                        await SaveAsFlow();
                    else
                        candidate.Save(false);
                    if (candidate.Document.IsUnsaved) return;
                }
            }
            foreach (DocumentSession candidate in sessions.ToList())
            {
                try
                {
                    candidate.Document.OnClosing();
                }
                catch
                {
                }
            }
            try
            {
                if (LinuxSettings.LoadedOk)
                    LinuxSettings.Instance.Save();
            }
            catch
            {
            }
            allowClose = true;
            Close();
        }


        private async void EditNode_Click(object sender, RoutedEventArgs e)
        {
            TreeNode selected = SelectedNode;
            if (selected == null || session?.Document == null) return;
            try
            {
                AttrItem ai = selected.GetRCInvoke();
                if (ai == null) return;
                var iw = AvaloniaInputSelector.SelectInputWindow(ai, ai.EditWindow, ai.AttrInput);
                if (iw is IAvaloniaInputWindow win && await win.ShowDialogAsync(this) == true)
                {
                    session.Document.AddAndExecuteCommand(new EditAttrCommand(ai, ai.AttrInput, win.Result));
                    RefreshAll();
                }
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError("Edit failed.\n" + ex);
            }
        }

        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            if (session?.Document == null) return;
            try
            {
                session.Document.Undo();
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError("Undo failed.\n" + ex);
            }
            RefreshAll();
        }

        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            if (session?.Document == null) return;
            try
            {
                session.Document.Redo();
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError("Redo failed.\n" + ex);
            }
            RefreshAll();
        }

        private void Cut_Click(object sender, RoutedEventArgs e)
        {
            TreeNode selected = SelectedNode;
            if (selected == null || session?.Document == null) return;
            try
            {
                clipBoard = (TreeNode)selected.Clone();
                TreeNode prev = selected.GetNearestEdited();
                session.Document.AddAndExecuteCommand(new DeleteCommand(selected));
                if (prev != null) Reveal(prev);
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError("Cut failed.\n" + ex);
            }
            RefreshAll();
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            TreeNode selected = SelectedNode;
            if (selected == null) return;
            try
            {
                clipBoard = (TreeNode)selected.Clone();
                SetStatus($"Copied: {clipBoard}");
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError("Copy failed.\n" + ex);
            }
        }

        private void Paste_Click(object sender, RoutedEventArgs e)
        {
            if (clipBoard == null || session?.Document == null) return;
            try
            {
                TreeNode node = (TreeNode)clipBoard.Clone();
                node.FixParentDoc(session.Document);
                Insert(node, false);
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError("Paste failed.\n" + ex);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            TreeNode selected = SelectedNode;
            if (selected == null || session?.Document == null) return;
            try
            {
                TreeNode prev = selected.GetNearestEdited();
                session.Document.AddAndExecuteCommand(new DeleteCommand(selected));
                if (prev != null) Reveal(prev);
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError("Delete failed.\n" + ex);
            }
            RefreshAll();
        }

        private void Ban_Click(object sender, RoutedEventArgs e)
        {
            TreeNode selected = SelectedNode;
            if (selected == null) return;
            try
            {
                selected.IsBanned_InvokeCommand = !selected.IsBanned;
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError("Switch ban failed.\n" + ex);
            }
            RefreshAll();
        }

        private void Fold_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SelectedNode?.FoldTree();
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError("Fold failed.\n" + ex);
            }
        }

        private void Unfold_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SelectedNode?.ExpandTree();
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError("Unfold failed.\n" + ex);
            }
        }

        private void FoldRegion_Click(object sender, RoutedEventArgs e)
        {
            TreeNode selected = SelectedNode;
            if (selected == null || session?.Document == null) return;
            try
            {
                if (!(selected is Region beg) || beg.Parent == null) return;
                Region end = null;
                var toFold = new ObservableCollection<TreeNode>();
                TreeNode p = beg.Parent;
                bool inSel = false;
                for (int i = 0; i < p.Children.Count; i++)
                {
                    if (p.Children[i] != beg && p.Children[i] is Region)
                    {
                        inSel = false;
                        end = p.Children[i] as Region;
                    }
                    if (inSel) toFold.Add(p.Children[i]);
                    if (p.Children[i] == beg) inSel = true;
                }
                session.Document.AddAndExecuteCommand(new FoldRegionCommand(toFold, beg, end));
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError("Fold region failed.\n" + ex);
            }
            RefreshAll();
        }

        private void UnfoldRegion_Click(object sender, RoutedEventArgs e)
        {
            TreeNode selected = SelectedNode;
            if (selected == null || session?.Document == null) return;
            try
            {
                session.Document.AddAndExecuteCommand(new UnfoldAsRegionCommand(selected));
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError("Unfold failed.\n" + ex);
            }
            RefreshAll();
        }

        private void GoToDef_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TreeNode t = SelectedNode?.GetReferredTreeNode();
                if (t != null) Reveal(t);
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError("Go to definition failed.\n" + ex);
            }
        }


        private async void FixAttrs_Click(object sender, RoutedEventArgs e)
        {
            var files = await this.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Fix node attributes",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("LuaSTG project") { Patterns = new[] { "*.lstges", "*.lstgproj" } }
                }
            });
            if (files.Count == 0) return;
            int fixedCount = 0;
            DocumentSession firstFixed = null;
            foreach (IStorageFile f in files)
            {
                try
                {
                    DocumentSession fixer = await DocumentSession.OpenAsync(f.Path.LocalPath);
                    fixer.Document.DocPath = "";
                    var queue = new Queue<TreeNode>();
                    queue.Enqueue(fixer.Document.TreeNodes[0]);
                    while (queue.Count > 0)
                    {
                        TreeNode n = queue.Dequeue();
                        n.FixAttributesList();
                        foreach (TreeNode tn in n.Children) queue.Enqueue(tn);
                        n.CheckMessage(null, new PropertyChangedEventArgs(""));
                    }
                    fixedCount++;
                    firstFixed ??= fixer;
                }
                catch (Exception ex)
                {
                    EditorAppContext.Dialogs.ShowError($"Failed to fix '{f.Path.LocalPath}'.\n" + ex);
                }
            }
            if (firstFixed != null)
            {
                sessions.Add(firstFixed);
                ActivateSession(firstFixed);
            }
            SetStatus($"Fixed attributes in {fixedCount} file(s).");
        }


        public void ApplyCompactView() => ApplyCompactView(LinuxSettings.Instance.CompactView);

        public void ApplyCompactView(bool compact)
        {
            try
            {
                var tree = this.FindControl<TreeView>("DocTree");
                if (compact)
                {
                    if (!tree.Classes.Contains("compact")) tree.Classes.Add("compact");
                }
                else
                {
                    tree.Classes.Remove("compact");
                }
                var attrs = this.FindControl<ListBox>("AttrList");
                if (compact)
                {
                    if (!attrs.Classes.Contains("compact")) attrs.Classes.Add("compact");
                }
                else
                {
                    attrs.Classes.Remove("compact");
                }
            }
            catch
            {
            }
        }

        private void SetInsertState(CommandTypeFac state)
        {
            insertState = state;
            this.FindControl<MenuItem>("InsBeforeMenu").IsChecked = state is BeforeFac;
            this.FindControl<MenuItem>("InsAfterMenu").IsChecked = state is AfterFac;
            this.FindControl<MenuItem>("InsChildMenu").IsChecked = state is ChildFac;
            this.FindControl<MenuItem>("InsParentMenu").IsChecked = state is ParentFac;
            SyncInsertRadios();
            SetStatus($"Insert mode: {state.GetType().Name.Replace("Fac", "")}");
        }


        private static string PresetDirectory()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "LuaSTG Editor Ava Presets");
            Directory.CreateDirectory(dir);
            return dir;
        }

        private void RefreshPresetList()
        {
            var menu = this.FindControl<MenuItem>("PresetsMenu");
            menu.Items.Clear();
            try
            {
                string[] files = Directory.GetFiles(PresetDirectory(), "*.lstgpreset", SearchOption.AllDirectories);
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                foreach (string file in files)
                {
                    var item = new MenuItem { Header = Path.GetFileName(file) };
                    ToolTip.SetTip(item, file);
                    item.Click += async (sender, e) => await InsertPresetFile(file);
                    menu.Items.Add(item);
                }
            }
            catch
            {
            }
            if (menu.Items.Count == 0)
                menu.Items.Add(new MenuItem { Header = "(no presets)", IsEnabled = false });
        }

        private async Task InsertPresetFile(string path)
        {
            if (session?.Document == null)
            {
                SetStatus("Open a document before inserting a preset.");
                return;
            }
            try
            {
                TreeNode t = await DocumentData.CreateNodeFromFileAsync(path, session.Document);
                if (t.Children == null || t.Children.Count < 1 || t.Children[0] == null)
                    throw new InvalidDataException("Preset is empty.");
                Insert(t.Children[0], false);
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError($"Failed to load preset.\n{ex}");
            }
        }

        private async void SavePreset_Click(object sender, RoutedEventArgs e)
        {
            TreeNode selected = SelectedNode;
            if (selected == null)
            {
                SetStatus("Select a node to save as preset.");
                return;
            }
            var file = await this.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save preset",
                SuggestedStartLocation = null,
                SuggestedFileName = "preset.lstgpreset",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Lua presets") { Patterns = new[] { "*.lstgpreset" } }
                }
            });
            if (file == null) return;
            try
            {
                TreeNode root = new RootFolder(null);
                root.AddChild((TreeNode)selected.Clone());
                using var stream = new FileStream(file.Path.LocalPath, FileMode.Create, FileAccess.Write);
                using var writer = new StreamWriter(stream, Encoding.UTF8);
                root.SerializeFile(writer, 0);
                RefreshPresetList();
                SetStatus($"Preset saved: {file.Path.LocalPath}");
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError($"Unable to write preset.\n{ex}");
            }
        }


        private bool TestError()
        {
            try
            {
                var settings = LinuxSettings.Instance;
                if (session?.Document != null && settings.UseFolderPacking)
                {
                    string docDir = Path.GetDirectoryName(session.Document.DocPath);
                    session.Document.GatherCompileInfo(settings);
                    if (docDir == session.Document.CompileProcess.targetZipPath)
                    {
                        EditorAppContext.Dialogs.ShowError(
                            "Project files are under output directory.\nThe output directory will be deleted before build. DO NOT save project files in the output directory!");
                        return true;
                    }
                }
                if (!MessageContainer.IsNoError())
                {
                    this.FindControl<TabControl>("BottomTabs").SelectedIndex = 0;
                    EditorAppContext.Dialogs.ShowError(
                        "Errors are found in the editor. The project cannot be compiled if any error is present.");
                    return true;
                }
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError("Pre-compile check failed.\n" + ex);
                return true;
            }
            return false;
        }

        private void PackProject(bool run, TreeNode scDebugger = null, TreeNode stageDebugger = null)
        {
            if (session?.Document == null)
            {
                SetStatus("No document open.");
                return;
            }
            DocumentData current = session.Document;
            var settings = LinuxSettings.Instance;
            bool saveMeta = settings.SaveResMeta;
            try
            {
                if (TestError()) return;
                if (!run)
                {
                    saveMeta = settings.SaveResMeta;
                    settings.SaveResMeta = false;
                }
                if (settings.DebugSaveProj && !SaveActiveFileSync()) return;
                GlobalCompileData.SCDebugger = scDebugger;
                GlobalCompileData.StageDebugger = stageDebugger;
                SetStatus(run ? "Compiling and preparing run..." : "Packing...");
                Dispatcher.UIThread.Post(() =>
                {
                    this.FindControl<TabControl>("BottomTabs").SelectedIndex = 2;
                    debugLogStickToBottom = true;
                    AppendDebugLog($"=== {(run ? "Run" : "Pack")} started: {current.DocName} ===");
                });
                Task.Run(() =>
                {
                    try
                    {
                        current.GatherCompileInfo(settings);
                        current.CompileProcess.ProgressChanged += (o, ev) =>
                        {
                            string line = ev.UserState?.ToString();
                            if (!string.IsNullOrEmpty(line))
                                Dispatcher.UIThread.Post(() => AppendDebugLog(line));
                        };
                        current.CompileProcess.ExecuteProcess(scDebugger != null, stageDebugger != null, settings);
                        Dispatcher.UIThread.Post(() => FinishPack(run, saveMeta, null));
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.UIThread.Post(() => FinishPack(run, saveMeta, ex));
                    }
                });
            }
            catch (Exception ex)
            {
                settings.SaveResMeta = saveMeta;
                EditorAppContext.Dialogs.ShowError("Pack failed.\n" + ex);
            }
        }

        private void FinishPack(bool run, bool saveMeta, Exception error)
        {
            var settings = LinuxSettings.Instance;
            if (!run) settings.SaveResMeta = saveMeta;
            if (error != null)
            {
                AppendDebugLog("Pack failed: " + error.Message);
                EditorAppContext.Dialogs.ShowError("Pack failed.\n" + error);
                SetStatus("Pack failed.");
                return;
            }
            if (run)
            {
                string target = "";
                try
                {
                    target = session.Document.CompileProcess.targetZipPath;
                }
                catch
                {
                }
                string modName = "";
                try
                {
                    modName = session.Document.CompileProcess.projName ?? "";
                }
                catch
                {
                }
                string parameter = WineRunner.BuildParameter(settings, modName);
                string engineExe = LinuxSettings.Instance.LuaSTGExecutablePath;
                if (activeEngine != null)
                {
                    bool wasAlive = !activeEngine.HasExited;
                    activeEngine.Detach();
                    activeEngine.Dispose();
                    activeEngine = null;
                    if (wasAlive)
                        AppendDebugLog("(previous engine detached)");
                }
                WineRunner.EngineCapture capture = null;
                var launched = WineRunner.LaunchWithLog(engineExe, parameter, settings,
                    line => EngineLog(line),
                    code =>
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            FlushEngineLog();
                            AppendDebugLog($"--- engine exited (code {code}) ---");
                            if (ReferenceEquals(activeEngine, capture))
                                activeEngine = null;
                            SetStatus($"Engine exited (code {code}).");
                        });
                    });
                string launchError = launched.Error;
                capture = launched.Capture;
                if (launchError == null)
                    activeEngine = capture;
                bool native = launchError == null &&
                    WineRunner.DetectBinaryKind(engineExe) == WineRunner.EngineBinaryKind.Native;
                AppendDebugLog(launchError == null
                    ? $"Launched engine{(native ? " (native Linux binary)" : "")}" +
                      $"{(string.IsNullOrEmpty(target) ? "." : $": {target}")}"
                    : "Launch failed: " + launchError);
                if (launchError != null)
                {
                    EditorAppContext.Dialogs.ShowError(
                        "Project compiled" + (string.IsNullOrEmpty(target) ? "." : $" to:\n{target}") +
                        "\n\n" + launchError, "Run");
                    SetStatus("Compiled. Engine launch failed.");
                }
                else
                {
                    SetStatus("Compiled. Engine launched.");
                }
            }
            else
            {
                AppendDebugLog("Pack successful.");
                SetStatus("Pack successful.");
            }
            RefreshAll();
        }

        private bool SaveActiveFileSync()
        {
            try
            {
                if (string.IsNullOrEmpty(session.Document.DocPath)) return true;
                return session.Save(false);
            }
            catch
            {
                return false;
            }
        }

        private void ScDebug_Click(object sender, RoutedEventArgs e)
        {
            TreeNode t = SelectedNode;
            while (t != null)
            {
                if (PluginHandler.Plugin != null && PluginHandler.Plugin.MatchBossSCNodeTypes(t.GetType()))
                    break;
                t = t.Parent;
            }
            PackProject(run: true, scDebugger: t);
        }

        private void StageDebug_Click(object sender, RoutedEventArgs e)
        {
            PackProject(run: true, stageDebugger: SelectedNode);
        }


        private void FileFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenWithShell(Path.GetDirectoryName(session?.Document?.DocPath));
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError("Cannot open folder.\n" + ex);
            }
        }

        private void ModFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string exe = LinuxSettings.Instance.LuaSTGExecutablePath;
                if (string.IsNullOrEmpty(exe))
                {
                    EditorAppContext.Dialogs.ShowError("LuaSTG executable path is not set (Settings/Compiler).");
                    return;
                }
                OpenWithShell(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(exe), "mod")));
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError("Cannot open folder.\n" + ex);
            }
        }

        private static void OpenWithShell(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new InvalidOperationException("No path.");
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }

        private void ViewCode_Click(object sender, RoutedEventArgs e)
        {
            TreeNode selected = SelectedNode;
            if (selected == null || session?.Document == null) return;
            try
            {
                if (TestError()) return;
                session.Document.GatherCompileInfo(LinuxSettings.Instance);
                var dialog = new CodePreviewDialog("Code preview",
                    string.Concat(selected.ToLua(0)));
                _ = dialog.ShowDialog(this);
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError("Failed to compile code.\n" + ex);
            }
        }

        private void Definitions_Click(object sender, RoutedEventArgs e)
        {
            if (session?.Document == null)
            {
                SetStatus("No document open.");
                return;
            }
            _ = new DefinitionsDialog(session.Document).ShowDialog(this);
        }


        private void OpenSettings(int tab)
        {
            _ = new SettingsDialog(tab).ShowDialog(this);
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            _ = new AboutDialog().ShowDialog(this);
        }

        private void BuildToolbar()
        {
            var strip = this.FindControl<StackPanel>("ToolbarStrip");
            if (strip == null) return;
            strip.Children.Clear();
            AddToolButton(strip, "new.png", "New File (Ctrl+N)", (s, e) => New_Click(s, null));
            AddToolButton(strip, "open.png", "Open File (Ctrl+O)", (s, e) => Open_Click(s, null));
            AddToolButton(strip, "save.png", "Save File (Ctrl+S)", (s, e) => Save_Click(s, null));
            AddToolSeparator(strip);
            AddToolButton(strip, "viewcode.png", "View code in this node", (s, e) => ViewCode_Click(s, null));
            AddToolButton(strip, "savecode.png", "Export Code", (s, e) => SaveLua_Click(s, null));
            AddToolSeparator(strip);
            AddToolButton(strip, "undo.png", "Undo (Ctrl+Z)", (s, e) => Undo_Click(s, null));
            AddToolButton(strip, "redo.png", "Redo (Ctrl+Y)", (s, e) => Redo_Click(s, null));
            AddToolSeparator(strip);
            AddToolButton(strip, "cut.png", "Cut (Ctrl+X)", (s, e) => Cut_Click(s, null));
            AddToolButton(strip, "copy.png", "Copy (Ctrl+C)", (s, e) => Copy_Click(s, null));
            AddToolButton(strip, "paste.png", "Paste (Ctrl+V)", (s, e) => Paste_Click(s, null));
            AddToolSeparator(strip);
            AddToolButton(strip, "delete.png", "Delete (Del)", (s, e) => Delete_Click(s, null));
            AddToolSeparator(strip);
            AddToolButton(strip, "foldregion.png", "Fold Region", (s, e) => FoldRegion_Click(s, null));
            AddToolButton(strip, "unfoldasregion.png", "Unfold As Region", (s, e) => UnfoldRegion_Click(s, null));
            AddToolSeparator(strip);
            AddToolButton(strip, "pack.png", "Pack Project", (s, e) => PackProject(run: false));
            AddToolButton(strip, "run.png", "Run Project", (s, e) => PackProject(run: true));
            AddToolButton(strip, "debugsc.png", "Test SpellCard", (s, e) => ScDebug_Click(s, null));
            AddToolButton(strip, "debugstage.png", "Test Stage Node", (s, e) => StageDebug_Click(s, null));
            AddToolSeparator(strip);
            insAfterButton = AddInsertButton(strip, "down.png", "Insert After (Alt+Down)",
                () => SetInsertState(new AfterFac()));
            insBeforeButton = AddInsertButton(strip, "Up.png", "Insert Before (Alt+Up)",
                () => SetInsertState(new BeforeFac()));
            insChildButton = AddInsertButton(strip, "child.png", "Insert As Child (Alt+Right)",
                () => SetInsertState(new ChildFac()));
            insParentButton = AddInsertButton(strip, "parent.png", "Insert As Parent (Alt+Left)",
                () => SetInsertState(new ParentFac()));
            SyncInsertRadios();
        }

        private void AddToolButton(StackPanel strip, string icon, string tip,
            EventHandler<RoutedEventArgs> onClick)
        {
            var image = new Image
            {
                Width = 24,
                Height = 24,
                Source = Services.IconCache.GetIcon(icon)
            };
            var button = new Button
            {
                Content = image,
                Padding = new Thickness(2),
                Margin = new Thickness(0)
            };
            ToolTip.SetTip(button, tip);
            button.Click += onClick;
            strip.Children.Add(button);
        }

        private static void AddToolSeparator(StackPanel strip)
        {
            strip.Children.Add(new Border
            {
                Width = 1,
                Background = Brushes.Gray,
                Opacity = 0.4,
                Margin = new Thickness(5, 4, 5, 4)
            });
        }

        private Button AddInsertButton(StackPanel strip, string icon, string tip, Action select)
        {
            var image = new Image
            {
                Width = 24,
                Height = 24,
                Source = Services.IconCache.GetIcon(icon)
            };
            var button = new Button
            {
                Content = image,
                Padding = new Thickness(2),
                Margin = new Thickness(0),
                BorderThickness = new Thickness(0)
            };
            ToolTip.SetTip(button, tip);
            button.Click += (sender, e) => select();
            strip.Children.Add(button);
            return button;
        }

        private void SyncInsertRadios()
        {
            if (insAfterButton == null) return;
            HighlightInsertButton(insAfterButton, insertState is AfterFac);
            HighlightInsertButton(insBeforeButton, insertState is BeforeFac);
            HighlightInsertButton(insChildButton, insertState is ChildFac);
            HighlightInsertButton(insParentButton, insertState is ParentFac);
        }

        private static void HighlightInsertButton(Button button, bool active)
        {
            if (active)
            {
                button.Background = new SolidColorBrush(Color.FromArgb(70, 100, 149, 237));
                button.BorderBrush = new SolidColorBrush(Color.FromRgb(100, 149, 237));
                button.BorderThickness = new Thickness(1);
            }
            else
            {
                button.ClearValue(Button.BackgroundProperty);
                button.ClearValue(Button.BorderBrushProperty);
                button.BorderThickness = new Thickness(0);
            }
        }

        private void EnsureToolbox()
        {
            if (toolbox != null) return;            try
            {
                toolbox = new PluginToolbox(this);
                var views = toolbox.ToolboxTabs.Select(t => new ToolboxTabView
                {
                    Header = t.Header,
                    Items = t.Data.ToList()
                }).ToList();
                var strip = this.FindControl<StackPanel>("ToolboxTabStrip");
                strip.Children.Clear();
                for (int i = 0; i < views.Count; i++)
                {
                    int index = i;
                    var tab = new Button
                    {
                        Content = views[index].Header,
                        FontSize = 12,
                        Padding = new Thickness(8, 3),
                        Margin = new Thickness(0),
                        Tag = index
                    };
                    tab.Click += (sender, e) => SelectToolboxTab(views, index);
                    strip.Children.Add(tab);
                }
                SelectToolboxTab(views, lastToolboxTab >= 0 && lastToolboxTab < views.Count
                    ? lastToolboxTab : 0);
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError("Failed to build toolbox.\n" + ex);
            }
        }

        private void SelectToolboxTab(List<ToolboxTabView> views, int index)
        {
            if (index < 0 || index >= views.Count) return;
            lastToolboxTab = index;
            var strip = this.FindControl<StackPanel>("ToolboxTabStrip");
            for (int i = 0; i < strip.Children.Count; i++)
            {
                if (strip.Children[i] is Button tab)
                    tab.FontWeight = i == index
                        ? FontWeight.Bold
                        : FontWeight.Normal;
            }
            var icons = this.FindControl<StackPanel>("ToolboxIconStrip");
            icons.Children.Clear();
            foreach (ToolboxItemData item in views[index].Items)
            {
                if (item.IsSeperator)
                {
                    icons.Children.Add(new Border
                    {
                        Width = 1,
                        Background = Brushes.Gray,
                        Opacity = 0.4,
                        Margin = new Thickness(6, 4, 6, 4)
                    });
                    continue;
                }
                var image = new Image
                {
                    Width = 24,
                    Height = 24,
                    Source = Services.IconCache.GetIcon(item.Image)
                };
                var button = new Button
                {
                    Content = image,
                    DataContext = item,
                    Padding = new Thickness(3),
                    Margin = new Thickness(0)
                };
                ToolTip.SetTip(button, item.ToolTip);
                button.Click += ToolboxButton_Click;
                icons.Children.Add(button);
            }
        }

        private void ToolboxButton_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.DataContext as ToolboxItemData;
            if (item == null || string.IsNullOrEmpty(item.Tag)) return;
            if (session?.Document == null)
            {
                SetStatus("Open a document before inserting nodes.");
                return;
            }
            if (toolbox == null) return;
            try
            {
                if (item.Tag.StartsWith("cusNode_", StringComparison.OrdinalIgnoreCase))
                {
                    // custom node buttons carry their script file name in CustomScripts
                    if (!toolbox.CNFuncs.TryGetValue(item.Tag, out var addCustom) || addCustom == null) return;
                    toolbox.CustomScripts.TryGetValue(item.Tag, out string script);
                    addCustom.Invoke(script ?? item.Tag.Substring("cusNode_".Length));
                }
                else if (toolbox.NFuncs.TryGetValue(item.Tag, out var add) && add != null)
                {
                    add.Invoke();
                }
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError($"Insert '{item.ToolTip}' failed.\n" + ex);
            }
            RefreshAll();
        }

        private async void CustomNodes_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CustomNodesDialog(ReloadToolboxAsync);
            await dialog.ShowDialog(this);
        }

        private System.Threading.Tasks.Task ReloadToolboxAsync()
        {
            toolbox = null;
            EnsureToolbox();
            int count = toolbox?.CustomScripts.Count ?? 0;
            SetStatus($"Custom nodes reloaded ({count} registered).");
            return System.Threading.Tasks.Task.CompletedTask;
        }


        private static readonly List<PluginTool> pluginTools = new List<PluginTool>
        {
            new DanmakuRandomizerPluginTool()
        };

        // Library Tools is temporarily hidden until its behavior can be verified.
        private static readonly bool EnableLibraryTools = false;

        private void RefreshPluginTools()
        {
            var parent = this.FindControl<MenuItem>("LibraryToolsMenu");
            if (parent == null) return;
            parent.Items.Clear();
            parent.IsVisible = EnableLibraryTools;
            if (!EnableLibraryTools) return;
            foreach (PluginTool tool in pluginTools)
            {
                PluginTool captured = tool;
                var item = new MenuItem { Header = captured.Name };
                item.Click += async (sender, e) => await ExecuteLibraryToolAsync(captured);
                parent.Items.Add(item);
            }
        }

        private async System.Threading.Tasks.Task ExecuteLibraryToolAsync(PluginTool tool)
        {
            PluginToolResult result;
            if (tool is DanmakuRandomizerPluginTool randomizer)
            {
                result = await randomizer.ExecuteAsync(this, SelectedNode);
            }
            else
            {
                result = tool.ExecutePlugin(new PluginToolParameter(SelectedNode));
            }
            clipBoard = result.clipBoard;
            if (result.newDocument != null)
            {
                session?.Collection.AddAndAllocHash(result.newDocument);
            }
            foreach (Command c in result.commands)
            {
                session?.Document.AddAndExecuteCommand(c);
            }
            RefreshAll();
            SetStatus($"Library tool '{tool.Name}': " +
                $"clipboard {(clipBoard == null ? "empty" : "ready")}.");
        }

        private void InsertNode(TreeNode t)
        {
            Insert(t, true);
        }


        private void DocTree_Changed(object sender, SelectionChangedEventArgs e)
        {
            TreeNode node = SelectedNode;
            this.FindControl<ListBox>("AttrList").ItemsSource = node?.attributes;
            Title = session?.Document == null
                ? VersionName
                : $"{VersionName} - {session.Document.RawDocName}";
        }

        private void DocTree_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            try
            {
                var props = e.GetCurrentPoint(this.FindControl<TreeView>("DocTree")).Properties;
                if (props.IsRightButtonPressed)
                {
                    if ((e.Source as Control)?.DataContext is TreeNode node)
                        this.FindControl<TreeView>("DocTree").SelectedItem = node;
                }
                else if (props.IsLeftButtonPressed)
                {
                    dragStartPoint = e.GetPosition(this.FindControl<TreeView>("DocTree"));
                    dragNode = (e.Source as Control)?.DataContext as TreeNode;
                }
            }
            catch
            {
            }
        }

        private async void DocTree_PointerMoved(object sender, PointerEventArgs e)
        {
            if (dragNode == null || dragStartPoint == null) return;
            try
            {
                var props = e.GetCurrentPoint(this.FindControl<TreeView>("DocTree")).Properties;
                if (!props.IsLeftButtonPressed) return;
                Point here = e.GetPosition(this.FindControl<TreeView>("DocTree"));
                var start = dragStartPoint.Value;
                if (Math.Abs(here.X - start.X) < 6 && Math.Abs(here.Y - start.Y) < 6) return;
                TreeNode dragged = dragNode;
                dragNode = null;
                dragStartPoint = null;
                var data = new DataObject();
                data.Set("lstg-node", dragged);
                await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
            }
            catch
            {
            }
        }

        private void DocTree_DragOver(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.Contains("lstg-node"))
                    e.DragEffects = DragDropEffects.Move;
            }
            catch
            {
            }
        }

        private void DocTree_Drop(object sender, DragEventArgs e)
        {
            TreeNode dragged = null;
            try
            {
                dragged = e.Data.Get("lstg-node") as TreeNode;
            }
            catch
            {
            }
            TreeNode target = (e.Source as Control)?.DataContext as TreeNode;
            if (dragged == null || target == null || session?.Document == null) return;
            if (dragged == target) return;
            try
            {
                if (!target.ValidateChild(dragged))
                {
                    EditorAppContext.Dialogs.ShowError("Cannot move there.", "Move");
                    return;
                }
                var move = new MoveCommand(dragged, target, target.Children.Count);
                if (session.Document.AddAndExecuteCommand(move))
                    Reveal(dragged);
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError("Move failed.\n" + ex);
            }
            RefreshAll();
        }

        private async void Window_Drop(object sender, DragEventArgs e)
        {
            List<string> paths = new List<string>();
            try
            {
                foreach (IStorageItem item in e.Data.GetFiles() ?? Enumerable.Empty<IStorageItem>())
                {
                    if (item is IStorageFile file)
                    {
                        string local = file.TryGetLocalPath();
                        if (local != null && (local.EndsWith(".lstges", StringComparison.OrdinalIgnoreCase) ||
                            local.EndsWith(".lstgproj", StringComparison.OrdinalIgnoreCase)))
                            paths.Add(local);
                    }
                }
            }
            catch
            {
            }
            if (paths.Count == 0) return;
            await OpenDocumentAsync(paths[0]);
        }

        private void SearchBox_Changed(object sender, TextChangedEventArgs e)
        {
            var box = this.FindControl<TextBox>("SearchBox");
            var results = this.FindControl<ListBox>("SearchResults");
            string query = box.Text?.Trim();
            if (session?.Document == null || string.IsNullOrEmpty(query) || query.Length < 2)
            {
                results.IsVisible = false;
                results.ItemsSource = null;
                return;
            }
            try
            {
                var hits = new List<TreeNode>();
                var queue = new Queue<TreeNode>();
                queue.Enqueue(session.Document.TreeNodes[0]);
                while (queue.Count > 0 && hits.Count < 100)
                {
                    TreeNode n = queue.Dequeue();
                    if (n.ToString() != null &&
                        n.ToString().Contains(query, StringComparison.OrdinalIgnoreCase))
                        hits.Add(n);
                    foreach (TreeNode child in n.Children) queue.Enqueue(child);
                }
                results.ItemsSource = hits;
                results.IsVisible = hits.Count > 0;
            }
            catch
            {
                results.IsVisible = false;
            }
        }

        private void SearchResult_DoubleTapped(object sender, TappedEventArgs e)
        {
            if ((e.Source as Control)?.DataContext is TreeNode node)
                Reveal(node);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (TryExecuteShortcut(e)) e.Handled = true;
        }

        public bool TryExecuteShortcut(KeyEventArgs e)
        {
            try
            {
                object focused = FocusManager?.GetFocusedElement();
                bool typing = focused is TextBox;
                var mod = e.KeyModifiers;
                switch (e.Key)
                {
                    case Key.N when mod.HasFlag(KeyModifiers.Control):
                        New_Click(this, null); return true;
                    case Key.O when mod.HasFlag(KeyModifiers.Control):
                        Open_Click(this, null); return true;
                    case Key.S when mod.HasFlag(KeyModifiers.Control):
                        Save_Click(this, null); return true;
                    case Key.Z when mod.HasFlag(KeyModifiers.Control):
                        Undo_Click(this, null); return true;
                    case Key.Y when mod.HasFlag(KeyModifiers.Control):
                        Redo_Click(this, null); return true;
                    case Key.F5:
                        Refresh_Click(this, null); return true;
                    case Key.Down when mod.HasFlag(KeyModifiers.Alt):
                        SetInsertState(new AfterFac()); return true;
                    case Key.Up when mod.HasFlag(KeyModifiers.Alt):
                        SetInsertState(new BeforeFac()); return true;
                    case Key.Right when mod.HasFlag(KeyModifiers.Alt):
                        SetInsertState(new ChildFac()); return true;
                    case Key.Left when mod.HasFlag(KeyModifiers.Alt):
                        SetInsertState(new ParentFac()); return true;
                    case Key.Delete when !typing:
                        Delete_Click(this, null); return true;
                    case Key.X when mod.HasFlag(KeyModifiers.Control) && !typing:
                        Cut_Click(this, null); return true;
                    case Key.C when mod.HasFlag(KeyModifiers.Control) && !typing:
                        Copy_Click(this, null); return true;
                    case Key.V when mod.HasFlag(KeyModifiers.Control) && !typing:
                        Paste_Click(this, null); return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private void AttrCombo_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox combo && combo.DataContext is AttrItem attr)
                combo.ItemsSource = AvaloniaInputSelector.SelectComboBox(attr.EditWindow ?? string.Empty);
        }

        private void AttrCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox combo && combo.SelectedItem is string selected)
                CommitAttrBox(combo, selected);
        }

        private void AttrBox_LostFocus(object sender, RoutedEventArgs e)
        {
            CommitAttrBox(sender as ComboBox);
        }

        private void AttrBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter && e.Key != Key.Return) return;
            if (CommitAttrBox(sender as ComboBox))
                FocusManager?.ClearFocus();
            e.Handled = true;
        }

        private bool CommitAttrBox(ComboBox combo, string newText = null)
        {
            if (combo?.DataContext is not AttrItem attr || session?.Document == null) return false;
            newText ??= combo.Text ?? string.Empty;
            if (!string.Equals(attr.AttrInput, newText, StringComparison.Ordinal))
                session.Document.AddAndExecuteCommand(new EditAttrCommand(attr, attr.AttrInput, newText));
            else
                return false;
            RefreshAll();
            return true;
        }

        private async void AttrList_DoubleTapped(object sender, TappedEventArgs e)
        {
            var attr = (e.Source as Control)?.DataContext as AttrItem;
            if (attr == null) return;
            await OpenAttrEditorAsync(attr);
        }

        private void AttrEditButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is AttrItem attr)
                _ = OpenAttrEditorAsync(attr);
        }

        private async Task OpenAttrEditorAsync(AttrItem attr)
        {
            if (session?.Document == null) return;
            LuaSTGEditorSharp.Windows.Input.IInputWindow iw;
            try
            {
                iw = AvaloniaInputSelector.SelectInputWindow(attr, attr.EditWindow ?? "", attr.AttrInput);
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError("Cannot open editor.\n" + ex);
                return;
            }
            if (iw is not IAvaloniaInputWindow win) return;
            bool? ok;
            try
            {
                ok = await win.ShowDialogAsync(this);
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError("Editor failed.\n" + ex);
                return;
            }
            if (ok == true)
            {
                try
                {
                    attr.AttrInput_InvokeCommand = win.Result;
                }
                catch (Exception ex)
                {
                    EditorAppContext.Dialogs.ShowError("Apply failed.\n" + ex);
                }
                RefreshAll();
            }
        }

        private void Message_DoubleTapped(object sender, TappedEventArgs e)
        {
            var message = (e.Source as Control)?.DataContext as MessageBase;
            if (message == null) return;
            try
            {
                message.Invoke();
            }
            catch (Exception ex)
            {
                EditorAppContext.Dialogs.ShowError("Reveal failed.\n" + ex);
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshAll();
        }

        private void RefreshAll()
        {
            RefreshPreview();
            RefreshMessages();
            RefreshDocStrip();
        }

        private void AppendDebugLog(string line)
        {
            try
            {
                var box = this.FindControl<TextBox>("DebugLog");
                string current = box.Text ?? string.Empty;
                string[] lines = (current + line + "\n").Split('\n');
                if (lines.Length > 500)
                    lines = lines.Skip(lines.Length - 500).ToArray();
                box.Text = string.Join("\n", lines);
                FollowDebugLog(box);
            }
            catch
            {
            }
        }

        private void EngineLog(string line)
        {
            if (line == null) return;
            lock (engineLogLock)
            {
                engineLogPending.Add(line);
            }
        }

        private void FlushEngineLog()
        {
            List<string> batch;
            lock (engineLogLock)
            {
                if (engineLogPending.Count == 0) return;
                batch = new List<string>(engineLogPending);
                engineLogPending.Clear();
            }
            AppendDebugLogs(batch);
        }

        private void AppendDebugLogs(IReadOnlyList<string> batch)
        {
            if (batch == null || batch.Count == 0) return;
            try
            {
                var box = this.FindControl<TextBox>("DebugLog");
                string current = box.Text ?? string.Empty;
                var sb = new StringBuilder(current.Length + batch.Count * 64);
                sb.Append(current);
                foreach (string line in batch)
                {
                    sb.Append(line);
                    sb.Append('\n');
                }
                string[] lines = sb.ToString().Split('\n');
                if (lines.Length > 500)
                    lines = lines.Skip(lines.Length - 500).ToArray();
                box.Text = string.Join("\n", lines);
                FollowDebugLog(box);
            }
            catch
            {
            }
        }

        private void HookDebugLogScroll(TextBox box)
        {
            if (debugLogScrollHooked || box == null) return;
            try
            {
                ScrollViewer scroller = box.GetVisualDescendants()
                    .OfType<ScrollViewer>().FirstOrDefault();
                if (scroller == null) return;
                debugLogScrollHooked = true;
                scroller.ScrollChanged += (sender, e) =>
                {
                    try
                    {
                        debugLogStickToBottom = scroller.Offset.Y >=
                            scroller.Extent.Height - scroller.Viewport.Height - 4;
                    }
                    catch
                    {
                    }
                };
            }
            catch
            {
            }
        }

        private void FollowDebugLog(TextBox box)
        {
            try
            {
                HookDebugLogScroll(box);
                if (debugLogStickToBottom && box?.Text != null)
                    box.CaretIndex = box.Text.Length;
            }
            catch
            {
            }
        }

        private void RefreshPreview()
        {
            var preview = this.FindControl<TextBox>("LuaPreview");
            if (session?.Document == null)
            {
                preview.Text = string.Empty;
                return;
            }
            try
            {
                preview.Text = session.GenerateLua();
            }
            catch (Exception ex)
            {
                preview.Text = string.Empty;
                EditorAppContext.Dialogs.ShowError("Preview failed.\n" + ex);
            }
        }

        private void RefreshMessages()
        {
            SetStatus(session?.Document == null
                ? "No document open."
                : $"'{session.Document.DocName}' | {MessageContainer.Messages.Count} message(s)");
        }

        private void SetStatus(string text)
        {
            this.FindControl<TextBlock>("StatusBar").Text = text;
        }
    }
}
