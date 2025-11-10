using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Safe.Models;

namespace Safe.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<ProtectedFolder> folderList = new();

        [ObservableProperty]
        private string searchText;

        [ObservableProperty]
        private bool isSidebarExpanded = true;

        [RelayCommand]
        private void AddFolder()
        {
            // Implementation will be added later
        }

        [RelayCommand]
        private void Search()
        {
            // Implementation will be added later
        }

        [RelayCommand]
        private void Encrypt(ProtectedFolder folder)
        {
            // Implementation will be added later
        }

        [RelayCommand]
        private void Access(ProtectedFolder folder)
        {
            // Implementation will be added later
        }

        [RelayCommand]
        private void Logout()
        {
            // Implementation will be added later
        }
    }
}