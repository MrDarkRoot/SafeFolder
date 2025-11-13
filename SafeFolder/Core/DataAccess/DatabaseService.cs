using LiteDB;
using SafeFolder.Core.Security;
using System.IO;
using System.Linq;

namespace SafeFolder.Core.DataAccess
{
    // Define models for LiteDB
    public class Configuration
    {
        public int Id { get; set; } // LiteDB needs an Id
        public string MasterHash { get; set; }
        public string Salt { get; set; }
        public int TimeoutMins { get; set; }
    }

    public class ProtectedFolder
    {
        public int Id { get; set; }
        public string FolderPath { get; set; }
        public string ProtectionMode { get; set; }
        public string EncKeyStorage { get; set; }
        public bool LockStatus { get; set; }
    }

    /// <summary>
    /// Service for managing the encrypted LiteDB database.
    /// </summary>
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService()
        {
            var credentialManager = new CredentialManager();
            string password = credentialManager.GetOrCreateDatabasePassword();

            string appDataPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
            string dbFolderPath = Path.Combine(appDataPath, "SafeFolderApp");
            string dbPath = Path.Combine(dbFolderPath, "safefolder.db");

            // LiteDB connection string with encryption
            _connectionString = $"Filename={dbPath};Password={password}";

            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var db = new LiteDatabase(_connectionString))
            {
                // Get a collection (or create it if it doesn't exist)
                var col = db.GetCollection<ProtectedFolder>("protected_folders");
                // You can ensure indexes here if needed
                col.EnsureIndex(x => x.FolderPath, true); // true for unique index
            }
        }

        public (string? hash, string? salt) GetConfiguration()
        {
            using (var db = new LiteDatabase(_connectionString))
            {
                var config = db.GetCollection<Configuration>("configuration").FindOne(x => true);
                if (config != null)
                {
                    return (config.MasterHash, config.Salt);
                }
            }
            return (null, null);
        }

        public void SaveConfiguration(string hash, string salt)
        {
            using (var db = new LiteDatabase(_connectionString))
            {
                var col = db.GetCollection<Configuration>("configuration");
                var config = col.FindOne(x => true);

                if (config == null)
                {
                    config = new Configuration { Id = 1 };
                }

                config.MasterHash = hash;
                config.Salt = salt;
                config.TimeoutMins = 5;

                col.Upsert(config); // Inserts if new, updates if exists
            }
        }
        
        // No need for GetConnection() method, LiteDB is used within each method.
    }
}