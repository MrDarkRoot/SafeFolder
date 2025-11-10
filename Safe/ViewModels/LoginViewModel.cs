using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Safe.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        [ObservableProperty]
        private string errorMessage;

        [ObservableProperty]
        private bool isFirstTime;

        [ObservableProperty]
        private bool isError;

        public LoginViewModel()
        {
            // TODO: Implement password service
            IsFirstTime = true; // This should check if password exists
        }

        [RelayCommand]
        private void Login(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                ShowError("Vui lòng nh?p m?t kh?u!");
                return;
            }

            // TODO: Implement actual password verification
            if (password == "test") // This is temporary
            {
                // Open main window and close login
                var mainWindow = new MainWindow();
                mainWindow.Show();
                Application.Current.MainWindow.Close();
            }
            else
            {
                ShowError("M?t kh?u không chính xác!");
            }
        }

        [RelayCommand]
        private void SetupPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                ShowError("Vui lòng nh?p m?t kh?u m?i!");
                return;
            }

            if (password.Length < 8)
            {
                ShowError("M?t kh?u ph?i có ít nh?t 8 ký t?!");
                return;
            }

            // TODO: Implement password setup
            IsFirstTime = false;
            ShowError("Thi?t l?p m?t kh?u thành công!", false);
        }

        private void ShowError(string message, bool isError = true)
        {
            ErrorMessage = message;
            IsError = isError;
        }
    }
}