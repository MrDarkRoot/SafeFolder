using SafeFolder.ViewModels;
using System.Windows.Controls;

namespace SafeFolder.Views
{
    /// <summary>
    /// Interaction logic for LoginView.xaml
    /// </summary>
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();

            // This is a common pattern to handle SecureString in MVVM.
            // We listen for changes and update the ViewModel's property programmatically.
            PasswordBox.PasswordChanged += (s, e) => UpdatePassword("Password");
            NewPasswordBox.PasswordChanged += (s, e) => UpdatePassword("NewPassword");
            ConfirmPasswordBox.PasswordChanged += (s, e) => UpdatePassword("ConfirmPassword");
        }

        private void UpdatePassword(string propertyName)
        {
            if (DataContext is LoginViewModel viewModel)
            {
                switch (propertyName)
                {
                    case "Password":
                        viewModel.Password = ((PasswordBox)this.FindName("PasswordBox")).SecurePassword;
                        break;
                    case "NewPassword":
                        viewModel.NewPassword = ((PasswordBox)this.FindName("NewPasswordBox")).SecurePassword;
                        break;
                    case "ConfirmPassword":
                        viewModel.ConfirmPassword = ((PasswordBox)this.FindName("ConfirmPasswordBox")).SecurePassword;
                        break;
                }
            }
        }
    }
}
