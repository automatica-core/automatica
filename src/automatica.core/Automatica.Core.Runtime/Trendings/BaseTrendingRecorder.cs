using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Trendings;
using Automatica.Core.Internals.Logger;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Automatica.Core.Runtime.Trendings
{
    internal abstract class BaseTrendingRecorder : ITrendingRecorder
    {
        private readonly IConfiguration config;

        private readonly Dictionary<Guid, List<TrendingValueRecorder>> _recorders = new Dictionary<Guid, List<TrendingValueRecorder>>();

        public BaseTrendingRecorder(string recorderName, IConfiguration config, IDispatcher dispatcher)
        {
            Logger = CoreLoggerFactory.GetLogger(recorderName);
            this.config = config;
            Dispatcher = dispatcher;
            DbContext = new AutomaticaContext(config);
        }

        public ILogger Logger { get; }
        public AutomaticaContext DbContext { get; }
        public IDispatcher Dispatcher { get; }

        public async Task AddTrend(Guid nodeInstance)
        {
            using (var dbContext = new AutomaticaContext(config))
            {
                var node = await dbContext.NodeInstances.SingleOrDefaultAsync(a => a.ObjId == nodeInstance);

                if(node == null)
                {
                    Logger.LogWarning($"Could not create recorder for {nodeInstance} - not found");
                    return;
                }

                if(!_recorders.ContainsKey(nodeInstance))
                {
                    _recorders.Add(nodeInstance, new List<TrendingValueRecorder>());
                }

                _recorders[nodeInstance].Add(new TrendingValueRecorder(node, this));
            }
            Dispatcher.RegisterDispatch(DispatchableType.NodeInstance, nodeInstance, DataCallback);
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
            var trending = new Trending();
            trending.This2NodeInstance = instance.ObjId;
            trending.Value = (double)value;
            trending.Timestamp = DateTime.UtcNow;
            trending.Source = source;
            trending.Source = source;

            Save(trending);
        }

        internal abstract void Save(Trending trend);

        public Task RemoveAll()
        {
            _recorders.Clear();
            return Task.CompletedTask;
        }

        public Task RemoveTrend(Guid nodeInstance)
        {
            var recorders = _recorders[nodeInstance];

            foreach (var rec in recorders)
            {
                rec.Stop();
            }

            _recorders.Remove(nodeInstance);
            return Task.CompletedTask;
        }

        public async Task Start()
        {
            foreach(var ls in _recorders.Values)
            {
                foreach (var rec in ls)
                {
                    await rec.Start();
                }
            }
        }

        public async Task Stop()
        {
            await RemoveAll();
        }
    }
}
