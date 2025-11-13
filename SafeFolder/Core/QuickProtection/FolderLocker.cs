using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace SafeFolder.Core.QuickProtection
{
    /// <summary>
    /// Manages the "Quick Protection" feature (Lock & Hide) for folders.
    /// </summary>
    public class FolderLocker
    {
        private const string LockedExtension = ".sflock"; // Custom extension for locked folders

        /// <summary>
        /// Applies "Quick Protection" to a folder: Hides, denies access, and renames it.
        /// </summary>
        /// <param name="folderPath">The absolute path to the folder to lock.</param>
        /// <returns>The new path of the locked folder, or null if locking fails.</returns>
        public string? LockFolder(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                // TODO: Log or handle "Directory not found" error
                return null;
            }

            try
            {
                // 1. Modify Access Control Lists (ACLs) to deny access
                DenyAccess(folderPath);

                // 2. Set Hidden and System attributes
                var directoryInfo = new DirectoryInfo(folderPath);
                directoryInfo.Attributes |= FileAttributes.Hidden | FileAttributes.System;

                // 3. Rename the folder to make it less accessible
                string newFolderPath = folderPath + LockedExtension;
                if (Directory.Exists(newFolderPath))
                {
                    // Handle case where a locked folder with the same name already exists
                    // For now, we'll fail, but a more robust solution might involve a different naming scheme.
                    RestoreAccess(folderPath); // Rollback ACL changes
                    directoryInfo.Attributes &= ~FileAttributes.Hidden; // Rollback attribute changes
                    directoryInfo.Attributes &= ~FileAttributes.System;
                    return null;
                }
                Directory.Move(folderPath, newFolderPath);

                // TODO: Implement FileSystemWatcher to monitor for unauthorized changes.

                return newFolderPath;
            }
            catch (Exception ex)
            {
                // TODO: Log the exception.
                // Attempt to roll back changes if something went wrong.
                if (Directory.Exists(folderPath))
                {
                    RestoreAccess(folderPath);
                }
                return null;
            }
        }

        /// <summary>
        /// Removes "Quick Protection" from a folder.
        /// </summary>
        /// <param name="lockedFolderPath">The path to the locked folder (e.g., "C:\MyFolder.sflock").</param>
        /// <returns>The original path of the unlocked folder, or null if unlocking fails.</returns>
        public string? UnlockFolder(string lockedFolderPath)
        {
            if (!Directory.Exists(lockedFolderPath) || !lockedFolderPath.EndsWith(LockedExtension))
            {
                // TODO: Log or handle "Invalid locked folder path" error
                return null;
            }

            string originalFolderPath = lockedFolderPath.Substring(0, lockedFolderPath.Length - LockedExtension.Length);

            try
            {
                // 1. Rename the folder back to its original name
                Directory.Move(lockedFolderPath, originalFolderPath);

                // 2. Restore normal attributes
                var directoryInfo = new DirectoryInfo(originalFolderPath);
                directoryInfo.Attributes &= ~FileAttributes.Hidden;
                directoryInfo.Attributes &= ~FileAttributes.System;

                // 3. Restore Access Control Lists (ACLs)
                RestoreAccess(originalFolderPath);

                return originalFolderPath;
            }
            catch (Exception ex)
            {
                // TODO: Log the exception.
                // If renaming succeeded but other steps failed, the folder might be left in an inconsistent state.
                // A more robust implementation would handle this.
                return null;
            }
        }

        /// <summary>
        /// Modifies the folder's ACL to deny the current user read access.
        /// </summary>
        private void DenyAccess(string folderPath)
        {
            var directoryInfo = new DirectoryInfo(folderPath);
            DirectorySecurity dSecurity = directoryInfo.GetAccessControl();

            // Get the current user's identity
            WindowsIdentity currentUser = WindowsIdentity.GetCurrent();
            
            // Create a rule to deny read access. This also implicitly denies list, execute, etc.
            FileSystemAccessRule denyRule = new FileSystemAccessRule(
                currentUser.User,
                FileSystemRights.Read,
                AccessControlType.Deny);

            dSecurity.AddAccessRule(denyRule);
            directoryInfo.SetAccessControl(dSecurity);
        }

        /// <summary>
        /// Modifies the folder's ACL to remove the specific deny rule for the current user.
        /// </summary>
        private void RestoreAccess(string folderPath)
        {
            var directoryInfo = new DirectoryInfo(folderPath);
            DirectorySecurity dSecurity = directoryInfo.GetAccessControl();

            WindowsIdentity currentUser = WindowsIdentity.GetCurrent();

            // Create the exact rule that was added so we can remove it
            FileSystemAccessRule denyRule = new FileSystemAccessRule(
                currentUser.User,
                FileSystemRights.Read,
                AccessControlType.Deny);

            dSecurity.RemoveAccessRule(denyRule);
            directoryInfo.SetAccessControl(dSecurity);
        }
    }
}
