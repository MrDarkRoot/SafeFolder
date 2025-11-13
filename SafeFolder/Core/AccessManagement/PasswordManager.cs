using System;
using System.Security.Cryptography;
using System.Text;

namespace SafeFolder.Core.AccessManagement
{
    /// <summary>
    /// Manages password hashing and verification using PBKDF2.
    /// </summary>
    public class PasswordManager
    {
        // Constants for PBKDF2
        private const int SaltSize = 32; // 32 bytes for salt
        private const int HashSize = 64; // 64 bytes for hash (SHA-256)
        private const int Iterations = 200000; // Minimum iterations as specified
        private static readonly HashAlgorithmName _hashAlgorithm = HashAlgorithmName.SHA256;

        // Placeholder for brute-force attack prevention
        private static int _failedLoginAttempts = 0;
        private static DateTime _lastFailedLoginTime = DateTime.MinValue;

        /// <summary>
        /// Hashes a password using PBKDF2 with a randomly generated salt.
        /// </summary>
        /// <param name="password">The password to hash.</param>
        /// <returns>A tuple containing the generated hash and salt.</returns>
        public (string Hash, string Salt) HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                Iterations,
                _hashAlgorithm,
                HashSize
            );

            return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
        }

        /// <summary>
        /// Verifies a password against a stored hash and salt.
        /// </summary>
        /// <param name="password">The password to verify.</param>
        /// <param name="storedHash">The stored password hash (Base64 encoded).</param>
        /// <param name="storedSalt">The stored salt (Base64 encoded).</param>
        /// <returns>True if the password is correct, otherwise false.</returns>
        public bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            // Progressive Time Penalty Logic
            if (_failedLoginAttempts >= 3)
            {
                TimeSpan delay = TimeSpan.FromSeconds(Math.Pow(2, _failedLoginAttempts - 3));
                if (DateTime.UtcNow - _lastFailedLoginTime < delay)
                {
                    // Enforce a delay to slow down brute-force attacks
                    System.Threading.Thread.Sleep(delay);
                }
            }

            byte[] salt = Convert.FromBase64String(storedSalt);
            byte[] hashToCompare = Convert.FromBase64String(storedHash);

            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                Iterations,
                _hashAlgorithm,
                HashSize
            );

            bool passwordsMatch = CryptographicOperations.FixedTimeEquals(hash, hashToCompare);

            if (passwordsMatch)
            {
                // Reset failed attempts on successful login
                _failedLoginAttempts = 0;
            }
            else
            {
                // Increment failed attempts and record time
                _failedLoginAttempts++;
                _lastFailedLoginTime = DateTime.UtcNow;
            }

            return passwordsMatch;
        }

        /// <summary>
        /// Changes the master password.
        /// </summary>
        /// <param name="oldPassword">The current master password.</param>
        /// <param name="newPassword">The new master password to set.</param>
        /// <param name="storedHash">The current stored hash.</param>
        /// <param name="storedSalt">The current stored salt.</param>
        /// <returns>A tuple with the new hash and salt if successful, otherwise null.</returns>
        public (string NewHash, string NewSalt)? ChangePassword(string oldPassword, string newPassword, string storedHash, string storedSalt)
        {
            // 1. Verify the old password first
            if (!VerifyPassword(oldPassword, storedHash, storedSalt))
            {
                return null; // Old password incorrect
            }

            // 2. Hash the new password
            var (newHash, newSalt) = HashPassword(newPassword);

            // 3. TODO: Persist the newHash and newSalt to the secure database (Configuration DB)
            // This step is crucial and would involve updating the database protected by SQLCipher/DPAPI.
            // Example: DatabaseService.UpdateMasterPassword(newHash, newSalt);

            return (newHash, newSalt);
        }
    }
}
