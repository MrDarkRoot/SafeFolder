using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Safe1.Services
{
    public class DatabaseManager
    {
        private const string DatabaseFileName = "SafeFolder.db";
        private string _databaseKey; // Key giải mã CSDL (đã được giải mã bằng DPAPI)

        public DatabaseManager(string decryptedDbKey)
        {
            _databaseKey = decryptedDbKey;
            // Khởi tạo SQLCipher runtime
            SQLitePCL.Batteries_V2.Init();
        }

        /// <summary>
        /// Tạo chuỗi kết nối SQLCipher.
        /// </summary>
        private string GetConnectionString()
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = DatabaseFileName,
                // Lệnh PRAGMA key sẽ mã hóa/giải mã toàn bộ CSDL.
                Password = _databaseKey
            }.ToString();
            return connectionString;
        }

        /// <summary>
        /// Mở kết nối và đảm bảo CSDL tồn tại và được mở khóa.
        /// </summary>
        public void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(GetConnectionString()))
            {
                connection.Open();
                // Thực hiện câu lệnh PRAGMA key (tự động)

                // Tạo bảng CONFIGURATION nếu chưa tồn tại
                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS CONFIGURATION (
                        config_id TEXT PRIMARY KEY,
                        master_hash TEXT NOT NULL,
                        salt TEXT NOT NULL,
                        iterations INTEGER,
                        timeout_mins INTEGER,
                        failed_attempts INTEGER DEFAULT 0,
                        last_failed_at TEXT
                    );";

                using (var command = new SqliteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }

                // Ensure additional columns exist if the table existed with an older schema
                // Check PRAGMA table_info and add missing columns
                var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var pragma = new SqliteCommand("PRAGMA table_info('CONFIGURATION');", connection))
                using (var reader = pragma.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        existingColumns.Add(reader.GetString(reader.GetOrdinal("name")));
                    }
                }

                // Add missing columns safely
                if (!existingColumns.Contains("iterations"))
                {
                    using (var cmd = new SqliteCommand("ALTER TABLE CONFIGURATION ADD COLUMN iterations INTEGER;", connection))
                    {
                        try { cmd.ExecuteNonQuery(); } catch { }
                    }
                }

                if (!existingColumns.Contains("failed_attempts"))
                {
                    using (var cmd = new SqliteCommand("ALTER TABLE CONFIGURATION ADD COLUMN failed_attempts INTEGER DEFAULT 0;", connection))
                    {
                        try { cmd.ExecuteNonQuery(); } catch { }
                    }
                }

                if (!existingColumns.Contains("last_failed_at"))
                {
                    using (var cmd = new SqliteCommand("ALTER TABLE CONFIGURATION ADD COLUMN last_failed_at TEXT;", connection))
                    {
                        try { cmd.ExecuteNonQuery(); } catch { }
                    }
                }

            }
        }

        // Phương thức khác để thực hiện truy vấn SELECT, INSERT, v.v.
        public SqliteConnection GetConnection()
        {
            // Trả về kết nối mở để các DataAccess classes khác sử dụng
            var connection = new SqliteConnection(GetConnectionString());
            connection.Open();
            return connection;
        }

    }
}
