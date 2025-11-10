using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Safe1.Services
{
    public static class SecretManager
    {
        // Sử dụng một 'Entropy' (Muối) cố định cho lớp bảo vệ bổ sung.
        // Đây là một mảng byte ngẫu nhiên, KHÔNG PHẢI là key bảo mật.
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("SafeFolder_Unique_Salt_2025");

        /// <summary>
        /// Mã hóa dữ liệu bằng DPAPI, chỉ có thể giải mã bởi cùng User trên cùng máy tính.
        /// </summary>
        /// <param name="plainText">Chuỗi cần mã hóa (Ví dụ: Key SQLCipher)</param>
        /// <returns>Chuỗi Base64 chứa dữ liệu đã mã hóa.</returns>
        public static string Protect(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return null;

            byte[] dataToProtect = Encoding.UTF8.GetBytes(plainText);
            byte[] protectedData = ProtectedData.Protect(
                dataToProtect,
                Entropy,
                DataProtectionScope.CurrentUser // Gắn với user hiện tại
            );

            return Convert.ToBase64String(protectedData);
        }

        /// <summary>
        /// Giải mã dữ liệu đã được mã hóa bằng DPAPI.
        /// </summary>
        /// <param name="cipherText">Chuỗi Base64 của dữ liệu đã mã hóa.</param>
        /// <returns>Chuỗi đã giải mã (Key SQLCipher).</returns>
        public static string Unprotect(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return null;

            try
            {
                byte[] dataToUnprotect = Convert.FromBase64String(cipherText);
                byte[] unprotectedData = ProtectedData.Unprotect(
                    dataToUnprotect,
                    Entropy,
                    DataProtectionScope.CurrentUser
                );
                return Encoding.UTF8.GetString(unprotectedData);
            }
            catch (CryptographicException)
            {
                // Thường xảy ra nếu dữ liệu bị hỏng hoặc cố gắng giải mã trên máy khác.
                return null;
            }


        }
    }
}
