using System;
using System.Collections.Generic;
using System.Linq;
using Automatica.Core.Base.Common;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.UserHelper;
using Automatica.Core.Model.Models.User;
using Automatica.Core.Runtime.Database;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Automatica.Core.Tests.Database
{
    /// <summary>
    /// One database migrated and seeded by <see cref="DatabaseInit"/>, shared by the read-only tests
    /// </summary>
    public sealed class SeededDatabaseFixture : IDisposable
    {
        public const string AdminPassword = "Initial-made-up-Pa55word";
        internal SqliteTestDatabase Database { get; }

        public SeededDatabaseFixture()
        {
            Database = new SqliteTestDatabase(new Dictionary<string, string> { { "server:initial_admin_password", AdminPassword } });
            DatabaseInit.EnsureDatabaseCreated(Database.Services());
        }

        public void Dispose() => Database.Dispose();
    }

    [CollectionDefinition(Name)]
    public class SqliteDatabaseCollection : ICollectionFixture<SeededDatabaseFixture>
    {
        public const string Name = "SqliteDatabase";
    }

    [Collection(SqliteDatabaseCollection.Name)]
    public class DatabaseSeederTests
    {
        private readonly SeededDatabaseFixture _fixture;

        public DatabaseSeederTests(SeededDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        private AutomaticaContext Context() => _fixture.Database.CreateContext();

        private static bool EnvironmentPasswordIsSet => !String.IsNullOrEmpty(Environment.GetEnvironmentVariable(DatabaseInit.InitialAdminPasswordEnv));

        [Fact]
        public void Migrations_AreAllApplied_AndNothingIsPending()
        {
            using var db = Context();

            Assert.Empty(db.Database.GetPendingMigrations());
            var applied = db.Database.GetAppliedMigrations().ToList();
            Assert.Contains(applied, a => a.EndsWith("_UpdateDeleteCascades"));
            Assert.Contains(applied, a => a.EndsWith("_AddUserSecurityFields"));
            Assert.Equal(db.Database.GetMigrations(), applied);
        }

        [Fact]
        public void Model_HasNoChangesWithoutAMigration()
        {
            // EF 9+ throws in Migrate() when the model and the snapshot differ (the missing cascade migration)
            using var db = Context();

            Assert.False(db.Database.HasPendingModelChanges());
        }

        [Fact]
        public void Seeder_CreatesTheAdministratorAndVisuRoles_WithTheirPrivileges()
        {
            using var db = Context();

            var roles = db.Roles.ToList();
            Assert.Equal(new[] { Role.AdminRole, Role.VisuRole }.OrderBy(a => a), roles.Select(a => a.Key).OrderBy(a => a));

            var links = db.Priviledge2Roles.Include(a => a.This2PriviledgeNavigation).Include(a => a.This2RoleNavigation).ToList();
            Assert.Contains(links, a => a.This2RoleNavigation.Key == Role.AdminRole && a.This2PriviledgeNavigation.Key == "superuser");
            Assert.Contains(links, a => a.This2RoleNavigation.Key == Role.VisuRole && a.This2PriviledgeNavigation.Key == "visu");
            Assert.DoesNotContain(links, a => a.This2RoleNavigation.Key == Role.VisuRole && a.This2PriviledgeNavigation.Key == "superuser");
        }

        [Fact]
        public void Seeder_CreatesSa_AsAdministrator_InTheAdminGroup()
        {
            using var db = Context();

            var sa = db.Users.Include(a => a.InverseThis2Roles).ThenInclude(a => a.This2RoleNavigation)
                .Include(a => a.InverseThis2UserGroups).ThenInclude(a => a.This2UserGroupNavigation)
                .Single(a => a.UserName == "sa");

            Assert.Equal(Role.AdminRole, Assert.Single(sa.InverseThis2Roles).This2RoleNavigation.Key);
            Assert.Equal("Admin", Assert.Single(sa.InverseThis2UserGroups).This2UserGroupNavigation.Name);
        }

        [Fact]
        public void Seeder_CreatesVisu_WithTheVisuRoleOnly()
        {
            using var db = Context();

            var visu = db.Users.Include(a => a.InverseThis2Roles).ThenInclude(a => a.This2RoleNavigation)
                .Include(a => a.InverseThis2UserGroups).ThenInclude(a => a.This2UserGroupNavigation)
                .Single(a => a.UserName == "visu");

            Assert.Equal(Role.VisuRole, Assert.Single(visu.InverseThis2Roles).This2RoleNavigation.Key);
            Assert.Equal("Visu", Assert.Single(visu.InverseThis2UserGroups).This2UserGroupNavigation.Name);
            var groupRole = db.UserGroup2Roles.Include(a => a.This2RoleNavigation).Include(a => a.This2UserGroupNavigation).Single();
            Assert.Equal("Visu", groupRole.This2UserGroupNavigation.Name);
            Assert.Equal(Role.VisuRole, groupRole.This2RoleNavigation.Key);
        }

        [Theory]
        [InlineData("sa")]
        [InlineData("visu")]
        public void Seeder_Users_MustChangeTheirPassword_AndHaveASecurityStamp(string userName)
        {
            using var db = Context();
            var user = db.Users.Single(a => a.UserName == userName);

            Assert.True(user.MustChangePassword);
            Assert.False(String.IsNullOrEmpty(user.SecurityStamp));
            Assert.False(String.IsNullOrEmpty(user.Salt));
        }

        [Fact]
        public void Seeder_UsersHaveDifferentSaltsAndStamps()
        {
            using var db = Context();
            var users = db.Users.ToList();

            Assert.Equal(2, users.Select(a => a.Salt).Distinct().Count());
            Assert.Equal(2, users.Select(a => a.SecurityStamp).Distinct().Count());
        }

        [Fact]
        public void Seeder_SaPassword_IsTheConfiguredOne_NotTheOldDefault()
        {
            if (EnvironmentPasswordIsSet)
            {
                return; // the environment wins over the configuration
            }

            using var db = Context();
            var sa = db.Users.Single(a => a.UserName == "sa");

            Assert.True(UserHelper.VerifyPassword(SeededDatabaseFixture.AdminPassword, sa.Salt, sa.Password, out _));
            Assert.False(UserHelper.VerifyPassword("sa", sa.Salt, sa.Password, out _));
            Assert.DoesNotContain(SeededDatabaseFixture.AdminPassword, sa.Password);
        }

        [Fact]
        public void Seeder_VisuPassword_IsNotTheOldDefault_NorTheAdminPassword()
        {
            using var db = Context();
            var visu = db.Users.Single(a => a.UserName == "visu");

            Assert.False(UserHelper.VerifyPassword("visu", visu.Salt, visu.Password, out _));
            Assert.False(UserHelper.VerifyPassword(SeededDatabaseFixture.AdminPassword, visu.Salt, visu.Password, out _));
        }

        [Fact]
        public void Seeder_CreatesTheLocalSatelliteRow_WithoutCredentials()
        {
            using var db = Context();

            var self = db.Slaves.Single();
            Assert.Equal(ServerInfo.SelfSlaveGuid, self.ObjId);
            Assert.Equal("", self.ClientId);
            Assert.Equal("", self.ClientKey);
        }

        [Fact]
        public void Seeder_CreatesTheLogicMetadata()
        {
            using var db = Context();

            Assert.Equal(new long[] { 1, 2, 3 }, db.RuleInterfaceDirections.OrderBy(a => a.ObjId).Select(a => a.ObjId).ToArray());
            Assert.Equal(new[] { "I", "O", "P" }, db.RuleInterfaceDirections.OrderBy(a => a.ObjId).Select(a => a.Key).ToArray());
            Assert.Equal("rules", db.RulePageTypes.Single().Key);
            Assert.Equal(new[] { "mobile", "pc" }, db.VisuPageTypes.Select(a => a.Key).OrderBy(a => a).ToArray());
            Assert.Equal("Logics", db.RulePages.Single().Name);
        }

        [Fact]
        public void Seeder_CreatesTheRootNodeAndTheProjectArea()
        {
            using var db = Context();

            Assert.NotEmpty(db.BoardTypes);
            Assert.Single(db.NodeInstances.Where(a => a.This2ParentNodeInstance == null));
            Assert.Contains(db.AreaInstances, a => a.Name == "Project");
        }

        [Theory]
        [InlineData("ConfigVersion")]
        [InlineData("Latitude")]
        [InlineData("Longitude")]
        [InlineData("apiKey")]
        [InlineData("autoUpdate")]
        [InlineData("cloudUrl")]
        [InlineData("projectName")]
        [InlineData("language")]
        public void Seeder_CreatesTheServerSettings(string key)
        {
            using var db = Context();

            Assert.Single(db.Settings.Where(a => a.ValueKey == key));
        }

        [Fact]
        public void UsersTable_HasTheSecurityColumns()
        {
            using var connection = _fixture.Database.OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM pragma_table_info('Users')";
            var columns = new List<string>();
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    columns.Add(reader.GetString(0));
                }
            }

            Assert.Contains("MustChangePassword", columns);
            Assert.Contains("SecurityStamp", columns);
        }
    }

    /// <summary>
    /// Seeder runs that change their database, each on its own file
    /// </summary>
    public class DatabaseSeederRunTests
    {
        [Fact]
        public void SecondRun_IsIdempotent_AndGivesUsersWithoutAStampANewOne()
        {
            // one database for both checks, seeding takes seconds
            using var database = new SqliteTestDatabase(new Dictionary<string, string> { { "server:initial_admin_password", "Another-made-up-Pa55" } });
            DatabaseInit.EnsureDatabaseCreated(database.Services());

            string saHash;
            int users, roles, settings, nodes, slaves, pages;
            using (var db = database.CreateContext())
            {
                saHash = db.Users.Single(a => a.UserName == "sa").Password;
                (users, roles, settings, nodes, slaves, pages) = (db.Users.Count(), db.Roles.Count(), db.Settings.Count(), db.NodeInstances.Count(), db.Slaves.Count(), db.RulePages.Count());
            }
            database.Execute("UPDATE Users SET SecurityStamp = NULL WHERE UserName = 'sa'");
            database.Execute("UPDATE Users SET SecurityStamp = '' WHERE UserName = 'visu'");

            DatabaseInit.EnsureDatabaseCreated(database.Services());

            using (var db = database.CreateContext())
            {
                Assert.Equal(saHash, db.Users.Single(a => a.UserName == "sa").Password);
                Assert.Equal((users, roles, settings, nodes, slaves, pages),
                    (db.Users.Count(), db.Roles.Count(), db.Settings.Count(), db.NodeInstances.Count(), db.Slaves.Count(), db.RulePages.Count()));
                Assert.All(db.Users.ToList(), u => Assert.False(String.IsNullOrEmpty(u.SecurityStamp)));
            }
        }

        [Fact]
        public void Location_IsSavedWithoutAServerReload_AlsoForAnOlderDatabase()
        {
            // automations and shading read the location live, a full server reload for it only blocked the ui
            using var database = new SqliteTestDatabase(new Dictionary<string, string> { { "server:initial_admin_password", "Location-made-up-Pa55" } });
            DatabaseInit.EnsureDatabaseCreated(database.Services());

            void AssertNoReload()
            {
                using var db = database.CreateContext();
                foreach (var key in new[] { "Latitude", "Longitude" })
                {
                    var setting = db.Settings.Single(a => a.ValueKey == key);
                    Assert.False(setting.NeedsReloadOnChange, key);
                    Assert.Equal(SettingReloadContext.None, setting.ReloadContext);
                }
            }

            AssertNoReload();

            // the flags an older version seeded
            database.Execute($"UPDATE Settings SET NeedsReloadOnChange = 1, ReloadContext = {(int)SettingReloadContext.Server} WHERE ValueKey IN ('Latitude', 'Longitude')");
            DatabaseInit.EnsureDatabaseCreated(database.Services());

            AssertNoReload();
        }

        [Fact]
        public void WithoutAConfiguredPassword_SaGetsARandomPassword()
        {
            if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable(DatabaseInit.InitialAdminPasswordEnv)))
            {
                return;
            }

            using var database = new SqliteTestDatabase();
            DatabaseInit.EnsureDatabaseCreated(database.Services());

            using var db = database.CreateContext();
            var sa = db.Users.Single(a => a.UserName == "sa");
            Assert.False(UserHelper.VerifyPassword("sa", sa.Salt, sa.Password, out _));
            Assert.False(UserHelper.VerifyPassword("", sa.Salt, sa.Password, out _));
            Assert.True(sa.MustChangePassword);
        }

        [Fact]
        public void ExistingUsers_AreNotReplacedByTheSeeder()
        {
            using var database = new SqliteTestDatabase();
            using (var db = database.CreateContext())
            {
                db.Database.Migrate();
                var salt = UserHelper.GenerateNewSalt();
                db.Users.Add(new User
                {
                    ObjId = Guid.NewGuid(), UserName = "owner", FirstName = "o", LastName = "o", Salt = salt,
                    Password = UserHelper.CreatePasswordHash("Owner-made-up-Pa55", salt),
                    CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), ModifiedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
                });
                db.SaveChanges();
            }

            DatabaseInit.EnsureDatabaseCreated(database.Services());

            using (var db = database.CreateContext())
            {
                Assert.Equal("owner", db.Users.Single().UserName);
                Assert.False(String.IsNullOrEmpty(db.Users.Single().SecurityStamp));
                Assert.Empty(db.Roles);
            }
        }
    }
}
