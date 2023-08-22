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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.Recorder.HyperSeries
{
    internal class HyperSeriesRecorder : BaseDataRecorderWriter
    {
        private readonly IConfigurationRoot _config;
        private readonly IServiceProvider _provider;
        private IHyperSeriesRepository? _repository;

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(0);

        private readonly Queue<RecordValue> _queue = new Queue<RecordValue>();
        private CancellationTokenSource _cancellationTokenSource;

        public HyperSeriesRecorder(IConfigurationRoot config, INodeInstanceCache nodeCache, IDispatcher dispatcher, IServiceProvider provider, ILoggerFactory factory) : base(config, DataRecorderType.HyperSeriesRecorder, nameof(HyperSeriesRecorder), nodeCache, dispatcher, factory)
        {
            _config = config;
            _provider = provider;

            _cancellationTokenSource = new CancellationTokenSource();
        }

        public override async Task Start()
        {
            try
            {
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Cancel(true);
                }

                _cancellationTokenSource = new CancellationTokenSource();
                _repository = _provider.GetRequiredService<IHyperSeriesRepository>();
                if (!_repository.IsActivated)
                {
                    Logger.LogWarning($"HyperSeriesRecorder is not activated!");
                    return;
                }

                _config.Providers.FirstOrDefault(p => p is DatabaseConfigurationProvider)!.Load();
                await using var hyperContext = new HyperSeriesContext(_config);
                await hyperContext.Database.MigrateAsync();

                _ = Task.Run(() => WorkerThread(_cancellationTokenSource.Token));

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

        private async Task WorkerThread(CancellationToken token)
        {
            Logger.LogInformation($"Started worker thread...");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await _semaphore.WaitAsync(token);

                    var record = _queue.Dequeue();
                    if (record != null && _repository != null)
                    {
                        Logger.LogDebug($"Adding record to hyperseries db: {record.NodeInstanceId} {record.Value}");
                        await _repository.Add(record);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
            Logger.LogInformation($"Exited worker thread...");
        }

        internal override Task Save(Trending trend, NodeInstance nodeInstance)
        {
            Logger.LogDebug($"Enqueue record: {nodeInstance.ObjId} {trend.Value}");
            if (_repository is { IsActivated: true })
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
