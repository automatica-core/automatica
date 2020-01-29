using System;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Trendings;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Runtime.Recorder;
using Moq;
using Xunit;

namespace Automatica.Core.Tests.Recorder
{
    public class TrendingValueRecorderTests
    {
        private async Task<MemoryDataRecorderWriter> CreateRecorder(NodeInstance node)
        {
            var dispatcherMock = new Mock<IDispatcher>();
            var nodeInstanceCache = new Mock<INodeInstanceCache>();
            nodeInstanceCache.Setup(a => a.Get(It.IsAny<Guid>())).Returns(node);

            var recorder = new MemoryDataRecorderWriter("testRecorder", nodeInstanceCache.Object, dispatcherMock.Object);

            await recorder.AddTrend(node.ObjId);

            return recorder;
        }

        [Fact]
        public async Task TestAverageRecording()
        {
            var node = new NodeInstance
            {
                ObjId = Guid.NewGuid(),
                Trending = false,
                TrendingType = TrendingTypes.Average
            };

            var recorder = await CreateRecorder(node);
            var trendingValueRecorder = new TrendingValueRecorder(node, recorder);
            await trendingValueRecorder.Start();

            trendingValueRecorder.ValueChanged(4, "testSource");
            trendingValueRecorder.ValueChanged(2, "testSource");
            trendingValueRecorder.RecordValue();

            Assert.Equal(3, recorder.LastTrending.Value);
        }

        [Fact]
        public async Task TestMaxRecording()
        {
            var node = new NodeInstance
            {
                ObjId = Guid.NewGuid(),
                Trending = false,
                TrendingType = TrendingTypes.Max
            };

            var recorder = await CreateRecorder(node);
            var trendingValueRecorder = new TrendingValueRecorder(node, recorder);
            await trendingValueRecorder.Start();

            trendingValueRecorder.ValueChanged(4, "testSource");
            trendingValueRecorder.ValueChanged(2, "testSource");
            trendingValueRecorder.RecordValue();

            Assert.Equal(4, recorder.LastTrending.Value);
        }

        [Fact]
        public async Task TestMinRecording()
        {
            var node = new NodeInstance
            {
                ObjId = Guid.NewGuid(),
                Trending = false,
                TrendingType = TrendingTypes.Min
            };

            var recorder = await CreateRecorder(node);
            var trendingValueRecorder = new TrendingValueRecorder(node, recorder);
            await trendingValueRecorder.Start();

            trendingValueRecorder.ValueChanged(4, "testSource");
            trendingValueRecorder.ValueChanged(2, "testSource");
            trendingValueRecorder.RecordValue();

            Assert.Equal(2, recorder.LastTrending.Value);
        }

        [Fact]
        public async Task TestRawRecording()
        {
            var node = new NodeInstance
            {
                ObjId = Guid.NewGuid(),
                Trending = false,
                TrendingType = TrendingTypes.Raw
            };

            var recorder = await CreateRecorder(node);
            var trendingValueRecorder = new TrendingValueRecorder(node, recorder);
            await trendingValueRecorder.Start();

            trendingValueRecorder.ValueChanged(4, "testSource");
            trendingValueRecorder.RecordValue();
            Assert.Equal(4, recorder.LastTrending.Value);
            
            trendingValueRecorder.ValueChanged(2, "testSource");
            trendingValueRecorder.RecordValue();
            Assert.Equal(2, recorder.LastTrending.Value);

        }
    }
}
