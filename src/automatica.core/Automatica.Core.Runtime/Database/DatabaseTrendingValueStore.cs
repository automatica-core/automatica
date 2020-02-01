using System;
using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Trendings;
using Automatica.Core.Runtime.Abstraction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.Database
{
    public class DatabaseTrendingValueStore : ITrendingValueStore
    {
        private readonly IConfiguration _config;
        private readonly ILogger _logger;

        public DatabaseTrendingValueStore(IConfiguration config, ILogger logger)
        {
            _config = config;
            _logger = logger;
        }

        public void Add(Trending value, NodeInstance nodeInstance)
        {
            using var context = new AutomaticaContext(_config);
            _logger.LogInformation($"Save trend for {nodeInstance.Name} ({nodeInstance.ObjId}) with value {value.Value}...");
            context.Add(value);
            try
            {
                context.SaveChanges();
            }
            catch (Exception e)
            {

            }
        }


    }
}
