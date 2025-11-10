using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Safe1.Models
{
    public enum ProtectionMode
    {
        NORMAL,
        LOCKED,
        ENCRYPTED
    }

    public class ProtectedFolderModel : INotifyPropertyChanged
    {
        private int _id;
        private string _folderPath;
        private string _displayName;
        private DateTime _createdAt;
        private ProtectionMode _protectionMode;

        public int Id { get => _id; set { _id = value; Notify(); } }
        public string FolderPath { get => _folderPath; set { _folderPath = value; Notify(); } }
        public string DisplayName { get => _displayName; set { _displayName = value; Notify(); } }
        public DateTime CreatedAt { get => _createdAt; set { _createdAt = value; Notify(); } }
        public ProtectionMode ProtectionMode { get => _protectionMode; set { _protectionMode = value; Notify(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify([CallerMemberName] string propName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
