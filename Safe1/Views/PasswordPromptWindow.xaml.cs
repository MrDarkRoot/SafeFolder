using System.Windows;

namespace Safe1.Views
{
    public partial class PasswordPromptWindow : Window
    {
        public string EnteredPassword => PasswordBox.Password;

        public PasswordPromptWindow(string message = "Enter master password:")
        {
            InitializeComponent();
            MessageText.Text = message;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
