using Safe1.Services;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Safe1.ViewModels
{
    public class CreateMasterPasswordViewModel : BaseViewModel
    {
        private string _errorMessage;
        public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

        private readonly AuthService _authService;

        public event Action? Created;

        public CreateMasterPasswordViewModel(AuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        public async Task<bool> CreateMasterPasswordAsync(string password, string confirm)
        {
            ErrorMessage = string.Empty;
            if (string.IsNullOrEmpty(password))
            {
                ErrorMessage = "Password cannot be empty.";
                return false;
            }
            if (password != confirm)
            {
                ErrorMessage = "Passwords do not match.";
                return false;
            }

            bool ok = await _authService.SetMasterPasswordAsync(password);
            if (ok)
            {
                Created?.Invoke();
                return true;
            }
            else
            {
                ErrorMessage = "Failed to set master password.";
                return false;
            }
        }
    }
}
