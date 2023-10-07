using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Cache.Common;
using Automatica.Core.Runtime.Recorder.Abstraction;
using Automatica.Core.Runtime.Recorder.Base;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Automatica.Core.Runtime.Recorder
{
    internal class TrendingContext : ITrendingContext
    {
        private readonly ILogger<TrendingContext> _logger;
        private readonly ISettingsCache _settingsCache;
        private readonly IRecorderFactory _recorderFactory;
        private readonly IConfiguration _config;
        private readonly IList<IDataRecorderWriter> _trendingRecorder = new List<IDataRecorderWriter>();

        public TrendingContext(ILogger<TrendingContext> logger, ISettingsCache settingsCache, IRecorderFactory recorderFactory, IConfiguration config)
        {
            _logger = logger;
            _settingsCache = settingsCache;
            _recorderFactory = recorderFactory;
            _config = config;
        }

        public async Task Configure(CancellationToken token = default)
        {
            await Task.CompletedTask;
            try
            {
                _logger.LogInformation($"Loading enabled recorders...");
                var trendingRecorder = _settingsCache.GetByKey("trendingRecorders");
                _trendingRecorder.Clear();
                if (!String.IsNullOrEmpty(trendingRecorder.ValueText))
                {
                    var trendingKvp =
                        JsonConvert.DeserializeObject<IList<KeyValuePair<DataRecorderType, String>>>(trendingRecorder
                            .ValueText);

                    foreach (var kvp in trendingKvp)
                    {
                        try
                        {
                            _trendingRecorder.Add(_recorderFactory.GetRecorder(kvp.Key));
                            _logger.LogInformation($"Added recorder for {kvp.Value}...");
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, $"Could not get recorder {kvp.Key} {kvp.Value}");
                        }
                    }
                }

                _logger.LogInformation($"Loading enabled recorders...done");
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Could not load recorders.....{e}");
            }


            _logger.LogInformation("Loading recording data-points...");
            var recordingDataPointCount = 0;

            await using (var db = new AutomaticaContext(_config))
            {
                var nodeInstances = db.NodeInstances.Where(a => a.Trending).ToList();

                foreach (var node in nodeInstances)
                {
                    _logger.LogDebug($"Node {node.Name} is selected for trending...");
                    foreach (var recorder in _trendingRecorder)
                    {
                        _logger.LogDebug($"{recorder.GetType().Name} added {node.Name} {node.ObjId} for recording...");
                        await recorder.AddTrend(node.ObjId);
                        recordingDataPointCount++;
                    }
                }
            }

            _logger.LogInformation($"Loading recording data-points (found {recordingDataPointCount})...done");
        }

        public async Task Start(CancellationToken token = default)
        {
            _logger.LogInformation("Starting recorders...");
            foreach (var rec in _trendingRecorder)
            {
                await rec.Start();
            }
            _logger.LogInformation("Starting recorders...done");
        }

        public async Task Stop(CancellationToken token = default)
        {
            foreach (var rec in _trendingRecorder)
            {
                await rec.Stop();
            }
        }

        public async Task AddRecording(Guid nodeInstanceId)
        {
            foreach (var recorder in _trendingRecorder)
            {
                _logger.LogDebug($"Node {nodeInstanceId} is added for trending...");
                await recorder.AddTrend(nodeInstanceId);
            }
        }

        public async Task RemoveRecording(Guid nodeInstanceId)
        {
            foreach (var recorder in _trendingRecorder)
            {
                _logger.LogDebug($"Node {nodeInstanceId} is added for trending...");
                await recorder.RemoveTrend(nodeInstanceId);
            }
        }

        public async Task Reload()
        {
            await Stop();
            await Configure();
            await Start();
        }
    }
}
