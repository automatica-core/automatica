using System;
using System.Linq;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Localization;
using Automatica.Core.Base.Templates;
using Automatica.Core.Driver;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Cache.Logic;
using Automatica.Core.Internals.Core;
using Automatica.Core.Internals.Recorder;
using Automatica.Core.WebApi.Controllers;
using Automatica.Core.WebApi.Tests.Base;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Automatica.Core.WebApi.Tests.Data
{
    /// <summary>
    /// Deleting nodes in the config tree against a real (SQLite) database and the real logic caches; the core server is a mock.
    /// </summary>
    public class NodeInstanceDeleteTests : BaseControllerTest<NodeInstanceV2Controller>
    {
        private readonly Mock<ICoreServer> _coreServer = new();
        private readonly Guid _device = Guid.NewGuid();
        private readonly Guid _dataPoint = Guid.NewGuid();
        private readonly Guid _other = Guid.NewGuid();
        private readonly Guid _page;
        private readonly Guid _dataPointOnPage = Guid.NewGuid();
        private readonly Guid _otherOnPage = Guid.NewGuid();
        private readonly Guid _link = Guid.NewGuid();

        public NodeInstanceDeleteTests()
        {
            using var db = new AutomaticaContext(Configuration);
            var rootId = db.NodeInstances.AsNoTracking().Single(a => a.This2ParentNodeInstance == null).ObjId;
            _page = db.RulePages.AsNoTracking().First().ObjId;

            // a device with a data point, and another data point, both placed on a logic page and linked with each other
            db.NodeInstances.Add(new NodeInstance { ObjId = _device, Name = "Wallbox", This2ParentNodeInstance = rootId, Description = "" });
            db.NodeInstances.Add(new NodeInstance { ObjId = _dataPoint, Name = "Ladeleistung", This2ParentNodeInstance = _device, Description = "" });
            db.NodeInstances.Add(new NodeInstance { ObjId = _other, Name = "Hausverbrauch", This2ParentNodeInstance = rootId, Description = "" });
            db.NodeInstance2RulePages.Add(new NodeInstance2RulePage { ObjId = _dataPointOnPage, This2NodeInstance = _dataPoint, This2RulePage = _page });
            db.NodeInstance2RulePages.Add(new NodeInstance2RulePage { ObjId = _otherOnPage, This2NodeInstance = _other, This2RulePage = _page });
            db.Links.Add(new Link { ObjId = _link, This2RulePage = _page, This2NodeInstance2RulePageOutput = _dataPointOnPage, This2NodeInstance2RulePageInput = _otherOnPage });
            db.SaveChanges();
        }

        private NodeInstanceV2Controller CreateController()
        {
            return new NodeInstanceV2Controller(new AutomaticaContext(Configuration), new Mock<INodeInstanceCache>().Object, new Mock<INotifyDriver>().Object,
                _coreServer.Object, new Mock<INodeTemplateCache>().Object, new Mock<IDriverNodesStore>().Object, new Mock<INodeInstanceService>().Object,
                Configuration, new Mock<IRecorderContext>().Object, new Mock<ISettingsCache>().Object, new Mock<ILocalizationProvider>().Object,
                ServiceProvider.GetRequiredService<ILogicCacheFacade>(), NullLogger<NodeInstanceV2Controller>.Instance);
        }

        [Fact]
        public async Task DeleteNode_DeviceWithADataPointOnALogicPage_RemovesTheDataPointFromThePageWithItsLinks()
        {
            var logicCache = ServiceProvider.GetRequiredService<ILogicCacheFacade>();
            Assert.NotNull(logicCache.LogicNodeInstanceCache.Get(_dataPointOnPage));

            var result = await CreateController().DeleteNode(new NodeInstance { ObjId = _device, Name = "Wallbox" });

            Assert.IsType<OkResult>(result);
            using var db = new AutomaticaContext(Configuration);
            Assert.False(db.NodeInstances.Any(a => a.ObjId == _device || a.ObjId == _dataPoint));
            Assert.False(db.NodeInstance2RulePages.Any(a => a.ObjId == _dataPointOnPage));
            Assert.False(db.Links.Any(a => a.ObjId == _link));

            // the other end of the link stays on the page
            Assert.True(db.NodeInstances.Any(a => a.ObjId == _other));
            Assert.True(db.NodeInstance2RulePages.Any(a => a.ObjId == _otherOnPage));

            Assert.Null(logicCache.LogicNodeInstanceCache.Get(_dataPointOnPage));
            _coreServer.Verify(a => a.RemoveLink(_link), Times.Once);
            _coreServer.Verify(a => a.ReloadLinks(), Times.Once);
        }

        [Fact]
        public async Task DeleteNode_DataPointOnALogicPage_IsDeleted()
        {
            var result = await CreateController().DeleteNode(new NodeInstance { ObjId = _other, Name = "Hausverbrauch" });

            Assert.IsType<OkResult>(result);
            using var db = new AutomaticaContext(Configuration);
            Assert.False(db.NodeInstances.Any(a => a.ObjId == _other));
            Assert.False(db.NodeInstance2RulePages.Any(a => a.ObjId == _otherOnPage));
            Assert.False(db.Links.Any(a => a.ObjId == _link));
            Assert.True(db.NodeInstance2RulePages.Any(a => a.ObjId == _dataPointOnPage));
        }

        [Fact]
        public async Task DeleteNode_NotOnALogicPage_DoesNotTouchTheLogics()
        {
            var lonely = Guid.NewGuid();
            using (var db = new AutomaticaContext(Configuration))
            {
                db.NodeInstances.Add(new NodeInstance { ObjId = lonely, Name = "Reserve", This2ParentNodeInstance = _device, Description = "" });
                db.SaveChanges();
            }

            var result = await CreateController().DeleteNode(new NodeInstance { ObjId = lonely, Name = "Reserve" });

            Assert.IsType<OkResult>(result);
            using var check = new AutomaticaContext(Configuration);
            Assert.False(check.NodeInstances.Any(a => a.ObjId == lonely));
            Assert.True(check.Links.Any(a => a.ObjId == _link));
            _coreServer.Verify(a => a.ReloadLinks(), Times.Never);
            _coreServer.Verify(a => a.RemoveLink(It.IsAny<Guid>()), Times.Never);
        }
    }
}
