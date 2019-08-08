using Automatica.Core.EF.Models;
using Automatica.Core.EF.Models.Trendings;
using Automatica.Core.Runtime.Abstraction;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.Runtime.Database
{
    public class DatabaseTrendingValueStore : ITrendingValueStore
    {
        private readonly AutomaticaContext _context;
        private readonly ILogger _logger;

        public DatabaseTrendingValueStore(AutomaticaContext context, ILogger logger)
        {
            _context = context;
            _logger = logger;
        }

        public void Add(Trending value)
        {
            _logger.LogInformation($"Save trend for {value.This2NodeInstance} with value {value.Value}...");
            _context.Add(value);
            _context.SaveChanges();
        }


    }
}
