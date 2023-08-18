using Automatica.Core.Internals.Recorder;
using System.Threading;
using System.Threading.Tasks;

namespace Automatica.Core.Runtime.Recorder.Abstraction
{
    internal interface ITrendingContext : IRecorderContext
    {
        Task Configure(CancellationToken token = default);
        Task Start(CancellationToken token = default);

        Task Stop(CancellationToken token = default);

    }
}
