using System;
using System.Linq;
using Automatica.Core.Runtime.Recorder.Base;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Trendings;
using Automatica.Core.HyperSeries;
using Automatica.Core.HyperSeries.Model;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Internals.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.DotNet.Scaffolding.Shared;

namespace Automatica.Core.Runtime.Recorder.HyperSeries
{
    internal class HyperSeriesRecorder : BaseDataRecorderWriter
    {
        private readonly IConfigurationRoot _config;

        public HyperSeriesRecorder(IConfigurationRoot config, INodeInstanceCache nodeCache, IDispatcher dispatcher, HyperSeriesContext hyperContext, ILoggerFactory factory) : base(config, DataRecorderType.HyperSeriesRecorder, nameof(HyperSeriesRecorder), nodeCache, dispatcher, factory)
        {
            _config = config;;
        }

        public override async Task Start()
        {
            try
            {
                _config.Providers.FirstOrDefault(p => p is DatabaseConfigurationProvider)!.Load();
                await using var hyperContext = new HyperSeriesContext(_config);
                await hyperContext.Database.MigrateAsync();

                await base.Start();
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Could not startup hyperseries recorder....{e}");
            }
        }

        internal override async Task Save(Trending trend, NodeInstance nodeInstance)
        {
            await using var hyperContext = new HyperSeriesContext(_config);
            await hyperContext.AddRecordValue(new RecordValue
            {
                NodeInstanceId = nodeInstance.ObjId,
                Timestamp = trend.Timestamp,
                Value = trend.Value,
                TrendId = trend.ObjId
            });

        }
    }
}
