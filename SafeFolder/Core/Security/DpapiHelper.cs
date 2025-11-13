using System;
using System.Security.Cryptography;
using System.Text;

namespace SafeFolder.Core.Security
{
    /// <summary>
    /// Provides helper methods for data protection using the Windows Data Protection API (DPAPI).
    /// </summary>
    public static class DpapiHelper
    {
        // Optional: A fixed salt to increase encryption complexity. 
        // This value does not need to be secret.
        private static readonly byte[] Entropy = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        /// <summary>
        /// Encrypts a string using DPAPI for the current user scope.
        /// </summary>
        /// <param name="dataToProtect">The string to encrypt.</param>
        /// <returns>A Base64 encoded string representing the encrypted data.</returns>
        public static string ProtectData(string dataToProtect)
        {
            if (string.IsNullOrEmpty(dataToProtect))
            {
                throw new ArgumentNullException(nameof(dataToProtect));
            }

            byte[] dataBytes = Encoding.UTF8.GetBytes(dataToProtect);
            byte[] protectedData = ProtectedData.Protect(dataBytes, Entropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(protectedData);
        }

        /// <summary>
        /// Decrypts a string using DPAPI for the current user scope.
        /// </summary>
        /// <param name="protectedData">The Base64 encoded string to decrypt.</param>
        /// <returns>The original, decrypted string.</returns>
        public static string UnprotectData(string protectedData)
        {
            if (string.IsNullOrEmpty(protectedData))
            {
                throw new ArgumentNullException(nameof(protectedData));
            }

            byte[] protectedDataBytes = Convert.FromBase64String(protectedData);
            byte[] dataBytes = ProtectedData.Unprotect(protectedDataBytes, Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(dataBytes);
        }
    }
}
