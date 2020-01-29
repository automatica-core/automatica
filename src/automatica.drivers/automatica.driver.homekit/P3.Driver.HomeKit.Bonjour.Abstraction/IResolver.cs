using System.Threading;
using System.Threading.Tasks;

namespace P3.Driver.HomeKit.Bonjour.Abstraction
{
    /// <summary>
    ///   Answers a question.
    /// </summary>
    public interface IResolver
    {
        /// <summary>
        ///   Get an answer to a question.
        /// </summary>
        /// <param name="request">
        ///   A <see cref="Message"/> containing a <see cref="Question"/> that
        ///   needs to be answered.
        /// </param>
        /// <param name="cancel">
        ///   Is used to stop the task.  When cancelled, the <see cref="TaskCanceledException"/> is raised.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation. The task's value is
        ///   the <see cref="Message"/> response to the <paramref name="request"/>.
        /// </returns>
        Task<Message> ResolveAsync(
            Message request,
            CancellationToken cancel = default(CancellationToken)
            );
    }
}
