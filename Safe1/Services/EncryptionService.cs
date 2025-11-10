using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using System.Text;

namespace Safe1.Services
{
    public static class EncryptionService
    {
        private const string NativeDll = "SafeFolder.NativeCrypto.dll";
        private const int FileKeySize = 32; // 256-bit
        private const int PBKDF2Iterations = 310000;

        // P/Invoke declarations
        [DllImport(NativeDll, CallingConvention = CallingConvention.Cdecl)]
        private static extern int PBKDF2_Derive(IntPtr passwordAnsi, byte[] salt, int saltLen, int iterations, byte[] outKey, int outKeyLen);

        [DllImport(NativeDll, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern int EncryptFileAesGcm(string inPath, string outPath, byte[] key, int keyLen, byte[] iv, int ivLen);

        [DllImport(NativeDll, CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern int DecryptFileAesGcm(string inPath, string outPath, byte[] key, int keyLen, byte[] iv, int ivLen);

        /// <summary>
        /// Derive a 32-byte file key from the given master password and salt using PBKDF2-SHA256 (native implementation).
        /// Uses SecureString -> global ANSI buffer to avoid leaving password as managed string bytes.
        /// </summary>
        public static byte[] DeriveFileKeyFromMaster(string masterPassword, byte[] salt)
        {
            if (masterPassword == null) throw new ArgumentNullException(nameof(masterPassword));
            if (salt == null) throw new ArgumentNullException(nameof(salt));

            var outKey = new byte[FileKeySize];
            IntPtr pwPtr = IntPtr.Zero;
            SecureString secure = null;
            try
            {
                // Build SecureString from plain string
                secure = new SecureString();
                foreach (char c in masterPassword)
                {
                    secure.AppendChar(c);
                }
                secure.MakeReadOnly();

                // Convert to ANSI in unmanaged memory (zeroable via ZeroFreeGlobalAllocAnsi)
                pwPtr = Marshal.SecureStringToGlobalAllocAnsi(secure);

                int rc = PBKDF2_Derive(pwPtr, salt, salt.Length, PBKDF2Iterations, outKey, outKey.Length);
                if (rc != 0)
                {
                    throw new InvalidOperationException($"PBKDF2_Derive failed with code {rc}.");
                }

                return outKey;
            }
            finally
            {
                // Zero and free unmanaged memory
                if (pwPtr != IntPtr.Zero)
                {
                    try { Marshal.ZeroFreeGlobalAllocAnsi(pwPtr); } catch { }
                }

                // Clear securestring
                if (secure != null)
                {
                    try
                    {
                        secure.Dispose();
                    }
                    catch { }
                }

                // Clear masterPassword reference in case caller passed a variable
                // (cannot zero managed string content; recommend caller avoid keeping password around)
            }
        }

        /// <summary>
        /// Encrypts inputPath to outputPath atomically using AES-256-GCM via native library.
        /// The final file format: [12-byte IV][ciphertext...][16-byte tag]
        /// </summary>
        public static Task EncryptFileAtomicAsync(string inputPath, string outputPath, byte[] fileKey)
        {
            if (string.IsNullOrEmpty(inputPath)) throw new ArgumentNullException(nameof(inputPath));
            if (string.IsNullOrEmpty(outputPath)) throw new ArgumentNullException(nameof(outputPath));
            if (fileKey == null || fileKey.Length != FileKeySize) throw new ArgumentException("fileKey must be 32 bytes", nameof(fileKey));

            return Task.Run(() =>
            {
                var outDir = Path.GetDirectoryName(outputPath);
                if (string.IsNullOrEmpty(outDir)) outDir = Directory.GetCurrentDirectory();
                Directory.CreateDirectory(outDir);

                var nativeTemp = Path.Combine(outDir, ".native_tmp_" + Guid.NewGuid().ToString("N"));
                var finalTemp = Path.Combine(outDir, ".tmp_" + Path.GetFileName(outputPath) + "_" + Guid.NewGuid().ToString("N"));

                byte[] iv = new byte[12]; // recommended 12 bytes for GCM
                using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(iv);

                int rc = EncryptFileAesGcm(inputPath, nativeTemp, fileKey, fileKey.Length, iv, iv.Length);
                if (rc != 0)
                {
                    try { if (File.Exists(nativeTemp)) File.Delete(nativeTemp); } catch { }
                    throw new InvalidOperationException($"EncryptFileAesGcm failed with code {rc}.");
                }

                // Create finalTemp containing IV + nativeTemp contents
                try
                {
                    using (var outFs = new FileStream(finalTemp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        outFs.Write(iv, 0, iv.Length);
                        using (var inFs = new FileStream(nativeTemp, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            inFs.CopyTo(outFs);
                        }
                        outFs.Flush(true);
                    }

                    // Replace/move to outputPath
                    if (File.Exists(outputPath)) File.Replace(finalTemp, outputPath, null);
                    else File.Move(finalTemp, outputPath);
                }
                finally
                {
                    try { if (File.Exists(nativeTemp)) File.Delete(nativeTemp); } catch { }
                    try { if (File.Exists(finalTemp)) File.Delete(finalTemp); } catch { }
                }
            });
        }

        /// <summary>
        /// Decrypts inputPath (expects [12-byte IV][ciphertext][16-byte tag]) to outputPath atomically.
        /// </summary>
        public static Task DecryptFileAtomicAsync(string inputPath, string outputPath, byte[] fileKey)
        {
            if (string.IsNullOrEmpty(inputPath)) throw new ArgumentNullException(nameof(inputPath));
            if (string.IsNullOrEmpty(outputPath)) throw new ArgumentNullException(nameof(outputPath));
            if (fileKey == null || fileKey.Length != FileKeySize) throw new ArgumentException("fileKey must be 32 bytes", nameof(fileKey));

            return Task.Run(() =>
            {
                // Read IV from input file header
                byte[] iv = new byte[12];
                var cipherTemp = Path.Combine(Path.GetDirectoryName(outputPath) ?? Directory.GetCurrentDirectory(), ".cipher_tmp_" + Guid.NewGuid().ToString("N"));
                var plainTemp = Path.Combine(Path.GetDirectoryName(outputPath) ?? Directory.GetCurrentDirectory(), ".tmp_" + Path.GetFileName(outputPath) + "_" + Guid.NewGuid().ToString("N"));

                try
                {
                    using (var inFs = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        int read = inFs.Read(iv, 0, iv.Length);
                        if (read != iv.Length) throw new InvalidOperationException("Encrypted file missing IV/header.");

                        using (var outCipher = new FileStream(cipherTemp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        {
                            inFs.CopyTo(outCipher);
                            outCipher.Flush(true);
                        }
                    }

                    int rc = DecryptFileAesGcm(cipherTemp, plainTemp, fileKey, fileKey.Length, iv, iv.Length);
                    if (rc != 0)
                    {
                        throw new InvalidOperationException($"DecryptFileAesGcm failed with code {rc}.");
                    }

                    // Move plaintext into outputPath
                    if (File.Exists(outputPath)) File.Replace(plainTemp, outputPath, null);
                    else File.Move(plainTemp, outputPath);
                }
                finally
                {
                    try { if (File.Exists(cipherTemp)) File.Delete(cipherTemp); } catch { }
                    try { if (File.Exists(plainTemp)) File.Delete(plainTemp); } catch { }
                }
            });
        }

        /// <summary>
        /// Encrypt all files in folderPath recursively using a randomly generated FEK encrypted with a KEK derived from masterPassword.
        /// Stores encrypted FEK and metadata in PROTECTED_FOLDER table. Reports progress via IProgress<double> (0..100).
        /// </summary>
        public static async Task EncryptFolderAsync(string folderPath, string masterPassword, DatabaseManager db, IProgress<double>? progress = null)
        {
            if (string.IsNullOrEmpty(folderPath)) throw new ArgumentNullException(nameof(folderPath));
            if (!Directory.Exists(folderPath)) throw new DirectoryNotFoundException(folderPath);
            if (db == null) throw new ArgumentNullException(nameof(db));

            // Generate salt for KEK
            byte[] salt = new byte[32];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);

            // Derive KEK from master password
            byte[] kek = DeriveFileKeyFromMaster(masterPassword, salt);

            // Generate FEK
            byte[] fek = new byte[FileKeySize];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(fek);

            // Encrypt FEK with KEK using AesGcm
            byte[] fekIv = new byte[12];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(fekIv);
            byte[] cipherFek = new byte[fek.Length];
            byte[] tag = new byte[16];
            try
            {
                using (var aes = new AesGcm(kek))
                {
                    aes.Encrypt(fekIv, fek, cipherFek, tag, null);
                }
            }
            finally
            {
                // zero kek from memory
                Array.Clear(kek, 0, kek.Length);
            }

            // Store enc_fek, salt, iv and tag in DB (base64)
            string encFekB64 = Convert.ToBase64String(Combine(cipherFek, tag));
            string saltB64 = Convert.ToBase64String(salt);
            string ivB64 = Convert.ToBase64String(fekIv);

            SaveFekToDb(db, folderPath, encFekB64, saltB64, ivB64);

            // Traverse files
            var files = Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories);
            var fileList = new System.Collections.Generic.List<string>(files);
            int total = fileList.Count;
            int processed = 0;

            foreach (var file in fileList)
            {
                // Skip DB file or config files if they are inside folder
                try
                {
                    await EncryptFileAtomicAsync(file, file, fek).ConfigureAwait(false);
                }
                catch
                {
                    // continue with best-effort; optionally log
                }
                processed++;
                progress?.Report(total == 0 ? 100 : (processed * 100.0 / total));
            }

            // Zero FEK
            Array.Clear(fek, 0, fek.Length);
        }

        /// <summary>
        /// Decrypt all files in folderPath using masterPassword and encrypted FEK stored in DB. Reports progress via IProgress<double>.
        /// </summary>
        public static async Task DecryptFolderAsync(string folderPath, string masterPassword, DatabaseManager db, IProgress<double>? progress = null)
        {
            if (string.IsNullOrEmpty(folderPath)) throw new ArgumentNullException(nameof(folderPath));
            if (!Directory.Exists(folderPath)) throw new DirectoryNotFoundException(folderPath);
            if (db == null) throw new ArgumentNullException(nameof(db));

            // Load enc_fek and salt from DB
            string encFekB64 = null;
            string saltB64 = null;
            string ivB64 = null;

            using (var conn = db.GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                // Ensure columns exist
                try
                {
                    cmd.CommandText = "PRAGMA table_info('PROTECTED_FOLDER');";
                    using (var r = cmd.ExecuteReader()) { }
                }
                catch { }

                cmd.CommandText = "SELECT enc_fek, fek_salt, fek_iv FROM PROTECTED_FOLDER WHERE folder_path = $path LIMIT 1;";
                cmd.Parameters.AddWithValue("$path", folderPath);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        encFekB64 = reader.IsDBNull(0) ? null : reader.GetString(0);
                        saltB64 = reader.IsDBNull(1) ? null : reader.GetString(1);
                        ivB64 = reader.IsDBNull(2) ? null : reader.GetString(2);
                    }
                }
            }

            if (string.IsNullOrEmpty(encFekB64) || string.IsNullOrEmpty(saltB64) || string.IsNullOrEmpty(ivB64))
            {
                throw new InvalidOperationException("No encrypted FEK found for folder.");
            }

            byte[] encFekWithTag = Convert.FromBase64String(encFekB64);
            byte[] salt = Convert.FromBase64String(saltB64);
            byte[] fekIv = Convert.FromBase64String(ivB64);

            // Split cipher and tag
            if (encFekWithTag.Length < 16) throw new InvalidOperationException("Invalid stored FEK.");
            byte[] cipherFek = new byte[encFekWithTag.Length - 16];
            byte[] tagFek = new byte[16];
            Array.Copy(encFekWithTag, 0, cipherFek, 0, cipherFek.Length);
            Array.Copy(encFekWithTag, cipherFek.Length, tagFek, 0, 16);

            // Derive KEK from master password
            byte[] kek = DeriveFileKeyFromMaster(masterPassword, salt);

            byte[] fek = new byte[FileKeySize];
            try
            {
                using (var aes = new AesGcm(kek))
                {
                    aes.Decrypt(fekIv, cipherFek, tagFek, fek, null);
                }
            }
            catch (Exception ex)
            {
                Array.Clear(kek, 0, kek.Length);
                throw new InvalidOperationException("Failed to decrypt FEK with provided master password.", ex);
            }
            finally
            {
                Array.Clear(kek, 0, kek.Length);
            }

            // Traverse files
            var files = Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories);
            var fileList = new System.Collections.Generic.List<string>(files);
            int total = fileList.Count;
            int processed = 0;

            foreach (var file in fileList)
            {
                try
                {
                    await DecryptFileAtomicAsync(file, file, fek).ConfigureAwait(false);
                }
                catch
                {
                    // continue best-effort
                }
                processed++;
                progress?.Report(total == 0 ? 100 : (processed * 100.0 / total));
            }

            // Zero FEK
            Array.Clear(fek, 0, fek.Length);

            // Remove FEK from DB
            using (var conn = db.GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "UPDATE PROTECTED_FOLDER SET enc_fek = NULL, fek_salt = NULL, fek_iv = NULL WHERE folder_path = $path;";
                cmd.Parameters.AddWithValue("$path", folderPath);
                cmd.ExecuteNonQuery();
            }
        }

        private static void SaveFekToDb(DatabaseManager db, string folderPath, string encFekB64, string saltB64, string ivB64)
        {
            // Ensure columns exist
            using (var conn = db.GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                // Add columns if missing
                cmd.CommandText = "PRAGMA table_info('PROTECTED_FOLDER');";
                using (var reader = cmd.ExecuteReader())
                {
                    var cols = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    while (reader.Read()) cols.Add(reader.GetString(reader.GetOrdinal("name")));
                    reader.Close();

                    if (!cols.Contains("enc_fek"))
                    {
                        using (var a = conn.CreateCommand()) { a.CommandText = "ALTER TABLE PROTECTED_FOLDER ADD COLUMN enc_fek TEXT;"; a.ExecuteNonQuery(); }
                    }
                    if (!cols.Contains("fek_salt"))
                    {
                        using (var a = conn.CreateCommand()) { a.CommandText = "ALTER TABLE PROTECTED_FOLDER ADD COLUMN fek_salt TEXT;"; a.ExecuteNonQuery(); }
                    }
                    if (!cols.Contains("fek_iv"))
                    {
                        using (var a = conn.CreateCommand()) { a.CommandText = "ALTER TABLE PROTECTED_FOLDER ADD COLUMN fek_iv TEXT;"; a.ExecuteNonQuery(); }
                    }
                }

                // Upsert record for folder
                cmd.CommandText = @"INSERT INTO PROTECTED_FOLDER (folder_path, display_name, created_at, enc_fek, fek_salt, fek_iv)
VALUES ($path, $name, $created, $enc, $salt, $iv)
ON CONFLICT(folder_path) DO UPDATE SET enc_fek = $enc, fek_salt = $salt, fek_iv = $iv;";
                cmd.Parameters.AddWithValue("$path", folderPath);
                cmd.Parameters.AddWithValue("$name", Path.GetFileName(folderPath));
                cmd.Parameters.AddWithValue("$created", DateTime.UtcNow.ToString("o"));
                cmd.Parameters.AddWithValue("$enc", encFekB64);
                cmd.Parameters.AddWithValue("$salt", saltB64);
                cmd.Parameters.AddWithValue("$iv", ivB64);
                cmd.ExecuteNonQuery();
            }
        }

        private static byte[] Combine(byte[] a, byte[] b)
        {
            var c = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, c, 0, a.Length);
            Buffer.BlockCopy(b, 0, c, a.Length, b.Length);
            return c;
        }
    }
}
