using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Automatica.Core.EF.Interceptors
{
    internal class LogQueryTimeInterceptor : DbCommandInterceptor
    {
        private readonly ILogger _logger;

        public LogQueryTimeInterceptor(ILogger logger)
        {
            _logger = logger;
        }

        public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
        {
            if (eventData.Duration.TotalMilliseconds > 500)
            {
                _logger.LogWarning($"Query {command.CommandText} took {eventData.Duration.TotalMilliseconds}ms");
            }


            return base.ReaderExecuted(command, eventData, result);
        }
    }
}
