using System;
using System.Collections.Generic;
using System.IO;
using Automatica.Core.Base.Visu;
using Automatica.Core.EF.Models;
using Automatica.Core.Visu;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Automatica.Core.Tests.Database
{
    /// <summary>
    /// A SQLite database file in its own temp directory, deleted again on dispose
    /// </summary>
    internal sealed class SqliteTestDatabase : IDisposable
    {
        public string Directory { get; }
        public string File { get; }
        public IConfigurationRoot Configuration { get; }

        public SqliteTestDatabase(IDictionary<string, string> settings = null, string copyFrom = null)
        {
            Directory = Path.Combine(Path.GetTempPath(), "automatica-core-tests", Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(Directory);
            File = Path.Combine(Directory, "automatica.db");
            if (copyFrom != null)
            {
                System.IO.File.Copy(copyFrom, File);
            }

            var values = new Dictionary<string, string>
            {
                { "ConnectionStrings:AutomaticaDatabaseType", "sqlite" },
                // no pooling: the file must be deletable on dispose (windows)
                { "ConnectionStrings:AutomaticaDatabaseSqlite", $"Data Source={File};Pooling=False" }
            };
            if (settings != null)
            {
                foreach (var s in settings)
                {
                    values[s.Key] = s.Value;
                }
            }
            Configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        }

        public AutomaticaContext CreateContext() => new(Configuration);

        public IServiceProvider Services()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(Configuration);
            services.AddSingleton(Configuration);
            services.AddSingleton<IDbContextFactory<AutomaticaContext>>(new AutomaticaContextFactory(Configuration));
            services.AddSingleton<IVisualisationFactory>(new VisuTempInit());
            return services.BuildServiceProvider();
        }

        public SqliteConnection OpenConnection()
        {
            var connection = new SqliteConnection($"Data Source={File};Pooling=False");
            connection.Open();
            return connection;
        }

        public object Scalar(string sql)
        {
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            return command.ExecuteScalar();
        }

        public int Execute(string sql)
        {
            using var connection = OpenConnection();
            using var pragma = connection.CreateCommand();
            pragma.CommandText = "PRAGMA foreign_keys = ON;";
            pragma.ExecuteNonQuery();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            return command.ExecuteNonQuery();
        }

        public void Dispose()
        {
            SqliteConnection.ClearAllPools();
            try
            {
                System.IO.Directory.Delete(Directory, true);
            }
            catch (IOException)
            {
                // a connection that is still finalizing, the temp directory is cleaned up by the OS
            }
        }
    }
}
