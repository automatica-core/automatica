﻿using System;
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

        internal BaseDataRecorderWriter(IConfiguration config, DataRecorderType recorderType, string recorderName, INodeInstanceCache nodeCache, IDispatcher dispatcher, ILoggerFactory factory)
        {
            Logger = factory.CreateLogger($"recording{LoggerConstants.FileSeparator}{recorderName}");
            _config = config;
            _nodeCache = nodeCache;
            RecorderType = recorderType;
            Dispatcher = dispatcher;
        }

        public ILogger Logger { get; }
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

            _recorders[nodeInstance].Add(new TrendingValueRecorder(node, this));

            await Dispatcher.RegisterDispatch(DispatchableType.NodeInstance, nodeInstance, DataCallback);
        }


        private void DataCallback(IDispatchable dispatchable, object value)
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
                SystemLogger.Instance.LogError(e, "Error save trending value...");
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
