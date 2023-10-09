using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Logger;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Driver
{
    /// <summary>
    /// Base implementation for <see cref="IDriver"/>
    /// </summary>
    public abstract class DriverBase : IDriver
    {
        private bool _isRunning;
        public IDriverContext DriverContext { get; }
        public DispatchableType Type => DispatchableType.NodeInstance;
        public Guid Id => DriverContext.NodeInstance.ObjId;
        public bool IsRemanent => DriverContext.NodeInstance.IsRemanent;
        public bool WriteOnlyIfChanged => DriverContext.NodeInstance.WriteOnlyIfChanged;
        public string Name => DriverContext?.NodeInstance.Name;

        private readonly Queue<(IDispatchable, DispatchValue)> _writeQueue = new Queue<(IDispatchable, DispatchValue)>();
        private readonly SemaphoreSlim _writeSemaphore = new SemaphoreSlim(0, short.MaxValue);
        private readonly CancellationTokenSource _cancellationToken = new CancellationTokenSource();
        private Task _writeTask;

        public string FullName => DriverContext.NodeInstance.FullName;


        public IDriverNode Parent { get; set; }

        public IList<IDriverNode> Children { get; set; }

        public DispatchableSource Source => DispatchableSource.NodeInstance;

        protected ITelegramMonitorInstance TelegramMonitor { get; private set; }
        public int ChildrensCreated { get; private set; }

        public NodeInstanceState State => DriverContext.NodeInstance.State;
        
        protected DriverBase(IDriverContext driverContext)
        {
            DriverContext = driverContext;
            Children = new List<IDriverNode>();

            TelegramMonitor = new EmptyTelegramMonitorInstance();
            ChildrensCreated = 0;
        }


        private void Enqueue(IDispatchable source, DispatchValue value)
        {
            _writeQueue.Enqueue((source, value));
            _writeSemaphore.Release(1);
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

                ChildrensCreated += 1;
                if (driverNode == null)
                {
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
                        DriverContext.LicenseContract.IncrementDriverCount();

                        if (DriverContext.LicenseContract.DriverLicenseCountExceeded())
                        {
                            node.State = NodeInstanceState.OutOfDataPoints;
                            DriverContext.Logger.LogError("Cannot instantiate more data-points, license exceeded");
                            return false;
                        }

                        await driverNode.Configure(token);

                        await DriverContext.Dispatcher.RegisterDispatch(DispatchableType.NodeInstance, node.ObjId, (source, value) =>
                        {
                            if (source.Id == node.ObjId && source.Source == DispatchableSource.NodeInstance)
                            {
                                return;
                            }
                            if (driverNode is DriverBase driverBase)
                            {
                                driverBase.Enqueue(source, value);
                            }
                        });

                        ChildrensCreated += driverNode.ChildrensCreated;
                    }
                    else
                    {
                        node.State = NodeInstanceState.UnknownError;
                        DriverContext.Logger.LogError($"Could not init {driverNode.Name}");
                    }
                }
                catch (Exception e)
                {
                    node.State = NodeInstanceState.UnknownError;
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
            return new Task<IList<NodeInstance>>(() => new List<NodeInstance>());
        }

        public virtual Task<IList<NodeInstance>> Import(string fileName, CancellationToken token = default)
        {
            return new Task<IList<NodeInstance>>(() => new List<NodeInstance>());
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
            return new Task<IList<NodeInstance>>(() => new List<NodeInstance>());
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
        public Task WriteValue(IDispatchable source, object value)
        {
            return Write(value, new WriteContext(DriverContext.Dispatcher, this));
        }

        public Task WriteValue(IDispatchable source, DispatchValue value, CancellationToken token = default)
        {
            return WriteValue(source, value.Value);
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

            return await Start(token);
        }

        public virtual Task<bool> Start(CancellationToken token = default)
        {
            _isRunning = true;
            _writeTask = Task.Run(WriteTask, _cancellationToken.Token);

            Parallel.ForEach(Children, async node => {
                var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromMinutes(2));
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
                        DriverContext.Logger.LogError($"Could not start {node.Name}");
                    }
                    else
                    {
                        if (node.DriverContext.NodeInstance.State == NodeInstanceState.Initialized)
                        {
                            node.DriverContext.NodeInstance.State = NodeInstanceState.InUse;
                        }
                    }
                }
                catch (Exception e)
                {
                    node.DriverContext.NodeInstance.State = NodeInstanceState.UnknownError;
                    DriverContext.Logger.LogError(e, $"Could not start {node.Name}");
                }
            });
            
            return Task.FromResult(true);
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
                await node.Started(token);
            }
            return true;
        }

        private async Task WriteTask()
        {
            try
            {
                while (_isRunning)
                {
                    await _writeSemaphore.WaitAsync(_cancellationToken.Token);

                    if (DriverContext.NodeInstance.IsDisabled)
                    {
                        DriverContext.Logger.LogWarning($"{FullName}: Node is disabled, stop write task");
                        return;
                    }

                    var writeData = _writeQueue.Dequeue();

                    DriverContext.Logger.LogInformation($"{FullName}: Dequeue write value from {writeData.Item1.Name} with value {writeData.Item2}");

                    var cts = new CancellationTokenSource();
                    cts.CancelAfter(TimeSpan.FromSeconds(30));

                    try
                    {
                        await WriteValue(writeData.Item1, writeData.Item2, cts.Token);
                    }
                    catch (Exception e)
                    {
                        DriverContext.Logger.LogError(e, $"{FullName}: Error write value...");
                    }
                }
            }
            catch(TaskCanceledException)
            {

            }
        }

        public virtual async Task<bool> Stop(CancellationToken token = default)
        {
            _isRunning = false;
            _cancellationToken.Cancel();

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
