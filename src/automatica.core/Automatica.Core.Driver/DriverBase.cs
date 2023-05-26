using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.Common;
using Automatica.Core.Base.Extensions;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Logger;
using Automatica.Core.Base.TelegramMonitor;
using Automatica.Core.EF.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Automatica.Core.Driver
{
    /// <summary>
    /// Base implementation for <see cref="IDriver"/>
    /// </summary>
    public abstract class DriverBase : IDriver
    {
        public IDriverContext DriverContext { get; }
        public DispatchableType Type => DispatchableType.NodeInstance;
        public Guid Id => DriverContext.NodeInstance.ObjId;
        public string Name => DriverContext?.NodeInstance.Name;

        private readonly Queue<(IDispatchable, object)> _writeQueue = new Queue<(IDispatchable, object)>();
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


        private void Enqueue(IDispatchable source, object value)
        {
            _writeQueue.Enqueue((source, value));
            _writeSemaphore.Release(1);
        }

        public bool Configure()
        {
            foreach (var node in DriverContext.NodeInstance.InverseThis2ParentNodeInstanceNavigation)
            {
                node.State = NodeInstanceState.Loaded;

                var logger = DriverContext.Logger;
                if (CreateCustomLogger())
                {
                    var loggerName = $"{DriverContext.Factory.DriverName.ToLowerInvariant()}{LoggerConstants.FileSeparator}{node.ObjId.ToString().ToLowerInvariant()}";
                    logger = DriverContext.LoggerFactory.CreateLogger(loggerName);
                    DriverContext.Logger.LogInformation($"Using logger {loggerName} for node {node.Name}");
                }

                var driverNode = CreateDriverNode(
                    new DriverContext(
                        node, 
                        DriverContext.Factory,
                        DriverContext.Dispatcher, 
                        DriverContext.NodeTemplateFactory, 
                        DriverContext.TelegramMonitor, 
                        DriverContext.LicenseState,
                        logger,
                        DriverContext.LearnMode,
                        DriverContext.CloudApi,
                        DriverContext.LicenseContract,
                        DriverContext.LoggerFactory,
                        DriverContext.IsTest));

                ChildrensCreated += 1;
                if (driverNode == null)
                {
                    continue;
                }
                ChildrenCreated(driverNode);
                try
                {
                    driverNode.Parent = this;
                    if (driverNode.Init())
                    {
                        node.State = NodeInstanceState.Initialized;
                        driverNode.Parent = this;
                        Children.Add(driverNode);

                        if (DriverContext.LicenseState != null && ChildrensCreated >= DriverContext.LicenseState.MaxDataPoints)
                        {
                            node.State = NodeInstanceState.OutOfDatapoits;
                            DriverContext.Logger.LogError("Cannot instantiate more data-points, license exceeded");
                            return false;
                        }

                        driverNode.Configure();

                        DriverContext.Dispatcher.RegisterDispatch(DispatchableType.NodeInstance, node.ObjId, (source, value) =>
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

        public bool BeforeInit()
        {
            if (CreateTelegramMonitor())
            {
                TelegramMonitor = DriverContext.TelegramMonitor.CreateTelegramMonitor(DriverContext.NodeInstance, DriverContext.NodeInstance.This2NodeTemplateNavigation.Key);
            }

            return Init();
        }

        public virtual bool Init()
        {
            return true;
        }

        

        public void DispatchValue(object value)
        {
            DriverContext.Logger.LogDebug($"Node {Name} dispatching value {value}");
            DriverContext.Dispatcher.DispatchValue(this, value);
        }

        public  virtual Task<IList<NodeInstance>> Scan()
        {
            return new Task<IList<NodeInstance>>(() => new List<NodeInstance>());
        }

        public virtual Task<IList<NodeInstance>> Import(string fileName)
        {
            return new Task<IList<NodeInstance>>(() => new List<NodeInstance>());
        }
        public virtual Task<IList<NodeInstance>> CustomAction(string actionName)
        {
            return new Task<IList<NodeInstance>>(() => new List<NodeInstance>());
        }
        public virtual Task<bool> EnableLearnMode()
        {
            DriverContext.Logger.LogWarning("Learn mode not implemented");
            return Task.FromResult(false);
        }

        public virtual Task<bool> DisableLearnMode()
        {
            DriverContext.Logger.LogWarning("Learn mode not implemented");
            return Task.FromResult(false);
        }
        public virtual Task WriteValue(IDispatchable source, object value)
        {
            DriverContext.Logger.LogError($"Write is not implemented in {DriverContext.NodeInstance.Name}");
            return Task.CompletedTask;
        }

        public virtual Task WriteValue(IDispatchable source, DispatchValue value)
        {
            return WriteValue(source, value.Value);
        }

        public virtual Task<bool> Read()
        {
            DriverContext.Logger.LogError($"Read is not implemented in {DriverContext.NodeInstance.Name}");
            return Task.FromResult(false);
        }

        public virtual Task OnSave(NodeInstance instance)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnDelete(NodeInstance instance)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnReinit()
        {
            return Task.CompletedTask;
        }

        public virtual Task<bool> Start()
        {
            _writeTask = Task.Run(WriteTask, _cancellationToken.Token);

            Parallel.ForEach(Children, async node => {
                var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromMinutes(2));
                try
                {
                    var driverStart = await node.Start().WithCancellation(cts.Token);

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

        private async Task WriteTask()
        {
            try
            {
                while (true) {
                    await _writeSemaphore.WaitAsync();

                    var writeData = _writeQueue.Dequeue();

                    DriverContext.Logger.LogDebug($"{FullName}: Dequeue write value from {writeData.Item1.Name} with value {writeData.Item2}");

                    var cts = new CancellationTokenSource();
                    cts.CancelAfter(TimeSpan.FromSeconds(30));

                    try
                    {
                        await WriteValue(writeData.Item1, writeData.Item2).WithCancellation(cts.Token);
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

        public virtual async Task<bool> Stop()
        {
            _cancellationToken.Cancel();

            foreach (var node in Children)
            {
                try
                {
                    await node.Stop();
                }
                catch (Exception e)
                {
                    DriverContext.Logger.LogError(e, "Could not stop successfully!");
                }

                DriverContext.NodeInstance.State = NodeInstanceState.Unloaded;
            }
            return true;
        }

        protected PropertyInstance GetProperty(NodeInstance instance, string propertyKey)
        {
            return DriverContext.NodeInstance.GetProperty(instance, propertyKey);
        }

        protected PropertyInstance GetProperty(string propertyKey)
        {
            return DriverContext.NodeInstance.GetProperty(propertyKey);
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

        public abstract IDriverNode CreateDriverNode(IDriverContext ctx);

    }
}
