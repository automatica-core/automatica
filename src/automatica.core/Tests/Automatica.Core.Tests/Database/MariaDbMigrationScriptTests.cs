using System.Collections.Generic;
using System.Text.RegularExpressions;
using Automatica.Core.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Automatica.Core.Tests.Database
{
    /// <summary>
    /// The MariaDB SQL of the migrations, generated offline (no database needed).
    /// Existing installations were created by Pomelo and the newer migrations are generated with the SQLite provider,
    /// both have to migrate on MariaDB.
    /// </summary>
    public class MariaDbMigrationScriptTests
    {
        private const string LastPomeloMigration = "20240313151911_AddNotificationsTable";

        private static string Script(string fromMigration)
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ConnectionStrings:AutomaticaDatabaseType"] = "mariadb",
                ["ConnectionStrings:AutomaticaDatabaseMaria"] = "Server=127.0.0.1;User Id=test;Database=automatica",
            }).Build();

            using var context = new AutomaticaContext(config);
            return context.GetService<IMigrator>().GenerateScript(fromMigration, null, MigrationsSqlGenerationOptions.Default);
        }

        [Fact]
        public void ForeignKeysAreOnlyDroppedIfTheyExist()
        {
            // installations that started on Pomelo have no NodeInstance_ibfk_2 / PropertyInstance_ibfk_1
            var script = Script(LastPomeloMigration);

            Assert.Contains("ALTER TABLE `NodeInstances` DROP FOREIGN KEY IF EXISTS `NodeInstance_ibfk_2`;", script);
            Assert.Contains("ALTER TABLE `PropertyInstances` DROP FOREIGN KEY IF EXISTS `PropertyInstance_ibfk_1`;", script);
            Assert.DoesNotContain("DROP CONSTRAINT", script);
        }

        [Fact]
        public void MigrationsGeneratedWithSqliteGetMariaDbColumnTypes()
        {
            var script = Script(LastPomeloMigration);

            Assert.Contains("ALTER TABLE `Notifications` MODIFY `This2RuleInstance` char(36) NULL;", script.Replace("MODIFY COLUMN", "MODIFY"));
            Assert.Contains("ALTER TABLE `Notifications` ADD `This2Automation` char(36) NULL;", script);
            Assert.Contains("`ObjId` char(36) NOT NULL", script);
            Assert.Contains("`CreatedAt` datetime(6) NOT NULL", script);
            Assert.Contains("ADD `MustChangePassword` tinyint(1) NOT NULL", script);

            // SQLite affinities as column types: TEXT keys/indexes fail on MariaDB, TEXT dates/guids are wrong types
            Assert.DoesNotMatch(new Regex(@"`\w+` (TEXT|INTEGER|REAL|BLOB)\b"), script);
        }
    }
}
