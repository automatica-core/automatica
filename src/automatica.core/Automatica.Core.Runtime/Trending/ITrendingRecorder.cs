using System;
using System.Threading.Tasks;

namespace Automatica.Core.Runtime.Trending
{
    public interface ITrendingRecorder
    {
        Task AddTrend(Guid nodeInstance);
        Task RemoveTrend(Guid nodeInstance);
        Task RemoveAll();
    }
}
