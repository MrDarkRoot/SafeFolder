using SafeFolder.Core.AccessManagement;
using System;
using System.Security;
using System.Windows.Input;

using System;
using System.Security;
using System.Windows.Input;
using SafeFolder.Core.DataAccess;
using SafeFolder.Core.Security;

namespace SafeFolder.ViewModels
{
    // A basic ICommand implementation for MVVM.
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object? parameter) => _execute(parameter);
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }

    public class LoginViewModel : BaseViewModel
    {
        private readonly DatabaseService _dbService;
        private string _errorMessage = string.Empty;
        private bool _isLoginSuccessful = false;

        private string? _storedHash;
        private string? _storedSalt;
        private bool _isFirstRun;

        public SecureString? Password { get; set; }
        public SecureString? NewPassword { get; set; }
        public SecureString? ConfirmPassword { get; set; }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public bool IsLoginSuccessful
        {
            get => _isLoginSuccessful;
            set { _isLoginSuccessful = value; OnPropertyChanged(); }
        }

        public bool IsFirstRun
        {
            get => _isFirstRun;
            set
            {
                _isFirstRun = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsExistingUser));
            }
        }

        public bool IsExistingUser => !IsFirstRun;

        public ICommand LoginCommand { get; }
        public ICommand SetPasswordCommand { get; }

        public LoginViewModel()
        {
            _dbService = App.DbService; // Get the service instance from App
            LoginCommand = new RelayCommand(Login, CanLogin);
            SetPasswordCommand = new RelayCommand(SetPassword, CanSetPassword);

            // Check the database to see if a master password has been set
            var config = _dbService.GetConfiguration();
            _storedHash = config.hash;
            _storedSalt = config.salt;

            if (string.IsNullOrEmpty(_storedHash))
            {
                IsFirstRun = true;
            }
            else
            {
                IsFirstRun = false;
            }
        }

        private bool CanLogin(object? parameter)
        {
            return Password != null && Password.Length > 0;
        }

        private void Login(object? parameter)
        {
            if (Password == null || _storedHash == null || _storedSalt == null) return;

            var passwordString = new System.Net.NetworkCredential(string.Empty, Password).Password;

            if (PasswordHasher.VerifyPassword(passwordString, _storedHash, _storedSalt))
            {
                ErrorMessage = string.Empty;
                IsLoginSuccessful = true; // This will trigger the view change
            }
            else
            {
                ErrorMessage = "Mật khẩu không chính xác. Vui lòng thử lại.";
            }
        }

        private bool CanSetPassword(object? parameter)
        {
            return NewPassword != null && NewPassword.Length > 4 && // Require a minimum length
                   ConfirmPassword != null && ConfirmPassword.Length > 0;
        }

        private void SetPassword(object? parameter)
        {
            if (NewPassword == null || ConfirmPassword == null) return;

            var newPass = new System.Net.NetworkCredential(string.Empty, NewPassword).Password;
            var confirmPass = new System.Net.NetworkCredential(string.Empty, ConfirmPassword).Password;

            if (newPass != confirmPass)
            {
                ErrorMessage = "Mật khẩu xác nhận không khớp.";
                return;
            }

            // Hash and store the new password in the database
            var (newHash, newSalt) = PasswordHasher.HashPassword(newPass);
            _dbService.SaveConfiguration(newHash, newSalt);

            // Update local state to allow immediate login
            _storedHash = newHash;
            _storedSalt = newSalt;

            ErrorMessage = string.Empty;
            IsFirstRun = false; // Move to the login screen
        }
    }
}
