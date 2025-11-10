using System.Text;
using System.Windows;
using System.Windows.Controls;
using Safe1.Views;
using Safe1.ViewModels;

namespace Safe1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            try
            {
                var loginView = new LoginView();
                MainContent.Content = loginView;

                if (loginView.DataContext is LoginViewModel vm)
                {
                    vm.LoginSucceeded += OnLoginSucceeded;
                }
            }
            catch (System.Exception ex)
            {
                // Surface any initialization errors to help debugging blank window
                MessageBox.Show($"Error initializing UI:\n{ex}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Optionally set a simple fallback UI so window isn't blank
                MainContent.Content = new TextBlock
                {
                    Text = "Failed to initialize UI. Check error message.",
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 16
                };
            }
        }

        private void OnLoginSucceeded()
        {
            Dispatcher.Invoke(() =>
            {
                var mainView = new MainView();
                MainContent.Content = mainView;
            });
        }
    }
}