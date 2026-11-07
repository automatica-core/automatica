using System;
using System.Threading.Tasks;

namespace Automatica.Core.Runtime.Recorder
{
    internal interface IDataRecorderWriter
    {
        Task Start();
        Task Stop();
        /// <param name="nodeInstance">the node to record</param>
        /// <param name="reload">read the node from the database (its settings were just changed), false: the node cache is current</param>
        Task AddTrend(Guid nodeInstance, bool reload = true);
        Task RemoveTrend(Guid nodeInstance);
        Task RemoveAll();
    }
}
