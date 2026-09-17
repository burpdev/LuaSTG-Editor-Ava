using Avalonia.Controls.Templates;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia;
using LuaSTGEditorSharp.EditorData.Document.Meta;
using LuaSTGEditorSharp.EditorData.Document;
using LuaSTGEditorSharp.EditorData;
using System.Collections.ObjectModel;
namespace LuaSTGEditorAva.Dialogs
{
    public partial class DefinitionsDialog : Window
    {
        public DefinitionsDialog(DocumentData document)
        {
            InitializeComponent();
            var roots = new ObservableCollection<MetaModel>();
            if (document != null)
            {
                AddCategory(roots, "User Defined Nodes", document, MetaType.UserDefined);
                AddCategory(roots, "Stage Group", document, MetaType.StageGroup);
                AddCategory(roots, "Bullet", document, MetaType.Bullet);
                AddCategory(roots, "Laser", document, MetaType.Laser);
                AddCategory(roots, "Bent Laser", document, MetaType.BentLaser);
                AddCategory(roots, "Object", document, MetaType.Object);
                AddCategory(roots, "Enemy", document, MetaType.Enemy);
                AddCategory(roots, "Task", document, MetaType.Task);
                AddCategory(roots, "Boss", document, MetaType.Boss);
                AddCategory(roots, "Image", document, MetaType.ImageLoad);
                AddCategory(roots, "Image Group", document, MetaType.ImageGroupLoad);
                AddCategory(roots, "Particle", document, MetaType.ParticleLoad);
                AddCategory(roots, "Animation", document, MetaType.AnimationLoad);
                AddCategory(roots, "Shader", document, MetaType.FXLoad);
                AddCategory(roots, "Font", document, MetaType.FontLoad);
                AddCategory(roots, "TTF", document, MetaType.TTFLoad);
                AddCategory(roots, "BGM", document, MetaType.BGMLoad);
                AddCategory(roots, "Sound", document, MetaType.SELoad);
            }
            var tree = this.FindControl<TreeView>("DefTree");
            tree.ItemsSource = roots;
            tree.SelectionChanged += (sender, e) =>
            {
                var details = this.FindControl<TextBox>("DefDetails");
                if (tree.SelectedItem is MetaModel m)
                    details.Text = $"Name: {m.FullName}\nLua: {m.Result}\nInfo1: {m.ExInfo1}\nInfo2: {m.ExInfo2}";
                else
                    details.Text = "";
            };
            this.FindControl<Button>("CloseBtn").Click += OnClose;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnClose(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static void AddCategory(ObservableCollection<MetaModel> roots, string title,
            DocumentData document, MetaType type)
        {
            try
            {
                var node = new MetaModel { Text = title };
                foreach (MetaModel info in document.Meta.aggregatableMetas[(int)type].GetAllFullWithDifficulty(""))
                    node.Children.Add(info);
                roots.Add(node);
            }
            catch
            {
            }
        }
    }
}
