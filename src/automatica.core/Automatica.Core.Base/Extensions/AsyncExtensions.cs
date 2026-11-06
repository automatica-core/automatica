using System;
using System.Threading;
using System.Threading.Tasks;

namespace Automatica.Core.Base.Extensions
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
        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            try
            {
                // rethrows the original exception of the task (not an AggregateException)
                return await task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested && !task.IsCompleted)
            {
                ObserveException(task);
                throw new OperationCanceledException(cancellationToken);
            }
        }

        public static async Task WithCancellation(this Task task, CancellationToken cancellationToken)
        {
            try
            {
                // propagates exceptions of the task, they were swallowed before
                await task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested && !task.IsCompleted)
            {
                ObserveException(task);
                throw new OperationCanceledException(cancellationToken);
            }
        }

        /// <summary>
        /// The abandoned task may still fail, observe it to not raise UnobservedTaskException
        /// </summary>
        private static void ObserveException(Task task)
        {
            task.ContinueWith(static t => _ = t.Exception, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
    }
}
