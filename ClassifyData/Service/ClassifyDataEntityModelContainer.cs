using System;
using System.IO;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Vidyano.Service;
using Vidyano.Service.ClientOperations;
using Vidyano.Service.Repository;

namespace ClassifyData.Service
{
    public class ClassifyDataEntityModelContainer : TargetDbContext
    {
        private const string connectionStringName = nameof(ClassifyDataEntityModelContainer);
        private static readonly string appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        private static string _connectionString;

        static ClassifyDataEntityModelContainer()
        {
            ConnectionString = new ConfigurationBuilder()
                .AddJsonFile(appSettingsPath)
                .Build()
                .GetConnectionString(connectionStringName);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(ConnectionString);
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
                        var appSettings = JsonNode.Parse(File.ReadAllText(appSettingsPath));
                        appSettings["ConnectionStrings"][connectionStringName] = _connectionString;
                        File.WriteAllText(appSettingsPath, appSettings.ToJsonString(new() { WriteIndented = true }));

                        Manager.Current.QueueClientOperation(ExecuteMethodOperation.ReloadPage());
                    }
                }
            }
        }

        public static string Exception { get; set; }
    }
}
