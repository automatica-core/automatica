using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Automatica.Core.WebApi.Tests.Http
{
    /// <summary>
    /// Node instances without drivers (the core server is a mock). The add/update round trip needs the node cache loaded
    /// with the driver templates of the plugins and is not covered here.
    /// </summary>
    public class NodeInstanceHttpTests : IClassFixture<WebApiHostFixture>
    {
        private readonly WebApiHost _host;
        private readonly HttpClient _admin;

        public NodeInstanceHttpTests(WebApiHostFixture fixture)
        {
            _host = fixture.Host;
            _admin = _host.CreateClient(_host.CreateAdminToken());
        }

        private NodeInstance NewNode()
        {
            using var db = _host.CreateDbContext();
            var root = db.NodeInstances.AsNoTracking().First(a => a.This2ParentNodeInstance == null);
            var template = db.NodeInstances.AsNoTracking().First(a => a.This2ParentNodeInstance == root.ObjId).This2NodeTemplate;
            return new NodeInstance
            {
                ObjId = Guid.NewGuid(),
                Name = _host.UniqueName("node"),
                Description = "",
                This2ParentNodeInstance = root.ObjId,
                This2NodeTemplate = template
            };
        }

        private NodeInstance Stored(Guid id)
        {
            using var db = _host.CreateDbContext();
            return db.NodeInstances.AsNoTracking().SingleOrDefault(a => a.ObjId == id);
        }

        [Fact]
        public async Task Delete_UnknownNode_Returns404()
        {
            var response = await _admin.PostJson("webapi/nodeInstancesV2/delete", new NodeInstance { ObjId = Guid.NewGuid(), Name = "ghost" });
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task NodeChanges_ByVisu_AreRejected()
        {
            var visu = _host.CreateClient(_host.CreateVisuToken());
            var node = NewNode();

            Assert.Equal(HttpStatusCode.Forbidden, (await visu.PutJson("webapi/nodeInstancesV2/add", node)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await visu.PostJson("webapi/nodeInstancesV2/update", node)).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await visu.PostJson("webapi/nodeInstancesV2/delete", node)).StatusCode);
            Assert.Null(Stored(node.ObjId));
        }
    }
}
