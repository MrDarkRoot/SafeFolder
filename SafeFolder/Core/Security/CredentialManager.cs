using System;
using System.IO;
using System.Security.Cryptography;

namespace SafeFolder.Core.Security
{
    /// <summary>
    /// Manages the creation, storage, and retrieval of the encrypted database password.
    /// </summary>
    public class CredentialManager
    {
        private readonly string _credentialFilePath;

        public CredentialManager()
        {
            // Store the credential file in a local, user-specific application data folder.
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string appFolderPath = Path.Combine(appDataPath, "SafeFolderApp");
            Directory.CreateDirectory(appFolderPath); // Ensure the directory exists
            _credentialFilePath = Path.Combine(appFolderPath, "config.dat");
        }

        /// <summary>
        /// Gets the database password. If not already created, it generates a new one,
        /// encrypts it with DPAPI, and saves it for future use.
        /// </summary>
        /// <returns>The plaintext database password.</returns>
        public string GetOrCreateDatabasePassword()
        {
            if (File.Exists(_credentialFilePath))
            {
                // If the file exists, read, decrypt, and return the password.
                string encryptedPassword = File.ReadAllText(_credentialFilePath);
                return DpapiHelper.UnprotectData(encryptedPassword);
            }
            else
            {
                // If it's the first run, generate a new strong password.
                string newPassword = GenerateRandomPassword();
                
                // Encrypt it using DPAPI.
                string encryptedPassword = DpapiHelper.ProtectData(newPassword);
                
                // Save the encrypted password to the file.
                File.WriteAllText(_credentialFilePath, encryptedPassword);
                
                return newPassword;
            }
        }

        /// <summary>
        /// Generates a cryptographically strong, random password.
        /// </summary>
        /// <param name="length">The desired length of the password.</param>
        /// <returns>A random password string.</returns>
        private string GenerateRandomPassword(int length = 32)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()";
            var randomBytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            var password = new char[length];
            for (int i = 0; i < length; i++)
            {
                password[i] = validChars[randomBytes[i] % validChars.Length];
            }
            return new string(password);
        }
    }
}
