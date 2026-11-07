using System;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Driver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Automatica.Core.Tests.Recorder
{
    /// <summary>
    /// At startup the trended nodes come from the node cache (reading each node from the database took ~15 s on a NAS),
    /// a node added at runtime (its trending settings were just saved) is read from the database
    /// </summary>
    public class AddTrendTests
    {
        private static (MemoryDataRecorderWriter Recorder, Mock<INodeInstanceCache> Cache) Create(NodeInstance cached, NodeInstance fromDatabase)
        {
            var cache = new Mock<INodeInstanceCache>();
            cache.Setup(a => a.Get(It.IsAny<Guid>())).Returns(cached);
            cache.Setup(a => a.GetSingle(It.IsAny<Guid>(), It.IsAny<AutomaticaContext>())).Returns(fromDatabase);

            var recorder = new MemoryDataRecorderWriter(new Mock<IConfiguration>().Object, "testRecorder", cache.Object, new Mock<IDispatcher>().Object, new Mock<ILoggerFactory>().Object);
            return (recorder, cache);
        }

        [Fact]
        public async Task WithoutReload_UsesTheCachedNode()
        {
            var node = new NodeInstance { ObjId = Guid.NewGuid(), Name = "cached" };
            var (recorder, cache) = Create(node, new NodeInstance { ObjId = node.ObjId, Name = "database" });

            await recorder.AddTrend(node.ObjId, reload: false);

            cache.Verify(a => a.GetSingle(It.IsAny<Guid>(), It.IsAny<AutomaticaContext>()), Times.Never);
            Assert.Equal("node_cached", recorder.GetMetricName(node.ObjId));
        }

        [Fact]
        public async Task WithoutReload_ReadsTheDatabaseIfTheNodeIsNotCached()
        {
            var node = new NodeInstance { ObjId = Guid.NewGuid(), Name = "database" };
            var (recorder, cache) = Create(null, node);

            await recorder.AddTrend(node.ObjId, reload: false);

            cache.Verify(a => a.GetSingle(node.ObjId, It.IsAny<AutomaticaContext>()), Times.Once);
            Assert.Equal("node_database", recorder.GetMetricName(node.ObjId));
        }

        [Fact]
        public async Task Default_ReadsTheDatabase()
        {
            var node = new NodeInstance { ObjId = Guid.NewGuid(), Name = "database" };
            var (recorder, cache) = Create(new NodeInstance { ObjId = node.ObjId, Name = "cached" }, node);

            await recorder.AddTrend(node.ObjId);

            cache.Verify(a => a.GetSingle(node.ObjId, It.IsAny<AutomaticaContext>()), Times.Once);
            Assert.Equal("node_database", recorder.GetMetricName(node.ObjId));
        }
    }
}
