using Automatica.Core.EF.Models;
using Automatica.Core.Internals.Logger;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Automatica.Core.Runtime.Trending
{
    internal abstract class BaseTrendingRecorder : ITrendingRecorder
    {
        private readonly IConfiguration config;

        private readonly List<TrendingValueRecorder> _recorders = new List<TrendingValueRecorder>();

        public BaseTrendingRecorder(string recorderName, IConfiguration config)
        {
            Logger = CoreLoggerFactory.GetLogger(recorderName);
            this.config = config;
        }

        public ILogger Logger { get; }
        public AutomaticaContext DbContext { get; }

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

                _recorders.Add(new TrendingValueRecorder(node));
            }
        }

        public Task RemoveAll()
        {
            _recorders.Clear();
            return Task.CompletedTask;
        }

        public Task RemoveTrend(Guid nodeInstance)
        {
            var recorders = _recorders.Where(a => a.Instance.ObjId == nodeInstance);

            foreach (var rec in recorders)
            {
                rec.Stop();
                _recorders.Remove(rec);
            }

            return Task.CompletedTask;
        }
    }
}
