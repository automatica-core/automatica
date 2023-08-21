using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

namespace Automatica.Core.Runtime.Recorder.HyperSeries
{
    internal class HyperSeriesRecorder : BaseDataRecorderWriter
    {
        private readonly IConfigurationRoot _config;
        private readonly IHyperSeriesRepository _repository;

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);

        private readonly Queue<RecordValue> _queue = new Queue<RecordValue>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public HyperSeriesRecorder(IConfigurationRoot config, INodeInstanceCache nodeCache, IDispatcher dispatcher, IHyperSeriesRepository repository, ILoggerFactory factory) : base(config, DataRecorderType.HyperSeriesRecorder, nameof(HyperSeriesRecorder), nodeCache, dispatcher, factory)
        {
            _config = config;
            _repository = repository;
        }

        public override async Task Start()
        {
            try
            {
                if (!_repository.IsActivated)
                {
                    Logger.LogWarning($"HyperSeriesRecorder is not activated!");
                    return;
                }

                _config.Providers.FirstOrDefault(p => p is DatabaseConfigurationProvider)!.Load();
                await using var hyperContext = new HyperSeriesContext(_config);
                await hyperContext.Database.MigrateAsync();

                _ = Task.Run(WorkerThread, _cancellationTokenSource.Token);

                await base.Start();
                Logger.LogInformation($"Started hyperseries recorder....");
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Could not startup hyperseries recorder....{e}");
            }
        }

        public override Task Stop()
        {
            _cancellationTokenSource.Cancel(true);
            return base.Stop();
        }

        private async Task WorkerThread()
        {
            Logger.LogInformation($"Started worker thread...");
            while (true)
            {
                try
                {
                    await _semaphore.WaitAsync(_cancellationTokenSource.Token);

                    var record = _queue.Dequeue();
                    if (record != null)
                    {
                        await _repository.Add(record);
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
            Logger.LogInformation($"Exited worker thread...");
        }

        internal override Task Save(Trending trend, NodeInstance nodeInstance)
        {
            if (_repository.IsActivated)
            {
                _queue.Enqueue(new RecordValue
                {
                    NodeInstanceId = nodeInstance.ObjId,
                    Timestamp = trend.Timestamp,
                    Value = trend.Value,
                    TrendId = trend.ObjId
                });
                _semaphore.Release();
            }

            return Task.CompletedTask;
        }
    }
}
