using Safe1.Models;
using Safe1.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Safe1.Views
{
    public partial class MainView : UserControl
    {
        public MainView()
        {
            InitializeComponent();
        }

        private async void OnActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn)) return;
            if (!(btn.DataContext is ProtectedFolderModel model)) return;

            var vm = this.DataContext as MainViewModel;
            if (vm == null) return;

            var action = btn.Tag as string ?? string.Empty;
            // Route to ViewModel command: pass a tuple (action, model)
            await vm.HandleFolderActionWithTypeAsync(action, model);
        }
    }
}
