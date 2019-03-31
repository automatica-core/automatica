using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models.Trendings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace Automatica.Core.Runtime.Trendings
{
    internal class DatabaseTrendingRecorder : BaseTrendingRecorder
    {
        public DatabaseTrendingRecorder(IConfiguration config, IDispatcher dispatcher) : base("DatabaseTrendingRecorder", config, dispatcher)
        {
        }

        internal override void Save(Trending trend)
        {
            Logger.LogInformation($"Save trend for {trend.This2NodeInstance} with value {trend.Value}...");
            DbContext.Trendings.Add(trend);
            try
            {
                DbContext.SaveChanges();
            }
            catch(Exception)
            {
                //ingore for now
            }

        }
    }
}
