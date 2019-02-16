using System;
using System.Threading;
using System.Threading.Tasks;

namespace Automatica.Core.Driver.Utility
{
    public static class AsyncExtensions
    {
        /// <summary>
        /// Allows to create a cancelable <see cref="Task"/> 
        /// </summary>
        /// <typeparam name="T">Task type</typeparam>
        /// <param name="task">This Task param</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns></returns>
        [Obsolete("Use Automatica.Core.Base.Extensions.AsyncExtensions.WithCancellation<T> instead")]
        public static Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            return Base.Extensions.AsyncExtensions.WithCancellation<T>(task, cancellationToken);
        }
    }
}
