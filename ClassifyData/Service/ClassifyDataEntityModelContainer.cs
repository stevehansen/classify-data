using System.Configuration;
using System.Data.Entity;
using Vidyano.Service.ClientOperations;
using Vidyano.Service.Repository;

namespace ClassifyData.Service
{
    public class ClassifyDataEntityModelContainer : DbContext
    {
        private static string _connectionString;

        static ClassifyDataEntityModelContainer()
        {
            System.Data.Entity.Database.SetInitializer<ClassifyDataEntityModelContainer>(null);

            ConnectionString = ConfigurationManager.ConnectionStrings["ClassifyDataEntityModelContainer"].ConnectionString;
        }

        public ClassifyDataEntityModelContainer()
            : base(ConnectionString)
        {
        }

        public static string ConnectionString
        {
            get => _connectionString;
            set
            {
                if (_connectionString != value)
                {
                    var newValue = _connectionString != null;
                    _connectionString = value;

                    if (newValue)
                    {
                        var cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                        var connectionStrings = (ConnectionStringsSection)cfg.GetSection("connectionStrings");
                        connectionStrings.ConnectionStrings["ClassifyDataEntityModelContainer"].ConnectionString = _connectionString;
                        cfg.Save(ConfigurationSaveMode.Minimal);

                        Manager.Current.QueueClientOperation(ExecuteMethodOperation.ReloadPage());
                    }
                }
            }
        }

        public static string Exception { get; set; }
    }
}