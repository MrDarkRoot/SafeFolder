using System;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;

namespace Safe1.Services
{
    /// <summary>
    /// Provides quick lock/unlock operations for a folder:
    /// - QuickLock: set Hidden+System attributes, apply a Deny ACL for the current user, and rename to obfuscated name.
    /// - QuickUnlock: remove Deny ACL, clear attributes, and restore original name when available.
    /// 
    /// Note: This implementation stores a small mapping file in the parent directory before renaming
    /// so the original name can be restored on unlock. The mapping content is protected using DPAPI
    /// via `SecretManager`.
    /// </summary>
    public static class QuickProtectService
    {
        private const string MapPrefix = ".map_";

        /// <summary>
        /// Quickly locks a folder: hides it, marks it system, applies a Deny ACL for the current user,
        /// and renames it to an obfuscated (invisible) name. Returns the new folder path.
        /// </summary>
        public static async Task<string> QuickLockAsync(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath)) throw new ArgumentNullException(nameof(folderPath));
            if (!Directory.Exists(folderPath)) throw new DirectoryNotFoundException(folderPath);

            var parent = Path.GetDirectoryName(folderPath);
            if (string.IsNullOrEmpty(parent)) throw new InvalidOperationException("Cannot lock root or top-level paths.");

            var originalName = Path.GetFileName(folderPath);

            // Create an obfuscated name (prepend a zero-width-space so it appears invisible)
            var obfuscatedName = "\u200B" + Guid.NewGuid().ToString("N");
            var obfuscatedPath = Path.Combine(parent, obfuscatedName);

            // Store mapping file in parent so we can restore the original name later.
            var mapFile = Path.Combine(parent, MapPrefix + obfuscatedName);
            try
            {
                var protectedName = SecretManager.Protect(originalName) ?? originalName;
                await File.WriteAllTextAsync(mapFile, protectedName).ConfigureAwait(false);
            }
            catch
            {
                // If mapping can't be written, continue but unlocking will require a manual name.
            }

            // Set attributes on folder and its contents
            SetHiddenAndSystemRecursive(folderPath);

            // Apply deny ACL to current user so casual access is blocked
            try
            {
                var identity = WindowsIdentity.GetCurrent()?.User;
                if (identity != null)
                {
                    var dirInfo = new DirectoryInfo(folderPath);
                    var dirSec = dirInfo.GetAccessControl();
                    var rule = new FileSystemAccessRule(identity,
                        FileSystemRights.FullControl,
                        InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                        PropagationFlags.None,
                        AccessControlType.Deny);

                    dirSec.AddAccessRule(rule);
                    dirInfo.SetAccessControl(dirSec);
                }
            }
            catch
            {
                // Swallow: ACL changes may require privileges. Folder still renamed and hidden.
            }

            // Rename folder to the obfuscated name
            try
            {
                Directory.Move(folderPath, obfuscatedPath);
                return obfuscatedPath;
            }
            catch
            {
                // If rename fails, attempt to remove the mapping file and rethrow
                try { if (File.Exists(mapFile)) File.Delete(mapFile); } catch { }
                throw;
            }
        }

        /// <summary>
        /// Unlocks a previously QuickLocked folder. If restoreName is null, the method will try to find
        /// the original name from the mapping file placed in the parent directory. Returns the restored path.
        /// </summary>
        public static async Task<string> QuickUnlockAsync(string obfuscatedFolderPath, string restoreName = null)
        {
            if (string.IsNullOrWhiteSpace(obfuscatedFolderPath)) throw new ArgumentNullException(nameof(obfuscatedFolderPath));
            if (!Directory.Exists(obfuscatedFolderPath)) throw new DirectoryNotFoundException(obfuscatedFolderPath);

            var parent = Path.GetDirectoryName(obfuscatedFolderPath);
            if (string.IsNullOrEmpty(parent)) throw new InvalidOperationException("Cannot unlock root or top-level paths.");

            var obfName = Path.GetFileName(obfuscatedFolderPath);
            var mapFile = Path.Combine(parent, MapPrefix + obfName);

            // If restoreName not provided, attempt to read mapping file (protected)
            if (string.IsNullOrEmpty(restoreName) && File.Exists(mapFile))
            {
                try
                {
                    var protectedName = await File.ReadAllTextAsync(mapFile).ConfigureAwait(false);
                    var unprotected = SecretManager.Unprotect(protectedName);
                    if (!string.IsNullOrEmpty(unprotected)) restoreName = unprotected;
                }
                catch
                {
                    // ignore and allow caller to provide restoreName
                }
            }

            // Remove Deny ACLs for current user if present
            try
            {
                var identity = WindowsIdentity.GetCurrent()?.User;
                if (identity != null)
                {
                    var dirInfo = new DirectoryInfo(obfuscatedFolderPath);
                    var dirSec = dirInfo.GetAccessControl();
                    var rule = new FileSystemAccessRule(identity,
                        FileSystemRights.FullControl,
                        InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                        PropagationFlags.None,
                        AccessControlType.Deny);

                    // Attempt to remove the exact rule we added earlier
                    dirSec.RemoveAccessRule(rule);
                    dirInfo.SetAccessControl(dirSec);
                }
            }
            catch
            {
                // ignore failures; best-effort
            }

            // Clear attributes recursively so files are visible
            ClearHiddenAndSystemRecursive(obfuscatedFolderPath);

            // Determine target name
            var targetName = restoreName;
            if (string.IsNullOrEmpty(targetName))
            {
                // Fallback name
                targetName = "Restored_" + Guid.NewGuid().ToString("N");
            }

            var restoredPath = Path.Combine(parent, targetName);

            // Move folder back to original name
            Directory.Move(obfuscatedFolderPath, restoredPath);

            // Remove mapping file if exists
            try { if (File.Exists(mapFile)) File.Delete(mapFile); } catch { }

            return restoredPath;
        }

        private static void SetHiddenAndSystemRecursive(string folderPath)
        {
            try
            {
                var dirInfo = new DirectoryInfo(folderPath);
                dirInfo.Attributes |= FileAttributes.Hidden | FileAttributes.System;

                foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    try
                    {
                        file.Attributes |= FileAttributes.Hidden | FileAttributes.System;
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static void ClearHiddenAndSystemRecursive(string folderPath)
        {
            try
            {
                var dirInfo = new DirectoryInfo(folderPath);
                dirInfo.Attributes &= ~(FileAttributes.Hidden | FileAttributes.System);

                foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    try
                    {
                        file.Attributes &= ~(FileAttributes.Hidden | FileAttributes.System);
                    }
                    catch { }
                }
            }
            catch { }
        }
    }
}
