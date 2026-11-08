using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Templates;
using Automatica.Core.Tests.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Automatica.Core.Tests.LogicTemplates
{
    /// <summary>
    /// The key of a logic parameter is what visu tiles (getInterfaceByKey), GetDataForVisu and automations look it up by.
    /// </summary>
    public sealed class LogicTemplateFactoryParameterKeyTests : IDisposable, IClassFixture<SeededTemplateFixture>
    {
        private static readonly Guid LogicId = Guid.NewGuid();
        private readonly SqliteTestDatabase _database;

        public LogicTemplateFactoryParameterKeyTests(SeededTemplateFixture template)
        {
            _database = new SqliteTestDatabase(copyFrom: template.Template.File);
        }

        public void Dispose() => _database.Dispose();

        private LogicTemplateFactory Factory(AutomaticaContext db)
        {
            var factory = new LogicTemplateFactory(NullLogger<LogicTemplateFactory>.Instance, db, _database.Configuration);
            factory.CreateLogicTemplate(LogicId, "TEST.LOGIC.NAME", "TEST.LOGIC.DESCRIPTION", "test-logic", "TEST", 100, 100);
            return factory;
        }

        private string StoredKey(Guid parameterId)
        {
            using var db = _database.CreateContext();
            return db.RuleInterfaceTemplates.AsNoTracking().Single(a => a.ObjId == parameterId).Key;
        }

        [Fact]
        public async Task ParameterWithKey_StoresTheKey()
        {
            var parameter = Guid.NewGuid();
            await using (var db = _database.CreateContext())
            {
                using var factory = Factory(db);
                factory.CreateParameterLogicInterfaceTemplate(parameter, "TEST.LOGIC.DELAY.NAME", "TEST.LOGIC.DELAY.DESCRIPTION", "delay",
                    LogicId, 1, RuleInterfaceParameterDataType.Double, 5);
                await factory.CommitChanges();
            }

            Assert.Equal("delay", StoredKey(parameter));
        }

        [Fact]
        public async Task ParameterWithoutKey_UsesTheName()
        {
            var parameter = Guid.NewGuid();
            await using (var db = _database.CreateContext())
            {
                using var factory = Factory(db);
                factory.CreateParameterLogicInterfaceTemplate(parameter, "TEST.LOGIC.DELAY.NAME", "TEST.LOGIC.DELAY.DESCRIPTION",
                    LogicId, 1, RuleInterfaceParameterDataType.Double, 5);
                await factory.CommitChanges();
            }

            Assert.Equal("TEST.LOGIC.DELAY.NAME", StoredKey(parameter));
        }

        [Fact]
        public async Task ExistingParameterStoredWithTheName_GetsTheKeyOnTheNextTemplateInit()
        {
            // installs that ran the templates before the fix have the name as key, a template init (LogicVersion bump) corrects it
            var parameter = Guid.NewGuid();
            await using (var db = _database.CreateContext())
            {
                using var factory = Factory(db);
                factory.CreateParameterLogicInterfaceTemplate(parameter, "TEST.LOGIC.DELAY.NAME", "TEST.LOGIC.DELAY.DESCRIPTION",
                    LogicId, 1, RuleInterfaceParameterDataType.Double, 5);
                await factory.CommitChanges();
            }
            Assert.Equal("TEST.LOGIC.DELAY.NAME", StoredKey(parameter));

            await using (var db = _database.CreateContext())
            {
                using var factory = Factory(db);
                factory.CreateParameterLogicInterfaceTemplate(parameter, "TEST.LOGIC.DELAY.NAME", "TEST.LOGIC.DELAY.DESCRIPTION", "delay",
                    LogicId, 1, RuleInterfaceParameterDataType.Double, 5);
                await factory.CommitChanges();
            }

            Assert.Equal("delay", StoredKey(parameter));
        }
    }
}
