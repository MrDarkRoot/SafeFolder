using SafeFolder.ViewModels;
using System.ComponentModel;
using System.Windows;

namespace SafeFolder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly LoginViewModel _loginViewModel;
        private readonly MainViewModel _mainViewModel;

        public MainWindow()
        {
            InitializeComponent();

            // NOTE: The icons referenced in MainWindow.xaml (e.g., /Assets/Lock.png)
            // have not been created. You will need to create an "Assets" folder in your
            // project and add the corresponding images for them to appear.

            // Create instances of the ViewModels
            _loginViewModel = new LoginViewModel();
            _mainViewModel = new MainViewModel();

            // Set the DataContext for the views
            LoginView.DataContext = _loginViewModel;
            MainAppView.DataContext = _mainViewModel;

            // Subscribe to the login success event
            _loginViewModel.PropertyChanged += LoginViewModel_PropertyChanged;
        }

        private void LoginViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Check if the property that changed is the one indicating successful login
            if (e.PropertyName == nameof(LoginViewModel.IsLoginSuccessful) && _loginViewModel.IsLoginSuccessful)
            {
                // Switch the visible view
                LoginView.Visibility = Visibility.Collapsed;
                MainAppView.Visibility = Visibility.Visible;

                // Unsubscribe from the event to prevent memory leaks
                _loginViewModel.PropertyChanged -= LoginViewModel_PropertyChanged;
            }
        }
    }
}