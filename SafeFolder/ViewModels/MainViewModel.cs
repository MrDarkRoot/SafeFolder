using Microsoft.Win32;
using SafeFolder.Core.QuickProtection;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;

namespace SafeFolder.ViewModels
{
    // Represents a single folder in the main list
    public class ProtectedFolderViewModel : BaseViewModel
    {
        private string _status = "Normal";
        public string Name { get; set; }
        public string Path { get; set; }
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        public ProtectedFolderViewModel(string path)
        {
            Path = path;
            Name = new DirectoryInfo(path).Name;
        }
    }

    public class MainViewModel : BaseViewModel
    {
        private readonly FolderLocker _folderLocker;
        private ProtectedFolderViewModel? _selectedFolder;
        private string _statusBarText = "Sẵn sàng";

        public ObservableCollection<ProtectedFolderViewModel> ProtectedFolders { get; }
        
        public ProtectedFolderViewModel? SelectedFolder
        {
            get => _selectedFolder;
            set
            {
                _selectedFolder = value;
                OnPropertyChanged();
            }
        }

        public string StatusBarText
        {
            get => _statusBarText;
            set
            {
                _statusBarText = value;
                OnPropertyChanged();
            }
        }

        public ICommand AddFolderCommand { get; }
        public ICommand LockCommand { get; }
        public ICommand UnlockCommand { get; }

        public MainViewModel()
        {
            _folderLocker = new FolderLocker();
            ProtectedFolders = new ObservableCollection<ProtectedFolderViewModel>();

            AddFolderCommand = new RelayCommand(AddFolder);
            LockCommand = new RelayCommand(LockFolder, CanLockOrUnlock);
            UnlockCommand = new RelayCommand(UnlockFolder, CanLockOrUnlock);

            // Add some sample data for demonstration
            LoadSampleData();
        }

        private void LoadSampleData()
        {
            // In a real app, this list would be loaded from your configuration database
            // For now, we'll create dummy folders if they don't exist.
            string samplePath1 = @"C:\Temp\MyPersonalDocs";
            string samplePath2 = @"C:\Temp\WorkProjects";

            if (!Directory.Exists(samplePath1)) Directory.CreateDirectory(samplePath1);
            if (!Directory.Exists(samplePath2)) Directory.CreateDirectory(samplePath2);

            ProtectedFolders.Add(new ProtectedFolderViewModel(samplePath1));
            ProtectedFolders.Add(new ProtectedFolderViewModel(samplePath2));
        }

        private void AddFolder(object? parameter)
        {
            // This uses the old folder browser dialog. For a better experience,
            // consider a NuGet package like Ookii.Dialogs.Wpf.
            var dialog = new OpenFolderDialog
            {
                Title = "Chọn thư mục để bảo vệ"
            };

            if (dialog.ShowDialog() == true)
            {
                string selectedPath = dialog.FolderName;
                // Check if the folder is already in the list
                foreach (var folder in ProtectedFolders)
                {
                    if (folder.Path == selectedPath)
                    {
                        StatusBarText = "Thư mục này đã có trong danh sách.";
                        return;
                    }
                }
                ProtectedFolders.Add(new ProtectedFolderViewModel(selectedPath));
                StatusBarText = $"Đã thêm thư mục: {selectedPath}";
            }
        }

        private bool CanLockOrUnlock(object? parameter)
        {
            return SelectedFolder != null;
        }

        private void LockFolder(object? parameter)
        {
            if (SelectedFolder == null || SelectedFolder.Status == "Locked") return;

            string? newPath = _folderLocker.LockFolder(SelectedFolder.Path);

            if (newPath != null)
            {
                SelectedFolder.Status = "Locked";
                SelectedFolder.Path = newPath; // Update path to the new locked path
                StatusBarText = $"Đã khóa thư mục: {SelectedFolder.Name}";
            }
            else
            {
                StatusBarText = $"Lỗi: Không thể khóa thư mục.";
            }
        }

        private void UnlockFolder(object? parameter)
        {
            if (SelectedFolder == null || SelectedFolder.Status != "Locked") return;

            string? originalPath = _folderLocker.UnlockFolder(SelectedFolder.Path);

            if (originalPath != null)
            {
                SelectedFolder.Status = "Normal";
                SelectedFolder.Path = originalPath; // Update path back to original
                StatusBarText = $"Đã mở khóa thư mục: {SelectedFolder.Name}";
            }
            else
            {
                StatusBarText = $"Lỗi: Không thể mở khóa thư mục.";
            }
        }
    }
}
