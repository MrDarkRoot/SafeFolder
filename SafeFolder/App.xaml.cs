using System.Windows;
using SafeFolder.Core.DataAccess;

namespace SafeFolder
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static DatabaseService DbService { get; private set; }

        public App()
        {
            // Create a single instance of the database service for the app's lifetime
            DbService = new DatabaseService();
        }
    }
}
