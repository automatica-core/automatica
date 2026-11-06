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

        // filled from the dispatcher/timer threads, emptied by the worker thread
        private readonly System.Collections.Concurrent.ConcurrentQueue<RecordValue> _queue = new System.Collections.Concurrent.ConcurrentQueue<RecordValue>();
        private CancellationTokenSource _cancellationTokenSource;

        private bool _isRunning;

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
            _isRunning = true;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await _semaphore.WaitAsync(token);

                    if (_queue.TryDequeue(out var record))
                    {
                        if (record != null && _repository != null)
                        {
                            Logger.LogDebug($"Adding record to hyperseries db: {record.NodeInstanceId} {record.Value}");
                            await _repository.Add(record);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Error in worker thread {ex}");
                }
            }
            _isRunning = false;
            Logger.LogWarning($"Exited worker thread...");
        }

        internal override Task Save(Trending trend, NodeInstance nodeInstance)
        {
            if (!_isRunning)
            {
                Logger.LogWarning($"Worker thread is not running, cannot enqueue items at the moment...");
                return Task.CompletedTask;
            }
            Logger.LogDebug($"Enqueue record: {nodeInstance.ObjId} {trend.Value} with queue length of {_queue.Count}");
            if (_repository is { IsActivated: true })
            {
                try
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
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"Error enqueue item, current count is {_queue.Count}");
                }
            }

            return Task.CompletedTask;
        }
    }
}
