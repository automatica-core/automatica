using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.HyperSeries;
using Automatica.Core.HyperSeries.Model;
using Automatica.Core.Internals.Cache.Driver;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Automatica.Core.WebApi.Tests.Http
{
    public sealed class ViewerVisibilityFixture : IDisposable
    {
        public const string Secret = "driver-secret-value";

        private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, NodeInstance> _nodes = new();

        public WebApiHost Host { get; }
        public Mock<IHyperSeriesRepository> Series { get; } = new();
        public Mock<IDispatcher> Dispatcher { get; } = new();
        public Guid OwnGroup { get; }
        public Guid OtherGroup { get; }
        public Guid OwnNode { get; }
        public Guid OtherNode { get; }
        public Guid OpenNode { get; }

        public ViewerVisibilityFixture()
        {
            var nodeCache = new Mock<INodeInstanceCache>();
            nodeCache.Setup(a => a.Get(It.IsAny<Guid>())).Returns((Guid id) => _nodes.TryGetValue(id, out var node) ? node : null);

            Series.SetupGet(a => a.IsActivated).Returns(true);
            Series.Setup(a => a.GetAggregatedValues(It.IsAny<AggregationType>(), It.IsAny<Guid>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>()))
                .ReturnsAsync(new List<AggregatedRecordValue>());

            Host = new WebApiHost(configureServices: services =>
            {
                services.RemoveAll<INodeInstanceCache>();
                services.AddSingleton(nodeCache.Object);
                services.RemoveAll<IHyperSeriesRepository>();
                services.AddSingleton(Series.Object);
                services.RemoveAll<IDispatcher>();
                services.AddSingleton(Dispatcher.Object);
            });

            OwnGroup = Host.CreateUserGroup("own");
            OtherGroup = Host.CreateUserGroup("other");
            OwnNode = AddNode(OwnGroup);
            OtherNode = AddNode(OtherGroup);
            OpenNode = AddNode(null);

            var timestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Dispatcher.Setup(a => a.GetValue(DispatchableType.NodeInstance, It.IsAny<Guid>()))
                .Returns((DispatchableType _, Guid id) => new DispatchValue(id, DispatchableType.NodeInstance, 42, timestamp, DispatchValueSource.Read));
            Dispatcher.Setup(a => a.GetValues(DispatchableType.NodeInstance))
                .Returns(() => new[] { OwnNode, OtherNode, OpenNode }.ToDictionary(a => a,
                    a => new DispatchValue(a, DispatchableType.NodeInstance, 1, timestamp, DispatchValueSource.Read)));
        }

        public Guid AddNode(Guid? group)
        {
            var id = Guid.NewGuid();
            _nodes[id] = Node(id, group);
            return id;
        }

        private NodeInstance Node(Guid id, Guid? group)
        {
            var passwordTemplate = new PropertyTemplate
            {
                ObjId = Guid.NewGuid(),
                Key = "password",
                This2PropertyType = (long)PropertyTemplateType.Password,
                This2PropertyTypeNavigation = new PropertyType { Type = (long)PropertyTemplateType.Password }
            };
            return new NodeInstance
            {
                ObjId = id,
                Name = "node-" + id,
                This2UserGroup = group,
                PropertyInstance = new List<PropertyInstance>
                {
                    new()
                    {
                        ObjId = Guid.NewGuid(),
                        This2PropertyTemplate = passwordTemplate.ObjId,
                        This2PropertyTemplateNavigation = passwordTemplate,
                        ValueString = Secret
                    }
                },
                InverseThis2ParentNodeInstanceNavigation = new List<NodeInstance>
                {
                    new() { ObjId = Guid.NewGuid(), Name = "child-of-other-group", This2UserGroup = OtherGroup },
                    new() { ObjId = Guid.NewGuid(), Name = "child-public" }
                }
            };
        }

        public void Dispose() => Host.Dispose();
    }

    /// <summary>
    /// Viewer endpoints over HTTP with a node cache, dispatcher and hyperseries repository of known content:
    /// nodes, values and series of other user groups look unknown, password values are masked for non-admins (84b6afda, f84b4855).
    /// </summary>
    public class ViewerVisibilityHttpTests : IClassFixture<ViewerVisibilityFixture>
    {
        private const string Secret = ViewerVisibilityFixture.Secret;

        private readonly ViewerVisibilityFixture _fixture;
        private readonly WebApiHost _host;
        private readonly Mock<IHyperSeriesRepository> _series;
        private readonly Guid _ownGroup;
        private readonly Guid _ownNode;
        private readonly Guid _otherNode;
        private readonly Guid _openNode;

        public ViewerVisibilityHttpTests(ViewerVisibilityFixture fixture)
        {
            _fixture = fixture;
            _host = fixture.Host;
            _series = fixture.Series;
            _ownGroup = fixture.OwnGroup;
            _ownNode = fixture.OwnNode;
            _otherNode = fixture.OtherNode;
            _openNode = fixture.OpenNode;
        }

        [Fact]
        public async Task SingleNode_OfOtherGroup_ForVisu_Returns404()
        {
            var visu = _host.CreateClient(_host.CreateVisuToken(_ownGroup));

            Assert.Equal(HttpStatusCode.NotFound, (await visu.GetAsync($"webapi/nodeInstances/single/{_otherNode}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await visu.GetAsync($"webapi/nodeInstances/single/{Guid.NewGuid()}")).StatusCode);
        }

        [Fact]
        public async Task SingleNode_ForVisu_MasksPasswordsAndHidesChildrenOfOtherGroups()
        {
            var visu = _host.CreateClient(_host.CreateVisuToken(_ownGroup));

            foreach (var id in new[] { _ownNode, _openNode })
            {
                var response = await visu.GetAsync($"webapi/nodeInstances/single/{id}");
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                var text = await response.Content.ReadAsStringAsync();
                Assert.DoesNotContain(Secret, text);
                Assert.DoesNotContain("child-of-other-group", text);
                Assert.Contains("child-public", text);
            }
        }

        [Fact]
        public async Task SingleNode_ForAdmin_IsUnfiltered()
        {
            var response = await _host.CreateClient(_host.CreateAdminToken()).GetAsync($"webapi/nodeInstances/single/{_otherNode}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var text = await response.Content.ReadAsStringAsync();
            Assert.Contains(Secret, text);
            Assert.Contains("child-of-other-group", text);
        }

        [Fact]
        public async Task NodeValue_OfOtherGroup_ForVisu_LooksUnknown()
        {
            var visu = _host.CreateClient(_host.CreateVisuToken(_ownGroup));

            Assert.Equal(HttpStatusCode.NoContent, (await visu.GetAsync($"webapi/data/node/{_otherNode}")).StatusCode);
            var own = await (await visu.GetAsync($"webapi/data/node/{_ownNode}")).ReadJson();
            Assert.Equal(42, (int)own["value"]);
        }

        [Fact]
        public async Task CurrentNodeValues_ForVisu_OnlyVisibleNodes_ForAdmin_All()
        {
            var visu = (JObject)await (await _host.CreateClient(_host.CreateVisuToken(_ownGroup)).GetAsync("webapi/data/node/current")).ReadJson();
            var keys = visu.Properties().Select(a => Guid.Parse(a.Name)).ToList();
            Assert.Contains(_ownNode, keys);
            Assert.Contains(_openNode, keys);
            Assert.DoesNotContain(_otherNode, keys);

            var admin = (JObject)await (await _host.CreateClient(_host.CreateAdminToken()).GetAsync("webapi/data/node/current")).ReadJson();
            Assert.Equal(3, admin.Count);
        }

        [Fact]
        public async Task Trend_OfOtherGroup_ForVisu_Returns404()
        {
            var response = await _host.CreateClient(_host.CreateVisuToken(_ownGroup))
                .GetAsync($"webapi/data/trend/{_otherNode}/2026-01-01T00:00:00Z/2026-01-02T00:00:00Z/2026-01-01T00:00:00Z/2026-01-02T00:00:00Z");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task HyperSeries_OfOtherGroup_ForVisu_IsEmpty_AndNotQueried()
        {
            var hidden = _fixture.AddNode(_fixture.OtherGroup);
            var response = await _host.CreateClient(_host.CreateVisuToken(_ownGroup)).GetAsync($"webapi/hyperseries/Hourly?id={hidden}&count=5");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty((JArray)await response.ReadJson());
            _series.Verify(a => a.GetAggregatedValues(It.IsAny<AggregationType>(), hidden, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task HyperSeries_UnknownNode_IsEmpty()
        {
            var unknown = Guid.NewGuid();
            var response = await _host.CreateClient(_host.CreateAdminToken()).GetAsync($"webapi/hyperseries/Hourly?id={unknown}");

            Assert.Empty((JArray)await response.ReadJson());
            _series.Verify(a => a.GetAggregatedValues(It.IsAny<AggregationType>(), unknown, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>()), Times.Never);
        }

        [Theory]
        [InlineData("&count=1000000", HyperSeriesController_MaxCount)]
        [InlineData("&count=0", 1)]
        [InlineData("&count=-5", 1)]
        [InlineData("&count=250", 250)]
        [InlineData("", 10)]
        public async Task HyperSeries_Count_IsClamped(string query, int expected)
        {
            var node = _fixture.AddNode(_ownGroup);
            var response = await _host.CreateClient(_host.CreateVisuToken(_ownGroup)).GetAsync($"webapi/hyperseries/Daily?id={node}{query}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _series.Verify(a => a.GetAggregatedValues(AggregationType.Daily, node, null, null, expected), Times.Once);
        }

        private const int HyperSeriesController_MaxCount = Automatica.Core.WebApi.Controllers.HyperSeriesController.MaxCount;

        [Fact]
        public async Task HyperSeries_OtherGroup_ForAdmin_IsQueried()
        {
            var hidden = _fixture.AddNode(_fixture.OtherGroup);
            await _host.CreateClient(_host.CreateAdminToken()).GetAsync($"webapi/hyperseries/Weekly?id={hidden}&count=3");
            _series.Verify(a => a.GetAggregatedValues(AggregationType.Weekly, hidden, null, null, 3), Times.Once);
        }
    }
}
