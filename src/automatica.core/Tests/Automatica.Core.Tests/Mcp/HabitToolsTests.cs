using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Habits;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Cache.Logic;
using Automatica.Core.Mcp;
using Automatica.Core.Mcp.Tools;
using Automatica.Core.Model.Models.User;
using Automatica.Core.Tests.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol;
using Moq;
using Xunit;

namespace Automatica.Core.Tests.Mcp
{
    public sealed class HabitToolsTests : IDisposable
    {
        private readonly SqliteTestDatabase _database = new();
        private readonly Guid _light = Guid.NewGuid();
        private readonly Guid _power = Guid.NewGuid();

        public HabitToolsTests()
        {
            using var context = _database.CreateContext();
            context.Database.Migrate();

            var now = DateTime.UtcNow;
            HabitAction Row(Guid target, HabitActionOrigin origin, string value, DateTime utc, double? min = null, double? max = null) =>
                new() { ObjId = Guid.NewGuid(), TargetId = target, Origin = origin, Value = value, UtcTime = utc, LocalTime = utc.ToLocalTime(), Min = min, Max = max };

            context.HabitActions.AddRange(
                Row(_light, HabitActionOrigin.DataPoint, "True", now.AddHours(-3)),
                Row(_light, HabitActionOrigin.DataPoint, "False", now.AddHours(-2)),
                Row(_light, HabitActionOrigin.Visualization, "True", now.AddHours(-1)),
                Row(_power, HabitActionOrigin.Measurement, "1333.333", now.AddMinutes(-30), 0, 3000),
                // outside the default 24 h
                Row(_power, HabitActionOrigin.Measurement, "1", now.AddHours(-30), 1, 1));
            context.SaveChanges();
        }

        private HabitTools Tools(string role)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "patrik"), new Claim(ClaimTypes.Role, role) }, "Bearer"));
            var access = new McpAccess(new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = user } }, Mock.Of<ISettingsCache>());

            var nodes = new Mock<INodeInstanceCache>();
            nodes.Setup(a => a.Get(_light)).Returns(new NodeInstance { ObjId = _light, Name = "Licht Küche" });
            nodes.Setup(a => a.Get(_power)).Returns(new NodeInstance { ObjId = _power, Name = "PV Leistung" });
            var describer = new NodeDescriber(nodes.Object, Mock.Of<IAreaCache>(), Mock.Of<ICategoryCache>(), Mock.Of<IDispatcher>(), Mock.Of<INodeTemplateCache>());

            return new HabitTools(nodes.Object, Mock.Of<ILogicInterfaceInstanceCache>(), Mock.Of<ILogicInstanceCache>(), describer, access, _database.Configuration);
        }

        [Fact]
        public async Task Stats_CountRowsPerKindAndDataPoint()
        {
            var stats = await Tools(Role.AdminRole).GetHabitStats();

            Assert.Equal(4, stats.Rows);
            Assert.Equal(2, stats.DataPoints);
            Assert.Equal(2, stats.RowsByKind["DataPoint"]);
            Assert.Equal(1, stats.RowsByKind["Visualization"]);
            Assert.Equal(1, stats.RowsByKind["Measurement"]);
            var top = stats.Top.First();
            Assert.Equal(("Licht Küche", "DataPoint", 2), (top.Name, top.Kind, top.Rows));
        }

        [Fact]
        public async Task Actions_FilterByKind_NewestFirst()
        {
            var tools = Tools(Role.AdminRole);

            var measurement = Assert.Single(await tools.GetHabitActions(kind: "measurement"));
            Assert.Equal(("PV Leistung", "1333.333", 0d, 3000d), (measurement.Name, measurement.Value, measurement.Min, measurement.Max));

            var light = await tools.GetHabitActions(id: _light);
            Assert.Equal(new[] { "Visualization", "DataPoint", "DataPoint" }, light.Select(a => a.Kind));

            await Assert.ThrowsAsync<McpException>(() => tools.GetHabitActions(kind: "sun"));
        }

        [Fact]
        public async Task NonAdmins_AreRefused()
        {
            await Assert.ThrowsAsync<McpException>(() => Tools(Role.ViewerRole).GetHabitStats());
        }

        public void Dispose() => _database.Dispose();
    }
}
