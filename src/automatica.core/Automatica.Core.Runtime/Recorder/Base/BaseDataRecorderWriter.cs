using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Automatica.Core.Base.IO;
using Automatica.Core.Base.Logger;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Trendings;
using Automatica.Core.Internals.Cache.Driver;
using Automatica.Core.Runtime.Recorder.Abstraction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

[assembly: InternalsVisibleTo("Automatica.Core.Tests")]

namespace Automatica.Core.Runtime.Recorder.Base
{
    internal abstract class BaseDataRecorderWriter : IDataRecorderWriter
    {
        private readonly IConfiguration _config;
        private readonly INodeInstanceCache _nodeCache;

        // _recorders, _registrations and _started are guarded by _lock, they are used from the dispatcher, the timers and the config api
        private readonly object _lock = new object();
        private readonly Dictionary<Guid, List<IDataRecorder>> _recorders = new Dictionary<Guid, List<IDataRecorder>>();
        private readonly Dictionary<Guid, IDisposable> _registrations = new Dictionary<Guid, IDisposable>();
        private bool _started;

        protected readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, string> MetricName = new();
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

        public async Task AddTrend(Guid nodeInstance, bool reload = true)
        {
            // reading a node from the database takes a dozen queries, at startup the node cache was just loaded
            var node = reload ? null : _nodeCache.Get(nodeInstance);
            if (node == null)
            {
                await using var db = new AutomaticaContext(_config);
                node = _nodeCache.GetSingle(nodeInstance, db);
            }

            if (node == null)
            {
                Logger.LogWarning($"Could not create recorderWriter for {nodeInstance} - not found");
                return;
            }

            MetricName.TryAdd(nodeInstance, GetFullNodeName(node));

            var recorder = new TrendingValueRecorder(node, this);
            List<IDataRecorder>? replaced;
            bool register;
            bool start;
            lock (_lock)
            {
                // one recorder per node, adding a node again replaces the recorder (e.g. changed trending settings)
                _recorders.TryGetValue(nodeInstance, out replaced);
                _recorders[nodeInstance] = new List<IDataRecorder> { recorder };
                register = !_registrations.ContainsKey(nodeInstance);
                start = _started;
            }

            if (replaced != null)
            {
                foreach (var rec in replaced)
                {
                    await rec.Stop();
                }
            }

            if (register)
            {
                var token = await Dispatcher.Subscribe(DispatchableType.NodeInstance, nodeInstance, DataCallback);
                lock (_lock)
                {
                    if (!_registrations.TryAdd(nodeInstance, token))
                    {
                        token.Dispose(); // added concurrently
                    }
                }
            }

            // added at runtime, the writer is already running
            if (start)
            {
                await recorder.Start();
            }
        }

        private string GetFullNodeName(NodeInstance nodeInstance)
        {
            var nameList = new List<string>();
            nameList.Add(nodeInstance.Name);
            GetFullNameRecursive(nodeInstance, ref nameList);
            nameList.Reverse();
            var name = string.Join("-", nameList);

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
        private Task DataCallback(IDispatchable dispatchable, DispatchValue value)
        {
            List<IDataRecorder>? recorder;
            lock (_lock)
            {
                if (!_recorders.TryGetValue(dispatchable.Id, out recorder))
                {
                    return Task.CompletedTask;
                }
            }

            foreach (var rec in recorder)
            {
                rec.ValueChanged(value, dispatchable.Name);
            }
            return Task.CompletedTask;
        }


        internal async Task SaveValue(NodeInstance instance, double value, string source)
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
                await Save(trending, instance);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error save trending value...{e}");
            }
        }

        internal abstract Task Save(Trending trend, NodeInstance nodeInstance);

        public async Task RemoveAll()
        {
            List<IDataRecorder> recorders;
            List<IDisposable> registrations;
            lock (_lock)
            {
                recorders = _recorders.Values.SelectMany(a => a).ToList();
                registrations = _registrations.Values.ToList();
                _recorders.Clear();
                _registrations.Clear();
            }

            // only our own registrations, links and other recorders of the nodes stay registered
            foreach (var registration in registrations)
            {
                registration.Dispose();
            }

            foreach (var rec in recorders)
            {
                await rec.Stop();
            }
        }

        public async Task RemoveTrend(Guid nodeInstance)
        {
            List<IDataRecorder>? recorders;
            IDisposable? registration;
            lock (_lock)
            {
                _recorders.Remove(nodeInstance, out recorders);
                _registrations.Remove(nodeInstance, out registration);
            }

            registration?.Dispose();

            if (recorders == null)
            {
                return;
            }

            foreach (var rec in recorders)
            {
                await rec.Stop();
            }
        }

        public virtual async Task Start()
        {
            List<IDataRecorder> recorders;
            lock (_lock)
            {
                _started = true;
                recorders = _recorders.Values.SelectMany(a => a).ToList();
            }

            foreach (var rec in recorders)
            {
                await rec.Start();
            }
        }

        public virtual async Task Stop()
        {
            lock (_lock)
            {
                _started = false;
            }
            await RemoveAll();
        }
    }
}
