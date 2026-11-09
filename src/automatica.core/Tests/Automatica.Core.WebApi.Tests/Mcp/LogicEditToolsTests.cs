using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Templates;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Cache.Logic;
using Automatica.Core.Internals.Core;
using Automatica.Core.Internals.Templates;
using Automatica.Core.Logic;
using Automatica.Core.Mcp;
using Automatica.Core.Mcp.Authentication;
using Automatica.Core.Mcp.Tools;
using Automatica.Core.Model.Models.User;
using Automatica.Core.Runtime.Abstraction.Plugins.Logic;
using Automatica.Core.WebApi.Controllers;
using Automatica.Core.WebApi.Tests.Base;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol;
using Moq;
using Xunit;

namespace Automatica.Core.WebApi.Tests.Mcp
{
    /// <summary>
    /// The MCP logic edit tools against a real (SQLite) database and the real logic caches; the core server is a mock that records
    /// which logics and links would be reloaded.
    /// </summary>
    public class LogicEditToolsTests : BaseControllerTest<LogicsController>
    {
        private readonly Mock<ICoreServer> _coreServer = new();
        private readonly Dictionary<string, string> _settings = new();
        private readonly NodeInstance _root;
        private readonly NodeInstance _temperature;
        private readonly NodeInstance _switch;
        private readonly NodeInstance _readOnly;

        public LogicEditToolsTests()
        {
            using (var db = new AutomaticaContext(Configuration))
            {
                var templates = new LogicTemplateFactory(NullLogger<LogicTemplateFactory>.Instance, db, Configuration) { Owner = Guid.NewGuid() };
                new McpTestLogicFactory().InitTemplates(templates);
            }
            ServiceProvider.GetRequiredService<ILogicCacheFacade>().TemplateCache.ClearAndLoad();

            using (var db = new AutomaticaContext(Configuration))
            {
                var rootId = db.NodeInstances.AsNoTracking().Single(a => a.This2ParentNodeInstance == null).ObjId;
                _root = new NodeInstance { ObjId = rootId, Name = "Root", InverseThis2ParentNodeInstanceNavigation = new List<NodeInstance>() };
                _temperature = AddNode(db, "Temperatur", readable: true, writeable: false);
                _switch = AddNode(db, "Licht", readable: true, writeable: true);
                _readOnly = AddNode(db, "Nur schreiben", readable: false, writeable: true);
                db.SaveChanges();
            }
        }

        private NodeInstance AddNode(AutomaticaContext db, string name, bool readable, bool writeable)
        {
            var id = Guid.NewGuid();
            db.NodeInstances.Add(new NodeInstance { ObjId = id, Name = name, This2ParentNodeInstance = _root.ObjId, IsReadable = readable, IsWriteable = writeable });

            // the describer only needs the tree in the cache, with a template that provides a value
            var cached = new NodeInstance
            {
                ObjId = id, Name = name, This2ParentNodeInstance = _root.ObjId, IsReadable = readable, IsWriteable = writeable,
                This2NodeTemplateNavigation = new NodeTemplate { ObjId = Guid.NewGuid(), ProvidesInterface2InterfaceType = NodeDescriber.ValueInterfaceType },
                InverseThis2ParentNodeInstanceNavigation = new List<NodeInstance>()
            };
            _root.InverseThis2ParentNodeInstanceNavigation.Add(cached);
            return cached;
        }

        private LogicEditTools Tools(ClaimsPrincipal user)
        {
            var settings = new Mock<ISettingsCache>();
            settings.Setup(a => a.GetByKey(It.IsAny<string>())).Returns<string>(key => _settings.TryGetValue(key, out var value)
                ? new Setting { ValueKey = key, Type = (long)PropertyTemplateType.Bool, Value = value }
                : null);
            var access = new McpAccess(new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = user } }, settings.Object);

            var nodeCache = new Mock<INodeInstanceCache>();
            nodeCache.SetupGet(a => a.Root).Returns(_root);
            nodeCache.Setup(a => a.Get(It.IsAny<Guid>())).Returns<Guid>(id => id == _root.ObjId ? _root : _root.InverseThis2ParentNodeInstanceNavigation.FirstOrDefault(a => a.ObjId == id));
            var describer = new NodeDescriber(nodeCache.Object, ServiceProvider.GetRequiredService<IAreaCache>(), ServiceProvider.GetRequiredService<ICategoryCache>(),
                ServiceProvider.GetRequiredService<IDispatcher>(), ServiceProvider.GetRequiredService<INodeTemplateCache>());

            return new LogicEditTools(ServiceProvider.GetRequiredService<ILogicCacheFacade>(), _coreServer.Object, ServiceProvider.GetRequiredService<ILogicInstancesStore>(),
                ServiceProvider.GetRequiredService<ILocalizationProvider>(), Configuration, describer, access, NullLogger<LogicEditTools>.Instance);
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

        private static Dictionary<string, JsonElement> Params(string json) => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);

        private async Task<(Guid Page, LogicEdited Logic)> PageWithLogic(LogicEditTools tools, string parameters = null)
        {
            var page = await tools.CreateLogicPage("Energie", "via MCP");
            var logic = await tools.AddLogic(page.Id, McpTestLogicFactory.Key, parameters: parameters == null ? null : Params(parameters));
            return (page.Id, logic);
        }

        // ---------- templates ----------

        [Fact]
        public void Templates_AreListedAndDescribedByKey()
        {
            var tools = Tools(Admin());

            var summary = Assert.Single(tools.ListLogicTemplates("mcp_test"));
            Assert.Equal(McpTestLogicFactory.Key, summary.Key);

            var details = tools.GetLogicTemplate(McpTestLogicFactory.Key);
            Assert.Equal(new[] { "valueIn", "valueOut", "factor", "offset" }, details.Interfaces.Select(a => a.Key));
            Assert.Equal("Param", details.Interfaces.Single(a => a.Key == "factor").Direction);
            Assert.Equal("Double", details.Interfaces.Single(a => a.Key == "factor").ParameterType);
            Assert.True(details.Interfaces.Single(a => a.Key == "offset").LinkableParameter);
            Assert.Equal(details.Id, tools.GetLogicTemplate(details.Id.ToString()).Id);

            var error = Assert.Throws<McpException>(() => tools.GetLogicTemplate("nope"));
            Assert.Contains("list_logic_templates", error.Message);
        }

        // ---------- pages and logics ----------

        [Fact]
        public async Task CreatePageAndAddLogic_StoresBothAndStartsTheLogic()
        {
            var tools = Tools(Admin());

            var (pageId, logic) = await PageWithLogic(tools, "{\"factor\": 2.5}");

            Assert.Equal(pageId, logic.PageId);
            Assert.Equal(McpTestLogicFactory.Key, logic.Type);
            Assert.Equal(2.5, logic.Interfaces.Single(a => a.Key == "factor").ParameterValue);
            Assert.Equal(1.0, logic.Interfaces.Single(a => a.Key == "offset").ParameterValue);
            _coreServer.Verify(a => a.ReloadLogic(logic.Id), Times.Once);

            // the logic editor sees the page and the logic
            var page = Controller.GetPage(pageId);
            Assert.Equal("Energie", page.Name);
            Assert.Equal(logic.Id, Assert.Single(page.RuleInstance).ObjId);

            await using var db = new AutomaticaContext(Configuration);
            var stored = db.RuleInterfaceInstances.Include(a => a.This2RuleInterfaceTemplateNavigation).Where(a => a.This2RuleInstance == logic.Id).ToList();
            Assert.Equal(4, stored.Count);
            Assert.Equal(2.5, stored.Single(a => a.This2RuleInterfaceTemplateNavigation.Key == "factor").ValueDouble);
        }

        [Fact]
        public async Task AddLogic_SecondLogicIsPlacedBelowTheFirst()
        {
            var tools = Tools(Admin());
            var (pageId, first) = await PageWithLogic(tools);

            var second = await tools.AddLogic(pageId, McpTestLogicFactory.Key, "Zweite");

            Assert.Equal("Zweite", second.Name);
            Assert.Equal(first.X, second.X);
            Assert.True(second.Y > first.Y + 100);
        }

        [Fact]
        public async Task AddLogic_UnknownPageTemplateOrParameter_IsRefused()
        {
            var tools = Tools(Admin());
            var page = await tools.CreateLogicPage("P");

            await Assert.ThrowsAsync<McpException>(() => tools.AddLogic(Guid.NewGuid(), McpTestLogicFactory.Key));
            await Assert.ThrowsAsync<McpException>(() => tools.AddLogic(page.Id, "nope"));
            var error = await Assert.ThrowsAsync<McpException>(() => tools.AddLogic(page.Id, McpTestLogicFactory.Key, parameters: Params("{\"valueIn\": 1}")));
            Assert.Contains("factor, offset", error.Message);
            await Assert.ThrowsAsync<McpException>(() => tools.AddLogic(page.Id, McpTestLogicFactory.Key, parameters: Params("{\"factor\": \"abc\"}")));

            await using var db = new AutomaticaContext(Configuration);
            Assert.False(db.RuleInstances.Any(a => a.This2RulePage == page.Id));
        }

        [Fact]
        public async Task UpdateLogic_RenamesMovesAndChangesParameters()
        {
            var tools = Tools(Admin());
            var (_, logic) = await PageWithLogic(tools);

            var updated = await tools.UpdateLogic(logic.Id, "Neu", Params("{\"factor\": 0.001}"), x: 10, y: 20);

            Assert.Equal("Neu", updated.Name);
            Assert.Equal(0.001, updated.Interfaces.Single(a => a.Key == "factor").ParameterValue);
            Assert.Equal((10f, 20f), (updated.X, updated.Y));
            _coreServer.Verify(a => a.ReloadLogic(logic.Id), Times.Exactly(2));
        }

        // ---------- links ----------

        [Fact]
        public async Task LinkDataPointToInput_PlacesTheDataPointLeftOfTheLogic()
        {
            var tools = Tools(Admin());
            var (pageId, logic) = await PageWithLogic(tools);

            var link = await tools.LinkLogic(sourceDatapointId: _temperature.ObjId, targetLogicId: logic.Id, targetInput: "valueIn");

            Assert.False(link.AlreadyExisted);
            Assert.Equal(pageId, link.PageId);
            _coreServer.Verify(a => a.ReloadLinks(), Times.Once);

            var page = await tools.GetLogicPage(pageId);
            var placed = Assert.Single(page.DataPoints);
            Assert.Equal(_temperature.ObjId, placed.DataPointId);
            Assert.Equal("Root / Temperatur", placed.Path);
            Assert.True(placed.X < logic.X);
            var described = Assert.Single(page.Links);
            Assert.Equal(("datapoint", _temperature.ObjId), (described.From.Kind, described.From.Id));
            Assert.Equal(("logic", logic.Id, "valueIn"), (described.To.Kind, described.To.Id, described.To.Interface));
        }

        [Fact]
        public async Task LinkTwice_ReturnsTheExistingLink()
        {
            var tools = Tools(Admin());
            var (_, logic) = await PageWithLogic(tools);

            var first = await tools.LinkLogic(sourceDatapointId: _temperature.ObjId, targetLogicId: logic.Id, targetInput: "VALUEIN");
            var second = await tools.LinkLogic(sourceDatapointId: _temperature.ObjId, targetLogicId: logic.Id, targetInput: "valueIn");

            Assert.True(second.AlreadyExisted);
            Assert.Equal(first.LinkId, second.LinkId);
            await using var db = new AutomaticaContext(Configuration);
            Assert.Single(db.Links.Where(a => a.This2RulePage == first.PageId));
            Assert.Single(db.NodeInstance2RulePages.Where(a => a.This2RulePage == first.PageId));
        }

        [Fact]
        public async Task LinkOutputToDataPointAndToAnotherLogic()
        {
            var tools = Tools(Admin());
            var (pageId, first) = await PageWithLogic(tools);
            var second = await tools.AddLogic(pageId, McpTestLogicFactory.Key, "Zweite");

            await tools.LinkLogic(sourceLogicId: first.Id, sourceOutput: "valueOut", targetDatapointId: _switch.ObjId);
            await tools.LinkLogic(sourceLogicId: first.Id, sourceOutput: "valueOut", targetLogicId: second.Id, targetInput: "valueIn");
            // a linkable parameter counts as an input
            await tools.LinkLogic(sourceLogicId: first.Id, sourceOutput: "valueOut", targetLogicId: second.Id, targetInput: "offset");

            var page = await tools.GetLogicPage(pageId);
            Assert.Equal(3, page.Links.Count);
            Assert.True(Assert.Single(page.DataPoints).X > first.X);
            _coreServer.Verify(a => a.ReloadLogic(second.Id), Times.Exactly(3));
        }

        [Fact]
        public async Task Link_WrongEndsAreRefused()
        {
            var tools = Tools(Admin());
            var (_, logic) = await PageWithLogic(tools);
            var (_, otherPageLogic) = await PageWithLogic(tools);

            async Task<string> Refused(Func<Task> call) => (await Assert.ThrowsAsync<McpException>(call)).Message;

            Assert.Contains("valueIn, offset", await Refused(() => tools.LinkLogic(sourceDatapointId: _temperature.ObjId, targetLogicId: logic.Id, targetInput: "factor")));
            Assert.Contains("valueOut", await Refused(() => tools.LinkLogic(sourceLogicId: logic.Id, sourceOutput: "valueIn", targetDatapointId: _switch.ObjId)));
            Assert.Contains("not writeable", await Refused(() => tools.LinkLogic(sourceLogicId: logic.Id, sourceOutput: "valueOut", targetDatapointId: _temperature.ObjId)));
            Assert.Contains("not readable", await Refused(() => tools.LinkLogic(sourceDatapointId: _readOnly.ObjId, targetLogicId: logic.Id, targetInput: "valueIn")));
            Assert.Contains("same logic page", await Refused(() => tools.LinkLogic(sourceLogicId: logic.Id, sourceOutput: "valueOut", targetLogicId: otherPageLogic.Id, targetInput: "valueIn")));
            Assert.Contains("directly", await Refused(() => tools.LinkLogic(sourceDatapointId: _temperature.ObjId, targetDatapointId: _switch.ObjId)));
            Assert.Contains("exactly one source", await Refused(() => tools.LinkLogic(targetLogicId: logic.Id, targetInput: "valueIn")));
            Assert.Contains("search_datapoints", await Refused(() => tools.LinkLogic(sourceDatapointId: Guid.NewGuid(), targetLogicId: logic.Id, targetInput: "valueIn")));

            await using var db = new AutomaticaContext(Configuration);
            Assert.False(db.Links.Any());
            Assert.False(db.NodeInstance2RulePages.Any());
        }

        [Fact]
        public async Task RemoveLink_RemovesItAndReloads()
        {
            var tools = Tools(Admin());
            var (pageId, logic) = await PageWithLogic(tools);
            var link = await tools.LinkLogic(sourceDatapointId: _temperature.ObjId, targetLogicId: logic.Id, targetInput: "valueIn");

            await tools.RemoveLink(link.LinkId);

            Assert.Empty((await tools.GetLogicPage(pageId)).Links);
            _coreServer.Verify(a => a.RemoveLink(link.LinkId), Times.Once);
            await Assert.ThrowsAsync<McpException>(() => tools.RemoveLink(link.LinkId));
        }

        [Fact]
        public async Task RemoveLogic_RemovesItsLinksAndStopsIt()
        {
            var tools = Tools(Admin());
            var (pageId, first) = await PageWithLogic(tools);
            var second = await tools.AddLogic(pageId, McpTestLogicFactory.Key, "Zweite");
            await tools.LinkLogic(sourceDatapointId: _temperature.ObjId, targetLogicId: first.Id, targetInput: "valueIn");
            await tools.LinkLogic(sourceLogicId: first.Id, sourceOutput: "valueOut", targetLogicId: second.Id, targetInput: "valueIn");

            var result = await tools.RemoveLogic(first.Id);

            Assert.Contains("2 links", result);
            var page = await tools.GetLogicPage(pageId);
            Assert.Equal(second.Id, Assert.Single(page.Logics).Id);
            Assert.Empty(page.Links);
            Assert.Single(page.DataPoints);
            _coreServer.Verify(a => a.StopLogic(first.Id), Times.Once);
            _coreServer.Verify(a => a.ReloadLogic(second.Id), Times.AtLeast(2));
            Assert.Empty(Controller.GetPage(pageId).RuleInstance.Where(a => a.ObjId == first.Id));
        }

        [Fact]
        public void Tools_RegisterWithTheMcpServerLikeOnTheHost()
        {
            var services = new ServiceCollection();
            services.AddMcpServer().WithTools<LogicEditTools>();
            using var provider = services.BuildServiceProvider();

            var tools = provider.GetServices<ModelContextProtocol.Server.McpServerTool>().ToDictionary(a => a.ProtocolTool.Name);

            Assert.Equal(new[] { "add_logic", "create_logic_page", "get_logic_page", "get_logic_template", "link_logic", "list_logic_templates", "remove_link", "remove_logic", "update_logic" },
                tools.Keys.OrderBy(a => a));
            Assert.True(tools["remove_logic"].ProtocolTool.Annotations?.DestructiveHint);
            Assert.True(tools["get_logic_page"].ProtocolTool.Annotations?.ReadOnlyHint);
            var schema = tools["add_logic"].ProtocolTool.InputSchema.ToString();
            Assert.Contains("\"parameters\"", schema);
            Assert.Contains("\"template\"", schema);
            Assert.Contains("\"sourceDatapointId\"", tools["link_logic"].ProtocolTool.InputSchema.ToString());
        }

        // ---------- access ----------

        [Fact]
        public async Task ApiKey_WithoutWrite_ReadsButCannotChange()
        {
            var admin = Tools(Admin());
            var (pageId, logic) = await PageWithLogic(admin);
            var tools = Tools(ApiKey());

            Assert.NotEmpty(tools.ListLogicTemplates());
            Assert.Single((await tools.GetLogicPage(pageId)).Logics);

            var error = await Assert.ThrowsAsync<McpException>(() => tools.CreateLogicPage("x"));
            Assert.Contains("mcpAllowWrite", error.Message);
            await Assert.ThrowsAsync<McpException>(() => tools.AddLogic(pageId, McpTestLogicFactory.Key));
            await Assert.ThrowsAsync<McpException>(() => tools.UpdateLogic(logic.Id, "x"));
            await Assert.ThrowsAsync<McpException>(() => tools.LinkLogic(sourceDatapointId: _temperature.ObjId, targetLogicId: logic.Id, targetInput: "valueIn"));
            await Assert.ThrowsAsync<McpException>(() => tools.RemoveLogic(logic.Id));
        }

        [Fact]
        public async Task ApiKey_WithWrite_CanChange()
        {
            _settings[McpSettings.AllowWriteKey] = "true";
            var tools = Tools(ApiKey());

            var (_, logic) = await PageWithLogic(tools);

            Assert.NotEqual(Guid.Empty, logic.Id);
        }

        [Theory]
        [InlineData(Role.ViewerRole)]
        [InlineData(Role.VisuRole)]
        public async Task NonAdminUsers_AreRefused(string role)
        {
            _settings[McpSettings.AllowWriteKey] = "true";
            var tools = Tools(User(role));

            Assert.Throws<McpException>(() => tools.ListLogicTemplates());
            await Assert.ThrowsAsync<McpException>(() => tools.CreateLogicPage("x"));
        }
    }

    public class McpTestLogicFactory : LogicFactory
    {
        public const string Key = "mcp_test.scale";
        public override string LogicName => nameof(McpTestLogicFactory);
        public override Guid LogicGuid => new("5a0f3c2e-6a63-4d8e-9c1d-2f4f5d6a7b01");
        public override System.Version LogicVersion => new(1, 0, 0, 0);

        public override void InitTemplates(ILogicTemplateFactory factory)
        {
            factory.CreateLogicTemplate(LogicGuid, "MCP_TEST.NAME", "MCP_TEST.DESCRIPTION", Key, "MCP_TEST.GROUP", 100, 100);
            factory.CreateLogicInterfaceTemplate(new Guid("5a0f3c2e-6a63-4d8e-9c1d-2f4f5d6a7b02"), "MCP_TEST.IN", "MCP_TEST.IN", "valueIn", LogicGuid, LogicInterfaceDirection.Input, 1, 1);
            factory.CreateLogicInterfaceTemplate(new Guid("5a0f3c2e-6a63-4d8e-9c1d-2f4f5d6a7b03"), "MCP_TEST.OUT", "MCP_TEST.OUT", "valueOut", LogicGuid, LogicInterfaceDirection.Output, 0, 1);
            factory.CreateParameterLogicInterfaceTemplate(new Guid("5a0f3c2e-6a63-4d8e-9c1d-2f4f5d6a7b04"), "MCP_TEST.FACTOR", "MCP_TEST.FACTOR", "factor", LogicGuid, 1,
                RuleInterfaceParameterDataType.Double, 1.0, false);
            factory.CreateParameterLogicInterfaceTemplate(new Guid("5a0f3c2e-6a63-4d8e-9c1d-2f4f5d6a7b05"), "MCP_TEST.OFFSET", "MCP_TEST.OFFSET", "offset", LogicGuid, 2,
                RuleInterfaceParameterDataType.Double, 1.0, true);
        }

        public override ILogic CreateLogicInstance(ILogicContext context) => throw new NotSupportedException("never started in these tests");
    }
}
