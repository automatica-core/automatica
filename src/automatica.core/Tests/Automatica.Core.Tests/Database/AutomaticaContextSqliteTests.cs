using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Automatica.Core.Base.Common;
using Automatica.Core.EF.Models;
using Automatica.Core.Runtime.Core;
using Automatica.Core.Runtime.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;
using Xunit;

namespace Automatica.Core.Tests.Database
{
    /// <summary>
    /// The EF model on SQLite: entity round trips, the delete cascades and the database satellite store of the broker.
    /// Every test gets its own seeded database file.
    /// </summary>
    public sealed class SeededTemplateFixture : IDisposable
    {
        internal SqliteTestDatabase Template { get; }

        public SeededTemplateFixture()
        {
            Template = new SqliteTestDatabase(new Dictionary<string, string> { { "server:initial_admin_password", "Context-made-up-Pa55" } });
            DatabaseInit.EnsureDatabaseCreated(Template.Services());
        }

        public void Dispose() => Template.Dispose();
    }

    public sealed class AutomaticaContextSqliteTests : IDisposable, IClassFixture<SeededTemplateFixture>
    {
        private static readonly DateTimeOffset Fixed = new(2026, 2, 3, 4, 5, 6, TimeSpan.Zero);
        private readonly SqliteTestDatabase _database;

        public AutomaticaContextSqliteTests(SeededTemplateFixture template)
        {
            // a copy of the seeded database, migrating and seeding takes seconds
            _database = new SqliteTestDatabase(copyFrom: template.Template.File);
        }

        public void Dispose() => _database.Dispose();

        private AutomaticaContext Context() => _database.CreateContext();

        /// <summary>
        /// A copy of a seeded node template (all foreign keys valid) with a new id
        /// </summary>
        private Guid CloneNodeTemplate(AutomaticaContext db)
        {
            var existing = db.NodeTemplates.AsNoTracking().First();
            var values = db.Entry(existing).CurrentValues.Clone();
            var copy = new NodeTemplate();
            db.Entry(copy).CurrentValues.SetValues(values);
            copy.ObjId = Guid.NewGuid();
            copy.Key = "test-template-" + copy.ObjId.ToString("N");
            copy.Name = "test template";
            db.NodeTemplates.Add(copy);
            db.SaveChanges();
            return copy.ObjId;
        }

        private NodeInstance Root(AutomaticaContext db) => db.NodeInstances.AsNoTracking().Single(a => a.This2ParentNodeInstance == null);

        private Guid AddNode(AutomaticaContext db, Guid template, Guid parent, string name, Guid? slave = null)
        {
            var node = new NodeInstance
            {
                ObjId = Guid.NewGuid(),
                Name = name,
                Description = "round trip",
                This2NodeTemplate = template,
                This2ParentNodeInstance = parent,
                This2Slave = slave,
                IsRemanent = true,
                CreatedAt = Fixed,
                ModifiedAt = Fixed
            };
            db.NodeInstances.Add(node);
            db.SaveChanges();
            return node.ObjId;
        }

        [Fact]
        public void NodeInstanceTree_RoundTrips()
        {
            Guid template, parent, child, root;
            using (var db = Context())
            {
                template = CloneNodeTemplate(db);
                root = Root(db).ObjId;
                parent = AddNode(db, template, root, "parent");
                child = AddNode(db, template, parent, "child");
            }

            using (var db = Context())
            {
                var loaded = db.NodeInstances.Include(a => a.InverseThis2ParentNodeInstanceNavigation).Single(a => a.ObjId == parent);
                Assert.Equal("parent", loaded.Name);
                Assert.True(loaded.IsRemanent);
                Assert.Equal(root, loaded.This2ParentNodeInstance);
                Assert.Equal(template, loaded.This2NodeTemplate);
                Assert.Equal(Fixed, loaded.CreatedAt);
                Assert.Equal(child, Assert.Single(loaded.InverseThis2ParentNodeInstanceNavigation).ObjId);
            }
        }

        [Fact]
        public void DeletingANodeTemplate_CascadesToItsInstances_InTheDatabase()
        {
            Guid template, node;
            using (var db = Context())
            {
                template = CloneNodeTemplate(db);
                node = AddNode(db, template, Root(db).ObjId, "cascaded");
            }

            // plain SQL: the cascade has to be in the schema (UpdateDeleteCascades), not only in the EF model
            _database.Execute($"DELETE FROM NodeTemplates WHERE ObjId = '{template.ToString().ToUpperInvariant()}' OR ObjId = '{template}'");

            using (var db = Context())
            {
                Assert.False(db.NodeTemplates.Any(a => a.ObjId == template));
                Assert.False(db.NodeInstances.Any(a => a.ObjId == node));
            }
        }

        [Fact]
        public void LogicPage_WithRulesInterfacesAndLinks_RoundTrips()
        {
            Guid page, link, ruleA, ruleB, outA, inB;
            using (var db = Context())
            {
                page = db.RulePages.Single().ObjId;
                var visuTemplate = db.VisuObjectTemplates.AsNoTracking().Select(a => a.ObjId).First();
                var ruleTemplate = new RuleTemplate
                {
                    ObjId = Guid.NewGuid(), Name = "pass", Key = "pass", Description = "pass", Group = "test", Width = 100, Height = 100,
                    This2DefaultMobileVisuTemplate = visuTemplate, CreatedAt = Fixed, ModifiedAt = Fixed
                };
                var inputTemplate = new RuleInterfaceTemplate
                {
                    ObjId = Guid.NewGuid(), Name = "I", Key = "i", This2RuleTemplate = ruleTemplate.ObjId, This2RuleInterfaceDirection = 1,
                    ParameterDataType = RuleInterfaceParameterDataType.Bool, CreatedAt = Fixed, ModifiedAt = Fixed
                };
                var outputTemplate = new RuleInterfaceTemplate
                {
                    ObjId = Guid.NewGuid(), Name = "O", Key = "o", This2RuleTemplate = ruleTemplate.ObjId, This2RuleInterfaceDirection = 2,
                    ParameterDataType = RuleInterfaceParameterDataType.Bool, CreatedAt = Fixed, ModifiedAt = Fixed
                };
                db.RuleTemplates.Add(ruleTemplate);
                db.RuleInterfaceTemplates.AddRange(inputTemplate, outputTemplate);
                db.SaveChanges();

                RuleInstance NewRule(string name) => new()
                {
                    ObjId = Guid.NewGuid(), Name = name, This2RulePage = page, This2RuleTemplate = ruleTemplate.ObjId, X = 1, Y = 2,
                    CreatedAt = Fixed, ModifiedAt = Fixed
                };
                RuleInterfaceInstance Port(RuleInstance rule, RuleInterfaceTemplate template, bool inverted) => new()
                {
                    ObjId = Guid.NewGuid(), This2RuleInstance = rule.ObjId, This2RuleInterfaceTemplate = template.ObjId, Inverted = inverted
                };

                var a = NewRule("a");
                var b = NewRule("b");
                var aOut = Port(a, outputTemplate, true);
                var bIn = Port(b, inputTemplate, false);
                db.RuleInstances.AddRange(a, b);
                db.RuleInterfaceInstances.AddRange(Port(a, inputTemplate, false), aOut, bIn, Port(b, outputTemplate, false));
                db.SaveChanges();

                var l = new Link { ObjId = Guid.NewGuid(), This2RulePage = page, This2RuleInterfaceInstanceOutput = aOut.ObjId, This2RuleInterfaceInstanceInput = bIn.ObjId };
                db.Links.Add(l);
                db.SaveChanges();

                (link, ruleA, ruleB, outA, inB) = (l.ObjId, a.ObjId, b.ObjId, aOut.ObjId, bIn.ObjId);
            }

            using (var db = Context())
            {
                var loaded = db.Links.Include(a => a.This2RuleInterfaceInstanceOutputNavigation).Include(a => a.This2RuleInterfaceInstanceInputNavigation).Single(a => a.ObjId == link);
                Assert.Equal(page, loaded.This2RulePage);
                Assert.Equal(outA, loaded.This2RuleInterfaceInstanceOutput);
                Assert.Equal(inB, loaded.This2RuleInterfaceInstanceInput);
                Assert.True(loaded.This2RuleInterfaceInstanceOutputNavigation.Inverted);
                Assert.Equal(ruleA, loaded.This2RuleInterfaceInstanceOutputNavigation.This2RuleInstance);
                Assert.Equal(ruleB, loaded.This2RuleInterfaceInstanceInputNavigation.This2RuleInstance);
                Assert.Null(loaded.This2NodeInstance2RulePageInput);

                var rules = db.RuleInstances.Include(a => a.RuleInterfaceInstance).Where(a => a.This2RulePage == page).ToList();
                Assert.Equal(2, rules.Count);
                Assert.All(rules, r => Assert.Equal(2, r.RuleInterfaceInstance.Count));
            }
        }

        [Fact]
        public void NodeOnALogicPage_KeepsItsInvertedFlag()
        {
            Guid onPage;
            using (var db = Context())
            {
                var template = CloneNodeTemplate(db);
                var node = AddNode(db, template, Root(db).ObjId, "on page");
                var entry = new NodeInstance2RulePage { ObjId = Guid.NewGuid(), This2NodeInstance = node, This2RulePage = db.RulePages.Single().ObjId, Inverted = true };
                db.NodeInstance2RulePages.Add(entry);
                db.SaveChanges();
                onPage = entry.ObjId;
            }

            using (var db = Context())
            {
                Assert.True(db.NodeInstance2RulePages.Single(a => a.ObjId == onPage).Inverted);
            }
        }

        [Fact]
        public void User_SecurityFields_RoundTrip()
        {
            using (var db = Context())
            {
                var sa = db.Users.Single(a => a.UserName == "sa");
                sa.MustChangePassword = false;
                sa.SecurityStamp = "stamp-made-up-0001";
                db.SaveChanges();
            }

            using (var db = Context())
            {
                var sa = db.Users.Single(a => a.UserName == "sa");
                Assert.False(sa.MustChangePassword);
                Assert.Equal("stamp-made-up-0001", sa.SecurityStamp);
            }
        }

        // ---- the database store behind the broker authenticator ----

        private Slave AddSatellite(string clientId, string clientKey)
        {
            using var db = Context();
            var slave = new Slave { ObjId = Guid.NewGuid(), Name = clientId, ClientId = clientId, ClientKey = clientKey, CreatedAt = Fixed, ModifiedAt = Fixed };
            db.Slaves.Add(slave);
            db.SaveChanges();
            return slave;
        }

        [Fact]
        public void SatelliteStore_FindsSatellitesByClientId_ButNeverTheLocalRow()
        {
            var sat = AddSatellite("sat1", SatelliteClientKey.Hash("store-made-up-key-001"));
            var store = new DatabaseMqttSatelliteStore(_database.Configuration);

            Assert.Equal(sat.ObjId, store.FindSatellite("sat1").ObjId);
            Assert.Null(store.FindSatellite("SAT1-unknown"));
            // the seeded local row has ClientId ""
            Assert.Null(store.FindSatellite(""));
        }

        [Fact]
        public void SatelliteStore_UpdateClientKey_IsPersisted()
        {
            var sat = AddSatellite("sat1", "plain");
            var store = new DatabaseMqttSatelliteStore(_database.Configuration);

            store.UpdateClientKey(sat.ObjId, "pbkdf2-sha512$1$a$b");
            store.UpdateClientKey(Guid.NewGuid(), "ignored");

            using var db = Context();
            Assert.Equal("pbkdf2-sha512$1$a$b", db.Slaves.Single(a => a.ObjId == sat.ObjId).ClientKey);
        }

        [Fact]
        public void SatelliteStore_GetNodeSatellite_ReadsThis2Slave()
        {
            var sat = AddSatellite("sat1", "k");
            Guid onSatellite, local;
            using (var db = Context())
            {
                var template = CloneNodeTemplate(db);
                onSatellite = AddNode(db, template, Root(db).ObjId, "remote", sat.ObjId);
                local = AddNode(db, template, Root(db).ObjId, "local");
            }
            var store = new DatabaseMqttSatelliteStore(_database.Configuration);

            Assert.Equal(sat.ObjId, store.GetNodeSatellite(onSatellite));
            Assert.Null(store.GetNodeSatellite(local));
            Assert.Null(store.GetNodeSatellite(Guid.NewGuid()));
        }

        [Fact]
        public void Authenticator_OnTheDatabase_ReplacesAPlaintextKeyByItsHash()
        {
            const string key = "legacy-db-made-up-key-01";
            var sat = AddSatellite("sat1", key);
            var auth = new MqttBrokerAuthenticator(new DatabaseMqttSatelliteStore(_database.Configuration), new MqttNodeRegistry(),
                new MqttPluginCredentials(new byte[32]), new MqttConnectThrottle(), true, NullLogger.Instance);

            var result = auth.Authenticate("sat1", "sat1", key, new IPEndPoint(IPAddress.Loopback, 1));

            Assert.Equal(MqttConnectReasonCode.Success, result.ReasonCode);
            using var db = Context();
            var stored = db.Slaves.Single(a => a.ObjId == sat.ObjId).ClientKey;
            Assert.True(SatelliteClientKey.IsHashed(stored));
            Assert.True(SatelliteClientKey.Verify(key, stored, out var rehash));
            Assert.False(rehash);
            Assert.Equal(MqttConnectReasonCode.Success, auth.Authenticate("sat1", "sat1", key, new IPEndPoint(IPAddress.Loopback, 1)).ReasonCode);
        }

        [Fact]
        public void Authenticator_OnTheDatabase_RejectsTheLocalRowWithEmptyCredentials()
        {
            var auth = new MqttBrokerAuthenticator(new DatabaseMqttSatelliteStore(_database.Configuration), new MqttNodeRegistry(),
                new MqttPluginCredentials(new byte[32]), new MqttConnectThrottle(), true, NullLogger.Instance);

            Assert.NotEqual(MqttConnectReasonCode.Success, auth.Authenticate("", "", "", new IPEndPoint(IPAddress.Loopback, 1)).ReasonCode);
            Assert.NotEqual(MqttConnectReasonCode.Success, auth.Authenticate("x", "", "x", new IPEndPoint(IPAddress.Loopback, 1)).ReasonCode);
        }

        [Fact]
        public void PluginNodeKnownOnlyInTheDatabase_IsAcceptedForItsSatellite()
        {
            var sat = AddSatellite("sat1", SatelliteClientKey.Hash("store-made-up-key-002"));
            var other = AddSatellite("sat2", SatelliteClientKey.Hash("store-made-up-key-003"));
            Guid node;
            using (var db = Context())
            {
                node = AddNode(db, CloneNodeTemplate(db), Root(db).ObjId, "plugin", sat.ObjId);
            }
            var credentials = new MqttPluginCredentials(new byte[32]);
            var auth = new MqttBrokerAuthenticator(new DatabaseMqttSatelliteStore(_database.Configuration), new MqttNodeRegistry(),
                credentials, new MqttConnectThrottle(), false, NullLogger.Instance);

            Assert.Equal(MqttConnectReasonCode.Success, auth.Authenticate(node.ToString(), "sat1", credentials.GetPassword("sat1", node), new IPEndPoint(IPAddress.Loopback, 1)).ReasonCode);
            Assert.Equal(MqttConnectReasonCode.ClientIdentifierNotValid, auth.Authenticate(node.ToString(), "sat2", credentials.GetPassword("sat2", node), new IPEndPoint(IPAddress.Loopback, 1)).ReasonCode);
            Assert.NotNull(other);
        }

        // ---- broker bind address "auto" ----

        private MqttConnectionValidation Validation(string bindMode)
        {
            _database.Configuration[MqttConnectionValidation.BindConfigKey] = bindMode;
            var options = new MqttServerOptionsBuilder().WithDefaultEndpoint().Build();
            var server = new MqttServerFactory().CreateMqttServer(options);
            return new MqttConnectionValidation(server, options, _database.Configuration, new MqttNodeRegistry(),
                new MqttBrokerAuthenticator(new DatabaseMqttSatelliteStore(_database.Configuration), new MqttNodeRegistry(), new MqttPluginCredentials(new byte[32]),
                    new MqttConnectThrottle(), true, NullLogger.Instance), NullLogger.Instance);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("auto")]
        [InlineData("AUTO")]
        public void AutoBind_WithoutSatellites_ListensOnLoopbackOnly(string mode)
        {
            Assert.Equal(IPAddress.Loopback, Validation(mode).ResolveBindAddress());
        }

        [Fact]
        public void AutoBind_WithASatellite_ListensOnAllAddresses()
        {
            AddSatellite("sat1", "k");

            Assert.Equal(IPAddress.Any, Validation("auto").ResolveBindAddress());
        }
    }
}
