using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
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

        private void LogSlowQuery(DbCommand command, CommandExecutedEventData eventData)
        {
            if (eventData.Duration.TotalMilliseconds > 500)
            {
                _logger.LogWarning($"Query {command.CommandText} took {eventData.Duration.TotalMilliseconds}ms");
            }
        }

        public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
        {
            LogSlowQuery(command, eventData);
            return base.ReaderExecuted(command, eventData, result);
        }

        // EF calls the async overloads for async queries (ToListAsync, SaveChangesAsync, ...)
        public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            LogSlowQuery(command, eventData);
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
        {
            LogSlowQuery(command, eventData);
            return base.NonQueryExecuted(command, eventData, result);
        }

        public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result,
            CancellationToken cancellationToken = default)
        {
            LogSlowQuery(command, eventData);
            return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override object ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object result)
        {
            LogSlowQuery(command, eventData);
            return base.ScalarExecuted(command, eventData, result);
        }

        public override ValueTask<object> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object result,
            CancellationToken cancellationToken = default)
        {
            LogSlowQuery(command, eventData);
            return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
        }
    }
}
