using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.WebApi.Controllers;
using Automatica.Core.WebApi.Tests.Base;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Automatica.Core.WebApi.Tests.Data
{
    /// <summary>
    /// The add-device call of learn mode: a sender that already has a node is refused, so one device never gets two nodes.
    /// </summary>
    public class LearnedNodeTests : BaseControllerTest<NodeInstanceV2Controller>
    {
        private const string SerialKey = "learn-test-serialnumber";

        private readonly Guid _learned;
        private readonly Guid _valve;
        private readonly Guid _elsewhere;

        public LearnedNodeTests()
        {
            using var db = new AutomaticaContext(Configuration);
            var rootId = db.NodeInstances.AsNoTracking().Single(a => a.This2ParentNodeInstance == null).ObjId;
            // the cache needs a template on every node, any seeded one will do
            var nodeTemplate = db.NodeTemplates.AsNoTracking().First().ObjId;

            _learned = Guid.NewGuid();
            db.NodeInstances.Add(new NodeInstance { ObjId = _learned, Name = "Learned", This2ParentNodeInstance = rootId, Description = "", This2NodeTemplate = nodeTemplate });

            // the old nested layout: the device sits one level deeper
            var folder = Guid.NewGuid();
            db.NodeInstances.Add(new NodeInstance { ObjId = folder, Name = "Heating", This2ParentNodeInstance = _learned, Description = "", This2NodeTemplate = nodeTemplate });

            var template = new PropertyTemplate
            {
                ObjId = Guid.NewGuid(), Key = SerialKey, Name = SerialKey, Description = "", Group = "ENOCEAN", Meta = "", DefaultValue = "",
                This2PropertyType = (long)PropertyTemplateType.Text, IsVisible = true
            };
            db.PropertyTemplates.Add(template);

            _valve = Guid.NewGuid();
            db.NodeInstances.Add(new NodeInstance { ObjId = _valve, Name = "Valve office", This2ParentNodeInstance = folder, Description = "", This2NodeTemplate = nodeTemplate });
            db.PropertyInstances.Add(new PropertyInstance
            {
                ObjId = Guid.NewGuid(), This2NodeInstance = _valve, This2PropertyTemplate = template.ObjId, ValueString = "0187 A1B2"
            });

            _elsewhere = Guid.NewGuid();
            db.NodeInstances.Add(new NodeInstance { ObjId = _elsewhere, Name = "Other driver", This2ParentNodeInstance = rootId, Description = "", This2NodeTemplate = nodeTemplate });
            db.SaveChanges();

            // the cache was filled when the database was created
            ServiceProvider.GetRequiredService<INodeInstanceCache>().Clear();
        }

        private LearnedNodeRequest Request(string serial, Guid? replace = null) => new()
        {
            Parent = _learned,
            NodeTemplate = Guid.NewGuid(),
            Name = "Valve kitchen",
            Properties = new Dictionary<string, string> { [SerialKey] = serial },
            UniqueProperty = SerialKey,
            Replace = replace
        };

        [Fact]
        public async Task UnknownParent_IsNotFound()
        {
            var request = Request("0187A1B2");
            request.Parent = Guid.NewGuid();

            var result = await Controller.AddLearnedNode("en", request);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Theory]
        [InlineData("0187A1B2")]
        [InlineData("0187 a1b2")]
        public async Task KnownSerial_IsAConflictNamingTheExistingNode(string serial)
        {
            var result = await Controller.AddLearnedNode("en", Request(serial));

            var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
            var body = Assert.IsType<LearnedNodeConflict>(conflict.Value);
            Assert.Equal(_valve, body.NodeId);
            Assert.Equal("Valve office", body.Name);
        }

        [Fact]
        public async Task Replace_NodeOutsideTheLearnNode_IsRefused()
        {
            var result = await Controller.AddLearnedNode("en", Request("0187FFFF", _elsewhere));

            Assert.IsType<BadRequestObjectResult>(result.Result);
        }
    }
}
