using System;
using System.Threading.Tasks;

namespace Automatica.Core.Runtime.Trendings
{
    public interface ITrendingRecorder
    {
        Task Start();
        Task Stop();
        Task AddTrend(Guid nodeInstance);
        Task RemoveTrend(Guid nodeInstance);
        Task RemoveAll();
    }
}
