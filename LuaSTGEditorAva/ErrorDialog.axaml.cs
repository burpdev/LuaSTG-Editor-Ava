using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace LuaSTGEditorAva
{
    public partial class ErrorDialog : Window
    {
        public ErrorDialog()
        {
            InitializeComponent();
        }

        public ErrorDialog(string title, string message)
            : this()
        {
            Title = title;
            this.FindControl<TextBlock>("MessageText").Text = message;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnOpened(System.EventArgs e)
        {
            base.OnOpened(e);
            this.FindControl<Button>("OkButton").Click += OnOk;
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}
