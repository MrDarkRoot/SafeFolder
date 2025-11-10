using System;

namespace Safe.Models
{
    public class ProtectedFolder
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public ProtectionMode ProtectionMode { get; set; }
        public bool IsLocked { get; set; }
        public DateTime LastModified { get; set; }
    }

    public enum ProtectionMode
    {
        Encrypted,
        Locked
    }
}