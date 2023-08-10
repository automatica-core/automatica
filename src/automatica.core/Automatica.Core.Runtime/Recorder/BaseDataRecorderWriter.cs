using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Logger;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Trendings;
using Automatica.Core.Internals;
using Automatica.Core.Internals.Cache.Driver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("Automatica.Core.Tests")]

namespace Automatica.Core.Runtime.Recorder
{
    internal abstract class BaseDataRecorderWriter : IDataRecorderWriter
    {
        private readonly IConfiguration _config;
        private readonly INodeInstanceCache _nodeCache;

        private readonly Dictionary<Guid, List<IDataRecorder>> _recorders = new Dictionary<Guid, List<IDataRecorder>>();
        protected readonly Dictionary<Guid, string> MetricName = new();
        private readonly ILogger _logger;


        internal BaseDataRecorderWriter(IConfiguration config, DataRecorderType recorderType, string recorderName, INodeInstanceCache nodeCache, IDispatcher dispatcher, ILoggerFactory factory)
        {
            _logger = factory.CreateLogger($"recording{LoggerConstants.FileSeparator}{recorderName}");
            _config = config;
            _nodeCache = nodeCache;
            RecorderType = recorderType;
            Dispatcher = dispatcher;

        }

        public ILogger Logger => _logger;

        public DataRecorderType RecorderType { get; }
        public IDispatcher Dispatcher { get; }

        public async Task AddTrend(Guid nodeInstance)
        {
            var node = _nodeCache.GetSingle(nodeInstance, new AutomaticaContext(_config));

            if (node == null)
            {
                Logger.LogWarning($"Could not create recorderWriter for {nodeInstance} - not found");
                return;
            }

            if (!_recorders.ContainsKey(nodeInstance))
            {
                _recorders.Add(nodeInstance, new List<IDataRecorder>());
            }

            if (!MetricName.ContainsKey(nodeInstance))
            {
                MetricName.Add(nodeInstance, GetFullNodeName(node));
            }
            
            _recorders[nodeInstance].Add(new TrendingValueRecorder(node, this));
            await Dispatcher.RegisterDispatch(DispatchableType.NodeInstance, nodeInstance, DataCallback);
        }

        private string GetFullNodeName(NodeInstance nodeInstance)
        {
            var nameList = new List<string>();
            nameList.Add(nodeInstance.Name);
            GetFullNameRecursive(nodeInstance, ref nameList);
            nameList.Reverse();
            var name = String.Join("-", nameList);

            name = name
                .Replace("*", "")
                .Replace(" ", "_")
                .Replace("/", "_")
                .Replace("-", "_")
                .Replace("(", "")
                .Replace(")", "")
                .Replace("[", "")
                .Replace("]", "").ToLowerInvariant();
            name = "node_" + name;
            return name;
        }

        private void GetFullNameRecursive(NodeInstance instance, ref List<string> names)
        {

            if (!instance.This2ParentNodeInstance.HasValue)
            {
                return;
            }
            var parent = _nodeCache.Get(instance.This2ParentNodeInstance.Value);
            if (parent == null)
            {
                return;
            }

            if (instance.This2NodeTemplateNavigation is not null &&
                instance.This2NodeTemplateNavigation.IsAdapterInterface.HasValue &&
                instance.This2NodeTemplateNavigation.IsAdapterInterface.Value)
            {
                return;
            }

            names.Add(parent.Name);
            GetFullNameRecursive(parent, ref names);
        }
        private void DataCallback(IDispatchable dispatchable, DispatchValue value)
        {
            if(_recorders.ContainsKey(dispatchable.Id))
            {
                foreach(var rec in _recorders[dispatchable.Id])
                {
                    rec.ValueChanged(value, dispatchable.Name);
                }
            }
        }


        internal void SaveValue(NodeInstance instance, double value, string source)
        {
            var trending = new Trending
            {
                This2NodeInstance = instance.ObjId,
                Value = value,
                Timestamp = DateTime.UtcNow,
                Source = source
            };
            trending.Source = source;
            try
            {
                Save(trending, instance);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error save trending value...");
            }
        }

        internal abstract void Save(Trending trend, NodeInstance nodeInstance);

        public Task RemoveAll()
        {
            _recorders.Clear();
            return Task.CompletedTask;
        }

        public async Task RemoveTrend(Guid nodeInstance)
        {
            var recorders = _recorders[nodeInstance];

            foreach (var rec in recorders)
            {
                await rec.Stop();
            }

            _recorders.Remove(nodeInstance);
        }

        public virtual async Task Start()
        {
            foreach(var ls in _recorders.Values)
            {
                foreach (var rec in ls)
                {
                    await rec.Start();
                }
            }
        }

        public virtual async Task Stop()
        {
            await RemoveAll();
        }
    }
}
