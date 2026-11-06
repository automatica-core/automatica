using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Logger;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Polly;

namespace Automatica.Core.Driver
{
    /// <summary>
    /// Base implementation for <see cref="IDriver"/>
    /// </summary>
    public abstract class DriverBase : IDriver
    {
        /// <summary>
        /// Pending writes, when the queue is full (e.g. the device is offline) the oldest write is dropped
        /// </summary>
        private const int WriteQueueCapacity = 100;
        private const int WriteQueueWarningThreshold = 10;
        private const int MaxWriteRetries = 5;

        public IDriverContext DriverContext { get; }
        public DispatchableType Type => DispatchableType.NodeInstance;
        public Guid Id => DriverContext.NodeInstance.ObjId;
        public bool IsRemanent => DriverContext.NodeInstance.IsRemanent;
        public bool WriteOnlyIfChanged => DriverContext.NodeInstance.WriteOnlyIfChanged;
        public string Name => DriverContext?.NodeInstance.Name;

        private readonly Channel<WriteRequest> _writeQueue;
        private readonly object _startLock = new();
        private CancellationTokenSource _stopTokenSource;

        private readonly ConcurrentDictionary<IDriverNode, Task> _childStartTasks = new(ReferenceEqualityComparer.Instance);

        private readonly record struct WriteRequest(IDispatchable Source, DispatchValue Value, DispatchChain Chain);

        public string FullName => DriverContext.NodeInstance.FullName;


        public IDriverNode Parent { get; set; }

        public IList<IDriverNode> Children { get; set; }

        public DispatchableSource Source => DispatchableSource.NodeInstance;

        protected ITelegramMonitorInstance TelegramMonitor { get; private set; }
        public int ChildrensCreated { get; private set; }
        public string Error { get; protected set; }

        public NodeInstanceState State => DriverContext.NodeInstance.State;
        
        protected DriverBase(IDriverContext driverContext)
        {
            DriverContext = driverContext;
            Children = new List<IDriverNode>();

            TelegramMonitor = new EmptyTelegramMonitorInstance();
            ChildrensCreated = 0;

            _writeQueue = Channel.CreateBounded<WriteRequest>(new BoundedChannelOptions(WriteQueueCapacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            }, WriteDropped);
        }

        private void WriteDropped(WriteRequest request)
        {
            DriverContext.Logger.LogError(
                $"{FullName} {Id} WriteQueue is full ({WriteQueueCapacity} elements), dropped the oldest write ({request.Value?.Value})...");
        }

        private void Enqueue(IDispatchable source, DispatchValue value)
        {
            var count = _writeQueue.Reader.Count;
            if (count >= WriteQueueWarningThreshold)
            {
                DriverContext.Logger.LogWarning(
                    $"{FullName} {Id} Enqueue write ({value.Value})! WriteQueue has {count} elements...");
            }
            else if (DriverContext.Logger.IsEnabled(LogLevel.Debug))
            {
                DriverContext.Logger.LogDebug(
                    $"{FullName} {Id} Enqueue write ({value.Value})! WriteQueue has {count} elements...");
            }

            // keep the dispatch chain, values the driver dispatches while writing continue it (loop detection)
            if (!_writeQueue.Writer.TryWrite(new WriteRequest(source, value, DispatchChain.Current)))
            {
                DriverContext.Logger.LogWarning($"{FullName} {Id} Could not enqueue write ({value.Value})");
            }
        }

        public async Task<bool> Configure(CancellationToken token = default)
        {
            if (DriverContext.NodeInstance.IsDisabled)
            {
                DriverContext.Logger.LogWarning($"Node {Name} {Id} is disabled");
                return false;
            }
            foreach (var node in DriverContext.NodeInstance.InverseThis2ParentNodeInstanceNavigation)
            {
                if (node.IsDisabled)
                {
                    node.State = NodeInstanceState.Disabled;
                    DriverContext.Logger.LogWarning($"Node {node.Name} {node.ObjId} is disabled");
                    continue;
                }

                node.State = NodeInstanceState.Loaded;

                var logger = DriverContext.Logger;
                if (CreateCustomLogger())
                {
                    var loggerName = $"{DriverContext.Factory.DriverName.ToLowerInvariant()}{LoggerConstants.FileSeparator}{node.ObjId.ToString().ToLowerInvariant()}";
                    logger = DriverContext.LoggerFactory.CreateLogger(loggerName);
                    DriverContext.Logger.LogInformation($"Using logger {loggerName} for node {node.Name}");
                }

                var driverNode = CreateDriverNode(DriverContext.Copy(node, logger));

                if (driverNode == null)
                {
                    DriverContext.Logger.LogError($"Could not create node name: {node.Name}, objId: {node.ObjId}, templateId: {node.This2NodeTemplateNavigation.ObjId}");
                    continue;
                }
                ChildrenCreated(driverNode);
                try
                {
                    driverNode.Parent = this;
                    if (await driverNode.Init(token))
                    {
                        node.State = NodeInstanceState.Initialized;
                        driverNode.Parent = this;
                        Children.Add(driverNode);
                        // ChildrensCreated has to match the IncrementDriverCount calls, it is decremented again when the driver is stopped
                        DriverContext.LicenseContract.IncrementDriverCount();
                        ChildrensCreated += 1;

                        if (DriverContext.LicenseContract.DriverLicenseCountExceeded())
                        {
                            node.State = NodeInstanceState.OutOfDataPoints;
                            DriverContext.Logger.LogError("Cannot instantiate more data-points, license exceeded");
                            return false;
                        }

                        try
                        {
                            await driverNode.Configure(token);
                        }
                        finally
                        {
                            ChildrensCreated += driverNode.ChildrensCreated;
                        }
                    }
                    else
                    {
                        node.Error = driverNode.Error;
                        node.State = NodeInstanceState.UnknownError;
                        DriverContext.Logger.LogError($"Could not init {driverNode.Name}");
                    }
                }
                catch (Exception e)
                {
                    node.State = NodeInstanceState.UnknownError;
                    node.Error = e.ToString();
                    DriverContext.Logger.LogError($"Could not init {driverNode.Name}. Error: {e}");
                }
            }

            return true;
        }

        protected virtual bool CreateTelegramMonitor() => false;

        protected virtual bool CreateCustomLogger() => false;

        protected virtual void ChildrenCreated(IDriverNode child)
        {
            
        }

        public Task<bool> BeforeInit(CancellationToken token = default)
        {
            if (CreateTelegramMonitor())
            {
                TelegramMonitor = DriverContext.TelegramMonitor.CreateTelegramMonitor(DriverContext.NodeInstance, DriverContext.NodeInstance.This2NodeTemplateNavigation.Key);
            }

            return Init(token);
        }

        public virtual Task<bool> Init(CancellationToken token = default)
        {
            return Task.FromResult(true);
        }


        public  virtual Task<IList<NodeInstance>> Scan(CancellationToken token = default)
        {
            return Task.FromResult<IList<NodeInstance>>(new List<NodeInstance>());
        }

        public virtual Task<IList<NodeInstance>> Import(string fileName, CancellationToken token = default)
        {
            return Task.FromResult<IList<NodeInstance>>(new List<NodeInstance>());
        }

        public virtual Task<IList<NodeInstance>> Import(ImportConfig config, CancellationToken token = default)
        {
            return Import(config.FileName, token);
        }

        public void DispatchRead(object value)
        {
            DriverContext.Logger.LogDebug($"Node {Name} dispatching read value {value}");
            DriverContext.Dispatcher.DispatchValue(this, value).ConfigureAwait(false);
        }

        public virtual Task<IList<NodeInstance>> CustomAction(string actionName, CancellationToken token = default)
        {
            return Task.FromResult<IList<NodeInstance>>(new List<NodeInstance>());
        }
        public virtual Task<bool> EnableLearnMode(CancellationToken token = default)
        {
            DriverContext.Logger.LogWarning("Learn mode not implemented");
            return Task.FromResult(false);
        }

        public virtual Task<bool> DisableLearnMode(CancellationToken token = default)
        {
            DriverContext.Logger.LogWarning("Learn mode not implemented");
            return Task.FromResult(false);
        }

        public Task WriteValue(IDispatchable source, DispatchValue value, CancellationToken token = default)
        {
            Enqueue(source, value);
            return Task.CompletedTask;
        }

        protected abstract Task Write(object value, IWriteContext writeContext, CancellationToken token = default);
        protected abstract Task<bool> Read(IReadContext readContext, CancellationToken token = default);

        public Task<bool> Read(CancellationToken token = default)
        {
            return Read(new ReadContext(DriverContext.Dispatcher, this), token);
        }

        public virtual Task OnSave(NodeInstance instance, CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnDelete(NodeInstance instance, CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnReInit(CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        public async Task<bool> StartInternal(CancellationToken token = default)
        {
            if (DriverContext.NodeInstance.IsDisabled)
            {
                DriverContext.Logger.LogWarning($"Node {Name} {Id} is disabled");
                return false;
            }

            DriverContext.Logger.LogInformation($"{FullName} {Id}: Starting...");
            var result = await Start(token);
            DriverContext.Logger.LogInformation($"{FullName} {Id}: Started...");

            return result;
        }

        public virtual Task<bool> Start(CancellationToken token = default)
        {
            CancellationToken stopToken;
            lock (_startLock)
            {
                // only one write task per node, Start can be called again (e.g. by the start retry of the parent)
                if (_stopTokenSource == null)
                {
                    _stopTokenSource = new CancellationTokenSource();
                    var writeToken = _stopTokenSource.Token;
                    _ = Task.Run(() => WriteTask(writeToken), writeToken);
                }

                stopToken = _stopTokenSource.Token;
            }

            // the children start in the background (with retries), Started() of a child is called once its start is done
            foreach (var node in Children.ToList())
            {
                _childStartTasks[node] = Task.Run(() => StartChild(node, stopToken));
            }

            return Task.FromResult(true);
        }

        private async Task StartChild(IDriverNode node, CancellationToken stopToken)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(stopToken);
                cts.CancelAfter(TimeSpan.FromMinutes(2));

                int retry = 0;
                var context = ResilienceContextPool.Shared.Get(cts.Token);
                try
                {
                    var pipeline = DriverContext.RetryContext.GetPipeline();
                    _ = await pipeline.ExecuteOutcomeAsync(async (_, _) =>
                    {
                        try
                        {
                            bool driverStart;
                            if (node is DriverBase driverNode)
                            {
                                driverStart = await driverNode.StartInternal(cts.Token);
                            }
                            else
                            {
                                driverStart = await node.Start(cts.Token);
                            }

                            if (!driverStart)
                            {
                                node.DriverContext.NodeInstance.State = NodeInstanceState.UnknownError;
                                node.DriverContext.NodeInstance.Error = node.Error;
                                throw new DriverDidNotStartProperlyException(
                                    $"{FullName} {Id}: Error starting...Retry: {retry + 1}");
                            }

                            if (node.DriverContext.NodeInstance.State == NodeInstanceState.Initialized)
                            {
                                node.DriverContext.NodeInstance.State = NodeInstanceState.InUse;
                            }

                            return Outcome.FromResult(true);
                        }
                        catch (DriverDidNotStartProperlyException ex)
                        {
                            return Outcome.FromException<bool>(ex);
                        }
                        catch (Exception e)
                        {
                            node.DriverContext.NodeInstance.State = NodeInstanceState.UnknownError;
                            node.DriverContext.NodeInstance.Error = e.ToString();
                            DriverContext.Logger.LogError(e, $"{FullName} {Id}:Could not start...Retry: {retry + 1}");
                            retry++;
                            return Outcome.FromException<bool>(e);
                        }
                    }, context, cts.Token);
                }
                finally
                {
                    ResilienceContextPool.Shared.Return(context);
                }
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, $"{FullName} {Id}: Could not start child {node.Name}");
            }
        }

        protected virtual Task<bool> StartedInternal(CancellationToken token = default)
        {
            return Task.FromResult(true);
        }
        public async Task<bool> Started(CancellationToken token = default)
        {
            await StartedInternal(token).ConfigureAwait(false);
            foreach(var node in Children)
            {
                if (_childStartTasks.TryGetValue(node, out var startTask) && !startTask.IsCompleted)
                {
                    // still starting (e.g. retrying), notify it as soon as its start is done
                    _ = StartedAfterStart(node, startTask, token);
                    continue;
                }
                await node.Started(token);
            }
            return true;
        }

        private async Task StartedAfterStart(IDriverNode node, Task startTask, CancellationToken token)
        {
            try
            {
                await startTask.ConfigureAwait(false);
                await node.Started(token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                DriverContext.Logger.LogError(e, $"{FullName} {Id}: Error notifying started to {node.Name}");
            }
        }

        private async Task WriteTask(CancellationToken token)
        {
            try
            {
                DriverContext.Logger.LogInformation($"{FullName} {Id}: Start write task");
                while (await _writeQueue.Reader.WaitToReadAsync(token))
                {
                    while (!token.IsCancellationRequested && _writeQueue.Reader.TryRead(out var writeData))
                    {
                        if (DriverContext.NodeInstance.IsDisabled)
                        {
                            DriverContext.Logger.LogWarning($"{FullName} {Id}: Node is disabled, stop write task");
                            return;
                        }

                        await WriteWithRetry(writeData, token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                DriverContext.Logger.LogInformation($"{FullName} {Id}: Write task was cancelled...");
            }
            catch (Exception ex)
            {
                DriverContext.Logger.LogError(ex, $"{FullName} {Id}: Error in write task");
            }
            DriverContext.Logger.LogWarning($"{FullName} {Id}: End write task");
        }

        /// <summary>
        /// Writes one value. A failed write is retried in place, but only as long as no newer value is pending, so a retry never overtakes a newer value.
        /// </summary>
        private async Task WriteWithRetry(WriteRequest writeData, CancellationToken stopToken)
        {
            if (writeData.Source == null)
            {
                return;
            }

            if (DriverContext.Logger.IsEnabled(LogLevel.Debug))
            {
                DriverContext.Logger.LogDebug(
                    $"{FullName} {Id}: Dequeue write value from {writeData.Source.Name} with value {writeData.Value}");
            }

            // values the driver dispatches while writing continue the chain of the written value (loop detection)
            DispatchChain.Current = writeData.Chain;

            for (var attempt = 0; ; attempt++)
            {
                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(stopToken))
                {
                    cts.CancelAfter(TimeSpan.FromSeconds(45));

                    try
                    {
                        await Write(writeData.Value.Value, new WriteContext(DriverContext.Dispatcher, this), cts.Token);
                        return;
                    }
                    catch (OperationCanceledException) when (stopToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        if (_writeQueue.Reader.Count > 0)
                        {
                            DriverContext.Logger.LogError(e, $"{FullName} {Id}: Error write value ({writeData.Value.Value}), a newer value is pending...ignore item");
                            return;
                        }

                        if (attempt >= MaxWriteRetries)
                        {
                            DriverContext.Logger.LogError(e, $"{FullName} {Id}: Error write value ({writeData.Value.Value}), write retry exceeded...ignore item");
                            return;
                        }

                        DriverContext.Logger.LogError(e, $"{FullName} {Id}: Error write value ({writeData.Value.Value})...retry {attempt + 1}");
                    }
                }

                if (await NewerWritePending(TimeSpan.FromMilliseconds(250 * (attempt + 1)), stopToken))
                {
                    DriverContext.Logger.LogWarning($"{FullName} {Id}: A newer value is pending, do not retry ({writeData.Value.Value})");
                    return;
                }
            }
        }

        /// <summary>
        /// Waits up to <paramref name="delay"/> for a new write, returns true if one is pending
        /// </summary>
        private async Task<bool> NewerWritePending(TimeSpan delay, CancellationToken stopToken)
        {
            using var delayCts = CancellationTokenSource.CreateLinkedTokenSource(stopToken);
            delayCts.CancelAfter(delay);
            try
            {
                await _writeQueue.Reader.WaitToReadAsync(delayCts.Token);
            }
            catch (OperationCanceledException) when (!stopToken.IsCancellationRequested)
            {
                // delay elapsed
            }

            stopToken.ThrowIfCancellationRequested();
            return _writeQueue.Reader.Count > 0;
        }

        public virtual async Task<bool> Stop(CancellationToken token = default)
        {
            CancellationTokenSource stopTokenSource;
            lock (_startLock)
            {
                stopTokenSource = _stopTokenSource;
                _stopTokenSource = null;
            }

            if (stopTokenSource != null)
            {
                await stopTokenSource.CancelAsync();
                stopTokenSource.Dispose();
            }

            // pending writes must not be written after a restart
            var dropped = 0;
            while (_writeQueue.Reader.TryRead(out _))
            {
                dropped++;
            }

            if (dropped > 0)
            {
                DriverContext.Logger.LogWarning($"{FullName} {Id}: Dropped {dropped} pending writes on stop");
            }

            foreach (var node in Children)
            {
                try
                {
                    await node.Stop(token);
                }
                catch (Exception e)
                {
                    DriverContext.Logger.LogError(e, "Could not stop successfully!");
                }

                DriverContext.NodeInstance.State = NodeInstanceState.Unloaded;
            }
            return true;
        }

        public async Task<bool> Stopped(CancellationToken token = default)
        {
            await StoppedInternal(token).ConfigureAwait(false);
            foreach (var node in Children)
            {
                await node.Stopped(token);
            }

            return true;
        }

        protected virtual Task<bool> StoppedInternal(CancellationToken token = default)
        {
            return Task.FromResult(true);
        }

        protected PropertyInstance GetProperty(NodeInstance instance, string propertyKey)
        {
            return DriverContext.NodeInstance.GetProperty(instance, propertyKey);
        }

        protected PropertyInstance GetProperty(string propertyKey)
        {
            return DriverContext.NodeInstance.GetProperty(propertyKey);
        }
        protected object GetPropertyValue(string propertyKey, object defaultValue)
        {
            return DriverContext.NodeInstance.GetPropertyValue(propertyKey, defaultValue);
        }

        protected double GetPropertyValueDouble(string property)
        {
            return DriverContext.NodeInstance.GetPropertyValueDouble(property);
        }
        protected string GetPropertyValueString(string property)
        {
            return DriverContext.NodeInstance.GetPropertyValueString(property);
        }
        protected long GetPropertyValueLong(string property)
        {
            return DriverContext.NodeInstance.GetPropertyValueLong(property);
        }

        protected int GetPropertyValueInt(string property)
        {
            return DriverContext.NodeInstance.GetPropertyValueInt(property);
        }

        protected bool? GetPropertyValueBool(string property)
        {
            return DriverContext.NodeInstance.GetPropertyValueBool(property);
        }

        public abstract IDriverNode CreateDriverNode(IDriverContext ctx);

    }
}
