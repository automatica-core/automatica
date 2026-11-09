using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Trendings;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Core;
using Automatica.Core.Internals.Recorder;
using Automatica.Core.Mcp;
using Automatica.Core.Mcp.Authentication;
using Automatica.Core.Mcp.Tools;
using Automatica.Core.Model.Models.User;
using Automatica.Core.WebApi.Controllers;
using Automatica.Core.WebApi.Tests.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol;
using Moq;
using Xunit;

namespace Automatica.Core.WebApi.Tests.Mcp
{
    /// <summary>
    /// update_node_settings against a real (SQLite) database; the core server is a mock that records which driver would restart.
    /// </summary>
    public class NodeConfigToolsTests : BaseControllerTest<NodeInstanceV2Controller>
    {
        private readonly Mock<ICoreServer> _coreServer = new();
        private readonly Mock<IRecorderContext> _recorder = new();
        private readonly Dictionary<string, string> _settings = new();
        private readonly NodeInstance _driver;
        private readonly NodeInstance _dataPoint;

        public NodeConfigToolsTests()
        {
            using var db = new AutomaticaContext(Configuration);
            var rootId = db.NodeInstances.AsNoTracking().Single(a => a.This2ParentNodeInstance == null).ObjId;

            var driverId = Guid.NewGuid();
            db.NodeInstances.Add(new NodeInstance { ObjId = driverId, Name = "ModBus TCP Keba P30", This2ParentNodeInstance = rootId, Description = "" });
            AddProperty(db, driverId, "modbus-master-ip", PropertyTemplateType.Ip, a => a.ValueString = "192.168.8.119");
            AddProperty(db, driverId, "modbus-master-port", PropertyTemplateType.Integer, a => a.ValueInt = 502);
            AddProperty(db, driverId, "modbus-timeout", PropertyTemplateType.Numeric, a => a.ValueDouble = 1.5);
            AddProperty(db, driverId, "modbus-enabled", PropertyTemplateType.Bool, a => a.ValueBool = true);
            AddProperty(db, driverId, "modbus-password", PropertyTemplateType.Password, a => a.ValueString = "secret");
            AddProperty(db, driverId, "modbus-type", PropertyTemplateType.Text, a => a.ValueString = "MODBUS-MASTER", isReadonly: true);
            AddProperty(db, driverId, "modbus-scan", PropertyTemplateType.Scan, _ => { });

            // the test database has no driver templates: a copy of a seeded template that provides a value makes a data point
            var valueTemplate = db.NodeTemplates.AsNoTracking().First();
            valueTemplate.ObjId = Guid.NewGuid();
            valueTemplate.Key = "mcp-test-value";
            valueTemplate.ProvidesInterface2InterfaceType = NodeDescriber.ValueInterfaceType;
            db.NodeTemplates.Add(valueTemplate);
            var dataPointId = Guid.NewGuid();
            db.NodeInstances.Add(new NodeInstance
            {
                ObjId = dataPointId, Name = "Verdichter-Starts", This2ParentNodeInstance = driverId, This2NodeTemplate = valueTemplate.ObjId, Description = ""
            });
            db.SaveChanges();

            _driver = new NodeInstance { ObjId = driverId, Name = "ModBus TCP Keba P30", This2ParentNodeInstance = rootId };
            _dataPoint = new NodeInstance { ObjId = dataPointId, Name = "Verdichter-Starts", This2ParentNodeInstance = driverId };
        }

        private static void AddProperty(AutomaticaContext db, Guid nodeId, string key, PropertyTemplateType type, Action<PropertyInstance> value, bool isReadonly = false)
        {
            var template = new PropertyTemplate
            {
                ObjId = Guid.NewGuid(), Key = key, Name = key, Description = "", Group = "MODBUS", Meta = "", DefaultValue = "",
                This2PropertyType = (long)type, IsVisible = true, IsReadonly = isReadonly
            };
            db.PropertyTemplates.Add(template);
            var instance = new PropertyInstance { ObjId = Guid.NewGuid(), This2NodeInstance = nodeId, This2PropertyTemplate = template.ObjId };
            value(instance);
            db.PropertyInstances.Add(instance);
        }

        private NodeConfigTools Tools(ClaimsPrincipal user)
        {
            var settings = new Mock<ISettingsCache>();
            settings.Setup(a => a.GetByKey(It.IsAny<string>())).Returns<string>(key => _settings.TryGetValue(key, out var value)
                ? new Setting { ValueKey = key, Type = (long)PropertyTemplateType.Bool, Value = value }
                : null);
            var access = new McpAccess(new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = user } }, settings.Object);

            var nodeCache = new Mock<INodeInstanceCache>();
            nodeCache.Setup(a => a.Get(_driver.ObjId)).Returns(_driver);
            nodeCache.Setup(a => a.Get(_dataPoint.ObjId)).Returns(_dataPoint);

            return new NodeConfigTools(nodeCache.Object, _coreServer.Object, Configuration, access, _recorder.Object, NullLogger<NodeConfigTools>.Instance);
        }

        private static ClaimsPrincipal Admin() => User(Role.AdminRole);

        private static ClaimsPrincipal User(string role)
        {
            return new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "patrik"), new Claim(ClaimTypes.Role, role) }, "Bearer"));
        }

        private static ClaimsPrincipal ApiKey()
        {
            return new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, McpApiKeyAuthenticationHandler.ApiKeyUserName), new Claim(ClaimTypes.Role, Role.ViewerRole) },
                McpApiKeyAuthenticationHandler.SchemeName));
        }

        private static Dictionary<string, JsonElement> Props(string json) => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        private PropertyInstance Stored(string key)
        {
            using var db = new AutomaticaContext(Configuration);
            return db.PropertyInstances.AsNoTracking().Include(a => a.This2PropertyTemplateNavigation).ThenInclude(a => a.This2PropertyTypeNavigation)
                .Single(a => a.This2NodeInstance == _driver.ObjId && a.This2PropertyTemplateNavigation.Key == key);
        }

        [Fact]
        public async Task IpAndPort_AreSavedAndTheDriverRestarts()
        {
            var tools = Tools(Admin());

            var result = await tools.UpdateNodeSettings(_driver.ObjId, Props("{\"modbus-master-ip\": \"192.168.8.247\", \"modbus-master-port\": \"503\"}"));
            await tools.LastDriverRestart;

            Assert.Equal("192.168.8.247", Stored("modbus-master-ip").ValueString);
            Assert.Equal(503, Stored("modbus-master-port").ValueInt);
            Assert.True(result.DriverRestarted);
            var ip = result.Changes.Single(a => a.Key == "modbus-master-ip");
            Assert.Equal("192.168.8.119", ip.OldValue);
            Assert.Equal("192.168.8.247", ip.NewValue);
            _coreServer.Verify(a => a.StartStopDriver(It.Is<NodeInstance>(n => n.ObjId == _driver.ObjId)), Times.Once);
        }

        [Fact]
        public async Task NumbersAndBooleans_AreConverted()
        {
            await Tools(Admin()).UpdateNodeSettings(_driver.ObjId, Props("{\"modbus-timeout\": 2.25, \"modbus-enabled\": false}"));

            Assert.Equal(2.25, Stored("modbus-timeout").ValueDouble);
            Assert.False(Stored("modbus-enabled").ValueBool);
        }

        [Theory]
        [InlineData("{\"modbus-master-port\": \"abc\"}")]
        [InlineData("{\"modbus-master-port\": 1.5}")]
        [InlineData("{\"modbus-master-ip\": \"not an ip\"}")]
        [InlineData("{\"modbus-enabled\": \"yes\"}")]
        [InlineData("{\"modbus-master-port\": [1]}")]
        public async Task InvalidValues_AreRejectedAndNothingIsSaved(string json)
        {
            var tools = Tools(Admin());

            await Assert.ThrowsAsync<McpException>(() => tools.UpdateNodeSettings(_driver.ObjId, Props(json)));

            Assert.Equal("192.168.8.119", Stored("modbus-master-ip").ValueString);
            Assert.Equal(502, Stored("modbus-master-port").ValueInt);
            _coreServer.Verify(a => a.StartStopDriver(It.IsAny<NodeInstance>()), Times.Never);
        }

        [Fact]
        public async Task OneInvalidValue_KeepsTheOthersUnchanged()
        {
            await Assert.ThrowsAsync<McpException>(() => Tools(Admin()).UpdateNodeSettings(_driver.ObjId,
                Props("{\"modbus-master-ip\": \"10.0.0.1\", \"modbus-master-port\": \"abc\"}")));

            Assert.Equal("192.168.8.119", Stored("modbus-master-ip").ValueString);
        }

        [Fact]
        public async Task UnknownKey_ListsTheEditableProperties()
        {
            var error = await Assert.ThrowsAsync<McpException>(() => Tools(Admin()).UpdateNodeSettings(_driver.ObjId, Props("{\"ip\": \"10.0.0.1\"}")));

            Assert.Contains("modbus-master-ip", error.Message);
            Assert.Contains("modbus-master-port", error.Message);
            Assert.DoesNotContain("modbus-type", error.Message);
            Assert.DoesNotContain("modbus-scan", error.Message);
        }

        [Theory]
        [InlineData("modbus-type")]
        [InlineData("modbus-scan")]
        public async Task ReadonlyAndUiOnlyProperties_AreRefused(string key)
        {
            var error = await Assert.ThrowsAsync<McpException>(() => Tools(Admin()).UpdateNodeSettings(_driver.ObjId, Props($"{{\"{key}\": \"x\"}}")));

            Assert.Contains("web UI", error.Message);
        }

        [Fact]
        public async Task Password_IsSavedButNeverReturned()
        {
            var result = await Tools(Admin()).UpdateNodeSettings(_driver.ObjId, Props("{\"modbus-password\": \"new-secret\"}"));

            Assert.Equal("new-secret", Stored("modbus-password").ValueString);
            var change = Assert.Single(result.Changes);
            Assert.Equal("***", change.OldValue);
            Assert.Equal("***", change.NewValue);
        }

        [Fact]
        public async Task NameOnly_RenamesWithoutRestart()
        {
            var tools = Tools(Admin());

            var result = await tools.UpdateNodeSettings(_driver.ObjId, name: "Keba P30 Garage");

            using var db = new AutomaticaContext(Configuration);
            Assert.Equal("Keba P30 Garage", db.NodeInstances.AsNoTracking().Single(a => a.ObjId == _driver.ObjId).Name);
            Assert.False(result.DriverRestarted);
            _coreServer.Verify(a => a.StartStopDriver(It.IsAny<NodeInstance>()), Times.Never);
        }

        [Fact]
        public async Task NothingToChange_IsRejected()
        {
            await Assert.ThrowsAsync<McpException>(() => Tools(Admin()).UpdateNodeSettings(_driver.ObjId));
        }

        [Fact]
        public async Task UnknownNode_IsRejected()
        {
            await Assert.ThrowsAsync<McpException>(() => Tools(Admin()).UpdateNodeSettings(Guid.NewGuid(), Props("{\"modbus-master-port\": 1}")));
        }

        [Fact]
        public async Task ApiKey_NeedsWriteAccess()
        {
            var denied = await Assert.ThrowsAsync<McpException>(() => Tools(ApiKey()).UpdateNodeSettings(_driver.ObjId, Props("{\"modbus-master-port\": 503}")));
            Assert.Contains("mcpAllowWrite", denied.Message);

            _settings[McpSettings.AllowWriteKey] = "True";
            await Tools(ApiKey()).UpdateNodeSettings(_driver.ObjId, Props("{\"modbus-master-port\": 503}"));
            Assert.Equal(503, Stored("modbus-master-port").ValueInt);
        }

        private NodeInstance StoredNode(Guid id)
        {
            using var db = new AutomaticaContext(Configuration);
            return db.NodeInstances.AsNoTracking().Single(a => a.ObjId == id);
        }

        [Fact]
        public async Task History_IsSwitchedOnAndRecordedWithoutDriverRestart()
        {
            var result = await Tools(Admin()).UpdateNodeSettings(_dataPoint.ObjId, history: true, historyType: "onchange");

            var stored = StoredNode(_dataPoint.ObjId);
            Assert.True(stored.Trending);
            Assert.Equal(TrendingTypes.OnChange, stored.TrendingType);
            Assert.Equal(60, stored.TrendingInterval);
            Assert.False(result.DriverRestarted);
            Assert.Contains(result.Changes, a => a.Key == "history" && Equals(a.OldValue, false) && Equals(a.NewValue, true));
            _recorder.Verify(a => a.AddRecording(_dataPoint.ObjId), Times.Once);
            _recorder.Verify(a => a.RemoveRecording(It.IsAny<Guid>()), Times.Never);
            _coreServer.Verify(a => a.StartStopDriver(It.IsAny<NodeInstance>()), Times.Never);
        }

        [Fact]
        public async Task HistoryInterval_OfARecordedDataPoint_RestartsItsRecorder()
        {
            await Tools(Admin()).UpdateNodeSettings(_dataPoint.ObjId, history: true, historyType: "Average", historyInterval: 30);
            _recorder.Invocations.Clear();

            var result = await Tools(Admin()).UpdateNodeSettings(_dataPoint.ObjId, historyInterval: 300);

            Assert.Equal(300, StoredNode(_dataPoint.ObjId).TrendingInterval);
            var change = Assert.Single(result.Changes);
            Assert.Equal("historyInterval", change.Key);
            _recorder.Verify(a => a.RemoveRecording(_dataPoint.ObjId), Times.Once);
            _recorder.Verify(a => a.AddRecording(_dataPoint.ObjId), Times.Once);
        }

        [Fact]
        public async Task History_IsSwitchedOff()
        {
            await Tools(Admin()).UpdateNodeSettings(_dataPoint.ObjId, history: true);
            _recorder.Invocations.Clear();

            await Tools(Admin()).UpdateNodeSettings(_dataPoint.ObjId, history: false);

            Assert.False(StoredNode(_dataPoint.ObjId).Trending);
            _recorder.Verify(a => a.RemoveRecording(_dataPoint.ObjId), Times.Once);
            _recorder.Verify(a => a.AddRecording(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task SameHistory_ChangesNothing()
        {
            await Tools(Admin()).UpdateNodeSettings(_dataPoint.ObjId, history: true, historyType: "Raw", historyInterval: 60);
            _recorder.Invocations.Clear();

            var result = await Tools(Admin()).UpdateNodeSettings(_dataPoint.ObjId, history: true, historyType: "Raw");

            Assert.Empty(result.Changes);
            Assert.Empty(_recorder.Invocations);
        }

        [Theory]
        [InlineData("Median", null)]
        [InlineData("1", null)]
        [InlineData(null, 0)]
        [InlineData(null, 86401)]
        public async Task InvalidHistory_IsRejected(string type, int? interval)
        {
            await Assert.ThrowsAsync<McpException>(() => Tools(Admin()).UpdateNodeSettings(_dataPoint.ObjId, history: true, historyType: type, historyInterval: interval));

            Assert.False(StoredNode(_dataPoint.ObjId).Trending);
            Assert.Empty(_recorder.Invocations);
        }

        [Fact]
        public async Task History_OfADriver_IsRejected()
        {
            var error = await Assert.ThrowsAsync<McpException>(() => Tools(Admin()).UpdateNodeSettings(_driver.ObjId, history: true));

            Assert.Contains("not a data point", error.Message);
        }

        [Fact]
        public async Task NonAdminUser_IsRefused()
        {
            await Assert.ThrowsAsync<McpException>(() => Tools(User(Role.ViewerRole)).UpdateNodeSettings(_driver.ObjId, Props("{\"modbus-master-port\": 503}")));
        }
    }
}
