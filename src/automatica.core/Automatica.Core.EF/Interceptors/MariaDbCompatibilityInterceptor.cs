using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Automatica.Core.EF.Interceptors
{
    /// <summary>
    /// Oracle's MySQL provider only targets MySQL. MariaDB rejects some of its statements, they are rewritten here.
    /// </summary>
    internal class MariaDbCompatibilityInterceptor : DbCommandInterceptor
    {
        // the migrator waits for its lock with timeout -1 ("forever" on MySQL), MariaDB answers NULL and the migration crashes.
        // MariaDB gets its maximum timeout instead. The migrator reads the result as long, MariaDB returns INT (also for CAST AS SIGNED),
        // the BIGINT operand widens it
        private const string InfiniteLockTimeout = "GET_LOCK('__EFMigrationsLock',-1)";
        private const string MaxLockTimeout = "(GET_LOCK('__EFMigrationsLock',4294967) + 0 * 9223372036854775807)";

        private static void Rewrite(DbCommand command)
        {
            if (command.CommandText.Contains(InfiniteLockTimeout))
            {
                command.CommandText = command.CommandText.Replace(InfiniteLockTimeout, MaxLockTimeout);
            }
        }

        public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
        {
            Rewrite(command);
            return base.ScalarExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
        {
            Rewrite(command);
            return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            Rewrite(command);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Rewrite(command);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}
