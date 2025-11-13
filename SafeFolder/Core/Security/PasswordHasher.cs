using System;
using System.Security.Cryptography;

namespace SafeFolder.Core.Security
{
    /// <summary>
    /// Provides functionality to hash and verify passwords using PBKDF2.
    /// </summary>
    public static class PasswordHasher
    {
        private const int SaltSize = 16; // 128 bit
        private const int HashSize = 32; // 256 bit
        private const int Iterations = 200000; // As per your design document
        private static readonly HashAlgorithmName _hashAlgorithm = HashAlgorithmName.SHA256;

        /// <summary>
        /// Hashes a password with a randomly generated salt.
        /// </summary>
        /// <param name="password">The password to hash.</param>
        /// <returns>A tuple containing the Base64 encoded hash and salt.</returns>
        public static (string hash, string salt) HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                Iterations,
                _hashAlgorithm,
                HashSize);

            return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
        }

        /// <summary>
        /// Verifies a password against a stored hash and salt.
        /// </summary>
        /// <param name="password">The password to verify.</param>
        /// <param name="storedHash">The stored Base64 encoded hash.</param>
        /// <param name="storedSalt">The stored Base64 encoded salt.</param>
        /// <returns>True if the password is correct, otherwise false.</returns>
        public static bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            byte[] saltBytes = Convert.FromBase64String(storedSalt);
            byte[] hashBytes = Convert.FromBase64String(storedHash);

            byte[] newHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                saltBytes,
                Iterations,
                _hashAlgorithm,
                HashSize);

            return CryptographicOperations.FixedTimeEquals(hashBytes, newHash);
        }
    }
}
